// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    // This is the required interface to UIElementsUtility for Runtime game components.
    [NativeHeader("Modules/UIElementsNative/UIElementsRuntimeUtilityNative.h")]
    [VisibleToOtherModules("Unity.UIElements")]
    internal static class UIElementsRuntimeUtilityNative
    {
        internal static Action RepaintOverlayPanelsCallback;

        [RequiredByNativeCode]
        public static void RepaintOverlayPanels()
        {
            RepaintOverlayPanelsCallback?.Invoke();
        }

        public extern static void RegisterPlayerloopCallback();
        public extern static void UnregisterPlayerloopCallback();
    }
}
