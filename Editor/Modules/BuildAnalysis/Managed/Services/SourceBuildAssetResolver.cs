// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>
    /// The assets of a previous complete Player build, used to stand in for an asset-less Player build
    /// (a scripts-only build, or an incremental build that reused its data cache and recorded no assets).
    /// </summary>
    internal readonly struct SourceBuildAssets
    {
        /// <summary>The source build the assets came from.</summary>
        public readonly GUID BuildGuid;

        /// <summary>Summary of the source build (build name, start time, …).</summary>
        public readonly BuildReportSummary BuildSummary;

        /// <summary>The source build's asset rows; may be empty if the source recorded none.</summary>
        public readonly BuildReportAssetData[] Assets;

        public SourceBuildAssets(GUID buildGuid, BuildReportSummary buildSummary, BuildReportAssetData[] assets)
        {
            BuildGuid = buildGuid;
            BuildSummary = buildSummary;
            Assets = assets;
        }
    }

    internal interface ISourceBuildAssetResolver
    {
        /// <summary>
        /// Follows the source-build pointer the build pipeline stamped for an asset-less <paramref name="target"/>
        /// and loads that exact source build's assets. No history scan — a result exists only when a valid pointer does.
        /// </summary>
        /// <remarks>
        /// Main-thread only: this loads build reports and runs the importer lookup inside
        /// <see cref="IBuildReportConverter"/>, both of which require the main thread.
        /// </remarks>
        /// <returns>
        /// True with <paramref name="sourceBuildAssets"/> set (its asset rows may be empty) when the target has a
        /// recorded source build whose report still loads; false otherwise.
        /// </returns>
        bool TryResolveSourceBuildAssets(BuildReportSummary target, out SourceBuildAssets sourceBuildAssets);
    }

    /// <summary>
    /// Resolves the exact previous build whose data cache an asset-less Player build reused, by following the
    /// pointer the build pipeline stamps into the build's metadata folder
    /// </summary>
    internal sealed class SourceBuildAssetResolver : ISourceBuildAssetResolver
    {
        readonly IBuildHistoryProvider m_BuildHistory;
        readonly IBuildReportConverter m_BuildReportConverter;

        public SourceBuildAssetResolver(IBuildHistoryProvider buildHistory, IBuildReportConverter buildReportConverter)
        {
            m_BuildHistory = buildHistory ?? throw new ArgumentNullException(nameof(buildHistory));
            m_BuildReportConverter = buildReportConverter ?? throw new ArgumentNullException(nameof(buildReportConverter));
        }

        public bool TryResolveSourceBuildAssets(BuildReportSummary target, out SourceBuildAssets sourceBuildAssets)
        {
            sourceBuildAssets = default;

            // Only Player builds source assets from another build. ContentDirectory builds pack their own content.
            if (target.BuildType != BuildType.Player)
                return false;

            // The build pipeline records the exact source build whose content this one reused, on the summary.
            // An empty value means the build produced its own (empty) content. The target's own BuildResult is
            // intentionally not gated: an asset-less Player build shows whatever content it actually reused, the
            // same way a build's own assets are shown regardless of result (the header still flags the failure).
            var sourceGuid = target.ContentSourceBuildSessionGUID;
            if (sourceGuid.Empty() || sourceGuid == target.BuildSessionGUID)
                return false;

            BuildReportSummary sourceSummary;
            try
            {
                sourceSummary = m_BuildHistory.GetBuildSummary(sourceGuid);
            }
            catch (ArgumentException)
            {
                // Source build was pruned from history since the pointer was written.
                return false;
            }

            // Borrow the source recorded, even an empty table - its assets are exactly the content the target reused.
            if (!m_BuildHistory.TryLoadBuildReport(sourceGuid, out var report))
                return false;

            var data = m_BuildReportConverter.Convert(report);
            sourceBuildAssets = new SourceBuildAssets(sourceGuid, sourceSummary, data.Assets);
            return true;
        }
    }
}
