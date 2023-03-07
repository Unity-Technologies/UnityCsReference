// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class UpmVersionList : IVersionList, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<UpmPackageVersion> m_Versions;

        [SerializeField]
        private int m_NumUnloadedVersions;
        public int numUnloadedVersions => m_NumUnloadedVersions;

        public IEnumerable<IPackageVersion> key
        {
            get
            {
                var installed = this.installed;
                var resolvedLifecycleVersion = this.resolvedLifecycleVersion;
                var resolvedLifecycleNextVersion = this.resolvedLifecycleNextVersion;

                // if installed is experimental, return all versions higher than it
                if (installed?.HasTag(PackageTag.Experimental) == true)
                    return m_Versions.Where(v => v == resolvedLifecycleVersion || v == resolvedLifecycleNextVersion
                        || v.version >= installed.version).Cast<IPackageVersion>();

                var recommended = this.recommended;
                var keyVersions = new HashSet<IPackageVersion>();

                if (recommended != null)
                    keyVersions.Add(recommended);

                if (installed != null)
                    keyVersions.Add(installed);

                if (resolvedLifecycleVersion != null)
                {
                    keyVersions.Add(resolvedLifecycleVersion);

                    // if the lifecycle.version is Release Candidate for this Editor version, still need to check if the release version is available
                    //  and add that
                    if (resolvedLifecycleVersion.HasTag(PackageTag.ReleaseCandidate) && resolvedLifecycleVersion.version?.HasPreReleaseVersionTag() == true)
                    {
                        var latestReleasePatchOfUnityLifecycleVersion = m_Versions.LastOrDefault(v => !v.HasTag(PackageTag.PreRelease | PackageTag.ReleaseCandidate | PackageTag.Experimental) &&
                            (v.version?.IsPatchOf(resolvedLifecycleVersion.version) == true || v.version?.IsMajorMinorPatchEqualTo(resolvedLifecycleVersion.version) == true));

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
                if (resolvedLifecycleNextVersion != null)
                    keyVersions.Add(resolvedLifecycleNextVersion);
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
        internal string lifecycleVersionString => m_LifecycleVersionString;

        // the lifeCycle version from the Editor manifest, if it exists
        public IPackageVersion lifecycleVersion
        {
            get
            {
                return m_LifecycleVersion == null ? null : m_Versions.FirstOrDefault(v => !v.HasTag(PackageTag.Custom | PackageTag.Local | PackageTag.Git) && v.version == m_LifecycleVersion);
            }
        }

        // the latest patch of lifecycle version if it exists, or the exact
        //  version if version is set to a pre-release
        private IPackageVersion resolvedLifecycleVersion
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
                    return m_Versions.LastOrDefault(v => !v.HasTag(PackageTag.PreRelease | PackageTag.Experimental) &&
                        ((v.version?.IsPatchOf(m_LifecycleVersion) == true) || v.version == m_LifecycleVersion));
            }
        }

        [SerializeField]
        private string m_LifecycleNextVersionString;
        private SemVersion? m_LifecycleNextVersion;
        internal string lifecycleNextVersion => m_LifecycleNextVersionString;
        private IPackageVersion resolvedLifecycleNextVersion
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

        public IPackageVersion latest => m_Versions.LastOrDefault();

        public IPackageVersion recommended
        {
            get
            {
                var installed = this.installed;
                if (installed != null && installed.HasTag(PackageTag.VersionLocked))
                    return installed;

                if (m_Versions.Any(v => v.availableRegistry != RegistryType.UnityRegistry))
                    return latest;

                // for Unity packages, we should only recommend versions that have been tested with the Editor;
                // this means they have to be either the lifecycle version or nextVersion in the manifest
                // lifecycle version will take precedence over nextVersion
                var resolvedLifecycleVersion = this.resolvedLifecycleVersion;
                var resolvedLifecycleNextVersion = this.resolvedLifecycleNextVersion;

                var recommendedLifecycleVersion = resolvedLifecycleVersion;

                if (installed == null)
                    return recommendedLifecycleVersion
                        ?? resolvedLifecycleNextVersion;
                // nextVersion, since in pre-release, will only be recommended if it's higher than the installed
                else
                {
                    var useNextVersion = resolvedLifecycleNextVersion != null && resolvedLifecycleNextVersion.version > installed.version;
                    return recommendedLifecycleVersion
                        ?? (useNextVersion ? resolvedLifecycleNextVersion : null);
                }
            }
        }

        public IPackageVersion primary => installed ?? recommended ?? latest;

        public IPackageVersion importAvailable => null;

        public bool isNonLifecycleVersionInstalled => CheckIsNonLifecycleVersionInstalled(installed, lifecycleVersion);

        // If the user installs a local, git or embedded package with the same version string as the lifecycle version, it is consider as a non lifecycle version
        // We also consider that installation as the `lifecycle` version. Patches of lifecycle version are also considered lifecycle version.
        // We might change this behaviour later
        internal static bool CheckIsNonLifecycleVersionInstalled(IPackageVersion installed, IPackageVersion lifecycleVersion)
        {
            return installed != null && lifecycleVersion != null && (installed.HasTag(PackageTag.Custom | PackageTag.Git) || installed.version?.IsEqualOrPatchOf(lifecycleVersion.version) != true);
        }

        public bool hasLifecycleVersion => m_LifecycleVersion != null || m_LifecycleNextVersion != null;

        public IPackageVersion GetUpdateTarget(IPackageVersion version)
        {
            if (version?.isInstalled == true && version != recommended)
                return key.LastOrDefault() ?? version;
            return version;
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
                    SemVersion? version;
                    SemVersionParser.TryParse(v, out version);
                    return new UpmPackageVersion(mainInfo, false, version, mainVersion.displayName, availableRegistry);
                }).ToList();
                AddToSortedVersions(m_Versions, mainVersion);

                if (mainInfo != installedInfo && installedInfo != null)
                    AddInstalledVersion(new UpmPackageVersion(installedInfo, true, availableRegistry));
            }
            m_InstalledIndex = m_Versions.FindIndex(v => v.isInstalled);
            var recommendedVersion = mainInfo?.unityLifecycle?.recommendedVersion;
            if (string.IsNullOrEmpty(recommendedVersion))
                recommendedVersion = mainInfo?.unityLifecycle?.version;
            SetLifecycleVersions(recommendedVersion, mainInfo?.unityLifecycle?.nextVersion);
            UpdateExtraPackageInfos(extraVersions, availableRegistry);
            m_NumUnloadedVersions = 0;
        }

        public UpmVersionList(IEnumerable<UpmPackageVersion> versions, string unityLifecycleInfoVersion = null, string unityLifecycleInfoNextVersion = null, int numUnloadedVersions = 0)
        {
            m_Versions = versions?.ToList() ?? new List<UpmPackageVersion>();
            m_InstalledIndex = m_Versions.FindIndex(v => v.isInstalled);
            SetLifecycleVersions(unityLifecycleInfoVersion, unityLifecycleInfoNextVersion);
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

        private void SetLifecycleVersions(string unityLifecycleInfoVersion, string unityLifecycleInfoNextVersion)
        {
            m_LifecycleVersionString = unityLifecycleInfoVersion;
            m_LifecycleNextVersionString = unityLifecycleInfoNextVersion;

            if (!string.IsNullOrEmpty(m_LifecycleVersionString))
                SemVersionParser.TryParse(m_LifecycleVersionString, out m_LifecycleVersion);
            if (!string.IsNullOrEmpty(m_LifecycleNextVersionString))
                SemVersionParser.TryParse(m_LifecycleNextVersionString, out m_LifecycleNextVersion);
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
