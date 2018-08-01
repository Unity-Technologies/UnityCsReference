// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEngine.Experimental.UIElements
{
    internal interface ITextInputField : IEventHandler, ITextElement
    {
        bool hasFocus { get; }

        bool doubleClickSelectsWord { get; }
        bool tripleClickSelectsLine { get; }

        void SyncTextEngine();
        bool AcceptCharacter(char c);
        string CullString(string s);
        void UpdateText(string value);
    }

    public abstract class TextInputFieldBase<T> : BaseField<T>, ITextInputField
    {
        public new class UxmlTraits : BaseField<T>.UxmlTraits
        {
            UxmlIntAttributeDescription m_MaxLength = new UxmlIntAttributeDescription { name = "max-length", obsoleteNames = new[] { "maxLength" }, defaultValue = kMaxLengthNone };
            UxmlBoolAttributeDescription m_Password = new UxmlBoolAttributeDescription { name = "password" };
            UxmlStringAttributeDescription m_MaskCharacter = new UxmlStringAttributeDescription { name = "mask-character", obsoleteNames = new[] { "maskCharacter" }, defaultValue = "*" };
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                TextInputFieldBase<T> field = ((TextInputFieldBase<T>)ve);
                field.maxLength = m_MaxLength.GetValueFromBag(bag, cc);
                field.isPasswordField = m_Password.GetValueFromBag(bag, cc);
                string maskCharacter = m_MaskCharacter.GetValueFromBag(bag, cc);
                if (maskCharacter != null && maskCharacter.Length > 0)
                {
                    field.maskChar = maskCharacter[0];
                }
                ((ITextElement)ve).text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        const string SelectionColorProperty = "selection-color";
        const string CursorColorProperty = "cursor-color";

        StyleValue<Color> m_SelectionColor;
        StyleValue<Color> m_CursorColor;

        private string m_Text;
        public string text
        {
            get { return m_Text; }
            protected set
            {
                if (m_Text == value)
                    return;

                m_Text = value;
                editorEngine.text = value;
                IncrementVersion(VersionChangeType.Layout);
            }
        }

        public void SelectAll()
        {
            if (editorEngine != null)
            {
                editorEngine.SelectAll();
            }
        }

        private void UpdateText(string value)
        {
            if (text != value)
            {
                // Setting the VisualElement text here cause a repaint since it dirty the layout flag.
                using (InputEvent evt = InputEvent.GetPooled(text, value))
                {
                    evt.target = this;
                    text = value;
                    SendEvent(evt);
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

        public int cursorIndex { get { return editorEngine.cursorIndex; } }
        public int selectIndex { get { return editorEngine.selectIndex; } }

        public int maxLength { get; set; }

        internal const int kMaxLengthNone = -1;

        public bool doubleClickSelectsWord { get; set; }
        public bool tripleClickSelectsLine { get; set; }

        public bool isDelayed { get; set; }

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

        public TextInputFieldBase(int maxLength, char maskChar)
        {
            requireMeasureFunction = true;

            m_Text = "";
            this.maxLength = maxLength;
            this.maskChar = maskChar;

            editorEngine = new TextEditorEngine(OnDetectFocusChange, OnCursorIndexChange);

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
        }

        DropdownMenu.MenuAction.StatusFlags CutCopyActionStatus(DropdownMenu.MenuAction a)
        {
            return (editorEngine.hasSelection && !isPasswordField) ? DropdownMenu.MenuAction.StatusFlags.Normal : DropdownMenu.MenuAction.StatusFlags.Disabled;
        }

        DropdownMenu.MenuAction.StatusFlags PasteActionStatus(DropdownMenu.MenuAction a)
        {
            return (editorEngine.CanPaste() ? DropdownMenu.MenuAction.StatusFlags.Normal : DropdownMenu.MenuAction.StatusFlags.Disabled);
        }

        void Cut(DropdownMenu.MenuAction a)
        {
            editorEngine.Cut();

            editorEngine.text = CullString(editorEngine.text);
            UpdateText(editorEngine.text);
        }

        void Copy(DropdownMenu.MenuAction a)
        {
            editorEngine.Copy();
        }

        void Paste(DropdownMenu.MenuAction a)
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

        protected override void DoRepaint(IStylePainter painter)
        {
            var stylePainter = (IStylePainterInternal)painter;
            // When this is used, we can get rid of the content.text trick and use mask char directly in the text to print
            if (touchScreenTextField)
            {
                var touchScreenEditor = editorEventHandler as TouchScreenTextEditorEventHandler;
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

                text = drawText;
            }
            else
            {
                if (!hasFocus)
                {
                    stylePainter.DrawText(text);
                }
                else
                    DrawWithTextSelectionAndCursor(stylePainter, text);
            }
        }

        internal void DrawWithTextSelectionAndCursor(IStylePainterInternal painter, string newText)
        {
            var keyboardTextEditor = editorEventHandler as KeyboardTextEditorEventHandler;
            if (keyboardTextEditor == null)
                return;

            keyboardTextEditor.PreDrawCursor(newText);

            int cursorIndex = editorEngine.cursorIndex;
            int selectIndex = editorEngine.selectIndex;
            Rect localPosition = editorEngine.localPosition;
            Vector2 scrollOffset = editorEngine.scrollOffset;

            IStyle style = this.style;

            float textScaling = TextNative.ComputeTextScaling(worldTransform);

            var textParams = TextStylePainterParameters.GetDefault(this, text);
            textParams.text = " ";
            textParams.wordWrapWidth = 0.0f;
            textParams.wordWrap = false;

            var textNativeSettings = textParams.GetTextNativeSettings(textScaling);
            float lineHeight = TextNative.ComputeTextHeight(textNativeSettings);
            float wordWrapWidth = editorEngine.multiline
                ? contentRect.width
                : 0.0f;

            Input.compositionCursorPos = editorEngine.graphicalCursorPos - scrollOffset +
                new Vector2(localPosition.x, localPosition.y + lineHeight);

            Color drawCursorColor = m_CursorColor.GetSpecifiedValueOrDefault(Color.grey);

            int selectionEndIndex = string.IsNullOrEmpty(Input.compositionString)
                ? selectIndex
                : cursorIndex + Input.compositionString.Length;

            CursorPositionStylePainterParameters cursorParams;

            // Draw highlighted section, if any
            if (cursorIndex != selectionEndIndex)
            {
                var painterParams = RectStylePainterParameters.GetDefault(this);
                painterParams.color = selectionColor;
                painterParams.border.SetWidth(0.0f);
                painterParams.border.SetRadius(0.0f);

                int min = cursorIndex < selectionEndIndex ? cursorIndex : selectionEndIndex;
                int max = cursorIndex > selectionEndIndex ? cursorIndex : selectionEndIndex;

                cursorParams = CursorPositionStylePainterParameters.GetDefault(this, text);
                cursorParams.text = editorEngine.text;
                cursorParams.wordWrapWidth = wordWrapWidth;
                cursorParams.cursorIndex = min;

                textNativeSettings = cursorParams.GetTextNativeSettings(textScaling);
                Vector2 minPos = TextNative.GetCursorPosition(textNativeSettings, cursorParams.rect, min);
                Vector2 maxPos = TextNative.GetCursorPosition(textNativeSettings, cursorParams.rect, max);

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
                    painterParams.rect = new Rect(minPos.x, minPos.y, contentRect.xMax - minPos.x, lineHeight);
                    painter.DrawRect(painterParams);

                    var inbetweenHeight = (maxPos.y - minPos.y) - lineHeight;
                    if (inbetweenHeight > 0f)
                    {
                        // Draw all lines in-between
                        painterParams.rect = new Rect(contentRect.x, minPos.y + lineHeight, wordWrapWidth, inbetweenHeight);
                        painter.DrawRect(painterParams);
                    }

                    // Draw last line if not empty
                    if (maxPos.x != contentRect.x)
                    {
                        painterParams.rect = new Rect(contentRect.x, maxPos.y, maxPos.x, lineHeight);
                        painter.DrawRect(painterParams);
                    }
                }
            }

            // Draw the text with the scroll offset
            if (!string.IsNullOrEmpty(editorEngine.text) && contentRect.width > 0.0f && contentRect.height > 0.0f)
            {
                textParams = TextStylePainterParameters.GetDefault(this, text);
                textParams.rect = new Rect(contentRect.x - scrollOffset.x, contentRect.y - scrollOffset.y, contentRect.width, contentRect.height);
                textParams.text = editorEngine.text;
                painter.DrawText(textParams);
            }

            // Draw the cursor
            if (cursorIndex == selectionEndIndex && (Font)style.font != null)
            {
                cursorParams = CursorPositionStylePainterParameters.GetDefault(this, text);
                cursorParams.text = editorEngine.text;
                cursorParams.wordWrapWidth = wordWrapWidth;
                cursorParams.cursorIndex = cursorIndex;

                textNativeSettings = cursorParams.GetTextNativeSettings(textScaling);
                Vector2 cursorPosition = TextNative.GetCursorPosition(textNativeSettings, cursorParams.rect, cursorParams.cursorIndex);
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
                cursorParams = CursorPositionStylePainterParameters.GetDefault(this, text);
                cursorParams.text = editorEngine.text.Substring(0, editorEngine.altCursorPosition);
                cursorParams.wordWrapWidth = wordWrapWidth;
                cursorParams.cursorIndex = editorEngine.altCursorPosition;

                textNativeSettings = cursorParams.GetTextNativeSettings(textScaling);
                Vector2 altCursorPosition = TextNative.GetCursorPosition(textNativeSettings, cursorParams.rect, cursorParams.cursorIndex);
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
            if (evt.target is TextInputFieldBase<T>)
            {
                evt.menu.AppendAction("Cut", Cut, CutCopyActionStatus);
                evt.menu.AppendAction("Copy", Copy, CutCopyActionStatus);
                evt.menu.AppendAction("Paste", Paste, PasteActionStatus);
            }
        }

        private void OnDetectFocusChange()
        {
            if (editorEngine.m_HasFocus && !hasFocus)
                editorEngine.OnFocus();
            if (!editorEngine.m_HasFocus && hasFocus)
                editorEngine.OnLostFocus();
        }

        private void OnCursorIndexChange()
        {
            IncrementVersion(VersionChangeType.Repaint);
        }

        protected internal override Vector2 DoMeasure(float width, MeasureMode widthMode, float height, MeasureMode heightMode)
        {
            return TextElement.MeasureVisualElementTextSize(this, m_Text, width, widthMode, height, heightMode);
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
                    e.menu.InsertSeparator(null, count);
                }
            }

            editorEventHandler.ExecuteDefaultActionAtTarget(evt);
        }

        protected internal override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            editorEventHandler.ExecuteDefaultAction(evt);
        }

        bool ITextInputField.hasFocus
        {
            get { return hasFocus; }
        }

        string ITextElement.text
        {
            get { return m_Text; }
            set { m_Text = value; }
        }

        void ITextInputField.SyncTextEngine()
        {
            SyncTextEngine();
        }

        bool ITextInputField.AcceptCharacter(char c)
        {
            return AcceptCharacter(c);
        }

        string ITextInputField.CullString(string s)
        {
            return CullString(s);
        }

        void ITextInputField.UpdateText(string value)
        {
            UpdateText(value);
        }
    }
}
