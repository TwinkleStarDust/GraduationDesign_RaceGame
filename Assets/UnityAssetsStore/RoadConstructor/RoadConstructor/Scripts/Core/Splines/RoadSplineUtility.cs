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
    public static class RoadSplineUtility
    {
        /********************************************************************************************************************************/
        // Offset X
        /********************************************************************************************************************************/
        public static void OffsetSplineX(Spline spline, float offsetX, float widthStart = 1f, float widthEnd = 1f)
        {
            if (spline.Count != 2)
            {
                OffsetSplineXSimple(spline, offsetX, widthStart, widthEnd);
                return;
            }

            var insertKnot = true;
            var knot01 = spline.Knots.First();
            var knot02 = spline.Knots.Last();
            var tangent01 = knot01.TangentOut;
            tangent01.y = 0f;
            var tangent02 = -knot02.TangentIn;
            tangent02.y = 0f;
            var angle = math.abs(math.degrees(PGTrigonometryUtility.AngleXZ(tangent01, tangent02)));
            var tangent01Perp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(tangent01);
            
            /********************************************************************************************************************************/
            // Use simple offset for straight line
            if (angle < 5f)
            {
                var directionalDistance = math.abs(PGTrigonometryUtility.DirectionalDistanceXZ(knot01.Position, tangent01Perp, knot02.Position));
                if (directionalDistance < 1f)
                {
                    OffsetSplineXSimple(spline, offsetX, widthStart, widthEnd);
                    return;
                }
            }

            /********************************************************************************************************************************/
            // Don't insert knot when offset goes inside curve
            if (angle > 10f)
            {
                var tangentSpline = knot02.Position - knot01.Position;
                var sameDirection = PGTrigonometryUtility.IsSameDirectionXZ(tangentSpline, tangent01Perp);
                if (offsetX < 0f) sameDirection = !sameDirection;
                insertKnot = !sameDirection;
            }

            /********************************************************************************************************************************/
            if (insertKnot)
            {
                InsertKnotAndOffset(spline, offsetX, widthStart, widthEnd);
            }
            else
            {
                // Handling widths differences manually
                if (!Mathf.Approximately(1f, widthStart))
                {
                    var totalOffset = offsetX * widthStart;
                    var initialOffset = totalOffset - offsetX;
                    OffsetKnotX(spline, 0, initialOffset);
                }

                if (!Mathf.Approximately(1f, widthEnd))
                {
                    var totalOffset = offsetX * widthEnd;
                    var initialOffset = totalOffset - offsetX;
                    OffsetKnotX(spline, spline.Count - 1, initialOffset);
                }

                var middlePos = spline.EvaluatePosition(0.5f);

                OffsetBezierCurveX(spline, offsetX);

                // Tiller Hanson may give wrong results for some tangents
                var newMiddlePos = spline.EvaluatePosition(0.5f);
                var offsetAbs = math.abs(offsetX);
                if (math.distance(middlePos, newMiddlePos) - offsetAbs > offsetAbs * 0.5f)
                {
                    spline.Clear();
                    spline.Add(knot01);
                    spline.Add(knot02);
                    InsertKnotAndOffset(spline, offsetX);
                }
            }
        }

        private static void InsertKnotAndOffset(Spline spline, float offsetX, float widthStart = 1f, float widthEnd = 1f)
        {
            // Handling widths differences manually
            if (!Mathf.Approximately(1f, widthStart))
            {
                var totalOffset = offsetX * widthStart;
                var initialOffset = totalOffset - offsetX;
                OffsetKnotX(spline, 0, initialOffset);
            }

            if (!Mathf.Approximately(1f, widthEnd))
            {
                var totalOffset = offsetX * widthEnd;
                var initialOffset = totalOffset - offsetX;
                OffsetKnotX(spline, spline.Count - 1, initialOffset);
            }
            
            InsertKnotSeamless(spline, 0.5f);

            var seperatedSplines = SeperateSpline(spline);
            
            for (var i = 0; i < seperatedSplines.Count; i++)
            {
                var splinePart = seperatedSplines[i];
                OffsetBezierCurveX(splinePart, offsetX);
            }

            var newKnot01 = seperatedSplines.First().Knots.First();
            var newKnot02 = seperatedSplines.First().Knots.Last();
            newKnot02.TangentOut = seperatedSplines.Last().Knots.First().TangentOut;
            var newKnot03 = seperatedSplines.Last().Knots.Last();

            spline.Clear();
            spline.Add(newKnot01, TangentMode.Broken);
            spline.Add(newKnot02, TangentMode.Broken);
            spline.Add(newKnot03, TangentMode.Broken);
        }

        /// <summary>
        ///     Simple heuristic approximation method by Tiller and Hanson.
        ///     Graph: https://feirell.github.io/offset-bezier/
        /// </summary>
        private static void OffsetBezierCurveX(Spline spline, float offsetX)
        {
            var knot01 = spline.Knots.First();
            var knot02 = spline.Knots.Last();

            var tangent01 = knot01.TangentOut;
            var tangent02 = knot02.TangentIn;
            var tangent01Flat = math.normalizesafe(new float3(tangent01.x, 0f, tangent01.z));
            var tangent02Flat = math.normalizesafe(new float3(tangent02.x, 0f, tangent02.z));
            var tangent01Perp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(tangent01Flat);
            var tangent02Perp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(-tangent02Flat);

            var p0 = knot01.Position;
            var p1 = p0 + knot01.TangentOut;
            var p2 = knot02.Position + knot02.TangentIn;
            var p3 = knot02.Position;
            var tangentConnection = p2 - p1;

            var position01 = p0 + tangent01Perp * offsetX;
            var position02 = p3 + tangent02Perp * offsetX;

            var tangentConnectionPerp = math.normalizesafe(PGTrigonometryUtility.RotateTangent90ClockwiseXZ(tangentConnection));

            // Tangents may be inverted with straight lines
            if (!PGTrigonometryUtility.IsSameDirectionXZ((tangent01Perp + tangent02Perp) * 0.5f, tangentConnectionPerp))
            {
                tangentConnection *= -1f;
                tangentConnectionPerp *= -1f;
            }

            var p1off = p1 + tangentConnectionPerp * offsetX;
            var p2off = p2 + tangentConnectionPerp * offsetX;

            var intersectionA = p1off;
            var intersectionB = p2off;

            if (CheckIntersectionAngle(knot01.TangentOut, tangentConnection, 1f))
                intersectionA = PGTrigonometryUtility.IntersectionPointXZ(position01, knot01.TangentOut, p1off, tangentConnection);
            if (CheckIntersectionAngle(knot02.TangentIn, tangentConnection, 1f))
                intersectionB = PGTrigonometryUtility.IntersectionPointXZ(position02, knot02.TangentIn, p2off, -tangentConnection);

            bool CheckIntersectionAngle(float3 _tangent01, float3 _tangent02, float tolerance)
            {
                var _angle = math.abs(math.degrees(PGTrigonometryUtility.AngleXZ(_tangent01, _tangent02)));
                var zeroAngleDifference = math.abs(_angle - 0f);
                var straightAngleDifference = math.abs(_angle - 180f);
                return zeroAngleDifference > tolerance && straightAngleDifference > tolerance;
            }

            var newTangent01 = intersectionA - position01;
            var newTangent02 = intersectionB - position02;

            knot01.Position = position01;
            knot02.Position = position02;
            knot01.TangentIn = newTangent01;
            knot01.TangentOut = newTangent01;
            knot02.TangentIn = newTangent02;
            knot02.TangentOut = newTangent02;

            spline.Clear();
            spline.Add(knot01, TangentMode.Broken);
            spline.Add(knot02, TangentMode.Broken);
        }

        private static void OffsetSplineXSimple(Spline spline, float offsetX, float widthStart = 1f, float widthEnd = 1f)
        {
            var knots = spline.Knots.ToList();
            if (knots.Count < 1) return;

            var originalLength = spline.GetLength();

            // Side offset
            for (var i = 0; i < knots.Count; i++)
            {
                var t = (float) i / (knots.Count - 1);
                var width = (widthEnd - widthStart) * t + widthStart;

                var knot = knots[i];
                var position = knot.Position;
                var tangent = knot.TangentOut;
                tangent.y = 0f;
                var tangentPerp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(math.normalizesafe(tangent));
                var newPosition = position + tangentPerp * offsetX * width;
                knot.Position = newPosition;
                spline.SetKnot(i, knot);
            }

            knots = spline.Knots.ToList();
            var relativeLength = math.pow(spline.GetLength() / originalLength, 1.25f); // Approximation.

            // Tangent Length
            for (var i = 0; i < knots.Count; i++)
            {
                var length = relativeLength;
                var knot = knots[i];
                var tangentIn = knot.TangentIn * length;
                var tangentOut = knot.TangentOut * length;
                knot.TangentIn = tangentIn;
                knot.TangentOut = tangentOut;
                spline.SetKnot(i, knot);
            }
        }

        private static void OffsetKnotX(Spline spline, int knotIndex, float offsetX)
        {
            var knot = spline.Knots.ElementAt(knotIndex);
            var tangentPerp = PGTrigonometryUtility.RotateTangent90ClockwiseXZ(math.normalizesafe(knot.TangentOut));
            knot.Position += tangentPerp * offsetX;
            spline.SetKnot(knotIndex, knot);
        }
        
        /********************************************************************************************************************************/
        /********************************************************************************************************************************/
        
        /// <summary>
        ///     Inserts a knot into a Bezier Curve without affecting the curvature (De Casteljau's algorithm).
        ///     The t-value differs from UnitySpline's t-value and has to be iterated towards it.
        /// </summary>
        public static void InsertKnotSeamless(Spline spline, float t, int iterations = 3)
        {
            var curveIndex = spline.SplineToCurveT(t, out var curveT);
            var curve = spline.GetCurve(curveIndex);
            
            var p0 = curve.P0;
            var p1 = curve.P1;
            var p2 = curve.P2;
            var p3 = curve.P3;

            float3 m0, m1, m2, q0, q1;

            float3 CalculatePoint(float _t)
            {
                m0 = math.lerp(p0, p1, _t);
                m1 = math.lerp(p1, p2, _t);
                m2 = math.lerp(p2, p3, _t);
                q0 = math.lerp(m0, m1, _t);
                q1 = math.lerp(m1, m2, _t);
                return math.lerp(q0, q1, _t);    
            }
            
            // Iterate towards Unity's t value
            var currentT = curveT;
            var point = CalculatePoint(currentT);
            var lastDifference = 0f;
            for (int i = 0; i < iterations; i++)
            {
                point = CalculatePoint(currentT);
                
                CurveUtility.GetNearestPoint(curve, new Ray(point, Vector3.forward), out var nearest, out var nearestT);
                
                // Approximation
                var difference = nearestT - curveT;

                if (i > 0 && ((lastDifference > 0f && difference > 0f) || (lastDifference < 0f && difference < 0f)))
                {
                    var deltaDif = difference / lastDifference;
                    difference *= 1 + deltaDif;
                }
                
                currentT -= difference;
                currentT = math.clamp(currentT, 0f, 1f);
                lastDifference = difference;
            }

            
            var knot01 = spline.Knots.ElementAt(curveIndex);
            var knot02 = spline.Knots.ElementAt(curveIndex + 1);
            
            knot01.TangentOut = m0 - p0;
            knot02.TangentIn = m2 - p3;
            
            var knotInsert = new BezierKnot
            {
                Position = point,
                TangentIn = q0 - point,
                TangentOut = q1 - point,
                Rotation = quaternion.identity
            };
            
            spline.SetKnot(curveIndex, knot01);
            spline.SetKnot(curveIndex + 1, knot02);
            spline.Insert(curveIndex + 1, knotInsert, TangentMode.Broken);
        }

        /********************************************************************************************************************************/
        /********************************************************************************************************************************/
        
        public static void ReduceSpline(Spline spline, bool start, float splineLength, float reducedLength)
        {
            var t = reducedLength / splineLength;
            if (!start) t = 1f - t;

            if (t >= 1f || t <= 0f) return;

            InsertKnotSeamless(spline, t);

            spline.RemoveAt(start ? 0 : spline.Count - 1);
        }

        public static Spline CreateIncreasedSpline(Spline spline, bool start, float increasedLength)
        {
            var knots = spline.Knots.ToList();
            if (start)
            {
                var knot = knots.First();
                var tangent = knot.TangentIn;
                knot.Position += tangent * increasedLength;
                knots[0] = knot;
            }
            else
            {
                var knot = knots.Last();
                var tangent = knot.TangentOut;
                knot.Position += tangent * increasedLength;
                knots[^1] = knot;
            }

            return new Spline(knots);
        }

        public static int GetNearestKnotIndex(Spline spline, float3 position)
        {
            var nearestKnotIndex = 0;
            var nearestDistance = float.MaxValue;
            var index = 0;
            foreach (var knot in spline.Knots)
            {
                var distance = math.distancesq(position, knot.Position);
                if (distance < nearestDistance)
                {
                    nearestKnotIndex = index;
                    nearestDistance = distance;
                }

                index++;
            }

            return nearestKnotIndex;
        }

        public static Spline FlattenSpline(Spline spline)
        {
            var flatKnots = new List<BezierKnot>(spline.Knots);
            for (var index = 0; index < flatKnots.Count; index++)
            {
                var flatKnot = flatKnots[index];
                var flatKnotPosition = new float3(flatKnot.Position.x, 0f, flatKnot.Position.z);
                var flatKnotTangentIn = new float3(flatKnot.TangentIn.x, 0f, flatKnot.TangentIn.z);
                var flatKnotTangentOut = new float3(flatKnot.TangentOut.x, 0f, flatKnot.TangentOut.z);
                var flatKnotRotation = flatKnot.Rotation;
                flatKnots[index] = new BezierKnot(flatKnotPosition, flatKnotTangentIn, flatKnotTangentOut, flatKnotRotation);
            }

            return new Spline(flatKnots);
        }


        public static List<Spline> CreateIntersectionSplines(float3 center, List<float3> positions)
        {
            var newSplines = new List<Spline>();

            for (var i = 0; i < positions.Count; i++)
            for (var j = i + 1; j < positions.Count; j++)
            {
                var pos1 = positions[i];
                var pos2 = positions[j];
                var tan1out = new float3(center - pos1) * Constants.TangentLengthIntersection;
                var tan1in = -tan1out;
                var tan2out = new float3(pos2 - center) * Constants.TangentLengthIntersection;
                var tan2in = -tan2out;

                var knot1 = new BezierKnot
                {
                    Position = pos1,
                    TangentOut = tan1out,
                    TangentIn = tan1in,
                    Rotation = quaternion.identity
                };
                var knot2 = new BezierKnot
                {
                    Position = pos2,
                    TangentOut = tan2out,
                    TangentIn = tan2in,
                    Rotation = quaternion.identity
                };

                var newSpline = new Spline
                {
                    {knot1, TangentMode.Broken},
                    {knot2, TangentMode.Broken}
                };

                newSplines.Add(newSpline);
            }

            return newSplines;
        }

        // SplineUtility.ReverseFlow won't properly update the tangents.
        public static void InvertSpline(Spline spline)
        {
            var knotsCount = spline.Knots.Count();
            var newKnots = new BezierKnot[knotsCount];
            for (var i = 0; i < knotsCount; i++)
            {
                var oldKnot = spline.Knots.ElementAt(knotsCount - 1 - i);
                newKnots[i] = new BezierKnot
                {
                    Position = oldKnot.Position,
                    TangentIn = oldKnot.TangentOut,
                    TangentOut = oldKnot.TangentIn,
                    Rotation = oldKnot.Rotation
                };
            }

            for (var i = 0; i < knotsCount; i++) spline.SetKnot(i, newKnots[i]);
        }

        public static Spline CreateConnectionSpline(ComponentSettings settings, Spline spline01, Spline spline02)
        {
            var knots01 = spline01.Knots.ToList();
            var knots02 = spline02.Knots.ToList();

            var distances = new float[4];
            distances[0] = math.distancesq(knots01.First().Position, knots02.First().Position);
            distances[1] = math.distancesq(knots01.Last().Position, knots02.Last().Position);
            distances[2] = math.distancesq(knots01.First().Position, knots02.Last().Position);
            distances[3] = math.distancesq(knots01.Last().Position, knots02.First().Position);

            var minDistanceIndex = Array.IndexOf(distances, distances.Min());

            var newKnots = new List<BezierKnot>();

            if (minDistanceIndex == 0)
            {
                newKnots.Add(knots01.First());
                newKnots.Add(knots02.First());
            }
            else if (minDistanceIndex == 1)
            {
                newKnots.Add(knots01.Last());
                newKnots.Add(knots02.Last());
            }
            else if (minDistanceIndex == 2)
            {
                newKnots.Add(knots01.First());
                newKnots.Add(knots02.Last());
            }
            else if (minDistanceIndex == 3)
            {
                newKnots.Add(knots01.Last());
                newKnots.Add(knots02.First());
            }

            var newSpline = new Spline();
            foreach (var knot in newKnots) newSpline.Add(knot, TangentMode.Broken);

            TangentCalculation.CalculateTangents(newSpline, settings.smoothSlope, settings.tangentLength);

            return newSpline;
        }

        public static List<Spline> SeperateSpline(Spline spline)
        {
            var seperatedSplines = new List<Spline>();
            var knots = spline.Knots.ToList();

            for (var i = 0; i < knots.Count - 1; i++)
            {
                var newSpline = new Spline(new List<BezierKnot> {knots[i], knots[i + 1]});
                seperatedSplines.Add(newSpline);
            }

            return seperatedSplines;
        }

        public static float GetCurvature(BezierKnot knot01, BezierKnot knot02)
        {
            return GetCurvature(knot01.Position, knot01.TangentOut, knot02.Position, knot02.TangentOut);
        }

        public static float GetCurvature(Vector3 position01, Vector3 tangent01, Vector3 position02, Vector3 tangent02)
        {
            var vector01 = position01 + tangent01.normalized * (position02 - position01).magnitude;
            var vector02 = position02 + tangent02.normalized * (position02 - position01).magnitude;
            var diff = vector02 - vector01;
            var curvature = Vector3.Angle(vector01 - position01, diff);
            return curvature;
        }

        public static int CalculateResolution(ComponentSettings settings, int resolution, BezierKnot knot01, BezierKnot knot02, float lodAmount)
        {
            if (settings.smartReduce)
            {
                var originalRes = resolution;

                resolution = SmartReduceResolution(resolution, knot01.Position, knot01.TangentOut, knot02.Position,
                    knot02.TangentOut);

                if (settings.smoothSlope)
                {
                    var slope = math.abs(math.degrees(PGTrigonometryUtility.Slope(knot01.Position, knot02.Position)));
                    var slopeRatio = 1 + slope / 10f;
                    resolution = (int) math.round(resolution * slopeRatio);
                }

                if (resolution > originalRes) resolution = originalRes;
            }

            resolution = (int) math.round(resolution * lodAmount);
            if (resolution == 0) resolution = 1;

            return resolution;
        }

        private static int SmartReduceResolution(int resolution, float3 position01, float3 tangent01, float3 position02, float3 tangent02)
        {
            var angleTan = math.abs(math.degrees(PGTrigonometryUtility.AngleXZ(tangent01, tangent02)));
            var anglePos = math.abs(math.degrees(PGTrigonometryUtility.AngleXZ(position02 - position01, tangent02)));

            var angle = math.max(anglePos, angleTan);

            if (angle > 90f) angle = 90f;
            if (angle > 2f)
            {
                var angleRatio = 1 / (1 + math.exp(-angle * 0.1f));
                resolution = math.max(1, (int) math.round(resolution * angleRatio));
            }
            else
            {
                resolution = 1;
            }

            return resolution;
        }

        public static float GetUnitCircleTangentLength(float radius)
        {
            // https://mechanicalexpressions.com/explore/geometric-modeling/circle-spline-approximation.pdf
            var value = 4f * (math.sqrt(2f) - 1f) / 3f;
            return value * radius * 2.5f;
        }

        public static List<BezierKnot> GetUniqueKnots(SplineContainer splineContainer, float tolerance = 0.01f)
        {
            var uniqueKnots = new List<BezierKnot>();

            var splines = splineContainer.Splines;
            for (int i = 0; i < splines.Count; i++)
            {
                var spline = splines[i];
                for (int j = 0; j < spline.Count; j++)
                {
                    var knot = spline[j];
                    if (!uniqueKnots.Any(existingKnot => 
                            Mathf.Abs(existingKnot.Position.x - knot.Position.x) < tolerance && 
                            Mathf.Abs(existingKnot.Position.z - knot.Position.z) < tolerance))
                    {
                        uniqueKnots.Add(knot);
                    }
                }
            }

            return uniqueKnots;
        }
        
    }
}