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
        static readonly string UXML = $"{UXMLPaths.UIRoot}/PlayerTagsView.uxml";
        internal ListView PlayerTagsList => this.Q<ListView>(nameof(PlayerTagsList));
        Button AddTagButton => this.Q<Button>(nameof(AddTagButton));
        Button RemoveTagButton => this.Q<Button>(nameof(RemoveTagButton));

        public event Action AddTagEvent;
        public event Action RemoveTagEvent;

        public PlayerTagsView()
        {
            (EditorGUIUtility.LoadRequired(UXML) as VisualTreeAsset).CloneTree(this);
            AddTagButton.clicked += () => AddTagEvent?.Invoke();
            RemoveTagButton.clicked += () => RemoveTagEvent?.Invoke();
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
