// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    internal static class Tessellation
    {
        internal static float kEpsilon = 0.001f;
        internal static UInt16 kSubdivisions = 6;

        static ProfilerMarker s_MarkerTessellateRect = new ProfilerMarker("TessellateRect");
        static ProfilerMarker s_MarkerTessellateBorder = new ProfilerMarker("TessellateBorder");

        /// <summary>
        /// Tessellates the border OR the content:
        /// If any of the left/right/top/bottom border parameters is greater than Epsilon, we tessellate ONLY a border.
        /// Otherwise we tessellate ONLY the content.
        /// </summary>
        public static void TessellateRect(MeshGenerationContextUtils.RectangleParams rectParams, float posZ, MeshBuilder.AllocMeshData meshAlloc, bool computeUVs)
        {
            if (rectParams.rect.width < kEpsilon || rectParams.rect.height < kEpsilon)
                return;

            s_MarkerTessellateRect.Begin();

            var halfSize = new Vector2(rectParams.rect.width * 0.5f, rectParams.rect.height * 0.5f);
            rectParams.topLeftRadius = Vector2.Min(rectParams.topLeftRadius, halfSize);
            rectParams.topRightRadius = Vector2.Min(rectParams.topRightRadius, halfSize);
            rectParams.bottomRightRadius = Vector2.Min(rectParams.bottomRightRadius, halfSize);
            rectParams.bottomLeftRadius = Vector2.Min(rectParams.bottomLeftRadius, halfSize);

            // Count the required triangles by calling the tessellation method with a "countOnly=true" flag, which skips
            // tessellation and only update the vertex and index counts.
            UInt16 vertexCount = 0, indexCount = 0;
            TessellateRectInternal(ref rectParams, 0, null, ref vertexCount, ref indexCount, true);

            var mesh = meshAlloc.Allocate(vertexCount, indexCount);

            vertexCount = 0;
            indexCount = 0;
            TessellateRectInternal(ref rectParams, posZ, mesh, ref vertexCount, ref indexCount);
            if (computeUVs)
                ComputeUVs(rectParams.rect, rectParams.uv, mesh.uvRegion, mesh.m_Vertices);
            Debug.Assert(vertexCount == mesh.vertexCount);
            Debug.Assert(indexCount == mesh.indexCount);

            s_MarkerTessellateRect.End();
        }

        /// <summary>
        /// Specialized, faster method than <see cref="TessellateRect"> for a rectangular quad when
        /// the rectangle does not have borders or rounded corners.
        /// </summary>
        public static void TessellateQuad(MeshGenerationContextUtils.RectangleParams rectParams, float posZ, MeshBuilder.AllocMeshData meshAlloc)
        {
            if (rectParams.rect.width < kEpsilon || rectParams.rect.height < kEpsilon)
                return;

            s_MarkerTessellateRect.Begin();

            // Count the required triangles by calling the tessellation method with a "countOnly=true" flag, which skips
            // tessellation and only update the vertex and index counts.
            UInt16 vertexCount = 0, indexCount = 0;
            TessellateQuad(rectParams.rect, Edges.All, rectParams.color, posZ, null, ref vertexCount, ref indexCount, true);

            var mesh = meshAlloc.Allocate(vertexCount, indexCount);

            vertexCount = 0;
            indexCount = 0;
            TessellateQuad(rectParams.rect, Edges.All, rectParams.color, posZ, mesh, ref vertexCount, ref indexCount, false);
            Debug.Assert(vertexCount == mesh.vertexCount);
            Debug.Assert(indexCount == mesh.indexCount);

            s_MarkerTessellateRect.End();
        }

        public static void TessellateBorder(MeshGenerationContextUtils.BorderParams borderParams, float posZ, MeshBuilder.AllocMeshData meshAlloc)
        {
            if (borderParams.rect.width < kEpsilon || borderParams.rect.height < kEpsilon)
                return;

            s_MarkerTessellateBorder.Begin();

            var halfSize = new Vector2(borderParams.rect.width * 0.5f, borderParams.rect.height * 0.5f);
            borderParams.topLeftRadius = Vector2.Min(borderParams.topLeftRadius, halfSize);
            borderParams.topRightRadius = Vector2.Min(borderParams.topRightRadius, halfSize);
            borderParams.bottomRightRadius = Vector2.Min(borderParams.bottomRightRadius, halfSize);
            borderParams.bottomLeftRadius = Vector2.Min(borderParams.bottomLeftRadius, halfSize);

            borderParams.leftWidth = Mathf.Min(borderParams.leftWidth, halfSize.x);
            borderParams.topWidth = Mathf.Min(borderParams.topWidth, halfSize.y);
            borderParams.rightWidth = Mathf.Min(borderParams.rightWidth, halfSize.x);
            borderParams.bottomWidth = Mathf.Min(borderParams.bottomWidth, halfSize.y);

            UInt16 vertexCount = 0, indexCount = 0;
            CountBorderTriangles(ref borderParams, ref vertexCount, ref indexCount);

            var mesh = meshAlloc.Allocate(vertexCount, indexCount);

            vertexCount = 0;
            indexCount = 0;
            TessellateBorderInternal(ref borderParams, posZ, mesh, ref vertexCount, ref indexCount);
            Debug.Assert(vertexCount == mesh.vertexCount);
            Debug.Assert(indexCount == mesh.indexCount);
            s_MarkerTessellateBorder.End();
        }

        private static void CountBorderTriangles(ref MeshGenerationContextUtils.BorderParams border, ref UInt16 vertexCount, ref UInt16 indexCount)
        {
            // To count the required triangles, we call the tessellation method with a "countOnly=true" flag, which skips
            // tessellation and only update the vertex and index counts.
            TessellateBorderInternal(ref border, 0, null, ref vertexCount, ref indexCount, true);
        }

        private static void TessellateRectInternal(ref MeshGenerationContextUtils.RectangleParams rectParams, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly = false)
        {
            TessellateRoundedCorners(ref rectParams, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
        }

        private static void TessellateBorderInternal(ref MeshGenerationContextUtils.BorderParams border, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly = false)
        {
            TessellateRoundedBorders(ref border, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
        }

        private static void TessellateRoundedCorners(ref MeshGenerationContextUtils.RectangleParams rectParams, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            // To tessellate the 4 corners, the rect is divided in 4 quadrants and every quadrant is computed as if they were the top-left corner.
            // The quadrants are then mirrored and the triangles winding flipped as necessary.
            UInt16 startVc = 0, startIc = 0;
            var halfSize = new Vector2(rectParams.rect.width * 0.5f, rectParams.rect.height * 0.5f);
            var quarterRect = new Rect(rectParams.rect.x, rectParams.rect.y, halfSize.x, halfSize.y);

            // Top-left
            TessellateRoundedCorner(quarterRect, rectParams.color, posZ, rectParams.topLeftRadius, mesh, ref vertexCount, ref indexCount, countOnly);

            // Top-right
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedCorner(quarterRect, rectParams.color, posZ, rectParams.topRightRadius, mesh, ref vertexCount, ref indexCount, countOnly);
            if (!countOnly)
            {
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, true);
                FlipWinding(mesh.m_Indices, startIc, indexCount - startIc);
            }

            // Bottom-right
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedCorner(quarterRect, rectParams.color, posZ, rectParams.bottomRightRadius, mesh, ref vertexCount, ref indexCount, countOnly);
            if (!countOnly)
            {
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, true);
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, false);
            }

            // Bottom-left
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedCorner(quarterRect, rectParams.color, posZ, rectParams.bottomLeftRadius, mesh, ref vertexCount, ref indexCount, countOnly);
            if (!countOnly)
            {
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, false);
                FlipWinding(mesh.m_Indices, startIc, indexCount - startIc);
            }
        }

        private static void TessellateRoundedBorders(ref MeshGenerationContextUtils.BorderParams border, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            // To tessellate the 4 corners, the rect is divided in 4 quadrants and every quadrant is computed as if they were the top-left corner.
            // The quadrants are then mirrored and the triangles winding flipped as necessary.
            UInt16 startVc = 0, startIc = 0;
            var halfSize = new Vector2(border.rect.width * 0.5f, border.rect.height * 0.5f);
            var quarterRect = new Rect(border.rect.x, border.rect.y, halfSize.x, halfSize.y);

            Color32 leftColor = border.leftColor;
            Color32 topColor = border.topColor;
            Color32 bottomColor = border.bottomColor;
            Color32 rightColor = border.rightColor;

            // Top-left
            TessellateRoundedBorder(quarterRect, leftColor, topColor, posZ, border.topLeftRadius, border.leftWidth, border.topWidth, mesh, ref vertexCount, ref indexCount, countOnly);

            // Top-right
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedBorder(quarterRect, rightColor, topColor, posZ, border.topRightRadius, border.rightWidth, border.topWidth, mesh, ref vertexCount, ref indexCount, countOnly);
            if (!countOnly)
            {
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, true);
                FlipWinding(mesh.m_Indices, startIc, indexCount - startIc);
            }

            // Bottom-right
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedBorder(quarterRect, rightColor, bottomColor, posZ, border.bottomRightRadius, border.rightWidth, border.bottomWidth, mesh, ref vertexCount, ref indexCount, countOnly);
            if (!countOnly)
            {
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, true);
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, false);
            }

            // Bottom-left
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedBorder(quarterRect, leftColor, bottomColor, posZ, border.bottomLeftRadius, border.leftWidth, border.bottomWidth, mesh, ref vertexCount, ref indexCount, countOnly);
            if (!countOnly)
            {
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, false);
                FlipWinding(mesh.m_Indices, startIc, indexCount - startIc);
            }
        }

        private static void TessellateRoundedCorner(Rect rect, Color32 color, float posZ, Vector2 radius, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            var cornerCenter = rect.position + radius;
            var subRect = Rect.zero;

            if (radius == Vector2.zero)
            {
                // Without radius, we use a single quad to fill the section
                // -------
                // |     |
                // |     |
                // -------
                TessellateQuad(rect, Edges.Left | Edges.Top, color, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
                return;
            }

            // Just fill the corner with radius, plus up to 2 quads to fill the remainder of the section
            //   _-------
            //  *\ |    |
            // *__\| A  |
            // |   |    |
            // | B |    |
            // |   |    |
            // ----------
            TessellateFilledFan(cornerCenter, radius, Vector2.zero, 0, 0, color, color, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            if (radius.x < rect.width)
            {
                // A
                subRect = new Rect(rect.x + radius.x, rect.y, rect.width - radius.x, rect.height);
                TessellateQuad(subRect, Edges.Top, color, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            }
            if (radius.y < rect.height)
            {
                // B
                subRect = new Rect(rect.x, rect.y + radius.y, radius.x < rect.width ? radius.x : rect.width, rect.height - radius.y);
                TessellateQuad(subRect, Edges.Left, color, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            }
        }

        private static void TessellateRoundedBorder(Rect rect, Color32 leftColor, Color32 topColor, float posZ, Vector2 radius, float leftWidth, float topWidth, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if (leftWidth < kEpsilon && topWidth < kEpsilon)
                return;

            leftWidth = Mathf.Max(0, leftWidth);
            topWidth = Mathf.Max(0, topWidth);
            radius.x = Mathf.Clamp(radius.x, 0, rect.width);
            radius.y = Mathf.Clamp(radius.y, 0, rect.height);

            var cornerCenter = rect.position + radius;
            var subRect = Rect.zero;

            if (radius.x < kEpsilon || radius.y < kEpsilon)
            {
                // Without radius, we use two quads for the outlines
                // ------------
                // | \    B   |
                // | A \-------
                // |   |
                // |   |
                // -----
                if (leftWidth > kEpsilon)
                {
                    // A
                    subRect = new Rect(rect.x, rect.y, leftWidth, rect.height);
                    TessellateStraightBorder(subRect, Edges.Left, topWidth, leftColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
                }
                if (topWidth > kEpsilon)
                {
                    // B
                    subRect = new Rect(rect.x, rect.y, rect.width, topWidth);
                    TessellateStraightBorder(subRect, Edges.Top, leftWidth, topColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
                }
                return;
            }

            if (LooseCompare(radius.x, leftWidth) == 0 && LooseCompare(radius.y, topWidth) == 0)
            {
                // When the corner radii match the widths, use a filled fan.
                //      ________
                //   -*  |     |
                //  * A\ |  F  |
                // *____\|_____|
                // |     |
                // |     |
                // |  E  |
                // |     |
                // |_____|

                // A
                TessellateFilledFan(cornerCenter, radius, Vector2.zero, leftWidth, topWidth, leftColor, topColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            }
            else if (LooseCompare(radius.x, leftWidth) > 0 && LooseCompare(radius.y, topWidth) > 0)
            {
                // When the radii are both larger than the widths, use a carved fan.
                //      _________
                //   -*  |   F   |
                //  * A\_|_______|
                // *__/
                // |  |
                // |  |
                // |E |
                // |  |
                // |__|

                // A
                TessellateBorderedFan(cornerCenter, radius, leftWidth, topWidth, leftColor, topColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            }
            else
            {
                // For all other cases (only one is illustrated below)
                //      __ _____
                //   -*   |     |
                //  *     |     |
                // *   A  |  F  |
                // |      |     |
                // |______|_____|
                // | |
                // |E|
                // | |
                // |_|
                subRect = new Rect(rect.x, rect.y, Mathf.Max(radius.x, leftWidth), Mathf.Max(radius.y, topWidth));
                TessellateComplexBorderCorner(subRect, radius, leftWidth, topWidth, leftColor, topColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            }

            // Tessellate the straight outlines
            // E
            float cornerSize = Mathf.Max(radius.y, topWidth);
            subRect = new Rect(rect.x, rect.y + cornerSize, leftWidth, rect.height - cornerSize);
            TessellateStraightBorder(subRect, Edges.Left, 0.0f, leftColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);

            // F
            cornerSize = Mathf.Max(radius.x, leftWidth);
            subRect = new Rect(rect.x + cornerSize, rect.y, rect.width - cornerSize, topWidth);
            TessellateStraightBorder(subRect, Edges.Top, 0.0f, topColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
        }

        internal enum Edges
        {
            None   = 0,
            Left   = 1 << 0,
            Top    = 1 << 1,
            Right  = 1 << 2,
            Bottom = 1 << 3,
            All = Left | Top | Right | Bottom
        }
        internal const int kMaxEdgeBit = 4;

        // The lines are defined by the following parametric equations:
        // Line A: p0 + (p1 - p0) * s
        // Line B: p2 + (p3 - p2) * t
        static Vector2 IntersectLines(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            Vector2 d32 = p3 - p2;
            Vector2 d20 = p2 - p0;
            Vector2 d10 = p1 - p0;

            float den = d32.x * d10.y - d10.x * d32.y;

            if (Mathf.Approximately(den, 0))
                return new Vector2(float.NaN, float.NaN);

            float num = d32.x * d20.y - d20.x * d32.y;
            float s = num / den;
            Vector2 i = p0 + d10 * s;
            return i;
        }

        static int LooseCompare(float a, float b)
        {
            if (a < b - kEpsilon)
                return -1;

            if (a > b + kEpsilon)
                return 1;

            return 0;
        }

        unsafe private static void TessellateComplexBorderCorner(Rect rect, Vector2 radius, float leftWidth, float topWidth, Color32 leftColor, Color32 topColor, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if (rect.width < kEpsilon || rect.height < kEpsilon)
                return;

            var center = rect.position + radius;
            var miterOffset = Vector2.zero;

            // Find a miter offset that makes the diagonal line go toward the radius
            float radiusRatio = radius.x / radius.y;
            var topLeft = center - radius;
            var innerCorner = new Vector2(leftWidth, topWidth);

            var intersection = IntersectLines(topLeft, innerCorner, new Vector2(0.0f, radius.y), radius);
            if (intersection.x >= 0.0f && LooseCompare(intersection.x, leftWidth) <= 0)
                miterOffset.x = Mathf.Min(0.0f, intersection.x - center.x);

            intersection = IntersectLines(topLeft, innerCorner, new Vector2(radius.x, 0.0f), radius);
            if (intersection.y >= 0.0f && LooseCompare(intersection.y, topWidth) <= 0)
                miterOffset.y = Mathf.Min(0.0f, intersection.y - center.y);

            TessellateFilledFan(center, radius, miterOffset, leftWidth, topWidth, leftColor, topColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);

            if (LooseCompare(rect.height, radius.y) > 0)
            {
                // Fill E
                //      ________
                //   -*   |     |
                //  *     |     |
                // *   A  |  F  |
                // |      |     |
                // |______|_____|
                // |   |
                // | E |
                // |___|

                var subRect = new Rect(rect.x, rect.y + radius.y, leftWidth, rect.height - radius.y);
                var offsets = stackalloc Vector2[4];
                offsets[2] = new Vector2(radius.x - leftWidth + miterOffset.x, miterOffset.y);
                TessellateQuad(subRect, Edges.Left | Edges.Right, offsets, leftColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            }
            else if (miterOffset.y < -kEpsilon)
            {
                // Fill the gap Y caused by miter y-offset
                //      ________
                //   -*   |     |
                //  *  A  |  F  |
                // *     _|_____|
                // |  __/ |
                // | /  Y |
                // |/_____|
                var subRect = new Rect(rect.x, rect.y + radius.y + miterOffset.y, leftWidth, -miterOffset.y);
                var offsets = stackalloc Vector2[4];
                offsets[1] = new Vector2(radius.x + miterOffset.x, 0.0f);
                TessellateQuad(subRect, Edges.Right, offsets, leftColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            }

            if (LooseCompare(rect.width, radius.x) > 0)
            {
                // Fill F
                //      ________
                //   -*   |     |
                //  *     |     |
                // *   A  |  F  |
                // |      |     |
                // |______|_____|
                // |   |
                // | E |
                // |___|

                var subRect = new Rect(rect.x + radius.x, rect.y, rect.width - radius.x, topWidth);
                var offsets = stackalloc Vector2[4];
                offsets[0] = new Vector2(miterOffset.x, radius.y - topWidth + miterOffset.y);
                TessellateQuad(subRect, Edges.Top | Edges.Bottom, offsets, topColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            }
            else if (miterOffset.x < -kEpsilon)
            {
                // Fill the gap X caused by miter x-offset
                //      ____
                //   -*    /|
                //  *  A  / |
                // *     /  |
                // |    / X |
                // |___/____|
                // |   |
                // | E |
                // |___|

                var subRect = new Rect(rect.x + radius.x + miterOffset.x, rect.y, -miterOffset.x, topWidth);
                var offsets = stackalloc Vector2[4];
                offsets[0] = new Vector2(leftWidth - (radius.x + miterOffset.x), 0.0f);
                offsets[1] = new Vector2(0.0f, radius.y);
                TessellateQuad(subRect, Edges.Bottom, offsets, topColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            }
        }

        private static void TessellateQuad(Rect rect, Color32 color, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            // Simple, non-smooted quads are tessellated with two triangles like so:
            //    +-------+
            //    |     / |
            //    |   /   |
            //    | /     |
            //    +-------+

            if (rect.width < kEpsilon || rect.height < kEpsilon)
                return;

            if (countOnly)
            {
                vertexCount += 4;
                indexCount += 6;
                return;
            }

            Vector3 topLeft = new Vector3(rect.x, rect.y, posZ);
            Vector3 topRight = new Vector3(rect.xMax, rect.y, posZ);
            Vector3 bottomLeft = new Vector3(rect.x, rect.yMax, posZ);
            Vector3 bottomRight = new Vector3(rect.xMax, rect.yMax, posZ);

            mesh.SetNextVertex(new Vertex { position = topLeft, tint = color });
            mesh.SetNextVertex(new Vertex { position = topRight, tint = color });
            mesh.SetNextVertex(new Vertex { position = bottomLeft, tint = color });
            mesh.SetNextVertex(new Vertex { position = bottomRight, tint = color });

            mesh.SetNextIndex((UInt16)(vertexCount + 0));
            mesh.SetNextIndex((UInt16)(vertexCount + 1));
            mesh.SetNextIndex((UInt16)(vertexCount + 2));
            mesh.SetNextIndex((UInt16)(vertexCount + 3));
            mesh.SetNextIndex((UInt16)(vertexCount + 2));
            mesh.SetNextIndex((UInt16)(vertexCount + 1));

            vertexCount += 4;
            indexCount += 6;
        }

        private static Edges[] s_AllEdges = { Edges.Left, Edges.Top, Edges.Right, Edges.Bottom };

        unsafe private static void TessellateQuad(Rect rect, Edges smoothedEdges, Color32 color, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            TessellateQuad(rect, smoothedEdges, null, color, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
        }

        private static int EdgesCount(Edges edges)
        {
            int count = 0;
            for (int i = 0; i < kMaxEdgeBit; ++i)
            {
                if ((((int)edges) & (1 << i)) != 0)
                    ++count;
            }
            return count;
        }

        unsafe private static void TessellateQuad(Rect rect, Edges smoothedEdges, Vector2* offsets, Color32 color, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            // Quads with smoothed edges are tessellated as a fan like so:
            //    +-------+
            //    | \   / |
            //    |   x   |
            //    | /   \ |
            //    +-------+
            // Vertices are duplicated for each triangle, which allows for independant per-edge arc encodings.
            // If a single edge is smoothed, there's a more optimized tessellation method used
            // to avoid the extra geometry of the edge extrusion (see TessellateQuadSingleEdge).

            if (rect.width < kEpsilon || rect.height < kEpsilon)
                return;

            if (smoothedEdges == Edges.None && offsets == null)
            {
                // Fallback to simpler case
                TessellateQuad(rect, color, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
                return;
            }

            if (EdgesCount(smoothedEdges) == 1 && offsets == null)
            {
                // We can optimize the single smoothed edge with only two triangles
                TessellateQuadSingleEdge(rect, smoothedEdges, color, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
                return;
            }

            if (countOnly)
            {
                vertexCount += 12;
                indexCount += 12;
                return;
            }

            const int kQuadsPointsLength = 4;
            var quadPoints = stackalloc Vector3[kQuadsPointsLength];
            quadPoints[0] = new Vector3(rect.xMin, rect.yMax, posZ);
            quadPoints[1] = new Vector3(rect.xMin, rect.yMin, posZ);
            quadPoints[2] = new Vector3(rect.xMax, rect.yMin, posZ);
            quadPoints[3] = new Vector3(rect.xMax, rect.yMax, posZ);

            var center = Vector3.zero;
            if (offsets != null)
            {
                quadPoints[0] += (Vector3)offsets[0];
                quadPoints[1] += (Vector3)offsets[1];
                quadPoints[2] += (Vector3)offsets[2];
                quadPoints[3] += (Vector3)offsets[3];

                // Compute the centroid of the quad
                center += quadPoints[0];
                center += quadPoints[1];
                center += quadPoints[2];
                center += quadPoints[3];
                center /= 4;
                center.z = posZ;
            }
            else
                center = new Vector3(rect.xMin + rect.width / 2, rect.yMin + rect.height / 2, posZ);

            UInt16 currentIndex = vertexCount;
            for (int i = 0; i < s_AllEdges.Length; ++i)
            {
                var currentEdge = s_AllEdges[i];
                var p = quadPoints[i];
                var q = quadPoints[(i + 1) % kQuadsPointsLength];
                float radius = (((p + q) / 2.0f) - center).magnitude;

                var v0 = new Vertex() { position = p, tint = color };
                var v1 = new Vertex() { position = q, tint = color };
                var v2 = new Vertex() { position = center, tint = color };
                if ((smoothedEdges & currentEdge) == currentEdge)
                    EncodeStraightArc(ref v0, ref v1, ref v2, radius);

                mesh.SetNextVertex(v0);
                mesh.SetNextVertex(v1);
                mesh.SetNextVertex(v2);
                mesh.SetNextIndex(currentIndex++);
                mesh.SetNextIndex(currentIndex++);
                mesh.SetNextIndex(currentIndex++);
            }

            vertexCount += 12;
            indexCount += 12;
        }

        static void EncodeStraightArc(ref Vertex v0, ref Vertex v1, ref Vertex center, float radius)
        {
            // Give space for AA computations.
            ExpandTriangle(ref v0.position, ref v1.position, center.position, 2.0f);

            var mid = (v0.position + v1.position) / 2.0f;
            var v = center.position - mid;

            var c = v0.position + v;
            v0.circle = new Vector4(c.x, c.y, radius, 0.0f);

            c = v1.position + v;
            v1.circle = new Vector4(c.x, c.y, radius, 0.0f);

            center.circle = new Vector4(center.position.x, center.position.y, radius, 0.0f);

            var arcFlags = new Color32(0, 0, 1, 0);
            v0.flags = arcFlags;
            v1.flags = arcFlags;
            center.flags = arcFlags;
        }

        static void ExpandTriangle(ref Vector3 v0, ref Vector3 v1, Vector3 center, float factor)
        {
            v0 += (v0 - center).normalized * factor;
            v1 += (v1 - center).normalized * factor;
        }

        private static void TessellateQuadSingleEdge(Rect rect, Edges smoothedEdge, Color32 color, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if (countOnly)
            {
                vertexCount += 4;
                indexCount += 6;
                return;
            }

            var p0 = new Vector3(rect.x, rect.y, posZ);
            var p1 = new Vector3(rect.x + rect.width, rect.y, posZ);
            var p2 = new Vector3(rect.x + rect.width, rect.y + rect.height, posZ);
            var p3 = new Vector3(rect.x, rect.y + rect.height, posZ);

            // Add offset on smoothed edge to give more space for AA
            var circleP0 = Vector4.zero;
            var circleP1 = Vector4.zero;
            var circleP2 = Vector4.zero;
            var circleP3 = Vector4.zero;
            switch (smoothedEdge)
            {
                case Edges.Left:
                    p0.x -= 1; p3.x -= 1;
                    circleP0 = circleP1 = new Vector4(p1.x, p1.y, rect.width, 0.0f);
                    circleP2 = circleP3 = new Vector4(p2.x, p2.y, rect.width, 0.0f);
                    break;
                case Edges.Top:
                    p0.y -= 1; p1.y -= 1;
                    circleP0 = circleP3 = new Vector4(p3.x, p3.y, rect.height, 0.0f);
                    circleP1 = circleP2 = new Vector4(p2.x, p2.y, rect.height, 0.0f);
                    break;
                case Edges.Right:
                    p1.x += 1; p2.x += 1;
                    circleP0 = circleP1 = new Vector4(p0.x, p0.y, rect.width, 0.0f);
                    circleP2 = circleP3 = new Vector4(p3.x, p3.y, rect.width, 0.0f);
                    break;
                case Edges.Bottom:
                    p2.y += 1; p3.y += 1;
                    circleP0 = circleP3 = new Vector4(p0.x, p0.y, rect.height, 0.0f);
                    circleP1 = circleP2 = new Vector4(p1.x, p1.y, rect.height, 0.0f);
                    break;
                default: break;
            }

            var arcFlags = new Color32(0, 0, 1, 0);

            UInt16 baseIndex = vertexCount;
            mesh.SetNextVertex(new Vertex() { position = p0, tint = color, flags = arcFlags, circle = circleP0 });
            mesh.SetNextVertex(new Vertex() { position = p1, tint = color, flags = arcFlags, circle = circleP1 });
            mesh.SetNextVertex(new Vertex() { position = p2, tint = color, flags = arcFlags, circle = circleP2 });
            mesh.SetNextVertex(new Vertex() { position = p3, tint = color, flags = arcFlags, circle = circleP3 });

            mesh.SetNextIndex((UInt16)(baseIndex));
            mesh.SetNextIndex((UInt16)(baseIndex + 1));
            mesh.SetNextIndex((UInt16)(baseIndex + 2));
            mesh.SetNextIndex((UInt16)(baseIndex));
            mesh.SetNextIndex((UInt16)(baseIndex + 2));
            mesh.SetNextIndex((UInt16)(baseIndex + 3));

            vertexCount += 4;
            indexCount += 6;
        }

        static void TessellateStraightBorder(Rect rect, Edges smoothedEdge, float miterOffset, Color color, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            Debug.Assert(smoothedEdge == Edges.Left || smoothedEdge == Edges.Top);

            if (rect.width < kEpsilon || rect.height < kEpsilon)
                return;

            if (countOnly)
            {
                vertexCount += 4;
                indexCount += 6;
                return;
            }

            var a = new Vector3(rect.xMin, rect.yMin, posZ);
            var b = new Vector3(rect.xMax, rect.yMin, posZ);
            var c = new Vector3(rect.xMax, rect.yMax, posZ);
            var d = new Vector3(rect.xMin, rect.yMax, posZ);

            // Inflate the geometry to give space to AA computations.
            // Carefully inflate in the direction of the miter diagonal.
            if (smoothedEdge == Edges.Left)
            {
                b.y += miterOffset;
                var v = (b - a).normalized * 1.4142f;
                a -= v;
                b += v;
                c.x += 1.0f;
                d.x -= 1.0f;

                // Arc center is on column x = rect.x + rect.width + 1
                float x = rect.x + rect.width + 1.0f;
                float outer = rect.width + 1.0f;
                float inner = 1.0f;
                var arcFlags = new Color32(0, 0, 1, 0);

                mesh.SetNextVertex(new Vertex() { position = a, tint = color, flags = arcFlags, circle = new Vector4(x, a.y, outer, inner), uv = new Vector2(x, a.y) });
                mesh.SetNextVertex(new Vertex() { position = b, tint = color, flags = arcFlags, circle = new Vector4(x, b.y, outer, inner), uv = new Vector2(x, b.y) });
                mesh.SetNextVertex(new Vertex() { position = c, tint = color, flags = arcFlags, circle = new Vector4(x, c.y, outer, inner), uv = new Vector2(x, c.y) });
                mesh.SetNextVertex(new Vertex() { position = d, tint = color, flags = arcFlags, circle = new Vector4(x, d.y, outer, inner), uv = new Vector2(x, d.y) });
            }
            else
            {
                d.x += miterOffset;
                var v = (d - a).normalized * 1.4142f;
                a -= v;
                d += v;
                b.y -= 1;
                c.y += 1;

                // Arc center is on row y = rect.y + rect.height + 1
                float y = rect.y + rect.height + 1.0f;
                float outer = rect.height + 1.0f;
                float inner = 1.0f;
                var arcFlags = new Color32(0, 0, 1, 0);

                mesh.SetNextVertex(new Vertex() { position = a, tint = color, flags = arcFlags, circle = new Vector4(a.x, y, outer, inner), uv = new Vector2(a.x, y) });
                mesh.SetNextVertex(new Vertex() { position = b, tint = color, flags = arcFlags, circle = new Vector4(b.x, y, outer, inner), uv = new Vector2(b.x, y) });
                mesh.SetNextVertex(new Vertex() { position = c, tint = color, flags = arcFlags, circle = new Vector4(c.x, y, outer, inner), uv = new Vector2(c.x, y) });
                mesh.SetNextVertex(new Vertex() { position = d, tint = color, flags = arcFlags, circle = new Vector4(d.x, y, outer, inner), uv = new Vector2(d.x, y) });
            }

            UInt16 currentIndex = vertexCount;
            mesh.SetNextIndex((UInt16)(currentIndex));
            mesh.SetNextIndex((UInt16)(currentIndex + 1));
            mesh.SetNextIndex((UInt16)(currentIndex + 2));
            mesh.SetNextIndex((UInt16)(currentIndex + 2));
            mesh.SetNextIndex((UInt16)(currentIndex + 3));
            mesh.SetNextIndex((UInt16)(currentIndex));

            vertexCount += 4;
            indexCount += 6;
        }

        static void FillEllipseData(Vector2 ellipseCenter, Vector2 outerRad, Vector2 innerRad, ref Vertex vertex)
        {
            float r = Mathf.Min(outerRad.x, outerRad.y);

            // Position of the POI relative to the center of the elipse
            var position = (Vector2)vertex.position;
            Vector2 relativePos = position - ellipseCenter;

            // Center should be offset by r when relativePos = radius (respectively for x and y)
            var circleCenter = position - relativePos * r / outerRad;
            vertex.circle = new Vector4(circleCenter.x, circleCenter.y, r, 0.0f);

            // Now, do the same for the inner radius
            r = Mathf.Min(innerRad.x, innerRad.y);
            circleCenter = position - relativePos * r / innerRad;
            vertex.uv = circleCenter;
            vertex.circle.w = r;
        }

        static Vector3 GetInterpolatedEllipseData(Vector2 ellipseCenter, Vector2 radius, Vector2 position)
        {
            float r = Mathf.Min(radius.x, radius.y);

            // Position of the POI relative to the center of the elipse
            Vector2 relativePos = position - ellipseCenter;

            // Center should be offset by r when relativePos = radius (respectively for x and y)
            var circleCenter = position - relativePos * r / radius;

            return new Vector3(circleCenter.x, circleCenter.y, r);
        }

        static Vector4 GetInterpolatedCircle(Vector2 p, ref Vertex v0, ref Vertex v1, ref Vertex v2)
        {
            // Interpolate using barycentric coordinates
            float u, v, w;
            ComputeBarycentricCoordinates(p, v0.position, v1.position, v2.position, out u, out v, out w);
            return v0.circle * u + v1.circle * v + v2.circle * w;
        }

        static void ComputeBarycentricCoordinates(Vector2 p, Vector2 a, Vector2 b, Vector2 c, out float u, out float v, out float w)
        {
            // From Christer Ericson's Real-Time Collision Detection:
            // https://gamedev.stackexchange.com/questions/23743/whats-the-most-efficient-way-to-find-barycentric-coordinates

            // Compute barycentric coordinates (u, v, w) for point p with respect to triangle (a, b, c)
            Vector2 v0 = b - a, v1 = c - a, v2 = p - a;
            float d00 = Vector2.Dot(v0, v0);
            float d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1);
            float d20 = Vector2.Dot(v2, v0);
            float d21 = Vector2.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            v = (d11 * d20 - d01 * d21) / denom;
            w = (d00 * d21 - d01 * d20) / denom;
            u = 1.0f - v - w;
        }

        static void TessellateFilledFan(Vector2 center, Vector2 radius, Vector2 miterOffset, float leftWidth, float topWidth, Color32 leftColor, Color32 topColor, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if (countOnly)
            {
                vertexCount += 6;
                indexCount += 6;
                return;
            }

            byte isArc = 1;
            Color32 flags = new Color32((byte)VertexFlags.IsSolid, 0, isArc, 0);
            float r = Mathf.Min(radius.x, radius.y); // This is arbitrary since we have a degree of freedom.

            var bottomRight = new Vertex { flags = flags };
            var bottomLeft = bottomRight;
            var topLeft = bottomRight;
            var topRight = bottomRight;

            bottomRight.position = new Vector3(center.x, center.y, posZ);
            bottomLeft.position = new Vector3(center.x - radius.x, center.y, posZ);
            topLeft.position = new Vector3(center.x - radius.x, center.y - radius.y, posZ);
            topRight.position = new Vector3(center.x, center.y - radius.y, posZ);

            bottomRight.circle = new Vector4(center.x, center.y, r, 0.0f);
            bottomLeft.circle = new Vector4(center.x - radius.x + r, center.y, r, 0.0f);
            topLeft.circle = new Vector4(center.x - radius.x + r, center.y - radius.y + r, r, 0.0f);
            topRight.circle = new Vector4(center.x, center.y - radius.y + r, r, 0.0f);

            if (miterOffset != Vector2.zero)
            {
                var newPos = bottomRight.position + (Vector3)miterOffset;
                bottomRight.circle = GetInterpolatedCircle(newPos, ref bottomRight, ref bottomLeft, ref topLeft);
                bottomRight.position = newPos;
            }

            var topLeft2 = topLeft;
            var bottomRight2 = bottomRight;

            bottomRight.tint = leftColor;
            bottomLeft.tint = leftColor;
            topLeft2.tint = leftColor;

            topLeft.tint = topColor;
            topRight.tint = topColor;
            bottomRight2.tint = topColor;

            mesh.SetNextVertex(bottomRight);
            mesh.SetNextVertex(bottomLeft);
            mesh.SetNextVertex(topLeft2);
            mesh.SetNextVertex(topLeft);
            mesh.SetNextVertex(topRight);
            mesh.SetNextVertex(bottomRight2);

            mesh.SetNextIndex((ushort)(vertexCount + 0));
            mesh.SetNextIndex((ushort)(vertexCount + 1));
            mesh.SetNextIndex((ushort)(vertexCount + 2));
            mesh.SetNextIndex((ushort)(vertexCount + 3));
            mesh.SetNextIndex((ushort)(vertexCount + 4));
            mesh.SetNextIndex((ushort)(vertexCount + 5));

            vertexCount += 6;
            indexCount += 6;
        }

        private static void TessellateBorderedFan(Vector2 center, Vector2 outerRadius, float leftWidth, float topWidth, Color32 leftColor, Color32 topColor, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if (countOnly)
            {
                vertexCount += 6;
                indexCount += 6;
                return;
            }
            Vector2 innerRadius = new Vector2(outerRadius.x - leftWidth, outerRadius.y - topWidth);

            var widths = new Vector2(leftWidth, topWidth);
            byte isArc = 1;
            Color32 flags = new Color32((byte)VertexFlags.IsSolid, 0, isArc, 0);

            var bottomRight = new Vertex { flags = flags, uv = widths };
            var bottomLeft = bottomRight;
            var topLeft = bottomRight;
            var topRight = bottomRight;

            bottomRight.position = new Vector3(center.x, center.y, posZ);
            bottomLeft.position = new Vector3(center.x - outerRadius.x, center.y, posZ);
            topLeft.position = new Vector3(center.x - outerRadius.x, center.y - outerRadius.y, posZ);
            topRight.position = new Vector3(center.x, center.y - outerRadius.y, posZ);

            FillEllipseData(center, outerRadius, innerRadius, ref bottomRight);
            FillEllipseData(center, outerRadius, innerRadius, ref bottomLeft);
            FillEllipseData(center, outerRadius, innerRadius, ref topLeft);
            FillEllipseData(center, outerRadius, innerRadius, ref topRight);

            var topLeft2 = topLeft;
            var bottomRight2 = bottomRight;

            bottomRight.tint = leftColor;
            bottomLeft.tint = leftColor;
            topLeft2.tint = leftColor;

            topLeft.tint = topColor;
            topRight.tint = topColor;
            bottomRight2.tint = topColor;

            mesh.SetNextVertex(bottomRight);
            mesh.SetNextVertex(bottomLeft);
            mesh.SetNextVertex(topLeft2);
            mesh.SetNextVertex(topLeft);
            mesh.SetNextVertex(topRight);
            mesh.SetNextVertex(bottomRight2);

            mesh.SetNextIndex((ushort)(vertexCount + 0));
            mesh.SetNextIndex((ushort)(vertexCount + 1));
            mesh.SetNextIndex((ushort)(vertexCount + 2));
            mesh.SetNextIndex((ushort)(vertexCount + 3));
            mesh.SetNextIndex((ushort)(vertexCount + 4));
            mesh.SetNextIndex((ushort)(vertexCount + 5));

            vertexCount += 6;
            indexCount += 6;
        }

        private static void MirrorVertices(Rect rect, NativeSlice<Vertex> vertices, int vertexStart, int vertexCount, bool flipHorizontal)
        {
            if (flipHorizontal)
            {
                for (int i = 0; i < vertexCount; ++i)
                {
                    var vertex = vertices[vertexStart + i];
                    vertex.position.x = rect.xMax - (vertex.position.x - rect.xMax);
                    vertex.circle.x = rect.xMax - (vertex.circle.x - rect.xMax);
                    if (vertex.circle.w > kEpsilon)
                        // UV stores inner-circle's center
                        vertex.uv.x = rect.xMax - (vertex.uv.x - rect.xMax);
                    else
                        vertex.uv.x = -vertex.uv.x;
                    vertices[vertexStart + i] = vertex;
                }
            }
            else
            {
                for (int i = 0; i < vertexCount; ++i)
                {
                    var vertex = vertices[vertexStart + i];
                    vertex.position.y = rect.yMax - (vertex.position.y - rect.yMax);
                    vertex.circle.y = rect.yMax - (vertex.circle.y - rect.yMax);
                    if (vertex.circle.w > kEpsilon)
                        // UV stores inner-circle's center
                        vertex.uv.y = rect.yMax - (vertex.uv.y - rect.yMax);
                    else
                        vertex.uv.y = -vertex.uv.y;
                    vertices[vertexStart + i] = vertex;
                }
            }
        }

        private static void FlipWinding(NativeSlice<UInt16> indices, int indexStart, int indexCount)
        {
            for (int i = 0; i < indexCount; i += 3)
            {
                UInt16 tmp = indices[indexStart + i];
                indices[indexStart + i] = indices[indexStart + i + 1];
                indices[indexStart + i + 1] = tmp;
            }
        }

        private static void ComputeUVs(Rect tessellatedRect, Rect textureRect, Rect uvRegion, NativeSlice<Vertex> vertices)
        {
            var offset = tessellatedRect.position;
            var scale = new Vector2(1.0f / tessellatedRect.width, 1.0f / tessellatedRect.height);
            for (int i = 0; i < vertices.Length; ++i)
            {
                var vertex = vertices[i];
                var uv = (Vector2)vertex.position;

                // Compute the relative location within the tessellation rect.
                uv -= offset;
                uv *= scale;

                // Map to the provided texture rect.
                vertex.uv.x = (uv.x * textureRect.width + textureRect.xMin) * uvRegion.width + uvRegion.xMin;
                vertex.uv.y = ((1.0f - uv.y) * textureRect.height + textureRect.yMin) * uvRegion.height + uvRegion.yMin;

                vertices[i] = vertex;
            }
        }
    }
}
