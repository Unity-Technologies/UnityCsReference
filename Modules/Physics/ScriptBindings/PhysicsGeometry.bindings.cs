// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine.LowLevelPhysics
{
    public interface IGeometry
    {
        GeometryType GeometryType { get; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct BoxGeometry : IGeometry
    {
        private int m_UnusedReserved;
        private Vector3 m_HalfExtents;

        public Vector3 HalfExtents { get { return m_HalfExtents; } set { m_HalfExtents = value; } }

        public BoxGeometry(Vector3 halfExtents)
        {
            m_UnusedReserved = -1;
            m_HalfExtents = halfExtents;
        }

        public GeometryType GeometryType => GeometryType.Box;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SphereGeometry : IGeometry
    {
        private int m_UnusedReserved;
        private float m_Radius;

        public float Radius { get { return m_Radius; } set { m_Radius = value; } }

        public SphereGeometry(float radius)
        {
            m_UnusedReserved = -1;
            m_Radius = radius;
        }

        public GeometryType GeometryType => GeometryType.Sphere;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CapsuleGeometry : IGeometry
    {
        private int m_UnusedReserved;
        private float m_Radius;
        private float m_HalfLength;

        public float Radius { get { return m_Radius; } set { m_Radius = value; } }
        public float HalfLength { get { return m_HalfLength; } set { m_HalfLength = value; } }

        public CapsuleGeometry(float radius, float halfLength)
        {
            m_UnusedReserved = -1;
            m_Radius = radius;
            m_HalfLength = halfLength;
        }

        public GeometryType GeometryType => GeometryType.Capsule;
    }

    // From PxConvexMeshGeometry.h
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ConvexMeshGeometry : IGeometry
    {
        private int m_UnusedReserved;
        private Vector3 m_Scale;
        private Quaternion m_Rotation;
        private IntPtr m_ConvexMesh;
        private byte m_MeshFlags;
        private fixed byte m_MeshFlagsPadding[3];

        public Vector3 Scale { get { return m_Scale; } set { m_Scale = value; } }
        public Quaternion ScaleAxisRotation { get { return m_Rotation; } set { m_Rotation = value; } }

        public GeometryType GeometryType => GeometryType.ConvexMesh;
    }

    // From PxTriangleMeshGeometry.h
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TriangleMeshGeometry : IGeometry
    {
        private int m_UnusedReserved;
        private Vector3 m_Scale;
        private Quaternion m_Rotation;
        private byte m_MeshFlags;
        private fixed byte m_MeshFlagsPadding[3];
        private IntPtr m_TriangleMesh;

        public Vector3 Scale { get { return m_Scale; } set { m_Scale = value; } }
        public Quaternion ScaleAxisRotation { get { return m_Rotation; } set { m_Rotation = value; } }

        public GeometryType GeometryType => GeometryType.TriangleMesh;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct TerrainGeometry : IGeometry
    {
        private int m_UnusedReserved;
        private IntPtr m_TerrainData;
        private float m_HeightScale;
        private float m_RowScale;
        private float m_ColumnScale;
        private byte m_TerrainFlags;
        private fixed byte m_TerrainFlagsPadding[3];

        public GeometryType GeometryType => GeometryType.Terrain;
    }

    public enum GeometryType : int
    {
        Sphere = 0,
        Capsule = 2,
        Box = 3,
        ConvexMesh = 4,
        TriangleMesh = 5,
        Terrain = 6,
        Invalid = -1
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct GeometryHolder
    {
        // !!!Keep in sync with PhysicsCollisionGeometry.h!!!
        //
        // physx::PxGeometryHolder blob data, the blob data members are provided in such as way so that the geometry type of the holder is the only non-opaque piece of data inside
        // the memory layout matches PxConvexMeshGeometry, ensuring we can fit all smaller types inside the holder blob
        //
        // PxTriangleMeshLayout (64bit):
        // [00...03] -- PxGeometryType
        // [04...31] -- PxMeshScale
        // [32...39] -- PxConvexMesh ptr
        // [40...43] -- PxConvexMeshGeometryFlag + 3 byte padding
        // [44...47] -- 4 byte padding
        internal fixed int m_Data[12];

        public T As<T>() where T : struct, IGeometry
        {
            T geometry = default;

            if (geometry.GeometryType != Type)
                throw new InvalidOperationException($"Unable to get geometry of type {geometry.GeometryType} from a geometry holder that stores {Type}.");

            UnsafeUtility.CopyPtrToStructure(UnsafeUtility.AddressOf(ref this), out geometry);

            return geometry;
        }

        public static GeometryHolder Create<T>(T geometry) where T : struct, IGeometry
        {
            GeometryHolder holder = default;
            UnsafeUtility.CopyStructureToPtr(ref geometry, UnsafeUtility.AddressOf(ref holder));
            //we need to ensure we properly patch in the geometry type as we can't ensure that the correct one is being provided to the struct due to the invalid value being -1 rather than 0
            holder.m_Data[0] = (int)geometry.GeometryType;

            return holder;
        }

        public GeometryType Type => (GeometryType)m_Data[0];
    }

    [NativeHeader("Modules/Physics/PhysicsCollisionGeometry.h")]
    internal static class PhysXGeometryHolderExtension
    {
        [FreeFunction("Physics::PhysXGeometryExtension::GetGeometryHolderFromCollider")]
        public static extern GeometryHolder GetGeometryHolder(this Collider col);
    }
}
