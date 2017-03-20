// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.RMGUI
{
    // TODO: Sync up with EditorGUI::DoTextField
    abstract class IMTextField : IMElement
    {
        protected TextEditor m_Editor;

        public int maxLength { get; set; }
        public bool multiline { get; set; }

        protected override int DoGenerateControlID()
        {
            return GUIUtility.GetControlID("IMTextField".GetHashCode(), focusType, position);
        }

        protected void SyncTextEditor()
        {
            //Pre-cull input string to maxLength.
            if (maxLength >= 0 && content.text != null && content.text.Length > maxLength)
                content.text = content.text.Substring(0, maxLength);

            m_Editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), id);
            m_Editor.text = content.text;
            m_Editor.SaveBackup();
            m_Editor.position = position;
            m_Editor.style = style;
            m_Editor.multiline = multiline;
            m_Editor.controlID = id;
            m_Editor.DetectFocusChange();
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
                content.text = m_Editor.text;
                if (maxLength >= 0 && content.text.Length > maxLength)
                    content.text = content.text.Substring(0, maxLength);
                evt.Use();
            }

            // Scroll offset might need to be updated
            m_Editor.UpdateScrollOffsetIfNeeded(evt);

            return used;
        }

        public override void DoRepaint(IStylePainter args)
        {
            // If we have keyboard focus, draw the cursor
            // TODO:    check if this OpenGL view has keyboard focus
            if (GUIUtility.keyboardControl != id)
            {
                style.Draw(position, content, id, false);
            }
            else
            {
                m_Editor.DrawCursor(content.text);
            }
        }

        protected override bool DoMouseDown(MouseEventArgs args)
        {
            if (position.Contains(args.mousePosition))
            {
                GUIUtility.hotControl = id;
                GUIUtility.keyboardControl = id;
                m_Editor.m_HasFocus = true;
                m_Editor.MoveCursorToPosition(args.mousePosition);
                if (args.clickCount == 2 && GUI.skin.settings.doubleClickSelectsWord)
                {
                    m_Editor.SelectCurrentWord();
                    m_Editor.DblClickSnap(TextEditor.DblClickSnapping.WORDS);
                    m_Editor.MouseDragSelectsWholeWords(true);
                }
                if (args.clickCount == 3 && GUI.skin.settings.tripleClickSelectsLine)
                {
                    m_Editor.SelectCurrentParagraph();
                    m_Editor.MouseDragSelectsWholeWords(true);
                    m_Editor.DblClickSnap(TextEditor.DblClickSnapping.PARAGRAPHS);
                }

                return true;
            }
            return false;
        }

        protected override bool DoMouseUp(MouseEventArgs args)
        {
            if (GUIUtility.hotControl == id)
            {
                m_Editor.MouseDragSelectsWholeWords(false);
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
                    m_Editor.MoveCursorToPosition(args.mousePosition);
                else
                    m_Editor.SelectToPosition(args.mousePosition);
                return true;
            }
            return false;
        }

        protected override bool DoKeyDown(KeyboardEventArgs args)
        {
            if (GUIUtility.keyboardControl != id)
                return false;

            // TODO: we need to pull Event out of HandleKeyEvent... just not now.
            if (m_Editor.HandleKeyEvent(args.ToEvent()))
            {
                m_Changed = true;
                content.text = m_Editor.text;
                return true;
            }

            // Ignore tab & shift-tab in textfields
            if (args.keyCode == KeyCode.Tab || args.character == '\t')
                return false;

            char c = args.character;

            if (c == '\n' && !multiline && !args.alt)
                return false;

            // Simplest test: only allow the character if the display font supports it.
            Font font = style.font;
            if (font == null)
                font = GUI.skin.font;

            if (font.HasCharacter(c) || c == '\n')
            {
                m_Editor.Insert(c);
                m_Changed = true;
                return false;
            }

            // On windows, keypresses also send events with keycode but no character. Eat them up here.
            if (c == 0)
            {
                // if we have a composition string, make sure we clear the previous selection.
                if (Input.compositionString.Length > 0)
                {
                    m_Editor.ReplaceSelection("");
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
            m_Editor.UpdateScrollOffsetIfNeeded(evt);

            return used;
        }

        protected override int DoGenerateControlID()
        {
            return GUIUtility.GetControlID("TouchScreenTextField".GetHashCode(), focusType, position);
        }

        public override void DoRepaint(IStylePainter args)
        {
            if (m_Editor.keyboardOnScreen != null)
            {
                content.text = m_Editor.keyboardOnScreen.text;
                if (maxLength >= 0 && content.text != null && content.text.Length > maxLength)
                    content.text = content.text.Substring(0, maxLength);

                if (m_Editor.keyboardOnScreen.done)
                {
                    m_Editor.keyboardOnScreen = null;
                    GUI.changed = true;
                }
            }

            // if we use system keyboard we will have normal text returned (hiding symbols is done inside os)
            // so before drawing make sure we hide them ourselves
            string clearText = content.text;

            if (!string.IsNullOrEmpty(secureText))
                content.text = GUI.PasswordFieldGetStrToShow(clearText, maskChar);

            style.Draw(position, content, id, false);
            content.text = clearText;
        }

        protected override bool DoMouseDown(MouseEventArgs args)
        {
            if (position.Contains(args.mousePosition))
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

                m_Editor.keyboardOnScreen = TouchScreenKeyboard.Open(
                        !string.IsNullOrEmpty(secureText) ? secureText : content.text,
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
