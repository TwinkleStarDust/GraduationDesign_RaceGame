// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using PampelGames.Shared.Utility;
using Unity.Mathematics;
using UnityEngine;

namespace PampelGames.RoadConstructor
{
    public static class IntersectionValidation
    {
        public static List<ConstructionFail> ValidateIntersection(ComponentSettings settings, Overlap overlap)
        {
            var constructionFails = new List<ConstructionFail>();
            
            if (!overlap.exists) return constructionFails;

            /********************************************************************************************************************************/
            // Height Range
            constructionFails.AddRange(ValidateGroundIntersection(settings, overlap));

            /********************************************************************************************************************************/
            // New replaced roads
            for (var i = 0; i < overlap.SceneObject.RoadConnections.Count; i++)
            {
                var roadConnection = overlap.SceneObject.RoadConnections[i];
                var knot01 = roadConnection.splineContainer.Spline[0];
                var knot02 = roadConnection.splineContainer.Spline[1];

                /********************************************************************************************************************************/
                // Road Length
                var splineLength = roadConnection.splineContainer.Spline.GetLength();
                if (splineLength < settings.roadLength.x)
                    constructionFails.Add(new ConstructionFail(FailCause.IntersectionRoadLength));
                else if (splineLength > settings.roadLength.y)
                    constructionFails.Add(new ConstructionFail(FailCause.IntersectionRoadLength));

                /********************************************************************************************************************************/
                // Slope
                var slope = math.degrees(PGTrigonometryUtility.Slope(knot01.Position, knot02.Position));
                if (math.abs(slope) > settings.maxSlope) constructionFails.Add(new ConstructionFail(FailCause.IntersectionRoadSlope));
            }


            return constructionFails;
        }

        private static List<ConstructionFail> ValidateGroundIntersection(ComponentSettings settings, Overlap overlap)
        {
            var constructionFails = new List<ConstructionFail>();
            if (!overlap.exists) return constructionFails;
            if (settings.elevatedIntersections) return constructionFails;
            if (overlap.roadType == RoadType.Intersection && overlap.intersectionObject.RoadConnections.Count == 1) return constructionFails;
            if (settings.groundLayers.value == 0) return constructionFails;

            var raycastOffset = Constants.RaycastOffset(settings);

            Vector3 halfExtents;
            if (overlap.roadType == RoadType.Intersection)
            {
                halfExtents = overlap.intersectionObject.meshRenderer.bounds.extents;
            }
            else
            {
                var halftWidth = overlap.roadObject.roadDescr.width * 0.5f;
                halfExtents = new Vector3(halftWidth, halftWidth, halftWidth);
            }

            var hit = Physics.BoxCast((Vector3) overlap.position + raycastOffset, halfExtents, Vector3.down, out var hitInfo, Quaternion.identity,
                Mathf.Infinity, settings.groundLayers);
            if (!hit)
            {
                constructionFails.Add(new ConstructionFail(FailCause.GroundMissing));
            }
            else
            {
                var heightDif = overlap.position.y - hitInfo.point.y;

                if (heightDif > 0 && heightDif > math.abs(settings.elevationStartHeight))
                    constructionFails.Add(new ConstructionFail(FailCause.ElevatedIntersection));

                if (heightDif < 0 && math.abs(heightDif) > math.abs(settings.heightRange.x))
                    constructionFails.Add(new ConstructionFail(FailCause.ElevatedIntersection));
            }

            return constructionFails;
        }

        /********************************************************************************************************************************/
        /********************************************************************************************************************************/

        public static List<ConstructionFail> ValidateRoundabout(ComponentSettings settings, RoundaboutObject roundabout)
        {
            var constructionFails = new List<ConstructionFail>();

            /********************************************************************************************************************************/
            // Height Range
            var bounds = roundabout.meshRenderer.bounds;
            constructionFails.AddRange(ValidateGroundRoundabout(settings, bounds));
            constructionFails.AddRange(ValidateGroundRoundabout(settings, bounds));

            return constructionFails;
        }

        /********************************************************************************************************************************/

        private static List<ConstructionFail> ValidateGroundRoundabout(ComponentSettings settings, Bounds bounds)
        {
            var constructionFails = new List<ConstructionFail>();
            if (settings.elevatedIntersections) return constructionFails;
            if (settings.groundLayers.value == 0) return constructionFails;

            var raycastOffset = Constants.RaycastOffset(settings);

            var hit = Physics.BoxCast(bounds.center + raycastOffset, bounds.extents, Vector3.down, out var hitInfo, Quaternion.identity,
                Mathf.Infinity, settings.groundLayers);
            if (!hit)
            {
                constructionFails.Add(new ConstructionFail(FailCause.GroundMissing));
            }
            else
            {
                var heightDif = bounds.center.y - hitInfo.point.y;

                if (heightDif > 0 && heightDif > math.abs(settings.elevationStartHeight))
                    constructionFails.Add(new ConstructionFail(FailCause.ElevatedIntersection));

                if (heightDif < 0 && math.abs(heightDif) > math.abs(settings.heightRange.x))
                    constructionFails.Add(new ConstructionFail(FailCause.ElevatedIntersection));
            }

            return constructionFails;
        }
    }
}