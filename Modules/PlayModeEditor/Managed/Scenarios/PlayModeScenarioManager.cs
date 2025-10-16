// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.PlayMode.Editor
{
    /// <summary>
    /// Manages the lifecycle of Play Mode Scenarios, which define custom behaviors for entering and exiting Play mode, such as launching multiple Player instances.
    /// </summary>
    /// <remarks>
    /// Use the PlayModeScenarioManager to assign, start, and stop the active scenario, and to query its state.
    ///
    /// This API extends, rather than replaces, methods like <c>EditorApplication.EnterPlaymode</c>. It's designed for advanced cases that require custom setup and teardown logic or the management of multiple Player instances during Play mode.
    ///
    /// [!NOTE]
    /// Avoid manually changing the play state via <c>EditorApplication.EnterPlaymode()</c> or <c>EditorApplication.isPlaying</c> when a custom scenario is active. Doing so has the following consequences:
    /// *   **Before a scenario runs:** Manually starting Play mode causes the manager to set the active scenario to the default one.
    /// *   **While a scenario is running:** Manually starting or altering the play state is unsupported and can cause errors.
    ///
    /// These restrictions don't apply when the default scenario is active. In that case, the behavior is identical to the standard Unity Play mode.
    /// 
    /// For safer operation, always use <c>PlayModeScenarioManager.Start()</c> and <c>PlayModeScenarioManager.Stop()</c> to control Play mode when using custom scenarios.
    /// </remarks>
    public static class PlayModeScenarioManager
    {
        internal struct ScenarioTypeData
        {
            public Type ScenarioType;
            public string Label;
            public string NewItemName;
        }

        static Dictionary<Type, ScenarioTypeData> s_ScenarioTypes = new();

        internal static void RegisterScenarioType<T>(string label, string newItemName = "NewPlayModeScenario") where T : PlayModeScenario
        {
            if (typeof(T).IsAbstract)
            {
                Debug.LogError($"Type '{typeof(T)}' is abstract. Only concrete types are allowed to have the CreatePlayModeConfigurationMenuAttribute.");
                return;
            }

            if (!string.IsNullOrEmpty(label) && string.IsNullOrEmpty(newItemName))
            {
                throw new ArgumentNullException("newItemName cannot be null or empty.");
            }

            s_ScenarioTypes[typeof(T)] = new ScenarioTypeData
            {
                ScenarioType = typeof(T),
                Label = label,
                NewItemName = newItemName
            };

            PlayModeButtonsExtension.Initialize(); // Refresh the buttons if needed
        }

        internal static void UnregisterScenarioType<T>() where T : PlayModeScenario
        {
            s_ScenarioTypes.Remove(typeof(T));
        }

        internal static bool IsScenarioTypeRegistered(Type type) => s_ScenarioTypes.ContainsKey(type);
        internal static IEnumerable<ScenarioTypeData> GetScenarioTypes() => s_ScenarioTypes.Values;
        internal static int ScenarioTypesCount => s_ScenarioTypes.Count;

        [RequiredByNativeCode]
        internal static void TogglePlayingShortcut()
        {
            if (ActiveScenario == ScenarioManagerProvider.instance.DefaultScenarioInstance)
                EditorApplication.TogglePlaying();
            else
                TogglePlaying();
        }

        static void TogglePlaying()
        {
            if (State == PlayModeScenarioState.Running)
                Stop();
            else
                Start();
        }


        /// <summary>
        /// Returns or sets the active play mode scenario.
        /// </summary>
        /// <remarks>
        /// Assigning a null value will revert to the default scenario.
        /// </remarks>
        public static PlayModeScenario ActiveScenario
        {
            get => ScenarioManagerProvider.instance.ActivePlayModeConfig;
            set => ScenarioManagerProvider.instance.ActivePlayModeConfig = value;
        }

        /// <summary>
        /// Returns the current state of the active play mode scenario.
        /// </summary>
        public static PlayModeScenarioState State => ScenarioManagerProvider.instance.CurrentState;

        /// <summary>
        /// Starts the play mode using the active play mode scenario.
        /// </summary>
        public static void Start() => ScenarioManagerProvider.instance.Start();

        /// <summary>
        /// Stops the play mode scenario.
        /// </summary>
        public static void Stop() => ScenarioManagerProvider.instance.Stop();
    }
}
