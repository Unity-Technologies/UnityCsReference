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

        protected virtual void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<FocusEvent>(OnFocus);
            target.RegisterCallback<BlurEvent>(OnBlur);
        }

        protected virtual void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<FocusEvent>(OnFocus);
            target.UnregisterCallback<BlurEvent>(OnBlur);
        }

        void OnFocus(FocusEvent evt)
        {
            OnFocus();
        }

        void OnBlur(BlurEvent evt)
        {
            OnLostFocus();
        }

        VisualElement m_Target;

        public VisualElement target
        {
            get
            {
                return m_Target;
            }

            set
            {
                if (target != null)
                {
                    UnregisterCallbacksFromTarget();
                }
                m_Target = value;
                if (target != null)
                {
                    RegisterCallbacksOnTarget();
                }
            }
        }

        protected TextEditor(TextField textField)
        {
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

            position = textField.layout;
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
