// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    public static class PhysicsEditorMeshExtensions
    {
        public static bool HasPreBakeCollisionMesh(this Mesh mesh, bool isConvex)
        {
            return mesh.HasPreBakeCollisionMeshInternal(isConvex);
        }

        public static void SetPreBakeCollisionMesh(this Mesh mesh, bool isConvex, bool preBake)
        {
            mesh.SetPreBakeCollisionMeshInternal(isConvex, preBake);
        }
    }
}
