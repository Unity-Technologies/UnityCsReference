// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.PlayMode.Editor;
using UnityEditor.Multiplayer.Internal;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class PlaymodeStatusElement : VisualElement
    {
        const string k_Stylesheet = "Multiplayer/UI/PlaymodeStatusWindow.uss";
        const string k_StylesheetDark = "Multiplayer/UI/PlaymodeStatusWindowDark.uss";
        const string k_StylesheetLight = "Multiplayer/UI/PlaymodeStatusWindowLight.uss";

        const string k_HeadlineName = "headline";
        const string k_InstanceListContainer = "status-list";
        const string k_EditorContainerName = "editor-instance-list";
        internal const string k_HelpBoxName = "help-box";
        static List<InstanceView> m_InstanceViewList = new();

        public PlaymodeStatusElement()
        {
            CreateGUI();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            ScenarioRunner.StatusChanged += UpdateInstanceStatus;
            Scenario currentScenario = ScenarioRunner.instance.ActiveScenario;
            if (currentScenario != null && ScenarioRunner.instance.IsRunning)
            {
                ScenarioStatus currentStatus = currentScenario.Status;
                UpdateInstanceStatus(currentStatus);
            }
            else
            {
                UpdateInstanceStatus(ScenarioStatus.Invalid);
            }
        }


        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            ScenarioRunner.StatusChanged -= UpdateInstanceStatus;
        }

        private void AttachHelpBox(VisualElement container, string prompt, HelpBoxMessageType promptType)
        {
            var helpBox = new HelpBox(prompt,promptType) { name = k_HelpBoxName };
            helpBox.AddToClassList("help-box");
            helpBox.styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
            container.Add(helpBox);
            Add(container);
        }

        private void CreateGUI()
        {
            var container = new ScrollView() { name = k_InstanceListContainer };
            var activeConfig = PlayModeManager.instance.ActivePlayModeConfig;

            // If the default config is set, show the default Help box.
            if (activeConfig.name == "Default")
            {
                var prompt = "No Play Mode Scenario selected. Please use the dropdown next to the play button to switch to a scenario.";
                AttachHelpBox(container, prompt, HelpBoxMessageType.Info);
                Add(container);
                return;
            }

            // Show the Edit Configuration Button.
            var configButton = new Button(); // Add a button to open the config window
            configButton.AddToClassList("config-button");
            configButton.text = "Edit Configuration";
            configButton.RegisterCallback<ClickEvent>(evt => PlayModeConfigurationsWindow.ShowWindow());
            container.Add(configButton);

            container.styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
            var stylesheet = EditorGUIUtility.isProSkin ? k_StylesheetDark : k_StylesheetLight;
            container.styleSheets.Add(EditorGUIUtility.LoadRequired(stylesheet) as StyleSheet);

            // If not a Scenario Configuration, nothing else to do, return.
            var currentConfig = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;
            if (currentConfig == null)
                return;

            // Display warnings if the scenario configuration is invalid
            var isValid = currentConfig.IsConfigurationValid(out _);
            if (!isValid)
            {
                var prompt = "This Scenario is not setup properly. Use the Edit Configuration button to fix the issues.";
                AttachHelpBox(container, prompt, HelpBoxMessageType.Warning);
                Add(container);
                return;
            }

            // Create the Instance Titles and add the instances
            var editorContainer = new VisualElement() { name = k_EditorContainerName };
            var editorTitle = new Label("Editor") { name = k_HeadlineName };
            editorTitle.style.left = -5;
            editorContainer.Add(editorTitle);
            var localContainer = new VisualElement();
            var localTitle = new Label("Local Instances") { name = k_HeadlineName };
            localTitle.style.left = -5;
            localContainer.Add(localTitle);
            var remoteContainer = new VisualElement();
            var remoteTitle = new Label("Remote Instances") { name = k_HeadlineName };
            remoteTitle.style.left = -5;
            remoteContainer.Add(remoteTitle);

            m_InstanceViewList.Clear();
            var instances = currentConfig.GetAllInstances();
            foreach (var instance in instances)
            {
                var instanceView = new InstanceView(instance);
                m_InstanceViewList.Add(instanceView);

                if (instance is EditorInstanceDescription)
                {
                    editorContainer.Add(instanceView);
                }
                else if (instance is LocalInstanceDescription)
                {
                    localContainer.Add(instanceView);
                }
                else if (instance is RemoteInstanceDescription)
                {
                    remoteContainer.Add(instanceView);
                }
            }

            if (editorContainer.childCount <= 1)
            {
                editorContainer.style.display = DisplayStyle.None;
            }

            if (remoteContainer.childCount <= 1)
            {
                remoteTitle.style.display = DisplayStyle.None;
            }

            if (localContainer.childCount <= 1)
            {
                localTitle.style.display = DisplayStyle.None;
            }

            container.Add(editorContainer);
            container.Add(localContainer);
            container.Add(remoteContainer);
            Add(container);
        }

        Node GetRunNodeForNodeName(string nodeName)
        {
            if (ScenarioRunner.instance.ActiveScenario == null)
                return null;

            var runNodes = ScenarioRunner.instance.ActiveScenario.GetNodes(ExecutionStage.Run);
            foreach (var node in runNodes)
            {
                if (node.Name == nodeName)
                    return node;
            }

            return null;
        }

        static void UpdateInstanceStatus(ScenarioStatus scenarioStatus)
        {
            foreach (var view in m_InstanceViewList)
            {
                view.SetScenarioStatus(scenarioStatus);
            }
        }

        internal class InstanceView : VisualElement
        {
            private InstanceDescription m_Instance;
            internal VisualElement StatusIndicator;
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
            internal Label StatusLabel;
            private Label m_ConnectedLabel;
            private FreeRunningStatusElement m_FreeRunningElement;
            private MultiplaySimulatorStatusElement m_SimulatorStatusElement;
            internal const string k_InstanceViewClass = "instance-view";
            internal const string k_InstanceIconName = "instance-icon";
            internal const string k_InstanceNameName = "instance-name";
            internal const string k_InstanceRoleTagsContentName = "instance-role-tags-content";
            internal const string k_InstanceContainerName = "instance-container";
            internal const string k_StatusContainerName = "status-container";
            internal const string k_StatusIndicatorName = "status-indicator";
            internal const string k_StatusLabelName = "status-label";
            internal const string k_LogInfoIcon = "LogInfoIcon";
            internal const string k_LogWarningIcon = "LogWarningIcon";
            internal const string k_LogErrorIcon = "LogErrorIcon";
            internal const string k_WarnIcon = "WarnIcon";

            internal const string k_ActiveClass = "active";
            internal const string k_ErrorClass = "error";
            internal const string k_IdleClass = "idle";
            internal const string k_LoadingClass = "loading";

            internal InstanceView(InstanceDescription instance)
            {
                m_Instance = instance;
                style.flexGrow = 0;
                style.paddingTop = 5;
                style.paddingBottom = 5;

                var instanceIcon = new VisualElement() { name = k_InstanceIconName };
                instanceIcon.AddToClassList("icon");
                var nameLabel = new Label(instance.Name) { name = k_InstanceNameName };
                var warnIcon = new VisualElement() { name = k_WarnIcon };
                warnIcon.AddToClassList("icon");
                warnIcon.style.display = DisplayStyle.None;

                var instanceContainer = new VisualElement() { name = k_InstanceContainerName };
                instanceContainer.style.alignItems = Align.FlexStart;
                instanceContainer.style.justifyContent = Justify.FlexStart;

                var instanceNameContainer = new VisualElement();
                instanceNameContainer.AddToClassList("instance-view-name-container");
                instanceNameContainer.style.flexDirection = FlexDirection.Row;
                instanceNameContainer.Add(instanceIcon);
                instanceNameContainer.Add(nameLabel);

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

                StatusLabel = new Label() { name = k_StatusLabelName }; ;
                StatusIndicator = new VisualElement() { name = k_StatusIndicatorName };
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
                StatusIndicator.AddToClassList("icon");

                var freeRunButtonContainer = new VisualElement();
                var statusContentContainer = new VisualElement();
                var statusFocusBtnContainer = new VisualElement();
                var statusRunDeviceContainer = new VisualElement();
                var statusRunmodeContainer = new VisualElement();
                var statusLabelContainer = new VisualElement();
                statusLabelContainer.AddToClassList("status-label-container");
                statusContentContainer.AddToClassList("status-content-container");
                statusContentContainer.style.flexDirection = FlexDirection.Row;
                statusLabelContainer.style.flexDirection = FlexDirection.Row;
                statusContainer.style.flexDirection = FlexDirection.Column;
                statusContentContainer.Add(warnIcon);
                statusLabelContainer.Add(StatusIndicator);
                statusLabelContainer.Add(StatusLabel);
                statusLabelContainer.Add(m_ConnectedLabel);
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
                    instanceIcon.style.backgroundImage = EditorGUIUtility.FindTexture("UnityLogo");
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
                    roleLabel.text = editorInstanceDescription.RoleMask.ToString();
                }

                if (instance is LocalInstanceDescription localInstanceDescription)
                {
                    instanceIcon.style.backgroundImage =
                        InternalUtilities.GetBuildProfileTypeIcon(localInstanceDescription.BuildProfile);

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

                    if (LocalDeploymentUtility.ShouldEnableLocalDeployment(localInstanceDescription))
                    {
                        m_SimulatorStatusElement = new MultiplaySimulatorStatusElement(localInstanceDescription);
                        m_SimulatorStatusElement.BindSimulatedFoldOutElement(
                            instanceRunModeContainer,
                            statusRunmodeContainer,
                            instanceSimulatorContainer,
                            this);
                        instanceSimulatorContainer.style.marginTop = 10;
                    }
                }

                if (instance is RemoteInstanceDescription remoteInstanceDescription)
                {
                    IpAddress = new TextField();
                    Port = new TextField();
                    IpAddress.isReadOnly = true;
                    Port.isReadOnly = true;
                    IpAddress.style.display = DisplayStyle.None;
                    Port.style.display = DisplayStyle.None;
                    IpCopyButton = new Button();
                    PortCopyButton = new Button();
                    IpCopyButton.tooltip = "Copy IP Address To ClipBoard";
                    PortCopyButton.tooltip = "Copy Port To ClipBoard";
                    IpCopyButton.RegisterCallback<ClickEvent>(evt => CopyTextToClipboard(IpAddress.value));
                    PortCopyButton.RegisterCallback<ClickEvent>(evt => CopyTextToClipboard(Port.value));
                    IpCopyButton.iconImage = EditorGUIUtility.FindTexture("Clipboard");
                    PortCopyButton.iconImage = EditorGUIUtility.FindTexture("Clipboard");
                    IpCopyButton.style.display = DisplayStyle.None;
                    PortCopyButton.style.display = DisplayStyle.None;
                    instanceIcon.style.backgroundImage =
                        InternalUtilities.GetBuildProfileTypeIcon(remoteInstanceDescription.BuildProfile);
                    logInfoIcon.style.display = DisplayStyle.None;
                    logWarningIcon.style.display = DisplayStyle.None;
                    logErrorIcon.style.display = DisplayStyle.None;
                    LogInfoText.style.display = DisplayStyle.None;
                    LogWarningText.style.display = DisplayStyle.None;
                    LogErrorText.style.display = DisplayStyle.None;
                    roleLabel.text = "no role";
                    if (remoteInstanceDescription.BuildProfile != null)
                        roleLabel.text = MultiplayerRolesSettings.instance
                            .GetMultiplayerRoleForBuildProfile(remoteInstanceDescription.BuildProfile).ToString();
                    var linkToDashboard = new VisualElement();
                    linkToDashboard.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        var orgId = CloudProjectSettings.organizationKey;
                        var projectId = CloudProjectSettings.projectId;

                        Application.OpenURL(
                            $"https://cloud.unity.com/home/organizations/{orgId}/projects/{projectId}/multiplay/overview");
                    });


                    linkToDashboard.AddToClassList("dashboard-link");
                    linkToDashboard.AddToClassList("icon");
                    statusFocusBtnContainer.style.marginTop = -14;
                    statusFocusBtnContainer.style.marginBottom = 4;
                    statusFocusBtnContainer.Add(linkToDashboard);
                    statusContentContainer.Add(IpAddress);
                    statusContentContainer.Add(IpCopyButton);
                    statusContentContainer.Add(Port);
                    statusContentContainer.Add(PortCopyButton);

                    m_FreeRunningElement = new FreeRunningStatusElement(remoteInstanceDescription);
                    m_FreeRunningElement.BindRunModeDropDownElement(
                        instanceRunModeContainer,
                        statusRunmodeContainer,
                        freeRunButtonContainer,
                        this);
                }

                statusContainer.Add(statusLabelContainer);
                statusContainer.Add(statusFocusBtnContainer);
                statusContainer.Add(statusRunDeviceContainer);
                statusContainer.Add(statusRunmodeContainer);
                statusContainer.Add(statusContentContainer);

                instanceContainer.Add(instanceNameContainer);
                instanceContainer.Add(instanceRoleAndTagsContainer);
                instanceContainer.Add(instanceRunDeviceContainer);
                instanceContainer.Add(instanceRunModeContainer);
                instanceContainer.Add(instanceSimulatorContainer);
                instanceContainer.AddToClassList(k_IdleClass);

                var parentContainer = new VisualElement();
                parentContainer.Add(instanceContainer);
                parentContainer.Add(statusContainer);
                parentContainer.AddToClassList(k_InstanceViewClass);

                Add(parentContainer);
                Add(freeRunButtonContainer);
            }

            internal void CopyTextToClipboard(string text)
            {
                // Copy the text field's value to clipboard
                GUIUtility.systemCopyBuffer = text;
            }

            private void CleanUpStatus()
            {
                RemoveFromClassList(k_ActiveClass);
                RemoveFromClassList(k_ErrorClass);
                RemoveFromClassList(k_IdleClass);

                StatusIndicator.tooltip = string.Empty;
                m_ConnectedLabel.text = string.Empty;
                StatusLabel.text = string.Empty;

                StatusLabel.style.display = DisplayStyle.None;

                if (m_Instance is RemoteInstanceDescription)
                {
                    Port.style.display = DisplayStyle.None;
                    IpAddress.style.display = DisplayStyle.None;
                    PortCopyButton.style.display = DisplayStyle.None;
                    IpCopyButton.style.display = DisplayStyle.None;
                }
            }

            private ExecutionStage ComputeInstanceStage(ScenarioStatus scenarioStatus)
            {
                return scenarioStatus.CurrentStage;
            }

            private List<NodeStatus> GetNodesStatusForInstance(ScenarioStatus scenarioStatus)
            {
                var nodesIds = m_Instance.GetCorrespondingNodes();
                var nodesStatus = new List<NodeStatus>();

                if (scenarioStatus.NodeStateReports != null)
                {
                    foreach (var nodeStatus in scenarioStatus.NodeStateReports)
                    {
                        if (nodesIds.Contains(nodeStatus.NodeName))
                            nodesStatus.Add(nodeStatus);
                    }
                }

                return nodesStatus;
            }

            private void AssignStatusIconClass(ExecutionState state, ExecutionStage stage)
            {
                if (stage == ExecutionStage.Run)
                {
                    switch (state)
                    {
                        case ExecutionState.Completed:
                            AddToClassList(k_IdleClass);
                            return;
                        case ExecutionState.Active:
                            AddToClassList(k_ActiveClass);
                            return;
                    }
                }

                switch (state)
                {
                    case ExecutionState.Idle:
                        AddToClassList(k_IdleClass);
                        break;
                    case ExecutionState.Completed:
                    case ExecutionState.Running:
                    case ExecutionState.Active:
                        AddToClassList(k_LoadingClass);
                        break;
                    case ExecutionState.Failed:
                    case ExecutionState.Aborted:
                    case ExecutionState.Invalid:
                    default:
                        AddToClassList(k_ErrorClass);
                        break;
                }
            }

            private void AssignStatusTooltip(ExecutionState state)
            {
                switch (state)
                {
                    case ExecutionState.Active:
                        StatusIndicator.tooltip = "active";
                        break;
                    case ExecutionState.Failed:
                        StatusIndicator.tooltip = "error";
                        break;
                    case ExecutionState.Completed:
                        StatusIndicator.tooltip = "completed";
                        break;
                    case ExecutionState.Running:
                        StatusIndicator.tooltip = "running";
                        break;
                    case ExecutionState.Idle:
                        StatusIndicator.tooltip = "idle";
                        break;
                    case ExecutionState.Invalid:
                        StatusIndicator.tooltip = "invalid";
                        break;
                    case ExecutionState.Aborted:
                        StatusIndicator.tooltip = "aborted";
                        break;
                    default:
                        StatusIndicator.tooltip = "idle";
                        break;
                }
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

            private void AssignIpAddress(ExecutionState state)
            {
                if (m_Instance is not RemoteInstanceDescription) return; // only Remote Instance have Ip info
                var nodes = m_Instance.GetCorrespondingNodes();

                Node connectableRemoteNode = null;
                foreach (var nodeName in nodes)
                {
                    var node = GetConnectableNode(nodeName);
                    if (node != null)
                    {
                        connectableRemoteNode = node;
                        break;
                    }
                }
                switch (state)
                {
                    case ExecutionState.Active:
                        if (IpAddress != null)
                        {
                            if (connectableRemoteNode is IConnectableNode connectableNode)
                            {
                                var ipAddress = connectableNode?.ConnectionDataOut?.GetValue<ConnectionData>()
                                    ?.IpAddress;
                                var port = connectableNode?.ConnectionDataOut?.GetValue<ConnectionData>()?.Port;

                                if (ipAddress != null)
                                    IpAddress.value = connectableNode?.ConnectionDataOut?.GetValue<ConnectionData>()
                                        .IpAddress;
                                Port.value = connectableNode?.ConnectionDataOut?.GetValue<ConnectionData>()?.Port
                                    .ToString();
                                IpAddress.style.display = DisplayStyle.Flex;
                                IpCopyButton.style.display = DisplayStyle.Flex;
                                Port.style.display = DisplayStyle.Flex;
                                PortCopyButton.style.display = DisplayStyle.Flex;
                            }
                        }

                        break;
                }
            }

            private void AssignStatusLabel(List<NodeStatus> status, ExecutionStage executionStage)
            {
                List<NodeStatus> inScopeNodes = GetCurrentNodes(status, m_Instance, executionStage);
                StatusLabel.text = GetNodesStageDisplayStr(inScopeNodes, executionStage);
                StatusLabel.style.display = DisplayStyle.Flex;
            }

            private string GetNodesStageDisplayStr(List<NodeStatus> nodesStatusList, ExecutionStage executionStage)
            {
                double progress = 1.0;
                if (nodesStatusList.Count > 0)
                {
                    progress = CalculateNodeProgress(
                        nodesStatusList); // extract to function, if length > 1, take average of the node progress
                }

                string stageStr = executionStage switch
                {
                    ExecutionStage.Prepare => $"Preparing {Math.Round(progress * 100, 0)}%",
                    ExecutionStage.Deploy => $"Deploying {Math.Round(progress * 100, 0)}%",
                    ExecutionStage.Run => "Running",
                    _ => "Idle"
                };
                var isAnyRunningOrIdleNodes = false;
                foreach (var nodeStatus in nodesStatusList)
                {
                    if (nodeStatus.State is ExecutionState.Running or ExecutionState.Idle)
                    {
                        isAnyRunningOrIdleNodes = true;
                        break;
                    }
                }
                if (executionStage == ExecutionStage.Run && isAnyRunningOrIdleNodes)
                {
                    stageStr = $"Launching {Math.Round(progress * 100, 0)}%";
                }

                return stageStr;
            }

            private double CalculateNodeProgress(List<NodeStatus> nodeStatusList)
            {
                if (nodeStatusList.Count == 0)
                {
                    return 1.0;
                }

                double totalProgress = 0.0;
                for (int i = 0; i < nodeStatusList.Count; i++)
                {
                    totalProgress += nodeStatusList[i].Progress;
                }
                return totalProgress / nodeStatusList.Count;
            }

            private static bool IsCurrentNode(InstanceDescription instance, NodeStatus nodeStatus, ExecutionStage executionStage)
            {
                switch (instance)
                {
                    case EditorInstanceDescription:
                        return executionStage switch
                        {
                            ExecutionStage.Prepare => false,
                            ExecutionStage.Deploy => nodeStatus.NodeName.Contains("deploy",
                                StringComparison.OrdinalIgnoreCase),
                            ExecutionStage.Run =>
                                nodeStatus.NodeName.Contains("run", StringComparison.OrdinalIgnoreCase),
                            _ => false
                        };
                    case LocalInstanceDescription:
                        return executionStage switch
                        {
                            ExecutionStage.Prepare => nodeStatus.NodeName.Contains("- build",
                                StringComparison.OrdinalIgnoreCase),
                            ExecutionStage.Deploy => false,
                            ExecutionStage.Run =>
                                nodeStatus.NodeName.Contains("- run", StringComparison.OrdinalIgnoreCase),
                            _ => false
                        };
                    case RemoteInstanceDescription:
                        return executionStage switch
                        {
                            ExecutionStage.Prepare => nodeStatus.NodeName.Contains(
                                ScenarioFactory.RemoteNodeConstants.k_BuildNodePostFix,
                                StringComparison.OrdinalIgnoreCase),
                            ExecutionStage.Deploy => nodeStatus.NodeName.Contains(
                                                        ScenarioFactory.RemoteNodeConstants
                                                            .k_DeployBuildNodePostfix,
                                                        StringComparison.OrdinalIgnoreCase)
                                                    || nodeStatus.NodeName.Contains(
                                                        ScenarioFactory.RemoteNodeConstants
                                                            .k_DeployConfigBuildNodePostfix,
                                                        StringComparison.OrdinalIgnoreCase)
                                                    || nodeStatus.NodeName.Contains(
                                                        ScenarioFactory.RemoteNodeConstants
                                                            .k_DeployFleetNodePostfix,
                                                        StringComparison.OrdinalIgnoreCase),
                            ExecutionStage.Run => nodeStatus.NodeName.Contains(
                                                        ScenarioFactory.RemoteNodeConstants.k_RunNodePostfix,
                                                        StringComparison.OrdinalIgnoreCase)
                                                    || nodeStatus.NodeName.Contains(
                                                        ScenarioFactory.RemoteNodeConstants.k_AllocateNodePostfix,
                                                        StringComparison.OrdinalIgnoreCase),
                            _ => false
                        };
                    default:
                        return false;
                }
            }

            private List<NodeStatus> GetCurrentNodes(List<NodeStatus> nodeStatusList, InstanceDescription instance,
                ExecutionStage executionStage)
            {
                var result = new List<NodeStatus>();
                foreach (var nodeStatus in nodeStatusList)
                {
                    if (IsCurrentNode(instance, nodeStatus, executionStage))
                    {
                        result.Add(nodeStatus);
                    }
                }
                return result;
            }

            internal void SetScenarioStatus(ScenarioStatus scenarioStatus)
            {
                // The status of a Free Run Instance in Play Mode Control is updated via
                // FreeRunningStatusElement - no need to update here and return.
                if (m_Instance.RunModeState == RunModeState.ManualControl)
                    return;


                var nodesStatus = GetNodesStatusForInstance(scenarioStatus);
                var currentStage = scenarioStatus.CurrentStage;
                RefreshStatusUI(currentStage, nodesStatus);
            }

            internal void RefreshStatusUI(ExecutionStage currentStage, List<NodeStatus> nodesStatus)
            {
                CleanUpStatus();

                var nodeStates = new List<ExecutionState>();
                foreach (var node in nodesStatus)
                {
                    nodeStates.Add(node.State);
                }
                var instanceExecutionState = Instance.ComputeInstanceState(nodeStates);

                AssignStatusIconClass(instanceExecutionState, currentStage);

                AssignStatusTooltip(instanceExecutionState);
                AssignStatusLabel(nodesStatus, currentStage);
                AssignLogs(instanceExecutionState);
                AssignIpAddress(instanceExecutionState);
            }

            internal string GetCurrentScenarioStageString(ExecutionStage stage)
            {
                var stageString = "";
                switch (stage)
                {
                    case ExecutionStage.Prepare:
                        stageString = "Preparing";
                        break;
                    case ExecutionStage.Deploy:
                        stageString = "Deploying";
                        break;
                    case ExecutionStage.Run:
                        stageString = "Launching";
                        break;
                }
                return $"{stageString}";
            }
        }
    }
}
