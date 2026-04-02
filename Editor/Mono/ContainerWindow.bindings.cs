// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor
{
    // How ContainerWindows are visualized. Used with ContainerWindow.Show
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal enum ShowMode
    {
        // Show as a normal window with max, min & close buttons.
        NormalWindow = 0,
        // Used for a popup menu. On mac this means light shadow and no titlebar.
        PopupMenu = 1,
        // Utility window - floats above the app. Disappears when app loses focus.
        Utility = 2,
        // Window has no shadow or decorations. Used internally for dragging stuff around.
        NoShadow = 3,
        // The Unity main window. On mac, this is the same as NormalWindow, except window doesn't have a close button.
        MainWindow = 4,
        // Aux windows. The ones that close the moment you move the mouse out of them.
        AuxWindow = 5,
        // Like PopupMenu, but without keyboard focus
        Tooltip = 6,
        // Modal Utility window
        ModalUtility = 7,
    }

    //[StaticAccessor("ContainerWindowBindings", StaticAccessorType.DoubleColon)]
    [NativeHeader("Editor/Src/Windowing/ContainerWindow.bindings.h")]
    internal partial class ContainerWindow
    {
        private const string k_ScriptingPrefix = "ContainerWindowBindings::";

        // Provide access to native handle for ref IntPtr parameter in Internal_Show
        private ref System.IntPtr nativeHandle => ref m_WindowPtr.m_IntPtr;

        // m_PixelRect is managed; only call native SetRect when window is valid
        Rect Internal_Position
        {
            get => m_PixelRect;
            set
            {
                if (m_WindowPtr.m_IntPtr == System.IntPtr.Zero)
                {
                    // When the window is not valid, we must set m_PixelRect directly.
                    m_PixelRect = value;
                    return;
                }
                // When the window is valid, we must NOT assign m_PixelRect because it is driven by OnRectChanged. rect is
                // user driven and may not be pixel aligned. Consider the following sequence:
                // 1. User calls SetPosition with rect R (not pixel aligned)
                // 2. SetRect computes a pixel aligned rect and requests it
                // 3. (window manager applies the new rect and eventually the message is processed)
                // 4. OnRectChanged is called and m_PixelRect is assigned a valid pixel aligned value
                // 5. m_PixelRect is assigned with a pixel-aligned value
                // 6. User calls SetPosition again with rect R (still not pixel aligned)
                // 7. SetRect computes a pixel aligned rect BUT DOES NOTHING BECAUSE THERE IS NO CHANGE
                // 8. OnRectChanged is NOT called
                // Had we assigned rect to m_PixelRect like above, it would now contain an invalid value.
                SetRect_Native(value);
            }
        }

        [FreeFunction(k_ScriptingPrefix + "SetRect", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void SetRect_Native(Rect rect);

        public extern bool maximized
        {
            [FreeFunction(k_ScriptingPrefix + "IsWindowMaximized", HasExplicitThis = true)]
            [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            get;
        }

        // Used by invisible "under the mouse" window of the eye dropper.
        [FreeFunction(k_ScriptingPrefix + "SetInvisible", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        public extern void SetInvisible();

        [FreeFunction(k_ScriptingPrefix + "IsZoomed", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        public extern bool IsZoomed();

        [FreeFunction(k_ScriptingPrefix + "ToggleMaximize", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        public extern void ToggleMaximize();

        [FreeFunction(k_ScriptingPrefix + "GetBackingScale", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        public extern float GetBackingScale();

        [FreeFunction(k_ScriptingPrefix + "Internal_Destroy", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        public extern void Internal_Destroy();

        [FreeFunction(k_ScriptingPrefix + "Internal_SetMinMaxSizes", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void Internal_SetMinMaxSizes(Vector2 minSize, Vector2 maxSize);

        // Internal_Show needs 'this' (as MonoBehaviour*) for InitializeContainer, passed via HasExplicitThis
        // pWindow is ref IntPtr to allow native code to set the window pointer
        [FreeFunction(k_ScriptingPrefix + "Internal_Show", HasExplicitThis = true, ThrowsException = true)]
        private extern void Internal_Show(ref System.IntPtr pWindow, Rect r, int showMode, Vector2 minSize, Vector2 maxSize);

        // Wrapper to pass nativeHandle by ref
        private void Internal_Show(Rect r, int showMode, Vector2 minSize, Vector2 maxSize)
            => Internal_Show(ref nativeHandle, r, showMode, minSize, maxSize);

        [FreeFunction(k_ScriptingPrefix + "Internal_BringLiveAfterCreation", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void Internal_BringLiveAfterCreation(bool displayImmediately, bool setFocus, bool showMaximized);

        [FreeFunction(k_ScriptingPrefix + "Internal_SetTitle", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void Internal_SetTitle(string title);

        [FreeFunction(k_ScriptingPrefix + "Internal_SetHasUnsavedChanges", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void Internal_SetHasUnsavedChanges(bool hasUnsavedChanges);

        [FreeFunction(k_ScriptingPrefix + "SetBackgroundColor", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern void SetBackgroundColor(Color color);

        [FreeFunction(k_ScriptingPrefix + "Internal_GetTopleftScreenPosition", HasExplicitThis = true)]
        [UnityMarshalThisAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
        private extern Vector2 Internal_GetTopleftScreenPosition();

        [FreeFunction(k_ScriptingPrefix + "GetOrderedWindowList")]
        internal static extern void GetOrderedWindowList();

        [FreeFunction(k_ScriptingPrefix + "FitRectToMouseScreen")]
        internal static extern Rect FitRectToMouseScreen(
            Rect rect,
            bool forceCompletelyVisible,
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ContainerWindow windowForBorderCalculation);

        [FreeFunction(k_ScriptingPrefix + "FitRectToScreen")]
        internal static extern Rect FitRectToScreen(
            Rect rect,
            Vector2 uiPositionToFindScreen,
            bool forceCompletelyVisible,
            [UnityMarshalAs(NativeType.Custom, CustomMarshaller = typeof(NativeHandleMarshaller))]
            ContainerWindow windowForBorderCalculation);

        // Called from native code to clear the native window pointer during close/destruction.
        [RequiredByNativeCode]
        internal void ClearNativeWindowPtr()
        {
            m_WindowPtr.m_IntPtr = System.IntPtr.Zero;
        }

        // Called from native code to update the pixel rect when the window rect changes.
        // Takes individual floats to avoid struct marshalling complexity in auto-generated proxies.
        [RequiredByNativeCode]
        internal void SetPixelRect(float x, float y, float width, float height)
        {
            m_PixelRect = new Rect(x, y, width, height);
        }
    }
}
