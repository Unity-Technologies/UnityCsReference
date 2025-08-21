// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class ProjectSettingsProvider : SettingsProvider
    {
        const string k_OperationsWhileInPlayMode = "Can't perform operations on Players Tags while in play mode";

        [SettingsProvider]
        public static SettingsProvider CreateMultiplayerPlaymodeSettingsProvider()
        {
            if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                return null;

            return new ProjectSettingsProvider(PlaymodeSettingsSection.k_SettingsPath, SettingsScope.Project);
        }

        ProjectSettingsProvider(string path, SettingsScope scopes) : base(path, scopes) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var settingsView = new PlaymodeSettingsSection();

            PlayerTagsView.BindData(settingsView.PlayerTagsView, MultiplayerPlaymode.PlayerTags);

            var numViews = 0;
            settingsView.PlayerTagsView.AddTagEvent += () =>
            {
                // Prevent multiple add tag windows from appearing
                if (numViews > 0)
                {
                    return;
                }

                numViews++;
                var tagDialogueView = new NewTagDialogueView();
                tagDialogueView.OnCancelSelectedEvent += () =>
                {
                    numViews--;
                    settingsView.PlayerTagsView.Remove(tagDialogueView);
                };
                tagDialogueView.OnSaveSelectedEvent += () =>
                {
                    var tag = tagDialogueView.TagField.value;
                    tag = tag.Replace('/', ' ').Trim();  // We don't want more layers of drop downs or to deal with serialization issues... as well as empty space
                    tagDialogueView.TagField.SetValueWithoutNotify(tag);
                    if (!string.IsNullOrWhiteSpace(tag) && !MultiplayerPlaymode.PlayerTags.Contains(tag))
                    {
                        if (!MultiplayerPlaymode.PlayerTags.Add(tag, out var tagError))
                        {
                            MppmLog.Error($"Error: {tagError}");
                        }

                        numViews--;
                        settingsView.PlayerTagsView.Remove(tagDialogueView);

                        settingsView.PlayerTagsView.PlayerTagsList.itemsSource = MultiplayerPlaymode.PlayerTags.Tags;
                        settingsView.PlayerTagsView.PlayerTagsList.RefreshItems();
                    }
                };

                settingsView.PlayerTagsView.Add(tagDialogueView);
            };
            settingsView.PlayerTagsView.RemoveTagEvent += () =>
            {
                if (settingsView.PlayerTagsView.PlayerTagsList.selectedItem is string tag)
                {
                    if (!MultiplayerPlaymode.PlayerTags.Remove(tag, out var playersWhoLostTags, out var tagError))
                    {
                        switch (tagError)
                        {
                            case TagError.InPlayMode:
                                MppmLog.Warning(k_OperationsWhileInPlayMode);
                                break;
                            case TagError.DoesNotExist:
                                MppmLog.Warning($"This list of tags was {string.Join(",", MultiplayerPlaymode.PlayerTags.Tags)} but attempted to delete '{tag}'.");
                                break;
                            default:
                                MppmLog.Error($"Error: {tagError}");
                                break;
                        }
                    }
                    else
                    {
                        foreach (var player in MultiplayerPlaymode.Players)
                        {
                            foreach (var p in playersWhoLostTags)
                            {
                                if (player.PlayerIdentifier == p)
                                {
                                    MppmLog.Warning($"Tag '{tag}' has been removed from {player.Name}");
                                    break;
                                }
                            }
                        }
                    }
                }

                settingsView.PlayerTagsView.PlayerTagsList.itemsSource = MultiplayerPlaymode.PlayerTags.Tags;
                settingsView.PlayerTagsView.PlayerTagsList.RefreshItems();
            };
            rootElement.Add(settingsView);
        }
    }

    class PlaymodeSettingsSection : VisualElement
    {
        static readonly string UXML = $"{UXMLPaths.UIRoot}/PlaymodeSettingsSection.uxml";
        public const string k_SettingsPath = "Project/Multiplayer/Playmode";

        public readonly PlayerTagsView PlayerTagsView;

        public PlaymodeSettingsSection()
        {
            (EditorGUIUtility.LoadRequired(UXML) as VisualTreeAsset).CloneTree(this);

            PlayerTagsView = new PlayerTagsView();
            this.Q<ScrollView>().Add(PlayerTagsView);
        }
    }
}
