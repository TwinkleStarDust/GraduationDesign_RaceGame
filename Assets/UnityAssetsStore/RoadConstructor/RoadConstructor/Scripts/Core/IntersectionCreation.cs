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
    internal static class IntersectionCreation
    {
        public static void CreateIntersection(ComponentSettings settings, Overlap overlap, RoadObject newRoadObject,
            ConstructionObjects constructionObjects)
        {
            var newIntersections = constructionObjects.newIntersections;
            var newReplacedRoads = constructionObjects.newReplacedRoads;
            var removableIntersections = constructionObjects.removableIntersections;
            var removableRoads = constructionObjects.removableRoads;

            var existingConnections = overlap.SceneObject.RoadConnections;
            var elevated = newRoadObject.elevated || existingConnections.Any(conn => conn.elevated);
            
            var newRoadSpline = newRoadObject.splineContainer.Spline;
            var newNearestKnotIndex = RoadSplineUtility.GetNearestKnotIndex(newRoadSpline, overlap.position);
            var newNearestKnot = newRoadSpline.Knots.ElementAt(newNearestKnotIndex);
            var tangent01 = newNearestKnot.TangentOut;

            var snappedRoad = overlap.IsSnappedRoad();
            
            var createIntersectionMeshDatas = new List<CreateIntersectionMeshData>();
            
            /********************************************************************************************************************************/
            /********************************************************************************************************************************/
            if (overlap.roadType == RoadType.Intersection || snappedRoad)
            {
                var oldConnections = new List<RoadObject>();
                if(!snappedRoad) oldConnections.AddRange(overlap.intersectionObject.RoadConnections);
                else oldConnections.AddRange(overlap.roadObject.RoadConnections);
                oldConnections.Add(newRoadObject);
                
                for (int i = 0; i < oldConnections.Count; i++)
                {
                    var roadConnection = oldConnections[i];
                    var connectionSpline = roadConnection.splineContainer.Spline;
                    var nearestKnotIndex = RoadSplineUtility.GetNearestKnotIndex(connectionSpline, overlap.position);
                    var nearestKnot = connectionSpline.Knots.ElementAt(nearestKnotIndex);
                    
                    createIntersectionMeshDatas.Add(new CreateIntersectionMeshData(roadConnection.roadDescr, nearestKnot, nearestKnotIndex));
                }
                
                var priorityRoadDescr = GetHighestPriorityMeshData(createIntersectionMeshDatas).roadDescr;
                
                
                /********************************************************************************************************************************/
                // Replacing existing Road Objects (reduced length)
                
                for (int i = 0; i < createIntersectionMeshDatas.Count; i++)
                {
                    if(oldConnections[i].iD == newRoadObject.iD) continue;
                    
                    var data = createIntersectionMeshDatas[i];
                    var knot = data.knot;
                    var otherDatas = new List<CreateIntersectionMeshData>(createIntersectionMeshDatas);
                    otherDatas.RemoveAt(i);
                    if (otherDatas.Count == 0) break;
                
                    var tangentIn = PGTrigonometryUtility.DirectionalTangentToPointXZ(overlap.position, knot.Position, knot.TangentOut);
                    var otherIndex = AngleDistanceUtility.GetIndexWithLeastDegrees(overlap.position, tangentIn, otherDatas);
                    var otherKnot = otherDatas[otherIndex].knot;
                    var otherTangentIn = PGTrigonometryUtility.DirectionalTangentToPointXZ(overlap.position, otherKnot.Position, otherKnot.TangentOut);
                
                    var angle = math.degrees(math.abs(PGTrigonometryUtility.AngleXZ(tangentIn, otherTangentIn)));
                    
                    var targetDistance = AngleDistanceUtility.GetAngleDistance(priorityRoadDescr, angle);
                    var currentDistance = math.distance(overlap.position, knot.Position);
                    var distanceDelta = targetDistance - currentDistance;
                    
                    var newConnectionSpline = new Spline(oldConnections[i].splineContainer.Spline);
                    RoadSplineUtility.ReduceSpline(newConnectionSpline, data.nearestKnotIndex == 0, newConnectionSpline.GetLength(), distanceDelta);
                        
                    var newRoadConnection = RoadCreation.CreateReplaceRoadObject(oldConnections[i], newConnectionSpline, 1f);
                
                    var nearestKnotIndex = RoadSplineUtility.GetNearestKnotIndex(newRoadConnection.splineContainer.Spline, overlap.position);
                    var nearestKnot = newRoadConnection.splineContainer.Spline[nearestKnotIndex];
                
                    newReplacedRoads.Add(newRoadConnection);
                    removableRoads.Add(oldConnections[i]);
                    
                    createIntersectionMeshDatas[i] = new CreateIntersectionMeshData(oldConnections[i].roadDescr, nearestKnot, nearestKnotIndex);
                }
                
                var oldIntersection = overlap.intersectionObject;
                var oldRoad = overlap.roadObject;
                
                if (snappedRoad)
                    removableRoads.Add(oldRoad);
                else
                    removableIntersections.Add(oldIntersection);
            }
            /********************************************************************************************************************************/
            /********************************************************************************************************************************/
            else if (overlap.roadType == RoadType.Road)
            {
                var oldRoadObject = overlap.roadObject;
                var oldRoadDescr = oldRoadObject.roadDescr;

                /********************************************************************************************************************************/
                // New Road Objects left and right (reduced length)
                    
                var angleLeft = math.degrees(math.abs(PGTrigonometryUtility.AngleXZ(tangent01, -overlap.tangent)));
                var angleRight = math.degrees(math.abs(PGTrigonometryUtility.AngleXZ(tangent01, overlap.tangent)));

                if (newNearestKnotIndex != 0) (angleLeft, angleRight) = (angleRight, angleLeft);
                
                var widthGapLeft = AngleDistanceUtility.GetAngleDistance(overlap.RoadDescr, angleLeft);
                var widthGapRight = AngleDistanceUtility.GetAngleDistance(overlap.RoadDescr, angleRight);
                    
                RoadCreation.SplitRoad(overlap.roadObject, overlap.t, widthGapLeft, widthGapRight, 1f,
                    out var splitRoadObjLeft, out var splitRoadObjRight, true, overlap.position.y);

                var nearestKnotIndexLeft = RoadSplineUtility.GetNearestKnotIndex(splitRoadObjLeft.splineContainer.Spline, overlap.position);
                var nearestKnotLeft = splitRoadObjLeft.splineContainer.Spline[nearestKnotIndexLeft];
                var nearestKnotIndexRight = RoadSplineUtility.GetNearestKnotIndex(splitRoadObjRight.splineContainer.Spline, overlap.position);
                var nearestKnotRight = splitRoadObjRight.splineContainer.Spline[nearestKnotIndexRight];
                
                createIntersectionMeshDatas.Add(new CreateIntersectionMeshData(newRoadObject.roadDescr, newNearestKnot, newNearestKnotIndex));
                createIntersectionMeshDatas.Add(new CreateIntersectionMeshData(oldRoadDescr, nearestKnotLeft, nearestKnotIndexLeft));
                createIntersectionMeshDatas.Add(new CreateIntersectionMeshData(oldRoadDescr, nearestKnotRight, nearestKnotIndexRight));

                newReplacedRoads.Add(splitRoadObjLeft);
                newReplacedRoads.Add(splitRoadObjRight);
                removableRoads.Add(oldRoadObject);
            }
            
            /********************************************************************************************************************************/
                
            var intersectionObject =  CreateIntersectionObject(settings, overlap.position, createIntersectionMeshDatas, elevated);
            newIntersections.Add(intersectionObject); 
        }


        private static IntersectionObject CreateIntersectionObject(ComponentSettings settings, 
            float3 centerPosition, List<CreateIntersectionMeshData> createIntersectionMeshDatas, bool elevated)
        {
            var priorityMeshData = GetHighestPriorityMeshData(createIntersectionMeshDatas);

            var intersectionMesh = CreateIntersectionMesh(settings, centerPosition,
                createIntersectionMeshDatas, elevated,
                out var newMaterials, out var newSplines, out var newPositions);

            CreateIntersectionObject(priorityMeshData.roadDescr.road,
                out var intersectionObject, out var splineContainer, out var meshFilter, out var meshRenderer);

            meshFilter.mesh = intersectionMesh;
            meshRenderer.materials = newMaterials.ToArray();

            intersectionObject.Initialize(priorityMeshData.roadDescr, meshFilter, meshRenderer, splineContainer, elevated);
            intersectionObject.centerPosition = centerPosition;
            
            var knotPositions = createIntersectionMeshDatas.Select(t => t.knot.Position).ToList();
            var intersectionSplines = RoadSplineUtility.CreateIntersectionSplines(centerPosition, knotPositions);
            
            intersectionObject!.splineContainer.RemoveSpline(intersectionObject.splineContainer.Spline);
            for (var i = 0; i < intersectionSplines.Count; i++)
                intersectionObject.splineContainer.AddSpline(intersectionSplines[i]);

            return intersectionObject;
        }
        
        public static void CreateIntersectionObject(Road road,
            out IntersectionObject intersectionObject, out SplineContainer splineContainer,
            out MeshFilter meshFilter, out MeshRenderer meshRenderer)
        {
            var intersectionObj = ObjectUtility.CreateIntersectionObject(road.shadowCastingMode, out meshFilter, out meshRenderer);

            intersectionObject = intersectionObj.AddComponent<IntersectionObject>();
            intersectionObject.meshFilter = meshFilter;
            intersectionObject.meshRenderer = meshRenderer;

            splineContainer = intersectionObj.AddComponent<SplineContainer>();
            splineContainer.RemoveSpline(splineContainer.Spline);
        }

        /********************************************************************************************************************************/

        public class CreateIntersectionMeshData
        {
            public readonly RoadDescr roadDescr;
            public readonly BezierKnot knot;
            public readonly int nearestKnotIndex;

            public CreateIntersectionMeshData(RoadDescr roadDescr, BezierKnot knot, int nearestKnotIndex)
            {
                this.roadDescr = roadDescr;
                this.knot = knot;
                this.nearestKnotIndex = nearestKnotIndex;
            }
        }

        public static Mesh CreateIntersectionMesh(ComponentSettings settings, float3 centerPosition,
            List<CreateIntersectionMeshData> datas, bool elevated,
            out List<Material> newMaterials, out List<Spline> newSplines, out List<float3> newPositions, float lodAmount = 1f)
        {
            var newMeshes = new List<Mesh>();
            newMaterials = new List<Material>();
            newSplines = new List<Spline>();
            newPositions = new List<float3>();

            var knots = datas.Select(t => t.knot).ToList();
            
            var centerAverage = new float3(knots.Average(knot => knot.Position.x), knots.Average(knot => knot.Position.y),
                knots.Average(knot => knot.Position.z));

            /********************************************************************************************************************************/
            // One Intersection road (Main Connection)
            // Only for the 2 knots which are looking at each other.

            AngleDistanceUtility.GetFacingIndexes(centerAverage, knots, out var indexLeft, out var indexRight);
            
            var exitDatas = datas.Where((_, index) => index != indexLeft && index != indexRight).ToList();

            var knotLeft = datas[indexLeft].knot;
            var knotRight = datas[indexRight].knot;
            var roadDescrLeft = datas[indexLeft].roadDescr;
            var roadDescrRight = datas[indexRight].roadDescr;

            var leadingRoadDescr = roadDescrLeft.road.length > roadDescrRight.road.length ? roadDescrLeft : roadDescrRight;
            var otherRoadDescr = roadDescrLeft.road.length <= roadDescrRight.road.length ? roadDescrLeft : roadDescrRight;

            var roadLanesIntersection = elevated ? leadingRoadDescr.lanesIntersectionElevated : leadingRoadDescr.lanesIntersection;
            var roadPos01 = knotLeft.Position;
            var roadTan01 = knotLeft.TangentOut;
            var roadPos02 = knotRight.Position;
            var roadTan02 = knotRight.TangentOut;
            var roadRes = leadingRoadDescr.resolution;

            /********************************************************************************************************************************/
            // Main Connection
            if (knots.Count == 2) roadRes = leadingRoadDescr.detailResolution;

            var knot01 = new BezierKnot(roadPos01, -roadTan01, roadTan01, quaternion.identity);
            var knot02 = new BezierKnot(roadPos02, -roadTan02, roadTan02, quaternion.identity);
            roadRes = RoadSplineUtility.CalculateResolution(settings, roadRes, knot01, knot02, lodAmount);

            var widthStart = otherRoadDescr.width / leadingRoadDescr.width;
            var widthEnd = 1f;

            CreateIntersectionRoadMesh(roadLanesIntersection, leadingRoadDescr.settings, centerAverage, leadingRoadDescr.width, leadingRoadDescr.road.length,
                roadRes, roadPos01, roadTan01, roadPos02, roadTan02,
                newMeshes, newMaterials, newSplines, widthStart, widthEnd);

            /********************************************************************************************************************************/
            // Other roads only get an exit road

            var mainConnectionCenter = centerPosition;
            var roadTan01center = PGTrigonometryUtility.DirectionalTangentToPointXZ(centerPosition, roadPos01, roadTan01);
            var roadTan02center = PGTrigonometryUtility.DirectionalTangentToPointXZ(centerPosition, roadPos02, roadTan02) * -1f;
            var degrees = math.abs(math.degrees(PGTrigonometryUtility.AngleXZ(roadTan01center, roadTan02center)));
            if (degrees > 80f) mainConnectionCenter = newSplines[0].EvaluatePosition(0.5f);

            for (var i = 0; i < exitDatas.Count; i++)
            {
                var exitRoadDescr = exitDatas[i].roadDescr;
                var pos1 = mainConnectionCenter + new float3(0, Constants.HeightOffsetExitRoad, 0);
                var tan1 = mainConnectionCenter - exitDatas[i].knot.Position;
                var pos2 = exitDatas[i].knot.Position;
                var tan2 = exitDatas[i].knot.TangentOut;

                var ResKnot1 = new BezierKnot
                    {Position = pos1, TangentIn = math.normalizesafe(tan1), TangentOut = math.normalizesafe(-tan1), Rotation = quaternion.identity};
                var ResKnot2 = new BezierKnot
                    {Position = pos2, TangentIn = math.normalizesafe(-tan2), TangentOut = math.normalizesafe(tan2), Rotation = quaternion.identity};

                var resolution = RoadSplineUtility.CalculateResolution(settings, exitRoadDescr.resolution, ResKnot1, ResKnot2, lodAmount);
                var exitLanes = elevated ? exitRoadDescr.lanesIntersectionElevated : exitRoadDescr.lanesIntersection;

                var center = (pos1 + pos2) * 0.5f;
                CreateIntersectionRoadMesh(exitLanes, leadingRoadDescr.settings, center,
                    exitRoadDescr.width, exitRoadDescr.road.length, resolution, pos1, tan1, pos2, tan2,
                    newMeshes, newMaterials, newSplines, 1f, 1f);
            }

            /********************************************************************************************************************************/
            // Closing Rectangles (Middle Lanes)

            var rectangleMaterials = new List<Material>();
            var rectangleCombines = new List<CombineInstance>();
            for (var i = 0; i < datas.Count; i++)
            {
                var closingCombineInstances = RoadEndCreation.ClosingRectangle(datas[i].roadDescr, datas[i].roadDescr.lanesMiddle, rectangleMaterials,
                    centerPosition, knots[i].Position, knots[i].TangentIn, datas[i].nearestKnotIndex == 0);
                rectangleCombines.AddRange(closingCombineInstances);
            }

            if (rectangleMaterials.Count > 0)
            {
                var closingRectangleMesh = new Mesh();
                closingRectangleMesh.CombineMeshes(rectangleCombines.ToArray(), true);

                newMaterials.Add(rectangleMaterials[0]);
                newMeshes.Add(closingRectangleMesh);
            }

            /********************************************************************************************************************************/
            // Side Connections

            var overlap2D = new float2(centerAverage.x, centerAverage.z);
            var pos2D = new List<float2>();
            for (var i = 0; i < knots.Count; i++) pos2D.Add(new float2(knots[i].Position.x, knots[i].Position.z));

            for (var i = 0; i < datas.Count; i++)
            {
                // Connect each item with the next road clockwise
                var lowestClockwiseAngle = float.MaxValue;
                var sideDescrIndex = 0;
                var tan1 = pos2D[i] - overlap2D;
                for (var j = 0; j < datas.Count; j++)
                {
                    if (j == i) continue;
                    var tan2 = pos2D[j] - overlap2D;
                    var angleRad = PGTrigonometryUtility.AngleClockwise(tan1, tan2);
                    if (angleRad < lowestClockwiseAngle)
                    {
                        lowestClockwiseAngle = angleRad;
                        sideDescrIndex = j;
                    }
                }

                var mainDescrIndex = i;
                var laneMainLeftSide = true;
                var laneSideLeftSide = false;

                newPositions.Add(knots[mainDescrIndex].Position);


                var switchRoadDescr = false;
                if (datas[mainDescrIndex].roadDescr.road.priority == datas[sideDescrIndex].roadDescr.road.priority &&
                    mainDescrIndex < sideDescrIndex)
                    switchRoadDescr = true;
                else if (datas[mainDescrIndex].roadDescr.road.priority < datas[sideDescrIndex].roadDescr.road.priority)
                    switchRoadDescr = true;
                if (switchRoadDescr)
                {
                    (mainDescrIndex, sideDescrIndex) = (sideDescrIndex, mainDescrIndex);
                    (laneMainLeftSide, laneSideLeftSide) = (laneSideLeftSide, laneMainLeftSide);
                }

                var roadDescrMain = datas[mainDescrIndex].roadDescr;
                var roadDescrSide = datas[sideDescrIndex].roadDescr;
                var knotMain = knots[mainDescrIndex];
                var knotSide = knots[sideDescrIndex];

                CreateSideConnection(datas.Count, roadDescrMain, laneMainLeftSide,
                    roadDescrSide, laneSideLeftSide, centerAverage,
                    knotMain.Position, knotMain.TangentOut,
                    knotSide.Position, knotSide.TangentOut,
                    out var newSideMeshes, out var newSideMaterials, lodAmount);

                newMeshes.AddRange(newSideMeshes);
                newMaterials.AddRange(newSideMaterials);
            }

            /********************************************************************************************************************************/
            PGMeshUtility.CombineAndPackMeshes(newMaterials, newMeshes, out var intersectionMaterials, out var intersectionMesh);
            newMaterials = new List<Material>(intersectionMaterials);
            /********************************************************************************************************************************/

            return intersectionMesh;
        }

        /********************************************************************************************************************************/
        /********************************************************************************************************************************/

        private static void CreateIntersectionRoadMesh(List<Lane> lanes, ComponentSettings settings, float3 center,
            float partWidth, float partLength, int resolution, float3 position01, float3 tangent01, float3 position02, float3 tangent02,
            List<Mesh> newMeshes, List<Material> newMaterials, List<Spline> newSplines, float widthStart, float widthEnd)
        {
            TangentCalculation.CalculateTangents(settings.smoothSlope, Constants.TangentLengthIntersection,  
                position01, tangent01, position02, tangent02,true, center,
                out tangent01, out tangent02);

            var knot01 = new BezierKnot
                {Position = position01, Rotation = quaternion.identity, TangentIn = -tangent01, TangentOut = tangent01};
            var knot02 = new BezierKnot
                {Position = position02, Rotation = quaternion.identity, TangentIn = tangent02, TangentOut = -tangent02};

            var newSpline = new Spline
            {
                {knot01, TangentMode.Broken},
                {knot02, TangentMode.Broken}
            };

            var splineMeshParameter = new SplineMeshParameter(partWidth, partLength, resolution, settings.roadLengthUV, newSpline);
            SplineMesh.CreateMultipleSplineMeshes(lanes, splineMeshParameter,
                out var _newMeshes, out var _newMaterials, 0f, 1f, widthStart, widthEnd);

            newMeshes.AddRange(_newMeshes);
            newMaterials.AddRange(_newMaterials);
            newSplines.Add(newSpline);
        }


        /********************************************************************************************************************************/


        private static void CreateSideConnection(int roadCount, RoadDescr roadDescrMain,
            bool mainLeftSide, RoadDescr roadDescrSide, bool sideLeftSide, float3 center,
            float3 positionMain, float3 tangentMain, float3 positionSide, float3 tangentSide,
            out List<Mesh> newMeshes, out List<Material> newMaterials, float lodAmount)
        {
            newMeshes = new List<Mesh>();
            newMaterials = new List<Material>();
            var settings = roadDescrMain.settings;

            if (roadDescrMain.lanesLeft.Count == 0 && roadDescrSide.lanesLeft.Count == 0) return;
            if (roadDescrMain.lanesLeft.Count == 0)
            {
                (roadDescrMain, roadDescrSide) = (roadDescrSide, roadDescrMain);
                (mainLeftSide, sideLeftSide) = (sideLeftSide, mainLeftSide);
                (positionMain, positionSide) = (positionSide, positionMain);
                (tangentMain, tangentSide) = (tangentSide, tangentMain);
            }

            var sideLanesMain = mainLeftSide ? roadDescrMain.lanesLeftOffset : roadDescrMain.lanesRightOffset;
            var sideLanesSide = mainLeftSide ? roadDescrSide.lanesRightOffset : roadDescrSide.lanesLeftOffset;

            tangentMain = math.normalizesafe(tangentMain);
            tangentSide = math.normalizesafe(tangentSide);

            var center_2D = new float2(center.x, center.z);
            var positionMain_2D = new float2(positionMain.x, positionMain.z);
            var tangentMain_2D = new float2(tangentMain.x, tangentMain.z);
            var positionSide_2D = new float2(positionSide.x, positionSide.z);
            var tangentSide_2D = new float2(tangentSide.x, tangentSide.z);

            tangentMain_2D = PGTrigonometryUtility.DirectionalTangentToPoint(center_2D, positionMain_2D, tangentMain_2D);
            tangentSide_2D = PGTrigonometryUtility.DirectionalTangentToPoint(center_2D, positionSide_2D, tangentSide_2D);

            var tangentMainPerp_2D = math.normalizesafe(PGTrigonometryUtility.RotateTangent90Clockwise(tangentMain_2D));
            var tangentSidePerp_2D = math.normalizesafe(PGTrigonometryUtility.RotateTangent90Clockwise(tangentSide_2D));
            if (mainLeftSide) tangentMainPerp_2D *= -1f;
            if (sideLeftSide) tangentSidePerp_2D *= -1f;

            positionMain_2D += tangentMainPerp_2D * roadDescrMain.sideLanesCenterDistance;
            positionSide_2D += tangentSidePerp_2D * roadDescrSide.sideLanesCenterDistance;


            var posMain = new float3(positionMain_2D.x, positionMain.y, positionMain_2D.y);
            var posSide = new float3(positionSide_2D.x, positionSide.y, positionSide_2D.y);
            var tanMain = new float3(tangentMain_2D.x, 0f, tangentMain_2D.y);
            var tanSide = new float3(tangentSide_2D.x, 0f, tangentSide_2D.y);


            var tangentLength = Constants.TangentLengthIntersection;

            tanMain = PGTrigonometryUtility.DirectionalTangentToPointXZ(center, posMain, tanMain);
            tanSide = PGTrigonometryUtility.DirectionalTangentToPointXZ(center, posSide, tanSide);

            var positionCross = PGTrigonometryUtility.IntersectionPointXZ(posMain, tanMain, posSide, tanSide);
            var distanceCrossMain = math.distance(positionMain_2D, new float2(positionCross.x, positionCross.z));
            var distanceCrossSide = math.distance(positionSide_2D, new float2(positionCross.x, positionCross.z));

            var angle = math.abs(math.degrees(PGTrigonometryUtility.AngleXZ(tanMain, tanSide * -1f)));

            if (angle > 10 && roadCount != 2)
            {
                tanMain = math.normalizesafe(tanMain) * distanceCrossMain * tangentLength;
                tanSide = math.normalizesafe(tanSide) * distanceCrossSide * tangentLength;
            }
            else
            {
                TangentCalculation.CalculateTangents(settings.smoothSlope, tangentLength,  posMain, tanMain, posSide, tanSide,false, float3.zero, 
                    out tanMain, out tanSide);
            }

            var knots = new List<BezierKnot>
            {
                new(posMain, -tanMain, tanMain, quaternion.identity),
                new(posSide, tanSide, -tanSide, quaternion.identity)
            };

            var relativeWidth = roadDescrSide.sideLanesWidth / roadDescrMain.sideLanesWidth;

            Vector3 posMainOffset = default;
            if (sideLanesSide.Count == 0) // Closing instead of connecting to neighbor
            {
                posMainOffset = posMain + math.normalizesafe(tanMain) * settings.intersectionDistance;
                TangentCalculation.CalculateTangents(settings.smoothSlope, tangentLength,  posMain, tanMain, posMainOffset, tanMain,true,center,
                    out tanMain, out tanSide);
                knots = new List<BezierKnot>
                {
                    new(posMain, -tanMain, tanMain, quaternion.identity),
                    new(posMainOffset, tanSide, -tanSide, quaternion.identity)
                };
                relativeWidth = 1f;
            }


            var spline = new Spline(knots);
            
            var detailResolution =
                RoadSplineUtility.CalculateResolution(roadDescrMain.settings, roadDescrMain.detailResolution, knots[0], knots[1], lodAmount);

            for (var i = 0; i < sideLanesMain.Count; i++)
            {
                var newMesh = new Mesh();

                var splineMeshParameter = new SplineMeshParameter(roadDescrMain.sideLanesWidth, roadDescrMain.road.length, detailResolution, RoadLengthUV.Cut, spline);
                SplineMesh.CreateSplineMesh(newMesh, sideLanesMain[i].splineEdges, splineMeshParameter, 0f, 1f, 1f, relativeWidth);

                newMeshes.Add(newMesh);
                newMaterials.Add(sideLanesMain[i].material);
            }

            if (sideLanesSide.Count == 0) // Closing rectangle
            {
                var closingCombineInstances = new List<CombineInstance>();
                for (var i = 0; i < sideLanesMain.Count; i++)
                {
                    var lane = sideLanesMain[i];
                    if (lane.height <= 0f) continue;

                    var rectangleCombine = RoadEndCreation.ClosingRectangleCombine(roadDescrMain, lane, posMainOffset, tanMain, mainLeftSide);
                    closingCombineInstances.Add(rectangleCombine);

                    var _newMesh = new Mesh();
                    _newMesh.CombineMeshes(closingCombineInstances.ToArray(), true);
                    newMeshes.Add(_newMesh);
                    newMaterials.Add(lane.material);
                    closingCombineInstances.Clear();
                }
            }
        }

        public static void CreateSideConnectionDirect(RoadDescr roadDescrMain, RoadDescr roadDescrSide, List<Lane> lanes,
            float3 positionMain, float3 tangentMain, float3 positionSide, float3 tangentSide,
            out List<Mesh> newMeshes, out List<Material> newMaterials, float lodAmount)
        {
            newMeshes = new List<Mesh>();
            newMaterials = new List<Material>();

            if (lanes.Count == 0) return;

            if (roadDescrSide.lanesLeft.Count == 0) // Closing rectangle
            {
                var closingCombineInstances = new List<CombineInstance>();
                for (var i = 0; i < lanes.Count; i++)
                {
                    var lane = lanes[i];
                    if (lane.height <= 0f) continue;

                    var rectangleCombine = RoadEndCreation.ClosingRectangleCombine(roadDescrMain, lane, positionMain, tangentMain, false);
                    closingCombineInstances.Add(rectangleCombine);

                    var _newMesh = new Mesh();
                    _newMesh.CombineMeshes(closingCombineInstances.ToArray(), true);
                    newMeshes.Add(_newMesh);
                    newMaterials.Add(lane.material);
                    closingCombineInstances.Clear();
                }

                return;
            }

            var tangentLength = Constants.TangentLengthIntersection;

            TangentCalculation.CalculateTangents(roadDescrMain.settings.smoothSlope, tangentLength,  positionMain, tangentMain, positionSide,
                tangentSide,false, float3.zero, 
                out tangentMain, out tangentSide);

            var knots = new List<BezierKnot>
            {
                new(positionMain, -tangentMain, tangentMain, quaternion.identity),
                new(positionSide, tangentSide, -tangentSide, quaternion.identity)
            };

            var relativeWidth = roadDescrSide.sideLanesWidth / roadDescrMain.sideLanesWidth;

            var spline = new Spline(knots);
            spline.SetTangentMode(TangentMode.Broken);

            var detailResolution =
                RoadSplineUtility.CalculateResolution(roadDescrMain.settings, roadDescrMain.detailResolution, knots[0], knots[1], lodAmount);

            for (var i = 0; i < lanes.Count; i++)
            {
                var newMesh = new Mesh();

                var splineMeshParameter = new SplineMeshParameter(roadDescrMain.sideLanesWidth, roadDescrMain.road.length, detailResolution, RoadLengthUV.Cut, spline);

                SplineMesh.CreateSplineMesh(newMesh, lanes[i].splineEdges, splineMeshParameter,0f, 1f, 1f, relativeWidth);

                newMeshes.Add(newMesh);
                newMaterials.Add(lanes[i].material);
            }
        }

        /********************************************************************************************************************************/
        
        private static CreateIntersectionMeshData GetHighestPriorityMeshData(List<CreateIntersectionMeshData> createIntersectionMeshDatas)
        {
            return createIntersectionMeshDatas
                .OrderByDescending(data => data.roadDescr.road.priority)
                .ThenByDescending(data => data.roadDescr.width)
                .FirstOrDefault();
        }
        
        private static RoadDescr GetHighestPriorityRoadDescr(List<RoadDescr> roadDescrs)
        {
            return roadDescrs
                .OrderByDescending(data => data.road.priority)
                .ThenByDescending(data => data.width)
                .FirstOrDefault();
        }

    }
}