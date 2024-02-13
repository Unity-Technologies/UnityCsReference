// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

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
    interface ReaderData
    {
        public void Initialize(Byte[] data, int offset);
    }

    class Lod : ReaderData
    {
        private enum OffsetData
        {
            Vertices = 0,
            Indices = 1,
            DrawCalls = 2
        }

        public Vertex[] Vertices { get; private set; }
        public uint[] Indices { get; private set; }
        public DrawCall[] DrawCalls { get; private set; }

        public void Initialize(Byte[] data, int offset)
        {
            Vertices = SpeedTree9ReaderUtility.GetArray<Vertex>(data, offset, (int)OffsetData.Vertices);
            Indices = SpeedTree9ReaderUtility.GetArray<uint>(data, offset, (int)OffsetData.Indices);
            DrawCalls = SpeedTree9ReaderUtility.GetArray<DrawCall>(data, offset, (int)OffsetData.DrawCalls);
        }
    }

    class BillboardInfo : ReaderData
    {
        private enum OffsetData
        {
            LastLodIsBillboard = 0,
            IncludesTopDown = 1,
            SideViewCount = 2
        }

        public bool LastLodIsBillboard { get; private set; }
        public bool IncludesTopDown { get; private set; }
        public uint SideViewCount { get; private set; }

        public void Initialize(Byte[] data, int offset)
        {
            LastLodIsBillboard = SpeedTree9ReaderUtility.GetBool(data, offset, (int)OffsetData.LastLodIsBillboard);
            IncludesTopDown = SpeedTree9ReaderUtility.GetBool(data, offset, (int)OffsetData.IncludesTopDown);
            SideViewCount = SpeedTree9ReaderUtility.GetUInt(data, offset, (int)OffsetData.SideViewCount);
        }
    }

    class CollisionObject : ReaderData
    {
        private enum OffsetData
        {
            Position = 0,
            Position2 = 1,
            Radius = 2,
            UserData = 3
        }

        public Vec3 Position { get; private set; }
        public Vec3 Position2 { get; private set; }
        public float Radius { get; private set; }
        public string UserData { get; private set; }

        public void Initialize(Byte[] data, int offset)
        {
            Position = SpeedTree9ReaderUtility.GetStruct<Vec3>(data, offset, (int)OffsetData.Position);
            Position2 = SpeedTree9ReaderUtility.GetStruct<Vec3>(data, offset, (int)OffsetData.Position2);
            Radius = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.Radius);
            UserData = SpeedTree9ReaderUtility.GetString(data, offset, (int)OffsetData.UserData);
        }
    }

    class MaterialMap : ReaderData
    {
        private enum OffsetData
        {
            Used = 0,
            Path = 1,
            Color = 2
        }

        public bool Used { get; private set; }
        public string Path { get; private set; }
        public Vec4 Color { get; private set; }

        public void Initialize(Byte[] data, int offset)
        {
            Used = SpeedTree9ReaderUtility.GetBool(data, offset, (int)OffsetData.Used);
            Path = SpeedTree9ReaderUtility.GetString(data, offset, (int)OffsetData.Path);
            Color = SpeedTree9ReaderUtility.GetStruct<Vec4>(data, offset, (int)OffsetData.Color);
        }
    }

    class STMaterial : ReaderData
    {
        private enum OffsetData
        {
            Name = 0,
            TwoSided = 1,
            FlipNormalsOnBackside = 2,
            Billboard = 3,
            Maps = 4
        }

        public string Name { get; private set; }
        public bool TwoSided { get; private set; }
        public bool FlipNormalsOnBackside { get; private set; }
        public bool Billboard { get; private set; }
        public MaterialMap[] Maps { get; private set; }

        public void Initialize(Byte[] data, int offset)
        {
            Name = SpeedTree9ReaderUtility.GetString(data, offset, (int)OffsetData.Name);
            TwoSided = SpeedTree9ReaderUtility.GetBool(data, offset, (int)OffsetData.TwoSided);
            FlipNormalsOnBackside = SpeedTree9ReaderUtility.GetBool(data, offset, (int)OffsetData.FlipNormalsOnBackside);
            Billboard = SpeedTree9ReaderUtility.GetBool(data, offset, (int)OffsetData.Billboard);
            Maps = SpeedTree9ReaderUtility.GetDataObjectArray<MaterialMap>(data, offset, (int)OffsetData.Maps);
        }
    }
    #endregion

    #region Wind Data Tables
    class WindConfigCommon : ReaderData
    {
        private enum OffsetData
        {
            StrengthResponse = 0,
            DirectionResponse = 1,
            GustFrequency = 5,
            GustStrengthMin = 6,
            GustStrengthMax = 7,
            GustDurationMin = 8,
            GustDurationMax = 9,
            GustRiseScalar = 10,
            GustFallScalar = 11,
            CurrentStrength = 15
        }

        public float StrengthResponse { get; private set; }
        public float DirectionResponse { get; private set; }
        public float GustFrequency { get; private set; }
        public float GustStrengthMin { get; private set; }
        public float GustStrengthMax { get; private set; }
        public float GustDurationMin { get; private set; }
        public float GustDurationMax { get; private set; }
        public float GustRiseScalar { get; private set; }
        public float GustFallScalar { get; private set; }
        public float CurrentStrength { get; private set; }

        public void Initialize(Byte[] data, int offset)
        {
            StrengthResponse = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.StrengthResponse);
            DirectionResponse = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.DirectionResponse);
            GustFrequency = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.GustFrequency);
            GustStrengthMin = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.GustStrengthMin);
            GustStrengthMax = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.GustStrengthMax);
            GustDurationMin = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.GustDurationMin);
            GustDurationMax = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.GustDurationMax);
            GustRiseScalar = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.GustRiseScalar);
            GustFallScalar = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.GustFallScalar);
            CurrentStrength = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.CurrentStrength);
        }
    };

    class WindConfigSDK : ReaderData
    {
        internal class WindBranch : ReaderData
        {
            private enum OffsetData
            {
                Bend = 0,
                Oscillation = 1,
                Speed = 2,
                Turbulence = 3,
                Flexibility = 4,
                Independence = 5
            }

            public float[] Bend { get; private set; }
            public float[] Oscillation { get; private set; }
            public float[] Speed { get; private set; }
            public float[] Turbulence { get; private set; }
            public float[] Flexibility { get; private set; }
            public float Independence { get; private set; }

            public void Initialize(Byte[] data, int offset)
            {
                Bend = SpeedTree9ReaderUtility.GetArray<float>(data, offset, (int)OffsetData.Bend);
                Oscillation = SpeedTree9ReaderUtility.GetArray<float>(data, offset, (int)OffsetData.Oscillation);
                Speed = SpeedTree9ReaderUtility.GetArray<float>(data, offset, (int)OffsetData.Speed);
                Turbulence = SpeedTree9ReaderUtility.GetArray<float>(data, offset, (int)OffsetData.Turbulence);
                Flexibility = SpeedTree9ReaderUtility.GetArray<float>(data, offset, (int)OffsetData.Flexibility);
                Independence = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.Independence);
            }
        };

        internal class WindRipple : ReaderData
        {
            private enum OffsetData
            {
                Planar = 0,
                Directional = 1,
                Speed = 2,
                Flexibility = 3,
                Shimmer = 4,
                Independence = 5
            }

            public float[] Planar { get; private set; }
            public float[] Directional { get; private set; }
            public float[] Speed { get; private set; }
            public float[] Flexibility { get; private set; }
            public float Shimmer { get; private set; }
            public float Independence { get; private set; }

            public void Initialize(Byte[] data, int offset)
            {
                Planar = SpeedTree9ReaderUtility.GetArray<float>(data, offset, (int)OffsetData.Planar);
                Directional = SpeedTree9ReaderUtility.GetArray<float>(data, offset, (int)OffsetData.Directional);
                Speed = SpeedTree9ReaderUtility.GetArray<float>(data, offset, (int)OffsetData.Speed);
                Flexibility = SpeedTree9ReaderUtility.GetArray<float>(data, offset, (int)OffsetData.Flexibility);
                Shimmer = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.Shimmer);
                Independence = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.Independence);
            }
        };

        private enum OffsetData
        {
            Common = 0,
            Shared = 1,
            Branch1 = 2,
            Branch2 = 3,
            Ripple = 4,
            SharedStartHeight = 10,
            Branch1StretchLimit = 11,
            Branch2StretchLimit = 12,
            DoShared = 15,
            DoBranch1 = 16,
            DoBranch2 = 17,
            DoRipple = 18,
            DoShimmer = 19
        }

        public WindConfigCommon Common { get; private set; }
        public WindBranch Shared { get; private set; }
        public WindBranch Branch1 { get; private set; }
        public WindBranch Branch2 { get; private set; }
        public WindRipple Ripple { get; private set; }

        public float SharedStartHeight { get; private set; }
        public float Branch1StretchLimit { get; private set; }
        public float Branch2StretchLimit { get; private set; }

        public bool DoShared { get; private set; }
        public bool DoBranch1 { get; private set; }
        public bool DoBranch2 { get; private set; }
        public bool DoRipple { get; private set; }
        public bool DoShimmer { get; private set; }

        public void Initialize(Byte[] data, int offset)
        {
            Common = SpeedTree9ReaderUtility.GetDataObject<WindConfigCommon>(data, offset, (int)OffsetData.Common);
            Shared = SpeedTree9ReaderUtility.GetDataObject<WindBranch>(data, offset, (int)OffsetData.Shared);
            Branch1 = SpeedTree9ReaderUtility.GetDataObject<WindBranch>(data, offset, (int)OffsetData.Branch1);
            Branch2 = SpeedTree9ReaderUtility.GetDataObject<WindBranch>(data, offset, (int)OffsetData.Branch2);
            Ripple = SpeedTree9ReaderUtility.GetDataObject<WindRipple>(data, offset, (int)OffsetData.Ripple);

            SharedStartHeight = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.SharedStartHeight);
            Branch1StretchLimit = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.Branch1StretchLimit);
            Branch2StretchLimit = SpeedTree9ReaderUtility.GetFloat(data, offset, (int)OffsetData.Branch2StretchLimit);

            DoShared = SpeedTree9ReaderUtility.GetBool(data, offset, (int)OffsetData.DoShared);
            DoBranch1 = SpeedTree9ReaderUtility.GetBool(data, offset, (int)OffsetData.DoBranch1);
            DoBranch2 = SpeedTree9ReaderUtility.GetBool(data, offset, (int)OffsetData.DoBranch2);
            DoRipple = SpeedTree9ReaderUtility.GetBool(data, offset, (int)OffsetData.DoRipple);
            DoShimmer = SpeedTree9ReaderUtility.GetBool(data, offset, (int)OffsetData.DoShimmer);
        }
    };
    #endregion

    #region Reader Utilities
    static class SpeedTree9ReaderUtility
    {
        public static int GetOffset(in Byte[] data, int offsetIn, int index)
        {
            int offset = offsetIn + (index + 1) * 4;
            return offsetIn + (int)BitConverter.ToUInt32(data, offset);
        }

        public static float GetFloat(in Byte[] data, int offsetIn, int index)
        {
            return BitConverter.ToSingle(data, GetOffset(data, offsetIn, index));
        }

        public static string GetString(in Byte[] data, int offsetIn, int index)
        {
            int offset = GetOffset(data, offsetIn, index);
            int length = (int)BitConverter.ToUInt32(data, offset);
            return System.Text.Encoding.UTF8.GetString(data, offset + 4, length - 1);
        }

        public static T GetStruct<T>(in Byte[] data, int offsetIn, int index) where T : struct
        {
            int offset = GetOffset(data, offsetIn, index);
            return MemoryMarshal.Cast<byte, T>(data.AsSpan().Slice(offset))[0];
        }

        public static bool GetBool(in Byte[] data, int offsetIn, int index)
        {
            return GetInt(data, offsetIn, index) != 0;
        }

        public static int GetInt(in Byte[] data, int offsetIn, int index)
        {
            return BitConverter.ToInt32(data, GetOffset(data, offsetIn, index));
        }

        public static uint GetUInt(in Byte[] data, int offsetIn, int index)
        {
            return BitConverter.ToUInt32(data, GetOffset(data, offsetIn, index));
        }

        public static T[] GetArray<T>(Byte[] data, int offset, int type) where T : struct
        {
            int offsetData = SpeedTree9ReaderUtility.GetOffset(data, offset, (int)type);
            uint countData = BitConverter.ToUInt32(data, offsetData);

            T[] array = new T[countData];

            for (int i = 0; i < countData; i++)
            {
                array[i] = MemoryMarshal.Cast<byte, T>(data.AsSpan().Slice(offsetData + 4))[i];
            }

            return array;
        }

        public static T GetDataObject<T>(in Byte[] data, int offset, int index) where T : ReaderData, new()
        {
            int dataOffset = SpeedTree9ReaderUtility.GetOffset(data, offset, index);

            T readData = new T();
            readData.Initialize(data, dataOffset);

            return readData;
        }

        public static T[] GetDataObjectArray<T>(in Byte[] data, int offset, int index) where T : ReaderData, new()
        {
            int dataOffset = SpeedTree9ReaderUtility.GetOffset(data, offset, index);
            uint dataCount = BitConverter.ToUInt32(data, dataOffset);

            T[] readData = new T[dataCount];

            for (int i = 0; i < dataCount; i++)
            {
                int offsetNew = SpeedTree9ReaderUtility.GetOffset(data, dataOffset, i);

                T newData = new T();
                newData.Initialize(data, offsetNew);

                readData[i] = newData;
            }

            return readData;
        }
    }
    #endregion

    internal class SpeedTree9Reader : IDisposable
    {
        internal enum FileStatus
        {
            Valid = 0,
            InvalidPath = 1,
            InvalidSignature = 2
        }

        enum OffsetData
        {
            VersionMajor = 0,
            VersionMinor = 1,
            Bounds = 2,
            Lod = 5,
            BillboardInfo = 6,
            CollisionObjects = 7,
            Materials = 10,
            Wind = 15
        }

        const string k_TokenKey = "SpeedTree9______";

        public uint VersionMajor { get; private set; }
        public uint VersionMinor { get; private set; }
        public Bounds3 Bounds { get; private set; }
        public Lod[] Lod { get; private set; }
        public BillboardInfo BillboardInfo { get; private set; }
        public CollisionObject[] CollisionObjects { get; private set; }
        public STMaterial[] Materials { get; private set; }
        public WindConfigSDK Wind { get; private set; }

        private string m_AssetPath;
        private FileStream m_FileStream;
        private int m_Offset;
        private bool m_Disposed;

        public FileStatus Initialize(string assetPath)
        {
            m_AssetPath = assetPath;
            m_FileStream = new FileStream(m_AssetPath, FileMode.Open, FileAccess.Read);

            if (!File.Exists(assetPath))
            {
                return FileStatus.InvalidPath;
            }

            if (!ValidateFileSignature())
            {
                return FileStatus.InvalidSignature;
            }

            return FileStatus.Valid;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                // Manual release of managed resources.
                if (disposing)
                {
                    if (m_FileStream != null)
                    {
                        m_FileStream.Close();
                    }
                }

                // Release unmanaged resources.
                m_Disposed = true;
            }
        }

        public void ReadContent()
        {
            byte[] data = File.ReadAllBytes(m_AssetPath);

            VersionMajor = SpeedTree9ReaderUtility.GetUInt(data, m_Offset, (int)OffsetData.VersionMajor);
            VersionMinor = SpeedTree9ReaderUtility.GetUInt(data, m_Offset, (int)OffsetData.VersionMinor);
            Bounds = SpeedTree9ReaderUtility.GetStruct<Bounds3>(data, m_Offset, (int)OffsetData.Bounds);
            Lod = SpeedTree9ReaderUtility.GetDataObjectArray<Lod>(data, m_Offset, (int)OffsetData.Lod);
            BillboardInfo = SpeedTree9ReaderUtility.GetDataObject<BillboardInfo>(data, m_Offset, (int)OffsetData.BillboardInfo);
            CollisionObjects = SpeedTree9ReaderUtility.GetDataObjectArray<CollisionObject>(data, m_Offset, (int)OffsetData.CollisionObjects);
            Materials = SpeedTree9ReaderUtility.GetDataObjectArray<STMaterial>(data, m_Offset, (int)OffsetData.Materials);
            Wind = SpeedTree9ReaderUtility.GetDataObject<WindConfigSDK>(data, m_Offset, (int)OffsetData.Wind);

            if (m_FileStream != null)
            {
                m_FileStream.Close();
            }
        }

        private unsafe bool ValidateFileSignature()
        {
            byte[] buffer = new byte[k_TokenKey.Length];

            try
            {
                int bytesRead = m_FileStream.Read(buffer, m_Offset, k_TokenKey.Length);

                if (bytesRead != buffer.Length)
                {
                    return false;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.LogError(ex.Message);
            }

            bool valid = true;
            for (int i = 0; i < k_TokenKey.Length && valid; ++i)
            {
                valid &= (k_TokenKey[i] == buffer[i]);
            }

            if (valid)
            {
                m_Offset = k_TokenKey.Length;
            }

            return valid;
        }
    }
}
