// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using PampelGames.Shared.Utility;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    internal static class RoundaboutCreation
    {
        public static void CreateRoundabout(ComponentSettings settings, Overlap overlap, RoadObject newRoadObject,
            ConstructionObjects constructionObjects, RoadDescr roadDescr, float3 position, float radius)
        {
            var update = overlap.IsRoundabout();
            
            var road = roadDescr.road;

            var roadConnections = new List<RoadObject>();
            if (update)
            {
                var oldRoundabout = overlap.intersectionObject as RoundaboutObject;
                roadConnections.AddRange(oldRoundabout!.RoadConnections);
                if(!roadConnections.Contains(newRoadObject)) roadConnections.Add(newRoadObject);
            }
            CreateRoundaboutMesh(settings, roadDescr, position, radius,
                roadConnections, out var combinedMaterials, out var combinedMesh, out var roadSplines, out var splineMiddle, 1f);

            var roundaboutObj = ObjectUtility.CreateIntersectionObject(road.shadowCastingMode, out var meshFilter, out var meshRenderer);
            meshFilter.mesh = combinedMesh;
            meshRenderer.materials = combinedMaterials;

            var _splineContainer = roundaboutObj.AddComponent<SplineContainer>();
            _splineContainer.Spline = splineMiddle;
            for (int i = 0; i < roadSplines.Count; i++) _splineContainer.AddSpline(roadSplines[i]);

            var newRoundabout = roundaboutObj.AddComponent<RoundaboutObject>();
            var elevated = WorldUtility.CheckElevation(roadDescr.settings, meshRenderer.bounds);
            newRoundabout.Initialize(roadDescr, meshFilter, meshRenderer, _splineContainer, elevated);
            newRoundabout.centerPosition = position;
            newRoundabout.radius = radius;
            newRoundabout.intersectionType = IntersectionType.Roundabout;
            
            constructionObjects.newIntersections.Add(newRoundabout);

            if(update)
            {
                var oldRoundabout = overlap.intersectionObject as RoundaboutObject;
                var existingConnections = oldRoundabout.RoadConnections; 

                for (int i = 0; i < existingConnections.Count; i++)
                {
                    var oldRoadConnection = existingConnections[i];
                    var newRoadConnection = RoadCreation.CreateReplaceRoadObject(oldRoadConnection, oldRoadConnection.splineContainer.Spline, 1f);
                    
                    var newRoad = newRoadConnection.GetComponent<RoadObject>();
                    constructionObjects.newReplacedRoads.Add(newRoad);
                    constructionObjects.removableRoads.Add(oldRoadConnection);
                }
            }
        }

        /********************************************************************************************************************************/
        /********************************************************************************************************************************/

        public static void CreateRoundaboutMesh(ComponentSettings settings, RoadDescr roundaboutRoadDescr, float3 centerPosition, float radius,
            List<RoadObject> roadConnections,
            out Material[] combinedMaterials, out Mesh combinedMesh, out List<Spline> roadSplines, out Spline splineMiddle, float lodAmount)
        {
            var road = roundaboutRoadDescr.road;
            roadSplines = new List<Spline>();

            var allMeshes = new List<Mesh>();
            var allMaterials = new List<Material>();
            
            var resolution = (int) math.round(settings.resolution * lodAmount);
            if (resolution == 0) resolution = 1;   

            splineMiddle = SplineCircle.CreateCircleSpline(radius, centerPosition, quaternion.identity, true);
            var radiusInside = math.max(0.01f, radius - roundaboutRoadDescr.sideLanesCenterDistance 
                                               + roundaboutRoadDescr.sideLanesWidth * 0.05f); // Small offset to fill gaps
            var splineInside = SplineCircle.CreateCircleSpline(radiusInside, centerPosition, quaternion.identity, true);
            var radiusOutside = radius + roundaboutRoadDescr.sideLanesCenterDistance;
            var splineOutside = SplineCircle.CreateCircleSpline(radiusOutside, centerPosition, quaternion.identity, true);
            
            /********************************************************************************************************************************/
            // Middle
            var splineMeshParameterMiddle = new SplineMeshParameter(roundaboutRoadDescr.width, road.length, resolution, settings.roadLengthUV, splineMiddle);
            SplineMesh.CreateMultipleSplineMeshes(roundaboutRoadDescr.lanesMiddle, splineMeshParameterMiddle,
                out var _meshes, out var _materials);
            allMeshes.AddRange(_meshes);
            allMaterials.AddRange(_materials);

            /********************************************************************************************************************************/
            // Inside
            var splineMeshParameterInside = new SplineMeshParameter(roundaboutRoadDescr.width, road.length, resolution, settings.roadLengthUV, splineInside);
            SplineMesh.CreateMultipleSplineMeshes(roundaboutRoadDescr.lanesLeftOffset, splineMeshParameterInside,
                out var _meshesIn, out var _materialsIn);
            allMeshes.AddRange(_meshesIn);
            allMaterials.AddRange(_materialsIn);


            /********************************************************************************************************************************/
            // Outside
            var outsideTGaps = new List<Vector2>();
            var splineOutsideLength = splineOutside.GetLength();

            for (var i = 0; i < roadConnections.Count; i++)
            {
                var roadDescr = roadConnections[i].roadDescr;
                var deltaWidth = roadDescr.width * 0.5f + settings.intersectionDistance;
                var deltaWidthT = deltaWidth / splineOutsideLength;
                var spline = roadConnections[i].splineContainer.Spline;
                var nearestKnotIndex = RoadSplineUtility.GetNearestKnotIndex(spline, centerPosition);
                var nearestKnot = spline.Knots.ElementAt(nearestKnotIndex);
                var startPart = nearestKnotIndex == 0;
                var tangent = PGTrigonometryUtility.DirectionalTangentToPointXZ(centerPosition, nearestKnot.Position, nearestKnot.TangentOut);
                tangent.y = 0f;
                tangent = math.normalizesafe(tangent);
                var tangentPerp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(tangent);

                SplineUtility.GetNearestPoint(splineOutside, nearestKnot.Position, out var nearestPositionOutside, out var nearestTOutside);

                /********************************************************************************************************************************/
                // For the outer ring (mesh created below)
                var nearestRoundTLeft = nearestTOutside - deltaWidthT;
                var nearestRoundTRight = nearestTOutside + deltaWidthT;
                if (nearestRoundTLeft < 0) outsideTGaps.Add(new Vector2(nearestRoundTLeft + 1, 1));
                if (nearestRoundTRight > 1) outsideTGaps.Add(new Vector2(0, nearestRoundTRight - 1));
                outsideTGaps.Add(new Vector2(Math.Max(0, nearestRoundTLeft), Math.Min(1, nearestRoundTRight)));

                if (nearestRoundTLeft < 0) nearestRoundTLeft += 1;
                if (nearestRoundTRight > 1) nearestRoundTRight -= 1;

                /********************************************************************************************************************************/
                // Side Connections
                splineOutside.Evaluate(nearestRoundTLeft, out var positionLeft, out var tangentLeft, out var upVectorLeft);
                tangentLeft.y = 0f;
                var nearestPosLeft = nearestKnot.Position - tangentPerp * roadDescr.sideLanesCenterDistance;

                var lanesLeft = roundaboutRoadDescr.lanesRightOffset;
                IntersectionCreation.CreateSideConnectionDirect(roundaboutRoadDescr, roadDescr, lanesLeft,
                    positionLeft, tangentLeft, nearestPosLeft, tangent,
                    out var newSideMeshesLeft, out var newSideMaterialsLeft, lodAmount);

                allMeshes.AddRange(newSideMeshesLeft);
                allMaterials.AddRange(newSideMaterialsLeft);

                splineOutside.Evaluate(nearestRoundTRight, out var positionRight, out var tangentRight, out var upVectorRight);
                tangentRight.y = 0f;
                tangentRight *= -1f;
                var nearestPosRight = nearestKnot.Position + tangentPerp * roadDescr.sideLanesCenterDistance;

                var lanesRight = roundaboutRoadDescr.lanesLeftOffset;
                IntersectionCreation.CreateSideConnectionDirect(roundaboutRoadDescr, roadDescr, lanesRight,
                    positionRight, tangentRight, nearestPosRight, tangent,
                    out var newSideMeshesRight, out var newSideMaterialsRight, lodAmount);

                allMeshes.AddRange(newSideMeshesRight);
                allMaterials.AddRange(newSideMaterialsRight);

                /********************************************************************************************************************************/
                // Adding one connecting road mesh
                SplineUtility.GetNearestPoint(splineMiddle, nearestKnot.Position, out var nearestPositionMiddle, out var nearestTMiddle);

                var knot02 = new BezierKnot
                {
                    Position = nearestPositionMiddle + new float3(0f, Constants.HeightOffsetExitRoad, 0f),
                    Rotation = quaternion.identity,
                    TangentOut = tangent,
                    TangentIn = tangent
                };

                var roadSpline = new Spline(new List<BezierKnot> {nearestKnot, knot02});
                TangentCalculation.CalculateTangents(roadSpline, settings.smoothSlope,  0.5f);

                var splineMeshParameter = new SplineMeshParameter(roadDescr.width, roadDescr.road.length, 1, settings.roadLengthUV, roadSpline);
                SplineMesh.CreateMultipleSplineMeshes(roadDescr.lanesIntersection, splineMeshParameter, 
                    out var roadMeshes, out var roadMaterials);
                
                allMeshes.AddRange(roadMeshes);
                allMaterials.AddRange(roadMaterials);
                roadSplines.Add(roadSpline);

                /********************************************************************************************************************************/
                // Closing Rectangles (Middle Lanes)

                var rectangleMaterials = new List<Material>();
                var rectangleCombines = new List<CombineInstance>();

                var closingCombineInstances = RoadEndCreation.ClosingRectangle(roadDescr, roadDescr.lanesMiddle, rectangleMaterials,
                    centerPosition, nearestKnot.Position, nearestKnot.TangentIn, startPart);
                rectangleCombines.AddRange(closingCombineInstances);

                if (rectangleMaterials.Count > 0)
                {
                    var closingRectangleMesh = new Mesh();
                    closingRectangleMesh.CombineMeshes(rectangleCombines.ToArray(), true);
                    allMaterials.Add(rectangleMaterials[0]);
                    allMeshes.Add(closingRectangleMesh);
                }
            }

            /********************************************************************************************************************************/
            /********************************************************************************************************************************/
            // Now creating the outer ring mesh
            
            outsideTGaps.Sort((v1, v2) => v1.x.CompareTo(v2.x));
            var outsideTs = new List<Vector2>(); // Outer ring with gaps for the roads.
            float lastEnd = 0;
            foreach (var val in outsideTGaps) // Inverting the outsideTGaps
            {
                if (val.x > lastEnd) outsideTs.Add(new Vector2(lastEnd, val.x));
                lastEnd = Math.Max(lastEnd, val.y);
            }

            if (lastEnd < 1) outsideTs.Add(new Vector2(lastEnd, 1));

            if (roadConnections.Count == 0) outsideTs.Add(new Vector2(0f, 1f));
            for (var i = 0; i < outsideTs.Count; i++) AddOutsideSplineMesh(outsideTs[i].x, outsideTs[i].y);

            void AddOutsideSplineMesh(float tStart, float tEnd)
            {
                var splineMeshParameter = new SplineMeshParameter(roundaboutRoadDescr.width, road.length, resolution, settings.roadLengthUV, splineOutside);
                SplineMesh.CreateMultipleSplineMeshes(roundaboutRoadDescr.lanesRightOffset, splineMeshParameter,
                    out var _meshesOut, out var _materialsOut, tStart, tEnd);
                allMeshes.AddRange(_meshesOut);
                allMaterials.AddRange(_materialsOut);
            }


            /********************************************************************************************************************************/

            PGMeshUtility.CombineAndPackMeshes(allMaterials, allMeshes, out combinedMaterials, out combinedMesh);
        }
    }
}