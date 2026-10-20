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
    ///<summary>Specialized values for the given states used by <see cref="GUIStyle" /> objects.</summary>
    ///<remarks>The GUIStyle contains all values for displaying GUI elements.</remarks>
    [NativeHeader("Modules/IMGUI/GUIStyle.bindings.h")]
    public partial class GUIStyleState
    {
        ///<summary>The background image used by GUI elements in this given state.</summary>
        ///<remarks>See also: <see cref="P:UnityEngine.GUIStyleState.scaledBackgrounds" />.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Assigns a texture to customStyles[0] for when the control
        ///    // is pressed down
        ///
        ///    Texture2D aTexture;
        ///
        ///    void OnGUI()
        ///    {
        ///        if (!aTexture)
        ///        {
        ///            Debug.LogError("Assign a texture on the editor first");
        ///            return;
        ///        }
        ///        if (GUI.skin.customStyles.Length > 0)
        ///        {
        ///            GUI.skin.customStyles[0].active.background = aTexture;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("Background", false, TargetType.Function)] public extern Texture2D background { get; set; }
        ///<summary>The text color used by GUI elements in this state.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Sets the text color to yellow of customStyles[0] when an
        ///    // element is turned on and pressed down
        ///    void OnGUI()
        ///    {
        ///        if (GUI.skin.customStyles.Length > 0)
        ///        {
        ///            GUI.skin.customStyles[0].onActive.textColor = Color.yellow;
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("textColor", false, TargetType.Field)] public extern Color textColor { get; set; }

        ///<summary>Background images used by this state when on a high-resolution screen. It should either be left empty, or contain a single image that is exactly twice the resolution of <see cref="background" />. This is only used by the editor. The field is not copied to player data, and is not accessible from player code.</summary>
        ///<seealso cref="P:UnityEditor.EditorGUIUtility.pixelsPerPoint" />
        [NativeProperty("scaledBackgrounds", false, TargetType.Function)]
        public extern Texture2D[] scaledBackgrounds { get; set; }

        [FreeFunction(Name = "GUIStyleState_Bindings::Init", IsThreadSafe = true)] private static extern IntPtr Init();
        [FreeFunction(Name = "GUIStyleState_Bindings::Cleanup", IsThreadSafe = true, HasExplicitThis = true)] private extern void Cleanup();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(GUIStyleState guiStyleState) => guiStyleState.m_Ptr;
        }
    }

    ///<summary>Styling information for GUI elements.</summary>
    ///<remarks>Most GUI functions accept an optional GUIStyle parameter to override the default style. This allows coloring, fonts and other details to be changed and switched for different states (eg, when the mouse is hovering over the control). Where a consistent look-and-feel is required over a whole GUI design, the GUISkin class is a useful way to collect a set of GUIStyle settings and apply them all at once.</remarks>
    [RequiredByNativeCode]
    [NativeHeader("Modules/IMGUI/GUIStyle.bindings.h")]
    [NativeHeader("IMGUIScriptingClasses.h")]
    public partial class GUIStyle
    {
        [NativeProperty("Name", false, TargetType.Function)] internal extern string rawName { get; set; }
        ///<summary>The font to use for rendering. If null, the default font for the current <see cref="GUISkin" /> is used instead.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints name of the font that button is using.
        ///
        ///    void OnGUI()
        ///    {
        ///        Debug.Log("Font name: " + GUI.skin.button.font.name);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("Font", false, TargetType.Function)] public extern Font font { get; set; }
        ///<summary>How image and text of the <see cref="GUIContent" /> is combined.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class ExampleScript : MonoBehaviour
        ///{
        ///    // Prints how image and text is placed.
        ///    void OnGUI()
        ///    {
        ///        Debug.Log(GUI.skin.button.imagePosition);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("m_ImagePosition", false, TargetType.Field)] public extern ImagePosition imagePosition { get; set; }
        ///<summary>Text alignment.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints how text alignment is set.
        ///
        ///    void OnGUI()
        ///    {
        ///        Debug.Log(GUI.skin.button.alignment);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("m_Alignment", false, TargetType.Field)] public extern TextAnchor alignment { get; set; }
        ///<summary>Should the text be wordwrapped?</summary>
        ///<remarks>This will cause any text contrained to be wordwrapped to fit within the width of a control.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.button.wordWrap = true;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("m_WordWrap", false, TargetType.Field)] public extern bool wordWrap { get; set; }
        ///<summary>What to do when the contents to be rendered is too large to fit within the area given.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints how is managed the text when the contents rendered
        ///    // are too large to fir in the area given.
        ///
        ///    void OnGUI()
        ///    {
        ///        Debug.Log(GUI.skin.button.clipping);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("m_Clipping", false, TargetType.Field)] public extern TextClipping clipping { get; set; }
        ///<summary>Pixel offset to apply to the content of this GUIstyle.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints the value of contentOffset.
        ///
        ///    void OnGUI()
        ///    {
        ///        Debug.Log(GUI.skin.button.contentOffset);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("m_ContentOffset", false, TargetType.Field)] public extern Vector2 contentOffset { get; set; }
        [NativeProperty("m_ContentSpacing", false, TargetType.Field)] internal extern float contentSpacing { get; set; }
        ///<summary>If non-0, any GUI elements rendered with this style will have the width specified here.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints the value of fixedWidth.
        ///
        ///    void OnGUI()
        ///    {
        ///        Debug.Log(GUI.skin.button.fixedWidth);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("m_FixedWidth", false, TargetType.Field)] public extern float fixedWidth { get; set; }
        ///<summary>If non-0, any GUI elements rendered with this style will have the height specified here.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints the value of fixedHeight.
        ///
        ///    void OnGUI()
        ///    {
        ///        Debug.Log(GUI.skin.button.fixedHeight);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("m_FixedHeight", false, TargetType.Field)] public extern float fixedHeight { get; set; }
        ///<summary>Can GUI elements of this style be stretched horizontally for better layouting?</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.button.stretchWidth = true;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("m_StretchWidth", false, TargetType.Field)] public extern bool stretchWidth { get; set; }
        ///<summary>Can GUI elements of this style be stretched vertically for better layout?</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.button.stretchHeight = true;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty("m_StretchHeight", false, TargetType.Field)] public extern bool stretchHeight { get; set; }
        ///<summary>The font size to use (for dynamic fonts).</summary>
        ///<remarks>If this is set to a non-zero value, the font size specified in the font importer is overriden with a custom size.
        ///This is only supported for fonts set to use dynamic font rendering. Other fonts will always use the default font size.</remarks>
        [NativeProperty("m_FontSize", false, TargetType.Field)] public extern int fontSize { get; set; }
        ///<summary>The font style to use (for dynamic fonts).</summary>
        ///<remarks>If this is set to a value other then normal, the font style set in the font importer is overriden with a custom style.
        ///This is only supported for fonts set to use dynamic font rendering. Other fonts will always render in normal style.</remarks>
        [NativeProperty("m_FontStyle", false, TargetType.Field)] public extern FontStyle fontStyle { get; set; }
        ///<summary>Enable HTML-style tags for Text Formatting Markup.</summary>
        ///<remarks>See the manual page about [Rich Text](xref:StyledText) for a list of supported tags.</remarks>
        [NativeProperty("m_RichText", false, TargetType.Field)] public extern bool richText { get; set; }
        [NativeProperty("m_ImageIsTopAligned", false, TargetType.Field)] internal extern bool imageIsTopAligned { get; set; }
        [NativeProperty("m_IsSDF", false, TargetType.Field)] internal extern bool isSDF { get; set; }

        ///<exclude />
        [Obsolete("Don't use clipOffset - put things inside BeginGroup instead. This functionality will be removed in a later version.", false)]
        [NativeProperty("m_ClipOffset", false, TargetType.Field)] public extern Vector2 clipOffset { get; set; }
        [NativeProperty("m_ClipOffset", false, TargetType.Field)] internal extern Vector2 Internal_clipOffset { get; set; }
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Create", IsThreadSafe = true)] private static extern IntPtr Internal_Create([UnityMarshalAs(NativeType.ScriptingObjectPtr)] GUIStyle self);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_Copy", IsThreadSafe = true)] private static extern IntPtr Internal_Copy([UnityMarshalAs(NativeType.ScriptingObjectPtr)] GUIStyle self, GUIStyle other);
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

        // Re-link the native peer's back-reference to its wrapper after the
        // unmanaged byte transfer overwrites it. GUIDebugger relies on it.
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_EnsureCachedScriptingObject", IsThreadSafe = true)]
        private static extern void Internal_EnsureCachedScriptingObject([UnityMarshalAs(NativeType.ScriptingObjectPtr)] GUIStyle self);

        private static void ManagedSerializationPostDispatchHook(object wrapper, IntPtr nativePtr)
        {
            var style = (GUIStyle)wrapper;
            Internal_EnsureCachedScriptingObject(style);
            style.InternalOnAfterDeserialize();
        }

        [RequiredByNativeCode]
        internal static unsafe IntPtr GetGUIStylePostDispatchHookFunctionPointer()
            => (IntPtr)(delegate*<object, IntPtr, void>)&ManagedSerializationPostDispatchHook;

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DrawPrefixLabel", HasExplicitThis = true)]
        private extern void Internal_DrawPrefixLabel(Rect position, GUIContent content, int controlID, bool on);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_DrawContent", HasExplicitThis = true)]
        internal extern void Internal_DrawContent(Rect screenRect, GUIContent content, bool isHover, bool isActive, bool on, bool hasKeyboardFocus,
            bool hasTextInput, bool drawSelectionAsComposition, Vector2 cursorFirst, Vector2 cursorLast, Color cursorColor, Color selectionColor,
            Color imageColor, float textOffsetX, float textOffsetY, float imageTopOffset, float imageLeftOffset, bool overflowX, bool overflowY);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_CalcContentLayoutForTests", HasExplicitThis = true)]
        internal extern bool Internal_CalcContentLayoutForTests(Rect position, GUIContent content, bool overflowX, bool overflowY,
            out Rect contentRect, out Rect textRect, out Rect imageRect, out Rect clipRect);

        [FreeFunction(Name = "GUIStyle_Bindings::Internal_GetTextRectOffset", HasExplicitThis = true)]
        internal extern Vector2 Internal_GetTextRectOffset(Rect screenRect, GUIContent content, Vector2 textSize);
        [FreeFunction(Name = "GUIStyle_Bindings::SetMouseTooltip")] internal static extern void SetMouseTooltip(string tooltip, Rect screenRect);
        [FreeFunction(Name = "GUIStyle_Bindings::IsTooltipActive")] internal static extern bool IsTooltipActive(string tooltip);
        [FreeFunction(Name = "GUIStyle_Bindings::Internal_GetCursorFlashOffset")] private static extern float Internal_GetCursorFlashOffset();
        ///<summary>Set the default font used if null is used.</summary>
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
