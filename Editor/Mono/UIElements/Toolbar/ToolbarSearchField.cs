// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    public class ToolbarSearchField : VisualElement, INotifyValueChanged<string>
    {
        public new class UxmlFactory : UxmlFactory<ToolbarSearchField> {}

        const string k_ClassName = "toolbarSearchField";

        const string k_SearchButtonClassName = "toolbarSearchFieldButton";

        const string k_EmptyEndClassName = "toolbarSearchFieldEnd";
        const string k_CancelButtonEndClassName = "toolbarSearchFieldCancelButton";

        protected Button m_SearchButton;
        Button m_CancelButton;
        TextField m_TextField;

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

        public ToolbarSearchField() :
            this(k_SearchButtonClassName)
        {
            Toolbar.SetToolbarStyleSheet(this);
        }

        protected ToolbarSearchField(string searchButtonStyleClassName)
        {
            m_CurrentText = String.Empty;

            AddToClassList(k_ClassName);

            m_SearchButton = new Button(() => {});
            m_SearchButton.AddToClassList(searchButtonStyleClassName);
            shadow.Add(m_SearchButton);

            m_TextField = new TextField();
            m_SearchButton.shadow.Add(m_TextField);
            m_TextField.OnValueChanged(OnTextChanged);
            m_TextField.RegisterCallback<KeyDownEvent>(OnTextFieldKeyDown);

            m_CancelButton = new Button(() => {});
            m_CancelButton.AddToClassList(k_EmptyEndClassName);
            shadow.Add(m_CancelButton);
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
                m_CancelButton.RemoveFromClassList(k_EmptyEndClassName);
                m_CancelButton.AddToClassList(k_CancelButtonEndClassName);
                m_CancelButton.clickable.clicked += OnCancelButtonClick;
            }
            else if (!string.IsNullOrEmpty(m_CurrentText) && string.IsNullOrEmpty(newValue))
            {
                m_CancelButton.AddToClassList(k_EmptyEndClassName);
                m_CancelButton.RemoveFromClassList(k_CancelButtonEndClassName);
                m_CancelButton.clickable.clicked -= OnCancelButtonClick;
            }

            m_CurrentText = newValue;
            m_TextField.value = m_CurrentText;
        }

        [Obsolete("This method is replaced by simply using this.value. The default behaviour has been changed to notify when changed. If the behaviour is not to be notified, SetValueWithoutNotify() must be used.", false)]
        public void SetValueAndNotify(string newValue)
        {
        }

        public void OnValueChanged(EventCallback<ChangeEvent<string>> callback)
        {
            RegisterCallback(callback);
        }

        public void RemoveOnValueChanged(EventCallback<ChangeEvent<string>> callback)
        {
            UnregisterCallback(callback);
        }
    }
}
