// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Overlays;
using UnityEngine.Bindings;

namespace UnityEditor.Toolbars
{
    [VisibleToOtherModules("UnityEditor.PlayModeModule")]
    sealed partial class PlayModeButtons : ScriptableSingleton<PlayModeButtons>
    {
        const string k_ElementId = "Play Mode Controls";
        const float k_ImguiOverrideWidth = 240f;

        bool m_IsAvailable = true;
        bool m_HasImguiOverride = false;

        [VisibleToOtherModules("UnityEditor.PlayModeModule")]
        [AutoStaticsCleanupOnCodeReload] internal static event Action<VisualElement> onPlayModeButtonsCreated;

        [UnityOnlyMainToolbarPreset]
        [MainToolbarElement(k_ElementId, ussName = "PlayMode", defaultDockIndex = 0,
                            defaultDockPosition = MainToolbarDockPosition.Middle, menuPriority = MainToolbarElementAttribute.defaultMenuPriority - 1)]
        static IEnumerable<MainToolbarElement> Create()
        {
            return instance.Build();
        }

        void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            ModeService.modeChanged += OnModeChanged;
            // Monitor for layout changes
            WindowLayout.lastLoadedLayoutChanged += OnLayoutChanged;

            //Immediately after a domain reload, Modes might be initialized after the toolbar so we wait a frame to check it
            EditorApplication.delayCall += () =>
            {
                CheckAvailability();
                CheckImguiOverride();
                EnsureOverlaySubscription();
            };
        }
        
        void OnLayoutChanged()
        {
            EditorApplication.delayCall += EnsureOverlaySubscription;
        }
        
        void EnsureOverlaySubscription()
        {
            if (MainToolbar.TryGetOverlay(k_ElementId, out var overlay) && overlay is MainToolbarOverlay mtOverlay)
            {
                mtOverlay.afterContentRebuilt += OnElementRebuilt;
                OnElementRebuilt(mtOverlay.rootVisualElement.Q<OverlayToolbar>());
            }
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            ModeService.modeChanged -= OnModeChanged;

            if (MainToolbar.TryGetOverlay(k_ElementId, out var overlay) && overlay is MainToolbarOverlay mtOverlay)
            {
                mtOverlay.afterContentRebuilt -= OnElementRebuilt;
            }
        }

        IEnumerable<MainToolbarElement> Build()
        {
            List<MainToolbarElement> elements = new List<MainToolbarElement>(3);

            if (m_IsAvailable)
            {
                if (!m_HasImguiOverride)
                {
                    elements.Add(new MainToolbarToggle(
                        EditorApplication.isPlayingOrWillChangePlaymode
                            ? new MainToolbarContent(EditorGUIUtility.LoadIcon("StopButton"), "Play")
                            : new MainToolbarContent(EditorGUIUtility.LoadIcon("PlayButton"), "Play"),
                        EditorApplication.isPlayingOrWillChangePlaymode,
                        OnPlayButtonValueChanged)
                    {
                        populateContextMenu = PopulatePlayContextMenu,
                    });

                    elements.Add(new MainToolbarToggle(
                        EditorApplication.isPaused
                            ? new MainToolbarContent(EditorGUIUtility.LoadIcon("PauseButton On"), "Pause")
                            : new MainToolbarContent(EditorGUIUtility.LoadIcon("PauseButton"), "Pause"),
                        EditorApplication.isPaused,
                        OnPauseButtonValueChanged));

                    elements.Add(new MainToolbarButton(new MainToolbarContent(EditorGUIUtility.LoadIcon("StepButton"), "Step"), OnStepButtonClicked)
                    {
                        enabled = EditorApplication.isPlaying,
                    });
                }
                else
                {
                    elements.Add(new MainToolbarCustom(() =>
                    {
                        var imgui = new IMGUIContainer(OverrideGUIHandler);
                        imgui.style.width = k_ImguiOverrideWidth;
                        return imgui;
                    }));
                }
            }

            return elements;
        }

        void OnElementRebuilt(VisualElement root)
        {
            try
            {
                onPlayModeButtonsCreated?.Invoke(root);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        void OnModeChanged(ModeService.ModeChangedArgs args)
        {
            CheckAvailability();
            CheckImguiOverride();
        }

        void CheckAvailability()
        {
            bool wasAvailable = m_IsAvailable;
            m_IsAvailable = ModeService.HasCapability(ModeCapability.Playbar, true);

            if (m_IsAvailable != wasAvailable)
                MainToolbar.Refresh(k_ElementId);
        }

        void CheckImguiOverride()
        {
            var wasOverriden = ModeService.HasExecuteHandler("gui_playbar");
            if (wasOverriden != m_HasImguiOverride)
                MainToolbar.Refresh(k_ElementId);
        }

        void OnPlayButtonValueChanged(bool value)
        {
            if (value)
            {
                EditorApplication.EnterPlaymode();
            }
            else
            {
                EditorApplication.ExitPlaymode();
            }
        }

        void PopulatePlayContextMenu(DropdownMenu menu)
        {
            menu.AppendAction(
                L10n.Tr("Open Game View On Play"),
                ChangeOpenGameViewOnPlayModeBehavior,
                GameView.openWindowOnEnteringPlayMode ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        void ChangeOpenGameViewOnPlayModeBehavior(DropdownMenuAction action)
        {
            PlayModeView.openWindowOnEnteringPlayMode = !PlayModeView.openWindowOnEnteringPlayMode;
        }

        void OnPauseButtonValueChanged(bool value)
        {
            EditorApplication.isPaused = value;
        }

        void OnStepButtonClicked()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.Step();
            }
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            UpdatePlayState();
            UpdateStepState();
        }

        void OnPauseStateChanged(PauseState state)
        {
            UpdatePauseState();
        }

        void UpdatePlayState()
        {
            MainToolbar.Refresh(k_ElementId);
        }

        void UpdatePauseState()
        {
            MainToolbar.Refresh(k_ElementId);
        }

        void UpdateStepState()
        {
            MainToolbar.Refresh(k_ElementId);
        }

        void OverrideGUIHandler()
        {
            ModeService.Execute("gui_playbar", EditorApplication.isPlayingOrWillChangePlaymode);
        }
    }
}
