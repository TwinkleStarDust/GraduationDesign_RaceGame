// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using PampelGames.Shared.Utility;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    internal static class TrafficUtility
    {
        /// <summary>
        ///     Roads must be added first!
        /// </summary>
        public static void AddTrafficComponent(ComponentSettings settings, SceneObject sceneObject, bool checkSettings)
        {
            if (checkSettings && !settings.addTrafficComponent) return;

            var trafficLanes = sceneObject.roadDescr.trafficLanes;
            if (trafficLanes.Count == 0) return;

            var splineContainerSplines = sceneObject.splineContainer.Splines.ToList();
            if (splineContainerSplines.Count == 0) return;

            if (sceneObject.traffic != null)
            {
                if (sceneObject.GetType() == typeof(RoundaboutObject)) return;
                ObjectUtility.DestroyObject(sceneObject.traffic.gameObject);
            }

            var trafficObj = new GameObject("Traffic");
            trafficObj.transform.SetParent(sceneObject.transform);
            trafficObj.transform.SetAsFirstSibling();
            var traffic = trafficObj.AddComponent<Traffic>();
            sceneObject.traffic = traffic;
            var trafficSplineContainer = trafficObj.AddComponent<SplineContainer>();
            trafficSplineContainer.RemoveSpline(trafficSplineContainer.Spline);
            traffic.splineContainer = trafficSplineContainer;
            traffic.sceneObject = sceneObject;

            var splines = new List<Spline>();

            if (sceneObject.GetType() == typeof(RoundaboutObject))
            {
                splines.Add(splineContainerSplines[0]);
            }
            else
            {
                splines.AddRange(splineContainerSplines);

                if (sceneObject.GetType() == typeof(IntersectionObject) &&
                    ((IntersectionObject) sceneObject).RoadConnections.Count > 2)
                    // Add each spline again inverted.
                    for (var i = 0; i < splineContainerSplines.Count; i++)
                    {
                        var newSpline = new Spline(splineContainerSplines[i]);
                        RoadSplineUtility.InvertSpline(newSpline);
                        splines.Add(newSpline);
                    }
            }

            CreateTrafficLanes(settings, trafficLanes, trafficSplineContainer, splines, sceneObject, traffic);
        }

        private class Connection
        {
            public BezierKnot nearestKnot;
            public float3 tangentIn;
            
            public List<TrafficLane> trafficLanesIn;
            public List<BezierKnot> nearestLaneKnotsIn;

            public List<TrafficLane> trafficLanesOut;
            public List<BezierKnot> nearestLaneKnotsOut;

            public float angleOut;
        }
        private static void CreateTrafficLanes(ComponentSettings settings, List<TrafficLaneEditor> trafficLanesEditor,
            SplineContainer trafficSplineContainer, List<Spline> splines, SceneObject sceneObject, Traffic traffic)
        {
            var isRoad = sceneObject.IsRoad();
            var isDirectConnection = sceneObject.IsDirectConnection();
            var isIntersection = sceneObject.IsIntersection();
            var isEndObject = sceneObject.IsEndObject();
            var isRoundabout = sceneObject.IsRoundabout();

            /********************************************************************************************************************************/
            // Roads and Roundabouts (Car)
            for (var i = 0; i < splines.Count; i++)
            for (var j = 0; j < trafficLanesEditor.Count; j++)
            {
                var trafficLaneEditor = trafficLanesEditor[j];
                if (isIntersection && trafficLaneEditor.trafficLaneType == TrafficLaneType.Pedestrian) continue;
                if (isIntersection && trafficLaneEditor.direction == TrafficLaneDirection.Backwards) continue;
                if (isEndObject && trafficLaneEditor.direction == TrafficLaneDirection.Backwards) continue;
                
                Spline newSpline = default;
                var offsetX = trafficLaneEditor.position - sceneObject.roadDescr.width * 0.5f;

                if (isRoad || isEndObject)
                {
                    newSpline = new Spline(splines[i]);
                    
                    RoadSplineUtility.OffsetSplineX(newSpline, offsetX);
                    
                    // End Object -> Rounded path
                    if (isEndObject && !sceneObject.road.oneWay)
                    {
                        var endObject = (IntersectionObject) sceneObject;
                        var centerPosition = endObject.centerPosition;

                        var knot01 = newSpline.Knots.First();
                        var tangent01 = math.normalizesafe(knot01.TangentOut);
                        tangent01.y = 0f;
                        var tangent01perp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(tangent01);
                        var centerDirection = (float3) centerPosition - knot01.Position;
                        if (!PGTrigonometryUtility.IsSameDirectionXZ(tangent01perp, centerDirection)) tangent01perp *= -1f;
                        var centerDistance = math.distance(knot01.Position, centerPosition);
                        var knot02 = newSpline.Knots.Last();
                        knot02.Position = knot01.Position + tangent01perp * centerDistance * 2f;
                        var tangentLength = RoadSplineUtility.GetUnitCircleTangentLength(centerDistance);
                        tangent01 *= tangentLength;
                        knot01.TangentOut = tangent01;
                        knot01.TangentIn = tangent01;
                        knot02.TangentOut = tangent01;
                        knot02.TangentIn = tangent01;

                        newSpline.SetKnot(0, knot01);
                        newSpline.SetKnot(newSpline.Count - 1, knot02);
                    }
                }
                else if (isRoundabout)
                {
                    var roundaboutObject = (RoundaboutObject) sceneObject;
                    var radius = roundaboutObject.radius + offsetX;
                    newSpline = SplineCircle.CreateCircleSpline(radius, roundaboutObject.centerPosition, quaternion.identity, true);
                }
                else if (isDirectConnection)
                {
                    newSpline = new Spline(splines[i]);
                    RoadSplineUtility.OffsetSplineX(newSpline, offsetX);

                    var intersection = (IntersectionObject) sceneObject;
                    var knot01 = newSpline.Knots.First();
                    var closestRoadObject01 = GetClosestRoadObject(intersection, knot01);
                    var closestTraffic01 = closestRoadObject01.traffic;
                    if (!GetClosestTrafficKnot(closestTraffic01, knot01, trafficLaneEditor.trafficLaneType, out var nearestTrafficKnot01)) continue;
                    knot01.Position = nearestTrafficKnot01.Position;
                    newSpline.SetKnot(0, knot01);

                    var knot02 = newSpline.Knots.Last();
                    var closestRoadObject02 = GetClosestRoadObject(intersection, knot02);
                    var closestTraffic02 = closestRoadObject02.traffic;
                    if (!GetClosestTrafficKnot(closestTraffic02, knot02, trafficLaneEditor.trafficLaneType, out var nearestTrafficKnot02)) continue;

                    knot02.Position = nearestTrafficKnot02.Position;
                    newSpline.SetKnot(newSpline.Count - 1, knot02);
                }
                else // Intersections handled below
                {
                    continue;
                }

                if (trafficLaneEditor.direction == TrafficLaneDirection.Backwards) RoadSplineUtility.InvertSpline(newSpline);

                trafficSplineContainer.AddSpline(newSpline);
                var trafficLane = new TrafficLane(trafficLaneEditor, newSpline, false);
                traffic.trafficLanes.Add(trafficLane);
            }
    
            /********************************************************************************************************************************/
            /********************************************************************************************************************************/
            // Intersections -> Connecting the roads directly
            if (isIntersection)
            {
                var intersection = (IntersectionObject) sceneObject;

                var connectionsCar = new List<Connection>();
                var connectionsPedestrian = new List<Connection>();

                /********************************************************************************************************************************/
                // Creating lists for calculation first
                for (var i = 0; i < intersection.RoadConnections.Count; i++)
                {
                    var road = intersection.RoadConnections[i];
                    var nearestKnotIndex = RoadSplineUtility.GetNearestKnotIndex(road.splineContainer.Spline, intersection.centerPosition);
                    var nearestKnot = road.splineContainer.Spline.Knots.ElementAt(nearestKnotIndex);

                    var roadTraffic = intersection.RoadConnections[i].traffic;
                    var roadTrafficLanesCar = new List<TrafficLane>();
                    var roadTrafficLanesPedestrian = new List<TrafficLane>();
                    for (var j = 0; j < roadTraffic.trafficLanes.Count; j++)
                    {
                        var lane = roadTraffic.trafficLanes[j];
                        if (lane.trafficLaneType == TrafficLaneType.Car) roadTrafficLanesCar.Add(lane);
                        else if (lane.trafficLaneType == TrafficLaneType.Pedestrian) roadTrafficLanesPedestrian.Add(lane);
                    }

                    roadTrafficLanesCar = roadTrafficLanesCar.OrderBy(lane => lane.position).ToList();
                    roadTrafficLanesPedestrian = roadTrafficLanesPedestrian.OrderBy(lane => lane.position).ToList();
                    
                    var tangentIn =
                        PGTrigonometryUtility.DirectionalTangentToPointXZ(intersection.centerPosition, nearestKnot.Position, nearestKnot.TangentOut);

                    var carLanesIn = CreateLanes(roadTrafficLanesCar, true);
                    var carLanesOut = CreateLanes(roadTrafficLanesCar, false);
                    var pedestrianLanesIn = CreateLanes(roadTrafficLanesPedestrian, true);
                    var pedestrianLanesOut = CreateLanes(roadTrafficLanesPedestrian, false);
                    
                    connectionsCar.Add(new Connection
                    {
                        nearestKnot = nearestKnot,
                        tangentIn = tangentIn,
                        trafficLanesIn = carLanesIn,
                        trafficLanesOut = carLanesOut,
                        nearestLaneKnotsIn = CreateNearestLaneKnots(carLanesIn),
                        nearestLaneKnotsOut = CreateNearestLaneKnots(carLanesOut),
                    });
                    connectionsPedestrian.Add(new Connection
                    {
                        nearestKnot = nearestKnot,
                        tangentIn = tangentIn,
                        trafficLanesIn = pedestrianLanesIn,
                        trafficLanesOut = pedestrianLanesOut,
                        nearestLaneKnotsIn = CreateNearestLaneKnots(pedestrianLanesIn),
                        nearestLaneKnotsOut = CreateNearestLaneKnots(pedestrianLanesOut),
                    });
                    
                    continue;

                    List<TrafficLane> CreateLanes(List<TrafficLane> trafficLanes, bool directionIn)
                    {
                        var lanes = new List<TrafficLane>(trafficLanes);
                        for (var j = lanes.Count - 1; j >= 0; j--)
                        {
                            var nearestLaneKnotIndexIn = RoadSplineUtility.GetNearestKnotIndex(lanes[j].spline, intersection.centerPosition);
                            if (directionIn && nearestLaneKnotIndexIn != 0) continue;
                            if (!directionIn && nearestLaneKnotIndexIn == 0) continue;
                            lanes.RemoveAt(j);
                        }
                        
                        lanes = lanes.OrderBy(lane => lane.position).ToList(); // Order from left to right
                        if (nearestKnotIndex == 0) lanes.Reverse();

                        return lanes;
                    }

                    List<BezierKnot> CreateNearestLaneKnots(List<TrafficLane> trafficLanes)
                    {
                        var knots = new List<BezierKnot>();
                        for (int j = 0; j < trafficLanes.Count; j++)
                        {
                            var nearestLaneKnotIndexIn = RoadSplineUtility.GetNearestKnotIndex(trafficLanes[j].spline, intersection.centerPosition);
                            var nearestLaneKnotIn = trafficLanes[j].spline.Knots.ElementAt(nearestLaneKnotIndexIn);
                            knots.Add(nearestLaneKnotIn);
                        }

                        return knots;
                    }
                }
                
                

                /********************************************************************************************************************************/
                // Intersections (Cars)
                
                for (var i = 0; i < connectionsCar.Count; i++)
                {
                    var connectionIn = connectionsCar[i];
                    
                    var roadLanesIn = connectionIn.trafficLanesIn;
                    var tangentIn = connectionIn.tangentIn;

                    if(roadLanesIn.Count == 0) continue;
                    
                    var connectionsCarOut = new List<Connection>();
                    for (int k = 0; k < connectionsCar.Count; k++)
                    {
                        if(i == k) continue;
                        var tangentOut = connectionsCar[k].tangentIn * -1f;
                        connectionsCar[k].angleOut = math.degrees(PGTrigonometryUtility.AngleXZ(tangentIn, tangentOut));
                        connectionsCarOut.Add(connectionsCar[k]);
                    }
                    connectionsCarOut = connectionsCarOut.OrderBy(connection => connection.angleOut).ToList();

                    if(connectionsCarOut.Count == 0) continue;

                    if (roadLanesIn.Count == 1)
                    {
                        for (int j = 0; j < connectionsCarOut.Count; j++)
                        {
                            CreateTrafficLanesPrivate(connectionIn, 0, connectionsCarOut[j], true);        
                        }
                    }
                    else
                    {
                        bool leftConnected = false;
                        bool rightConnected = false;
                        var angleOutLeft = connectionsCarOut[0].angleOut;
                        var angleOutRight = connectionsCarOut[^1].angleOut;
                        if(roadLanesIn.Count < 4 && Constants.LeftAngle(angleOutLeft))
                        {
                            CreateTrafficLanesPrivate(connectionIn, 0, connectionsCarOut[0], true);
                            leftConnected = true;
                        }
                        if(roadLanesIn.Count < 3 && Constants.RightAngle(angleOutRight))
                        {
                            CreateTrafficLanesPrivate(connectionIn, roadLanesIn.Count - 1, connectionsCarOut[^1], true);
                            rightConnected = true;
                        }
                        
                        for (int j = 0; j < roadLanesIn.Count; j++)
                        {
                            var left = j < roadLanesIn.Count / 2;
                            
                            for (int k = 0; k < connectionsCarOut.Count; k++)
                            {
                                if(leftConnected && j == 0 && k == 0) continue;
                                if(rightConnected && j == roadLanesIn.Count - 1 && k == connectionsCarOut.Count - 1) continue;
                                
                                var angleOut = connectionsCarOut[k].angleOut;
                                var leftAngle = Constants.LeftAngle(angleOut);
                                var rightAngle = Constants.RightAngle(angleOut);
                                
                                if(left && rightAngle) continue;
                                if(!left && leftAngle) continue;
                                
                                CreateTrafficLanesPrivate(connectionIn, j, connectionsCarOut[k], false);
                            }    
                        }
                    }
                }
                
                void CreateTrafficLanesPrivate(Connection connectionIn, int laneIndexIn, Connection connectionOut, bool connectAll)
                {
                    var roadLaneIn = connectionIn.trafficLanesIn[laneIndexIn];
                    var nearestLaneKnotIn = connectionIn.nearestLaneKnotsIn[laneIndexIn];
                    var laneKnotsOut = connectionOut.nearestLaneKnotsOut;
                    
                    if (connectAll)
                    {
                        for (int i = 0; i < laneKnotsOut.Count; i++)
                        {
                            AddLane(i);
                        }
                    }
                    else // Getting the opposite index
                    {
                        int knotIndexOut;
                        if (laneIndexIn == 0) knotIndexOut = laneKnotsOut.Count - 1;
                        else if (laneIndexIn == connectionIn.trafficLanesIn.Count - 1) knotIndexOut = 0;
                        else knotIndexOut = laneKnotsOut.Count - 1 - laneIndexIn;
                        knotIndexOut = Mathf.Clamp(knotIndexOut, 0, laneKnotsOut.Count - 1);
                        
                        AddLane(knotIndexOut);
                    }
                    
                    void AddLane(int index)
                    {
                        index = Mathf.Clamp(index, 0, laneKnotsOut.Count - 1);
                        if (laneKnotsOut.Count == 0) return;
                        var nearestLaneKnotOut = laneKnotsOut[index];
                        
                        TangentCalculation.CalculateTangents(settings.smoothSlope, Constants.TangentLengthIntersection, 
                            nearestLaneKnotIn.Position, nearestLaneKnotIn.TangentOut,
                            nearestLaneKnotOut.Position, nearestLaneKnotOut.TangentIn, true, intersection.centerPosition,
                            out var tangentIn, out var tangentOut);

                        nearestLaneKnotIn.TangentOut = tangentIn;
                        nearestLaneKnotOut.TangentIn = tangentOut;

                        var newSpline = new Spline
                        {
                            {nearestLaneKnotIn, TangentMode.Broken},
                            {nearestLaneKnotOut, TangentMode.Broken}
                        };
                        
                        trafficSplineContainer.AddSpline(newSpline);
                        var trafficLane = new TrafficLane(roadLaneIn, newSpline, false);
                        traffic.trafficLanes.Add(trafficLane);
                    }
                }
                

                /********************************************************************************************************************************/
                // Intersections (Pedestrians)

                var crossingSplines = new List<Spline>();

                for (var i = 0; i < connectionsPedestrian.Count; i++)
                {
                    var connectionIn = connectionsPedestrian[i];
                    var pedestrianLanesIn = connectionIn.trafficLanesIn;
                    
                    if(pedestrianLanesIn.Count == 0)
                    {
                        crossingSplines.Add(null);
                        continue;
                    }

                    var roadConnection = intersection.RoadConnections[i];
                    var intersectionDist = settings.intersectionDistance * 0.5f;
                    

                    // Crossing
                    var crossingPositions = new List<Vector3>();
                    for (var j = 0; j < pedestrianLanesIn.Count; j++)
                    {
                        var nearestLaneKnotIndexIn =
                            RoadSplineUtility.GetNearestKnotIndex(pedestrianLanesIn[j].spline, intersection.centerPosition);
                        var nearestKnot = pedestrianLanesIn[j].spline.Knots.ElementAt(nearestLaneKnotIndexIn);

                        var nearestTangent = math.normalizesafe(nearestKnot.TangentOut);
                        nearestTangent.y = 0f;
                        nearestTangent = PGTrigonometryUtility.DirectionalTangentToPointXZ(
                            intersection.centerPosition, nearestKnot.Position, nearestTangent);
                        var middlePos = nearestKnot.Position + nearestTangent * intersectionDist;

                        crossingPositions.Add(middlePos);
                    }

                    if (crossingPositions.Count < 2)
                    {
                        crossingSplines.Add(null);
                        continue;
                    }

                    var knots = new List<BezierKnot>();

                    for (var j = 0; j < crossingPositions.Count - 1; j++)
                    {
                        var pos01 = crossingPositions[j];
                        var pos02 = crossingPositions[j + 1];
                        var tanOut = (pos02 - pos01) * 0.5f;
                        var tanIn = (pos01 - pos02) * 0.5f;

                        var knot = new BezierKnot
                        {
                            Position = pos01,
                            Rotation = quaternion.identity,
                            TangentOut = tanOut,
                            TangentIn = tanIn
                        };

                        knots.Add(knot);

                        if (j == crossingPositions.Count - 2)
                        {
                            var knot02 = new BezierKnot
                            {
                                Position = pos02,
                                Rotation = quaternion.identity,
                                TangentOut = tanOut,
                                TangentIn = tanIn
                            };

                            knots.Add(knot02);
                        }
                    }

                    var newSpline = new Spline(knots);
                    newSpline.SetTangentMode(TangentMode.Broken);

                    trafficSplineContainer.AddSpline(newSpline);
                    var trafficLane = new TrafficLane(pedestrianLanesIn[0], newSpline, true);
                    traffic.trafficLanes.Add(trafficLane);
                    crossingSplines.Add(newSpline);
                    
                    knots.Reverse();
                    for (var j = 0; j < knots.Count; j++)
                    {
                        var knot = knots[j];
                        knot.TangentIn *= -1f;
                        knot.TangentOut *= -1f;
                        knots[j] = knot;
                    }

                    var newSplineReversed = new Spline(knots);
                    newSplineReversed.SetTangentMode(TangentMode.Broken);

                    trafficSplineContainer.AddSpline(newSplineReversed);
                    var trafficLaneReversed = new TrafficLane(pedestrianLanesIn[0], newSplineReversed, true);
                    trafficLaneReversed.direction = trafficLaneReversed.direction == TrafficLaneDirection.Backwards
                        ? TrafficLaneDirection.Forward : TrafficLaneDirection.Backwards;
                    traffic.trafficLanes.Add(trafficLaneReversed);
                }

                // Connect each crossing with the next clockwise

                for (var i = 0; i < connectionsPedestrian.Count; i++)
                {
                    var connectionPedestrian = connectionsPedestrian[i];
                    if (connectionPedestrian.trafficLanesIn.Count == 0) continue;

                    var knot01 = connectionPedestrian.nearestKnot;
                    var tangentOut01 = connectionPedestrian.tangentIn * -1f;

                    var lowestClockwiseAngle = float.MaxValue;
                    var sideKnotIndex = 0;
                    var sideTangent = float3.zero;
                    for (var j = 0; j < connectionsPedestrian.Count; j++)
                    {
                        if (j == i) continue;
                        
                        var tan = connectionsPedestrian[j].nearestKnot.Position - knot01.Position;
                        var angleRad = PGTrigonometryUtility.AngleClockwiseXZ(tangentOut01, tan);
                        
                        if (angleRad < lowestClockwiseAngle)
                        {
                            lowestClockwiseAngle = angleRad;
                            sideKnotIndex = j;
                            sideTangent = connectionsPedestrian[j].tangentIn;
                        }
                    }

                    var tangentOut01Perp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(tangentOut01);
                    var tangentOut01PerpPos = knot01.Position + tangentOut01Perp;
                    var crossingSpline01 = crossingSplines[i];
                    var crossingSpline02 = crossingSplines[sideKnotIndex];

                    if(crossingSpline01 == null || crossingSpline02 == null) continue;
                    
                    var connectionPedestrian02 = connectionsPedestrian[sideKnotIndex];
                    var knot02 = connectionPedestrian02.nearestKnot;
                    var tangentOut02 = connectionPedestrian02.tangentIn * -1f;
                    var tangentOut02Perp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(tangentOut02);
                    var tangentOut02PerpPos = knot02.Position - tangentOut02Perp; 

                    var nearestIndex01 = RoadSplineUtility.GetNearestKnotIndex(crossingSpline01, tangentOut01PerpPos);
                    var crossSpline01Knot = crossingSpline01.Knots.ElementAt(nearestIndex01);
                    
                    var nearestIndex02 = RoadSplineUtility.GetNearestKnotIndex(crossingSpline02, tangentOut02PerpPos);
                    var crossSpline02Knot = crossingSpline02.Knots.ElementAt(nearestIndex02);
                    
                    var center = (crossSpline01Knot.Position + crossSpline02Knot.Position) / 2f;
                    TangentCalculation.CalculateTangents(settings.smoothSlope, settings.tangentLength,  
                        crossSpline01Knot.Position, connectionPedestrian.tangentIn, crossSpline02Knot.Position, sideTangent, 
                        true, center,
                        out var tangent01, out var tangent02);

                    crossSpline01Knot.TangentIn = tangent01;
                    crossSpline01Knot.TangentOut = tangent01;
                    crossSpline02Knot.TangentIn = tangent02;
                    crossSpline02Knot.TangentOut = tangent02;
                    
                    var newSpline = new Spline
                    {
                        {crossSpline01Knot, TangentMode.Broken},
                        {crossSpline02Knot, TangentMode.Broken}
                    };
                    
                    trafficSplineContainer.AddSpline(newSpline);
                    var trafficLane = new TrafficLane(connectionPedestrian.trafficLanesIn[0], newSpline, false);
                    traffic.trafficLanes.Add(trafficLane);

                    var newSplineReversed = new Spline(newSpline);
                    RoadSplineUtility.InvertSpline(newSplineReversed);
                    var trafficLaneReversed = new TrafficLane(connectionPedestrian.trafficLanesIn[0], newSplineReversed, false);
                    trafficSplineContainer.AddSpline(newSplineReversed);
                    trafficLaneReversed.direction = trafficLaneReversed.direction == TrafficLaneDirection.Backwards
                        ? TrafficLaneDirection.Forward : TrafficLaneDirection.Backwards;
                    traffic.trafficLanes.Add(trafficLaneReversed);
                }
            }
        }

        /********************************************************************************************************************************/

        public static List<TrafficLaneEditor> GetTrafficLanesEditor(List<TrafficLanePreset> trafficLanePresets, RoadDescr roadDescr)
        {
            var road = roadDescr.road;
            var trafficLanesEditor = new List<TrafficLaneEditor>();
            trafficLanesEditor.AddRange(road.trafficLanes);
            var centerPosX = roadDescr.width * 0.5f;

            /********************************************************************************************************************************/
            // Mirror
            for (var i = 0; i < trafficLanePresets.Count; i++)
            {
                var preset = trafficLanePresets[i];
                if (road.category != preset.category) continue;
                trafficLanesEditor.AddRange(preset.trafficLanes);

                for (var j = 0; j < preset.trafficLanes.Count; j++)
                {
                    var lane = preset.trafficLanes[j];

                    if (!lane.mirror) continue;
                    if (Mathf.Approximately(centerPosX, lane.position)) continue;

                    var laneCopy = PGClassUtility.CopyClass(lane) as TrafficLaneEditor;
                    if (lane.direction == TrafficLaneDirection.Forward) laneCopy.direction = TrafficLaneDirection.Backwards;
                    else if (lane.direction == TrafficLaneDirection.Backwards) laneCopy.direction = TrafficLaneDirection.Forward;
                    var dif = math.abs(centerPosX - lane.position);
                    if (lane.position < centerPosX) laneCopy.position = centerPosX + dif;
                    else laneCopy.position = centerPosX - dif;
                    trafficLanesEditor.Add(laneCopy);
                }
            }

            /********************************************************************************************************************************/
            // Direction -> Both
            var count = trafficLanesEditor.Count;
            for (var i = 0; i < count; i++)
            {
                var lane = trafficLanesEditor[i];

                if (lane.direction != TrafficLaneDirection.Both) continue;

                var laneCopy = PGClassUtility.CopyClass(lane) as TrafficLaneEditor;
                laneCopy.direction = TrafficLaneDirection.Backwards;
                trafficLanesEditor.Add(laneCopy);
            }

            return trafficLanesEditor;
        }

        /********************************************************************************************************************************/
        /********************************************************************************************************************************/

        private static RoadObject GetClosestRoadObject(IntersectionObject intersection, BezierKnot knot)
        {
            var nearestIndex01 = 0;
            var closestDistance = float.MaxValue;
            for (var k = 0; k < intersection.RoadConnections.Count; k++)
            {
                var roadSpline = intersection.RoadConnections[k].splineContainer.Spline;
                var nearestKnotIndex = RoadSplineUtility.GetNearestKnotIndex(roadSpline, knot.Position);
                var nearestKnot = roadSpline.Knots.ElementAt(nearestKnotIndex);
                var distance = math.distancesq(knot.Position, nearestKnot.Position);
                if (distance < closestDistance)
                {
                    nearestIndex01 = k;
                    closestDistance = distance;
                }
            }

            return intersection.RoadConnections[nearestIndex01];
        }

        private static bool GetClosestTrafficKnot(Traffic traffic, BezierKnot knot, TrafficLaneType trafficLaneType,
            out BezierKnot nearestTrafficKnot)
        {
            nearestTrafficKnot = default;
            var nearestFound = false;

            var maxDistance = float.MaxValue;
            for (var i = 0; i < traffic.trafficLanes.Count; i++)
            {
                if (traffic.trafficLanes[i].trafficLaneType != trafficLaneType) continue;
                var nearestKnotIndex = RoadSplineUtility.GetNearestKnotIndex(traffic.trafficLanes[i].spline, knot.Position);
                var nearestKnot = traffic.trafficLanes[i].spline.ElementAt(nearestKnotIndex);
                var distance = math.distancesq(knot.Position, nearestKnot.Position);
                if (distance > maxDistance) continue;
                nearestTrafficKnot = nearestKnot;
                maxDistance = distance;
                nearestFound = true;
            }

            return nearestFound;
        }
    }
}