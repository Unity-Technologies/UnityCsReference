// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class RunServerNode : MultiplayNode, IInstanceRunNode
    {
        // The monitoring task will send requests to the server every interval to get the logs
        // and confirm the running state.
        private const int k_MonitorStepIntervalMS = 1000;
        private const int k_MaxLogsPerRequest = 100;

        [SerializeReference] public NodeInput<long> ServerId;
        [SerializeReference] public NodeInput<bool> StreamLogs;
        [SerializeReference] public NodeInput<Color> LogsColor;
        [SerializeReference] public NodeInput<ConnectionData> ConnectionData;

        [SerializeReference] public NodeOutput<ConnectionData> ConnectionDataOut;

        NodeInput<ConnectionData> IConnectableNode.ConnectionDataIn => ConnectionData;
        NodeOutput<ConnectionData> IConnectableNode.ConnectionDataOut => ConnectionDataOut;

        [SerializeField] private long m_LastLogsTime; // This is long instead of DateTime to make sure it survives domain reloads.
        [SerializeField] private int m_MonitorStepIntervalMS;
        [SerializeField] private bool m_IsRunning;

        public bool IsRunning() => m_IsRunning;

        public RunServerNode(string name, int monitorStepInteval = k_MonitorStepIntervalMS) : base(name)
        {
            ServerId = new(this);
            StreamLogs = new(this);
            LogsColor = new(this);
            ConnectionData = new(this);
            ConnectionDataOut = new(this);
            m_MonitorStepIntervalMS = monitorStepInteval;
            m_LastLogsTime = DateTime.Now.ToBinary();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ValidateInputs();
            await IPlayModeServices.Instance.RunFleetServersAsync(GetInput(ServerId), cancellationToken);
            SetOutput(ConnectionDataOut, GetInput(ConnectionData));
        }

        private void ValidateInputs()
        {
            ValidateInputIsSet(ServerId, nameof(ServerId));
            ValidateInputIsSet(LogsColor, nameof(LogsColor));
            ValidateInputIsSet(ConnectionData, nameof(ConnectionData));
        }

        protected override async Task MonitorAsync(CancellationToken cancellationToken)
        {
            // This check is a consistency guard on domain reloads.
            // Depending on how fields are implemented, domain reloads could reset their values.
            Assert.IsTrue(m_LastLogsTime != 0, "Start time must be set before monitoring the server.");

            var serverId = GetInput(ServerId);
            try
            {
                var streamLogs = GetInput(StreamLogs);
                var logConfig = new IPlayModeServices.ServerLogConfiguration()
                {
                    StreamLogs = streamLogs,
                    MaxLogsPerRequest = k_MaxLogsPerRequest,
                    LastLogsTime = m_LastLogsTime,
                    MonitorStepIntervalMS = m_MonitorStepIntervalMS,
                    OnRecievedlLog = OnReceivedServerLog
                };

                await IPlayModeServices.Instance.MonitorRunningServersAsync(serverId,
                                                                            logConfig,
                                                                            OnServerRunningStateChange,
                                                                            cancellationToken);
            }
            catch
            {
                await IPlayModeServices.Instance.StopFleetServersAsync(serverId);
                throw;
            }

            await IPlayModeServices.Instance.StopFleetServersAsync(serverId);
        }

        private void OnReceivedServerLog(string message)
        {
            // Stream the logs to the console
            var ip = GetInput(ConnectionData).IpAddress;
            var port = GetInput(ConnectionData).Port;

            var color = GetInput(LogsColor);
            var identifier = $"Remote({ip}:{port})";
            IInstanceRunNode.PrintReceivedLog(identifier, color, message);
        }

        private void OnServerRunningStateChange(bool isRunning)
        {
            m_IsRunning = isRunning;
        }
    }
}
