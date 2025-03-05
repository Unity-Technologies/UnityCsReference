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
    [NativeHeader("Modules/UIElements/Core/Native/UIElementsRuntimeUtilityNative.h")]
    [VisibleToOtherModules("Unity.UIElements")]
    internal static class UIElementsRuntimeUtilityNative
    {
        private static Action UpdatePanelsCallback;
        private static Action<bool> RepaintPanelsCallback;
        private static Action RenderOffscreenPanelsCallback;

        [RequiredByNativeCode]
        public static void UpdatePanels()
        {
            UpdatePanelsCallback?.Invoke();
        }

        [RequiredByNativeCode]
        public static void RepaintPanels(bool onlyOffscreen)
        {
            RepaintPanelsCallback?.Invoke(onlyOffscreen);
        }

        [RequiredByNativeCode]
        public static void RenderOffscreenPanels()
        {
            RenderOffscreenPanelsCallback?.Invoke();
        }

        public static void SetUpdateCallback(Action callback)
        {
            UpdatePanelsCallback = callback;
        }

        public static void SetRenderingCallbacks(Action<bool> repaintPanels, Action renderOffscreenPanels)
        {
            RepaintPanelsCallback = repaintPanels;
            RenderOffscreenPanelsCallback = renderOffscreenPanels;
            RegisterRenderingCallbacks();
        }

        public static void UnsetRenderingCallbacks()
        {
            RepaintPanelsCallback = null;
            RenderOffscreenPanelsCallback = null;
            UnregisterRenderingCallbacks();
        }

        private extern static void RegisterRenderingCallbacks();
        private extern static void UnregisterRenderingCallbacks();

        public extern static void VisualElementCreation();
    }
}
