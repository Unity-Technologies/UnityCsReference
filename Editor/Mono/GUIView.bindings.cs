// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    [UsedByNativeCode,
     NativeHeader("Runtime/Misc/InputEvent.h"),
     NativeHeader("Runtime/Graphics/RenderTexture.h"),
     NativeHeader("Editor/Src/Windowing/GUIView.bindings.h"),
     NativeHeader("Editor/Src/Windowing/ContainerWindow.bindings.h")]
    [UnityEngine.Bindings.VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.GraphToolkitModule", "UnityEditor.BurstModule")]
    internal partial class GUIView
    {
        [VisibleToOtherModules("UnityEditor.GraphToolkitModule")]
        public static extern GUIView current {[NativeMethod("GetCurrentGUIView")] get; }
        public static extern GUIView focusedView {[NativeMethod("GetFocusedGUIView")] get; }
        public static extern GUIView mouseOverView {[NativeMethod("GetMouseOverGUIView")] get; }

        public extern bool hasFocus
        {
            [NativeMethod("MonoGUIView::IsViewFocused", HasExplicitThis = true)]
            [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            get;
        }

        [NativeMethod("MonoGUIView::Repaint", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        public extern void Repaint();

        [UnityEngine.Bindings.VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        [NativeMethod("MonoGUIView::Focus", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        public extern void Focus();

        [NativeMethod("MonoGUIView::RepaintImmediately", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        public extern void RepaintImmediately();

        [NativeMethod("MonoGUIView::CaptureRenderDocScene", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        public extern void CaptureRenderDocScene();

        [NativeMethod("MonoGUIView::CaptureRenderDocFullContent", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        public extern void CaptureRenderDocFullContent();

        [NativeMethod("MonoGUIView::BeginCaptureRenderDoc", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        public extern void BeginCaptureRenderDoc();

        [NativeMethod("MonoGUIView::EndCaptureRenderDoc", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        public extern void EndCaptureRenderDoc();

        internal extern bool vSyncEnabled
        {
            [NativeMethod("MonoGUIView::IsVSyncEnabled", HasExplicitThis = true)]
            [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            get;
        }

        [NativeMethod("MonoGUIView::RenderCurrentSceneForCapture", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void RenderCurrentSceneForCapture();


        internal extern bool mouseRayInvisible
        {
            [NativeMethod("MonoGUIView::IsMouseRayInvisible", HasExplicitThis = true)]
            [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            get;
            [NativeMethod("MonoGUIView::SetMouseRayInvisible", HasExplicitThis = true)]
            [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            set;
        }
        internal extern bool disableInputEvents
        {
            [NativeMethod("MonoGUIView::AreInputEventsDisabled", HasExplicitThis = true)]
            [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            get;
            [NativeMethod("MonoGUIView::SetDisableInputEvents", HasExplicitThis = true)]
            [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            set;
        }

        internal extern bool hdrActive
        {
            [NativeMethod("MonoGUIView::IsHDRActive", HasExplicitThis = true)]
            [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            get;
        }

        [NativeMethod("MonoGUIView::SetTitle", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void SetTitle(string title);

        [NativeMethod("MonoGUIView::AddToAuxWindowList", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void AddToAuxWindowList();

        [NativeMethod("MonoGUIView::SetInternalGameViewDimensions", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void SetInternalGameViewDimensions(Rect rect, Rect clippedRect, Vector2 targetSize);

        internal extern void SetMainPlayModeViewSize(Vector2 targetSize);
        internal extern void SetDisplayViewSize(int displayId, Vector2 targetSize);

        [NativeMethod("MonoGUIView::GetDisplayViewSize", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern Vector2 GetDisplayViewSize(int displayId);

        [NativeMethod("MonoGUIView::SetAsStartView", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void SetAsStartView();

        [NativeMethod("MonoGUIView::SetAsLastPlayModeView", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void SetAsLastPlayModeView();

        [NativeMethod("MonoGUIView::SetPlayModeView", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void SetPlayModeView(bool value);

        internal extern void ClearStartView();

        [NativeMethod("MonoGUIView::SetEyeDropperOpen", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void SetEyeDropperOpen(bool isOpen);

        [NativeMethod("MonoGUIView::StealMouseCapture", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void StealMouseCapture();

        [NativeMethod("MonoGUIView::ClearKeyboardControl", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void ClearKeyboardControl();

        [NativeMethod("MonoGUIView::SetKeyboardControl", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void SetKeyboardControl(int id);

        [NativeMethod("MonoGUIView::GetKeyboardControl", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern int GetKeyboardControl();

        [NativeMethod("MonoGUIView::GrabPixels", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void GrabPixels(RenderTexture rd, Rect rect);

        [NativeMethod("MonoGUIView::GetBackingScaleFactor", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern float GetBackingScaleFactor();

        [NativeMethod("MonoGUIView::MarkHotRegion", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void MarkHotRegion(Rect hotRegionRect);

        [NativeMethod("MonoGUIView::EnableVSync", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void EnableVSync(bool value);

        [NativeMethod("MonoGUIView::SetActualViewName", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        internal extern void SetActualViewName(string viewName);

        [NativeMethod("MonoGUIView::Internal_SetAsActiveWindow", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        protected extern void Internal_SetAsActiveWindow();

        [NativeMethod(ThrowsException = true)]
        private extern void Internal_Init(ref IntPtr pView, int depthBits, int antiAliasing, bool isPlayModeView, bool isVsync);

        [NativeMethod("MonoGUIView::Internal_Recreate", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void Internal_Recreate(int depthBits, int antiAliasing);

        [NativeMethod("MonoGUIView::Internal_Close", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void Internal_Close();

        [NativeMethod("MonoGUIView::Internal_SendEvent", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern bool Internal_SendEvent(Event e);

        [NativeMethod("MonoGUIView::Internal_SetWantsMouseMove", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void Internal_SetWantsMouseMove(bool wantIt);

        [NativeMethod("MonoGUIView::Internal_SetWantsMouseEnterLeaveWindow", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void Internal_SetWantsMouseEnterLeaveWindow(bool wantIt);

        [NativeMethod("MonoGUIView::Internal_SetAutoRepaint", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void Internal_SetAutoRepaint(bool doit);

        [NativeMethod("MonoGUIView::Internal_SetWindow", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void Internal_SetWindow(
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(ContainerWindow.NativeHandleMarshaller))]
            ContainerWindow win);

        [NativeMethod("MonoGUIView::Internal_UnsetWindow", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void Internal_UnsetWindow();

        [NativeMethod("MonoGUIView::Internal_SetPosition", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void Internal_SetPosition(Rect windowPosition);

        internal static class NativeHandleMarshaller
        {
            public static IntPtr ConvertToUnmanaged(GUIView view) => view != null ? view.nativeHandle : IntPtr.Zero;
        }

        // Called from native code to clear the native view pointer during destruction.
        [RequiredByNativeCode]
        internal void ClearNativeViewPtr()
        {
            nativeHandle = IntPtr.Zero;
        }
    }
}
