// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.PlayMode.Editor;
using UnityEditor;
using UnityEditor.Multiplayer.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

internal class CommonInstanceStatusElement : VisualElement
{
    internal const string k_InstanceViewClass = "instance-view";
    internal const string k_InstanceIconName = "instance-icon";
    internal const string k_InstanceRoleTagsContentName = "instance-role-tags-content";
    internal const string k_InstanceContainerName = "instance-container";
    internal const string k_StatusContainerName = "status-container";
    internal const string k_LogInfoIcon = "LogInfoIcon";
    internal const string k_LogWarningIcon = "LogWarningIcon";
    internal const string k_LogErrorIcon = "LogErrorIcon";
    internal const string k_WarnIcon = "WarnIcon";

    private InstanceDescription m_Instance;
    private Label m_ConnectedLabel;
    private FreeRunningStatusElement m_FreeRunningElement;

    internal Label LogInfoText;
    internal Label LogWarningText;
    internal Label LogErrorText;
    internal TextField IpAddress;
    internal TextField Port;
    internal Button IpCopyButton;
    internal Button PortCopyButton;
    internal UnityPlayer Player;
    internal TextField RunDevice;
    internal Label RunDeviceName;

    internal CommonInstanceStatusElement(InstanceDescription instance)
    {
        CreateContent(instance);
    }

