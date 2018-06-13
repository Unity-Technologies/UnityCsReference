// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeHeader("Modules/IMGUI/GUIStyle.bindings.h")]
    partial class GUIStyleState
    {
        [NativeProperty("Background", false, TargetType.Function)] public extern Texture2D background { get; set; }
        [NativeProperty("textColor", false, TargetType.Field)] public extern Color textColor { get; set; }

        [NativeProperty("scaledBackgrounds", false, TargetType.Field)]
        public extern Texture2D[] scaledBackgrounds { get; set; }

        [FreeFunction(Name = "GUIStyleState_Bindings::Init", IsThreadSafe = true)] private static extern IntPtr Init();
        [FreeFunction(Name = "GUIStyleState_Bindings::Cleanup", IsThreadSafe = true, HasExplicitThis = true)] private extern void Cleanup();
    }

    [RequiredByNativeCode]
    [NativeHeader("Modules/IMGUI/GUIStyle.bindings.h")]
    [NativeHeader("IMGUIScriptingClasses.h")]
    partial class GUIStyle
    {
        [NativeProperty("Name", false, TargetType.Function)] public extern string name { get; set; }
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

        [Obsolete("Don't use clipOffset - put things inside BeginGroup instead. This functionality will be removed in a later version.", false)]
        [NativeProperty("m_ClipOffset", false, TargetType.Field)] public extern Vector2 clipOffset { get; set; }
        [NativeProperty("m_ClipOffset", false, TargetType.Field)] internal extern Vector2 Internal_clipOffset { get; set; }

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Create", IsThreadSafe = true)] private static extern IntPtr Internal_Create(GUIStyle self);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Copy", IsThreadSafe = true)] private static extern IntPtr Internal_Copy(GUIStyle self, GUIStyle other);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Destroy", IsThreadSafe = true)] private static extern void Internal_Destroy(IntPtr self);

        [FreeFunction(Name = "GUIStyle_Bindings::GetStyleStatePtr", IsThreadSafe = true, HasExplicitThis = true)]
        private extern IntPtr GetStyleStatePtr(int idx);

        [FreeFunction(Name = "GUIStyle_Bindings::AssignStyleState", HasExplicitThis = true)]
        private extern void AssignStyleState(int idx, IntPtr srcStyleState);

        [FreeFunction(Name = "GUIStyle_Bindings::GetRectOffsetPtr", HasExplicitThis = true)]
        private extern IntPtr GetRectOffsetPtr(int idx);

        [FreeFunction(Name = "GUIStyle_Bindings::AssignRectOffset", HasExplicitThis = true)]
        private extern void AssignRectOffset(int idx, IntPtr srcRectOffset);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_GetLineHeight")]
        private static extern float Internal_GetLineHeight(IntPtr target);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Draw", HasExplicitThis = true)]
        private extern void Internal_Draw(Rect screenRect, GUIContent content, bool isHover, bool isActive, bool on, bool hasKeyboardFocus);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Draw2", HasExplicitThis = true)]
        private extern void Internal_Draw2(Rect position, GUIContent content, int controlID, bool on);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DrawCursor", HasExplicitThis = true)]
        private extern void Internal_DrawCursor(Rect position, GUIContent content, int pos, Color cursorColor);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DrawWithTextSelection", HasExplicitThis = true)]
        private extern void Internal_DrawWithTextSelection(Rect screenRect, GUIContent content, bool isHover, bool isActive,
            bool on, bool hasKeyboardFocus, bool drawSelectionAsComposition, int cursorFirst, int cursorLast, Color cursorColor,
            Color selectionColor);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_GetCursorPixelPosition", HasExplicitThis = true)]
        internal extern Vector2 Internal_GetCursorPixelPosition(Rect position, GUIContent content, int cursorStringIndex);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_GetCursorStringIndex", HasExplicitThis = true)]
        internal extern int Internal_GetCursorStringIndex(Rect position, GUIContent content, Vector2 cursorPixelPosition);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_GetNumCharactersThatFitWithinWidth", HasExplicitThis = true)]
        internal extern int Internal_GetNumCharactersThatFitWithinWidth(string text, float width);

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

        [FreeFunction(Name = "GUIStyle_Bindings::SetMouseTooltip")] internal static extern void SetMouseTooltip(string tooltip, Rect screenRect);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_GetCursorFlashOffset")] private static extern float Internal_GetCursorFlashOffset();
        [FreeFunction(Name = "GUIStyle::SetDefaultFont")] internal static extern void SetDefaultFont(Font font);
    }
}
