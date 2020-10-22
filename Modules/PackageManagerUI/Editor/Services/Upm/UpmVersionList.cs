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
    internal class UpmVersionList : IVersionList, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<UpmPackageVersion> m_Versions;

        public IEnumerable<IPackageVersion> key
        {
            get
            {
                var installed = this.installed;
                var actualLifecycleVersion = this.actualLifecycleVersion;
                var actualLifecycleNextVersion = this.actualLifecycleNextVersion;

                // if installed is experimental, return all versions higher than it
                if (installed?.HasTag(PackageTag.Experimental) == true)
                    return m_Versions.Where(v => v == actualLifecycleVersion || v == actualLifecycleNextVersion
                        || v.version >= installed.version).Cast<IPackageVersion>();

                var recommended = this.recommended;
                var keyVersions = new HashSet<IPackageVersion>();

                if (recommended != null)
                    keyVersions.Add(recommended);

                if (installed != null)
                    keyVersions.Add(installed);

                if (actualLifecycleVersion != null)
                {
                    keyVersions.Add(actualLifecycleVersion);

                    // if the lifecycle.version is Release Candidate for this Editor version, still need to check if the release version is available
                    //  and add that
                    if (actualLifecycleVersion.HasTag(PackageTag.ReleaseCandidate) && actualLifecycleVersion.version?.HasPreReleaseVersionTag() == true)
                    {
                        var latestReleasePatchOfUnityLifecycleVersion = m_Versions.LastOrDefault(v => v.HasTag(PackageTag.Release) &&
                            (v.version?.IsPatchOf(actualLifecycleVersion.version) == true || v.version?.IsMajorMinorPatchEqualTo(actualLifecycleVersion.version) == true));

                        if (latestReleasePatchOfUnityLifecycleVersion != null)
                            keyVersions.Add(latestReleasePatchOfUnityLifecycleVersion);
                    }
                }
                // if no version is set but installed is release, check if there exists a patch of it to add
                else if (installed?.HasTag(PackageTag.Release) == true)
                {
                    var latestReleasePatchOfInstalled = m_Versions.LastOrDefault(v => v.HasTag(PackageTag.Release)
                        && v.version?.IsPatchOf(installed.version) == true);

                    if (latestReleasePatchOfInstalled != null)
                        keyVersions.Add(latestReleasePatchOfInstalled);
                }

                // now add the proper Pre-Release version to key versions; latestUnityLifecycleNextVersion takes priority
                if (actualLifecycleNextVersion != null)
                    keyVersions.Add(actualLifecycleNextVersion);
                // if nextVersion is not set but the installed version is Pre-Release, add the latest iteration on it to key versions
                else if (installed?.HasTag(PackageTag.PreRelease) == true)
                {
                    var keyPreRelease = m_Versions.LastOrDefault(v => v.HasTag(PackageTag.PreRelease)
                        && (v.version?.IsHigherPreReleaseIterationOf(installed.version) == true));

                    if (keyPreRelease != null)
                        keyVersions.Add(keyPreRelease);
                }

                if (!keyVersions.Any())
                    keyVersions.Add(primary);

                return keyVersions.OrderBy(v => v.version);
            }
        }

        [SerializeField]
        private int m_InstalledIndex;
        public IPackageVersion installed { get { return m_InstalledIndex < 0 ? null : m_Versions[m_InstalledIndex]; } }

        [SerializeField]
        private string m_LifecycleVersionString;
        private SemVersion? m_LifecycleVersion;
        internal string lifecycleVersion => m_LifecycleVersionString;
        // the latest patch of lifecycle version if it exists, or the exact
        //  version if version is set to a pre-release
        private IPackageVersion actualLifecycleVersion
        {
            get
            {
                // if it has a -pre tag in the version, it's a Release Candidate, and we must return the exact version which matches, not a
                //  higher patch or iteration
                if (m_LifecycleVersion?.HasPreReleaseVersionTag() == true)
                    return m_Versions.LastOrDefault(v => v.HasTag(PackageTag.ReleaseCandidate) &&
                        v.version == m_LifecycleVersion);
                // otherwise, it's either Release or tagged as Release Candidate because Editor is in Alpha or Beta, and we should
                //  take the latest patch of it
                else
                    return m_Versions.LastOrDefault(v => v.HasTag(PackageTag.Release | PackageTag.ReleaseCandidate) &&
                        ((v.version?.IsPatchOf(m_LifecycleVersion) == true) || v.version == m_LifecycleVersion));
            }
        }

        [SerializeField]
        private string m_LifecycleNextVersionString;
        private SemVersion? m_LifecycleNextVersion;
        internal string lifecycleNextVersion => m_LifecycleNextVersionString;
        private IPackageVersion actualLifecycleNextVersion
        {
            get
            {
                return m_Versions.LastOrDefault(v => v.HasTag(PackageTag.PreRelease) &&
                    (v.version?.IsHigherPreReleaseIterationOf(m_LifecycleNextVersion) == true || v.version == m_LifecycleNextVersion));
            }
        }

        public void OnBeforeSerialize()
        {
            // do nothing
        }

        public void OnAfterDeserialize()
        {
            SemVersionParser.TryParse(m_LifecycleVersionString, out m_LifecycleVersion);
            SemVersionParser.TryParse(m_LifecycleNextVersionString, out m_LifecycleNextVersion);
        }

        private bool isUnityPackage => m_Versions.All(v => v.isUnityPackage);

        public IPackageVersion latest => m_Versions.LastOrDefault();

        public IPackageVersion recommended
        {
            get
            {
                var installed = this.installed;

                if (installed != null && installed.HasTag(PackageTag.VersionLocked))
                    return installed;

                if (!isUnityPackage)
                    return latest;

                // for Unity packages, we should only recommend versions that have been tested with
                //  the Editor; this means they have to be either version or nextVersion in the manifest
                // version will take precedence over nextVersion
                var actualLifecycleVersion = this.actualLifecycleVersion;
                var actualLifecycleNextVersion = this.actualLifecycleNextVersion;

                var recommendedLifecycleVersion = actualLifecycleVersion;

                if (installed == null)
                    return recommendedLifecycleVersion
                        ?? actualLifecycleNextVersion;
                // nextVersion, since in pre-release, will only be recommended if it's higher than the installed
                else
                {
                    var useNextVersion = actualLifecycleNextVersion != null && actualLifecycleNextVersion.version > installed.version;
                    return recommendedLifecycleVersion
                        ?? (useNextVersion ? actualLifecycleNextVersion : null)
                        ?? installed;
                }
            }
        }

        public IPackageVersion primary => installed ?? recommended ?? latest;

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

        public UpmVersionList(IEnumerable<UpmPackageVersion> versions = null, string unityLifecycleInfoVersion = null, string unityLifecycleInfoNextVersion = null)
        {
            m_Versions = versions?.ToList() ?? new List<UpmPackageVersion>();
            m_InstalledIndex = m_Versions.FindIndex(v => v.isInstalled);

            m_LifecycleVersionString = unityLifecycleInfoVersion;
            m_LifecycleNextVersionString = unityLifecycleInfoNextVersion;

            if (m_LifecycleVersionString != null)
                SemVersionParser.TryParse(m_LifecycleVersionString, out m_LifecycleVersion);
            if (m_LifecycleNextVersionString != null)
                SemVersionParser.TryParse(m_LifecycleNextVersionString, out m_LifecycleNextVersion);
        }

        public UpmVersionList(PackageInfo info, bool isInstalled, bool isUnityPackage)
        {
            m_LifecycleVersionString = info.unityLifecycle?.version;
            m_LifecycleNextVersionString = info.unityLifecycle?.nextVersion;

            if (m_LifecycleVersionString != null)
                SemVersionParser.TryParse(m_LifecycleVersionString, out m_LifecycleVersion);
            if (m_LifecycleNextVersionString != null)
                SemVersionParser.TryParse(m_LifecycleNextVersionString, out m_LifecycleNextVersion);

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
