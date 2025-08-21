// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System;
using UnityEditor;
using UnityEditor.Build;
using Debug = UnityEngine.Debug;

namespace Unity.Multiplayer.PlayMode.Editor
{
    internal abstract class AdbLogcatBase
    {
        protected AdbBridgeHelper.ADB m_ADB;
        protected string m_LogPrintFormat;
        protected string m_DeviceID;
        protected Action<string> m_LogCallbackAction;

        internal AdbLogcatBase(AdbBridgeHelper.ADB adb, string logPrintFormat, string device, Action<string> logCallbackAction)
        {
            m_ADB = adb;
            m_LogPrintFormat = logPrintFormat;
            m_DeviceID = device;
            m_LogCallbackAction = logCallbackAction;
        }

        public abstract void Start(string logPath);
        public abstract void Stop();
        public abstract void Kill();
        public abstract bool HasExited { get; }
    }

    internal class AdbLogcat : AdbLogcatBase
    {
        public Process m_LogcatProcess;

        internal AdbLogcat(AdbBridgeHelper.ADB adb, string logPrintFormat, string device, Action<string> logCallbackAction)
            : base(adb, logPrintFormat, device, logCallbackAction)
        {
        }

        private string LogcatArguments(string logPath)
        {
            var filterArg = "Unity";
            return string.Format("-s {0} logcat | grep {1} | grep -i {2} > \"{3}\"",
                m_DeviceID, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) , filterArg, logPath);
        }

        public Process GetLogcatProcess()
        {
            return m_LogcatProcess;
        }

        public override void Start(string logPath)
        {
            var arguments = LogcatArguments(logPath);
            var executablePath = m_ADB.GetADBPath();
            var streamLogsArgument = string.Empty;
            DebugUtils.Trace($"Starting logcat: {m_ADB.GetADBPath()} {arguments}");
            m_LogcatProcess = new Process();
            {
                streamLogsArgument = " > \\\"" + logPath + "\\\"";
                m_LogcatProcess.StartInfo.FileName = "/bin/bash";
                m_LogcatProcess.StartInfo.Arguments = $"-c \"exec {executablePath} {arguments}{streamLogsArgument}\"";
            }
            m_LogcatProcess.StartInfo.RedirectStandardError = true;
            m_LogcatProcess.StartInfo.RedirectStandardOutput = true;
            m_LogcatProcess.StartInfo.UseShellExecute = false;
            m_LogcatProcess.StartInfo.CreateNoWindow = true;
            m_LogcatProcess.OutputDataReceived += OutputDataReceived;
            m_LogcatProcess.ErrorDataReceived += OutputDataReceived;
            m_LogcatProcess.Start();

            m_LogcatProcess.BeginOutputReadLine();
            m_LogcatProcess.BeginErrorReadLine();
        }

        public override void Stop()
        {
            if (m_LogcatProcess != null && !m_LogcatProcess.HasExited)
                 Kill();

            m_LogcatProcess = null;
        }

        public override void Kill()
        {
            // NOTE: DONT CALL CLOSE, or ADB process will stay alive all the time
            DebugUtils.Trace("Stopping logcat  " + m_LogcatProcess.Id);
            m_LogcatProcess.Kill();
            m_LogcatProcess.WaitForExit(100); // possibly with a timeout
        }

        public override bool HasExited
        {
            get
            {
                return m_LogcatProcess.HasExited;
            }
        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            m_LogCallbackAction(e.Data);
        }
    }
}
