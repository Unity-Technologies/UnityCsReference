// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlayMode.Editor
{
    class DefaultScenario : PlayModeScenario
    {
        const string k_ActiveScenarioHelpText = "No Play Mode Scenario selected. Please use the dropdown next to the play button to switch to a scenario.";
        internal const string k_HelpBoxClass = "help-box";

        internal override bool SupportsPauseAndStep => true;

        internal override void ExecuteStart()
        {
            // Save assets before entering Playmode to synchronize project settings for virtual player and main editor
            // TODO: we should do this only for the virtual players case, not for all the cases of the default playmode
            AssetDatabase.SaveAssets();

            EditorApplication.EnterPlaymode();
        }

        internal override void ExecuteStop()
        {
            EditorApplication.ExitPlaymode();
        }

        internal override void OnSelected()
        {
            SetupEvents();
            SetState(ComputeExpectedState());
        }
        internal override void OnDeselected() => ClearEvents();

        protected virtual void Awake()
        {
            SetState(ComputeExpectedState());
        }

        protected virtual void OnEnable()
        {
            if (PlayModeScenarioManager.ActiveScenario == this)
            {
                SetupEvents();
                StateSyncedSanityCheck();
            }
        }

        void StateSyncedSanityCheck()
        {
            var expectedState = ComputeExpectedState();
            if (GetState() != expectedState)
            {
                Debug.LogAssertion($"Mismatched state in DefaultScenario, was {GetState()} but expected {expectedState}");
                SetState(expectedState);
            }
        }

        PlayModeScenarioState ComputeExpectedState()
        {
            if (EditorApplication.isPlaying)
                return PlayModeScenarioState.Running;
            if (EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isPlaying)
                return PlayModeScenarioState.Starting;
            return PlayModeScenarioState.Idle;
        }

        private void SetupEvents()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void ClearEvents()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    SetState(PlayModeScenarioState.Running);
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    // If EditorApplication.ExitPlaymode is called inside a `OnPlayModeStateChanged` callback, the EnteredEditMode event is not called.
                    // But in that case isPlaying will be false, so we can use that to determine that the state should be Idle without passing through Stopping.
                    SetState(EditorApplication.isPlaying ? PlayModeScenarioState.Stopping : PlayModeScenarioState.Idle);
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    SetState(PlayModeScenarioState.Idle);
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    SetState(PlayModeScenarioState.Starting);
                    break;
            }
        }

        internal override VisualElement CreateScenarioUI()
        {
            var helpBox = new HelpBox(k_ActiveScenarioHelpText, HelpBoxMessageType.Info);
            helpBox.AddToClassList(k_HelpBoxClass);
            return helpBox;
        }
    }
}
