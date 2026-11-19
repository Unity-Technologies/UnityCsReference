// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.PlayMode.Editor;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class ConvertMPPMToScenarioHelpBox : HelpBox
    {
        internal const string k_HelpBoxName = "scenario-recommendation-help-box";
        internal const string k_TryButtonName = "scenario-recommendation-try-button";

        static readonly string k_ConvertedScenarioBaseName = L10n.Tr("Multiplayer Play Mode", null);
        static readonly string k_MessageText = L10n.Tr("Multiplayer Play Mode will be leaving in Unity 7.1. The replacement solution is Play Mode Scenarios. You can convert your current configuration to a scenario and continue testing multiplayer in the editor.", null);
        internal static readonly string k_TryScenariosLabel = L10n.Tr("Use Play Mode Scenarios", null);
        internal const string k_LearnMoreLinkName = "scenario-recommendation-learn-more-link";
        internal const string k_LearnMoreLinkClass = "scenario-recommendation__learn-more";
        static readonly string s_UssPath = $"{UXMLPaths.UIRoot}/ConvertMPPMToScenarioHelpBox.uss";
        internal const string k_LearnMoreUrl = "https://docs.unity3d.com/Packages/com.unity.multiplayer.playmode@3.0/manual/play-mode-scenario/play-mode-scenario-create.html";
        static readonly string k_LearnMoreLabel = L10n.Tr("Learn more", null);

        readonly EditorWindow m_Host;
        readonly Button m_TryButton;

        public ConvertMPPMToScenarioHelpBox(EditorWindow host)
            : base(
                k_MessageText,
                HelpBoxMessageType.Warning)
        {
            m_Host = host;

            name = k_HelpBoxName;

            if (EditorGUIUtility.LoadRequired(s_UssPath) is StyleSheet styleSheet)
                styleSheets.Add(styleSheet);
            else
                Debug.LogError($"Failed to load USS stylesheet at path: {s_UssPath}");

            var buttonsContainer = new VisualElement();
            m_TryButton = new Button(TryScenariosAction) { text = k_TryScenariosLabel, name = k_TryButtonName };
            buttonsContainer.Add(m_TryButton);
            buttonsContainer.Add(CreateLearnMoreLink());
            Add(buttonsContainer);

            buttonsContainer.style.flexDirection = FlexDirection.Row;
            buttonsContainer.style.alignSelf = Align.FlexEnd;
            buttonsContainer.style.marginTop = 8;
            style.marginLeft = 6;
            style.marginRight = 16;
            style.marginTop = 8;
            style.marginBottom = 10;
            style.alignSelf = Align.Auto;
            style.height = StyleKeyword.Auto;
            style.flexShrink = 0;

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                ScenarioManagerProvider.instance.StateChanged -= OnScenarioStateChanged;
                ScenarioManagerProvider.instance.StateChanged += OnScenarioStateChanged;
                UpdateVisibility();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => ScenarioManagerProvider.instance.StateChanged -= OnScenarioStateChanged);

            UpdateVisibility();
        }

        void OnScenarioStateChanged(PlayModeScenarioState _) => UpdateVisibility();

        static Label CreateLearnMoreLink()
        {
            // A Label rather than a Button so it reads as a hyperlink, not a second call to action.
            // The colour comes from the theme's link variable in the stylesheet, so it follows the Editor skin.
            // TextElement sets tabIndex to -1, which keeps it out of Tab navigation even when focusable.
            var link = new Label($"<u>{k_LearnMoreLabel}</u>") { name = k_LearnMoreLinkName, tooltip = k_LearnMoreUrl, focusable = true, tabIndex = 0 };
            link.AddToClassList(k_LearnMoreLinkClass);
            link.AddManipulator(new Clickable(() => Help.BrowseURL(k_LearnMoreUrl)));
            // Clickable only reacts to the pointer; this lets keyboard users open the link too.
            link.RegisterCallback<NavigationSubmitEvent>(_ => Help.BrowseURL(k_LearnMoreUrl));
            return link;
        }

        void TryScenariosAction()
        {
            ConvertAndSwap(m_Host);
        }

        internal void UpdateVisibility()
        {
            style.display = ShouldShowRecommendation() ? DisplayStyle.Flex : DisplayStyle.None;

            m_TryButton.SetEnabled(CanConvert);
        }

        // Activating the converted scenario is rejected while the active scenario is starting, running or stopping
        // (which includes the Default scenario during Play Mode), so conversion is only offered when it can complete.
        internal static bool CanConvert => PlayModeScenarioManager.CanChangeActiveScenario;

        internal static bool ShouldShowRecommendation()
        {
            return IsDefaultScenarioActive();
        }

        static bool IsDefaultScenarioActive()
        {
            var active = PlayModeScenarioManager.ActiveScenario;
            return active == null || active.name == "Default";
        }

        public static void ConvertAndSwap(EditorWindow host)
        {
            ConvertActiveConfigToScenario(host);
        }

        /// <param name="host">Window to close once the scenario is created, or null to keep it open.</param>
        /// <param name="interactiveRename">
        /// When true and a Project window exists, the asset is only created after the user confirms its name there.
        /// Pass false to create it immediately with the default unique name.
        /// </param>
        /// <param name="readLegacyPlayers">
        /// When false, the MPPM players are treated as unavailable and the default list is converted.
        /// Lets tests cover that fallback whether or not MPPM has loaded its players in this editor.
        /// </param>
        internal static OrchestratedScenario ConvertActiveConfigToScenario(EditorWindow host = null, bool interactiveRename = true, bool readLegacyPlayers = true)
        {
            var players = (readLegacyPlayers ? ReadCurrentPlayers() : null) ?? DefaultPlayers();

            EnsureAssetFolderExists(PlayModeScenarioUtils.k_ConfigAssetsPath);

            // Resolve the name before creating the instance so the scenario cache is not rebuilt
            // with the unsaved, nameless scenario in it.
            var uniqueName = MakeUniqueScenarioName(k_ConvertedScenarioBaseName);
            var path = $"{PlayModeScenarioUtils.k_ConfigAssetsPath}/{uniqueName}.asset";

            var scenario = ScriptableObject.CreateInstance<OrchestratedScenario>();
            PopulateFromPlayers(scenario, players);

            var endAction = ScriptableObject.CreateInstance<DoCreateConvertedScenario>();
            endAction.Host = host;

            if (interactiveRename)
            {
                ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                    scenario.GetEntityId(),
                    endAction,
                    path,
                    scenario.Icon,
                    null);
            }
            else
            {
                endAction.Action(scenario.GetEntityId(), path, null);
                ScriptableObject.DestroyImmediate(endAction);
            }

            return scenario;
        }

        internal static void PopulateFromPlayers(OrchestratedScenario scenario, IReadOnlyList<PlayerConfig> players)
        {
            ref var settings = ref scenario.Settings;

            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player.IsMain)
                {
                    UpdateMainEditor(ref settings, player);
                }
                else
                {
                    settings.AddInstance<CloneEditorController, CloneEditorController.InstanceSettings>(
                        player.Name,
                        new CloneEditorController.InstanceSettings
                        {
                            PlayerTag = FirstTag(player.Tags),
                            RoleMask = player.Role,
                            PlayerInstanceIndex = i,
                        });
                }
            }
        }

        static void UpdateMainEditor(ref OrchestratedScenarioSettings settings, PlayerConfig main)
        {
            var mainSettings = new MainEditorController.InstanceSettings
            {
                PlayerTag = FirstTag(main.Tags),
                RoleMask = main.Role,
            };

            for (var i = 0; i < settings.InstanceCount; i++)
            {
                if (!settings[i].IsInstanceType(typeof(MainEditorController)))
                    continue;

                settings.SetInstanceSettings(settings[i].GetId(), mainSettings);
                return;
            }

            settings.AddInstance<MainEditorController, MainEditorController.InstanceSettings>("Main Editor", mainSettings);
        }

        static IReadOnlyList<PlayerConfig> ReadCurrentPlayers()
        {
            var players = MultiplayerPlaymode.Players;
            if (players == null)
                return null;

            var configs = new List<PlayerConfig>(players.Length);
            foreach (var player in players)
                configs.Add(new PlayerConfig(player.Name, player.Tags, player.Role, player.Type == PlayerType.Main, player.IsActive));

            var selected = SelectPlayersToConvert(configs);
            NormalizeServerRoles(selected);
            return selected;
        }

        // Scenario instances have no "disabled" state, so slots the user never activated are
        // left out instead of becoming extra clone editors. The main editor is always kept.
        internal static List<PlayerConfig> SelectPlayersToConvert(IReadOnlyList<PlayerConfig> players)
        {
            var selected = new List<PlayerConfig>(players.Count);
            foreach (var player in players)
            {
                if (player.IsMain || player.IsActive)
                    selected.Add(player);
            }

            return selected;
        }

        internal static void NormalizeServerRoles(IList<PlayerConfig> players)
        {
            var hasServer = false;
            for (var i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (!player.Role.HasFlag(MultiplayerRoleFlags.Server))
                    continue;

                if (hasServer)
                    players[i] = player.WithRole(MultiplayerRoleFlags.Client);

                hasServer = true;
            }
        }

        static void EnsureAssetFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            var parts = folderPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }
        }

        static IReadOnlyList<PlayerConfig> DefaultPlayers()
        {
            return new[]
            {
                new PlayerConfig("Player 1", Array.Empty<string>(), MultiplayerRoleFlags.Client, isMain: true),
                new PlayerConfig("Player 2", Array.Empty<string>(), MultiplayerRoleFlags.Client, isMain: false),
                new PlayerConfig("Player 3", Array.Empty<string>(), MultiplayerRoleFlags.Client, isMain: false),
                new PlayerConfig("Player 4", Array.Empty<string>(), MultiplayerRoleFlags.Client, isMain: false),
            };
        }

        static void SwapToScenarioWindow(EditorWindow host)
        {
            if (host == null)
                return;

            EditorWindow.GetWindow<ActiveScenarioWindow>(host.GetType());
            EditorApplication.delayCall += () =>
            {
                if (host != null)
                    host.Close();
            };
        }

        class DoCreateConvertedScenario : AssetCreationEndAction
        {
            public EditorWindow Host;

            public override void Action(EntityId entityId, string pathName, string resourceFile)
            {
                if (EditorUtility.EntityIdToObject(entityId) is not OrchestratedScenario scenario)
                    return;

                // Pressing Play while the name is being edited commits the name on ExitingEditMode,
                // before the Default scenario leaves Idle, so CanConvert alone does not catch it.
                if (!CanConvert || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    DestroyImmediate(scenario);
                    PlayModeScenarioUtils.NotifyScenariosChanged();
                    return;
                }

                AssetDatabase.CreateAsset(scenario, AssetDatabase.GenerateUniqueAssetPath(pathName));
                AssetDatabase.SaveAssets();
                PlayModeScenarioUtils.NotifyScenariosChanged();

                PlayModeScenarioManager.ActiveScenario = scenario;
                SwapToScenarioWindow(Host);
            }

            public override void Cancelled(EntityId entityId, string pathName, string resourceFile)
            {
                if (EditorUtility.EntityIdToObject(entityId) is not OrchestratedScenario scenario)
                    return;

                DestroyImmediate(scenario);
                PlayModeScenarioUtils.NotifyScenariosChanged();
            }
        }

        static string MakeUniqueScenarioName(string desiredName)
        {
            var allConfigs = PlayModeScenarioUtils.GetAllConfigs();
            var existingNames = new string[allConfigs.Count];
            for (var i = 0; i < allConfigs.Count; i++)
                existingNames[i] = allConfigs[i].name;
            return ObjectNames.GetUniqueName(existingNames, desiredName);
        }

        static string FirstTag(string[] tags) => tags is { Length: > 0 } ? tags[0] : string.Empty;

        internal readonly struct PlayerConfig
        {
            public readonly string Name;
            public readonly string[] Tags;
            public readonly MultiplayerRoleFlags Role;
            public readonly bool IsMain;
            public readonly bool IsActive;

            public PlayerConfig(string name, string[] tags, MultiplayerRoleFlags role, bool isMain, bool isActive = true)
            {
                Name = name;
                Tags = tags;
                Role = role;
                IsMain = isMain;
                IsActive = isActive;
            }

            public PlayerConfig WithRole(MultiplayerRoleFlags role) => new PlayerConfig(Name, Tags, role, IsMain, IsActive);
        }
    }
}
