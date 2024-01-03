// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.LowLevelPhysics
{
    public interface IGeometry
    {
        GeometryType GeometryType { get; }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BoxGeometry : IGeometry
    {
        private Vector3 m_HalfExtents;

        public Vector3 HalfExtents { get { return m_HalfExtents; } set { m_HalfExtents = value; } }

        public BoxGeometry(Vector3 halfExtents)
        {
            m_HalfExtents = halfExtents;
        }

        public GeometryType GeometryType => GeometryType.Box;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SphereGeometry : IGeometry
    {
        private float m_Radius;

        public float Radius { get { return m_Radius; } set { m_Radius = value; } }

        public SphereGeometry(float radius)
        {
            m_Radius = radius;
        }

        public GeometryType GeometryType => GeometryType.Sphere;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CapsuleGeometry : IGeometry
    {
        private float m_Radius;
        private float m_HalfLength;

        public float Radius { get { return m_Radius; } set { m_Radius = value; } }
        public float HalfLength { get { return m_HalfLength; } set { m_HalfLength = value; } }

        public CapsuleGeometry(float radius, float halfLength)
        {
            m_Radius = radius;
            m_HalfLength = halfLength;
        }

        public GeometryType GeometryType => GeometryType.Capsule;
    }

    // From PxConvexMeshGeometry.h
    [StructLayout(LayoutKind.Sequential)]
    public struct ConvexMeshGeometry : IGeometry
    {
        private Vector3 m_Scale;
        private Quaternion m_Rotation;
        private IntPtr m_ConvexMesh;
        private byte m_MeshFlags;
        private byte pad1;
        private short pad2;
        private uint pad3;

        public Vector3 Scale { get { return m_Scale; } set { m_Scale = value; } }
        public Quaternion ScaleAxisRotation { get { return m_Rotation; } set { m_Rotation = value; } }

        public GeometryType GeometryType => GeometryType.ConvexMesh;
    }

    // From PxTriangleMeshGeometry.h
    [StructLayout(LayoutKind.Sequential)]
    public struct TriangleMeshGeometry : IGeometry
    {
        private Vector3 m_Scale;
        private Quaternion m_Rotation;
        private byte m_MeshFlags;
        private byte pad1;
        private short pad2;
        private IntPtr m_TriangleMesh;
        private uint pad3;

        public Vector3 Scale { get { return m_Scale; } set { m_Scale = value; } }
        public Quaternion ScaleAxisRotation { get { return m_Rotation; } set { m_Rotation = value; } }

        public GeometryType GeometryType => GeometryType.TriangleMesh;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainGeometry : IGeometry
    {
        private IntPtr m_TerrainData;
        private float m_HeightScale;
        private float m_RowScale;
        private float m_ColumnScale;
        private byte m_TerrainFlags;
        private byte pad1;
        private short pad2;
        private uint pad3;

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
                                                //   32  |   64
        private int m_Type;                     //  0-4  |  0-4
        private UInt32 m_DataStart;             //  4-8  |  4-8
        private IntPtr m_FakePointer0;          //  8-12 |  8-16
        private IntPtr m_FakePointer1;          // 12-16 | 16-24
        private fixed UInt32 m_Blob[6];         // 16-40 | 24-48

        private void SetGeometry<T>(T geometry) where T : struct, IGeometry
        {
            m_Type = (int)geometry.GeometryType;
            UnsafeUtility.CopyStructureToPtr(ref geometry, UnsafeUtility.AddressOf(ref m_DataStart));
        }

        public T As<T>() where T : struct, IGeometry
        {
            T geometry = default(T);

            if ((int)geometry.GeometryType != m_Type)
                throw new InvalidOperationException($"Unable to get geometry of type {geometry.GeometryType} from a geometry holder that stores {m_Type}.");

            UnsafeUtility.CopyPtrToStructure(UnsafeUtility.AddressOf(ref m_DataStart), out geometry);

            return geometry;
        }

        public static GeometryHolder Create<T>(T geometry) where T : struct, IGeometry
        {
            GeometryHolder holder = new GeometryHolder()
            {
                m_DataStart = 0,
                m_Type = (int)GeometryType.Invalid,
                m_FakePointer0 = new IntPtr(0xDEADBEEF),
                m_FakePointer1 = new IntPtr(0xDEADBEEF)
            };
            holder.SetGeometry<T>(geometry);
            return holder;
        }

        public GeometryType Type { get { return (GeometryType)m_Type; } }
    }
}
