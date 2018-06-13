// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    // Utility class for making new GUI controls.
    [NativeHeader("Modules/IMGUI/GUIUtility.h"),
     NativeHeader("Modules/IMGUI/GUIManager.h"),
     NativeHeader("Runtime/Input/InputManager.h"),
     NativeHeader("Runtime/Utilities/CopyPaste.h"),
     NativeHeader("Runtime/Camera/RenderLayers/GUITexture.h")]
    public partial class GUIUtility
    {
        // Check to see if there's a modal IMGUI window that's currently open
        public static extern bool hasModalWindow { get; }

        [NativeProperty("GetGUIState().m_PixelsPerPoint", true, TargetType.Field)]
        internal static extern float pixelsPerPoint {[VisibleToOtherModules("UnityEngine.UIElementsModule")] get; }

        [NativeProperty("GetGUIState().m_OnGUIDepth", true, TargetType.Field)]
        internal static extern int guiDepth {[VisibleToOtherModules("UnityEngine.UIElementsModule")] get; }

        internal static extern Vector2 s_EditorScreenPointOffset
        {
            [NativeMethod("GetGUIManager().GetGUIPixelOffset", true)]
            get;
            [NativeMethod("GetGUIManager().SetGUIPixelOffset", true)]
            set;
        }

        [NativeProperty("GetGUIState().m_CanvasGUIState.m_IsMouseUsed", true, TargetType.Field)]
        internal static extern bool mouseUsed { get; set; }

        [StaticAccessor("GetInputManager()", StaticAccessorType.Dot)]
        internal static extern bool textFieldInput { get; set; }

        internal static extern bool manualTex2SRGBEnabled
        {
            [FreeFunction("GUITexture::IsManualTex2SRGBEnabled")][VisibleToOtherModules("UnityEngine.UIElementsModule")] get;
            [FreeFunction("GUITexture::SetManualTex2SRGBEnabled")][VisibleToOtherModules("UnityEngine.UIElementsModule")] set;
        }

        // Get access to the system-wide pasteboard.
        public static extern string systemCopyBuffer
        {
            [FreeFunction("GetCopyBuffer")] get;
            [FreeFunction("SetCopyBuffer")] set;
        }

        [StaticAccessor("GetGUIState()", StaticAccessorType.Dot)]
        public static extern int GetControlID(int hint, FocusType focusType, Rect rect);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern void BeginContainerFromOwner(ScriptableObject owner);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern void BeginContainer(ObjectGUIState objectGUIState);

        [NativeMethod("EndContainer")]
        internal static extern void Internal_EndContainer();

        [FreeFunction("GetSpecificGUIState(0).m_EternalGUIState->GetNextUniqueID")]
        internal static extern int GetPermanentControlID();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        internal static extern void UpdateUndoName();

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern int CheckForTabEvent(Event evt);

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern void SetKeyboardControlToFirstControlId();

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern void SetKeyboardControlToLastControlId();

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern bool HasFocusableControls();

        public static extern Rect AlignRectToDevice(Rect rect, out int widthInPixels, out int heightInPixels);

        // This is used in sensitive alignment-related operations. Avoid calling this method if you can.
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern Vector3 Internal_MultiplyPoint(Vector3 point, Matrix4x4 transform);

        internal static extern bool GetChanged();
        internal static extern void SetChanged(bool changed);
        internal static extern void SetDidGUIWindowsEatLastEvent(bool value);

        private static extern int Internal_GetHotControl();
        private static extern int Internal_GetKeyboardControl();
        private static extern void Internal_SetHotControl(int value);
        private static extern void Internal_SetKeyboardControl(int value);
        private static extern System.Object Internal_GetDefaultSkin(int skinMode);
        private static extern Object Internal_GetBuiltinSkin(int skin);
        private static extern void Internal_ExitGUI();
        private static extern Vector2 InternalWindowToScreenPoint(Vector2 windowPoint);
        private static extern Vector2 InternalScreenToWindowPoint(Vector2 screenPoint);
    }
}
