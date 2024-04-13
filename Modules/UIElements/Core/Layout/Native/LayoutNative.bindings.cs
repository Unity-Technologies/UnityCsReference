// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements.Layout;

[NativeHeader("Modules/UIElements/Core/Layout/Native/LayoutNative.h")]
static class LayoutNative
{
    [NativeMethod(IsThreadSafe = false)]
    internal static extern void CalculateLayout(
        IntPtr node,
        float parentWidth,
        float parentHeight,
        int parentDirection,
        IntPtr state,
        IntPtr exceptionGCHandle);

    internal enum LayoutLogEventType
    {
        None = 0,
        Error = 1,
        Measure = 2,
        Layout = 3,
        CacheUsage = 4,
        BeginLayout = 5,
        EndLayout = 6,
    }

    internal class LayoutLogData
    {
        public LayoutNode node;
        public LayoutLogEventType eventType;
        public string message;
    }


    internal static event Action<LayoutLogData> onLayoutLog;

    [RequiredByNativeCode]
    private static void LayoutLog_Internal(IntPtr nodePtr, LayoutLogEventType type, string message)
    {
        LayoutLogData data = new LayoutLogData();
        unsafe
        {
            data.node = *(LayoutNode*)(nodePtr);
            data.message = message;
            data.eventType = type;
        }

        onLayoutLog(data);
    }
}
