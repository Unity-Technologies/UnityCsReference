// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal class StageStatusElement : VisualElement
    {
        private ExecutionStage m_Stage;
        private IEnumerable<ExecutionNode> m_Nodes;

        private Label m_StateLabel;

        public StageStatusElement(ExecutionStage stage, IEnumerable<ExecutionNode> nodes)
        {
            m_Stage = stage;
            m_Nodes = nodes;
            BuildUI();
        }

        private void BuildUI()
        {
            AddToClassList("stage-status");

            var stageNameLabel = new Label($"{m_Stage}") { name = "stage-name" };
            m_StateLabel = new Label("[]") { name = "stage-state" };
            var nodesContainer = new VisualElement { name = "nodes-container" };

            Add(stageNameLabel);
            Add(m_StateLabel);
            Add(nodesContainer);

            if (m_Nodes == null)
                return;

            foreach (var node in m_Nodes)
            {
                var nodeVisualElement = new NodeStatusElement(node);
                nodesContainer.Add(nodeVisualElement);
            }
        }

        public void SetState(ExecutionState state, float progress)
        {
            m_StateLabel.text = $"[{state}]";
        }
    }
}
