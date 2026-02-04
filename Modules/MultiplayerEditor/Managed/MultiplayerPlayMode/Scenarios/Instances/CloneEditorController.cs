// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Multiplayer.Internal;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class CloneEditorController : EditorController<CloneEditorController, CloneEditorController.InstanceSettings>
{
    [Serializable]
    public struct InstanceSettings
    {
        public string PlayerTag;
        public MultiplayerRoleFlags RoleMask = MultiplayerRoleFlags.Client;
        public int PlayerInstanceIndex;
        public bool StreamLogsToMainEditor;
        public Color LogsColor = new(0.3643f, 0.581f, 0.8679f);

        public InstanceSettings()
        {
        }
    }
    
    internal override string GetTypeNameForAnalytics() => "VirtualEditor";

    protected internal override void SetupExecutionGraph(ExecutionGraph executionGraph)
    {
        var editorRunNode = new EditorMultiplayerPlaymodeRunNode($"CloneEditor|{Settings.PlayerInstanceIndex}_run");
        var deployNode = new EditorMultiplayerPlaymodeDeployNode($"CloneEditor|{Settings.PlayerInstanceIndex}_deploy");

        executionGraph.AddNode(deployNode, ExecutionStage.Deploy);
        executionGraph.ConnectConstant(deployNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
        executionGraph.ConnectConstant(deployNode.PlayerTags, Settings.PlayerTag);
        executionGraph.ConnectConstant(deployNode.MultiplayerRole, Settings.RoleMask);
        executionGraph.ConnectConstant(deployNode.InitialScene, null);

        executionGraph.AddNode(editorRunNode, ExecutionStage.Run);
        executionGraph.ConnectConstant(editorRunNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
        executionGraph.ConnectConstant(editorRunNode.PlayerTags, Settings.PlayerTag);

        executionGraph.ConnectConstant(editorRunNode.StreamLogs, Settings.StreamLogsToMainEditor);
        executionGraph.ConnectConstant(editorRunNode.LogsColor, Settings.LogsColor);
    }

    protected internal override VisualElement CreateControllerUI(Instance instance)
    {
        return new CloneEditorInstanceStatusElement(instance, Settings);
    }
}
