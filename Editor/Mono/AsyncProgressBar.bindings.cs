// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEditor
{
    // Progress bar located in the status bar. A non blocking way of showing progress of tasks, e.g. lightmapping.
    // Currently can properly show progress only for one task at a time.
    [StaticAccessor("GetAsyncProgressBar()", StaticAccessorType.Dot)]
    [NativeHeader("Editor/Src/AsyncProgressBar.h")]
    internal partial class AsyncProgressBar
    {
        public static extern float progress { get; }
        public static extern string progressInfo { get; }
        public static extern bool isShowing {[NativeName("IsShowing")] get; }

        public static extern void Display(string progressInfo, float progress);
        public static extern void Clear();
    }
}
