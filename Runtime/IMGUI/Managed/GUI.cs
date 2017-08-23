// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngineInternal;


namespace UnityEngine
{
    public partial class GUI
    {
        private static float s_ScrollStepSize = 10f;
        private static int s_ScrollControlId;

        private static int s_HotTextField = -1;

        private static readonly int s_BoxHash               = "Box".GetHashCode();
        private static readonly int s_RepeatButtonHash      = "repeatButton".GetHashCode();
        private static readonly int s_ToggleHash            = "Toggle".GetHashCode();
        private static readonly int s_SliderHash            = "Slider".GetHashCode();
        private static readonly int s_BeginGroupHash        = "BeginGroup".GetHashCode();
        private static readonly int s_ScrollviewHash        = "scrollView".GetHashCode();
        // *undocumented*

        static GUI()
        {
            nextScrollStepTime = DateTime.Now; // whatever but null
        }

        internal static int scrollTroughSide { get; set; }
        internal static DateTime nextScrollStepTime { get; set; }

        private static GUISkin s_Skin;

        // The global skin to use.
        public static GUISkin skin
        {
            set
            {
                GUIUtility.CheckOnGUI();
                DoSetSkin(value);
            }
            get
            {
                GUIUtility.CheckOnGUI();
                return s_Skin;
            }
        }

        internal static void DoSetSkin(GUISkin newSkin)
        {
            if (!newSkin)
                newSkin = GUIUtility.GetDefaultSkin();
            s_Skin = newSkin;
            newSkin.MakeCurrent();
        }

        static internal void CleanupRoots()
        {
            // Remove references from roots, so GC can collect them.
            // Required for Windows Store Apps when cleaning up everything on application quit.
            //  Otherwise managed roots are being freed after Unity managers are destroyed, in this case when finalizers are invoked for GUI* objects
            //  they're accessing non existent managers.
            s_Skin = null;

            // GUILayoutUtility.CleanupRoots indirectly invokes GUILayoutUtility ctor, which indirectly sets GUIStyle s_None, that's
            // why we need to invoke GUILayoutUtility.CleanupRoots before GUIStyle.CleanupRoots
            //    UnityEngine.DLL!UnityEngine.GUIStyle.Init()   Unknown
            //    UnityEngine.DLL!UnityEngine.GUIStyle.GUIStyle() Line 172  C#
            //> UnityEngine.DLL!UnityEngine.GUIStyle.none.get() Line 590    C#
            //    UnityEngine.DLL!UnityEngine.GUILayoutGroup.GUILayoutGroup() Line 592  C#
            //    UnityEngine.DLL!UnityEngine.GUILayoutUtility.LayoutCache.LayoutCache() Line 19    C#
            //    UnityEngine.DLL!UnityEngine.GUILayoutUtility.GUILayoutUtility() Line 43   C#
            //    [Native to Managed Transition]
            //    UnityEngine.DLL!UnityEngine.GUILayoutUtility.CleanupRoots() Line 50   C#
            GUIUtility.CleanupRoots();
            GUILayoutUtility.CleanupRoots();
            GUISkin.CleanupRoots();
            GUIStyle.CleanupRoots();
        }

        // MATRIX
        // The GUI transform matrix.
        public static Matrix4x4 matrix
        {
            get { return GUIClip.GetMatrix(); }
            set { GUIClip.SetMatrix(value); }
        }

        // The tooltip of the control the mouse is currently over, or which has keyboard focus. (RO).
        public static string tooltip
        {
            get
            {
                string str = Internal_GetTooltip();
                if (str != null)
                    return str;
                return "";
            }
            set { Internal_SetTooltip(value); }
        }


        // *undocumented*
        protected static string mouseTooltip
        {
            get { return Internal_GetMouseTooltip(); }
        }

        // *undocumented*
        protected static Rect tooltipRect
        {
            get { return s_ToolTipRect; }
            set { s_ToolTipRect = value; }
        }

        internal static Rect s_ToolTipRect;

        /// *listonly*
        public static void Label(Rect position, string text)
        {
            Label(position, GUIContent.Temp(text), s_Skin.label);
        }

        /// *listonly*
        public static void Label(Rect position, Texture image)
        {
            Label(position, GUIContent.Temp(image), s_Skin.label);
        }

        /// *listonly*
        public static void Label(Rect position, GUIContent content)
        {
            Label(position, content, s_Skin.label);
        }

        /// *listonly*
        public static void Label(Rect position, string text, GUIStyle style)
        {
            Label(position, GUIContent.Temp(text), style);
        }

        /// *listonly*
        public static void Label(Rect position, Texture image, GUIStyle style)
        {
            Label(position, GUIContent.Temp(image), style);
        }

        // Make a text or texture label on screen.
        public static void Label(Rect position, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            DoLabel(position, content, style.m_Ptr);
        }

        // Draw a texture within a rectangle.
        public static void DrawTexture(Rect position, Texture image)
        {
            DrawTexture(position, image, ScaleMode.StretchToFill);
        }

        // Draw a texture within a rectangle.
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode)
        {
            DrawTexture(position, image, scaleMode, true);
        }

