// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using System.IO;
using System;
using System.Linq;
using System.Text;
using UnityEditor.Scripting.Compilers;
using UnityEngine;
using UnityEditor.Modules;

namespace UnityEditor
{
    internal class ManagedEditorCodeRebuilder
    {
        private static readonly char[] kNewlineChars = new[] { '\r', '\n' };

        //called from native code.
        static bool Run(out CompilerMessage[] messages, bool includeModules)
        {
            int exitcode;
            messages = ParseResults(GetOutputStream(GetJamStartInfo(includeModules), out exitcode));
            return exitcode == 0;
        }

        private static ProcessStartInfo GetJamStartInfo(bool includeModules)
        {
            StringBuilder moduleArgs = new StringBuilder();
            moduleArgs.Append("jam.pl LiveReloadableEditorAssemblies");
            if (includeModules)
            {
                foreach (string target in ModuleManager.GetJamTargets())
                    moduleArgs.Append(" ").Append(target);
            }

            return new ProcessStartInfo
            {
                WorkingDirectory = Unsupported.GetBaseUnityDeveloperFolder(),
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                //The only reason jam.pl exists is that I cannot figure out how to call jam.bat, or jam.exe directly. magic.
                Arguments = moduleArgs.ToString(),
                FileName = "perl",
            };
        }

        private static CompilerMessage[] ParseResults(string text)
        {
            Console.Write(text);
            var lines = text.Split(kNewlineChars, StringSplitOptions.RemoveEmptyEntries);
            var prefix = Unsupported.GetBaseUnityDeveloperFolder();

            var msgs = new MonoCSharpCompilerOutputParser().Parse(lines, false).ToList();
            for (var index = 0; index < msgs.Count; index++)
            {
                var msg = msgs[index];
                msg.file = Path.Combine(prefix, msg.file);
                msgs[index] = msg;
            }
            return msgs.ToArray();
        }

        private static string GetOutputStream(ProcessStartInfo startInfo, out int exitCode)
        {
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            var p = new Process { StartInfo = startInfo };

            // Read data asynchronously
            var sbStandardOut = new StringBuilder();
            var sbStandardError = new StringBuilder();
            p.OutputDataReceived += (sender, data) => sbStandardOut.AppendLine(data.Data);
            p.ErrorDataReceived += (sender, data) => sbStandardError.AppendLine(data.Data);
            p.Start();
            if (startInfo.RedirectStandardError)
                p.BeginErrorReadLine();
            else
                p.BeginOutputReadLine();

            // Wain until process is done
            p.WaitForExit();

            var output = startInfo.RedirectStandardError ? sbStandardError.ToString() : sbStandardOut.ToString();
            exitCode = p.ExitCode;
            return output;
        }
    }
}
