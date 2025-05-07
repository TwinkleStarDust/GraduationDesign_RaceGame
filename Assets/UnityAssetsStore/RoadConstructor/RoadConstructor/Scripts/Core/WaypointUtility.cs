// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    internal static class WaypointUtility
    {
        public static void CreateWaypoints(List<RoadObject> roadObjects, List<IntersectionObject> intersectionObjects,
            TrafficLaneType trafficLaneType, Vector2 maxDistance)
        {
            var sceneObjects = new List<SceneObject>();
            sceneObjects.AddRange(roadObjects);
            sceneObjects.AddRange(intersectionObjects);

            /********************************************************************************************************************************/
            // Destroying existing
            for (var i = 0; i < sceneObjects.Count; i++)
            {
                var trafficLanes = sceneObjects[i].GetTrafficLanes(trafficLaneType);

                for (var j = 0; j < trafficLanes.Count; j++)
                {
                    var waypoints = trafficLanes[j].GetWaypoints();
                    for (int k = 0; k < waypoints.Count; k++)
                    {
                        for (int l = 0; l < waypoints[k].prev.Count; l++) waypoints[k].prev[l].next.Remove(waypoints[k]);
                        for (int l = 0; l < waypoints[k].next.Count; l++) waypoints[k].next[l].prev.Remove(waypoints[k]);
                        
                        ObjectUtility.DestroyObject(waypoints[k].gameObject);
                    }
                    trafficLanes[j].waypoints.Clear();
                }
            }

            /********************************************************************************************************************************/
            // Adding adjacent from own road first
            for (var i = 0; i < sceneObjects.Count; i++)
            {
                var isIntersection = sceneObjects[i].GetType() == typeof(IntersectionObject);
                var isRoundabout = sceneObjects[i].IsRoundabout();
                var isRoad = sceneObjects[i].GetType() == typeof(RoadObject);

                var trafficLanes = sceneObjects[i].GetTrafficLanes(trafficLaneType);

                var spline = sceneObjects[i].splineContainer.Spline;
                
                for (var j = 0; j < trafficLanes.Count; j++)
                {
                    var laneWaypoints = new List<Waypoint>();
                    var trafficSpline = trafficLanes[j].spline;

                    var curvature = RoadSplineUtility.GetCurvature(trafficSpline.Knots.First(), trafficSpline.Knots.Last());
                    if (isIntersection || isRoundabout) curvature = float.MaxValue;
                    var t = Mathf.Clamp01(curvature / 90f);
                    var distance = Mathf.Lerp(maxDistance.y, maxDistance.x, t);
                    distance = math.max(0.01f, distance);

                    var totalWaypoints = Mathf.Max(Mathf.RoundToInt(trafficSpline.GetLength() / distance) - 1, 0);
                    totalWaypoints += 2; // +2 for the beginning and end points
                    
                    
                    for (var k = 0; k < totalWaypoints; k++)
                    {
                        if (totalWaypoints > 2 && (k == 0 || k == totalWaypoints - 1))
                        {
                            if (isRoad && !((RoadObject) sceneObjects[i]).snapPositionSet) continue;
                            if (k == totalWaypoints - 1 && isRoundabout) continue;
                        }
                        
                        var evaluate = (float) k / (totalWaypoints - 1);
                        if (isRoad && totalWaypoints == 2) evaluate = k == 0 ? 0.25f : 0.75f;
                        
                        // Left and right positions have wrong heights. Use middle spline height, identical to SplineMesh.cs.
                        var middlePosition = spline.EvaluatePosition(evaluate);
                        
                        if (trafficLanes[j].direction == TrafficLaneDirection.Backwards) evaluate = 1f - evaluate;
                        
                        var waypointPosition = trafficSpline.EvaluatePosition(evaluate);
                        waypointPosition.y = middlePosition.y;

                        var waypointObj = new GameObject();
                        var waypoint = waypointObj.AddComponent<Waypoint>();
                        {
                            waypoint.roadID = sceneObjects[i].name;
                            waypoint.laneType = trafficLanes[j].trafficLaneType;
                            waypoint.direction = trafficLanes[j].direction;
                            waypoint.laneWidth = trafficLanes[j].width;
                        }

                        laneWaypoints.Add(waypoint);
                        waypointObj.name = waypoint.laneType + "_"+ j + "_" + (laneWaypoints.Count - 1);
                        waypointObj.transform.position = waypointPosition;
                        waypointObj.transform.SetParent(sceneObjects[i].traffic.gameObject.transform);
                    }

                    if (trafficLanes[j].direction == TrafficLaneDirection.Backwards) laneWaypoints.Reverse();

                    for (var k = 0; k < laneWaypoints.Count; k++)
                    {
                        var waypoint = laneWaypoints[k];
                        if (k > 0) waypoint.prev.Add(laneWaypoints[k - 1]);
                        if (k < laneWaypoints.Count - 1) waypoint.next.Add(laneWaypoints[k + 1]);
                    }

                    if (!isRoundabout && laneWaypoints.Count > 0)
                    {
                        laneWaypoints[0].startPoint = true;
                        laneWaypoints[^1].endPoint = true;
                    }

                    if (isRoundabout && laneWaypoints.Count > 1)
                    {
                        laneWaypoints[0].prev.Add(laneWaypoints[^1]);
                        laneWaypoints[^1].next.Add(laneWaypoints[0]);
                    }

#if UNITY_EDITOR
                    for (int k = 0; k < laneWaypoints.Count; k++) EditorUtility.SetDirty(laneWaypoints[k]);    
#endif
                    
                    trafficLanes[j].SetWaypoints(laneWaypoints);
                }
            }
        }

        /********************************************************************************************************************************/
        // Now connecting waypoints of intersections and extended roads connecting to these roads
        public static void ConnectWaypoints<T>(List<T> sceneObjects, TrafficLaneType trafficLaneType) where T : SceneObject
        {
            var connectionDistance = trafficLaneType== TrafficLaneType.Car ? Constants.WaypointFindRangeCar : Constants.WaypointFindRangePedestrian;
            var connectionDistanceSq = connectionDistance * connectionDistance;

            for (var i = 0; i < sceneObjects.Count; i++)
            {
                var sceneObject = sceneObjects[i];
                
                var connections = new List<SceneObject>();
                
                if (sceneObject is IntersectionObject intersectionObject)
                {
                    connections.AddRange(intersectionObject.RoadConnections);
                }
                else if (sceneObject is RoadObject roadObject)
                {
                    connections.AddRange(roadObject.IntersectionConnections);
                    connections.AddRange(roadObject.RoadConnections);
                }
                
                if(connections.Count == 0) continue;
                

                var lanes = sceneObject.GetTrafficLanes(trafficLaneType);

                for (var j = 0; j < lanes.Count; j++)
                {
                    var roadWaypoints = lanes[j].GetWaypoints();
                    if (roadWaypoints.Count == 0) continue;
                    
                    var nearestKnotFirst = lanes[j].spline.Knots.First();
                    var nearestKnotLast = lanes[j].spline.Knots.Last();
                    
                    /********************************************************************************************************************************/
                    // Incoming (Prev)

                    var firstWaypoint = roadWaypoints[0];
                    
                    var closestConnectionIn = connections
                        .OrderBy(t => t.GetClosestDistanceSq(nearestKnotFirst.Position)).FirstOrDefault();

                    var intersectionType = IntersectionType.Intersection;
                    if (closestConnectionIn! is IntersectionObject) intersectionType = ((IntersectionObject) closestConnectionIn).intersectionType;
                    
                    var trafficLanesConnectionIn = closestConnectionIn!.GetTrafficLanes(trafficLaneType);
                    
                    ProcessIntersection(trafficLanesConnectionIn, firstWaypoint, true, intersectionType, nearestKnotFirst);

                    /********************************************************************************************************************************/
                    // Outgoing (Next)
                    var lastWaypoint = roadWaypoints[^1];
                    
                    var closestConnectionOut =
                        connections.OrderBy(t => t.GetClosestDistanceSq(nearestKnotLast.Position)).FirstOrDefault();

                    intersectionType = IntersectionType.Intersection;
                    if (closestConnectionOut! is IntersectionObject) intersectionType = ((IntersectionObject) closestConnectionOut).intersectionType;
                    
                    var trafficLanesConnectionOut = closestConnectionOut!.GetTrafficLanes(trafficLaneType);

                    ProcessIntersection(trafficLanesConnectionOut, lastWaypoint, false, intersectionType, nearestKnotLast);
                }
            }

            return;


            void ProcessIntersection(List<TrafficLane> trafficLanesConnection, Waypoint waypoint, bool incoming,
                IntersectionType intersectionType, BezierKnot endKnot)
            {
                if (trafficLanesConnection.Count == 0) return;

                /********************************************************************************************************************************/
                if (intersectionType == IntersectionType.Intersection)
                {
                    var connectionWaypointPositions = new Vector3[trafficLanesConnection.Count];
                    for (var k = 0; k < trafficLanesConnection.Count; k++)
                    {
                        var connectionWaypoints = trafficLanesConnection[k].GetWaypoints();

                        if (connectionWaypoints.Count == 0) return;

                        if (incoming) connectionWaypointPositions[k] = connectionWaypoints[^1].transform.position;
                        else connectionWaypointPositions[k] = connectionWaypoints[0].transform.position;
                    }

                    var closestIndexIn = 0;
                    var closestDistance = float.MaxValue;
                    for (var k = 0; k < connectionWaypointPositions.Length; k++)
                    {
                        var distanceSq = math.distancesq(connectionWaypointPositions[k], endKnot.Position);
                        if (distanceSq < closestDistance)
                        {
                            closestIndexIn = k;
                            closestDistance = distanceSq;
                        }

                        if (distanceSq <= connectionDistanceSq) // Add all for which distance fits
                        {
                            var trafficLaneConnection = trafficLanesConnection[k];
                            AddWaypoints(trafficLaneConnection);
                        }
                    }

                    // Adding at least one
                    var closestTrafficLane = trafficLanesConnection[closestIndexIn];
                    AddWaypoints(closestTrafficLane);

                    void AddWaypoints(TrafficLane trafficLaneConnection)
                    {
                        var _waypointsConnection = trafficLaneConnection.GetWaypoints();
                        if (incoming)
                        {
                            var _closestWaypoint = _waypointsConnection[^1];
                            if (!_closestWaypoint.next.Contains(waypoint)) _closestWaypoint.next.Add(waypoint);
                            if (!waypoint.prev.Contains(_closestWaypoint)) waypoint.prev.Add(_closestWaypoint);
#if UNITY_EDITOR
                            EditorUtility.SetDirty(waypoint);
                            EditorUtility.SetDirty(_closestWaypoint);
#endif
                        }
                        else
                        {
                            var _closestWaypoint = _waypointsConnection[0];
                            if (!_closestWaypoint.prev.Contains(waypoint)) _closestWaypoint.prev.Add(waypoint);
                            if (!waypoint.next.Contains(_closestWaypoint)) waypoint.next.Add(_closestWaypoint);
#if UNITY_EDITOR
                            EditorUtility.SetDirty(waypoint);
                            EditorUtility.SetDirty(_closestWaypoint);
#endif
                        }
                    }
                }
                /********************************************************************************************************************************/
                else if (intersectionType == IntersectionType.Roundabout)
                {
                    for (var k = 0; k < trafficLanesConnection.Count; k++)
                    {
                        var trafficLaneConnection = trafficLanesConnection[k];
                        
                        var connectionWaypoints = new List<Waypoint>(trafficLaneConnection.GetWaypoints());
                        if(connectionWaypoints.Count == 0) continue;
                        
                        if (trafficLaneConnection.direction == TrafficLaneDirection.Forward)
                            connectionWaypoints.Reverse();
                        
                        var closestWaypoint = trafficLaneConnection.GetNearestWaypoint(waypoint.transform.position);
                        
                        if (incoming)
                        {
                            if (!waypoint.prev.Contains(closestWaypoint)) waypoint.prev.Add(closestWaypoint);
                            if (!closestWaypoint.next.Contains(waypoint)) closestWaypoint.next.Add(waypoint);
                        }
                        else
                        {
                            if (!waypoint.next.Contains(closestWaypoint)) waypoint.next.Add(closestWaypoint);
                            if (!closestWaypoint.prev.Contains(waypoint)) closestWaypoint.prev.Add(waypoint);
                        }
                        
#if UNITY_EDITOR
                        EditorUtility.SetDirty(waypoint);
                        EditorUtility.SetDirty(closestWaypoint);
#endif
                    }
                }
            }
        }


        /********************************************************************************************************************************/

        // Removing waypoints of intersections connecting to these roads
        public static void RemoveConnectingWaypoints(List<RoadObject> roads, TrafficLaneType trafficLaneType)
        {
            for (var i = 0; i < roads.Count; i++)
            {
                var trafficLanes = roads[i].GetTrafficLanes(trafficLaneType);
                for (int j = 0; j < trafficLanes.Count; j++)
                {
                    var waypoints = trafficLanes[j].GetWaypoints();
                    if(waypoints.Count == 0) continue;
                    
                    // Previous
                    for (var l = waypoints[0].prev.Count - 1; l >= 0; l--)
                    {
                        var prevWaypoint = waypoints[0].prev[l];
                        prevWaypoint.next.Remove(waypoints[0]);
#if UNITY_EDITOR
                        if(prevWaypoint != null) EditorUtility.SetDirty(prevWaypoint);
#endif
                    }
                    
                    // Next
                    for (var l = waypoints[^1].next.Count - 1; l >= 0; l--)
                    {
                        var nextWaypoint = waypoints[^1].next[l];
                        nextWaypoint.prev.Remove(waypoints[^1]);
#if UNITY_EDITOR
                        if(nextWaypoint != null) EditorUtility.SetDirty(nextWaypoint);
#endif
                    }
                }
                
            }
        }
    }
}