// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/ExternalPlayModeView.bindings.h"),
     StaticAccessor("ExternalPlayModeViewBindings", StaticAccessorType.DoubleColon)]
    internal partial class ExternalPlayModeView
    {
        private static extern IntPtr Internal_InitWindow();

        private static extern void Internal_SetPlayerLaunchPath(IntPtr nativeContextPtr, string pathToExe);

        private static extern void AttachWindow_Native(IntPtr nativeContextPtr, IntPtr hostWindowHandle, Rect position);

        private static extern void ResizeWindow_Native(IntPtr nativeContextPtr, System.IntPtr hostWindowHandle, Rect newPosition);

        private static extern void BeforeRemoveTab_Native(IntPtr nativeContextPtr, System.IntPtr hostWindowHandle);

        private static extern void AddedAsTab_Native(IntPtr nativeContextPtr, System.IntPtr hostWindowHandle, Rect position);

        private static extern void OnBecameVisible_Native(IntPtr nativeContextPtr, System.IntPtr hostWindowHandle, Rect position);

        private static extern void OnBecameInvisible_Native(IntPtr nativeContextPtr, System.IntPtr hostWindowHandle);

        private static extern void DestroyWindow_Native(IntPtr nativeContextPtr, System.IntPtr hostWindowHandle);
    }
}
