// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // How ContainerWindows are visualized. Used with ContainerWindow.Show
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
        Tooltip = 6
    }

    //[StaticAccessor("ContainerWindowBindings", StaticAccessorType.DoubleColon)]
    [NativeHeader("Editor/Src/ContainerWindow.bindings.h")]
    [MarshalUnityObjectAs(typeof(MonoBehaviour))]
    internal partial class ContainerWindow
    {
        private const string k_ScriptingPrefix = "ContainerWindowBindings::";

        // Pixel-position on screen.
        public extern Rect position
        {
            [FreeFunction(k_ScriptingPrefix + "GetPosition", HasExplicitThis = true)] get;
            [FreeFunction(k_ScriptingPrefix + "SetPosition", HasExplicitThis = true)] set;
        }

        public extern bool maximized {[FreeFunction(k_ScriptingPrefix + "IsWindowMaximized", HasExplicitThis = true)] get; }

        [FreeFunction(k_ScriptingPrefix + "SetAlpha", HasExplicitThis = true)]
        public extern void SetAlpha(float alpha);

        // Used by invisible "under the mouse" window of the eye dropper.
        [FreeFunction(k_ScriptingPrefix + "SetInvisible", HasExplicitThis = true)]
        public extern void SetInvisible();

        [FreeFunction(k_ScriptingPrefix + "IsZoomed", HasExplicitThis = true)]
        public extern bool IsZoomed();

        [FreeFunction(k_ScriptingPrefix + "DisplayAllViews", HasExplicitThis = true)]
        public extern void DisplayAllViews();

        [FreeFunction(k_ScriptingPrefix + "Minimize", HasExplicitThis = true)]
        public extern void Minimize();

        [FreeFunction(k_ScriptingPrefix + "ToggleMaximize", HasExplicitThis = true)]
        public extern void ToggleMaximize();

        [FreeFunction(k_ScriptingPrefix + "MoveInFrontOf", HasExplicitThis = true)]
        public extern void MoveInFrontOf(ContainerWindow other);

        [FreeFunction(k_ScriptingPrefix + "MoveBehindOf", HasExplicitThis = true)]
        public extern void MoveBehindOf(ContainerWindow other);

        // Fit a container window to the screen.
        [FreeFunction(k_ScriptingPrefix + "FitWindowRectToScreen", HasExplicitThis = true)]
        public extern Rect FitWindowRectToScreen(Rect r, bool forceCompletelyVisible, bool useMouseScreen);

        // Close the editor window.
        [FreeFunction(k_ScriptingPrefix + "InternalClose", HasExplicitThis = true)]
        public extern void InternalClose();

        [FreeFunction(k_ScriptingPrefix + "Internal_SetMinMaxSizes", HasExplicitThis = true)]
        private extern void Internal_SetMinMaxSizes(Vector2 minSize, Vector2 maxSize);

        [FreeFunction(k_ScriptingPrefix + "Internal_Show", HasExplicitThis = true)]
        private extern void Internal_Show(Rect r, int showMode, Vector2 minSize, Vector2 maxSize);

        [FreeFunction(k_ScriptingPrefix + "Internal_BringLiveAfterCreation", HasExplicitThis = true)]
        private extern void Internal_BringLiveAfterCreation(bool displayImmediately, bool setFocus);

        [FreeFunction(k_ScriptingPrefix + "Internal_SetTitle", HasExplicitThis = true)]
        private extern void Internal_SetTitle(string title);

        [FreeFunction(k_ScriptingPrefix + "SetBackgroundColor", HasExplicitThis = true)]
        private extern void SetBackgroundColor(Color color);

        [FreeFunction(k_ScriptingPrefix + "Internal_GetTopleftScreenPosition", HasExplicitThis = true)]
        private extern Vector2 Internal_GetTopleftScreenPosition();

        // Disables any repaints until freeze is set to false again.
        [FreeFunction(k_ScriptingPrefix + "SetFreezeDisplay")]
        public static extern void SetFreezeDisplay(bool freeze);

        [FreeFunction(k_ScriptingPrefix + "GetOrderedWindowList")]
        internal static extern void GetOrderedWindowList();

        [FreeFunction(k_ScriptingPrefix + "FitRectToScreen")]
        internal static extern Rect FitRectToScreen(Rect defaultRect, bool forceCompletelyVisible, bool useMouseScreen);
    }
}
