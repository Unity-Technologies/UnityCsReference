// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class UpmPackage : IPackage
    {
        private string m_Name;
        public string name { get { return m_Name; } }
        public string uniqueId { get { return m_Name; } }

        public string displayName { get { return m_Versions.First().displayName; } }

        private PackageState m_State;
        public PackageState state { get { return m_State; } }

        private bool m_IsDiscoverable;
        public bool isDiscoverable { get { return m_IsDiscoverable; } }

        private List<UpmPackageVersion> m_Versions;
        public IEnumerable<IPackageVersion> versions { get { return m_Versions.Cast<IPackageVersion>(); } }

        public IEnumerable<IPackageVersion> keyVersions
        {
            get
            {
                // Get key versions -- Latest, Verified, LatestPatch, Installed.
                var keyVersions = new HashSet<IPackageVersion>();

                var installed = installedVersion;
                var latestRelease = m_Versions.LastOrDefault(v => v.HasTag(PackageTag.Release));
                var verifiedVersion = m_Versions.FirstOrDefault(v => v.HasTag(PackageTag.Verified));
                keyVersions.Add(installed);
                keyVersions.Add(latestRelease);
                keyVersions.Add(verifiedVersion);
                keyVersions.Add(latestPatch);
                keyVersions.Add(recommendedVersion);
                if (installed == null && latestRelease == null)
                    keyVersions.Add(latestVersion);
                return keyVersions.Where(v => v != null).OrderBy(package => package.version);
            }
        }

        // keeping the index makes it easier to find newer versions
        private int m_InstalledIndex;
        public IPackageVersion installedVersion { get { return m_InstalledIndex < 0 ? null : m_Versions[m_InstalledIndex]; } }

        public IPackageVersion latestPatch
        {
            get
            {
                if (m_InstalledIndex < 0)
                    return null;

                var installed = m_Versions[m_InstalledIndex].version;
                for (var i = m_Versions.Count - 1; i > m_InstalledIndex; --i)
                {
                    if (m_Versions[i].version.IsPatchOf(installed))
                        return m_Versions[i];
                }
                return null;
            }
        }

        public IPackageVersion latestVersion { get { return m_Versions.Last(); } }

        public IPackageVersion recommendedVersion
        {
            get
            {
                // Override with current when it's version locked
                var installed = installedVersion;
                if (installed != null && installed.isVersionLocked)
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
                return installed ?? latestVersion;
            }
        }

        public IPackageVersion primaryVersion { get { return installedVersion ?? recommendedVersion; } }

        // errors on the package level (not just about a particular version)
        private List<Error> m_Errors;
        public IEnumerable<Error> errors { get { return m_Errors; } }

        public UpmPackage(string name, IEnumerable<UpmPackageVersion> versions, bool isDiscoverable)
        {
            Initialize(name, versions, isDiscoverable);
        }

        public UpmPackage(PackageInfo info, bool isInstalled, bool isDiscoverable)
        {
            var mainVersion = new UpmPackageVersion(info, isInstalled);

            var versions = info.versions.compatible.Select(v => new UpmPackageVersion(info, false, v, mainVersion.displayName)).ToList();
            AddToSortedVersions(versions, mainVersion);
            Initialize(info.name, versions, isDiscoverable);
        }

        private void Initialize(string name, IEnumerable<UpmPackageVersion> versions, bool isDiscoverable)
        {
            m_Name = name;
            m_Versions = versions.ToList();
            m_IsDiscoverable = isDiscoverable;

            m_Errors = new List<Error>();

            SetInstalledVersion(m_Versions.FindIndex(v => v.isInstalled));
        }

        private void SetInstalledVersion(int newInstalledIndex)
        {
            m_InstalledIndex = newInstalledIndex;
            m_State = PackageState.UpToDate;
            if (m_Versions.Any(v => v.errors.Any()))
                m_State = PackageState.Error;
            else if (m_InstalledIndex >= 0 && !recommendedVersion.isInstalled)
                m_State = PackageState.Outdated;
        }

        internal void UpdateVersions(IEnumerable<UpmPackageVersion> updatedVersions)
        {
            Initialize(name, updatedVersions, isDiscoverable);
        }

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

        private static int AddToSortedVersions(List<UpmPackageVersion> sortedVersions, UpmPackageVersion versionToAdd)
        {
            for (var i = 0; i < sortedVersions.Count; ++i)
            {
                if (sortedVersions[i].version.CompareByPrecedence(versionToAdd.version) < 0)
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

        // This function is only used to update the object, not to actually perform the add operation
        public void AddInstalledVersion(UpmPackageVersion newVersion)
        {
            if (m_InstalledIndex >= 0)
            {
                m_Versions[m_InstalledIndex].isInstalled = false;
                if (m_Versions[m_InstalledIndex].HasTag(PackageTag.Git | PackageTag.Local | PackageTag.InDevelopment))
                    m_Versions.RemoveAt(m_InstalledIndex);
            }
            newVersion.isInstalled = true;
            SetInstalledVersion(AddToSortedVersions(m_Versions, newVersion));
        }

        // This function is only used to update the object, not to actually perform the remove operation
        public void RemoveInstalledVersion()
        {
            if (m_InstalledIndex >= 0)
            {
                m_Versions[m_InstalledIndex].isInstalled = false;
                if (m_Versions[m_InstalledIndex].HasTag(PackageTag.Git | PackageTag.Local | PackageTag.InDevelopment))
                    m_Versions.RemoveAt(m_InstalledIndex);
                SetInstalledVersion(-1);
            }
        }

        public void AddError(Error error)
        {
            m_Errors.Add(error);
        }

        public void ClearErrors()
        {
            m_Errors.Clear();
        }
    }
}
