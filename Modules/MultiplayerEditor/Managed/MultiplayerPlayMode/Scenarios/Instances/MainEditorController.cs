// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Multiplayer.Internal;
using UnityEditor.SceneManagement;
using UnityEngine.Multiplayer.Internal;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class MainEditorController : EditorController<MainEditorController.InstanceSettings>
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

    protected internal override void SetupExecutionGraph(ExecutionGraphBuilder graphBuilder)
    {
        if (EditorMultiplayerManager.enableMultiplayerRoles)
        {
            var roleNode = graphBuilder.AddNode<SetupEditorMultiplayerRoleNode>(ExecutionStage.Deploy);
            graphBuilder.ConnectConstant(roleNode.PlayerInstanceIndex, 0);
            graphBuilder.ConnectConstant(roleNode.Role, Settings.RoleMask);

            var restoreRoleNode = graphBuilder.AddNode<SetupEditorMultiplayerRoleNode>(ExecutionStage.Cleanup);
            graphBuilder.ConnectConstant(restoreRoleNode.PlayerInstanceIndex, 0);
            graphBuilder.ConnectConstant(restoreRoleNode.Role, EditorMultiplayerManager.activeMultiplayerRoleMask);
        }

        if (Settings.InitialScene != null)
        {
            var openSceneNode = graphBuilder.AddNode<OpenSceneNode>(ExecutionStage.Deploy);
            graphBuilder.ConnectConstant(openSceneNode.Scene, Settings.InitialScene);

            var restoreSceneNode = graphBuilder.AddNode<RestoreSceneManagerSetupNode>(ExecutionStage.Cleanup);
            graphBuilder.ConnectConstant(restoreSceneNode.ScenesSetup, EditorSceneManager.GetSceneManagerSetup());
        }

        var player = MultiplayerPlaymode.Players[0];
        if (player != null)
        {
            var tagsNode = graphBuilder.AddNode<SetupEditorTagsNode>(ExecutionStage.Deploy);
            graphBuilder.ConnectConstant(tagsNode.PlayerInstanceIndex, 0);
            graphBuilder.ConnectConstant(tagsNode.Tags, new[] { Settings.PlayerTag });

            var restoreTagsNode = graphBuilder.AddNode<SetupEditorTagsNode>(ExecutionStage.Cleanup);
            graphBuilder.ConnectConstant(restoreTagsNode.PlayerInstanceIndex, 0);
            graphBuilder.ConnectConstant(restoreTagsNode.Tags, player.Tags);
        }


        graphBuilder.AddNode<MainEditorStartNode>(ExecutionStage.Start);

        graphBuilder.AddNode<MainEditorRunNode>(ExecutionStage.Run);
    }

    protected internal override VisualElement CreateControllerUI(Instance instance)
    {
        return new EditorInstanceStatusElement(Settings.RoleMask, Settings.PlayerTag);
    }
}
