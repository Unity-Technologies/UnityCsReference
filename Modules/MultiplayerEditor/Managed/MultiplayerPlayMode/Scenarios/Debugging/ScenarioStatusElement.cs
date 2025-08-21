// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal class ScenarioStatusElement : VisualElement
    {
        private Scenario m_Scenario;
        private Dictionary<ExecutionStage, StageStatusElement> m_StageElements = new Dictionary<ExecutionStage, StageStatusElement>();

        private Label m_StateLabel;
        private Label m_MessageLabel;

        public ScenarioStatusElement(Scenario config)
        {
            m_Scenario = config;
            BuildUI();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            EditorApplication.update += Refresh;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            EditorApplication.update -= Refresh;
        }

        private void BuildUI()
        {
            AddToClassList("scenario-status");

            if (m_Scenario == null || m_Scenario.Status.StageState == ExecutionState.Invalid)
                return;

            var nameLabel = new Label($"Scenario: {m_Scenario.Name}") { name = "scenario-name" };
            m_StateLabel = new Label("[]") { name = "scenario-state" };
            m_MessageLabel = new Label("Message:") { name = "scenario-message" };
            var stagesScrollView = new ScrollView(ScrollViewMode.Horizontal) { name = "stages-scroll-view" };
            var stagesContainer = new VisualElement { name = "stages-container" };

            Add(nameLabel);
            Add(m_StateLabel);
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

        private void Refresh()
        {
            if (m_Scenario == null || m_Scenario.Status.StageState == ExecutionState.Invalid)
                return;

            var state = m_Scenario.Status;

            var message = new StringBuilder();
            foreach (var error in state.Errors)
            {
                message.AppendLine(error.Message);
            }

            m_StateLabel.text = $"[{state.State} ({state.CurrentStage}) - total progress: {state.TotalProgress * 100}%]";
            m_MessageLabel.text = $"Message: {message}";

            var stageKeys = Enum.GetValues(typeof(ExecutionStage));
            foreach (ExecutionStage stage in stageKeys)
            {
                var stageElement = m_StageElements[stage];
                var stageState = CalculateStageState(state, stage);
                stageElement.SetState(stageState, state.TotalProgress);
            }
        }

        private static ExecutionState CalculateStageState(ScenarioStatus scenarioState, ExecutionStage stage)
        {
            if (scenarioState.StageStates == null || scenarioState.StageStates.Length == 0)
                return ExecutionState.Invalid;

            return scenarioState.StageStates[(int)stage];
        }
    }
}