        // Draw a texture within a rectangle.
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode , bool alphaBlend)
        {
            DrawTexture(position, image, scaleMode, alphaBlend, 0);
        }

        // Draw a texture within a rectangle.
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect)
        {
            DrawTexture(position, image, scaleMode, alphaBlend, imageAspect, GUI.color, 0, 0);
        }

        // Draw a texture within a rectangle.
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect, Color color, float borderWidth, float borderRadius)
        {
            var borderWidths = Vector4.one * borderWidth;
            DrawTexture(position, image, scaleMode, alphaBlend, imageAspect, color, borderWidths, borderRadius);
        }

        // Draw a texture within a rectangle.
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect, Color color, Vector4 borderWidths, float borderRadius)
        {
            var borderRadiuses = Vector4.one * borderRadius;
            DrawTexture(position, image, scaleMode, alphaBlend, imageAspect, color, borderWidths, borderRadiuses);
        }

        // Draw a texture within a rectangle.
        public static void DrawTexture(Rect position, Texture image, ScaleMode scaleMode, bool alphaBlend, float imageAspect, Color color, Vector4 borderWidths, Vector4 borderRadiuses)
        {
            GUIUtility.CheckOnGUI();
            if (Event.current.type == EventType.Repaint)
            {
                if (image == null)
                {
                    Debug.LogWarning("null texture passed to GUI.DrawTexture");
                    return;
                }

                if (imageAspect == 0)
                    imageAspect = (float)image.width / image.height;

                Material mat = null;
                if (borderWidths != Vector4.zero || borderRadiuses != Vector4.zero)
                {
                    mat = roundedRectMaterial;
                }
                else
                {
                    mat = alphaBlend ? blendMaterial : blitMaterial;
                }

                Internal_DrawTextureArguments arguments = new Internal_DrawTextureArguments();
                arguments.leftBorder = 0; arguments.rightBorder = 0; arguments.topBorder = 0; arguments.bottomBorder = 0;
                arguments.color = color;
                arguments.borderWidths = borderWidths;
                arguments.cornerRadiuses = borderRadiuses;
                arguments.texture = image;
                arguments.mat = mat;
                CalculateScaledTextureRects(position, scaleMode, imageAspect, ref arguments.screenRect, ref arguments.sourceRect);
                Graphics.Internal_DrawTexture(ref arguments);
            }
        }

        // Calculate screenrect and sourcerect for different scalemodes
        internal static bool CalculateScaledTextureRects(Rect position, ScaleMode scaleMode, float imageAspect, ref Rect outScreenRect, ref Rect outSourceRect)
        {
            float destAspect = position.width / position.height;
            bool ret = false;

            switch (scaleMode)
            {
                case ScaleMode.StretchToFill:
                    outScreenRect = position;
                    outSourceRect = new Rect(0, 0, 1, 1);
                    ret = true;
                    break;
                case ScaleMode.ScaleAndCrop:
                    if (destAspect > imageAspect)
                    {
                        float stretch = imageAspect / destAspect;
                        outScreenRect = position;
                        outSourceRect = new Rect(0, (1 - stretch) * .5f, 1, stretch);
                        ret = true;
                    }
                    else
                    {
                        float stretch = destAspect / imageAspect;
                        outScreenRect = position;
                        outSourceRect = new Rect(.5f - stretch * .5f, 0, stretch, 1);
                        ret = true;
                    }
                    break;
                case ScaleMode.ScaleToFit:
                    if (destAspect > imageAspect)
                    {
                        float stretch = imageAspect / destAspect;
                        outScreenRect = new Rect(position.xMin + position.width * (1.0f - stretch) * .5f, position.yMin, stretch * position.width, position.height);
                        outSourceRect = new Rect(0, 0, 1, 1);
                        ret = true;
                    }
                    else
                    {
                        float stretch = destAspect / imageAspect;
                        outScreenRect = new Rect(position.xMin, position.yMin + position.height * (1.0f - stretch) * .5f, position.width, stretch * position.height);
                        outSourceRect = new Rect(0, 0, 1, 1);
                        ret = true;
                    }
                    break;
            }

            return ret;
        }

        // Draw a texture within a rectangle with the given texture coordinates. Use this function for clipping or tiling the image within the given rectangle.
        public static void DrawTextureWithTexCoords(Rect position, Texture image, Rect texCoords)
        {
            DrawTextureWithTexCoords(position, image, texCoords, true);
        }

        // Draw a texture within a rectangle with the given texture coordinates. Use this function for clipping or tiling the image within the given rectangle.
        public static void DrawTextureWithTexCoords(Rect position, Texture image, Rect texCoords, bool alphaBlend)
        {
            GUIUtility.CheckOnGUI();

            if (Event.current.type == EventType.Repaint)
            {
                Material mat = alphaBlend ? blendMaterial : blitMaterial;

                Internal_DrawTextureArguments arguments = new Internal_DrawTextureArguments();
                arguments.texture = image;
                arguments.mat = mat;
                arguments.leftBorder = 0; arguments.rightBorder = 0; arguments.topBorder = 0; arguments.bottomBorder = 0;
                arguments.color = GUI.color;
                arguments.screenRect = position;
                arguments.sourceRect = texCoords;
                Graphics.Internal_DrawTexture(ref arguments);
            }
        }

        /// *listonly*
        public static void Box(Rect position, string text)
        {
            Box(position, GUIContent.Temp(text), s_Skin.box);
        }

        /// *listonly*
        public static void Box(Rect position, Texture image)
        {
            Box(position, GUIContent.Temp(image), s_Skin.box);
        }

        /// *listonly*
        public static void Box(Rect position, GUIContent content)
        {
            Box(position, content, s_Skin.box);
        }

        /// *listonly*
        public static void Box(Rect position, string text, GUIStyle style)
        {
            Box(position, GUIContent.Temp(text), style);
        }

        /// *listonly*
        public static void Box(Rect position, Texture image, GUIStyle style)
        {
            Box(position, GUIContent.Temp(image), style);
        }

        // Make a graphical box.
        public static void Box(Rect position, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            int id = GUIUtility.GetControlID(s_BoxHash, FocusType.Passive);
            if (Event.current.type == EventType.Repaint)
            {
                style.Draw(position, content, id);
            }
        }

        /// *listonly*
        public static bool Button(Rect position, string text)
        {
            return Button(position, GUIContent.Temp(text), s_Skin.button);
        }

        /// *listonly*
        public static bool Button(Rect position, Texture image)
        {
            return Button(position, GUIContent.Temp(image), s_Skin.button);
        }

        /// *listonly*
        public static bool Button(Rect position, GUIContent content)
        {
            return Button(position, content, s_Skin.button);
        }

        /// *listonly*
        public static bool Button(Rect position, string text, GUIStyle style)
        {
            return Button(position, GUIContent.Temp(text), style);
        }

        /// *listonly*
        public static bool Button(Rect position, Texture image, GUIStyle style)
        {
            return Button(position, GUIContent.Temp(image), style);
        }

        // Make a single press button. The user clicks them and something happens immediately.
        public static bool Button(Rect position, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoButton(position, content, style.m_Ptr);
        }

        /// *listonly*
        public static bool RepeatButton(Rect position, string text)
        {
            return DoRepeatButton(position, GUIContent.Temp(text), s_Skin.button, FocusType.Passive);
        }

        /// *listonly*
        public static bool RepeatButton(Rect position, Texture image)
        {
            return DoRepeatButton(position, GUIContent.Temp(image), s_Skin.button, FocusType.Passive);
        }

        /// *listonly*
        public static bool RepeatButton(Rect position, GUIContent content)
        {
            return DoRepeatButton(position, content, s_Skin.button, FocusType.Passive);
        }

        /// *listonly*
        public static bool RepeatButton(Rect position, string text, GUIStyle style)
        {
            return DoRepeatButton(position, GUIContent.Temp(text), style, FocusType.Passive);
        }

        /// *listonly*
        public static bool RepeatButton(Rect position, Texture image, GUIStyle style)
        {
            return DoRepeatButton(position, GUIContent.Temp(image), style, FocusType.Passive);
        }

        // Make a button that is active as long as the user holds it down.
        public static bool RepeatButton(Rect position, GUIContent content, GUIStyle style)
        {
            return DoRepeatButton(position, content, style, FocusType.Passive);
        }

        private static bool DoRepeatButton(Rect position, GUIContent content, GUIStyle style, FocusType focusType)
        {
            GUIUtility.CheckOnGUI();
            int id = GUIUtility.GetControlID(s_RepeatButtonHash, focusType, position);
            switch (Event.current.GetTypeForControl(id))
            {
                case EventType.MouseDown:
                    // If the mouse is inside the button, we say that we're the hot control
                    if (position.Contains(Event.current.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        Event.current.Use();
                    }
                    return false;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;

                        // If we got the mousedown, the mouseup is ours as well
                        // (no matter if the click was in the button or not)
                        Event.current.Use();

                        // But we only return true if the button was actually clicked
                        return position.Contains(Event.current.mousePosition);
                    }
                    return false;
                case EventType.Repaint:
                    style.Draw(position, content, id);
                    return id == GUIUtility.hotControl && position.Contains(Event.current.mousePosition);
            }
            return false;
        }

        /// *listonly*
        public static string TextField(Rect position, string text)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, false, -1, GUI.skin.textField);
            return t.text;
        }

        /// *listonly*
        public static string TextField(Rect position, string text, int maxLength)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, false, maxLength, GUI.skin.textField);
            return t.text;
        }

        /// *listonly*
        public static string TextField(Rect position, string text, GUIStyle style)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, false, -1, style);
            return t.text;
        }

        // Make a single-line text field where the user can edit a string.
        public static string TextField(Rect position, string text, int maxLength, GUIStyle style)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, false, maxLength, style);
            return t.text;
        }

        /// *listonly*
        public static string PasswordField(Rect position, string password, char maskChar)
        {
            return PasswordField(position, password, maskChar, -1, GUI.skin.textField);
        }

        /// *listonly*
        public static string PasswordField(Rect position, string password, char maskChar, int maxLength)
        {
            return PasswordField(position, password, maskChar, maxLength, GUI.skin.textField);
        }

        /// *listonly*
        public static string PasswordField(Rect position, string password, char maskChar, GUIStyle style)
        {
            return PasswordField(position, password, maskChar, -1, style);
        }

        // Make a text field where the user can enter a password.
        public static string PasswordField(Rect position, string password, char maskChar, int maxLength, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();

            string strPassword = PasswordFieldGetStrToShow(password, maskChar);
            GUIContent t = GUIContent.Temp(strPassword);

            bool oldGUIChanged = GUI.changed;
            GUI.changed = false;

            if (TouchScreenKeyboard.isSupported)
                DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard), t, false, maxLength, style, password, maskChar);
            else
                DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, false, maxLength, style);

            strPassword = GUI.changed ? t.text : password;

            GUI.changed |= oldGUIChanged;

            return strPassword;
        }

        // *undocumented*
        internal static string PasswordFieldGetStrToShow(string password, char maskChar)
        {
            return (Event.current.type == EventType.Repaint || Event.current.type == EventType.MouseDown) ?
                "".PadRight(password.Length, maskChar) : password;
        }

        /// *listonly*
        public static string TextArea(Rect position, string text)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, true, -1, GUI.skin.textArea);
            return t.text;
        }

        /// *listonly*
        public static string TextArea(Rect position, string text, int maxLength)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, true, maxLength, GUI.skin.textArea);
            return t.text;
        }

        /// *listonly*
        public static string TextArea(Rect position, string text, GUIStyle style)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, true, -1, style);
            return t.text;
        }

        // Make a Multi-line text area where the user can edit a string.
        public static string TextArea(Rect position, string text, int maxLength, GUIStyle style)
        {
            GUIContent t = GUIContent.Temp(text);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, false, maxLength, style);
            return t.text;
        }

        // LATER...
        private static string TextArea(Rect position, GUIContent content, int maxLength, GUIStyle style)
        {
            GUIContent t = GUIContent.Temp(content.text, content.image);
            DoTextField(position, GUIUtility.GetControlID(FocusType.Keyboard, position), t, false, maxLength, style);
            return t.text;
        }

        internal static void DoTextField(Rect position, int id, GUIContent content, bool multiline, int maxLength, GUIStyle style)
        {
            DoTextField(position, id, content, multiline, maxLength, style, null);
        }

        internal static void DoTextField(Rect position, int id, GUIContent content, bool multiline, int maxLength, GUIStyle style, string secureText)
        {
            DoTextField(position, id, content, multiline, maxLength, style, secureText, '\0');
        }

        internal static void DoTextField(Rect position, int id, GUIContent content, bool multiline, int maxLength, GUIStyle style, string secureText, char maskChar)
        {
            GUIUtility.CheckOnGUI();

            //Pre-cull input string to maxLength.
            if (maxLength >= 0 && content.text.Length > maxLength)
                content.text = content.text.Substring(0, maxLength);


            TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), id);
            editor.text = content.text;
            editor.SaveBackup();
            editor.position = position;
            editor.style = style;
            editor.multiline = multiline;
            editor.controlID = id;
            editor.DetectFocusChange();

            if (TouchScreenKeyboard.isSupported)
            {
                HandleTextFieldEventForTouchscreen(position, id, content, multiline, maxLength, style, secureText, maskChar, editor);
            }
            else // Not supported means we have a physical keyboard attached
            {
                HandleTextFieldEventForDesktop(position, id, content, multiline, maxLength, style, editor);
            }

            // Scroll offset might need to be updated
            editor.UpdateScrollOffsetIfNeeded(Event.current);
        }

        private static void HandleTextFieldEventForTouchscreen(Rect position, int id, GUIContent content, bool multiline, int maxLength,
            GUIStyle style, string secureText, char maskChar, TextEditor editor)
        {
            var evt = Event.current;

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = id;

                        // Disable keyboard for previously active text field, if any
                        if (s_HotTextField != -1 && s_HotTextField != id)
                        {
                            TextEditor currentEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), s_HotTextField);
                            currentEditor.keyboardOnScreen = null;
                        }

                        s_HotTextField = id;

                        // in player setting keyboard control calls OnFocus every time, don't want that. In editor it does not do that for some reason
                        if (GUIUtility.keyboardControl != id)
                            GUIUtility.keyboardControl = id;

                        editor.keyboardOnScreen = TouchScreenKeyboard.Open(
                                (secureText != null) ? secureText : content.text,
                                TouchScreenKeyboardType.Default,
                                true, // autocorrection
                                multiline,
                                (secureText != null));

                        evt.Use();
                    }
                    break;
                case EventType.Repaint:
                    if (editor.keyboardOnScreen != null)
                    {
                        content.text = editor.keyboardOnScreen.text;
                        if (maxLength >= 0 && content.text.Length > maxLength)
                            content.text = content.text.Substring(0, maxLength);

                        if (editor.keyboardOnScreen.done)
                        {
                            editor.keyboardOnScreen = null;
                            changed = true;
                        }
                    }

                    // if we use system keyboard we will have normal text returned (hiding symbols is done inside os)
                    // so before drawing make sure we hide them ourselves
                    string clearText = content.text;

                    if (secureText != null)
                        content.text = PasswordFieldGetStrToShow(clearText, maskChar);

                    style.Draw(position, content, id, false);
                    content.text = clearText;

                    break;
            }
        }

        private static void HandleTextFieldEventForDesktop(Rect position, int id, GUIContent content, bool multiline, int maxLength, GUIStyle style, TextEditor editor)
        {
            var evt = Event.current;

            bool change = false;
            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (position.Contains(evt.mousePosition))
                    {
                        GUIUtility.hotControl = id;
                        GUIUtility.keyboardControl = id;
                        editor.m_HasFocus = true;
                        editor.MoveCursorToPosition(Event.current.mousePosition);
                        if (Event.current.clickCount == 2 && GUI.skin.settings.doubleClickSelectsWord)
                        {
                            editor.SelectCurrentWord();
                            editor.DblClickSnap(TextEditor.DblClickSnapping.WORDS);
                            editor.MouseDragSelectsWholeWords(true);
                        }
                        if (Event.current.clickCount == 3 && GUI.skin.settings.tripleClickSelectsLine)
                        {
                            editor.SelectCurrentParagraph();
                            editor.MouseDragSelectsWholeWords(true);
                            editor.DblClickSnap(TextEditor.DblClickSnapping.PARAGRAPHS);
                        }
                        evt.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        if (evt.shift)
                            editor.MoveCursorToPosition(Event.current.mousePosition);
                        else
                            editor.SelectToPosition(Event.current.mousePosition);
                        evt.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        editor.MouseDragSelectsWholeWords(false);
                        GUIUtility.hotControl = 0;
                        evt.Use();
                    }
                    break;
                case EventType.KeyDown:
                    if (GUIUtility.keyboardControl != id)
                        return;

                    if (editor.HandleKeyEvent(evt))
                    {
                        evt.Use();
                        change = true;
                        content.text = editor.text;
                        break;
                    }

                    // Ignore tab & shift-tab in textfields
                    if (evt.keyCode == KeyCode.Tab || evt.character == '\t')
                        return;

                    char c = evt.character;

                    if (c == '\n' && !multiline && !evt.alt)
                        return;


                    // Simplest test: only allow the character if the display font supports it.
                    Font font = style.font;
                    if (!font)
                        font = GUI.skin.font;

                    if (font.HasCharacter(c) || c == '\n')
                    {
                        editor.Insert(c);
                        change = true;
                        break;
                    }

                    // On windows, keypresses also send events with keycode but no character. Eat them up here.
                    if (c == 0)
                    {
                        // if we have a composition string, make sure we clear the previous selection.
                        if (Input.compositionString.Length > 0)
                        {
                            editor.ReplaceSelection("");
                            change = true;
                        }

                        evt.Use();
                    }
                    //              else {
                    // REALLY USEFUL:
                    //              Debug.Log ("unhandled " +evt);
                    //              evt.Use ();
                    //          }
                    break;
                case EventType.Repaint:
                    // If we have keyboard focus, draw the cursor
                    // TODO:    check if this OpenGL view has keyboard focus
                    if (GUIUtility.keyboardControl != id)
                    {
                        style.Draw(position, content, id, false);
                    }
                    else
                    {
                        editor.DrawCursor(content.text);
                    }
                    break;
            }

            if (GUIUtility.keyboardControl == id)
                GUIUtility.textFieldInput = true;

            if (change)
            {
                changed = true;
                content.text = editor.text;
                if (maxLength >= 0 && content.text.Length > maxLength)
                    content.text = content.text.Substring(0, maxLength);
                evt.Use();
            }
        }

        /// *listonly*
        public static bool Toggle(Rect position, bool value, string text)
        {
            return Toggle(position, value, GUIContent.Temp(text), s_Skin.toggle);
        }

        /// *listonly*
        public static bool Toggle(Rect position, bool value, Texture image)
        {
            return Toggle(position, value, GUIContent.Temp(image), s_Skin.toggle);
        }

        /// *listonly*
        public static bool Toggle(Rect position, bool value, GUIContent content)
        {
            return Toggle(position, value, content, s_Skin.toggle);
        }

        /// *listonly*
        public static bool Toggle(Rect position, bool value, string text, GUIStyle style)
        {
            return Toggle(position, value, GUIContent.Temp(text), style);
        }

        /// *listonly*
        public static bool Toggle(Rect position, bool value, Texture image, GUIStyle style)
        {
            return Toggle(position, value, GUIContent.Temp(image), style);
        }

        // Make an on/off toggle button.
        public static bool Toggle(Rect position, bool value, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoToggle(position, GUIUtility.GetControlID(s_ToggleHash, FocusType.Passive, position), value, content, style.m_Ptr);
        }

        // TODO: Passing in ID is bad. We should demote this.
        /// *listonly*
        public static bool Toggle(Rect position, int id, bool value, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoToggle(position, id, value, content, style.m_Ptr);
        }

        public enum ToolbarButtonSize
        {
            Fixed,
            FitToContents
        };

        /// *listonly*
        public static int Toolbar(Rect position, int selected, string[] texts)
        {
            return Toolbar(position, selected, GUIContent.Temp(texts), s_Skin.button);
        }

        /// *listonly*
        public static int Toolbar(Rect position, int selected, Texture[] images)
        {
            return Toolbar(position, selected, GUIContent.Temp(images), s_Skin.button);
        }

        /// *listonly*
        public static int Toolbar(Rect position, int selected, GUIContent[] contents)
        {
            return Toolbar(position, selected, contents, s_Skin.button);
        }

        /// *listonly*
        public static int Toolbar(Rect position, int selected, string[] texts, GUIStyle style)
        {
            return Toolbar(position, selected, GUIContent.Temp(texts), style);
        }

        /// *listonly*
        public static int Toolbar(Rect position, int selected, Texture[] images, GUIStyle style)
        {
            return Toolbar(position, selected, GUIContent.Temp(images), style);
        }

        /// *listonly*
        public static int Toolbar(Rect position, int selected, GUIContent[] contents, GUIStyle style)
        {
            return Toolbar(position, selected, contents, null, style, ToolbarButtonSize.Fixed);
        }

        // Make a toolbar
        public static int Toolbar(Rect position, int selected, GUIContent[] contents, GUIStyle style, ToolbarButtonSize buttonSize)
        {
            return Toolbar(position, selected, contents, null, style, buttonSize);
        }

        internal static int Toolbar(Rect position, int selected, GUIContent[] contents, string[] controlNames, GUIStyle style, ToolbarButtonSize buttonSize)
        {
            GUIUtility.CheckOnGUI();

            // Get the styles here
            GUIStyle firstStyle, midStyle, lastStyle;
            FindStyles(ref style, out firstStyle, out midStyle, out lastStyle, "left", "mid", "right");

            return DoButtonGrid(position, selected, contents, controlNames, contents.Length, style, firstStyle, midStyle, lastStyle, buttonSize);
        }

        /// *listonly*
        public static int SelectionGrid(Rect position, int selected, string[] texts, int xCount)
        {
            return SelectionGrid(position, selected, GUIContent.Temp(texts), xCount, null);
        }

        /// *listonly*
        public static int SelectionGrid(Rect position, int selected, Texture[] images, int xCount)
        {
            return SelectionGrid(position, selected, GUIContent.Temp(images), xCount, null);
        }

        /// *listonly*
        public static int SelectionGrid(Rect position, int selected, GUIContent[] content, int xCount)
        {
            return SelectionGrid(position, selected, content, xCount, null);
        }

        /// *listonly*
        public static int SelectionGrid(Rect position, int selected, string[] texts, int xCount, GUIStyle style)
        {
            return SelectionGrid(position, selected, GUIContent.Temp(texts), xCount, style);
        }

        /// *listonly*
        public static int SelectionGrid(Rect position, int selected, Texture[] images, int xCount, GUIStyle style)
        {
            return SelectionGrid(position, selected, GUIContent.Temp(images), xCount, style);
        }

        // Make a grid of buttons.
        public static int SelectionGrid(Rect position, int selected, GUIContent[] contents, int xCount, GUIStyle style)
        {
            if (style == null) style = s_Skin.button;
            return DoButtonGrid(position, selected, contents, null, xCount, style, style, style, style, ToolbarButtonSize.Fixed);
        }

        // Find many GUIStyles from style.name permutations (Helper function for toolbars).
        internal static void FindStyles(ref GUIStyle style, out GUIStyle firstStyle, out GUIStyle midStyle, out GUIStyle lastStyle, string first, string mid, string last)
        {
            if (style == null)
                style = GUI.skin.button;
            string baseName = style.name;
            midStyle = GUI.skin.FindStyle(baseName + mid);
            if (midStyle == null)
                midStyle = style;
            firstStyle = GUI.skin.FindStyle(baseName + first);
            if (firstStyle == null)
                firstStyle = midStyle;
            lastStyle = GUI.skin.FindStyle(baseName + last);
            if (lastStyle == null)
                lastStyle = midStyle;
        }

        internal static int CalcTotalHorizSpacing(int xCount, GUIStyle style, GUIStyle firstStyle, GUIStyle midStyle, GUIStyle lastStyle)
        {
            if (xCount < 2)
                return 0;
            if (xCount == 2)
                return Mathf.Max(firstStyle.margin.right, lastStyle.margin.left);

            int internalSpace = Mathf.Max(midStyle.margin.left, midStyle.margin.right);
            return Mathf.Max(firstStyle.margin.right, midStyle.margin.left) + Mathf.Max(midStyle.margin.right, lastStyle.margin.left) + internalSpace * (xCount - 3);
        }

        // Make a button grid
        private static int DoButtonGrid(Rect position, int selected, GUIContent[] contents, string[] controlNames, int xCount, GUIStyle style, GUIStyle firstStyle, GUIStyle midStyle, GUIStyle lastStyle, ToolbarButtonSize buttonSize)
        {
            GUIUtility.CheckOnGUI();
            int count = contents.Length;
            if (count == 0)
                return selected;
            if (xCount <= 0)
            {
                Debug.LogWarning("You are trying to create a SelectionGrid with zero or less elements to be displayed in the horizontal direction. Set xCount to a positive value.");
                return selected;
            }

            // Figure out how large each element should be
            int rows = count / xCount;
            if (count % xCount != 0)
                rows++;
            float totalHorizSpacing = CalcTotalHorizSpacing(xCount, style, firstStyle, midStyle, lastStyle);
            float totalVerticalSpacing = Mathf.Max(style.margin.top, style.margin.bottom) * (rows - 1);
            float elemWidth = (position.width - totalHorizSpacing) / xCount;
            float elemHeight = (position.height - totalVerticalSpacing) / (float)rows;

            if (style.fixedWidth != 0)
                elemWidth = style.fixedWidth;
            if (style.fixedHeight != 0)
                elemHeight = style.fixedHeight;

            Rect[] buttonRects = CalcMouseRects(position, contents, xCount, elemWidth, elemHeight, style, firstStyle, midStyle, lastStyle, false, buttonSize);
            GUIStyle selectedButtonStyle = null;
            int selectedButtonID = 0;
            for (int buttonIndex = 0; buttonIndex < count; ++buttonIndex)
            {
                var buttonRect = buttonRects[buttonIndex];
                var content = contents[buttonIndex];

                if (controlNames != null)
                    GUI.SetNextControlName(controlNames[buttonIndex]);
                var id = GUIUtility.GetControlID(content, FocusType.Passive, buttonRect);
                if (buttonIndex == selected)
                    selectedButtonID = id;

                switch (Event.current.GetTypeForControl(id))
                {
                    case EventType.MouseDown:
                        if (buttonRect.Contains(Event.current.mousePosition))
                        {
                            GUIUtility.hotControl = id;
                            Event.current.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        if (GUIUtility.hotControl == id)
                            Event.current.Use();
                        break;
                    case EventType.MouseUp:
                        if (GUIUtility.hotControl == id)
                        {
                            GUIUtility.hotControl = 0;
                            Event.current.Use();

                            GUI.changed = true;
                            return buttonIndex;
                        }
                        break;
                    case EventType.Repaint:
                        var buttonStyle = count == 1 ? style : (buttonIndex == 0 ? firstStyle : (buttonIndex == count - 1 ? lastStyle : midStyle));
                        var isMouseOver = buttonRect.Contains(Event.current.mousePosition);
                        var isHotControl = GUIUtility.hotControl == id;
                        var isSelected = selected == buttonIndex;

                        if (!isSelected)
                            buttonStyle.Draw(buttonRect, content, isMouseOver && (enabled || isHotControl) && (isHotControl || GUIUtility.hotControl == 0), enabled && isHotControl, false, false);
                        else
                            selectedButtonStyle = buttonStyle;

                        if (isMouseOver)
                        {
                            GUIUtility.mouseUsed = true;
                            if (!string.IsNullOrEmpty(content.tooltip))
                                GUIStyle.SetMouseTooltip(content.tooltip, buttonRect);
                        }
                        break;
                }
            }

            // draw selected button at the end so it overflows nicer
            if (selectedButtonStyle != null)
            {
                var buttonRect = buttonRects[selected];
                var content = contents[selected];
                var isMouseOver = buttonRect.Contains(Event.current.mousePosition);
                var isHotControl = GUIUtility.hotControl == selectedButtonID;
                selectedButtonStyle.Draw(buttonRect, content, isMouseOver && (enabled || isHotControl) && (isHotControl || GUIUtility.hotControl == 0), enabled && isHotControl, true, false);
            }

            return selected;
        }

        // Helper function: Get all mouse rects
        private static Rect[] CalcMouseRects(Rect position, GUIContent[] contents, int xCount, float elemWidth, float elemHeight, GUIStyle style, GUIStyle firstStyle, GUIStyle midStyle, GUIStyle lastStyle, bool addBorders, ToolbarButtonSize buttonSize)
        {
            int count = contents.Length;
            int y = 0;
            int x = 0;
            float xPos = position.xMin, yPos = position.yMin;
            GUIStyle currentStyle = style;
            Rect[] retval = new Rect[count];
            if (count > 1)
                currentStyle = firstStyle;
            for (int i = 0; i < count; i++)
            {
                float w = 0;
                switch (buttonSize)
                {
                    case ToolbarButtonSize.Fixed:
                        w = elemWidth;
                        break;
                    case ToolbarButtonSize.FitToContents:
                        w = currentStyle.CalcSize(contents[i]).x;
                        break;
                }

                if (!addBorders)
                    retval[i] = new Rect(xPos, yPos, w, elemHeight);
                else
                    retval[i] = currentStyle.margin.Add(new Rect(xPos, yPos, w, elemHeight));

                // Correct way to get the rounded width:
                retval[i].width = Mathf.Round(retval[i].xMax) - Mathf.Round(retval[i].x);
                // Round the position *after* the width has been rounded:
                retval[i].x = Mathf.Round(retval[i].x);

                // Don't round xPos here. If rounded, the right edge of this rect may
                // not line up correctly with the left edge of the next,
                // plus it can cause cumulative rounding errors.
                // (See case 366967)

                GUIStyle nextStyle = midStyle;
                if (i == count - 2 || i == xCount - 2)
                    nextStyle = lastStyle;

                xPos += w + Mathf.Max(currentStyle.margin.right, nextStyle.margin.left);

                x++;
                if (x >= xCount)
                {
                    y++;
                    x = 0;
                    yPos += elemHeight + Mathf.Max(style.margin.top, style.margin.bottom);
                    xPos = position.xMin;
                    nextStyle = firstStyle;
                }

                currentStyle = nextStyle;
            }
            return retval;
        }

        /// *listonly*
        public static float HorizontalSlider(Rect position, float value, float leftValue, float rightValue)
        {
            return Slider(position, value, 0, leftValue, rightValue, skin.horizontalSlider, skin.horizontalSliderThumb, true, 0);
        }

        // A horizontal slider the user can drag to change a value between a min and a max.
        public static float HorizontalSlider(Rect position, float value, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb)
        {
            return Slider(position, value, 0, leftValue, rightValue, slider, thumb, true, 0);
        }

        /// *listonly*
        public static float VerticalSlider(Rect position, float value, float topValue, float bottomValue)
        {
            return Slider(position, value, 0, topValue, bottomValue, skin.verticalSlider, skin.verticalSliderThumb, false, 0);
        }

        // A vertical slider the user can drag to change a value between a min and a max.
        public static float VerticalSlider(Rect position, float value, float topValue, float bottomValue, GUIStyle slider, GUIStyle thumb)
        {
            return Slider(position, value, 0, topValue, bottomValue, slider, thumb, false, 0);
        }

        // Main slider function.
        // Handles scrollbars & sliders in both horizontal & vertical directions.
        //*undocumented*
        public static float Slider(Rect position, float value, float size, float start, float end, GUIStyle slider, GUIStyle thumb, bool horiz, int id)
        {
            GUIUtility.CheckOnGUI();
            if (id == 0)
            {
                id = GUIUtility.GetControlID(s_SliderHash, FocusType.Passive, position);
            }
            return new SliderHandler(position, value, size, start, end, slider, thumb, horiz, id).Handle();
        }

        /// *listonly*
        public static float HorizontalScrollbar(Rect position, float value, float size, float leftValue, float rightValue)
        {
            return Scroller(position, value, size, leftValue, rightValue, skin.horizontalScrollbar, skin.horizontalScrollbarThumb, skin.horizontalScrollbarLeftButton, skin.horizontalScrollbarRightButton, true);
        }

        // Make a horizontal scrollbar. Scrollbars are what you use to scroll through a document. Most likely, you want to use scrollViews instead.
        public static float HorizontalScrollbar(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle style)
        {
            return Scroller(position, value, size, leftValue, rightValue, style, skin.GetStyle(style.name + "thumb"), skin.GetStyle(style.name + "leftbutton"), skin.GetStyle(style.name + "rightbutton"), true);
        }

        // *undocumented*
        internal static bool ScrollerRepeatButton(int scrollerID, Rect rect, GUIStyle style)
        {
            bool changed = false;

            if (DoRepeatButton(rect, GUIContent.none, style, FocusType.Passive))
            {
                bool firstClick = s_ScrollControlId != scrollerID;
                s_ScrollControlId = scrollerID;

                if (firstClick)
                {
                    changed = true;
                    nextScrollStepTime = DateTime.Now.AddMilliseconds(ScrollWaitDefinitions.firstWait);
                }
                else
                {
                    if (DateTime.Now >= nextScrollStepTime)
                    {
                        changed = true;
                        nextScrollStepTime = DateTime.Now.AddMilliseconds(ScrollWaitDefinitions.regularWait);
                    }
                }

                if (Event.current.type == EventType.Repaint)
                    InternalRepaintEditorWindow();
            }

            return changed;
        }

        /// *listonly*
        public static float VerticalScrollbar(Rect position, float value, float size, float topValue, float bottomValue)
        {
            return Scroller(position, value, size, topValue, bottomValue, skin.verticalScrollbar, skin.verticalScrollbarThumb, skin.verticalScrollbarUpButton, skin.verticalScrollbarDownButton, false);
        }

        // Make a vertical scrollbar. Scrollbars are what you use to scroll through a document. Most likely, you want to use scrollViews instead.
        public static float VerticalScrollbar(Rect position, float value, float size, float topValue, float bottomValue, GUIStyle style)
        {
            return Scroller(position, value, size, topValue, bottomValue, style, skin.GetStyle(style.name + "thumb"), skin.GetStyle(style.name + "upbutton"), skin.GetStyle(style.name + "downbutton"), false);
        }

        internal static float Scroller(Rect position, float value, float size, float leftValue, float rightValue, GUIStyle slider, GUIStyle thumb, GUIStyle leftButton, GUIStyle rightButton, bool horiz)
        {
            GUIUtility.CheckOnGUI();
            int id = GUIUtility.GetControlID(s_SliderHash, FocusType.Passive, position);

            Rect sliderRect, minRect, maxRect;

            if (horiz)
            {
                sliderRect = new Rect(
                        position.x + leftButton.fixedWidth, position.y,
                        position.width - leftButton.fixedWidth - rightButton.fixedWidth, position.height
                        );
                minRect = new Rect(position.x, position.y, leftButton.fixedWidth, position.height);
                maxRect = new Rect(position.xMax - rightButton.fixedWidth, position.y, rightButton.fixedWidth, position.height);
            }
            else
            {
                sliderRect = new Rect(
                        position.x, position.y + leftButton.fixedHeight,
                        position.width, position.height - leftButton.fixedHeight - rightButton.fixedHeight
                        );
                minRect = new Rect(position.x, position.y, position.width, leftButton.fixedHeight);
                maxRect = new Rect(position.x, position.yMax - rightButton.fixedHeight, position.width, rightButton.fixedHeight);
            }

            value = Slider(sliderRect, value, size, leftValue, rightValue, slider, thumb, horiz, id);

            bool wasMouseUpEvent = false;
            if (Event.current.type == EventType.MouseUp)
                wasMouseUpEvent = true;

            if (ScrollerRepeatButton(id, minRect, leftButton))
                value -= s_ScrollStepSize * (leftValue < rightValue ? 1f : -1f);

            if (ScrollerRepeatButton(id, maxRect, rightButton))
                value += s_ScrollStepSize * (leftValue < rightValue ? 1f : -1f);

            if (wasMouseUpEvent && Event.current.type == EventType.Used) // repeat buttons ate mouse up event - release scrolling
                s_ScrollControlId = 0;

            if (leftValue < rightValue)
                value = Mathf.Clamp(value, leftValue, rightValue - size);
            else
                value = Mathf.Clamp(value, rightValue, leftValue - size);
            return value;
        }

        public static void BeginClip(Rect position, Vector2 scrollOffset, Vector2 renderOffset, bool resetOffset)
        {
            GUIUtility.CheckOnGUI();
            GUIClip.Push(position, scrollOffset, renderOffset, resetOffset);
        }

        /// *listonly*
        public static void BeginGroup(Rect position)                                { BeginGroup(position, GUIContent.none, GUIStyle.none); }
        /// *listonly*
        public static void BeginGroup(Rect position, string text)                   { BeginGroup(position, GUIContent.Temp(text), GUIStyle.none); }
        /// *listonly*
        public static void BeginGroup(Rect position, Texture image)                 { BeginGroup(position, GUIContent.Temp(image), GUIStyle.none); }
        /// *listonly*
        public static void BeginGroup(Rect position, GUIContent content)            { BeginGroup(position, content, GUIStyle.none); }
        /// *listonly*
        public static void BeginGroup(Rect position, GUIStyle style)                { BeginGroup(position, GUIContent.none, style); }
        /// *listonly*
        public static void BeginGroup(Rect position, string text, GUIStyle style)   { BeginGroup(position, GUIContent.Temp(text), style); }
        /// *listonly*
        public static void BeginGroup(Rect position, Texture image, GUIStyle style) { BeginGroup(position, GUIContent.Temp(image), style); }

        public static void BeginGroup(Rect position, GUIContent content, GUIStyle style) { BeginGroup(position, content, style, Vector2.zero); }

        // Begin a group. Must be matched with a call to ::ref::EndGroup.
        internal static void BeginGroup(Rect position, GUIContent content, GUIStyle style, Vector2 scrollOffset)
        {
            GUIUtility.CheckOnGUI();
            int id = GUIUtility.GetControlID(s_BeginGroupHash, FocusType.Passive);

            if (content != GUIContent.none || style != GUIStyle.none)
            {
                switch (Event.current.type)
                {
                    case EventType.Repaint:
                        style.Draw(position, content, id);
                        break;
                    default:
                        if (position.Contains(Event.current.mousePosition))
                            GUIUtility.mouseUsed = true;
                        break;
                }
            }
            GUIClip.Push(position, scrollOffset, Vector2.zero, false);
        }

        // End a group.
        public static void EndGroup()
        {
            GUIUtility.CheckOnGUI();
            GUIClip.Internal_Pop();
        }

        // Begin a clipping rect. Must be matched with a call to ::ref::EndClip.
        // Similar to BeginGroup but does not use GUIUtility.GetControlID () for style rendering
        // and can therefore be used in Repaint events only. BeginGroup needs to be called
        // on every event for consistent controlIDs.
        public static void BeginClip(Rect position)
        {
            GUIUtility.CheckOnGUI();
            GUIClip.Push(position, Vector2.zero, Vector2.zero, false);
        }

        // End a BeginClip
        public static void EndClip()
        {
            GUIUtility.CheckOnGUI();
            GUIClip.Pop();
        }

        private static UnityEngineInternal.GenericStack s_ScrollViewStates = new UnityEngineInternal.GenericStack();

        /// *listonly*
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect)
        {
            return BeginScrollView(position, scrollPosition, viewRect, false, false, skin.horizontalScrollbar, skin.verticalScrollbar, GUI.skin.scrollView);
        }

        /// *listonly*
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical)
        {
            return BeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, skin.horizontalScrollbar, skin.verticalScrollbar, GUI.skin.scrollView);
        }

        /// *listonly*
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar)
        {
            return BeginScrollView(position, scrollPosition, viewRect, false, false, horizontalScrollbar, verticalScrollbar, GUI.skin.scrollView);
        }

        // Begin a scrolling view inside your GUI.
        public static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar)
        {
            return BeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, skin.scrollView);
        }

        // *undocumented
        protected static Vector2 DoBeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
        {
            return BeginScrollView(position, scrollPosition, viewRect, alwaysShowHorizontal, alwaysShowVertical, horizontalScrollbar, verticalScrollbar, background);
        }

        internal static Vector2 BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, bool alwaysShowHorizontal, bool alwaysShowVertical, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar, GUIStyle background)
        {
            GUIUtility.CheckOnGUI();
            if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition))
            {
                if (Mathf.Abs(Event.current.mousePosition.y - position.y) < 8)
                {
                    scrollPosition.y -= 16;
                    InternalRepaintEditorWindow();
                }
                else if (Mathf.Abs(Event.current.mousePosition.y - position.yMax) < 8)
                {
                    scrollPosition.y += 16;
                    InternalRepaintEditorWindow();
                }
            }

            int id = GUIUtility.GetControlID(s_ScrollviewHash, FocusType.Passive);
            ScrollViewState state = (ScrollViewState)GUIUtility.GetStateObject(typeof(ScrollViewState), id);

            if (state.apply)
            {
                scrollPosition = state.scrollPosition;
                state.apply = false;
            }
            state.position = position;
            state.scrollPosition = scrollPosition;
            state.visibleRect = state.viewRect = viewRect;
            state.visibleRect.width = position.width;
            state.visibleRect.height = position.height;
            s_ScrollViewStates.Push(state);

            Rect clipRect = new Rect(position);
            switch (Event.current.type)
            {
                case EventType.Layout:
                    GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                    GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                    break;
                case EventType.Used:
                    break;
                default:
                    bool needsVertical = alwaysShowVertical, needsHorizontal = alwaysShowHorizontal;

                    // Check if we need a horizontal scrollbar
                    if (needsHorizontal || viewRect.width > clipRect.width)
                    {
                        state.visibleRect.height = position.height - horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
                        clipRect.height -= horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
                        needsHorizontal = true;
                    }
                    if (needsVertical || viewRect.height > clipRect.height)
                    {
                        state.visibleRect.width = position.width - verticalScrollbar.fixedWidth + verticalScrollbar.margin.left;
                        clipRect.width -= verticalScrollbar.fixedWidth + verticalScrollbar.margin.left;
                        needsVertical = true;
                        if (!needsHorizontal && viewRect.width > clipRect.width)
                        {
                            state.visibleRect.height = position.height - horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
                            clipRect.height -= horizontalScrollbar.fixedHeight + horizontalScrollbar.margin.top;
                            needsHorizontal = true;
                        }
                    }

                    if (Event.current.type == EventType.Repaint && background != GUIStyle.none)
                    {
                        background.Draw(position, position.Contains(Event.current.mousePosition), false, needsHorizontal && needsVertical, false);
                    }
                    if (needsHorizontal && horizontalScrollbar != GUIStyle.none)
                    {
                        scrollPosition.x = HorizontalScrollbar(new Rect(position.x, position.yMax - horizontalScrollbar.fixedHeight, clipRect.width, horizontalScrollbar.fixedHeight),
                                scrollPosition.x, Mathf.Min(clipRect.width, viewRect.width), 0, viewRect.width,
                                horizontalScrollbar);
                    }
                    else
                    {
                        GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
                        GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                        GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                        if (horizontalScrollbar != GUIStyle.none)
                            scrollPosition.x = 0;
                        else
                            scrollPosition.x = Mathf.Clamp(scrollPosition.x, 0, Mathf.Max(viewRect.width - position.width, 0));
                    }

                    if (needsVertical && verticalScrollbar != GUIStyle.none)
                    {
                        scrollPosition.y = VerticalScrollbar(new Rect(clipRect.xMax + verticalScrollbar.margin.left, clipRect.y, verticalScrollbar.fixedWidth, clipRect.height),
                                scrollPosition.y, Mathf.Min(clipRect.height, viewRect.height), 0, viewRect.height,
                                verticalScrollbar);
                    }
                    else
                    {
                        GUIUtility.GetControlID(s_SliderHash, FocusType.Passive);
                        GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                        GUIUtility.GetControlID(s_RepeatButtonHash, FocusType.Passive);
                        if (verticalScrollbar != GUIStyle.none)
                            scrollPosition.y = 0;
                        else
                            scrollPosition.y = Mathf.Clamp(scrollPosition.y, 0, Mathf.Max(viewRect.height - position.height, 0));
                    }
                    break;
            }
            GUIClip.Push(clipRect, new Vector2(Mathf.Round(-scrollPosition.x - viewRect.x), Mathf.Round(-scrollPosition.y - viewRect.y)), Vector2.zero, false);
            return scrollPosition;
        }

        // Ends a scrollview started with a call to BeginScrollView.
        public static void EndScrollView()
        {
            EndScrollView(true);
        }

        public static void EndScrollView(bool handleScrollWheel)
        {
            GUIUtility.CheckOnGUI();
            ScrollViewState state = (ScrollViewState)s_ScrollViewStates.Peek();

            GUIClip.Pop();

            s_ScrollViewStates.Pop();

            // This is the mac way of handling things: if the mouse is over a scrollview, the scrollview gets the event.
            if (handleScrollWheel && Event.current.type == EventType.ScrollWheel && state.position.Contains(Event.current.mousePosition))
            {
                state.scrollPosition.x = Mathf.Clamp(state.scrollPosition.x + (Event.current.delta.x * 20f), 0f, state.viewRect.width - state.visibleRect.width);
                state.scrollPosition.y = Mathf.Clamp(state.scrollPosition.y + (Event.current.delta.y * 20f), 0f, state.viewRect.height - state.visibleRect.height);

                // If one of the visible rect dimensions is larger than the view rect dimensions
                if (state.scrollPosition.x < 0f)
                    state.scrollPosition.x = 0f;
                if (state.scrollPosition.y < 0f)
                    state.scrollPosition.y = 0f;

                state.apply = true;
                Event.current.Use();
            }
        }

        internal static ScrollViewState GetTopScrollView()
        {
            if (s_ScrollViewStates.Count != 0)
                return (ScrollViewState)s_ScrollViewStates.Peek();
            return null;
        }

        // Scrolls all enclosing scrollviews so they try to make /position/ visible.
        public static void ScrollTo(Rect position)
        {
            ScrollViewState topmost = GetTopScrollView();
            if (topmost != null)
                topmost.ScrollTo(position);
        }

        // Scrolls all enclosing scrollviews towards making /position/ visible.
        public static bool ScrollTowards(Rect position, float maxDelta)
        {
            ScrollViewState topmost = GetTopScrollView();
            if (topmost == null)
                return false;
            return topmost.ScrollTowards(position, maxDelta);
        }

        /// *listonly*
        public delegate void WindowFunction(int id);
        /// *listonly*
        public static Rect Window(int id, Rect clientRect, WindowFunction func, string text)
        {
            GUIUtility.CheckOnGUI();
            return DoWindow(id, clientRect, func, GUIContent.Temp(text), GUI.skin.window, GUI.skin, true);
        }

        /// *listonly*
        public static Rect Window(int id, Rect clientRect, WindowFunction func, Texture image)
        {
            GUIUtility.CheckOnGUI();
            return DoWindow(id, clientRect, func, GUIContent.Temp(image), GUI.skin.window, GUI.skin, true);
        }

        /// *listonly*
        public static Rect Window(int id, Rect clientRect, WindowFunction func, GUIContent content)
        {
            GUIUtility.CheckOnGUI();
            return DoWindow(id, clientRect, func, content, GUI.skin.window, GUI.skin, true);
        }

        /// *listonly*
        public static Rect Window(int id, Rect clientRect, WindowFunction func, string text, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoWindow(id, clientRect, func, GUIContent.Temp(text), style, GUI.skin, true);
        }

        /// *listonly*
        public static Rect Window(int id, Rect clientRect, WindowFunction func, Texture image, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoWindow(id, clientRect, func, GUIContent.Temp(image), style, GUI.skin, true);
        }

        // Make a popup window.
        public static Rect Window(int id, Rect clientRect, WindowFunction func, GUIContent title, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoWindow(id, clientRect, func, title, style, GUI.skin, true);
        }

        /// *listonly*
        public static Rect ModalWindow(int id, Rect clientRect, WindowFunction func, string text)
        {
            GUIUtility.CheckOnGUI();
            return DoModalWindow(id, clientRect, func, GUIContent.Temp(text), GUI.skin.window, GUI.skin);
        }

        /// *listonly*
        public static Rect ModalWindow(int id, Rect clientRect, WindowFunction func, Texture image)
        {
            GUIUtility.CheckOnGUI();
            return DoModalWindow(id, clientRect, func, GUIContent.Temp(image), GUI.skin.window, GUI.skin);
        }

        /// *listonly*
        public static Rect ModalWindow(int id, Rect clientRect, WindowFunction func, GUIContent content)
        {
            GUIUtility.CheckOnGUI();
            return DoModalWindow(id, clientRect, func, content, GUI.skin.window, GUI.skin);
        }

        /// *listonly*
        public static Rect ModalWindow(int id, Rect clientRect, WindowFunction func, string text, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoModalWindow(id, clientRect, func, GUIContent.Temp(text), style, GUI.skin);
        }

        /// *listonly*
        public static Rect ModalWindow(int id, Rect clientRect, WindowFunction func, Texture image, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoModalWindow(id, clientRect, func, GUIContent.Temp(image), style, GUI.skin);
        }

        public static Rect ModalWindow(int id, Rect clientRect, WindowFunction func, GUIContent content, GUIStyle style)
        {
            GUIUtility.CheckOnGUI();
            return DoModalWindow(id, clientRect, func, content, style, GUI.skin);
        }

        static Rect DoWindow(int id, Rect clientRect, WindowFunction func, GUIContent title, GUIStyle style, GUISkin skin, bool forceRectOnLayout)
        {
            return Internal_DoWindow(id, GUIUtility.s_OriginalID, clientRect, func, title, style, skin, forceRectOnLayout);
        }

        static Rect DoModalWindow(int id, Rect clientRect, WindowFunction func, GUIContent content, GUIStyle style, GUISkin skin)
        {
            return Internal_DoModalWindow(id, GUIUtility.s_OriginalID, clientRect, func, content, style, skin);
        }

        [RequiredByNativeCode]
        internal static void CallWindowDelegate(WindowFunction func, int id, int instanceID, GUISkin _skin, int forceRect, float width, float height, GUIStyle style)
        {
            GUILayoutUtility.SelectIDList(id, true);
            GUISkin temp = skin;
            if (Event.current.type == EventType.Layout)
            {
                if (forceRect != 0)
                {
                    GUILayoutOption[] options = { GUILayout.Width(width), GUILayout.Height(height) };

                    // Tell the GUILayout system we're starting a window, our style and our size. Then layouting is just the same as anything else
                    GUILayoutUtility.BeginWindow(id, style, options);
                }
                else
                {
                    // If we don't want to force the rect (which is when we come from GUILayout.window), don't pass in the fixedsize options
                    GUILayoutUtility.BeginWindow(id, style, null);
                }
            }
            else
            {
                GUILayoutUtility.BeginWindow(id, GUIStyle.none, null);
            }

            skin = _skin;
            func(id);

            if (Event.current.type == EventType.Layout)
            {
                // Now layout the window.
                GUILayoutUtility.Layout();
            }
            skin = temp;
        }

        // If you want to have the entire window background to act as a drag area, use the version of DragWindow that takes no parameters and put it at the end of the window function.
        public static void DragWindow() { DragWindow(new Rect(0, 0, 10000, 10000)); }

        // Call at the beginning of a frame.
        // e event to process
        // windowInfo - the list of windows we're currently using.
        // *undocumented*
        internal static void BeginWindows(int skinMode, int editorWindowInstanceID)
        {
            // Let's just remember where we came from
            GUILayoutGroup oldTopLevel = GUILayoutUtility.current.topLevel;
            UnityEngineInternal.GenericStack oldLayoutGroups = GUILayoutUtility.current.layoutGroups;
            GUILayoutGroup oldWindows = GUILayoutUtility.current.windows;
            Matrix4x4 mat = GUI.matrix;

            // Call into C++ land
            Internal_BeginWindows();

            GUI.matrix = mat;
            GUILayoutUtility.current.topLevel = oldTopLevel;
            GUILayoutUtility.current.layoutGroups = oldLayoutGroups;
            GUILayoutUtility.current.windows = oldWindows;
        }

        // Call at the end of frame (at layer 0) to do all windows
        internal static void EndWindows()
        {
            // Let's just remember where we came from
            GUILayoutGroup oldTopLevel = GUILayoutUtility.current.topLevel;
            UnityEngineInternal.GenericStack oldLayoutGroups = GUILayoutUtility.current.layoutGroups;
            GUILayoutGroup oldWindows = GUILayoutUtility.current.windows;

            // Call Into C++ land
            Internal_EndWindows();

            GUILayoutUtility.current.topLevel = oldTopLevel;
            GUILayoutUtility.current.layoutGroups = oldLayoutGroups;
            GUILayoutUtility.current.windows = oldWindows;
        }
    }
}
