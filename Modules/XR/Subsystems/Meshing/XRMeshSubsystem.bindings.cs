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
        Canceled,
        UnknownError,
    }

    internal static class HashCodeHelper
    {
        const int k_HashCodeMultiplier = 486187739;

        public static int Combine(int hash1, int hash2)
        {
            return hash1 * k_HashCodeMultiplier + hash2;
        }
    }

    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshGenerationResult : IEquatable<MeshGenerationResult>
    {
        public TrackableId MeshId { get; }
        public Mesh Mesh { get; }
        public MeshCollider MeshCollider { get; }
        public MeshGenerationStatus Status { get; }
        public MeshVertexAttributes Attributes { get; }

        public override bool Equals(object obj)
        {
            if (!(obj is MeshGenerationResult))
                return false;

            return Equals((MeshGenerationResult)obj);
        }

        public bool Equals(MeshGenerationResult other)
        {
            return
                (MeshId.Equals(other.MeshId)) &&
                (Mesh.Equals(other.Mesh)) &&
                (MeshCollider.Equals(other.MeshCollider)) &&
                (Status.Equals(other.Status)) &&
                (Attributes.Equals(other.Attributes));
        }

        public static bool operator==(MeshGenerationResult lhs, MeshGenerationResult rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(MeshGenerationResult lhs, MeshGenerationResult rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override int GetHashCode()
        {
            return
                HashCodeHelper.Combine(
                HashCodeHelper.Combine(
                    HashCodeHelper.Combine(
                        HashCodeHelper.Combine(MeshId.GetHashCode(), Mesh.GetHashCode()),
                        MeshCollider.GetHashCode()), Status.GetHashCode()), Attributes.GetHashCode());
        }
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
    public struct MeshInfo : IEquatable<MeshInfo>
    {
        public TrackableId MeshId { get; set; }
        public MeshChangeState ChangeState { get; set; }
        public int PriorityHint { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is MeshInfo))
                return false;

            return Equals((MeshInfo)obj);
        }

        public bool Equals(MeshInfo other)
        {
            return
                (MeshId.Equals(other.MeshId)) &&
                (ChangeState.Equals(other.ChangeState)) &&
                (PriorityHint.Equals(other.PriorityHint));
        }

        public static bool operator==(MeshInfo lhs, MeshInfo rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(MeshInfo lhs, MeshInfo rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override int GetHashCode()
        {
            return
                HashCodeHelper.Combine(
                HashCodeHelper.Combine(MeshId.GetHashCode(), ChangeState.GetHashCode()), PriorityHint);
        }
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
