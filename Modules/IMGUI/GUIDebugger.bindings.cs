// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System;

namespace UnityEngine
{
    [NativeHeader("Modules/IMGUI/GUIDebugger.bindings.h")]
    internal partial class GUIDebugger
    {
        //TODO: We could skip the trip to native if we check here if the current GUIVIew is being debugged.
        [NativeConditional("UNITY_EDITOR")]
        public static extern void LogLayoutEntry(Rect rect, RectOffset margins, GUIStyle style);

        [NativeConditional("UNITY_EDITOR")]
        public static extern void LogLayoutGroupEntry(Rect rect, RectOffset margins, GUIStyle style, bool isVertical);

        [NativeConditional("UNITY_EDITOR")]
        [StaticAccessor("GetGUIDebuggerManager()", StaticAccessorType.Dot)]
        [NativeMethod("LogEndGroup")]
        public static extern void LogLayoutEndGroup();

        [NativeConditional("UNITY_EDITOR")]
        [StaticAccessor("GetGUIDebuggerManager()", StaticAccessorType.Dot)]
        public static extern void LogBeginProperty(string targetTypeAssemblyQualifiedName, string path, Rect position);

        [NativeConditional("UNITY_EDITOR")]
        [StaticAccessor("GetGUIDebuggerManager()", StaticAccessorType.Dot)]
        public static extern void LogEndProperty();

        [NativeConditional("UNITY_EDITOR")]
        public static extern bool active {get; }
    }
}
