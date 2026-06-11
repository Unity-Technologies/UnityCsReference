// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEditor.Build.Reporting;
using System.IO;

namespace UnityEditor.Build
{
    /// <summary>
    /// Provides access to the build history generated during builds.
    /// </summary>
    /// <remarks>
    /// For each Player or Content Directory build, Unity creates a *build report directory* inside the build history. This directory holds the
    /// <see cref="Build.Reporting.BuildReport"/> file and the supporting data captured during that build, including profiling information and
    /// type-usage information. The precise content is influenced by the type of build, build options
    /// and certain settings in the **Preferences** > **Build Pipeline** window.
    ///
    /// By default, build history is stored in `Library/BuildHistory`. You can change this location, for example
    /// to consolidate builds from multiple machines into a shared build history folder.
    ///
    /// Unity assigns each build a unique GUID, which the `BuildHistory` API uses to precisely identify each build.
    /// For more information, refer to <see cref="BuildReportSummary.BuildSessionGUID"/>.
    ///
    /// The files in the build history are for development and debugging purposes only. They are not meant to be shipped along with
    /// the content and are not required by the runtime. Deleting build history does not impact the built content or the ability
    /// to run the Player, but can limit the ability to analyze or debug the results of the build. Incorporate the collection
    /// of the build history content into automated build pipelines.
    ///
    /// Retention Policy
    ///
    /// To prevent unbounded growth of the build history folder, Unity applies a retention policy at the start of each Player and Content Directory build.
    /// <see cref="BuildHistoryLimit"/> sets the maximum number of builds to retain; when a new build pushes the count over the limit, the oldest entries are deleted.
    /// Set the limit to 0 to disable automatic deletion. You can also invoke the policy manually with <see cref="ApplyRetentionPolicy"/>.
    ///
    /// Build Lifecycle
    ///
    /// For Player builds, <see cref="BuildPlayerProcessor.PrepareForBuild"/> runs before the Player build is added to the
    /// history. This allows any Content Directory builds triggered during that callback to appear in the
    /// history before the Player build itself, resulting in a chronological ordering.
    ///
    /// Unity adds a build to the history early in the build process (but after <see cref="BuildPlayerProcessor.PrepareForBuild"/>) and
    /// sets the result to <see cref="Build.Reporting.BuildResult.Pending"/>.
    /// If the Editor process terminates during a build, the build remains in the history with its initial
    /// `Pending` result.
    ///
    /// Some BuildHistory methods, for example <see cref="TryGetBuildReportDirectory"/> and
    /// <see cref="TryGetFilePath"/>, are available for use in the
    /// <see cref="IPreprocessBuildWithReport"/> and <see cref="IPostprocessBuildWithReport"/> build callbacks.
    ///
    /// When the build completes, the <see cref="BuildReportSummary"/> is updated with the final result
    /// (<see cref="Build.Reporting.BuildResult.Succeeded"/>, <see cref="Build.Reporting.BuildResult.Failed"/>,
    /// or <see cref="Build.Reporting.BuildResult.Cancelled"/>).
    /// </remarks>
    /// <seealso cref="Build.BuildReportSummary"/>
    /// <seealso cref="Build.Reporting.BuildReport"/>
    /// <example>
    /// <code source="../Tests/BuildReporting/Assets/Editor/ReferenceExamples/BuildHistoryStats.cs"/>
    /// </example>
    [VisibleToOtherModules]
    [NativeHeader("Modules/BuildReportingEditor/Public/BuildHistory.h")]
    public static class BuildHistory
    {
        // ==================== Configuration Properties ====================

        /// <summary>
        /// The default build history root directory path, regardless of the current build history directory.
        /// </summary>
        public static string DefaultRootDirectory
        {
            get { return GetDefaultRootDirectory(); }
        }

