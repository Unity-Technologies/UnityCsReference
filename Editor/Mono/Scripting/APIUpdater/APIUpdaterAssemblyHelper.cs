// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

using UnityEditor.Utils;
using UnityEngine;

using UnityEditor.Scripting.Compilers;

namespace UnityEditor.Scripting
{
    internal class APIUpdaterAssemblyHelper
    {
        // See AssemblyUpdater/Program.cs
        internal const byte Success                       = 0x00;
        internal const byte ContainsUpdaterConfigurations = (1 << 3) + 2;
        internal const byte UpdatesApplied                = (1 << 3) + 3;

        internal static int Run(string arguments, string workingDir, out string stdOut, out string stdErr)
        {
            var assemblyUpdaterPath = EditorApplication.applicationContentsPath + "/Tools/ScriptUpdater/AssemblyUpdater.exe";
            var monodistribution = MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge");

            var monoexe = Path.Combine(monodistribution, "bin/mono");
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                monoexe = Application.platform == RuntimePlatform.WindowsEditor
                    ? CommandLineFormatter.EscapeCharsWindows(monoexe + ".exe")
                    : CommandLineFormatter.EscapeCharsQuote(monoexe + ".exe");
            }

            var startInfo = new ProcessStartInfo
            {
                Arguments = assemblyUpdaterPath + " " + arguments,
                CreateNoWindow = true,
                FileName = monoexe,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = workingDir,
                UseShellExecute = false
            };

            var assemblyUpdaterProcess = new Program(startInfo);
            assemblyUpdaterProcess.LogProcessStartInfo();
            assemblyUpdaterProcess.Start();

            assemblyUpdaterProcess.WaitForExit();

            stdOut = assemblyUpdaterProcess.GetStandardOutputAsString();
            stdErr = string.Join("\r\n", assemblyUpdaterProcess.GetErrorOutput());

            return assemblyUpdaterProcess.ExitCode;
        }

        internal static string ArgumentsForUpdateAssembly(string assemblyPath, string tempOutputPath, IEnumerable<string> updateConfigSourcePaths)
        {
            var assemblyFullPath = ResolveAssemblyPath(assemblyPath);
            return "update -a "
                + assemblyFullPath
                + " --output " + CommandLineFormatter.PrepareFileName(tempOutputPath)
                + APIVersionArgument()
                + AssemblySearchPathArgument(updateConfigSourcePaths.Select(Path.GetDirectoryName).Distinct())
                + ConfigurationProviderAssembliesPathArgument(updateConfigSourcePaths);
        }

        internal static string ArgumentsForCheckingForUpdaterConfigsOn(string assemblyPath)
        {
            var assemblyFullPath = ResolveAssemblyPath(assemblyPath);
            return "checkupdaterconfigs -a " + assemblyFullPath
                + TimeStampArgument()
                + AssemblySearchPathArgument();
        }

        internal static bool IsError(int exitCode)
        {
            // See AssemblyUpdater/Program.cs
            return (exitCode & (1 << 7)) != 0;
        }

        private static string ResolveAssemblyPath(string assemblyPath)
        {
            return CommandLineFormatter.PrepareFileName(assemblyPath);
        }

        private static string AssemblySearchPathArgument(IEnumerable<string> configurationSourceDirectories = null)
        {
            var searchPath = Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), "Managed") + ","
                + "+" + Path.Combine(EditorApplication.applicationContentsPath, "UnityExtensions/Unity") + ","
                + "+" + Application.dataPath;

            if (configurationSourceDirectories != null)
            {
                var searchPathFromConfigSources = configurationSourceDirectories.Aggregate("", (acc, curr) =>  acc + ",+" + curr);
                searchPath += searchPathFromConfigSources;
            }

            return " -s \"" + searchPath + "\"";
        }

        private static string ConfigurationProviderAssembliesPathArgument(IEnumerable<string> updateConfigSourcePaths)
        {
            var paths = new StringBuilder();
            foreach (var configSourcePath in updateConfigSourcePaths)
            {
                paths.AppendFormat(" {0}", CommandLineFormatter.PrepareFileName(configSourcePath));
            }

            return paths.ToString();
        }

        private static string APIVersionArgument()
        {
            return " --api-version " + Application.unityVersion + " ";
        }

        private static string TimeStampArgument()
        {
            return " --timestamp " + DateTime.Now.Ticks + " ";
        }
    }
}
