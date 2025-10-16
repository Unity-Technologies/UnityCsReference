// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    internal class LocalDeviceRunNode : Node, IInstanceRunNode
    {
        private const int k_LogMonitorIntervalMS = 100;
        private const string k_TempLogsPath = Constants.k_TempRootPath + "ScenariosLogs/";

        // We may need some of these inputs for the log streaming process but for now only BuildReport is necessary
        [SerializeReference] public NodeInput<string> ExecutablePath;
        [SerializeReference] public NodeInput<string> Arguments;
        [SerializeReference] public NodeInput<bool> StreamLogs;
        [SerializeReference] public NodeInput<Color> LogsColor;
        [SerializeReference] public NodeInput<ConnectionData> ConnectionData;
        [SerializeReference] public NodeInput<BuildReport> BuildReport;
        [SerializeReference] public NodeInput<string> DeviceName;

        [SerializeReference] public NodeOutput<ConnectionData> ConnectionDataOut;
        [SerializeReference] public NodeOutput<int> ProcessId;

        NodeInput<ConnectionData> IConnectableNode.ConnectionDataIn => ConnectionData;
        NodeOutput<ConnectionData> IConnectableNode.ConnectionDataOut => ConnectionDataOut;

        [SerializeField] private int m_LogCatProcessId;
        [SerializeField] private int m_ApkProcessId;
        [SerializeField] private string m_LogcatArguments;
        [SerializeField] private int m_LogReaderPosition;
        [SerializeField] private AdbLogcat m_Logcat;

        private Task m_ApkRunningTask;


        public bool IsRunning()
        {
            return m_ApkRunningTask != null && !m_ApkRunningTask.IsCompleted;
        }

        public LocalDeviceRunNode(string name) : base(name)
        {
            ExecutablePath = new(this);
            Arguments = new(this);
            StreamLogs = new(this);
            LogsColor = new(this);
            ConnectionData = new(this);
            DeviceName = new(this);


            ConnectionDataOut = new(this);
            ProcessId = new(this);
            BuildReport = new(this);
        }

        private string GetRandomString(int count)
        {
            var random = new System.Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var randomString = new char[count];
            for (var i = 0; i < count; i++)
            {
                randomString[i] = chars[random.Next(chars.Length)];
            }
            return new string(randomString);
        }

        private static string GetLogPath(string arguments, string workingDirectory)
        {
            if (arguments == null)
                return string.Empty;
            const string k_LogFileRegex = @"-logFile\s+(?:(?:\""(.*)\"")|([^\-][^\s]*)|())";
            var match = Regex.Match(arguments, k_LogFileRegex, RegexOptions.IgnoreCase);
            var logFile = string.Empty;

            if (match.Success)
            {
                for (var i = 1; i < 4; i++)
                {
                    if (match.Groups[i].Success)
                    {
                        logFile = match.Groups[i].Value;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(logFile))
                logFile = Path.GetFullPath(logFile, workingDirectory);

            return logFile;
        }

        private string GetWorkingDirectory()
            => Path.GetDirectoryName(GetInput(ExecutablePath));

        private void ProcessLogcatArguments()
        {
            m_LogcatArguments = string.Empty;

            // Streaming logs requires that the logs are written to a file. If the user has not provided a log file, we will generate one.
            if (GetInput(StreamLogs))
            {
                // We produce a unique log file name to avoid conflicts between multiple instances of the same node
                var logFileName = $"{Name.Replace(" ", "").Replace("(", "").Replace(")", "")}_{GetRandomString(8)}_Android.log";
                var logPath = Path.Combine(Path.GetFullPath(k_TempLogsPath), logFileName);
                m_LogcatArguments += $" -logFile \"{logPath}\"";
            }
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ProcessLogcatArguments();

            if (GetInput(StreamLogs))
            {
                m_Logcat = StartLogCatProcess();
                m_LogCatProcessId = m_Logcat.GetLogcatProcess().Id;
                m_LogReaderPosition = 0;
            }


            var buildpath = BuildReport.GetValue<BuildReport>().summary.outputPath;
            string escapedPath = $"\"{buildpath}\"";

            m_ApkProcessId = await AdbUtilities.StartApk(escapedPath, DeviceName.GetValue<string>());
            ProcessId.SetValue(m_ApkProcessId);
        }

        void StopProcess(CancellationToken cancellationToken)
        {
            var terminationType = cancellationToken.IsCancellationRequested ? "Terminated by Unity" : "terminated externally";
            DebugUtils.Trace($"Requested to cancel : {terminationType}");

            try
            {
                if (IsRunning())
                {
                    AdbUtilities.StopApk(DeviceName.GetValue<String>());
                    if (GetInput(StreamLogs))
                        m_Logcat.Stop();
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to Stop Android Process: {e.Message}");
            }

            if (GetInput(StreamLogs))
            {
                try
                {
                    using var logcatProcess = Process.GetProcessById(m_LogCatProcessId);
                    if (!logcatProcess.HasExited)
                    {
                        AdbUtilities.StopApk(DeviceName.GetValue<String>());
                        m_Logcat.Stop();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to Stop Android Process: {e.Message}");
                }
            }
        }

        private AdbLogcat StartLogCatProcess()
        {
            if (!Directory.Exists(k_TempLogsPath))
                Directory.CreateDirectory(k_TempLogsPath);

            var logcat = new AdbLogcat(AdbBridgeHelper.ADB.GetInstance(),  "threadtime", DeviceName.GetValue<string>(), Debug.Log);
            logcat.Start(GetLogPath(m_LogcatArguments, GetWorkingDirectory()));
            return logcat;
        }

        protected override async Task MonitorAsync(CancellationToken cancellationToken)
        {
            // Monitor the App process and its logs
            RunProcessMonitoring(cancellationToken);
            var streamLogsTask = GetInput(StreamLogs) ? StreamLogsAsync(cancellationToken) : Task.CompletedTask;

            while (IsRunning() && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100);
            }

            StopProcess(cancellationToken);

            await streamLogsTask;

        }

        private void RunProcessMonitoring(CancellationToken cancellationToken)
        {
            m_ApkRunningTask = Task.Run(async () =>
            {
                const int k_CheckInterval = 1_000;
                Thread.CurrentThread.Name = $"Android Process ({m_ApkProcessId}) Monitor";
                while (AdbUtilities.GetAndroidProcessRunning(DeviceName.GetValue<string>(), m_ApkProcessId)
                        && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(k_CheckInterval);
                }
            });
        }

        private async Task StreamLogsAsync(CancellationToken cancellationToken)
        {
            var logPath = GetLogPath(m_LogcatArguments, GetWorkingDirectory());
            var logID = Path.GetFileName(GetInput(ExecutablePath));
            var logColor = GetInput(LogsColor);


            if (string.IsNullOrEmpty(logPath))
            {
                Debug.LogError("Unable to stream logs because the log file was not found.");
                return;
            }

            while (!File.Exists(logPath))
            {
                if (!IsRunning())
                    return;

                await Task.Delay(k_LogMonitorIntervalMS, cancellationToken);
            }

            {
                using var fileStream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var streamReader = new StreamReader(fileStream);
                streamReader.BaseStream.Seek(m_LogReaderPosition, SeekOrigin.Begin);

                DebugUtils.Trace($"Starting streaming logs from {logPath}");

                while (!cancellationToken.IsCancellationRequested)
                {
                    var logMessage = streamReader.ReadLine();
                    m_LogReaderPosition = (int)streamReader.BaseStream.Position;

                    if (logMessage == null)
                    {
                        if (!IsRunning())
                            break;

                        await Task.Delay(k_LogMonitorIntervalMS, cancellationToken);
                        await Task.Yield();
                        continue;
                    }

                    if (logMessage.Length == 0) continue;

                    IInstanceRunNode.PrintReceivedLog(logID, logColor, logMessage);
                }
            }

            try
            {
                File.Delete(logPath);
            }
            catch (Exception e)
            {
                DebugUtils.Trace($"Failed to delete log file {logPath}: {e.Message}");
            }
            DebugUtils.Trace($"Finished streaming logs from {logPath}");
        }
    }
}
