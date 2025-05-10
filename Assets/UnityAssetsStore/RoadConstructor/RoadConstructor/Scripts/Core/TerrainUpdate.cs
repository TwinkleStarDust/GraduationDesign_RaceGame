// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using PampelGames.Shared.Utility;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace PampelGames.RoadConstructor
{
    internal static class TerrainUpdate
    {
        public static void CreateTerrainUpdateSplines(
            EndObjectClass endObjectClass01, EndObjectClass endObjectClass02, 
            Overlap overlap01, Overlap overlap02, ConstructionObjects constructionObjects,
            out List<Spline> roadSplines, out List<float> roadWidths, out List<Spline> intersectionSplines, out List<float> intersectionWidths)
        {
            roadSplines = new List<Spline>();
            roadWidths = new List<float>();
            intersectionSplines = new List<Spline>();
            intersectionWidths = new List<float>();

            var roads = new List<RoadObject>();
            roads.AddRange(constructionObjects.newRoads);
            roads.AddRange(constructionObjects.newReplacedRoads);

            var intersections = constructionObjects.newIntersections;

            TryAddOverlapSpline(intersectionSplines, intersectionWidths, overlap01, endObjectClass01);
            TryAddOverlapSpline(intersectionSplines, intersectionWidths, overlap02, endObjectClass02);
            
            void TryAddOverlapSpline(List<Spline> intersectionSplines, List<float> intersectionWidths, 
                Overlap _overlap, EndObjectClass _endObjectClass)
            {
                if (_overlap.exists) return;
                var pixelSize = GetMinHeightmapPixelSize(_endObjectClass.roadDescr.settings.terrain.terrainData);
                var increasedSpline = RoadSplineUtility.CreateIncreasedSpline(_endObjectClass.spline, false, pixelSize * 3f);
                AddSpline(intersectionSplines, intersectionWidths, increasedSpline, _endObjectClass.roadDescr.width);
            }
            
            for (var i = 0; i < roads.Count; i++)
            {
                var replacedRoad = roads[i];
                AddSpline(roadSplines, roadWidths, replacedRoad.splineContainer.Spline, replacedRoad.roadDescr.width);
            }

            for (var i = 0; i < intersections.Count; i++)
            {
                var intersection = intersections[i];
                
                var _intersectionSplines = intersection.splineContainer.Splines.ToList();
                for (var j = 0; j < _intersectionSplines.Count; j++)
                {
                    var _spline = _intersectionSplines[j];
                    var width = intersection.roadDescr.width * 1.1f; // Adding some extra space
                    AddSpline(intersectionSplines, intersectionWidths, _spline, width);
                }
            }
        }

        private static void AddSpline(List<Spline> splines, List<float> widths, Spline newSpline, float newWidth)
        {
            splines.Add(newSpline);
            widths.Add(newWidth);
        }

        /********************************************************************************************************************************/
        /********************************************************************************************************************************/

        public static TerrainUpdateUndo UpdateTerrain(ComponentSettings settings, List<Spline> splines, List<float> widths, bool checkHeight)
        {
            var undoHeightPixels = new List<Vector2Int>();
            var undoHeights = new List<float[,]>();
            var undoAlphaPixels = new List<Vector2Int>();
            var undoAlphamaps = new List<float[,,]>();
            var undoDetail = new List<TerrainDetailUndo>();
            var undoTrees = new List<TreeInstance[]>();
            
            for (var i = 0; i < splines.Count; i++)
            {
                UpdateTerrainInternal(settings, splines[i], widths[i], checkHeight,
                    out var _undoHeightPixels, out var _undoHeights,
                    out var _undoAlphaPixels, out var _undoAlphamap,
                    out var _undoDetail, out var _undoTrees);

                undoHeightPixels.AddRange(_undoHeightPixels);
                undoHeights.AddRange(_undoHeights);
                undoAlphaPixels.Add(_undoAlphaPixels);
                undoAlphamaps.Add(_undoAlphamap);
                undoDetail.Add(_undoDetail);
                undoTrees.Add(_undoTrees);
            }

            var terrainUpdateUndo = new TerrainUpdateUndo(undoHeightPixels, undoHeights, undoAlphaPixels, undoAlphamaps, undoDetail, undoTrees);
            return terrainUpdateUndo;
        }
        private static void UpdateTerrainInternal(ComponentSettings settings, Spline spline, float width, bool checkHeight,
            out List<Vector2Int> undoHeightPixels, out List<float[,]> undoHeights, 
            out Vector2Int undoAlphaPixels, out float[,,] undoAlphamaps,
            out TerrainDetailUndo detailUndo, out TreeInstance[] treeUndo)
        {
            undoHeightPixels = new List<Vector2Int>();
            undoHeights = new List<float[,]>();
            undoAlphaPixels = new Vector2Int();
            undoAlphamaps = new float[0, 0, 0];
            detailUndo = new TerrainDetailUndo();
            treeUndo = Array.Empty<TreeInstance>();
            
            var terrain = settings.terrain;
            var terrainData = settings.terrain.terrainData;

            var minPixelSize = GetMinHeightmapPixelSize(terrainData);
            var offsetX = width * 0.5f + minPixelSize;

            SplineWorldUtility.SplineEvaluationMiddle(spline, minPixelSize, out var ts,
                out var splinePositionsArrayMiddle, out var splineTangentsArray);
            
            var splineLeft = new Spline(spline);
            var splineRight = new Spline(spline);
            RoadSplineUtility.OffsetSplineX(splineLeft, -offsetX);
            RoadSplineUtility.OffsetSplineX(splineRight, offsetX);
            
            SplineWorldUtility.SplineEvaluationCenterToLeftRight(spline, splineLeft, splineRight, minPixelSize, 
                out var splinePositionsArrayLeft, out var splinePositionsArrayRight);

            var raycastOffset = Constants.RaycastOffset(settings);
            var raycastsLeft = WorldUtility.RaycastBatch(splinePositionsArrayLeft, settings.groundLayers, raycastOffset);
            var raycastsRight = WorldUtility.RaycastBatch(splinePositionsArrayRight, settings.groundLayers, raycastOffset);

            var splinePositions = new List<float3>();
            var splinePositionsLeft = new List<float3>();
            var splinePositionsRight = new List<float3>();
            var splineTangents = new List<float3>();
            bool heightRangeSuccess = false;
            
            for (var i = 0; i < raycastsLeft.Length; i++) // Looping from 2 to avoid clipping at the start.
            {
                var hitLeft = raycastsLeft[i];
                var splinePosLeft = splinePositionsArrayLeft[i];
                
                var hitRight = raycastsRight[i];
                var splinePosRight = splinePositionsArrayRight[i];
                
                if (hitLeft.distance == 0 && hitLeft.point == Vector3.zero &&
                    hitRight.distance == 0 && hitRight.point == Vector3.zero) continue;
                
                var groundDistanceLeft = splinePosLeft.y - hitLeft.point.y;
                var groundDistanceRight = splinePosRight.y - hitRight.point.y;

                if (groundDistanceLeft >= 0 && groundDistanceLeft > settings.elevationStartHeight &&
                    groundDistanceRight >= 0 && groundDistanceRight > settings.elevationStartHeight)
                {
                    continue;
                }
                
                heightRangeSuccess = true;
                
                if(checkHeight)
                {
                    splinePositions.Add(splinePositionsArrayMiddle[i]);
                    splineTangents.Add(splineTangentsArray[i]);
                    splinePositionsLeft.Add(splinePositionsArrayLeft[i]);
                    splinePositionsRight.Add(splinePositionsArrayRight[i]);
                }
            }

            if(!checkHeight && heightRangeSuccess) // For intersections either all positions or none
                for (int i = 0; i < splinePositionsArrayMiddle.Length; i++)
                {
                    splinePositions.Add(splinePositionsArrayMiddle[i]);
                    splineTangents.Add(splineTangentsArray[i]);
                    splinePositionsLeft.Add(splinePositionsArrayLeft[i]);
                    splinePositionsRight.Add(splinePositionsArrayRight[i]);
                }
            
            
            /********************************************************************************************************************************/
            // Remove Trees
            
            if (settings.removeTrees)
            {
                treeUndo = RemoveTrees(terrain, spline, width);
            }
            
            /********************************************************************************************************************************/
            
            if (splinePositions.Count == 0) return;

            CalculateTerrainData(settings, terrain, splinePositions, splineTangents,
                splinePositionsLeft, splinePositionsRight,
                out var pixels, out var heights,
                out var pixelsSmooth,
                out var pixelsTextureSmooth, out var textureStrengths, out var texturePositions);

            if (pixels.Length == 0) return;

            /********************************************************************************************************************************/
            /********************************************************************************************************************************/
            // Level Height
            if (settings.levelHeight)
            {
                // Fill undo first
                for (var i = 0; i < pixels.Length; i++)
                {
                    var pixelX = pixels[i].x;
                    var pixelY = pixels[i].y;
                    if (pixelX < 0 || pixelX >= terrainData.heightmapResolution || pixelY < 0 || pixelY >= terrainData.heightmapResolution) continue;

                    undoHeightPixels.Add(new Vector2Int(pixelX, pixelY));
                    undoHeights.Add(terrainData.GetHeights(pixelX, pixelY, 1, 1));
                }

                for (var i = 0; i < pixelsSmooth.Length; i++)
                {
                    var pixelX = pixelsSmooth[i].x;
                    var pixelY = pixelsSmooth[i].y;
                    if (pixelX < 0 || pixelX >= terrainData.heightmapResolution || pixelY < 0 || pixelY >= terrainData.heightmapResolution) continue;

                    undoHeightPixels.Add(new Vector2Int(pixelX, pixelY));
                    undoHeights.Add(terrainData.GetHeights(pixelX, pixelY, 1, 1));
                }

                /********************************************************************************************************************************/
                // Set Heights of the road.

                for (var i = 0; i < pixels.Length; i++)
                {
                    var pixelX = pixels[i].x;
                    var pixelY = pixels[i].y;
                    if (pixelX < 0 || pixelX >= terrainData.heightmapResolution || pixelY < 0 || pixelY >= terrainData.heightmapResolution) continue;
                
                    terrainData.SetHeightsDelayLOD(pixelX, pixelY, heights[i]);
                }
                
                /********************************************************************************************************************************/
                // Smooth Sides of the road.

                for (var i = 0; i < pixelsSmooth.Length; i++)
                {
                    var pixelX = pixelsSmooth[i].x;
                    var pixelY = pixelsSmooth[i].y;
                    if (pixelX < 0 || pixelX >= terrainData.heightmapResolution || pixelY < 0 || pixelY >= terrainData.heightmapResolution) continue;

                    var _heights = new List<float>
                    {
                        terrainData.GetHeight(pixelX, pixelY),
                        terrainData.GetHeight(pixelX + 1, pixelY),
                        terrainData.GetHeight(pixelX - 1, pixelY),
                        terrainData.GetHeight(pixelX, pixelY + 1),
                        terrainData.GetHeight(pixelX, pixelY - 1)
                    };

                    var averageHeight = (_heights.Max() + _heights.Min()) / 2f;
                    averageHeight /= terrainData.size.y;

                    var heightsArray = new float[1, 1] {{Mathf.Min(averageHeight, _heights[0])}};

                    terrainData.SetHeightsDelayLOD(pixelX, pixelY, heightsArray);
                }

                terrainData.SyncHeightmap();
            }

            
            /********************************************************************************************************************************/
            /********************************************************************************************************************************/
            // Remove Details

            if (settings.removeDetails)
            {
                int minX = (int)((pixels.Min(pixel => pixel.x) / (float)terrainData.heightmapResolution) * terrainData.detailWidth);
                int minY = (int)((pixels.Min(pixel => pixel.y) / (float)terrainData.heightmapResolution) * terrainData.detailHeight);

                int maxX = (int)((pixels.Max(pixel => pixel.x) / (float)terrainData.heightmapResolution) * terrainData.detailWidth);
                int maxY = (int)((pixels.Max(pixel => pixel.y) / (float)terrainData.heightmapResolution) * terrainData.detailHeight);

                int detailWidth = maxX - minX + 2;
                int detailHeight = maxY - minY + 2;

                detailUndo.detailPixels = new Vector2Int(minX, minY);
            
                for (int layer = 0; layer < terrainData.detailPrototypes.Length; layer++)
                {
                    detailUndo.detailLayers.Add(terrainData.GetDetailLayer(minX, minY, detailWidth, detailHeight, layer));
                    var detailLayer = terrainData.GetDetailLayer(minX, minY, detailWidth, detailHeight, layer);

                    for (int i = 0; i < pixels.Length; i++)
                    {
                        var pixelX = pixels[i].x;
                        var pixelY = pixels[i].y;

                        var detailX = (int)((pixelX / (float)terrainData.heightmapResolution) * terrainData.detailWidth) - minX;
                        var detailY = (int)((pixelY / (float)terrainData.heightmapResolution) * terrainData.detailHeight) - minY;     

                        for (int x = Math.Max(detailX - 1, 0); x <= detailX && x < detailWidth; x++)
                        {
                            for (int y = Math.Max(detailY - 1, 0); y <= detailY && y < detailHeight; y++)
                            { 
                                detailLayer[y, x] = 0; 
                            }
                        }
                    }

                    terrainData.SetDetailLayer(minX, minY, layer, detailLayer);
                }
            }
            
            /********************************************************************************************************************************/
            /********************************************************************************************************************************/
            // Slope Texture

            if (!settings.levelHeight) return;
            if (settings.slopeTextureIndex < 0) return;
            if (pixelsTextureSmooth.Length < 2) return;

            var ratioX = (float) terrainData.alphamapWidth / terrainData.heightmapResolution;
            var ratioY = (float) terrainData.alphamapHeight / terrainData.heightmapResolution;

            var isFirstPixel = true;
            int minAlphaMapX = 0, maxAlphaMapX = 0;
            int minAlphaMapY = 0, maxAlphaMapY = 0;

            for (var i = 0; i < pixelsTextureSmooth.Length; i++)
            {
                var pixelX = pixelsTextureSmooth[i].x;
                var pixelY = pixelsTextureSmooth[i].y;

                var alphaMapX = Mathf.FloorToInt(pixelX * ratioX);
                var alphaMapY = Mathf.FloorToInt(pixelY * ratioY);

                if (isFirstPixel)
                {
                    minAlphaMapX = maxAlphaMapX = alphaMapX;
                    minAlphaMapY = maxAlphaMapY = alphaMapY;
                    isFirstPixel = false;
                }
                else
                {
                    minAlphaMapX = Mathf.Min(minAlphaMapX, alphaMapX);
                    maxAlphaMapX = Mathf.Max(maxAlphaMapX, alphaMapX);
                    minAlphaMapY = Mathf.Min(minAlphaMapY, alphaMapY);
                    maxAlphaMapY = Mathf.Max(maxAlphaMapY, alphaMapY);
                }
            }

            var alphaMapWidth = terrainData.alphamapWidth;
            var alphaMapHeight = terrainData.alphamapHeight;

            if (minAlphaMapX < 0 || minAlphaMapY < 0 || maxAlphaMapX >= alphaMapWidth || maxAlphaMapY >= alphaMapHeight) return;

            var alphaWidth = maxAlphaMapX - minAlphaMapX + 1;
            var alphsHeight = maxAlphaMapY - minAlphaMapY + 1;
            var existingAlphamaps = terrainData.GetAlphamaps(minAlphaMapX, minAlphaMapY, alphaWidth, alphsHeight);

            if (settings.slopeTextureIndex >= existingAlphamaps.GetLength(2)) return;
            
            undoAlphamaps = (float[,,]) existingAlphamaps.Clone();
            undoAlphaPixels = new Vector2Int(minAlphaMapX, minAlphaMapY);

            for (var i = 0; i < pixelsTextureSmooth.Length; i++)
            {
                var rayOffsetAdd = new float3(0.01f, 0f, 0.01f);
                var slope01 = RaycastSlope(rayOffsetAdd);
                var slope02 = RaycastSlope(-rayOffsetAdd);

                float RaycastSlope(float3 offset)
                {
                    var ray = new Ray(texturePositions[i] + offset + (float3) raycastOffset, Vector3.down);
                    var hit = Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, settings.groundLayers);
                    if (!hit) return 0f;
                    var groundNormal = hitInfo.normal;
                    var slopeRad = Mathf.Acos(Vector3.Dot(groundNormal, Vector3.up));
                    var slopeDegrees = slopeRad * Mathf.Rad2Deg;
                    return slopeDegrees;
                }

                var slope = Mathf.Max(slope01, slope02);
                if (slope < 1f) continue;
                var slopeRatio = slope / 90f;
                var textureStrength = settings.slopeTextureStrength * textureStrengths[i] * slopeRatio;

                var pixelX = pixelsTextureSmooth[i].x;
                var pixelY = pixelsTextureSmooth[i].y;
                var alphaMapX = Mathf.FloorToInt(pixelX * ratioX);
                var alphaMapY = Mathf.FloorToInt(pixelY * ratioY);

                var currenLayerValue = existingAlphamaps[alphaMapY - minAlphaMapY, alphaMapX - minAlphaMapX, settings.slopeTextureIndex];
                var newLayerValue = Math.Max(currenLayerValue, textureStrength);
                existingAlphamaps[alphaMapY - minAlphaMapY, alphaMapX - minAlphaMapX, settings.slopeTextureIndex] = newLayerValue;
            }

            terrainData.SetAlphamaps(minAlphaMapX, minAlphaMapY, existingAlphamaps);
        }

        /********************************************************************************************************************************/

        private static TreeInstance[] RemoveTrees(Terrain terrain, Spline spline, float width)
        {
            var terrainData = terrain.terrainData;
            var treeInstances = terrainData.treeInstances;
            var splineNative = new NativeSpline(spline, Allocator.TempJob);
            var splinePositionsTreesNative = new NativeList<float3>(Allocator.TempJob);
            var treeInstancesNative = new NativeArray<TreeInstance>(treeInstances, Allocator.TempJob);
            var newTreeInstancesNative = new NativeList<TreeInstance>(Allocator.TempJob);
            var removedTreeInstancesNative = new NativeList<TreeInstance>(Allocator.TempJob);
                
            var treeJob = new TreeJob  
            {  
                _spline = splineNative,
                _splinePositions = splinePositionsTreesNative,
                _treeInstances = treeInstancesNative,
                _newTreeInstances = newTreeInstancesNative,
                _removedTreeInstances = removedTreeInstancesNative,
                _width = width,
                _terrainSize = terrainData.size,
                _terrainPosition = terrain.transform.position
            };  
                
            var randomCircleAreaJobHandle = treeJob.Schedule();  
            randomCircleAreaJobHandle.Complete();

            var newTreeInstances = newTreeInstancesNative.AsArray().ToArray();
            var treeUndo = removedTreeInstancesNative.AsArray().ToArray();
                
            splineNative.Dispose();
            splinePositionsTreesNative.Dispose();
            treeInstancesNative.Dispose();
            newTreeInstancesNative.Dispose();
            removedTreeInstancesNative.Dispose();

            terrainData.SetTreeInstances(newTreeInstances, false);

            return treeUndo;
        }
        
        /********************************************************************************************************************************/
        private static void CalculateTerrainData(ComponentSettings settings,
            Terrain terrain, List<float3> splinePositions, List<float3> splineTangents,
            List<float3> splinePositionsLeft, List<float3> splinePositionsRight,
            out Vector2Int[] pixels, out List<float[,]> heights,
            out Vector2Int[] pixelsSmooth,
            out Vector2Int[] pixelsTextureSmooth, out List<float> textureStrengths, out List<float3> texturePositions)
        {
            var terrainData = terrain.terrainData;
            var terrainPosition = terrain.GetPosition();
            var minPixelSize = GetMinHeightmapPixelSize(terrainData);

            var splinePositionsNative = new NativeArray<float3>(splinePositions.ToArray(), Allocator.TempJob);
            var splineTangentsNative = new NativeArray<float3>(splineTangents.ToArray(), Allocator.TempJob);
            var splinePositionsNativeLeft = new NativeArray<float3>(splinePositionsLeft.ToArray(), Allocator.TempJob);
            var splinePositionsNativeRight = new NativeArray<float3>(splinePositionsRight.ToArray(), Allocator.TempJob);

            var pixelsHashNative = new NativeHashSet<Vector2Int>(splinePositions.Count, Allocator.TempJob);
            var pixelsNative = new NativeList<Vector2Int>(Allocator.TempJob);
            var heightsFloatNative = new NativeList<float>(Allocator.TempJob);

            var pixelsSmoothHashNative = new NativeHashSet<Vector2Int>(splinePositions.Count, Allocator.TempJob);
            var pixelsSmoothNative = new NativeList<Vector2Int>(Allocator.TempJob);
            var heightsFloatSmoothNative = new NativeList<float>(Allocator.TempJob);

            var pixelsTextureSmoothHashNative = new NativeHashSet<Vector2Int>(splinePositions.Count, Allocator.TempJob);
            var pixelsTextureSmoothNative = new NativeList<Vector2Int>(Allocator.TempJob);
            var textureStrengthsNative = new NativeList<float>(Allocator.TempJob);
            var texturePositionsNative = new NativeList<float3>(Allocator.TempJob);

            var currentPositionsCalcNative = new NativeList<float3>(Allocator.TempJob);
            var lastPositionsCalcNative = new NativeList<float3>(Allocator.TempJob);
            
            // ExecuteCalculateTerrainData(splinePositionsNative, splineTangentsNative,
            //     splinePositionsNativeLeft, splinePositionsNativeRight,
            //     pixelsHashNative, pixelsNative, heightsFloatNative,
            //     pixelsSmoothHashNative, pixelsSmoothNative, heightsFloatSmoothNative,
            //     pixelsTextureSmoothHashNative, pixelsTextureSmoothNative, textureStrengthsNative, texturePositionsNative,
            //     currentPositionsCalcNative, lastPositionsCalcNative,
            //     minPixelSize, terrainPosition, terrainData.size, terrainData.heightmapResolution,
            //     settings.slopeTextureIndex, settings.slopeSmooth);
            
            var terrainDataJob = new CalculateTerrainDataJob
            {
                _splinePositions = splinePositionsNative,
                _splineTangents = splineTangentsNative,
                _splinePositionsLeft = splinePositionsNativeLeft,
                _splinePositionsRight = splinePositionsNativeRight,
            
                _pixelsHashNative = pixelsHashNative,
                _pixelsNative = pixelsNative,
                _heightsFloatNative = heightsFloatNative,
            
                _pixelsSmoothHashNative = pixelsSmoothHashNative,
                _pixelsSmoothNative = pixelsSmoothNative,
                _heightsFloatSmoothNative = heightsFloatSmoothNative,
            
                _pixelsTextureSmoothHashNative = pixelsTextureSmoothHashNative,
                _pixelsTextureSmoothNative = pixelsTextureSmoothNative,
                _textureStrengthsNative = textureStrengthsNative,
                _texturePositionsNative = texturePositionsNative,
            
                _currentPositionsCalcNative = currentPositionsCalcNative,
                _lastPositionsCalcNative = lastPositionsCalcNative,
                
                _minPixelSize = minPixelSize,
            
                _terrainPosition = terrainPosition,
                _terrainSize = terrainData.size,
                _terrainHeightmapResolution = terrainData.heightmapResolution,
            
                _slopeTextureIndex = settings.slopeTextureIndex,
                _slopeSmooth = settings.slopeSmooth
            };
            
            var jobHandle = terrainDataJob.Schedule();
            jobHandle.Complete();

            heights = new List<float[,]>();
            for (var i = 0; i < heightsFloatNative.Length; i++) heights.Add(new float[1, 1] {{heightsFloatNative[i]}});

            pixels = pixelsNative.AsArray().ToArray();
            pixelsSmooth = pixelsSmoothNative.AsArray().ToArray();
            pixelsTextureSmooth = pixelsTextureSmoothNative.AsArray().ToArray();
            textureStrengths = textureStrengthsNative.AsArray().ToList();
            texturePositions = texturePositionsNative.AsArray().ToList();


            splinePositionsNative.Dispose();
            splineTangentsNative.Dispose();
            splinePositionsNativeLeft.Dispose();
            splinePositionsNativeRight.Dispose();

            pixelsHashNative.Dispose();
            pixelsNative.Dispose();
            heightsFloatNative.Dispose();

            pixelsSmoothHashNative.Dispose();
            pixelsSmoothNative.Dispose();
            heightsFloatSmoothNative.Dispose();

            pixelsTextureSmoothHashNative.Dispose();
            pixelsTextureSmoothNative.Dispose();
            textureStrengthsNative.Dispose();
            texturePositionsNative.Dispose();

            currentPositionsCalcNative.Dispose();
            lastPositionsCalcNative.Dispose();
        }

        [BurstCompile]
        private struct CalculateTerrainDataJob : IJob
        {
            public NativeArray<float3> _splinePositions;
            public NativeArray<float3> _splineTangents;
            public NativeArray<float3> _splinePositionsLeft;
            public NativeArray<float3> _splinePositionsRight;

            public NativeHashSet<Vector2Int> _pixelsHashNative;
            public NativeList<Vector2Int> _pixelsNative;
            public NativeList<float> _heightsFloatNative;

            public NativeHashSet<Vector2Int> _pixelsSmoothHashNative;
            public NativeList<Vector2Int> _pixelsSmoothNative;
            public NativeList<float> _heightsFloatSmoothNative;

            public NativeHashSet<Vector2Int> _pixelsTextureSmoothHashNative;
            public NativeList<Vector2Int> _pixelsTextureSmoothNative;
            public NativeList<float> _textureStrengthsNative;
            public NativeList<float3> _texturePositionsNative;

            public NativeList<float3> _currentPositionsCalcNative;
            public NativeList<float3> _lastPositionsCalcNative;

            public float _minPixelSize;

            public float3 _terrainPosition;
            public float3 _terrainSize;
            public int _terrainHeightmapResolution;

            public int _slopeTextureIndex;
            public int _slopeSmooth;

            public void Execute()
            {
                ExecuteCalculateTerrainData(_splinePositions, _splineTangents, _splinePositionsLeft, _splinePositionsRight,
                    _pixelsHashNative, _pixelsNative, _heightsFloatNative,
                    _pixelsSmoothHashNative, _pixelsSmoothNative, _heightsFloatSmoothNative,
                    _pixelsTextureSmoothHashNative, _pixelsTextureSmoothNative, _textureStrengthsNative, _texturePositionsNative,
                    _currentPositionsCalcNative, _lastPositionsCalcNative,
                    _minPixelSize, _terrainPosition, _terrainSize, _terrainHeightmapResolution,
                    _slopeTextureIndex, _slopeSmooth);
            }
        }

        private static void ExecuteCalculateTerrainData(NativeArray<float3> _splinePositions, NativeArray<float3> _splineTangents,
            NativeArray<float3> _splinePositionsLeft, NativeArray<float3> _splinePositionsRight,
            NativeHashSet<Vector2Int> _pixelsHashNative, NativeList<Vector2Int> _pixelsNative, NativeList<float> _heightsFloatNative,
            NativeHashSet<Vector2Int> _pixelsSmoothHashNative, NativeList<Vector2Int> _pixelsSmoothNative,
            NativeList<float> _heightsFloatSmoothNative,
            NativeHashSet<Vector2Int> _pixelsTextureSmoothHashNative, NativeList<Vector2Int> _pixelsTextureSmoothNative,
            NativeList<float> _textureStrengthsNative,
            NativeList<float3> _texturePositionsNative,
            NativeList<float3> _currentPositionsCalcNative, NativeList<float3> _lastPositionsCalcNative,
            float _minPixelSize, float3 _terrainPosition, float3 _terrainSize, int _terrainHeightmapResolution,
            int _slopeTextureIndex, int _slopeSmooth)
        {
            var distance = 0f;
            var widthSteps = 0;
            var distancePerStep = 0f;
            var pixelSizeSq = math.pow(_minPixelSize * 2f, 2f);
            
            for (var i = 0; i < _splinePositions.Length; i++)
            {
                var tangent = math.normalizesafe(_splineTangents[i]);
                tangent.y = 0f;
                var tangentPerp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(tangent);

                var posLeft = _splinePositionsLeft[i];
                var posRight = _splinePositionsRight[i];

                // PGInformationUtility.CreateSphere(_splinePositions[i], i + " " +  "Pos");
                // PGInformationUtility.CreateSphere(posLeft, "Left");
                // PGInformationUtility.CreateSphere(posRight, "Right");
                
                if (widthSteps == 0)
                {
                    distance = math.distance(posLeft, posRight);
                    widthSteps = Mathf.CeilToInt(distance / _minPixelSize);
                    distancePerStep = distance / widthSteps;
                }

                for (var step = 0; step <= widthSteps - 1; step++) // Right to left on each spline length position
                {
                    var t = step / (float) widthSteps;
                    var pos01 = math.lerp(posRight, posLeft, t);
                    _currentPositionsCalcNative.Add(pos01);
                    
                    if (i == 0) continue;

                    var pos02 = pos01 - tangentPerp * distancePerStep;
                    var pos01Last = _lastPositionsCalcNative[step]; // Last positions are one row behind
                    var pos02Last = pos01Last - tangentPerp * distancePerStep;

                    if (math.distancesq(pos01Last, pos01) > pixelSizeSq) // Correcting issue when there's space between spline positions.
                    {
                        pos01Last = pos01 - tangentPerp * _minPixelSize;
                        pos02Last = pos02 - tangentPerp * _minPixelSize;    
                    }
                    
                    var textureStrength = 0f; // For savety to get all pixels textured.
                    if (step <= 1 || step >= widthSteps - 2) textureStrength = 1f;
                    if (_slopeTextureIndex == -1) textureStrength = 0f;

                    FillHeightsDataArrays(_pixelsHashNative, _pixelsNative, _heightsFloatNative,
                        _terrainPosition, _terrainSize, _terrainHeightmapResolution,
                        pos01, pos01Last, pos02, pos02Last,
                        textureStrength, _pixelsTextureSmoothHashNative, _pixelsTextureSmoothNative,
                        _textureStrengthsNative, _texturePositionsNative);

                    if (step != widthSteps - 1) continue; // Slope Smoothing

                    for (var j = 0; j < _slopeSmooth; j++)
                    {
                        textureStrength = (float) (_slopeSmooth - j) / _slopeSmooth;
                        if (_slopeTextureIndex < 0) textureStrength = 0f;

                        var leftAdd = 1;
                        // if (tangentPerp.x > 0) leftAdd = -1; // Offsetting the pixels here if needed.
                        var rightAdd = 0;
                        // if (tangentPerp.z > 0) rightAdd = 1;

                        var pos01Left = pos01 - tangentPerp * ((j + leftAdd) * distancePerStep);
                        var pos02Left = pos02 - tangentPerp * ((j + leftAdd) * distancePerStep);
                        var pos01LastLeft = pos01Last - tangentPerp * ((j + leftAdd) * distancePerStep);
                        var pos02LastLeft = pos02Last - tangentPerp * ((j + leftAdd) * distancePerStep);

                        var pos01Right = pos01 + tangentPerp * distance + tangentPerp * ((j + rightAdd) * distancePerStep);
                        var pos02Right = pos02 + tangentPerp * distance + tangentPerp * ((j + rightAdd) * distancePerStep);
                        var pos01LastRight = pos01Last + tangentPerp * distance + tangentPerp * ((j + rightAdd) * distancePerStep);
                        var pos02LastRight = pos02Last + tangentPerp * distance + tangentPerp * ((j + rightAdd) * distancePerStep);

                        FillHeightsDataArrays(_pixelsSmoothHashNative, _pixelsSmoothNative, _heightsFloatSmoothNative,
                            _terrainPosition, _terrainSize, _terrainHeightmapResolution,
                            pos01Left, pos01LastLeft, pos02Left, pos02LastLeft,
                            textureStrength, _pixelsTextureSmoothHashNative, _pixelsTextureSmoothNative,
                            _textureStrengthsNative, _texturePositionsNative);

                        FillHeightsDataArrays(_pixelsSmoothHashNative, _pixelsSmoothNative, _heightsFloatSmoothNative,
                            _terrainPosition, _terrainSize, _terrainHeightmapResolution,
                            pos01Right, pos01LastRight, pos02Right, pos02LastRight,
                            textureStrength, _pixelsTextureSmoothHashNative, _pixelsTextureSmoothNative,
                            _textureStrengthsNative, _texturePositionsNative);
                    }
                }

                _lastPositionsCalcNative.Clear();
                for (var j = 0; j < _currentPositionsCalcNative.Length; j++) _lastPositionsCalcNative.Add(_currentPositionsCalcNative[j]);
                _currentPositionsCalcNative.Clear();
            }
        }

        private static void FillHeightsDataArrays(NativeHashSet<Vector2Int> pixelsHash, NativeList<Vector2Int> pixels, NativeList<float> heights,
            float3 terrainPosition, float3 terrainSize, int heightmapResolution, float3 pos01, float3 pos01Last, float3 pos02, float3 pos02Last,
            float textureStrength, NativeHashSet<Vector2Int> texturePixelsHash, NativeList<Vector2Int> texturePixels,
            NativeList<float> textureStrengths, NativeList<float3> texturePositions)
        {
            pos01.y -= terrainPosition.y;
            pos01Last.y -= terrainPosition.y;
            pos02.y -= terrainPosition.y;
            pos02Last.y -= terrainPosition.y;

            var minPosX = pos01.x;
            if (pos01Last.x < minPosX) minPosX = pos01Last.x;
            if (pos02.x < minPosX) minPosX = pos02.x;
            if (pos02Last.x < minPosX) minPosX = pos02Last.x;
            minPosX -= terrainPosition.x;

            var minPosZ = pos01.z;
            if (pos01Last.z < minPosZ) minPosZ = pos01Last.z;
            if (pos02.z < minPosZ) minPosZ = pos02.z;
            if (pos02Last.z < minPosZ) minPosZ = pos02Last.z;
            minPosZ -= terrainPosition.z;

            var posX = (int) (minPosX / terrainSize.x * heightmapResolution);
            var posZ = (int) (minPosZ / terrainSize.z * heightmapResolution);
            
            var pixelSizeX = terrainSize.x / (heightmapResolution - 1); // -1 important! x pixels will only have x-1 gaps.
            var pixelSizeZ = terrainSize.z / (heightmapResolution - 1);

            for (var i = 0; i < 3; i++) // Looping with 3, because 2 may skip some
            for (var j = 0; j < 3; j++) // Looping with 3, because 2 may skip some
            {
                var terrainX = (posX + i) * pixelSizeX + terrainPosition.x;
                var terrainZ = (posZ + j) * pixelSizeZ + terrainPosition.z;

                var worldPosition = new float3(terrainX, pos01.y, terrainZ);
                
                if (!IsPositionInside(worldPosition, pos01, pos01Last, pos02, pos02Last)) continue;

                var worldPosition2D = new float2(worldPosition.x, worldPosition.z);
                var pos012D = new float2(pos01.x, pos01.z);
                var pos01Last2D = new float2(pos01Last.x, pos01Last.z);

                var tan2D = pos012D - pos01Last2D;
                var t = math.dot(worldPosition2D - pos01Last2D, tan2D) / math.dot(tan2D, tan2D);
                var objHeight = math.lerp(pos01Last.y, pos01.y, t);
                worldPosition.y = objHeight + terrainPosition.y;
                objHeight /= terrainSize.y;

                var _pixels = new Vector2Int(posX + i, posZ + j);

                var didAddToPixelsHash = pixelsHash.Add(_pixels);

                if (didAddToPixelsHash)
                {
                    pixels.Add(_pixels);
                    heights.Add(objHeight);

                    if (textureStrength > 0.001f)
                    {
                        var didAddToTexturePixelsHash = texturePixelsHash.Add(_pixels);

                        if (didAddToTexturePixelsHash)
                        {
                            texturePixels.Add(_pixels);
                            textureStrengths.Add(textureStrength);
                            texturePositions.Add(worldPosition);
                        }
                    }
                }
            }
        }

        private static bool IsPositionInside(Vector3 center, Vector3 pos1, Vector3 pos2, Vector3 pos3, Vector3 pos4)
        {
            var center2D = new float2(center.x, center.z);
            var pos12D = new float2(pos1.x, pos1.z);
            var pos22D = new float2(pos2.x, pos2.z);
            var pos32D = new float2(pos3.x, pos3.z);
            var pos42D = new float2(pos4.x, pos4.z);

            float maxX, maxY;

            var minX = maxX = pos12D.x;
            if (pos22D.x < minX) minX = pos22D.x;
            if (pos32D.x < minX) minX = pos32D.x;
            if (pos42D.x < minX) minX = pos42D.x;
            if (pos22D.x > maxX) maxX = pos22D.x;
            if (pos32D.x > maxX) maxX = pos32D.x;
            if (pos42D.x > maxX) maxX = pos42D.x;

            var minY = maxY = pos12D.y;
            if (pos22D.y < minY) minY = pos22D.y;
            if (pos32D.y < minY) minY = pos32D.y;
            if (pos42D.y < minY) minY = pos42D.y;
            if (pos22D.y > maxY) maxY = pos22D.y;
            if (pos32D.y > maxY) maxY = pos32D.y;
            if (pos42D.y > maxY) maxY = pos42D.y;

            return center2D.x >= minX && center2D.x <= maxX && center2D.y >= minY && center2D.y <= maxY;
        }

        private static float GetMinHeightmapPixelSize(TerrainData terrainData)
        {
            var heightmapPixelSize = new Vector2(terrainData.size.x / (terrainData.heightmapResolution - 1),
                terrainData.size.z / (terrainData.heightmapResolution - 1));
            var minPixelSize = math.min(heightmapPixelSize.x, heightmapPixelSize.y);
            return minPixelSize;
        }
        
        /********************************************************************************************************************************/
        
        [BurstCompile]  
        private struct TreeJob : IJob  
        {  
            public NativeSpline _spline;  
            public NativeList<float3> _splinePositions;  
            public NativeArray<TreeInstance> _treeInstances;
            public NativeList<TreeInstance> _newTreeInstances;
            public NativeList<TreeInstance> _removedTreeInstances;
            public float _width;
            public float3 _terrainSize;
            public float3 _terrainPosition;
            
  
            public void Execute()  
            {        
                var normalizedDistance = _width * 0.5f / _spline.GetLength();
                for (var t = 0f; t <= 1f; t += normalizedDistance) _splinePositions.Add(_spline.EvaluatePosition(t));

                for (int i = _treeInstances.Length - 1; i >= 0; i--)
                {
                    float3 worldPosition = Vector3.Scale(_treeInstances[i].position, _terrainSize);
                    worldPosition += _terrainPosition;
                    var worldPos2D = new float2(worldPosition.x, worldPosition.z);

                    var added = false;
                    for (int j = 1; j < _splinePositions.Length; j++)
                    {
                        if (math.distance(worldPos2D, new float2(_splinePositions[j].x, _splinePositions[j].z) ) < _width * 0.6f)
                        {
                            _removedTreeInstances.Add(_treeInstances[i]);
                            added = true;
                            break;
                        }
                    }
                    if(!added) _newTreeInstances.Add(_treeInstances[i]);
                }
            }
        }
    }
}