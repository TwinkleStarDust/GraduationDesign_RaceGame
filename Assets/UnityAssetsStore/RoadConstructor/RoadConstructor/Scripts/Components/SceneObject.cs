// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    public abstract class SceneObject : MonoBehaviour
    {
        public string iD;
        public Road road;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public List<MeshFilter> meshFilterLODs = new();
        public List<MeshRenderer> meshRendererLODs = new();
        public SplineContainer splineContainer;
        public bool elevated;
        public Traffic traffic;

        public float Width => roadDescr.width;
        public Bounds Bounds => meshRenderer.bounds;
        
        internal RoadDescr roadDescr;
        
        [SerializeField] private List<IntersectionObject> intersectionConnections = new();
        
        [SerializeField] private List<RoadObject> roadConnections = new();
        
        /// <summary>
        ///     Incoming / outgoing <see cref="IntersectionObject" />s.
        /// </summary>
        public List<IntersectionObject> IntersectionConnections => intersectionConnections;
        
        /// <summary>
        ///     Incoming / outgoing <see cref="RoadObject" />s.
        /// </summary>
        public List<RoadObject> RoadConnections => roadConnections;
        
        public List<SceneObject> Connections => intersectionConnections.Cast<SceneObject>().Concat(roadConnections).ToList();
        
        /********************************************************************************************************************************/
        public void Initialize(RoadDescr roadDescr, MeshFilter meshFilter, MeshRenderer meshRenderer, SplineContainer splineContainer, bool elevated)
        {
            iD = name;
            road = roadDescr.road;
            this.roadDescr = roadDescr;
            this.meshFilter = meshFilter;
            this.meshRenderer = meshRenderer;
            this.splineContainer = splineContainer;
            this.roadDescr = roadDescr;
            this.elevated = elevated;
        }

        public List<MeshFilter> GetMeshFilters()
        {
            var meshFilters = new List<MeshFilter>();
            meshFilters.AddRange(meshFilterLODs);
            if (meshFilters.Count == 0) meshFilters.Add(meshFilter);
            return meshFilters;
        }

        public List<MeshRenderer> GetMeshRenderers()
        {
            var meshRenderers = new List<MeshRenderer>();
            meshRenderers.AddRange(meshRendererLODs);
            if (meshRenderers.Count == 0) meshRenderers.Add(meshRenderer);
            return meshRenderers;
        }

        public SpawnedObject[] GetSpawnedObjects()
        {
            var spawnedObjects = GetComponentsInChildren<SpawnedObject>();
            return spawnedObjects;
        }

        public void DestroySpawnedObjects()
        {
            var spawnedObjects = GetComponentsInChildren<SpawnedObject>();
            for (var i = 0; i < spawnedObjects.Length; i++) ObjectUtility.DestroyObject(spawnedObjects[i].gameObject);
        }

        public List<TrafficLane> GetTrafficLanes()
        {
            return traffic.trafficLanes;
        }

        public List<TrafficLane> GetTrafficLanes(TrafficLaneType trafficLaneType)
        {
            return traffic.trafficLanes.Where(t => t.trafficLaneType == trafficLaneType).ToList();
        }

        public List<TrafficLane> GetTrafficLanes(TrafficLaneType trafficLaneType, TrafficLaneDirection trafficLaneDirection)
        {
            if (trafficLaneDirection == TrafficLaneDirection.Both) return GetTrafficLanes(trafficLaneType);
            return traffic.trafficLanes.Where(t => t.trafficLaneType == trafficLaneType && t.direction == trafficLaneDirection).ToList();
        }

        /// <summary>
        ///     Returns the squared distance to the closest point on the road.
        /// </summary>
        public float GetClosestDistanceSq(Vector3 point)
        {
            var splines = splineContainer.Splines.ToList();
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < splines.Count; i++)
            {
                SplineUtility.GetNearestPoint(splines[i], point, out var nearest, out var t);
                var distance = math.distancesq(point, nearest);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                }
            }
            
            return nearestDistance;
        }

        /********************************************************************************************************************************/

        public bool IsRoad()
        {
            return GetType() == typeof(RoadObject);
        }

        public bool IsDirectConnection()
        {
            if (GetType() == typeof(IntersectionObject))
            {
                var intersectionObject = (IntersectionObject) this;
                return intersectionObject.RoadConnections.Count == 2 &&
                       intersectionObject.intersectionType == IntersectionType.Intersection;
            }

            return false;
        }

        public bool IsIntersection()
        {
            if (GetType() == typeof(IntersectionObject))
            {
                var intersectionObject = (IntersectionObject) this;
                return intersectionObject.RoadConnections.Count > 2 &&
                       intersectionObject.intersectionType == IntersectionType.Intersection;
            }

            return false;
        }

        public bool IsEndObject()
        {
            if (GetType() == typeof(IntersectionObject))
            {
                var intersectionObject = (IntersectionObject) this;
                return intersectionObject.intersectionType == IntersectionType.Intersection &&
                       intersectionObject.RoadConnections.Count == 1;
            }

            return false;
        }

        public bool IsRoundabout()
        {
            if (GetType() == typeof(RoundaboutObject))
            {
                var roundaboutObject = (RoundaboutObject) this;
                return roundaboutObject.intersectionType == IntersectionType.Roundabout;
            }

            return false;
        }
        
        /********************************************************************************************************************************/

        public void AddConnection(SceneObject sceneObject)
        {
            if(sceneObject is RoadObject roadObject)
            {
                if(!roadConnections.Contains(roadObject)) roadConnections.Add(roadObject);
            }
            else
            {
                var intersectionObject = sceneObject as IntersectionObject;
                if(!intersectionConnections.Contains(intersectionObject)) intersectionConnections.Add(intersectionObject);
            }
        }

        public void ClearConnections()
        {
            intersectionConnections.Clear();
            roadConnections.Clear();
        }
        
        /********************************************************************************************************************************/
        
        public void AddIntersectionConnection(IntersectionObject intersectionConnection)
        {
            intersectionConnections.Add(intersectionConnection);
        }
        
        /********************************************************************************************************************************/
        
        public void ClearRoadConnections()
        {
            roadConnections.Clear();
        }
        public void AddRoadConnection(RoadObject roadConnection)
        {
            roadConnections.Add(roadConnection);
        }
        
        public void AddRoadConnections(List<RoadObject> _roadConnections)
        {
            roadConnections.AddRange(_roadConnections);
        }
        
        public void RemoveRoadConnection(RoadObject roadConnection)
        {
            roadConnections.Remove(roadConnection);
        }
    }
}