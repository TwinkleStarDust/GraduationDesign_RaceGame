// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using PampelGames.Shared.Utility;
using Unity.Mathematics;
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    internal static class RoadExtension
    {
        public static void CreateExtension(ComponentSettings settings, Overlap overlap, RoadObject newRoadObject,
            ConstructionObjects constructionObjects)
        {
            var newReplacedRoads = constructionObjects.newReplacedRoads;
            var removableIntersections = constructionObjects.removableIntersections;
            var removableRoads = constructionObjects.removableRoads;


            var newRoadSpline = newRoadObject.splineContainer.Spline;

            var existingRoad = overlap.intersectionObject.RoadConnections[0];

            /********************************************************************************************************************************/
            // Reduced existing road
            var reducedSpline = CreateReducedSpline(newRoadObject, overlap);
            var replaceRoad = RoadCreation.CreateReplaceRoadObject(existingRoad, reducedSpline, 1f);

            newReplacedRoads.Add(replaceRoad);

            /********************************************************************************************************************************/
            // Connection road
            var connectionSpline = RoadSplineUtility.CreateConnectionSpline(settings, newRoadSpline, reducedSpline);

            var connectionRoad = RoadCreation.CreateReplaceRoadObject(existingRoad, connectionSpline, 1f);

            var splineLeft = new Spline(connectionSpline);
            var splineRight = new Spline(connectionSpline);

            RoadSplineUtility.OffsetSplineX(splineLeft, -connectionRoad.roadDescr.width * 0.5f);
            RoadSplineUtility.OffsetSplineX(splineRight, connectionRoad.roadDescr.width * 0.5f);

            connectionRoad.snapPositionSet = true;
            connectionRoad.snapPosition = overlap.position;

            newReplacedRoads.Add(connectionRoad);
            removableIntersections.Add(overlap.intersectionObject);
            removableRoads.Add(existingRoad);
        }


        private static Spline CreateReducedSpline(RoadObject newRoadObject, Overlap overlap)
        {
            var newRoadDescr = newRoadObject.roadDescr;

            var newRoadSpline = newRoadObject.splineContainer.Spline;
            var newNearestKnotIndex = RoadSplineUtility.GetNearestKnotIndex(newRoadSpline, overlap.position);
            var newNearestKnot = newRoadSpline.Knots.ElementAt(newNearestKnotIndex);
            

            var position01 = newNearestKnot.Position;
            var tangent01 = newNearestKnot.TangentOut;

            var existingRoad = overlap.intersectionObject.RoadConnections[0];
            var existingRoadData = overlap.roadConnectionDatas[0];

            var tangent01Perp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(overlap.tangent);

            var newPosPerpRight = position01 + tangent01Perp * newRoadDescr.width * 0.5f;
            var newPosPerpLeft = position01 - tangent01Perp * newRoadDescr.width * 0.5f;

            var oldKnot = existingRoadData.nearestKnot;

            var overlapTangentPerp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(overlap.tangent);
            var oldPosPerpRight = oldKnot.Position + overlapTangentPerp * existingRoad.roadDescr.width * 0.5f;
            var oldPosPerpLeft = oldKnot.Position - overlapTangentPerp * existingRoad.roadDescr.width * 0.5f;

            var rightSide = PGTrigonometryUtility.DistanceXZ(oldPosPerpRight, position01) <
                            PGTrigonometryUtility.DistanceXZ(oldPosPerpLeft, position01);

            var oldPosPerp = rightSide ? oldPosPerpRight : oldPosPerpLeft;
            var newPosPerp = rightSide ? newPosPerpRight : newPosPerpLeft;

            var angle = math.abs(math.degrees(PGTrigonometryUtility.AngleXZ(overlap.tangent, tangent01)));

            var intersectionPoint = PGTrigonometryUtility.IntersectionPointXZ(oldPosPerp, overlap.tangent,
                newPosPerp, tangent01);

            var reducedLength = 0f;

            if (angle < 80)
            {
                reducedLength = PGTrigonometryUtility.DistanceXZ(overlap.position, position01);
            }
            else // This just matches the length. If curve distance no good, change it in RoadCreation.CalculateValues() not here.
            {
                var distanceNewToPoint = PGTrigonometryUtility.DistanceXZ(newPosPerp, intersectionPoint);
                var distanceOldToPoint = PGTrigonometryUtility.DirectionalDistanceXZ(oldKnot.Position, overlap.tangent, intersectionPoint);
                reducedLength += math.abs(distanceOldToPoint) + distanceNewToPoint;
            }

            // Approximation
            var currentLength = existingRoad.splineContainer.Spline.GetLength();
            if (currentLength < reducedLength) reducedLength = currentLength * 0.5f;

            var reducedSpline = new Spline(existingRoad.splineContainer.Spline);
            RoadSplineUtility.ReduceSpline(reducedSpline, existingRoadData.nearestKnotIndex == 0, existingRoad.length, reducedLength);

            return reducedSpline;
        }
    }
}