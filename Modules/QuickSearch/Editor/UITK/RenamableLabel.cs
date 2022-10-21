// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    class RenamableLabel : VisualElement
    {
        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public static readonly string ussClassName = "search-renamable-label";

        Label m_Label;
        TextField m_TextField;

        public string text
        {
            get => m_Label.text;
            set => m_Label.text = value;
        }

        public event Action<string> renameFinished;

        public RenamableLabel()
            : this(string.Empty) { }

        public RenamableLabel(string text)
        {
            AddToClassList(ussClassName);

            m_Label = new Label(text);
            Add(m_Label);
        }

        public void StartRename()
        {
            SetElementDisplay(m_Label, false);
            m_TextField = CreateNewTextField();
            m_TextField.SetValueWithoutNotify(m_Label.text);
            m_TextField.RegisterValueChangedCallback(HandleTextFieldValueChanged);
            m_TextField.RegisterCallback<BlurEvent>(HandleBlurEvent);
            Add(m_TextField);
            m_TextField.Focus();
            m_TextField.SelectAll();
        }

        public void StopRename()
        {
            m_TextField.UnregisterValueChangedCallback(HandleTextFieldValueChanged);
            m_TextField.UnregisterCallback<BlurEvent>(HandleBlurEvent);
            Remove(m_TextField);
            SetElementDisplay(m_Label, true);
        }

        void HandleBlurEvent(BlurEvent evt)
        {
            AcceptRename(m_TextField.text);
        }

        void HandleTextFieldValueChanged(ChangeEvent<string> evt)
        {
            AcceptRename(evt.newValue);
        }

        void AcceptRename(string newName)
        {
            text = Utils.Simplify(newName);
            StopRename();
            SendRenameFinishedEvent();
        }

        void SendRenameFinishedEvent()
        {
            renameFinished?.Invoke(text);
        }

        static void SetElementDisplay(VisualElement element, bool value)
        {
            if (element == null)
                return;

            element.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            element.style.visibility = value ? Visibility.Visible : Visibility.Hidden;
        }

        static TextField CreateNewTextField()
        {
            var textField = new TextField
            {
                multiline = false,
                isDelayed = true
            };
            return textField;
        }
    }
}
