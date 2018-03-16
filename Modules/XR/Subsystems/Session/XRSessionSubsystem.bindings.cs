// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine.Experimental;

namespace UnityEngine.Experimental.XR
{
    // Must match UnityXRTrackingState
    [UsedByNativeCode]
    public enum TrackingState
    {
        Unknown = 0,
        Tracking = 1,
        Unavailable = 2
    }

    public struct SessionTrackingStateChangedEventArgs
    {
        internal XRSessionSubsystem m_Session;
        public XRSessionSubsystem SessionSubsystem { get { return m_Session; } }
        public TrackingState NewState { get; set; }
    }

    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeHeader("Modules/XR/Subsystems/Session/XRSessionSubsystem.h")]
    [UsedByNativeCode]
    [NativeConditional("ENABLE_XR")]
    public class XRSessionSubsystem : Subsystem<XRSessionSubsystemDescriptor>
    {
        public event Action<SessionTrackingStateChangedEventArgs> TrackingStateChanged;

        [NativeConditional("ENABLE_XR", StubReturnStatement = "kUnityXRTrackingStateUnknown")]
        public extern TrackingState TrackingState { get; }

        public extern int LastUpdatedFrame { get; }

        [RequiredByNativeCode]
        private void InvokeTrackingStateChangedEvent(TrackingState newState)
        {
            if (TrackingStateChanged != null)
            {
                TrackingStateChanged(new SessionTrackingStateChangedEventArgs()
                {
                    m_Session = this,
                    NewState = newState
                });
            }
        }
    }
}
