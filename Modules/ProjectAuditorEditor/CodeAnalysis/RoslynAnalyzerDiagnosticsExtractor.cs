// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEditor;
using UnityEditor.Scripting;

using Json = Unity.ProjectAuditor.Editor.Utils.Json;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    /// <summary>
    /// Extracts Roslyn diagnostic metadata from analyzer DLLs by running the DiagnosticsInspector tool
    /// (com.unity.project-auditor-rules/RoslynAnalyzers) through the editor's bundled .NET runtime.
    /// </summary>
    static class RoslynAnalyzerDiagnosticsExtractor
    {
        const string k_ToolRelativePath = "RoslynAnalyzers/ProjectAuditorRules.DiagnosticsInspector.dll";
        const string k_CommonAnalyzerRelativePath = "Unity.Analyzers/Unity.Analyzers.Common.dll"; // Load whitelisted analyzers from Unity's common set

#pragma warning disable CS0649 // assigned during JSON deserialization

        [Serializable]
        internal struct Diagnostic
        {
            public string id;
            public string title;
            public string messageFormat;
            public string description;
            public string category;
            public string defaultSeverity;
            public string helpLinkUri;
            public bool isEnabledByDefault;
            public string[] customTags;
        }

        [Serializable]
        struct DllResult
        {
            public string dll;
            public Diagnostic[] diagnostics;
            public string[] warnings;   // Some diagnostics may still be reported
            public string error;        // No diagnostics reported
        }

#pragma warning restore CS0649

        internal sealed class Result
        {
            public List<Diagnostic> Diagnostics = new List<Diagnostic>();
            public List<string> Warnings = new List<string>();
        }

        /// <summary>
        /// Resolves the full path to Unity's bundled "common" analyzer, or null if it isn't present.
        /// </summary>
        public static string ResolveUnityCommonAnalyzerPath()
        {
            var path = Path.Combine(EditorApplication.applicationBuildPipelinePath, k_CommonAnalyzerRelativePath);
            return File.Exists(path) ? Path.GetFullPath(path) : null;
        }

        /// <summary>
        /// Runs the tool over the given analyzer DLLs and returns the diagnostics.
        /// </summary>
        public static Result Extract(IReadOnlyCollection<string> analyzerDllPaths)
        {
            var dllPaths = AbsoluteDllPaths(analyzerDllPaths);
            if (dllPaths.Count == 0)
                return new Result();

            var json = RunTool(dllPaths);
            return Parse(json);
        }

        // Finds the Roslyn folder inside the bundled SDK ("DotNetSdk/sdk/<version>/Roslyn/bincore"), preferring
        // the highest SDK version present. Returns null when no usable Roslyn is found.
        static string ResolveRoslynDir(string dotNetSdkDir)
        {
            var sdkRoot = Path.Combine(dotNetSdkDir, "sdk");
            if (!Directory.Exists(sdkRoot))
                return null;

            var versionDirs = Directory.GetDirectories(sdkRoot);
            Array.Sort(versionDirs, CompareSdkVersionDirsDescending);
            foreach (var versionDir in versionDirs)
            {
                var bincore = Path.Combine(versionDir, "Roslyn", "bincore");
                if (File.Exists(Path.Combine(bincore, "Microsoft.CodeAnalysis.dll")))
                    return bincore;
            }

            return null;
        }

        static int CompareSdkVersionDirsDescending(string leftDir, string rightDir) =>
            Utility.CompareVersions(Path.GetFileName(rightDir), Path.GetFileName(leftDir));

        static List<string> AbsoluteDllPaths(IReadOnlyCollection<string> paths)
        {
            var normalized = new List<string>(paths.Count);
            if (paths == null)
                return normalized;

            foreach (var path in paths)
                normalized.Add(Path.GetFullPath(path));

            return normalized;
        }

        static string RunTool(IReadOnlyList<string> analyzerDllPaths)
        {
            // NetCoreProgram resolves the bundled dotnet muxer/runtime for us; just confirm it's present.
            if (!File.Exists(NetCoreProgram.GetDotNetMuxerPath()))
                throw new Exception($"DiagnosticsInspector could not find the DotNet Muxer Path");

            var toolDll = Path.GetFullPath(Path.Combine(ProjectAuditorRulesPackage.Path, k_ToolRelativePath));
            if (!File.Exists(toolDll))
                throw new Exception($"DiagnosticsInspector could not find {Path.GetFileName(k_ToolRelativePath)}");

            // NetCoreProgram launches the tool through the editor's bundled dotnet muxer with `exec "<dll>"`.
            using (var program = new NetCoreProgram(toolDll, string.Empty, startInfo =>
            {
                var roslynDir = ResolveRoslynDir(NetCoreProgram.GetDotNetRuntimePath().ToString());
                if (roslynDir == null)
                    throw new Exception($"DiagnosticsInspector could not find Roslyn directory");

                // Tells the tool where to load Roslyn from (the bundled SDK).
                startInfo.EnvironmentVariables["PAR_INSPECTOR_ROSLYN_DIR"] = roslynDir;
            }))
            {
                program.Start();

                using (var stdin = new StreamWriter(program.GetStandardInput()))
                {
                    foreach (var dll in analyzerDllPaths)
                        stdin.WriteLine(dll);
                }

                program.WaitForExit();

                if (program.ExitCode != 0)
                    throw new Exception($"DiagnosticsInspector exited with code {program.ExitCode}: {program.GetErrorOutputAsString()}");

                return program.GetStandardOutputAsString();
            }
        }

        static Result Parse(string json)
        {
            var result = new Result();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            var dllResults = Json.DeserializeArray<DllResult>(json);
            foreach (var dllResult in dllResults)
            {
                if (!string.IsNullOrEmpty(dllResult.error))
                {
                    result.Warnings.Add($"{Path.GetFileName(dllResult.dll)}: {dllResult.error}");
                    continue;
                }

                if (dllResult.warnings != null)
                {
                    foreach (var warning in dllResult.warnings)
                        result.Warnings.Add($"{Path.GetFileName(dllResult.dll)}: {warning}");
                }

                if (dllResult.diagnostics == null)
                    continue;

                foreach (var diagnostic in dllResult.diagnostics)
                {
                    if (!string.IsNullOrEmpty(diagnostic.id) && seen.Add(diagnostic.id))
                        result.Diagnostics.Add(diagnostic);
                }
            }

            return result;
        }
    }
}
