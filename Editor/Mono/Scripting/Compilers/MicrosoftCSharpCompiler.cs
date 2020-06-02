// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Scripting.Compilers
{
    internal class MicrosoftCSharpCompiler : ScriptCompilerBase
    {
        private Program process;
        public const string Name = "InternalCSharpCompiler";

        public MicrosoftCSharpCompiler(ScriptAssembly assembly, string tempOutputDirectory)
            : base(assembly, tempOutputDirectory) {}

        static void FillCompilerOptions(List<string> arguments, BuildTarget BuildTarget)
        {
            // This will ensure that csc.exe won't include csc.rsp
            // csc.rsp references .NET 4.5 assemblies which cause conflicts for us
            arguments.Add("/nostdlib+");

            // Case 755238: Always use english for outputing errors, the same way as Mono compilers do
            arguments.Add("/preferreduilang:en-US");
            arguments.Add("/langversion:latest");
        }

        internal static string GenerateResponseFile(ScriptAssembly assembly, string tempBuildDirectory)
        {
            var assemblyOutputPath = PrepareFileName(AssetPath.Combine(tempBuildDirectory, assembly.Filename));

            var arguments = new List<string>
            {
                "/target:library",
                "/nowarn:0169",
                "/out:" + assemblyOutputPath
            };

            if (assembly.CompilerOptions.EmitReferenceAssembly)
            {
                var referenceAssemblyOutputPath = PrepareFileName(AssetPath.Combine(tempBuildDirectory, assembly.ReferenceAssemblyFilename));
                arguments.Add($"/refout:{referenceAssemblyOutputPath}");
            }

            if (assembly.CompilerOptions.AllowUnsafeCode)
            {
                arguments.Add("/unsafe");
            }

            if (assembly.CompilerOptions.UseDeterministicCompilation)
            {
                arguments.Add("/deterministic");
            }

            arguments.Add("/debug:portable");

            if (assembly.CompilerOptions.CodeOptimization == CodeOptimization.Release)
            {
                arguments.Add("/optimize+");
            }
            else
            {
                arguments.Add("/optimize-");
            }

            FillCompilerOptions(arguments, assembly.BuildTarget);

            string[] scriptAssemblyReferences = new string[assembly.ScriptAssemblyReferences.Length];
            for (var index = 0; index < assembly.ScriptAssemblyReferences.Length; index++)
            {
                var reference = assembly.ScriptAssemblyReferences[index];
                scriptAssemblyReferences[index] = "/reference:" +
                    PrepareFileName(AssetPath.Combine(assembly.OutputDirectory,
                    reference.Filename));
            }
            Array.Sort(scriptAssemblyReferences, StringComparer.Ordinal);
            arguments.AddRange(scriptAssemblyReferences);

            Array.Sort(assembly.References, StringComparer.Ordinal);
            foreach (var reference in assembly.References)
            {
                arguments.Add("/reference:" + PrepareFileName(reference));
            }

            var defines = assembly.Defines.Distinct().ToArray();
            Array.Sort(defines, StringComparer.Ordinal);
            foreach (var define in defines)
            {
                arguments.Add("/define:" + define);
            }

            Array.Sort(assembly.Files, StringComparer.Ordinal);
            foreach (var source in assembly.Files)
            {
                var f = PrepareFileName(source);
                f = Paths.UnifyDirectorySeparator(f);
                arguments.Add(f);
            }

            var responseFileProvider = assembly.Language?.CreateResponseFileProvider();
            var responseFilesList = responseFileProvider?.Get(assembly.OriginPath) ?? new List<string>();
            HashSet<string> responseFiles = new HashSet<string>(responseFilesList);

            string obsoleteResponseFile = responseFiles.SingleOrDefault(
                x => CompilerSpecificResponseFiles.MicrosoftCSharpCompilerObsolete.Contains(AssetPath.GetFileName(x)));

            if (!string.IsNullOrEmpty(obsoleteResponseFile))
            {
                Debug.LogWarning($"Using obsolete custom response file '{AssetPath.GetFileName(obsoleteResponseFile)}'. Please use '{CompilerSpecificResponseFiles.MicrosoftCSharpCompiler}' instead.");
            }

            foreach (var file in responseFiles)
            {
                AddResponseFileToArguments(arguments, file, assembly.CompilerOptions.ApiCompatibilityLevel);
            }

            var responseFile = CommandLineFormatter.GenerateResponseFile(arguments);

            return responseFile;
        }

        private static void ThrowCompilerNotFoundException(string path)
        {
            throw new Exception(string.Format("'{0}' not found. Is your Unity installation corrupted?", path));
        }

        public override void BeginCompiling()
        {
            if (process != null)
                throw new InvalidOperationException("Compilation has already begun!");

            var csc = Paths.Combine(EditorApplication.applicationContentsPath, "Tools", "RoslynScripts", "unity_csc");
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                csc += ".bat";
            }
            else
            {
                csc += ".sh";
            }

            csc = Paths.UnifyDirectorySeparator(csc);

            if (!File.Exists(csc))
                ThrowCompilerNotFoundException(csc);

            if (assembly.GeneratedResponseFile == null)
            {
                assembly.GeneratedResponseFile = GenerateResponseFile(assembly, tempOutputDirectory);
            }

            var psi = new ProcessStartInfo() { Arguments = "/noconfig @" + assembly.GeneratedResponseFile, FileName = csc, CreateNoWindow = true };
            var program = new Program(psi);
            program.Start();

            process = program;
        }

        protected string[] GetStreamContainingCompilerMessages()
        {
            return GetStandardOutput();
        }

        protected CompilerOutputParserBase CreateOutputParser()
        {
            return new MicrosoftCSharpCompilerOutputParser();
        }

        protected string[] GetErrorOutput()
        {
            return process.GetErrorOutput();
        }

        protected string[] GetStandardOutput()
        {
            return process.GetStandardOutput();
        }

        protected void DumpStreamOutputToLog(string outputFile)
        {
            bool hadCompilationFailure = CompilationHadFailure();

            string[] errorOutput = GetErrorOutput();

            if (hadCompilationFailure || errorOutput.Length != 0)
            {
                Console.WriteLine("");
                Console.WriteLine("-----Compiler Commandline Arguments:");
                process.LogProcessStartInfo();

                string[] stdOutput = GetStandardOutput();

                Console.WriteLine(
                    "-----CompilerOutput:-stdout--exitcode: " + process.ExitCode
                    + "--compilationhadfailure: " + hadCompilationFailure
                    + "--outfile: " + outputFile
                );
                foreach (string line in stdOutput)
                    Console.WriteLine(line);

                if (errorOutput != null && errorOutput.Any())
                {
                    Console.WriteLine("-----CompilerOutput:-stderr----------");
                    foreach (string line in errorOutput)
                        Console.WriteLine(line);
                }
                Console.WriteLine("-----EndCompilerOutput---------------");
            }
        }

        public override void Dispose()
        {
            if (process != null)
            {
                process.Dispose();
                process = null;
            }
        }

        public override bool Poll()
        {
            if (process == null)
                return true;

            return process.HasExited;
        }

        public override void WaitForCompilationToFinish()
        {
            process.WaitForExit();
        }

        public override CompilerMessage[] GetCompilerMessages()
        {
            if (!Poll())
                Debug.LogWarning("Compile process is not finished yet. This should not happen.");

            if (process == null)
            {
                return new CompilerMessage[0];
            }

            var outputFile = AssetPath.Combine(tempOutputDirectory, assembly.Filename);

            DumpStreamOutputToLog(outputFile);

            return CreateOutputParser().Parse(
                GetStreamContainingCompilerMessages(),
                CompilationHadFailure(),
                assembly.Filename
                ).ToArray();
        }

        public override ProcessStartInfo GetProcessStartInfo()
        {
            return process.GetProcessStartInfo();
        }

        private bool CompilationHadFailure()
        {
            return (process.ExitCode != 0);
        }

        public static string[] Compile(string[] sources, string[] references, string[] defines, string outputFile, bool allowUnsafeCode)
        {
            var assembly = new ScriptAssembly
            {
                BuildTarget = BuildTarget.StandaloneWindows,
                Files = sources,
                References = references,
                Defines = defines,
                OutputDirectory = AssetPath.GetDirectoryName(outputFile),
                Filename = AssetPath.GetFileName(outputFile),
                CompilerOptions = new Compilation.ScriptCompilerOptions(),
                ScriptAssemblyReferences = new ScriptAssembly[0]
            };

            assembly.CompilerOptions.AllowUnsafeCode = allowUnsafeCode;
            assembly.CompilerOptions.ApiCompatibilityLevel = ApiCompatibilityLevel.NET_Standard_2_0;

            using (var c = new MicrosoftCSharpCompiler(assembly, assembly.OutputDirectory))
            {
                c.BeginCompiling();
                while (!c.Poll())
                    System.Threading.Thread.Sleep(50);
                return c.GetCompilerMessages().Select(cm => cm.message).ToArray();
            }
        }
    }
}
