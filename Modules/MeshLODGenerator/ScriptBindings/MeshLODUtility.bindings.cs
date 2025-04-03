// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Modules/MeshLODGenerator/Public/MeshLODUtility.bindings.h")]
    public sealed class MeshLodUtility
    {
        [NativeType(Header = "Modules/MeshLODGenerator/Public/MeshLODGenerator.h")]
        [Flags]
        public enum LodGenerationFlags
        {
            DiscardOddLevels = 1 << 0,
        }

        static public void GenerateMeshLods(Mesh mesh, int meshLodLimit = -1)
        {
            GenerateMeshLods(mesh, 0, meshLodLimit);
        }

        static public void GenerateMeshLods(Mesh mesh, LodGenerationFlags flags, int meshLodLimit = -1)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            Internal_GenerateMeshLodsEditor(mesh, flags, meshLodLimit);
        }

        [FreeFunction(Name = "MeshLodUtility::GenerateMeshLods")]
        extern static void Internal_GenerateMeshLodsEditor([NotNull] Mesh mesh, LodGenerationFlags flags, int meshLodLimit);
    }
}
