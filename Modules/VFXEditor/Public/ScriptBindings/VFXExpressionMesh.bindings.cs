// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace UnityEditor.VFX
{
    [RequiredByNativeCode]
    [NativeType(Header = "Modules/VFX/Public/VFXExpressionMeshFunctions.h")]
    [StaticAccessor("VFXExpressionMeshFunctions", StaticAccessorType.DoubleColon)]
    internal class VFXExpressionMesh
    {
        extern static internal uint GetVertexCount(Mesh mesh);

        //Compatibility code for package 9.x.x, the mesh was considered on a unique streamIndex
        static internal uint GetVertexStride(Mesh mesh)
        {
            return GetVertexStride(mesh, (UInt32)VertexAttribute.Position);
        }

        extern static internal uint GetVertexStride(Mesh mesh, uint channelIndex);
        extern static internal uint GetChannelOffset(Mesh mesh, uint channelIndex);
        extern static internal uint GetChannelInfos(Mesh mesh, uint channelIndex);

        //Compatibility code for package 9.x.x There wasn't ChannelInfos, assume a fixed layout.
        static internal float GetFloat(Mesh mesh, uint vertexIndex, uint channelOffset, uint vertexStride)
        {
            if (channelOffset == ~0u)
                return 0.0f;
            return GetFloat(mesh, vertexIndex * vertexStride + channelOffset, (byte)VertexAttributeFormat.Float32 | (1 << 8));
        }

        static internal Vector2 GetFloat2(Mesh mesh, uint vertexIndex, uint channelOffset, uint vertexStride)
        {
            if (channelOffset == ~0u)
                return Vector2.zero;
            return GetFloat2(mesh, vertexIndex * vertexStride + channelOffset, (byte)VertexAttributeFormat.Float32 | (2 << 8));
        }

        static internal Vector3 GetFloat3(Mesh mesh, uint vertexIndex, uint channelOffset, uint vertexStride)
        {
            if (channelOffset == ~0u)
                return Vector3.zero;
            return GetFloat3(mesh, vertexIndex * vertexStride + channelOffset, (byte)VertexAttributeFormat.Float32 | (3 << 8));
        }

        static internal Vector4 GetFloat4(Mesh mesh, uint vertexIndex, uint channelOffset, uint vertexStride)
        {
            if (channelOffset == ~0u)
                return Vector4.zero;
            return GetFloat4(mesh, vertexIndex * vertexStride + channelOffset, (byte)VertexAttributeFormat.Float32 | (4 << 8));
        }

        static internal Vector4 GetColor(Mesh mesh, uint vertexIndex, uint channelOffset, uint vertexStride)
        {
            if (channelOffset == ~0u)
                return Vector4.zero;
            return GetColor(mesh, vertexIndex * vertexStride + channelOffset, (byte)VertexAttributeFormat.UNorm8 | (4 << 8));
        }

        extern static internal float GetFloat(Mesh mesh, uint vertexOffset, uint channelInfos);
        extern static internal Vector2 GetFloat2(Mesh mesh, uint vertexOffset, uint channelInfos);
        extern static internal Vector3 GetFloat3(Mesh mesh, uint vertexOffset, uint channelInfos);
        extern static internal Vector4 GetFloat4(Mesh mesh, uint vertexOffset, uint channelInfos);
        extern static internal Vector4 GetColor(Mesh mesh, uint vertexOffset, uint channelInfos);

        extern static internal uint GetIndexFormat(Mesh mesh);
        extern static internal uint GetIndexCount(Mesh mesh);
        extern static internal uint GetIndex(Mesh mesh, uint index, uint indexFormat);
    }
}
