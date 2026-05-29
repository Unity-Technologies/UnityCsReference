// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.Bindings;
using NiceIO;
using System.Collections.Generic;
using UnityEditor.Build;

namespace UnityEditor.Build.Reporting
{
    /// <summary>The BuildReport API gives you information about the Unity build process.</summary>
    /// <remarks>A BuildReport object is returned by <see cref="BuildPipeline.BuildPlayer" /> and can be used to discover information about the files output, the build steps taken, and other platform-specific information such as native code stripping.
    ///
    /// For AssetBundle builds the BuildReport is available by calling <see cref="GetLatestReport" /> immediately after calling <see cref="BuildPipeline.BuildAssetBundles" />.
    ///
    /// For builds tracked in the <see cref="BuildHistory"/>, use <see cref="BuildHistory.LoadBuildReport"/> to load a BuildReport from the build history.</remarks>
    /// <example>
    /// <code source="../Tests/BuildReporting/Assets/Editor/ReferenceExamples/BuildReport.cs"/>
    /// </example>
    /// <seealso cref="BuildHistory"/>
    /// <seealso cref="Build.BuildReportSummary"/>
    [NativeHeader("NativeKernel/Time/DateTime.h")]
    [NativeHeader("Modules/BuildReportingEditor/Public/BuildReport.h")]
    [NativeClass("BuildReporting::BuildReport")]
    public sealed class BuildReport : Object
    {
        private BuildReport()
        {
        }

        [System.Obsolete("Use GetFiles() method instead (UnityUpgradable) -> GetFiles()", true)]
        public BuildFile[] files => throw new NotSupportedException();

        ///<summary>Returns an array of all the files output by the build process.</summary>
        ///<remarks>The returned array is a copy and this method execution length scales linearly with number of files.</remarks>
        ///<returns>An array of all the files output by the build process.</returns>
        public extern BuildFile[] GetFiles();

        /// <summary>
        /// Retrieve the array of paths of root assets that were built in a ContentDirectory build.
        /// </summary>
        /// <returns>An array of root asset paths, or an empty array for Player and AssetBundle builds.</returns>
        /// <seealso cref="BuildContentDirectoryParameters.rootAssetPaths" />
        public extern string[] GetRootAssetPaths();

        ///<summary>An array of all the <see cref="BuildStep" />s that took place during the build process.</summary>
        [NativeName("BuildSteps")]
        public extern BuildStep[] steps { get; }

        ///<summary>A <see cref="BuildSummary" /> containing overall statistics and data about the build process.</summary>
        public extern BuildSummary summary { get; }

        ///<summary>The <see cref="StrippingInfo" /> object for the build.</summary>
        ///<remarks>The StrippingInfo object contains information about which native code modules in the engine are still present in the build, and the reasons why they are still present.
        ///
        ///This is only available when building for platforms that support code stripping. When building for other platforms, this property will be null.</remarks>
        public StrippingInfo strippingInfo
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            get { return GetAppendices<StrippingInfo>().SingleOrDefault(); }
#pragma warning restore UA2001
        }

