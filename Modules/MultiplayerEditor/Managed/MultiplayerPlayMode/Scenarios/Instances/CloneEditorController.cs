// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.PlayMode.Editor;
using UnityEditor;
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

    [Serializable]
    struct CloneEditorAnalyticsData : ICustomInstanceAnalyticsData
    {
        public bool IsKeepActive;
        public bool IsEditorActiveOnStart;
        public string ToJsonString() => JsonUtility.ToJson(this);
    }

    internal const bool k_DefaultKeepAliveEnabled = true;

    internal override string GetTypeNameForAnalytics() => "VirtualEditor";

    protected internal override ICustomInstanceAnalyticsData GetCustomAnalyticsData(ExecutionGraph graph)
    {
        var keepActive = GetUserSettings(
            new UserSettings { KeepAliveEnabled = k_DefaultKeepAliveEnabled }).KeepAliveEnabled;
        var editorActiveOnStart = false;
        foreach (var node in graph.GetNodes(ExecutionStage.Deploy))
        {
            if (node is CloneEditorDeployNode deployNode)
            {
                editorActiveOnStart = deployNode.AlreadyActive.GetValue<bool>();
                break;
            }
        }
        return new CloneEditorAnalyticsData { IsKeepActive = keepActive, IsEditorActiveOnStart = editorActiveOnStart };
    }

    protected internal override void SetupExecutionGraph(ExecutionGraphBuilder graphBuilder)
    {
        var players = MultiplayerPlaymode.Players;
        var player = players != null && Settings.PlayerInstanceIndex < players.Length
            ? players[Settings.PlayerInstanceIndex]
            : null;

        if (EditorMultiplayerManager.enableMultiplayerRoles && player != null)
        {
            var roleNode = graphBuilder.AddNode<SetupEditorMultiplayerRoleNode>(ExecutionStage.Deploy);
            graphBuilder.ConnectConstant(roleNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
            graphBuilder.ConnectConstant(roleNode.Role, Settings.RoleMask);

            var restoreRoleNode = graphBuilder.AddNode<SetupEditorMultiplayerRoleNode>(ExecutionStage.Cleanup);
            graphBuilder.ConnectConstant(restoreRoleNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
            graphBuilder.ConnectConstant(restoreRoleNode.Role, player.Role);
        }

        if (player != null)
        {
            var tagsNode = graphBuilder.AddNode<SetupEditorTagsNode>(ExecutionStage.Deploy);
            graphBuilder.ConnectConstant(tagsNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
            graphBuilder.ConnectConstant(tagsNode.Tags, new[] { Settings.PlayerTag });

            var restoreTagsNode = graphBuilder.AddNode<SetupEditorTagsNode>(ExecutionStage.Cleanup);
            graphBuilder.ConnectConstant(restoreTagsNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
            graphBuilder.ConnectConstant(restoreTagsNode.Tags, player.Tags);
        }


        var deployNode = graphBuilder.AddNode<CloneEditorDeployNode>(ExecutionStage.Deploy);
        graphBuilder.ConnectConstant(deployNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);

        var editorRunNode = graphBuilder.AddNode<CloneEditorRunNode>(ExecutionStage.Run);
        graphBuilder.ConnectConstant(editorRunNode.PlayerInstanceIndex, Settings.PlayerInstanceIndex);
        graphBuilder.ConnectConstant(editorRunNode.StreamLogs, Settings.StreamLogsToMainEditor);
        graphBuilder.ConnectConstant(editorRunNode.LogsColor, Settings.LogsColor);
    }
    private static void RefreshVirtualProjectFolder()
    {
        VirtualProjectWorkflow.ClearVirtualProjectFolder();
    }
    [ActiveScenarioWindowMenu]
    public static void SetupActiveScenarioWindowMenu(GenericMenu menu)
    {
        if (!HasActiveCloneEditors())
            menu.AddItem(new GUIContent("Refresh Virtual Project Folder"), false, RefreshVirtualProjectFolder);
        else
            menu.AddDisabledItem(new GUIContent("Refresh Virtual Project Folder"));
    }

    internal static bool HasActiveCloneEditors()
    {
        var players = MultiplayerPlaymode.Players;
        if (players == null)
            return false;

        foreach (var player in players)
        {
            if (player.PlayerState is PlayerState.Launched or PlayerState.Launching && player.PlayerIdentifier != MultiplayerPlaymode.PlayerOne.PlayerIdentifier)
                return true;
        }

        return false;
    }

    // An activated clone slot left behind on remove/switch orphans the virtual player (UUM-138111).
    internal override bool NeedsTearDown(Instance instance, out string reason)
    {
        if (IsActiveCloneSlot())
        {
            reason = $"\"{instance.Name}\" is an activated virtual player.";
            return true;
        }

        reason = null;
        return false;
    }

    internal override void TearDown(Instance instance)
    {
        if (IsActiveCloneSlot())
            MultiplayerPlaymode.Players[Settings.PlayerInstanceIndex].Deactivate(out _);
    }

    bool IsActiveCloneSlot()
    {
        var players = MultiplayerPlaymode.Players;
        if (players == null)
            return false;

        var index = Settings.PlayerInstanceIndex;
        if (index < 0 || index >= players.Length)
            return false;

        var player = players[index];
        // Clone indices are assigned >= 1, but guard against a stale one resolving to the main editor.
        if (player.PlayerIdentifier == MultiplayerPlaymode.PlayerOne.PlayerIdentifier)
            return false;

        return player.PlayerState is PlayerState.Launched or PlayerState.Launching;
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
