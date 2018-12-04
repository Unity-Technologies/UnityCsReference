// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine.Profiling;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    internal static class UIRTessellation
    {
        internal static float kEpsilon = 0.001f;
        internal static UInt16 kSubdivisions = 6;

        ///<summary>
        /// Tessellates the border OR the content:
        /// If any of the left/right/top/bottom border parameters is greater than Epsilon, we tessellate ONLY a border.
        /// Otherwise we tessellate ONLY the content.
        /// </summary>
        /// <param name="vertexFlags">Flags are only used for content, not for a border.</param>
        public static MeshHandle TessellateBorderedRect(IUIRenderDevice device, Matrix4x4 transform, MeshHandle oldMeshHandle, Rect rect, Rect texCoords, Color color, BorderParameters border, uint transformID, uint clippingID, VertexFlags vertexFlags)
        {
            if (rect.width < kEpsilon || rect.height < kEpsilon)
                return null;

            Profiler.BeginSample("TessellateBorderedRect");

            border = AdjustBorderParameters(rect, border);

            UInt16 vertexCount = 0, indexCount = 0;
            CountBorderedRectTriangles(rect, border, ref vertexCount, ref indexCount);

            NativeSlice<Vertex> vertices;
            NativeSlice<UInt16> indices;
            UInt16 indexOffset;
            var handle = device.Allocate(vertexCount, indexCount, out vertices, out indices, out indexOffset);

            vertexCount = 0;
            indexCount = 0;
            TessellateBorderedRectInternal(rect, color, transformID, clippingID, vertexFlags, border, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount);
            if (!HasBorder(border) &&
                (VertexFlagsUtil.TypeIsEqual(vertexFlags, VertexFlags.IsTextured) ||
                 VertexFlagsUtil.TypeIsEqual(vertexFlags, VertexFlags.IsCustom)))
            {
                ComputeUVs(rect, texCoords, vertices);
            }

            // Apply the transform, but keep the Z intact
            for (int i = 0; i < vertices.Length; ++i)
            {
                var v = vertices[i];
                var z = v.position.z;
                v.position = transform.MultiplyPoint3x4(v.position);
                v.position.z = z;
                vertices[i] = v;
            }

            Profiler.EndSample();

            return handle;
        }

        private static void CountBorderedRectTriangles(Rect rect, BorderParameters border, ref UInt16 vertexCount, ref UInt16 indexCount)
        {
            // To count the required triangles, we call the tessellation method with a "countOnly=true" flag, which skips
            // tessellation and only update the vertex and index counts.

            UInt16 indexOffset = 0; // Won't be used
            TessellateBorderedRectInternal(rect, Color.white, 0, 0, VertexFlags.IsSolid, border, null, null, ref indexOffset, ref vertexCount, ref indexCount, true);
        }

        private static void TessellateBorderedRectInternal(Rect rect, Color color, uint transformID, uint clippingID, VertexFlags vertexFlags, BorderParameters border, NativeSlice<Vertex>? vertices, NativeSlice<UInt16>? indices, ref UInt16 indexOffset, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly = false)
        {
            if (IsSimpleRectangle(border))
            {
                TessellateQuad(rect, TessellationType.Content, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            }
            else
            {
                TessellateRoundBorders(rect, color, transformID, clippingID, vertexFlags, border, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            }
        }

        private static void TessellateRoundBorders(Rect rect, Color color, uint transformID, uint clippingID, VertexFlags vertexFlags, BorderParameters border, NativeSlice<Vertex>? vertices, NativeSlice<UInt16>? indices, ref UInt16 indexOffset, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            // To tessellate the 4 corners, the rect is divided in 4 quadrants and every quadrant is computed as if they were the top-left corner.
            // The quadrants are then mirrored and the triangles winding flipped as necessary.
            UInt16 startVc = 0, startIc = 0;
            bool hasBorder = HasBorder(border);
            var halfSize = new Vector2(rect.width / 2.0f, rect.height / 2.0f);
            var quarterRect = new Rect(rect.x, rect.y, halfSize.x, halfSize.y);

            // Top-left
            TessellateRoundedCorner(quarterRect, color, transformID, clippingID, vertexFlags, border.topLeftRadius, border.leftWidth, border.topWidth, hasBorder, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);

            // Top-right
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedCorner(quarterRect, color, transformID, clippingID, vertexFlags, border.topRightRadius, border.rightWidth, border.topWidth, hasBorder, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            MirrorVertices(quarterRect, vertices, startVc, vertexCount - startVc, true);
            FlipWinding(indices, startIc, indexCount - startIc);

            // Bottom-right
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedCorner(quarterRect, color, transformID, clippingID, vertexFlags, border.bottomRightRadius, border.rightWidth, border.bottomWidth, hasBorder, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            MirrorVertices(quarterRect, vertices, startVc, vertexCount - startVc, true);
            MirrorVertices(quarterRect, vertices, startVc, vertexCount - startVc, false);

            // Bottom-left
            startVc = vertexCount;
            startIc = indexCount;
            TessellateRoundedCorner(quarterRect, color, transformID, clippingID, vertexFlags, border.bottomLeftRadius, border.leftWidth, border.bottomWidth, hasBorder, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            MirrorVertices(quarterRect, vertices, startVc, vertexCount - startVc, false);
            FlipWinding(indices, startIc, indexCount - startIc);
        }

        private static void TessellateRoundedCorner(Rect rect, Color color, uint transformID, uint clippingID, VertexFlags vertexFlags, float radius, float leftWidth, float topWidth, bool hasBorder, NativeSlice<Vertex>? vertices, NativeSlice<UInt16>? indices, ref UInt16 indexOffset, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            var cornerCenter = rect.position + new Vector2(radius, radius);
            var subRect = Rect.zero;

            if (radius < kEpsilon)
            {
                if (!hasBorder)
                {
                    // Without radius and no borders, we use a single quad to fill the section
                    // -------
                    // |     |
                    // |     |
                    // -------
                    TessellateQuad(rect, TessellationType.Content, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                }
                else
                {
                    // Without radius with borders, we use two quads for the outlines
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
                        TessellateQuad(subRect, TessellationType.EdgeVertical, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                    }
                    if (topWidth > kEpsilon)
                    {
                        // B
                        subRect = new Rect(rect.x + leftWidth, rect.y, rect.width - leftWidth, topWidth);
                        TessellateQuad(subRect, TessellationType.EdgeHorizontal, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                    }
                }
                return;
            }

            if (!hasBorder)
            {
                // Without borders, we can just fill the corner with radius, plus up to 2 quads to fill the remainder of the section
                //   _-------
                //  *\ |    |
                // *__\| A  |
                // |   |    |
                // | B |    |
                // |   |    |
                // ----------
                TessellateFilledFan(TessellationType.Content, cornerCenter, radius, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                if (radius < rect.width)
                {
                    // A
                    subRect = new Rect(rect.x + radius, rect.y, rect.width - radius, rect.height);
                    TessellateQuad(subRect, TessellationType.Content, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                }
                if (radius < rect.height)
                {
                    // B
                    subRect = new Rect(rect.x, rect.y + radius, radius < rect.width ? radius : rect.width, rect.height - radius);
                    TessellateQuad(subRect, TessellationType.Content, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                }
            }
            else
            {
                // With borders, we have to create a fan-shaped outline, plus up to 2 quads for the straight outlines
                // If the radius is smaller than the border width, we create a filled fan plus the required quads instead.
                if (radius < leftWidth || radius < topWidth)
                {
                    // A
                    TessellateFilledFan(TessellationType.EdgeCorner, cornerCenter, radius, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                    if (radius < leftWidth && radius < topWidth)
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
                        subRect = new Rect(rect.x, rect.y + radius, leftWidth, topWidth - radius);
                        TessellateQuad(subRect, TessellationType.EdgeCorner, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);

                        // C
                        subRect = new Rect(rect.x + radius, rect.y, leftWidth - radius, radius);
                        TessellateQuad(subRect, TessellationType.EdgeCorner, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                    }
                    else if (radius < leftWidth)
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
                        subRect = new Rect(rect.x + radius, rect.y, leftWidth - radius, Mathf.Max(radius, topWidth));
                        TessellateQuad(subRect, TessellationType.EdgeCorner, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
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
                        subRect = new Rect(rect.x, rect.y + radius, Mathf.Max(radius, leftWidth), topWidth - radius);
                        TessellateQuad(subRect, TessellationType.EdgeCorner, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
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
                    TessellateBorderedFan(TessellationType.EdgeCorner, cornerCenter, radius, leftWidth, topWidth, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
                }

                // Tessellate the straight outlines
                // E
                float cornerSize = Mathf.Max(radius, topWidth);
                subRect = new Rect(rect.x, rect.y + cornerSize, leftWidth, rect.height - cornerSize);
                TessellateQuad(subRect, TessellationType.EdgeVertical, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);

                // F
                cornerSize = Mathf.Max(radius, leftWidth);
                subRect = new Rect(rect.x + cornerSize, rect.y, rect.width - cornerSize, topWidth);
                TessellateQuad(subRect, TessellationType.EdgeHorizontal, color, transformID, clippingID, vertexFlags, vertices, indices, ref indexOffset, ref vertexCount, ref indexCount, countOnly);
            }
        }

        enum TessellationType { EdgeHorizontal, EdgeVertical, EdgeCorner, Content }

        private static void TessellateQuad(Rect rect, TessellationType tessellationType, Color color, uint transformID, uint clippingID, VertexFlags vertexFlags, NativeSlice<Vertex>? vertices, NativeSlice<UInt16>? indices, ref UInt16 indexOffset, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
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
                    uv0 = new Vector2(x0, y3);
                    uv1 = new Vector2(x3, y3);
                    uv2 = new Vector2(x0, y0);
                    uv3 = new Vector2(x3, y0);
                    flags0 = flags1 = (float)VertexFlags.IsSolid;
                    flags2 = flags3 = (float)VertexFlags.IsEdge;
                    break;
                case TessellationType.EdgeVertical:
                    uv0 = new Vector2(x3, y0);
                    uv1 = new Vector2(x0, y0);
                    uv2 = new Vector2(x3, y3);
                    uv3 = new Vector2(x0, y3);
                    flags0 = flags2 = (float)VertexFlags.IsSolid;
                    flags1 = flags3 = (float)VertexFlags.IsEdge;
                    break;
                case TessellationType.EdgeCorner:
                    uv0 = uv1 = uv2 = uv3 = Vector2.zero;
                    flags0 = flags1 = flags2 = flags3 = (float)VertexFlags.IsSolid;
                    break;
                case TessellationType.Content:
                    uv0 = uv1 = uv2 = uv3 = Vector2.zero; // UVs are computed later for content
                    flags0 = flags1 = flags2 = flags3 = (float)vertexFlags;
                    break;
                default:
                    throw new NotImplementedException();
            }

            var verts = vertices.GetValueOrDefault();
            verts[vertexCount++] = new Vertex { position = new Vector3(x0, y0, UIR.MeshRenderer.k_PosZ), uv = uv0, tint = color, transformID = transformID, clippingID = clippingID, flags = flags0 };
            verts[vertexCount++] = new Vertex { position = new Vector3(x3, y0, UIR.MeshRenderer.k_PosZ), uv = uv1, tint = color, transformID = transformID, clippingID = clippingID, flags = flags1 };
            verts[vertexCount++] = new Vertex { position = new Vector3(x0, y3, UIR.MeshRenderer.k_PosZ), uv = uv2, tint = color, transformID = transformID, clippingID = clippingID, flags = flags2 };
            verts[vertexCount++] = new Vertex { position = new Vector3(x3, y3, UIR.MeshRenderer.k_PosZ), uv = uv3, tint = color, transformID = transformID, clippingID = clippingID, flags = flags3 };

            var inds = indices.GetValueOrDefault();
            inds[indexCount++] = (UInt16)(indexOffset + 0);
            inds[indexCount++] = (UInt16)(indexOffset + 2);
            inds[indexCount++] = (UInt16)(indexOffset + 1);
            inds[indexCount++] = (UInt16)(indexOffset + 3);
            inds[indexCount++] = (UInt16)(indexOffset + 1);
            inds[indexCount++] = (UInt16)(indexOffset + 2);

            indexOffset += 4;
        }

        private static void TessellateFilledFan(TessellationType tessellationType, Vector2 center, float radius, Color color, uint transformID, uint clippingID, VertexFlags vertexFlags, NativeSlice<Vertex>? vertices, NativeSlice<UInt16>? indices, ref UInt16 indexOffset, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            VertexFlags innerVertexFlags, outerVertexFlags;
            if (tessellationType == TessellationType.EdgeCorner)
            {
                innerVertexFlags = VertexFlags.IsEdge;
                outerVertexFlags = VertexFlags.IsSolid;
            }
            else
            {
                innerVertexFlags = vertexFlags;
                outerVertexFlags = vertexFlags;
            }

            if (countOnly)
            {
                vertexCount += (UInt16)(kSubdivisions + 1);
                indexCount += (UInt16)((kSubdivisions - 1) * 3);
                return;
            }

            var p = new Vector2(center.x - radius, center.y);

            var verts = vertices.GetValueOrDefault();
            var inds = indices.GetValueOrDefault();

            verts[vertexCount++] = new Vertex() { position = new Vector3(center.x, center.y, UIR.MeshRenderer.k_PosZ), uv = p, tint = color, transformID = transformID, clippingID = clippingID, flags = (float)innerVertexFlags };
            verts[vertexCount++] = new Vertex() { position = new Vector3(p.x, p.y, UIR.MeshRenderer.k_PosZ), uv = center, tint = color, transformID = transformID, clippingID = clippingID, flags = (float)outerVertexFlags };

            for (int k = 1; k < kSubdivisions; ++k)
            {
                float angle = (Mathf.PI / 2.0f) * ((float)k) / (kSubdivisions - 1);
                p = center + new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle)) * radius;
                verts[vertexCount++] = new Vertex() { position = new Vector3(p.x, p.y, UIR.MeshRenderer.k_PosZ), uv = center, tint = color, transformID = transformID, clippingID = clippingID, flags = (float)outerVertexFlags };

                inds[indexCount++] = (UInt16)(indexOffset + 0);
                inds[indexCount++] = (UInt16)(indexOffset + k + 1);
                inds[indexCount++] = (UInt16)(indexOffset + k);
            }

            indexOffset += (UInt16)(kSubdivisions + 1);
        }

        private static void TessellateBorderedFan(TessellationType tessellationType, Vector2 center, float radius, float leftWidth, float topWidth, Color color, uint transformID, uint clippingID, VertexFlags vertexFlags, NativeSlice<Vertex>? vertices, NativeSlice<UInt16>? indices, ref UInt16 indexOffset, ref UInt16 vertexCount, ref UInt16 indexCount, bool countOnly)
        {
            VertexFlags innerVertexFlags, outerVertexFlags;
            if (tessellationType == TessellationType.EdgeCorner)
            {
                innerVertexFlags = VertexFlags.IsEdge;
                outerVertexFlags = VertexFlags.IsSolid;
            }
            else
            {
                innerVertexFlags = vertexFlags;
                outerVertexFlags = vertexFlags;
            }

            if (countOnly)
            {
                vertexCount += (UInt16)(kSubdivisions * 2);
                indexCount += (UInt16)((kSubdivisions - 1) * 6);
                return;
            }

            var a = radius - leftWidth;
            var b = radius - topWidth;
            var p = new Vector2(center.x - radius, center.y);
            var q = new Vector2(center.x - a, center.y);

            var verts = vertices.GetValueOrDefault();
            var inds = indices.GetValueOrDefault();

            verts[vertexCount++] = new Vertex { position = new Vector3(q.x, q.y, UIR.MeshRenderer.k_PosZ), uv = p, tint = color, transformID = transformID, clippingID = clippingID, flags = (float)innerVertexFlags };
            verts[vertexCount++] = new Vertex { position = new Vector3(p.x, p.y, UIR.MeshRenderer.k_PosZ), uv = q, tint = color, transformID = transformID, clippingID = clippingID, flags = (float)outerVertexFlags };

            for (int k = 1; k < kSubdivisions; ++k)
            {
                float percent = ((float)k) / (kSubdivisions - 1);
                float angle = (Mathf.PI / 2.0f) * percent;
                p = center + new Vector2(-Mathf.Cos(angle), -Mathf.Sin(angle)) * radius;
                q = center + new Vector2(-a * Mathf.Cos(angle), -b * Mathf.Sin(angle));
                verts[vertexCount++] = new Vertex { position = new Vector3(q.x, q.y, UIR.MeshRenderer.k_PosZ), uv = p, tint = color, transformID = transformID, clippingID = clippingID, flags = (float)innerVertexFlags };
                verts[vertexCount++] = new Vertex { position = new Vector3(p.x, p.y, UIR.MeshRenderer.k_PosZ), uv = q, tint = color, transformID = transformID, clippingID = clippingID, flags = (float)outerVertexFlags };

                int i = k * 2;
                inds[indexCount++] = (UInt16)(indexOffset + (i - 2));
                inds[indexCount++] = (UInt16)(indexOffset + (i));
                inds[indexCount++] = (UInt16)(indexOffset + (i - 1));
                inds[indexCount++] = (UInt16)(indexOffset + (i - 1));
                inds[indexCount++] = (UInt16)(indexOffset + (i));
                inds[indexCount++] = (UInt16)(indexOffset + (i + 1));
            }

            indexOffset += (UInt16)(kSubdivisions * 2);
        }

        private static void MirrorVertices(Rect rect, NativeSlice<Vertex>? vertices, int vertexStart, int vertexCount, bool flipHorizontal)
        {
            if (!vertices.HasValue)
                return;

            var verts = vertices.GetValueOrDefault();
            var center = new Vector2(rect.xMax, rect.yMax);
            if (flipHorizontal)
            {
                for (int i = 0; i < vertexCount; ++i)
                {
                    var vertex = verts[vertexStart + i];
                    center.y = vertex.position.y;
                    vertex.position.x = center.x - (vertex.position.x - center.x);
                    vertex.position.y = center.y - (vertex.position.y - center.y);
                    center.y = vertex.uv.y;
                    vertex.uv.x = center.x - (vertex.uv.x - center.x);
                    vertex.uv.y = center.y - (vertex.uv.y - center.y);
                    verts[vertexStart + i] = vertex;
                }
            }
            else
            {
                for (int i = 0; i < vertexCount; ++i)
                {
                    var vertex = verts[vertexStart + i];
                    center.x = vertex.position.x;
                    vertex.position.x = center.x - (vertex.position.x - center.x);
                    vertex.position.y = center.y - (vertex.position.y - center.y);
                    center.x = vertex.uv.x;
                    vertex.uv.x = center.x - (vertex.uv.x - center.x);
                    vertex.uv.y = center.y - (vertex.uv.y - center.y);
                    verts[vertexStart + i] = vertex;
                }
            }
        }

        private static void FlipWinding(NativeSlice<UInt16>? indices, int indexStart, int indexCount)
        {
            if (!indices.HasValue)
                return;

            var inds = indices.GetValueOrDefault();
            for (int i = 0; i < indexCount; i += 3)
            {
                UInt16 tmp = inds[indexStart + i];
                inds[indexStart + i] = inds[indexStart + i + 1];
                inds[indexStart + i + 1] = tmp;
            }
        }

        private static void ComputeUVs(Rect tessellatedRect, Rect textureRect, NativeSlice<Vertex> vertices)
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
                uv.x = Mathf.Lerp(textureRect.xMin, textureRect.xMax, uv.x);
                uv.y = Mathf.Lerp(textureRect.yMin, textureRect.yMax, 1 - uv.y);

                vertex.uv = uv;
                vertices[i] = vertex;
            }
        }

        private static bool HasBorder(BorderParameters border)
        {
            return
                border.leftWidth > kEpsilon ||
                border.topWidth > kEpsilon ||
                border.rightWidth > kEpsilon ||
                border.bottomWidth > kEpsilon;
        }

        private static bool IsSimpleRectangle(BorderParameters border)
        {
            return
                border.leftWidth < kEpsilon &&
                border.topWidth < kEpsilon &&
                border.rightWidth < kEpsilon &&
                border.bottomWidth < kEpsilon &&
                border.topLeftRadius < kEpsilon &&
                border.topRightRadius < kEpsilon &&
                border.bottomRightRadius < kEpsilon &&
                border.bottomLeftRadius < kEpsilon;
        }

        private static BorderParameters AdjustBorderParameters(Rect rect, BorderParameters border)
        {
            var halfSize = new Vector2(rect.width / 2.0f, rect.height / 2.0f);
            var minHalfSize = Mathf.Min(halfSize.x, halfSize.y);
            return new BorderParameters() {
                leftWidth = Mathf.Min(border.leftWidth, halfSize.x),
                topWidth = Mathf.Min(border.topWidth, halfSize.y),
                rightWidth = Mathf.Min(border.rightWidth, halfSize.x),
                bottomWidth = Mathf.Min(border.bottomWidth, halfSize.y),
                topLeftRadius = Mathf.Min(border.topLeftRadius, minHalfSize),
                topRightRadius = Mathf.Min(border.topRightRadius, minHalfSize),
                bottomRightRadius = Mathf.Min(border.bottomRightRadius, minHalfSize),
                bottomLeftRadius = Mathf.Min(border.bottomLeftRadius, minHalfSize),
            };
        }
    }
}
