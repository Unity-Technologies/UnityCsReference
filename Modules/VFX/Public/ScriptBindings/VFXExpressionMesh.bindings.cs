// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.VFX
{
    [RequiredByNativeCode]
    [NativeType(Header = "Modules/VFX/Public/VFXExpressionMeshFunctions.h")]
    [StaticAccessor("VFXExpressionMeshFunctions", StaticAccessorType.DoubleColon)]
    internal class VFXExpressionMesh
    {
        extern static internal int GetVertexStride(Mesh mesh);
        extern static internal int GetChannelOffset(Mesh mesh, int channelIndex);
        extern static internal float GetFloat(Mesh mesh, int vertexIndex, int channelOffset, int vertexStride);
        extern static internal Vector2 GetFloat2(Mesh mesh, int vertexIndex, int channelOffset, int vertexStride);
        extern static internal Vector3 GetFloat3(Mesh mesh, int vertexIndex, int channelOffset, int vertexStride);
        extern static internal Vector4 GetFloat4(Mesh mesh, int vertexIndex, int channelOffset, int vertexStride);
        extern static internal Vector4 GetColor(Mesh mesh, int vertexIndex, int channelOffset, int vertexStride);
    }
}
