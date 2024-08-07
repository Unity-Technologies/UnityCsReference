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

        public override IEnumerable<IPackageVersion> key
        {
            get
            {
                var installedVersion = installed;
                var recommendedVersion = recommended;

                // if installed is experimental, return all versions higher than it
                if (installedVersion != null && installedVersion.HasTag(PackageTag.Experimental))
                    return m_Versions.Where(v => v == recommendedVersion || v.version >= installedVersion.version);

                var keyVersions = new HashSet<IPackageVersion>();

                if (installedVersion != null)
                    keyVersions.Add(installedVersion);

                keyVersions.Add(recommendedVersion ?? latest);

                var suggestedUpdateVersion = suggestedUpdate;
                if (suggestedUpdateVersion != null)
                    keyVersions.Add(suggestedUpdateVersion);

                return keyVersions.OrderBy(v => v.version);
            }
        }

        [SerializeField]
        private int m_InstalledIndex;
        public override IPackageVersion installed => m_InstalledIndex < 0 ? null : m_Versions[m_InstalledIndex];

        [SerializeField]
        private int m_RecommendedIndex;
        public override IPackageVersion recommended => m_RecommendedIndex < 0 ? null : m_Versions[m_RecommendedIndex];

        public override IPackageVersion suggestedUpdate
        {
            get
            {
                var installedVersion = installed;
                if (installedVersion == null || installedVersion.HasTag(PackageTag.VersionLocked | PackageTag.InstalledFromPath))
                    return null;

                if (installedVersion.HasTag(PackageTag.Experimental) || m_Versions.Any(v => v.availableRegistry != RegistryType.UnityRegistry))
                    return latest.isInstalled ? null : latest;

                var recommendedVersion = recommended;
                if (recommendedVersion is { isInstalled: false } && recommendedVersion.version >= installedVersion.version)
                    return recommendedVersion;

                var latestSafePatch = GetLatestSafePatch(installedVersion);
                return latestSafePatch is { isInstalled: true } ? null : latestSafePatch;
            }
        }

        // A "safe" patch is a patch that is not lower in safety level (regarding prerelease) compared to the original version. For example,
        // `1.0.1` is a safe patch for both `1.0.0-pre.0` and `1.0.0`, however
        // `1.0.1-pre.1` is NOT a safe patch for `1.0.0` but can be considered a safe patch for `1.0.0-pre.0`
        private IPackageVersion GetLatestSafePatch(IPackageVersion version)
        {
            if (version == null)
                return null;
            var availableVersions = m_Versions.Where(i => i != version && !i.HasTag(PackageTag.InstalledFromPath)).ToArray();
            if (availableVersions.Length == 0)
                return null;
            var patchVersionString = SemVersionHelper.MaxSatisfying(version.versionString, availableVersions.Select(v => v.versionString).ToArray(), ResolutionStrategy.HighestPatch, !string.IsNullOrEmpty(version.version?.Prerelease));
            return string.IsNullOrEmpty(patchVersionString) ? null : availableVersions.LastOrDefault(v => v.versionString == patchVersionString);
        }

        public override IPackageVersion latest => m_Versions.LastOrDefault();

        public override IPackageVersion primary => installed ?? recommended ?? latest;

        public override IPackageVersion GetUpdateTarget(IPackageVersion version)
        {
            return version?.isInstalled == true ? suggestedUpdate ?? version : version;
        }

        // This function is only used to update the object, not to actually perform the add operation
        public void AddInstalledVersion(UpmPackageVersion newVersion)
        {
            if (m_InstalledIndex >= 0)
            {
                m_Versions[m_InstalledIndex].SetInstalled(false);
                if (m_Versions[m_InstalledIndex].HasTag(PackageTag.InstalledFromPath))
                    m_Versions.RemoveAt(m_InstalledIndex);
            }
            newVersion.SetInstalled(true);
            m_InstalledIndex = AddToSortedVersions(m_Versions, newVersion);
        }

        private static int AddToSortedVersions(List<UpmPackageVersion> sortedVersions, UpmPackageVersion versionToAdd)
        {
            for (var i = 0; i < sortedVersions.Count; ++i)
            {
                if (versionToAdd.version != null && (sortedVersions[i].version?.CompareTo(versionToAdd.version) ?? -1) < 0)
                    continue;
                // note that the difference between this and the previous function is that
                // two upm package versions could have the same version but different package id
                if (sortedVersions[i].packageId == versionToAdd.packageId)
                {
                    sortedVersions[i] = versionToAdd;
                    return i;
                }
                sortedVersions.Insert(i, versionToAdd);
                return i;
            }
            sortedVersions.Add(versionToAdd);
            return sortedVersions.Count - 1;
        }

        public UpmVersionList(PackageInfo searchInfo, PackageInfo installedInfo, RegistryType availableRegistry, Dictionary<string, PackageInfo> extraVersions = null)
        {
            // We prioritize searchInfo over installedInfo, because searchInfo is fetched from the server
            // while installedInfo sometimes only contain local data
            var mainInfo = searchInfo ?? installedInfo;
            if (mainInfo != null)
            {
                var mainVersion = new UpmPackageVersion(mainInfo, mainInfo == installedInfo, availableRegistry);
                m_Versions = mainInfo.versions.compatible.Select(v =>
                {
                    SemVersionParser.TryParse(v, out var version);
                    return new UpmPackageVersion(mainInfo, false, version, mainVersion.displayName, availableRegistry);
                }).ToList();
                AddToSortedVersions(m_Versions, mainVersion);

                if (mainInfo != installedInfo && installedInfo != null)
                    AddInstalledVersion(new UpmPackageVersion(installedInfo, true, availableRegistry));
            }
            m_InstalledIndex = m_Versions?.FindIndex(v => v.isInstalled) ?? -1;
            SetRecommendedVersion(mainInfo?.versions.recommended);
            UpdateExtraPackageInfos(extraVersions, availableRegistry);
            m_NumUnloadedVersions = 0;
        }

        public UpmVersionList(IEnumerable<UpmPackageVersion> versions, string recommendedVersionString = null, int numUnloadedVersions = 0)
        {
            m_Versions = versions?.ToList() ?? new List<UpmPackageVersion>();
            m_InstalledIndex = m_Versions.FindIndex(v => v.isInstalled);
            SetRecommendedVersion(recommendedVersionString);
            m_NumUnloadedVersions = numUnloadedVersions;
        }

        private void UpdateExtraPackageInfos(Dictionary<string, PackageInfo> extraVersions, RegistryType availableRegistry)
        {
            if (extraVersions?.Any() != true)
                return;
            foreach (var version in m_Versions.Where(v => !v.isFullyFetched))
                if (extraVersions.TryGetValue(version.version.ToString(), out var packageInfo))
                    version.UpdatePackageInfo(packageInfo, availableRegistry);
        }

        private void SetRecommendedVersion(string versionString)
        {
            if (!string.IsNullOrEmpty(versionString))
                m_RecommendedIndex = m_Versions.FindIndex(v => v.HasTag(PackageTag.Unity) && v.versionString == versionString);
            else
                m_RecommendedIndex = -1;
        }

        public override IEnumerator<IPackageVersion> GetEnumerator()
        {
            return m_Versions.Cast<IPackageVersion>().GetEnumerator();
        }
    }
}
