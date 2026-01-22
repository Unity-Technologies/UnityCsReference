// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.VectorGraphics
{
    public static partial class VectorUtils
    {
        /// <summary>
        /// Structure to store the tessellation options.
        /// </summary>
        public struct TessellationOptions
        {
            private float m_MaxCordDev, m_MaxCordDevSq, m_MaxTanAngleDev, m_MaxTanAngleDevCosine, m_StepSize;

            /// <summary>
            /// The uniform tessellation step distance.
            /// </summary>
            public float StepDistance { get; set; } // A split to happen uniformly at fixed distances

            /// <summary>
            /// The maximum distance on the cord to a straight line between to points after which more tessellation will be generated.
            /// To disable, specify float.MaxValue.
            /// </summary>
            public float MaxCordDeviation  // Maximum distance allowed between a cord and its line projection
            {
                get { return m_MaxCordDev; }
                set
                {
                    m_MaxCordDev = Mathf.Max(value, 0.0f);
                    m_MaxCordDevSq = (m_MaxCordDev == float.MaxValue) ? float.MaxValue : m_MaxCordDev * m_MaxCordDev;
                }
            }
            internal float MaxCordDeviationSquared { get { return m_MaxCordDevSq; } }

            /// <summary>
            /// The maximum angle (in degrees) between the curve tangent and the next point after which more tessellation will be generated.
            /// To disable, specify float.MaxValue.
            /// </summary>
            public float MaxTanAngleDeviation // The maximum angle allowed (in radians) between tangents before a split happens
            {
                get { return m_MaxTanAngleDev; }
                set
                {
                    m_MaxTanAngleDev = Mathf.Clamp(value, VectorUtils.Epsilon, Mathf.PI * 0.5f);
                    m_MaxTanAngleDevCosine = Mathf.Cos(m_MaxTanAngleDev);
                }
            }
            internal float MaxTanAngleDeviationCosine { get { return m_MaxTanAngleDevCosine; } } // Cosine of the maximum angle allowed between tangents before a split happens

            /// <summary>
            /// The number of samples used internally to evaluate the curves. More samples = higher quality.
            /// Should be between 0 and 1 (inclusive).
            /// </summary>
            public float SamplingStepSize
            {
                get { return m_StepSize; }
                set { m_StepSize = Mathf.Clamp(value, Epsilon, 1.0f); }
            }
        }

        /// <summary>
        /// Tessellates a path.
        /// </summary>
        /// <param name="contour">The path to tessellate</param>
        /// <param name="pathProps">The path properties</param>
        /// <param name="tessellateOptions">The tessellation options</param>
        /// <param name="vertices">The resulting vertices</param>
        /// <param name="indices">The resulting triangles</param>
        /// <remarks>
        /// The individual line segments generated during tessellation are made out of a set of ordered vertices. It is important
        /// to honor this ordering so joining and and capping connect properly with the existing vertices without generating dupes.
        /// The ordering assumed is as follows:
        /// The last two vertices of a piece must be such that the first is generated at the end with a positive half-thickness
        /// while the second vertex is at the end too but at a negative half-thickness.
        /// No assumptions are enforced for other vertices before the two last vertices.
        /// </remarks>
        public static void TessellatePath(BezierContour contour, PathProperties pathProps, TessellationOptions tessellateOptions, out Vector2[] vertices, out UInt16[] indices)
        {
            if (tessellateOptions.StepDistance < Epsilon)
                throw new Exception("stepDistance too small");

            if (contour.Segments.Length < 2)
            {
                vertices = Array.Empty<Vector2>();
                indices = Array.Empty<ushort>();
                return;
            }

            tessellateOptions.MaxCordDeviation = Mathf.Max(0.0001f, tessellateOptions.MaxCordDeviation);
            tessellateOptions.MaxTanAngleDeviation = Mathf.Max(0.0001f, tessellateOptions.MaxTanAngleDeviation);

            UnityEngine.Profiling.Profiler.BeginSample("TessellatePath");

            float[] segmentLengths = VectorUtils.SegmentsLengths(contour.Segments, contour.Closed);

            // Approximate the number of vertices/indices we need to store the results so we reduce memory reallocations during work
            float approxTotalLength = 0.0f;
            foreach (var s in segmentLengths)
                approxTotalLength += s;

            int approxStepCount = Math.Max((int)(approxTotalLength / tessellateOptions.StepDistance + 0.5f), 2);
            if (pathProps.Stroke.Pattern != null)
                approxStepCount += pathProps.Stroke.Pattern.Length * 2;

            List<Vector2> verts = new List<Vector2>(approxStepCount * 2 + 32); // A little bit possibly for the endings
            List<UInt16> inds = new List<UInt16>((int)(verts.Capacity * 1.5f)); // Usually every 4 verts represent a quad that uses 6 indices

            var patternIt = new PathPatternIterator(pathProps.Stroke.Pattern, pathProps.Stroke.PatternOffset);
            var pathIt = new PathDistanceForwardIterator(contour.Segments, contour.Closed, tessellateOptions.MaxCordDeviationSquared, tessellateOptions.MaxTanAngleDeviationCosine, tessellateOptions.SamplingStepSize);

            JoiningInfo[] joiningInfo = new JoiningInfo[2];
            HandleNewSegmentJoining(pathIt, patternIt, joiningInfo, pathProps.Stroke.HalfThickness, segmentLengths);

            int rangeIndex = 0;
            while (!pathIt.Ended)
            {
                if (patternIt.IsSolid)
                    TessellateRange(patternIt.SegmentLength, pathIt, patternIt, pathProps, tessellateOptions, joiningInfo, segmentLengths, approxTotalLength, rangeIndex++, verts, inds);
                else
                    SkipRange(patternIt.SegmentLength, pathIt, patternIt, pathProps, joiningInfo, segmentLengths);
                patternIt.Advance();
            }

            vertices = verts.ToArray();
            indices = inds.ToArray();

            UnityEngine.Profiling.Profiler.EndSample();
        }

        static Vector2[] TraceShape(BezierContour contour, Stroke stroke, TessellationOptions tessellateOptions)
        {
            if (tessellateOptions.StepDistance < Epsilon)
                throw new Exception("stepDistance too small");

            if (contour.Segments.Length < 2)
                return Array.Empty<Vector2>();

            float[] segmentLengths = VectorUtils.SegmentsLengths(contour.Segments, contour.Closed);

            // Approximate the number of vertices/indices we need to store the results so we reduce memory reallocations during work
            float approxTotalLength = 0.0f;
            foreach (var s in segmentLengths)
                approxTotalLength += s;

            int approxStepCount = Math.Max((int)(approxTotalLength / tessellateOptions.StepDistance + 0.5f), 2);
            var strokePattern = stroke != null ? stroke.Pattern : null;
            var strokePatternOffset = stroke != null ? stroke.PatternOffset : 0.0f;
            if (strokePattern != null)
                approxStepCount += strokePattern.Length * 2;

            List<Vector2> verts = new List<Vector2>(approxStepCount); // A little bit possibly for the endings

            var patternIt = new PathPatternIterator(strokePattern, strokePatternOffset);
            var pathIt = new PathDistanceForwardIterator(contour.Segments, true, tessellateOptions.MaxCordDeviationSquared, tessellateOptions.MaxTanAngleDeviationCosine, tessellateOptions.SamplingStepSize);
            verts.Add(pathIt.EvalCurrent());

            while (!pathIt.Ended)
            {
                float distance = patternIt.SegmentLength;
                float startingLength = pathIt.LengthSoFar;
                float unitsRemaining = Mathf.Min(tessellateOptions.StepDistance, distance);
                bool endedEntirePath = false;
                for (;;)
                {
                    var result = pathIt.AdvanceBy(unitsRemaining, out unitsRemaining);
                    if (result == PathDistanceForwardIterator.Result.Ended)
                    {
                        endedEntirePath = true;
                        break;
                    }
                    else if (result == PathDistanceForwardIterator.Result.NewSegment)
                        verts.Add(pathIt.EvalCurrent());

                    if ((unitsRemaining <= Epsilon) &&
                        !TryGetMoreRemainingUnits(ref unitsRemaining, pathIt, startingLength, distance, tessellateOptions.StepDistance))
                    {
                        break;
                    }

                    if (result == PathDistanceForwardIterator.Result.Stepped)
                        verts.Add(pathIt.EvalCurrent());
                }

                // Ending
                if (endedEntirePath)
                    break;
                else verts.Add(pathIt.EvalCurrent());
                patternIt.Advance();
            }

            if ((verts[0] - verts[verts.Count - 1]).sqrMagnitude < Epsilon)
                verts.RemoveAt(verts.Count - 1);
            return verts.ToArray(); // Why not return verts itself?
        }

        static bool TryGetMoreRemainingUnits(ref float unitsRemaining, PathDistanceForwardIterator pathIt, float startingLength, float distance, float stepDistance)
        {
            float distanceCrossedSoFar = pathIt.LengthSoFar - startingLength;
            float epsilon = Math.Max(Epsilon, distance * Epsilon * 100.0f);
            if ((distance - distanceCrossedSoFar) <= epsilon)
                return false;
            if (distanceCrossedSoFar + stepDistance > distance)
                unitsRemaining = distance - distanceCrossedSoFar;
            else unitsRemaining = stepDistance;
            return true;
        }

        static void HandleNewSegmentJoining(PathDistanceForwardIterator pathIt, PathPatternIterator patternIt, JoiningInfo[] joiningInfo, float halfThickness, float[] segmentLengths)
        {
            joiningInfo[0] = joiningInfo[1];
            joiningInfo[1] = null;

            if (!patternIt.IsSolidAt(pathIt.LengthSoFar + segmentLengths[pathIt.CurrentSegment]))
                return; // The joining center falls outside the pattern, so don't join... period

            if (pathIt.Closed && pathIt.Segments.Count <= 2)
                return; // Not enough segments to do proper closing

            if (pathIt.Closed)
            {
                JoiningInfo closing;
                if ((pathIt.CurrentSegment == 0) || (pathIt.CurrentSegment == pathIt.Segments.Count - 2))
                {
                    closing = ForeseeJoining(
                            VectorUtils.PathSegmentAtIndex(pathIt.Segments, pathIt.Segments.Count - 2),
                            VectorUtils.PathSegmentAtIndex(pathIt.Segments, 0),
                            halfThickness, segmentLengths[pathIt.Segments.Count - 2]);

                    if (pathIt.CurrentSegment == 0)
                        joiningInfo[0] = closing;
                    else
                    {
                        joiningInfo[1] = closing;
                        return;
                    }
                }
                else if (pathIt.CurrentSegment > pathIt.Segments.Count - 2)
                    return;
            }
            else if (pathIt.CurrentSegment >= pathIt.Segments.Count - 2)
                return;

            joiningInfo[1] = ForeseeJoining(
                    VectorUtils.PathSegmentAtIndex(pathIt.Segments, pathIt.CurrentSegment),
                    VectorUtils.PathSegmentAtIndex(pathIt.Segments, pathIt.CurrentSegment + 1),
                    halfThickness, segmentLengths[pathIt.CurrentSegment]);
        }

        static void SkipRange(
            float distance, PathDistanceForwardIterator pathIt, PathPatternIterator patternIt,
            PathProperties pathProps, JoiningInfo[] joiningInfo, float[] segmentLengths)
        {
            float unitsRemaining = distance;
            while (unitsRemaining > Epsilon)
            {
                var result = pathIt.AdvanceBy(unitsRemaining, out unitsRemaining);
                switch (result)
                {
                    case PathDistanceForwardIterator.Result.Ended:
                        return;
                    case PathDistanceForwardIterator.Result.Stepped:
                        if (unitsRemaining < Epsilon)
                            return;
                        break;
                    case PathDistanceForwardIterator.Result.NewSegment:
                        HandleNewSegmentJoining(pathIt, patternIt, joiningInfo, pathProps.Stroke.HalfThickness, segmentLengths);
                        break;
                }
            }
        }

        static void TessellateRange(
            float distance, PathDistanceForwardIterator pathIt, PathPatternIterator patternIt, PathProperties pathProps,
            TessellationOptions tessellateOptions, JoiningInfo[] joiningInfo, float[] segmentLengths, float totalLength, int rangeIndex, List<Vector2> verts, List<UInt16> inds)
        {
            bool startOfLoop = pathIt.Closed && (pathIt.CurrentSegment == 0) && (pathIt.CurrentT == 0.0f);
            if (startOfLoop && (joiningInfo[0] != null))
            {
                GenerateJoining(joiningInfo[0], pathProps.Corners, pathProps.Stroke.HalfThickness, pathProps.Stroke.TippedCornerLimit, tessellateOptions, verts, inds);
            }
            else
            {
                var pathEnding = pathProps.Head;

                // If pattern at the end will overlap with beginning, use a chopped ending to allow merging
                if (pathIt.Closed && rangeIndex == 0 && patternIt.IsSolidAt(pathIt.CurrentT) && patternIt.IsSolidAt(totalLength))
                    pathEnding = PathEnding.Chop;

                GenerateTip(VectorUtils.PathSegmentAtIndex(pathIt.Segments, pathIt.CurrentSegment), true, pathIt.CurrentT, pathEnding, pathProps.Stroke.HalfThickness, tessellateOptions, verts, inds);
            }

            float startingLength = pathIt.LengthSoFar;
            float unitsRemaining = Mathf.Min(tessellateOptions.StepDistance, distance);
            bool endedEntirePath = false;
            for (;;)
            {
                var result = pathIt.AdvanceBy(unitsRemaining, out unitsRemaining);
                if (result == PathDistanceForwardIterator.Result.Ended)
                {
                    endedEntirePath = true;
                    break;
                }
                else if (result == PathDistanceForwardIterator.Result.NewSegment)
                {
                    if (joiningInfo[1] != null)
                        GenerateJoining(joiningInfo[1], pathProps.Corners, pathProps.Stroke.HalfThickness, pathProps.Stroke.TippedCornerLimit, tessellateOptions, verts, inds);
                    else AddSegment(VectorUtils.PathSegmentAtIndex(pathIt.Segments, pathIt.CurrentSegment), pathIt.CurrentT, pathProps.Stroke.HalfThickness, null, pathIt.SegmentLengthSoFar, verts, inds);
                    HandleNewSegmentJoining(pathIt, patternIt, joiningInfo, pathProps.Stroke.HalfThickness, segmentLengths);
                }

                if ((unitsRemaining <= Epsilon) &&
                    !TryGetMoreRemainingUnits(ref unitsRemaining, pathIt, startingLength, distance, tessellateOptions.StepDistance))
                {
                    break;
                }

                if (result == PathDistanceForwardIterator.Result.Stepped)
                    AddSegment(VectorUtils.PathSegmentAtIndex(pathIt.Segments, pathIt.CurrentSegment), pathIt.CurrentT, pathProps.Stroke.HalfThickness, joiningInfo, pathIt.SegmentLengthSoFar, verts, inds);
            }

            // Ending
            if (endedEntirePath && pathIt.Closed)
            {
                // No joining needed, the start and end of the path should just connect
                inds.Add(0);
                inds.Add(1);
                inds.Add((UInt16)(verts.Count - 2));
                inds.Add((UInt16)(verts.Count - 1));
                inds.Add((UInt16)(verts.Count - 2));
                inds.Add(1);
            }
            else
            {
                AddSegment(VectorUtils.PathSegmentAtIndex(pathIt.Segments, pathIt.CurrentSegment), pathIt.CurrentT, pathProps.Stroke.HalfThickness, joiningInfo, pathIt.SegmentLengthSoFar, verts, inds);
                GenerateTip(VectorUtils.PathSegmentAtIndex(pathIt.Segments, pathIt.CurrentSegment), false, pathIt.CurrentT, pathProps.Tail, pathProps.Stroke.HalfThickness, tessellateOptions, verts, inds);
            }
        }

        static void AddSegment(BezierSegment segment, float toT, float halfThickness, JoiningInfo[] joinInfo, float segmentLengthSoFar, List<Vector2> verts, List<UInt16> inds)
        {
            Vector2 tanTo, normTo;
            Vector2 posTo = VectorUtils.EvalFull(segment, toT, out tanTo, out normTo);

            Vector2 posThickness = posTo + normTo * halfThickness;
            Vector2 negThickness = posTo + normTo * -halfThickness;

            if (joinInfo != null)
            {
                if ((joinInfo[0] != null) && (segmentLengthSoFar < joinInfo[0].InnerCornerDistFromStart))
                {
                    if (joinInfo[0].RoundPosThickness)
                        negThickness = joinInfo[0].InnerCornerVertex;
                    else posThickness = joinInfo[0].InnerCornerVertex;
                }

                if ((joinInfo[1] != null) && (segmentLengthSoFar > joinInfo[1].InnerCornerDistToEnd))
                {
                    if (joinInfo[1].RoundPosThickness)
                        negThickness = joinInfo[1].InnerCornerVertex;
                    else posThickness = joinInfo[1].InnerCornerVertex;
                }
            }

            System.Diagnostics.Debug.Assert(verts.Count >= 2);
            int indexStart = verts.Count - 2;
            verts.Add(posThickness);
            verts.Add(negThickness);
            inds.Add((UInt16)(indexStart + 0));
            inds.Add((UInt16)(indexStart + 3));
            inds.Add((UInt16)(indexStart + 1));
            inds.Add((UInt16)(indexStart + 0));
            inds.Add((UInt16)(indexStart + 2));
            inds.Add((UInt16)(indexStart + 3));
        }

        class JoiningInfo
        {
            public Vector2 JoinPos;
            public Vector2 TanAtEnd, TanAtStart;
            public Vector2 NormAtEnd, NormAtStart;
            public Vector2 PosThicknessStart, NegThicknessStart;
            public Vector2 PosThicknessEnd, NegThicknessEnd;
            public Vector2 PosThicknessClosingPoint, NegThicknessClosingPoint;
            public bool RoundPosThickness;
            public bool SimpleJoin;
            public Vector2 InnerCornerVertex;
            public float InnerCornerDistToEnd, InnerCornerDistFromStart;
        }

        static JoiningInfo ForeseeJoining(BezierSegment end, BezierSegment start, float halfThickness, float endSegmentLength)
        {
            JoiningInfo joinInfo = new JoiningInfo();

            // The joining generates the vertices at both ends as well as the joining itself
            joinInfo.JoinPos = end.P3;
            joinInfo.TanAtEnd = VectorUtils.EvalTangent(end, 1.0f);
            joinInfo.NormAtEnd = Vector2.Perpendicular(joinInfo.TanAtEnd);
            joinInfo.TanAtStart = VectorUtils.EvalTangent(start, 0.0f);
            joinInfo.NormAtStart = Vector2.Perpendicular(joinInfo.TanAtStart);

            // If the tangents are continuous at the join location, we don't have
            // to generate a corner, we do a "simple" join by just connecting the vertices
            // from the two segments directly
            float cosAngleBetweenTans = Vector2.Dot(joinInfo.TanAtEnd, joinInfo.TanAtStart);
            joinInfo.SimpleJoin = Mathf.Approximately(Mathf.Abs(cosAngleBetweenTans), 1.0f);
            if (joinInfo.SimpleJoin)
                return null;

            joinInfo.PosThicknessEnd = joinInfo.JoinPos + joinInfo.NormAtEnd * halfThickness;
            joinInfo.NegThicknessEnd = joinInfo.JoinPos - joinInfo.NormAtEnd * halfThickness;
            joinInfo.PosThicknessStart = joinInfo.JoinPos + joinInfo.NormAtStart * halfThickness;
            joinInfo.NegThicknessStart = joinInfo.JoinPos - joinInfo.NormAtStart * halfThickness;

            if (joinInfo.SimpleJoin)
            {
                joinInfo.PosThicknessClosingPoint = Vector2.LerpUnclamped(joinInfo.PosThicknessEnd, joinInfo.PosThicknessStart, 0.5f);
                joinInfo.NegThicknessClosingPoint = Vector2.LerpUnclamped(joinInfo.NegThicknessEnd, joinInfo.NegThicknessStart, 0.5f);
            }
            else
            {
                joinInfo.PosThicknessClosingPoint = VectorUtils.IntersectLines(joinInfo.PosThicknessEnd, joinInfo.PosThicknessEnd + joinInfo.TanAtEnd, joinInfo.PosThicknessStart, joinInfo.PosThicknessStart + joinInfo.TanAtStart);
                joinInfo.NegThicknessClosingPoint = VectorUtils.IntersectLines(joinInfo.NegThicknessEnd, joinInfo.NegThicknessEnd + joinInfo.TanAtEnd, joinInfo.NegThicknessStart, joinInfo.NegThicknessStart + joinInfo.TanAtStart);

                if (float.IsInfinity(joinInfo.PosThicknessClosingPoint.x) || float.IsInfinity(joinInfo.PosThicknessClosingPoint.y))
                    joinInfo.PosThicknessClosingPoint = joinInfo.JoinPos;
                if (float.IsInfinity(joinInfo.NegThicknessClosingPoint.x) || float.IsInfinity(joinInfo.NegThicknessClosingPoint.y))
                    joinInfo.NegThicknessClosingPoint = joinInfo.JoinPos;
            }

            // Should we round the positive thickness side or the negative thickness side?
            joinInfo.RoundPosThickness = PointOnTheLeftOfLine(Vector2.zero, joinInfo.TanAtEnd, joinInfo.TanAtStart);

            // Inner corner vertex should be calculated by intersection of the inner segments
            Vector2[] startTrail = null, endTrail = null;
            Vector2 intersectionOnStart = Vector2.zero, intersectionOnEnd = Vector2.zero;
            if (!joinInfo.SimpleJoin)
            {
                BezierSegment endFlipped = VectorUtils.FlipSegment(end);
                Vector2 thicknessClosingPoint = joinInfo.RoundPosThickness ? joinInfo.PosThicknessClosingPoint : joinInfo.NegThicknessClosingPoint;
                Vector2 meetingPoint = end.P3;
                Vector2 thicknessDiagonalEnd = meetingPoint + (thicknessClosingPoint - meetingPoint) * 10.0f;
                startTrail = LineBezierThicknessIntersect(
                        start, joinInfo.RoundPosThickness ? -halfThickness : halfThickness, meetingPoint, thicknessDiagonalEnd,
                        out joinInfo.InnerCornerDistFromStart, out intersectionOnStart);
                endTrail = LineBezierThicknessIntersect(
                        endFlipped, joinInfo.RoundPosThickness ? halfThickness : -halfThickness, meetingPoint, thicknessDiagonalEnd,
                        out joinInfo.InnerCornerDistToEnd, out intersectionOnEnd);
            }

            bool intersectionFound = false;
            if ((startTrail != null) && (endTrail != null))
            {
                var intersect = VectorUtils.IntersectLines(startTrail[0], startTrail[1], endTrail[0], endTrail[1]);
                var isOnStartTrail = PointOnLineIsWithinSegment(startTrail[0], startTrail[1], intersect);
                var isOnEndTrail = PointOnLineIsWithinSegment(endTrail[0], endTrail[1], intersect);
                if (!float.IsInfinity(intersect.x) && isOnStartTrail && isOnEndTrail)
                {
                    var vStart = intersectionOnStart - intersect;
                    var vEnd = intersectionOnEnd - intersect;
                    joinInfo.InnerCornerDistFromStart += (vStart == Vector2.zero) ? 0.0f : vStart.magnitude;
                    joinInfo.InnerCornerDistToEnd += (vEnd == Vector2.zero) ? 0.0f : vEnd.magnitude;
                    joinInfo.InnerCornerDistToEnd = endSegmentLength - joinInfo.InnerCornerDistToEnd;
                    joinInfo.InnerCornerVertex = intersect; // Found it!
                    intersectionFound = true;
                }
            }

            if (!intersectionFound)
            {
                joinInfo.InnerCornerVertex = joinInfo.JoinPos + ((joinInfo.TanAtStart - joinInfo.TanAtEnd) / 2.0f).normalized * halfThickness;
                joinInfo.InnerCornerDistFromStart = 0;
                joinInfo.InnerCornerDistToEnd = endSegmentLength;
            }
            return joinInfo;
        }

        static Vector2[] LineBezierThicknessIntersect(BezierSegment seg, float thickness, Vector2 lineFrom, Vector2 lineTo, out float distanceToIntersection, out Vector2 intersection)
        {
            Vector2 tan = VectorUtils.EvalTangent(seg, 0.0f);
            Vector2 nrm = Vector2.Perpendicular(tan);
            Vector2 lastPoint = seg.P0 + nrm * thickness;
            distanceToIntersection = 0.0f;
            intersection = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            float stepT = 0.01f;
            float t = 0;
            while (t < 1.0f)
            {
                t += stepT;
                var point = VectorUtils.EvalFull(seg, t, out tan, out nrm) + nrm * thickness;
                intersection = VectorUtils.IntersectLines(lineFrom, lineTo, lastPoint, point);
                if (PointOnLineIsWithinSegment(lastPoint, point, intersection))
                {
                    distanceToIntersection += (lastPoint - intersection).magnitude;
                    return new Vector2[] { lastPoint, point };
                }
                distanceToIntersection += (lastPoint - point).magnitude;
                lastPoint = point;
            }
            return null;
        }

        static bool PointOnLineIsWithinSegment(Vector2 lineFrom, Vector2 lineTo, Vector2 point)
        {
            // Point is assumed to be already on the line, but we would like to know if it is within the segment specified
            var v = (lineTo - lineFrom).normalized;
            if (Vector2.Dot(point - lineFrom,  v) < -Epsilon)
                return false;
            if (Vector2.Dot(point - lineTo, v) > Epsilon)
                return false;
            return true;
        }

        static void GenerateJoining(JoiningInfo joinInfo, PathCorner corner, float halfThickness, float tippedCornerLimit, TessellationOptions tessellateOptions, List<Vector2> verts, List<UInt16> inds)
        {
            // The joining generates the vertices at both ends as well as the joining itself
            if (verts.Count == 0)
            {
                // Starting a path with a joining (meaning a loop)
                verts.Add(joinInfo.RoundPosThickness ? joinInfo.PosThicknessEnd : joinInfo.InnerCornerVertex);
                verts.Add(joinInfo.RoundPosThickness ? joinInfo.InnerCornerVertex : joinInfo.NegThicknessEnd);
            }

            System.Diagnostics.Debug.Assert(verts.Count >= 2);
            int indexStart = verts.Count - 2; // Using the last two vertices

            // Convert a tipped corner to a beveled one if tippedCornerLimit ratio is reached
            if (corner == PathCorner.Tipped && tippedCornerLimit >= 1.0f)
            {
                var theta = Vector2.Angle(-joinInfo.TanAtEnd, joinInfo.TanAtStart) * Mathf.Deg2Rad;
                var ratio = 1.0f / Mathf.Sin(theta / 2.0f);
                if (ratio > tippedCornerLimit)
                    corner = PathCorner.Beveled;
            }

            if (joinInfo.SimpleJoin)
            {
                // TODO
            }
            else if (corner == PathCorner.Tipped)
            {
                verts.Add(joinInfo.PosThicknessClosingPoint);
                verts.Add(joinInfo.NegThicknessClosingPoint);
                verts.Add(joinInfo.RoundPosThickness ? joinInfo.PosThicknessStart : joinInfo.InnerCornerVertex);
                verts.Add(joinInfo.RoundPosThickness ? joinInfo.InnerCornerVertex : joinInfo.NegThicknessStart);

                // Ending to tip
                inds.Add((UInt16)(indexStart + 0));
                inds.Add((UInt16)(indexStart + 3));
                inds.Add((UInt16)(indexStart + 1));
                inds.Add((UInt16)(indexStart + 0));
                inds.Add((UInt16)(indexStart + 2));
                inds.Add((UInt16)(indexStart + 3));

                // Tip to starting
                inds.Add((UInt16)(indexStart + 4));
                inds.Add((UInt16)(indexStart + 3));
                inds.Add((UInt16)(indexStart + 2));
                inds.Add((UInt16)(indexStart + 4));
                inds.Add((UInt16)(indexStart + 5));
                inds.Add((UInt16)(indexStart + 3));

                return;
            }
            else if (corner == PathCorner.Beveled)
            {
                verts.Add(joinInfo.RoundPosThickness ? joinInfo.PosThicknessEnd : joinInfo.InnerCornerVertex); // 2
                verts.Add(joinInfo.RoundPosThickness ? joinInfo.InnerCornerVertex : joinInfo.NegThicknessEnd); // 3
                verts.Add(joinInfo.RoundPosThickness ? joinInfo.PosThicknessStart : joinInfo.InnerCornerVertex); // 4
                verts.Add(joinInfo.RoundPosThickness ? joinInfo.InnerCornerVertex : joinInfo.NegThicknessStart); // 5

                // Ending to tip
                inds.Add((UInt16)(indexStart + 0));
                inds.Add((UInt16)(indexStart + 2));
                inds.Add((UInt16)(indexStart + 1));
                inds.Add((UInt16)(indexStart + 1));
                inds.Add((UInt16)(indexStart + 2));
                inds.Add((UInt16)(indexStart + 3));

                // Bevel
                if (joinInfo.RoundPosThickness)
                {
                    inds.Add((UInt16)(indexStart + 2));
                    inds.Add((UInt16)(indexStart + 4));
                    inds.Add((UInt16)(indexStart + 3));
                }
                else
                {
                    inds.Add((UInt16)(indexStart + 3));
                    inds.Add((UInt16)(indexStart + 2));
                    inds.Add((UInt16)(indexStart + 5));
                }

                return;
            }

            if (corner == PathCorner.Round)
            {
                float sweepAngle = Mathf.Acos(Vector2.Dot(joinInfo.NormAtEnd, joinInfo.NormAtStart));
                bool flipArc = false;
                if (!PointOnTheLeftOfLine(Vector2.zero, joinInfo.NormAtEnd, joinInfo.NormAtStart))
                {
                    sweepAngle = -sweepAngle;
                    flipArc = true;
                }

                UInt16 innerCornerVertexIndex = (UInt16)verts.Count;
                verts.Add(joinInfo.InnerCornerVertex);

                int arcSegments = CalculateArcSteps(halfThickness, 0, sweepAngle, tessellateOptions);
                for (int i = 0; i <= arcSegments; i++)
                {
                    float angle = sweepAngle * (i / (float)arcSegments);
                    Vector2 nrm = Matrix2D.RotateLH(angle) * joinInfo.NormAtEnd;
                    if (flipArc) nrm = -nrm;
                    verts.Add(nrm * halfThickness + joinInfo.JoinPos);

                    if (i == 0)
                    {
                        inds.Add((UInt16)(indexStart + 0));
                        inds.Add((UInt16)(indexStart + 3));
                        inds.Add((UInt16)(indexStart + (joinInfo.RoundPosThickness ? 2 : 1)));

                        inds.Add((UInt16)(indexStart + 0));
                        inds.Add((UInt16)(indexStart + 2));
                        inds.Add((UInt16)(indexStart + (joinInfo.RoundPosThickness ? 1 : 3)));
                    }
                    else
                    {
                        if (joinInfo.RoundPosThickness)
                        {
                            inds.Add((UInt16)(indexStart + i + (flipArc ? 3 : 2)));
                            inds.Add((UInt16)(indexStart + i + (flipArc ? 2 : 3)));
                            inds.Add(innerCornerVertexIndex);
                        }
                        else
                        {
                            inds.Add((UInt16)(indexStart + i + (flipArc ? 3 : 2)));
                            inds.Add((UInt16)(indexStart + i + (flipArc ? 2 : 3)));
                            inds.Add(innerCornerVertexIndex);
                        }
                    }
                }

                // Manually add the last segment, maintain the expected vertex positioning
                int endingVerticesIndex = verts.Count;
                if (joinInfo.RoundPosThickness)
                {
                    verts.Add(joinInfo.PosThicknessStart);
                    verts.Add(joinInfo.InnerCornerVertex);
                }
                else
                {
                    verts.Add(joinInfo.InnerCornerVertex);
                    verts.Add(joinInfo.NegThicknessStart);
                }
                inds.Add((UInt16)(endingVerticesIndex - 1));
                inds.Add((UInt16)(endingVerticesIndex + 0));
                inds.Add(innerCornerVertexIndex);
            }
        }

        static void GenerateTip(BezierSegment segment, bool atStart, float t, PathEnding ending, float halfThickness, TessellationOptions tessellateOptions, List<Vector2> verts, List<UInt16> inds)
        {
            // The tip includes the vertices at the end itself
            Vector2 tan, nrm;
            var pos = VectorUtils.EvalFull(segment, t, out tan, out nrm);
            int indexStart = verts.Count;

            switch (ending)
            {
                case PathEnding.Chop:
                    if (atStart)
                    {
                        verts.Add(pos + nrm * halfThickness);
                        verts.Add(pos - nrm * halfThickness);
                    }
                    else
                    {
                        // Not much, path segments are always expected to be generated perpendicular to the path
                        // at the segment point location, so we don't have to do anything for the ending
                    }
                    break;

                case PathEnding.Square:
                    if (atStart)
                    {
                        verts.Add(pos + nrm * halfThickness - tan * halfThickness);
                        verts.Add(pos - nrm * halfThickness - tan * halfThickness);
                        verts.Add(pos + nrm * halfThickness);
                        verts.Add(pos - nrm * halfThickness);

                        inds.Add((UInt16)(indexStart + 0));
                        inds.Add((UInt16)(indexStart + 3));
                        inds.Add((UInt16)(indexStart + 1));
                        inds.Add((UInt16)(indexStart + 0));
                        inds.Add((UInt16)(indexStart + 2));
                        inds.Add((UInt16)(indexStart + 3));
                    }
                    else
                    {
                        // Relying on the last two vertices, and just adding two of our own here
                        verts.Add(pos + nrm * halfThickness + tan * halfThickness);
                        verts.Add(pos - nrm * halfThickness + tan * halfThickness);

                        inds.Add((UInt16)(indexStart + 0 - 2));
                        inds.Add((UInt16)(indexStart + 3 - 2));
                        inds.Add((UInt16)(indexStart + 1 - 2));
                        inds.Add((UInt16)(indexStart + 0 - 2));
                        inds.Add((UInt16)(indexStart + 2 - 2));
                        inds.Add((UInt16)(indexStart + 3 - 2));
                    }
                    break;

                case PathEnding.Round:
                    float arcSign = atStart ? -1 : 1;
                    int arcSegments = CalculateArcSteps(halfThickness, 0, Mathf.PI, tessellateOptions);
                    for (int i = 1; i < arcSegments; i++)
                    {
                        float angle = Mathf.PI * (i / (float)arcSegments);
                        verts.Add(pos + Matrix2D.RotateLH(angle) * nrm * halfThickness * arcSign);
                    }

                    if (atStart)
                    {
                        // Note how we maintain the last two vertices being setup for connection by the rest of the path vertices
                        int indexTipStart = verts.Count;
                        verts.Add(pos + nrm * halfThickness);
                        verts.Add(pos - nrm * halfThickness);

                        for (int i = 1; i < arcSegments; i++)
                        {
                            inds.Add((UInt16)(indexTipStart + 1));
                            inds.Add((UInt16)(indexStart + i - 1));
                            inds.Add((UInt16)(indexStart + i));
                        }
                    }
                    else
                    {
                        inds.Add((UInt16)(indexStart - 1));
                        inds.Add((UInt16)(indexStart - 2));
                        inds.Add((UInt16)(indexStart + 0));
                        for (int i = 1; i < arcSegments - 1; i++)
                        {
                            inds.Add((UInt16)(indexStart - 1));
                            inds.Add((UInt16)(indexStart + i - 1));
                            inds.Add((UInt16)(indexStart + i));
                        }
                    }
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false); // Joining has its own function
                    break;
            }
        }

        static int CalculateArcSteps(float radius, float fromAngle, float toAngle, TessellationOptions tessellateOptions)
        {
            float stepDivisor = float.MaxValue;

            if (tessellateOptions.StepDistance != float.MaxValue)
                stepDivisor = tessellateOptions.StepDistance / radius;

            if (tessellateOptions.MaxCordDeviation != float.MaxValue)
            {
                float y = radius - tessellateOptions.MaxCordDeviation;
                float cordHalfLength = Mathf.Sqrt(radius * radius - y * y);
                float div = Mathf.Min(stepDivisor, Mathf.Asin(cordHalfLength / radius));
                if (div > VectorUtils.Epsilon)
                    stepDivisor = div;
            }

            if (tessellateOptions.MaxTanAngleDeviation < Mathf.PI * 0.5f)
                stepDivisor = Mathf.Min(stepDivisor, tessellateOptions.MaxTanAngleDeviation * 2.0f);

            float stepsInFullCircle = (Mathf.PI * 2.0f) / stepDivisor;
            float arcPercentage = Mathf.Abs(fromAngle - toAngle) / (Mathf.PI * 2.0f);
            return (int)Mathf.Max(stepsInFullCircle * arcPercentage + 0.5f, 3); // Never less than 3 segments
        }

        /// <summary>Tessellates a rectangle.</summary>
        /// <param name="rect">Rectangle to tessellate</param>
        /// <param name="vertices">The output vertices</param>
        /// <param name="indices">The output triangles</param>
        public static void TessellateRect(Rect rect, out Vector2[] vertices, out UInt16[] indices)
        {
            vertices = new Vector2[] {
                new Vector2(rect.xMin, rect.yMin),
                new Vector2(rect.xMax, rect.yMin),
                new Vector2(rect.xMax, rect.yMax),
                new Vector2(rect.xMin, rect.yMax)
            };
            indices = new UInt16[] {
                1, 0, 2, 2, 0, 3
            };
        }

        /// <summary>Tessellates a rectangle border.</summary>
        /// <param name="rect">Rectangle to tessellate</param>
        /// <param name="halfThickness">The half-thickness of the border</param>
        /// <param name="vertices">The output vertices</param>
        /// <param name="indices">The output triangles</param>
        public static void TessellateRectBorder(Rect rect, float halfThickness, out Vector2[] vertices, out UInt16[] indices)
        {
            var verts = new List<Vector2>(16);
            var inds = new List<UInt16>(24);

            // Left edge
            var p0 = new Vector2(rect.x, rect.y + rect.height);
            var p1 = new Vector2(rect.x, rect.y);

            var q0 = p0 + new Vector2(-halfThickness, halfThickness);
            var q1 = p1 + new Vector2(-halfThickness, -halfThickness);
            var q2 = p1 + new Vector2(halfThickness, halfThickness);
            var q3 = p0 + new Vector2(halfThickness, -halfThickness);

            verts.Add(q0); verts.Add(q1); verts.Add(q2); verts.Add(q3);
            inds.Add(0); inds.Add(3); inds.Add(2); inds.Add(2); inds.Add(1); inds.Add(0);

            // Top edge
            p0 = new Vector2(rect.x, rect.y);
            p1 = new Vector2(rect.x + rect.width, rect.y);

            q0 = p0 + new Vector2(-halfThickness, -halfThickness);
            q1 = p1 + new Vector2(halfThickness, -halfThickness);
            q2 = p1 + new Vector2(-halfThickness, halfThickness);
            q3 = p0 + new Vector2(halfThickness, halfThickness);

            verts.Add(q0); verts.Add(q1); verts.Add(q2); verts.Add(q3);
            inds.Add(4); inds.Add(7); inds.Add(6); inds.Add(6); inds.Add(5); inds.Add(4);

            // Right edge
            p0 = new Vector2(rect.x + rect.width, rect.y);
            p1 = new Vector2(rect.x + rect.width, rect.y + rect.height);

            q0 = p0 + new Vector2(halfThickness, -halfThickness);
            q1 = p1 + new Vector2(halfThickness, halfThickness);
            q2 = p1 + new Vector2(-halfThickness, -halfThickness);
            q3 = p0 + new Vector2(-halfThickness, halfThickness);

            verts.Add(q0); verts.Add(q1); verts.Add(q2); verts.Add(q3);
            inds.Add(8); inds.Add(11); inds.Add(10); inds.Add(10); inds.Add(9); inds.Add(8);

            // Bottom edge
            p0 = new Vector2(rect.x + rect.width, rect.y + rect.height);
            p1 = new Vector2(rect.x, rect.y + rect.height);

            q0 = p0 + new Vector2(halfThickness, halfThickness);
            q1 = p1 + new Vector2(-halfThickness, halfThickness);
            q2 = p1 + new Vector2(halfThickness, -halfThickness);
            q3 = p0 + new Vector2(-halfThickness, -halfThickness);

            verts.Add(q0); verts.Add(q1); verts.Add(q2); verts.Add(q3);
            inds.Add(12); inds.Add(15); inds.Add(14); inds.Add(14); inds.Add(13); inds.Add(12);

            vertices = verts.ToArray();
            indices = inds.ToArray();
        }
    }
}
