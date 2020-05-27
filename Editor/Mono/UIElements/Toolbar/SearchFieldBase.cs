// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public abstract class SearchFieldBase<TextInputType, T> : VisualElement, INotifyValueChanged<T>
        where TextInputType : TextInputBaseField<T>, new()
    {
        private readonly Button m_SearchButton;
        private readonly Button m_CancelButton;
        private readonly TextInputType m_TextField;
        protected TextInputType textInputField { get { return m_TextField; } }

        protected Button searchButton
        {
            get { return m_SearchButton; }
        }

        public T value
        {
            get { return m_TextField.value; }
            set { m_TextField.value = value; }
        }

        public static readonly string ussClassName = "unity-search-field-base";
        public static readonly string textUssClassName = ussClassName + "__text-field";
        public static readonly string textInputUssClassName = textUssClassName + "__input";
        public static readonly string searchButtonUssClassName = ussClassName + "__search-button";
        public static readonly string cancelButtonUssClassName = ussClassName + "__cancel-button";
        public static readonly string cancelButtonOffVariantUssClassName = cancelButtonUssClassName + "--off";
        public static readonly string popupVariantUssClassName = ussClassName + "--popup";

        protected SearchFieldBase()
        {
            isCompositeRoot = true;
            focusable = true;
            tabIndex = 0;
            excludeFromFocusRing = true;
            delegatesFocus = false;

            AddToClassList(ussClassName);

            m_SearchButton = new Button(() => {}) { name = "unity-search" };
            m_SearchButton.AddToClassList(searchButtonUssClassName);
            hierarchy.Add(m_SearchButton);

            m_TextField = new TextInputType();
            m_TextField.AddToClassList(textUssClassName);
            hierarchy.Add(m_TextField);
            m_TextField.RegisterValueChangedCallback(OnValueChanged);

            var textInput = m_TextField.Q(TextField.textInputUssName);
            textInput.RegisterCallback<KeyDownEvent>(OnTextFieldKeyDown);
            textInput.AddToClassList(textInputUssClassName);

            m_CancelButton = new Button(() => {}) { name = "unity-cancel" };
            m_CancelButton.AddToClassList(cancelButtonUssClassName);
            m_CancelButton.AddToClassList(cancelButtonOffVariantUssClassName);
            hierarchy.Add(m_CancelButton);

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEvent);
            m_CancelButton.clickable.clicked += OnCancelButtonClick;
        }

        private void OnAttachToPanelEvent(AttachToPanelEvent evt)
        {
            UpdateCancelButton();
        }

        private void OnValueChanged(ChangeEvent<T> change)
        {
            UpdateCancelButton(); // We need to update it on value changed because in most cases the TextField is modified directly
        }

        protected abstract void ClearTextField();

        private void OnTextFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
                ClearTextField();
        }

        private void OnCancelButtonClick()
        {
            ClearTextField();
        }

        public virtual void SetValueWithoutNotify(T newValue)
        {
            m_TextField.SetValueWithoutNotify(newValue);
            UpdateCancelButton(); // We need to update it in that case because OnValueChanged will not be called
        }

        protected abstract bool FieldIsEmpty(T fieldValue);

        private void UpdateCancelButton()
        {
            m_CancelButton.EnableInClassList(cancelButtonOffVariantUssClassName, FieldIsEmpty(m_TextField.value));
        }
    }
}
