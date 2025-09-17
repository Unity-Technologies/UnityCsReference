// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

// [TODO] Defined proper way  to finish the nodes that would match the lifecycle of its thread.
namespace Unity.Multiplayer.PlayMode.Editor
{
    internal class LocalRunNode : Node, IInstanceRunNode
    {
        private const int k_LogMonitorIntervalMS = 100;
        private const string k_TempLogsPath = Constants.k_TempRootPath + "ScenariosLogs/";

        [SerializeReference] public NodeInput<string> ExecutablePath;
        [SerializeReference] public NodeInput<string> Arguments;
        [SerializeReference] public NodeInput<bool> StreamLogs;
        [SerializeReference] public NodeInput<Color> LogsColor;
        [SerializeReference] public NodeInput<ConnectionData> ConnectionData;

        [SerializeReference] public NodeOutput<ConnectionData> ConnectionDataOut;
        [SerializeReference] public NodeOutput<int> ProcessId;

        NodeInput<ConnectionData> IConnectableNode.ConnectionDataIn => ConnectionData;
        NodeOutput<ConnectionData> IConnectableNode.ConnectionDataOut => ConnectionDataOut;

        [SerializeField] private int m_ProcessId;
        [SerializeField] private string m_Arguments;
        [SerializeField] private int m_LogReaderPosition;

        public bool IsRunning()
        {
            try
            {
                using var process = Process.GetProcessById(m_ProcessId);
                return !process.HasExited;
            }
            catch (ArgumentException)
            {
                return false;
            }
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

        public LocalRunNode(string name) : base(name)
        {
            ExecutablePath = new(this);
            Arguments = new(this);
            StreamLogs = new(this);
            LogsColor = new(this);
            ConnectionData = new(this);

            ConnectionDataOut = new(this);
            ProcessId = new(this);
        }

        private void ProcessArguments()
        {
            m_Arguments = GetInput(Arguments) ?? "";
            if (m_Arguments.Contains("{{$$IP$$}}"))
                m_Arguments = m_Arguments.Replace("{{$$IP$$}}", GetInput(ConnectionData).IpAddress);

            // Streaming logs requires that the logs are written to a file. If the user has not provided a log file, we will generate one.
            if (GetInput(StreamLogs))
            {
                // We produce a unique log file name to avoid conflicts between multiple instances of the same node
                var logFileName = $"{Name.Replace(" ", "")}_{GetRandomString(8)}.log";
                var logPath = Path.Combine(Path.GetFullPath(k_TempLogsPath), logFileName);
                m_Arguments += $" -logFile \"{logPath}\"";
            }
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            ProcessArguments();

            m_ProcessId = StartProcess().Id;
            m_LogReaderPosition = 0;

            SetOutput(ConnectionDataOut, GetInput(ConnectionData) == default ? new ConnectionData() : GetInput(ConnectionData));
            SetOutput(ProcessId, m_ProcessId);

            return Task.CompletedTask;
        }

        protected override async Task MonitorAsync(CancellationToken cancellationToken)
        {
            var streamLogsTask = GetInput(StreamLogs) ? StreamLogsAsync(cancellationToken) : Task.CompletedTask;

            try
            {
                while (IsRunning() && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                StopProcess(cancellationToken);
                await streamLogsTask;
            }
        }

        private Process StartProcess()
        {
            var executablePath = GetInput(ExecutablePath);
            DebugUtils.Trace($"Starting {executablePath}");

            if (!File.Exists(executablePath))
                throw new FileNotFoundException($"Build file does not exist, maybe you need to run a build? (missing: {executablePath})");

            if (!Directory.Exists(k_TempLogsPath))
                Directory.CreateDirectory(k_TempLogsPath);

            var process = new Process();
            process.EnableRaisingEvents = true;

            // Monitor the process exit signal
            process.Exited += ProcessExitedHandler;

            process.StartInfo.FileName = executablePath;
            process.StartInfo.Arguments = m_Arguments;
            process.StartInfo.WorkingDirectory = GetWorkingDirectory();
            // The Process object must have the UseShellExecute property set to false in order to use environment variables. If not, throws a InvalidOperationException upon start.
            process.StartInfo.UseShellExecute = false;

            process.Start();
            if (process.HasExited)
            {
                throw new Exception("Process exited immediately, likely caused by an issue with the executable.");
            }

            DebugUtils.Trace($"Process '{executablePath}' launched [Process id:{process.Id} ]");

            return process;
        }

        void StopProcess(CancellationToken cancellationToken)
        {
            var terminationType = cancellationToken.IsCancellationRequested ? "Terminated by Unity" : "terminated externally";
            DebugUtils.Trace($"Requested to cancel : {terminationType}");
            try
            {
                using var process = Process.GetProcessById(m_ProcessId);
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(100); // possibly with a timeout
                }
            }
            catch (ArgumentException)
            { }
        }

        void ProcessExitedHandler(object sender, EventArgs e)
        {
            var process = (Process)sender;
            DebugUtils.Trace($"Process exited with exit code {process.ExitCode}");
        }

        private async Task StreamLogsAsync(CancellationToken cancellationToken)
        {
            var logPath = GetLogPath(m_Arguments, GetWorkingDirectory());
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
                try
                {
                    streamReader.BaseStream.Seek(m_LogReaderPosition, SeekOrigin.Begin);
                }
                catch (Exception e)
                {
                    DebugUtils.Trace($"Failed to seek to the end of the log file {logPath}: {e.Message}");
                }

                DebugUtils.Trace($"Starting streaming logs from {logPath}");

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
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
                    catch (Exception e)
                    {
                        DebugUtils.Trace($"Failed to stream logs from {logPath}: {e.Message}");
                    }
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
