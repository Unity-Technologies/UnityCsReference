// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: HeadlessRuntime not yet converted
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using Unity.PlayMode.Editor;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal class ScenarioStatusWindow : EditorWindow
    {
        private const string k_StylesheetPath = "Multiplayer/UI/ScenarioStatusWindow.uss";

        internal static void OpenWindow()
        {
            GetWindow<ScenarioStatusWindow>();
        }

        private Scenario m_Scenario;
        private ScenarioStatusElement m_ScenarioElement;

        private void OnEnable()
        {
            titleContent = new GUIContent("Play Mode Scenarios Status");

            var styleSheet = EditorGUIUtility.LoadRequired(k_StylesheetPath) as StyleSheet;
            rootVisualElement.styleSheets.Add(styleSheet);

            ScenarioManagerProvider.instance.ConfigAssetChanged += Refresh;
            ScenarioManagerProvider.instance.StateChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            ScenarioManagerProvider.instance.ConfigAssetChanged -= Refresh;
            ScenarioManagerProvider.instance.StateChanged -= Refresh;
        }

        private void Refresh(PlayModeScenarioState _) => Refresh();

        private void Refresh()
        {
            var orchestratedScenario = PlayModeScenarioManager.ActiveScenario as OrchestratedScenario;
            m_Scenario = orchestratedScenario == null ? null : orchestratedScenario.Scenario;

            rootVisualElement.Clear();

            if (m_Scenario == null)
            {
                rootVisualElement.Add(new Label("No active orchestrated scenario"));
                return;
            }

            m_ScenarioElement = new ScenarioStatusElement(m_Scenario);
            rootVisualElement.Add(m_ScenarioElement);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
