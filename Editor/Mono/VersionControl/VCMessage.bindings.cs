// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEditor.VersionControl
{
    [NativeHeader("Editor/Src/VersionControl/VCMessage.h")]
    [NativeHeader("Editor/Src/VersionControl/VC_bindings.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public partial class Message
    {
        [NativeType("Editor/Src/VersionControl/VCMessage.h")]
        public enum Severity
        {
            Data = 0,
            Verbose = 1,
            Info = 2,
            Warning = 3,
            Error = 4
        };

        // The bindings generator will set the instance pointer in this field
        IntPtr m_Self;

        internal Message() {}

        ~Message()
        {
            Dispose();
        }

        public void Dispose()
        {
            Destroy(m_Self);
            m_Self = IntPtr.Zero;
        }

        [FreeFunction("VersionControlBindings::Message::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr message);

        [NativeMethod(IsThreadSafe = true)]
        public extern Severity severity { get; }

        [NativeMethod(IsThreadSafe = true)]
        public extern string message { get; }
    }
}
