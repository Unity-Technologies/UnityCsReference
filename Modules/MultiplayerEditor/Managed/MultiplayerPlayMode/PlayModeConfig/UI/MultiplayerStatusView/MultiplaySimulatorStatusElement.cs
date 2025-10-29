// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.PlayMode.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class MultiplaySimulatorStatusElement
    {
        internal const string k_AllocateButtonName = "multiplayer-mode-multiplay-allocate-button";
        private const string k_IPLabelText = "IP and Port";
        private const string k_IPPlaceHolderText = "999.999.999.999";
        private const string k_PortPlaceHolderText = "999";
        private const string k_AllocateButtonStartText = "Allocate";
        private const string k_AllocateButtonStopText = "Deallocate";
        private const string k_AllocateToggleText = "Auto Allocate";
        private const string k_FleetNameLabelText = "Fleet Name";
        private const string k_FleetIDLabelText = "Fleet ID";
        private const string k_ServerStateLabelText = "Server State";
        private const string k_AllocationIDLabelText = "Allocation ID";
        private const string k_PlayersLabelText = "Players";
        private const string k_MetaDataFoldoutText = "Simulation Metadata";
        private const string k_SimulationFoldoutText = "Cloud Simulation";
        private const string k_MetaDataPlaceholderText = "Available when the instance is running";
        private const string k_MetricsPlaceholderText = "Available when metrics are fetched";
        private const string k_AllocationIDPlaceholderText = "Available when allocated";
        private const string k_FoldoutClassName = "simulated-foldout";
        private const string k_LocalHostDefaultText = "127.0.0.1";
        private const string k_LocalPortDefaultText = "9000";

        private readonly InstanceDescription m_InstanceDescription;
        private PlaymodeStatusElement.InstanceView m_ParentInstanceView;
        private Foldout m_MetaDataFoldout;
        private Foldout m_SimulationFoldout;
        private Button m_AllocateButton;
        private Toggle m_AllocateToggle;
        private TextField m_FleetIDField;
        private TextField m_FleetNameField;
        private TextField m_ServerStateField;
        private TextField m_AllocationIDField;
        private TextField m_IPField;
        private TextField m_PlayersField;

        private CancellationTokenSource m_MetricsPollingCancellationTokenSource;

        public MultiplaySimulatorStatusElement(InstanceDescription instanceDescription)
        {
            m_InstanceDescription = instanceDescription;

            var instance = GetInstanceForThisElement();
            if (instance != null)
                instance.AddInstanceExecutionEventListener(OnInstanceExecutionUpdate);

        }

        ~MultiplaySimulatorStatusElement()
        {
            // Ensure polling is stopped when the object is disposed
            StopMetricsPolling();
        }

        private Instance GetInstanceForThisElement()
        {
            var currentConfig = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;
            if (currentConfig == null || currentConfig.Scenario == null)
                return null;

            return currentConfig.Scenario.GetInstanceByName(m_InstanceDescription.Name);
        }

        private void OnInstanceExecutionUpdate(Instance instance, Node node)
        {
            RefreshStatusUI();
        }

        void UpdateContainerMargin(VisualElement statusContainer)
        {
            var simulationMargin = m_SimulationFoldout.value ? 30 : 0;
            var metaDataMargin = m_MetaDataFoldout.value && m_SimulationFoldout.value ? 110 : 0;

            statusContainer.style.marginBottom = simulationMargin + metaDataMargin;
        }

        private void RefreshStatusUI()
        {
            var instance = GetInstanceForThisElement();
            var projectId = "";
            var environmentId = "";
            var fleetIDValue = "";
            var authToken = "";

            var nodes = m_InstanceDescription.GetCorrespondingNodes();
            InitialiseFleetNode initialiseFleetNode = null;
            foreach (var nodeName in nodes)
            {
                initialiseFleetNode = GetInitialiseFleetNode(nodeName);
                if (initialiseFleetNode != null)
                    break;
            }
            if (instance != null && instance.IsActive())
            {
                if (initialiseFleetNode is InitialiseFleetNode fleetNode)
                {
                    SetFieldValues(fleetNode);

                    projectId = fleetNode.ProjectId.GetValue<string>();
                    environmentId = fleetNode.EnvironmentId.GetValue<string>();
                    fleetIDValue = fleetNode.FleetID.GetValue<string>();
                    authToken = fleetNode.AuthToken.GetValue<string>();
                }

                SetAllocationButtonEnabled(projectId, environmentId, authToken, fleetIDValue, instance, fleetNode: initialiseFleetNode);
                return;
            }

            StopMetricsPolling();
            SetAllocationButtonEnabled(projectId, environmentId, authToken, fleetIDValue, instance, fleetNode: initialiseFleetNode);
            m_AllocateToggle.SetEnabled(!EditorApplication.isPlaying && !instance.IsActive());
        }

        internal void BindSimulatedFoldOutElement(VisualElement contentContainer, VisualElement statusContainer,
            VisualElement instanceContainer,
            PlaymodeStatusElement.InstanceView parentInstanceView)
        {
            m_ParentInstanceView = parentInstanceView;

            m_SimulationFoldout = new Foldout
            {
                text = k_SimulationFoldoutText,
                name = "simulated-foldout",
                value = false
            };

            var ipContainer = new VisualElement();
            ipContainer.style.marginTop = 5;

            var ipLabel = new Label { text = k_IPLabelText };
            ipContainer.Add(ipLabel);
            contentContainer.Add(ipContainer);

            m_SimulationFoldout.RegisterValueChangedCallback(evt =>
            {
                UpdateContainerMargin(statusContainer);
                RefreshStatusUI();
            });

            m_MetaDataFoldout = new Foldout { text = k_MetaDataFoldoutText, name = "metadata-foldout", value = false, };

            m_MetaDataFoldout.RegisterValueChangedCallback(evt =>
            {
                UpdateContainerMargin(statusContainer);
                RefreshStatusUI();
            });

            m_AllocateToggle = new Toggle(k_AllocateToggleText);
            m_AllocateToggle.SetEnabled(!EditorApplication.isPlaying && !GetInstanceForThisElement().IsActive());
            m_AllocateToggle.tooltip = "When enabled, resources will be allocated automatically when the instance is running. This toggle is only available when the instance is inactive";
            m_FleetIDField = new TextField(k_FleetIDLabelText) { isReadOnly = true };
            m_FleetNameField = new TextField(k_FleetNameLabelText) { isReadOnly = true };
            m_ServerStateField = new TextField(k_ServerStateLabelText) { isReadOnly = true };
            m_AllocationIDField = new TextField(k_AllocationIDLabelText) { isReadOnly = true };
            m_IPField = new TextField() { value = k_IPPlaceHolderText, isReadOnly = true };

            // Current players field
            m_PlayersField = new TextField(k_PlayersLabelText) { isReadOnly = true, value = k_MetricsPlaceholderText };

            var nodes = m_InstanceDescription.GetCorrespondingNodes();
            InitialiseFleetNode initialiseFleetNode = null;
            SetupWebSocketNode webSocketNode = null;
            foreach (var nodeName in nodes)
            {
                if (initialiseFleetNode == null)
                    initialiseFleetNode = GetInitialiseFleetNode(nodeName);
                if (webSocketNode == null)
                    webSocketNode = GetSetupWebSocketNode(nodeName);
                if (initialiseFleetNode != null && webSocketNode != null)
                    break;
            }

            CreateAllocateButton(initialiseFleetNode, webSocketNode);

            if (initialiseFleetNode is InitialiseFleetNode fleetNode)
            {
                SetFieldValues(fleetNode);
            }

            m_AllocateToggle.RegisterValueChangedCallback(evt =>
            {
                if (initialiseFleetNode is not InitialiseFleetNode fleetNode) return;
                var instance = GetInstanceForThisElement();
                var instanceDescription = instance.GetInstanceDescription();
                if (instanceDescription is LocalInstanceDescription localInstanceDescription)
                {
                    var settings = localInstanceDescription.ServerSettings.SimulatorSettings;
                    settings.AutoAllocate = evt.newValue;
                    var serverSettings = localInstanceDescription.ServerSettings;
                    serverSettings.SimulatorSettings = settings;
                    localInstanceDescription.ServerSettings = serverSettings;
                }
                fleetNode.AutoAllocate.SetValue(evt.newValue);

                if (fleetNode.AutoAllocate.GetValue<bool>())
                {
                    fleetNode.AllocationId.SetValue(String.Empty);
                    m_AllocationIDField.SetValueWithoutNotify(k_AllocationIDPlaceholderText);
                }

                SetAllocationButtonEnabled(fleetNode.ProjectId.GetValue<string>(),
                    fleetNode.EnvironmentId.GetValue<string>(),
                    fleetNode.AuthToken.GetValue<string>(),
                    fleetNode.FleetID.GetValue<string>(), instance, fleetNode);
            });

            statusContainer.Add(m_IPField);
            instanceContainer.Add(m_SimulationFoldout);
            statusContainer.Add(m_AllocateButton);
            m_SimulationFoldout.Add(m_AllocateToggle);
            m_MetaDataFoldout.Add(m_FleetIDField);
            m_MetaDataFoldout.Add(m_FleetNameField);
            m_MetaDataFoldout.Add(m_ServerStateField);
            m_MetaDataFoldout.Add(m_AllocationIDField);
            m_MetaDataFoldout.Add(m_PlayersField);
            m_SimulationFoldout.Add(m_MetaDataFoldout);

            RefreshStatusUI();
        }

        private void SetFieldValues(InitialiseFleetNode fleetNode)
        {
            m_AllocateButton.text = String.IsNullOrEmpty(fleetNode.AllocationId.GetValue<string>())
                ? k_AllocateButtonStartText
                : k_AllocateButtonStopText;
            m_FleetIDField.value = String.IsNullOrEmpty(fleetNode.FleetID.GetValue<string>())
                ? k_MetaDataPlaceholderText
                : fleetNode.FleetID.GetValue<string>();
            m_FleetNameField.value = String.IsNullOrEmpty(fleetNode.FleetName.GetValue<string>())
                ? k_MetaDataPlaceholderText
                : fleetNode.FleetName.GetValue<string>();
            m_ServerStateField.value = String.IsNullOrEmpty(fleetNode.ServerState.GetValue<string>())
                ? k_MetaDataPlaceholderText
                : fleetNode.ServerState.GetValue<string>();
            m_AllocationIDField.value = String.IsNullOrEmpty(fleetNode.AllocationId.GetValue<string>())
                ? k_AllocationIDPlaceholderText
                : fleetNode.AllocationId.GetValue<string>();
            var host = String.IsNullOrEmpty(fleetNode.LocalHost.GetValue<string>())
                ? k_LocalHostDefaultText
                : fleetNode.LocalHost.GetValue<string>();
            var port = fleetNode.LocalPort.GetValue<int>() > 0
                ? fleetNode.LocalPort.GetValue<int>().ToString()
                : k_LocalPortDefaultText;
            m_IPField.value = $"{host}:{port}";
            m_AllocateToggle.value = fleetNode.AutoAllocate.GetValue<bool>();
            m_PlayersField.value = k_MetricsPlaceholderText;
        }

        private async Task StartMetricsPolling(InitialiseFleetNode fleetNode)
        {
            // Stop any existing polling
            StopMetricsPolling();

            m_MetricsPollingCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = m_MetricsPollingCancellationTokenSource.Token;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (PlayModeManager.instance.CurrentState != PlayModeState.Running)
                    {
                        MppmLog.Debug("Metrics polling started but play mode is not active. Exiting polling.");
                        StopMetricsPolling();
                        return;
                    }
                    try
                    {
                        var queryProtocol = fleetNode.QueryType.GetValue<SimulatorSettings.ProtocolType>();
                        var result = await IPlayModeServices.Instance.QueryServerMetricsAsync(
                            queryProtocol,
                            cancellationToken);

                        // Update only the current players field
                        var playerText = $"{result.CurrentPlayers.ToString()}/{result.MaxPlayers.ToString()}";
                        EditorApplication.delayCall += () =>
                        {
                            if (m_PlayersField != null &&
                                !cancellationToken.IsCancellationRequested)
                            {
                                m_PlayersField.SetValueWithoutNotify(playerText);
                            }
                        };
                        MppmLog.Debug($"Current Players: {result.CurrentPlayers}, Max Players: {result.MaxPlayers}");
                    }
                    catch (Exception e)
                    {
                        // Log error but continue polling
                        Debug.LogError($"Failed to fetch server metrics: {e.Message}");
                    }

                    // Wait 5 seconds before next poll
                    await Task.Delay(5000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        private void StopMetricsPolling()
        {
            if (m_MetricsPollingCancellationTokenSource != null)
            {
                MppmLog.Debug("Stopping metrics polling...");
            }
            m_MetricsPollingCancellationTokenSource?.Cancel();
            m_MetricsPollingCancellationTokenSource?.Dispose();
            m_MetricsPollingCancellationTokenSource = null;

            // Reset the field to placeholder text
            EditorApplication.delayCall += () =>
            {
                m_PlayersField?.SetValueWithoutNotify(k_MetricsPlaceholderText);
            };

        }

        private void CreateAllocateButton(InitialiseFleetNode fleetNode, SetupWebSocketNode webSocketNode)
        {
            var projectId = "";
            var environmentId = "";
            var fleetIDValue = "";
            var allocationIdValue = "";
            var authToken = "";

            // Sanity check
            if (fleetNode == null || webSocketNode == null)
                return;

            projectId = fleetNode.ProjectId.GetValue<string>();
            environmentId = fleetNode.EnvironmentId.GetValue<string>();
            fleetIDValue = fleetNode.FleetID.GetValue<string>();
            allocationIdValue = fleetNode.AllocationId.GetValue<string>();
            authToken = fleetNode.AuthToken.GetValue<string>();

            m_AllocateButton = new Button(async () =>
            {
                // Sanity check
                var instance = GetInstanceForThisElement();
                if (instance == null)
                    return;

                if (string.IsNullOrEmpty(allocationIdValue))
                {
                    projectId = fleetNode.ProjectId.GetValue<string>();
                    environmentId = fleetNode.EnvironmentId.GetValue<string>();
                    fleetIDValue = fleetNode.FleetID.GetValue<string>();
                    allocationIdValue = fleetNode.AllocationId.GetValue<string>();
                    authToken = fleetNode.AuthToken.GetValue<string>();

                    if (!string.IsNullOrEmpty(allocationIdValue))
                    {
                        Debug.LogError("No existing allocations may exist, Cannot Allocate");

                        // Auto-correct by updating the allocation field to reflect the instance's nodes
                        m_AllocationIDField.SetValueWithoutNotify(allocationIdValue);
                        return;
                    }

                    try
                    {
                        // Finally Allocate the game server
                        var result = await IPlayModeServices.Instance.AllocateServerInSimFleetAsync(
                            projectId,
                            environmentId,
                            fleetIDValue,
                            authToken,
                            CancellationToken.None);

                        // Update the fields with the final allocated result
                        allocationIdValue = result.AllocationId;
                        fleetNode.AllocationId.SetValue(allocationIdValue);
                        m_AllocationIDField.SetValueWithoutNotify(allocationIdValue);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        return;
                    }

                    webSocketNode.AllocationId.SetValue(allocationIdValue);

                    RefreshStatusUI();
                    m_AllocateButton.text = k_AllocateButtonStopText;

                    // Start metrics polling after successful allocation
                    _ = Task.Run(() => StartMetricsPolling(fleetNode));
                }
                else if (!string.IsNullOrEmpty(allocationIdValue))
                {
                    if (string.IsNullOrEmpty(allocationIdValue))
                    {
                        Debug.LogError("Allocation ID is empty. Cannot deallocate resources.");
                        return;
                    }

                    try
                    {
                        await IPlayModeServices.Instance.DeallocateServerInSimFleetAsync(
                            projectId,
                            environmentId,
                            allocationIdValue,
                            authToken,
                            CancellationToken.None);

                        allocationIdValue = "";
                        m_AllocationIDField.SetValueWithoutNotify(allocationIdValue);
                        fleetNode.AllocationId.SetValue(allocationIdValue);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        return;
                    }

                    RefreshStatusUI();
                    m_AllocateButton.text = k_AllocateButtonStartText;

                    // Stop metrics polling after successful deallocation
                    StopMetricsPolling();
                }
            });

            SetAllocationButtonEnabled(projectId, environmentId, authToken, fleetIDValue, GetInstanceForThisElement(), fleetNode: fleetNode);
            m_AllocateButton.name = k_AllocateButtonName;
            m_AllocateButton.tooltip = "Click to allocate or deallocate resources for the cloud simulation. You will not be charged for allocating and deallocating resources using this button. This button is only available when the instance is running";
        }

        private InitialiseFleetNode GetInitialiseFleetNode(string nodeName)
        {
            if (ScenarioRunner.instance.ActiveScenario == null)
                return null;

            var runNodes = ScenarioRunner.instance.ActiveScenario.GetNodes(ExecutionStage.Deploy);
            foreach (var node in runNodes)
            {
                if (node.Name == nodeName && node is InitialiseFleetNode)
                    return node as InitialiseFleetNode;
            }

            return null;
        }

        private SetupWebSocketNode GetSetupWebSocketNode(string nodeName)
        {
            if (ScenarioRunner.instance.ActiveScenario == null)
                return null;

            var runNodes = ScenarioRunner.instance.ActiveScenario.GetNodes(ExecutionStage.Run);
            foreach (var node in runNodes)
            {
                if (node.Name == nodeName && node is SetupWebSocketNode)
                    return node as SetupWebSocketNode;
            }

            return null;
        }

        private void SetAllocationButtonEnabled(string projectId, string environmentId, string authToken, string fleetIDValue, Instance instance, InitialiseFleetNode fleetNode)
        {
            if (fleetNode.AutoAllocate.GetValue<bool>() && !EditorApplication.isPlaying && !instance.IsActive())
            {
                m_AllocateButton.text = k_AllocateToggleText;
                m_AllocateButton.tooltip =
                    "Cloud simulation is set to auto allocate. Once the server is running, resources will be allocated automatically. This can be changed in this section";
                m_AllocateButton.SetEnabled(false);
            }
            else
            {
                m_AllocateButton.SetEnabled(!String.IsNullOrEmpty(projectId) && !String.IsNullOrEmpty(environmentId) &&
                                            !String.IsNullOrEmpty(authToken) && !String.IsNullOrEmpty(fleetIDValue) &&
                                            instance.IsActive() && instance.GetCurrentStage() == ExecutionStage.Run);
                m_AllocateButton.text = String.IsNullOrEmpty(fleetNode.AllocationId.GetValue<string>())
                    ? k_AllocateButtonStartText
                    : k_AllocateButtonStopText;
                m_AllocateButton.tooltip = "Click to allocate or deallocate resources for the cloud simulation. You will not be charged for allocating and deallocating resources using this button. This button is only available when the instance is running";
            }
        }
    }
}
