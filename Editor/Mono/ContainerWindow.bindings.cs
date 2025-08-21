// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    // How ContainerWindows are visualized. Used with ContainerWindow.Show
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
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

        extern Rect Internal_Position
        {
            [FreeFunction(k_ScriptingPrefix + "GetPosition", HasExplicitThis = true)] get;
            [FreeFunction(k_ScriptingPrefix + "SetPosition", HasExplicitThis = true)] set;
        }

        public extern bool maximized {[FreeFunction(k_ScriptingPrefix + "IsWindowMaximized", HasExplicitThis = true)] get; }

        public void SetAlpha(float alpha)
        {
            // UUM-113705
            // This functionality has been removed but ProBuilder package is referencing the internal API.
            // Therefore this shell API is being added back in until ProBuilder can be updated to remove the reference.
        }

        // Used by invisible "under the mouse" window of the eye dropper.
        [FreeFunction(k_ScriptingPrefix + "SetInvisible", HasExplicitThis = true)]
        public extern void SetInvisible();

        [FreeFunction(k_ScriptingPrefix + "IsZoomed", HasExplicitThis = true)]
        public extern bool IsZoomed();

        [FreeFunction(k_ScriptingPrefix + "ToggleMaximize", HasExplicitThis = true)]
        public extern void ToggleMaximize();

        [FreeFunction(k_ScriptingPrefix + "Internal_Destroy", HasExplicitThis = true)]
        public extern void Internal_Destroy();

        [FreeFunction(k_ScriptingPrefix + "Internal_SetMinMaxSizes", HasExplicitThis = true)]
        private extern void Internal_SetMinMaxSizes(Vector2 minSize, Vector2 maxSize);

        [FreeFunction(k_ScriptingPrefix + "Internal_Show", HasExplicitThis = true, ThrowsException = true)]
        private extern void Internal_Show(Rect r, int showMode, Vector2 minSize, Vector2 maxSize);

        [FreeFunction(k_ScriptingPrefix + "Internal_BringLiveAfterCreation", HasExplicitThis = true)]
        private extern void Internal_BringLiveAfterCreation(bool displayImmediately, bool setFocus, bool showMaximized);

        [FreeFunction(k_ScriptingPrefix + "Internal_SetTitle", HasExplicitThis = true)]
        private extern void Internal_SetTitle(string title);

        [FreeFunction(k_ScriptingPrefix + "Internal_SetHasUnsavedChanges", HasExplicitThis = true)]
        private extern void Internal_SetHasUnsavedChanges(bool hasUnsavedChanges);

        [FreeFunction(k_ScriptingPrefix + "SetBackgroundColor", HasExplicitThis = true)]
        private extern void SetBackgroundColor(Color color);

        [FreeFunction(k_ScriptingPrefix + "Internal_GetTopleftScreenPosition", HasExplicitThis = true)]
        private extern Vector2 Internal_GetTopleftScreenPosition();

        [FreeFunction(k_ScriptingPrefix + "GetOrderedWindowList")]
        internal static extern void GetOrderedWindowList();

        [FreeFunction(k_ScriptingPrefix + "FitRectToMouseScreen")]
        internal static extern Rect FitRectToMouseScreen(Rect rect, bool forceCompletelyVisible, ContainerWindow windowForBorderCalculation);

        [FreeFunction(k_ScriptingPrefix + "FitRectToScreen")]
        internal static extern Rect FitRectToScreen(Rect rect, Vector2 uiPositionToFindScreen, bool forceCompletelyVisible, ContainerWindow windowForBorderCalculation);

    }
}
