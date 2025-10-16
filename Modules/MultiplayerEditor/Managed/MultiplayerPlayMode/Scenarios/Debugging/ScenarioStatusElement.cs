// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal class ScenarioStatusElement : VisualElement
    {
        private Scenario m_Scenario;
        private SerializedObject m_SerializedScenario;
        private Dictionary<ExecutionStage, StageStatusElement> m_StageElements = new Dictionary<ExecutionStage, StageStatusElement>();

        private PropertyField m_StatusField;
        private Label m_NameLabel;
        private Label m_MessageLabel;

        public ScenarioStatusElement(Scenario scenario)
        {
            m_Scenario = scenario;
            m_SerializedScenario = new SerializedObject(scenario);
            this.Bind(m_SerializedScenario);

            BuildUI();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_Scenario.StatusRefreshed += OnScenarioStatusUpdated;
            Refresh();
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_Scenario.StatusRefreshed -= OnScenarioStatusUpdated;
        }

        private void BuildUI()
        {
            AddToClassList("scenario-status");

            if (m_Scenario == null)
                return;

            m_NameLabel = new Label($"Scenario: {m_Scenario.name}") { name = "scenario-name" };
            m_StatusField = new PropertyField(m_SerializedScenario.FindProperty("m_Status"))
            {
                label = "Status Data",
                enabledSelf = false,
            };
            m_MessageLabel = new Label("Message:") { name = "scenario-message" };
            var stagesScrollView = new ScrollView(ScrollViewMode.Horizontal) { name = "stages-scroll-view" };
            var stagesContainer = new VisualElement { name = "stages-container" };

            Add(m_NameLabel);
            Add(m_StatusField);
            Add(m_MessageLabel);
            Add(stagesScrollView);
            stagesScrollView.Add(stagesContainer);

            var stageKeys = Enum.GetValues(typeof(ExecutionStage));
            foreach (ExecutionStage stage in stageKeys)
            {
                var stageElement = new StageStatusElement(stage, m_Scenario.GetNodes(stage));
                m_StageElements[stage] = stageElement;
                stagesContainer.Add(stageElement);
            }
        }

        private void OnScenarioStatusUpdated(ScenarioStatusData status)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (m_Scenario == null)
                return;

            var statusData = m_Scenario.StatusData;

            var message = new StringBuilder();
            foreach (var error in m_Scenario.GetAllNonFreeRunNodeErrors())
            {
                message.AppendLine(error.Message);
            }

            m_NameLabel.text = $"Scenario: {m_Scenario.name} (Stage: {statusData.CurrentStage}, State: {statusData.OverallStatus.State}, Progress: {statusData.OverallStatus.Progress:P1})";
            m_MessageLabel.text = $"Message: {message}";

            var stageKeys = Enum.GetValues(typeof(ExecutionStage));
            foreach (ExecutionStage stage in stageKeys)
            {
                var stageElement = m_StageElements[stage];
                var stageState = CalculateStageState(statusData, stage);
                stageElement.SetState(stageState, statusData.OverallStatus.Progress);
            }
        }

        private static ExecutionState CalculateStageState(ScenarioStatusData scenarioState, ExecutionStage stage)
        {
            if (scenarioState.StageStatuses == null || scenarioState.StageStatuses.Length == 0)
                return ExecutionState.Invalid;

            return scenarioState.StageStatuses[(int)stage].State;
        }
    }
}
