// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Modules/IMGUI/GUI.bindings.h"),
     NativeHeader("Modules/IMGUI/GUISkin.bindings.h")]
    partial class GUI
    {
        public static extern Color color { get; set; }
        public static extern Color backgroundColor { get; set; }
        public static extern Color contentColor { get; set; }
        public static extern bool changed { get; set; }
        public static extern bool enabled { get; set; }

        public static extern int depth { get; set; }

        internal static extern bool usePageScrollbars { get; }
        internal static extern Material blendMaterial {[FreeFunction("GetGUIBlendMaterial")] get; }
        internal static extern Material blitMaterial {[FreeFunction("GetGUIBlitMaterial")] get; }
        internal static extern Material roundedRectMaterial {[FreeFunction("GetGUIRoundedRectMaterial")] get; }

        private static extern void GrabMouseControl(int id);
        private static extern bool HasMouseControl(int id);
        private static extern void ReleaseMouseControl();

        [FreeFunction("GetGUIState().SetNameOfNextControl")]
        public static extern void SetNextControlName(string name);

        [FreeFunction("GetGUIState().GetNameOfFocusedControl")]
        public static extern string GetNameOfFocusedControl();

        [FreeFunction("GetGUIState().FocusKeyboardControl")]
        public static extern void FocusControl(string name);

        internal static extern void InternalRepaintEditorWindow();
        private static extern string Internal_GetTooltip();
        private static extern void Internal_SetTooltip(string value);
        private static extern string Internal_GetMouseTooltip();
        private static extern Rect Internal_DoModalWindow(int id, int instanceID, Rect clientRect, WindowFunction func, GUIContent content, GUIStyle style, System.Object skin);
        private static extern Rect Internal_DoWindow(int id, int instanceID, Rect clientRect, WindowFunction func, GUIContent title, GUIStyle style, System.Object skin, bool forceRectOnLayout);

        public static extern void DragWindow(Rect position);
        public static extern void BringWindowToFront(int windowID);
        public static extern void BringWindowToBack(int windowID);
        public static extern void FocusWindow(int windowID);
        public static extern void UnfocusWindow();
        private static extern void Internal_BeginWindows();
        private static extern void Internal_EndWindows();

        internal static extern string Internal_Concatenate(GUIContent first, GUIContent second);
    }
}
