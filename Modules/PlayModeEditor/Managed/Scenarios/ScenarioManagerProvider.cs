// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor;

namespace Unity.PlayMode.Editor
{
    /// <summary>
    /// The Play Mode Manager provides an API to control advanced scenarios of the play mode state of the editor.
    /// </summary>
    class ScenarioManagerProvider : ScriptableSingleton<ScenarioManagerProvider>
    {
        [SerializeField] private PlayModeScenario m_Config;

        /// <summary>
        /// An event that is raised when the play mode state changes.
        /// </summary>
        internal event Action<PlayModeScenarioState> StateChanged;

        /// <summary>
        /// An event that is raised when the active config asset changes.
        /// </summary>
        internal event Action ConfigAssetChanged;

        /// <summary>
        /// The current play mode state.
        /// </summary>
        internal PlayModeScenarioState CurrentState => ActivePlayModeConfig.GetState();

        /// <summary>
        /// True if the current play mode config supports pause and step.
        /// </summary>
        internal bool SupportsPauseAndStep => ActivePlayModeConfig.SupportsPauseAndStep;

        DefaultScenario m_DefaultScenario;
        internal DefaultScenario DefaultScenarioInstance
        {
            get
            {
                if (m_DefaultScenario == null)
                {
                    var defaultScenarios = Resources.FindObjectsOfTypeAll<DefaultScenario>();
                    foreach (var scenario in defaultScenarios)
                    {
                        if (scenario.GetType() == typeof(DefaultScenario))
                        {
                            m_DefaultScenario = scenario;
                            return m_DefaultScenario;
                        }
                    }

                    m_DefaultScenario = CreateInstance<DefaultScenario>();
                    m_DefaultScenario.name = "Default";
                    m_DefaultScenario.Description = "Default play mode";
                    m_DefaultScenario.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontUnloadUnusedAsset;
                }

                return m_DefaultScenario;
            }
        }

        /// <summary>
        /// The active play mode config asset.
        /// </summary>
        internal PlayModeScenario ActivePlayModeConfig
        {
            get
            {
                if (m_Config == null)
                    AssignActiveConfig(PlayModeUserSettings.instance.LastActiveConfiguration);

                return m_Config;
            }
            set
            {
                if (CurrentState is PlayModeScenarioState.Starting or PlayModeScenarioState.Running or PlayModeScenarioState.Stopping)
                    throw new InvalidOperationException($"Cannot set config while in a running state ({CurrentState})");

                if (m_Config == value)
                    return;

                // Deselect to clear any previous config
                if (m_Config != null)
                    m_Config.OnDeselected();

                // Select the new one if set
                AssignActiveConfig(value);
            }
        }

        internal void ReportScenarioStateChanged()
        {
            StateChanged?.Invoke(CurrentState);
        }

        void AssignActiveConfig(PlayModeScenario config)
        {
            if (config == null)
                config = DefaultScenarioInstance;

            m_Config = config;
            PlayModeUserSettings.instance.LastActiveConfiguration = m_Config;

            m_Config.OnSelected();
            ConfigAssetChanged?.Invoke();
        }

        /// <summary>
        /// Starts the play mode.
        /// </summary>
        internal void Start()
        {
            if (!ActivePlayModeConfig.IsValid(out var reason))
            {
                Debug.LogError("Cannot enter Playmode: " + reason);
                return;
            }

            ActivePlayModeConfig.ExecuteStart();
        }

        /// <summary>
        /// Stops the play mode.
        /// </summary>
        internal void Stop()
        {
            ActivePlayModeConfig.ExecuteStop();
        }   
    }
}
