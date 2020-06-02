// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Scripting.Compilers;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation
{
    class PostProcessorTask : IDisposable
    {
        internal class PostProcessorOutputParser : CSharpCompilerOutputParserBase
        {
            private static Regex sCompilerOutput = new Regex(@"\s*(?<filename>.*)\((?<line>\d+),(?<column>\d+)\):\s*(?<type>warning|error)\s*(?<message>.*)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);

            protected override Regex GetOutputRegex()
            {
                return sCompilerOutput;
            }

            protected override string GetErrorIdentifier()
            {
                return "error";
            }
        }


        public ScriptAssembly Assembly { get; private set; }
        List<CompilerMessage> CompilerMessages;
        string tempOutputDirectory;
        IILPostProcessing ilPostProcessing;
        Program process;

        public PostProcessorTask(ScriptAssembly assembly,
                                 List<CompilerMessage> compilerMessages,
                                 string tempOutputDirectory,
                                 IILPostProcessing ilPostProcessing)
        {
            Assembly = assembly;
            CompilerMessages = compilerMessages;
            this.tempOutputDirectory = tempOutputDirectory;
            this.ilPostProcessing = ilPostProcessing;
        }

        public void Dispose()
        {
            if (process != null)
            {
                process.Dispose();
                process = null;
            }
        }

        public ProcessStartInfo GetProcessStartInfo()
        {
            return process.GetProcessStartInfo();
        }

        bool ProcessHadFailure()
        {
            return (process.ExitCode != 0);
        }

        public List<CompilerMessage> GetCompilerMessages()
        {
            var standardOutput = process.GetStandardOutput();
            var errorOutput = process.GetErrorOutput();

            var standardOutputString = string.Join("\n", standardOutput);
            Console.WriteLine(standardOutputString);

            if (errorOutput != null && errorOutput.Length > 0)
            {
                var standardErrorString = string.Join("\n", errorOutput);
                Console.WriteLine(standardErrorString);
            }

            var processMessages = new PostProcessorOutputParser().Parse(
                errorOutput,
                ProcessHadFailure(),
                Assembly.Filename
            );

            return CompilerMessages.Concat(processMessages).ToList();
        }

        public bool IsFinished
        {
            get
            {
                if (process == null)
                    return false;

                return process.HasExited;
            }
        }

        static string PrepareFileName(string fileName)
        {
            return CommandLineFormatter.PrepareFileName(fileName);
        }

        public void Start()
        {
            var runner = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "ILPostProcessorRunner", "ILPostProcessorRunner.exe");

            if (!File.Exists(runner))
                throw new Exception(string.Format($"'{runner}' not found. Is your Unity installation corrupted?"));

            var assemblyPath = AssetPath.GetFullPath(AssetPath.Combine(tempOutputDirectory, Assembly.Filename));
            var assemblyReferencesPaths = Assembly.GetAllReferences().ToArray();
            var assemblyFolderPaths = ilPostProcessing.AssemblySearchPaths;

            var outputDirectory = AssetPath.GetFullPath(tempOutputDirectory);
            var postProcessorPaths = ilPostProcessing.PostProcessorAssemblyPaths;

            var arguments = new List<string>
            {
                $"-a \"{assemblyPath}\"",
                $"-f \"{string.Join(",", assemblyFolderPaths)}\"",
                $"-r \"{string.Join(",", assemblyReferencesPaths)}\"",
                $"-d \"{string.Join(",", Assembly.Defines)}\"",
                $"-p \"{string.Join(",", postProcessorPaths)}\"",
                $"-o \"{outputDirectory}\"",
            };

            var responseFilePath = PrepareFileName(AssetPath.GetFullPath(CommandLineFormatter.GenerateResponseFile(arguments)));

            var args = $"@{responseFilePath}";

            // Always run on Mono until all ILPostProcessors have been fixed
            // to run on NET Core.
            //if (NetCoreRunProgram.IsSupported())
            //{
            //    process = new NetCoreRunProgram(runner, args, null);
            //}
            //else
            {
                process = new ManagedProgram(MonoInstallationFinder.GetMonoInstallation("MonoBleedingEdge"), null, runner, args, false, null);
            }

            process.Start();
        }

        public bool Poll()
        {
            return process.HasExited;
        }
    }
}
