// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    public class ToolbarSearchField : VisualElement, INotifyValueChanged<string>
    {
        public new class UxmlFactory : UxmlFactory<ToolbarSearchField> {}

        Button m_SearchButton;
        Button m_CancelButton;
        TextField m_TextField;

        protected Button searchButton
        {
            get { return m_SearchButton; }
        }

        string m_CurrentText;

        public string value
        {
            get
            {
                return m_CurrentText;
            }
            set
            {
                if (m_CurrentText == value)
                    return;

                if (panel != null)
                {
                    using (ChangeEvent<string> evt = ChangeEvent<string>.GetPooled(m_CurrentText, value))
                    {
                        evt.target = this;
                        SetValueWithoutNotify(value);
                        SendEvent(evt);
                    }
                }
                else
                {
                    SetValueWithoutNotify(value);
                }
            }
        }

        public static readonly string ussClassName = "unity-toolbar-search-field";
        public static readonly string textUssClassName = ussClassName + "__text-field";
        public static readonly string searchButtonUssClassName = ussClassName + "__search-button";
        public static readonly string cancelButtonUssClassName = ussClassName + "__cancel-button";
        public static readonly string cancelButtonOffVariantUssClassName = cancelButtonUssClassName + "--off";
        public static readonly string popupVariantUssClassName = ussClassName + "--popup";

        public ToolbarSearchField()
        {
            Toolbar.SetToolbarStyleSheet(this);
            m_CurrentText = String.Empty;

            AddToClassList(ussClassName);

            m_TextField = new TextField();
            m_TextField.AddToClassList(textUssClassName);
            hierarchy.Add(m_TextField);
            m_TextField.RegisterValueChangedCallback(OnTextChanged);
            m_TextField.Q(TextField.textInputUssName).RegisterCallback<KeyDownEvent>(OnTextFieldKeyDown);

            m_SearchButton = new Button(() => {}) { name = "unity-search" };
            m_SearchButton.AddToClassList(searchButtonUssClassName);
            m_TextField.hierarchy.Add(m_SearchButton);


            m_CancelButton = new Button(() => {}) { name = "unity-cancel" };
            m_CancelButton.AddToClassList(cancelButtonUssClassName);
            m_CancelButton.AddToClassList(cancelButtonOffVariantUssClassName);
            m_TextField.hierarchy.Add(m_CancelButton);
        }

        void OnTextChanged(ChangeEvent<string> change)
        {
            value = change.newValue;
        }

        void ClearTextField()
        {
            value = String.Empty;
        }

        void OnTextFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Escape)
                ClearTextField();
        }

        void OnCancelButtonClick()
        {
            ClearTextField();
        }

        public void SetValueWithoutNotify(string newValue)
        {
            if (m_CurrentText == newValue)
                return;

            if (string.IsNullOrEmpty(m_CurrentText) && !string.IsNullOrEmpty(newValue))
            {
                m_CancelButton.RemoveFromClassList(cancelButtonOffVariantUssClassName);
                m_CancelButton.clickable.clicked += OnCancelButtonClick;
            }
            else if (!string.IsNullOrEmpty(m_CurrentText) && string.IsNullOrEmpty(newValue))
            {
                m_CancelButton.AddToClassList(cancelButtonOffVariantUssClassName);
                m_CancelButton.clickable.clicked -= OnCancelButtonClick;
            }

            m_CurrentText = newValue;
            m_TextField.value = m_CurrentText;
        }
    }
}
