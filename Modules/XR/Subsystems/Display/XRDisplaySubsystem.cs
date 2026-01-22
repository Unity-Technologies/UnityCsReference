// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.SubsystemsImplementation;
using System.Collections.Generic;
using System.ComponentModel;

namespace UnityEngine.XR
{
    // This partial class contains the pure C# logic and helper APIs for the XRDisplaySubsystem.
    // The native bindings (extern methods) are defined in the corresponding XRDisplaySubsystem.bindings.cs file.
    // New pure C# helper methods should be added to this file.

    public partial class XRDisplaySubsystem
    {
        private static readonly List<XRDisplaySubsystem> s_DisplaySubsystems = new List<XRDisplaySubsystem>();
        private static readonly XRDisplaySubsystemDefault s_Default = XRDisplaySubsystemDefault.instance;

        public static XRDisplaySubsystem activeSubsystem
        {
            get
            {
                SubsystemManager.GetSubsystems(s_DisplaySubsystems);
                return s_DisplaySubsystems.Count > 0 ? s_DisplaySubsystems[0] : null;
            }
        }

        public static XRDisplaySubsystem activeSubsystemOrStub
        {
            get
            {
                var subsystem = activeSubsystem;
                return subsystem ?? s_Default;
            }
        }
    }
}
