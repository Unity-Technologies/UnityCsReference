// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor.Utils;
using UnityEngine;
using System.Linq;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil;
using UnityEditor.Modules;
using UnityEditor.Scripting.Compilers;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Scripting
{
    internal class APIUpdaterAssemblyHelper
    {
        public static void Run(string commaSeparatedListOfAssemblies)
        {
            var assemblies = commaSeparatedListOfAssemblies.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

            APIUpdaterHelper.HandleFilesInPackagesVirtualFolder(assemblies);

            foreach (var assemblyPath in assemblies)
            {
                if ((File.GetAttributes(assemblyPath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    APIUpdaterLogger.WriteErrorToConsole("Error can't update assembly {0} the file is read-only", assemblyPath);
                    return;
                }
            }

            APIUpdaterLogger.WriteToFile("Started to update {0} assemblie(s)", assemblies.Count());

            var sw = new Stopwatch();
            sw.Start();

            foreach (var assemblyPath in assemblies)
            {
                if (!AssemblyHelper.IsManagedAssembly(assemblyPath))
                    continue;

                string stdOut, stdErr;
                var assemblyFullPath = ResolveAssemblyPath(assemblyPath);
                var exitCode = RunUpdatingProgram("AssemblyUpdater.exe", "-u -a " + assemblyFullPath + APIVersionArgument() + AssemblySearchPathArgument() + ConfigurationProviderAssembliesPathArgument() + NuGetArgument(), out stdOut, out stdErr);
                if (stdOut.Length > 0)
                    APIUpdaterLogger.WriteToFile("Assembly update output ({0})\r\n{1}", assemblyFullPath, stdOut);

                if (IsWarning(exitCode))
                    APIUpdaterLogger.WriteWarningToConsole(stdOut);

                if (IsError(exitCode))
                    APIUpdaterLogger.WriteErrorToConsole("Error {0} running AssemblyUpdater. Its output is: `{1}`", exitCode, stdErr);
            }

            APIUpdaterLogger.WriteToFile("Update finished in {0}s", sw.Elapsed.TotalSeconds);
        }

        private static string NuGetArgument()
        {
            var nugetPath = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "NuGet.exe");
            if (!File.Exists(nugetPath))
                return string.Empty;

            return " --nuget-path \"" + nugetPath + "\"";
        }

        private static bool IsWarning(int exitCode)
        {
            return (exitCode & (1 << 6)) != 0;
        }

        private static bool IsError(int exitCode)
        {
            return (exitCode & (1 << 7)) != 0;
        }

        private static string ResolveAssemblyPath(string assemblyPath)
        {
            return CommandLineFormatter.PrepareFileName(assemblyPath);
        }

        public static bool DoesAssemblyRequireUpgrade(string assemblyFullPath)
        {
            if (!File.Exists(assemblyFullPath))
                return false;

            if (!AssemblyHelper.IsManagedAssembly(assemblyFullPath))
                return false;

            if (!MayContainUpdatableReferences(assemblyFullPath))
                return false;

            string stdOut, stdErr;
            var ret = RunUpdatingProgram("AssemblyUpdater.exe", TimeStampArgument() + APIVersionArgument() + "--check-update-required -a " + CommandLineFormatter.PrepareFileName(assemblyFullPath) + AssemblySearchPathArgument() + ConfigurationProviderAssembliesPathArgument() + NuGetArgument(), out stdOut, out stdErr);
            {
                Console.WriteLine("{0}{1}", stdOut, stdErr);
                switch (ret)
                {
                    // See AssemblyUpdater/Program.cs
                    case 0:
                    case 1: return false;
                    case 2: return true;

                    default:
                        if (IsWarning(ret))
                        {
                            Debug.LogWarning(stdOut + Environment.NewLine + stdErr);
                        }
                        else
                        {
                            Debug.LogError(stdOut + Environment.NewLine + stdErr);
                        }
                        return false;
                }
            }
        }

        private static string AssemblySearchPathArgument()
        {
            var searchPath = Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), "Managed") + ","
                + "+" + Path.Combine(EditorApplication.applicationContentsPath, "UnityExtensions/Unity") + ","
                + "+" + Application.dataPath;

            return " -s \"" + searchPath + "\"";
        }

        private static string ConfigurationProviderAssembliesPathArgument()
        {
            var paths = new StringBuilder();
            foreach (var ext in ModuleManager.packageManager.unityExtensions)
            {
                foreach (var dllPath in ext.files.Where(f => f.Value.type == Unity.DataContract.PackageFileType.Dll).Select(pi => pi.Key))
                {
                    paths.AppendFormat(" {0}", CommandLineFormatter.PrepareFileName(Path.Combine(ext.basePath, dllPath)));
                }
            }

            var editorManagedPath = GetUnityEditorManagedPath();
            paths.AppendFormat(" {0}", CommandLineFormatter.PrepareFileName(Path.Combine(editorManagedPath, "UnityEngine.dll")));
            paths.AppendFormat(" {0}", CommandLineFormatter.PrepareFileName(Path.Combine(editorManagedPath, "UnityEditor.dll")));

            return paths.ToString();
        }

        private static string GetUnityEditorManagedPath()
        {
            return Path.Combine(MonoInstallationFinder.GetFrameWorksFolder(), "Managed");
        }

        private static int RunUpdatingProgram(string executable, string arguments, out string stdOut, out string stdErr)
        {
            var scriptUpdater = EditorApplication.applicationContentsPath + "/Tools/ScriptUpdater/" + executable;
            var program = new ManagedProgram(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), null, scriptUpdater, arguments, false, null);

            program.LogProcessStartInfo();
            program.Start();
            program.WaitForExit();

            stdOut = program.GetStandardOutputAsString();
            stdErr = string.Join("\r\n", program.GetErrorOutput());

            return program.ExitCode;
        }

        private static string APIVersionArgument()
        {
            return " --api-version " + Application.unityVersion + " ";
        }

        private static string TimeStampArgument()
        {
            return " --timestamp " + DateTime.Now.Ticks + " ";
        }

        internal static bool MayContainUpdatableReferences(string assemblyPath)
        {
            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyPath))
            {
                if (assembly.Name.IsWindowsRuntime)
                    return false;

                if (!IsTargetFrameworkValidOnCurrentOS(assembly))
                    return false;
            }

            return true;
        }

        private static bool IsTargetFrameworkValidOnCurrentOS(AssemblyDefinition assembly)
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT
                || !(assembly.HasCustomAttributes && assembly.CustomAttributes.Any(attr => TargetsWindowsSpecificFramework(attr)));
        }

        private static bool TargetsWindowsSpecificFramework(CustomAttribute targetFrameworkAttr)
        {
            if (!targetFrameworkAttr.AttributeType.FullName.Contains("System.Runtime.Versioning.TargetFrameworkAttribute"))
                return false;

            var regex = new Regex("\\.NETCore|\\.NETPortable");
            var targetsNetCoreOrPCL = targetFrameworkAttr.ConstructorArguments.Any(arg => arg.Type.FullName == typeof(string).FullName && regex.IsMatch((string)arg.Value));

            return targetsNetCoreOrPCL;
        }
    }
}
