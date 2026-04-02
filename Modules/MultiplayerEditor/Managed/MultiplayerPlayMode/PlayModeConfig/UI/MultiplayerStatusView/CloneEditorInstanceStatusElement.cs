// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Multiplayer.Internal;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Unity.PlayMode.Editor;
using UnityEngine;
using System.ComponentModel;
using UnityEngine.Multiplayer.Internal;
using UnityEngine.PlayerLoop;

namespace Unity.Multiplayer.PlayMode.Editor;

internal class CloneEditorInstanceStatusElement : VisualElement
{
    internal const string k_ActivationButtonClass = "unity-instance-status__activate-button";
    internal const string k_LogInfoIcon = "LogInfoIcon";
    internal const string k_LogWarningIcon = "LogWarningIcon";
    internal const string k_LogErrorIcon = "LogErrorIcon";
    private const string k_KeepAliveLabel = "Keep Active";
    private const string k_LogsValuesContainerClass = "unity-instance-status__logs-info-container";
    private const string k_InstanceButtonCloneActivateText = "Activate";
    private const string k_InstanceButtonCloneDeactivateText = "Deactivate";
    private const string k_InstanceCancelText = "Cancel";
    private const string k_LogsContainerClass = "unity-instance-status__log-container";

    private Instance m_Instance;
    private Button m_ActivateButton;
    private Button m_DeactivateButton;


    private Label m_LogInfoText;
    private Label m_LogWarningText;
    private Label m_LogErrorText;
    private UnityPlayer m_Player;

    internal CloneEditorInstanceStatusElement(Instance instance, CloneEditorController.InstanceSettings settings, SerializedProperty usersettings)
    {
        RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
        RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        m_Instance = instance;
        m_Player = MultiplayerPlaymode.Players[settings.PlayerInstanceIndex];
        Add(CreatePills(settings.RoleMask, settings.PlayerTag));

        var keepAliveProperty = usersettings.FindPropertyRelative(nameof(CloneEditorController.UserSettings.KeepAliveEnabled));
        var keepAliveField = new PropertyField(keepAliveProperty) { label = k_KeepAliveLabel };
        keepAliveField.BindProperty(keepAliveProperty);
        keepAliveField.AddToClassList("unity-base-field__aligned");
        Add(keepAliveField);

        Add(new CloneEditorLogsField("Logs", CreateLogsField()));

        var activateButton = new Button();
        var deactivateButton = new Button();
        activateButton.AddToClassList(k_ActivationButtonClass);
        deactivateButton.AddToClassList(k_ActivationButtonClass);
        activateButton.RegisterCallback<ClickEvent>(OnActivateButtonClicked);
        deactivateButton.RegisterCallback<ClickEvent>(OnDeactivateButtonClicked);
        m_ActivateButton = activateButton;
        m_DeactivateButton = deactivateButton;
        m_ActivateButton.text = k_InstanceButtonCloneActivateText;
        m_DeactivateButton.text = k_InstanceButtonCloneDeactivateText;


        Add(activateButton);
        Add(deactivateButton);

        // TODO: we don't want this , we need to add an event everytime the clone's state has changed
        schedule.Execute(() =>
        {
            UpdateUI();
        }).Every(1000);
    }

    VisualElement CreatePills(MultiplayerRoleFlags role, string playerTag)
    {
        var container = new VisualElement();
        container.AddToClassList(EditorInstanceStatusElement.k_PillContainerClass);

        if (EditorMultiplayerManager.enableMultiplayerRoles)
        {
            var rolePill = new Label(role.ToString());
            rolePill.AddToClassList(EditorInstanceStatusElement.k_PillClass, EditorInstanceStatusElement.k_RolePillClass);
            container.Add(rolePill);
        }

        if (!string.IsNullOrEmpty(playerTag))
        {
            var tagPill = new Label(playerTag);
            tagPill.AddToClassList(EditorInstanceStatusElement.k_PillClass, EditorInstanceStatusElement.k_TagPillClass);
            container.Add(tagPill);
        }

        return container;
    }

    internal class CloneEditorLogsField(string label, VisualElement visualInput)
        : BaseField<CloneEditorLogsData>(label, visualInput);

