// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.TextCore.Text;

namespace UnityEngine
{
    [NativeHeader("Modules/IMGUI/GUIStyle.bindings.h")]
    partial class GUIStyleState
    {
        [NativeProperty("Background", false, TargetType.Function)] public extern Texture2D background { get; set; }
        [NativeProperty("textColor", false, TargetType.Field)] public extern Color textColor { get; set; }

        [NativeProperty("scaledBackgrounds", false, TargetType.Function)]
        public extern Texture2D[] scaledBackgrounds { get; set; }

        [FreeFunction(Name = "GUIStyleState_Bindings::Init", IsThreadSafe = true)] private static extern IntPtr Init();
        [FreeFunction(Name = "GUIStyleState_Bindings::Cleanup", IsThreadSafe = true, HasExplicitThis = true)] private extern void Cleanup();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(GUIStyleState guiStyleState) => guiStyleState.m_Ptr;
        }
    }

    [RequiredByNativeCode]
    [NativeHeader("Modules/IMGUI/GUIStyle.bindings.h")]
    [NativeHeader("IMGUIScriptingClasses.h")]
    partial class GUIStyle
    {
        [NativeProperty("Name", false, TargetType.Function)] internal extern string rawName { get; set; }
        [NativeProperty("Font", false, TargetType.Function)] public extern Font font { get; set; }
        [NativeProperty("m_ImagePosition", false, TargetType.Field)] public extern ImagePosition imagePosition { get; set; }
        [NativeProperty("m_Alignment", false, TargetType.Field)] public extern TextAnchor alignment { get; set; }
        [NativeProperty("m_WordWrap", false, TargetType.Field)] public extern bool wordWrap { get; set; }
        [NativeProperty("m_Clipping", false, TargetType.Field)] public extern TextClipping clipping { get; set; }
        [NativeProperty("m_ContentOffset", false, TargetType.Field)] public extern Vector2 contentOffset { get; set; }
        [NativeProperty("m_FixedWidth", false, TargetType.Field)] public extern float fixedWidth { get; set; }
        [NativeProperty("m_FixedHeight", false, TargetType.Field)] public extern float fixedHeight { get; set; }
        [NativeProperty("m_StretchWidth", false, TargetType.Field)] public extern bool stretchWidth { get; set; }
        [NativeProperty("m_StretchHeight", false, TargetType.Field)] public extern bool stretchHeight { get; set; }
        [NativeProperty("m_FontSize", false, TargetType.Field)] public extern int fontSize { get; set; }
        [NativeProperty("m_FontStyle", false, TargetType.Field)] public extern FontStyle fontStyle { get; set; }
        [NativeProperty("m_RichText", false, TargetType.Field)] public extern bool richText { get; set; }
        [NativeProperty("m_IsGizmo", false, TargetType.Field)] internal extern bool isGizmo { get; set; }

        [Obsolete("Don't use clipOffset - put things inside BeginGroup instead. This functionality will be removed in a later version.", false)]
        [NativeProperty("m_ClipOffset", false, TargetType.Field)] public extern Vector2 clipOffset { get; set; }
        [NativeProperty("m_ClipOffset", false, TargetType.Field)] internal extern Vector2 Internal_clipOffset { get; set; }
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Create", IsThreadSafe = true)] private static extern IntPtr Internal_Create([Unmarshalled] GUIStyle self);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Copy", IsThreadSafe = true)] private static extern IntPtr Internal_Copy([Unmarshalled] GUIStyle self, GUIStyle other);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Destroy", IsThreadSafe = true)] private static extern void Internal_Destroy(IntPtr self);

        [FreeFunction(Name = "GUIStyle_Bindings::GetStyleStatePtr", IsThreadSafe = true, HasExplicitThis = true)]
        private extern IntPtr GetStyleStatePtr(int idx);

        [FreeFunction(Name = "GUIStyle_Bindings::AssignStyleState", HasExplicitThis = true)]
        private extern void AssignStyleState(int idx, IntPtr srcStyleState);

        [FreeFunction(Name = "GUIStyle_Bindings::GetRectOffsetPtr", HasExplicitThis = true)]
        private extern IntPtr GetRectOffsetPtr(int idx);

        [FreeFunction(Name = "GUIStyle_Bindings::AssignRectOffset", HasExplicitThis = true)]
        private extern void AssignRectOffset(int idx, IntPtr srcRectOffset);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Draw", HasExplicitThis = true)]
        private extern void Internal_Draw(Rect screenRect, GUIContent content, bool isHover, bool isActive, bool on, bool hasKeyboardFocus);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Draw2", HasExplicitThis = true)]
        private extern void Internal_Draw2(Rect position, GUIContent content, int controlID, bool on);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DrawCursor", HasExplicitThis = true)]
        private extern void Internal_DrawCursor(Rect position, GUIContent content, Vector2 pos, Color cursorColor);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DrawWithTextSelection", HasExplicitThis = true)]
        private extern void Internal_DrawWithTextSelection(Rect screenRect, GUIContent content, bool isHover, bool isActive,
            bool on, bool hasKeyboardFocus, bool drawSelectionAsComposition, Vector2 cursorFirstPosition, Vector2 cursorLastPosition, Color cursorColor,
            Color selectionColor);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_CalcSize", HasExplicitThis = true)]
        internal extern Vector2 Internal_CalcSize(GUIContent content);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_CalcSizeWithConstraints", HasExplicitThis = true)]
        internal extern Vector2 Internal_CalcSizeWithConstraints(GUIContent content, Vector2 maxSize);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_CalcHeight", HasExplicitThis = true)]
        private extern float Internal_CalcHeight(GUIContent content, float width);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_CalcMinMaxWidth", HasExplicitThis = true)]
        private extern Vector2 Internal_CalcMinMaxWidth(GUIContent content);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DrawPrefixLabel", HasExplicitThis = true)]
        private extern void Internal_DrawPrefixLabel(Rect position, GUIContent content, int controlID, bool on);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DrawContent", HasExplicitThis = true)]
        internal extern void Internal_DrawContent(Rect screenRect, GUIContent content, bool isHover, bool isActive, bool on, bool hasKeyboardFocus,
            bool hasTextInput, bool drawSelectionAsComposition, Vector2 cursorFirst, Vector2 cursorLast, Color cursorColor, Color selectionColor,
            Color imageColor, float textOffsetX, float textOffsetY, float imageTopOffset, float imageLeftOffset, bool overflowX, bool overflowY);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_GetTextRectOffset", HasExplicitThis = true)]
        internal extern Vector2 Internal_GetTextRectOffset(Rect screenRect, GUIContent content, Vector2 textSize);
        [FreeFunction(Name = "GUIStyle_Bindings::SetMouseTooltip")] internal static extern void SetMouseTooltip(string tooltip, Rect screenRect);
        [FreeFunction(Name = "GUIStyle_Bindings::IsTooltipActive")] internal static extern bool IsTooltipActive(string tooltip);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_GetCursorFlashOffset")] private static extern float Internal_GetCursorFlashOffset();
        [FreeFunction(Name = "GUIStyle::SetDefaultFont")] internal static extern void SetDefaultFont(Font font);
        [FreeFunction(Name = "GUIStyle::GetDefaultFont")] internal static extern Font GetDefaultFont();

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DestroyTextGenerator")]
        internal static extern void Internal_DestroyTextGenerator(int meshInfoId);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_CleanupAllTextGenerator")]
        internal static extern void Internal_CleanupAllTextGenerator();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(GUIStyle guiStyle) => guiStyle.m_Ptr;
        }
    }
}
