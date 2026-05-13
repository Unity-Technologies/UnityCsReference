// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// Main service for build analysis functionality
    /// </summary>
    internal class BuildAnalysisService
    {
        private readonly IBuildEnumerator m_Enumerator;
        private readonly IBuildAnalyzer m_Analyzer;
        private readonly IBuildAnalysisFileSystem m_FileSystem;
        private readonly IBuildAnalysisProgressReporter m_ProgressReporter;
        private readonly IBuildHistoryProvider m_BuildHistory;
        private readonly LRUCache<GUID, BuildAnalysis> m_Cache;

        public BuildAnalysisService(
            IBuildEnumerator enumerator,
            IBuildAnalyzer analyzer,
            IBuildAnalysisFileSystem fileSystem,
            IBuildAnalysisProgressReporter progressReporter,
            IBuildHistoryProvider buildHistory)
        {
            m_Enumerator = enumerator ?? throw new ArgumentNullException(nameof(enumerator));
            m_Analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            m_FileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            m_ProgressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
            m_BuildHistory = buildHistory ?? throw new ArgumentNullException(nameof(buildHistory));
            m_Cache = new LRUCache<GUID, BuildAnalysis>(20);
        }

        /// <summary>
        /// Refresh the build history state from disk, then clear the cache.
        /// Call this before GetBuilds when an explicit user-initiated refresh is needed.
        /// </summary>
        public void Refresh()
        {
            m_BuildHistory.Refresh();
            ClearCache();
        }

        /// <summary>
        /// Get all available builds
        /// </summary>
        public BuildEntry[] GetBuilds()
        {
            try
            {
                return m_Enumerator.GetBuilds();
            }
            catch (Exception e)
            {
                Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to get builds: {e.Message}");
                return Array.Empty<BuildEntry>();
            }
        }

        /// <summary>
        /// Try to get a specific build by session GUID
        /// </summary>
        public bool TryGetBuild(GUID buildSessionGUID, out BuildEntry entry)
        {
            return m_Enumerator.TryGetBuild(buildSessionGUID, out entry);
        }

        /// <summary>
        /// Clear all cached data
        /// </summary>
        public void ClearCache()
        {
            m_Cache.Clear();
        }

        /// <summary>
        /// Delete a single build by its session GUID.
        /// </summary>
        public void DeleteBuild(GUID buildSessionGUID)
        {
            if (buildSessionGUID.Empty())
                throw new ArgumentException("BuildSessionGUID is empty.", nameof(buildSessionGUID));

            var deleted = m_BuildHistory.DeleteHistory(new[] { buildSessionGUID });
            if (deleted == 0)
                throw new ArgumentException($"No build found for '{buildSessionGUID}'.", nameof(buildSessionGUID));

            m_Cache.Remove(buildSessionGUID);
        }

        /// <summary>
        /// Delete all builds from build history.
        /// </summary>
        public void DeleteAllBuilds()
        {
            m_BuildHistory.DeleteAllHistory();
            ClearCache();
        }

        /// <summary>
        /// Get analysis for a specific build
        /// </summary>
        public BuildAnalysis GetBuildAnalysis(GUID buildSessionGUID)
        {
            if (buildSessionGUID.Empty())
                throw new ArgumentException("BuildSessionGUID is empty.", nameof(buildSessionGUID));

            var cachedAnalysis = m_Cache.Get(buildSessionGUID);
            if (cachedAnalysis != null)
                return cachedAnalysis;

            if (!m_Enumerator.TryGetBuild(buildSessionGUID, out var entry))
            {
                Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} No build found for BuildSessionGUID '{buildSessionGUID}'.");
                return null;
            }

            try
            {
                BuildAnalysis analysis;
                if (TryGetBuildAnalysisPath(buildSessionGUID, out var analysisPath))
                {
                    analysis = LoadBuildAnalysisFromDisk(analysisPath);
                }
                else
                {
                    m_ProgressReporter.Show("Build Analysis", $"Generating analysis for '{entry.BuildName}'...", 0.5f);
                    try
                    {
                        analysis = m_Analyzer.Generate(entry);
                    }
                    finally
                    {
                        m_ProgressReporter.Clear();
                    }
                }

                m_Cache.Put(buildSessionGUID, analysis);
                return analysis;
            }
            catch (Exception e)
            {
                Debug.LogError($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Failed to get analysis for '{buildSessionGUID}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Force regeneration of BuildAnalysis.json for the given build and update cache.
        /// </summary>
        public BuildAnalysis RegenerateBuildAnalysis(GUID buildSessionGUID)
        {
            if (buildSessionGUID.Empty())
                throw new ArgumentException("BuildSessionGUID is empty.", nameof(buildSessionGUID));

            if (!m_Enumerator.TryGetBuild(buildSessionGUID, out var entry))
                throw new ArgumentException($"No build found for BuildSessionGUID '{buildSessionGUID}'.", nameof(buildSessionGUID));

            m_ProgressReporter.Show("Build Analysis", $"Regenerating analysis for '{entry.BuildName}'...", 0.5f);
            try
            {
                var analysis = m_Analyzer.Generate(entry);
                m_Cache.Put(buildSessionGUID, analysis);
                return analysis;
            }
            finally
            {
                m_ProgressReporter.Clear();
            }
        }

        /// <summary>
        /// Check if analysis is available for a build
        /// </summary>
        public bool HasBuildAnalysis(GUID buildSessionGUID)
        {
            if (buildSessionGUID.Empty())
                return false;

            if (m_Cache.Contains(buildSessionGUID))
                return true;
            if (!m_Enumerator.TryGetBuild(buildSessionGUID, out _))
                return false;

            return TryGetBuildAnalysisPath(buildSessionGUID, out _);
        }

        private bool TryGetBuildAnalysisPath(GUID buildSessionGUID, out string path)
        {
            return m_BuildHistory.TryGetFilePath(buildSessionGUID, BuildAnalysisConstants.k_BuildAnalysisFileName, out path);
        }

        private BuildAnalysis LoadBuildAnalysisFromDisk(string analysisPath)
        {
            var json = m_FileSystem.ReadAllText(analysisPath);
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException($"Build analysis file is empty: '{analysisPath}'.");

            var analysis = JsonUtility.FromJson<BuildAnalysis>(json);
            NormalizeBuildAnalysis(analysis, analysisPath);
            return analysis;
        }

        private static void NormalizeBuildAnalysis(BuildAnalysis analysis, string path)
        {
            if (analysis == null)
                throw new InvalidDataException($"Build analysis could not be parsed: '{path}'.");
            if (analysis.Summary == null)
                throw new InvalidDataException($"Build analysis summary is missing: '{path}'.");
            if (analysis.Version <= 0)
                throw new InvalidDataException($"Build analysis has invalid Version '{analysis.Version}' in '{path}'.");

            if (analysis.Tables == null)
                analysis.Tables = new BuildAnalysisTables();
            analysis.Tables.Steps ??= Array.Empty<BuildAnalysisStep>();

            analysis.Messages ??= Array.Empty<BuildAnalysisMessage>();
            analysis.Summary.BuildOptions ??= Array.Empty<string>();
            analysis.Summary.BuildContentOptions ??= Array.Empty<string>();

            if (analysis.Computed == null)
                analysis.Computed = new BuildAnalysisComputed();
        }
    }

    internal interface IBuildAnalysisProgressReporter
    {
        void Show(string title, string info, float progress);
        void Clear();
    }

    internal sealed class BuildAnalysisProgressReporter : IBuildAnalysisProgressReporter
    {
        public void Show(string title, string info, float progress)
        {
            EditorUtility.DisplayProgressBar(title, info, progress);
        }

        public void Clear()
        {
            EditorUtility.ClearProgressBar();
        }
    }
}
