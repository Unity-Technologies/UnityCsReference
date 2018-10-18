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
    internal enum PoseStatus
    {
        Initializing = 0,
        Valid,
        Invalid,
        Unknown
    }

    [UsedByNativeCode]
    [NativeHeader("ARScriptingClasses.h")]
    internal struct PoseData
    {
        public double orientation_x;
        public double orientation_y;
        public double orientation_z;
        public double orientation_w;
        public double translation_x;
        public double translation_y;
        public double translation_z;
        public PoseStatus statusCode;

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
        extern private static bool Internal_TryGetPoseAtTime(out PoseData pose);

        internal static bool TryGetPoseAtTime(out PoseData pose)
        {
            return Internal_TryGetPoseAtTime(out pose);
        }
    }
}
