// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine.Profiling;

namespace UnityEngine.UIElements.UIR
{
    internal static class Tessellation
    {
        internal static float kEpsilon = 0.001f;
        internal static UInt16 kSubdivisions = 6;

        ///<summary>
        /// Tessellates the border OR the content:
        /// If any of the left/right/top/bottom border parameters is greater than Epsilon, we tessellate ONLY a border.
        /// Otherwise we tessellate ONLY the content.
        /// </summary>
        /// <param name="vertexFlags">Flags are only used for content, not for a border.</param>
        public static void TessellateRect(MeshGenerationContextUtils.RectangleParams rectParams, float posZ, MeshBuilder.AllocMeshData meshAlloc)
        {
            if (rectParams.rect.width < kEpsilon || rectParams.rect.height < kEpsilon)
                return;

            Profiler.BeginSample("TessellateRect");

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
            if ((mesh.m_Flags == VertexFlags.IsAtlasTexturedPoint) || (mesh.m_Flags == VertexFlags.IsAtlasTexturedBilinear) || (mesh.m_Flags == VertexFlags.IsCustomTextured))
            {
                ComputeUVs(rectParams.rect, rectParams.uv, mesh.uvRegion, mesh.m_Vertices);
            }
            Debug.Assert(vertexCount == mesh.vertexCount);
            Debug.Assert(indexCount == mesh.indexCount);

            Profiler.EndSample();
        }

