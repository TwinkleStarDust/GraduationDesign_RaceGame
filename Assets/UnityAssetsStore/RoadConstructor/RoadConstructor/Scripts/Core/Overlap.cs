// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    public class Overlap
    {
        /// <summary>
        ///     If the overlap exists.
        /// </summary>
        public bool exists;

        /// <summary>
        ///     Type of the overlapping part.
        /// </summary>
        public RoadType roadType;

        /// <summary>
        ///     Type of the intersection (only applicable for <see cref="RoadType.Road"/>).
        /// </summary>
        public IntersectionType intersectionType = IntersectionType.Intersection;

        /// <summary>
        ///     Position of the overlap.
        /// </summary>
        public float3 position;

        /// <summary>
        ///     List index in the <see cref="RoadConstructor" /> <see cref="RoadObject" /> list or <see cref="IntersectionObject" /> list,
        ///     depending on the roadType.
        /// </summary>
        public int index;

        /// <summary>
        ///     Outgoing connections.
        /// </summary>
        public List<RoadConnectionData> roadConnectionDatas;

        /// <summary>
        ///     Only applicable for <see cref="RoadType" /> <see cref="IntersectionObject" />.
        /// </summary>
        public IntersectionObject intersectionObject;

        /// <summary>
        ///     Only applicable for <see cref="RoadType" /> <see cref="RoadObject" />.
        /// </summary>
        public RoadObject roadObject;

        public Spline spline;
        public float t;
        public float3 tangent;
        public float3 upVector;

        public SceneObject SceneObject => roadType == RoadType.Road ? roadObject : intersectionObject;
        public RoadDescr RoadDescr => roadType == RoadType.Road ? roadObject.roadDescr : intersectionObject.roadDescr;
        
        public Road Road => roadType == RoadType.Road ? roadObject.road : intersectionObject.road;
        public float Width => roadType == RoadType.Road ? roadObject.roadDescr.width : intersectionObject.roadDescr.width;
        public SplineContainer SplineContainer => roadType == RoadType.Road ? roadObject.splineContainer : intersectionObject.splineContainer; 
        public float3 BoundsCenter => roadType == RoadType.Road ? roadObject.meshRenderer.bounds.center : intersectionObject.meshRenderer.bounds.center;
        public bool IsEndObject()
        {
            return exists && roadType == RoadType.Intersection && intersectionObject.IsEndObject();
        }

        public bool IsExtension(Road newRoad)
        {
            return exists && roadType == RoadType.Intersection && intersectionObject.intersectionType == IntersectionType.Intersection && 
                   intersectionObject.RoadConnections.Count == 1 &&
                   intersectionObject.RoadConnections[0].road.roadName == newRoad.roadName;
        }

        public bool IsSnappedRoad()
        {
            return exists && roadType == RoadType.Road && roadObject.snapPositionSet;
        }

        public bool IsRoundabout()
        {
            return exists && roadType == RoadType.Intersection && intersectionObject.intersectionType == IntersectionType.Roundabout;
        }
    }

    public class RoadConnectionData
    {
        /// <summary>
        ///     Nearest spline knot index to the overlap.
        ///     Only applicable for <see cref="RoadType.Intersection" /> .
        /// </summary>
        public int nearestKnotIndex;

        /// <summary>
        ///     Nearest spline knot to the overlap.
        ///     Only applicable for <see cref="RoadType.Intersection" /> .
        /// </summary>
        public BezierKnot nearestKnot;
        
        /// <summary>
        ///     Opposite spline knot index to the overlap.
        ///     Only applicable for <see cref="RoadType.Intersection" /> .
        /// </summary>
        public int otherKnotIndex;

        /// <summary>
        ///     Opposite spline knot to the overlap.
        ///     Only applicable for <see cref="RoadType.Intersection" /> .
        /// </summary>
        public BezierKnot otherKnot;

        /// <summary>
        ///     Angle in degrees to the new tangent, from -180 to 180.
        ///     Positive value means the existing road is clockwise to the new one.
        /// </summary>
        public float angle;

        public RoadConnectionData(int nearestKnotIndex, BezierKnot nearestKnot, int otherKnotIndex, BezierKnot otherKnot)
        {
            this.nearestKnotIndex = nearestKnotIndex;
            this.nearestKnot = nearestKnot;
            this.otherKnotIndex = otherKnotIndex;
            this.otherKnot = otherKnot;
        }
    }
}