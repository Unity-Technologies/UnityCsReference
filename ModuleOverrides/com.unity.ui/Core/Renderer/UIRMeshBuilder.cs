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
    }
}
