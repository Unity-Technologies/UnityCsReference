// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using Bee.BeeDriver;
using NiceIO;
using ScriptCompilationBuildProgram.Data;
using Unity.Profiling;
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
        private static string CscPath => EditorApplication.applicationContentsPath + $"/Tools/Roslyn/csc{ExecutableExtension}";

        public static void AddScriptCompilationData(BeeDriver beeDriver,
            EditorCompilation editorCompilation,
            ScriptAssembly[] assemblies,
            bool debug,
            string outputDirectory,
            BuildTarget buildTarget, bool buildingForEditor)
        {
            // Need to call AssemblyDataFrom before calling CompilationPipeline.GetScriptAssemblies,
            // as that acts on the same ScriptAssemblies, and modifies them with different build settings.
            var cachedAssemblies = AssemblyDataFrom(assemblies);

            AssemblyData[] codeGenAssemblies;
            using (new ProfilerMarker("GetScriptAssembliesForCodeGen").Auto())
            {
                codeGenAssemblies = buildingForEditor
                    ? null
                    : AssemblyDataFrom(CodeGenAssemblies(CompilationPipeline.GetScriptAssemblies(editorCompilation, AssembliesType.Editor)));
            }

            var movedFromExtractorPath = EditorApplication.applicationContentsPath + $"/Tools/ScriptUpdater/ApiUpdater.MovedFromExtractor.exe";
            var netcorerunPath = EditorApplication.applicationContentsPath + $"/Tools/netcorerun/netcorerun{ExecutableExtension}";

            var localization = "en-US";
            if (LocalizationDatabase.currentEditorLanguage != SystemLanguage.English && EditorPrefs.GetBool("Editor.kEnableCompilerMessagesLocalization", false))
                localization = LocalizationDatabase.GetCulture(LocalizationDatabase.currentEditorLanguage);

            beeDriver.DataForBuildProgram.Add(new ScriptCompilationData
            {
                outputdirectory = outputDirectory,
                cscPath = CscPath,
                movedFromExtractorPath = movedFromExtractorPath,
                netcorerunPath = netcorerunPath,
                assemblies = cachedAssemblies,
                codegenAssemblies = codeGenAssemblies,
                debug = debug,
                buildTarget = buildTarget.ToString(),
                localization = localization,
                packages = PackageManager.PackageInfo.GetAll().Select(p =>
                {
                    NPath resolvedPath = new NPath(p.resolvedPath);
                    if (resolvedPath.IsChildOf(editorCompilation.projectDirectory))
                        resolvedPath = resolvedPath.RelativeTo(editorCompilation.projectDirectory);

                    return new ScriptCompilationBuildProgram.Data.PackageInfo()
                    {
                        name = p.name,
                        resolvedpath = resolvedPath.ToString(),
                        immutable = (p.source != PackageSource.Embedded && p.source != PackageSource.Local)
                    };
                }).ToArray()
            });
        }

        private static ScriptAssembly[] CodeGenAssemblies(ScriptAssembly[] assemblies) =>
            assemblies
                .Where(assembly => UnityCodeGenHelpers.IsCodeGen(FileUtil.GetPathWithoutExtension(assembly.Filename)))
                .SelectMany(assembly => assembly.AllRecursiveScripAssemblyReferencesIncludingSelf())
                .Distinct()
                .OrderBy(a => a.Filename)
                .ToArray();

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
                name = new NPath(a.Filename).FileNameWithoutExtension,
                sourceFiles = a.Files,
                defines = a.Defines,
                prebuiltReferences = a.References,
                references = references,
                allowUnsafeCode = a.CompilerOptions.AllowUnsafeCode,
                ruleSet = a.CompilerOptions.RoslynAnalyzerRulesetPath,
                languageVersion = a.CompilerOptions.LanguageVersion,
                analyzers = a.CompilerOptions.RoslynAnalyzerDllPaths,
                useDeterministicCompilation = a.CompilerOptions.UseDeterministicCompilation,
                asmdef = a.AsmDefPath,
                customCompilerOptions = a.CompilerOptions.AdditionalCompilerArguments,
                bclDirectories = MonoLibraryHelpers.GetSystemReferenceDirectories(a.CompilerOptions.ApiCompatibilityLevel),
                debugIndex = index
            };
        }

        /// <summary>
        /// the returned array of compiler messages corresponds to the input array of noderesult. Each node result can result in 0,1 or more compilermessages.
        /// We return them as an array of arrays, so on the caller side you're still able to map a compilermessage to the noderesult where it originated from,
        /// which we need when invoking per assembly compilation callbacks.
        /// </summary>
        public static CompilerMessage[][] ParseAllNodeResultsIntoCompilerMessages(NodeResult[] nodeResults, EditorCompilation editorCompilation)
        {
            var result = new CompilerMessage[nodeResults.Length][];

            int totalErrors = 0;
            for (int i = 0; i != nodeResults.Length; i++)
            {
                var compilerMessages = ParseCompilerOutput(nodeResults[i]);

                //To be more kind to performance issues in situations where there are thousands of compiler messages, we're going to assume
                //that after the first 10 compiler error messages, we get very little benefit from augmenting the rest with higher quality unity specific messaging.
                if (totalErrors < 10)
                {
                    UnitySpecificCompilerMessages.AugmentMessagesInCompilationErrorsWithUnitySpecificAdvice(compilerMessages, editorCompilation);
                    totalErrors += compilerMessages.Count(m => m.type == CompilerMessageType.Error);
                }

                result[i] = compilerMessages;
            }

            return result;
        }

        public static CompilerMessage[] ParseCompilerOutput(NodeResult nodeResult)
        {
            // TODO: future improvement opportunity: write a single parser that can parse warning, errors files from all tools that we use.
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
