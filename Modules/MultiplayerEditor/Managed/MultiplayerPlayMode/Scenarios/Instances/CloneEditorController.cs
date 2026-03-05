// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Multiplayer.Internal;
using UnityEditor.Multiplayer.Internal;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class CloneEditorController : EditorController<CloneEditorController.InstanceSettings>
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
        if (EditorMultiplayerManager.enableMultiplayerRoles)
        {
            var roleNode = new SetupEditorMultiplayerRoleNode("CloneEditor_SetupMultiplayerRole");
            executionGraph.AddNode(roleNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(roleNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
            executionGraph.ConnectConstant(roleNode.Role, Settings.RoleMask);

            var restoreRoleNode = new SetupEditorMultiplayerRoleNode("CloneEditor_RestoreMultiplayerRole");
            executionGraph.AddNode(restoreRoleNode, ExecutionStage.Cleanup);
            executionGraph.ConnectConstant(restoreRoleNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
            executionGraph.ConnectConstant(restoreRoleNode.Role, MultiplayerPlaymode.Players[Settings.PlayerInstanceIndex].Role);
        }

        var tagsNode = new SetupEditorTagsNode("CloneEditor_SetupTags");
        executionGraph.AddNode(tagsNode, ExecutionStage.Deploy);
        executionGraph.ConnectConstant(tagsNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
        executionGraph.ConnectConstant(tagsNode.Tags, new[] { Settings.PlayerTag });

        var restoreTagsNode = new SetupEditorTagsNode("CloneEditor_RestoreTags");
        executionGraph.AddNode(restoreTagsNode, ExecutionStage.Cleanup);
        executionGraph.ConnectConstant(restoreTagsNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
        executionGraph.ConnectConstant(restoreTagsNode.Tags, MultiplayerPlaymode.Players[Settings.PlayerInstanceIndex].Tags);


        var editorRunNode = new CloneEditorRunNode($"CloneEditor|{Settings.PlayerInstanceIndex}_run");
        var deployNode = new CloneEditorDeployNode($"CloneEditor|{Settings.PlayerInstanceIndex}_deploy");

        executionGraph.AddNode(deployNode, ExecutionStage.Deploy);
        executionGraph.ConnectConstant(deployNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);

        executionGraph.AddNode(editorRunNode, ExecutionStage.Run);
        executionGraph.ConnectConstant(editorRunNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
        executionGraph.ConnectConstant(editorRunNode.StreamLogs, Settings.StreamLogsToMainEditor);
        executionGraph.ConnectConstant(editorRunNode.LogsColor, Settings.LogsColor);
    }

    protected internal override VisualElement CreateControllerUI(Instance instance)
    {
        return new CloneEditorInstanceStatusElement(instance, Settings);
    }
}
