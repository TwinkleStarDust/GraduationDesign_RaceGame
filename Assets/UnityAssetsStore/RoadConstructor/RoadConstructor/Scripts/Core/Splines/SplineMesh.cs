// ----------------------------------------------------
// Road Constructor
// Copyright (c) Pampel Games e.K. All Rights Reserved.
// https://www.pampelgames.com
// ----------------------------------------------------

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
    public class SplineMeshParameter
    {
        public readonly float partWidth;
        public readonly float partLength;
        public readonly int resolution;
        public readonly RoadLengthUV roadLengthUV;
        public readonly Spline spline;

        public SplineMeshParameter(float partWidth, float partLength, int resolution, RoadLengthUV roadLengthUV, Spline spline)
        {
            this.partWidth = partWidth;
            this.partLength = partLength;
            this.resolution = resolution;
            this.roadLengthUV = roadLengthUV;
            this.spline = spline;
        }
    }

    public static class SplineMesh
    {
        public static Mesh CreateCombinedSplineMesh(List<Lane> lanes, SplineMeshParameter splineMeshParameter,
            out Material[] _materials)
        {
            CreateMultipleSplineMeshes(lanes, splineMeshParameter,
                out var _meshes, out var _combinedMaterials);

            PGMeshUtility.CombineAndPackMeshes(_combinedMaterials, _meshes, out var combinedMaterials, out var combinedMesh);

            _materials = combinedMaterials.ToArray();
            return combinedMesh;
        }

        public static void CreateMultipleSplineMeshes(List<Lane> lanes, SplineMeshParameter splineMeshParameter,
            out List<Mesh> _meshes, out List<Material> _materials, float tStart = 0f, float tEnd = 1f, float widthStart = 1f, float widthEnd = 1f)
        {
            _meshes = new List<Mesh>();
            _materials = new List<Material>();
            
            for (var i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];

                var splineEdges = lane.splineEdges;
                if (splineEdges.Length == 0) continue;

                var mesh = new Mesh();
                CreateSplineMesh(mesh, splineEdges, splineMeshParameter, tStart, tEnd, widthStart, widthEnd);

                _meshes.Add(mesh);
                _materials.Add(lane.material);
            }
        }

        public static void CreateSplineMesh(Mesh mesh, SplineEdge[] splineEdges, SplineMeshParameter splineMeshParameter, 
            float tStart, float tEnd, float widthStart = 1f, float widthEnd = 1f)
        {
            var spline = splineMeshParameter.spline;
            var partWidth = splineMeshParameter.partWidth;
            var partLength = splineMeshParameter.partLength;
            var resolution = splineMeshParameter.resolution;
            var roadLengthUV = splineMeshParameter.roadLengthUV;
            
            mesh.Clear();

            var splineLeft = new Spline(spline);
            var splineRight = new Spline(spline);
            
            RoadSplineUtility.OffsetSplineX(splineLeft, -partWidth * 0.5f, widthStart, widthEnd);
            RoadSplineUtility.OffsetSplineX(splineRight, partWidth * 0.5f, widthStart, widthEnd);
            
            var vertexStructs = new NativeList<VertexStruct01>(Allocator.TempJob);
            var nativeSpline = new NativeSpline(spline, Allocator.TempJob);
            var nativeSplineLeft = new NativeSpline(splineLeft, Allocator.TempJob);
            var nativeSplineRight = new NativeSpline(splineRight, Allocator.TempJob);

            var trianglesList = new NativeList<int>(Allocator.TempJob);
            var splineEdgesNative = new NativeArray<SplineEdge>(splineEdges, Allocator.TempJob);

            var job = new CreateSplineMeshJob
            {
                _splineEdges = splineEdgesNative,

                _partWidth = partWidth,
                _partLength = partLength,
                _partResolution = resolution,
                _roadLengthUV = roadLengthUV,

                _tStart = tStart,
                _tEnd = tEnd,

                _vertexStructs = vertexStructs,
                _trianglesList = trianglesList,
                _nativeSpline = nativeSpline,
                _nativeSplineLeft = nativeSplineLeft,
                _nativeSplineRight = nativeSplineRight
            };

            var handle = job.Schedule();
            handle.Complete();

            PGMeshAPIUtility.SetBufferData01(mesh, vertexStructs.AsArray(), trianglesList.AsArray());
            PGMeshAPIUtility.SetSubMesh(mesh, 0, vertexStructs.Length, trianglesList.Length);
            PGMeshUtility.RecalculateMeshData(mesh, false);

            vertexStructs.Dispose();
            trianglesList.Dispose();
            nativeSpline.Dispose();
            nativeSplineLeft.Dispose();
            nativeSplineRight.Dispose();
            splineEdgesNative.Dispose();
        }


        [BurstCompile]
        private struct CreateSplineMeshJob : IJob
        {
            public NativeArray<SplineEdge> _splineEdges;
            public float _partWidth;
            public float _partLength;
            public int _partResolution;
            public RoadLengthUV _roadLengthUV;
            public float _tStart;
            public float _tEnd;

            public NativeList<VertexStruct01> _vertexStructs;
            public NativeList<int> _trianglesList;
            public NativeSpline _nativeSpline;
            public NativeSpline _nativeSplineLeft;
            public NativeSpline _nativeSplineRight;
            
            public void Execute()
            {
                SplineMeshExecute(_splineEdges, _partWidth, _partLength, _partResolution,
                    _roadLengthUV, _tStart, _tEnd, _vertexStructs, _trianglesList,
                    _nativeSpline, _nativeSplineLeft, _nativeSplineRight);
            }
        }

        private static void SplineMeshExecute(NativeArray<SplineEdge> _splineEdges,
            float _partWidth, float _partLength, int _partResolution, RoadLengthUV _roadLengthUV,
            float _tStart, float _tEnd,
            NativeList<VertexStruct01> _vertexStructs, NativeList<int> _trianglesList,
            NativeSpline _nativeSpline, NativeSpline _nativeSplineLeft, NativeSpline _nativeSplineRight)
        {
            var resCount = 0;
            var splineLength = _nativeSpline.GetLength();
            if (splineLength <= 0f) return;
            if (splineLength > Constants.MaxSplineLength) return;

            if (_roadLengthUV == RoadLengthUV.Stretch)
            {
                var numberOfParts = math.round(splineLength / _partLength);
                if (numberOfParts == 0) numberOfParts = 1;
                _partLength = splineLength / numberOfParts;
            }

            var segmentLength = _partLength / _partResolution;
            var distancesAmount = math.floor(splineLength / segmentLength);

            if (distancesAmount == 0) distancesAmount = 1;

            var t = segmentLength / splineLength;

            for (var i = 0; i < distancesAmount + 2; i++) // +2 to make sure last is added.
            {
                var tCalc = i * t + _tStart;
                var uvValueY = (float) resCount / _partResolution;

                var last = tCalc >= _tEnd;
                if (last)
                {
                    var tCalcPart = _tEnd - ((i - 1) * t + _tStart);
                    var tCalcRatio = tCalcPart / t;

                    var lastUvValue = (float) (resCount - 1) / _partResolution;
                    var uvValuePart = uvValueY - lastUvValue;

                    uvValueY = uvValuePart * tCalcRatio + lastUvValue;
                    tCalc = _tEnd;
                }

                AddEdgeLoop(tCalc, uvValueY);
                if (last) break;
            }


            // Triangles
            var verticesPerRing = _splineEdges.Length;
            var totalRings = _vertexStructs.Length / verticesPerRing;

            for (var i = 0; i < totalRings - 1; i++)
            for (var j = 0; j < verticesPerRing - 1; j++)
            {
                var v0 = i * verticesPerRing + j;
                var v1 = v0 + 1;
                var v2 = v0 + verticesPerRing;
                var v3 = v2 + 1;

                _trianglesList.Add(v0);
                _trianglesList.Add(v2);
                _trianglesList.Add(v1);

                _trianglesList.Add(v2);
                _trianglesList.Add(v3);
                _trianglesList.Add(v1);
            }

            return;

            void AddEdgeLoop(float _t, float uvValueY)
            {
                _nativeSpline.Evaluate(_t, out var position, out var tangent, out var upVector);
                var positionLeft = _nativeSplineLeft.EvaluatePosition(_t);
                var positionRight = _nativeSplineRight.EvaluatePosition(_t);

                upVector = math.up(); // Using math.up() for upVector, otherwise mesh gets distorted on slopes.

                var left = math.normalizesafe(math.cross(tangent, upVector) * -1f);

                var splineEdges = _splineEdges;
                var vertexStructs = _vertexStructs;

                ProcessSplineEdge(uvValueY);

                if (resCount == _partResolution) // Need to add additional edge for each part, so the UVs are seperated
                    ProcessSplineEdge(0f);

                resCount++;
                if (resCount > _partResolution) resCount = 1;

                return;

                void ProcessSplineEdge(float _uvValueY)
                {
                    for (var i = 0; i < splineEdges.Length; i++)
                    {
                        var posX = left * splineEdges[i].position.x;
                        var posY = new float3(0f, splineEdges[i].position.y, 0f);

                        var positionSide = splineEdges[i].position.x > 0 ? positionRight : positionLeft;
                        var centerDistance = math.distance(position + posX, position) / (_partWidth * 0.5f);
                        var point = math.lerp(position, positionSide, centerDistance);
                        point.y = position.y; // Side splines have wrong heights in curves.
                        point += posY;

                        var uv = new float2(splineEdges[i].uvX, _uvValueY);

                        var normal = RotateAroundTangent(upVector, tangent, splineEdges[i].normalRotation);
                        AddVertexStruct(point, normal, uv);
                    }

                    void AddVertexStruct(float3 vertex, float3 normal, float2 uv)
                    {
                        vertexStructs.Add(new VertexStruct01
                        {
                            vertex = vertex, normal = normal, uv = uv
                        });
                    }

                    float3 RotateAroundTangent(float3 _upVector, float3 _tangent, float _degrees)
                    {
                        _degrees *= -1f; // Unity spline tangents go backward direction.
                        _upVector = math.normalizesafe(_upVector);
                        _tangent = math.normalizesafe(_tangent);
                        var radians = math.radians(_degrees);
                        var rotation = quaternion.AxisAngle(_tangent, radians);
                        var rotatedVector = math.mul(rotation, _upVector);
                        return rotatedVector;
                    }
                }
            }
        }


        /********************************************************************************************************************************/
        /********************************************************************************************************************************/

        // Simple connection without side-splines
        public static void CreateSplineMeshSimple(Mesh mesh, Spline spline, SplineEdge[] splineEdges,
            float partLength, int resolution, RoadLengthUV roadLengthUV, float tStart, float tEnd, float widthStart = 1f, float widthEnd = 1f)
        {
            mesh.Clear();
            
            var vertexStructs = new NativeList<VertexStruct01>(Allocator.TempJob);
            var nativeSpline = new NativeSpline(spline, Allocator.TempJob);
            var trianglesList = new NativeList<int>(Allocator.TempJob);

            var splineEdgesNative = new NativeArray<SplineEdge>(splineEdges, Allocator.TempJob);
            
            var job = new CreateSplineMeshJobSimple
            {
                _splineEdges = splineEdgesNative,

                _partLength = partLength,
                _partResolution = resolution,
                _roadLengthUV = roadLengthUV,

                _tStart = tStart,
                _tEnd = tEnd,
                
                _widthStart = widthStart,
                _widthEnd = widthEnd,

                _vertexStructs = vertexStructs,
                _trianglesList = trianglesList,
                _nativeSpline = nativeSpline
            };

            var handle = job.Schedule();
            handle.Complete();
            
            PGMeshAPIUtility.SetBufferData01(mesh, vertexStructs.AsArray(), trianglesList.AsArray());
            PGMeshAPIUtility.SetSubMesh(mesh, 0, vertexStructs.Length, trianglesList.Length);
            PGMeshUtility.RecalculateMeshData(mesh, false);
            
            vertexStructs.Dispose();
            trianglesList.Dispose();
            nativeSpline.Dispose();
            splineEdgesNative.Dispose();
        }

        [BurstCompile]
        private struct CreateSplineMeshJobSimple : IJob 
        {
            public NativeArray<SplineEdge> _splineEdges;
            public float _partLength;
            public int _partResolution;
            public RoadLengthUV _roadLengthUV;
            public float _tStart;
            public float _tEnd;
            public float _widthStart;
            public float _widthEnd;

            public NativeList<VertexStruct01> _vertexStructs;
            public NativeList<int> _trianglesList;
            public NativeSpline _nativeSpline;

            private int resCount;

            public void Execute()
            {
                var splineLength = _nativeSpline.GetLength();
                if (splineLength <= 0f) return;
                if (splineLength > Constants.MaxSplineLength) return;

                if (_roadLengthUV == RoadLengthUV.Stretch)
                {
                    var numberOfParts = math.round(splineLength / _partLength);
                    if (numberOfParts == 0) numberOfParts = 1;
                    _partLength = splineLength / numberOfParts;
                }

                var segmentLength = _partLength / _partResolution;
                var distancesAmount = math.floor(splineLength / segmentLength);

                if (distancesAmount == 0) distancesAmount = 1;

                var t = segmentLength / splineLength;

                for (var i = 0; i < distancesAmount + 2; i++) // +2 to make sure last is added.
                {
                    var tCalc = i * t + _tStart;
                    var uvValueY = (float) resCount / _partResolution;

                    var last = tCalc >= _tEnd;
                    if (last)
                    {
                        var tCalcPart = _tEnd - ((i - 1) * t + _tStart);
                        var tCalcRatio = tCalcPart / t;

                        var lastUvValue = (float) (resCount - 1) / _partResolution;
                        var uvValuePart = uvValueY - lastUvValue;

                        uvValueY = uvValuePart * tCalcRatio + lastUvValue;
                        tCalc = _tEnd;
                    }

                    AddEdgeLoop(tCalc, uvValueY);
                    if (last) break;
                }


                // Triangles
                var verticesPerRing = _splineEdges.Length;
                var totalRings = _vertexStructs.Length / verticesPerRing;

                for (var i = 0; i < totalRings - 1; i++)
                for (var j = 0; j < verticesPerRing - 1; j++)
                {
                    var v0 = i * verticesPerRing + j;
                    var v1 = v0 + 1;
                    var v2 = v0 + verticesPerRing;
                    var v3 = v2 + 1;

                    _trianglesList.Add(v0);
                    _trianglesList.Add(v2);
                    _trianglesList.Add(v1);

                    _trianglesList.Add(v2);
                    _trianglesList.Add(v3);
                    _trianglesList.Add(v1);
                }
            }


            private void AddEdgeLoop(float t, float uvValueY)
            {
                _nativeSpline.Evaluate(t, out var position, out var tangent, out var upVector);

                upVector = math.up(); // Using math.up() for upVector, otherwise mesh gets distorted on slopes.

                var left = math.normalizesafe(math.cross(tangent, upVector) * -1f);

                var witdhReduction = math.lerp(_widthStart, _widthEnd, t);

                var splineEdges = _splineEdges;
                var vertexStructs = _vertexStructs;

                ProcessSplineEdge(uvValueY);

                if (resCount == _partResolution) // Need to add additional edge for each part, so the UVs are seperated
                    ProcessSplineEdge(0f);


                resCount++;
                if (resCount > _partResolution) resCount = 1;

                return;

                void ProcessSplineEdge(float _uvValueY)
                {
                    for (var i = 0; i < splineEdges.Length; i++)
                    {
                        var posX = left * splineEdges[i].position.x * witdhReduction;
                        var posY = new float3(0f, splineEdges[i].position.y, 0f);
                        var p = position + posX + posY;
                        var uv = new float2(splineEdges[i].uvX, _uvValueY);

                        var normal = RotateAroundTangent(upVector, tangent, splineEdges[i].normalRotation);
                        AddVertexStruct(p, normal, uv);
                    }

                    void AddVertexStruct(float3 vertex, float3 normal, float2 uv)
                    {
                        vertexStructs.Add(new VertexStruct01
                        {
                            vertex = vertex, normal = normal, uv = uv
                        });
                    }

                    float3 RotateAroundTangent(float3 _upVector, float3 _tangent, float _degrees)
                    {
                        _degrees *= -1f; // Unity spline tangents go backward direction.
                        _upVector = math.normalizesafe(_upVector);
                        _tangent = math.normalizesafe(_tangent);
                        var radians = math.radians(_degrees);
                        var rotation = quaternion.AxisAngle(_tangent, radians);
                        var rotatedVector = math.mul(rotation, _upVector);
                        return rotatedVector;
                    }
                }
            }
        }
    }
}