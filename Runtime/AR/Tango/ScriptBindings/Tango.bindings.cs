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
    [StructLayout(LayoutKind.Explicit, Size = 92)]
    internal struct PoseData
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


    [NativeHeader("Runtime/AR/Tango/TangoScriptApi.h")]
    [NativeConditional("PLATFORM_ANDROID")]
    internal static partial class TangoInputTracking
    {
        extern private static bool Internal_TryGetPoseAtTime(double time, ScreenOrientation screenOrientation,
            CoordinateFrame baseFrame, CoordinateFrame targetFrame, out PoseData pose);

        internal static bool TryGetPoseAtTime(out PoseData pose, CoordinateFrame baseFrame, CoordinateFrame targetFrame,
            double time, ScreenOrientation screenOrientation)
        {
            return Internal_TryGetPoseAtTime(time, screenOrientation, baseFrame, targetFrame, out pose);
        }

        internal static bool TryGetPoseAtTime(out PoseData pose, CoordinateFrame baseFrame, CoordinateFrame targetFrame,
            double time = 0.0)
        {
            return Internal_TryGetPoseAtTime(time, Screen.orientation, baseFrame, targetFrame, out pose);
        }
    }
}
