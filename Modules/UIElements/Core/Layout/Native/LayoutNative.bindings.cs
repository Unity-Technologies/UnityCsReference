// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using System;
using JetBrains.Annotations;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements.Layout;

[NativeHeader("External/Yoga/LayoutNative.h")]
static partial class LayoutNative
{
    [NativeMethod(IsThreadSafe = false)]
    internal static extern void CalculateLayout(
        IntPtr node,
        float parentWidth,
        float parentHeight,
        int parentDirection,
        IntPtr state,
        IntPtr exceptionGCHandle);

    // CSS Grid feature flag. Pushed from UIToolkitProjectSettings at editor boot;
    // layout tests opt in. When disabled, display:grid falls back to the flex algorithm.
    [VisibleToOtherModules("UnityEditor.UIElementsModule")]
    [NativeMethod(IsThreadSafe = false)]
    internal static extern void SetGridLayoutEnabled(bool enabled);

    [NativeMethod(IsThreadSafe = false)]
    internal static extern void MeasureNode(
        IntPtr node,
        float availableWidth,
        int widthMode,
        float availableHeight,
        int heightMode,
        int parentDirection,
        IntPtr state,
        IntPtr exceptionGCHandle,
        out float outWidth,
        out float outHeight);

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


    [AutoStaticsCleanupOnCodeReload]
    internal static event Action<LayoutLogData> onLayoutLog;

    [RequiredByNativeCode(Optional = true)]
    [RequiredMember]
    [UsedImplicitly]
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
