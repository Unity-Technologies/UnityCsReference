// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEditor.SpeedTree.Importer
{
    #region Data Classes

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct Vec2
    {
        private float x, y;

        public float X
        {
            get { return x; }
            set { x = value; }
        }

        public float Y
        {
            get { return y; }
            set { y = value; }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct Vec3
    {
        private float x, y, z;

        public float X
        {
            get { return x; }
            set { x = value; }
        }

        public float Y
        {
            get { return y; }
            set { y = value; }
        }

        public float Z
        {
            get { return z; }
            set { z = value; }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct Vec4
    {
        private float x, y, z, w;

        public float X
        {
            get { return x; }
            set { x = value; }
        }

        public float Y
        {
            get { return y; }
            set { y = value; }
        }

        public float Z
        {
            get { return z; }
            set { z = value; }
        }

        public float W
        {
            get { return w; }
            set { w = value; }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct Bounds3
    {
        public Vec3 Min { get; private set; }
        public Vec3 Max { get; private set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct Vertex
    {
        public Vec3 Anchor { get; private set; }
        public Vec3 Offset { get; private set; }
        public Vec3 LodOffset { get; private set; }
        public Vec3 Normal { get; private set; }
        public Vec3 Tangent { get; private set; }
        public Vec3 Binormal { get; private set; }
        public Vec2 TexCoord { get; private set; }
        public Vec2 LightmapTexCoord { get; private set; }
        public Vec3 Color { get; private set; }
        public float AmbientOcclusion { get; private set; }
        public float BlendWeight { get; private set; }
        public Vec3 BranchWind1 { get; private set; } // pos, dir, weight
        public Vec3 BranchWind2 { get; private set; }
        public float RippleWeight { get; private set; }
        public bool CameraFacing { get; private set; }
        public uint BoneID { get; private set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct DrawCall
    {
        public uint MaterialIndex { get; private set; }
        public bool ContainsFacingGeometry { get; private set; }
        public uint IndexStart { get; private set; }
        public uint IndexCount { get; private set; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct Bone
    {
        public uint ID { get; private set; }
        public bool ParentID { get; private set; }
        public Vec3 Start { get; private set; }
        public Vec3 End { get; private set; }
        public float Radius { get; private set; }
    }

    #endregion

    #region Data Tables

    class Lod : Table
    {
        public SpeedTreeDataArray<Vertex> Vertices => GetContainer<SpeedTreeDataArray<Vertex>>(0);
        public SpeedTreeDataArray<uint> Indices => GetContainer<SpeedTreeDataArray<uint>>(1);
        public SpeedTreeDataArray<DrawCall> DrawCalls => GetContainer<SpeedTreeDataArray<DrawCall>>(2);
    }

    class MaterialMap : Table
    {
        public bool Used => GetBool(0);
        public string Path => GetString(1);
        public Vec4 Color => GetStruct<Vec4>(2);
    }

    class STMaterial : Table
    {
        public string Name => GetString(0);
        public bool TwoSided => GetBool(1);
        public bool FlipNormalsOnBackside => GetBool(2);
        public bool Billboard => GetBool(3);
        public TableArray<MaterialMap> Maps => GetContainer<TableArray<MaterialMap>>(4);
    }

    class BillboardInfo : Table
    {
        public bool LastLodIsBillboard => GetBool(0);
        public bool IncludesTopDown => GetBool(1);
        public uint SideViewCount => GetUInt(2);
    }

    class CollisionObject : Table
    {
        public Vec3 Position => GetStruct<Vec3>(0);
        public Vec3 Position2 => GetStruct<Vec3>(1);
        public float Radius => GetFloat(2);
        public string UserData => GetString(3);
    }

    #endregion

    #region Wind Data Tables

    class WindConfigCommon : Table
    {
        public float StrengthResponse => GetFloat(0);
        public float DirectionResponse => GetFloat(1);
        public float GustFrequency => GetFloat(5);
        public float GustStrengthMin => GetFloat(6);
        public float GustStrengthMax => GetFloat(7);
        public float GustDurationMin => GetFloat(8);
        public float GustDurationMax => GetFloat(9);
        public float GustRiseScalar => GetFloat(10);
        public float GustFallScalar => GetFloat(11);
        public float CurrentStrength => GetFloat(15);
    };

    class WindConfigSDK : Table
    {
        internal class WindBranch : Table
        {
            public SpeedTreeDataArray<float> Bend => GetContainer<SpeedTreeDataArray<float>>(0);
            public SpeedTreeDataArray<float> Oscillation => GetContainer<SpeedTreeDataArray<float>>(1);
            public SpeedTreeDataArray<float> Speed => GetContainer<SpeedTreeDataArray<float>>(2);
            public SpeedTreeDataArray<float> Turbulence => GetContainer<SpeedTreeDataArray<float>>(3);
            public SpeedTreeDataArray<float> Flexibility => GetContainer<SpeedTreeDataArray<float>>(4);
            public float Independence => GetFloat(5);
        };

        internal class WindRipple : Table
        {
            public SpeedTreeDataArray<float> Planar => GetContainer<SpeedTreeDataArray<float>>(0);
            public SpeedTreeDataArray<float> Directional => GetContainer<SpeedTreeDataArray<float>>(1);
            public SpeedTreeDataArray<float> Speed => GetContainer<SpeedTreeDataArray<float>>(2);
            public SpeedTreeDataArray<float> Flexibility => GetContainer<SpeedTreeDataArray<float>>(3);
            public float Shimmer => GetFloat(4);
            public float Independence => GetFloat(5);
        };

        public WindConfigCommon Common => GetContainer<WindConfigCommon>(0);
        public WindBranch Shared => GetContainer<WindBranch>(1);
        public WindBranch Branch1 => GetContainer<WindBranch>(2);
        public WindBranch Branch2 => GetContainer<WindBranch>(3);
        public WindRipple Ripple => GetContainer<WindRipple>(4);

        public float SharedStartHeight => GetFloat(10);
        public float Branch1StretchLimit => GetFloat(11);
        public float Branch2StretchLimit => GetFloat(12);

        public bool DoShared => GetBool(15);
        public bool DoBranch1 => GetBool(16);
        public bool DoBranch2 => GetBool(17);
        public bool DoRipple => GetBool(18);
        public bool DoShimmer => GetBool(19);
    };

    #endregion

    class SpeedTree9Reader : Reader
    {
        private uint m_VersionMajor;
        private uint m_VersionMinor;
        private Bounds3 m_Bounds;
        private TableArray<Lod> m_Lods;
        private BillboardInfo m_BillboardInfo;
        private TableArray<CollisionObject> m_CollisionObjects;
        private TableArray<STMaterial> m_Materials;
        private WindConfigSDK m_Wind;

        public SpeedTree9Reader(string filename)
        {
            base.LoadFile(filename, "SpeedTree9______");

            m_VersionMajor = GetUInt(0);
            m_VersionMinor = GetUInt(1);
            m_Bounds = GetStruct<Bounds3>(2);
            m_Lods = GetContainer<TableArray<Lod>>(5);
            m_BillboardInfo = GetContainer<BillboardInfo>(6);
            m_CollisionObjects = GetContainer<TableArray<CollisionObject>>(7);
            m_Materials = GetContainer<TableArray<STMaterial>>(10);
            m_Wind = GetContainer<WindConfigSDK>(15);
        }


        // File info
        public uint VersionMajor => m_VersionMajor;
        public uint VersionMinor => m_VersionMinor;
        public Bounds3 Bounds => m_Bounds;

        // Geometry info
        public TableArray<Lod> Lods => m_Lods;
        public BillboardInfo BillboardInfo => m_BillboardInfo;
        public TableArray<CollisionObject> CollisionObjects => m_CollisionObjects;

        // Material info
        public TableArray<STMaterial> Materials => m_Materials;

        // Wind
        public WindConfigSDK Wind => m_Wind;
    }
}
