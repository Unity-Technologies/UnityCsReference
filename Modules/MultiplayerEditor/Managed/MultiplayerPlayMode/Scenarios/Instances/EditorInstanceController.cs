// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Multiplayer.PlayMode.Editor
{
    class EditorInstanceController : PlayModeController
    {
        private readonly EditorInstanceDescription m_Settings;

        internal EditorInstanceController(EditorInstanceDescription editorInstanceDescription)
        {
            m_Settings = editorInstanceDescription;
        }

        protected internal override void SetupExecutionGraph(ExecutionGraph executionGraph)
        {
            var editorRunNode = new EditorMultiplayerPlaymodeRunNode($"{m_Settings.Name}|{m_Settings.PlayerInstanceIndex}_run");
            var deployNode = new EditorMultiplayerPlaymodeDeployNode($"{m_Settings.Name}|{m_Settings.PlayerInstanceIndex}_deploy");

            executionGraph.AddNode(deployNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(deployNode.PlayerInstanceIndex, m_Settings.PlayerInstanceIndex);
            executionGraph.ConnectConstant(deployNode.PlayerTags, m_Settings.PlayerTag);
            executionGraph.ConnectConstant(deployNode.MultiplayerRole, m_Settings.RoleMask);
            executionGraph.ConnectConstant(deployNode.InitialScene, m_Settings.InitialScene);

            // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
            m_Settings.CorrespondingNodeId = editorRunNode.Name;
            m_Settings.SetCorrespondingNodes(editorRunNode, deployNode);

            executionGraph.AddNode(editorRunNode, ExecutionStage.Run);
            executionGraph.ConnectConstant(editorRunNode.PlayerInstanceIndex, m_Settings.PlayerInstanceIndex);
            executionGraph.ConnectConstant(editorRunNode.PlayerTags, m_Settings.PlayerTag);

            if (m_Settings is VirtualEditorInstanceDescription virtualEditorInstanceDescription)
            {
                executionGraph.ConnectConstant(editorRunNode.StreamLogs, virtualEditorInstanceDescription.AdvancedConfiguration.StreamLogsToMainEditor);
                executionGraph.ConnectConstant(editorRunNode.LogsColor, virtualEditorInstanceDescription.AdvancedConfiguration.LogsColor);
            }
        }
    }
}
