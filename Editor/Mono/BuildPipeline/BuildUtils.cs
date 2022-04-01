// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEngine;
using UnityEditor.Utils;
using Debug = UnityEngine.Debug;

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
            Program p;

            if (Path.GetExtension(exe).Equals(".dll", StringComparison.OrdinalIgnoreCase))
            {
                p = new NetCoreProgram(exe, args, setupStartInfo);
            }
            else
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

            RunProgram(p, exe, args, workingDirectory, parser);
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
                UnityLogWriter.WriteStringToUnityLog($"{exe} exited after {stopwatch.ElapsedMilliseconds} ms.\n");

                var messages = new List<CompilerMessage>();
                if (parser != null)
                {
                    var errorOutput = p.GetErrorOutput();
                    var standardOutput = p.GetStandardOutput();
                    messages.AddRange(parser.Parse(errorOutput, standardOutput, true));
                }

                foreach (var message in NonerrorMessages(messages))
                    Debug.LogWarning(message.message);

                var errorMessages = ErrorMessages(messages).ToArray();
                if (p.ExitCode != 0)
                {
                    if (errorMessages.Any())
                    {
                        // Use the last error as the exception message to cause the build to fail. But
                        // log any other errors that might exist.
                        var lastError = messages.Last();
                        foreach (var message in errorMessages.Take(errorMessages.Length - 1))
                            Debug.LogPlayerBuildError(message.message, message.file, message.line, message.column);
                        throw new Exception(lastError.message);
                    }

                    // No messages were parsed, so just put all of the output in the error.
                    throw new Exception("Failed running " + exe + " " + args + "\n\n" + p.GetAllOutput());
                }

                // The exit code was zero, but there are error messages. Don't fail the build by throwing an exception,
                // but log the messages to the editor log.
                foreach (var message in errorMessages)
                    Console.WriteLine(message.message + " - " + message.file + " - " + message.line + " - " + message.column);
            }
        }

        private static IEnumerable<CompilerMessage> ErrorMessages(List<CompilerMessage> messages)
        {
            return messages.Where(m => m.type == CompilerMessageType.Error);
        }

        private static IEnumerable<CompilerMessage> NonerrorMessages(List<CompilerMessage> messages)
        {
            return messages.Where(m => m.type != CompilerMessageType.Error);
        }
    }
}
