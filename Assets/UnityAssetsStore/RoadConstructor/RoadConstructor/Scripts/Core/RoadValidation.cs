// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using PampelGames.Shared.Utility;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    internal static class RoadValidation
    {
        public static List<ConstructionFail> ValidateRoad(ComponentSettings settings, ConstructionData roadData, RoadDescr roadDescr, Spline spline,
            RoadConstructor.SceneData sceneData, List<int> overlapIntersectionIndexes, List<int> overlapRoadIndexes)
        {
            var constructionFails = new List<ConstructionFail>();
            var road = roadDescr.road;

            var roadObjects = sceneData.roadObjects;
            var intersectionBounds = sceneData.intersectionBounds;

            /********************************************************************************************************************************/
            // Overlap Intersections

            SplineWorldUtility.SplineEvaluationLeftRight(spline, road.length, roadDescr.width * 0.5f,
                out var splinePositionsLeft, out var splinePositionsRight, out var splineTangents);
            
            var splinePositions = splinePositionsLeft.Concat(splinePositionsRight).ToArray();

            var _intersecBounds = new Bounds[overlapIntersectionIndexes.Count];
            for (var i = 0; i < overlapIntersectionIndexes.Count; i++)
            {
                var index = overlapIntersectionIndexes[i];
                var _bounds = intersectionBounds[index];
                _bounds.size = new Vector3(_bounds.size.x + settings.minOverlapDistance * 2f,
                    _bounds.size.y + settings.minOverlapHeight * 2f, _bounds.size.z + settings.minOverlapDistance * 2f);
                _intersecBounds[i] = _bounds;
            }

            var insidePositionsIntersection = SplineWorldUtility.CheckPositionsInsideBounds(splinePositions, _intersecBounds);
            if (insidePositionsIntersection.Length > 0) constructionFails.Add(new ConstructionFail(FailCause.OverlapIntersection));

            /********************************************************************************************************************************/
            // Overlap Roads
            
            for (var i = 0; i < overlapRoadIndexes.Count; i++)
            {
                var existingRoadObject = roadObjects[overlapRoadIndexes[i]];
                var minOverlapDistance = settings.minOverlapDistance;
                
                var existingRoadSpline = existingRoadObject.splineContainer.Spline;
                
                // Left + right splitting necessary to skip indexes correctly
                var insidePositionsRoadLeft = SplineWorldUtility.CheckPositionsInsideSpline(splinePositionsLeft, existingRoadSpline,
                    existingRoadObject.roadDescr.width * 0.5f, minOverlapDistance, settings.minOverlapHeight);
                
                if (insidePositionsRoadLeft.Length > 0)
                {
                    constructionFails.Add(new ConstructionFail(FailCause.OverlapRoad));
                }
                else
                {
                    var insidePositionsRoadRight = SplineWorldUtility.CheckPositionsInsideSpline(splinePositionsLeft, existingRoadSpline,
                        existingRoadObject.roadDescr.width * 0.5f, minOverlapDistance, settings.minOverlapHeight);
                    if (insidePositionsRoadRight.Length > 0)
                    {
                        constructionFails.Add(new ConstructionFail(FailCause.OverlapRoad));
                    }
                }
            }

            /********************************************************************************************************************************/
            // Elevation per road
            if (!road.elevatable && roadData.elevated) constructionFails.Add(new ConstructionFail(FailCause.RoadNotElevatable));

            /********************************************************************************************************************************/
            // Ground
            constructionFails.AddRange(ValidateGround(settings, roadDescr, splinePositions, splineTangents));

            /********************************************************************************************************************************/
            // Road Length
            var length = roadData.length;
            if (length < settings.roadLength.x) constructionFails.Add(new ConstructionFail(FailCause.RoadLength));
            else if (length > settings.roadLength.y) constructionFails.Add(new ConstructionFail(FailCause.RoadLength));

            /********************************************************************************************************************************/
            // Curvature
            if (roadData.curvature > settings.maxCurvature) constructionFails.Add(new ConstructionFail(FailCause.Curvature));

            /********************************************************************************************************************************/
            // Slope
            var slope = math.degrees(PGTrigonometryUtility.Slope(roadData.position01, roadData.position02));
            if (math.abs(slope) > settings.maxSlope) constructionFails.Add(new ConstructionFail(FailCause.Slope));

            return constructionFails;
        }

        public static void ValidateRamp(List<ConstructionFail> constructionFails, ConstructionData roadData, RoadDescr roadDescr,
            Overlap overlap01, Overlap overlap02)
        {
            if(!roadDescr.road.oneWay)
            {
                constructionFails.Add(new ConstructionFail(FailCause.RampMissingOneWayRoad));
            }

            if ((!overlap01.exists || overlap01.roadType != RoadType.Road) && (!overlap02.exists || overlap02.roadType != RoadType.Road))
            {
                constructionFails.Add(new ConstructionFail(FailCause.RampMissingRoadConnection));
            }
        }


        /********************************************************************************************************************************/

        private static List<ConstructionFail> ValidateGround(ComponentSettings settings, RoadDescr roadDescr, float3[] splinePositions,
            float3[] splineTangents)
        {
            var constructionFails = new List<ConstructionFail>();
            if (settings.groundLayers.value == 0) return constructionFails;

            var queryParameters = new QueryParameters
            {
                layerMask = settings.groundLayers
            };

            var raycastOffset = Constants.RaycastOffset(settings);

            var commands = new NativeArray<BoxcastCommand>(splinePositions.Length / 2, Allocator.TempJob);
            var results = new NativeArray<RaycastHit>(splinePositions.Length / 2, Allocator.TempJob);
            var index = 0;
            var extends = Vector3.one * (roadDescr.width * 0.5f);
            for (var i = 0; i < splinePositions.Length; i += 2)
            {
                var center = (Vector3) (splinePositions[i] + splinePositions[i + 1]) * 0.5f + raycastOffset;
                var orientation = quaternion.LookRotationSafe(splineTangents[index], Vector3.up);
                commands[index] = new BoxcastCommand(center, extends, orientation, Vector3.down, queryParameters);

                index++;
            }

            var raycastJobHandle = BoxcastCommand.ScheduleBatch(commands, results, 1);
            raycastJobHandle.Complete();

            var resultsArray = results.ToArray();

            commands.Dispose();
            results.Dispose();

            for (var i = 0; i < resultsArray.Length; i++)
            {
                var point = resultsArray[i].point;

                // Null-check for collider takes 4x the time compared to those two checks.
                if (resultsArray[i].distance == 0 && point == Vector3.zero)
                {
                    constructionFails.Add(new ConstructionFail(FailCause.GroundMissing));
                    break;
                }

                var splinePosition = (splinePositions[i * 2] + splinePositions[i * 2 + 1]) * 0.5f;
                var heightDif = splinePosition.y - point.y;

                if (heightDif > 0 && heightDif > math.abs(settings.heightRange.y))
                {
                    constructionFails.Add(new ConstructionFail(FailCause.HeightRange));
                    break;
                }

                if (heightDif < 0 && math.abs(heightDif) > math.abs(settings.heightRange.x))
                {
                    constructionFails.Add(new ConstructionFail(FailCause.HeightRange));
                    break;
                }
            }

            return constructionFails;
        }
    }
}