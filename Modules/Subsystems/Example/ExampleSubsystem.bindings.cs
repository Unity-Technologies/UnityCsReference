// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Subsystems
{
    [NativeType(Header = "Modules/Subsystems/Example/ExampleSubsystem.h")]
    [UsedByNativeCode]
    public class ExampleSubsystem : IntegratedSubsystem<ExampleSubsystemDescriptor>
    {
        public extern void PrintExample();
        public extern bool GetBool();

        new internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(ExampleSubsystem exampleSubsystem) => exampleSubsystem.m_Ptr;
        }
    }
}
