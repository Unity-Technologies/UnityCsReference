// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.Multiplayer.Internal;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class MainEditorController : EditorController<MainEditorController, MainEditorController.InstanceSettings>
{
    [Serializable]
    public struct InstanceSettings
    {
        public string PlayerTag;
        public MultiplayerRoleFlags RoleMask = MultiplayerRoleFlags.Client;
        public SceneAsset InitialScene;

        public InstanceSettings()
        {
        }
    }

    internal override string GetTypeNameForAnalytics() => "MainEditor";

    protected internal override void SetupExecutionGraph(ExecutionGraph executionGraph)
    {
        var editorRunNode = new EditorMultiplayerPlaymodeRunNode($"MainEditor_run");
        var deployNode = new EditorMultiplayerPlaymodeDeployNode($"MainEditor_deploy");

        executionGraph.AddNode(deployNode, ExecutionStage.Deploy);
        executionGraph.ConnectConstant(deployNode.PlayerInstanceIndex, 0);
        executionGraph.ConnectConstant(deployNode.PlayerTags, Settings.PlayerTag);
        executionGraph.ConnectConstant(deployNode.MultiplayerRole, Settings.RoleMask);
        executionGraph.ConnectConstant(deployNode.InitialScene, Settings.InitialScene);

        executionGraph.AddNode(editorRunNode, ExecutionStage.Run);
        executionGraph.ConnectConstant(editorRunNode.PlayerInstanceIndex, 0);
        executionGraph.ConnectConstant(editorRunNode.PlayerTags, Settings.PlayerTag);
    }

    protected internal override VisualElement CreateControllerUI(Instance instance)
    {
        return new EditorInstanceStatusElement(Settings.RoleMask, Settings.PlayerTag);
    }
}
