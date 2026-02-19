// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    class StreamLogsFromFileNode : Node
    {
        const int k_LogMonitorIntervalMS = 120;

        [SerializeReference] private NodeInput<string> m_LogLabel;
        [SerializeReference] private NodeInput<string> m_LogPath;
        [SerializeReference] private NodeInput<Color> m_LogColor;
        [SerializeReference] private NodeInput<int> m_ProcessId;

        public NodeInput<string> LogLabel => m_LogLabel;
        public NodeInput<string> LogPath => m_LogPath;
        public NodeInput<Color> LogColor => m_LogColor;
        public NodeInput<int> ProcessId => m_ProcessId;

        public StreamLogsFromFileNode(string name) : base(name)
        {
            m_LogLabel = new(this);
            m_LogPath = new(this);
            m_LogColor = new(this);
            m_ProcessId = new(this);
        }

        public static void PrintReceivedLog(string identifier, Color color, string message, LogType logType = LogType.Log)
        {
            UnityEngine.Debug.LogFormat(logType, LogOption.NoStacktrace, null, "{0}", CalculateLogString(identifier, color, message));
        }

        public static string CalculateLogString(string identifier, Color color, string message)
        {
            var colorHex = $"#{ColorUtility.ToHtmlStringRGB(color)}";
            return $"<color={colorHex}>[{identifier}]</color> {message}";
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var logLabel = GetInput(LogLabel);
            var logPath = GetInput(LogPath);
            var logColor = GetInput(LogColor);
            var processId = GetInput(ProcessId);

            try
            {
                var process = await MonitorProcessNode.FindProcessById(processId, cancellationToken);

                if (string.IsNullOrEmpty(logPath))
                    throw new ArgumentException("Log path cannot be null or empty", nameof(logPath));

                while (!File.Exists(logPath))
                {
                    if (process.HasExited)
                        return;

                    await Task.Delay(k_LogMonitorIntervalMS, cancellationToken);
                }

                {
                    using var fileStream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var streamReader = new StreamReader(fileStream);

                    DebugUtils.Trace($"Starting streaming logs from {logPath}");


                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            var logMessage = streamReader.ReadLine();

                            if (logMessage == null)
                            {
                                if (process.HasExited)
                                    break;

                                await Task.Delay(k_LogMonitorIntervalMS, cancellationToken);
                                continue;
                            }

                            if (logMessage.Length == 0) continue;

                            PrintReceivedLog(logLabel, logColor, logMessage);
                        }
                        catch (Exception e)
                        {
                            DebugUtils.Trace($"Failed to stream logs from {logPath}: {e.Message}");
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // A cancellation at this point means it was requested by the user,
                // which means the node actually completed properly. Setting progress to 1.0 will prevent its state to be set to Aborted.
                SetProgress(1f);
            }
        }
    }
}
