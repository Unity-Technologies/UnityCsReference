// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class CloneEditorController : EditorController<CloneEditorController, VirtualEditorInstanceDescription>
{
    protected internal override void SetupExecutionGraph(ExecutionGraph executionGraph)
    {
        var editorRunNode = new EditorMultiplayerPlaymodeRunNode($"{Settings.Name}|{Settings.PlayerInstanceIndex}_run");
        var deployNode = new EditorMultiplayerPlaymodeDeployNode($"{Settings.Name}|{Settings.PlayerInstanceIndex}_deploy");

        executionGraph.AddNode(deployNode, ExecutionStage.Deploy);
        executionGraph.ConnectConstant(deployNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
        executionGraph.ConnectConstant(deployNode.PlayerTags, Settings.PlayerTag);
        executionGraph.ConnectConstant(deployNode.MultiplayerRole, Settings.RoleMask);
        executionGraph.ConnectConstant(deployNode.InitialScene, Settings.InitialScene);

        // [TODO]: We need to remove this line, since 1 instance could have multiple nodes
        Settings.CorrespondingNodeId = editorRunNode.Name;
        Settings.SetCorrespondingNodes(editorRunNode, deployNode);

        executionGraph.AddNode(editorRunNode, ExecutionStage.Run);
        executionGraph.ConnectConstant(editorRunNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
        executionGraph.ConnectConstant(editorRunNode.PlayerTags, Settings.PlayerTag);

        executionGraph.ConnectConstant(editorRunNode.StreamLogs, Settings.AdvancedConfiguration.StreamLogsToMainEditor);
        executionGraph.ConnectConstant(editorRunNode.LogsColor, Settings.AdvancedConfiguration.LogsColor);
    }

    protected internal override VisualElement CreateControllerUI()
    {
        return new CommonInstanceStatusElement(Settings);
    }
}
