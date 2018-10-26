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
    internal enum CoordinateFrame
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

    internal enum PoseStatus
    {
        Initializing = 0,
        Valid,
        Invalid,
        Unknown
    }

    [UsedByNativeCode]
    [NativeHeader("ARScriptingClasses.h")]
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal struct CoordinateFramePair
    {
        [FieldOffset(0)] public CoordinateFrame baseFrame;
        [FieldOffset(4)] public CoordinateFrame targetFrame;
    }

    [UsedByNativeCode]
    [NativeHeader("ARScriptingClasses.h")]
    [StructLayout(LayoutKind.Explicit, Size = 60)]
    internal struct PoseData
    {
        [FieldOffset(0)] public double orientation_x;
        [FieldOffset(8)] public double orientation_y;
        [FieldOffset(16)] public double orientation_z;
        [FieldOffset(24)] public double orientation_w;
        [FieldOffset(32)] public double translation_x;
        [FieldOffset(40)] public double translation_y;
        [FieldOffset(48)] public double translation_z;
        [FieldOffset(56)] public PoseStatus statusCode;

        public Quaternion rotation
        {
            get { return new Quaternion((float)orientation_x, (float)orientation_y, (float)orientation_z, (float)orientation_w); }
        }

        public Vector3 position
        {
            get { return new Vector3((float)translation_x, (float)translation_y, (float)translation_z); }
        }
    }

    internal struct PointCloudData
    {
        public uint version;
        public double timestamp;
        public List<Vector4> points;
    }

    internal struct ImageData
    {
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        [NativeHeader("Runtime/AR/Tango/TangoScriptApi.h")]
        // Must match Tango::ImagePlaneInfo in TangoScriptApi.h
        public struct PlaneInfo
        {
            public int size;
            public int rowStride;
            public int pixelStride;
            public uint offset;
        }

        [StructLayout(LayoutKind.Sequential)]
        // Must match Tango::CameraMetadata in TangoTypes.h
        public struct CameraMetadata
        {
            public long timestampNs;
            public long frameNumber;
            public long exposureDurationNs;
            public int sensitivityIso;
            public float lensAperture;
            public int colorCorrectionMode;
            public float colorCorrectionGains0;
            public float colorCorrectionGains1;
            public float colorCorrectionGains2;
            public float colorCorrectionGains3;
            public float colorCorrectionTransform0;
            public float colorCorrectionTransform1;
            public float colorCorrectionTransform2;
            public float colorCorrectionTransform3;
            public float colorCorrectionTransform4;
            public float colorCorrectionTransform5;
            public float colorCorrectionTransform6;
            public float colorCorrectionTransform7;
            public float colorCorrectionTransform8;
            public float sensorNeutralColorPoint0;
            public float sensorNeutralColorPoint1;
            public float sensorNeutralColorPoint2;
        }

        public uint width;
        public uint height;
        public int format;
        public long timestampNs;
        public List<byte> planeData;
        public List<PlaneInfo> planeInfos;
        public CameraMetadata metadata;
    }

    internal struct NativePointCloud
    {
        public uint version;
        public double timestamp;
        public uint numPoints;
        public IntPtr points;
        public IntPtr nativePtr;
    }

    internal struct NativeImage
    {
        public uint width;
        public uint height;
        public int format;
        public long timestampNs;
        public IntPtr planeData;
        public IntPtr nativePtr;
        public List<ImageData.PlaneInfo> planeInfos;
        public ImageData.CameraMetadata metadata;
    }

    [NativeHeader("Runtime/AR/Tango/TangoScriptApi.h")]
    [NativeConditional("PLATFORM_ANDROID")]
    internal static partial class TangoDevice
    {
        extern internal static CoordinateFrame baseCoordinateFrame
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

        extern internal static void Disconnect();

        extern internal static bool TryGetHorizontalFov(out float fovOut);

        extern internal static bool TryGetVerticalFov(out float fovOut);

        extern internal static void SetRenderMode(ARRenderMode mode);

        extern internal static uint depthCameraRate { get; set; }

        extern internal static bool synchronizeFramerateWithColorCamera { get; set; }

        extern internal static void SetBackgroundMaterial(Material material);

        internal static bool TryGetLatestPointCloud(ref PointCloudData pointCloudData)
        {
            if (pointCloudData.points == null)
                pointCloudData.points = new List<Vector4>();

            pointCloudData.points.Clear();
            return TryGetLatestPointCloudInternal(pointCloudData.points, out pointCloudData.version, out pointCloudData.timestamp);
        }

        extern private static bool TryGetLatestPointCloudInternal(List<Vector4> pointCloudData, out uint version, out double timestamp);

        internal static bool TryGetLatestImageData(ref ImageData image)
        {
            if (image.planeData == null)
                image.planeData = new List<byte>();

            if (image.planeInfos == null)
                image.planeInfos = new List<ImageData.PlaneInfo>();

            image.planeData.Clear();
            return TryGetLatestImageDataInternal(
                image.planeData,
                image.planeInfos,
                out image.width,
                out image.height,
                out image.format,
                out image.timestampNs,
                out image.metadata);
        }

        extern private static bool TryGetLatestImageDataInternal(
            List<byte> imageData,
            List<ImageData.PlaneInfo> planeInfos,
            out uint width,
            out uint height,
            out int format,
            out long timestampNs,
            out ImageData.CameraMetadata metadata);

        extern internal static bool isServiceConnected { get; }
        extern internal static bool isServiceAvailable { get; }

        internal static bool TryAcquireLatestPointCloud(ref NativePointCloud pointCloud)
        {
            return Internal_TryAcquireLatestPointCloud(
                out pointCloud.version,
                out pointCloud.timestamp,
                out pointCloud.numPoints,
                out pointCloud.points,
                out pointCloud.nativePtr);
        }

        internal static void ReleasePointCloud(IntPtr pointCloudNativePtr)
        {
            Internal_ReleasePointCloud(pointCloudNativePtr);
        }

        internal static bool TryAcquireLatestImageBuffer(ref NativeImage nativeImage)
        {
            if (nativeImage.planeInfos == null)
                nativeImage.planeInfos = new List<ImageData.PlaneInfo>();

            return Internal_TryAcquireLatestImageBuffer(
                nativeImage.planeInfos,
                out nativeImage.width,
                out nativeImage.height,
                out nativeImage.format,
                out nativeImage.timestampNs,
                out nativeImage.planeData,
                out nativeImage.nativePtr,
                out nativeImage.metadata);
        }

        internal static void ReleaseImageBuffer(IntPtr imageBufferNativePtr)
        {
            Internal_ReleaseImageBuffer(imageBufferNativePtr);
        }

        extern private static bool Internal_TryAcquireLatestImageBuffer(
            List<ImageData.PlaneInfo> planeInfos,
            out uint width,
            out uint height,
            out int format,
            out Int64 timestampNs,
            out IntPtr planeData,
            out IntPtr nativePtr,
            out ImageData.CameraMetadata metadata);

        extern private static bool Internal_TryAcquireLatestPointCloud(
            out uint version,
            out double timestamp,
            out uint numPoints,
            out IntPtr points,
            out IntPtr nativePtr);

        [NativeThrows]
        extern private static void Internal_ReleasePointCloud(IntPtr pointCloudPtr);

        [NativeThrows]
        extern private static void Internal_ReleaseImageBuffer(IntPtr imageBufferPtr);
    }

    [NativeHeader("Runtime/AR/Tango/TangoScriptApi.h")]
    [NativeConditional("PLATFORM_ANDROID")]
    internal static partial class TangoInputTracking
    {
        extern private static bool Internal_TryGetPoseAtTime(double time, ScreenOrientation screenOrientation, CoordinateFrame baseFrame, CoordinateFrame targetFrame, out PoseData pose);

        internal static bool TryGetPoseAtTime(out PoseData pose, CoordinateFrame baseFrame, CoordinateFrame targetFrame, double time, ScreenOrientation screenOrientation)
        {
            return Internal_TryGetPoseAtTime(time, screenOrientation, baseFrame, targetFrame, out pose);
        }

        internal static bool TryGetPoseAtTime(out PoseData pose, CoordinateFrame baseFrame, CoordinateFrame targetFrame, double time = 0.0)
        {
            return Internal_TryGetPoseAtTime(time, Screen.orientation, baseFrame, targetFrame, out pose);
        }
    }

    [UsedByNativeCode]
    [NativeHeader("Runtime/AR/Tango/TangoScriptApi.h")]
    [NativeHeader("PhysicsScriptingClasses.h")]
    [NativeConditional("PLATFORM_ANDROID")]
    internal partial class MeshReconstructionServer
    {
        internal IntPtr m_ServerPtr = IntPtr.Zero;

        extern private static void Internal_ClearMeshes(IntPtr server);

        extern private static bool Internal_GetEnabled(IntPtr server);

        extern private static void Internal_SetEnabled(IntPtr server, bool enabled);

        extern private static IntPtr Internal_GetNativeReconstructionContextPtr(IntPtr server);

        extern private static int Internal_GetNumGenerationRequests(IntPtr server);

        internal void Dispose()
        {
            if (m_ServerPtr != IntPtr.Zero)
            {
                Destroy(m_ServerPtr);
                m_ServerPtr = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        extern private static IntPtr Internal_Create(MeshReconstructionServer self, MeshReconstructionConfig config, out int status);

        extern internal static void Destroy(IntPtr server);

        [NativeMethod(IsThreadSafe = true)]
        extern internal static void DestroyThreaded(IntPtr server);

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
