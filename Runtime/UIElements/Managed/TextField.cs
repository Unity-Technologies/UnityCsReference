// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Experimental.UIElements.StyleSheets;
using System;

namespace UnityEngine.Experimental.UIElements
{
    public class TextField : VisualContainer
    {
        public Action<string> OnTextChanged;
        public Action OnTextChangeValidated;

        // TODO: Switch over to default style properties
        const string SelectionColorProperty = "selection-color";
        const string CursorColorProperty = "cursor-color";

        StyleValue<Color> m_SelectionColor;
        StyleValue<Color> m_CursorColor;

        public Color selectionColor
        {
            get { return m_SelectionColor.GetSpecifiedValueOrDefault(Color.clear); }
        }

        public Color cursorColor
        {
            get { return m_CursorColor.GetSpecifiedValueOrDefault(Color.clear); }
        }

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

        public bool hasFocus { get { return elementPanel != null && elementPanel.focusController.focusedElement == this; } }

        internal TextEditor editor { get; set; }

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

            // Make the editor style unique across all textfields
            editor.style = new GUIStyle(editor.style);

            // TextField are focusable by default.
            focusIndex = 0;

            this.AddManipulator(editor);
        }

        internal void TextFieldChanged()
        {
            if (OnTextChanged != null)
            {
                OnTextChanged(text);
            }
        }

        internal void TextFieldChangeValidated()
        {
            if (OnTextChangeValidated != null)
            {
                OnTextChangeValidated();
            }
        }

        public override void OnPersistentDataReady()
        {
            base.OnPersistentDataReady();

            string key = GetFullHierarchicalPersistenceKey();

            OverwriteFromPersistedData(this, key);
        }

        public override void OnStyleResolved(ICustomStyle style)
        {
            base.OnStyleResolved(style);

            effectiveStyle.ApplyCustomProperty(SelectionColorProperty, ref m_SelectionColor); // TODO: Switch over to default style properties
            effectiveStyle.ApplyCustomProperty(CursorColorProperty, ref m_CursorColor);
            effectiveStyle.WriteToGUIStyle(editor.style);
        }

        internal override void DoRepaint(IStylePainter painter)
        {
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

                base.DoRepaint(painter);

                text = drawText;

                return;
            }

            if (isPasswordField)
            {
                // if we use system keyboard we will have normal text returned (hiding symbols is done inside os)
                // so before drawing make sure we hide them ourselves
                string drawText = text;
                text = "".PadRight(text.Length, maskChar);

                if (!hasFocus)
                    base.DoRepaint(painter);
                else
                    DrawWithTextSelectionAndCursor(painter, text);

                text = drawText;

                return;
            }

