// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal static class RoslynAnalyzers
    {
        private static readonly string[] Unset = null;
        private static readonly string[] CyclicDependencies = {};

        private static string[] SetAnalyzers(ScriptAssembly scriptAssembly, IEnumerable<(string scriptAssemblyFileName, string analyzerDll)> allAnalyzers, bool scanPrecompiledReferences)
        {
            if (scriptAssembly.CompilerOptions.RoslynAnalyzerDllPaths == Unset)
            {
                // If this is a cyclic chain we want to detect that and do two iterations
                // Doing two iterations ensures that all participants in the chain will see all the analyzers of all members involved in the chain.
                scriptAssembly.CompilerOptions.RoslynAnalyzerDllPaths = CyclicDependencies;
            }
            else if (scriptAssembly.CompilerOptions.RoslynAnalyzerDllPaths == CyclicDependencies)
            {
                // On second iteration return an empty array (this will be replaced be actual content of the cyclic chain)
                scriptAssembly.CompilerOptions.RoslynAnalyzerDllPaths = Array.Empty<string>();
            }
            else
            {
                // Analyzers for this ScriptAssembly has already been setup
                return scriptAssembly.CompilerOptions.RoslynAnalyzerDllPaths;
            }

            scriptAssembly.CompilerOptions.RoslynAnalyzerDllPaths =
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                scriptAssembly.ScriptAssemblyReferences
#pragma warning restore RS0030
                    .SelectMany(sa => SetAnalyzers(sa, allAnalyzers, scanPrecompiledReferences))
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    .Concat(allAnalyzers
#pragma warning restore RS0030
                        .Where(a => a.scriptAssemblyFileName == null ||
                                    a.scriptAssemblyFileName == scriptAssembly.Filename ||
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                                    scanPrecompiledReferences && scriptAssembly.References.Select(Path.GetFileName).Contains(a.scriptAssemblyFileName))
#pragma warning restore RS0030
                        .Select(a => a.analyzerDll))
                    .Distinct()
                    .ToArray();

            if (scriptAssembly.CompilerOptions.RoslynAnalyzerDllPaths.Length > 0)
            {
                if(scriptAssembly.TargetAssemblyType == TargetAssemblyType.Predefined)
                {
                    var originPath = Path.ChangeExtension(scriptAssembly.Filename, null);
                    scriptAssembly.CompilerOptions.RoslynAnalyzerRulesetPath = RuleSetFileCache.GetRuleSetFilePathInRootFolder(originPath);
                    scriptAssembly.CompilerOptions.AnalyzerConfigPath = RoslynAnalyzerConfigFiles.GetAnalyzerConfigRootFolder(originPath);
                }
                else
                {
                    scriptAssembly.CompilerOptions.RoslynAnalyzerRulesetPath = RuleSetFileCache.GetPathForAssembly(scriptAssembly.OriginPath);
                    scriptAssembly.CompilerOptions.AnalyzerConfigPath = RoslynAnalyzerConfigFiles.GetAnalyzerConfigForAssembly(scriptAssembly.OriginPath);
                }
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                scriptAssembly.CompilerOptions.RoslynAdditionalFilePaths = scriptAssembly.CompilerOptions.RoslynAnalyzerDllPaths
#pragma warning restore RS0030
                    .SelectMany(a=>RoslynAdditionalFiles.GetAnalyzerAdditionalFilesForTargetAssembly(a, scriptAssembly.OriginPath))
                    .Distinct()
                    .ToArray();
            }

            return scriptAssembly.CompilerOptions.RoslynAnalyzerDllPaths;
        }

        internal static void SetAnalyzers(ScriptAssembly[] scriptAssemblies, TargetAssembly[] potentialAnalyzerOwners, string[] analyzerDlls, bool scanPrecompiledReferences)
        {
            // Figure out what assemblies own each analyzer
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var analyzerAssemblies = analyzerDlls.Select(analyzerDll =>
#pragma warning restore RS0030
            {
#pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var potentialAnalyzerOwner = potentialAnalyzerOwners
#pragma warning restore RS0030
                    .Where(targetAssembly => targetAssembly.PathFilter(analyzerDll) > 0)
                    .OrderBy(targetAssembly => targetAssembly.PathFilter(analyzerDll))
                    .LastOrDefault();

                return (potentialOwnerOfAnalyzer: potentialAnalyzerOwner?.Filename, dll: analyzerDll);

            }).ToArray();

            // Null out all RoslynAnalyzerDllPaths to indicate they need to be set
            foreach (var scriptAssembly in scriptAssemblies)
                scriptAssembly.CompilerOptions.RoslynAnalyzerDllPaths = Unset;

            foreach (var scriptAssembly in scriptAssemblies)
                SetAnalyzers(scriptAssembly, analyzerAssemblies, scanPrecompiledReferences);
        }
    }
}