        /// <summary>
        /// Retrieve statistics about the content of the build, such as total sizes, object counts,
        /// and breakdowns by Unity Object type and source Asset.
        /// </summary>
        /// <remarks>
        /// ContentSummary is populated for Player builds, AssetBundle builds, and ContentDirectory builds.
        /// It is not populated for scripts-only Player builds (see <see cref="BuildOptions.BuildScriptsOnly"/>).
        /// For incremental AssetBundle builds, the statistics only reflect the AssetBundles that were
        /// rebuilt in the current build invocation; unchanged AssetBundles reused from previous builds
        /// are not included.
        /// This property returns <c>null</c> if the ContentSummary was not populated.
        /// </remarks>
        /// <seealso cref="ContentSummary"/>
        public ContentSummary contentSummary
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            get { return GetAppendicesByType<ContentSummary>().SingleOrDefault(); }
#pragma warning restore UA2001
        }

        ///<summary>An array of all the <see cref="PackedAssets" /> generated by the build process.</summary>
        public PackedAssets[] packedAssets
        {
            get { return GetAppendicesByType<PackedAssets>(); }
        }

        ///<summary>An optional array of <see cref="ScenesUsingAssets" /> generated by the build process if <see cref="BuildOptions.DetailedBuildReport" /> was used during the build.</summary>
        public ScenesUsingAssets[] scenesUsingAssets
        {
            get { return GetAppendicesByType<ScenesUsingAssets>(); }
        }

        [NativeMethod("RelocateFiles")]
        internal extern void RecordFilesMoved(string originalPathPrefix, string newPathPrefix);

        [NativeMethod("AddFile")]
        internal extern void RecordFileAdded(string path, string role, bool fileIsFromCache = false);

        [NativeMethod("AddFilesRecursive")]
        internal extern void RecordFilesAddedRecursive(string rootDir, string role);

        [NativeMethod("DeleteFile")]
        internal extern void RecordFileDeleted(string path);

        [NativeMethod("DeleteFilesRecursive")]
        internal extern void RecordFilesDeletedRecursive(string rootDir);

        [NativeMethod("DeleteAllFiles")]
        internal extern void DeleteAllFileEntries();

        ///<summary>Returns a string summarizing any errors that occurred during the build</summary>
        ///<remarks>Convenience method for summarizing errors (or exceptions) that occurred during a build into a single line of text.
        ///If no error was logged this returns an empty string.  If a single error was logged this reports the error messages.  Otherwise it reports the number of errors, for example "5 errors".
        ///
        ///Note: To examine all errors, warnings and other messages recorded during a build you can enumerating through the build <see cref="steps" /> and check <see cref="Build.Reporting.BuildStep.messages" />.
        ///And to retrieve the count of errors call <see cref="Build.Reporting.BuildSummary.totalErrors" />.</remarks>
        [FreeFunction("BuildReporting::SummarizeErrors", HasExplicitThis = true)]
        public extern string SummarizeErrors();

        internal extern void AddMessage(LogType messageType, string message, string exceptionType);

        internal extern void SetBuildResult(BuildResult result);

        internal extern int BeginBuildStep(string stepName);
        internal extern void ResumeBuildStep(int depth);
        internal extern void EndBuildStep(int depth);

        internal extern void AddAppendix([NotNull] Object obj);

        internal TAppendix[] GetAppendices<TAppendix>() where TAppendix : Object
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return GetAppendices(typeof(TAppendix)).Cast<TAppendix>().ToArray();
#pragma warning restore UA2001
        }

        internal extern Object[] GetAppendices([NotNull] Type type);

        internal TAppendix[] GetAppendicesByType<TAppendix>() where TAppendix : Object
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return GetAppendicesByType(typeof(TAppendix)).Cast<TAppendix>().ToArray();
#pragma warning restore UA2001
        }

        [NativeMethod(ThrowsException = true)]
        internal extern Object[] GetAppendicesByType([NotNull] Type type);

        internal extern Object[] GetAllAppendices();

        /// <summary>Obtains the build report generated by the most recent Player, Content Directory, or AssetBundle build.</summary>
        /// <returns>The most recent <see cref="BuildReport"/>, or `null` if no build has been performed.</returns>
        /// <remarks>For Player and Content Directory builds, the build report is loaded from the <see cref="BuildHistory"/> folder.
        /// For AssetBundle builds, the build report is loaded from `Library/LastBuild.buildreport`.
        ///
        /// To access build reports from earlier builds, use the <see cref="BuildHistory"/> API.</remarks>
        public static BuildReport GetLatestReport()
        {
            const string legacyReportPath = "Library/LastBuild.buildreport";
            string reportPath = legacyReportPath;

            // An in-progress build at the head of the history must not mask the previous
            // completed report - this matches the prior native behavior of reading
            // LatestBuild.link, which was only written by FinalizeBuild.
            // GetLastWriteTimeUtc returns 1601-01-01 for missing files, so no Exists check needed.
            if (BuildHistory.TryGetLatestCompletedBuild(out GUID guid) &&
                BuildHistory.TryGetFilePath(guid, guid + ".buildreport", out string historyReportPath) &&
                File.GetLastWriteTimeUtc(legacyReportPath) < File.GetLastWriteTimeUtc(historyReportPath))
            {
                reportPath = historyReportPath;
            }

            return LoadReport(reportPath);
        }

        [FreeFunction("BuildReporting::GetReport")]
        internal static extern BuildReport GetReport(GUID guid);

        /// <summary>Loads a BuildReport from the specified file path.</summary>
        /// <param name="buildReportPath">The absolute or project-relative path to a `.buildreport` file.</param>
        /// <returns>The loaded BuildReport, or `null` if the file doesn't exist or couldn't be loaded.</returns>
        /// <remarks>This method can be used to load BuildReport files no matter where on the file system they are located,
        /// including locations outside the Asset folder of a project.
        /// </remarks>
        /// <seealso cref="BuildHistory.LoadBuildReport"/>
        [FreeFunction("BuildReporting::LoadReport")]
        public static extern BuildReport LoadReport(string buildReportPath);

        internal extern void SetBuildGUID(GUID guid);

        internal extern void SetBuildSessionGUID(GUID guid);

        internal void ReplaceAllFileEntries(IEnumerable<NPath> paths)
        {
            if (summary.buildType == BuildType.ContentDirectory)
                // ContentDirectory builds store paths relative to summary.outputPath.
                // ReplaceAllFileEntries re-adds via RecordFileAdded which canonicalizes to absolute,
                // so it would corrupt a ContentDirectory report (see CBD-1988)
                throw new InvalidOperationException("ReplaceAllFileEntries is not supported for ContentDirectory builds.");

            // Keep a copy of existing files in the build report to preserve file roles
            var fileNameToExistingBuildFile = new Dictionary<string, BuildFile>();
            foreach (var file in GetFiles())
            {
                // Files with the same name should have the same role
                fileNameToExistingBuildFile.TryAdd(file.path.ToNPath().FileName, file);
            }

            DeleteAllFileEntries();

            foreach (var path in paths)
            {
                if (fileNameToExistingBuildFile.TryGetValue(path.FileName, out BuildFile buildFile))
                {
                    RecordFileAdded(path.ToString(), buildFile.role, buildFile.isFromCache);
                }
                else
                {
                    // The file is not matched, so use the extension as the "role"
                    RecordFileAdded(path.ToString(), path.Extension);
                }
            }
        }
    }
}