            if (!hasFocus)
                base.DoRepaint(painter);
            else
                DrawWithTextSelectionAndCursor(painter, text);
        }

        void DrawWithTextSelectionAndCursor(IStylePainter painter, string newText)
        {
            KeyboardTextEditor keyboardTextEditor = editor as KeyboardTextEditor;
            if (keyboardTextEditor == null)
                return;

            keyboardTextEditor.PreDrawCursor(newText);

            int cursorIndex = keyboardTextEditor.cursorIndex;
            int selectIndex = keyboardTextEditor.selectIndex;
            Rect localPosition = keyboardTextEditor.localPosition;
            Vector2 scrollOffset = keyboardTextEditor.scrollOffset;

            IStyle style = this.style;

            var textParams = painter.GetDefaultTextParameters(this);
            textParams.text = " ";
            textParams.wordWrapWidth = 0.0f;
            textParams.wordWrap = false;

            float lineHeight = painter.ComputeTextHeight(textParams);
            float wordWrapWidth = style.wordWrap ? contentRect.width : 0.0f;

            Input.compositionCursorPos = keyboardTextEditor.graphicalCursorPos - scrollOffset +
                new Vector2(localPosition.x, localPosition.y + lineHeight);

            Color drawCursorColor = cursorColor != Color.clear ? cursorColor : GUI.skin.settings.cursorColor;

            int selectionEndIndex = string.IsNullOrEmpty(Input.compositionString)
                ? selectIndex
                : cursorIndex + Input.compositionString.Length;

            // Draw the background as in VisualElement
            painter.DrawBackground(this);

            CursorPositionStylePainterParameters cursorParams;

            // Draw highlighted section, if any
            if (cursorIndex != selectionEndIndex)
            {
                var painterParams = painter.GetDefaultRectParameters(this);
                painterParams.color = selectionColor;
                painterParams.borderLeftWidth = 0.0f;
                painterParams.borderTopWidth = 0.0f;
                painterParams.borderRightWidth = 0.0f;
                painterParams.borderBottomWidth = 0.0f;
                painterParams.borderTopLeftRadius = 0.0f;
                painterParams.borderTopRightRadius = 0.0f;
                painterParams.borderBottomRightRadius = 0.0f;
                painterParams.borderBottomLeftRadius = 0.0f;

                int min = cursorIndex < selectionEndIndex ? cursorIndex : selectionEndIndex;
                int max = cursorIndex > selectionEndIndex ? cursorIndex : selectionEndIndex;

                cursorParams = painter.GetDefaultCursorPositionParameters(this);
                cursorParams.text = keyboardTextEditor.text;
                cursorParams.wordWrapWidth = wordWrapWidth;
                cursorParams.cursorIndex = min;

                Vector2 minPos = painter.GetCursorPosition(cursorParams);
                cursorParams.cursorIndex = max;
                Vector2 maxPos = painter.GetCursorPosition(cursorParams);

                minPos -= scrollOffset;
                maxPos -= scrollOffset;

                if (Mathf.Approximately(minPos.y, maxPos.y))
                {
                    painterParams.layout = new Rect(minPos.x, minPos.y, maxPos.x - minPos.x, lineHeight);
                    painter.DrawRect(painterParams);
                }
                else
                {
                    // Draw first line
                    painterParams.layout = new Rect(minPos.x, minPos.y, wordWrapWidth - minPos.x, lineHeight);
                    painter.DrawRect(painterParams);

                    var inbetweenHeight = (maxPos.y - minPos.y) - lineHeight;
                    if (inbetweenHeight > 0f)
                    {
                        // Draw all lines in-between
                        painterParams.layout = new Rect(0f, minPos.y + lineHeight, wordWrapWidth, inbetweenHeight);
                        painter.DrawRect(painterParams);
                    }

                    // Draw last line
                    painterParams.layout = new Rect(0f, maxPos.y, maxPos.x, lineHeight);
                    painter.DrawRect(painterParams);
                }
            }

            // Draw the border as in VisualElement
            painter.DrawBorder(this);

            // Draw the text with the scroll offset
            if (!string.IsNullOrEmpty(keyboardTextEditor.text) && contentRect.width > 0.0f && contentRect.height > 0.0f)
            {
                textParams = painter.GetDefaultTextParameters(this);
                textParams.layout = new Rect(contentRect.x - scrollOffset.x, contentRect.y - scrollOffset.y, contentRect.width, contentRect.height);
                textParams.text = keyboardTextEditor.text;
                painter.DrawText(textParams);
            }

            // Draw the cursor
            if (cursorIndex == selectionEndIndex && (Font)style.font != null)
            {
                cursorParams = painter.GetDefaultCursorPositionParameters(this);
                cursorParams.text = keyboardTextEditor.text;
                cursorParams.wordWrapWidth = wordWrapWidth;
                cursorParams.cursorIndex = cursorIndex;

                Vector2 cursorPosition = painter.GetCursorPosition(cursorParams);
                cursorPosition -= scrollOffset;
                var painterParams = new RectStylePainterParameters
                {
                    layout = new Rect(cursorPosition.x, cursorPosition.y, 1f, lineHeight),
                    color = drawCursorColor
                };
                painter.DrawRect(painterParams);
            }

            // Draw alternate cursor, if any
            if (keyboardTextEditor.altCursorPosition != -1)
            {
                cursorParams = painter.GetDefaultCursorPositionParameters(this);
                cursorParams.text = keyboardTextEditor.text.Substring(0, keyboardTextEditor.altCursorPosition);
                cursorParams.wordWrapWidth = wordWrapWidth;
                cursorParams.cursorIndex = keyboardTextEditor.altCursorPosition;

                Vector2 altCursorPosition = painter.GetCursorPosition(cursorParams);
                altCursorPosition -= scrollOffset;

                var painterParams = new RectStylePainterParameters
                {
                    layout = new Rect(altCursorPosition.x, altCursorPosition.y, 1f, lineHeight),
                    color = drawCursorColor
                };
                painter.DrawRect(painterParams);
            }

            keyboardTextEditor.PostDrawCursor();
        }
    }
}
