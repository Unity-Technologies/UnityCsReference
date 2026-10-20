// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Scripting;
using UnityEngine.TextCore.Text;

namespace UnityEngine
{
    // Specialized values for the given states used by [[GUIStyle]] objects.
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public sealed partial class GUIStyleState
    {
        // Pointer to the GUIStyleState INSIDE a GUIStyle.
        ///<exclude />
        [NonSerialized]
        internal IntPtr m_Ptr;

        // Pointer to the source GUIStyle so it doesn't get garbage collected.
        // If NULL, it means we own m_Ptr and need to delete it when this gets disposed
        readonly GUIStyle m_SourceStyle;

        ///<exclude />
        public GUIStyleState()
        {
            m_Ptr = Init();
        }

        private GUIStyleState(GUIStyle sourceStyle, IntPtr source)
        {
            m_SourceStyle = sourceStyle;
            m_Ptr = source;
        }

        //It's only safe to call this during a deserialization operation.
        internal static GUIStyleState ProduceGUIStyleStateFromDeserialization(GUIStyle sourceStyle, IntPtr source)
        {
            GUIStyleState newState = new GUIStyleState(sourceStyle, source);
            return newState;
        }

        internal static GUIStyleState GetGUIStyleState(GUIStyle sourceStyle, IntPtr source)
        {
            GUIStyleState newState = new GUIStyleState(sourceStyle, source);
            return newState;
        }

#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
        ~GUIStyleState()
        {
            if (m_SourceStyle == null)
            {
                Cleanup();
                m_Ptr = IntPtr.Zero;
            }
        }
#pragma warning restore UA5000
    }

    ///<summary>How image and text is placed inside <see cref="GUIStyle" />.</summary>
    public enum ImagePosition
    {
        ///<summary>Image is to the left of the text.</summary>
        ImageLeft = 0,
        ///<summary>Image is above the text.</summary>
        ImageAbove = 1,
        ///<summary>Only the image is displayed.</summary>
        ImageOnly = 2,
        ///<summary>Only the text is displayed.</summary>
        TextOnly = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public sealed partial class GUIStyle
    {
        ///<summary>Constructor for empty GUIStyle.</summary>
        public GUIStyle()
        {
            m_Ptr = Internal_Create(this);
        }

        ///<summary>Constructs GUIStyle identical to given other GUIStyle.</summary>
        public GUIStyle(GUIStyle other)
        {
            if (other == null)
            {
                Debug.LogError("Copied style is null. Using StyleNotFound instead.");
                other = GUISkin.error;
            }
            m_Ptr = Internal_Copy(this, other);
        }

#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
        ~GUIStyle()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }
#pragma warning restore UA5000

        //Called during Deserialization from cpp
        internal void InternalOnAfterDeserialize()
        {
            m_Normal    = GUIStyleState.ProduceGUIStyleStateFromDeserialization(this, GetStyleStatePtr(0));
            m_Hover     = GUIStyleState.ProduceGUIStyleStateFromDeserialization(this, GetStyleStatePtr(1));
            m_Active    = GUIStyleState.ProduceGUIStyleStateFromDeserialization(this, GetStyleStatePtr(2));
            m_Focused   = GUIStyleState.ProduceGUIStyleStateFromDeserialization(this, GetStyleStatePtr(3));
            m_OnNormal  = GUIStyleState.ProduceGUIStyleStateFromDeserialization(this, GetStyleStatePtr(4));
            m_OnHover   = GUIStyleState.ProduceGUIStyleStateFromDeserialization(this, GetStyleStatePtr(5));
            m_OnActive  = GUIStyleState.ProduceGUIStyleStateFromDeserialization(this, GetStyleStatePtr(6));
            m_OnFocused = GUIStyleState.ProduceGUIStyleStateFromDeserialization(this, GetStyleStatePtr(7));
        }

        ///<exclude />
        [NonSerialized]
        internal IntPtr m_Ptr;

