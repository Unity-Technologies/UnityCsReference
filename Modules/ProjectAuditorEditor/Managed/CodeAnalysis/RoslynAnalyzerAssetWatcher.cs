// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    /// <summary>
    /// Maintains a list of known Roslyn Analyzer Dlls.
    /// Watches the AssetDatabase for changes to <c>RoslynAnalyzer</c>-labelled DLLs and rebuilds the
    /// <see cref="RoslynDiagnosticsLibrary"/> whenever one is added, removed, or modified.
    /// </summary>
    sealed partial class RoslynAnalyzerAssetWatcher : AssetPostprocessor
    {
        const string k_AnalyzerLabel = "RoslynAnalyzer";

        [NoAutoStaticsCleanup]
        static HashSet<string> s_KnownAnalyzerDllPaths = new HashSet<string>();
        [NoAutoStaticsCleanup]
        static bool s_Initialized;

        public static IReadOnlyCollection<string> AnalyzerDllPaths => s_KnownAnalyzerDllPaths;

        public static void Initialize()
        {
            if (s_Initialized)
                return;
            s_Initialized = true;

            s_KnownAnalyzerDllPaths.Clear();
            foreach (var guid in AssetDatabase.FindAssets("l:RoslynAnalyzer"))
                s_KnownAnalyzerDllPaths.Add(AssetDatabase.GUIDToAssetPath(guid));

            RoslynDiagnosticsLibrary.Reload();
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool changed = false;

            // Firstly, if any dlls were deleted, we will need to rebuild the diagnostic library.
            foreach (var asset in deletedAssets)
            {
                if (s_KnownAnalyzerDllPaths.Remove(asset))
                    changed = true;
            }
            foreach (var asset in movedFromAssetPaths)
            {
                if (s_KnownAnalyzerDllPaths.Remove(asset))
                    changed = true;
            }

            // Secondly, add any new dlls
            foreach (var asset in importedAssets)
            {
                if (IsAnalyzerDll(asset))
                {
                    s_KnownAnalyzerDllPaths.Add(asset);
                    changed = true;
                }
            }
            foreach (var asset in movedAssets)
            {
                if (IsAnalyzerDll(asset))
                {
                    s_KnownAnalyzerDllPaths.Add(asset);
                    changed = true;
                }
            }

            // Rebuild?
            if (changed)
                RoslynDiagnosticsLibrary.Reload();
        }

        static bool IsAnalyzerDll(string path) =>
            path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
            Array.IndexOf(AssetDatabase.GetLabels(AssetDatabase.GUIDFromAssetPath(path)), k_AnalyzerLabel) >= 0;
    }
}
