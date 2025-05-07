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
    internal static class RoadEndCreation
    {
        public static List<IntersectionObject> CreateMissingEndObjects(ComponentSettings settings, SO_DefaultReferences _DefaultReferences,
            ConstructionObjects constructionObjects)
        {
            var endObjects = new List<IntersectionObject>();
            var newRoads = constructionObjects.CombinedNewRoads;
            for (var i = 0; i < newRoads.Count; i++)
            {
                var connections = newRoads[i].Connections;

                if (connections.Count >= 2) continue;

                var startConnected = false;
                var endConnected = false;

                var spline = newRoads[i].splineContainer.Spline;
                var center = newRoads[i].meshRenderer.bounds.center;

                var startKnot = spline[0];
                var endKnot = spline[^1];

                for (var j = 0; j < connections.Count; j++)
                {
                    var connectionCenter = connections[j].meshRenderer.bounds.center;

                    if (math.distancesq(startKnot.Position, connectionCenter) < math.distancesq(endKnot.Position, connectionCenter))
                        startConnected = true;
                    else
                        endConnected = true;
                }

                if (!startConnected) _CreateEndObject(startKnot, true);
                if (!endConnected) _CreateEndObject(endKnot, false);
                continue;

                void _CreateEndObject(BezierKnot knot, bool startPart)
                {
                    var forward = PGTrigonometryUtility.DirectionalTangentToPointXZ(center, knot.Position, knot.TangentOut);
                    var endObject = CreateEndObject(settings, _DefaultReferences, newRoads[i].roadDescr, startPart,
                        knot.Position, forward);
                    constructionObjects.newIntersections.Add(endObject);
                    newRoads[i].AddConnection(endObject);
                    endObject.AddConnection(newRoads[i]);
                    endObjects.Add(endObject);
                }
            }

            return endObjects;
        }


        public static void CreateEndObjectClasses(RoadDescr roadDescr, Spline spline,
            out EndObjectClass endObjectClass01, out EndObjectClass endObjectClass02)
        {
            var knot01 = spline.Knots.First();
            var knot02 = spline.Knots.Last();

            var endSpline01 = CreateEndSpline(roadDescr, knot01.Position, ref knot01.TangentOut);
            var endSpline02 = CreateEndSpline(roadDescr, knot02.Position, ref knot02.TangentIn);

            endObjectClass01 = new EndObjectClass(roadDescr, endSpline01);
            endObjectClass02 = new EndObjectClass(roadDescr, endSpline02);
        }

        public static void CreateEndObjects(ComponentSettings settings, SO_DefaultReferences _DefaultReferences, RoadDescr roadDescr, Spline spline,
            out IntersectionObject endObject01, out IntersectionObject endObject02)
        {
            var knot01 = spline.Knots.First();
            var knot02 = spline.Knots.Last();

            endObject01 = CreateEndObject(settings, _DefaultReferences, roadDescr,
                true, knot01.Position, knot01.TangentOut);
            endObject02 = CreateEndObject(settings, _DefaultReferences, roadDescr,
                false, knot02.Position, knot02.TangentIn);
        }

        public static IntersectionObject CreateEndObject(ComponentSettings settings, SO_DefaultReferences _DefaultReferences,
            RoadDescr roadDescr, bool startPart, float3 position, float3 forward)
        {
            var road = roadDescr.road;
            var combinedInstances = new List<CombineInstance>();
            var combinedMaterials = new List<Material>();

            var spline = CreateEndSpline(roadDescr, position, ref forward);
            var forwardNormalized = math.normalizesafe(forward);

            /********************************************************************************************************************************/
            if (settings.roadEnd == RoadEnd.Rounded)
            {
                if (roadDescr.lanesRoadEnd.Count == 0)
                {
                    /********************************************************************************************************************************/
                    // One circle.

                    var circleCombine01 = Circle(90f, Mathf.Max(10, roadDescr.detailResolution * 2), 180f);
                    circleCombine01.PGTranslateCombine(new Vector3(0, settings.baseRoadHeight, 0));

                    combinedInstances.Add(circleCombine01);
                    combinedMaterials.Add(roadDescr.intersectionMaterial);

                    /********************************************************************************************************************************/
                    // Rounded lanes

                    if (roadDescr.lanesLeft.Count > 0)
                    {
                        CreateQuadrantSplineMesh(roadDescr.lanesLeftOffset, true);
                        CreateQuadrantSplineMesh(roadDescr.lanesRightOffset, false);
                    }

                    void CreateQuadrantSplineMesh(List<Lane> lanes, bool left)
                    {
                        for (var i = 0; i < lanes.Count; i++)
                        {
                            var lane = lanes[i];
                            var circleForwardRotation = quaternion.RotateY(startPart == left ? math.radians(180f) : math.radians(90f));
                            var circleForward = math.mul(circleForwardRotation, forward);
                            var splineCircleRotation = quaternion.LookRotationSafe(math.normalizesafe(circleForward), math.up());

                            var quadrantSpline =
                                SplineCircle.CreateQuarterCircleSpline(roadDescr.sideLanesCenterDistance, position, splineCircleRotation, !left);

                            var quadrantSplineMesh = new Mesh();
                            SplineMesh.CreateSplineMeshSimple(quadrantSplineMesh, quadrantSpline, lane.splineEdges, road.length,
                                roadDescr.detailResolution, RoadLengthUV.Cut, 0f, 1f);

                            var splineCircleCombine = new CombineInstance
                            {
                                transform = Matrix4x4.identity,
                                mesh = quadrantSplineMesh
                            };

                            combinedInstances.Add(splineCircleCombine);
                            combinedMaterials.Add(lane.material);
                        }
                    }
                }
                else
                {
                    /********************************************************************************************************************************/
                    // Simple straight road end.
                    var splineMeshParameter = new SplineMeshParameter(roadDescr.width, road.length * 0.5f, 1, settings.roadLengthUV, spline);

                    var newSplineMesh = SplineMesh.CreateCombinedSplineMesh(roadDescr.lanesRoadEnd, splineMeshParameter, out var _materials);

                    combinedInstances.Add(new CombineInstance
                    {
                        mesh = newSplineMesh,
                        transform = Matrix4x4.identity
                    });
                    combinedMaterials.AddRange(_materials);
                }
            }
            /********************************************************************************************************************************/
            else if (settings.roadEnd == RoadEnd.None)
            {
                /********************************************************************************************************************************/
                // One Rectangle.

                var rectangleCombine = Rectangle();
                rectangleCombine.PGTranslateCombine(new Vector3(0, settings.baseRoadHeight, 0));

                combinedInstances.Add(rectangleCombine);
                combinedMaterials.Add(_DefaultReferences.invisibleMaterial);
            }


            /********************************************************************************************************************************/
            // Small closing rectangles for the directly closed parts

            var tangent = PGTrigonometryUtility.DirectionalTangentToPointXZ(position - forward, position, forward);

            for (var i = 0; i < roadDescr.lanesMiddle.Count; i++)
            {
                var lane = roadDescr.lanesMiddle[i];
                if (lane.height <= 0f) continue;
                if (!lane.closedEnds) continue;

                var rectangleCombine = ClosingRectangleCombineOffset(roadDescr, lane, position, tangent, startPart);

                combinedInstances.Add(rectangleCombine);
                combinedMaterials.Add(lane.material);
            }

            /********************************************************************************************************************************/


            var combinedMesh = new Mesh();
            combinedMesh.CombineMeshes(combinedInstances.ToArray(), false, true);

            var endObj = ObjectUtility.CreateIntersectionObject(road.shadowCastingMode, out var meshFilter, out var meshRenderer);
            meshFilter.mesh = combinedMesh;
            meshRenderer.materials = combinedMaterials.ToArray();

            var _splineContainer = endObj.AddComponent<SplineContainer>();
            _splineContainer.Spline = spline;

            var intersectionObject = endObj.AddComponent<IntersectionObject>();
            var elevated = WorldUtility.CheckElevation(roadDescr.settings, meshRenderer.bounds);
            intersectionObject.Initialize(roadDescr, meshFilter, meshRenderer, _splineContainer, elevated);
            intersectionObject.centerPosition = position;

            return intersectionObject;

            /********************************************************************************************************************************/

            CombineInstance Circle(float rotationY, int resolution, float _degrees)
            {
                var circleMesh = new Mesh();
                var radius = roadDescr.width * 0.5f;
                PGMeshCreation.Circle(circleMesh, radius, resolution, _degrees, 0.5f, rotationY);

                var circleCombine = new CombineInstance
                {
                    transform = Matrix4x4.identity,
                    mesh = circleMesh
                };

                var circleRotation = quaternion.LookRotationSafe(forwardNormalized, math.up());
                circleCombine.PGTranslateCombine(position);
                circleCombine.PGRotateCombine(position, circleRotation);

                return circleCombine;
            }

            CombineInstance Rectangle()
            {
                var rectangleMesh = new Mesh();
                PGMeshCreation.Rectangle(rectangleMesh, roadDescr.width, roadDescr.width, PGEnums.Axis.Z);

                var circleCombine = new CombineInstance
                {
                    transform = Matrix4x4.identity,
                    mesh = rectangleMesh
                };

                var circleRotation = quaternion.LookRotationSafe(forwardNormalized, math.up());
                circleCombine.PGTranslateCombine(position);
                circleCombine.PGRotateCombine(position, circleRotation);

                return circleCombine;
            }
        }

        private static Spline CreateEndSpline(RoadDescr roadDescr, float3 position, ref float3 forward)
        {
            var road = roadDescr.road;

            forward = new float3(forward.x, 0f, forward.z); // End parts (intersections in general) are always flat.
            var forwardNormalized = math.normalizesafe(forward);

            var knotStart = new BezierKnot
            {
                Position = position,
                TangentOut = -forwardNormalized,
                TangentIn = forwardNormalized,
                Rotation = quaternion.identity
            };
            var knotEnd = new BezierKnot
            {
                Position = position - forwardNormalized * roadDescr.width * 0.5f,
                TangentOut = -forwardNormalized,
                TangentIn = forwardNormalized,
                Rotation = quaternion.identity
            };

            var spline = new Spline(new List<BezierKnot> {knotStart, knotEnd});
            return spline;
        }

        /********************************************************************************************************************************/
        /********************************************************************************************************************************/

        public static List<CombineInstance> ClosingRectangle(RoadDescr roadDescr, List<Lane> lanes, List<Material> newMaterials, float3 center,
            float3 position, float3 tangent, bool startPart)
        {
            var closingCombineInstances = new List<CombineInstance>();

            tangent = PGTrigonometryUtility.DirectionalTangentToPointXZ(center, position, tangent);
            tangent.y = 0f;

            for (var i = 0; i < lanes.Count; i++)
            {
                var lane = lanes[i];
                if (lane.height <= 0f) continue;
                if (!lane.closedEnds) continue;

                var rectangleCombine = ClosingRectangleCombineOffset(roadDescr, lane, position, tangent, startPart);

                closingCombineInstances.Add(rectangleCombine);
                newMaterials.Add(lane.material);
            }

            return closingCombineInstances;
        }

        public static CombineInstance ClosingRectangleCombineOffset(RoadDescr roadDescr, Lane lane, float3 _position, float3 forward,
            bool startPart)
        {
            var baseHeightOffset = math.up() * roadDescr.settings.baseRoadHeight;

            var forwardNormalized = math.normalizesafe(forward);
            var perpendicularTangent = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(new float3(forwardNormalized.x, 0f, forwardNormalized.z));

            if (lane.centerIsLeftSide) perpendicularTangent *= -1f;
            if (startPart) perpendicularTangent *= -1f;
            _position += perpendicularTangent * lane.centerDistance;
            _position += baseHeightOffset;

            var rectangleRotation = quaternion.LookRotationSafe(forwardNormalized, math.up());

            var rectangleMesh = new Mesh();
            var uvX = float2.zero;
            if (lane.splineEdges.Length >= 3)
            {
                if (!startPart) uvX = new float2(lane.splineEdges[1].uvX, lane.splineEdges[2].uvX);
                else uvX = new float2(lane.splineEdges[2].uvX, lane.splineEdges[1].uvX);
            }

            var uvY = new float2(0f, lane.height / roadDescr.road.length);
            PGMeshCreation.Rectangle(rectangleMesh, lane.width, lane.height, PGEnums.Axis.Y, uvX, uvY);

            var rectangleCombine = new CombineInstance
            {
                transform = Matrix4x4.identity,
                mesh = rectangleMesh
            };

            rectangleCombine.PGRotateCombine(Vector3.zero, rectangleRotation);
            rectangleCombine.PGTranslateCombine(_position + new float3(0f, lane.height * 0.5f, 0f));

            return rectangleCombine;
        }

        public static CombineInstance ClosingRectangleCombine(RoadDescr roadDescr, Lane lane, float3 _position, float3 forward,
            bool startPart)
        {
            var baseHeightOffset = math.up() * roadDescr.settings.baseRoadHeight;
            _position += baseHeightOffset;

            var rectangleRotation = quaternion.LookRotationSafe(math.normalizesafe(forward), math.up());

            var rectangleMesh = new Mesh();
            var uvX = float2.zero;
            if (lane.splineEdges.Length >= 3)
            {
                if (!startPart) uvX = new float2(lane.splineEdges[1].uvX, lane.splineEdges[2].uvX);
                else uvX = new float2(lane.splineEdges[2].uvX, lane.splineEdges[1].uvX);
            }

            var uvY = new float2(0f, lane.height / roadDescr.road.length);
            PGMeshCreation.Rectangle(rectangleMesh, lane.width, lane.height * 2f, PGEnums.Axis.Y, uvX, uvY);

            var rectangleCombine = new CombineInstance
            {
                transform = Matrix4x4.identity,
                mesh = rectangleMesh
            };

            rectangleCombine.PGRotateCombine(Vector3.zero, rectangleRotation);
            rectangleCombine.PGTranslateCombine(_position);

            return rectangleCombine;
        }
    }
}