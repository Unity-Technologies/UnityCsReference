// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Utility class that allocates a <c>UIRenderDevice</c> mesh and stores data that comes from a mesh that it
    /// builds according to the provided settings, or from an existing mesh.
    /// </summary>
    internal static class UIRMeshBuilder
    {
        internal static MeshHandle MakeRectMeshHandle(IUIRenderDevice uir, RectStylePainterParameters rectParams, Matrix4x4 transform, MeshHandle oldMeshHandle, uint transformID, uint clippingID)
        {
            if (IsSimpleRect(rectParams.border))
            {
                return MakeQuad(uir, transform, oldMeshHandle, rectParams.rect, Rect.zero, rectParams.color, transformID, clippingID, VertexFlags.IsSolid);
            }
            else
            {
                return UIRTessellation.TessellateBorderedRect(uir, transform, oldMeshHandle, rectParams.rect, Rect.zero, rectParams.color, rectParams.border, transformID, clippingID, VertexFlags.IsSolid);
            }
        }

        internal static MeshHandle MakeTextureMeshHandle(IUIRenderDevice uir, TextureStylePainterParameters textureParams, Matrix4x4 transform, MeshHandle oldMeshHandle, uint transformID, uint clippingID, VertexFlags vertexFlags)
        {
            if (textureParams.sliceLeft <= Mathf.Epsilon &&
                textureParams.sliceTop <= Mathf.Epsilon &&
                textureParams.sliceRight <= Mathf.Epsilon &&
                textureParams.sliceBottom <= Mathf.Epsilon)
            {
                if (IsSimpleRect(textureParams.border))
                {
                    return MakeQuad(uir, transform, oldMeshHandle, textureParams.rect, textureParams.uv, textureParams.color, transformID, clippingID, vertexFlags);
                }
                else
                {
                    return UIRTessellation.TessellateBorderedRect(uir, transform, oldMeshHandle, textureParams.rect, textureParams.uv, textureParams.color, textureParams.border, transformID, clippingID, vertexFlags);
                }
            }
            else
            {
                return MakeSlicedQuad(uir, transform, oldMeshHandle, textureParams, transformID, clippingID, vertexFlags);
            }
        }

        private static Vertex ConvertTextVertexToUIRVertex(TextVertex textVertex, Matrix4x4 totalMatrix, uint transformID, uint clippingID)
        {
            Vector3 position = totalMatrix.MultiplyPoint(textVertex.position);
            position.z = UIR.MeshRenderer.k_PosZ;

            return new Vertex
            {
                position = position,
                uv = textVertex.uv0,
                tint = textVertex.color,
                transformID = transformID,
                clippingID = clippingID,
                flags = (float)VertexFlags.IsText
            };
        }

        private const int k_MaxTextMeshVertices = ushort.MaxValue + 1;
        private const int k_MaxTextMeshIndices = ushort.MaxValue + 1;
        private static readonly int k_MaxTextQuadCount = Math.Min(k_MaxTextMeshVertices / 4, k_MaxTextMeshIndices / 6);

        internal static MeshHandle MakeTextMeshHandle(IUIRenderDevice uir, NativeArray<TextVertex> uiVertices, Matrix4x4 transform, MeshHandle oldMeshHandle, uint transformID, uint clippingID)
        {
            int quadCount = uiVertices.Length / 4;
            if (quadCount > k_MaxTextQuadCount)
            {
                Debug.LogError("MakeTextMeshHandle: text is too long and generates too many vertices");
                quadCount = k_MaxTextQuadCount;
            }

            NativeSlice<Vertex> vertices;
            NativeSlice<UInt16> indices;
            UInt16 indexOffset;
            MeshHandle mesh = uir.Allocate((uint)(quadCount * 4), (uint)(quadCount * 6), out vertices, out indices, out indexOffset);

            for (int q = 0, v = 0, i = 0; q < quadCount; ++q, v += 4, i += 6)
            {
                vertices[v + 0] = ConvertTextVertexToUIRVertex(uiVertices[v + 0], transform, transformID, clippingID);
                vertices[v + 1] = ConvertTextVertexToUIRVertex(uiVertices[v + 1], transform, transformID, clippingID);
                vertices[v + 2] = ConvertTextVertexToUIRVertex(uiVertices[v + 2], transform, transformID, clippingID);
                vertices[v + 3] = ConvertTextVertexToUIRVertex(uiVertices[v + 3], transform, transformID, clippingID);

                indices[i + 0] = (UInt16)(v + 0 + indexOffset);
                indices[i + 1] = (UInt16)(v + 2 + indexOffset);
                indices[i + 2] = (UInt16)(v + 1 + indexOffset);
                indices[i + 3] = (UInt16)(v + 2 + indexOffset);
                indices[i + 4] = (UInt16)(v + 0 + indexOffset);
                indices[i + 5] = (UInt16)(v + 3 + indexOffset);
            }

            return mesh;
        }

        internal static void UpdateTextMeshHandle(IUIRenderDevice uir, NativeArray<TextVertex> uiVertices, Matrix4x4 transform, MeshHandle meshHandle, uint transformID, uint clippingID)
        {
            NativeSlice<Vertex> newVertices;
            uir.Update(meshHandle, (uint)uiVertices.Length, out newVertices);
            for (int i = 0; i < uiVertices.Length; ++i)
                newVertices[i] = ConvertTextVertexToUIRVertex(uiVertices[i], transform, transformID, clippingID);
        }

        internal static MeshHandle MakeMeshHandle(IUIRenderDevice uir, Rect offsetRect, Mesh mesh, MeshHandle oldMeshHandle, uint transformID, uint clippingID, VertexFlags vertexFlags)
        {
            var meshVertices = mesh.vertices;
            var meshIndices = mesh.triangles;
            var uvs = mesh.uv;
            var colors = mesh.colors;

            NativeSlice<Vertex> vertices;
            NativeSlice<UInt16> indices;
            UInt16 indexOffset;
            MeshHandle meshUIR = uir.Allocate((uint)meshVertices.Length, (uint)meshIndices.Length, out vertices, out indices, out indexOffset);

            if (uvs.Length > 0)
            {
                var color = Color.clear;
                for (int i = 0; i < meshVertices.Length; i++)
                {
                    var pos = meshVertices[i];
                    pos.x += offsetRect.x;
                    pos.y += offsetRect.y;
                    pos.z = UIR.MeshRenderer.k_PosZ;
                    var uv = uvs[i];
                    vertices[i] = new Vertex() {position = pos, tint = color, uv = uv, transformID = transformID, clippingID = clippingID, flags = (float)vertexFlags };
                }
            }
            else if (colors.Length > 0)
            {
                var uv = new Vector2(0, 1);
                for (int i = 0; i < meshVertices.Length; i++)
                {
                    var pos = meshVertices[i];
                    pos.x += offsetRect.x;
                    pos.y += offsetRect.y;
                    pos.z = UIR.MeshRenderer.k_PosZ;
                    var color = colors[i];
                    vertices[i] = new Vertex() { position = pos, tint = color, uv = uv, transformID = transformID, clippingID = clippingID, flags = (float)vertexFlags };
                }
            }
            else
            {
                var uv = new Vector2(0, 1);
                var color = Color.clear;
                for (int i = 0; i < meshVertices.Length; i++)
                {
                    var pos = meshVertices[i];
                    pos.x += offsetRect.x;
                    pos.y += offsetRect.y;
                    pos.z = UIR.MeshRenderer.k_PosZ;
                    vertices[i] = new Vertex() { position = pos, tint = color, uv = uv, transformID = transformID, clippingID = clippingID, flags = (float)vertexFlags };
                }
            }

            for (int i = 0; i < meshIndices.Length; i++)
                indices[i] = (UInt16)(meshIndices[i] + indexOffset);

            return meshUIR;
        }

        private static MeshHandle MakeQuad(IUIRenderDevice uir, Matrix4x4 transform, MeshHandle oldMeshHandle, Rect rcPosition, Rect rcTexCoord, Color color, uint transformID, uint clippingID, VertexFlags vertexFlags)
        {
            float x0 = rcPosition.x;
            float x3 = rcPosition.xMax;
            float y0 = rcPosition.yMax;
            float y3 = rcPosition.y;

            float u0 = rcTexCoord.x;
            float u3 = rcTexCoord.xMax;
            float v0 = rcTexCoord.y;
            float v3 = rcTexCoord.yMax;

            NativeSlice<Vertex> vertices;
            NativeSlice<UInt16> indices;
            UInt16 indexOffset;
            MeshHandle mesh = uir.Allocate(4, 6, out vertices, out indices, out indexOffset);

            vertices[0] = new Vertex()
            {
                position = new Vector3(x0, y0, 0),
                tint = color,
                uv = new Vector2(u0, v0),
                transformID = transformID,
                clippingID = clippingID,
                flags = (float)vertexFlags
            };
            vertices[1] = new Vertex()
            {
                position =  new Vector3(x3, y0, 0),
                tint = color,
                uv = new Vector2(u3, v0),
                transformID = transformID,
                clippingID = clippingID,
                flags = (float)vertexFlags
            };
            vertices[2] = new Vertex()
            {
                position =  new Vector3(x0, y3, 0),
                tint = color,
                uv = new Vector2(u0, v3),
                transformID = transformID,
                clippingID = clippingID,
                flags = (float)vertexFlags
            };
            vertices[3] = new Vertex()
            {
                position =  new Vector3(x3, y3, 0),
                tint = color,
                uv = new Vector2(u3, v3),
                transformID = transformID,
                clippingID = clippingID,
                flags = (float)vertexFlags
            };

            // Transform the positions and assign the Zs
            for (int i = 0; i < 4; ++i)
            {
                var v = vertices[i];
                v.position = transform.MultiplyPoint3x4(v.position);
                v.position.z = UIR.MeshRenderer.k_PosZ;
                vertices[i] = v;
            }

            indices[0] = (UInt16)(indexOffset + 0);
            indices[1] = (UInt16)(indexOffset + 1);
            indices[2] = (UInt16)(indexOffset + 2);

            indices[3] = (UInt16)(indexOffset + 1);
            indices[4] = (UInt16)(indexOffset + 3);
            indices[5] = (UInt16)(indexOffset + 2);

            return mesh;
        }

        private static readonly UInt16[] slicedQuadIndices = new UInt16[]
        {
            0, 1, 4, 4, 1, 5,
            1, 2, 5, 5, 2, 6,
            2, 3, 6, 6, 3, 7,
            4, 5, 8, 8, 5, 9,
            5, 6, 9, 9, 6, 10,
            6, 7, 10, 10, 7, 11,
            8, 9, 12, 12, 9, 13,
            9, 10, 13, 13, 10, 14,
            10, 11, 14, 14, 11, 15
        };

        // Caches.
        static readonly float[] k_TexCoordSlicesX = new float[4];
        static readonly float[] k_TexCoordSlicesY = new float[4];
        static readonly float[] k_PositionSlicesX = new float[4];
        static readonly float[] k_PositionSlicesY = new float[4];

        private static MeshHandle MakeSlicedQuad(IUIRenderDevice uir, Matrix4x4 transform, MeshHandle oldMeshHandle, TextureStylePainterParameters texParams, uint transformID, uint clippingID, VertexFlags vertexFlags)
        {
            var texture = texParams.texture;
            if (texture == null)
            {
                // Early exit without slicing.
                return MakeQuad(uir, transform, oldMeshHandle, texParams.rect, texParams.uv, texParams.color, transformID, clippingID, vertexFlags);
            }

            NativeSlice<Vertex> vertices;
            NativeSlice<UInt16> indices;
            UInt16 indexOffset;
            MeshHandle mesh = uir.Allocate(16, 9 * 6, out vertices, out indices, out indexOffset);

            float pixelsPerPoint = 1;
            var texture2D = texParams.texture as Texture2D;
            if (texture2D != null)
                pixelsPerPoint = texture2D.pixelsPerPoint;

            // The following offsets are in texels (not normalized).
            float uvSliceLeft = texParams.sliceLeft * pixelsPerPoint;
            float uvSliceTop = texParams.sliceTop * pixelsPerPoint;
            float uvSliceRight = texParams.sliceRight * pixelsPerPoint;
            float uvSliceBottom = texParams.sliceBottom * pixelsPerPoint;

            // When an atlas is used, relative coordinates must not be used.
            bool isAtlassed = VertexFlagsUtil.TypeIsEqual(vertexFlags, VertexFlags.IsTextured);
            float uConversion = isAtlassed ? 1 : 1f / texture.width;
            float vConversion = isAtlassed ? 1 : 1f / texture.height;

            k_TexCoordSlicesX[0] = texParams.uv.min.x;
            k_TexCoordSlicesX[1] = texParams.uv.min.x + uvSliceLeft * uConversion;
            k_TexCoordSlicesX[2] = texParams.uv.max.x - uvSliceRight * uConversion;
            k_TexCoordSlicesX[3] = texParams.uv.max.x;

            k_TexCoordSlicesY[0] = texParams.uv.max.y;
            k_TexCoordSlicesY[1] = texParams.uv.max.y - uvSliceBottom * vConversion;
            k_TexCoordSlicesY[2] = texParams.uv.min.y + uvSliceTop * vConversion;
            k_TexCoordSlicesY[3] = texParams.uv.min.y;

            k_PositionSlicesX[0] = texParams.rect.x;
            k_PositionSlicesX[1] = texParams.rect.x + texParams.sliceLeft;
            k_PositionSlicesX[2] = texParams.rect.xMax - texParams.sliceRight;
            k_PositionSlicesX[3] = texParams.rect.xMax;

            k_PositionSlicesY[0] = texParams.rect.yMax;
            k_PositionSlicesY[1] = texParams.rect.yMax - texParams.sliceBottom;
            k_PositionSlicesY[2] = texParams.rect.y + texParams.sliceTop;
            k_PositionSlicesY[3] = texParams.rect.y;

            for (int i = 0; i < 16; ++i)
            {
                int x = i % 4;
                int y = i / 4;

                var p = transform.MultiplyPoint3x4(new Vector3(k_PositionSlicesX[x], k_PositionSlicesY[y], 0));
                p.z = UIR.MeshRenderer.k_PosZ;

                vertices[i] = new Vertex() {
                    position = p,
                    uv = new Vector2(k_TexCoordSlicesX[x], texParams.uv.min.y + texParams.uv.max.y - k_TexCoordSlicesY[y]),
                    tint = texParams.color,
                    transformID = transformID,
                    clippingID = clippingID,
                    flags = (float)vertexFlags
                };
            }

            for (int i = 0; i < 9 * 6; i++)
                indices[i] = (UInt16)(slicedQuadIndices[i] + indexOffset);

            return mesh;
        }

        public static bool IsSimpleRect(BorderParameters border)
        {
            return border.topLeftRadius < Mathf.Epsilon &&
                border.topRightRadius < Mathf.Epsilon &&
                border.bottomRightRadius < Mathf.Epsilon &&
                border.bottomLeftRadius < Mathf.Epsilon &&
                !IsBorder(border);
        }

        public static bool IsBorder(BorderParameters border)
        {
            return border.leftWidth >= Mathf.Epsilon ||
                border.topWidth >= Mathf.Epsilon ||
                border.rightWidth >= Mathf.Epsilon ||
                border.bottomWidth >= Mathf.Epsilon;
        }
    }
}
