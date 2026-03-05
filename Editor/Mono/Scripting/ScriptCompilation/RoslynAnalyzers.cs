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
#pragma warning disable UA2003 // The way this is used means it must be an explicit empty array, not Array.Empty<string>()
        private static readonly string[] CyclicDependencies = {};
#pragma warning restore UA2003

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
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                scriptAssembly.ScriptAssemblyReferences
#pragma warning restore UA2001
                    .SelectMany(sa => SetAnalyzers(sa, allAnalyzers, scanPrecompiledReferences))
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    .Concat(allAnalyzers
#pragma warning restore UA2001
                        .Where(a => a.scriptAssemblyFileName == null ||
                                    a.scriptAssemblyFileName == scriptAssembly.Filename ||
                                    scanPrecompiledReferences && Array.Exists(scriptAssembly.References, b => Path.GetFileName(b) == a.scriptAssemblyFileName))
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
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                scriptAssembly.CompilerOptions.RoslynAdditionalFilePaths = scriptAssembly.CompilerOptions.RoslynAnalyzerDllPaths
#pragma warning restore UA2001
                    .SelectMany(a=>RoslynAdditionalFiles.GetAnalyzerAdditionalFilesForTargetAssembly(a, scriptAssembly.OriginPath))
                    .Distinct()
                    .ToArray();
            }

            return scriptAssembly.CompilerOptions.RoslynAnalyzerDllPaths;
        }

        internal static void SetAnalyzers(ScriptAssembly[] scriptAssemblies, TargetAssembly[] potentialAnalyzerOwners, string[] analyzerDlls, bool scanPrecompiledReferences)
        {
            // Figure out what assemblies own each analyzer
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var analyzerAssemblies = analyzerDlls.Select(analyzerDll =>
#pragma warning restore UA2001
            {
#pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                var potentialAnalyzerOwner = potentialAnalyzerOwners
#pragma warning restore UA2001
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
