// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using UnityEngine;

namespace PampelGames.RoadConstructor
{
    public static class Enums
    {

        
    }

    public enum EditorDisplay
    {
        ComponentSettings,
        RoadSet
    }
    
    public enum RoadType
    {
        Intersection,
        Road,
    }
    
    public enum IntersectionType
    {
        Intersection,
        Roundabout,
    }

    public enum LaneType
    {
        Road,
        LeftSide,
        Intersection,
        ElevatedOnly,
        RoadEnd
    }
    
    public enum TrafficLaneType
    {
        Car,
        Pedestrian,
    }
    public enum TrafficLaneDirection
    {
        Forward,
        Backwards,
        Both
    }

    public enum FailCause
    {
        RoadLength,
        OverlapIntersection,
        OverlapRoad,
        GroundMissing,
        HeightRange,
        ElevatedIntersection,
        Curvature,
        Slope,
        IntersectionRoadLength,
        IntersectionRoadSlope,
        RoadNotElevatable,
        RampMissingOneWayRoad,
        RampMissingRoadConnection
    }

    public enum Connections
    {
        Free,
        Align
    }

    public enum RoadEnd
    {
        Rounded,
        None
    }
    public enum PartType
    {
        Roads,
        LanePreset,
        SpawnObjectPreset,
        TrafficLanePreset
    }

    public enum RoadLengthUV
    {
        Stretch,
        Cut,
    }
    
    public enum Elevation
    {
        Any,
        GroundOnly,
        ElevatedOnly
    }

    public enum SpawnObjectType
    {
        Road,
        IntersectionApproach,
        IntersectionExit
    }

    public enum SpacingType
    {
        WorldUnits,
        Bounds
    }
    public enum SpawnObjectPosition
    {
        Middle,
        Side,
        BothSides
    }
    public enum SpawnObjectRotation
    {
        Inside,
        Outside,
        Forward,
        Backward,
        Random
    }
    
    public enum BuilderRoadType
    {
        Road,
        Roundabout,
        // [InspectorName("Ramp (One-Way)")] Ramp
    }

    public enum AddCollider
    {
        None,
        Convex,
        NonConvex
    }

    public enum MoveStatus
    {
        Select,
        Move
    }
    
    public enum DrawGizmos
    {
        None,
        Selected,
        Always
    }
    
    public enum DrawGizmosColor
    {
        Object,
        Lane
    }
    
}
