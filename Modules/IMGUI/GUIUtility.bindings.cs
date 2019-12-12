// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    // Utility class for making new GUI controls.
    [NativeHeader("Modules/IMGUI/GUIUtility.h"),
     NativeHeader("Modules/IMGUI/GUIManager.h"),
     NativeHeader("Runtime/Input/InputBindings.h"),
     NativeHeader("Runtime/Input/InputManager.h"),
     NativeHeader("Runtime/Camera/RenderLayers/GUITexture.h"),
     NativeHeader("Runtime/Utilities/CopyPaste.h")]
    public partial class GUIUtility
    {
        // Check to see if there's a modal IMGUI window that's currently open
        public static extern bool hasModalWindow { get; }

        [NativeProperty("GetGUIState().m_PixelsPerPoint", true, TargetType.Field)]
        internal static extern float pixelsPerPoint { get; }

        [NativeProperty("GetGUIState().m_OnGUIDepth", true, TargetType.Field)]
        internal static extern int guiDepth { get; }

        internal static extern Vector2 s_EditorScreenPointOffset
        {
            [NativeMethod("GetGUIState().GetGUIPixelOffset", true)]
            get;
            [NativeMethod("GetGUIState().SetGUIPixelOffset", true)]
            set;
        }

        [NativeProperty("GetGUIState().m_CanvasGUIState.m_IsMouseUsed", true, TargetType.Field)]
        internal static extern bool mouseUsed { get; set; }

        [StaticAccessor("GetInputManager()", StaticAccessorType.Dot)]
        internal static extern bool textFieldInput { get; set; }

        internal static extern bool manualTex2SRGBEnabled
        {
            [FreeFunction("GUITexture::IsManualTex2SRGBEnabled")] get;
            [FreeFunction("GUITexture::SetManualTex2SRGBEnabled")] set;
        }

        // Get access to the system-wide pasteboard.
        public static extern string systemCopyBuffer
        {
            [FreeFunction("GetCopyBuffer")] get;
            [FreeFunction("SetCopyBuffer")] set;
        }

        [StaticAccessor("GetGUIState()", StaticAccessorType.Dot)]
        public static extern int GetControlID(int hint, FocusType focusType, Rect rect);


        internal static extern void BeginContainerFromOwner(ScriptableObject owner);


        internal static extern void BeginContainer(ObjectGUIState objectGUIState);

        [NativeMethod("EndContainer")]
        internal static extern void Internal_EndContainer();

        [FreeFunction("GetSpecificGUIState(0).m_EternalGUIState->GetNextUniqueID")]
        internal static extern int GetPermanentControlID();

        [StaticAccessor("GetUndoManager()", StaticAccessorType.Dot)]
        internal static extern void UpdateUndoName();


        internal static extern int CheckForTabEvent(Event evt);


        internal static extern void SetKeyboardControlToFirstControlId();


        internal static extern void SetKeyboardControlToLastControlId();


        internal static extern bool HasFocusableControls();


        internal static extern bool OwnsId(int id);

        public static extern Rect AlignRectToDevice(Rect rect, out int widthInPixels, out int heightInPixels);

        // Need to reverse the dependency here when moving native legacy Input code out of Core module.
        [StaticAccessor("InputBindings", StaticAccessorType.DoubleColon)]
        internal extern static string compositionString
        {
            get;
        }

        // Need to reverse the dependency here when moving native legacy Input code out of Core module.
        [StaticAccessor("InputBindings", StaticAccessorType.DoubleColon)]
        internal extern static IMECompositionMode imeCompositionMode
        {
            get;
            set;
        }

        // Need to reverse the dependency here when moving native legacy Input code out of Core module.
        [StaticAccessor("InputBindings", StaticAccessorType.DoubleColon)]
        internal extern static Vector2 compositionCursorPos
        {
            get;
            set;
        }

        // This is used in sensitive alignment-related operations. Avoid calling this method if you can.

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
