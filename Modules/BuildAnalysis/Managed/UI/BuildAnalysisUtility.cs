// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    /// <summary>Opens the <b>Build Analysis</b> window from script.</summary>
    /// <seealso cref="BuildHistory"/>
    public static class BuildAnalysisUtility
    {
        /// <summary>Opens the <b>Build Analysis</b> window and selects a build from the build history.</summary>
        /// <param name="buildSessionGuid">The build session GUID of the build to select, for example
        /// <see cref="Reporting.BuildSummary.buildSessionGuid"/> or a GUID returned by <see cref="BuildHistory.GetAllBuilds"/>. Passing an empty GUID throws an <see cref="ArgumentException"/>.</param>
        /// <remarks>Use this method to show a build right after it completes.
        /// If the build history contains no build with this GUID, the window still opens, keeps its current
        /// selection, and logs a warning to the Console.</remarks>
        /// <example>
        /// <code source="../Tests/UTFTests/Editor/ReferenceExamples/OpenWindowExample.cs"/>
        /// </example>
        public static void OpenWindow(GUID buildSessionGuid)
        {
            if (buildSessionGuid.Empty())
                throw new ArgumentException("The build session GUID is empty.", nameof(buildSessionGuid));

            BuildAnalysisWindow.ShowWindow(buildSessionGuid);
        }

        // For the Addressables drawer's Related Builds row, which selects a build with OpenWindow.
        internal static IReadOnlyList<BuildReportSummary> GetRelatedBuilds(GUID buildSessionGuid)
        {
            return GetRelatedBuilds(new BuildHistoryProvider(), buildSessionGuid);
        }

        // Ancestors first, in the same order as the window's own Related Builds row.
        internal static IReadOnlyList<BuildReportSummary> GetRelatedBuilds(IBuildHistoryProvider history, GUID buildSessionGuid)
        {
            var builds = history.GetAllBuilds();
            var relationships = BuildRelationships.FromHistory(history, builds);
            var listedBuilds = new HashSet<GUID>(builds);

            var related = new List<BuildReportSummary>();
            foreach (var guid in relationships.GetRelated(buildSessionGuid))
            {
                // A child keeps its parent's GUID after the parent is deleted.
                if (listedBuilds.Contains(guid))
                    related.Add(history.GetBuildSummary(guid));
            }

            return related;
        }
    }
}
