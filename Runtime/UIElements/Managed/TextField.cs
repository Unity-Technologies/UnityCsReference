// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEngine.Experimental.UIElements
{
    public class TextField : VisualElement
    {
        // Multiline (lossy behaviour when deactivated)
        private bool m_Multiline;
        public bool multiline
        {
            get { return m_Multiline; }
            set
            {
                m_Multiline = value;
                if (!value)
                    text = text.Replace("\n", "");
            }
        }

        // Password field (indirectly lossy behaviour when activated via multiline)
        private bool m_IsPasswordField;
        public bool isPasswordField
        {
            get { return m_IsPasswordField; }
            set
            {
                m_IsPasswordField = value;
                if (value)
                    multiline = false;
            }
        }

        public char maskChar { get; set; }
        public bool doubleClickSelectsWord { get; set; }
        public bool tripleClickSelectsLine { get; set; }
        public int maxLength { get; set; }

        internal const int kMaxLengthNone = -1;

        bool touchScreenTextField { get { return TouchScreenKeyboard.isSupported; } }

        GUIStyle m_DrawGUIStyle;
        internal GUIStyle style
        {
            get { return m_DrawGUIStyle ?? (m_DrawGUIStyle = new GUIStyle()); }
        }

        public bool hasFocus { get { return elementPanel != null && elementPanel.focusedElement == this; } }

        public TextEditor editor { get; protected set; }

        public TextField() : this(kMaxLengthNone, false, false, char.MinValue)
        {
        }

        public TextField(int maxLength, bool multiline, bool isPasswordField, char maskChar)
        {
            this.maxLength = maxLength;
            this.multiline = multiline;
            this.isPasswordField = isPasswordField;
            this.maskChar = maskChar;

            if (touchScreenTextField)
            {
                editor = new TouchScreenTextEditor(this);
            }
            else
            {
                // TODO: Default values should come from GUI.skin.settings
                doubleClickSelectsWord = true;
                tripleClickSelectsLine = true;

                editor = new KeyboardTextEditor(this);
            }

            AddManipulator(editor);
        }

        // TODO: This should disappear when we're able to use style sheets directly here
        public override void OnStylesResolved(ICustomStyles styles)
        {
            base.OnStylesResolved(styles);
            m_Styles.WriteToGUIStyle(style);
        }

        internal override void DoRepaint(IStylePainter painter)
        {
            // TODO: Use painter instead of either style or editor
            // When this is used, we can get rid of the content.text trick and use mask char directly in the text to print
            if (touchScreenTextField)
            {
                TouchScreenTextEditor touchScreenEditor = editor as TouchScreenTextEditor;
                if (touchScreenEditor != null && touchScreenEditor.keyboardOnScreen != null)
                {
                    text = touchScreenEditor.keyboardOnScreen.text;
                    if (editor.maxLength >= 0 && text != null && text.Length > editor.maxLength)
                        text = text.Substring(0, editor.maxLength);

                    if (touchScreenEditor.keyboardOnScreen.done)
                    {
                        touchScreenEditor.keyboardOnScreen = null;
                        GUI.changed = true;
                    }
                }

                // if we use system keyboard we will have normal text returned (hiding symbols is done inside os)
                // so before drawing make sure we hide them ourselves
                string drawText = text;
                if (touchScreenEditor != null && !string.IsNullOrEmpty(touchScreenEditor.secureText))
                    drawText = "".PadRight(touchScreenEditor.secureText.Length, maskChar);

                style.Draw(position, GUIContent.Temp(drawText), 0, false);

                return;
            }

            if (isPasswordField)
            {
                // if we use system keyboard we will have normal text returned (hiding symbols is done inside os)
                // so before drawing make sure we hide them ourselves
                string passwordText = "".PadRight(text.Length, maskChar);

                if (!hasFocus)
                    style.Draw(position, GUIContent.Temp(passwordText), 0, false);
                else
                    DrawCursor(passwordText);

                return;
            }

            if (!hasFocus)
                style.Draw(position, GUIContent.Temp(text), 0, false);
            else
                DrawCursor(text);
        }

        void DrawCursor(string newText)
        {
            KeyboardTextEditor keyboardTextEditor = editor as KeyboardTextEditor;
            if (keyboardTextEditor == null)
                return;

            keyboardTextEditor.PreDrawCursor(newText);

            int cursorIndex = keyboardTextEditor.cursorIndex;
            int selectIndex = keyboardTextEditor.selectIndex;
            Rect localPosition = keyboardTextEditor.localPosition;
            Vector2 scrollOffset = keyboardTextEditor.scrollOffset;
            Vector2 originalContentOffset = style.contentOffset;

            style.contentOffset -= scrollOffset;
            style.Internal_clipOffset = scrollOffset;

            Input.compositionCursorPos = keyboardTextEditor.graphicalCursorPos - scrollOffset +
                new Vector2(localPosition.x, localPosition.y + style.lineHeight);

            GUIContent editorContent = new GUIContent(keyboardTextEditor.text);

            if (!string.IsNullOrEmpty(Input.compositionString))
                style.DrawWithTextSelection(position, editorContent, this.HasCapture(), hasFocus, cursorIndex, cursorIndex + Input.compositionString.Length, true);
            else
                style.DrawWithTextSelection(position, editorContent, this.HasCapture(), hasFocus, cursorIndex, selectIndex, false);

            if (keyboardTextEditor.altCursorPosition != -1)
                style.DrawCursor(position, editorContent, 0, keyboardTextEditor.altCursorPosition);

            // reset
            style.contentOffset = originalContentOffset;
            style.Internal_clipOffset = Vector2.zero;

            keyboardTextEditor.PostDrawCursor();
        }
    }
}
