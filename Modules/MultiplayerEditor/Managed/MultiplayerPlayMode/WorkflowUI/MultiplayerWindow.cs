// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Unity.PlayMode.Editor;
using UnityEditor.Multiplayer.Internal;
using UnityEngine.Multiplayer.Internal;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class MultiplayerWindow : EditorWindow
    {
        public MainView MainView { get; private set; }
        private HelpBox m_DisabledHelpBox;
        private bool m_IsNarrowWindow;
        private const float k_WindowMinWidth = 275f;
        private const float k_WindowMinHeight = 400f;

        void OnFocus()
        {
            MultiplayerWindowController.ShouldUpdateUI = true;
        }

        public void CreateGUI()
        {
            if (!MultiplayerWindowController.IsVirtualProjectWorkflowInitialized)
            {
                DestroyImmediate(this);
                return;
            }

            // Set minimum window size to prevent UI overlapping issues for non-docked mppm window
            minSize = new Vector2(k_WindowMinWidth, k_WindowMinHeight);

            m_DisabledHelpBox = new HelpBox("Play Mode is currently managed by Play Mode Scenarios. Please use the dropdown next to the play button to change scenarios, " +
                                          "and use the configuration window to modify the scenario settings." + "\n" + "\n" +
                                          "To use the original Multiplayer Play Mode, select 'Default' from the scenario dropdown. When there is an active scenario selected all currently active players will be deactivated", HelpBoxMessageType.Info);
            rootVisualElement.Add(m_DisabledHelpBox);



            MainView = new MainView();
            rootVisualElement.Add(MainView);
            var currentConfig = PlayModeScenarioManager.ActiveScenario;
            MainView.SetEnabled(currentConfig.name == "Default");
            UnityPlayer[] players = MultiplayerPlaymode.Players;
            m_DisabledHelpBox.style.display = currentConfig.name == "Default" ? DisplayStyle.None : DisplayStyle.Flex;
            m_DisabledHelpBox.style.marginLeft = 6;
            m_DisabledHelpBox.style.marginRight = 16;
            m_DisabledHelpBox.style.marginTop = 8;
            m_DisabledHelpBox.style.marginBottom = 10;
            m_DisabledHelpBox.style.alignSelf = Align.Auto;
            m_DisabledHelpBox.style.height = StyleKeyword.Auto;
            m_DisabledHelpBox.style.flexShrink = 0;

            ScenarioManagerProvider.instance.ConfigAssetChanged += () =>
            {
                var newConfig = PlayModeScenarioManager.ActiveScenario;
                MainView.SetEnabled(newConfig.name == "Default");
                m_DisabledHelpBox.style.display = newConfig.name == "Default" ? DisplayStyle.None : DisplayStyle.Flex;
                foreach (var player in players)
                {
                    if (player.PlayerState is PlayerState.Launched or PlayerState.Launching && newConfig.name != "Default")
                    {
                        player.Deactivate(out _);
                    }
                }
            };
            // handles domain reload for narrow window UI
            ApplyNarrowWindowClass(position.width, true);

            // listen for window size changes to handle narrow window UI
            rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnWindowSizeChanged);

            MultiplayerWindowController.ShouldStartWindow = true;
        }

        void OnWindowSizeChanged(GeometryChangedEvent evt)
        {
            ApplyNarrowWindowClass(evt.newRect.width);
        }

        void ApplyNarrowWindowClass(float width, bool initialize = false)
        {
            var isNarrow = width < k_WindowMinWidth;
            if (!initialize && isNarrow == m_IsNarrowWindow)
                return;

            m_IsNarrowWindow = isNarrow;

            if (isNarrow)
            {
                MainView?.AddToClassList("player-view__window--narrow");
            }
            else
            {
                MainView?.RemoveFromClassList("player-view__window--narrow");
            }
        }
    }

    static class MultiplayerWindowController
    {
        const string k_Title = "Multiplayer Play Mode";
        internal const string RoleServerClient = "Client and Server";
        internal const string RoleServer = "Server";
        internal const string RoleClient = "Client";

        static MultiplayerWindow s_MultiplayerWindow;
        static readonly Dictionary<PlayerView, UnityPlayer> APIModelToViewMapping = new Dictionary<PlayerView, UnityPlayer>();

        public static bool ShouldUpdateUI;
        public static bool ShouldStartWindow;
        public static bool IsVirtualProjectWorkflowInitialized;

        internal static void ShowConfiguration()
        {
            if (IsVirtualProjectWorkflowInitialized)
            {
                ShouldStartWindow = true;
            }
            else
            {
                Debug.LogWarning("MPPM is not enabled. [Preferences->Multiplayer Play Mode]");
            }
        }

        static MultiplayerWindowController()
        {
            VirtualProjectWorkflow.OnInitialized += isMainEditor =>
            {
                if (!isMainEditor)
                {
                    MppmLog.Debug("We are a clone. No need to open the projects window.");
                    return;
                }
                IsVirtualProjectWorkflowInitialized = true;
            };
            VirtualProjectWorkflow.OnDisabled += isMainEditor =>
            {
                if (!isMainEditor)
                {
                    MppmLog.Debug("We are a clone. No need to open the projects window.");
                    return;
                }
                IsVirtualProjectWorkflowInitialized = false;
            };

            if (!IsVirtualProjectWorkflowInitialized)
                return;

            // Events that update the UI
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
            UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished += (_, _) => { ShouldUpdateUI = true; };
            EditorApplication.playModeStateChanged += _ => { ShouldUpdateUI = true; };
            MultiplayerPlaymode.PlayerTags.OnUpdated += () => { ShouldUpdateUI = true; };
            EditorMultiplayerManager.activeMultiplayerRoleChanged += () => { ShouldUpdateUI = true; };
            EditorMultiplayerManager.enableMultiplayerRolesChanged += () => { ShouldUpdateUI = true; };

            // Main window update loop
            EditorApplication.update += () =>
            {
                foreach (var (view, player) in APIModelToViewMapping)
                {
                    var roleString = player.Role switch
                    {
                        MultiplayerRoleFlags.ClientAndServer => RoleServerClient,
                        MultiplayerRoleFlags.Server => RoleServer,
                        MultiplayerRoleFlags.Client => RoleClient,
                        _ => RoleServerClient,
                    };
                    // Updates from the role if the json file change
                    // see :UpdatedDataStore for where this would have changed
                    if (view.MultiplayerRolesDropdown.value != roleString)
                    {
                        ShouldUpdateUI = true;
                        break;
                    }
                }

                if (ShouldStartWindow && IsVirtualProjectWorkflowInitialized)
                {
                    ShouldStartWindow = false;
                    Start();
                }

                if (ShouldUpdateUI && IsVirtualProjectWorkflowInitialized && s_MultiplayerWindow != null)
                {
                    ShouldUpdateUI = false;
                    Update();
                }

                if (MppmLog.AreLogsEnabled())
                {
                    foreach (var (view, player) in APIModelToViewMapping)
                    {
                        var isPlayerMain = player.Type == PlayerType.Main;
                        view.UIActivateState(isPlayerMain, (PlayerView.PlayerState)player.PlayerState, MultiplayerPlaymodeEditorUtility.IsPlayerActivateProhibited);
                    }
                }
            };
        }

        static void Start()
        {
            if (s_MultiplayerWindow == null)
            {
                s_MultiplayerWindow = EditorWindow.GetWindow<MultiplayerWindow>();
                s_MultiplayerWindow.titleContent = new GUIContent(k_Title);

                APIModelToViewMapping.Clear();

                for (var index = 0; index < MultiplayerPlaymode.Players.Length; index++)
                {
                    var player = MultiplayerPlaymode.Players[index];
                    var view = new PlayerView(player);
                    var captureIndex = index;
                    view.MultiplayerRolesDropdown.RegisterValueChangedCallback(evt =>
                    {
                        var unityPlayer = APIModelToViewMapping[view];
                        unityPlayer.Role = evt.newValue switch
                        {
                            RoleServerClient => MultiplayerRoleFlags.ClientAndServer,
                            RoleServer => MultiplayerRoleFlags.Server,
                            RoleClient => MultiplayerRoleFlags.Client,
                            _ => throw new ArgumentOutOfRangeException(),
                        };
                    });
                    view.ActiveUpdatedEvent += newValue =>
                    {
                        ShouldUpdateUI = true;

                        if (newValue)
                        {
                            if (!EditorApplication.isPlaying && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                            {
                                return;
                            }

                            if (!player.Activate(out var error))
                            {
                                if (error == ActivationError.CompileErrors)
                                {
                                    MppmLog.Warning("Cannot activate a player while there are compile errors");
                                }
                            }
                        }
                        else
                        {
                            player.Deactivate(out _);
                        }
                    };
                    view.PlayerTagDropdown.RegisterValueChangedCallback(evt =>
                    {
                        Debug.Assert(player.Tags != null, "Tags should never be null");

                        var newValue = evt.newValue;
                        if (newValue == PlayerView.TagDefault) return;
                        if (newValue == PlayerView.TagLineBreak) return;

                        if (newValue != PlayerView.TagCreateTag)
                        {
                            if (!player.AddTag(newValue, out var tagError))
                            {
                                switch (tagError)
                                {
                                    case TagError.InPlayMode:
                                        MppmLog.Warning("Cannot modify tag while player is active in Play Mode");
                                        break;
                                    case TagError.Duplicate:
                                        MppmLog.Warning($"Attempting to add tag \"{newValue}\" to a player that already have \"{newValue}\" assigned to the player.");
                                        break;
                                    case TagError.None:
                                    case TagError.DoesNotExist:
                                    case TagError.Empty:
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }
                            }
                        }
                        else
                        {
                            var result = StandardMainEditorWorkflow.TryOpenProjectSettingsWindow("Project/Multiplayer/Playmode");
                            Debug.Assert(result, "Could not open project settings window.");
                        }

                        view.PlayerTagDropdown.SetValueWithoutNotify(PlayerView.TagDefault);

                        ShouldUpdateUI = true;
                    });
                    view.PillCloseEvent += tagEntry =>
                    {
                        if (!player.RemoveTag(tagEntry, out var tagError))
                        {
                            switch (tagError)
                            {
                                case TagError.DoesNotExist:
                                    MppmLog.Warning($"Attempting to remove tag \"{tagEntry}\" from a player without \"{tagEntry}\" assigned to the player.");
                                    break;
                                case TagError.None:
                                case TagError.InPlayMode:
                                case TagError.Duplicate:
                                case TagError.Empty:
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        var canModifyTag1 = player.PlayerState is not (PlayerState.Launched or PlayerState.Launching) || !EditorApplication.isPlaying;
                        view.RepopulateTagsAndPills(MultiplayerPlaymode.PlayerTags.Tags, player.Tags, canModifyTag1);
                        ShouldUpdateUI = true;
                    };

                    player.OnPlayerCommunicative += () =>
                    {
                        ShouldUpdateUI = true;
                    };

                    var isPlayerActive = player.PlayerState is PlayerState.Launched or PlayerState.Launching;
                    var isPlayerMainEditor = player.Type == PlayerType.Main;

                    if (isPlayerMainEditor)
                    {
                        view.AddToClassList(PlayerView.ClassNames.mainEditorPlayer);
                    }
                    else
                    {
                        view.PlayerViewContent.AddManipulator(new ContextualMenuManipulator(populateEvent =>
                        {
                            PlayerContextMenuOptions(populateEvent, player, captureIndex);
                        }));

                        var t = new ContextualMenuManipulator(populateEvent =>
                        {
                            AnalyticsOnTreeDotsClickedEvent.Send(new OnThreeDotsClickedData()
                            {
                                IsPlayMode = EditorApplication.isPlaying,
                            });

                            PlayerContextMenuOptions(populateEvent, player, captureIndex);
                        });
                        t.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
                        view.EllipsesContainer.AddManipulator(t);
                        view.EllipseIcon.AddToClassList("ellipse-icon");
                        view.EllipsesContainer.Add(view.EllipseIcon);
                    }

                    // Format Name Foldout
                    view.NameToggle.text = player.Name;
                    view.NameToggle.value = isPlayerActive;

                    var checkMark = view.NameToggle.Q<VisualElement>("unity-checkmark");
                    var spacing = new VisualElement { style = { minWidth = 24, }, };
                    view.RefreshEditorIconBackgroundImage(isPlayerMainEditor);
                    view.EditorIcon.AddToClassList("editorIcon");
                    var foldoutComponents = new VisualElement
                    {
                        name = "foldout-components",
                        style = { flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row), },
                    };
                    foldoutComponents.Add(spacing);
                    foldoutComponents.Add(view.EditorIcon);
                    checkMark.parent.Insert(1, foldoutComponents);
                    view.NameToggle.SetEnabled(!MultiplayerPlaymodeEditorUtility.IsPlayerActivateProhibited || isPlayerActive);
                    var canModifyTag = player.PlayerState is not (PlayerState.Launched or PlayerState.Launching) ||
                                       !EditorApplication.isPlaying;
                    var logs = MultiplayerPlaymodeLogUtility.PlayerLogs(player.PlayerIdentifier).LogCounts;
                    view.RefreshEditorIconBackgroundImage(isPlayerMainEditor);
                    view.RepopulateTagsAndPills(MultiplayerPlaymode.PlayerTags.Tags, player.Tags, canModifyTag);
                    view.UILogCounts(isPlayerMainEditor, logs.Logs.ToString(), logs.Warnings.ToString(), logs.Errors.ToString());
                    view.UIActivateState(isPlayerMainEditor, (PlayerView.PlayerState)player.PlayerState, MultiplayerPlaymodeEditorUtility.IsPlayerActivateProhibited);
                    view.MultiplayerRolesDropdown.style.display = EditorMultiplayerManager.enableMultiplayerRoles
                            ? DisplayStyle.Flex
                            : DisplayStyle.None;
                    view.MultiplayerRolesDropdown.SetValueWithoutNotify(isPlayerMainEditor
                        ? RoleServer
                        : RoleClient);
                    view.MultiplayerRolesDropdown.choices = new List<string>
                    {
                        RoleClient,
                        RoleServer,
                        RoleServerClient,
                    };

                    if (isPlayerMainEditor)
                    {
                        s_MultiplayerWindow.MainView.MainListView.Add(view);
                    }
                    else if (player.Type == PlayerType.Clone)
                    {
                        s_MultiplayerWindow.MainView.VirtualListView.Add(view);
                    }

                    APIModelToViewMapping.Add(view, player); // bind our view to the api model
                }
            }
            else
            {
                s_MultiplayerWindow.Focus();
            }
        }

        static void PlayerContextMenuOptions(ContextualMenuPopulateEvent populateEvent, UnityPlayer player, int index)
        {
            var openInExplorerContextualMenuLabel = Application.platform switch
            {
                RuntimePlatform.WindowsEditor => "Open in Explorer",
                RuntimePlatform.OSXEditor => "Show in Finder",
                _ => "Open Directory",
            };

            populateEvent.menu.AppendAction(
                openInExplorerContextualMenuLabel,
                _ =>
                {
                    AnalyticsThreeDotsSelectedEvent.Send(new ThreeDotsSelectedData()
                    {
                        IsPlayMode = EditorApplication.isPlaying,
                        OptionSelected = "ShowInFinder"
                    });

                    MultiplayerPlaymodeEditorUtility.RevealInFinder(player);
                },
                DropdownMenuAction.AlwaysEnabled);
            populateEvent.menu.AppendAction(
                "Focus on Player",
                _ =>
                {
                    AnalyticsThreeDotsSelectedEvent.Send(new ThreeDotsSelectedData()
                    {
                        IsPlayMode = EditorApplication.isPlaying,
                        OptionSelected = "FocusOnPlayer"
                    });

                    var err = MultiplayerPlaymodeEditorUtility.FocusPlayerView((PlayerIndex)index + 1);
                    if (err != default)
                    {
                        MppmLog.Debug(err == MultiplayerPlaymodeEditorUtility.FocusPlayerStatus.PlayerNotFound
                            ? $"Failed to find project for {player.Name} using {index}"
                            : $"Failed to open the window {err}");
                    }
                },
                DropdownMenuAction.AlwaysEnabled);
        }

        static void Update()
        {
            var showPlayerLaunchingHelpBox = false;
            foreach (var (view, player) in APIModelToViewMapping)
            {
                // double check log counts. this was broken and this probably fixes it
                var canModifyTag = player.PlayerState is not (PlayerState.Launched or PlayerState.Launching) ||
                                   !EditorApplication.isPlaying;

                var logs = MultiplayerPlaymodeLogUtility.PlayerLogs(player.PlayerIdentifier).LogCounts;
                var isPlayerMain = player.Type == PlayerType.Main;
                view.RefreshEditorIconBackgroundImage(isPlayerMain);
                view.RepopulateTagsAndPills(MultiplayerPlaymode.PlayerTags.Tags, player.Tags, canModifyTag);
                view.UILogCounts(isPlayerMain, logs.Logs.ToString(), logs.Warnings.ToString(), logs.Errors.ToString());
                view.UIActivateState(isPlayerMain, (PlayerView.PlayerState)player.PlayerState, MultiplayerPlaymodeEditorUtility.IsPlayerActivateProhibited);

                showPlayerLaunchingHelpBox = showPlayerLaunchingHelpBox || (!isPlayerMain && player.PlayerState is PlayerState.Launching);
                var role = player.Role switch
                {
                    MultiplayerRoleFlags.ClientAndServer => RoleServerClient,
                    MultiplayerRoleFlags.Server => RoleServer,
                    MultiplayerRoleFlags.Client => RoleClient,
                    _ => RoleServerClient,
                };
                view.MultiplayerRolesDropdown.style.display = EditorMultiplayerManager.enableMultiplayerRoles
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                view.MultiplayerRolesDropdown.SetValueWithoutNotify(role);
            }

            if (MultiplayerPlaymodeEditorUtility.IsPlayerActivateProhibited)
            {
                s_MultiplayerWindow.MainView.AddToClassList(MainView.k_HasCompileErrorsClassName);
            }
            else
            {
                s_MultiplayerWindow.MainView.RemoveFromClassList(MainView.k_HasCompileErrorsClassName);
            }

            if (!MultiplayerPlayModeSettings.GetIsMppmActive())
            {
                s_MultiplayerWindow.MainView.AddToClassList(MainView.k_HasMPPMDisabled);
            }
            else
            {
                s_MultiplayerWindow.MainView.RemoveFromClassList(MainView.k_HasMPPMDisabled);
            }

            if (showPlayerLaunchingHelpBox)
            {
                s_MultiplayerWindow.MainView.AddToClassList(MainView.k_HasPlayerLaunchingClassName);
            }
            else
            {
                s_MultiplayerWindow.MainView.RemoveFromClassList(MainView.k_HasPlayerLaunchingClassName);
            }
        }

        static void OnSceneChanged(Scene _, Scene __)
        {
            foreach (var (view, player) in APIModelToViewMapping)
            {
                view.RefreshEditorIconBackgroundImage(player.Type == PlayerType.Main);
            }
        }
        [Shortcut("Focus Player One", KeyCode.F9, ShortcutModifiers.Control)]
        public static void FocusPlayerOne()
        {
            _ = MultiplayerPlaymodeEditorUtility.FocusPlayerView(PlayerIndex.Player1);
        }
        [Shortcut("Focus Player Two", KeyCode.F10, ShortcutModifiers.Control)]
        public static void FocusPlayerTwo()
        {
            _ = MultiplayerPlaymodeEditorUtility.FocusPlayerView(PlayerIndex.Player2);
        }
        [Shortcut("Focus Player Three", KeyCode.F11, ShortcutModifiers.Control)]
        public static void FocusPlayerThree()
        {
            _ = MultiplayerPlaymodeEditorUtility.FocusPlayerView(PlayerIndex.Player3);
        }
        [Shortcut("Focus Player Four", KeyCode.F12, ShortcutModifiers.Control)]
        public static void FocusPlayerFour()
        {
            _ = MultiplayerPlaymodeEditorUtility.FocusPlayerView(PlayerIndex.Player4);
        }
    }
}
