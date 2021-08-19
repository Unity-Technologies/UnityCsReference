// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor.Scripting;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;

namespace UnityEditor.Build.Player
{
    internal class BuildPlayerDataGeneratorOptions
    {
        public string[] Assemblies { get; set; }
        public string[] SearchPaths { get; set; }
        public string OutputPath { get; set; }
        public string GeneratedTypeDbName { get; set; }
        public string GeneratedRuntimeInitializeOnLoadName { get; set; }
    }

    internal interface IBuildPlayerDataGeneratorProcess
    {
/// <summary>
/// Will execute the BuildPlayerDataGenerator process with the given options
/// </summary>
/// <param name="options"></param>
/// <returns>returns true if the Execution was successful</returns>
        bool Execute(BuildPlayerDataGeneratorOptions options);
    }

    internal class BuildPlayerDataGeneratorProcess : IBuildPlayerDataGeneratorProcess
    {
        private const string buildDataGenerator = "BuildPlayerDataGenerator";
        private const string buildDataGeneratorPathExe = buildDataGenerator + ".exe";
        const int fiveMinInMs = 300000;

        private static string buildDataGeneratorPath = Paths.Combine(EditorApplication.applicationContentsPath, "Tools",
            buildDataGenerator, buildDataGeneratorPathExe);

        public bool Execute(BuildPlayerDataGeneratorOptions options)
        {
            Program typeDbGeneratorProcess;
            var arguments = OptionsToStringArgument(options);
            var responseFile = Path.Combine(Directory.GetParent(options.OutputPath).FullName, "response.rsp");
            File.WriteAllText(responseFile, arguments);

            if (NetCoreRunProgram.IsSupported())
            {
                typeDbGeneratorProcess = new NetCoreRunProgram(buildDataGeneratorPath, $"@{CommandLineFormatter.PrepareFileName(responseFile)}", null);
            }
            else
            {
                typeDbGeneratorProcess =
                    new ManagedProgram(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), null,
                        buildDataGeneratorPath, $"@{CommandLineFormatter.PrepareFileName(responseFile)}", false, null);
            }

            typeDbGeneratorProcess.Start((s, e) =>
            {
                ProcessExit(typeDbGeneratorProcess);
            });

            if (!typeDbGeneratorProcess.WaitForExit(fiveMinInMs))
            {
                Console.WriteLine($"Stopping BuildPlayerGenerator after running for {fiveMinInMs/1000} seconds");
                ProcessExit(typeDbGeneratorProcess);
                typeDbGeneratorProcess.Kill();
            }
            return typeDbGeneratorProcess.ExitCode == 0;
        }

        private void ProcessExit(Program typeDbGeneratorProcess)
        {
            if (typeDbGeneratorProcess.HasExited)
            {
                var stderr = typeDbGeneratorProcess.GetErrorOutput();
                foreach (var s in stderr)
                    Debug.LogError(s);

                if (typeDbGeneratorProcess.ExitCode == 0)
                {
                    var stdout = typeDbGeneratorProcess.GetStandardOutput();
                    foreach (var s in stdout)
                        Debug.Log(s);

                    Console.WriteLine("BuildPlayerGenerator: Succeeded");
                    return;
                }
            }
            Console.WriteLine($"Failure running BuildPlayerGenerator\nArguments:\n{typeDbGeneratorProcess.GetProcessStartInfo().Arguments}{Environment.NewLine}{typeDbGeneratorProcess.GetAllOutput()}");
        }

        private string OptionsToStringArgument(BuildPlayerDataGeneratorOptions options)
        {
            // Quote Windows style even on Unix
            var sb = new StringBuilder();
            foreach (var assembly in options.Assemblies)
            {
                sb.Append("-a=").Append("\"").Append(assembly).AppendLine("\"");
            }
            foreach (var searchPath in options.SearchPaths)
            {
                sb.Append("-s=").Append("\"").Append(searchPath).AppendLine("\"");
            }
            sb.Append("-o=").Append("\"").Append(options.OutputPath).AppendLine("\"");
            sb.Append("-rn=").Append("\"").Append(options.GeneratedRuntimeInitializeOnLoadName).AppendLine("\"");
            sb.Append("-tn=").Append("\"").Append(options.GeneratedTypeDbName).AppendLine("\"");
            return sb.ToString();
        }
    }
}
