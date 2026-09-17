// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class PlayerTagsView : VisualElement
    {
        internal const string k_PlayModeHelpText = "Player Tags cannot be modified while in Play Mode.";

        static readonly string UXML = $"{UXMLPaths.UIRoot}/PlayerTagsView.uxml";
        internal ListView PlayerTagsList => this.Q<ListView>(nameof(PlayerTagsList));
        Button AddTagButton => this.Q<Button>(nameof(AddTagButton));
        Button RemoveTagButton => this.Q<Button>(nameof(RemoveTagButton));
        internal HelpBox PlayModeHelpBox { get; }

        bool m_Editable = true;

        public event Action AddTagEvent;
        public event Action RemoveTagEvent;

        public PlayerTagsView()
        {
            (EditorGUIUtility.LoadRequired(UXML) as VisualTreeAsset).CloneTree(this);
            AddTagButton.clicked += () => AddTagEvent?.Invoke();
            RemoveTagButton.clicked += () =>
            {
                RemoveTagEvent?.Invoke();
                PlayerTagsList.ClearSelection();
            };

            PlayModeHelpBox = new HelpBox(k_PlayModeHelpText, HelpBoxMessageType.Info);
            PlayModeHelpBox.style.display = DisplayStyle.None;
            Insert(1, PlayModeHelpBox);

            RemoveTagButton.SetEnabled(false);
            PlayerTagsList.selectionChanged += _ => UpdateRemoveButtonEnabled();
            PlayerTagsList.itemsSourceChanged += UpdateRemoveButtonEnabled;

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
                SetEditable(!EditorApplication.isPlayingOrWillChangePlaymode);
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            });
        }

        void OnPlayModeStateChanged(PlayModeStateChange _) =>
            SetEditable(!EditorApplication.isPlayingOrWillChangePlaymode);

        internal void SetEditable(bool editable)
        {
            m_Editable = editable;
            AddTagButton.SetEnabled(editable);
            UpdateRemoveButtonEnabled();
            PlayModeHelpBox.style.display = editable ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void UpdateRemoveButtonEnabled()
        {
            var src = PlayerTagsList.itemsSource;
            RemoveTagButton.SetEnabled(m_Editable
                && src != null
                && src.Count > 0
                && PlayerTagsList.selectedIndex >= 0
                && PlayerTagsList.selectedIndex < src.Count);
        }

        public static void BindData(PlayerTagsView playerTagsView, UnityPlayerTags playerTagsViewModel)
        {
            playerTagsView.PlayerTagsList.itemsSource = playerTagsViewModel.Tags;
            playerTagsView.PlayerTagsList.bindItem += (element, i) => { ((Label)element).text = playerTagsViewModel.Tags[i]; };
            playerTagsView.PlayerTagsList.makeItem = () =>
            {
                var label = new Label();
                label.AddToClassList("player-tag");
                return label;
            };
        }
    }
}
