// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Build
{
    /// <summary>
    /// Internal state management for BuildHistory.
    /// Handles loading, caching, and querying BuildReportSummary.json files.
    /// </summary>
    internal class BuildHistoryState
    {
        /// <summary>
        /// Represents an entry in the build history cache.
        /// </summary>
        private struct BuildEntry
        {
            /// <summary>
            /// The absolute path to the build metadata directory (not relative to BuildHistoryDirectory).
            /// Example: "C:/UnityProject/Library/BuildHistory/a1b2c3d4-..."
            /// </summary>
            public string MetadataPath;

            /// <summary>
            /// The timestamp when the build started (parsed from BuildReportSummary.BuildStartedAt).
            /// </summary>
            public DateTime BuildStartTime;

            /// <summary>
            /// The cached BuildReportSummary.json content.
            /// </summary>
            public BuildReportSummary Summary;
        }

        private static BuildHistoryState s_Instance;

        public static BuildHistoryState Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new BuildHistoryState();
                return s_Instance;
            }
        }

        // Cached state
        private Dictionary<GUID, BuildEntry> m_Builds; // Build session GUID to BuildEntry
        private List<GUID> m_BuildOrder; // Build session GUIDs, sorted by build start type (most recent first)
        private int m_Revision; // Incremented any time the build history state changes
        private string m_CachedRootDirectory;
        private bool m_IsDirty;
        private bool m_IsLoaded;

        /// <summary>
        /// Private constructor to enforce singleton pattern.
        /// </summary>
        private BuildHistoryState()
        {
            m_Builds = new Dictionary<GUID, BuildEntry>();
            m_BuildOrder = new List<GUID>();
            m_Revision = 0;
            m_IsLoaded = false;
            m_IsDirty = true;
            m_CachedRootDirectory = null;
        }

        // ==================== Public Query API ====================

        // Gets the total number of builds in the build history.
        public int GetBuildCount()
        {
            EnsureCacheLoaded();
            return m_BuildOrder.Count;
        }

        // Returns the session GUIDs for all builds in the build history.
        public GUID[] GetAllBuilds()
        {
            EnsureCacheLoaded();
            return m_BuildOrder.ToArray();
        }

        // Attempts to get the GUID of the most recent build.
        public bool TryGetLatestBuild(out GUID latestBuildSessionGuid)
        {
            EnsureCacheLoaded();

            if (m_BuildOrder.Count > 0)
            {
                latestBuildSessionGuid = m_BuildOrder[0];
                return true;
            }

            latestBuildSessionGuid = new GUID();
            return false;
        }

        // Sorted by end time (BuildStartedAt + TotalTimeMs) rather than start time so that
        // nested builds report correctly: the inner build starts later but finishes first.
        internal bool TryGetLatestCompletedBuild(out GUID buildSessionGuid)
        {
            EnsureCacheLoaded();

            DateTime bestEndTime = DateTime.MinValue;
            buildSessionGuid = default;
            bool found = false;

            foreach (var entry in m_Builds.Values)
            {
                if (entry.Summary.BuildResult == BuildResult.Pending)
                    continue;

                DateTime endTime = entry.BuildStartTime.AddMilliseconds(entry.Summary.TotalTimeMs);
                if (!found || endTime > bestEndTime)
                {
                    bestEndTime = endTime;
                    buildSessionGuid = entry.Summary.BuildSessionGUID;
                    found = true;
                }
            }

            return found;
        }

        // Gets the build summary for a specific build.
        public BuildReportSummary GetBuildSummary(GUID buildSessionGuid)
        {
            EnsureCacheLoaded();

            if (m_Builds.TryGetValue(buildSessionGuid, out BuildEntry entry))
                return entry.Summary;

            throw new ArgumentException($"Build not found in history: {buildSessionGuid}");
        }

        // Attempts to get the build summary for the most recent build that was made to a specific output path.
        public bool TryGetBuildSummaryForOutputPath(string buildOutputPath, out BuildReportSummary buildSummary)
        {
            if (string.IsNullOrEmpty(buildOutputPath))
            {
                buildSummary = default;
                return false;
            }

            EnsureCacheLoaded();

            string normalizedOutputPath = NormalizePath(buildOutputPath);

            // Search in order (most recent first) for a build with matching output path
            foreach (var buildGUID in m_BuildOrder)
            {
                if (m_Builds.TryGetValue(buildGUID, out BuildEntry entry) &&
                    !string.IsNullOrEmpty(entry.Summary.OutputPath))
                {
                    string entryOutputPath = NormalizePath(entry.Summary.OutputPath);
                    if (entryOutputPath.Equals(normalizedOutputPath, StringComparison.OrdinalIgnoreCase))
                    {
                        buildSummary = entry.Summary;
                        return true;
                    }
                }
            }

            buildSummary = default;
            return false;
        }

        // Attempts to get the build summary for the most recent build that produced a specific manifest hash.
        public bool TryGetBuildSummaryForManifestHash(Hash128 manifestHash, out BuildReportSummary buildSummary)
        {
            EnsureCacheLoaded();

            string manifestHashString = manifestHash.ToString();

            // Search in order (most recent first) for a build with matching manifest hash
            foreach (var buildGUID in m_BuildOrder)
            {
                if (m_Builds.TryGetValue(buildGUID, out BuildEntry entry) &&
                    !string.IsNullOrEmpty(entry.Summary.BuildManifestHash) &&
                    entry.Summary.BuildManifestHash.Equals(manifestHashString, StringComparison.OrdinalIgnoreCase))
                {
                    buildSummary = entry.Summary;
                    return true;
                }
            }

            buildSummary = default;
            return false;
        }

        // Loads the BuildReport for a specific build from its metadata folder.
        public BuildReport LoadBuildReport(GUID buildSessionGuid)
        {
            if (!TryGetFilePath(buildSessionGuid, buildSessionGuid.ToString() + ".buildreport", out string filePath))
                return null;

            return BuildReport.LoadReport(filePath);
        }

        // Attempts to get the path to a specific file within a build's metadata folder.
        // Returns true only if the build is tracked and the file exists on disk.
        public bool TryGetFilePath(GUID buildSessionGuid, string filename, out string filePath)
        {
            EnsureCacheLoaded();

            if (m_Builds.TryGetValue(buildSessionGuid, out BuildEntry entry))
            {
                filePath = entry.MetadataPath + "/" + filename;
                return File.Exists(filePath);
            }

            filePath = string.Empty;
            return false;
        }

        // Gets the current revision number of the build history.
        public int GetRevision()
        {
            EnsureCacheLoaded();
            return m_Revision;
        }

        // ==================== Public Mutation API ====================

        // Updates the BuildHistory incrementally, detecting added/removed build directories.
        public void Refresh()
        {
            IncrementalRefresh();
        }

        // Performs a full reload of the BuildHistory from disk, discarding any cached state.
        public void RefreshFull()
        {
            InvalidateCache();
            EnsureCacheLoaded();
        }

        // Invalidates the cache, forcing a full reload on next access.
        internal void InvalidateCache()
        {
            m_IsDirty = true;
            m_Revision++;
        }

        // ==================== Internal Build Lifecycle API ====================

        /// <summary>
        /// Adds a new build to the cache, or updates an existing build with its latest state.
        /// Called by the build pipeline after BuildReportSummary.json is written (both at
        /// the start of a build and when it completes).
        /// </summary>
        /// <param name="metadataPath">The path to the build metadata directory.</param>
        internal void AddOrUpdateBuild(string metadataPath)
        {
            // Only update cache if it's already loaded
            if (!m_IsLoaded)
                return;

            // Try to load the build entry
            if (!TryLoadBuildEntry(metadataPath, out BuildEntry entry))
                return;

            // Check if build already exists (update scenario)
            if (m_Builds.ContainsKey(entry.Summary.BuildSessionGUID))
            {
                AddOrUpdateBuildEntry(entry);
                // Rebuild sort order in case build time changed
                RebuildSortOrder();
            }
            else
            {
                AddOrUpdateBuildEntry(entry);
                InsertBuildInOrder(entry);
            }

            m_Revision++;
        }

        /// <summary>
        /// Removes specific builds from the cache.
        /// </summary>
        /// <param name="buildSessionGuids">The GUIDs of the builds to remove.</param>
        internal void RemoveBuilds(GUID[] buildSessionGuids)
        {
            if (!m_IsLoaded)
                return; // Cache not loaded, nothing to update

            bool anyRemoved = false;
            foreach (var buildGUID in buildSessionGuids)
            {
                if (RemoveBuildEntry(buildGUID))
                    anyRemoved = true;
            }

            if (anyRemoved)
                m_Revision++;
        }

        // Deletes specific build metadata directories from disk and removes them from the cache.
        public int DeleteHistory(GUID[] buildSessionGuids)
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                Debug.LogWarning("Cannot delete build history while a build is in progress.");
                return 0;
            }

            return DeleteHistoryUnchecked(buildSessionGuids);
        }

        // ApplyRetentionPolicy runs while BuildPipeline.isBuildingPlayer is true; it must bypass
        // the public DeleteHistory's guard against deletion-during-build.
        private int DeleteHistoryUnchecked(GUID[] buildSessionGuids)
        {
            var successfullyDeleted = new List<GUID>();

            foreach (var buildGUID in buildSessionGuids)
            {
                if (TryGetBuildReportDirectory(buildGUID, out string metadataPath))
                {
                    try
                    {
                        if (Directory.Exists(metadataPath))
                        {
                            // Note: CBD-1862 For ContentDirectory builds also remove root from UDS
                            Directory.Delete(metadataPath, true);
                            successfullyDeleted.Add(buildGUID);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to delete build {buildGUID}: {e.Message}");
                    }
                }
            }

            // Only remove builds that were actually deleted
            if (successfullyDeleted.Count > 0)
                RemoveBuilds(successfullyDeleted.ToArray());

            return successfullyDeleted.Count;
        }

        public int ApplyRetentionPolicy(int limit)
        {
            if (limit <= 0)
                return 0;

            EnsureCacheLoaded();

            if (m_BuildOrder.Count <= limit)
                return 0;

            // m_BuildOrder is sorted most-recent-first.
            int removeCount = m_BuildOrder.Count - limit;
            var toDelete = new GUID[removeCount];
            for (int i = 0; i < removeCount; i++)
                toDelete[i] = m_BuildOrder[limit + i];

            return DeleteHistoryUnchecked(toDelete);
        }

        // Deletes all build history from disk and clears the cache.
        // Returns the number of build metadata directories successfully deleted.
        public int DeleteAllHistory()
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                Debug.LogWarning("Cannot delete build history while a build is in progress.");
                return 0;
            }

            string rootPath = BuildHistory.BuildHistoryDirectory;
            int deletedCount = 0;

            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
            {
                // No directory exists, but still clear cache if loaded
                ClearCache();
                return 0;
            }

            var directories = Directory.GetDirectories(rootPath);
            foreach (var dir in directories)
            {
                // For safety, only remove directories that are actual build-metadata folders (CBD-1861).
                // A directory is considered a build-metadata folder if it contains BuildReportSummary.json
                if (IsValidMetadataDirectory(dir))
                {
                    try
                    {
                        // Note: CBD-1862 For ContentDirectory builds also remove root from UDS
                        Directory.Delete(dir, true);
                        deletedCount++;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to delete build metadata directory {dir}: {e.Message}");
                    }
                }
            }

            // Clear the cache immediately
            ClearCache();

            return deletedCount;
        }

        // Attempts to get the build report directory for a specific build.
        public bool TryGetBuildReportDirectory(GUID buildSessionGuid, out string directory)
        {
            EnsureCacheLoaded();

            if (m_Builds.TryGetValue(buildSessionGuid, out BuildEntry entry))
            {
                directory = entry.MetadataPath;
                return true;
            }

            directory = string.Empty;
            return false;
        }

        // ==================== Private Core Cache Management ====================

        /// <summary>
        /// Ensures the cache is loaded and up-to-date.
        /// Only triggers a reload if the cache is invalid or the directory has changed.
        /// </summary>
        private void EnsureCacheLoaded()
        {
            string currentRoot = BuildHistory.BuildHistoryDirectory;

            // Check if cache needs rebuild
            if (!m_IsLoaded || m_IsDirty || currentRoot != m_CachedRootDirectory)
            {
                LoadBuildHistory();
                m_IsLoaded = true;
                m_IsDirty = false;
                m_CachedRootDirectory = currentRoot;
            }
        }

        /// <summary>
        /// Loads the build history by scanning the BuildHistory directory.
        /// Clears any existing state and rebuilds from scratch.
        /// </summary>
        private void LoadBuildHistory()
        {
            // Clear existing state
            m_Builds.Clear();
            m_BuildOrder.Clear();

            string rootDirectory = BuildHistory.BuildHistoryDirectory;

            if (string.IsNullOrEmpty(rootDirectory) || !Directory.Exists(rootDirectory))
                return;

            // Scan all subdirectories for BuildReportSummary.json
            var directories = Directory.GetDirectories(rootDirectory);

            foreach (var dir in directories)
            {
                if (TryLoadBuildEntry(dir, out BuildEntry entry))
                {
                    AddOrUpdateBuildEntry(entry);
                }
            }

            // Sort by build time (most recent first)
            RebuildSortOrder();
            m_Revision++;
        }

        /// <summary>
        /// Performs an incremental refresh, detecting added/removed directories.
        /// </summary>
        /// <remarks>Note: we assume that once established the BuildReportSummary.json
        /// files do not change.</remarks>
        private void IncrementalRefresh()
        {
            // If cache isn't loaded, just do a full load instead of duplicating work
            if (!m_IsLoaded)
            {
                EnsureCacheLoaded();
                return;
            }

            string rootDirectory = BuildHistory.BuildHistoryDirectory;
            if (string.IsNullOrEmpty(rootDirectory) || !Directory.Exists(rootDirectory))
            {
                // Directory doesn't exist, clear cache
                ClearCache();
                return;
            }

            // Get current directories on disk
            var directories = Directory.GetDirectories(rootDirectory);
            var diskDirs = new HashSet<string>(directories);

            // Find directories to remove (in cache but not on disk)
            var toRemove = new List<GUID>();
            foreach (var entry in m_Builds.Values)
            {
                if (!diskDirs.Contains(entry.MetadataPath))
                    toRemove.Add(entry.Summary.BuildSessionGUID);
            }

            // Remove deleted directories from cache
            bool cacheChanged = toRemove.Count > 0;
            foreach (var buildGUID in toRemove)
                RemoveBuildEntry(buildGUID);

            // Find directories to add (on disk but not in cache)
            var newEntries = new List<BuildEntry>();
            foreach (var dir in directories)
            {
                // Check if already in cache using the metadata path
                bool alreadyCached = false;
                foreach (var entry in m_Builds.Values)
                {
                    if (entry.MetadataPath == dir)
                    {
                        alreadyCached = true;
                        break;
                    }
                }

                if (alreadyCached)
                    continue;

                // Try to load the new build entry
                if (TryLoadBuildEntry(dir, out BuildEntry newEntry))
                {
                    newEntries.Add(newEntry);
                    AddOrUpdateBuildEntry(newEntry);
                    cacheChanged = true;
                }
            }

            // Rebuild sort order if anything changed
            if (cacheChanged)
            {
                RebuildSortOrder();
                m_Revision++;
            }
        }

        /// <summary>
        /// Clears the cache and increments the revision.
        /// </summary>
        private void ClearCache()
        {
            if (m_IsLoaded && (m_Builds.Count > 0 || m_BuildOrder.Count > 0))
            {
                m_Builds.Clear();
                m_BuildOrder.Clear();
                m_Revision++;
            }
        }

        // ==================== Private Build Entry Management ====================

        /// <summary>
        /// Attempts to load a BuildEntry from a metadata directory.
        /// </summary>
        /// <param name="metadataPath">The path to the metadata directory.</param>
        /// <param name="entry">When this method returns, contains the BuildEntry if successful.</param>
        /// <returns>True if the entry was successfully loaded; otherwise false.</returns>
        private bool TryLoadBuildEntry(string metadataPath, out BuildEntry entry)
        {
            entry = default;

            var summaryPath = Path.Combine(metadataPath, BuildReportSummary.kBuildReportSummaryFileName);
            if (!File.Exists(summaryPath))
                return false;

            try
            {
                var summary = BuildReportSummary.Load(summaryPath);

                // Parse the build time from the summary, fallback to directory creation time
                DateTime buildStartTime;
                if (!DateTime.TryParse(summary.BuildStartedAt, out buildStartTime))
                    buildStartTime = Directory.GetCreationTime(metadataPath);

                entry = new BuildEntry
                {
                    MetadataPath = metadataPath,
                    Summary = summary,
                    BuildStartTime = buildStartTime
                };

                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load BuildReportSummary from {summaryPath}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Inserts a build entry into the build order list at the correct position.
        /// Assumes the entry has already been added to m_Builds.
        /// </summary>
        /// <param name="entry">The build entry to insert.</param>
        private void InsertBuildInOrder(BuildEntry entry)
        {
            // Find the correct position (most recent first)
            int insertIndex = m_BuildOrder.Count; // Default to end
            for (int i = 0; i < m_BuildOrder.Count; i++)
            {
                if (m_Builds[m_BuildOrder[i]].BuildStartTime < entry.BuildStartTime)
                {
                    insertIndex = i;
                    break;
                }
            }
            m_BuildOrder.Insert(insertIndex, entry.Summary.BuildSessionGUID);
        }

        /// <summary>
        /// Rebuilds the build order list by sorting all builds by time.
        /// </summary>
        private void RebuildSortOrder()
        {
            var entries = new List<BuildEntry>(m_Builds.Values);
            entries.Sort((a, b) => b.BuildStartTime.CompareTo(a.BuildStartTime)); // Descending order

            m_BuildOrder.Clear();
            foreach (var entry in entries)
                m_BuildOrder.Add(entry.Summary.BuildSessionGUID);
        }

        /// <summary>
        /// Adds or updates a build entry, keeping tracking data structures in sync.
        /// </summary>
        /// <remarks>
        /// m_BuildOrder needs to be updated separately because in some cases it requires a full rebuild
        /// and in other cases we can just insert a new entry.
        /// </remarks>
        private void AddOrUpdateBuildEntry(BuildEntry entry)
        {
            m_Builds[entry.Summary.BuildSessionGUID] = entry;
        }

        /// <summary>
        /// Removes a build entry, keeping data structures in sync
        /// </summary>
        /// <param name="sessionGuid">The GUID of the build to remove.</param>
        /// <returns>True if the build was found and removed; otherwise false.</returns>
        private bool RemoveBuildEntry(GUID sessionGuid)
        {
            if (!m_Builds.Remove(sessionGuid))
                return false;

            m_BuildOrder.Remove(sessionGuid);

            return true;
        }

        // ==================== Private Helper Methods ====================

        /// <summary>
        /// Checks if a directory is a valid metadata directory.
        /// </summary>
        /// <param name="directoryPath">The directory path to check.</param>
        /// <returns>True if the directory contains a BuildReportSummary.json file.</returns>
        private bool IsValidMetadataDirectory(string directoryPath)
        {
            var summaryPath = Path.Combine(directoryPath, BuildReportSummary.kBuildReportSummaryFileName);
            return File.Exists(summaryPath);
        }

        /// <summary>
        /// Normalizes a file path for comparison.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path with forward slashes.</returns>
        private string NormalizePath(string path)
        {
            return Path.GetFullPath(path).Replace('\\', '/');
        }
    }
}
