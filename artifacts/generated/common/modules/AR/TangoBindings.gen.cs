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

[StructLayout(LayoutKind.Explicit, Size = 92)]
public partial struct PoseData
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
    public extern static CoordinateFrame baseCoordinateFrame
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

    public static bool TryGetLatestPointCloud(ref PointCloudData pointCloudData)
        {
            if (pointCloudData.points == null)
            {
                pointCloudData.points = new List<Vector4>();
            }
            return false;
        }
    
    
    public static bool TryGetLatestImageData(ref ImageData imageData)
        {
            if (imageData.data == null)
            {
                imageData.data = new List<byte>();
            }
            return false;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool TryGetLatestPointCloudInternal (object pointCloudData, out uint version, out double timestamp) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool TryGetLatestImageDataInternal (object imageData, out uint width, out uint height,
            out uint stride, out double timestamp, out long frameNumber, out int format, out long exposureDurationNs) ;

    public extern static bool isServiceConnected
    {
        [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
        [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
        get;
    }

}

public static partial class TangoInputTracking
{
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  bool TryGetPoseAtTime (CoordinateFrame baseFrame, CoordinateFrame targetFrame, out PoseData pose, [uei.DefaultValue("0.0f")]  double time ) ;

    [uei.ExcludeFromDocs]
    public static bool TryGetPoseAtTime (CoordinateFrame baseFrame, CoordinateFrame targetFrame, out PoseData pose) {
        double time = 0.0f;
        return TryGetPoseAtTime ( baseFrame, targetFrame, out pose, time );
    }

}

public sealed partial class MeshReconstructionServer : IDisposable
{
    private IntPtr m_ServerPtr = IntPtr.Zero;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_ClearMeshes (IntPtr server) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  bool Internal_GetEnabled (IntPtr server) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_SetEnabled (IntPtr server, bool enabled) ;

    private static IntPtr Internal_GetNativeReconstructionContextPtr (IntPtr server) {
        IntPtr result;
        INTERNAL_CALL_Internal_GetNativeReconstructionContextPtr ( server, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_GetNativeReconstructionContextPtr (IntPtr server, out IntPtr value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  int Internal_GetNumGenerationRequests (IntPtr server) ;

    public void Dispose()
        {
            if (m_ServerPtr != IntPtr.Zero)
            {
                Destroy(m_ServerPtr);
                m_ServerPtr = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }
    
    
    private IntPtr Internal_Create (MeshReconstructionConfig config) {
        IntPtr result;
        INTERNAL_CALL_Internal_Create ( this, ref config, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_Create (MeshReconstructionServer self, ref MeshReconstructionConfig config, out IntPtr value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Destroy (IntPtr server) ;

    [ThreadAndSerializationSafe ()]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void DestroyThreaded (IntPtr server) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_GetChangedSegments (IntPtr serverPtr, SegmentChangedDelegate onSegmentChanged) ;

    private static void Internal_GenerateSegmentAsync (
            IntPtr serverPtr,
            GridIndex gridIndex,
            MeshFilter destinationMeshFilter,
            MeshCollider destinationMeshCollider,
            SegmentReadyDelegate onSegmentReady,
            bool provideNormals,
            bool provideColors,
            bool providePhysics) {
        INTERNAL_CALL_Internal_GenerateSegmentAsync ( serverPtr, ref gridIndex, destinationMeshFilter, destinationMeshCollider, onSegmentReady, provideNormals, provideColors, providePhysics );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_Internal_GenerateSegmentAsync (IntPtr serverPtr, ref GridIndex gridIndex, MeshFilter destinationMeshFilter, MeshCollider destinationMeshCollider, SegmentReadyDelegate onSegmentReady, bool provideNormals, bool provideColors, bool providePhysics);
}


}
