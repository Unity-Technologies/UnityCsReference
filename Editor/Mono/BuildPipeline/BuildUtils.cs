// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Modules;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEngine;
using UnityEditor;
using UnityEditor.BuildReporting;
using UnityEditor.Utils;
using Debug = UnityEngine.Debug;
using PackageInfo = Unity.DataContract.PackageInfo;
using System.Xml.Linq;
using System.Xml.XPath;

namespace UnityEditorInternal
{
    internal class NativeProgram : Program
    {
        public NativeProgram(string executable, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                Arguments = arguments,
                CreateNoWindow = true,
                FileName = executable,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = Application.dataPath + "/..",
                UseShellExecute = false
            };

            _process.StartInfo = startInfo;
        }
    }

    internal class Runner
    {
        internal static void RunManagedProgram(string exe, string args)
        {
            RunManagedProgram(exe, args, Application.dataPath + "/..", null, null);
        }

        internal static void RunManagedProgram(string exe, string args, string workingDirectory, CompilerOutputParserBase parser, Action<ProcessStartInfo> setupStartInfo)
        {
            Program p;

            // Run on .NET if running on windows
            // It's twice as fast as Mono for IL2CPP.exe
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                var startInfo = new ProcessStartInfo()
                {
                    Arguments = args,
                    CreateNoWindow = true,
                    FileName = exe
                };

                if (setupStartInfo != null)
                    setupStartInfo(startInfo);

                p = new Program(startInfo);
            }
            else
            {
                p = new ManagedProgram(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), null, exe, args, false, setupStartInfo);
            }

            RunProgram(p, exe, args, workingDirectory, parser);
        }

        internal static void RunNetCoreProgram(string exe, string args, string workingDirectory, CompilerOutputParserBase parser, Action<ProcessStartInfo> setupStartInfo)
        {
            var p = new NetCoreProgram(exe, args, setupStartInfo);
            RunProgram(p, exe, args, workingDirectory, parser);
        }

        // Used when debugging il2cpp.exe from Windows, please don't remove it
        public static void RunNativeProgram(string exe, string args)
        {
            using (var p = new NativeProgram(exe, args))
            {
                p.Start();
                p.WaitForExit();
                if (p.ExitCode != 0)
                {
                    Debug.LogError("Failed running " + exe + " " + args + "\n\n" + p.GetAllOutput());

                    throw new Exception(string.Format("{0} did not run properly!", exe));
                }
            }
        }

        private static void RunProgram(Program p, string exe, string args, string workingDirectory, CompilerOutputParserBase parser)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            using (p)
            {
                p.GetProcessStartInfo().WorkingDirectory = workingDirectory;
                p.Start();
                p.WaitForExit();

                stopwatch.Stop();
                Console.WriteLine("{0} exited after {1} ms.", exe, stopwatch.ElapsedMilliseconds);

                IEnumerable<CompilerMessage> messages = null;
                if (parser != null)
                {
                    var errorOutput = p.GetErrorOutput();
                    var standardOutput = p.GetStandardOutput();
                    messages = parser.Parse(errorOutput, standardOutput, true);
                }

                if (p.ExitCode != 0)
                {
                    if (messages != null)
                    {
                        foreach (var message in messages)
                            Debug.LogPlayerBuildError(message.message, message.file, message.line, message.column);
                    }

                    Debug.LogError("Failed running " + exe + " " + args + "\n\n" + p.GetAllOutput());

                    throw new Exception(string.Format("{0} did not run properly!", exe));
                }
                else
                {
                    if (messages != null)
                    {
                        foreach (var message in messages)
                            Console.WriteLine(message.message + " - " + message.file + " - " + message.line + " - " + message.column);
                    }
                }
            }
        }
    }
}
