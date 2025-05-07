// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    internal static class RoadCreation
    {
        public static RoadObject CreateRoad(RoadDescr roadDescr, Spline spline, bool elevated, float lodAmount)
        {
            CreateRoadMesh(roadDescr, spline, lodAmount, elevated, out var _materials, out var newSplineMesh);

            var roadObj = ObjectUtility.CreateRoadObject(roadDescr.road.shadowCastingMode, out var roadMeshFilter, out var roadMeshRenderer);

            roadMeshFilter.mesh = newSplineMesh;
            roadMeshRenderer.materials = _materials;

            var roadObject = roadObj.AddComponent<RoadObject>();
            roadObject.length = spline.GetLength();
            var splineContainer = roadObject.gameObject.AddComponent<SplineContainer>();
            splineContainer.Spline = spline;
            roadObject.Initialize(roadDescr, roadMeshFilter, roadMeshRenderer, splineContainer, elevated);

            return roadObject;
        }

        public static void CreateRoadMesh(RoadDescr roadDescr, Spline spline, float lodAmount, bool elevated,
            out Material[] materials, out Mesh newSplineMesh)
        {
            var road = roadDescr.road;
            var settings = roadDescr.settings;

            var _lanes = elevated ? roadDescr.lanesElevated : roadDescr.lanes;

            var resolution = RoadSplineUtility.CalculateResolution(roadDescr.settings, roadDescr.resolution, spline.Knots.First(),
                spline.Knots.Last(),
                lodAmount);

            var splineMeshParameter = new SplineMeshParameter(roadDescr.width, road.length, resolution, settings.roadLengthUV, spline);

            newSplineMesh = SplineMesh.CreateCombinedSplineMesh(_lanes, splineMeshParameter, out materials);
        }

        /********************************************************************************************************************************/

        public static RoadObject CreateReplaceRoadObject(RoadObject oldRoadObject, Spline spline, float lodAmount)
        {
            var roadDescr = oldRoadObject.roadDescr;
            var _lanes = oldRoadObject.elevated ? roadDescr.lanesElevated : roadDescr.lanes;
            return CreateReplaceRoadObject(oldRoadObject, spline, _lanes, lodAmount);
        }

        public static RoadObject CreateReplaceRoadObject(RoadObject oldRoadObject, Spline spline, List<Lane> lanes, float lodAmount)
        {
            var roadDescr = oldRoadObject.roadDescr;

            var roadObj = CreateReplaceRoadObj(roadDescr, lanes, spline,
                out var roadMeshFilter, out var roadMeshRenderer, lodAmount);

            var roadObject = roadObj.AddComponent<RoadObject>();
            roadObject.length = spline.GetLength();
            var splineContainer = roadObject.gameObject.AddComponent<SplineContainer>();
            splineContainer.Spline = spline;
            roadObject.Initialize(roadDescr, roadMeshFilter, roadMeshRenderer, splineContainer, oldRoadObject.elevated);

            return roadObject;
        }

        private static GameObject CreateReplaceRoadObj(RoadDescr roadDescr, List<Lane> lanes, Spline spline,
            out MeshFilter roadMeshFilter, out MeshRenderer roadMeshRenderer, float lodAmount)
        {
            var road = roadDescr.road;
            var settings = roadDescr.settings;

            var resolution = RoadSplineUtility.CalculateResolution(roadDescr.settings, roadDescr.resolution, spline.Knots.First(),
                spline.Knots.Last(),
                lodAmount);

            var splineMeshParameter = new SplineMeshParameter(roadDescr.width, road.length, resolution, settings.roadLengthUV, spline);

            var newSplineMesh = SplineMesh.CreateCombinedSplineMesh(lanes, splineMeshParameter, out var _materials);

            var roadObj = ObjectUtility.CreateRoadObject(road.shadowCastingMode, out roadMeshFilter, out roadMeshRenderer);

            roadMeshFilter.mesh = newSplineMesh;
            roadMeshRenderer.materials = _materials;

            return roadObj;
        }

        /********************************************************************************************************************************/

        public static void SplitRoad(RoadObject road, float t, float gapLeft, float gapRight, float lodAmount,
            out RoadObject road01, out RoadObject road02,
            bool flattenGap = false, float gapHeight = 0f)
        {
            var spline = road.splineContainer.Spline;

            var roadLength = spline.GetLength();
            var relativeGapLeft = gapLeft / roadLength;
            var relativeGapRight = gapRight / roadLength;

            var splineLeft = new Spline(spline);
            RoadSplineUtility.InsertKnotSeamless(splineLeft, t - relativeGapLeft);
            splineLeft.RemoveAt(splineLeft.Count - 1);

            var splineRight = new Spline(spline);
            RoadSplineUtility.InsertKnotSeamless(splineRight, t + relativeGapRight);
            splineRight.RemoveAt(0);

            if (flattenGap)
            {
                var knotLeft = splineLeft.Knots.Last();
                knotLeft.Position = new Vector3(knotLeft.Position.x, gapHeight, knotLeft.Position.z);
                splineLeft.SetKnot(splineLeft.Knots.Count() - 1, knotLeft);
                var knotRight = splineRight.Knots.First();
                knotRight.Position = new Vector3(knotRight.Position.x, gapHeight, knotRight.Position.z);
                splineRight.SetKnot(0, knotRight);
            }

            road01 = CreateReplaceRoadObject(road, splineLeft, lodAmount);
            road02 = CreateReplaceRoadObject(road, splineRight, lodAmount);
        }
    }
}