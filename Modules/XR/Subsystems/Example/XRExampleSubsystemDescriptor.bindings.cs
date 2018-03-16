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
    [NativeType(Header = "Modules/XR/Subsystems/Example/XRExampleSubsystemDescriptor.h")]
    [UsedByNativeCode]
    public class XRExampleSubsystemDescriptor : SubsystemDescriptor<XRExampleSubsystem>
    {
        public extern bool supportsEditorMode { get; }
        public extern bool disableBackbufferMSAA { get; }
        public extern bool stereoscopicBackbuffer { get; }
        public extern bool usePBufferEGL { get; }
    }
}