        /// <summary>
        /// Gets or sets the path where the build history will be stored.
        /// </summary>
        /// <remarks>
        /// The default location is `Library/BuildHistory`.  When setting a custom path, folders beneath the `Assets`
        /// and `Temp` folders are not allowed. The path can also be changed in **Project Settings** > **Analysis** > **Build Pipeline**.
        /// Changing this path doesn't move the existing build history and only affects where new builds are stored.
        /// </remarks>
        public static string BuildHistoryDirectory
        {
            get
            {
                var raw = BuildPipelineUserSettings.instance.BuildHistoryDirectoryRaw;
                var resolved = string.IsNullOrEmpty(raw) ? DefaultRootDirectory : raw;
                // Push to the C++ in-memory cache only when it changes — SetRootDirectory does
                // path validation (PathToAbsolutePath x3 + starts_with x2) on every call, and
                // this getter runs multiple times per IMGUI frame from the settings panel.
                if (resolved != s_LastPushedDirectory)
                {
                    if (SetRootDirectory(resolved))
                        s_LastPushedDirectory = resolved;
                    // else: native rejected the path (e.g. someone wrote Assets/Foo to the
                    //       singleton bypassing setter validation). Native keeps its previous
                    //       value; we don't cache so the next read retries and the warning
                    //       repeats until the user fixes their setting.
                }
                return resolved;
            }
            set
            {
                // Resolve empty/null the same way the getter does, so the native cache never
                // holds an unresolved "" while the C# raw value still means "use the default".
                var resolved = string.IsNullOrEmpty(value) ? DefaultRootDirectory : value;
                if (!SetRootDirectory(resolved))
                {
                    throw new System.ArgumentException($"Invalid build history directory path: {value}. Path cannot be inside Assets or Temp folders.");
                }
                BuildPipelineUserSettings.instance.BuildHistoryDirectoryRaw = value;
                s_LastPushedDirectory = resolved;
            }
        }

        static string s_LastPushedDirectory;

        /// <summary>
        /// Returns the build report directory of the most recent build.
        /// </summary>
        /// <returns>The path of the most recent build report directory, or an empty string if the build history is empty.</returns>
        public static string LatestBuildReportDirectory
        {
            get
            {
                if (TryGetLatestCompletedBuild(out GUID guid) &&
                    TryGetBuildReportDirectory(guid, out string directory))
                {
                    return directory;
                }
                return string.Empty;
            }
        }

        /// <undoc/>
        [Obsolete("LatestBuildDirectory has been renamed to LatestBuildReportDirectory to disambiguate from the build output directory. (UnityUpgradable) -> LatestBuildReportDirectory", true)]
        public static string LatestBuildDirectory => LatestBuildReportDirectory;

        // The most recent build that has finished (any non-Pending result). Distinct from
        // TryGetLatestBuild, which includes the in-progress build during preprocess callbacks.
        internal static bool TryGetLatestCompletedBuild(out GUID buildSessionGuid)
        {
            return BuildHistoryState.Instance.TryGetLatestCompletedBuild(out buildSessionGuid);
        }

        /// <summary>
        /// Maximum number of builds to retain in the build history.
        /// </summary>
        /// <remarks>
        /// When a new build pushes the count over this limit, the oldest entries are removed,
        /// where "oldest" is determined by the build start time recorded in each entry's <see cref="BuildReportSummary"/>.
        /// Build folders copied in from another machine are subject to the same policy.
        ///
        /// A value of 0 disables automatic deletion. Negative values are clamped to 0.
        ///
        /// Changes take effect on the next build. The existing history isn't pruned when this value is changed.
        /// This setting is also exposed in **Project Settings** > **Analysis** > **Build Pipeline**.
        /// </remarks>
        /// <seealso cref="ApplyRetentionPolicy"/>
        public static int BuildHistoryLimit
        {
            get => BuildPipelineUserSettings.instance.BuildHistoryLimit;
            set => BuildPipelineUserSettings.instance.BuildHistoryLimit = value;
        }

        // ==================== Query API - Collection Operations ====================

        /// <summary>
        /// Gets the total number of builds in the build history.
        /// </summary>
        /// <returns>Total count of all builds.</returns>
        public static int GetBuildCount()
        {
            return BuildHistoryState.Instance.GetBuildCount();
        }

        /// <summary>
        /// Returns the session GUIDs for all builds in the build history.
        /// </summary>
        /// <returns>Array of build session GUIDs, sorted by build start time (most recent to oldest).</returns>
        public static GUID[] GetAllBuilds()
        {
            return BuildHistoryState.Instance.GetAllBuilds();
        }

        /// <summary>
        /// Attempts to get the GUID of the most recent build.
        /// </summary>
        /// <param name="latestBuildSessionGuid">When this method returns, contains the GUID of the most recent build if found.</param>
        /// <returns>`true` if a build was found; `false` if the build history is empty.</returns>
        public static bool TryGetLatestBuild(out GUID latestBuildSessionGuid)
        {
            return BuildHistoryState.Instance.TryGetLatestBuild(out latestBuildSessionGuid);
        }

        // ==================== Query API - Single Build Operations ====================

