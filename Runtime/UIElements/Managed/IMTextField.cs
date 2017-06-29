// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    // TODO: Sync up with EditorGUI::DoTextField
    abstract class IMTextField : IMElement
    {
        public UnityEngine.TextEditor editor { get; protected set; }

        public int maxLength { get; set; }
        public bool multiline { get; set; }

        protected override int DoGenerateControlID()
        {
            return GUIUtility.GetControlID("IMTextField".GetHashCode(), focusType, position);
        }

        protected void SyncTextEditor()
        {
            //Pre-cull input string to maxLength.
            if (maxLength >= 0 && text != null && text.Length > maxLength)
                text = text.Substring(0, maxLength);

            editor = (UnityEngine.TextEditor)GUIUtility.GetStateObject(typeof(UnityEngine.TextEditor), id);
            editor.text = text;
            editor.SaveBackup();
            editor.position = position;
            editor.style = guiStyle;
            editor.multiline = multiline;
            editor.controlID = id;
            editor.DetectFocusChange();
        }
    }

    class IMKeyboardTextField : IMTextField
    {
        private bool m_Changed;

        public override bool OnGUI(Event evt)
        {
            SyncTextEditor();

            m_Changed = false;

            bool used = false;
            switch (evt.type)
            {
                case EventType.MouseDown:
                    used = DoMouseDown(new MouseEventArgs(evt.mousePosition, evt.clickCount, evt.modifiers));
                    break;

                case EventType.MouseDrag:
                    used = DoMouseDrag(new MouseEventArgs(evt.mousePosition, evt.clickCount, evt.modifiers));
                    break;

                case EventType.MouseUp:
                    used = DoMouseUp(new MouseEventArgs(evt.mousePosition, evt.clickCount, evt.modifiers));
                    break;

                case EventType.KeyDown:
                    used = DoKeyDown(new KeyboardEventArgs(evt.character, evt.keyCode, evt.modifiers));
                    break;

                case EventType.Repaint:
                    DoRepaint(new StylePainter(evt.mousePosition));
                    break;
            }

            if (used)
            {
                evt.Use();
            }

            if (GUIUtility.keyboardControl == id)
                GUIUtility.textFieldInput = true;

            if (m_Changed)
            {
                GUI.changed = true;
                text = editor.text;
                if (maxLength >= 0 && text.Length > maxLength)
                    text = text.Substring(0, maxLength);
                evt.Use();
            }

            // Scroll offset might need to be updated
            editor.UpdateScrollOffsetIfNeeded(evt);

            return used;
        }

        internal override void DoRepaint(IStylePainter args)
        {
            // If we have keyboard focus, draw the cursor
            // TODO:    check if this OpenGL view has keyboard focus
            if (GUIUtility.keyboardControl != id)
            {
                guiStyle.Draw(position, GUIContent.Temp(text), id, false);
            }
            else
            {
                editor.DrawCursor(text);
            }
        }

        protected override bool DoMouseDown(MouseEventArgs args)
        {
            if (position.Contains(args.mousePosition))
            {
                GUIUtility.hotControl = id;
                GUIUtility.keyboardControl = id;
                editor.m_HasFocus = true;
                editor.MoveCursorToPosition(args.mousePosition);
                if (args.clickCount == 2 && GUI.skin.settings.doubleClickSelectsWord)
                {
                    editor.SelectCurrentWord();
                    editor.DblClickSnap(UnityEngine.TextEditor.DblClickSnapping.WORDS);
                    editor.MouseDragSelectsWholeWords(true);
                }
                else if (args.clickCount == 3 && GUI.skin.settings.tripleClickSelectsLine)
                {
                    editor.SelectCurrentParagraph();
                    editor.MouseDragSelectsWholeWords(true);
                    editor.DblClickSnap(UnityEngine.TextEditor.DblClickSnapping.PARAGRAPHS);
                }

                return true;
            }
            return false;
        }

        protected override bool DoMouseUp(MouseEventArgs args)
        {
            if (GUIUtility.hotControl == id)
            {
                editor.MouseDragSelectsWholeWords(false);
                GUIUtility.hotControl = 0;
                return true;
            }
            return false;
        }

        protected override bool DoMouseDrag(MouseEventArgs args)
        {
            if (GUIUtility.hotControl == id)
            {
                if (args.shift)
                    editor.MoveCursorToPosition(args.mousePosition);
                else
                    editor.SelectToPosition(args.mousePosition);
                return true;
            }
            return false;
        }

        protected override bool DoKeyDown(KeyboardEventArgs args)
        {
            if (GUIUtility.keyboardControl != id)
                return false;

            // TODO: we need to pull Event out of HandleKeyEvent... just not now.
            if (editor.HandleKeyEvent(args.ToEvent()))
            {
                m_Changed = true;
                text = editor.text;
                return true;
            }

            // Ignore tab & shift-tab in textfields
            if (args.keyCode == KeyCode.Tab || args.character == '\t')
                return false;

            char c = args.character;

            if (c == '\n' && !multiline && !args.alt)
                return false;

            // Simplest test: only allow the character if the display font supports it.
            Font font = guiStyle.font;
            if (font == null)
                font = GUI.skin.font;

            if (font.HasCharacter(c) || c == '\n')
            {
                editor.Insert(c);
                m_Changed = true;
                return c == '\n';
            }

            // On windows, keypresses also send events with keycode but no character. Eat them up here.
            if (c == 0)
            {
                // if we have a composition string, make sure we clear the previous selection.
                if (Input.compositionString.Length > 0)
                {
                    editor.ReplaceSelection("");
                    m_Changed = true;
                }
                return true;
            }
            return false;
        }
    }

    class IMTouchScreenTextField : IMTextField
    {
        private static int s_HotTextField = -1;

        private string m_SecureText;
        private char m_MaskChar;

        public string secureText
        {
            get { return m_SecureText; }
            set
            {
                string temp = value ?? string.Empty;
                if (temp != m_SecureText)
                {
                    m_SecureText = temp;
                }
            }
        }

        public char maskChar
        {
            get { return m_MaskChar; }
            set
            {
                if (m_MaskChar != value)
                {
                    m_MaskChar = value;
                }
            }
        }

        public IMTouchScreenTextField()
        {
            this.secureText = string.Empty;
            this.maskChar = char.MinValue;
        }

        public override bool OnGUI(Event evt)
        {
            SyncTextEditor();

            bool used = false;
            switch (evt.type)
            {
                case EventType.MouseDown:
                    used = DoMouseDown(new MouseEventArgs(evt.mousePosition, evt.clickCount, evt.modifiers));
                    break;

                case EventType.Repaint:
                    DoRepaint(new StylePainter(evt.mousePosition));
                    break;
            }

            if (used)
            {
                evt.Use();
            }

            // Scroll offset might need to be updated
            editor.UpdateScrollOffsetIfNeeded(evt);

            return used;
        }

        protected override int DoGenerateControlID()
        {
            return GUIUtility.GetControlID("TouchScreenTextField".GetHashCode(), focusType, position);
        }

        internal override void DoRepaint(IStylePainter args)
        {
            if (editor.keyboardOnScreen != null)
            {
                text = editor.keyboardOnScreen.text;
                if (maxLength >= 0 && text != null && text.Length > maxLength)
                    text = text.Substring(0, maxLength);

                if (editor.keyboardOnScreen.done)
                {
                    editor.keyboardOnScreen = null;
                    GUI.changed = true;
                }
            }

            // if we use system keyboard we will have normal text returned (hiding symbols is done inside os)
            // so before drawing make sure we hide them ourselves
            string clearText = text;

            if (!string.IsNullOrEmpty(secureText))
                text = GUI.PasswordFieldGetStrToShow(clearText, maskChar);

            guiStyle.Draw(position, GUIContent.Temp(text), id, false);
            text = clearText;
        }

        protected override bool DoMouseDown(MouseEventArgs args)
        {
            if (position.Contains(args.mousePosition))
            {
                GUIUtility.hotControl = id;

                // Disable keyboard for previously active text field, if any
                if (s_HotTextField != -1 && s_HotTextField != id)
                {
                    UnityEngine.TextEditor currentEditor = (UnityEngine.TextEditor)GUIUtility.GetStateObject(typeof(UnityEngine.TextEditor), s_HotTextField);
                    currentEditor.keyboardOnScreen = null;
                }

                s_HotTextField = id;

                // in player setting keyboard control calls OnFocus every time, don't want that. In editor it does not do that for some reason
                if (GUIUtility.keyboardControl != id)
                    GUIUtility.keyboardControl = id;

                editor.keyboardOnScreen = TouchScreenKeyboard.Open(
                        !string.IsNullOrEmpty(secureText) ? secureText : text,
                        TouchScreenKeyboardType.Default,
                        true, // autocorrection
                        multiline,
                        !string.IsNullOrEmpty(secureText));

                return true;
            }
            return false;
        }
    }
}
