// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlayMode.Editor
{
    class PlayModeButtonsView : VisualElement
    {
        private EditorToolbarToggle m_PlayButton;
        private EditorToolbarToggle m_PauseButton;
        private EditorToolbarButton m_StepButton;
        private PlaymodeDropdownButton m_DropdownButton;

        internal EditorToolbarToggle PlayButton => m_PlayButton;
        internal EditorToolbarToggle PauseButton => m_PauseButton;
        internal EditorToolbarButton StepButton => m_StepButton;
        internal PlaymodeDropdownButton DropdownButton => m_DropdownButton;

        public PlayModeButtonsView()
        {
            style.flexDirection = FlexDirection.Row;

            AddPlayStopButton();
            AddPauseButton();
            AddStepButton();
            AddDropdownButton();

            EditorToolbarUtility.SetupChildrenAsButtonStrip(this);

            RegisterCallback<AttachToPanelEvent>(OnAttachedToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachedToPanel(AttachToPanelEvent evt)
        {
            ScenarioManagerProvider.instance.ConfigAssetChanged += OnConfigAssetChanged;
            ScenarioManagerProvider.instance.StateChanged += OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged += OnPauseStateChanged;

            EditorApplication.playModeStateChanged += OnEditorChangedPlaymodeState;

            // Setup initial states
            var isPlaying = PlayModeScenarioManager.State == PlayModeScenarioState.Running || EditorApplication.isPlaying;
            var isPaused = EditorApplication.isPaused;
            OnConfigAssetChanged();
            OnPlayModeStateChanged(isPlaying ? PlayModeScenarioState.Running : PlayModeScenarioState.Idle);
            OnPauseStateChanged(isPaused ? PauseState.Paused : PauseState.Unpaused);
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ScenarioManagerProvider.instance.ConfigAssetChanged -= OnConfigAssetChanged;
            ScenarioManagerProvider.instance.StateChanged -= OnPlayModeStateChanged;
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
        }

        // We need this because a user can bypass the UI and start the PlayMode via the Editor
        // or via Rider. Later we want to capture all those possibilities and make sure the scenario will be started.
        void OnEditorChangedPlaymodeState(PlayModeStateChange playModeStateChange)
        {
            if (playModeStateChange == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.delayCall += () =>
                {
                    m_PlayButton.SetValueWithoutNotify(true);

                    if (PlayModeScenarioManager.State != PlayModeScenarioState.Running &&
                        PlayModeScenarioManager.ActiveScenario != ScenarioManagerProvider.instance.DefaultScenarioInstance)
                    {
                        Debug.LogWarning(
                            "Entered Playmode, without starting a Scenario (Did you enter PlayMode from an IDE?). Starting scenarios only works via the UI at the moment. \n" +
                            "Please start a scenario via the UI to make sure the scenario is started correctly.");
                    }
                };
            }

            // Maybe we exit PlayMode from rider, make sure we stop the scenario in that case.
            if (playModeStateChange == PlayModeStateChange.ExitingPlayMode)
            {
                // But we do it in the next frame as there could be cases where user code can exit play mode to
                // perform some set up actions and then enter it again.
                EditorApplication.delayCall += () =>
                {
                    m_PlayButton.SetValueWithoutNotify(false);
                    if (!EditorApplication.isPlayingOrWillChangePlaymode && PlayModeScenarioManager.State == PlayModeScenarioState.Running)
                    {
                        PlayModeScenarioManager.Stop();
                    }
                };
            }
        }

        private void AddPlayStopButton()
        {
            Add(m_PlayButton = new EditorToolbarToggle
            {
                name = "Play",
                tooltip = "Play",
                onIcon = LoadIcon("StopButton"),
                offIcon = LoadIcon("PlayButton"),
            });
            m_PlayButton.RegisterValueChangedCallback(OnPlayButtonValueChanged);
        }

        private void AddPauseButton()
        {
            Add(m_PauseButton = new EditorToolbarToggle
            {
                name = "Pause",
                tooltip = "Pause",
                onIcon = LoadIcon("PauseButton On"),
                offIcon = LoadIcon("PauseButton"),
            });
            m_PauseButton.RegisterValueChangedCallback(OnPauseButtonValueChanged);
        }

        private void AddStepButton()
        {
            Add(m_StepButton = new EditorToolbarButton
            {
                name = "Step",
                tooltip = "Step",
                icon = LoadIcon("StepButton"),
            });
            m_StepButton.clickable.activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            m_StepButton.clicked += OnStepButtonClicked;
        }

        private void AddDropdownButton()
        {
            Insert(0, m_DropdownButton = new PlaymodeDropdownButton());
        }

        static private Texture2D LoadIcon(string name)
        {
            return EditorGUIUtility.IconContent(name).image as Texture2D;
        }


        private void OnConfigAssetChanged()
        {
            var currentConfig = PlayModeScenarioManager.ActiveScenario;
            var so = new SerializedObject(currentConfig);
            this.Unbind();
            this.TrackSerializedObjectValue(so, o =>
            {
                ValidateCurrentConfiguration(currentConfig);
            });
            ValidateCurrentConfiguration(currentConfig);

            var supportsPauseAndStep = ScenarioManagerProvider.instance.SupportsPauseAndStep;

            m_PauseButton.SetEnabled(supportsPauseAndStep);

            // The Step button’s state depends on whether SupportsPauseAndStep, current PlayModeState and whether the editor is currently in play mode
            m_StepButton.SetEnabled(ScenarioManagerProvider.instance.SupportsPauseAndStep && (PlayModeScenarioManager.State == PlayModeScenarioState.Running || EditorApplication.isPlaying));
        }

        void ValidateCurrentConfiguration(PlayModeScenario currentConfig)
        {
            var playmodeConfigIsValid = currentConfig.IsValid(out var reason);
            m_DropdownButton.UpdateUI(currentConfig);
            m_PlayButton.tooltip = playmodeConfigIsValid ? "Play" : "Playmode not available.\n" + reason;
            m_PlayButton.SetEnabled(playmodeConfigIsValid);
        }


        private void OnPlayModeStateChanged(PlayModeScenarioState state)
        {
            m_PlayButton.SetValueWithoutNotify(state == PlayModeScenarioState.Running);
            m_StepButton.SetEnabled(ScenarioManagerProvider.instance.SupportsPauseAndStep && state == PlayModeScenarioState.Running);
        }

        private void OnPauseStateChanged(PauseState state)
        {
            m_PauseButton.SetValueWithoutNotify(state == PauseState.Paused);
        }

        private void OnStepButtonClicked()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.Step();
            }
        }

        private void OnPlayButtonValueChanged(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                ScenarioManagerProvider.instance.Start();
            }
            else
            {
                if (PlayModeScenarioManager.State != PlayModeScenarioState.Running)
                {
                    EditorApplication.isPlaying = false;
                }
                PlayModeScenarioManager.Stop();
            }

            // set the state of the button manually to make sure we are in sync with the actual state.
            var button = evt.target as EditorToolbarToggle;
            button.SetValueWithoutNotify(PlayModeScenarioManager.State == PlayModeScenarioState.Running);
        }

        private void OnPauseButtonValueChanged(ChangeEvent<bool> evt)
        {
            EditorApplication.isPaused = evt.newValue;
        }
    }
}
