// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace UnityEngine.Experimental.XR
{
    [NativeHeader("Modules/XR/Subsystems/Environment/Session/XRSession.h")]
    [UsedByNativeCode]
    public struct SessionConfiguration
    {
        public bool EnablePlaneDetection { get; set; }
        public bool EnableDepthData { get; set; }
        public bool EnableCamera { get; set; }
        public bool EnableLightEstimation { get; set; }
    }

    // Must match UnityXRTrackingState
    public enum TrackingState
    {
        Unknown = 0,
        Initializing = 1,
        Tracking = 2,
        Suspended = 3,
        Aborted = 4
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Environment/Session/XRSession.h")]
    [NativeHeader("XRScriptingClasses.h")]
    [StructLayout(LayoutKind.Sequential)]
    public class XRSession
    {
        private IntPtr m_Ptr;
        private XREnvironment m_Environment;

        public bool Valid
        {
            get
            {
                return m_Ptr != IntPtr.Zero;
            }
        }

        public XREnvironment Environment
        {
            get
            {
                return m_Environment;
            }
        }

        // NativeConditional can't handle enums at the moment
        public extern TrackingState TrackingState { get; }

        [NativeConditional("ENABLE_XR")]
        public extern int FrameOfLastTrackingStateUpdate { get; }

        [NativeConditional("ENABLE_XR")]
        public extern void SetConfig(SessionConfiguration config);

        internal XRSession(IntPtr nativeEnvironment, XREnvironment managedEnvironment)
        {
            m_Ptr = Internal_Create(nativeEnvironment);
            m_Environment = managedEnvironment;
            SetHandle(this);
        }

        [RequiredByNativeCode]
        private void NotifySessionDestruction()
        {
            m_Ptr = IntPtr.Zero;
        }

        [NativeConditional("ENABLE_XR")]
        private static extern IntPtr Internal_Create(IntPtr xrEnvironment);

        [NativeConditional("ENABLE_XR")]
        private extern void SetHandle(XRSession inst);
    }
}
