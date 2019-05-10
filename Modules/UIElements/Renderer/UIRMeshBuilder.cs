// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    /// <summary>
    /// Utility class that facilitates mesh allocation and building according to the provided settings
    /// </summary>
    internal static class MeshBuilder
    {
        // IL2CPP doesn't support out parameters of unsafe containers yet, so we return a struct instead.
        internal struct MeshOutput
        {
            public NativeSlice<Vertex> vertices;
            public NativeSlice<UInt16> indices;
        }
        internal delegate MeshOutput AllocMeshData(uint vertexCount, uint indexCount);

        internal static void MakeBorder(MeshGenerationContextUtils.BorderParams borderParams, float posZ, AllocMeshData meshAlloc)
        {
            Tessellation.TessellateBorder(borderParams, posZ, meshAlloc);
        }

        internal static void MakeRect(MeshGenerationContextUtils.RectangleParams rectParams, float posZ, AllocMeshData meshAlloc)
        {
            if (!rectParams.HasRadius(Tessellation.kEpsilon))
                MakeQuad(rectParams.rect, Rect.zero, rectParams.color, posZ, VertexFlags.IsSolid, meshAlloc);
            else Tessellation.TessellateRect(rectParams, posZ, VertexFlags.IsSolid, meshAlloc);
        }

        internal static void MakeTexture(MeshGenerationContextUtils.RectangleParams rectParams, float posZ, VertexFlags vertexFlags, AllocMeshData meshAlloc)
        {
            if (rectParams.leftSlice <= Mathf.Epsilon &&
                rectParams.topSlice <= Mathf.Epsilon &&
                rectParams.rightSlice <= Mathf.Epsilon &&
                rectParams.bottomSlice <= Mathf.Epsilon)
            {
                if (!rectParams.HasRadius(Tessellation.kEpsilon))
                    MakeQuad(rectParams.rect, rectParams.uv, rectParams.color, posZ, vertexFlags, meshAlloc);
                else Tessellation.TessellateRect(rectParams, posZ, vertexFlags, meshAlloc);
            }
            else if (rectParams.texture == null)
            {
                MakeQuad(rectParams.rect, rectParams.uv, rectParams.color, posZ, vertexFlags, meshAlloc);
            }
            else MakeSlicedQuad(ref rectParams, posZ, vertexFlags, meshAlloc);
        }

        private static Vertex ConvertTextVertexToUIRVertex(TextVertex textVertex, Vector2 offset)
        {
            return new Vertex
            {
                position = new Vector3(textVertex.position.x + offset.x, textVertex.position.y + offset.y, UIRUtility.k_MeshPosZ),
                uv = textVertex.uv0,
                tint = textVertex.color,
                flags = (float)VertexFlags.IsText
            };
        }

        private const int k_MaxTextMeshVertices = ushort.MaxValue + 1;
        private const int k_MaxTextMeshIndices = ushort.MaxValue + 1;
        private static readonly int k_MaxTextQuadCount = Math.Min(k_MaxTextMeshVertices / 4, k_MaxTextMeshIndices / 6);

        internal static void MakeText(NativeArray<TextVertex> uiVertices, Vector2 offset, AllocMeshData meshAlloc)
        {
            int quadCount = uiVertices.Length / 4;
            if (quadCount > k_MaxTextQuadCount)
            {
                Debug.LogError("MakeTextMeshHandle: text is too long and generates too many vertices");
                quadCount = k_MaxTextQuadCount;
            }

            var mesh = meshAlloc((uint)(quadCount * 4), (uint)(quadCount * 6));

            for (int q = 0, v = 0, i = 0; q < quadCount; ++q, v += 4, i += 6)
            {
                mesh.vertices[v + 0] = ConvertTextVertexToUIRVertex(uiVertices[v + 0], offset);
                mesh.vertices[v + 1] = ConvertTextVertexToUIRVertex(uiVertices[v + 1], offset);
                mesh.vertices[v + 2] = ConvertTextVertexToUIRVertex(uiVertices[v + 2], offset);
                mesh.vertices[v + 3] = ConvertTextVertexToUIRVertex(uiVertices[v + 3], offset);

                mesh.indices[i + 0] = (UInt16)(v + 0);
                mesh.indices[i + 1] = (UInt16)(v + 2);
                mesh.indices[i + 2] = (UInt16)(v + 1);
                mesh.indices[i + 3] = (UInt16)(v + 2);
                mesh.indices[i + 4] = (UInt16)(v + 0);
                mesh.indices[i + 5] = (UInt16)(v + 3);
            }
        }

        internal static void UpdateText(NativeArray<TextVertex> uiVertices, Vector2 offset, Matrix4x4 transform, float transformID, float clipRectID, NativeSlice<Vertex> vertices)
        {
            Debug.Assert(vertices.Length == uiVertices.Length);
            int vertexCount = uiVertices.Length;
            for (int v = 0; v < vertexCount; v++)
            {
                var textVertex = uiVertices[v];
                vertices[v] = new Vertex
                {
                    position = transform.MultiplyPoint3x4(new Vector3(textVertex.position.x + offset.x, textVertex.position.y + offset.y, UIRUtility.k_MeshPosZ)),
                    uv = textVertex.uv0,
                    tint = textVertex.color,
                    transformID = transformID,
                    clipRectID = clipRectID,
                    flags = (float)VertexFlags.IsText
                };
            }
        }

        private static void MakeQuad(Rect rcPosition, Rect rcTexCoord, Color color, float posZ, VertexFlags vertexFlags, AllocMeshData meshAlloc)
        {
            float x0 = rcPosition.x;
            float x3 = rcPosition.xMax;
            float y0 = rcPosition.yMax;
            float y3 = rcPosition.y;

            float u0 = rcTexCoord.x;
            float u3 = rcTexCoord.xMax;
            float v0 = rcTexCoord.y;
            float v3 = rcTexCoord.yMax;

            var mesh = meshAlloc(4, 6);

            mesh.vertices[0] = new Vertex()
            {
                position = new Vector3(x0, y0, posZ),
                tint = color,
                uv = new Vector2(u0, v0),
                flags = (float)vertexFlags
            };
            mesh.vertices[1] = new Vertex()
            {
                position = new Vector3(x3, y0, posZ),
                tint = color,
                uv = new Vector2(u3, v0),
                flags = (float)vertexFlags
            };
            mesh.vertices[2] = new Vertex()
            {
                position = new Vector3(x0, y3, posZ),
                tint = color,
                uv = new Vector2(u0, v3),
                flags = (float)vertexFlags
            };
            mesh.vertices[3] = new Vertex()
            {
                position = new Vector3(x3, y3, posZ),
                tint = color,
                uv = new Vector2(u3, v3),
                flags = (float)vertexFlags
            };

            mesh.indices[0] = 0;
            mesh.indices[1] = 1;
            mesh.indices[2] = 2;

            mesh.indices[3] = 1;
            mesh.indices[4] = 3;
            mesh.indices[5] = 2;
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

        private static void MakeSlicedQuad(ref MeshGenerationContextUtils.RectangleParams rectParams, float posZ, VertexFlags vertexFlags, AllocMeshData meshAlloc)
        {
            var mesh = meshAlloc(16, 9 * 6);

            float pixelsPerPoint = 1;
            var texture2D = rectParams.texture as Texture2D;
            if (texture2D != null)
                pixelsPerPoint = texture2D.pixelsPerPoint;

            // The following offsets are in texels (not normalized).
            float uvSliceLeft = rectParams.leftSlice * pixelsPerPoint;
            float uvSliceTop = rectParams.topSlice * pixelsPerPoint;
            float uvSliceRight = rectParams.rightSlice * pixelsPerPoint;
            float uvSliceBottom = rectParams.bottomSlice * pixelsPerPoint;

            // When an atlas is used, relative coordinates must not be used.
            bool isAtlassed = vertexFlags == VertexFlags.IsAtlasTexturedPoint || vertexFlags == VertexFlags.IsAtlasTexturedBilinear;
            float uConversion = isAtlassed ? 1 : 1f / rectParams.texture.width;
            float vConversion = isAtlassed ? 1 : 1f / rectParams.texture.height;

            k_TexCoordSlicesX[0] = rectParams.uv.min.x;
            k_TexCoordSlicesX[1] = rectParams.uv.min.x + uvSliceLeft * uConversion;
            k_TexCoordSlicesX[2] = rectParams.uv.max.x - uvSliceRight * uConversion;
            k_TexCoordSlicesX[3] = rectParams.uv.max.x;

            k_TexCoordSlicesY[0] = rectParams.uv.max.y;
            k_TexCoordSlicesY[1] = rectParams.uv.max.y - uvSliceBottom * vConversion;
            k_TexCoordSlicesY[2] = rectParams.uv.min.y + uvSliceTop * vConversion;
            k_TexCoordSlicesY[3] = rectParams.uv.min.y;

            k_PositionSlicesX[0] = rectParams.rect.x;
            k_PositionSlicesX[1] = rectParams.rect.x + rectParams.leftSlice;
            k_PositionSlicesX[2] = rectParams.rect.xMax - rectParams.rightSlice;
            k_PositionSlicesX[3] = rectParams.rect.xMax;

            k_PositionSlicesY[0] = rectParams.rect.yMax;
            k_PositionSlicesY[1] = rectParams.rect.yMax - rectParams.bottomSlice;
            k_PositionSlicesY[2] = rectParams.rect.y + rectParams.topSlice;
            k_PositionSlicesY[3] = rectParams.rect.y;

            for (int i = 0; i < 16; ++i)
            {
                int x = i % 4;
                int y = i / 4;
                mesh.vertices[i] = new Vertex() {
                    position = new Vector3(k_PositionSlicesX[x], k_PositionSlicesY[y], posZ),
                    uv = new Vector2(k_TexCoordSlicesX[x], rectParams.uv.min.y + rectParams.uv.max.y - k_TexCoordSlicesY[y]),
                    tint = rectParams.color,
                    flags = (float)vertexFlags
                };
            }
            mesh.indices.CopyFrom(slicedQuadIndices);
        }
    }
}