        /// <summary>
        /// Gets the build summary for a specific build.
        /// </summary>
        /// <param name="buildSessionGuid">The unique session GUID of the build to retrieve from the history.</param>
        /// <returns>The <see cref="BuildReportSummary"/> for the specified build.</returns>
        /// <exception cref="System.ArgumentException">Thrown when the build is not tracked in the Build History.</exception>
        /// <seealso cref="LoadBuildReport"/>
        public static BuildReportSummary GetBuildSummary(GUID buildSessionGuid)
        {
            return BuildHistoryState.Instance.GetBuildSummary(buildSessionGuid);
        }

        /// <summary>
        /// Loads the <see cref="Build.Reporting.BuildReport"/> for a specific build from its metadata folder.
        /// </summary>
        /// <param name="buildSessionGuid">The unique session GUID of the build to load the report for.</param>
        /// <returns>The <see cref="Build.Reporting.BuildReport"/> object, or `null` if not found or failed to load.</returns>
        /// <seealso cref="GetBuildSummary"/>
        /// <seealso cref="Build.Reporting.BuildReport"/>
        public static BuildReport LoadBuildReport(GUID buildSessionGuid)
        {
            return BuildHistoryState.Instance.LoadBuildReport(buildSessionGuid);
        }

        /// <summary>
        /// Attempts to get the path to a specific file within a build's metadata folder.
        /// </summary>
        /// <param name="buildSessionGuid">The unique session GUID of the build to search within.</param>
        /// <param name="filename">The name of the file to locate, for example `contentlayout.json`.</param>
        /// <param name="filePath">When this method returns, contains the absolute path to the file if it exists.</param>
        /// <returns>`true` if the build is tracked and the file exists on disk; otherwise `false`.</returns>
        /// <example>
        /// <code source="../Tests/BuildReporting/Assets/Editor/ReferenceExamples/GetTepFile.cs"/>
        /// </example>
        public static bool TryGetFilePath(GUID buildSessionGuid, string filename, out string filePath)
        {
            return BuildHistoryState.Instance.TryGetFilePath(buildSessionGuid, filename, out filePath);
        }

        /// <summary>
        /// Attempts to get the build report directory for a specific build.
        /// </summary>
        /// <param name="buildSessionGuid">The unique session GUID of the build to look up.</param>
        /// <param name="directory">When this method returns, contains the absolute path to the build report directory.</param>
        /// <returns>`true` if the build is tracked in the build history; otherwise `false`.</returns>
        /// <remarks>The build report directory holds the <see cref="Build.Reporting.BuildReport"/> file and the
        /// supporting data captured during that build. The directory is guaranteed to exist on disk if this method
        /// returns `true`. Use this to locate files written during the build, or to write additional files alongside
        /// the build report.</remarks>
        public static bool TryGetBuildReportDirectory(GUID buildSessionGuid, out string directory)
        {
            return BuildHistoryState.Instance.TryGetBuildReportDirectory(buildSessionGuid, out directory);
        }

        /// <undoc/>
        [Obsolete("TryGetMetadataPath has been renamed to TryGetBuildReportDirectory. (UnityUpgradable) -> TryGetBuildReportDirectory(*)", true)]
        public static bool TryGetMetadataPath(GUID buildSessionGuid, out string directory)
        {
            return TryGetBuildReportDirectory(buildSessionGuid, out directory);
        }

        // ==================== Query API - Search Operations ====================

        /// <summary>
        /// Attempts to get the build summary for the most recent build that produced a specific manifest hash.
        /// </summary>
        /// <param name="manifestHash">The manifest hash to search for.</param>
        /// <param name="buildSummary">When this method returns, contains the build summary if found.</param>
        /// <returns>`true` if a build with the specified manifest hash was found; otherwise `false`.</returns>
        /// <remarks>This method is only applicable to Content Directory builds.</remarks>
        public static bool TryGetBuildSummaryForManifestHash(Hash128 manifestHash, out BuildReportSummary buildSummary)
        {
            return BuildHistoryState.Instance.TryGetBuildSummaryForManifestHash(manifestHash, out buildSummary);
        }

        /// <summary>
        /// Attempts to get the build summary for the most recent build that was made to a specific output path.
        /// </summary>
        /// <param name="buildOutputPath">The build output path to search for.</param>
        /// <param name="buildSummary">When this method returns, contains the build summary if found.</param>
        /// <returns>`true` if a build with the specified output path was found; otherwise `false`.</returns>
        public static bool TryGetBuildSummaryForOutputPath(string buildOutputPath, out BuildReportSummary buildSummary)
        {
            return BuildHistoryState.Instance.TryGetBuildSummaryForOutputPath(buildOutputPath, out buildSummary);
        }

