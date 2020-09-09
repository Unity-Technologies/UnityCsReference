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
                var installed = this.installed;
                var recommended = this.recommended;
                if (installed != null)
                {
                    if (installed.HasTag(PackageTag.Preview) && !recommended.HasTag(PackageTag.Verified))
                    {
                        var verified = m_Versions.LastOrDefault(v => v.HasTag(PackageTag.Verified));
                        if (verified != null)
                            yield return verified;
                    }
                    yield return installed;
                }

                if (installed != recommended)
                    yield return recommended;
            }
        }

        [SerializeField]
        private int m_InstalledIndex;
        public IPackageVersion installed { get { return m_InstalledIndex < 0 ? null : m_Versions[m_InstalledIndex]; } }

        public IPackageVersion latest => m_Versions.LastOrDefault();

        public IPackageVersion recommended
        {
            get
            {
                var installed = this.installed;
                if (installed != null)
                {
                    if (installed.HasTag(PackageTag.VersionLocked))
                        return installed;

                    var newerVersions = m_Versions.Skip(m_InstalledIndex + 1).SkipWhile(v => v.version <= installed.version);
                    return newerVersions.LastOrDefault(v => v.HasTag(PackageTag.Verified))
                        ?? newerVersions.LastOrDefault(v => v.HasTag(PackageTag.Release) && v.version?.IsPatchOf(installed.version) == true)
                        ?? (installed.HasTag(PackageTag.Preview) ? newerVersions.LastOrDefault(v => v.version?.IsPatchOf(installed.version) == true) : installed)
                        ?? installed;
                }
                else
                {
                    return m_Versions.LastOrDefault(v => v.HasTag(PackageTag.Verified))
                        ?? m_Versions.LastOrDefault(v => v.HasTag(PackageTag.Release))
                        ?? m_Versions.LastOrDefault(v => v.HasTag(PackageTag.Preview))
                        ?? latest;
                }
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

        public UpmVersionList(PackageInfo info, bool isInstalled, bool isUnityPackage)
        {
            var mainVersion = new UpmPackageVersion(info, isInstalled, isUnityPackage);
            m_Versions = info.versions.compatible.Select(v =>
            {
                SemVersion? version;
                SemVersionParser.TryParse(v, out version);
                return new UpmPackageVersion(info, false, version, mainVersion.displayName, isUnityPackage);
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
