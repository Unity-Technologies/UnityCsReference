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

        ///<summary>
        /// Tessellates the border OR the content:
        /// If any of the left/right/top/bottom border parameters is greater than Epsilon, we tessellate ONLY a border.
        /// Otherwise we tessellate ONLY the content.
        /// </summary>
        /// <param name="vertexFlags">Flags are only used for content, not for a border.</param>
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

            UInt16 vertexCount = 0, indexCount = 0;
            CountRectTriangles(ref rectParams, ref vertexCount, ref indexCount);

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

        private static void CountRectTriangles(ref MeshGenerationContextUtils.RectangleParams rectParams, ref UInt16 vertexCount, ref UInt16 indexCount)
        {
            // To count the required triangles, we call the tessellation method with a "countOnly=true" flag, which skips
            // tessellation and only update the vertex and index counts.
            TessellateRectInternal(ref rectParams, 0, null, ref vertexCount, ref indexCount, true);
        }

        private static void CountBorderTriangles(ref MeshGenerationContextUtils.BorderParams border, ref UInt16 vertexCount, ref UInt16 indexCount)
        {
            // To count the required triangles, we call the tessellation method with a "countOnly=true" flag, which skips
            // tessellation and only update the vertex and index counts.
            TessellateBorderInternal(ref border, 0, null, ref vertexCount, ref indexCount, true);
        }

        private static void TessellateRectInternal(ref MeshGenerationContextUtils.RectangleParams rectParams, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly = false)
        {
            if (!rectParams.HasRadius(kEpsilon))
            {
                TessellateQuad(rectParams.rect, 0, 0, 0, TessellationType.Content, rectParams.color, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            }
            else
            {
                TessellateRoundedCorners(ref rectParams, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            }
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
                TessellateQuad(rect, 0, 0, 0, TessellationType.Content, color, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
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
            TessellateFilledFan(TessellationType.Content, cornerCenter, radius, 0, 0, color, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            if (radius.x < rect.width)
            {
                // A
                subRect = new Rect(rect.x + radius.x, rect.y, rect.width - radius.x, rect.height);
                TessellateQuad(subRect, 0,  0, 0, TessellationType.Content, color, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            }
            if (radius.y < rect.height)
            {
                // B
                subRect = new Rect(rect.x, rect.y + radius.y, radius.x < rect.width ? radius.x : rect.width, rect.height - radius.y);
                TessellateQuad(subRect, 0, 0, 0, TessellationType.Content, color, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
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
                    TessellateQuad(subRect, topWidth, leftWidth, topWidth, TessellationType.EdgeVertical, leftColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
                }
                if (topWidth > kEpsilon)
                {
                    // B
                    subRect = new Rect(rect.x, rect.y, rect.width, topWidth);
                    TessellateQuad(subRect, leftWidth, leftWidth, topWidth, TessellationType.EdgeHorizontal, topColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
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
                if (leftColor.InternalEquals(topColor))
                    TessellateFilledFan(TessellationType.EdgeCorner, cornerCenter, radius, leftWidth, topWidth, leftColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
                else
                    TessellateFilledFan(cornerCenter, radius, leftWidth, topWidth, leftColor, topColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
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
                if (leftColor.InternalEquals(topColor))
                    TessellateBorderedFan(cornerCenter, radius, leftWidth, topWidth, leftColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
                else
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
                if (leftColor.InternalEquals(topColor))
                    TessellateComplexBorderCorner(subRect, radius, leftWidth, topWidth, leftColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
                else
                    TessellateComplexBorderCorner(subRect, radius, leftWidth, topWidth, leftColor, topColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
            }

            // Tessellate the straight outlines
            // E
            float cornerSize = Mathf.Max(radius.y, topWidth);
            subRect = new Rect(rect.x, rect.y + cornerSize, leftWidth, rect.height - cornerSize);
            TessellateQuad(subRect, 0, leftWidth, topWidth, TessellationType.EdgeVertical, leftColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);

            // F
            cornerSize = Mathf.Max(radius.x, leftWidth);
            subRect = new Rect(rect.x + cornerSize, rect.y, rect.width - cornerSize, topWidth);
            TessellateQuad(subRect, 0, leftWidth, topWidth, TessellationType.EdgeHorizontal, topColor, posZ, mesh, ref vertexCount, ref indexCount, countOnly);
        }

        enum TessellationType { EdgeHorizontal, EdgeVertical, EdgeCorner, Content }

        // This method assumes that we are intersecting a top-left corner that has an ellipse shape with a line that
        // goes through the origin and the ellipse quarter. The ellipse is touching the axes.
        //
        // o--x                -
        // |\          *       |
        // y \    *            |
        //    i*               |
        //   * \               b
        //  *   \              |
        // *     \             |
        // |----------a--------\
        //
        // o: origin
        // x: x axis increasing towards the right
        // y: y axis increasing towards the bottom
        // a: half width of the ellipse
        // b: half height of the ellipse
        // i: intersection point
        static Vector2 IntersectEllipseWithLine(float a, float b, Vector2 dir)
        {
            Debug.Assert(dir.x > 0 || dir.y > 0);

            if (a < Mathf.Epsilon || b < Mathf.Epsilon)
                return new Vector2(0, 0); // Degenerate case

            if (dir.y < 0.001 * dir.x)
                return new Vector2(a, 0); // The line is almost horizontal

            if (dir.x < 0.001 * dir.y)
                return new Vector2(0, b); // The line is almost vertical

            float m = dir.y / dir.x; // slope of the line
            float r = b / a;

            // Ellipse equation: y = b - r*sqrt(a²-(x-a)²)
            // Line equation: y = m*x

            // We get x by substracting both equations and solving for x:
            float x = b * (r + m - Mathf.Sqrt(2 * m * r)) / (m * m + r * r);

            // We get y by substituting x in the line equation:
            float y = m * x;

            return new Vector2(x, y);
        }

        // Let an ellipse centered at the origin, defined as:
        // x = a*cos(theta)
        // y = b*sin(theta)
        // where theta goes from 0..Pi/2
        //
        // And a line going through the ellipse and the origin, this method returns the value of theta that allows to
        // get the intersection point between the two.
        //
        // -            \
        // |             i *
        // |          *   \
        // |      *        \
        // b   *            \ y
        // | *               \|
        // |*              x--o
        // \---------a--------|
        //
        // o: origin
        // x: x axis increasing towards the left
        // y: y axis increasing towards the top
        // a: half width of the ellipse
        // b: half height of the ellipse
        // i: intersection point
        static float GetCenteredEllipseLineIntersectionTheta(float a, float b, Vector2 dir)
        {
            // The slope of the line intersecting the ellipse at i is the same as any point of the line, so it is given by:
            // y/x = (b * sin(theta)) / (a * cos(theta)) <=> (y * a) / (x * b) = tan(theta)
            // Solving for theta, we get:
            return Mathf.Atan2(dir.y * a, dir.x * b);
        }

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

        static void TessellateComplexBorderCorner(Rect rect, Vector2 radius, float leftWidth, float topWidth, Color32 color, float posZ, MeshWriteData mesh, ref UInt16 refVertexCount, ref UInt16 refIndexCount, bool countOnly)
        {
            if (rect.width < kEpsilon || rect.height < kEpsilon)
                return;

            int widthDiff = LooseCompare(leftWidth, radius.x);
            int heightDiff = LooseCompare(topWidth, radius.y);

            Debug.Assert(widthDiff != heightDiff || widthDiff > 0 && heightDiff > 0);

            UInt16 vertexCount = refVertexCount;
            UInt16 indexCount = refIndexCount;
            int fanTriangles = kSubdivisions - 1;

            if (countOnly)
            {
                int triangleCount = fanTriangles;
                if (heightDiff != 0)
                    triangleCount += 1;
                if (widthDiff != 0)
                    triangleCount += 1;

                vertexCount += (ushort)(triangleCount + 3);
                indexCount += (ushort)(triangleCount * 3);

                refIndexCount = indexCount;
                refVertexCount = vertexCount;
                return;
            }

            Color32 innerFlags = new Color32((byte)VertexFlags.IsEdge, 0, 0, 0);
            Color32 outerFlags = new Color32((byte)VertexFlags.IsSolid, 0, 0, 0);
            var widths = new Vector2(leftWidth, topWidth);

            // Tessellate
            {
                // inner-corner (top)
                ushort innerTopIndex = vertexCount;
                mesh.SetNextVertex(new Vertex { position = new Vector3(leftWidth, topWidth, posZ), tint = color, uv = widths, flags = innerFlags });
                ++vertexCount;

                // inner-corner (left)
                ushort innerLeftIndex = vertexCount;
                mesh.SetNextVertex(new Vertex { position = new Vector3(leftWidth, topWidth, posZ), tint = color, uv = widths, flags = innerFlags });
                ++vertexCount;

                if (heightDiff < 0)
                {
                    // bottom-right
                    mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMax, rect.yMax, posZ), tint = color, uv = widths, flags = innerFlags });
                    // bottom-left
                    mesh.SetNextVertex(new Vertex { position = new Vector3(0, rect.yMax, posZ), tint = color, flags = outerFlags });
                    vertexCount += 2;

                    mesh.SetNextIndex(innerLeftIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }
                else
                {
                    // bottom-left
                    mesh.SetNextVertex(new Vertex { position = new Vector3(0, rect.yMax, posZ), tint = color, flags = outerFlags });
                    ++vertexCount;
                }

                if (heightDiff > 0)
                {
                    // fan left
                    mesh.SetNextVertex(new Vertex { position = new Vector3(0, radius.y, posZ), tint = color, flags = outerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(innerLeftIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }

                float deltaAngle = Mathf.PI * 0.5f / fanTriangles;
                for (int i = 1; i < fanTriangles; ++i)
                {
                    float angle = i * deltaAngle;
                    var p = new Vector2(radius.x - Mathf.Cos(angle) * radius.x, radius.y - Mathf.Sin(angle) * radius.y);
                    mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), tint = color, flags = outerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(innerLeftIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }

                if (widthDiff > 0)
                {
                    // fan top
                    mesh.SetNextVertex(new Vertex { position = new Vector3(radius.x, 0, posZ), tint = color, flags = outerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(innerLeftIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }

                {
                    // top-right
                    mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMax, 0, posZ), tint = color, flags = outerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(innerTopIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }

                if (widthDiff < 0)
                {
                    // bottom-right
                    mesh.SetNextVertex(new Vertex { position = new Vector3(rect.xMax, rect.yMax, posZ), tint = color, uv = widths, flags = innerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(innerTopIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }
            }

            refIndexCount = indexCount;
            refVertexCount = vertexCount;
        }

        static void TessellateComplexBorderCorner(Rect rect, Vector2 radius, float leftWidth, float topWidth, Color32 leftColor, Color32 topColor, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if (rect.width < kEpsilon || rect.height < kEpsilon)
                return;

            int widthDiff = LooseCompare(leftWidth, radius.x);
            int heightDiff = LooseCompare(topWidth, radius.y);

            Debug.Assert(widthDiff != heightDiff || widthDiff > 0 && heightDiff > 0);

            if (countOnly)
            {
                vertexCount += kSubdivisions; // fan
                vertexCount += 2; // center, inner-corner
                vertexCount += 3; // vertices on the boundary are doubled

                int triangleCount = 2;
                triangleCount += kSubdivisions - 1; // fan

                if (widthDiff != 0)
                {
                    ++vertexCount;
                    ++triangleCount;
                }

                if (heightDiff != 0)
                {
                    ++vertexCount;
                    ++triangleCount;
                }

                indexCount += (UInt16)(triangleCount * 3);
                return;
            }

            // Compute key points
            Vector2 innerCorner = new Vector2(rect.x + leftWidth, rect.y + topWidth);
            Vector2 outerCorner = new Vector2(rect.x, rect.y);
            Vector2 ellipseLeft = new Vector2(rect.x, rect.y + radius.y);
            Vector2 ellipseTop = new Vector2(rect.x + radius.x, rect.y);
            Vector2 fanCorner = new Vector2(ellipseTop.x, ellipseLeft.y);
            Vector2 tessCenter = IntersectLines(ellipseLeft, ellipseTop, innerCorner, outerCorner);
            Vector2 outerIntersection = IntersectEllipseWithLine(radius.x, radius.y, innerCorner - outerCorner);
            Vector2 topRight = new Vector2(rect.xMax, rect.y);
            Vector2 bottomLeft = new Vector2(rect.x, rect.yMax);
            Vector2 bottomRight = new Vector2(rect.xMax, rect.yMax);

            // Partition fan triangles
            float outerIntersectionAngle = GetCenteredEllipseLineIntersectionTheta(radius.x, radius.y, radius - outerIntersection);
            outerIntersection.x += rect.x; // Note that we delayed this translation to be able to perform the previous calculation.
            outerIntersection.y += rect.y;
            int fanTriangles = kSubdivisions - 1;
            int fanLeftTriangles = Mathf.Clamp(Mathf.RoundToInt(outerIntersectionAngle / (0.5f * Mathf.PI) * fanTriangles), 1, fanTriangles - 1);
            int fanTopTriangles = fanTriangles - fanLeftTriangles;

            Color32 innerFlags = new Color32((byte)VertexFlags.IsEdge, 0, 0, 0);
            Color32 outerFlags = new Color32((byte)VertexFlags.IsSolid, 0, 0, 0);
            var widths = new Vector2(leftWidth, topWidth);

            // Tessellate left
            {
                ushort centerIndex = vertexCount;
                mesh.SetNextVertex(new Vertex { position = new Vector3(tessCenter.x, tessCenter.y, posZ), tint = leftColor, flags = outerFlags });
                mesh.SetNextVertex(new Vertex { position = new Vector3(innerCorner.x, innerCorner.y, posZ), tint = leftColor, uv = widths, flags = innerFlags });
                vertexCount += 2;

                if (heightDiff < 0)
                {
                    mesh.SetNextVertex(new Vertex { position = new Vector3(bottomRight.x, bottomRight.y, posZ), tint = leftColor, uv = widths, flags = innerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(centerIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }

                {
                    mesh.SetNextVertex(new Vertex { position = new Vector3(bottomLeft.x, bottomLeft.y, posZ), tint = leftColor, flags = outerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(centerIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }

                if (heightDiff > 0)
                {
                    mesh.SetNextVertex(new Vertex { position = new Vector3(ellipseLeft.x, ellipseLeft.y, posZ), tint = leftColor, flags = outerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(centerIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }

                float deltaAngle = outerIntersectionAngle / fanLeftTriangles;
                for (int i = 1; i < fanLeftTriangles; ++i)
                {
                    float angle = i * deltaAngle;
                    Vector2 p = fanCorner - new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), tint = leftColor, flags = outerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(centerIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }

                {
                    mesh.SetNextVertex(new Vertex { position = new Vector3(outerIntersection.x, outerIntersection.y, posZ), tint = leftColor, flags = outerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(centerIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }
            }

            // Tessellate top
            {
                ushort centerIndex = vertexCount;
                mesh.SetNextVertex(new Vertex { position = new Vector3(tessCenter.x, tessCenter.y, posZ), tint = topColor, flags = outerFlags });
                mesh.SetNextVertex(new Vertex { position = new Vector3(outerIntersection.x, outerIntersection.y, posZ), tint = topColor, flags = outerFlags });
                vertexCount += 2;

                float deltaAngle = (Mathf.PI * 0.5f - outerIntersectionAngle) / fanTopTriangles;
                for (int i = 1; i < fanTopTriangles; ++i)
                {
                    float angle = outerIntersectionAngle + i * deltaAngle;
                    Vector2 p = fanCorner - new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), tint = topColor, flags = outerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(centerIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }

                if (widthDiff > 0)
                {
                    mesh.SetNextVertex(new Vertex { position = new Vector3(ellipseTop.x, ellipseTop.y, posZ), tint = topColor, flags = outerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(centerIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }

                {
                    mesh.SetNextVertex(new Vertex { position = new Vector3(topRight.x, topRight.y, posZ), tint = topColor, flags = outerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(centerIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }

                if (widthDiff < 0)
                {
                    mesh.SetNextVertex(new Vertex { position = new Vector3(bottomRight.x, bottomRight.y, posZ), tint = topColor, uv = widths, flags = innerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(centerIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }

                {
                    mesh.SetNextVertex(new Vertex { position = new Vector3(innerCorner.x, innerCorner.y, posZ), tint = topColor, uv = widths, flags = innerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(centerIndex);
                    mesh.SetNextIndex((ushort)(vertexCount - 2));
                    mesh.SetNextIndex((ushort)(vertexCount - 1));
                    indexCount += 3;
                }
            }
        }

        private static void TessellateQuad(Rect rect, float miterOffset, float leftWidth, float topWidth, TessellationType tessellationType, Color32 color, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if ((rect.width < kEpsilon || rect.height < kEpsilon) &&
                // For vertical/horizontal edge tessellation, skip this check since quads may act as a ligature that fixes seams.
                tessellationType != TessellationType.EdgeHorizontal && tessellationType != TessellationType.EdgeVertical)
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

            var widths = new Vector2(leftWidth, topWidth);
            Vector2 uvTopLeft, uvTopRight, uvBottomLeft, uvBottomRight;
            Color32 flagsTopLeft, flagsTopRight, flagsBottomLeft, flagsBottomRight;
            switch (tessellationType)
            {
                case TessellationType.EdgeHorizontal:
                    bottomLeft.x += miterOffset;
                    // The uvs contain the displacement from the vertically opposed corner.
                    uvTopLeft = uvTopRight = Vector2.zero;
                    uvBottomLeft = uvBottomRight = widths;
                    flagsTopLeft = flagsTopRight = new Color32((byte)VertexFlags.IsSolid, 0, 0, 0);
                    flagsBottomLeft = new Color32((byte)VertexFlags.IsEdge, 0, 0, 0);
                    flagsBottomRight = new Color32((byte)VertexFlags.IsEdgeNoShrinkX, 0, 0, 0);
                    break;
                case TessellationType.EdgeVertical:
                    topRight.y += miterOffset;
                    // The uvs contain the displacement from the horizontally opposed corner.
                    uvTopLeft = uvBottomLeft = Vector2.zero;
                    uvTopRight = uvBottomRight = widths;
                    flagsTopLeft = flagsBottomLeft = new Color32((byte)VertexFlags.IsSolid, 0, 0, 0);
                    flagsTopRight = new Color32((byte)VertexFlags.IsEdge, 0, 0, 0);
                    flagsBottomRight = new Color32((byte)VertexFlags.IsEdgeNoShrinkY, 0, 0, 0);
                    break;
                case TessellationType.EdgeCorner:
                    uvTopLeft = uvTopRight = uvBottomLeft = uvBottomRight = Vector2.zero;
                    flagsTopLeft = flagsTopRight = flagsBottomLeft = flagsBottomRight = new Color32((byte)VertexFlags.IsSolid, 0, 0, 0);
                    break;
                case TessellationType.Content:
                    uvTopLeft = uvTopRight = uvBottomLeft = uvBottomRight = Vector2.zero; // UVs are computed later for content
                    flagsTopLeft = flagsTopRight = flagsBottomLeft = flagsBottomRight = new Color32(0, 0, 0, 0); // Primed for later update by the chain build
                    break;
                default:
                    throw new NotImplementedException();
            }

            mesh.SetNextVertex(new Vertex { position = topLeft, uv = uvTopLeft, tint = color, flags = flagsTopLeft });
            mesh.SetNextVertex(new Vertex { position = topRight, uv = uvTopRight, tint = color, flags = flagsTopRight });
            mesh.SetNextVertex(new Vertex { position = bottomLeft, uv = uvBottomLeft, tint = color, flags = flagsBottomLeft });
            mesh.SetNextVertex(new Vertex { position = bottomRight, uv = uvBottomRight, tint = color, flags = flagsBottomRight });

            mesh.SetNextIndex((UInt16)(vertexCount + 0));
            mesh.SetNextIndex((UInt16)(vertexCount + 1));
            mesh.SetNextIndex((UInt16)(vertexCount + 2));
            mesh.SetNextIndex((UInt16)(vertexCount + 3));
            mesh.SetNextIndex((UInt16)(vertexCount + 2));
            mesh.SetNextIndex((UInt16)(vertexCount + 1));

            vertexCount += 4;
            indexCount += 6;
        }

        static void TessellateFilledFan(Vector2 center, Vector2 radius, float leftWidth, float topWidth, Color32 leftColor, Color32 topColor, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if (countOnly)
            {
                vertexCount += (UInt16)(kSubdivisions + 3);
                indexCount += (UInt16)((kSubdivisions - 1) * 3);
                return;
            }

            Color32 cornerFlags = new Color32((byte)VertexFlags.IsEdge, 0, 0, 0);
            Color32 outerFlags = new Color32((byte)VertexFlags.IsSolid, 0, 0, 0);
            var widths = new Vector2(leftWidth, topWidth);

            // Partition the triangles
            float splitAngle = GetCenteredEllipseLineIntersectionTheta(radius.x, radius.y, radius);
            int triangles = kSubdivisions - 1;
            int leftTriangles = Mathf.Clamp(Mathf.RoundToInt(splitAngle / (0.5f * Mathf.PI) * triangles), 1, triangles - 1);
            int topTriangles = triangles - leftTriangles;

            Vector2 p;
            // Tessellate left
            {
                UInt16 cornerIndex = vertexCount;
                p = new Vector2(center.x - radius.x, center.y);
                mesh.SetNextVertex(new Vertex { position = new Vector3(center.x, center.y, posZ), tint = leftColor, flags = cornerFlags, uv = widths });
                mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), tint = leftColor, flags = outerFlags });
                vertexCount += 2;

                float deltaAngle = splitAngle / leftTriangles;
                for (int i = 1; i <= leftTriangles; ++i)
                {
                    float angle = deltaAngle * i;
                    p = center - new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), tint = leftColor, flags = outerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(cornerIndex);
                    mesh.SetNextIndex((UInt16)(vertexCount - 2));
                    mesh.SetNextIndex((UInt16)(vertexCount - 1));
                    indexCount += 3;
                }
            }

            // Tessellate top
            {
                UInt16 cornerIndex = vertexCount;
                mesh.SetNextVertex(new Vertex { position = new Vector3(center.x, center.y, posZ), tint = topColor, flags = cornerFlags, uv = widths });
                mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), tint = topColor, flags = outerFlags });
                vertexCount += 2;

                float deltaAngle = (Mathf.PI * 0.5f - splitAngle) / topTriangles;
                for (int i = 1; i <= topTriangles; ++i)
                {
                    float angle = splitAngle + deltaAngle * i;
                    p = center - new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), tint = topColor, flags = outerFlags });
                    ++vertexCount;

                    mesh.SetNextIndex(cornerIndex);
                    mesh.SetNextIndex((UInt16)(vertexCount - 2));
                    mesh.SetNextIndex((UInt16)(vertexCount - 1));
                    indexCount += 3;
                }
            }
        }

        private static void TessellateFilledFan(TessellationType tessellationType, Vector2 center, Vector2 radius, float leftWidth, float topWidth, Color32 color, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if (countOnly)
            {
                vertexCount += (UInt16)(kSubdivisions + 1);
                indexCount += (UInt16)((kSubdivisions - 1) * 3);
                return;
            }

            Color32 innerVertexFlags, outerVertexFlags;
            if (tessellationType == TessellationType.EdgeCorner)
            {
                innerVertexFlags = new Color32((byte)VertexFlags.IsEdge, 0, 0, 0);
                outerVertexFlags = new Color32((byte)VertexFlags.IsSolid, 0, 0, 0);
            }
            else
            {
                outerVertexFlags = innerVertexFlags = new Color32(0, 0, 0, 0); // Primed for later update by the chain builder
            }

            var widths = new Vector2(leftWidth, topWidth);

            var p = new Vector2(center.x - radius.x, center.y);

            UInt16 indexOffset = vertexCount;

            mesh.SetNextVertex(new Vertex() { position = new Vector3(center.x, center.y, posZ), tint = color, flags = innerVertexFlags, uv = widths });
            mesh.SetNextVertex(new Vertex() { position = new Vector3(p.x, p.y, posZ), tint = color, flags = outerVertexFlags });
            vertexCount += 2;

            for (int k = 1; k < kSubdivisions; ++k)
            {
                float angle = (Mathf.PI * 0.5f) * ((float)k) / (kSubdivisions - 1);
                p = center + new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle)) * radius;
                mesh.SetNextVertex(new Vertex() { position = new Vector3(p.x, p.y, posZ), tint = color, flags = outerVertexFlags });
                vertexCount++;

                mesh.SetNextIndex((UInt16)(indexOffset + 0));
                mesh.SetNextIndex((UInt16)(indexOffset + k));
                mesh.SetNextIndex((UInt16)(indexOffset + k + 1));
                indexCount += 3;
            }

            indexOffset += (UInt16)(kSubdivisions + 1);
        }

        private static void TessellateBorderedFan(Vector2 center, Vector2 outerRadius, float leftWidth, float topWidth, Color32 leftColor, Color32 topColor, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if (countOnly)
            {
                vertexCount += (UInt16)(kSubdivisions * 2 + 2);
                indexCount += (UInt16)((kSubdivisions - 1) * 6);
                return;
            }

            Color32 innerVertexFlags = new Color32((byte)VertexFlags.IsEdge, 0, 0, 0);
            Color32 outerVertexFlags = new Color32((byte)VertexFlags.IsSolid, 0, 0, 0);
            Vector2 innerRadius = new Vector2(outerRadius.x - leftWidth, outerRadius.y - topWidth);
            var widths = new Vector2(leftWidth, topWidth);

            // Determine the inner/outer split angles
            Vector2 splitDir = new Vector2(leftWidth, topWidth);
            Vector2 outerIntersection = IntersectEllipseWithLine(outerRadius.x, outerRadius.y, splitDir);
            Vector2 innerIntersection = IntersectEllipseWithLine(innerRadius.x, innerRadius.y, splitDir);
            float outerSplitAngle = GetCenteredEllipseLineIntersectionTheta(outerRadius.x, outerRadius.y, outerRadius - outerIntersection);
            float innerSplitAngle = GetCenteredEllipseLineIntersectionTheta(innerRadius.x, innerRadius.y, innerRadius - innerIntersection);

            // Partition the quads
            float partitionAngle = 0.5f * (outerSplitAngle + innerSplitAngle);
            int quads = kSubdivisions - 1;
            int leftQuads = Mathf.Clamp(Mathf.RoundToInt(partitionAngle * (2 / Mathf.PI) * quads), 1, quads - 1);
            int topQuads = quads - leftQuads;

            // Tessellate left
            {
                float outerDeltaAngle = outerSplitAngle / leftQuads;
                float innerDeltaAngle = innerSplitAngle / leftQuads;

                var p = new Vector2(center.x - outerRadius.x, center.y);
                var q = new Vector2(center.x - innerRadius.x, center.y);

                mesh.SetNextVertex(new Vertex { position = new Vector3(q.x, q.y, posZ), tint = leftColor, flags = innerVertexFlags, uv = widths });
                mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), tint = leftColor, flags = outerVertexFlags });
                vertexCount += 2;

                for (int i = 1; i <= leftQuads; ++i)
                {
                    float outerAngle = i * outerDeltaAngle;
                    float innerAngle = i * innerDeltaAngle;
                    p = center - new Vector2(Mathf.Cos(outerAngle), Mathf.Sin(outerAngle)) * outerRadius;
                    q = center - new Vector2(Mathf.Cos(innerAngle), Mathf.Sin(innerAngle)) * innerRadius;

                    mesh.SetNextVertex(new Vertex { position = new Vector3(q.x, q.y, posZ), tint = leftColor, flags = innerVertexFlags, uv = widths });
                    mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), tint = leftColor, flags = outerVertexFlags });
                    vertexCount += 2;

                    mesh.SetNextIndex((UInt16)(vertexCount - 4));
                    mesh.SetNextIndex((UInt16)(vertexCount - 3));
                    mesh.SetNextIndex((UInt16)(vertexCount - 2));
                    mesh.SetNextIndex((UInt16)(vertexCount - 3));
                    mesh.SetNextIndex((UInt16)(vertexCount - 1));
                    mesh.SetNextIndex((UInt16)(vertexCount - 2));
                    indexCount += 6;
                }
            }

            // Tessellate top
            {
                float outerDeltaAngle = (Mathf.PI / 2 - outerSplitAngle) / topQuads;
                float innerDeltaAngle = (Mathf.PI / 2 - innerSplitAngle) / topQuads;
                innerVertexFlags = outerVertexFlags = new Color32(0, 0, 0, 0); // Primed for later update by the chain builder
                var p = center - new Vector2(Mathf.Cos(outerSplitAngle), Mathf.Sin(outerSplitAngle)) * outerRadius;
                var q = center - new Vector2(Mathf.Cos(innerSplitAngle), Mathf.Sin(innerSplitAngle)) * innerRadius;

                mesh.SetNextVertex(new Vertex { position = new Vector3(q.x, q.y, posZ), tint = topColor, flags = innerVertexFlags, uv = widths });
                mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), tint = topColor, flags = outerVertexFlags });
                vertexCount += 2;

                for (int i = 1; i <= topQuads; ++i)
                {
                    float outerAngle = outerSplitAngle + i * outerDeltaAngle;
                    float innerAngle = innerSplitAngle + i * innerDeltaAngle;
                    p = center - new Vector2(Mathf.Cos(outerAngle), Mathf.Sin(outerAngle)) * outerRadius;
                    q = center - new Vector2(Mathf.Cos(innerAngle), Mathf.Sin(innerAngle)) * innerRadius;

                    mesh.SetNextVertex(new Vertex { position = new Vector3(q.x, q.y, posZ), tint = topColor, flags = innerVertexFlags, uv = widths });
                    mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), tint = topColor, flags = outerVertexFlags });
                    vertexCount += 2;

                    mesh.SetNextIndex((UInt16)(vertexCount - 4));
                    mesh.SetNextIndex((UInt16)(vertexCount - 3));
                    mesh.SetNextIndex((UInt16)(vertexCount - 2));
                    mesh.SetNextIndex((UInt16)(vertexCount - 3));
                    mesh.SetNextIndex((UInt16)(vertexCount - 1));
                    mesh.SetNextIndex((UInt16)(vertexCount - 2));
                    indexCount += 6;
                }
            }
        }

        private static void TessellateBorderedFan(Vector2 center, Vector2 radius, float leftWidth, float topWidth, Color32 color, float posZ, MeshWriteData mesh, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if (countOnly)
            {
                vertexCount += (UInt16)(kSubdivisions * 2);
                indexCount += (UInt16)((kSubdivisions - 1) * 6);
                return;
            }

            Color32 innerVertexFlags = new Color32((byte)VertexFlags.IsEdge, 0, 0, 0);
            Color32 outerVertexFlags = new Color32((byte)VertexFlags.IsSolid, 0, 0, 0);
            var widths = new Vector2(leftWidth, topWidth);

            var a = radius.x - leftWidth;
            var b = radius.y - topWidth;
            var p = new Vector2(center.x - radius.x, center.y);
            var q = new Vector2(center.x - a, center.y);

            UInt16 indexOffset = vertexCount;

            mesh.SetNextVertex(new Vertex { position = new Vector3(q.x, q.y, posZ), tint = color, flags = innerVertexFlags, uv = widths });
            mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), tint = color, flags = outerVertexFlags });
            vertexCount += 2;

            for (int k = 1; k < kSubdivisions; ++k)
            {
                float percent = ((float)k) / (kSubdivisions - 1);
                float angle = (Mathf.PI * 0.5f) * percent;
                p = center + new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle)) * radius;
                q = center + new Vector2(-a * Mathf.Cos(angle), -b * Mathf.Sin(angle));
                mesh.SetNextVertex(new Vertex { position = new Vector3(q.x, q.y, posZ), tint = color, flags = innerVertexFlags, uv = widths });
                mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), tint = color, flags = outerVertexFlags });
                vertexCount += 2;

                int i = k * 2;
                mesh.SetNextIndex((UInt16)(indexOffset + (i - 2)));
                mesh.SetNextIndex((UInt16)(indexOffset + (i - 1)));
                mesh.SetNextIndex((UInt16)(indexOffset + (i)));
                mesh.SetNextIndex((UInt16)(indexOffset + (i - 1)));
                mesh.SetNextIndex((UInt16)(indexOffset + (i + 1)));
                mesh.SetNextIndex((UInt16)(indexOffset + (i)));
                indexCount += 6;
            }

            indexOffset += (UInt16)(kSubdivisions * 2);
        }

        private static void MirrorVertices(Rect rect, NativeSlice<Vertex> vertices, int vertexStart, int vertexCount, bool flipHorizontal)
        {
            if (flipHorizontal)
            {
                for (int i = 0; i < vertexCount; ++i)
                {
                    var vertex = vertices[vertexStart + i];
                    vertex.position.x = rect.xMax - (vertex.position.x - rect.xMax);
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