        // ==================== Mutation API ====================

        /// <summary>
        /// Updates the BuildHistory incrementally, detecting build directories that have been added or removed on disk.
        /// </summary>
        /// <remarks>This is useful when build folders have been added or removed externally, such as when downloading additional builds.
        /// Existing cached build metadata is assumed to be unchanged. Use <see cref="RefreshFull"/> to force a complete reload.</remarks>
        public static void Refresh()
        {
            BuildHistoryState.Instance.Refresh();
        }

        /// <summary>
        /// Performs a full reload of the BuildHistory from disk, discarding all cached state.
        /// </summary>
        /// <remarks>Use this when the contents of existing build metadata directories may have changed on disk.
        /// For detecting added or removed directories only, prefer <see cref="Refresh"/> which is less expensive.</remarks>
        public static void RefreshFull()
        {
            BuildHistoryState.Instance.RefreshFull();
        }

        /// <summary>
        /// Gets the current revision number of the build history.
        /// </summary>
        /// <returns>The revision number, which increments each time a build is added or removed.</returns>
        /// <remarks>UI can cache this value to efficiently detect when a refresh is needed.</remarks>
        public static int GetRevision()
        {
            return BuildHistoryState.Instance.GetRevision();
        }

        /// <summary>
        /// Deletes the oldest builds in the build history so that no more than <see cref="BuildHistoryLimit"/> remain.
        /// </summary>
        /// <returns>The number of build metadata directories successfully deleted.</returns>
        /// <remarks>
        /// If <see cref="BuildHistoryLimit"/> is 0, or the current build count is at or below the limit,
        /// no entries are removed and this method returns 0.
        ///
        /// If a folder cannot be deleted (for example, due to a file lock or a permissions issue),
        /// a warning is logged and the operation continues with the remaining entries.
        /// </remarks>
        /// <seealso cref="BuildHistoryLimit"/>
        public static int ApplyRetentionPolicy()
        {
            return BuildHistoryState.Instance.ApplyRetentionPolicy(BuildHistoryLimit);
        }

        /// <summary>
        /// Deletes all recorded build history.
        /// </summary>
        /// <returns>The number of build metadata directories successfully deleted.</returns>
        public static int DeleteHistory()
        {
            return BuildHistoryState.Instance.DeleteAllHistory();
        }

        /// <summary>
        /// Deletes specific build metadata directories.
        /// </summary>
        /// <param name="buildSessionGuids">The unique session GUIDs of the builds to delete from the history.</param>
        /// <returns>The number of builds successfully deleted.</returns>
        /// <example>
        /// <code source="../Tests/BuildReporting/Assets/Editor/ReferenceExamples/DeleteOldBuilds.cs"/>
        /// </example>
        public static int DeleteHistory(GUID[] buildSessionGuids)
        {
            return BuildHistoryState.Instance.DeleteHistory(buildSessionGuids);
        }

        // ==================== Build Lifecycle (called from C++ build pipeline) ====================

        // Folder name format: YYYYMMDD-HHMMSSZ-<8 hex chars of build session GUID>.
        // The short suffix is for tie-breaking within a one-second window; the
        // authoritative identifier is the full GUID in BuildReportSummary.BuildSessionGUID.
        // The folder name is a label, not an ID - no production code parses it.
        const int kFolderNameGuidPrefixLength = 8;

        internal static string FormatBuildHistoryFolderName(GUID buildSessionGuid, DateTime startTimeUtc)
        {
            DateTime utc = startTimeUtc.Kind == DateTimeKind.Utc
                ? startTimeUtc
                : startTimeUtc.ToUniversalTime();
            string timestamp = utc.ToString("yyyyMMdd-HHmmss'Z'", CultureInfo.InvariantCulture);
            string shortGuid = buildSessionGuid.ToString().Substring(0, kFolderNameGuidPrefixLength);
            return $"{timestamp}-{shortGuid}";
        }