     VisualElement CreateLogsField()
    {
        var container = new VisualElement();
        container.AddToClassList("unity-base-field");
        container.AddToClassList("unity-base-field__aligned");

        var logsValuesContainer = new VisualElement();
        logsValuesContainer.AddToClassList(k_LogsValuesContainerClass);

        m_LogInfoText = new Label();
        m_LogWarningText = new Label();
        m_LogErrorText = new Label();

        var logInfoIcon = new VisualElement() { name = k_LogInfoIcon };
        var logWarningIcon = new VisualElement() { name = k_LogWarningIcon };
        var logErrorIcon = new VisualElement() { name = k_LogErrorIcon };

        logInfoIcon.AddToClassList("icon");
        logWarningIcon.AddToClassList("icon");
        logErrorIcon.AddToClassList("icon");

        logsValuesContainer.Add(logInfoIcon);
        logsValuesContainer.Add(m_LogInfoText);

        logsValuesContainer.Add(logWarningIcon);
        logsValuesContainer.Add(m_LogWarningText);

        logsValuesContainer.Add(logErrorIcon);
        logsValuesContainer.Add(m_LogErrorText);
        logsValuesContainer.AddToClassList(k_LogsContainerClass);

        container.Add(logsValuesContainer);
        return container;
    }

    private void OnAttachToPanel(AttachToPanelEvent evt)
    {
        ScenarioRunner.StatusChanged += UpdateInstanceStatus;
        UpdateUI();
    }

    private void OnDetachFromPanel(DetachFromPanelEvent evt)
    {
        ScenarioRunner.StatusChanged -= UpdateInstanceStatus;
    }

    void UpdateInstanceStatus(ScenarioStatusData scenarioStatus)
    {
        UpdateUI();
    }

    private void OnActivateButtonClicked(ClickEvent ev)
    {
        if (m_Instance == null)
        {
            Debug.LogWarning("Unable to toggle instance - Please ensure Scenario configurations are valid.");
            return;
        }
        var isRunning = IsCloneActiveOrActivating();

        if (isRunning)
        {
            Debug.LogWarning("Clone Editor Instance Status Element Error: Cannot activate instance that is already running.");
            return;
        }
        var args = new List<string> { CommandLineParameters.k_ScenarioClone };
        m_Player.Activate(out _, args);
        UpdateUI();

    }

    private void OnDeactivateButtonClicked(ClickEvent evt)
    {
        if (m_Instance == null)
        {
            Debug.LogWarning("Unable to toggle instance - Please ensure Scenario configurations are valid.");
            return;
        }
        var isRunning = IsCloneActiveOrActivating();

        if (!isRunning)
        {
            Debug.LogWarning("Clone Editor Instance Status Element Error: Cannot deactivate instance that is not running.");
            return;
        }

        m_Player.Deactivate(out _);
        UpdateUI();

    }


    private void UpdateUI()
    {
        UpdateActivationButtons();
        AssignLogs();
    }

    private void UpdateActivationButtons()
    {
        var isInstanceExecuting = m_Instance.StatusData.IsExecuting();
        var isInPlayMode = UnityEditor.EditorApplication.isPlaying;
        var shouldEnable = !isInPlayMode &&  !isInstanceExecuting;
        var isLaunched = m_Player.PlayerState == PlayerState.Launched;
        var isLaunching = m_Player.PlayerState == PlayerState.Launching;
        m_ActivateButton.SetEnabled(shouldEnable);
        m_DeactivateButton.SetEnabled(shouldEnable);

        m_ActivateButton.style.display = isLaunched || isLaunching ? DisplayStyle.None : DisplayStyle.Flex;
        m_DeactivateButton.style.display = isLaunched || isLaunching ? DisplayStyle.Flex : DisplayStyle.None;

        m_DeactivateButton.text = isLaunching ? k_InstanceCancelText : k_InstanceButtonCloneDeactivateText;

    }

    private bool IsCloneActiveOrActivating()
    {
        if (m_Player != null)
        {
            return m_Player.PlayerState == PlayerState.Launching || m_Player.PlayerState == PlayerState.Launched;
        }

        return false;
    }

    private void AssignLogs()
    {
        if ( m_Player != null && m_Player.PlayerState == PlayerState.Launched)
        {
            var logs = MultiplayerPlaymodeLogUtility.PlayerLogs(m_Player.PlayerIdentifier).LogCounts;
            m_LogInfoText.text = logs.Logs.ToString();
            m_LogWarningText.text = logs.Warnings.ToString();
            m_LogErrorText.text = logs.Errors.ToString();
        }
        else
        {
            m_LogInfoText.text = 0.ToString();
            m_LogWarningText.text = 0.ToString();
            m_LogErrorText.text = 0.ToString();
        }
    }
}

internal abstract class CloneEditorLogsData
{
}