        [NonSerialized]
        GUIStyleState m_Normal, m_Hover, m_Active, m_Focused, m_OnNormal, m_OnHover, m_OnActive, m_OnFocused;

        [NonSerialized]
        RectOffset m_Border, m_Padding, m_Margin, m_Overflow;

        [NonSerialized]
        string m_Name;

        // Internal callback used to override how gui styles are rendered.
        [AutoStaticsCleanupOnCodeReload]
        internal static DrawHandler onDraw;
        // Cache StyleBlock ID
        internal int blockId;

        ///<summary>The name of this GUIStyle. Used for getting them based on name.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints the name of the style.
        ///
        ///    void OnGUI()
        ///    {
        ///        Debug.Log(GUI.skin.button.name);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public string name
        {
            get { return m_Name ?? (m_Name = rawName); }
            set
            {
                m_Name = value;
                rawName = value;
            }
        }

        ///<summary>Rendering settings for when the component is displayed normally.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints the text color that button is using.
        ///
        ///    void OnGUI()
        ///    {
        ///        Debug.Log(GUI.skin.button.normal.textColor);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyleState normal
        {
            get
            {
                //GUIStyleState can't be initialized in the constructor
                //since constructors can be called within and outside a serialization operation
                //So we delay the initialization here where we know we will be on the main thread, outside
                //any loading operation.
                return m_Normal ?? (m_Normal = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(0)));
            }
            set { AssignStyleState(0, value.m_Ptr); }
        }

