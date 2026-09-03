// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VectorGraphics
{
    /// <summary>
    /// Provides various tools to work with vector graphics.
    /// </summary>
    public static partial class VectorUtils
    {
        /// <summary>A small value used everywhere by the vector graphics package.</summary>
        public static readonly float Epsilon = 0.000001f;

        /// <summary>Convert a segments into a path.</summary>
        /// <param name="segment">The BezierSegment</param>
        /// <returns>An array of two path segments</returns>
        /// <remarks>The second path segment will hold the ending position of the curve.</remarks>
        public static BezierPathSegment[] BezierSegmentToPath(BezierSegment segment)
        {
            return new BezierPathSegment[] {
                new BezierPathSegment() { P0 = segment.P0, P1 = segment.P1, P2 = segment.P2 },
                new BezierPathSegment() { P0 = segment.P3 }
            };
        }

        /// <summary>Converts an array of BezierSegments into a connected path.</summary>
        /// <param name="segments">An array of BezierSegment</param>
        /// <returns>An array of path segments</returns>
        /// <remarks>If two consecutive segments are disconnected, a straight line will be added between the two endpoints.</remarks>
        public static BezierPathSegment[] BezierSegmentsToPath(BezierSegment[] segments)
        {
            if (segments.Length == 0)
                return Array.Empty<BezierPathSegment>();

            int segmentCount = segments.Length;
            var path = new List<BezierPathSegment>(segments.Length*2 + 1);
            for (int i = 0; i < segmentCount; ++i)
            {
                var seg = segments[i];
                path.Add(new BezierPathSegment() { P0 = seg.P0, P1 = seg.P1, P2 = seg.P2 });

                if (i == (segmentCount-1))
                {
                    // Last segment, close the path
                    path.Add(new BezierPathSegment() { P0 = seg.P3 });
                }
                else
                {
                    // Check for connectivity, insert path to connect the endpoints when needed
                    var nextSeg = segments[i+1];
                    if (seg.P3 != nextSeg.P0)
                    {
                        var line = VectorUtils.MakeLine(seg.P3, nextSeg.P0);
                        path.Add(new BezierPathSegment() { P0 = line.P0, P1 = line.P1, P2 = line.P2 });
                    }
                }
            }

            return path.ToArray();
        }

        /// <summary>
        /// Computes the BezierSegment at a given index from a list of BezierPathSegments.
        /// </summary>
        /// <param name="path">The chain of BezierPathSegments</param>
        /// <param name="index">The segment index</param>
        /// <returns>The BezierSegment at the given index</returns>
        public static BezierSegment PathSegmentAtIndex(IList<BezierPathSegment> path, int index)
        {
            if (index < 0 || index >= (path.Count-1))
                throw new IndexOutOfRangeException("Invalid index passed to PathSegmentAtIndex");

            return new BezierSegment() { P0 = path[index].P0, P1 = path[index].P1, P2 = path[index].P2, P3 = path[index + 1].P0 };
        }

        /// <summary>
        /// Checks if the two ends of a BezierPathSegment chain are at the same location.
        /// </summary>
        /// <param name="path">The chain of BezierPathSegments</param>
        /// <returns>True if the two ends of the chain are at the same location, false otherwise</returns>
        public static bool PathEndsPerfectlyMatch(IList<BezierPathSegment> path)
        {
            if (path.Count < 2)
                return false;

            if ((path[0].P0 - path[path.Count - 1].P0).sqrMagnitude > Epsilon)
                return false;

            return true;
        }

        /// <summary>Builds a rectangle shape.</summary>
        /// <param name="rectShape">The shape object that will be filled with a rectangle.</param>
        /// <param name="rect">The position and dimensions of the rectangle.</param>
        public static void MakeRectangleShape(Shape rectShape, Rect rect)
        {
            MakeRectangleShape(rectShape, rect, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        }

        /// <summary>Builds a rectangle shape.</summary>
        /// <param name="rectShape">The shape object that will be filled with a rectangle.</param>
        /// <param name="rect">The position and dimensions of the rectangle.</param>
        /// <param name="radiusTL">The top-left radius of the rectangle</param>
        /// <param name="radiusTR">The top-right radius of the rectangle</param>
        /// <param name="radiusBR">The bottom-right radius of the rectangle</param>
        /// <param name="radiusBL">The bottom-left radius of the rectangle</param>
        public static void MakeRectangleShape(Shape rectShape, Rect rect, Vector2 radiusTL, Vector2 radiusTR, Vector2 radiusBR, Vector2 radiusBL)
        {
            var contour = BuildRectangleContour(rect, radiusTL, radiusTR, radiusBR, radiusBL);
            if (rectShape.Contours == null || rectShape.Contours.Length != 1)
                rectShape.Contours = new BezierContour[1];
            rectShape.Contours[0] = contour;
            rectShape.IsConvex = true;
        }

        /// <summary>Builds an ellipse shape.</summary>
        /// <param name="ellipseShape">The shape object that will be filled with an ellipse.</param>
        /// <param name="pos">The position of the circle, relative to its center.</param>
        /// <param name="radiusX">The x component of the radius of the circle.</param>
        /// <param name="radiusY">The y component of the radius of the circle.</param>
        public static void MakeEllipseShape(Shape ellipseShape, Vector2 pos, float radiusX, float radiusY)
        {
            var rect = new Rect(pos.x-radiusX, pos.y-radiusY, radiusX+radiusX, radiusY+radiusY);
            var rad = new Vector2(radiusX, radiusY);
            MakeRectangleShape(ellipseShape, rect, rad, rad, rad, rad);
        }

        /// <summary>Builds a circle shape.</summary>
        /// <param name="circleShape">The shape object that will be filled with a circle.</param>
        /// <param name="pos">The position of the circle, relative to its center.</param>
        /// <param name="radius">The radius of the circle.</param>
        public static void MakeCircleShape(Shape circleShape, Vector2 pos, float radius)
        {
            MakeEllipseShape(circleShape, pos, radius, radius);
        }

        /// <summary>Computes the bounds of a bezier path.</summary>
        /// <param name="path">The path to compute the bounds from</param>
        /// <returns>A Rect containing the axis-aligned bounding-box of the contour</returns>
        public static Rect Bounds(BezierPathSegment[] path)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(-float.MaxValue, -float.MaxValue);
            foreach (var s in VectorUtils.SegmentsInPath(path))
            {
                Vector2 segMin, segMax;
                Bounds(s, out segMin, out segMax);
                min = Vector2.Min(min, segMin);
                max = Vector2.Max(max, segMax);
            }
            return (min.x != float.MaxValue) ? new Rect(min, max - min) : Rect.zero;
        }

        /// <summary>Computes the bounds of a list of vertices.</summary>
        /// <param name="vertices">The list of vertices to compute the bounds from</param>
        /// <returns>A Rect containing the axis-aligned bounding-box of the vertices</returns>
        public static Rect Bounds(IEnumerable<Vector2> vertices)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(-float.MaxValue, -float.MaxValue);
            foreach (var v in vertices)
            {
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
            return (min.x != float.MaxValue) ? new Rect(min, max - min) : Rect.zero;
        }

        /// <summary>Builds a line segment.</summary>
        /// <param name="from">The starting position of the line segment</param>
        /// <param name="to">The ending position of the line segment</param>
        /// <returns>A straight line BezierSegment</returns>
        /// <remarks>The control points are spaced out equally to maintain a constant speed on t</remarks>
        public static BezierSegment MakeLine(Vector2 from, Vector2 to)
        {
            return new BezierSegment()
            {
                P0 = from,
                P1 = (to - from) / 3.0f + from,
                P2 = (to - from) * 2.0f / 3.0f + from,
                P3 = to
            };
        }

        /// <summary>Converts a quadratic bezier to a cubic bezier</summary>
        /// <param name="p0">The starting position of the quadratic segment</param>
        /// <param name="p1">The control position of the quadratic segment</param>
        /// <param name="p2">The ending position of the quadratic segment</param>
        /// <returns>The resulting BezierSegment</returns>
        public static BezierSegment QuadraticToCubic(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            var p = p1;
            var t = 2.0f / 3.0f;
            return new BezierSegment() {
                P0 = p0,
                P1 = p0 + t * (p - p0),
                P2 = p2 + t * (p - p2),
                P3 = p2,
            };
        }

        /// <summary>Builds a line path segment.</summary>
        /// <param name="from">The starting position of the line segment</param>
        /// <param name="to">The ending position of the line segment</param>
        /// <returns>A BezierPathSegment array of two elements, configured in a straight line</returns>
        /// <remarks>The control points are spaced out equally to maintain a constant speed on t</remarks>
        public static BezierPathSegment[] MakePathLine(Vector2 from, Vector2 to)
        {
            return new BezierPathSegment[] {
                new BezierPathSegment() { P0 = from, P1 = (to - from) / 3.0f + from, P2 = (to - from) * 2.0f / 3.0f + from },
                new BezierPathSegment() { P0 = to }
            };
        }

        internal static BezierSegment MakeArcQuarter(Vector2 center, float startAngleRads, float sweepAngleRads)
        {
            // Approximation adapted from http://spencermortensen.com/articles/bezier-circle/
            float s = Mathf.Sin(sweepAngleRads);
            float c = Mathf.Cos(sweepAngleRads);
            Matrix2D m = Matrix2D.RotateLH(startAngleRads);
            m.m02 = center.x;
            m.m12 = center.y;
            float f = 0.551915024494f;
            return new BezierSegment()
            {
                P0 = m * new Vector2(1, 0),
                P1 = m * new Vector2(1, f),
                P2 = m * new Vector2(c + f * s, s),
                P3 = m * new Vector2(c, s)
            };
        }

        /// <summary>Approximates a circle arc with up to 4 segments.</summary>
        /// <param name="center">The center of the arc</param>
        /// <param name="startAngleRads">The starting angle of the arc, in radians</param>
        /// <param name="sweepAngleRads">The "length" of the arc, in radians</param>
        /// <param name="radius">The radius of the arc</param>
        /// <returns>An array of up to four BezierSegments holding the arc</returns>
        public static BezierPathSegment[] MakeArc(Vector2 center, float startAngleRads, float sweepAngleRads, float radius)
        {
            bool shouldFlip = false;
            if (sweepAngleRads < 0.0f)
            {
                startAngleRads += sweepAngleRads;
                sweepAngleRads = -sweepAngleRads;
                shouldFlip = true;
            }

            sweepAngleRads = Mathf.Min(sweepAngleRads, Mathf.PI * 2);

            BezierSegment subSeg1;
            BezierSegment subSeg2;

            var segments = new List<BezierSegment>();
            int endQuadrant = QuadrantAtAngle(sweepAngleRads);

            for (int quadrant = 0; quadrant <= endQuadrant; ++quadrant)
            {
                var seg = ArcSegmentForQuadrant(quadrant);

                // Check if we need to split the segment
                var p0 = Vector2.zero;
                var p1 = new Vector2(2.0f, 0.0f);
                var intersects = FindBezierLineIntersections(seg, p0, p1);
                if (quadrant != 3 && intersects.Length > 0)
                {
                    VectorUtils.SplitSegment(seg, intersects[0], out subSeg1, out subSeg2);
                    seg = subSeg2;
                }

                p1 = new Vector2(Mathf.Cos(sweepAngleRads), Mathf.Sin(sweepAngleRads)) * 2.0f;
                intersects = FindBezierLineIntersections(seg, p0, p1);
                if (intersects.Length > 0)
                {
                    VectorUtils.SplitSegment(seg, intersects[0], out subSeg1, out subSeg2);
                    seg = subSeg1;
                }

                if (!VectorUtils.IsEmptySegment(seg))
                    segments.Add(seg);
            }

            for (int i = 0; i < segments.Count; ++i)
                segments[i] = TransformSegment(segments[i], center, -startAngleRads, Vector2.one * radius);

            if (shouldFlip)
            {
                // Path is reversed, so we should flip it now
                for (int i = 0; i < segments.Count / 2; ++i)
                {
                    int j = segments.Count - i - 1;
                    var seg0 = VectorUtils.FlipSegment(segments[i]);
                    var seg1 = VectorUtils.FlipSegment(segments[j]);
                    segments[i] = seg1;
                    segments[j] = seg0;
                }
                if ((segments.Count % 2) == 1)
                {
                    int i = segments.Count / 2;
                    segments[i] = VectorUtils.FlipSegment(segments[i]);
                }
            }

            return VectorUtils.BezierSegmentsToPath(segments.ToArray());
        }

        internal static int QuadrantAtAngle(float angle)
        {
            angle = angle % (Mathf.PI * 2);
            if (angle < 0.0f)
                angle = Mathf.PI * 2 + angle;
            if (angle <= Mathf.PI / 2.0f)
                return 0;
            else if (angle <= Mathf.PI)
                return 1;
            else if (angle <= Mathf.PI / 2.0f * 3.0f)
                return 2;
            else
                return 3;
        }

        internal static BezierSegment ArcSegmentForQuadrant(int quadrant)
        {
            switch (quadrant)
            {
                case 0: return VectorUtils.MakeArcQuarter(Vector2.zero, 0.0f, Mathf.PI / 2.0f);
                case 1: return VectorUtils.MakeArcQuarter(Vector2.zero, -Mathf.PI / 2.0f, Mathf.PI / 2.0f);
                case 2: return VectorUtils.MakeArcQuarter(Vector2.zero, -Mathf.PI, Mathf.PI / 2.0f);
                case 3: return VectorUtils.MakeArcQuarter(Vector2.zero, -Mathf.PI / 2.0f * 3.0f, Mathf.PI / 2.0f);
                default: return new BezierSegment();
            }
        }

        /// <summary>Flips a segment direction.</summary>
        /// <param name="segment">The segment to flip</param>
        /// <returns>The flipped segment</returns>
        public static BezierSegment FlipSegment(BezierSegment segment)
        {
            var s = segment;

            var tmp = s.P0;
            s.P0 = s.P3;
            s.P3 = tmp;

            tmp = s.P1;
            s.P1 = s.P2;
            s.P2 = tmp;

            return s;
        }

        /// <summary>Computes the bounds of a segment.</summary>
        /// <param name="segment">The segment to flip</param>
        /// <param name="min">The output min value of the segment</param>
        /// <param name="max">The output max value of the segment</param>
        public static void Bounds(BezierSegment segment, out Vector2 min, out Vector2 max)
        {
            min = Vector2.Min(segment.P0, segment.P3);
            max = Vector2.Max(segment.P0, segment.P3);

            Vector2 a = 3.0f * segment.P3 - 9.0f * segment.P2 + 9.0f * segment.P1 - 3.0f * segment.P0;
            Vector2 b = 6.0f * segment.P2 - 12.0f * segment.P1 + 6.0f * segment.P0;
            Vector2 c = 3.0f * segment.P1 - 3.0f * segment.P0;

            float[] solutions = new float[4];
            SolveQuadratic(a.x, b.x, c.x, out solutions[0], out solutions[1]);
            SolveQuadratic(a.y, b.y, c.y, out solutions[2], out solutions[3]);
            foreach (var s in solutions)
            {
                if (float.IsNaN(s) || (s < 0.0f) || (s > 1.0f))
                    continue;
                Vector2 v = Eval(segment, s);
                min = Vector2.Min(min, v);
                max = Vector2.Max(max, v);
            }
        }

        /// <summary>Evaluates the position on a curve segment.</summary>
        /// <param name="segment">The curve segment on which to evaluate the position</param>
        /// <param name="t">The parametric location on the curve</param>
        /// <returns>The position on the curve at parametric location "t"</returns>
        public static Vector2 Eval(BezierSegment segment, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return
                (segment.P3 - 3.0f * segment.P2 + 3.0f * segment.P1 - segment.P0) * t3
                + (3.0f * segment.P2 - 6.0f * segment.P1 + 3.0f * segment.P0) * t2
                + (3.0f * segment.P1 - 3.0f * segment.P0) * t
                + segment.P0;
        }

        /// <summary>Evaluates the tangent on a curve segment.</summary>
        /// <param name="segment">The curve segment on which to evaluate the tangent</param>
        /// <param name="t">The parametric location on the curve</param>
        /// <returns>The tangent of the curve at parametric location "t"</returns>
        public static Vector2 EvalTangent(BezierSegment segment, float t)
        {
            var tan = (segment.P3 - 3.0f * segment.P2 + 3.0f * segment.P1 - segment.P0) * 3.0f * t * t
                + (3.0f * segment.P2 - 6.0f * segment.P1 + 3.0f * segment.P0) * 2.0f * t
                + (3.0f * segment.P1 - 3.0f * segment.P0);

            // If the result is a zero vector (happens at coincident p0 and p1 or p2 and p3) try again by manual stepping
            if (tan.sqrMagnitude < Epsilon)
            {
                if (t > 0.5f)
                    tan = Eval(segment, t) - Eval(segment, t - 0.01f);
                else tan = Eval(segment, t + 0.01f) - Eval(segment, t);
            }
            return tan.normalized;
        }

        /// <summary>Evalutes the normal on a curve segment.</summary>
        /// <param name="segment">The curve segment on which to evaluate the normal</param>
        /// <param name="t">The parametric location on the curve</param>
        /// <returns>The normal of the curve at parametric location "t"</returns>
        /// <remarks>
        /// A positive normal at a point on the bezier curve is always on the
        /// right side of the forward direction (tangent) of the curve at that point.
        /// </remarks>
        public static Vector2 EvalNormal(BezierSegment segment, float t)
        {
            return Vector2.Perpendicular(EvalTangent(segment, t));
        }

        /// <summary>Evalutes both the position and tangent on a curve segment.</summary>
        /// <param name="segment">The curve segment on which to evaluate the normal</param>
        /// <param name="t">The parametric location on the curve</param>
        /// <param name="tangent">The output tangent at parametric location "t"</param>
        /// <returns>The position on the curve at parametric location "t"</returns>
        /// <remarks>
        /// This is more efficient than calling "Eval" and "EvalTangent" successively.
        /// </remarks>
        public static Vector2 EvalFull(BezierSegment segment, float t, out Vector2 tangent)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            Vector2 C1 = segment.P3 - 3.0f * segment.P2 + 3.0f * segment.P1 - segment.P0;
            Vector2 C2 = 3.0f * segment.P2 - 6.0f * segment.P1 + 3.0f * segment.P0;
            Vector2 C3 = 3.0f * segment.P1 - 3.0f * segment.P0;
            Vector2 C4 = segment.P0;

            var pos = C1 * t3 + C2 * t2 + C3 * t + C4;
            tangent = ((3.0f * C1 * t2) + (2.0f * C2 * t) + C3);

            // If the result is a zero vector (happens at coincident p0 and p1 or p2 and p3) try again by manual stepping
            if (tangent.sqrMagnitude < Epsilon)
            {
                if (t > 0.5f)
                    tangent = pos - Eval(segment, t - 0.01f);
                else tangent = Eval(segment, t + 0.01f) - pos;
            }

            tangent = tangent.normalized;
            return pos;
        }

        /// <summary>Evalutes the position, tangent and normal on a curve segment.</summary>
        /// <param name="segment">The curve segment on which to evaluate the normal</param>
        /// <param name="t">The parametric location on the curve</param>
        /// <param name="tangent">The output tangent at parametric location "t"</param>
        /// <param name="normal">The output normal at parametric location "t"</param>
        /// <returns>The position on the curve at parametric location "t"</returns>
        /// <remarks>
        /// This is more efficient than calling "Eval", "EvalTangent" and "EvalNormal" successively.
        /// </remarks>
        public static Vector2 EvalFull(BezierSegment segment, float t, out Vector2 tangent, out Vector2 normal)
        {
            Vector2 pos = EvalFull(segment, t, out tangent);
            normal = Vector2.Perpendicular(tangent);
            return pos;
        }

        /// <summary>Computes the individual lengths of a segment chain.</summary>
        /// <param name="segments">The segments on which to compute the lengths</param>
        /// <param name="closed">A boolean indicating if the length of the segment joining the first and last points should be computed</param>
        /// <param name="precision">The precision of the lengths computation</param>
        /// <returns>An array containing the lenghts of the segments</returns>
        public static float[] SegmentsLengths(IList<BezierPathSegment> segments, bool closed, float precision = 0.001f)
        {
            float[] segmentLengths = new float[segments.Count - 1 + (closed ? 1 : 0)];
            int i = 0;
            foreach (var segment in SegmentsInPath(segments, closed))
                segmentLengths[i++] = SegmentLength(segment, precision);
            return segmentLengths;
        }

        /// <summary>Computes the combined length of a segment chain.</summary>
        /// <param name="segments">The curve segments on which to evaluate the length</param>
        /// <param name="closed">A boolean indicating if the length of the segment joining the first and last points should be computed</param>
        /// <param name="precision">The precision of the length computation</param>
        /// <returns>The combined length of the segment chain</returns>
        public static float SegmentsLength(IList<BezierPathSegment> segments, bool closed, float precision = 0.001f)
        {
            if (segments.Count < 2)
                return 0.0f;

            float length = 0.0f;
            foreach (var segment in SegmentsInPath(segments))
                length += SegmentLength(segment, precision);
            if (closed)
                length += (segments[segments.Count - 1].P0 - segments[0].P0).magnitude;
            return length;
        }

        /// <summary>Computes the length of a single curve segment.</summary>
        /// <param name="segment">The curve segment on which to evaluate the length</param>
        /// <param name="precision">The precision of the length computation</param>
        /// <returns>The length of the segment</returns>
        public static float SegmentLength(BezierSegment segment, float precision = 0.001f)
        {
            // This adaptive algorithm doesn't behave well at the limit of float precision,
            // so we revert to a dummy iterative approach in this case
            if (VectorUtils.HasLargeCoordinates(segment))
            {
                int steps = Math.Min(100, (int)(1.0f/precision));
                return SegmentLengthIterative(segment, steps);
            }

            float tmax = 0.0f;
            float length = 0.0f;
            while ((tmax = AdaptiveQuadraticApproxSplitPoint(segment, precision)) < 1.0f)
            {
                BezierSegment b1, b2;
                SplitSegment(segment, tmax, out b1, out b2);
                float midPointLength = MidPointQuadraticApproxLength(b1);
                if (float.IsNaN(midPointLength)) // Could happen because of float precision issues
                    midPointLength = SegmentLengthIterative(b1);
                length += midPointLength;
                segment = b2;
            }
            length += MidPointQuadraticApproxLength(segment);
            return length;
        }

        internal static float SegmentLengthIterative(BezierSegment segment, int steps = 10)
        {
            if (steps <= 2)
                return (segment.P3 - segment.P0).magnitude;

            float length = 0.0f;
            var p = segment.P0;
            for (int i = 1; i <= steps; ++i)
            {
                float t = (float)i/steps;
                var q = VectorUtils.Eval(segment, t);
                length += (q-p).magnitude;
                p = q;
            }
            return length;
        }

        internal static bool HasLargeCoordinates(BezierSegment segment)
        {
            const float kMaxCoord = 10000.0f;
            return
                segment.P0.x > kMaxCoord || segment.P0.y > kMaxCoord ||
                segment.P1.x > kMaxCoord || segment.P1.y > kMaxCoord ||
                segment.P2.x > kMaxCoord || segment.P2.y > kMaxCoord ||
                segment.P3.x > kMaxCoord || segment.P3.y > kMaxCoord;
        }

        static float AdaptiveQuadraticApproxSplitPoint(BezierSegment segment, float precision)
        {
            float quadraticApproxDist = (segment.P3 - 3.0f * segment.P2 + 3.0f * segment.P1 - segment.P0).magnitude * 0.5f;
            return Mathf.Pow((18.0f / Mathf.Sqrt(3.0f)) * precision / quadraticApproxDist, 1.0f / 3.0f);
        }

        static float MidPointQuadraticApproxLength(BezierSegment segment)
        {
            var A = segment.P0;
            var B = (3.0f * segment.P2 - segment.P3 + 3.0f * segment.P1 - segment.P0) / 4.0f;
            var C = segment.P3;

            if (A == C)
                return (A == B) ? 0.0f : (A - B).magnitude;

            if (B == A || B == C)
                return (A - C).magnitude;

            var A0 = B - A;
            var A1 = A - 2.0f * B + C;

            if (A1 != Vector2.zero)
            {
                double c = 4.0f * Vector2.Dot(A1, A1);
                double b = 8.0f * Vector2.Dot(A0, A1);
                double a = 4.0f * Vector2.Dot(A0, A0);
                double q = 4.0f * a * c - b * b;

                double twoCpB = 2.0f * c + b;
                double sumCBA = c + b + a;

                var l0 = (0.25f / c) * (twoCpB * Math.Sqrt(sumCBA) - b * Math.Sqrt(a));
                if (Math.Abs(q) <= VectorUtils.Epsilon)
                    return (float)l0;

                var l1 = (q / (8.0f * Math.Pow(c, 1.5f))) * (Math.Log(2.0f * Math.Sqrt(c * sumCBA) + twoCpB) - Math.Log(2.0f * Math.Sqrt(c * a) + b));
                return (float)(l0 + l1);
            }
            else return 2.0f * A0.magnitude;
        }

        /// <summary>Splits a curve segment at a given parametric location.</summary>
        /// <param name="segment">The curve segment to split</param>
        /// <param name="t">The parametric location at which the segment will be split</param>
        /// <param name="b1">The output of the first segment</param>
        /// <param name="b2">The output of the second segment</param>
        public static void SplitSegment(BezierSegment segment, float t, out BezierSegment b1, out BezierSegment b2)
        {
            var a = Vector2.LerpUnclamped(segment.P0, segment.P1, t);
            var b = Vector2.LerpUnclamped(segment.P1, segment.P2, t);
            var c = Vector2.LerpUnclamped(segment.P2, segment.P3, t);
            var m = Vector2.LerpUnclamped(a, b, t);
            var n = Vector2.LerpUnclamped(b, c, t);
            var p = Eval(segment, t);

            b1 = new BezierSegment() { P0 = segment.P0, P1 = a, P2 = m, P3 = p };
            b2 = new BezierSegment() { P0 = p, P1 = n, P2 = c, P3 = segment.P3 };
        }

        /// <summary>Transforms a curve segment by a translation, rotation and scaling.</summary>
        /// <param name="segment">The curve segment to transform</param>
        /// <param name="translation">The translation to apply on the curve segment</param>
        /// <param name="rotation">The rotation to apply on the curve segment</param>
        /// <param name="scaling">The scaling to apply on the curve segment</param>
        /// <returns>The transformed curve segment</returns>
        public static BezierSegment TransformSegment(BezierSegment segment, Vector2 translation, float rotation, Vector2 scaling)
        {
            var m = Matrix2D.RotateLH(rotation);
            var newSeg = new BezierSegment() {
                P0 = m * Vector2.Scale(segment.P0, scaling) + translation,
                P1 = m * Vector2.Scale(segment.P1, scaling) + translation,
                P2 = m * Vector2.Scale(segment.P2, scaling) + translation,
                P3 = m * Vector2.Scale(segment.P3, scaling) + translation
            };
            return newSeg;
        }

        /// <summary>Transforms a curve segment by a transformation matrix.</summary>
        /// <param name="segment">The curve segment to transform</param>
        /// <param name="matrix">The transformation matrix to apply on the curve segment</param>
        /// <returns>The transformed curve segment</returns>
        public static BezierSegment TransformSegment(BezierSegment segment, Matrix2D matrix)
        {
            var newSeg = new BezierSegment() {
                P0 = matrix * segment.P0,
                P1 = matrix * segment.P1,
                P2 = matrix * segment.P2,
                P3 = matrix * segment.P3
            };
            return newSeg;
        }

        /// <summary>Transforms a path by a transformation matrix.</summary>
        /// <param name="path">The path to transform</param>
        /// <param name="translation">The translation to apply</param>
        /// <param name="rotation">The rotation to apply, in radians</param>
        /// <param name="scaling">The scaling to apply</param>
        /// <returns>The transformed path</returns>
        public static BezierPathSegment[] TransformBezierPath(BezierPathSegment[] path, Vector2 translation, float rotation, Vector2 scaling)
        {
            var m = Matrix2D.RotateLH(rotation);
            var newPath = new BezierPathSegment[path.Length];
            for (int i = 0; i < newPath.Length; ++i)
            {
                var seg = path[i];
                newPath[i] = new BezierPathSegment()
                {
                    P0 = m * Vector2.Scale(seg.P0, scaling) + translation,
                    P1 = m * Vector2.Scale(seg.P1, scaling) + translation,
                    P2 = m * Vector2.Scale(seg.P2, scaling) + translation
                };
            }
            return newPath;
        }

        /// <summary>Transforms a path by a transformation matrix.</summary>
        /// <param name="path">The path to transform</param>
        /// <param name="matrix">The transformation matrix to apply on the curve segment</param>
        /// <returns>The transformed path</returns>
        public static BezierPathSegment[] TransformBezierPath(BezierPathSegment[] path, Matrix2D matrix)
        {
            var newPath = new BezierPathSegment[path.Length];
            for (int i = 0; i < newPath.Length; ++i)
            {
                var seg = path[i];
                newPath[i] = new BezierPathSegment() {
                    P0 = matrix * seg.P0,
                    P1 = matrix * seg.P1,
                    P2 = matrix * seg.P2
                };
            }
            return newPath;
        }

        /// <summary>Lists every nodes under a root node.</summary>
        /// <param name="root">The root node</param>
        /// <returns>The enumerable listing every nodes under "root", including the root itself.</returns>
        public static IEnumerable<SceneNode> SceneNodes(SceneNode root)
        {
            yield return root;
            if (root.Children != null)
            {
                foreach (var c in root.Children)
                {
                    foreach (var n in SceneNodes(c))
                        yield return n;
                }
            }
        }

        /// <summary>Structure holding the SceneNode computed transforms, opacities and enumeration path.</summary>
        /// <remarks>This helper structure is used by the WorldTransformedSceneNodes method.</remarks>
        public struct SceneNodeWorldTransform
        {
            /// <summary>The node we are currently visiting.</summary>
            public SceneNode Node;

            /// <summary>The parent of the node we are currently visiting.</summary>
            public SceneNode Parent;

            /// <summary>The accumulated world transform of this node.</summary>
            public Matrix2D WorldTransform;

            /// <summary>The accumulated world opacity of this node.</summary>
            public float WorldOpacity;
        }

        static IEnumerable<SceneNodeWorldTransform> WorldTransformedSceneNodes(SceneNode child, Dictionary<SceneNode, float> nodeOpacities, SceneNodeWorldTransform parent)
        {
            var childOpacity = 1.0f;
            if (nodeOpacities == null || !nodeOpacities.TryGetValue(child, out childOpacity))
                childOpacity = 1.0f;

            var childWorldTransform = new SceneNodeWorldTransform()
            {
                Node = child,
                WorldTransform = parent.WorldTransform * child.Transform,
                WorldOpacity = parent.WorldOpacity * childOpacity,
                Parent = parent.Node
            };

            yield return childWorldTransform;

            if (child.Children != null)
            {
                foreach (var c in child.Children)
                {
                    foreach (var n in WorldTransformedSceneNodes(c, nodeOpacities, childWorldTransform))
                        yield return n;
                }
            }
        }

        /// <summary>Iterates through every nodes under a root with computed transform and opacities.</summary>
        /// <param name="root">The starting node of the hierarchy</param>
        /// <param name="nodeOpacities">Storage for the resulting node opacities, may be null</param>
        /// <returns>An enumeration of every node with their pre-computed world transforms, opacities and paths.</returns>
        public static IEnumerable<SceneNodeWorldTransform> WorldTransformedSceneNodes(SceneNode root, Dictionary<SceneNode, float> nodeOpacities)
        {
            var rootNodeWorldTransform = new SceneNodeWorldTransform() {
                Node = root,
                WorldTransform = Matrix2D.identity,
                WorldOpacity = 1,
                Parent = null
            };
            return WorldTransformedSceneNodes(root, nodeOpacities, rootNodeWorldTransform);
        }

        /// <summary>Realigns the vertices (in-place) inside their axis-aligned bounding-box.</summary>
        /// <param name="vertices">The vertices to realign</param>
        /// <param name="bounds">The bounds into which the vertices will be realigned</param>
        /// <param name="flip">A boolean indicating whether to flip the coordinates on the Y axis</param>
        public static void RealignVerticesInBounds(IList<Vector2> vertices, Rect bounds, bool flip)
        {
            var p = bounds.position;
            var h = bounds.height;
            for (int i = 0; i < vertices.Count; ++i)
            {
                var v = vertices[i];
                v -= p;
                if (flip)
                    v.y = h - v.y;
                vertices[i] = v;
            }
        }

        /// <summary>Flip the vertices (in-place) inside their axis-aligned bounding-box.</summary>
        /// <param name="vertices">The vertices to realign</param>
        /// <param name="bounds">The bounds into which the vertices will be realigned</param>
        public static void FlipVerticesInBounds(IList<Vector2> vertices, Rect bounds)
        {
            var h = bounds.height;
            for (int i = 0; i < vertices.Count; ++i)
            {
                var v = vertices[i];
                v.y = h - v.y;
                vertices[i] = v;
            }
        }

        internal static void ClampVerticesInBounds(IList<Vector2> vertices, Rect bounds)
        {
            for (int i = 0; i < vertices.Count; ++i)
                vertices[i] = Vector2.Max(bounds.min, Vector2.Min(bounds.max, vertices[i]));
        }

        /// <summary>Iterates through every segment in a list of path segments.</summary>
        /// <param name="segments">The path segments to iterate from</param>
        /// <param name="closed">Whether to return the segment connecting the last point to the beginning of the path</param>
        /// <returns>An enumerable of every segments in the path</returns>
        public static IEnumerable<BezierSegment> SegmentsInPath(IEnumerable<BezierPathSegment> segments, bool closed = false)
        {
            var e = segments.GetEnumerator();
            if (!e.MoveNext())
                yield break;

            var s1 = e.Current;
            if (!e.MoveNext())
                yield break;

            do
            {
                var s2 = e.Current;
                yield return new BezierSegment { P0 = s1.P0, P1 = s1.P1, P2 = s1.P2, P3 = s2.P0 };
                s1 = s2;
            }
            while (e.MoveNext());

            if (closed)
            {
                var first = Vector2.zero;
                foreach (var seg in segments)
                {
                    first = seg.P0;
                    break;
                }

                yield return new BezierSegment { P0 = s1.P0, P1 = s1.P1, P2 = s1.P2, P3 = first };
            }
        }

        static void SolveQuadratic(float a, float b, float c, out float s1, out float s2)
        {
            float det = b * b - 4.0f * a * c;
            if (det < 0.0f)
            {
                s1 = s2 = float.NaN;
                return;
            }

            float detSqrt = Mathf.Sqrt(det);
            s1 = (-b + detSqrt) / (2.0f * a);
            if (Mathf.Abs(a) > float.Epsilon)
                s2 = (-b - detSqrt) / (2.0f * a);
            else s2 = float.NaN;
        }

        /// <summary>Finds the intersection between two infinite lines</summary>
        /// <param name="line1Pt1">The first point of the first line</param>
        /// <param name="line1Pt2">The second point of the first line</param>
        /// <param name="line2Pt1">The first point of the second line</param>
        /// <param name="line2Pt2">The second point of the second line</param>
        /// <returns>The intersection point, or (float.PositiveInfinity, float.PositiveInfinity) if the lines are parallel</returns>
        public static Vector2 IntersectLines(Vector2 line1Pt1, Vector2 line1Pt2, Vector2 line2Pt1, Vector2 line2Pt2)
        {
            var a1 = line1Pt2.y - line1Pt1.y;
            var b1 = line1Pt1.x - line1Pt2.x;

            var a2 = line2Pt2.y - line2Pt1.y;
            var b2 = line2Pt1.x - line2Pt2.x;

            var det = a1 * b2 - a2 * b1;
            if (Mathf.Abs(det) <= Epsilon)
                return new Vector2(float.PositiveInfinity, float.PositiveInfinity); // Parallel, no intersection

            var c1 = a1 * line1Pt1.x + b1 * line1Pt1.y;
            var c2 = a2 * line2Pt1.x + b2 * line2Pt1.y;
            var detInv = 1.0f / det;
            return new Vector2((b2 * c1 - b1 * c2) * detInv, (a1 * c2 - a2 * c1) * detInv);
        }

        /// <summary>Finds the intersection between two line segments</summary>
        /// <param name="line1Pt1">The first point of the first line</param>
        /// <param name="line1Pt2">The second point of the first line</param>
        /// <param name="line2Pt1">The first point of the second line</param>
        /// <param name="line2Pt2">The second point of the second line</param>
        /// <returns>The intersection point, or (float.PositiveInfinity, float.PositiveInfinity) if the lines are parallel</returns>
        public static Vector2 IntersectLineSegments(Vector2 line1Pt1, Vector2 line1Pt2, Vector2 line2Pt1, Vector2 line2Pt2)
        {
            var a1 = (line1Pt1.x - line2Pt2.x) * (line1Pt2.y - line2Pt2.y) - (line1Pt1.y - line2Pt2.y) * (line1Pt2.x - line2Pt2.x);
            var a2 = (line1Pt1.x - line2Pt1.x) * (line1Pt2.y - line2Pt1.y) - (line1Pt1.y - line2Pt1.y) * (line1Pt2.x - line2Pt1.x);
            if (a1 * a2 <= 0.0f)
            {
                var a3 = (line2Pt1.x - line1Pt1.x) * (line2Pt2.y - line1Pt1.y) - (line2Pt1.y - line1Pt1.y) * (line2Pt2.x - line1Pt1.x);
                var a4 = a3 + a2 - a1;
                if (a3 * a4 <= 0.0f)
                {
                    float t = a3 / (a3 - a4);
                    var p = line1Pt1 + t * (line1Pt2 - line1Pt1);
                    return p;
                }
            }
            return new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        }

        static bool PointOnTheLeftOfLine(Vector2 lineFrom, Vector2 lineTo, Vector2 point)
        {
            return ((lineFrom.x - lineTo.x) * (point.y - lineTo.y) - (lineFrom.y - lineTo.y) * (point.x - lineTo.x)) > 0;
        }

        /// <summary>Find the intersections (up to three) between a line and a curve segment.</summary>
        /// <param name="segment">The curve segment</param>
        /// <param name="p0">The first point</param>
        /// <param name="p1">The second point</param>
        /// <returns>Returns the Bezier's 't' parametric values where the line p0-p1 intersects the segment, up to 3 values</returns>
        public static float[] FindBezierLineIntersections(BezierSegment segment, Vector2 p0, Vector2 p1)
        {
            var A = p1.y - p0.y;
            var B = p0.x - p1.x;
            var C = p0.x * (p0.y - p1.y) + p0.y * (p1.x - p0.x);
            var coeffs = BezierCoefficients(segment);

            var P = new float[4];
            P[0] = A * coeffs[0].x + B * coeffs[0].y;
            P[1] = A * coeffs[1].x + B * coeffs[1].y;
            P[2] = A * coeffs[2].x + B * coeffs[2].y;
            P[3] = A * coeffs[3].x + B * coeffs[3].y + C;

            var roots = CubicRoots(P[0], P[1], P[2], P[3]);
            var validRoots = new List<float>(roots.Length);

            foreach (var t in roots)
            {
                var t2 = t * t;
                var t3 = t2 * t;
                var p = coeffs[0] * t3 + coeffs[1] * t2 + coeffs[2] * t + coeffs[3];

                var s = 0.0f;
                if (Mathf.Abs(p1.x - p0.x) > VectorUtils.Epsilon)
                    s = (p.x - p0.x) / (p1.x - p0.x);
                else
                    s = (p.y - p0.y) / (p1.y - p0.y);

                if (t >= 0.0f && t <= 1.0f && s >= 0.0f && s <= 1.0f)
                    validRoots.Add(t);
            }

            return validRoots.ToArray();
        }

        private static float[] CubicRoots(double a, double b, double c, double d)
        {
            var A = b / a;
            var B = c / a;
            var C = d / a;
            var Q = (3 * B - Math.Pow(A, 2)) / 9;
            var R = (9 * A * B - 27 * C - 2 * Math.Pow(A, 3)) / 54;
            var D = Math.Pow(Q, 3) + Math.Pow(R, 2);
            var Im = 0.0;
            var t = new List<double>(3);
            t.AddRange(new double[] { -1.0, -1.0, -1.0 });

            if (D >= 0)
            {
                var sqrtD = Math.Sqrt(D);
                var S = Math.Sign(R + sqrtD) * Math.Pow(Math.Abs(R + sqrtD), 1.0 / 3.0);
                var T = Math.Sign(R - sqrtD) * Math.Pow(Math.Abs(R - sqrtD), 1.0 / 3.0);

                t[0] = -A / 3 + (S + T);
                t[1] = -A / 3 - (S + T) / 2;
                t[2] = t[1];
                Im = Math.Abs(Math.Sqrt(3.0) * (S - T) / 2);

                if (Math.Abs(Im) > VectorUtils.Epsilon)
                {
                    t[1] = -1;
                    t[2] = -1;
                }
            }
            else
            {
                var th = Math.Acos(R / Math.Sqrt(-Math.Pow(Q, 3)));
                var sqrtMinusQ = Math.Sqrt(-Q);
                t[0] = 2 * sqrtMinusQ * Math.Cos(th / 3) - A / 3;
                t[1] = 2 * sqrtMinusQ * Math.Cos((th + 2 * Math.PI) / 3) - A / 3;
                t[2] = 2 * sqrtMinusQ * Math.Cos((th + 4 * Math.PI) / 3) - A / 3;
            }

            for (int i = 0; i < 3; ++i)
            {
                if (t[i] < 0.0 || t[i] > 1.0)
                    t[i] = -1;
            }

            // Remove -1 values which means no root was found
            t.RemoveAll(x => Math.Abs(x + 1.0) < (double)Epsilon);

            var result = new float[t.Count];
            for (int i = 0; i < t.Count; ++i)
                result[i] = (float)t[i];

            return result;
        }

        private static Vector2[] BezierCoefficients(BezierSegment segment)
        {
            var coeffs = new Vector2[4];
            coeffs[0] = -segment.P0 + 3 * segment.P1 + -3 * segment.P2 + segment.P3;
            coeffs[1] = 3 * segment.P0 - 6 * segment.P1 + 3 * segment.P2;
            coeffs[2] = -3 * segment.P0 + 3 * segment.P1;
            coeffs[3] = segment.P0;
            return coeffs;
        }

        /// <summary>Computes a pretty accurate approximation of the scene bounds.</summary>
        /// <param name="root">The root node of the hierarchy to computes the bounds from</param>
        /// <returns>An approximation of the node hierarchy axis-aligned bounding-box</returns>
        /// <remarks>
        /// This will properly evaluate the bounds of the paths and shapes, but will ignore the paths stroke widths.
        /// </remarks>
        #pragma warning disable 612, 618 // Silence use of deprecated IDrawable
        public static Rect SceneNodeBounds(SceneNode root)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(-float.MaxValue, -float.MaxValue);
            foreach (var tnode in WorldTransformedSceneNodes(root, null))
            {
                var shapeMin = new Vector2(float.MaxValue, float.MaxValue);
                var shapeMax = new Vector2(-float.MaxValue, -float.MaxValue);

                if (tnode.Node.Shapes != null)
                {
                    foreach (var shape in tnode.Node.Shapes)
                    {
                        foreach (var contour in shape.Contours)
                        {
                            var bbox = Bounds(TransformBezierPath(contour.Segments, tnode.WorldTransform));
                            shapeMin = Vector2.Min(shapeMin, bbox.min);
                            shapeMax = Vector2.Max(shapeMax, bbox.max);
                        }
                    }
                }

                if (shapeMin.x != float.MaxValue)
                {
                    min = Vector2.Min(min, shapeMin);
                    max = Vector2.Max(max, shapeMax);
                }
            }
            return (min.x != float.MaxValue) ? new Rect(min, max - min) : Rect.zero;
        }

        /// <summary>Computes a rough approximation of the node hierarchy bounds.</summary>
        /// <param name="root">The root node of the hierarchy to computes the bounds from</param>
        /// <returns>An approximation of the root hierarchy axis-aligned bounding-box</returns>
        /// <remarks>
        /// This will use the control point positions as a rough estimate of the bounds for the paths and shapes.
        /// </remarks>
        public static Rect ApproximateSceneNodeBounds(SceneNode root)
        {
            var vertices = new List<Vector2>(100);

            foreach (var tnode in WorldTransformedSceneNodes(root, null))
            {
                if (tnode.Node.Shapes != null)
                {
                    foreach (var shape in tnode.Node.Shapes)
                    {
                        foreach (var contour in shape.Contours)
                        {
                            foreach (var seg in TransformBezierPath(contour.Segments, tnode.WorldTransform))
                            {
                                vertices.Add(seg.P0);
                                vertices.Add(seg.P1);
                                vertices.Add(seg.P2);
                            }
                        }
                    }
                }
            }

            return Bounds(vertices);
        }
        #pragma warning restore 612, 618

        internal static bool IsEmptySegment(BezierSegment bs)
        {
            return (bs.P0 - bs.P1).sqrMagnitude <= Epsilon && (bs.P0 - bs.P2).sqrMagnitude <= Epsilon && (bs.P0 - bs.P3).sqrMagnitude <= Epsilon;
        }
    } // VectorUtils class

    internal class PathDistanceForwardIterator
    {
        class BezierLoop : IList<BezierPathSegment>
        {
            IList<BezierPathSegment> OpenPath;

            public BezierLoop(IList<BezierPathSegment> openPath)
            {
                this.OpenPath = openPath;
            }

            public BezierPathSegment this[int index]
            {
                get
                {
                    if (index == OpenPath.Count)
                        return OpenPath[0];
                    return OpenPath[index];
                }
                set { throw new NotSupportedException(); }
            }

            public int Count { get { return OpenPath.Count + 1; } }
            public bool IsReadOnly { get { return true; } }
            public void Add(BezierPathSegment item) { throw new NotSupportedException(); }
            public void Clear() {}
            public bool Contains(BezierPathSegment item) { throw new NotImplementedException(); }
            public void CopyTo(BezierPathSegment[] array, int arrayIndex) { throw new NotImplementedException(); }
            public IEnumerator<BezierPathSegment> GetEnumerator() { throw new NotImplementedException(); }
            public int IndexOf(BezierPathSegment item) { throw new NotImplementedException(); }
            public void Insert(int index, BezierPathSegment item) { throw new NotSupportedException(); }
            public bool Remove(BezierPathSegment item) { throw new NotSupportedException(); }
            public void RemoveAt(int index) { throw new NotSupportedException(); }
            IEnumerator IEnumerable.GetEnumerator() { throw new NotImplementedException(); }
        }

        public enum Result { Stepped, NewSegment, Ended };
        public PathDistanceForwardIterator(IList<BezierPathSegment> pathSegments, bool closed, float maxCordDeviationSq, float maxTanAngleDevCosine, float stepSizeT)
        {
            if (pathSegments.Count < 2)
                throw new Exception("Cannot iterate a path with no segments in it");

            Segments = closed && !VectorUtils.PathEndsPerfectlyMatch(pathSegments) ? new BezierLoop(pathSegments) : pathSegments;
            this.closed = closed;
            this.needTangentsDuringEval = maxTanAngleDevCosine < 1.0f;
            this.maxCordDeviationSq = maxCordDeviationSq;
            this.maxTanAngleDevCosine = maxTanAngleDevCosine;
            this.stepSizeT = stepSizeT;
            currentBezSeg = new BezierSegment() { P0 = pathSegments[0].P0, P1 = pathSegments[0].P1, P2 = pathSegments[0].P2, P3 = pathSegments[1].P0 };
            lastPointEval = pathSegments[0].P0;
            currentTTangent = needTangentsDuringEval ? VectorUtils.EvalTangent(currentBezSeg, 0.0f) : Vector2.zero;
        }

        float PointToLineDistanceSq(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            float lineMagSq = (lineEnd - lineStart).sqrMagnitude;
            if (lineMagSq < VectorUtils.Epsilon)
                return (point - lineStart).sqrMagnitude;
            float num = (lineEnd.y - lineStart.y) * point.x - (lineEnd.x - lineStart.x) * point.y + lineEnd.x * lineStart.y - lineEnd.y * lineStart.x;
            return (num * num) / lineMagSq;
        }

        public Result AdvanceBy(float units, out float unitsRemaining)
        {
            unitsRemaining = units;
            if (Ended)
                return Result.Ended; // Reached the end

            float t = currentT;
            Vector2 currentTPosition = lastPointEval;
            for (;;)
            {
                float nextT = Mathf.Min(t + stepSizeT, 1.0f);
                Vector2 tangent = Vector2.zero;
                Vector2 point = needTangentsDuringEval ? VectorUtils.EvalFull(currentBezSeg, nextT, out tangent) : VectorUtils.Eval(currentBezSeg, nextT);

                bool generateStepHere = false;
                if (needTangentsDuringEval)
                {
                    float tangentDiffCosAngle = Vector2.Dot(tangent, currentTTangent);
                    generateStepHere = tangentDiffCosAngle < this.maxTanAngleDevCosine;
                }

                if (!generateStepHere && (maxCordDeviationSq != float.MaxValue))
                {
                    Vector2 firstPoint = currentTPosition;
                    float distPtToFirstSq = (point - firstPoint).sqrMagnitude;
                    if (distPtToFirstSq > VectorUtils.Epsilon)
                    {
                        Vector2 secondPoint = VectorUtils.Eval(currentBezSeg, Mathf.Min((nextT - currentT) * 2.0f + currentT, 1.0f));
                        float midPointDistSq = PointToLineDistanceSq(point, firstPoint, secondPoint);
                        generateStepHere = midPointDistSq >= maxCordDeviationSq;
                    }
                }

                float dist = (point - lastPointEval).magnitude;
                if (dist > unitsRemaining)
                {
                    nextT = t + stepSizeT * (unitsRemaining / dist); // A linear approximation, not too bad for small step sizes
                    dist = unitsRemaining;
                    point = VectorUtils.Eval(currentBezSeg, nextT);
                }

                segmentLengthSoFar += dist;
                lengthSoFar += dist;
                unitsRemaining -= dist;
                lastPointEval = point;
                t = nextT;

                if (nextT < 1.0f)
                {
                    if ((unitsRemaining > 0) && !generateStepHere)
                        continue;

                    currentT = nextT;
                    currentTTangent = tangent;

                    return Result.Stepped;
                }

                // Crossing to a new segment
                if (currentSegment + 1 == Segments.Count - 1)
                {
                    currentT = 1.0f;
                    return Result.Ended; // Reached the end
                }

                currentSegment++;
                currentBezSeg = new BezierSegment()
                {
                    P0 = Segments[currentSegment].P0,
                    P1 = Segments[currentSegment].P1,
                    P2 = Segments[currentSegment].P2,
                    P3 = Segments[currentSegment + 1].P0
                };
                segmentLengthSoFar = 0.0f;
                currentT = 0.0f;
                currentTTangent = tangent;
                lastPointEval = currentBezSeg.P0;

                return Result.NewSegment;
            }
        }

        public IList<BezierPathSegment> Segments { get; }
        public bool Closed { get { return closed; } }
        public int CurrentSegment { get { return currentSegment; } }
        public float CurrentT { get { return currentT; } }
        public float LengthSoFar { get { return lengthSoFar; } }
        public float SegmentLengthSoFar { get { return segmentLengthSoFar; } }
        public bool Ended { get { return (currentT == 1.0f) && (currentSegment + 1 == Segments.Count - 1); } }
        public Vector2 EvalCurrent() { return VectorUtils.Eval(currentBezSeg, currentT); }

        // Path data and settings
        readonly bool closed, needTangentsDuringEval;
        readonly float maxCordDeviationSq, maxTanAngleDevCosine, stepSizeT; // Quality control variables

        // State
        int currentSegment;
        float currentT;
        float segmentLengthSoFar; // For user's tracking purposes, not really used in our calculations
        float lengthSoFar; // For user's tracking purposes, not really used in our calculations
        Vector2 lastPointEval, currentTTangent;
        BezierSegment currentBezSeg;
    }

    internal class PathPatternIterator
    {
        public PathPatternIterator(float[] pattern, float patternOffset = 0.0f)
        {
            if (pattern != null)
            {
                foreach (var l in pattern)
                    patternLength += l;
            }

            if (patternLength < VectorUtils.Epsilon)
            {
                segmentLength = float.MaxValue;
                return;
            }

            this.pattern = pattern;
            this.patternOffset = patternOffset;
            if (patternOffset == 0.0f)
                segmentLength = pattern[0];
            else this.solid = IsSolidAt(0.0f, out currentSegment, out segmentLength);
        }

        public void Advance()
        {
            if (pattern == null)
                return;

            currentSegment++;
            if (currentSegment >= pattern.Length)
                currentSegment = 0;

            solid = !solid;
            segmentLength = pattern[currentSegment];
        }

        public bool IsSolidAt(float unitsFromPathStart)
        {
            int patternSegmentIndex;
            float patternSegmentLength;
            return IsSolidAt(unitsFromPathStart, out patternSegmentIndex, out patternSegmentLength);
        }

        public bool IsSolidAt(float unitsFromPathStart, out int patternSegmentIndex, out float patternSegmentLength)
        {
            patternSegmentIndex = 0;
            patternSegmentLength = 0;
            if (pattern == null)
                return true;

            bool isSolid = true;
            unitsFromPathStart += patternOffset;
            int hops = (int)(Mathf.Abs(unitsFromPathStart) / patternLength);
            if (unitsFromPathStart < 0.0f)
            {
                unitsFromPathStart = patternLength - ((-unitsFromPathStart) % patternLength);
                if ((pattern.Length & 1) == 1)
                    isSolid = (hops & 1) == 0;
            }
            else
            {
                unitsFromPathStart = unitsFromPathStart % patternLength;
                if ((pattern.Length & 1) == 1)
                    isSolid = (hops & 1) == 1;
            }

            while (unitsFromPathStart > pattern[patternSegmentIndex])
            {
                unitsFromPathStart -= pattern[patternSegmentIndex++];
                isSolid = !isSolid;
            }
            patternSegmentLength = pattern[patternSegmentIndex] - unitsFromPathStart;
            return isSolid;
        }

        public float SegmentLength { get { return segmentLength; } }
        public bool IsSolid { get { return solid; } }

        float[] pattern;

        int currentSegment;
        bool solid = true;
        float segmentLength;
        float patternLength;
        float patternOffset;
    }
}
