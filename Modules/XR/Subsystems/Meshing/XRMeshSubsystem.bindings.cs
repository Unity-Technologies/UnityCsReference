// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Collections.Generic;

namespace UnityEngine.Experimental.XR
{
    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [RequiredByNativeCode]
    public enum MeshGenerationStatus
    {
        Success,
        InvalidMeshId,
        GenerationAlreadyInProgress,
        UnknownError,
    }

    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshGenerationResult
    {
        public TrackableId MeshId { get; }
        public Mesh Mesh { get; }
        public MeshCollider MeshCollider { get; }
        public MeshGenerationStatus Status { get; }
        public MeshVertexAttributes Attributes { get; }
    }

    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [UsedByNativeCode]
    [Flags]
    public enum MeshVertexAttributes
    {
        None = 0,
        Normals = 1 << 0,
        Tangents = 1 << 1,
        UVs = 1 << 2,
        Colors = 1 << 3,
    }

    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [UsedByNativeCode]
    public enum MeshChangeState
    {
        Added,
        Updated,
        Removed,
        Unchanged
    }

    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshInfo
    {
        public TrackableId MeshId { get; }
        public MeshChangeState ChangeState { get; }
        public int PriorityHint { get; }
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshSubsystem.h")]
    [NativeConditional("ENABLE_XR")]
    [UsedByNativeCode]
    public class XRMeshSubsystem : IntegratedSubsystem<XRMeshSubsystemDescriptor>
    {
        public bool TryGetMeshInfos(List<MeshInfo> meshInfosOut)
        {
            if (meshInfosOut == null)
                throw new ArgumentNullException("meshInfosOut");

            return GetMeshInfosAsList(meshInfosOut);
        }

        private extern bool GetMeshInfosAsList(List<MeshInfo> meshInfos);

        private extern MeshInfo[] GetMeshInfosAsFixedArray();

        public extern void GenerateMeshAsync(
            TrackableId meshId,
            Mesh mesh,
            MeshCollider meshCollider,
            MeshVertexAttributes attributes,
            Action<MeshGenerationResult> onMeshGenerationComplete);

        [RequiredByNativeCode]
        private void InvokeMeshReadyDelegate(
            MeshGenerationResult result,
            Action<MeshGenerationResult> onMeshGenerationComplete)
        {
            if (onMeshGenerationComplete != null)
                onMeshGenerationComplete(result);
        }
    }
}
