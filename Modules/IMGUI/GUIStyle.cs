// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    // Specialized values for the given states used by [[GUIStyle]] objects.
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public sealed partial class GUIStyleState
    {
        // Pointer to the GUIStyleState INSIDE a GUIStyle.
        [NonSerialized]
        internal IntPtr m_Ptr;

        // Pointer to the source GUIStyle so it doesn't get garbage collected.
        // If NULL, it means we own m_Ptr and need to delete it when this gets disposed
        readonly GUIStyle m_SourceStyle;

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

        ~GUIStyleState()
        {
            if (m_SourceStyle == null)
            {
                Cleanup();
                m_Ptr = IntPtr.Zero;
            }
        }
    }

    // How image and text is placed inside [[GUIStyle]].
    public enum ImagePosition
    {
        // Image is to the left of the text.
        ImageLeft = 0,
        // Image is above the text.
        ImageAbove = 1,
        // Only the image is displayed.
        ImageOnly = 2,
        // Only the text is displayed.
        TextOnly = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public sealed partial class GUIStyle
    {
        // Constructor for empty GUIStyle.
        public GUIStyle()
        {
            m_Ptr = Internal_Create(this);
        }

        // Constructs GUIStyle identical to given other GUIStyle.
        public GUIStyle(GUIStyle other)
        {
            if (other == null)
            {
                Debug.LogError("Copied style is null. Using StyleNotFound instead.");
                other = GUISkin.error;
            }
            m_Ptr = Internal_Copy(this, other);
        }

        ~GUIStyle()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        static internal void CleanupRoots()
        {
            // See GUI.CleanupRoots
            s_None = null;
        }

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

        [NonSerialized]
        internal IntPtr m_Ptr;

        [NonSerialized]
        GUIStyleState m_Normal, m_Hover, m_Active, m_Focused, m_OnNormal, m_OnHover, m_OnActive, m_OnFocused;

        [NonSerialized]
        RectOffset m_Border, m_Padding, m_Margin, m_Overflow;

        [NonSerialized]
        string m_Name;

        // Internal callback used to override how gui styles are rendered.
        internal static DrawHandler onDraw;
        // Cache StyleBlock ID
        internal int blockId;

        public string name
        {
            get { return m_Name ?? (m_Name = rawName); }
            set
            {
                m_Name = value;
                rawName = value;
            }
        }

        // Rendering settings for when the component is displayed normally.
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

        // Rendering settings for when the mouse is hovering over the control
        public GUIStyleState hover
        {
            get { return m_Hover ?? (m_Hover = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(1))); }
            set { AssignStyleState(1, value.m_Ptr); }
        }

        // Rendering settings for when the control is pressed down.
        public GUIStyleState active
        {
            get { return m_Active ?? (m_Active = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(2))); }
            set { AssignStyleState(2, value.m_Ptr); }
        }

        // Rendering settings for when the control is turned on.
        public GUIStyleState onNormal
        {
            get { return m_OnNormal ?? (m_OnNormal = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(4))); }
            set { AssignStyleState(4, value.m_Ptr); }
        }

        // Rendering settings for when the control is turned on and the mouse is hovering it.
        public GUIStyleState onHover
        {
            get { return m_OnHover ?? (m_OnHover = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(5))); }
            set { AssignStyleState(5, value.m_Ptr); }
        }

        // Rendering settings for when the element is turned on and pressed down.
        public GUIStyleState onActive
        {
            get { return m_OnActive ?? (m_OnActive = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(6))); }
            set { AssignStyleState(6, value.m_Ptr); }
        }

        // Rendering settings for when the element has keyboard focus.
        public GUIStyleState focused
        {
            get { return m_Focused ?? (m_Focused = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(3))); }
            set { AssignStyleState(3, value.m_Ptr); }
        }

        // Rendering settings for when the element has keyboard and is turned on.
        public GUIStyleState onFocused
        {
            get { return m_OnFocused ?? (m_OnFocused = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(7))); }
            set { AssignStyleState(7, value.m_Ptr); }
        }

        // The borders of all background images.
        public RectOffset border
        {
            get { return m_Border ?? (m_Border = new RectOffset(this, GetRectOffsetPtr(0))); }
            set { AssignRectOffset(0, value.m_Ptr); }
        }

        // The margins between elements rendered in this style and any other GUI elements
        public RectOffset margin
        {
            get { return m_Margin ?? (m_Margin = new RectOffset(this, GetRectOffsetPtr(1))); }
            set { AssignRectOffset(1, value.m_Ptr); }
        }

        // Space from the edge of [[GUIStyle]] to the start of the contents.
        public RectOffset padding
        {
            get { return m_Padding ?? (m_Padding = new RectOffset(this, GetRectOffsetPtr(2))); }
            set { AssignRectOffset(2, value.m_Ptr); }
        }

        // Extra space to be added to the background image.
        public RectOffset overflow
        {
            get { return m_Overflow ?? (m_Overflow = new RectOffset(this, GetRectOffsetPtr(3))); }
            set { AssignRectOffset(3, value.m_Ptr); }
        }

        // The height of one line of text with this style, measured in pixels.
        public float lineHeight => Mathf.Round(Internal_GetLineHeight(m_Ptr));

        // Draw plain GUIStyle without text nor image.
        public void Draw(Rect position, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            Draw(position, GUIContent.none, -1, isHover, isActive, on, hasKeyboardFocus);
        }

        // Draw the GUIStyle with a text string inside.
        public void Draw(Rect position, string text, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            Draw(position, GUIContent.Temp(text), -1, isHover, isActive, on, hasKeyboardFocus);
        }

        // Draw the GUIStyle with an image inside. If the image is too large to fit within the content area of the style it is scaled down.
        public void Draw(Rect position, Texture image, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            Draw(position, GUIContent.Temp(image), -1, isHover, isActive, on, hasKeyboardFocus);
        }

        // Draw the GUIStyle with text and an image inside. If the image is too large to fit within the content area of the style it is scaled down.
        public void Draw(Rect position, GUIContent content, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            Draw(position, content, -1, isHover, isActive, on, hasKeyboardFocus);
        }

        public void Draw(Rect position, GUIContent content, int controlID)
        {
            Draw(position, content, controlID, false, false, false, false);
        }

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


        // Does the ID-based Draw function show keyboard focus? Disabled by windows when they don't have keyboard focus
        internal static bool showKeyboardFocus = true;

        // Draw this GUIStyle with selected content.
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

                Internal_DrawCursor(position, content, character, cursorColor);
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

            // Figure out the cursor color...
            Color cursorColor = new Color(0, 0, 0, 0);
            float cursorFlashSpeed = GUI.skin.settings.cursorFlashSpeed;
            float cursorFlashRel = (Time.realtimeSinceStartup - Internal_GetCursorFlashOffset()) % cursorFlashSpeed / cursorFlashSpeed;
            if (cursorFlashSpeed == 0 || cursorFlashRel < .5f)
                cursorColor = GUI.skin.settings.cursorColor;

            bool hovered = position.Contains(Event.current.mousePosition);
            var drawStates = new DrawStates(-1, hovered, isActive, false, hasKeyboardFocus,
                drawSelectionAsComposition, firstSelectedCharacter, lastSelectedCharacter, cursorColor, selectionColor);
            if (onDraw == null || !onDraw(this, position, content, drawStates))
            {
                Internal_DrawWithTextSelection(position, content, hovered, isActive, false, hasKeyboardFocus,
                    drawSelectionAsComposition, firstSelectedCharacter, lastSelectedCharacter, cursorColor, selectionColor);
            }
        }

        internal void DrawWithTextSelection(Rect position, GUIContent content, int controlID, int firstSelectedCharacter,
            int lastSelectedCharacter, bool drawSelectionAsComposition)
        {
            DrawWithTextSelection(position, content, controlID == GUIUtility.hotControl,
                controlID == GUIUtility.keyboardControl && showKeyboardFocus,
                firstSelectedCharacter, lastSelectedCharacter, drawSelectionAsComposition, GUI.skin.settings.selectionColor);
        }

        // Draw this GUIStyle with selected content.
        public void DrawWithTextSelection(Rect position, GUIContent content, int controlID, int firstSelectedCharacter, int lastSelectedCharacter)
        {
            DrawWithTextSelection(position, content, controlID, firstSelectedCharacter, lastSelectedCharacter, false);
        }

        // Get a named GUI style from the current skin.
        public static implicit operator GUIStyle(string str)
        {
            if (GUISkin.current == null)
            {
                Debug.LogError("Unable to use a named GUIStyle without a current skin. Most likely you need to move your GUIStyle initialization code to OnGUI");
                return GUISkin.error;
            }
            return GUISkin.current.GetStyle(str);
        }

        // Shortcut for an empty GUIStyle.
        public static GUIStyle none => s_None ?? (s_None = new GUIStyle());
        static GUIStyle s_None;

        // Get the pixel position of a given string index.
        public Vector2 GetCursorPixelPosition(Rect position, GUIContent content, int cursorStringIndex)
        {
            return Internal_GetCursorPixelPosition(position, content, cursorStringIndex);
        }

        // Get the cursor position (indexing into contents.text) when the user clicked at cursorPixelPosition
        public int GetCursorStringIndex(Rect position, GUIContent content, Vector2 cursorPixelPosition)
        {
            return Internal_GetCursorStringIndex(position, content, cursorPixelPosition);
        }

        // Returns number of characters that can fit within width, returns -1 if fails due to missing font
        internal int GetNumCharactersThatFitWithinWidth(string text, float width)
        {
            return Internal_GetNumCharactersThatFitWithinWidth(text, width);
        }

        // Calculate the size of a some content if it is rendered with this style.
        public Vector2 CalcSize(GUIContent content)
        {
            return Internal_CalcSize(content);
        }

        // Calculate the size of a some content if it is rendered with this style.
        internal Vector2 CalcSizeWithConstraints(GUIContent content, Vector2 constraints)
        {
            return Internal_CalcSizeWithConstraints(content, constraints);
        }

        // Calculate the size of an element formatted with this style, and a given space to content.
        public Vector2 CalcScreenSize(Vector2 contentSize)
        {
            return new Vector2(
                (fixedWidth != 0.0f ? fixedWidth : Mathf.Ceil(contentSize.x + padding.left + padding.right)),
                (fixedHeight != 0.0f ? fixedHeight : Mathf.Ceil(contentSize.y + padding.top + padding.bottom))
            );
        }

        // How tall this element will be when rendered with /content/ and a specific /width/.
        public float CalcHeight(GUIContent content, float width)
        {
            return Internal_CalcHeight(content, width);
        }

        public bool isHeightDependantOnWidth => fixedHeight == 0 && (wordWrap && imagePosition != ImagePosition.ImageOnly);

        // Calculate the minimum and maximum widths for this style rendered with /content/.
        public void CalcMinMaxWidth(GUIContent content, out float minWidth, out float maxWidth)
        {
            Vector2 size = Internal_CalcMinMaxWidth(content);
            minWidth = size.x;
            maxWidth = size.y;
        }

        public override string ToString()
        {
            return UnityString.Format("GUIStyle '{0}'", name);
        }
    }


    // Different methods for how the GUI system handles text being too large to fit the rectangle allocated.
    public enum TextClipping
    {
        // Text flows freely outside the element.
        Overflow = 0,
        // Text gets clipped to be inside the element.
        Clip = 1,

        // Text gets truncated with dots to show it is too long
        //  Truncate = 2
    }
}
