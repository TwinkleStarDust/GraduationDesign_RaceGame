// ----------------------------------------------------
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
    internal static class AngleDistanceUtility
    {
        private const float Factor = 0.35f;

        /// <summary>
        ///     Calculates the distance to the intersection center.
        /// </summary>
        /// <param name="intersectionRoadDescr"></param>
        /// <param name="closestAngle">Angle to the nearest neighbour in degrees.</param>
        public static float GetAngleDistance(RoadDescr intersectionRoadDescr, float closestAngle)
        {
            var distance = intersectionRoadDescr.width * 0.5f + intersectionRoadDescr.settings.intersectionDistance;
            if (closestAngle >= 90) return distance;
            closestAngle = math.max(closestAngle, intersectionRoadDescr.settings.minAngleIntersection);
            var additionalDistance = (90 - closestAngle) * Factor;
            return distance + additionalDistance;
        }

        /********************************************************************************************************************************/

        public static int GetIndexWithLeastDegrees(float3 centerPosition, float3 tangentIn,
            List<IntersectionCreation.CreateIntersectionMeshData> createIntersectionMeshData)
        {
            var knots = createIntersectionMeshData.Select(t => t.knot).ToList();
            return GetIndexWithLeastDegrees(centerPosition, tangentIn, knots);
        }
        public static int GetIndexWithLeastDegrees(float3 centerPosition, float3 tangentIn, List<BezierKnot> knots)
        {
            var minDegree = float.MaxValue;
            var nearestIndex = 0;
            
            for (var i = 0; i < knots.Count; i++)
            {
                var knotTangentIn = PGTrigonometryUtility.DirectionalTangentToPointXZ(centerPosition, knots[i].Position, knots[i].TangentOut);
                var angleRad = math.abs(PGTrigonometryUtility.AngleXZ(knotTangentIn, tangentIn));
                
                if (angleRad < minDegree)
                {
                    minDegree = angleRad;
                    nearestIndex = i;
                }
            }

            return nearestIndex;
        }

        /// <summary>
        ///     Gets the indexes of the two knots that are oriented towards each other with the smallest angle between them.
        /// </summary>
        public static void GetFacingIndexes(float3 centerPosition, List<BezierKnot> knots, out int index1, out int index2)
        {
            index1 = 0;
            index2 = 0;
            var minDegree = float.MaxValue;

            for (var i = 0; i < knots.Count; i++)
            {
                var tan = PGTrigonometryUtility.DirectionalTangentToPointXZ(centerPosition, knots[i].Position, knots[i].TangentOut);
                for (var j = i + 1; j < knots.Count; j++)
                {
                    var tanOut = PGTrigonometryUtility.DirectionalTangentToPointXZ(centerPosition, knots[j].Position, knots[j].TangentOut) * -1f;

                    var degrees = math.abs(PGTrigonometryUtility.AngleXZ(tan, tanOut));

                    if (degrees < minDegree)
                    {
                        minDegree = degrees;
                        index1 = i;
                        index2 = j;
                    }
                }
            }
        }
    }
}