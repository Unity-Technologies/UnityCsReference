// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bee.BeeDriver;
using NiceIO;
using ScriptCompilationBuildProgram.Data;
using Unity.Profiling;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEditor.Scripting.Compilers;
using UnityEngine;
using CompilerMessage = UnityEditor.Scripting.Compilers.CompilerMessage;
using CompilerMessageType = UnityEditor.Scripting.Compilers.CompilerMessageType;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal static class BeeScriptCompilation
    {
        internal static string ExecutableExtension => Application.platform == RuntimePlatform.WindowsEditor ? ".exe" : "";
        private static string projectPath = Path.GetDirectoryName(Application.dataPath);

        public static void AddScriptCompilationData(BeeDriver beeDriver,
            EditorCompilation editorCompilation,
            ScriptAssembly[] assemblies,
            bool debug,
            string outputDirectory,
            BuildTarget buildTarget,
            bool buildingForEditor,
            string[] extraScriptingDefines = null)
        {
            // Need to call AssemblyDataFrom before calling CompilationPipeline.GetScriptAssemblies,
            // as that acts on the same ScriptAssemblies, and modifies them with different build settings.
            var cachedAssemblies = AssemblyDataFrom(assemblies);

            var movedFromExtractorPath = EditorApplication.applicationContentsPath + $"/Tools/ScriptUpdater/ApiUpdater.MovedFromExtractor.exe";
            var dotNetSdkRoslynPath = EditorApplication.applicationContentsPath + $"/DotNetSdkRoslyn";

            var localization = "en-US";
            if (LocalizationDatabase.currentEditorLanguage != SystemLanguage.English && EditorPrefs.GetBool("Editor.kEnableCompilerMessagesLocalization", false))
                localization = LocalizationDatabase.GetCulture(LocalizationDatabase.currentEditorLanguage);


            var assembliesToScanForTypeDB = new HashSet<string>();
            var searchPaths = new HashSet<string>(BuildPlayerDataGenerator.GetStaticSearchPaths(buildTarget));
            var options = EditorScriptCompilationOptions.BuildingIncludingTestAssemblies;
            if (buildingForEditor)
                options |= EditorScriptCompilationOptions.BuildingForEditor;
            foreach (var a in editorCompilation.GetAllScriptAssemblies(options, extraScriptingDefines))
            {
                if (!a.Flags.HasFlag(AssemblyFlags.EditorOnly))
                {
                    var path = a.FullPath.ToNPath();
                    assembliesToScanForTypeDB.Add(path.ToString());
                    searchPaths.Add(path.Parent.ToString());
                }
            }

            var precompileAssemblies = editorCompilation.PrecompiledAssemblyProvider.GetPrecompiledAssembliesDictionary(
                options,
                BuildPipeline.GetBuildTargetGroup(buildTarget),
                buildTarget,
                extraScriptingDefines);
            if (precompileAssemblies != null)
            {
                foreach (var a in precompileAssemblies)
                {
                    if (!a.Value.Flags.HasFlag(AssemblyFlags.EditorOnly))
                    {
                        var path = a.Value.Path.ToNPath();
                        assembliesToScanForTypeDB.Add(path.ToString());
                        searchPaths.Add(path.Parent.ToString());
                    }
                }
            }

            beeDriver.DataForBuildProgram.Add(new ScriptCompilationData
            {
                OutputDirectory = outputDirectory,
                DotnetRuntimePath = NetCoreProgram.DotNetRuntimePath.ToString(),
                DotnetRoslynPath = dotNetSdkRoslynPath,
                MovedFromExtractorPath = movedFromExtractorPath,
                Assemblies = cachedAssemblies,
                Debug = debug,
                BuildTarget = buildTarget.ToString(),
                Localization = localization,
                EnableDiagnostics = editorCompilation.EnableDiagnostics,
                BuildPlayerDataOutput = $"Library/BuildPlayerData/{(buildingForEditor ? "Editor" : "Player")}",
                ExtractRuntimeInitializeOnLoads = !buildingForEditor,
                AssembliesToScanForTypeDB = assembliesToScanForTypeDB.OrderBy(p => p).ToArray(),
                SearchPaths = searchPaths.OrderBy(p => p).ToArray()
            });
        }

        private static AssemblyData[] AssemblyDataFrom(ScriptAssembly[] assemblies)
        {
            Array.Sort(assemblies, (a1, a2) => string.Compare(a1.Filename, a2.Filename, StringComparison.Ordinal));
            return assemblies.Select((scriptAssembly, index) =>
            {
                using (new ProfilerMarker($"AssemblyDataFrom {scriptAssembly.Filename}").Auto())
                    return AssemblyDataFrom(scriptAssembly, assemblies, index);
            }).ToArray();
        }

        private static AssemblyData AssemblyDataFrom(ScriptAssembly a, ScriptAssembly[] allAssemblies, int index)
        {
            Array.Sort(a.Files, StringComparer.InvariantCulture);
            var references = a.ScriptAssemblyReferences.Select(r => Array.IndexOf(allAssemblies, r)).ToArray();
            Array.Sort(references);

            return new AssemblyData
            {
                Name = new NPath(a.Filename).FileNameWithoutExtension,
                SourceFiles = a.Files,
                Defines = a.Defines,
                PrebuiltReferences = a.References,
                References = references,
                AllowUnsafeCode = a.CompilerOptions.AllowUnsafeCode,
                RuleSet = a.CompilerOptions.RoslynAnalyzerRulesetPath,
                LanguageVersion = a.CompilerOptions.LanguageVersion,
                Analyzers = a.CompilerOptions.RoslynAnalyzerDllPaths,
                AdditionalFiles = a.CompilerOptions.RoslynAdditionalFilePaths,
                AnalyzerConfigPath = a.CompilerOptions.AnalyzerConfigPath,
                UseDeterministicCompilation = a.CompilerOptions.UseDeterministicCompilation,
                SuppressCompilerWarnings = (a.Flags & AssemblyFlags.SuppressCompilerWarnings) != 0,
                Asmdef = a.AsmDefPath,
                CustomCompilerOptions = a.CompilerOptions.AdditionalCompilerArguments,
                BclDirectories = MonoLibraryHelpers.GetSystemReferenceDirectories(a.CompilerOptions.ApiCompatibilityLevel),
                DebugIndex = index,
                SkipCodeGen = a.SkipCodeGen,
                Path = projectPath,
            };
        }

        private static CompilerMessage AsCompilerMessage(BeeDriverResult.Message message)
        {
            return new CompilerMessage
            {
                message = message.Text,
                type = message.Kind == BeeDriverResult.MessageKind.Error
                    ? CompilerMessageType.Error
                    : CompilerMessageType.Warning,
            };
        }

        /// <summary>
        /// the returned array of compiler messages corresponds to the input array of noderesult. Each node result can result in 0,1 or more compilermessages.
        /// We return them as an array of arrays, so on the caller side you're still able to map a compilermessage to the noderesult where it originated from,
        /// which we need when invoking per assembly compilation callbacks.
        /// </summary>
        public static CompilerMessage[][] ParseAllResultsIntoCompilerMessages(BeeDriverResult.Message[] beeDriverMessages, NodeResult[] nodeResults, EditorCompilation editorCompilation)
        {
            // If there's any messages from the bee driver, we add one additional array to the result which contains all of the driver messages converted and augmented like the nodes messages arrays.
            bool hasBeeDriverMessages = beeDriverMessages.Length > 0;
            var result = new CompilerMessage[nodeResults.Length + (hasBeeDriverMessages ? 1 : 0)][];

            int resultIndex = 0;
            if (hasBeeDriverMessages)
            {
                result[resultIndex] = beeDriverMessages.Select(AsCompilerMessage).ToArray();
                ++resultIndex;
            }
            for (int i = 0; i != nodeResults.Length; i++)
            {
                result[resultIndex] = ParseCompilerOutput(nodeResults[i]);
                ++resultIndex;
            }

            //To be more kind to performance issues in situations where there are thousands of compiler messages, we're going to assume
            //that after the first 10 compiler error messages, we get very little benefit from augmenting the rest with higher quality unity specific messaging.
            int totalErrors = 0;
            int nextResultToAugment = 0;
            while (totalErrors < 10 && nextResultToAugment < result.Length)
            {
                UnitySpecificCompilerMessages.AugmentMessagesInCompilationErrorsWithUnitySpecificAdvice(result[nextResultToAugment], editorCompilation);
                totalErrors += result[nextResultToAugment].Count(m => m.type == CompilerMessageType.Error);
                ++nextResultToAugment;
            }

            return result;
        }

        public static CompilerMessage[] ParseCompilerOutput(NodeResult nodeResult)
        {
            // TODO: future improvement opportunity: write a single parser that can parse warning, errors files from all tools that we use.
            if (nodeResult.annotation.StartsWith("CopyFiles"))
            {
                if (nodeResult.exitcode == 0)
                {
                    return Array.Empty<CompilerMessage>();
                }
                return new[]
                {
                    new CompilerMessage
                    {
                        file = nodeResult.outputfile,
                        message = $"{nodeResult.outputfile}: {nodeResult.stdout}",
                        type = CompilerMessageType.Error
                    }
                };
            }
            var parser = nodeResult.annotation.StartsWith("ILPostProcess")
                ? (CompilerOutputParserBase) new PostProcessorOutputParser()
                : (CompilerOutputParserBase) new MicrosoftCSharpCompilerOutputParser();

            return parser
                .Parse(
                (nodeResult.stdout ?? string.Empty).Split(new[] {'\r', '\n'},
                    StringSplitOptions.RemoveEmptyEntries),
                nodeResult.exitcode != 0).ToArray();
        }
    }
}