        /// <summary>
        /// Determine the build report directory path and ensure the root BuildHistory directory exists.
        /// This can happen very early in a build, as soon as the unique build session guid has been generated.
        /// The directory itself is not created here; it is created later in BeginBuildTracking
        /// when we are ready to write the initial BuildReportSummary.json.
        /// </summary>
        /// <param name="buildSessionGuidString">The build session GUID as a string.</param>
        /// <param name="buildStartTimeTicks">Official build start time in UTC ticks
        /// (System.DateTime.Ticks / C++ DateTime::ticks).</param>
        [RequiredByNativeCode]
        internal static string ReserveBuildReportDirectory(string buildSessionGuidString, long buildStartTimeTicks)
        {
            try
            {
                if (buildStartTimeTicks <= 0)
                    throw new ArgumentException("buildStartTimeTicks must be a positive UTC tick count", nameof(buildStartTimeTicks));

                string rootDirectory = BuildHistoryDirectory;
                Directory.CreateDirectory(rootDirectory);

                DateTime startTime = new DateTime(buildStartTimeTicks, DateTimeKind.Utc);

                var buildSessionGuid = new GUID(buildSessionGuidString);
                string folderName = FormatBuildHistoryFolderName(buildSessionGuid, startTime);
                string directoryPath = rootDirectory + "/" + folderName;

                if (Directory.Exists(directoryPath))
                {
                    Debug.LogError($"Build report directory already exists at: {directoryPath}. The folder name should be unique.");
                    return "";
                }

                return directoryPath;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"BuildHistory.ReserveBuildReportDirectory failed: {e}");
                return "";
            }
        }

        /// <summary>
        /// Called early in a build after the BuildReport is initialized with info about the planned build.
        /// Writes an initial BuildReportSummary.json with BuildResult.Pending
        /// and registers the build in BuildHistoryState so that API methods
        /// like TryGetBuildReportDirectory() and TryGetFilePath() work during the build.
        /// </summary>
        [RequiredByNativeCode]
        internal static void BeginBuildTracking(BuildReport report, string metadataPath)
        {
            try
            {
                if (report == null || string.IsNullOrEmpty(metadataPath))
                    return;

                Directory.CreateDirectory(metadataPath);

                BuildReportSummary.Save(report, metadataPath);
                BuildHistoryState.Instance.AddOrUpdateBuild(metadataPath);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"BuildHistory.BeginBuildTracking failed: {e.Message}");
            }
        }

        /// <summary>
        /// Called when the build completes (success, failure, or cancellation).
        /// Rewrites BuildReportSummary.json with the final state and updates BuildHistoryState.
        /// </summary>
        [RequiredByNativeCode]
        internal static void FinalizeBuild(BuildReport report)
        {
            try
            {
                if (report == null)
                    return;

                if (!BuildHistoryState.Instance.TryGetBuildReportDirectory(report.summary.buildSessionGuid, out string metadataPath))
                    return;

                // Rewrite BuildReportSummary.json with the final build state
                BuildReportSummary.Save(report, metadataPath);

                if (report.summary.buildType == BuildType.Player)
                    TryImportPlayerBuildProfile(metadataPath);

                // Update the cached state with the final summary
                BuildHistoryState.Instance.AddOrUpdateBuild(metadataPath);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"BuildHistory.FinalizeBuild failed: {e.Message}");
            }
        }

        // The BeeDriver writes the player build trace-event profile to this fixed location
        const string k_PlayerBuildProfileSourcePath = "Library/Bee/buildreport.json";

        // We use a different name in the build report directory, to avoid confusion
        // with the primary BuildReport file.
        internal const string PlayerBuildProfileFileName = "BuildPlayerTEP.json";

        // Copy player build profile into the build report directory.
        // The original file is left in place, for backward compatibility with
        // tools and CI pipelines that still consume the legacy location.
        static void TryImportPlayerBuildProfile(string buildReportDirectory)
        {
            if (!File.Exists(k_PlayerBuildProfileSourcePath))
            {
                Debug.LogWarning($"Player build profile not found at '{k_PlayerBuildProfileSourcePath}'; " +
                    $"{PlayerBuildProfileFileName} will not be tracked in BuildHistory for this build.");
                return;
            }

            string destPath = Path.Combine(buildReportDirectory, PlayerBuildProfileFileName);
            try
            {
                File.Copy(k_PlayerBuildProfileSourcePath, destPath, overwrite: true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to copy player build profile to '{destPath}': {e.Message}");
            }
        }

        // Exceptions are intentionally allowed to propagate so the C++ caller can attach its
        // contextLabel ("Player build" / "ContentDirectory build") to the warning.
        [RequiredByNativeCode]
        internal static int ApplyRetentionPolicyFromNative() => ApplyRetentionPolicy();

        // ==================== Native Bindings ====================

        extern private static string GetDefaultRootDirectory();
        extern private static string GetRootDirectory();
        extern private static bool SetRootDirectory(string buildHistoryPath);
    }
}