    void CreateContent(InstanceDescription instance)
    {
        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

        m_Instance = instance;
        var warnIcon = new VisualElement() { name = k_WarnIcon };
        warnIcon.AddToClassList("icon");
        warnIcon.style.display = DisplayStyle.None;

        var instanceContainer = new VisualElement() { name = k_InstanceContainerName };
        instanceContainer.style.alignItems = Align.FlexStart;
        instanceContainer.style.justifyContent = Justify.FlexStart;

        var instanceNameContainer = new VisualElement();
        instanceNameContainer.AddToClassList("instance-view-name-container");
        instanceNameContainer.style.flexDirection = FlexDirection.Row;

        var instanceRoleAndTagsContainer = new VisualElement() { name = k_InstanceRoleTagsContentName };
        instanceRoleAndTagsContainer.AddToClassList("instance-role-tags-container");
        instanceRoleAndTagsContainer.style.paddingTop = 3;

        var instanceRunDeviceContainer = new VisualElement();
        instanceRunDeviceContainer.AddToClassList("instance-run-device-container");
        instanceRunDeviceContainer.style.display = DisplayStyle.None;

        var instanceRunModeContainer = new VisualElement();
        instanceRunModeContainer.AddToClassList("instance-content-runmode-container");

        var instanceSimulatorContainer = new VisualElement();

        var statusContainer = new VisualElement() { name = k_StatusContainerName };
        statusContainer.style.alignItems = Align.FlexEnd;
        statusContainer.style.justifyContent = Justify.FlexEnd;
        statusContainer.style.alignSelf = Align.Stretch;

        m_ConnectedLabel = new Label();
        var logInfoIcon = new VisualElement() { name = k_LogInfoIcon };
        logInfoIcon.AddToClassList("icon");
        LogInfoText = new Label();
        LogWarningText = new Label();
        LogErrorText = new Label();
        var logWarningIcon = new VisualElement() { name = k_LogWarningIcon };
        logWarningIcon.AddToClassList("icon");
        var logErrorIcon = new VisualElement() { name = k_LogErrorIcon };
        logErrorIcon.AddToClassList("icon");

        var freeRunButtonContainer = new VisualElement();
        var statusContentContainer = new VisualElement();
        var statusFocusBtnContainer = new VisualElement();
        var statusRunDeviceContainer = new VisualElement();
        var statusRunmodeContainer = new VisualElement();
        statusContentContainer.AddToClassList("status-content-container");
        statusContentContainer.style.flexDirection = FlexDirection.Row;
        statusContainer.style.flexDirection = FlexDirection.Column;
        statusContentContainer.Add(warnIcon);
        statusContentContainer.Add(logInfoIcon);
        statusContentContainer.Add(LogInfoText);
        statusContentContainer.Add(logWarningIcon);
        statusContentContainer.Add(LogWarningText);
        statusContentContainer.Add(logErrorIcon);
        statusContentContainer.Add(LogErrorText);

        var roleLabel = new Label();
        instanceRoleAndTagsContainer.Add(roleLabel);
        // Todo: this info should come from the configuration and not be branched here.
        if (instance is EditorInstanceDescription editorInstanceDescription)
        {
            Player = MultiplayerPlaymode.Players[editorInstanceDescription.PlayerInstanceIndex];
            bool hasTag = editorInstanceDescription.PlayerTag != "";
            if (hasTag)
            {
                var pill = new Pill();
                pill.Text = editorInstanceDescription.PlayerTag;
                pill.AddToClassList("player-tag-pill");
                instanceRoleAndTagsContainer.Add(pill);
            }

            if (editorInstanceDescription.Name.Contains("Main"))
            {
                logInfoIcon.style.display = DisplayStyle.None;
                logWarningIcon.style.display = DisplayStyle.None;
                logErrorIcon.style.display = DisplayStyle.None;
                LogInfoText.style.display = DisplayStyle.None;
                LogWarningText.style.display = DisplayStyle.None;
                LogErrorText.style.display = DisplayStyle.None;
            }

            if (editorInstanceDescription.Name.Contains("Player"))
            {
                // Build the button for focusing the Clone Editors
                var focusButton = new VisualElement() { name = k_InstanceIconName };
                focusButton.AddToClassList("focus-icon");
                focusButton.AddToClassList("icon");
                focusButton.RegisterCallback<ClickEvent>(evt =>
                    MultiplayerPlaymodeEditorUtility.FocusPlayerView(
                        (PlayerIndex)editorInstanceDescription.PlayerInstanceIndex + 1));
                statusFocusBtnContainer.style.marginTop = -14;
                statusFocusBtnContainer.style.marginBottom = 4;
                statusFocusBtnContainer.Add(focusButton);

                // Add manual UI adjustments when Tag controls are included
                if (hasTag)
                {
                    instanceRunModeContainer.style.paddingTop = 0;
                    statusFocusBtnContainer.style.marginTop = -10;
                    statusFocusBtnContainer.style.marginBottom = 7;
                }

                // Finally build and bind Free Running UI Elements if it applies
                if (editorInstanceDescription is VirtualEditorInstanceDescription virtualDescription)
                {
                    m_FreeRunningElement = new FreeRunningStatusElement(virtualDescription);
                    m_FreeRunningElement.BindRunModeDropDownElement(
                        instanceRunModeContainer,
                        statusRunmodeContainer,
                        freeRunButtonContainer,
                        this);
                }
            }
            roleLabel.style.display = EditorMultiplayerManager.enableMultiplayerRoles ? DisplayStyle.Flex : DisplayStyle.None;
            if (EditorMultiplayerManager.enableMultiplayerRoles)
                roleLabel.text = editorInstanceDescription.RoleMask.ToString();
        }

        if (instance is LocalInstanceDescription localInstanceDescription)
        {
            logInfoIcon.style.display = DisplayStyle.None;
            logWarningIcon.style.display = DisplayStyle.None;
            logErrorIcon.style.display = DisplayStyle.None;
            LogInfoText.style.display = DisplayStyle.None;
            LogWarningText.style.display = DisplayStyle.None;
            LogErrorText.style.display = DisplayStyle.None;
            roleLabel.text = "no role";
            if (localInstanceDescription.BuildProfile != null)
                roleLabel.text = MultiplayerRolesSettings.instance
                    .GetMultiplayerRoleForBuildProfile(localInstanceDescription.BuildProfile).ToString();
            if (InternalUtilities.IsAndroidBuildTarget(localInstanceDescription.BuildProfile))
            {
                RunDevice = new TextField() { isReadOnly = true, focusable = false };
                RunDeviceName = new Label("Run Device");
                RunDevice.SetEnabled(false);

                if (localInstanceDescription.AdvancedConfiguration.DeviceName == "")
                {
                    RunDevice.SetValueWithoutNotify("No Device Selected");
                    warnIcon.style.display = DisplayStyle.Flex;
                    warnIcon.tooltip = "Select a device using the Configuration Window";
                }
                else
                {
                    RunDevice.SetValueWithoutNotify(localInstanceDescription.AdvancedConfiguration.DeviceName);
                    warnIcon.style.display = DisplayStyle.None;
                }
                RunDevice.tooltip = "Selected device the instance will run on";
                RunDevice.AddToClassList("unity-base-field__aligned");
                RunDevice.style.top = 15;
                RunDevice.style.alignContent = Align.FlexEnd;
                RunDevice.style.width = 200;
                RunDeviceName.style.top = 5;
                RunDeviceName.style.bottom = 5;
                instanceRunDeviceContainer.Add(RunDeviceName);
                statusRunDeviceContainer.Add(RunDevice);
                instanceRunDeviceContainer.style.display = DisplayStyle.Flex;
                instanceRunModeContainer.style.marginTop = 13;
                statusRunmodeContainer.style.marginTop = 20;
            }
            else
            {
                statusRunmodeContainer.style.marginTop = 5;
            }

            m_FreeRunningElement = new FreeRunningStatusElement(localInstanceDescription);
            m_FreeRunningElement.BindRunModeDropDownElement(
                instanceRunModeContainer,
                statusRunmodeContainer,
                freeRunButtonContainer,
                this);
            freeRunButtonContainer.style.marginTop = -6;
        }

        statusContainer.Add(statusFocusBtnContainer);
        statusContainer.Add(statusRunDeviceContainer);
        statusContainer.Add(statusRunmodeContainer);
        statusContainer.Add(statusContentContainer);

        instanceContainer.Add(instanceNameContainer);
        instanceContainer.Add(instanceRoleAndTagsContainer);
        instanceContainer.Add(instanceRunDeviceContainer);
        instanceContainer.Add(instanceRunModeContainer);
        instanceContainer.Add(instanceSimulatorContainer);

        var parentContainer = new VisualElement();
        parentContainer.Add(instanceContainer);
        parentContainer.Add(statusContainer);
        parentContainer.AddToClassList(k_InstanceViewClass);

        Add(parentContainer);
        Add(freeRunButtonContainer);
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        ScenarioRunner.StatusChanged += UpdateInstanceStatus;
        RefreshStatusUI();
    }

