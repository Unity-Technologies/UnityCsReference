// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Multiplayer.Internal;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

internal class CloneEditorInstanceStatusElement : VisualElement
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

    private Instance m_Instance;
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

    internal CloneEditorInstanceStatusElement(Instance instance, CloneEditorController.InstanceSettings settings)
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

        Player = MultiplayerPlaymode.Players[settings.PlayerInstanceIndex];
        bool hasTag = settings.PlayerTag != "";
        if (hasTag)
        {
            var tagPill = new Label(settings.PlayerTag);
            tagPill.AddToClassList(EditorInstanceStatusElement.k_PillClass, EditorInstanceStatusElement.k_TagPillClass);
            instanceRoleAndTagsContainer.Add(tagPill);
        }

        // Build the button for focusing the Clone Editors
        var focusButton = new VisualElement() { name = k_InstanceIconName };
        focusButton.AddToClassList("focus-icon");
        focusButton.AddToClassList("icon");
        focusButton.RegisterCallback<ClickEvent>(evt =>
            MultiplayerPlaymodeEditorUtility.FocusPlayerView(
                (PlayerIndex)settings.PlayerInstanceIndex + 1));
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

        m_FreeRunningElement = new FreeRunningStatusElement(instance);
        m_FreeRunningElement.BindRunModeDropDownElement(
            instanceRunModeContainer,
            statusRunmodeContainer,
            freeRunButtonContainer);

        roleLabel.style.display = EditorMultiplayerManager.enableMultiplayerRoles ? DisplayStyle.Flex : DisplayStyle.None;
        if (EditorMultiplayerManager.enableMultiplayerRoles)
            roleLabel.text = ObjectNames.NicifyVariableName(settings.RoleMask.ToString());

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

    private void CleanUpStatus()
    {
        m_ConnectedLabel.text = string.Empty;
    }

    private void AssignLogs(InstanceStatusData status)
    {
        if (status.IsExecutingRunningStage())
        {
            if (Player != null)
            {
                var logs = MultiplayerPlaymodeLogUtility.PlayerLogs(Player.PlayerIdentifier).LogCounts;
                LogInfoText.text = logs.Logs.ToString();
                LogWarningText.text = logs.Warnings.ToString();
                LogErrorText.text = logs.Errors.ToString();
            }
        }
        else
        {
            LogInfoText.text = 0.ToString();
            LogWarningText.text = 0.ToString();
            LogErrorText.text = 0.ToString();
        }
    }

    internal void RefreshStatusUI()
    {
        CleanUpStatus();
        AssignLogs(m_Instance.StatusData);
    }
}
