// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Tizen
{
    [NativeHeader("PlatformDependent/TizenPlayer/TizenBindings.h")]
    [NativeConditional("UNITY_TIZEN_API")]
    public sealed partial class Window
    {
        // Get the applications main window
        public static IntPtr windowHandle
        {
            get { return (IntPtr)null; }
        }

        // Get the applications EvasGL object
        public static IntPtr evasGL
        {
            get { return IntPtr.Zero; }
        }
    }
}
