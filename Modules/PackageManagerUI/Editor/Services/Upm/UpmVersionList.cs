// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmVersionList : IVersionList
    {
        [SerializeField]
        private List<UpmPackageVersion> m_Versions;

        public IEnumerable<IPackageVersion> key
        {
            get
            {
                // Get key versions -- Latest, Verified, LatestPatch, Installed.
                var keyVersions = new HashSet<IPackageVersion>();

                var installed = this.installed;
                var latestRelease = m_Versions.LastOrDefault(v => v.HasTag(PackageTag.Release));
                var verifiedVersion = m_Versions.FirstOrDefault(v => v.HasTag(PackageTag.Verified));
                keyVersions.Add(installed);
                keyVersions.Add(latestRelease);
                keyVersions.Add(verifiedVersion);
                keyVersions.Add(latestPatch);
                keyVersions.Add(recommended);
                if (installed == null && latestRelease == null)
                    keyVersions.Add(latest);
                return keyVersions.Where(v => v != null).OrderBy(package => package.version);
            }
        }

        [SerializeField]
        private int m_InstalledIndex;
        public IPackageVersion installed { get { return m_InstalledIndex < 0 ? null : m_Versions[m_InstalledIndex]; } }

        public IPackageVersion latestPatch
        {
            get
            {
                if (m_InstalledIndex < 0)
                    return null;

                var installed = m_Versions[m_InstalledIndex].version;
                for (var i = m_Versions.Count - 1; i > m_InstalledIndex; --i)
                {
                    if (m_Versions[i].version?.IsPatchOf(installed) == true)
                        return m_Versions[i];
                }
                return null;
            }
        }

        public IPackageVersion latest => m_Versions.Last();

        public IPackageVersion recommended
        {
            get
            {
                // Override with current when it's version locked
                var installed = this.installed;
                if (installed?.HasTag(PackageTag.VersionLocked) ?? false)
                    return installed;

                // Only try to find recommended version in versions newer than the installed version
                var newerVersions = installed == null ? m_Versions : m_Versions.Skip(m_InstalledIndex + 1).SkipWhile(v => v.version <= installed.version);

                var verifiedVersion = newerVersions.FirstOrDefault(v => v.HasTag(PackageTag.Verified));
                if (verifiedVersion != null)
                    return verifiedVersion;

                var latestRelease = newerVersions.LastOrDefault(v => v.HasTag(PackageTag.Release));
                if (latestRelease != null && (installed == null || !installed.HasTag(PackageTag.Verified)))
                    return latestRelease;

                var latestPreview = newerVersions.LastOrDefault(package => package.HasTag(PackageTag.Preview));
                if (latestPreview != null && (installed == null || installed.HasTag(PackageTag.Preview)))
                    return latestPreview;

                // Show current if it exists, otherwise latest user visible, and then otherwise show the absolute latest
                return installed ?? latest;
            }
        }

        public IPackageVersion primary => installed ?? recommended;

        public IPackageVersion importAvailable => null;

        internal void UpdateVersion(UpmPackageVersion version)
        {
            for (var i = 0; i < m_Versions.Count; ++i)
            {
                if (m_Versions[i].uniqueId != version.uniqueId)
                    continue;
                m_Versions[i] = version;
                return;
            }
        }

        // This function is only used to update the object, not to actually perform the add operation
        public void AddInstalledVersion(UpmPackageVersion newVersion)
        {
            if (m_InstalledIndex >= 0)
            {
                m_Versions[m_InstalledIndex].SetInstalled(false);
                if (m_Versions[m_InstalledIndex].installedFromPath)
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
                // two upm package versions could have the the same version but different package id
                if (sortedVersions[i].uniqueId == versionToAdd.uniqueId)
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

        public UpmVersionList(IEnumerable<UpmPackageVersion> versions = null)
        {
            m_Versions = versions?.ToList() ?? new List<UpmPackageVersion>();
            m_InstalledIndex = m_Versions.FindIndex(v => v.isInstalled);
        }

        public UpmVersionList(PackageInfo info, bool isInstalled)
        {
            var mainVersion = new UpmPackageVersion(info, isInstalled);
            m_Versions = info.versions.compatible.Select(v =>
            {
                SemVersion? version;
                SemVersionParser.TryParse(v, out version);
                return new UpmPackageVersion(info, false, version, mainVersion.displayName);
            }).ToList();

            AddToSortedVersions(m_Versions, mainVersion);

            m_InstalledIndex = m_Versions.FindIndex(v => v.isInstalled);
        }

        public IEnumerator<IPackageVersion> GetEnumerator()
        {
            return m_Versions.Cast<IPackageVersion>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Versions.GetEnumerator();
        }
    }
}