        ///<summary>Rendering settings for when the mouse is hovering over the control.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints the text color that button is using
        ///    // when the mouse is hovering over a control
        ///
        ///    void OnGUI()
        ///    {
        ///        Debug.Log(GUI.skin.button.hover.textColor);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyleState hover
        {
            get { return m_Hover ?? (m_Hover = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(1))); }
            set { AssignStyleState(1, value.m_Ptr); }
        }

        ///<summary>Rendering settings for when the control is pressed down.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Assigns a texture to button for when the control
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
        ///
        ///        GUI.skin.button.active.background = aTexture;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyleState active
        {
            get { return m_Active ?? (m_Active = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(2))); }
            set { AssignStyleState(2, value.m_Ptr); }
        }

        ///<summary>Rendering settings for when the control is turned on.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.button.onNormal.textColor = Color.red;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyleState onNormal
        {
            get { return m_OnNormal ?? (m_OnNormal = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(4))); }
            set { AssignStyleState(4, value.m_Ptr); }
        }

        ///<summary>Rendering settings for when the control is turned on and the mouse is hovering it.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.button.onHover.textColor = Color.cyan;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyleState onHover
        {
            get { return m_OnHover ?? (m_OnHover = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(5))); }
            set { AssignStyleState(5, value.m_Ptr); }
        }

        ///<summary>Rendering settings for when the element is turned on and pressed down.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Sets the text color of button to yellow when an
        ///    // element is turned on and pressed down.
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.button.onActive.textColor = Color.yellow;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyleState onActive
        {
            get { return m_OnActive ?? (m_OnActive = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(6))); }
            set { AssignStyleState(6, value.m_Ptr); }
        }

        ///<summary>Rendering settings for when the element has keyboard focus.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.button.focused.textColor = Color.blue;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyleState focused
        {
            get { return m_Focused ?? (m_Focused = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(3))); }
            set { AssignStyleState(3, value.m_Ptr); }
        }

        ///<summary>Rendering settings for when the element has keyboard and is turned on.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        GUI.skin.button.onFocused.textColor = Color.green;
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public GUIStyleState onFocused
        {
            get { return m_OnFocused ?? (m_OnFocused = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(7))); }
            set { AssignStyleState(7, value.m_Ptr); }
        }

        ///<summary>The borders of all background images.</summary>
        ///<remarks>This corresponds to the border settings for IMGUI elements. It only affects the rendering of the background image and has no effect on positioning.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints the left, right, top and down values of the GUIStyle border
        ///
        ///    RectOffset bdr;
        ///    void OnGUI()
        ///    {
        ///        bdr = GUI.skin.button.border;
        ///        Debug.Log("Left: " + bdr.left + " Right: " + bdr.right);
        ///        Debug.Log("Top: " + bdr.top + " Bottom: " + bdr.bottom);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public RectOffset border
        {
            get { return m_Border ?? (m_Border = new RectOffset(this, GetRectOffsetPtr(0))); }
            set { AssignRectOffset(0, value.m_Ptr); }
        }

        ///<summary>The margins between elements rendered in this style and any other GUI elements.</summary>
        ///<remarks>This only has effect when using automatic layout ().</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints the left, right, top and down values of the GUIStyle margin
        ///
        ///    RectOffset rctOff;
        ///
        ///    void OnGUI()
        ///    {
        ///        rctOff = GUI.skin.button.margin;
        ///        Debug.Log("Left: " + rctOff.left + " Right: " + rctOff.right);
        ///        Debug.Log("Top: " + rctOff.top + " Bottom: " + rctOff.bottom);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="GUILayout" />
        public RectOffset margin
        {
            get { return m_Margin ?? (m_Margin = new RectOffset(this, GetRectOffsetPtr(1))); }
            set { AssignRectOffset(1, value.m_Ptr); }
        }

        ///<summary>Space from the edge of <see cref="GUIStyle" /> to the start of the contents.</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints the left, right, top and down values of the GUIStyle overflow
        ///
        ///    RectOffset rctOff;
        ///
        ///    void OnGUI()
        ///    {
        ///        rctOff = GUI.skin.button.padding;
        ///        Debug.Log("Left: " + rctOff.left + " Right: " + rctOff.right);
        ///        Debug.Log("Top: " + rctOff.top + " Bottom: " + rctOff.bottom);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public RectOffset padding
        {
            get { return m_Padding ?? (m_Padding = new RectOffset(this, GetRectOffsetPtr(2))); }
            set { AssignRectOffset(2, value.m_Ptr); }
        }

        ///<summary>Extra space to be added to the background image.</summary>
        ///<remarks>This is used if your image has a drop shadow and you want to extend the background image beyond the rectangles specified for gui elements that use this style.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints the left, right, top and down values of the GUIStyle overflow
        ///
        ///    RectOffset rctOff;
        ///
        ///    void OnGUI()
        ///    {
        ///        rctOff = GUI.skin.button.overflow;
        ///        Debug.Log("Left: " + rctOff.left + " Right: " + rctOff.right);
        ///        Debug.Log("Top: " + rctOff.top + " Bottom: " + rctOff.bottom);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public RectOffset overflow
        {
            get { return m_Overflow ?? (m_Overflow = new RectOffset(this, GetRectOffsetPtr(3))); }
            set { AssignRectOffset(3, value.m_Ptr); }
        }

        ///<summary>The height of one line of text with this style, measured in pixels. (RO)</summary>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    // Prints the lineHeight value.
        ///
        ///    void OnGUI()
        ///    {
        ///        Debug.Log(GUI.skin.button.lineHeight);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public float lineHeight => GUIUtility.RoundToPixelGrid(IMGUITextHandle.GetLineHeight(this), GUIUtility.pixelsPerPoint);

        ///<summary>Draw this GUIStyle on to the screen, internal version.</summary>
        ///<remarks>Draw plain GUIStyle without text nor image.</remarks>
        public void Draw(Rect position, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            Draw(position, GUIContent.none, -1, isHover, isActive, on, hasKeyboardFocus);
        }

        ///<summary>Draw the GUIStyle with a text string inside.</summary>
        public void Draw(Rect position, string text, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            Draw(position, GUIContent.Temp(text), -1, isHover, isActive, on, hasKeyboardFocus);
        }

        ///<summary>Draw the GUIStyle with an image inside. If the image is too large to fit within the content area of the style it is scaled down.</summary>
        public void Draw(Rect position, Texture image, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            Draw(position, GUIContent.Temp(image), -1, isHover, isActive, on, hasKeyboardFocus);
        }

        ///<summary>Draw the GUIStyle with text and an image inside. If the image is too large to fit within the content area of the style it is scaled down.</summary>
        public void Draw(Rect position, GUIContent content, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            Draw(position, content, -1, isHover, isActive, on, hasKeyboardFocus);
        }

        ///<summary>Draw the GUIStyle with text and an image inside. If the image is too large to fit within the content area of the style it is scaled down.</summary>
        public void Draw(Rect position, GUIContent content, int controlID)
        {
            Draw(position, content, controlID, false, false, false, false);
        }

        ///<summary>Draw the GUIStyle with text and an image inside. If the image is too large to fit within the content area of the style it is scaled down.</summary>
        public void Draw(Rect position, GUIContent content, int controlID, bool on)
        {
            Draw(position, content, controlID, false, false, on, false);
        }

        public void Draw(Rect position, GUIContent content, int controlID, bool on, bool hover)
        {
            Draw(position, content, controlID, hover, GUIUtility.hotControl == controlID, on, GUIUtility.HasKeyFocus(controlID));
        }

        private void Draw(Rect position, GUIContent content, int controlId, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            if (Event.current.type != EventType.Repaint)
                throw new Exception("Style.Draw may not be called if it is not a repaint event");

            if (content == null)
                throw new Exception("Style.Draw may not be called with GUIContent that is null.");

            var drawStates = new DrawStates(controlId, isHover, isActive, on, hasKeyboardFocus);
            if (onDraw == null || !onDraw(this, position, content, drawStates))
            {
                if (controlId == -1)
                    Internal_Draw(position, content, isHover, isActive, on, hasKeyboardFocus);
                else
                    Internal_Draw2(position, content, controlId, on);
            }
        }

        // PrefixLabel has to be drawn with an alternative draw method.
        // The normal draw methods use MonoGUIContentToTempNative which means they all share the same temp GUIContent on the native side.
        // A native IMGUI control such as GUIButton is already using this temp GUIContent when it calls GetControlID, which,
        // because of the delayed feature in PrefixLabel, can end up calling a style draw function again to draw the PrefixLabel.
        // This draw call cannot use the same temp GUIContent that is already needed for the GUIButton control itself,
        // so it has to use this alternative code path that uses a different GUIContent to store the content in.
        // We can all agree this workaround is not nice at all. But nobody seemed to be able to come up with something better.
        internal void DrawPrefixLabel(Rect position, GUIContent content, int controlID)
        {
            if (content != null)
            {
                var drawStates = new DrawStates(controlID, position.Contains(Event.current.mousePosition), false, false,
                    GUIUtility.HasKeyFocus(controlID));
                if (onDraw == null || !onDraw(this, position, content, drawStates))
                    Internal_DrawPrefixLabel(position, content, controlID, false);
            }
            else
                Debug.LogError("Style.DrawPrefixLabel may not be called with GUIContent that is null.");
        }


        [AutoStaticsCleanupOnCodeReload]
        // Does the ID-based Draw function show keyboard focus? Disabled by windows when they don't have keyboard focus
        internal static bool showKeyboardFocus = true;

        ///<summary>Draw this GUIStyle with selected content.</summary>
        public void DrawCursor(Rect position, GUIContent content, int controlID, int character)
        {
            Event e = Event.current;
            if (e.type == EventType.Repaint)
            {
                // Figure out the cursor color...
                Color cursorColor = new Color(0, 0, 0, 0);
                float cursorFlashSpeed = GUI.skin.settings.cursorFlashSpeed;
                float cursorFlashRel = (Time.realtimeSinceStartup - Internal_GetCursorFlashOffset()) % cursorFlashSpeed / cursorFlashSpeed;
                if (cursorFlashSpeed == 0 || cursorFlashRel < .5f)
                    cursorColor = GUI.skin.settings.cursorColor;

                Internal_DrawCursor(position, content, GetCursorPixelPosition(position, content, character), cursorColor);
            }
        }

        internal void DrawWithTextSelection(Rect position, GUIContent content, bool isActive, bool hasKeyboardFocus,
            int firstSelectedCharacter, int lastSelectedCharacter, bool drawSelectionAsComposition, Color selectionColor)
        {
            if (Event.current.type != EventType.Repaint)
            {
                Debug.LogError("Style.Draw may not be called if it is not a repaint event");
                return;
            }

            if (firstSelectedCharacter > lastSelectedCharacter)
            {
                var temp = lastSelectedCharacter;
                lastSelectedCharacter = firstSelectedCharacter;
                firstSelectedCharacter = temp;
            }

            Vector2 firstSelectedCharacterPosition = GetCursorPixelPosition(position, content, firstSelectedCharacter);
            Vector2 lastSelectedCharacterPosition = GetCursorPixelPosition(position, content, lastSelectedCharacter);

            // This offset is only required for IMGUI, so the change from TextNative->TextCore doesn't change the rendering
            var imguiOffset = new Vector2(string.IsNullOrEmpty(content.text) ? 0f : 1f, 0f);
            firstSelectedCharacterPosition -= imguiOffset;
            lastSelectedCharacterPosition -= imguiOffset;

            // Figure out the cursor color...
            Color cursorColor = new Color(0, 0, 0, 0);
            float cursorFlashSpeed = GUI.skin.settings.cursorFlashSpeed;
            float cursorFlashRel = (Time.realtimeSinceStartup - Internal_GetCursorFlashOffset()) % cursorFlashSpeed / cursorFlashSpeed;
            if (cursorFlashSpeed == 0 || cursorFlashRel < .5f)
                cursorColor = GUI.skin.settings.cursorColor;

            bool hovered = position.Contains(Event.current.mousePosition);
            var drawStates = new DrawStates(-1, hovered, isActive, false, hasKeyboardFocus,
                drawSelectionAsComposition, firstSelectedCharacterPosition, lastSelectedCharacterPosition, cursorColor, selectionColor);
            if (onDraw == null || !onDraw(this, position, content, drawStates))
            {
                Internal_DrawWithTextSelection(position, content, hovered, isActive, false, hasKeyboardFocus,
                    drawSelectionAsComposition, firstSelectedCharacterPosition, lastSelectedCharacterPosition, cursorColor, selectionColor);
            }
        }

        internal void DrawWithTextSelection(Rect position, GUIContent content, int controlID, int firstSelectedCharacter,
            int lastSelectedCharacter, bool drawSelectionAsComposition)
        {
            DrawWithTextSelection(position, content, controlID == GUIUtility.hotControl,
                controlID == GUIUtility.keyboardControl && showKeyboardFocus,
                firstSelectedCharacter, lastSelectedCharacter, drawSelectionAsComposition, GUI.skin.settings.selectionColor);
        }

        ///<summary>Draw this GUIStyle with selected content.</summary>
        public void DrawWithTextSelection(Rect position, GUIContent content, int controlID, int firstSelectedCharacter, int lastSelectedCharacter)
        {
            DrawWithTextSelection(position, content, controlID, firstSelectedCharacter, lastSelectedCharacter, false);
        }

        ///<summary>Get a named GUI style from the current skin.</summary>
        public static implicit operator GUIStyle(string str)
        {
            if (GUISkin.current == null)
            {
                Debug.LogError("Unable to use a named GUIStyle without a current skin. Most likely you need to move your GUIStyle initialization code to OnGUI");
                return GUISkin.error;
            }
            return GUISkin.current.GetStyle(str);
        }

        ///<summary>Shortcut for an empty GUIStyle.</summary>
        ///<remarks>This style contains no decoration and just renders everything in the default font.</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///
        ///public class Example : MonoBehaviour
        ///{
        ///    void OnGUI()
        ///    {
        ///        // Make a button with no decoration
        ///        GUI.Button(new Rect(0, 0, 250, 100), "Basic Button", GUIStyle.none);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static GUIStyle none => s_None ?? (s_None = new GUIStyle());
        [NoAutoStaticsCleanup] // lazy-cache empty GUIStyle sentinel; recreated on null check; no user types
        static GUIStyle s_None;

        // This is to be used only internally in tests. It will affect all of IMGUI.
        [NoAutoStaticsCleanup] // test override flag; null (default) restores default behavior after reload
        internal static bool? useAdvancedText = null;

        ///<summary>Get the pixel position of a given string index.</summary>
        public Vector2 GetCursorPixelPosition(Rect position, GUIContent content, int cursorStringIndex)
        {
            Rect drawRect = position;
            drawRect.width = fixedWidth == 0f ? drawRect.width : fixedWidth;
            drawRect.height = fixedHeight == 0f ? drawRect.height : fixedHeight;
            var handle = IMGUITextHandle.GetTextHandle(this, padding.Remove(drawRect), IMGUITextHandle.IsAdvancedTextEnabled() ? content.text : content.textWithWhitespace, Color.white, false);
            var cursorPos = handle.GetCursorPositionFromStringIndexUsingLineHeight(cursorStringIndex);
            cursorPos = new Vector2(Mathf.Max(0.0f, cursorPos.x), cursorPos.y);
            var rectOffset = Internal_GetTextRectOffset(drawRect, content, new Vector2(handle.preferredSize.x, handle.preferredSize.y > 0 ? handle.preferredSize.y : lineHeight));
            return cursorPos + rectOffset + Internal_clipOffset - new Vector2(0, lineHeight);
        }

        internal Rect[] GetHyperlinkRects(IMGUITextHandle handle, Rect content)
        {
            content = padding.Remove(content);
            return handle.GetHyperlinkRects(content);
        }

        ///<summary>Get the cursor position (indexing into contents.text) when the user clicked at cursorPixelPosition.</summary>
        ///<remarks>This does not respect any images inside content.</remarks>
        public int GetCursorStringIndex(Rect position, GUIContent content, Vector2 cursorPixelPosition)
        {
            var handle = IMGUITextHandle.GetTextHandle(this, position, IMGUITextHandle.IsAdvancedTextEnabled() ? content.text : content.textWithWhitespace, Color.white, false);
            handle.AddToPermanentCacheAndGenerateMesh();
            return handle.GetCursorIndexFromPosition(cursorPixelPosition);
        }

        ///<summary>Returns number of characters that can fit within width, returns -1 if fails due to missing font.</summary>
        internal int GetNumCharactersThatFitWithinWidth(string text, float width)
        {
            return IMGUITextHandle.GetTextHandle(this, new Rect(0, 0, width, 1), text, Color.white, false).GetNumCharactersThatFitWithinWidth(width);
        }

        ///<summary>Calculate the size of some content if it is rendered with this style.</summary>
        ///<remarks>This function does not take word wrapping into account. To do that, you
        ///        need to determine the allocated width and then call <see cref="CalcHeight" /> to figure out
        ///        the word wrapped height.</remarks>
        ///<example>
        ///  <code><![CDATA[
        /// // Example for the GUIStyle.CalcSize
        ///
        ///using UnityEngine;
        ///
        ///public class CalcSizeExample : MonoBehaviour
        ///{
        ///    string s;
        ///
        ///    void Start()
        ///    {
        ///        s = "A string for GUIContent()";
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUIContent content = new GUIContent(s);
        ///
        ///        GUIStyle style = GUI.skin.box;
        ///        style.alignment = TextAnchor.MiddleCenter;
        ///
        ///        // Compute how large the button needs to be.
        ///        Vector2 size = style.CalcSize(content);
        ///
        ///        // make the Box double sized
        ///        GUI.Box(new Rect(10.0f, 10.0f, 2.0f * size.x, 2.0f * size.y), s);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public Vector2 CalcSize(GUIContent content)
        {
            return Internal_CalcSize(content);
        }

        // Calculate the size of a some content if it is rendered with this style.
        internal Vector2 CalcSizeWithConstraints(GUIContent content, Vector2 constraints)
        {
            var size = Internal_CalcSizeWithConstraints(content, constraints);
            if (constraints.x > 0)
                size.x = Mathf.Min(size.x, constraints.x);
            if (constraints.y > 0)
                size.y = Mathf.Min(size.y, constraints.y);
            return size;
        }

        ///<summary>Calculate the size of an element formatted with this style, and a given space to content.</summary>
        public Vector2 CalcScreenSize(Vector2 contentSize)
        {
            return new Vector2(
                (fixedWidth != 0.0f ? fixedWidth : Mathf.Ceil(contentSize.x + padding.left + padding.right)),
                (fixedHeight != 0.0f ? fixedHeight : Mathf.Ceil(contentSize.y + padding.top + padding.bottom))
            );
        }

        ///<summary>How tall this element will be when rendered with <c>content</c> and a specific <c>width</c>.</summary>
        public float CalcHeight(GUIContent content, float width)
        {
            var height = Internal_CalcHeight(content, width);
            return height;
        }

        internal Vector2 GetPreferredSize(string content, Rect rect)
        {
            return IMGUITextHandle.GetTextHandle(this, padding.Remove(rect), content, Color.white).preferredSize;
        }

        ///<exclude />
        public bool isHeightDependantOnWidth => fixedHeight == 0 && (wordWrap && imagePosition != ImagePosition.ImageOnly);

        ///<summary>Calculate the minimum and maximum widths for this style rendered with <c>content</c>.</summary>
        ///<remarks>Used by <see cref="GUILayout" /> to handle word-wrapping elements correctly.</remarks>
        public void CalcMinMaxWidth(GUIContent content, out float minWidth, out float maxWidth)
        {
            Vector2 size = Internal_CalcMinMaxWidth(content);
            minWidth = size.x;
            maxWidth = size.y;
        }

        ///<exclude />
        public override string ToString()
        {
            return string.Format("GUIStyle '{0}'", name);
        }

        [RequiredByNativeCode]
        internal static void GetMeshInfo(GUIStyle style, Color color, string content, Rect rect, ref MeshInfoBindings[] meshInfos, ref Vector2 dimensions, ref int generationId)
        {
            IMGUITextHandle.GetMeshInfo(style, color, content, rect, ref meshInfos, ref dimensions, ref generationId);
        }

        [RequiredByNativeCode]
        internal static void GetDimensions(GUIStyle style, Color color, string content, Rect rect, ref Vector2 dimensions)
        {
            dimensions = style.GetPreferredSize(content, rect);
        }

        [RequiredByNativeCode]
        internal static void GetLineHeight(GUIStyle style, ref float lineHeight)
        {
            lineHeight = style.lineHeight;
        }

        [RequiredByNativeCode]
        internal static void EmptyManagedCache()
        {
            IMGUITextHandle.EmptyManagedCache();
        }
    }


    ///<summary>Different methods for how the GUI system handles text being too large to fit the rectangle allocated.</summary>
    public enum TextClipping
    {
        ///<summary>Text flows freely outside the element.</summary>
        Overflow = 0,
        ///<summary>Text gets clipped to be inside the element.</summary>
        Clip = 1,
        ///<summary>Text gets clipped to be inside the element and added ... at the end.</summary>
        Ellipsis = 2,
    }

    // Helper struct to temporarily enable SDF rendering on GUIStyles with automatic cleanup
    internal struct SDFStyleScope : System.IDisposable
    {
        private GUIStyle[] m_Styles;
        private bool[] m_OriginalValues;

        public SDFStyleScope(params GUIStyle[] styles)
        {
            m_Styles = styles;
            m_OriginalValues = new bool[styles.Length];
            for (int i = 0; i < styles.Length; i++)
            {
                m_OriginalValues[i] = styles[i].isSDF;
                styles[i].isSDF = true;
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < m_Styles.Length; i++)
            {
                m_Styles[i].isSDF = m_OriginalValues[i];
            }
        }
    }
}
