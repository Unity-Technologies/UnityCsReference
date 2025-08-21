// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// This interface provides an abstraction layer for interacting with Play Mode Game Services backends.
    /// </summary>
    internal interface IPlayModeServices
    {
        private static IPlayModeServices s_Instance;
        internal static IPlayModeServices Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var apiTypes = new List<Type>(TypeCache.GetTypesDerivedFrom<IPlayModeServices>());

                    // Filter out Mocked Test classes used
                    for (int i = apiTypes.Count - 1; i >= 0; i--)
                    {
                        if (apiTypes[i].GetCustomAttribute(typeof(MockedServiceProviderAttribute), false) != null)
                            apiTypes.RemoveAt(i);
                    }

                    // Now enforce if only one PlayModeServices class def exists.
                    if (apiTypes.Count > 1)
                    {
                        Debug.LogError("Multiple implementations of IServicesApi found. Please ensure only one is defined.");
                    }
                    else if (apiTypes.Count > 0)
                    {
                        s_Instance = (IPlayModeServices)System.Activator.CreateInstance(apiTypes[0]);
                    }
                }
                return s_Instance;
            }
        }

        // Used to define Mocked Play Mode Service providers for MPPM tests
        [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
        internal class MockedServiceProviderAttribute : System.Attribute
        {
            public MockedServiceProviderAttribute() { }
        }

        struct CreateAndSyncTestAllocationResult
        {
            public long ServerId;
            public string Ipv4Address;
            public ushort GamePort;
        }

        Task<CreateAndSyncTestAllocationResult> CreateAndSyncTestAllocationAsync(
            string fleetName,
            string buildConfigurationName,
            CancellationToken cancellationToken);

        struct DeployBuildConfigsResult
        {
            public long BuildConfigurationId;
        }

        Task<DeployBuildConfigsResult> DeployBuildConfigurationAsync(
            long buildId,
            string buildConfigurationName,
            string buildName,
            string binaryPath,
            string commandLineArguments,
            int coresCount,
            int memoryMiB,
            int speedMhz,
            Action<float> onProgress,
            CancellationToken cancellationToken);

        struct UploadAndSyncBuildsResult
        {
            public long BuildId;
        }

        Task<UploadAndSyncBuildsResult> UploadAndSyncBuildsAsync(
            string buildName,
            string buildPath,
            string executablePath,
            Action<float> onProgress,
            CancellationToken cancellationToken);

        struct GetVersionOfBuildResult
        {
            public long Version;
        }

        Task<GetVersionOfBuildResult> GetVersionOfBuildAsync(
            long buildId,
            CancellationToken cancellationToken);

        Task DeployFleetsAsync(
            string fleetName,
            string region,
            string architecture,
            string buildConfigurationName,
            long buildConfigurationId,
            Action<float> onProgress,
            CancellationToken cancellationToken);

        Task RunFleetServersAsync(
            long serverId,
            CancellationToken cancellationToken);

        Task MonitorRunningServersAsync(
            long serverId,
            ServerLogConfiguration logConfig,
            Action<bool> onServerRunStateChanged,
            CancellationToken cancellationToken);

        struct ServerLogConfiguration
        {
            public bool StreamLogs;
            public int MaxLogsPerRequest;
            public long LastLogsTime;
            public int MonitorStepIntervalMS;
            public Action<string> OnRecievedlLog;
        }

        Task StopFleetServersAsync(
            long serverId
        );

        Task<InitaliseSimFleetResult> InitialiseSimFleetAsync(
            string projectId,
            string environmentId,
            string authToken,
            bool autoAllocate,
            string queryType,
            string localHost,
            string localPort,
            CancellationToken cancellationToken);

        struct InitaliseSimFleetResult
        {
            public string FleetID;
            public string FleetName;
            public string ServerLocation;
            public string ServerRemoteHost;
            public int ServerRemotePort;
            public string ServerState;
            public string AllocationId;
            public long? ServerId;
        }

        Task InitialiseSimServerJsonAsync(string fleetId,
            string serverId,
            string allocationId,
            string regionId,
            string ip,
            string localPort,
            string queryPort,
            string queryType,
            CancellationToken cancellationToken);

        Task<SetupSimEnvironmentResult> SetupSimEnvironmentAsync(CancellationToken cancellationToken);

        struct SetupSimEnvironmentResult
        {
            public string AuthToken;
            public string EnvironmentId;
            public string ProjectId;
        }

        Task MonitorSimFleetAsync(
            bool waitForEditorPlaying,
            string projectId,
            string environmentId,
            string authToken,
            string allocationId,
            string serverHost,
            int serverPort,
            CancellationToken cancellationToken);

        Task<AllocateServerInFleetResult> AllocateServerInSimFleetAsync(
            string projectId,
            string environmentId,
            string fleetId,
            string authToken,
            CancellationToken cancellationToken);

        struct AllocateServerInFleetResult
        {
            public string AllocationId;
        }

        Task DeallocateServerInSimFleetAsync(
            string projectId,
            string environmentId,
            string allocationId,
            string authToken,
            CancellationToken cancellationToken);

        Task<QueryServerMetricsResult> QueryServerMetricsAsync(
            SimulatorSettings.ProtocolType queryProtocol,
            CancellationToken cancellationToken);

        struct QueryServerMetricsResult
        {
            public int CurrentPlayers;
            public int MaxPlayers;
            public string ServerName;
            public string GameType;
            public string BuildId;
            public string Map;
            public ushort Port;
        }

        struct SimFleetServerDefaultConfig
        {
            public string Ip;
            public string Port;
        }

        void ClearSimFleetAllocationLogs();
    }
}
