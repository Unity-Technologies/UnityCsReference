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
        // If NULL, it means we own m_Ptr and need to delete it when this gets displosed
        readonly GUIStyle m_SourceStyle;

        // Pointer to the texture that is referenced from the GUIStyleState.
        // Necessary for the Asset Garbage Collector to find the texture reference
    #pragma warning disable 414
        [NonSerialized]
        private Texture2D m_Background;
        [System.NonSerialized]
        private Texture2D[] m_ScaledBackgrounds;
    #pragma warning restore 414

        public GUIStyleState()
        {
            Init();
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
            newState.m_Background = newState.GetBackgroundInternalFromDeserialization();
            newState.m_ScaledBackgrounds = newState.GetScaledBackgroundsInternalFromDeserialization();
            return newState;
        }

        internal static GUIStyleState GetGUIStyleState(GUIStyle sourceStyle, IntPtr source)
        {
            GUIStyleState newState = new GUIStyleState(sourceStyle, source);
            newState.m_Background = newState.GetBackgroundInternal();
            newState.m_ScaledBackgrounds = newState.GetScaledBackgroundsInternalFromDeserialization();
            return newState;
        }

        ~GUIStyleState()
        {
            if (m_SourceStyle == null)
                Cleanup();
        }

        public Texture2D background
        {
            get { return GetBackgroundInternal(); }
            set { SetBackgroundInternal(value); m_Background = value; }
        }
        public Texture2D[] scaledBackgrounds
        {
            get { return GetScaledBackgroundsInternal(); }
            set { SetScaledBackgroundsInternal(value); m_ScaledBackgrounds = value; }
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
            Init();
        }

        // Constructs GUIStyle identical to given other GUIStyle.
        public GUIStyle(GUIStyle other)
        {
            InitCopy(other);
        }

        ~GUIStyle()
        {
            Cleanup();
        }

        static internal void CleanupRoots()
        {
            // See GUI.CleanupRoots
            s_None = null;
        }

        //Called during Deserialization from cpp
        internal void InternalOnAfterDeserialize()
        {
            m_FontInternal = GetFontInternalDuringLoadingThread();
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

    #pragma warning disable 414
        [NonSerialized]
        private Font m_FontInternal;
    #pragma warning restore 414

        // Rendering settings for when the component is displayed normally.
        public GUIStyleState normal
        {
            get
            {
                //GUIStyleState can't be initialized in the constructor
                //since constructors can be called within and outside a serialization operation
                //So we delay the initialization here where we know we will be on the main thread, outside
                //any loading operation.
                if (m_Normal == null)
                    m_Normal = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(0));

                return m_Normal;
            }
            set { AssignStyleState(0, value.m_Ptr); }
        }

        // Rendering settings for when the mouse is hovering over the control
        public GUIStyleState hover
        {
            get
            {
                if (m_Hover == null)
                    m_Hover = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(1));

                return m_Hover;
            }
            set { AssignStyleState(1, value.m_Ptr); }
        }

        // Rendering settings for when the control is pressed down.
        public GUIStyleState active
        {
            get
            {
                if (m_Active == null)
                    m_Active = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(2));

                return m_Active;
            }
            set { AssignStyleState(2, value.m_Ptr); }
        }

        // Rendering settings for when the control is turned on.
        public GUIStyleState onNormal
        {
            get
            {
                if (m_OnNormal == null)
                    m_OnNormal = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(4));

                return m_OnNormal;
            }
            set { AssignStyleState(4, value.m_Ptr); }
        }

        // Rendering settings for when the control is turned on and the mouse is hovering it.
        public GUIStyleState onHover
        {
            get
            {
                if (m_OnHover == null)
                    m_OnHover = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(5));

                return m_OnHover;
            }
            set { AssignStyleState(5, value.m_Ptr); }
        }

        // Rendering settings for when the element is turned on and pressed down.
        public GUIStyleState onActive
        {
            get
            {
                if (m_OnActive == null)
                    m_OnActive = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(6));

                return m_OnActive;
            }
            set { AssignStyleState(6, value.m_Ptr); }
        }

        // Rendering settings for when the element has keyboard focus.
        public GUIStyleState focused
        {
            get
            {
                if (m_Focused == null)
                    m_Focused = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(3));

                return m_Focused;
            }
            set { AssignStyleState(3, value.m_Ptr); }
        }

        // Rendering settings for when the element has keyboard and is turned on.
        public GUIStyleState onFocused
        {
            get
            {
                if (m_OnFocused == null)
                    m_OnFocused = GUIStyleState.GetGUIStyleState(this, GetStyleStatePtr(7));

                return m_OnFocused;
            }
            set { AssignStyleState(7, value.m_Ptr); }
        }


        // RECT OFFSETS
        // ================================================================================================================================================


        // The borders of all background images.
        public RectOffset border
        {
            get
            {
                if (m_Border == null)
                    m_Border = new RectOffset(this, GetRectOffsetPtr(0));
                return m_Border;
            }
            set { AssignRectOffset(0, value.m_Ptr); }
        }

        // The margins between elements rendered in this style and any other GUI elements
        public RectOffset margin
        {
            get
            {
                if (m_Margin == null)
                    m_Margin = new RectOffset(this, GetRectOffsetPtr(1));
                return m_Margin;
            }
            set { AssignRectOffset(1, value.m_Ptr); }
        }

        // Space from the edge of [[GUIStyle]] to the start of the contents.
        public RectOffset padding
        {
            get
            {
                if (m_Padding == null)
                    m_Padding = new RectOffset(this, GetRectOffsetPtr(2));
                return m_Padding;
            }
            set { AssignRectOffset(2, value.m_Ptr); }
        }

        // Extra space to be added to the background image.
        public RectOffset overflow
        {
            get
            {
                if (m_Overflow == null)
                    m_Overflow = new RectOffset(this, GetRectOffsetPtr(3));
                return m_Overflow;
            }
            set { AssignRectOffset(3, value.m_Ptr); }
        }

        // *undocumented* Clip offset to apply to the content of this GUIstyle
        [Obsolete("warning Don't use clipOffset - put things inside BeginGroup instead. This functionality will be removed in a later version.")]
        public Vector2 clipOffset { get { return Internal_clipOffset; }   set {Internal_clipOffset = value; } }

        // The font to use for rendering. If null, the default font for the current [[GUISkin]] is used instead.
        public Font font
        {
            get { return GetFontInternal(); }
            set { SetFontInternal(value); m_FontInternal = value; }
        }

        // The height of one line of text with this style, measured in pixels. (RO)
        public float lineHeight { get { return Mathf.Round(Internal_GetLineHeight(m_Ptr)); } }


        // Draw this GUIStyle on to the screen.
        private static void Internal_Draw(IntPtr target, Rect position, GUIContent content, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            Internal_DrawArguments arguments = new Internal_DrawArguments();
            arguments.target = target;
            arguments.position = position;
            arguments.isHover = isHover ? 1 : 0;
            arguments.isActive = isActive ? 1 : 0;
            arguments.on = on ? 1 : 0;
            arguments.hasKeyboardFocus = hasKeyboardFocus ? 1 : 0;
            Internal_Draw(content, ref arguments);
        }

        // Draw plain GUIStyle without text nor image.
        public void Draw(Rect position, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            if (Event.current.type != EventType.Repaint)
            {
                Debug.LogError("Style.Draw may not be called if it is not a repaint event");
                return;
            }
            Internal_Draw(m_Ptr, position, GUIContent.none, isHover, isActive, on, hasKeyboardFocus);
        }

        // Draw the GUIStyle with a text string inside.
        public void Draw(Rect position, string text, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            if (Event.current.type != EventType.Repaint)
            {
                Debug.LogError("Style.Draw may not be called if it is not a repaint event");
                return;
            }
            Internal_Draw(m_Ptr, position, GUIContent.Temp(text), isHover, isActive, on, hasKeyboardFocus);
        }

        // Draw the GUIStyle with an image inside. If the image is too large to fit within the content area of the style it is scaled down.
        public void Draw(Rect position, Texture image, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            if (Event.current.type != EventType.Repaint)
            {
                Debug.LogError("Style.Draw may not be called if it is not a repaint event");
                return;
            }
            Internal_Draw(m_Ptr, position, GUIContent.Temp(image), isHover, isActive, on, hasKeyboardFocus);
        }

        // Draw the GUIStyle with text and an image inside. If the image is too large to fit within the content area of the style it is scaled down.
        public void Draw(Rect position, GUIContent content, bool isHover, bool isActive, bool on, bool hasKeyboardFocus)
        {
            if (Event.current.type != EventType.Repaint)
            {
                Debug.LogError("Style.Draw may not be called if it is not a repaint event");
                return;
            }

            Internal_Draw(m_Ptr, position, content, isHover, isActive, on, hasKeyboardFocus);

        }

        public void Draw(Rect position, GUIContent content, int controlID)
        {
            Draw(position, content, controlID, false);
        }

        public void Draw(Rect position, GUIContent content, int controlID, bool on)
        {
            if (Event.current.type != EventType.Repaint)
            {
                Debug.LogError("Style.Draw may not be called if it is not a repaint event.");
                return;
            }

            if (content != null)
                Internal_Draw2(m_Ptr, position, content, controlID, on);
            else
                Debug.LogError("Style.Draw may not be called with GUIContent that is null.");
        }

        // PrefixLabel has to be drawn with an alternative draw mathod.
        // The normal draw methods use MonoGUIContentToTempNative which means they all share the same temp GUIContent on the native side.
        // A native IMGUI control such as GUIButton is already using this temp GUIContent when it calls GetControlID, which,
        // because of the delayed feature in PrefixLabel, can end up calling a style draw function again to draw the PrefixLabel.
        // This draw call cannot use the same temp GUIContent that is already needed for the GUIButton control itself,
        // so it has to use this alternative code path that uses a different GUIContent to store the content in.
        // We can all agree this workaround is not nice at all. But nobody seemed to be able to come up with something better.
        internal void DrawPrefixLabel(Rect position, GUIContent content, int controlID)
        {
            if (content != null)
                Internal_DrawPrefixLabel(m_Ptr, position, content, controlID, false);
            else
                Debug.LogError("Style.DrawPrefixLabel may not be called with GUIContent that is null.");
        }



        // Does the ID-based Draw function show keyboard focus? Disabled by windows when they don't have keyboard focus
        internal static bool showKeyboardFocus = true;

        // Draw this GUIStyle with selected content.
        public void DrawCursor(Rect position, GUIContent content, int controlID, int Character)
        {
            Event e = Event.current;
            if (e.type == EventType.Repaint)
            {
                // Figure out the cursor color...
                Color cursorColor = new Color(0, 0, 0, 0);
                float cursorFlashSpeed = GUI.skin.settings.cursorFlashSpeed;
                float cursorFlashRel = ((Time.realtimeSinceStartup - Internal_GetCursorFlashOffset()) % cursorFlashSpeed) / cursorFlashSpeed;
                if (cursorFlashSpeed == 0 || cursorFlashRel < .5f)
                {
                    cursorColor = GUI.skin.settings.cursorColor;
                }

                Internal_DrawCursor(m_Ptr, position, content, Character, cursorColor);
            }
        }

        internal void DrawWithTextSelection(Rect position, GUIContent content, bool isActive, bool hasKeyboardFocus,
            int firstSelectedCharacter, int lastSelectedCharacter, bool drawSelectionAsComposition)
        {
            if (Event.current.type != EventType.Repaint)
            {
                Debug.LogError("Style.Draw may not be called if it is not a repaint event");
                return;
            }

            Event e = Event.current;

            // Figure out the cursor color...
            Color cursorColor = new Color(0, 0, 0, 0);
            float cursorFlashSpeed = GUI.skin.settings.cursorFlashSpeed;
            float cursorFlashRel = ((Time.realtimeSinceStartup - Internal_GetCursorFlashOffset()) % cursorFlashSpeed) / cursorFlashSpeed;
            if (cursorFlashSpeed == 0 || cursorFlashRel < .5f)
                cursorColor = GUI.skin.settings.cursorColor;

            Internal_DrawWithTextSelectionArguments arguments = new Internal_DrawWithTextSelectionArguments();
            arguments.target = m_Ptr;
            arguments.position = position;
            arguments.firstPos = firstSelectedCharacter;
            arguments.lastPos = lastSelectedCharacter;
            arguments.cursorColor = cursorColor;
            arguments.selectionColor = GUI.skin.settings.selectionColor;
            arguments.isHover = position.Contains(e.mousePosition) ? 1 : 0;
            arguments.isActive = isActive ? 1 : 0;
            arguments.on = 0;
            arguments.hasKeyboardFocus = hasKeyboardFocus ? 1 : 0;
            arguments.drawSelectionAsComposition = drawSelectionAsComposition ? 1 : 0;

            Internal_DrawWithTextSelection(content, ref arguments);
        }

        internal void DrawWithTextSelection(Rect position, GUIContent content, int controlID, int firstSelectedCharacter,
            int lastSelectedCharacter, bool drawSelectionAsComposition)
        {
            DrawWithTextSelection(position, content, controlID == GUIUtility.hotControl,
                controlID == GUIUtility.keyboardControl && showKeyboardFocus,
                firstSelectedCharacter, lastSelectedCharacter, drawSelectionAsComposition);
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
        public static GUIStyle none { get { if (s_None == null) s_None = new GUIStyle(); return s_None; } }
        static GUIStyle s_None;

        // Get the pixel position of a given string index.
        public Vector2 GetCursorPixelPosition(Rect position, GUIContent content, int cursorStringIndex)
        {
            Vector2 temp;
            Internal_GetCursorPixelPosition(m_Ptr, position, content, cursorStringIndex, out temp);
            return temp;
        }

        // Get the cursor position (indexing into contents.text) when the user clicked at cursorPixelPosition
        public int GetCursorStringIndex(Rect position, GUIContent content, Vector2 cursorPixelPosition)
        {
            return Internal_GetCursorStringIndex(m_Ptr, position, content, cursorPixelPosition);
        }

        // Returns number of characters that can fit within width, returns -1 if fails due to missing font
        internal int GetNumCharactersThatFitWithinWidth(string text, float width)
        {
            return Internal_GetNumCharactersThatFitWithinWidth(m_Ptr, text, width);
        }

        // Calculate the size of a some content if it is rendered with this style.
        public Vector2 CalcSize(GUIContent content)
        {
            Vector2 temp; Internal_CalcSize(m_Ptr, content, out temp);
            return temp;
        }

        // Calculate the size of a some content if it is rendered with this style.
        internal Vector2 CalcSizeWithConstraints(GUIContent content, Vector2 constraints)
        {
            Vector2 temp;
            Internal_CalcSizeWithConstraints(m_Ptr, content, constraints, out temp);
            return temp;
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
            return Internal_CalcHeight(m_Ptr, content, width);
        }

        // *undocumented*
        public bool isHeightDependantOnWidth
        {
            get
            {
                return fixedHeight == 0 && (wordWrap == true && imagePosition != ImagePosition.ImageOnly);
            }
        }

        // Calculate the minimum and maximum widths for this style rendered with /content/.
        public void CalcMinMaxWidth(GUIContent content, out float minWidth, out float maxWidth)
        {
            Internal_CalcMinMaxWidth(m_Ptr, content, out minWidth, out maxWidth);
        }

        // *undocumented
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
