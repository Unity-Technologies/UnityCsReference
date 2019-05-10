// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Modules;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEditor.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Scripting.Compilers
{
    internal class MicrosoftCSharpCompiler : ScriptCompilerBase
    {
        public static readonly string ResponseFilename = "csc.rsp";

        public MicrosoftCSharpCompiler(ScriptAssembly assembly, EditorScriptCompilationOptions options, string tempOutputDirectory) : base(assembly, options, tempOutputDirectory)
        {
        }

        static void FillCompilerOptions(List<string> arguments, bool buildingForEditor, BuildTarget BuildTarget)
        {
            // This will ensure that csc.exe won't include csc.rsp
            // csc.rsp references .NET 4.5 assemblies which cause conflicts for us
            arguments.Add("/nostdlib+");

            // Case 755238: Always use english for outputing errors, the same way as Mono compilers do
            arguments.Add("/preferreduilang:en-US");
            arguments.Add("/langversion:latest");

            var platformSupportModule = ModuleManager.FindPlatformSupportModule(ModuleManager.GetTargetStringFromBuildTarget(BuildTarget));
            if (platformSupportModule != null && !buildingForEditor)
            {
                var compilationExtension = platformSupportModule.CreateCompilationExtension();

                arguments.AddRange(compilationExtension.GetAdditionalAssemblyReferences().Select(r => "/reference:\"" + r + "\""));
                arguments.AddRange(compilationExtension.GetWindowsMetadataReferences().Select(r => "/reference:\"" + r + "\""));
                arguments.AddRange(compilationExtension.GetAdditionalDefines().Select(d => "/define:" + d));
                arguments.AddRange(compilationExtension.GetAdditionalSourceFiles());
            }
        }

        internal static string GenerateResponseFile(ScriptAssembly assembly, EditorScriptCompilationOptions options, string tempBuildDirectory)
        {
            bool buildingForEditor = (options & EditorScriptCompilationOptions.BuildingForEditor) == EditorScriptCompilationOptions.BuildingForEditor;
            bool developmentBuild = (options & EditorScriptCompilationOptions.BuildingDevelopmentBuild) == EditorScriptCompilationOptions.BuildingDevelopmentBuild;

            var assemblyOutputPath = PrepareFileName(AssetPath.Combine(tempBuildDirectory, assembly.Filename));

            var arguments = new List<string>
            {
                "/target:library",
                "/nowarn:0169",
                "/out:" + assemblyOutputPath
            };

            if (assembly.CompilerOptions.AllowUnsafeCode)
            {
                arguments.Add("/unsafe");
            }

            arguments.Add("/debug:portable");

            var disableOptimizations = developmentBuild || (buildingForEditor && EditorPrefs.GetBool("AllowAttachedDebuggingOfEditor", true));

            if (!disableOptimizations)
            {
                arguments.Add("/optimize+");
            }
            else
            {
                arguments.Add("/optimize-");
            }

            FillCompilerOptions(arguments, buildingForEditor, assembly.BuildTarget);

            foreach (var reference in assembly.ScriptAssemblyReferences)
            {
                arguments.Add("/reference:" + PrepareFileName(AssetPath.Combine(assembly.OutputDirectory, reference.Filename)));
            }

            foreach (var reference in assembly.References)
            {
                arguments.Add("/reference:" + PrepareFileName(reference));
            }

            foreach (var define in assembly.Defines.Distinct())
            {
                arguments.Add("/define:" + define);
            }

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

        protected override Program StartCompiler()
        {
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
                assembly.GeneratedResponseFile = GenerateResponseFile(assembly, options, tempOutputDirectory);
            }

            var psi = new ProcessStartInfo() { Arguments = "/noconfig @" + assembly.GeneratedResponseFile, FileName = csc, CreateNoWindow = true };
            var program = new Program(psi);
            program.Start();

            return program;
        }

        protected override string[] GetStreamContainingCompilerMessages()
        {
            return GetStandardOutput();
        }

        protected override CompilerOutputParserBase CreateOutputParser()
        {
            return new MicrosoftCSharpCompilerOutputParser();
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

            using (var c = new MicrosoftCSharpCompiler(assembly, EditorScriptCompilationOptions.BuildingEmpty, assembly.OutputDirectory))
            {
                c.BeginCompiling();
                while (!c.Poll())
                    System.Threading.Thread.Sleep(50);
                return c.GetCompilerMessages().Select(cm => cm.message).ToArray();
            }
        }
    }
}
