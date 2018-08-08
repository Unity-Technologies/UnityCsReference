// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEditor.VersionControl
{
    [NativeType("Editor/Src/VersionControl/VCCustomCommand.h")]
    internal enum CommandContext
    {
        Global = 1
    }

    [UsedByNativeCode]
    [NativeHeader("Editor/Src/VersionControl/VCCustomCommand.h")]
    [NativeHeader("Editor/Src/VersionControl/VC_bindings.h")]
    [NativeHeader("Editor/Src/VersionControl/VCTask.h")]
    [NativeHeader("Editor/Src/VersionControl/VCProvider.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal class CustomCommand
    {
        // The bindings generator will set the instance pointer in this field
        internal IntPtr m_Self;

        internal CustomCommand() {}

        [FreeFunction("VersionControlBindings::VCCustomCommand::StartTask")]
        static extern Task StartCustomCommand(string name);

        [StaticAccessor("GetVCProvider()", StaticAccessorType.Dot)]
        [NativeMethod("AddCustomCommand")]
        internal static extern bool Create(string name, string label, CommandContext context);

        public Task StartTask()
        {
            return StartCustomCommand(name);
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern string name { get; }

        [NativeMethod(IsThreadSafe = true)]
        public extern string label { get; }

        [NativeMethod(IsThreadSafe = true)]
        public extern CommandContext context { get; }
    }
}
