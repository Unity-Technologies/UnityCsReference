// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.UI.Framework;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.Scripting.ScriptCompilation.MsBuild;
using UnityEngine;

using Diagnostic = Unity.ProjectAuditor.Editor.CodeAnalysis.RoslynAnalyzerDiagnosticsExtractor.Diagnostic;
using Json = Unity.ProjectAuditor.Editor.Utils.Json;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Unity.ProjectAuditor.Editor.CodeAnalysis
{
    /// <summary>
    /// Holds Roslyn analyzer diagnostic metadata keyed by diagnostic code (e.g. "PAR0001").
    /// </summary>
    static partial class RoslynDiagnosticsLibrary
    {
        // Codes to surface from Unity's analyzers, with optional metadata such as a recommendation.
        const string k_UnityFileName = "UnityDiagnostics.json";

        // Older rules packages list bare codes instead of objects.
        const string k_UnityDiagnosticsObjectFormatVersion = "3.2.0";

        sealed class LoadResult
        {
            public Dictionary<string, Descriptor> Diagnostics;
            public List<string> Warnings;
        }

#pragma warning disable CS0649
        [Serializable]
        internal sealed class UnityDiagnostic
        {
            public string id;
            public string recommendation;
        }
#pragma warning restore CS0649

        [AutoStaticsCleanup]
        internal static Dictionary<string, UnityDiagnostic> s_UnityDiagnostics;

        [NoAutoStaticsCleanup] // Lazy cache; survives code reload (the underlying analyzers don't change during one)
        static Dictionary<string, Descriptor> s_Diagnostics;

        [AutoStaticsCleanup]
        static bool s_HasRegisterDiagnosticsCallback = false;

        [NoAutoStaticsCleanup]
        static readonly object s_LoadLock = new object();

        [AutoStaticsCleanup]
        static volatile bool s_Loading;

        // An analyzer changed while a load was in flight, so its result is stale and must be rebuilt.
        [AutoStaticsCleanup]
        static volatile bool s_ReloadRequested;

        // A finished load waiting for RegisterDiagnostics to apply it on the main thread.
        [AutoStaticsCleanup]
        static volatile LoadResult s_PendingResult;

        static void StartLoad(bool force)
        {
            if (force || s_UnityDiagnostics == null)
                s_UnityDiagnostics = ReadUnityDiagnostics();

            if (!MsBuildCompilationInterface.IsEnabled())
            {
                s_ReloadRequested = false;
                s_Diagnostics ??= new Dictionary<string, Descriptor>();
                return;
            }

            lock (s_LoadLock)
            {
                if (s_Loading)
                {
                    // Fold the request into the running load: RegisterDiagnostics chains a fresh one once
                    // it finishes, because its result was built from the now-outdated analyzer list.
                    s_ReloadRequested |= force;
                    return;
                }

                if (!force && (s_Diagnostics != null || s_PendingResult != null))
                    return;

                s_Loading = true;
                s_ReloadRequested = false;
                s_PendingResult = null;
            }

            // RegisterDiagnostics applies the result on the main thread once the load completes.
            if (!s_HasRegisterDiagnosticsCallback)
            {
                EditorApplication.update += RegisterDiagnostics;
                s_HasRegisterDiagnosticsCallback = true;
            }

            // Call all Unity APIs before starting the loading thread.
            var analyzerDlls = new List<string>(RoslynAnalyzerAssetWatcher.AnalyzerDllPaths);
            var unityAnalyzerPaths = RoslynAnalyzerDiagnosticsExtractor.ResolveUnityAnalyzerPaths();
            var unityDiagnostics = s_UnityDiagnostics;

            var loadThread = new Thread(() => Load(analyzerDlls, unityAnalyzerPaths, unityDiagnostics))
            {
                Name = "Diagnostics Load",
                Priority = ThreadPriority.BelowNormal,
                IsBackground = true
            };
            loadThread.Start();
        }

        // Build the Diagnostic dictionary and publish it for the main thread to pick up in RegisterDiagnostics.
        static void Load(IReadOnlyCollection<string> analyzerDllPaths, List<string> unityAnalyzerPaths, Dictionary<string, UnityDiagnostic> unityDiagnostics)
        {
            var result = new LoadResult { Warnings = new List<string>() };
            try
            {
                result.Diagnostics = Build(analyzerDllPaths, unityAnalyzerPaths, unityDiagnostics, result.Warnings);
            }
            catch (Exception e)
            {
                result.Diagnostics = new Dictionary<string, Descriptor>();
                result.Warnings.Add($"Failed to load analyzer diagnostics: {e}");
            }
            finally
            {
                lock (s_LoadLock)
                {
                    s_PendingResult = result;
                    s_Loading = false;
                }
            }
        }

        // Applies a completed load to the descriptor library on the main thread, and starts a follow-up load
        // if the analyzers changed while the last one was running. Runs from EditorApplication.update.
        static void RegisterDiagnostics()
        {
            if (s_Loading || (s_PendingResult == null && !s_ReloadRequested))
                return;

            LoadResult result;
            bool reloadRequested;
            lock (s_LoadLock)
            {
                if (s_Loading)
                    return;

                result = s_PendingResult;
                s_PendingResult = null;
                reloadRequested = s_ReloadRequested;
            }

            // An analyzer changed while we were loading: drop the stale result and rebuild. The previously
            // registered descriptors stay in place until the rebuild publishes its replacements.
            if (reloadRequested)
            {
                StartLoad(force: true);
                return;
            }

            if (result == null)
                return;

            var previous = s_Diagnostics;
            s_Diagnostics = result.Diagnostics;

            // Remove all the old descriptors that came from Roslyn, before adding the new ones.
            if (previous != null)
            {
                foreach (var previousId in previous.Keys)
                {
                    if (!s_Diagnostics.ContainsKey(previousId))
                        DescriptorLibrary.UnregisterDescriptor(previousId);
                }
            }

            // Add new descriptors.
            foreach (var diagnostic in s_Diagnostics)
                DescriptorLibrary.RegisterDescriptor(diagnostic.Key, diagnostic.Value);

            LogWarnings(result.Warnings);
        }

        internal static Descriptor ToDescriptor(Diagnostic diagnostic)
        {
            Enum.TryParse(diagnostic.category, out Areas area);
            var isCodeQualityRule = diagnostic.id.StartsWith("CA", StringComparison.Ordinal);

            // Map some areas used in our own analyzers, to areas Project Auditor understands
            if (area == Areas.None)
            {
                if (diagnostic.category == "Optimizations")
                    area = Areas.CPU;
                else if (diagnostic.category == "CodeReloadSafety")
                    area = Areas.IterationTime | Areas.MigrationToCoreCLR;
                else if (diagnostic.category == "BinaryFormatter")
                    area = Areas.MigrationToCoreCLR;
                else if (isCodeQualityRule)
                    area = Areas.Quality;
            }

            var messageParts = diagnostic.messageFormat.Split('\n');

            return new Descriptor(
                diagnostic.id,
                diagnostic.title,
                area,
                messageParts[0],
                (messageParts.Length > 1) ? messageParts[1] : ""
            )
            {
                MessageFormat = messageParts[0].Contains("{0}") ? messageParts[0] : string.Empty,
                DocumentationUrl = diagnostic.helpLinkUri,
                DefaultSeverity = isCodeQualityRule ? Severity.Minor : Severity.Moderate
            };
        }

        /// <summary>
        /// Start a background load if the diagnostics aren't loaded (or loading) yet.
        /// </summary>
        public static void EnsureLoaded()
        {
            StartLoad(force: false);
        }

        /// <summary>
        /// Applies a completed background load if one is waiting, then reports whether the diagnostics are
        /// ready to read from <see cref="Diagnostics"/>. Main thread only.
        /// </summary>
        public static bool PollLoading()
        {
            RegisterDiagnostics();
            return !s_Loading && s_Diagnostics != null;
        }

        /// <summary>
        /// A snapshot of the currently loaded diagnostics. Null until the first load completes, and the previous
        /// (stale but usable) set while a rebuild is in flight. Call <see cref="EnsureLoaded"/> first to start the
        /// load, and <see cref="PollLoading"/> to find out when this is up to date.
        /// </summary>
        public static IReadOnlyCollection<Descriptor> Diagnostics => s_Diagnostics?.Values;

        /// <summary>
        /// Rebuilds the diagnostics after an analyzer DLL is added, removed, or modified. Main thread only.
        /// </summary>
        public static void Reload()
        {
            s_PendingResult = null; // Any result built before this point was built from the outdated analyzer list.
            StartLoad(force: true);
        }

        // Runs the inspector over the labelled analyzers and Unity's analyzers.
        static Dictionary<string, Descriptor> Build(
            IReadOnlyCollection<string> analyzerDllPaths,
            List<string> unityAnalyzerPaths,
            Dictionary<string, UnityDiagnostic> unityDiagnostics,
            List<string> warnings)
        {
            var diagnostics = new Dictionary<string, Descriptor>();

            // Every RoslynAnalyzer-labelled DLL.
            if (analyzerDllPaths != null && analyzerDllPaths.Count > 0)
            {
                try
                {
                    var result = RoslynAnalyzerDiagnosticsExtractor.Extract(analyzerDllPaths);
                    warnings.AddRange(result.Warnings);
                    foreach (var d in result.Diagnostics)
                        diagnostics.Add(d.id, ToDescriptor(d));
                }
                catch (Exception e)
                {
                    warnings.Add($"Failed to introspect labelled analyzer DLLs: {e}");
                }
            }

            // Unity's analyzers.
            if (unityAnalyzerPaths?.Count > 0)
            {
                try
                {
                    if (unityDiagnostics.Count > 0)
                    {
                        var result = RoslynAnalyzerDiagnosticsExtractor.Extract(unityAnalyzerPaths);
                        warnings.AddRange(result.Warnings);
                        foreach (var d in result.Diagnostics)
                        {
                            if (!unityDiagnostics.TryGetValue(d.id, out var unityDiagnostic))
                                continue;

                            var descriptor = ToDescriptor(d);
                            ApplyMetadata(descriptor, unityDiagnostic);
                            diagnostics.Add(d.id, descriptor);
                        }
                    }
                }
                catch (Exception e)
                {
                    warnings.Add($"Failed to introspect Unity's analyzers: {e}");
                }
            }

            return diagnostics;
        }

        /// <summary>
        /// Applies the metadata from the rules package, such as a recommendation, to a Unity analyzer descriptor. Main thread only.
        /// </summary>
        public static void ApplyUnityMetadata(Descriptor descriptor)
        {
            if (s_UnityDiagnostics.TryGetValue(descriptor.Id, out var unityDiagnostic))
                ApplyMetadata(descriptor, unityDiagnostic);
        }

        static void ApplyMetadata(Descriptor descriptor, UnityDiagnostic unityDiagnostic)
        {
            if (!string.IsNullOrEmpty(unityDiagnostic.recommendation))
                descriptor.Recommendation = unityDiagnostic.recommendation;
        }

        static Dictionary<string, UnityDiagnostic> ReadUnityDiagnostics()
        {
            var path = Path.GetFullPath(Path.Combine(ProjectAuditor.s_RulesDataPath, k_UnityFileName));
            return ReadUnityDiagnostics(path, ProjectAuditorRulesPackage.Version);
        }

        internal static Dictionary<string, UnityDiagnostic> ReadUnityDiagnostics(string path, string rulesPackageVersion)
        {
            // Older rules packages don't have the file.
            if (!File.Exists(path))
                return new Dictionary<string, UnityDiagnostic>();

            try
            {
                return ParseUnityDiagnostics(File.ReadAllText(path), UsesObjectFormat(rulesPackageVersion));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Project Auditor: Failed to read {path}: {e.Message}");
                return new Dictionary<string, UnityDiagnostic>();
            }
        }

        internal static bool UsesObjectFormat(string rulesPackageVersion)
        {
            return Utility.CompareVersions(rulesPackageVersion, k_UnityDiagnosticsObjectFormatVersion) >= 0;
        }

        internal static Dictionary<string, UnityDiagnostic> ParseUnityDiagnostics(string json, bool objectFormat)
        {
            var unityDiagnostics = new Dictionary<string, UnityDiagnostic>(StringComparer.Ordinal);

            if (!objectFormat)
            {
                foreach (var code in Json.DeserializeArray<string>(json))
                {
                    if (!string.IsNullOrEmpty(code))
                        unityDiagnostics[code] = new UnityDiagnostic { id = code };
                }

                return unityDiagnostics;
            }

            foreach (var unityDiagnostic in Json.DeserializeArray<UnityDiagnostic>(json))
            {
                if (!string.IsNullOrEmpty(unityDiagnostic?.id))
                    unityDiagnostics[unityDiagnostic.id] = unityDiagnostic;
            }

            return unityDiagnostics;
        }

        static void LogWarnings(List<string> warnings)
        {
            foreach (var warning in warnings)
                Debug.LogWarning($"Project Auditor: {warning}");
        }
    }
}