        public static void TessellateBorder(MeshGenerationContextUtils.BorderParams borderParams, float posZ, MeshBuilder.AllocMeshData meshAlloc)
        {
            if (borderParams.rect.width < kEpsilon || borderParams.rect.height < kEpsilon)
                return;

            Profiler.BeginSample("TessellateBorder");

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
            Profiler.EndSample();
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
                UInt16 indexOffset = 0;
                TessellateQuad(rectParams.rect, TessellationType.Content, rectParams.color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
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
            UInt16 indexOffset = 0;
            UInt16 startVc = 0, startIc = 0;
            var halfSize = new Vector2(rectParams.rect.width * 0.5f, rectParams.rect.height * 0.5f);
            var quarterRect = new Rect(rectParams.rect.x, rectParams.rect.y, halfSize.x, halfSize.y);

            // Top-left
            TessellateRoundedCorner(quarterRect, rectParams.color, posZ, rectParams.topLeftRadius, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);

            // Top-right
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedCorner(quarterRect, rectParams.color, posZ, rectParams.topRightRadius, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            if (!countOnly)
            {
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, true);
                FlipWinding(mesh.m_Indices, startIc, indexCount - startIc);
            }

            // Bottom-right
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedCorner(quarterRect, rectParams.color, posZ, rectParams.bottomRightRadius, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            if (!countOnly)
            {
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, true);
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, false);
            }

            // Bottom-left
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedCorner(quarterRect, rectParams.color, posZ, rectParams.bottomLeftRadius, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
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
            UInt16 indexOffset = 0;
            UInt16 startVc = 0, startIc = 0;
            var halfSize = new Vector2(border.rect.width * 0.5f, border.rect.height * 0.5f);
            var quarterRect = new Rect(border.rect.x, border.rect.y, halfSize.x, halfSize.y);

            // Top-left
            TessellateRoundedBorder(quarterRect, border.color, posZ, border.topLeftRadius, border.leftWidth, border.topWidth, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);

            // Top-right
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedBorder(quarterRect, border.color, posZ, border.topRightRadius, border.rightWidth, border.topWidth, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            if (!countOnly)
            {
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, true);
                FlipWinding(mesh.m_Indices, startIc, indexCount - startIc);
            }

            // Bottom-right
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedBorder(quarterRect, border.color, posZ, border.bottomRightRadius, border.rightWidth, border.bottomWidth, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            if (!countOnly)
            {
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, true);
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, false);
            }

            // Bottom-left
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedBorder(quarterRect, border.color, posZ, border.bottomLeftRadius, border.leftWidth, border.bottomWidth, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            if (!countOnly)
            {
                MirrorVertices(quarterRect, mesh.m_Vertices, startVc, vertexCount - startVc, false);
                FlipWinding(mesh.m_Indices, startIc, indexCount - startIc);
            }
        }

        private static void TessellateRoundedCorner(Rect rect, Color color, float posZ, Vector2 radius, MeshWriteData mesh, ref UInt16 indexOffset, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
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
                TessellateQuad(rect, TessellationType.Content, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
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
            TessellateFilledFan(TessellationType.Content, cornerCenter, radius, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            if (radius.x < rect.width)
            {
                // A
                subRect = new Rect(rect.x + radius.x, rect.y, rect.width - radius.x, rect.height);
                TessellateQuad(subRect, TessellationType.Content, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            }
            if (radius.y < rect.height)
            {
                // B
                subRect = new Rect(rect.x, rect.y + radius.y, radius.x < rect.width ? radius.x : rect.width, rect.height - radius.y);
                TessellateQuad(subRect, TessellationType.Content, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            }
        }

        private static void TessellateRoundedBorder(Rect rect, Color color, float posZ, Vector2 radius, float leftWidth, float topWidth, MeshWriteData mesh, ref UInt16 indexOffset, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            var cornerCenter = rect.position + radius;
            var subRect = Rect.zero;

            if (radius == Vector2.zero)
            {
                // Without radius, we use two quads for the outlines
                // ------------
                // |   |  B   |
                // | A |-------
                // |   |
                // |   |
                // -----
                if (leftWidth > kEpsilon)
                {
                    // A
                    subRect = new Rect(rect.x, rect.y, leftWidth, rect.height);
                    TessellateQuad(subRect, TessellationType.EdgeVertical, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                }
                if (topWidth > kEpsilon)
                {
                    // B
                    subRect = new Rect(rect.x + leftWidth, rect.y, rect.width - leftWidth, topWidth);
                    TessellateQuad(subRect, TessellationType.EdgeHorizontal, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                }
                return;
            }

            // With radius, we have to create a fan-shaped outline, plus up to 2 quads for the straight outlines
            // If the radius is smaller than the border width, we create a filled fan plus the required quads instead.
            if (radius.x < leftWidth || radius.y < topWidth)
            {
                // A
                TessellateFilledFan(TessellationType.EdgeCorner, cornerCenter, radius, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                if (radius.x < leftWidth && radius.y < topWidth)
                {
                    //      _______________
                    //   -*  |  |          |
                    //  * A\ | C|          |
                    // *____\|__|    F     |
                    // |    B   |          |
                    // |________|__________|
                    // |        |
                    // |   E    |
                    // |        |
                    // |________|

                    // B
                    subRect = new Rect(rect.x, rect.y + radius.y, leftWidth, topWidth - radius.x);
                    TessellateQuad(subRect, TessellationType.EdgeCorner, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);

                    // C
                    subRect = new Rect(rect.x + radius.x, rect.y, leftWidth - radius.x, radius.y);
                    TessellateQuad(subRect, TessellationType.EdgeCorner, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                }
                else if (radius.x < leftWidth)
                {
                    //      ____________
                    //   -*  |  |  F    |
                    //  * A\ | B|_______|
                    // *____\|__|
                    // |        |
                    // |        |
                    // |   E    |
                    // |        |
                    // |        |
                    // |________|

                    // B
                    subRect = new Rect(rect.x + radius.x, rect.y, leftWidth - radius.x, Mathf.Max(radius.y, topWidth));
                    TessellateQuad(subRect, TessellationType.EdgeCorner, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                }
                else
                {
                    //      ____________
                    //   -*  |          |
                    //  * A\ |          |
                    // *____\|    F     |
                    // |  B  |          |
                    // |_____|__________|
                    // |  |
                    // |E |
                    // |  |
                    // |__|

                    // B
                    subRect = new Rect(rect.x, rect.y + radius.y, Mathf.Max(radius.x, leftWidth), topWidth - radius.y);
                    TessellateQuad(subRect, TessellationType.EdgeCorner, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                }
            }
            else
            {
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
                TessellateBorderedFan(TessellationType.EdgeCorner, cornerCenter, radius, leftWidth, topWidth, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            }

            // Tessellate the straight outlines
            // E
            float cornerSize = Mathf.Max(radius.y, topWidth);
            subRect = new Rect(rect.x, rect.y + cornerSize, leftWidth, rect.height - cornerSize);
            TessellateQuad(subRect, TessellationType.EdgeVertical, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);

            // F
            cornerSize = Mathf.Max(radius.x, leftWidth);
            subRect = new Rect(rect.x + cornerSize, rect.y, rect.width - cornerSize, topWidth);
            TessellateQuad(subRect, TessellationType.EdgeHorizontal, color, posZ, mesh, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
        }

        enum TessellationType { EdgeHorizontal, EdgeVertical, EdgeCorner, Content }

        private static void TessellateQuad(Rect rect, TessellationType tessellationType, Color color, float posZ, MeshWriteData mesh, ref UInt16 indexOffset, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if (rect.width < kEpsilon || rect.height < kEpsilon)
                return;

            if (countOnly)
            {
                vertexCount += 4;
                indexCount += 6;
                return;
            }

            float x0 = rect.x;
            float x3 = rect.xMax;
            float y0 = rect.y;
            float y3 = rect.yMax;

            Vector2 uv0, uv1, uv2, uv3;
            float flags0, flags1, flags2, flags3;
            switch (tessellationType)
            {
                case TessellationType.EdgeHorizontal:
                    // The uvs contain the displacement from the vertically opposed corner.
                    uv0 = new Vector2(0, y0 - y3);
                    uv1 = new Vector2(0, y0 - y3);
                    uv2 = new Vector2(0, y3 - y0);
                    uv3 = new Vector2(0, y3 - y0);
                    flags0 = flags1 = (float)VertexFlags.IsSolid;
                    flags2 = flags3 = (float)VertexFlags.IsEdge;
                    break;
                case TessellationType.EdgeVertical:
                    // The uvs contain the displacement from the horizontally opposed corner.
                    uv0 = new Vector2(x0 - x3, 0);
                    uv1 = new Vector2(x3 - x0, 0);
                    uv2 = new Vector2(x0 - x3, 0);
                    uv3 = new Vector2(x3 - x0, 0);
                    flags0 = flags2 = (float)VertexFlags.IsSolid;
                    flags1 = flags3 = (float)VertexFlags.IsEdge;
                    break;
                case TessellationType.EdgeCorner:
                    uv0 = uv1 = uv2 = uv3 = Vector2.zero;
                    flags0 = flags1 = flags2 = flags3 = (float)VertexFlags.IsSolid;
                    break;
                case TessellationType.Content:
                    uv0 = uv1 = uv2 = uv3 = Vector2.zero; // UVs are computed later for content
                    flags0 = flags1 = flags2 = flags3 = (float)mesh.m_Flags;
                    break;
                default:
                    throw new NotImplementedException();
            }

            mesh.SetNextVertex(new Vertex { position = new Vector3(x0, y0, posZ), uv = uv0, tint = color, flags = flags0 });
            mesh.SetNextVertex(new Vertex { position = new Vector3(x3, y0, posZ), uv = uv1, tint = color, flags = flags1 });
            mesh.SetNextVertex(new Vertex { position = new Vector3(x0, y3, posZ), uv = uv2, tint = color, flags = flags2 });
            mesh.SetNextVertex(new Vertex { position = new Vector3(x3, y3, posZ), uv = uv3, tint = color, flags = flags3 });

            mesh.SetNextIndex((UInt16)(indexOffset + 0));
            mesh.SetNextIndex((UInt16)(indexOffset + 2));
            mesh.SetNextIndex((UInt16)(indexOffset + 1));
            mesh.SetNextIndex((UInt16)(indexOffset + 3));
            mesh.SetNextIndex((UInt16)(indexOffset + 1));
            mesh.SetNextIndex((UInt16)(indexOffset + 2));

            vertexCount += 4;
            indexCount += 6;
            indexOffset += 4;
        }

        private static void TessellateFilledFan(TessellationType tessellationType, Vector2 center, Vector2 radius, Color color, float posZ, MeshWriteData mesh, ref UInt16 indexOffset, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if (countOnly)
            {
                vertexCount += (UInt16)(kSubdivisions + 1);
                indexCount += (UInt16)((kSubdivisions - 1) * 3);
                return;
            }

            float innerVertexFlags, outerVertexFlags;
            if (tessellationType == TessellationType.EdgeCorner)
            {
                innerVertexFlags = (float)VertexFlags.IsEdge;
                outerVertexFlags = (float)VertexFlags.IsSolid;
            }
            else
            {
                outerVertexFlags = innerVertexFlags = (float)mesh.m_Flags;
            }

            var p = new Vector2(center.x - radius.x, center.y);

            mesh.SetNextVertex(new Vertex() { position = new Vector3(center.x, center.y, posZ), uv = p, tint = color, flags = innerVertexFlags });
            mesh.SetNextVertex(new Vertex() { position = new Vector3(p.x, p.y, posZ), uv = center, tint = color, flags = outerVertexFlags });
            vertexCount += 2;

            for (int k = 1; k < kSubdivisions; ++k)
            {
                float angle = (Mathf.PI * 0.5f) * ((float)k) / (kSubdivisions - 1);
                p = center + new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle)) * radius;
                mesh.SetNextVertex(new Vertex() { position = new Vector3(p.x, p.y, posZ), uv = center, tint = color, flags = outerVertexFlags });
                vertexCount++;

                mesh.SetNextIndex((UInt16)(indexOffset + 0));
                mesh.SetNextIndex((UInt16)(indexOffset + k + 1));
                mesh.SetNextIndex((UInt16)(indexOffset + k));
                indexCount += 3;
            }

            indexOffset += (UInt16)(kSubdivisions + 1);
        }

        private static void TessellateBorderedFan(TessellationType tessellationType, Vector2 center, Vector2 radius, float leftWidth, float topWidth, Color color, float posZ, MeshWriteData mesh, ref UInt16 indexOffset, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            if (countOnly)
            {
                vertexCount += (UInt16)(kSubdivisions * 2);
                indexCount += (UInt16)((kSubdivisions - 1) * 6);
                return;
            }

            float innerVertexFlags, outerVertexFlags;
            if (tessellationType == TessellationType.EdgeCorner)
            {
                innerVertexFlags = (float)VertexFlags.IsEdge;
                outerVertexFlags = (float)VertexFlags.IsSolid;
            }
            else
            {
                innerVertexFlags = outerVertexFlags = (float)mesh.m_Flags;
            }

            var a = radius.x - leftWidth;
            var b = radius.y - topWidth;
            var p = new Vector2(center.x - radius.x, center.y);
            var q = new Vector2(center.x - a, center.y);

            mesh.SetNextVertex(new Vertex { position = new Vector3(q.x, q.y, posZ), uv = q - p, tint = color, flags = innerVertexFlags });
            mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), uv = p - q, tint = color, flags = outerVertexFlags });
            vertexCount += 2;

            for (int k = 1; k < kSubdivisions; ++k)
            {
                float percent = ((float)k) / (kSubdivisions - 1);
                float angle = (Mathf.PI * 0.5f) * percent;
                p = center + new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle)) * radius;
                q = center + new Vector2(-a * Mathf.Cos(angle), -b * Mathf.Sin(angle));
                mesh.SetNextVertex(new Vertex { position = new Vector3(q.x, q.y, posZ), uv = q - p, tint = color, flags = innerVertexFlags });
                mesh.SetNextVertex(new Vertex { position = new Vector3(p.x, p.y, posZ), uv = p - q, tint = color, flags = outerVertexFlags });
                vertexCount += 2;

                int i = k * 2;
                mesh.SetNextIndex((UInt16)(indexOffset + (i - 2)));
                mesh.SetNextIndex((UInt16)(indexOffset + (i)));
                mesh.SetNextIndex((UInt16)(indexOffset + (i - 1)));
                mesh.SetNextIndex((UInt16)(indexOffset + (i - 1)));
                mesh.SetNextIndex((UInt16)(indexOffset + (i)));
                mesh.SetNextIndex((UInt16)(indexOffset + (i + 1)));
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
