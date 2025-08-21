// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal class ScenarioStatusWindow : EditorWindow
    {
        private const string k_StylesheetPath = "Multiplayer/UI/ScenarioStatusWindow.uss";

        internal static void OpenWindow()
        {
            GetWindow<ScenarioStatusWindow>();
        }

        private static Scenario m_LastScenario;

        private Scenario m_Scenario;
        private ScenarioStatusElement m_ScenarioElement;

        private void OnEnable()
        {
            titleContent = new GUIContent("Play Mode Scenarios Status");

            var styleSheet = EditorGUIUtility.LoadRequired(k_StylesheetPath) as StyleSheet;
            rootVisualElement.styleSheets.Add(styleSheet);

            m_Scenario = m_LastScenario != null ? m_LastScenario : ScenarioRunner.instance.ActiveScenario;
            Scenario.ScenarioStarted += OnScenarioStarted;

            Refresh();
        }

        private void OnDisable()
        {
            Scenario.ScenarioStarted -= OnScenarioStarted;
        }

        private void OnScenarioStarted(Scenario scenario)
        {
            m_Scenario = scenario;
            m_LastScenario = scenario;
            Refresh();
        }

        private void Refresh()
        {
            rootVisualElement.Clear();

            if (m_Scenario == null)
                return;

            m_ScenarioElement = new ScenarioStatusElement(m_Scenario);
            rootVisualElement.Add(m_ScenarioElement);
        }
    }
}
