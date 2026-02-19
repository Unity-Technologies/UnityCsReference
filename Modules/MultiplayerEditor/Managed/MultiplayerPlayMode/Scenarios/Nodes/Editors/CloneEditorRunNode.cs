// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.PlayMode.Editor;
using System;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class CloneEditorRunNode : Node
    {
        [SerializeReference] public NodeInput<int> PlayerInstanceIndex;
        [SerializeReference] public NodeInput<bool> StreamLogs;
        [SerializeReference] public NodeInput<Color> LogsColor;

        [SerializeReference] public NodeOutput<int> ProcessId;

        [SerializeField] private int m_ProcessId;

        public UnityPlayer GetPlayer() => MultiplayerPlaymode.Players[GetInput(PlayerInstanceIndex)];
        public bool IsRunning()
        {
            var player = GetPlayer();
            return player.PlayerState == PlayerState.Launched;
        }

        public CloneEditorRunNode(string name) : base(name)
        {
            PlayerInstanceIndex = new(this);
            StreamLogs = new(this);
            LogsColor = new(this);

            ProcessId = new(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var player = GetPlayer();
            var streamLogs = GetInput(StreamLogs);

            Debug.Assert(player.PlayerState == PlayerState.Launched);

            m_ProcessId = MultiplayerPlaymodeEditorUtility.GetProcessID(player);
            SetOutput(ProcessId, m_ProcessId);

            SetupListeningLogs(streamLogs);

            SendSyncStateMessage(player, new CloneState()
            {
                StreamLogsToMainEditor = streamLogs
            });

            // Entering play mode could take a few frames, so we wait until the state changes (clone receive a message to go into playmode from MPPM)
            while (EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode) { await Task.Delay(100, cancellationToken); }

            if (cancellationToken.IsCancellationRequested)
            {
                await StopPlayer();
                return;
            }

            await MonitorAsync(cancellationToken);
        }

        protected override async Task ExecuteResumeAsync(CancellationToken cancellationToken)
        {
            while (EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode) { await Task.Delay(100, cancellationToken); }

            Assert.IsTrue(EditorApplication.isPlaying, $"Editor should be already in play mode when resuming the editor play node. isPlaying: {EditorApplication.isPlaying}, isPlayingOrWillChangePlaymode: {EditorApplication.isPlayingOrWillChangePlaymode}");

            if (cancellationToken.IsCancellationRequested)
            {
                await StopPlayer();
                return;
            }

            await MonitorAsync(cancellationToken);
        }

        async Task MonitorAsync(CancellationToken cancellationToken)
        {
            SetupListeningLogs(GetInput(StreamLogs));

            try
            {
                while (!cancellationToken.IsCancellationRequested && IsRunning()) { await Task.Delay(100, cancellationToken); }
            }
            catch (OperationCanceledException)
            {
                // A cancellation at this point means it was requested by the user,
                // which means the node actually completed properly. Setting progress to 1.0 will prevent its state to be set to Aborted.
                SetProgress(1f);
            }
            finally
            {
                await StopPlayer();
            }
        }

        private async Task StopPlayer()
        {
            var playerInstanceIndex = GetInput(PlayerInstanceIndex);
            var player = MultiplayerPlaymode.Players[playerInstanceIndex];

            SendSyncStateMessage(player, new CloneState()
            {
                StreamLogsToMainEditor = false
            });
            SetupListeningLogs(false);

            // Grab the current Running Mode from current configurations, if any.
            // TODO - Remove in favor of new Playmode API, where we can rebuild graphs
            // directly from instance configurations (with new API) instead of a shallow reset.
            var isFreeRunningPlayer = false;
            if (PlayModeScenarioManager.ActiveScenario is OrchestratedScenario config)
            {
                var instance = config.Scenario.GetInstanceByName(player.Name);
                isFreeRunningPlayer = instance != null && instance.IsFreeRunMode();
            }

            // Wait until Scenario Mode players (including main player) are out of playmode
            while (EditorApplication.isPlaying && !isFreeRunningPlayer) { await Task.Delay(100); }

            var hasDeactivated = player.Deactivate(out _);
            DebugUtils.Trace(hasDeactivated
                ? $"Successfully deactivated '{player.Name}'"
                : $"Failed to deactivate '{player.Name}'");
        }

        private void OnLogMessageReceived(VirtualProjectIdentifier identifier, string message, string stackTrace, LogType type)
        {
            var player = GetPlayer();
            if (player.TypeDependentPlayerInfo.VirtualProjectIdentifier != identifier)
                return;

            StreamLogsFromFileNode.PrintReceivedLog(player.Name, GetInput(LogsColor), message, type);
        }

        private void SetupListeningLogs(bool listen)
        {
            VirtualProjectWorkflow.WorkflowMainEditorContext.MainPlayerSystems.ApplicationEvents.LogMessageReceived -= OnLogMessageReceived;
            if (listen)
                VirtualProjectWorkflow.WorkflowMainEditorContext.MainPlayerSystems.ApplicationEvents.LogMessageReceived += OnLogMessageReceived;
        }

        private static void SendSyncStateMessage(UnityPlayer player, CloneState state)
        {
            EditorContexts.MainEditorContext.MessagingService.Send(new SyncStateMessage(state), player.TypeDependentPlayerInfo.VirtualProjectIdentifier);
        }
    }
}