    private void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        ScenarioRunner.StatusChanged -= UpdateInstanceStatus;
    }

    void UpdateInstanceStatus(ScenarioStatusData scenarioStatus)
    {
        RefreshStatusUI();
    }

    internal void CopyTextToClipboard(string text)
    {
        // Copy the text field's value to clipboard
        GUIUtility.systemCopyBuffer = text;
    }

    private void CleanUpStatus()
    {
        m_ConnectedLabel.text = string.Empty;
    }

    private void AssignLogs(ExecutionState state)
    {
        if (m_Instance is not EditorInstanceDescription) return; // only Editor Instance have Logs
        switch (state)
        {
            case ExecutionState.Active:
                if (Player != null)
                {
                    var logs = MultiplayerPlaymodeLogUtility.PlayerLogs(Player.PlayerIdentifier).LogCounts;
                    LogInfoText.text = logs.Logs.ToString();
                    LogWarningText.text = logs.Warnings.ToString();
                    LogErrorText.text = logs.Errors.ToString();
                }

                break;
            default:
                LogInfoText.text = 0.ToString();
                LogWarningText.text = 0.ToString();
                LogErrorText.text = 0.ToString();
                break;
        }
    }

    Node GetConnectableNode(string nodeName)
    {
        if (ScenarioRunner.instance.ActiveScenario == null)
            return null;

        var runNodes = ScenarioRunner.instance.ActiveScenario.GetNodes(ExecutionStage.Run);
        foreach (var node in runNodes)
        {
            if (node.Name == nodeName && node is IConnectableNode)
                return node;
        }

        return null;
    }

    internal void RefreshStatusUI()
    {
        CleanUpStatus();

        var instanceExecutionState = GetInstanceForThisElement().StatusData.OverallStatus.State;

        AssignLogs(instanceExecutionState);
    }

    private Instance GetInstanceForThisElement()
    {
        var currentConfig = PlayModeScenarioManager.ActiveScenario as OrchestratedScenario;
        if (currentConfig == null || currentConfig.Scenario == null)
            return null;

        return currentConfig.Scenario.GetInstanceByName(m_Instance.Name);
    }
}
