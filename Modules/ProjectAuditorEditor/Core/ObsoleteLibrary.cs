// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.UI.Framework;
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace Unity.ProjectAuditor.Editor.Core
{
    static class ObsoleteLibrary
    {
#pragma warning disable CS0649

        [Serializable]
        private sealed class SerializedApi
        {
            public string type;
            public string description;
            public string warningSince;
            public string errorSince;
            public string removedIn;
        }

        [Serializable]
        private class SerializedApiCollection
        {
            public SerializedApi[] items;
        }

#pragma warning restore CS0649

        [NoAutoStaticsCleanup] // Lazy cache populated from disk; code reload does not invalidate these
        static Dictionary<string, ReportItem> s_LibraryDictionary;
        [NoAutoStaticsCleanup] // Lazy cache populated from disk; code reload does not invalidate these
        static List<ReportItem> s_LibraryList;
        [NoAutoStaticsCleanup] // Lazy cache populated from disk; code reload does not invalidate these
        static string[] s_UnityVersions;

        internal static IReadOnlyDictionary<string, ReportItem> LibraryDictionary
        {
            get
            {
                if (s_LibraryDictionary == null)
                    ReadFromDisk();
                return s_LibraryDictionary;
            }
        }

        internal static IReadOnlyList<ReportItem> LibraryList
        {
            get
            {
                if (s_LibraryList == null)
                    ReadFromDisk();
                return s_LibraryList;
            }
        }

        // If the running Unity version is so new that we don't have any information about
        // future versions in our database, then this check will return false
        internal static bool HasAnyUpgradeVersions => UnityVersions.Length > 0;

        // True if the issue is relevant when upgrading to the given target Unity version. Upgrade
        // issues match only when the target falls within their [MinVersion, MaxVersion) range (an
        // empty MaxVersion means "still applies in all later versions").
        internal static bool MatchesTargetVersion(ReportItem issue, string targetVersion)
        {
            if (!issue.IsUpgradeIssue)
                return true;

            // If there are no upgrade versions, fall back to the latest
            if (string.IsNullOrEmpty(targetVersion))
            {
                if (UnityVersions.Length > 0)
                    targetVersion = UnityVersions[^1];
                else
                    targetVersion = Application.unityVersion;
            }

            var targetVersionInt = Utility.VersionToInt(targetVersion);

            var since = issue.UpgradeProperties[(int)UpgradeProperties.MinVersion];
            var until = issue.UpgradeProperties[(int)UpgradeProperties.MaxVersion];

            var sinceInt = string.IsNullOrEmpty(since) ? int.MinValue : Utility.VersionToInt(since);
            var untilInt = string.IsNullOrEmpty(until) ? int.MaxValue : Utility.VersionToInt(until);

            return sinceInt <= targetVersionInt && untilInt > targetVersionInt;
        }

        internal static string[] UnityVersions
        {
            get
            {
                if (s_UnityVersions == null)
                    ReadFromDisk();
                return s_UnityVersions;
            }
        }

        static void ReadFromDisk()
        {
            if (!ProjectAuditorRulesPackage.IsInstalled)
                throw new InvalidOperationException("Install the Project Auditor Rules package before using Project Auditor");

            var path = Path.Combine(ProjectAuditor.s_RulesDataPath, "ObsoleteDatabase.gen.json");

            // TEMP: support both paths while we migrate to new name
            string filename = File.Exists(path) ? path : Path.Combine(ProjectAuditor.s_RulesDataPath, "ObsoleteDatabase.json");

            var json = File.ReadAllText(filename);
            var obsoleteApi = JsonUtility.FromJson<SerializedApiCollection>(json).items;
            var uniqueVersions = new HashSet<string>();

            s_LibraryList = new List<ReportItem>(obsoleteApi.Length);
            foreach (var api in obsoleteApi)
            {
                var earliestVersion = api.warningSince;
                if (string.IsNullOrEmpty(earliestVersion))
                    earliestVersion = api.errorSince;
                if (string.IsNullOrEmpty(earliestVersion))
                    earliestVersion = api.removedIn;

                s_LibraryList.Add(new ReportItemBuilder(IssueCategory.ObsoleteAPI, api.type).WithCustomProperties(
                [
                    api.description,
                    api.description.Contains("(UnityUpgradable)"),
                    api.warningSince,
                    api.errorSince,
                    api.removedIn,
                    earliestVersion
                ]));

                if (!string.IsNullOrEmpty(api.warningSince))
                    uniqueVersions.Add(api.warningSince);
                if (!string.IsNullOrEmpty(api.errorSince))
                    uniqueVersions.Add(api.errorSince);
                if (!string.IsNullOrEmpty(api.removedIn))
                    uniqueVersions.Add(api.removedIn);
            }

            s_LibraryDictionary = new Dictionary<string, ReportItem>(obsoleteApi.Length);
            foreach (var reportItem in s_LibraryList)
                s_LibraryDictionary.Add(reportItem.Description, reportItem);

            var currentVersion = Utility.VersionToInt(Application.unityVersion);

            var unityVersions = new List<string>(uniqueVersions.Count + 1);
            foreach (var version in uniqueVersions)
            {
                var versionInt = Utility.VersionToInt(version);
                if (versionInt >= currentVersion)
                    unityVersions.Add(version);
            }

            s_UnityVersions = new string[unityVersions.Count];
            unityVersions.CopyTo(s_UnityVersions);

            if (s_UnityVersions.Length > 0)
                Array.Sort(s_UnityVersions, (a, b) => Utility.VersionToInt(a).CompareTo(Utility.VersionToInt(b)));
        }
    }
}
