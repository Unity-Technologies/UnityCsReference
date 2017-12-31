// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEngine.Experimental.UIElements
{
    public abstract class TextInputFieldBase : BaseTextElement
    {
        const string SelectionColorProperty = "selection-color";
        const string CursorColorProperty = "cursor-color";

        StyleValue<Color> m_SelectionColor;
        StyleValue<Color> m_CursorColor;

        public void SelectAll()
        {
            if (editorEngine != null)
            {
                editorEngine.SelectAll();
            }
        }

        public void UpdateText(string value)
        {
            if (text != value)
            {
                // Setting the VisualElement text here cause a repaint since it dirty the layout flag.
                using (InputEvent evt = InputEvent.GetPooled(text, value))
                {
                    evt.target = this;
                    text = value;
                    UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
                }
            }
        }

        // Password field (indirectly lossy behaviour when activated via multiline)
        public virtual bool isPasswordField { get; set; }

        public Color selectionColor
        {
            get { return m_SelectionColor.GetSpecifiedValueOrDefault(Color.clear); }
        }

        public Color cursorColor
        {
            get { return m_CursorColor.GetSpecifiedValueOrDefault(Color.clear); }
        }

        public int maxLength { get; set; }

        internal const int kMaxLengthNone = -1;

        public bool doubleClickSelectsWord { get; set; }
        public bool tripleClickSelectsLine { get; set; }

        bool touchScreenTextField
        {
            get { return TouchScreenKeyboard.isSupported; }
        }

        internal bool hasFocus
        {
            get { return elementPanel != null && elementPanel.focusController.focusedElement == this; }
        }

        /* internal for VisualTree tests */
        internal TextEditorEventHandler editorEventHandler { get; private set; }

        /* internal for VisualTree tests */
        internal TextEditorEngine editorEngine { get; private set; }

        public char maskChar { get; set; }

        public override string text
        {
            set
            {
                base.text = value;
                editorEngine.text = value;
            }
        }

        public TextInputFieldBase(int maxLength, char maskChar)
        {
            this.maxLength = maxLength;
            this.maskChar = maskChar;

            editorEngine = new TextEditorEngine(this);

            if (touchScreenTextField)
            {
                editorEventHandler = new TouchScreenTextEditorEventHandler(editorEngine, this);
            }
            else
            {
                // TODO: Default values should come from GUI.skin.settings
                doubleClickSelectsWord = true;
                tripleClickSelectsLine = true;

                editorEventHandler = new KeyboardTextEditorEventHandler(editorEngine, this);
            }

            // Make the editor style unique across all textfields
            editorEngine.style = new GUIStyle(editorEngine.style);

            // TextField are focusable by default.
            focusIndex = 0;
        }

        ContextualMenu.MenuAction.StatusFlags CutCopyActionStatus(EventBase e)
        {
            return (editorEngine.hasSelection && !isPasswordField) ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled;
        }

        ContextualMenu.MenuAction.StatusFlags PasteActionStatus(EventBase e)
        {
            return (editorEngine.CanPaste() ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled);
        }

        void Cut(EventBase e)
        {
            editorEngine.Cut();

            editorEngine.text = CullString(editorEngine.text);
            UpdateText(editorEngine.text);
        }

        void Copy(EventBase e)
        {
            editorEngine.Copy();
        }

        void Paste(EventBase e)
        {
            editorEngine.Paste();

            editorEngine.text = CullString(editorEngine.text);
            UpdateText(editorEngine.text);
        }

        protected override void OnStyleResolved(ICustomStyle style)
        {
            base.OnStyleResolved(style);

            effectiveStyle.ApplyCustomProperty(SelectionColorProperty, ref m_SelectionColor); // TODO: Switch over to default style properties
            effectiveStyle.ApplyCustomProperty(CursorColorProperty, ref m_CursorColor);
            effectiveStyle.WriteToGUIStyle(editorEngine.style);
        }

        internal virtual void SyncTextEngine()
        {
            editorEngine.text = CullString(text);

            editorEngine.SaveBackup();

            editorEngine.position = layout;

            editorEngine.DetectFocusChange();
        }

        internal string CullString(string s)
        {
            if (maxLength >= 0 && s != null && s.Length > maxLength)
                return s.Substring(0, maxLength);
            return s;
        }

        internal override void DoRepaint(IStylePainter painter)
        {
            // When this is used, we can get rid of the content.text trick and use mask char directly in the text to print
            if (touchScreenTextField)
            {
                TouchScreenTextEditorEventHandler touchScreenEditor = editorEventHandler as TouchScreenTextEditorEventHandler;
                if (touchScreenEditor != null && editorEngine.keyboardOnScreen != null)
                {
                    UpdateText(CullString(editorEngine.keyboardOnScreen.text));

                    if (editorEngine.keyboardOnScreen.status != TouchScreenKeyboard.Status.Visible)
                    {
                        editorEngine.keyboardOnScreen = null;
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
            }
            else
            {
                if (!hasFocus)
                    base.DoRepaint(painter);
                else
                    DrawWithTextSelectionAndCursor(painter, text);
            }
        }

        internal void DrawWithTextSelectionAndCursor(IStylePainter painter, string newText)
        {
            KeyboardTextEditorEventHandler keyboardTextEditor = editorEventHandler as KeyboardTextEditorEventHandler;
            if (keyboardTextEditor == null)
                return;

            keyboardTextEditor.PreDrawCursor(newText);

            int cursorIndex = editorEngine.cursorIndex;
            int selectIndex = editorEngine.selectIndex;
            Rect localPosition = editorEngine.localPosition;
            Vector2 scrollOffset = editorEngine.scrollOffset;

            IStyle style = this.style;

            var textParams = painter.GetDefaultTextParameters(this);
            textParams.text = " ";
            textParams.wordWrapWidth = 0.0f;
            textParams.wordWrap = false;

            float lineHeight = painter.ComputeTextHeight(textParams);
            float wordWrapWidth = style.wordWrap ? contentRect.width : 0.0f;

            Input.compositionCursorPos = editorEngine.graphicalCursorPos - scrollOffset +
                new Vector2(localPosition.x, localPosition.y + lineHeight);

            Color drawCursorColor = m_CursorColor.GetSpecifiedValueOrDefault(Color.grey);

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
                painterParams.border.SetWidth(0.0f);
                painterParams.border.SetRadius(0.0f);

                int min = cursorIndex < selectionEndIndex ? cursorIndex : selectionEndIndex;
                int max = cursorIndex > selectionEndIndex ? cursorIndex : selectionEndIndex;

                cursorParams = painter.GetDefaultCursorPositionParameters(this);
                cursorParams.text = editorEngine.text;
                cursorParams.wordWrapWidth = wordWrapWidth;
                cursorParams.cursorIndex = min;

                Vector2 minPos = painter.GetCursorPosition(cursorParams);
                cursorParams.cursorIndex = max;
                Vector2 maxPos = painter.GetCursorPosition(cursorParams);

                minPos -= scrollOffset;
                maxPos -= scrollOffset;

                if (Mathf.Approximately(minPos.y, maxPos.y))
                {
                    painterParams.rect = new Rect(minPos.x, minPos.y, maxPos.x - minPos.x, lineHeight);
                    painter.DrawRect(painterParams);
                }
                else
                {
                    // Draw first line
                    painterParams.rect = new Rect(minPos.x, minPos.y, wordWrapWidth - minPos.x, lineHeight);
                    painter.DrawRect(painterParams);

                    var inbetweenHeight = (maxPos.y - minPos.y) - lineHeight;
                    if (inbetweenHeight > 0f)
                    {
                        // Draw all lines in-between
                        painterParams.rect = new Rect(0f, minPos.y + lineHeight, wordWrapWidth, inbetweenHeight);
                        painter.DrawRect(painterParams);
                    }

                    // Draw last line
                    painterParams.rect = new Rect(0f, maxPos.y, maxPos.x, lineHeight);
                    painter.DrawRect(painterParams);
                }
            }

            // Draw the border as in VisualElement
            painter.DrawBorder(this);

            // Draw the text with the scroll offset
            if (!string.IsNullOrEmpty(editorEngine.text) && contentRect.width > 0.0f && contentRect.height > 0.0f)
            {
                textParams = painter.GetDefaultTextParameters(this);
                textParams.rect = new Rect(contentRect.x - scrollOffset.x, contentRect.y - scrollOffset.y, contentRect.width, contentRect.height);
                textParams.text = editorEngine.text;
                painter.DrawText(textParams);
            }

            // Draw the cursor
            if (cursorIndex == selectionEndIndex && (Font)style.font != null)
            {
                cursorParams = painter.GetDefaultCursorPositionParameters(this);
                cursorParams.text = editorEngine.text;
                cursorParams.wordWrapWidth = wordWrapWidth;
                cursorParams.cursorIndex = cursorIndex;

                Vector2 cursorPosition = painter.GetCursorPosition(cursorParams);
                cursorPosition -= scrollOffset;
                var painterParams = new RectStylePainterParameters
                {
                    rect = new Rect(cursorPosition.x, cursorPosition.y, 1f, lineHeight),
                    color = drawCursorColor
                };
                painter.DrawRect(painterParams);
            }

            // Draw alternate cursor, if any
            if (editorEngine.altCursorPosition != -1)
            {
                cursorParams = painter.GetDefaultCursorPositionParameters(this);
                cursorParams.text = editorEngine.text.Substring(0, editorEngine.altCursorPosition);
                cursorParams.wordWrapWidth = wordWrapWidth;
                cursorParams.cursorIndex = editorEngine.altCursorPosition;

                Vector2 altCursorPosition = painter.GetCursorPosition(cursorParams);
                altCursorPosition -= scrollOffset;

                var painterParams = new RectStylePainterParameters
                {
                    rect = new Rect(altCursorPosition.x, altCursorPosition.y, 1f, lineHeight),
                    color = drawCursorColor
                };
                painter.DrawRect(painterParams);
            }

            keyboardTextEditor.PostDrawCursor();
        }

        internal virtual bool AcceptCharacter(char c)
        {
            return true;
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is TextInputFieldBase)
            {
                evt.menu.AppendAction("Cut", Cut, CutCopyActionStatus);
                evt.menu.AppendAction("Copy", Copy, CutCopyActionStatus);
                evt.menu.AppendAction("Paste", Paste, PasteActionStatus);
            }
        }

        protected internal override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (elementPanel != null && elementPanel.contextualMenuManager != null)
            {
                elementPanel.contextualMenuManager.DisplayMenuIfEventMatches(evt, this);
            }

            if (evt.GetEventTypeId() == ContextualMenuPopulateEvent.TypeId())
            {
                ContextualMenuPopulateEvent e = evt as ContextualMenuPopulateEvent;
                int count = e.menu.MenuItems().Count;
                BuildContextualMenu(e);

                if (count > 0 && e.menu.MenuItems().Count > count)
                {
                    e.menu.InsertSeparator(count);
                }
            }

            editorEventHandler.ExecuteDefaultActionAtTarget(evt);
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            editorEventHandler.ExecuteDefaultAction(evt);
        }
    }
}
