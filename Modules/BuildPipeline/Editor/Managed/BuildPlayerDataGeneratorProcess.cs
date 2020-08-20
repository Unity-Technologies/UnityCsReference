// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Scripting;
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
        void Execute(BuildPlayerDataGeneratorOptions options);
    }

    internal class BuildPlayerDataGeneratorProcess : IBuildPlayerDataGeneratorProcess
    {
        private const string buildDataGenerator = "BuildPlayerDataGenerator";
        private const string buildDataGeneratorPathExe = buildDataGenerator + ".exe";
        const int fiveMinInMs = 300000;

        private static string buildDataGeneratorPath = Paths.Combine(EditorApplication.applicationContentsPath, "Tools",
            buildDataGenerator, buildDataGeneratorPathExe);


        public void Execute(BuildPlayerDataGeneratorOptions options)
        {
            Program typeDbGeneratorProcess;
            var arguments = OptionsToStringArgument(options);

            if (NetCoreRunProgram.IsSupported())
            {
                typeDbGeneratorProcess = new NetCoreRunProgram(buildDataGeneratorPath, arguments, null);
            }
            else
            {
                typeDbGeneratorProcess =
                    new ManagedProgram(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), null,
                        buildDataGeneratorPath, arguments, false, null);
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
        }

        private void ProcessExit(Program typeDbGeneratorProcess)
        {
            if (typeDbGeneratorProcess.HasExited && typeDbGeneratorProcess.ExitCode == 0)
            {
                Console.WriteLine("BuildPlayerGenerator: Succeeded");
                return;
            }
            Console.WriteLine($"Failure running BuildPlayerGenerator\nArguments:\n{typeDbGeneratorProcess.GetProcessStartInfo().Arguments}{Environment.NewLine}{typeDbGeneratorProcess.GetAllOutput()}");
        }

        private string OptionsToStringArgument(BuildPlayerDataGeneratorOptions options)
        {
            string result =
                $"-a \"{string.Join(",", options.Assemblies)}\" -s \"{string.Join(",", options.SearchPaths)}\" -o \"{options.OutputPath}\" -rn=\"{options.GeneratedRuntimeInitializeOnLoadName}\" -tn=\"{options.GeneratedTypeDbName}\"";
            return result;
        }
    }
}
