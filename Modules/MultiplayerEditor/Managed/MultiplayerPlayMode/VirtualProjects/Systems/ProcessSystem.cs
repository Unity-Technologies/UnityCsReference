// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace Unity.Multiplayer.PlayMode.Editor
{
    struct ProcessSystemDelegates
    {   // This simply represents the static methods of a ProcessSystem
        public delegate bool IsRunning(int processID);
        public delegate void Kill(int processId);
        public delegate bool TryRun(string filename, string arguments, out int processID, out string error);
        public delegate int OurId();

        public IsRunning IsRunningFunc;
        public Kill KillFunc;
        public TryRun TryRunFunc;
        public OurId OurIdFunc;
    }

    static class ProcessSystem
    {
        public static ProcessSystemDelegates Delegates { get; } = new ProcessSystemDelegates
        {
            IsRunningFunc = IsRunning,
            KillFunc = TerminateProcess,
            TryRunFunc = TryRunProcess,
            OurIdFunc = GetCurrentProcessId,
        };

        public static bool IsRunning(int processId)
        {
            using var process = GetProcessById(processId);
            return !(process == null || process.HasExited);
        }

        public static bool TryRunProcess(string executableFullPath, string executableArguments, out int processID,
            out string error)
        {
            return InnerTryRunProcess(waitForExit: false,
                executableFullPath, executableArguments, out processID, out error);
        }

        public static bool TryRunProcessWaitForExit(string executableFullPath, string executableArguments,
            out int processID, out string error)
        {
            return InnerTryRunProcess(waitForExit: true,
                executableFullPath, executableArguments, out processID, out error);
        }

        static bool InnerTryRunProcess(bool waitForExit, string executableFullPath, string executableArguments,
            out int processID, out string error)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executableFullPath,
                Arguments = executableArguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            using var process = new Process();
            process.StartInfo = startInfo;

            var success = process.Start();
            if (!success)
            {
                var output = process.StandardOutput.ReadLine();
                var error1 = process.StandardError.ReadLine();

                error = output + error1;
                processID = -1;
                return false;
            }

            if (waitForExit)
            {
                process.WaitForExit();

                // Check both StandardError output and exit code
                var errorOutput = process.StandardError.ReadToEnd();
                var exitCode = process.ExitCode;

                if (!string.IsNullOrEmpty(errorOutput) || exitCode != 0)
                {
                    error = string.IsNullOrEmpty(errorOutput)
                        ? $"Process exited with code {exitCode}"
                        : errorOutput;
                    processID = process.Id;
                    return false;
                }
            }

            error = string.Empty;
            processID = process.Id;
            return true;
        }

        private static void TerminateProcess(int processId)
        {
            using var process = GetProcessById(processId);
            if (process == null || process.HasExited) return;
            if (process.CloseMainWindow() && process.WaitForExit(2000))
                return;

            KillProcess(process);
        }

        private static void KillProcess(Process process)
        {
            try
            {
                process.Kill();
            }
            catch (Win32Exception)
            {
                // No-op.
                // Happens if still terminating
            }
            catch (InvalidOperationException)
            {
                // No-op.
                // Happens if already terminated
            }
        }

        public static int GetCurrentProcessId() => Process.GetCurrentProcess().Id;

        internal static Process GetProcessById(int processId)
        {
            try { return Process.GetProcessById(processId); }
            catch (ArgumentException) { return null; }
        }
    }
}
