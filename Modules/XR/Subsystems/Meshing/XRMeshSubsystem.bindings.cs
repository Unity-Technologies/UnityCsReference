// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Collections.Generic;

namespace UnityEngine.XR
{
    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshId : IEquatable<MeshId>
    {
        public override string ToString()
        {
            return string.Format("{0}-{1}",
                m_SubId1.ToString("X16"),
                m_SubId2.ToString("X16"));
        }

        public override int GetHashCode()
        {
            return m_SubId1.GetHashCode() ^ m_SubId2.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return obj is MeshId && Equals((MeshId)obj);
        }

        public bool Equals(MeshId other)
        {
            return (m_SubId1 == other.m_SubId1) && (m_SubId2 == other.m_SubId2);
        }

        public static bool operator==(MeshId id1, MeshId id2)
        {
            return
                (id1.m_SubId1 == id2.m_SubId1) &&
                (id1.m_SubId2 == id2.m_SubId2);
        }

        public static bool operator!=(MeshId id1, MeshId id2)
        {
            return
                (id1.m_SubId1 != id2.m_SubId1) ||
                (id1.m_SubId2 != id2.m_SubId2);
        }

        private static MeshId s_InvalidId = new MeshId();
        public static MeshId InvalidId { get { return s_InvalidId; } }

        private ulong m_SubId1;
        private ulong m_SubId2;
    }

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
            unchecked
            {
                return hash1 * k_HashCodeMultiplier + hash2;
            }
        }

        public static int Combine(int hash1, int hash2, int hash3) => Combine(Combine(hash1, hash2), hash3);
        public static int Combine(int hash1, int hash2, int hash3, int hash4) => Combine(Combine(hash1, hash2, hash3), hash4);
        public static int Combine(int hash1, int hash2, int hash3, int hash4, int hash5) => Combine(Combine(hash1, hash2, hash3, hash4), hash5);
        public static int Combine(int hash1, int hash2, int hash3, int hash4, int hash5, int hash6) => Combine(Combine(hash1, hash2, hash3, hash4, hash5), hash6);
        public static int Combine(int hash1, int hash2, int hash3, int hash4, int hash5, int hash6, int hash7) => Combine(Combine(hash1, hash2, hash3, hash4, hash5, hash6), hash7);
        public static int Combine(int hash1, int hash2, int hash3, int hash4, int hash5, int hash6, int hash7, int hash8) => Combine(Combine(hash1, hash2, hash3, hash4, hash5, hash6, hash7), hash8);
    }

    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshGenerationResult : IEquatable<MeshGenerationResult>
    {
        public MeshId MeshId { get; }
        public Mesh Mesh { get; }
        public MeshCollider MeshCollider { get; }
        public MeshGenerationStatus Status { get; }
        public MeshVertexAttributes Attributes { get; }
        public ulong Timestamp { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }

        public override bool Equals(object obj)
        {
            if (!(obj is MeshGenerationResult))
                return false;

            return Equals((MeshGenerationResult)obj);
        }

        public bool Equals(MeshGenerationResult other)
        {
            return
                MeshId.Equals(other.MeshId) &&
                Mesh.Equals(other.Mesh) &&
                MeshCollider.Equals(other.MeshCollider) &&
                Status == other.Status &&
                Attributes == other.Attributes &&
                Position.Equals(other.Position) &&
                Rotation.Equals(other.Rotation) &&
                Scale.Equals(other.Scale);
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
            return HashCodeHelper.Combine(
                MeshId.GetHashCode(), Mesh.GetHashCode(), MeshCollider.GetHashCode(),
                ((int)Status).GetHashCode(), ((int)Attributes).GetHashCode(),
                Position.GetHashCode(), Rotation.GetHashCode(), Scale.GetHashCode());
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
    [Flags, UsedByNativeCode]
    public enum MeshGenerationOptions
    {
        None = 0,
        ConsumeTransform = 1 << 0,
    }

    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [UsedByNativeCode]
    public enum MeshChangeState
    {
        Added,
        Updated,
        Removed,
        Unchanged,
    }

    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct MeshInfo : IEquatable<MeshInfo>
    {
        public MeshId MeshId { get; set; }
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
                MeshId.Equals(other.MeshId) &&
                ChangeState.Equals(other.ChangeState) &&
                PriorityHint.Equals(other.PriorityHint);
        }

        public static bool operator==(MeshInfo lhs, MeshInfo rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(MeshInfo lhs, MeshInfo rhs)
        {
            return !lhs.Equals(rhs);
        }

        public override int GetHashCode() =>
            HashCodeHelper.Combine(MeshId.GetHashCode(), ((int)ChangeState).GetHashCode(), PriorityHint.GetHashCode());
    }

    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshBindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    readonly public struct MeshTransform : IEquatable<MeshTransform>
    {
        public MeshId MeshId { get; }
        public ulong Timestamp { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Scale { get; }

        public MeshTransform(in MeshId meshId, ulong timestamp, in Vector3 position, in Quaternion rotation, in Vector3 scale)
        {
            MeshId = meshId;
            Timestamp = timestamp;
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        public override bool Equals(object obj) => obj is MeshTransform other && Equals(other);
        public bool Equals(MeshTransform other) =>
            MeshId.Equals(other.MeshId) &&
            Timestamp == other.Timestamp &&
            Position.Equals(other.Position) &&
            Rotation.Equals(other.Rotation) &&
            Scale.Equals(other.Scale);
        public static bool operator==(MeshTransform lhs, MeshTransform rhs) => lhs.Equals(rhs);
        public static bool operator!=(MeshTransform lhs, MeshTransform rhs) => !lhs.Equals(rhs);
        public override int GetHashCode() =>
            HashCodeHelper.Combine(MeshId.GetHashCode(), Timestamp.GetHashCode(), Position.GetHashCode(), Rotation.GetHashCode(), Scale.GetHashCode());
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Meshing/XRMeshingSubsystem.h")]
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

        public void GenerateMeshAsync(
            MeshId meshId,
            Mesh mesh,
            MeshCollider meshCollider,
            MeshVertexAttributes attributes,
            Action<MeshGenerationResult> onMeshGenerationComplete)
        {
            GenerateMeshAsync(meshId, mesh, meshCollider, attributes, onMeshGenerationComplete, MeshGenerationOptions.None);
        }

        public extern void GenerateMeshAsync(
            MeshId meshId,
            Mesh mesh,
            MeshCollider meshCollider,
            MeshVertexAttributes attributes,
            Action<MeshGenerationResult> onMeshGenerationComplete,
            MeshGenerationOptions options);

        [RequiredByNativeCode]
        private void InvokeMeshReadyDelegate(
            MeshGenerationResult result,
            Action<MeshGenerationResult> onMeshGenerationComplete)
        {
            if (onMeshGenerationComplete != null)
                onMeshGenerationComplete(result);
        }

        public extern float meshDensity { get; set; }

        public extern bool SetBoundingVolume(Vector3 origin, Vector3 extents);

        [NativeConditional("ENABLE_XR")]
        readonly struct MeshTransformList : IDisposable
        {
            readonly IntPtr m_Self;

            public MeshTransformList(IntPtr self) => m_Self = self;

            public int Count => GetLength(m_Self);

            public IntPtr Data => GetData(m_Self);

            public void Dispose() => Dispose(m_Self);

            [FreeFunction("UnityXRMeshTransformList_get_Length")]
            static extern int GetLength(IntPtr self);

            [FreeFunction("UnityXRMeshTransformList_get_Data")]
            static extern IntPtr GetData(IntPtr self);

            [FreeFunction("UnityXRMeshTransformList_Dispose")]
            static extern void Dispose(IntPtr self);
        }

        public NativeArray<MeshTransform> GetUpdatedMeshTransforms(Allocator allocator)
        {
            unsafe
            {
                using var transforms = new MeshTransformList(GetUpdatedMeshTransforms());
                var result = new NativeArray<MeshTransform>(transforms.Count, allocator, NativeArrayOptions.UninitializedMemory);
                UnsafeUtility.MemCpy(result.GetUnsafePtr(), transforms.Data.ToPointer(), transforms.Count * sizeof(MeshTransform));
                return result;
            }
        }

        extern IntPtr GetUpdatedMeshTransforms();

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(XRMeshSubsystem subsystem) => subsystem.m_Ptr;
        }
    }
}
