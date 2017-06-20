// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace UnityEngine.XR.Tango
{
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

[StructLayout(LayoutKind.Explicit, Size = 8)]
public partial struct CoordinateFramePair
{
    [FieldOffset(0)] public CoordinateFrame baseFrame;
    [FieldOffset(4)] public CoordinateFrame targetFrame;
}

[StructLayout(LayoutKind.Explicit, Size = 88)]
public partial struct PoseData
{
    [FieldOffset(0)] public uint version;
    [FieldOffset(4)] public double timestamp;
    [FieldOffset(12)] public double orientation_x;
    [FieldOffset(20)] public double orientation_y;
    [FieldOffset(28)] public double orientation_z;
    [FieldOffset(36)] public double orientation_w;
    [FieldOffset(44)] public double translation_x;
    [FieldOffset(52)] public double translation_y;
    [FieldOffset(60)] public double translation_z;
    [FieldOffset(68)] public PoseStatus statusCode;
    [FieldOffset(72)] public CoordinateFramePair frame;
    [FieldOffset(80)] public uint confidence;
    [FieldOffset(84)] public float accuracy;
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct SimplePoseData
{
    public Vector3 position;
    public Quaternion rotation;
}

[StructLayout(LayoutKind.Explicit, Size = 24)]
public partial struct PointCloudData
{
    [FieldOffset(0)] public uint version;
    [FieldOffset(8)] public double timestamp;
    [FieldOffset(16)] public List<Vector4> points;
}

[StructLayout(LayoutKind.Explicit, Size = 48)]
public partial struct ImageData
{
    [FieldOffset(0)] public uint width;
    [FieldOffset(4)] public uint height;
    [FieldOffset(8)] public uint stride;
    [FieldOffset(12)] public double timestamp;
    [FieldOffset(20)] public long frameNumber;
    [FieldOffset(28)] public int format;
    [FieldOffset(32)] public List<byte> data;
    [FieldOffset(40)] public long exposureDurationNs;
}

public static partial class Marshal
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  System.Array ExtractArrayFromList (object list) ;

    
    
}

public static partial class TangoDevice
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool Connect (string[] boolKeys, bool[] boolValues,
            string[] intKeys, int[] intValues,
            string[] longKeys, long[] longValues,
            string[] doubleKeys, double[] doubleValues,
            string[] stringKeys, string[] stringValues) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Disconnect () ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool TryGetHorizontalFov (out float fovOut) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool TryGetVerticalFov (out float fovOut) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetRenderMode (ARRenderMode mode) ;

    public extern static uint depthCameraRate
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    public extern static bool synchronizeFramerateWithColorCamera
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        set;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetBackgroundMaterial (Material material) ;

    public static bool TryGetLatestPointCloudTangoCoords(ref PointCloudData pointCloudData)
        {
            if (pointCloudData.points == null)
            {
                pointCloudData.points = new List<Vector4>();
            }
            return false;
        }
    
    
    public static bool TryGetLatestPointCloud(CoordinateFrame baseFrame, CoordinateFrame targetFrame, ref PointCloudData pointCloudData)
        {
            if (pointCloudData.points == null)
            {
                throw new ArgumentNullException("pointCloudData");
            }
            return false;
        }
    
    
    public static bool TryGetLatestImageDataTangoCoords(ref ImageData imageData)
        {
            if (imageData.data == null)
            {
                imageData.data = new List<byte>();
            }
            return false;
        }
    
    
    public static bool FindPlane (Vector2 normalizedScreenPos, out Vector3 planeCenter, out Plane plane) {
        return INTERNAL_CALL_FindPlane ( ref normalizedScreenPos, out planeCenter, out plane );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_FindPlane (ref Vector2 normalizedScreenPos, out Vector3 planeCenter, out Plane plane);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool TryGetLatestPointCloudTangoCoordsInternal (object pointCloudData, out uint version, out double timestamp) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool TryGetLatestPointCloudInternal (CoordinateFrame baseFrame,
            CoordinateFrame targetFrame, object pointCloudData, out uint version, out double timestamp) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool TryGetLatestImageDataTangoCoordsInternal (object imageData, out uint width, out uint height,
            out uint stride, out double timestamp, out long frameNumber, out int format, out long exposureDurationNs) ;

}

public static partial class InputTracking
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool TryGetSimplePoseAtTime (CoordinateFrame baseFrame, CoordinateFrame targetFrame, out SimplePoseData pose, [uei.DefaultValue("0.0f")]  double time ) ;

    [uei.ExcludeFromDocs]
    public static bool TryGetSimplePoseAtTime (CoordinateFrame baseFrame, CoordinateFrame targetFrame, out SimplePoseData pose) {
        double time = 0.0f;
        return TryGetSimplePoseAtTime ( baseFrame, targetFrame, out pose, time );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool TryGetPoseAtTimeTangoCoords (CoordinateFrame baseFrame, CoordinateFrame targetFrame, out PoseData pose, [uei.DefaultValue("0.0f")]  double time ) ;

    [uei.ExcludeFromDocs]
    public static bool TryGetPoseAtTimeTangoCoords (CoordinateFrame baseFrame, CoordinateFrame targetFrame, out PoseData pose) {
        double time = 0.0f;
        return TryGetPoseAtTimeTangoCoords ( baseFrame, targetFrame, out pose, time );
    }

}


}
