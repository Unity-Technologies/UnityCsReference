// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

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
            if (_process != null)
                LogProcessStartInfo(_process.StartInfo);
            else
                Console.WriteLine("Failed to retrieve process startInfo");
        }

        //please dont kill this code.
        private static void LogProcessStartInfo(ProcessStartInfo si)
        {
            Console.WriteLine("Filename: " + si.FileName);
            Console.WriteLine("Arguments: " + si.Arguments);

            foreach (DictionaryEntry envVar in si.EnvironmentVariables)
                if (envVar.Key.ToString().StartsWith("MONO"))
                    Console.WriteLine("{0}: {1}", envVar.Key, envVar.Value);

            int responsefileindex = si.Arguments.IndexOf("Temp/UnityTempFile");
            Console.WriteLine("index: " + responsefileindex);

            if (responsefileindex > 0)
            {
                var responsefile = si.Arguments.Substring(responsefileindex);
                Console.WriteLine("Responsefile: " + responsefile + " Contents: ");
                Console.WriteLine(System.IO.File.ReadAllText(responsefile));
            }
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

        public void WaitForExit()
        {
            _process.WaitForExit();
        }

        public bool WaitForExit(int milliseconds)
        {
            return _process.WaitForExit(milliseconds);
        }
    }
}
