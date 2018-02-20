// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    // Utility functions for implementing and extending the GUILayout class.
    [NativeHeader("Modules/IMGUI/GUILayoutUtility.bindings.h")]
    partial class GUILayoutUtility
    {
        private static extern Rect Internal_GetWindowRect(int windowID);
        private static extern void Internal_MoveWindow(int windowID, Rect r);
        internal static extern Rect GetWindowsBounds();
    }
}
