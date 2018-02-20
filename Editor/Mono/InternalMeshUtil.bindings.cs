// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/InternalMeshUtil.bindings.h")]
    internal static class InternalMeshUtil
    {
        public static extern int GetPrimitiveCount(Mesh mesh);

        public static extern int CalcTriangleCount(Mesh mesh);

        public static extern bool HasNormals(Mesh mesh);

        public static extern string GetVertexFormat(Mesh mesh);

        public static extern float GetCachedMeshSurfaceArea(MeshRenderer meshRenderer);
    }
}
