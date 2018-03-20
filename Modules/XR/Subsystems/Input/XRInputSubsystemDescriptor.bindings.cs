// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.XR
{
    [NativeType(Header = "Modules/XR/Subsystems/Input/XRInputSubsystemDescriptor.h")]
    [UsedByNativeCode]
    public class XRInputSubsystemDescriptor : SubsystemDescriptor<XRInputSubsystem>
    {}
}
