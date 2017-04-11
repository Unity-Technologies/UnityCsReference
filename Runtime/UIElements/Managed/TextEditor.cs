// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    public class TextEditor : UnityEngine.TextEditor, IManipulator
    {
        public int maxLength { get; set; }
        public char maskChar { get; set; }
        public bool doubleClickSelectsWord { get; set; }
        public bool tripleClickSelectsLine { get; set; }

        protected TextField textField { get; set; }

        internal override Rect localPosition { get { return new Rect(0, 0, position.width, position.height); } }

        // Default manipulator implementation
        public VisualElement target { get; set; }
        public EventPhase phaseInterest { get; set; }
        public IPanel panel
        {
            get
            {
                if (target != null)
                    return target.panel;
                return null;
            }
        }

        public virtual EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
        {
            return EventPropagation.Continue;
        }

        public virtual void OnLostCapture()
        {
        }

        public virtual void OnLostKeyboardFocus()
        {
        }

        protected TextEditor(TextField textField)
        {
            phaseInterest = EventPhase.BubbleUp;
            this.textField = textField;
            SyncTextEditor();
        }

        protected void SyncTextEditor()
        {
            // Pre-cull input string to maxLength.
            string textFieldText = textField.text;
            if (maxLength >= 0 && textFieldText != null && textFieldText.Length > maxLength)
                textFieldText = textFieldText.Substring(0, maxLength);
            text = textFieldText;

            SaveBackup();

            style = textField.style;
            position = textField.position;
            maxLength = textField.maxLength;
            multiline = textField.multiline;
            isPasswordField = textField.isPasswordField;
            maskChar = textField.maskChar;
            doubleClickSelectsWord = textField.doubleClickSelectsWord;
            tripleClickSelectsLine = textField.tripleClickSelectsLine;

            DetectFocusChange();
        }

        internal override void OnDetectFocusChange()
        {
            if (m_HasFocus && !textField.hasFocus)
                OnFocus();
            if (!m_HasFocus && textField.hasFocus)
                OnLostFocus();
        }
    }
}
