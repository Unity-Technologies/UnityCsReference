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

    [Serializable]
    internal struct UserSettings
    {
        public bool KeepAliveEnabled;
    }

    internal const bool k_DefaultKeepAliveEnabled = true;

    internal override string GetTypeNameForAnalytics() => "VirtualEditor";

    protected internal override void SetupExecutionGraph(ExecutionGraphBuilder graphBuilder)
    {
        if (EditorMultiplayerManager.enableMultiplayerRoles)
        {
            var roleNode = graphBuilder.AddNode<SetupEditorMultiplayerRoleNode>(ExecutionStage.Deploy);
            graphBuilder.ConnectConstant(roleNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
            graphBuilder.ConnectConstant(roleNode.Role, Settings.RoleMask);

            var restoreRoleNode = graphBuilder.AddNode<SetupEditorMultiplayerRoleNode>(ExecutionStage.Cleanup);
            graphBuilder.ConnectConstant(restoreRoleNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
            graphBuilder.ConnectConstant(restoreRoleNode.Role, MultiplayerPlaymode.Players[Settings.PlayerInstanceIndex].Role);
        }

        var tagsNode = graphBuilder.AddNode<SetupEditorTagsNode>(ExecutionStage.Deploy);
        graphBuilder.ConnectConstant(tagsNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
        graphBuilder.ConnectConstant(tagsNode.Tags, new[] { Settings.PlayerTag });

        var restoreTagsNode = graphBuilder.AddNode<SetupEditorTagsNode>(ExecutionStage.Cleanup);
        graphBuilder.ConnectConstant(restoreTagsNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
        graphBuilder.ConnectConstant(restoreTagsNode.Tags, MultiplayerPlaymode.Players[Settings.PlayerInstanceIndex].Tags);


        var deployNode = graphBuilder.AddNode<CloneEditorDeployNode>(ExecutionStage.Deploy);
        graphBuilder.ConnectConstant(deployNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);

        var editorRunNode = graphBuilder.AddNode<CloneEditorRunNode>(ExecutionStage.Run);
        graphBuilder.ConnectConstant(editorRunNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
        graphBuilder.ConnectConstant(editorRunNode.StreamLogs, Settings.StreamLogsToMainEditor);
        graphBuilder.ConnectConstant(editorRunNode.LogsColor, Settings.LogsColor);
    }

    protected internal override VisualElement CreateControllerUI(Instance instance)
    {
        return new CloneEditorInstanceStatusElement(instance, Settings, GetUserSettingsSerializedProperty(new UserSettings { KeepAliveEnabled = k_DefaultKeepAliveEnabled }));
    }

    protected internal override VisualElement CreateTitleBarUI(Instance instance)
    {
        var focusButton = new VisualElement();
        focusButton.AddToClassList("focus-icon");
        focusButton.AddToClassList("icon");
        focusButton.RegisterCallback<MouseDownEvent>(evt =>
        {
            evt.StopImmediatePropagation();
        }, TrickleDown.TrickleDown);
        focusButton.RegisterCallback<ClickEvent>(evt =>
        {
            MultiplayerPlaymodeEditorUtility.FocusPlayerView(
                (PlayerIndex)Settings.PlayerInstanceIndex + 1);
            evt.StopImmediatePropagation();
        });
        
        return focusButton;
    }
}
