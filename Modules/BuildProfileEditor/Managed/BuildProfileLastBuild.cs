// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.IO;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Data the build-profile header needs to render the "Last build" line.
    /// </summary>
    internal struct LastBuildHeaderInfo
    {
        public string resultIconClass;
        public string label;
        public GUID buildSessionGuid;
    }

    /// <summary>
    /// Looks up the most recent build for a build profile from the build history. Provided
    /// as the data source for the header's "Last build" line.
    /// </summary>
    static class BuildProfileLastBuild
    {
        internal const string k_IconSuccessClass = "last-build-result-icon--success";
        internal const string k_IconFailedClass = "last-build-result-icon--failed";

        /// <summary>
        /// Returns the most recent build for the given profile asset, or null when none
        /// exists. Matches on the profile's current asset path against the path recorded in
        /// each build; a profile moved or renamed after its last build will not match.
        /// </summary>
        internal static LastBuildHeaderInfo? GetLatestForProfile(BuildProfile profile)
        {
            if (profile == null
                || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(profile, out var assetGuid, out long _))
                return null;

            var profilePath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (string.IsNullOrEmpty(profilePath))
                return null;

            foreach (var buildSessionGuid in BuildHistory.GetAllBuilds())
            {
                var summary = BuildHistory.GetBuildSummary(buildSessionGuid);
                if (summary.BuildProfilePath != profilePath)
                    continue;

                return new LastBuildHeaderInfo
                {
                    resultIconClass = GetResultIconClass(summary.BuildResult),
                    label = FormatBuildTime(summary.BuildStartedAt),
                    buildSessionGuid = summary.BuildSessionGUID
                };
            }

            return null;
        }

        static string GetResultIconClass(BuildResult result) => result switch
        {
            BuildResult.Succeeded => k_IconSuccessClass,
            BuildResult.Failed or BuildResult.Cancelled => k_IconFailedClass,
            _ => null
        };

        static string FormatBuildTime(string buildStartedAt)
        {
            if (!DateTime.TryParse(buildStartedAt, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var startedAt))
                return TrText.lastBuildUnknownDate;

            return startedAt.ToLocalTime().ToString("MM'/'dd'/'yyyy • HH:mm");
        }
    }
}
