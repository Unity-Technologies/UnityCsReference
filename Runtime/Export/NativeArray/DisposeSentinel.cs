// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace UnityEngine.Collections
{
    public enum NativeLeakDetectionMode
    {
        Enabled = 0,
        Disabled = 1
    }

    public static class NativeLeakDetection
    {
        // For performance reasons no assignment operator (static initializer cost in il2cpp)
        // and flipped enabled / disabled enum value
        static int s_NativeLeakDetectionMode;

        public static NativeLeakDetectionMode Mode { get { return (NativeLeakDetectionMode)s_NativeLeakDetectionMode; } set { s_NativeLeakDetectionMode = (int)value; } }
    }

}
