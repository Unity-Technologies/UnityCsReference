// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public abstract class SearchFieldBase<TextInputType, T> : VisualElement, INotifyValueChanged<T>
        where TextInputType : TextInputBaseField<T>, new()
    {
        Button m_SearchButton;
        Button m_CancelButton;
        TextInputType m_TextField;
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
            m_TextField.Q(TextField.textInputUssName).RegisterCallback<KeyDownEvent>(OnTextFieldKeyDown);

            m_CancelButton = new Button(() => {}) { name = "unity-cancel" };
            m_CancelButton.AddToClassList(cancelButtonUssClassName);
            m_CancelButton.AddToClassList(cancelButtonOffVariantUssClassName);
            hierarchy.Add(m_CancelButton);
        }

        void OnValueChanged(ChangeEvent<T> change)
        {
            UpdateCancelButtonOnModify(FieldIsEmpty(change.previousValue), FieldIsEmpty(change.newValue));
        }

        protected abstract void ClearTextField();

        void OnTextFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
                ClearTextField();
        }

        void OnCancelButtonClick()
        {
            ClearTextField();
        }

        public virtual void SetValueWithoutNotify(T newValue)
        {
            value = newValue;
        }

        protected abstract bool FieldIsEmpty(T fieldValue);

        private void UpdateCancelButtonOnModify(bool currentValueNull, bool newValueNull)
        {
            if (currentValueNull && !newValueNull)
            {
                m_CancelButton.RemoveFromClassList(cancelButtonOffVariantUssClassName);
                m_CancelButton.clickable.clicked += OnCancelButtonClick;
            }
            if (!currentValueNull && newValueNull)
            {
                m_CancelButton.AddToClassList(cancelButtonOffVariantUssClassName);
                m_CancelButton.clickable.clicked -= OnCancelButtonClick;
            }
        }
    }
}
