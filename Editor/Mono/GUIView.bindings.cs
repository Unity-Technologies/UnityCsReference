// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [UsedByNativeCode,
     NativeHeader("Runtime/Misc/InputEvent.h"),
     NativeHeader("Runtime/Graphics/RenderTexture.h"),
     NativeHeader("Editor/Src/GUIView.bindings.h"),
     NativeHeader("Editor/Src/ContainerWindow.bindings.h")]
    internal partial class GUIView
    {
        public static extern GUIView current {[NativeMethod("GetCurrentGUIView")] get; }
        public static extern GUIView focusedView {[NativeMethod("GetFocusedGUIView")] get; }
        public static extern GUIView mouseOverView {[NativeMethod("GetMouseOverGUIView")] get; }

        public extern bool hasFocus {[NativeMethod("IsViewFocused")] get; }

        public extern void Repaint();
        public extern void Focus();
        public extern void RepaintImmediately();
        public extern void CaptureRenderDocScene();
        public extern void CaptureRenderDocFullContent();

        internal extern bool mouseRayInvisible {[NativeMethod("IsMouseRayInvisible")] get; [NativeMethod("SetMouseRayInvisible")] set; }
        internal extern bool disableInputEvents {[NativeMethod("AreInputEventsDisabled")] get; [NativeMethod("SetDisableInputEvents")] set; }


        internal extern void SetTitle(string title);
        internal extern void AddToAuxWindowList();
        internal extern void RemoveFromAuxWindowList();
        internal extern void SetInternalGameViewDimensions(Rect rect, Rect clippedRect, Vector2 targetSize);
        internal extern void SetAsStartView();
        internal extern void ClearStartView();
        internal extern void MakeVistaDWMHappyDance();
        internal extern void StealMouseCapture();
        internal extern void ClearKeyboardControl();
        internal extern void SetKeyboardControl(int id);
        internal extern int GetKeyboardControl();
        internal extern void GrabPixels(RenderTexture rd, Rect rect);
        internal extern void MarkHotRegion(Rect hotRegionRect);

        protected extern void Internal_SetAsActiveWindow();

        private extern void Internal_Init(int depthBits, int antiAliasing);
        private extern void Internal_Recreate(int depthBits, int antiAliasing);
        private extern void Internal_Close();
        private extern bool Internal_SendEvent(Event e);
        private extern void Internal_SetWantsMouseMove(bool wantIt);
        private extern void Internal_SetWantsMouseEnterLeaveWindow(bool wantIt);
        private extern void Internal_SetAutoRepaint(bool doit);
        private extern void Internal_SetWindow(ScriptableObject win);
        private extern void Internal_SetPosition(Rect windowPosition);
    }
}
