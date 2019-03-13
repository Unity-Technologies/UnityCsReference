// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Events;
using UnityEngine.Scripting;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.XR
{
    [NativeType(Header = "Modules/XR/Subsystems/Display/XRDisplaySubsystem.h")]
    [UsedByNativeCode]
    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeConditional("ENABLE_XR")]
    public class XRDisplaySubsystem : IntegratedSubsystem<XRDisplaySubsystemDescriptor>
    {
        public static event Action<bool> displayFocusChanged;

        [RequiredByNativeCode]
        private static void InvokeDisplayFocusChanged(bool focus)
        {
            if (displayFocusChanged != null)
                displayFocusChanged.Invoke(focus);
        }

        extern public bool singlePassRenderingDisabled { get; set; }
        extern public bool displayOpaque { get; }
        extern public bool contentProtectionEnabled { get; set; }


        public enum ReprojectionMode
        {
            Unspecified,
            PositionAndOrientation,
            OrientationOnly,
            None
        };

        extern public ReprojectionMode reprojectionMode { get; set; }

        extern public void SetFocusPlane(Vector3 point, Vector3 normal, Vector3 velocity);
    }
}
