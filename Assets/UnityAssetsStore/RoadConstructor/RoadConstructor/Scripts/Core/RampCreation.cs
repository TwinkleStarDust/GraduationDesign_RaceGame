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
    internal static class RampCreation
    {
        public static ConstructionObjects CreateRamp(RampObjectClass rampObjectClass, Overlap overlap, float lodAmount)
        {
            var constructionObjects = new ConstructionObjects();

            var spline = rampObjectClass.spline;
            
            var overlapRoad = overlap.roadObject;
            var overlapSpline = overlap.spline;
            var overlapSplineLength = overlapSpline.GetLength();
            var overlapRoadDescr = overlapRoad.roadDescr;
            

            // TODO: position and rampLength as method parameter 
            var nearestKnotIndex = RoadSplineUtility.GetNearestKnotIndex(spline, overlap.position);
            var nearestKnot = spline.Knots.ElementAt(nearestKnotIndex);
            var position = nearestKnot.Position;

            // TODO: Also method parameter to determine side
            var directionalPosition = position;
            var directionalPosition2D = new float2(directionalPosition.x, directionalPosition.z);

            var rampLength = 15f;
            
            var relativeRampLength = rampLength / overlapSplineLength;
            
            
            
            SplineUtility.GetNearestPoint(overlapSpline, position, out var nearestPosition, out var nearestT);

            // Determine direction of the road
            var tangentCalc = new float2(nearestPosition.x, nearestPosition.z) - directionalPosition2D;
            var overlapKnotFirst = overlapSpline.Knots.First();
            var overlapKnotLast = overlapSpline.Knots.Last();
            var tangentAngleFirst = new float2(overlapKnotFirst.Position.x, overlapKnotFirst.Position.z) - directionalPosition2D;
            var tangentAngleLast = new float2(overlapKnotLast.Position.x, overlapKnotLast.Position.z) - directionalPosition2D;
            var angleFirst = PGTrigonometryUtility.Angle(tangentCalc, tangentAngleFirst); 
            var angleLast = PGTrigonometryUtility.Angle(tangentCalc, tangentAngleLast); 
            var directionEndKnotIndex = angleFirst > angleLast ? 0 : 1;
            var directionEndKnot = directionEndKnotIndex == 0 ? overlapKnotFirst : overlapKnotLast;
            
            var otherT = directionEndKnotIndex == 0 ? nearestT - relativeRampLength : nearestT + relativeRampLength;
            

            var splitSplineLeft = new Spline(overlapSpline);
            RoadSplineUtility.InsertKnotSeamless(splitSplineLeft, nearestT);
            splitSplineLeft.RemoveAt(directionEndKnotIndex == 0 ? 0 : 2);
            var splitRoadObjLeft = RoadCreation.CreateReplaceRoadObject(overlapRoad, splitSplineLeft, lodAmount);
            
            var splitSplineRight = new Spline(overlapSpline);
            RoadSplineUtility.InsertKnotSeamless(splitSplineRight, otherT);
            splitSplineRight.RemoveAt(directionEndKnotIndex == 0 ? 2 : 0);
            var splitRoadObjRight = RoadCreation.CreateReplaceRoadObject(overlapRoad, splitSplineRight, lodAmount);
            
            var splitSplineMiddle = new Spline(overlapSpline);
            RoadSplineUtility.InsertKnotSeamless(splitSplineMiddle, nearestT);
            RoadSplineUtility.InsertKnotSeamless(splitSplineMiddle, otherT);
            RoadSplineUtility.InsertKnotSeamless(splitSplineMiddle, (nearestT + otherT) / 2f);
            splitSplineMiddle.RemoveAt(splitSplineMiddle.Count - 1);
            splitSplineMiddle.RemoveAt(0);
            var splitMiddleLanes = new List<Lane>(overlapRoadDescr.lanesMiddle);
            splitMiddleLanes.AddRange(directionEndKnotIndex == 0 ? overlapRoadDescr.lanesRight : overlapRoadDescr.lanesLeft);
            var splitRoadObjMiddle = RoadCreation.CreateReplaceRoadObject(overlapRoad, splitSplineMiddle, splitMiddleLanes, lodAmount);
            
            
            constructionObjects.newReplacedRoads.Add(splitRoadObjMiddle);
            constructionObjects.newReplacedRoads.Add(splitRoadObjLeft);
            constructionObjects.newReplacedRoads.Add(splitRoadObjRight);
            constructionObjects.removableRoads.Add(overlapRoad);
            
            return constructionObjects;

        }
        
        
    }
}