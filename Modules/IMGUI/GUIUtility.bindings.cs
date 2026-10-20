// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>Utility class for making new GUI controls.</summary>
    ///<remarks>Unless you are creating your own GUI controls from scratch, you should not use these functions.</remarks>
    [NativeHeader("Modules/IMGUI/GUIUtility.h"),
     NativeHeader("Modules/IMGUI/GUIManager.h"),
     NativeHeader("Runtime/Input/InputBindings.h"),
     NativeHeader("Runtime/Input/InputManager.h"),
     NativeHeader("Runtime/Camera/RenderLayers/GUITexture.h"),
     NativeHeader("Runtime/Utilities/CopyPaste.h")]
    public partial class GUIUtility
    {
        ///<summary>A global property, which is true if a ModalWindow is being displayed, false otherwise.</summary>
        public static extern bool hasModalWindow { get; }

        [NativeProperty("GetGUIState().m_PixelsPerPoint", true, TargetType.Field)]
        internal static extern float pixelsPerPoint
        {
            [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEditor.UIToolkitAuthoringModule")]
            get;

            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            set;
        }

        [NativeProperty("GetGUIState().m_TargetPixelSize", true, TargetType.Field)]
        internal static extern Vector2 rootTargetPixelSize { get; set; }

        [NativeProperty("GetGUIState().m_OnGUIDepth", true, TargetType.Field)]
        internal static extern int guiDepth
        {
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            get;
        }

        ///<exclude />
        internal static extern Vector2 s_EditorScreenPointOffset
        {
            [NativeMethod("GetGUIState().GetGUIPixelOffset", true)]
            get;
            [NativeMethod("GetGUIState().SetGUIPixelOffset", true)]
            set;
        }

        [NativeProperty("GetGUIState().m_CanvasGUIState.m_IsMouseUsed", true, TargetType.Field)]
        internal static extern bool mouseUsed { get; set; }

        ///<exclude />
        [StaticAccessor("GetInputManager()", StaticAccessorType.Dot)]
        internal static extern bool textFieldInput { get; set; }

        internal static extern bool manualTex2SRGBEnabled
        {
            [FreeFunction("GUITexture::IsManualTex2SRGBEnabled")] get;
            [FreeFunction("GUITexture::SetManualTex2SRGBEnabled")] set;
        }

        ///<summary>Get access to the system-wide clipboard.</summary>
        ///<remarks>**Note:** tvOS does not support this feature.</remarks>
        public static extern string systemCopyBuffer
        {
            [FreeFunction("GetCopyBuffer")] get;
            [FreeFunction("SetCopyBuffer")] set;
        }

        [FreeFunction("GetGUIState().GetControlID")]
        static extern int Internal_GetControlID(int hint, FocusType focusType, Rect rect);

        // Control counting is required by ReorderableList. Element rendering callbacks can change and use
        // different number of controls to represent an element each frame. We need a way to be able to track
        // if the control count changed from the last frame so we can recache those elements.
        [NoAutoStaticsCleanup] // simple int counter reset each layout pass; default 0 after reload is a safe starting state
        internal static int s_ControlCount = 0;
        ///<summary>Get a unique ID for a control, using an integer as a hint to help ensure correct matching of IDs to controls.</summary>
        public static int GetControlID(int hint, FocusType focusType, Rect rect)
        {
            s_ControlCount++;
            return Internal_GetControlID(hint, focusType, rect);
        }

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

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static extern bool OwnsId(int id);

        ///<summary>Align a local space rectangle to the pixel grid.</summary>
        ///<remarks>Aligns the top-left and bottom-right corners of the provided local space rectangle to the pixel grid and returns the local space axis-aligned bounding box that encompasses those points.</remarks>
        ///<param name="widthInPixels">Width, in pixel units, of the axis-aligned bounding box that encompasses the aligned points.</param>
        ///<param name="heightInPixels">Height, in pixel units, of the axis-aligned bounding box that encompasses the aligned points.</param>
        ///<returns>The aligned rectangle in local space.</returns>
        public static extern Rect AlignRectToDevice(Rect rect, out int widthInPixels, out int heightInPixels);

        // Snaps each edge to the device pixel grid on every event type, unlike AlignRectToDevice which only snaps during Repaint.
        internal static extern Rect AlignRectToPixelGrid(Rect rect);

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
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
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
