// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Bee.BeeDriver;
using Bee.BinLog;
using NiceIO;
using ScriptCompilationBuildProgram.Data;
using System.Text.RegularExpressions;
using Unity.Profiling;
using UnityEditor.Build.Player;
using UnityEditor.Compilation;
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

        private static Lazy<string> _RoslynPath = new(() => {
            NPath sdkPath = new NPath(EditorApplication.applicationScriptingPath).Combine("DotNetSdk/sdk");
            var dirs = sdkPath.Directories(recurse: false);
            var re = new Regex(@"^[1-9]+[0-9]*\.[0-9]+\.[0-9]+$");
            foreach (var dir in dirs)
                if (re.IsMatch(dir.FileName))
                    return dir.Combine("Roslyn/bincore").ToString(SlashMode.Native);

            throw new Exception($"Error: unable to locate {EditorApplication.applicationScriptingPath}/sdk/*/Roslyn/bincore directory");
        });
        private static string RoslynPath => _RoslynPath.Value;

        public static ScriptCompilationData ScriptCompilationDataFor(
            EditorCompilation editorCompilation,
            ScriptAssembly[] assemblies,
            bool debug,
            string outputDirectory,
            BuildTarget buildTarget,
            bool buildingForEditor,
            bool enableScriptUpdater,
            string[] extraScriptingDefines = null, ScriptAssemblySettings settings = null)
        {
            // Need to call AssemblyDataFrom before calling CompilationPipeline.GetScriptAssemblies,
            // as that acts on the same ScriptAssemblies, and modifies them with different build settings.
            var cachedAssemblies = AssemblyDataFrom(assemblies);

            AssemblyData[] codeGenAssemblies;
            using (new ProfilerMarker("GetScriptAssembliesForCodeGen").Auto())
            {
                codeGenAssemblies = buildingForEditor
                    ? null
                    : AssemblyDataFrom(CodeGenAssemblies(CompilationPipeline.GetScriptAssemblies(editorCompilation, AssembliesType.Editor, extraScriptingDefines)));
            }

            var movedFromExtractorPath = $"{EditorApplication.applicationBuildPipelinePath}/Compilation/ApiUpdater/ApiUpdater.MovedFromExtractor.dll";

            var localization = "en-US";
            if (LocalizationDatabase.currentEditorLanguage != SystemLanguage.English && EditorPrefs.GetBool("Editor.kEnableCompilerMessagesLocalization", false))
                localization = LocalizationDatabase.GetCulture(LocalizationDatabase.currentEditorLanguage);

            var assembliesToScanForTypeDB = new HashSet<string>();
            var unityAssembliesToScanForLifecycle = new HashSet<string>();
            var searchPaths = new HashSet<string>(BuildPlayerDataGenerator.GetStaticSearchPaths(buildTarget));
            EditorScriptCompilationOptions options;

            if (settings is null)
            {
                options = EditorScriptCompilationOptions.BuildingIncludingTestAssemblies;
                if (buildingForEditor)
                    options |= EditorScriptCompilationOptions.BuildingForEditor;
            }
            else
            {
                options = settings.CompilationOptions;
            }

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
                buildTarget,
                extraScriptingDefines,
                includeRedirectedAssemblies: false);
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

            foreach (var a in editorCompilation.GetUnityAssemblies())
            {
                if (!a.Flags.HasFlag(AssemblyFlags.EditorOnly) && !assembliesToScanForTypeDB.Contains(a.Path))
                {
                    var path = a.Path;
                    unityAssembliesToScanForLifecycle.Add(path);
                    searchPaths.Add(Path.GetDirectoryName(path));
                }
            }

            return new ScriptCompilationData()
            {
                OutputDirectory = outputDirectory,
                DotnetRuntimePath = NetCoreProgram.DotNetRuntimePath.ToString(),
                DotnetRoslynPath = RoslynPath,
                MovedFromExtractorPath = movedFromExtractorPath,
                Assemblies = cachedAssemblies,
                CodegenAssemblies = codeGenAssemblies,
                Debug = debug,
                BuildTarget = buildTarget.ToString(),
                Localization = localization,
                EnableDiagnostics = editorCompilation.EnableDiagnostics,
                BuildPlayerDataOutput = $"Library/BuildPlayerData/{(buildingForEditor ? "Editor" : "Player")}",
                ExtractRuntimeInitializeOnLoads = !buildingForEditor,
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                AssembliesToScanForTypeDB = assembliesToScanForTypeDB.OrderBy(p => p).ToArray(),
#pragma warning restore UA2001
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                SearchPaths = searchPaths.OrderBy(p => p).ToArray(),
#pragma warning restore UA2001
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                UnityAssembliesToScanForLifecycle = unityAssembliesToScanForLifecycle.ToArray(),
#pragma warning restore UA2001
                EmitInfoForScriptUpdater = enableScriptUpdater,
                TargetingPacks = new[]
                {
                    new TargetingPackData
                    {
                        Name = "Unity.BCLExtensions.Netstandard21",
                        Path = BCLExtensions.NetstandardTargetingPackDirectory()
                    }
                }
            };
        }

        private static ScriptAssembly[] CodeGenAssemblies(ScriptAssembly[] assemblies) =>
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            assemblies
#pragma warning restore UA2001
                .Where(assembly => UnityCodeGenHelpers.IsCodeGen(FileUtil.GetPathWithoutExtension(assembly.Filename)))
                .SelectMany(assembly => assembly.AllRecursiveScripAssemblyReferencesIncludingSelf())
                .Distinct()
                .OrderBy(a => a.Filename)
                .ToArray();

        private static AssemblyData[] AssemblyDataFrom(ScriptAssembly[] assemblies)
        {
            Array.Sort(assemblies, (a1, a2) => string.Compare(a1.Filename, a2.Filename, StringComparison.Ordinal));
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return assemblies.Select((scriptAssembly, index) =>
#pragma warning restore UA2001
            {
                using (new ProfilerMarker($"AssemblyDataFrom {scriptAssembly.Filename}").Auto())
                    return AssemblyDataFrom(scriptAssembly, assemblies, index);
            }).ToArray();
        }

        private static AssemblyData AssemblyDataFrom(ScriptAssembly a, ScriptAssembly[] allAssemblies, int index)
        {
            Array.Sort(a.Files, StringComparer.Ordinal);
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var references = a.ScriptAssemblyReferences.Select(r => Array.IndexOf(allAssemblies, r)).ToArray();
#pragma warning restore UA2001
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
        public static CompilerMessage[][] ParseAllNodeResultsIntoCompilerMessages(BeeDriverResult.Message[] beeDriverMessages, NodeFinishedMessage[] nodeResults, EditorCompilation editorCompilation)
        {
            // If there's any messages from the bee driver, we add one additional array to the result which contains all of the driver messages converted and augmented like the nodes messages arrays.
            bool hasBeeDriverMessages = beeDriverMessages.Length > 0;
            var result = new CompilerMessage[nodeResults.Length + (hasBeeDriverMessages ? 1 : 0)][];

            int resultIndex = 0;
            if (hasBeeDriverMessages)
            {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                result[resultIndex] = beeDriverMessages.Select(AsCompilerMessage).ToArray();
#pragma warning restore UA2001
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
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                totalErrors += result[nextResultToAugment].Count(m => m.type == CompilerMessageType.Error);
#pragma warning restore UA2001
                ++nextResultToAugment;
            }

            return result;
        }

        public static CompilerMessage[] ParseCompilerOutput(NodeFinishedMessage nodeResult)
        {
            // TODO: future improvement opportunity: write a single parser that can parse warning, errors files from all tools that we use.
            if (nodeResult.Node.Annotation.StartsWith("CopyFiles", StringComparison.Ordinal))
            {
                if (nodeResult.ExitCode == 0)
                {
                    return Array.Empty<CompilerMessage>();
                }
                return new[]
                {
                    new CompilerMessage
                    {
                        file = nodeResult.Node.OutputFile,
                        message = $"{nodeResult.Node.OutputFile}: {nodeResult.Output}",
                        type = CompilerMessageType.Error
                    }
                };
            }
            var parser = nodeResult.Node.Annotation.StartsWith("ILPostProcess", StringComparison.Ordinal)
                ? (CompilerOutputParserBase) new PostProcessorOutputParser()
                : (CompilerOutputParserBase) new MicrosoftCSharpCompilerOutputParser();

#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return parser
#pragma warning restore UA2001
                .Parse(
                (nodeResult.Output ?? string.Empty).Split(new[] {'\r', '\n'},
                    StringSplitOptions.RemoveEmptyEntries),
                nodeResult.ExitCode != 0).ToArray();
        }
    }
}
