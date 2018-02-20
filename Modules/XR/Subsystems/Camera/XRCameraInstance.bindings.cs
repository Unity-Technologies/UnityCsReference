// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.XR
{
    [NativeHeader("Modules/XR/XRPrefix.h")]
    [NativeType(Header = "Modules/XR/Subsystems/Camera/XRCameraInstance.h")]
    [UsedByNativeCode]
    public class XRCameraInstance : XRInstance<XRCameraSubsystemDescriptor>
    {
        [NativeConditional("ENABLE_XR")]
        public extern void SetMaterial(Material mat);

        [NativeConditional("ENABLE_XR")]
        public extern float GetAverageBrightness();

        [NativeConditional("ENABLE_XR")]
        public extern float GetAverageColorTemperature();
    }
}
