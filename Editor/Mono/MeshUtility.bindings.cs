// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/MeshUtility.bindings.h")]
    [NativeHeader("Runtime/Graphics/Mesh/MeshOptimizer.h")]
    [NativeHeader("Runtime/Graphics/Texture.h")]
    [StaticAccessor("MeshUtility", StaticAccessorType.DoubleColon)]
    public class MeshUtility
    {
        [FreeFunction] extern private static void OptimizeIndexBuffers([NotNull("NullExceptionObject")] Mesh mesh);
        [FreeFunction] extern private static void OptimizeReorderVertexBuffer([NotNull("NullExceptionObject")] Mesh mesh);

        public static void Optimize(Mesh mesh)
        {
            OptimizeIndexBuffers(mesh);
            OptimizeReorderVertexBuffer(mesh);
        }

        extern public static void SetMeshCompression([NotNull] Mesh mesh, ModelImporterMeshCompression compression);
        extern public static ModelImporterMeshCompression GetMeshCompression([NotNull] Mesh mesh);

        [NativeName("SetPerTriangleUV2")]
        static extern bool SetPerTriangleUV2NoCheck(Mesh src, Vector2[] triUV);
        public static bool SetPerTriangleUV2(Mesh src, Vector2[] triUV)
        {
            if (src == null)
                throw new ArgumentNullException("src");

            if (triUV == null)
                throw new ArgumentNullException("triUV");

            int triCount = InternalMeshUtil.CalcTriangleCount(src), uvCount  = triUV.Length;
            if (uvCount != 3 * triCount)
            {
                Debug.LogError("mesh contains " + triCount + " triangles but " + uvCount + " uvs are provided");
                return false;
            }
            return SetPerTriangleUV2NoCheck(src, triUV);
        }

        extern internal static Vector2[] ComputeTextureBoundingHull(Texture texture, int vertexCount);

        public static Mesh.MeshDataArray AcquireReadOnlyMeshData(Mesh mesh)
        {
            return new Mesh.MeshDataArray(mesh, false);
        }

        public static Mesh.MeshDataArray AcquireReadOnlyMeshData(Mesh[] meshes)
        {
            if (meshes == null)
                throw new ArgumentNullException(nameof(meshes), "Mesh array is null");
            return new Mesh.MeshDataArray(meshes, meshes.Length, false);
        }

        public static Mesh.MeshDataArray AcquireReadOnlyMeshData(List<Mesh> meshes)
        {
            if (meshes == null)
                throw new ArgumentNullException(nameof(meshes), "Mesh list is null");
            return new Mesh.MeshDataArray(NoAllocHelpers.ExtractArrayFromListT(meshes), meshes.Count, false);
        }
    }
}
