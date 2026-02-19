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
        if (EditorMultiplayerManager.enableMultiplayerRoles)
        {
            var roleNode = new SetupEditorMultiplayerRoleNode("MainEditor_SetupMultiplayerRole");
            executionGraph.AddNode(roleNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(roleNode.PlayerInstanceIndex, 0);
            executionGraph.ConnectConstant(roleNode.Role, Settings.RoleMask);

            var restoreRoleNode = new SetupEditorMultiplayerRoleNode("MainEditor_RestoreMultiplayerRole");
            executionGraph.AddNode(restoreRoleNode, ExecutionStage.Cleanup);
            executionGraph.ConnectConstant(restoreRoleNode.PlayerInstanceIndex, 0);
            executionGraph.ConnectConstant(restoreRoleNode.Role, EditorMultiplayerManager.activeMultiplayerRoleMask);
        }

        if (Settings.InitialScene != null)
        {
            var openSceneNode = new OpenSceneNode("MainEditor_OpenInitialScene");
            executionGraph.AddNode(openSceneNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(openSceneNode.Scene, Settings.InitialScene);

            var restoreSceneNode = new RestoreSceneManagerSetupNode("MainEditor_RestoreSceneSetup");
            executionGraph.AddNode(restoreSceneNode, ExecutionStage.Cleanup);
            executionGraph.ConnectConstant(restoreSceneNode.ScenesSetup, EditorSceneManager.GetSceneManagerSetup());
        }

        var player = MultiplayerPlaymode.Players[0];
        if (player != null)
        {
            var tagsNode = new SetupEditorTagsNode("MainEditor_SetupTags");
            executionGraph.AddNode(tagsNode, ExecutionStage.Deploy);
            executionGraph.ConnectConstant(tagsNode.PlayerInstanceIndex, 0);
            executionGraph.ConnectConstant(tagsNode.Tags, new[] { Settings.PlayerTag });

            var restoreTagsNode = new SetupEditorTagsNode("MainEditor_RestoreTags");
            executionGraph.AddNode(restoreTagsNode, ExecutionStage.Cleanup);
            executionGraph.ConnectConstant(restoreTagsNode.PlayerInstanceIndex, 0);
            executionGraph.ConnectConstant(restoreTagsNode.Tags, player.Tags);
        }


        var startNode = new MainEditorStartNode("MainEditor_Start");
        executionGraph.AddNode(startNode, ExecutionStage.Start);

        var runNode = new MainEditorRunNode("MainEditor_Run");
        executionGraph.AddNode(runNode, ExecutionStage.Run);
    }

    protected internal override VisualElement CreateControllerUI(Instance instance)
    {
        return new EditorInstanceStatusElement(Settings.RoleMask, Settings.PlayerTag);
    }
}
