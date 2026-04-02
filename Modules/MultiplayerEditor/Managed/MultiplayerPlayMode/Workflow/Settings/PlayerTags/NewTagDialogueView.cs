// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class NewTagDialogueView : VisualElement
    {
        static readonly string UXML = $"{UXMLPaths.UIRoot}/NewTagDialogueView.uxml";

        HelpBox m_ErrorBox;

        Button SaveButton => this.Q<Button>(nameof(SaveButton));
        Button CancelButton => this.Q<Button>(nameof(CancelButton));
        public TextField TagField => this.Q<TextField>(nameof(TagField));

        public Action OnSaveSelectedEvent;
        public Action OnCancelSelectedEvent;

        public NewTagDialogueView()
        {
            (EditorGUIUtility.LoadRequired(UXML) as VisualTreeAsset).CloneTree(this);

            m_ErrorBox = new HelpBox(string.Empty, HelpBoxMessageType.Error);
            m_ErrorBox.style.display = DisplayStyle.None;
            Insert(IndexOf(this.Q<VisualElement>("ButtonContainer")), m_ErrorBox);

            RegisterCallback((EventCallback<AttachToPanelEvent>)OnAttach);
            RegisterCallback((EventCallback<DetachFromPanelEvent>)OnDetach);
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            CancelButton.clicked -= OnCancelButtonClicked;
            SaveButton.clicked -= OnSaveButtonClicked;
            TagField.UnregisterCallback<KeyDownEvent>(OnTagFieldKeyDown, TrickleDown.TrickleDown);
            TagField.UnregisterValueChangedCallback(OnTagFieldValueChanged);
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            TagField.RegisterCallback<KeyDownEvent>(OnTagFieldKeyDown, TrickleDown.TrickleDown);
            TagField.RegisterValueChangedCallback(OnTagFieldValueChanged);
            SaveButton.clicked += OnSaveButtonClicked;
            CancelButton.clicked += OnCancelButtonClicked;
            TagField.Focus();
        }

        void OnTagFieldKeyDown(KeyDownEvent keyboardEvent)
        {
            if (keyboardEvent.keyCode != KeyCode.Return && keyboardEvent.keyCode != KeyCode.KeypadEnter) return;

            OnSaveSelectedEvent.Invoke();
        }

        void OnTagFieldValueChanged(ChangeEvent<string> evt) => ClearError();

        void OnCancelButtonClicked() => OnCancelSelectedEvent?.Invoke();
        void OnSaveButtonClicked() => OnSaveSelectedEvent?.Invoke();

        public void ShowError(string message)
        {
            m_ErrorBox.text = message;
            m_ErrorBox.style.display = DisplayStyle.Flex;
        }

        public void ClearError()
        {
            m_ErrorBox.text = string.Empty;
            m_ErrorBox.style.display = DisplayStyle.None;
        }
    }
}
