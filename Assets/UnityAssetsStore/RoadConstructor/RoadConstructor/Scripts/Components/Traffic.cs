// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    public class Traffic : MonoBehaviour
    {
        public SceneObject sceneObject;
        public SplineContainer splineContainer;

        public List<TrafficLane> trafficLanes = new();
    }

    [Serializable]
    public class TrafficLane
    {
        public TrafficLaneType trafficLaneType;
        public TrafficLaneDirection direction;
        public float position;
        public float width;
        public float maxSpeed;
        public Spline spline;
        public bool crossing;

        public List<Waypoint> waypoints = new();

        public TrafficLane(TrafficLaneEditor trafficLaneEditor, Spline spline, bool crossing)
        {
            trafficLaneType = trafficLaneEditor.trafficLaneType;
            position = trafficLaneEditor.position;
            width = trafficLaneEditor.width;
            direction = trafficLaneEditor.direction == TrafficLaneDirection.Both ? TrafficLaneDirection.Forward : trafficLaneEditor.direction;
            maxSpeed = trafficLaneEditor.maxSpeed;
            this.spline = spline;
            this.crossing = crossing;
        }

        public TrafficLane(TrafficLane trafficLane, Spline spline, bool crossing)
        {
            trafficLaneType = trafficLane.trafficLaneType;
            position = trafficLane.position;
            width = trafficLane.width;
            direction = trafficLane.direction == TrafficLaneDirection.Both ? TrafficLaneDirection.Forward : trafficLane.direction;
            maxSpeed = trafficLane.maxSpeed;
            this.spline = spline;
            this.crossing = crossing;
        }

        public void SetWaypoints(List<Waypoint> _waypoints)
        {
            waypoints = _waypoints;
        }

        public List<Waypoint> GetWaypoints()
        {
            return waypoints;
        }

        public Waypoint GetNearestWaypoint(Vector3 point)
        {
            if (waypoints.Count == 0) return null;

            var nearestIndex = new NativeReference<int>(Allocator.TempJob);
            var waypointPositions = new NativeArray<float3>(waypoints.Count, Allocator.TempJob);
            for (var i = 0; i < waypoints.Count; i++) waypointPositions[i] = waypoints[i].transform.position;

            var job = new NearestWaypointJob
            {
                _waypointPositions = waypointPositions,
                _nearestIndex = nearestIndex,
                _point = point
            };

            var jobHandle = job.Schedule();
            jobHandle.Complete();

            var nearestWaypoint = waypoints[nearestIndex.Value];

            waypointPositions.Dispose();
            nearestIndex.Dispose();

            return nearestWaypoint;
        }

        [BurstCompile]
        private struct NearestWaypointJob : IJob
        {
            [ReadOnly] public NativeArray<float3> _waypointPositions;
            public NativeReference<int> _nearestIndex;
            public float3 _point;

            public void Execute()
            {
                var nearestDistanceSqr = float.MaxValue;
                var nearestIndex = -1;

                for (var i = 0; i < _waypointPositions.Length; i++)
                {
                    var distanceSqr = math.distancesq(_waypointPositions[i], _point);
                    if (!(distanceSqr < nearestDistanceSqr)) continue;
                    nearestDistanceSqr = distanceSqr;
                    nearestIndex = i;
                }

                _nearestIndex.Value = nearestIndex;
            }
        }
    }
}