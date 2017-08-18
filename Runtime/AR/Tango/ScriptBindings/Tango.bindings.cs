// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using UnityEngine.XR;

namespace UnityEngine.XR.Tango
{
    // This must correspond to Tango::CoordinateFrame in TangoTypes.h
    public enum CoordinateFrame
    {
        GlobalWGS84 = 0,
        AreaDescription,
        StartOfService,
        PreviousDevicePose,
        Device,
        IMU,
        Display,
        CameraColor,
        CameraDepth,
        CameraFisheye,
        UUID,
        Invalid,
        MaxCoordinateFrameType
    }

    public enum PoseStatus
    {
        Initializing = 0,
        Valid,
        Invalid,
        Unknown
    }

    [UsedByNativeCode]
    [NativeHeader("ARScriptingClasses.h")]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct CoordinateFramePair
    {
        [FieldOffset(0)] public CoordinateFrame baseFrame;
        [FieldOffset(4)] public CoordinateFrame targetFrame;
    }

    [UsedByNativeCode]
    [NativeHeader("ARScriptingClasses.h")]
    [StructLayout(LayoutKind.Explicit, Size = 92)]
    public struct PoseData
    {
        [FieldOffset(0)] public uint version;
        [FieldOffset(8)] public double timestamp;
        [FieldOffset(16)] public double orientation_x;
        [FieldOffset(24)] public double orientation_y;
        [FieldOffset(32)] public double orientation_z;
        [FieldOffset(40)] public double orientation_w;
        [FieldOffset(48)] public double translation_x;
        [FieldOffset(56)] public double translation_y;
        [FieldOffset(64)] public double translation_z;
        [FieldOffset(72)] public PoseStatus statusCode;
        [FieldOffset(76)] public CoordinateFramePair frame;
        [FieldOffset(84)] public uint confidence;
        [FieldOffset(88)] public float accuracy;

        public Quaternion rotation
        {
            get { return new Quaternion((float)orientation_x, (float)orientation_y, (float)orientation_z, (float)orientation_w); }
        }

        public Vector3 position
        {
            get { return new Vector3((float)translation_x, (float)translation_y, (float)translation_z); }
        }
    }

    public struct PointCloudData
    {
        public uint version;
        public double timestamp;
        public List<Vector4> points;
    }

    public struct ImageData
    {
        public uint width;
        public uint height;
        public uint stride;
        public double timestamp;
        public long frameNumber;
        public int format;
        public List<byte> data;
        public long exposureDurationNs;
    }

    [NativeHeader("Runtime/AR/Tango/TangoScriptApi.h")]
    [NativeConditional("PLATFORM_ANDROID")]
    public static partial class TangoDevice
    {
        extern public static CoordinateFrame baseCoordinateFrame
        {
            [NativeConditional(false)]
            get;

            [NativeThrows]
            set;
        }

        extern internal static bool Connect(
            string[] boolKeys, bool[] boolValues,
            string[] intKeys, int[] intValues,
            string[] longKeys, long[] longValues,
            string[] doubleKeys, double[] doubleValues,
            string[] stringKeys, string[] stringValues);

        extern public static void Disconnect();

        extern public static bool TryGetHorizontalFov(out float fovOut);

        extern public static bool TryGetVerticalFov(out float fovOut);

        extern internal static void SetRenderMode(ARRenderMode mode);

        extern public static uint depthCameraRate { get; set; }

        extern public static bool synchronizeFramerateWithColorCamera { get; set; }

        extern internal static void SetBackgroundMaterial(Material material);

        public static bool TryGetLatestPointCloud(ref PointCloudData pointCloudData)
        {
            if (pointCloudData.points == null)
                pointCloudData.points = new List<Vector4>();

            pointCloudData.points.Clear();
            return TryGetLatestPointCloudInternal(pointCloudData.points, out pointCloudData.version, out pointCloudData.timestamp);
        }

        extern private static bool TryGetLatestPointCloudInternal(List<Vector4> pointCloudData, out uint version, out double timestamp);

        public static bool TryGetLatestImageData(ref ImageData imageData)
        {
            if (imageData.data == null)
                imageData.data = new List<byte>();

            imageData.data.Clear();
            return TryGetLatestImageDataInternal(
                imageData.data,
                out imageData.width,
                out imageData.height,
                out imageData.stride,
                out imageData.timestamp,
                out imageData.frameNumber,
                out imageData.format,
                out imageData.exposureDurationNs);
        }

        extern private static bool TryGetLatestImageDataInternal(
            List<byte> imageData,
            out uint width,
            out uint height,
            out uint stride,
            out double timestamp,
            out long frameNumber,
            out int format,
            out long exposureDurationNs);

        extern public static bool isServiceConnected { get; }
    }

    [NativeHeader("Runtime/AR/Tango/TangoScriptApi.h")]
    [NativeConditional("PLATFORM_ANDROID")]
    public static partial class TangoInputTracking
    {
        extern public static bool TryGetPoseAtTime(CoordinateFrame baseFrame, CoordinateFrame targetFrame, out PoseData pose, [DefaultValue("0.0f")] double time);
        public static bool TryGetPoseAtTime(CoordinateFrame baseFrame, CoordinateFrame targetFrame, out PoseData pose)
        {
            return TryGetPoseAtTime(baseFrame, targetFrame, out pose, 0.0f);
        }
    }

    [UsedByNativeCode]
    [NativeHeader("Runtime/AR/Tango/TangoScriptApi.h")]
    [NativeHeader("PhysicsScriptingClasses.h")]
    [NativeConditional("PLATFORM_ANDROID")]
    public partial class MeshReconstructionServer : IDisposable
    {
        internal IntPtr m_ServerPtr = IntPtr.Zero;

        extern private static void Internal_ClearMeshes(IntPtr server);

        extern private static bool Internal_GetEnabled(IntPtr server);

        extern private static void Internal_SetEnabled(IntPtr server, bool enabled);

        extern private static IntPtr Internal_GetNativeReconstructionContextPtr(IntPtr server);

        extern private static int Internal_GetNumGenerationRequests(IntPtr server);

        public void Dispose()
        {
            if (m_ServerPtr != IntPtr.Zero)
            {
                Destroy(m_ServerPtr);
                m_ServerPtr = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        extern private static IntPtr Internal_Create(MeshReconstructionServer self, MeshReconstructionConfig config, out int status);

        extern private static void Destroy(IntPtr server);

        [NativeMethod(IsThreadSafe = true)]
        extern private static void DestroyThreaded(IntPtr server);

        extern private static void Internal_GetChangedSegments(IntPtr serverPtr, SegmentChangedDelegate onSegmentChanged);

        extern private static void Internal_GenerateSegmentAsync(
            IntPtr serverPtr,
            GridIndex gridIndex,
            MeshFilter destinationMeshFilter,
            MeshCollider destinationMeshCollider,
            SegmentReadyDelegate onSegmentReady,
            bool provideNormals,
            bool provideColors,
            bool providePhysics);
    }
}
