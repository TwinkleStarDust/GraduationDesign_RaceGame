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
    internal static class RoadCreationData
    {
        public static ConstructionData GenerateRoadData(RoadSettings roadSettings,
            RoadConstructor.SceneData sceneData, RoadDescr roadDescr,
            float3 initialPosition01, float3 initialPosition02,
            bool construct,
            out Overlap overlap01, out Overlap overlap02, out Spline roadSpline)
        {
            var settings = roadDescr.settings;

            var position01 = initialPosition01;
            var position02 = initialPosition02;

            overlap01 = OverlapUtility.GetOverlap(settings, roadDescr.width, settings.snapHeight * 2f, position01, sceneData);
            overlap02 = OverlapUtility.GetOverlap(settings, roadDescr.width, settings.snapHeight * 2f, position02, sceneData);

            if (overlap01.exists) position01 = overlap01.position;
            if (overlap02.exists) position02 = overlap02.position;

            var tangent01 = math.normalizesafe(position02 - position01);
            var tangent02 = -tangent01;

            /********************************************************************************************************************************/
            // User settings
            if (roadSettings.setTangent01)
            {
                tangent01 = roadSettings.tangent01;
                tangent01 = PGTrigonometryUtility.DirectionalTangentToPointXZ(position02, position01, tangent01);
            }

            if (roadSettings.setTangent02)
            {
                tangent02 = roadSettings.tangent02;
                tangent02 = PGTrigonometryUtility.DirectionalTangentToPointXZ(position01, position02, tangent02);
            }

            /********************************************************************************************************************************/
            // Overlap RoadConnectionData
            overlap01.roadConnectionDatas = CreateConnectionDatas(overlap01);
            overlap02.roadConnectionDatas = CreateConnectionDatas(overlap02);
            UpdateConnectionAngles(overlap01, tangent01);
            UpdateConnectionAngles(overlap02, tangent02);

            /********************************************************************************************************************************/
            // Make tangents fit to existing splines and correct length
            Spline_Angle(roadSettings, roadDescr, overlap01, overlap02, ref position01, ref tangent01, ref position02, ref tangent02, construct);
            Spline_Angle(roadSettings, roadDescr, overlap02, overlap01, ref position02, ref tangent02, ref position01, ref tangent01, construct);

            /********************************************************************************************************************************/
            // Make tangents fit to existing intersections and correct length
            Intersection_Angle(roadSettings, roadDescr, overlap01, overlap02, ref position01, ref tangent01, ref position02, ref tangent02,
                construct);
            Intersection_Angle(roadSettings, roadDescr, overlap02, overlap01, ref position02, ref tangent02, ref position01, ref tangent01,
                construct);

            /********************************************************************************************************************************/
            // Make tangents fit to roundabout.
            Roundabout_Angle(roadDescr, overlap01, overlap02, ref position01, ref tangent01, ref position02, ref tangent02);
            Roundabout_Angle(roadDescr, overlap02, overlap01, ref position02, ref tangent02, ref position01, ref tangent01);

            /********************************************************************************************************************************/
            // Set final angles
            UpdateConnectionAngles(overlap01, tangent01);
            UpdateConnectionAngles(overlap02, tangent02);

            /********************************************************************************************************************************/
            // Road Data

            var center = (position01 + position02) * 0.5f;
            TangentCalculation.CalculateTangents(settings.smoothSlope, settings.tangentLength, position01, tangent01, position02, tangent02,
                true, center,
                out tangent01, out tangent02);

            roadSpline = CreateRoadSpline(position01, tangent01, position02, tangent02);
            var angle01 = GetMinAngle(overlap01);
            var angle02 = GetMinAngle(overlap02);
            var roadAngle = math.abs(math.degrees(PGTrigonometryUtility.AngleXZ(tangent01, -tangent02)));
            var curvature = RoadSplineUtility.GetCurvature(position01, tangent01, position02, -tangent02);
            var slope = math.degrees(PGTrigonometryUtility.Slope(position01, position02));
            var height01 = GetHeight(settings, roadDescr, position01);
            var height02 = GetHeight(settings, roadDescr, position02);
            var elevated = WorldUtility.CheckElevation(settings, roadSpline, roadDescr.road.length);

            /********************************************************************************************************************************/

            var knotFirst = roadSpline.Knots.First();
            if (math.distancesq(knotFirst.Position, position01) > math.distancesq(knotFirst.Position, position02))
                RoadSplineUtility.InvertSpline(roadSpline);

            /********************************************************************************************************************************/


            var roadData = new ConstructionData(position01, tangent01, angle01, height01,
                position02, tangent02, angle02, height02,
                roadSpline.GetLength(), roadAngle, curvature, slope, elevated);

            return roadData;
        }

        private static List<RoadConnectionData> CreateConnectionDatas(Overlap overlap)
        {
            var connectionDatas = new List<RoadConnectionData>();
            if (!overlap.exists) return connectionDatas;

            var snappedRoad = overlap.IsSnappedRoad();

            if (overlap.roadType == RoadType.Intersection || snappedRoad)
            {
                var roadConnections = snappedRoad ? overlap.roadObject.RoadConnections : overlap.intersectionObject.RoadConnections;
                for (var i = 0; i < roadConnections.Count; i++)
                {
                    var roadConnection = roadConnections[i];
                    var roadSpline = roadConnection.splineContainer.Spline;
                    var nearestKnotIndex = RoadSplineUtility.GetNearestKnotIndex(roadSpline, overlap.position);
                    var nearestKnot = roadSpline.Knots.ElementAt(nearestKnotIndex);
                    var otherKnotIndex = nearestKnotIndex == 0 ? roadSpline.Count - 1 : 0;
                    var otherKnot = roadSpline.Knots.ElementAt(otherKnotIndex);
                    var connectionData = new RoadConnectionData(nearestKnotIndex, nearestKnot, otherKnotIndex, otherKnot);
                    connectionDatas.Add(connectionData);
                }
            }
            else if (overlap.roadType == RoadType.Road)
            {
                var connectionData = new RoadConnectionData(0, new BezierKnot(), 0, new BezierKnot());
                connectionDatas.Add(connectionData);
            }

            return connectionDatas;
        }

        /********************************************************************************************************************************/
        private static void Spline_Angle(RoadSettings roadSettings, RoadDescr roadDescr, Overlap overlap01, Overlap overlap02,
            ref float3 position01, ref float3 tangent01, ref float3 position02, ref float3 tangent02, bool construct)
        {
            if (!overlap01.exists) return;
            if (overlap01.roadType != RoadType.Road) return;
            if (overlap01.IsSnappedRoad()) return;

            var posCenter = (position01 + position02) / 2;
            var tangentPerp = PGTrigonometryUtility.PerpendicularTangentToPointXZ(posCenter, position01, overlap01.tangent);

            var angleLeft = math.abs(PGTrigonometryUtility.AngleXZ(overlap01.tangent, tangent01));
            var angleRight = math.abs(PGTrigonometryUtility.AngleXZ(-overlap01.tangent, tangent01));
            var closestAngle = math.degrees(math.min(angleLeft, angleRight));

            var distance = AngleDistanceUtility.GetAngleDistance(overlap01.RoadDescr, closestAngle);

            CalculateValues(roadSettings, roadDescr, overlap01.tangent, tangentPerp, distance, overlap01, overlap02,
                ref position01, ref tangent01, ref position02, ref tangent02, construct);
        }

        private static void Intersection_Angle(RoadSettings roadSettings, RoadDescr roadDescr, Overlap overlap01, Overlap overlap02,
            ref float3 position01, ref float3 tangent01, ref float3 position02, ref float3 tangent02, bool construct)
        {
            if (!overlap01.exists) return;
            var snappedRoad = overlap01.IsSnappedRoad();
            if (overlap01.roadType != RoadType.Intersection && !snappedRoad) return;
            if (overlap01.roadType == RoadType.Intersection && overlap01.intersectionObject.GetType() != typeof(IntersectionObject)) return;

            var roadConnections = snappedRoad ? overlap01.roadObject.RoadConnections : overlap01.intersectionObject.RoadConnections;
            var centerPosition = snappedRoad ? overlap01.roadObject.snapPosition : overlap01.intersectionObject.centerPosition;

            var overlapNearestKnots = new List<BezierKnot>();
            for (var i = 0; i < overlap01.roadConnectionDatas.Count; i++) overlapNearestKnots.Add(overlap01.roadConnectionDatas[i].nearestKnot);

            var tangentIn = PGTrigonometryUtility.DirectionalTangentToPointXZ(overlap01.position, position02, tangent01);
            
            var nearestOverlapKnotIndex = AngleDistanceUtility.GetIndexWithLeastDegrees(overlap01.position, tangentIn, overlapNearestKnots);
            var connectionCenter = roadConnections[nearestOverlapKnotIndex].meshRenderer.bounds.center;
            var nearestOverlapKnot = overlap01.roadConnectionDatas[nearestOverlapKnotIndex].nearestKnot;
            var overlapTangentIn = PGTrigonometryUtility.DirectionalTangentToPointXZ(connectionCenter, nearestOverlapKnot.Position, nearestOverlapKnot.TangentOut);
            
            var closestAngle = math.abs(math.degrees(PGTrigonometryUtility.AngleXZ(-tangentIn, overlapTangentIn)));
            var distance = AngleDistanceUtility.GetAngleDistance(overlap01.RoadDescr, closestAngle);
            
            if (overlap01.IsEndObject())
            {
                var tangentPerp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(overlap01.tangent);
                
                CalculateValues(roadSettings, roadDescr, overlap01.tangent, tangentPerp, distance, overlap01, overlap02,
                    ref position01, ref tangent01, ref position02, ref tangent02);
            }
            else if (roadConnections.Count == 2)
            {
                var overlapTan = overlap01.SplineContainer.Spline.EvaluateTangent(0.5f);
                overlapTan = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(overlapTan);
                var posCenter = (position01 + position02) / 2;
                var tangentPerp =
                    PGTrigonometryUtility.DirectionalTangentToPointXZ(posCenter, centerPosition, overlapTan);

                var nearestKnot = overlap01.roadConnectionDatas[0].nearestKnot;
                var otherKnot = overlap01.roadConnectionDatas[0].otherKnot;
                var fixPoint = (otherKnot.Position + nearestKnot.Position) / 2f;
                var tangentFixPoint = PGTrigonometryUtility.DirectionalTangentToPointXZ(fixPoint, nearestKnot.Position, nearestKnot.TangentOut);
                tangentFixPoint = new float3(-tangentFixPoint.x, tangentFixPoint.y, -tangentFixPoint.z);


                CalculateValues(roadSettings, roadDescr, tangentFixPoint, tangentPerp, distance, overlap01, overlap02,
                    ref position01, ref tangent01, ref position02, ref tangent02, construct);
            }
            else if (roadConnections.Count > 2)
            {
                AngleDistanceUtility.GetFacingIndexes(overlap01.position, overlapNearestKnots, out var firstIndex, out var secondIndex);

                var firstKnot = overlap01.roadConnectionDatas[firstIndex].nearestKnot;
                var secondKnot = overlap01.roadConnectionDatas[secondIndex].nearestKnot;
                var overlapTangent = firstKnot.Position - secondKnot.Position;
                var tangentPerp = PGTrigonometryUtility.PerpendicularTangentToPointXZ(position02, overlap01.position, overlapTangent);

                CalculateValues(roadSettings, roadDescr, overlapTangent, tangentPerp, distance, overlap01, overlap02,
                    ref position01, ref tangent01, ref position02, ref tangent02);
            }
        }

        private static void Roundabout_Angle(RoadDescr roadDescr, Overlap overlap01, Overlap overlap02,
            ref float3 position01, ref float3 tangent01, ref float3 position02, ref float3 tangent02)
        {
            if (!overlap01.exists || overlap01.roadType != RoadType.Intersection) return;
            if (overlap01.intersectionObject.GetType() != typeof(RoundaboutObject)) return;

            var overlapTangentPerp = math.normalizesafe(PGTrigonometryUtility.RotateTangent90ClockwiseXZ(overlap01.tangent));
            tangent01 =
                PGTrigonometryUtility.DirectionalTangentToPointXZ(overlap01.intersectionObject.centerPosition, position01, overlapTangentPerp) * -1f;

            var distance = overlap01.intersectionObject.roadDescr.width * 0.5f + roadDescr.settings.intersectionDistance;
            position01 += tangent01 * distance;
        }

        /********************************************************************************************************************************/
        /********************************************************************************************************************************/
        private static void CalculateValues(RoadSettings roadSettings, RoadDescr roadDescr, float3 overlapTangent, float3 overlapTangentPerp,
            float distance, Overlap overlap01, Overlap overlap02,
            ref float3 position01, ref float3 tangent01, ref float3 position02, ref float3 tangent02, bool construct = false)
        {
            
            /********************************************************************************************************************************/
            // Snap Angle (Int.)

            var defaultPosition01 = position01;
            var snapAngleInt = roadDescr.settings.snapAngleIntersection;
            if (snapAngleInt > 0f) tangent01 = PGTrigonometryUtility.SnapTangentXZ(tangent01, overlapTangentPerp, math.radians(snapAngleInt));

            /********************************************************************************************************************************/
            // Connection

            var angleAligned = false;
            if (roadDescr.settings.connections == Connections.Align && overlap01.exists && overlap02.exists)
                if (math.distancesq(overlap01.position, overlap02.position) > 0.01f)
                {
                    if (overlap01.roadConnectionDatas.Count == 1)
                    {
                        var overlapTangentPerpDir = PGTrigonometryUtility.DirectionalTangentToPointXZ(position02, position01, overlapTangentPerp);
                        var angleTan = math.degrees(math.abs(PGTrigonometryUtility.AngleXZ(tangent01, overlapTangent)));
                        var anglePerp = math.degrees(math.abs(PGTrigonometryUtility.AngleXZ(tangent01, overlapTangentPerpDir)));
                        if (overlap01.IsEndObject() && angleTan < anglePerp) tangent01 = overlapTangent;
                        else tangent01 = overlapTangentPerpDir;
                        tangent01 = PGTrigonometryUtility.DirectionalTangentToPointXZ(position02, position01, tangent01);
                    }

                    angleAligned = true;
                }

            /********************************************************************************************************************************/

            UpdateConnectionAngles(overlap01, tangent01);

            /********************************************************************************************************************************/
            // Min Angle (Int.)
            if (!angleAligned)
            {
                var minAngleInt = roadDescr.settings.minAngleIntersection;
                AdjacentConnectionAngles(overlap01.roadConnectionDatas, out var angleLeft, out var angleRight);

                if (overlap01.roadConnectionDatas.Count == 1 && overlap01.roadType == RoadType.Intersection)
                {
                    var connection = overlap01.roadConnectionDatas[0];
                    var overlapTan = connection.otherKnot.Position - connection.nearestKnot.Position;

                    var angle = math.degrees(PGTrigonometryUtility.AngleXZ(tangent01, overlapTan));
                    var angleAbs = math.abs(angle);

                    if (angleAbs < minAngleInt)
                    {
                        if (angle > 0)
                            tangent01 = PGTrigonometryUtility.RotateTangentXZ(tangent01, -math.radians(minAngleInt - angle));
                        else
                            tangent01 = PGTrigonometryUtility.RotateTangentXZ(tangent01, math.radians(minAngleInt - angleAbs));
                    }
                }
                else
                {
                    const float AngleToleranz = 0.001f;
                    if (angleRight < minAngleInt && math.abs(angleLeft) >= minAngleInt + (minAngleInt - angleRight) - AngleToleranz)
                        tangent01 = PGTrigonometryUtility.RotateTangentXZ(tangent01, -math.radians(minAngleInt - angleRight));
                    else if (math.abs(angleLeft) < minAngleInt &&
                             math.abs(angleRight) >= minAngleInt + (minAngleInt - math.abs(angleLeft)) - AngleToleranz)
                        tangent01 = PGTrigonometryUtility.RotateTangentXZ(tangent01, math.radians(minAngleInt - math.abs(angleLeft)));
                }
            }

            /********************************************************************************************************************************/
            // Final position01
            position01 += math.normalizesafe(tangent01) * distance;
            position01.y = defaultPosition01.y;

            if (overlap02.exists) return;

            /********************************************************************************************************************************/
            // Distance Angle Curve
            if (!roadSettings.setTangent02)
            {
                var distanceAngleCurve = roadDescr.settings.distanceRatioAngleCurve;

                var ratio = GetStraightToPerpRatio(position01, tangent01, position02, tangent02);
                if (ratio > 0)
                {
                    var angleDeg = distanceAngleCurve.Evaluate(ratio);
                    tangent02 = tangent01;

                    var tangent02Temp = position02 - position01;
                    var angleTemp = PGTrigonometryUtility.AngleXZ(tangent01, tangent02Temp);
                    var rotationRad = angleTemp > 0f ? math.radians(angleDeg) : -math.radians(angleDeg);
                    tangent02 = PGTrigonometryUtility.RotateTangentXZ(tangent02, rotationRad);
                }
            }

            /********************************************************************************************************************************/
            // Snap Angle (Road)
            var snapAngleRoad = roadDescr.settings.snapAngleRoad;
            if (snapAngleRoad > 0f)
                tangent02 = PGTrigonometryUtility.SnapTangentXZ(tangent02, tangent01,
                    math.radians(snapAngleRoad));
        }

        private static void UpdateConnectionAngles(Overlap overlap, float3 tangent)
        {
            if (!overlap.exists) return;

            var connectionDatas = overlap.roadConnectionDatas;

            if (overlap.roadType == RoadType.Intersection || overlap.IsSnappedRoad())
            {
                for (var i = 0; i < connectionDatas.Count; i++)
                {
                    var nearestKnot = connectionDatas[i].nearestKnot;
                    var tangentOut =
                        -PGTrigonometryUtility.DirectionalTangentToPointXZ(overlap.position, nearestKnot.Position, nearestKnot.TangentOut);
                    var angle = PGTrigonometryUtility.AngleXZ(tangent, tangentOut);
                    angle = math.degrees(angle);
                    connectionDatas[i].angle = angle;
                }
            }
            else if (overlap.roadType == RoadType.Road)
            {
                var angle = PGTrigonometryUtility.AngleXZ(tangent, overlap.tangent);
                angle = math.degrees(angle);
                connectionDatas[0].angle = angle;
            }
        }

        /********************************************************************************************************************************/
        private static void AdjacentConnectionAngles(List<RoadConnectionData> roadConnectionDatas,
            out float angleLeft, out float angleRight)
        {
            angleLeft = float.MinValue;
            angleRight = float.MaxValue;

            for (var i = 0; i < roadConnectionDatas.Count; i++)
            {
                var angle = roadConnectionDatas[i].angle;
                if (angle <= 0 && angle > angleLeft) angleLeft = angle;
                else if (angle > 0 && angle < angleRight) angleRight = angle;
            }

            if (angleRight > 360f) angleRight = 180f + angleLeft;
            else if (angleLeft < -360f) angleLeft = -180f + angleRight;
        }

        /********************************************************************************************************************************/

        private static Spline CreateRoadSpline(float3 position01, float3 tangent01, float3 position02, float3 tangent02)
        {
            var knot01 = new BezierKnot
                {Position = position01, Rotation = quaternion.identity, TangentIn = -tangent01, TangentOut = tangent01};
            var knot02 = new BezierKnot
                {Position = position02, Rotation = quaternion.identity, TangentIn = tangent02, TangentOut = -tangent02};

            var spline = new Spline
            {
                {knot01, TangentMode.Broken},
                {knot02, TangentMode.Broken}
            };
            return spline;
        }

        private static float GetMinAngle(Overlap overlap)
        {
            var angle = -1f;
            if (!overlap.exists) return angle;
            if (overlap.roadConnectionDatas.Count == 0) return angle;
            angle = math.abs(overlap.roadConnectionDatas.Min(roadConnectionData => Math.Abs(roadConnectionData.angle)));
            if (angle > 90f) angle = 180f - angle;
            return angle;
        }

        private static float GetHeight(ComponentSettings settings, RoadDescr roadDescr, float3 position)
        {
            var raycastOffset = (float3) Constants.RaycastOffset(settings);
            var ray = new Ray(position + raycastOffset, Vector3.down);
            var hit = Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, settings.groundLayers);
            if (!hit) return 0f;
            return position.y - hitInfo.point.y;
        }

        private static float GetStraightToPerpRatio(float3 position01, float3 tangent01, float3 position02, float3 tangent02)
        {
            var tangentPerp = math.normalizesafe(PGTrigonometryUtility.PerpendicularTangentToPointXZ(position02, position01, tangent01));
            var intersectionPointPerp = PGTrigonometryUtility.IntersectionPointXZ(position01, tangentPerp, position02, -tangent01);
            var distancePerp = math.distance(position01, intersectionPointPerp);
            var intersectionPointStraight = PGTrigonometryUtility.IntersectionPointXZ(position01, tangent01, position02, tangentPerp);
            var distanceStraight = math.distance(position01, intersectionPointStraight);

            if (distanceStraight > 0.001f && distancePerp > 0.001f)
            {
                var ratio = distanceStraight / distancePerp;
                return ratio;
            }

            return -1f;
        }
    }
}