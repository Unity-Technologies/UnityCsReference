// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;

namespace UnityEditor.Utils
{
    internal class Program : IDisposable
    {
        private ProcessOutputStreamReader _stdout;
        private ProcessOutputStreamReader _stderr;
        private Stream _stdin;
        public Process _process;

        protected Program()
        {
            _process = new Process();
        }

        public Program(ProcessStartInfo si)
            : this()
        {
            _process.StartInfo = si;
        }

        public void Start()
        {
            Start(null);
        }

        public void Start(EventHandler exitCallback)
        {
            if (exitCallback != null)
            {
                _process.EnableRaisingEvents = true;
                _process.Exited += exitCallback;
            }

            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.UseShellExecute = false;

            _process.Start();
            _stdout = new ProcessOutputStreamReader(_process, _process.StandardOutput);
            _stderr = new ProcessOutputStreamReader(_process, _process.StandardError);
            _stdin  = _process.StandardInput.BaseStream;
        }

        public ProcessStartInfo GetProcessStartInfo()
        {
            return _process.StartInfo;
        }

        public void LogProcessStartInfo()
        {
            foreach (string line in RetrieveProcessStartInfo())
            {
                Console.WriteLine(line);
            }
        }

        public List<string> RetrieveProcessStartInfo()
        {
            return _process != null
                ? RetrieveProcessStartInfo(_process.StartInfo)
                : new List<string> {"Failed to retrieve process startInfo"};
        }

        //please dont kill this code.
        private static List<string> RetrieveProcessStartInfo(ProcessStartInfo si)
        {
            List<string> processStartInfo = new List<string> {"Filename: " + si.FileName, "Arguments: " + si.Arguments};

            foreach (DictionaryEntry envVar in si.EnvironmentVariables)
                if (envVar.Key.ToString().StartsWith("MONO"))
                {
                    processStartInfo.Add($"{envVar.Key}: {envVar.Value}");
                }

            int responsefileindex = si.Arguments.IndexOf("Temp/UnityTempFile");
            if (responsefileindex > 0)
            {
                var responsefile = si.Arguments.Substring(responsefileindex);
                processStartInfo.Add($"Responsefile: {responsefile} Contents: ");
                processStartInfo.Add(File.ReadAllText(responsefile));
            }

            return processStartInfo;
        }

        public string GetAllOutput()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("stdout:");
            foreach (var s in GetStandardOutput())
                sb.AppendLine(s);
            sb.AppendLine("stderr:");
            foreach (var s in GetErrorOutput())
                sb.AppendLine(s);
            return sb.ToString();
        }

        public bool HasExited
        {
            get
            {
                if (_process == null)
                    throw new InvalidOperationException("You cannot call HasExited before calling Start");
                try
                {
                    return _process.HasExited;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
            }
        }

        public int ExitCode
        {
            get { return _process.ExitCode; }
        }

        public int Id
        {
            get { return _process.Id; }
        }

        public void Dispose()
        {
            Kill();
            _process.Dispose();
        }

        public void Kill()
        {
            if (!HasExited)
            {
                _process.Kill();
                _process.WaitForExit();
            }
        }

        public Stream GetStandardInput()
        {
            return _stdin;
        }

        public string[] GetStandardOutput()
        {
            return _stdout.GetOutput();
        }

        public string GetStandardOutputAsString()
        {
            var output = GetStandardOutput();
            return GetOutputAsString(output);
        }

        public string[] GetErrorOutput()
        {
            return _stderr.GetOutput();
        }

        public string GetErrorOutputAsString()
        {
            var output = GetErrorOutput();
            return GetOutputAsString(output);
        }

        private static string GetOutputAsString(string[] output)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var t in output)
                sb.AppendLine(t);
            return sb.ToString();
        }

        private int SleepTimeoutMiliseconds
        {
            get { return 10; }
        }

        public void WaitForExit()
        {
            // Case 1111601: Process.WaitForExit hangs on OSX platform
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                while (!_process.HasExited)
                {
                    // Don't consume 100% of CPU while waiting for process to exit
                    Thread.Sleep(SleepTimeoutMiliseconds);
                }
            }
            else
            {
                _process.WaitForExit();
            }
        }

        public bool WaitForExit(int milliseconds)
        {
            // Case 1111601: Process.WaitForExit hangs on OSX platform
            if (Application.platform == RuntimePlatform.OSXEditor)
            {
                var start = DateTime.Now;
                while (!_process.HasExited && (DateTime.Now - start).TotalMilliseconds < milliseconds)
                {
                    // Don't consume 100% of CPU while waiting for process to exit
                    Thread.Sleep(SleepTimeoutMiliseconds);
                }
                return _process.HasExited;
            }
            else
            {
                return _process.WaitForExit(milliseconds);
            }
        }
    }
}
