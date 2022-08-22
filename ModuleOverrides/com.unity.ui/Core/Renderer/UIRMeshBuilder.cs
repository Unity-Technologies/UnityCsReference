// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.TextCore.Text;
using System.IO;

namespace UnityEngine.UIElements.UIR
{
    /// <summary>
    /// Utility class that facilitates mesh allocation and building according to the provided settings
    /// </summary>
    internal static class MeshBuilder
    {
        static ProfilerMarker s_VectorGraphics9Slice = new ProfilerMarker("UIR.MakeVector9Slice");
        static ProfilerMarker s_VectorGraphicsSplitTriangle = new ProfilerMarker("UIR.SplitTriangle");
        static ProfilerMarker s_VectorGraphicsScaleTriangle = new ProfilerMarker("UIR.ScaleTriangle");
        static ProfilerMarker s_VectorGraphicsStretch = new ProfilerMarker("UIR.MakeVectorStretch");

        internal static readonly int s_MaxTextMeshVertices = 0xC000; // Max 48k vertices. We leave room for masking, borders, background, etc.

        internal struct AllocMeshData
        {
            internal delegate MeshWriteData Allocator(uint vertexCount, uint indexCount, ref AllocMeshData allocatorData);
            internal MeshWriteData Allocate(uint vertexCount, uint indexCount) { return alloc(vertexCount, indexCount, ref this); }

            internal Allocator alloc;

            // Additional allocation params
            internal Texture texture;
            internal TextureId svgTexture;
            internal Material material;
            internal MeshGenerationContext.MeshFlags flags;
            internal BMPAlloc colorAlloc;
        }

        private static Vertex ConvertTextVertexToUIRVertex(MeshInfo info, int index, Vector2 offset, VertexFlags flags = VertexFlags.IsText, bool isDynamicColor = false)
        {
            float dilate = 0.0f;
            // If Bold, dilate the shape (this value is hardcoded, should be set from the font actual bold weight)
            if (info.uvs2[index].y < 0.0f) dilate = 1.0f;
            return new Vertex
            {
                position = new Vector3(info.vertices[index].x + offset.x, info.vertices[index].y + offset.y, UIRUtility.k_MeshPosZ),
                uv = new Vector2(info.uvs0[index].x, info.uvs0[index].y),
                tint = info.colors32[index],
                flags = new Color32((byte)flags, (byte)(dilate * 255), 0, isDynamicColor ? (byte)1 : (byte)0)
            };
        }

        private static Vertex ConvertTextVertexToUIRVertex(TextVertex textVertex, Vector2 offset)
        {
            return new Vertex
            {
                position = new Vector3(textVertex.position.x + offset.x, textVertex.position.y + offset.y, UIRUtility.k_MeshPosZ),
                uv = textVertex.uv0,
                tint = textVertex.color,
                flags = new Color32((byte)VertexFlags.IsText, 0, 0, 0) // same flag for both text engines
            };
        }

        static int LimitTextVertices(int vertexCount, bool logTruncation = true)
        {
            if (vertexCount <= s_MaxTextMeshVertices)
                return vertexCount;

            if (logTruncation)
                Debug.LogWarning($"Generated text will be truncated because it exceeds {s_MaxTextMeshVertices} vertices.");

            return s_MaxTextMeshVertices;
        }

        internal static void MakeText(MeshInfo meshInfo, Vector2 offset, AllocMeshData meshAlloc, VertexFlags flags = VertexFlags.IsText, bool isDynamicColor = false)
        {
            int vertexCount = LimitTextVertices(meshInfo.vertexCount);
            int quadCount = vertexCount / 4;
            var mesh = meshAlloc.Allocate((uint)(quadCount * 4), (uint)(quadCount * 6));

            for (int q = 0, v = 0; q < quadCount; ++q, v += 4)
            {
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(meshInfo, v + 0, offset, flags, isDynamicColor));
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(meshInfo, v + 1, offset, flags, isDynamicColor));
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(meshInfo, v + 2, offset, flags, isDynamicColor));
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(meshInfo, v + 3, offset, flags, isDynamicColor));

                mesh.SetNextIndex((UInt16)(v + 0));
                mesh.SetNextIndex((UInt16)(v + 1));
                mesh.SetNextIndex((UInt16)(v + 2));
                mesh.SetNextIndex((UInt16)(v + 2));
                mesh.SetNextIndex((UInt16)(v + 3));
                mesh.SetNextIndex((UInt16)(v + 0));
            }
        }

        internal static void MakeText(NativeArray<TextVertex> uiVertices, Vector2 offset, AllocMeshData meshAlloc)
        {
            int vertexCount = LimitTextVertices(uiVertices.Length);
            int quadCount = vertexCount / 4;
            var mesh = meshAlloc.Allocate((uint)(quadCount * 4), (uint)(quadCount * 6));

            for (int q = 0, v = 0; q < quadCount; ++q, v += 4)
            {
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(uiVertices[v + 0], offset));
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(uiVertices[v + 1], offset));
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(uiVertices[v + 2], offset));
                mesh.SetNextVertex(ConvertTextVertexToUIRVertex(uiVertices[v + 3], offset));

                mesh.SetNextIndex((UInt16)(v + 0));
                mesh.SetNextIndex((UInt16)(v + 1));
                mesh.SetNextIndex((UInt16)(v + 2));
                mesh.SetNextIndex((UInt16)(v + 2));
                mesh.SetNextIndex((UInt16)(v + 3));
                mesh.SetNextIndex((UInt16)(v + 0));
            }
        }

        internal static void UpdateText(NativeArray<TextVertex> uiVertices,
            Vector2 offset, Matrix4x4 transform,
            Color32 xformClipPages, Color32 ids, Color32 flags, Color32 opacityPageSettingIndex,
            NativeSlice<Vertex> vertices, TextureId textureId)
        {
            int vertexCount = LimitTextVertices(uiVertices.Length, false);
            Debug.Assert(vertexCount == vertices.Length);
            flags.r = (byte)VertexFlags.IsText;
            for (int v = 0; v < vertexCount; v++)
            {
                var textVertex = uiVertices[v];
                vertices[v] = new Vertex
                {
                    position = transform.MultiplyPoint3x4(new Vector3(textVertex.position.x + offset.x, textVertex.position.y + offset.y, UIRUtility.k_MeshPosZ)),
                    uv = textVertex.uv0,
                    tint = textVertex.color,
                    xformClipPages = xformClipPages,
                    ids = ids,
                    flags = flags,
                    opacityColorPages = opacityPageSettingIndex,
                    textureId = textureId.ConvertToGpu()
                };
            }
        }
    }
}
