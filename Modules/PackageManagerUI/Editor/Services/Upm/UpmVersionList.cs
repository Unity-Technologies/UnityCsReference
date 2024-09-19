// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmVersionList : BaseVersionList
    {
        [SerializeField]
        private List<UpmPackageVersion> m_Versions;

        [SerializeField]
        private int m_NumUnloadedVersions;
        public override int numUnloadedVersions => m_NumUnloadedVersions;

        [SerializeField]
        private int m_InstalledIndex;
        public override IPackageVersion installed => m_InstalledIndex < 0 ? null : m_Versions[m_InstalledIndex];

        [SerializeField]
        private int m_RecommendedIndex;
        public override IPackageVersion recommended => m_RecommendedIndex < 0 ? null : m_Versions[m_RecommendedIndex];

        [SerializeField]
        private int m_SuggestedUpdateIndex;
        public override IPackageVersion suggestedUpdate => m_SuggestedUpdateIndex < 0 ? null : m_Versions[m_SuggestedUpdateIndex];

        public override IPackageVersion latest => m_Versions.LastOrDefault();

        public override IPackageVersion primary => installed ?? recommended ?? latest;

        private static UpmPackageVersion FindSuggestedUpdate(List<UpmPackageVersion> sortedVersions, UpmPackageVersion installedVersion, UpmPackageVersion recommendedVersion)
        {
            if (installedVersion == null || installedVersion.HasTag(PackageTag.InstalledFromPath) || sortedVersions.Count <= 1)
                return null;

            if (installedVersion.HasTag(PackageTag.Experimental) || sortedVersions.Any(v => v.availableRegistry != RegistryType.UnityRegistry))
                return sortedVersions.Last().isInstalled ? null : sortedVersions.Last();

            if (recommendedVersion is { isInstalled: false } && recommendedVersion.version >= installedVersion.version)
                return recommendedVersion;

            var latestSafePatch = GetLatestSafePatch(sortedVersions, installedVersion);
            return latestSafePatch is { isInstalled: true } ? null : latestSafePatch;
        }

        // A "safe" patch is a patch that is not lower in safety level (regarding prerelease) compared to the original version. For example,
        // `1.0.1` is a safe patch for both `1.0.0-pre.0` and `1.0.0`, however
        // `1.0.1-pre.1` is NOT a safe patch for `1.0.0` but can be considered a safe patch for `1.0.0-pre.0`
        private static UpmPackageVersion GetLatestSafePatch(IEnumerable<UpmPackageVersion> sortedVersions, UpmPackageVersion version)
        {
            var availableVersions = sortedVersions.Where(i => i != version && !i.HasTag(PackageTag.InstalledFromPath)).ToArray();
            if (availableVersions.Length == 0)
                return null;
            var patchVersionString = SemVersionHelper.MaxSatisfying(version.versionString, availableVersions.Select(v => v.versionString).ToArray(), ResolutionStrategy.HighestPatch, !string.IsNullOrEmpty(version.version?.Prerelease));
            return string.IsNullOrEmpty(patchVersionString) ? null : availableVersions.LastOrDefault(v => v.versionString == patchVersionString);
        }

        private static void AddToSortedVersions(List<UpmPackageVersion> sortedVersions, UpmPackageVersion versionToAdd)
        {
            if (versionToAdd == null)
                return;

            for (var i = 0; i < sortedVersions.Count; ++i)
            {
                if (sortedVersions[i].version < versionToAdd.version)
                    continue;

                // Two upm package versions could have the same version but different package id, when one is installed from path for instance
                // We only want to overwrite when the package id is exactly the same (either both from the registry, or from the same path)
                if (sortedVersions[i].packageId == versionToAdd.packageId)
                {
                    sortedVersions[i] = versionToAdd;
                    return;
                }
                sortedVersions.Insert(i, versionToAdd);
                return;
            }
            sortedVersions.Add(versionToAdd);
        }

        public UpmVersionList(PackageInfo searchInfo, PackageInfo installedInfo, RegistryType availableRegistry, PackageTag tagsToExclude, bool loadAllVersions, Dictionary<string, PackageInfo> extraInfos = null)
        {
            // We prioritize searchInfo over installedInfo, because searchInfo is fetched from the server
            // while installedInfo sometimes only contain local data
            var mainInfo = searchInfo ?? installedInfo;
            var allSortedVersions = mainInfo.versions.compatible.Select(versionString =>
            {
                SemVersionParser.TryParse(versionString, out var parsedVersion);
                return new UpmPackageVersion(mainInfo, false, parsedVersion, mainInfo.displayName, availableRegistry);
            }).ToList();

            var installedVersion = installedInfo != null ? new UpmPackageVersion(installedInfo, true, availableRegistry) : null;
            if (installedVersion != null)
            {
                AddToSortedVersions(allSortedVersions, installedVersion);
                if (installedVersion.HasTag(PackageTag.Experimental))
                    tagsToExclude &= ~(PackageTag.Experimental | PackageTag.PreRelease);
                else if (installedVersion.HasTag(PackageTag.PreRelease))
                    tagsToExclude &= ~PackageTag.PreRelease;
            }

            var versionsToKeep = allSortedVersions.Where(version => version.isInstalled || !version.HasTag(tagsToExclude)).ToList();

            // Since a purchased Upm Package on AssetStore will always be visible when users are on the "My Assets" page,
            // we are adding a special handling here to at least show one version of the Upm package even if that version
            // does not match the filtering criteria.
            // For example, if an Upm package from the Asset Store only contains `Pre-release` versions, but in project
            // settings "Show Pre-release versions" is not checked, we will only show the latest `Pre-release` version
            // for the Upm package because we can't hide the whole package.
            if (versionsToKeep.Count == 0 && allSortedVersions.Count > 0 && availableRegistry == RegistryType.AssetStore)
                versionsToKeep.Add(allSortedVersions.Last());

            var recommendedVersion = versionsToKeep.Find(v => v.HasTag(PackageTag.Unity) && v.versionString == mainInfo.versions.recommended);
            var suggestedUpdateVersion = FindSuggestedUpdate(versionsToKeep, installedVersion, recommendedVersion);
            var numVersionsBeforeUnload = versionsToKeep.Count;

            if (!loadAllVersions && versionsToKeep.Count > 1)
            {
                // We want to trim down the amount of versions we keep in memory to a list of key versions (the installed version, suggested update,
                // recommended version or the latest version if recommended doesn't exist). We do this to save on memory, and also to avoid long
                // domain reload time, as each version in the memory will need to be serialized on domain reload.
                var recommendedOrLatest = recommendedVersion ?? versionsToKeep.Last();
                versionsToKeep.Clear();
                AddToSortedVersions(versionsToKeep, installedVersion);
                AddToSortedVersions(versionsToKeep, recommendedOrLatest);
                AddToSortedVersions(versionsToKeep, suggestedUpdateVersion);
            }

            foreach (var version in versionsToKeep)
                if (!version.isFullyFetched && extraInfos?.TryGetValue(version.versionString, out var packageInfo) == true)
                    version.UpdatePackageInfo(packageInfo, availableRegistry);

            m_Versions = versionsToKeep;
            m_InstalledIndex = installedVersion != null ? m_Versions.FindIndex(v => v == installedVersion) : -1;
            m_RecommendedIndex = recommendedVersion != null ? m_Versions.FindIndex(v => v == recommendedVersion) : -1;
            m_SuggestedUpdateIndex = suggestedUpdateVersion != null ? m_Versions.FindIndex(v => v == suggestedUpdateVersion) : -1;
            m_NumUnloadedVersions = numVersionsBeforeUnload - versionsToKeep.Count;
        }

        public override IEnumerator<IPackageVersion> GetEnumerator()
        {
            return m_Versions.Cast<IPackageVersion>().GetEnumerator();
        }
    }
}
