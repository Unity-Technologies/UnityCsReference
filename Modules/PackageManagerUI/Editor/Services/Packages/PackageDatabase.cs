// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;
using Unity.Collections;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPackageDatabase : IService
    {
        event Action<string, string> onPackageUniqueIdFinalize;
        event Action<PackagesChangeArgs> onPackagesChanged;
        event Action<SamplesChangeArgs> onSamplesChanged;

        IReadOnlyCollection<IPackage> allPackages { get; }
        IReadOnlyCollection<SampleCollection> sampleCollections { get; }

        IPackage GetPackage(string uniqueId);
        IPackage GetPackage(long productId);
        IPackage GetPackageByIdOrName(string packageIdOrName);
        IPackage GetPackageByDisplayName(string displayName);
        void GetPackageAndVersion(DependencyInfo info, out IPackage package, out IPackageVersion version);
        IEnumerable<IPackageVersion> EnumerateDirectReverseDependencies(IPackageVersion version, bool featureOnly);
        bool IsUsedByFeature(IPackageVersion version);
        bool HasCustomizedDependencies(IPackageVersion version, CustomizedDependencyType dependencyType);
        IReadOnlyCollection<IPackage> GetCustomizedDependencies(IPackageVersion version, CustomizedDependencyType dependencyType);
        SampleCollection GetSamples(string packageUniqueId);
        Sample GetSample(string sampleUniqueId);
        void UpdateSamples(IReadOnlyCollection<SampleCollection> toAddOrUpdate = null, IReadOnlyCollection<string> toRemove = null);
        void OnPackagesModified(IList<IPackage> modified, bool isProgressUpdated = false);
        void UpdatePackages(IReadOnlyCollection<IPackage> toAddOrUpdate = null, IReadOnlyCollection<string> toRemove = null, PackagesChangedSource changedSource = PackagesChangedSource.Other);
        void FinalizePackageUniqueId(string tempUniqueId, string finalizedUniqueId);

        PackageInUseState GetPackagesInUseState();
    }

    internal class SamplesChangeArgs
    {
        public IReadOnlyCollection<SampleCollection> added = Array.Empty<SampleCollection>();
        public IReadOnlyCollection<SampleCollection> updated = Array.Empty<SampleCollection>();
        public IReadOnlyCollection<SampleCollection> removed = Array.Empty<SampleCollection>();

        // preUpdate is the same size as the postUpdate list
        public IReadOnlyCollection<SampleCollection> preUpdate = Array.Empty<SampleCollection>();
    }

    internal class PackagesChangeArgs
    {
        public IList<IPackage> added = Array.Empty<IPackage>();
        public IList<IPackage> removed = Array.Empty<IPackage>();
        public IList<IPackage> updated = Array.Empty<IPackage>();

        // To avoid unnecessary cloning of packages, preUpdate is now set to be optional, the list is either empty or the same size as the postUpdate list
        public IList<IPackage> preUpdate = Array.Empty<IPackage>();
        public IList<IPackage> progressUpdated = Array.Empty<IPackage>();
        public PackagesChangedSource packagesChangedSource = PackagesChangedSource.Other;
    }

    [Serializable]
    internal class PackageDatabase : BaseService<IPackageDatabase>, IPackageDatabase, ISerializationCallbackReceiver
    {
        // Normally packageUniqueId never changes for a package, but when we are installing a package from git or a tarball
        // we only had a temporary unique id at first. For example, for `com.unity.a` is a unique id for a package, but when
        // we are installing from git, the only identifier we know is something like `git@example.com/com.unity.a.git`.
        // We only know the id `com.unity.a` after the package has been successfully installed, and we'll trigger an event for that.
        public event Action<string, string> onPackageUniqueIdFinalize = delegate {};

        public event Action<PackagesChangeArgs> onPackagesChanged = delegate {};
        public event Action<SamplesChangeArgs> onSamplesChanged = delegate {};

        private Sample.SampleModifier m_SampleModifier = new();

        private readonly Dictionary<string, IPackage> m_Packages = new();
        // we added m_Feature to speed up reverse dependencies lookup
        private readonly Dictionary<string, IPackage> m_Features = new();
        private readonly Dictionary<string, string> m_TechnicalNameToUniqueIdMap = new();

        // We add two dictionaries to speed up sample lookups, but we only serialize the list of SampleCollections once
        private readonly Dictionary<string, SampleCollection> m_PackageUniqueIdToSampleCollectionsMap = new();
        private readonly Dictionary<string, Sample> m_SampleUniqueIdToSamplesMap = new();

        [SerializeField]
        private Package[] m_SerializedPackages = Array.Empty<Package>();

        [SerializeField]
        private SampleCollection[] m_SerializedSamples = Array.Empty<SampleCollection>();

        public IReadOnlyCollection<IPackage> allPackages => m_Packages.Values;
        public IReadOnlyCollection<SampleCollection> sampleCollections => m_PackageUniqueIdToSampleCollectionsMap.Values;

        public IPackage GetPackage(long productId)
        {
            return GetPackage(productId.ToString());
        }

        public IPackage GetPackage(string uniqueId) => m_Packages.Get(uniqueId);

        public PackageInUseState GetPackagesInUseState()
        {
            var hasError = false;
            var hasWarning = false;
            var hasExperimental = false;

            if (m_Packages.Count > 0)
            {
                foreach (var package in m_Packages.Values)
                {
                    if (package.versions.installed == null && package.versions.imported == null)
                        continue;

                    switch (package.state)
                    {
                        case PackageState.Restricted:
                            return PackageInUseState.NonCompliant;
                        case PackageState.Error:
                            hasError = true;
                            break;
                        case PackageState.Warning:
                            hasWarning = true;
                            break;
                    }

                    if (package.versions.installed?.HasTag(PackageTag.Experimental) == true)
                        hasExperimental = true;
                }
            }
            else
            {
                foreach (var info in PackageInfo.GetAllRegisteredPackages())
                {
                    if (info.compliance.status == PackageComplianceStatus.NonCompliant)
                        return PackageInUseState.NonCompliant;

                    if (SemVersionParser.TryParse(info.version, out var parsedVersion) &&
                        parsedVersion?.GetExpOrPreOrReleaseTag() == PackageTag.Experimental)
                        hasExperimental = true;
                }
            }

            if (hasError)
                return PackageInUseState.Error;
            if (hasWarning)
                return PackageInUseState.Warning;
            if (hasExperimental)
                return PackageInUseState.Experimental;

            return PackageInUseState.None;
        }

        public IPackage GetPackageByIdOrName(string packageIdOrName)
        {
            if (string.IsNullOrEmpty(packageIdOrName))
                return null;

            var potentialUniqueId = m_TechnicalNameToUniqueIdMap.GetValueOrDefault(packageIdOrName, packageIdOrName);
            var package = GetPackage(potentialUniqueId);
            if (package != null)
                return package;

            var technicalNameSplitIndex = packageIdOrName.IndexOf('@');
            if (technicalNameSplitIndex <= 0)
                return null;

            var potentialName = packageIdOrName[..technicalNameSplitIndex];
            potentialUniqueId = m_TechnicalNameToUniqueIdMap.GetValueOrDefault(potentialName, potentialName);
            return GetPackage(potentialUniqueId);
        }

        public IPackage GetPackageByDisplayName(string displayName)
        {
            if (!string.IsNullOrEmpty(displayName))
                foreach (var package in m_Packages.Values)
                    if (package.displayName == displayName)
                        return package;
            return null;
        }

        public void GetPackageAndVersion(DependencyInfo info, out IPackage package, out IPackageVersion version)
        {
            m_TechnicalNameToUniqueIdMap.TryGetValue(info.name, out var potentialUniqueId);
            package = GetPackage(potentialUniqueId ?? info.name);

            if (package == null)
            {
                version = null;
                return;
            }

            // the versionIdentifier could either be SemVersion or file, git or ssh reference
            // and the two cases are handled differently.
            if (!string.IsNullOrEmpty(info.version) && char.IsDigit(info.version[0]))
            {
                SemVersionParser.TryParse(info.version, out var parsedVersion);
                version = package.versions.FirstMatch(v => v.version == parsedVersion);
            }
            else
            {
                var packageId = UpmPackageVersion.FormatPackageId(info.name, info.version);
                version = package.versions.FirstMatch(v => v.uniqueId == packageId);
            }
        }

        public IEnumerable<IPackageVersion> EnumerateDirectReverseDependencies(IPackageVersion version, bool featureOnly)
        {
            if (version == null)
                yield break;

            var packagesToCheck = featureOnly ? m_Features.Values : m_Packages.Values;
            foreach (var p in packagesToCheck)
            {
                var installed = p.versions.installed;
                if (installed != null && installed.dependencies.Exists(d => d.name == version.name))
                    yield return installed;
            }
        }

        public bool IsUsedByFeature(IPackageVersion version)
        {
            return version != null && m_Features.Values.AnyMatches(p => p.versions.installed?.dependencies.Exists(d => d.name == version.name) ?? false);
        }

        public bool HasCustomizedDependencies(IPackageVersion version, CustomizedDependencyType dependencyType)
        {
            using var enumerator = EnumerateCustomizedDependencies(version, dependencyType).GetEnumerator();
            return enumerator.MoveNext();
        }

        public IReadOnlyCollection<IPackage> GetCustomizedDependencies(IPackageVersion version, CustomizedDependencyType dependencyType)
        {
            return new List<IPackage>(EnumerateCustomizedDependencies(version, dependencyType));
        }

        private IEnumerable<IPackage> EnumerateCustomizedDependencies(IPackageVersion version, CustomizedDependencyType dependencyType)
        {
            if (version?.dependencies == null)
                yield break;

            foreach (var d in version.dependencies)
            {
                var package = GetPackage(d.name);
                var installed = package?.versions.installed;
                var isCustomized = installed is { isDirectDependency: true } && package.versions.recommended?.isInstalled == false;
                if (!isCustomized)
                    continue;
                var isResettable = !installed.HasTag(PackageTag.Custom) && installed.versionString == installed.versionInManifest;
                if ((isResettable && (dependencyType & CustomizedDependencyType.Resettable) != 0) || (!isResettable && (dependencyType & CustomizedDependencyType.NonResettable) != 0))
                    yield return package;
            }
        }

        public SampleCollection GetSamples(string packageUniqueId) => m_PackageUniqueIdToSampleCollectionsMap.GetValueOrDefault(packageUniqueId);
        public Sample GetSample(string sampleUniqueId) => m_SampleUniqueIdToSamplesMap.GetValueOrDefault(sampleUniqueId);
        public void UpdateSamples(IReadOnlyCollection<SampleCollection> toAddOrUpdate = null, IReadOnlyCollection<string> toRemove = null)
        {
            toAddOrUpdate ??= Array.Empty<SampleCollection>();
            toRemove ??= Array.Empty<string>();
            if (toAddOrUpdate.Count == 0 && toRemove.Count == 0)
                return;

            var added = new List<SampleCollection>();
            var updated = new List<SampleCollection>();
            var preUpdate = new List<SampleCollection>();
            foreach (var newSampleCollection in toAddOrUpdate)
            {
                var oldSamples = GetSamples(newSampleCollection.packageUniqueId);
                if (oldSamples?.Count > 0)
                {
                    foreach (var sample in oldSamples)
                        m_SampleUniqueIdToSamplesMap.Remove(sample.uniqueId);
                    preUpdate.Add(oldSamples);
                    updated.Add(newSampleCollection);
                }
                else
                {
                    added.Add(newSampleCollection);
                }

                foreach (var sample in newSampleCollection)
                    m_SampleUniqueIdToSamplesMap[sample.uniqueId] = sample;
                m_PackageUniqueIdToSampleCollectionsMap[newSampleCollection.packageUniqueId] = newSampleCollection;
            }

            var sampleRemoved = new List<SampleCollection>();
            foreach (var packageUniqueId in toRemove)
            {
                var oldSampleCollection = GetSamples(packageUniqueId);
                if (oldSampleCollection != null)
                {
                    sampleRemoved.Add(oldSampleCollection);
                    foreach (var sample in oldSampleCollection)
                        m_SampleUniqueIdToSamplesMap.Remove(sample.uniqueId);
                    m_PackageUniqueIdToSampleCollectionsMap.Remove(packageUniqueId);
                }
            }
            LinkSamplesToPackage();
            onSamplesChanged?.Invoke(new SamplesChangeArgs { added = added, updated = updated, removed = sampleRemoved, preUpdate = preUpdate});
        }

        public void OnAfterDeserialize()
        {
            foreach (var p in m_SerializedPackages)
                AddOrUpdatePackage(p);

            foreach (var sampleCollection in m_SerializedSamples)
            {
                m_PackageUniqueIdToSampleCollectionsMap[sampleCollection.packageUniqueId] = sampleCollection;
                foreach (var sample in sampleCollection)
                    m_SampleUniqueIdToSamplesMap[sample.uniqueId] = sample;
            }
            LinkSamplesToPackage();
        }

        public void OnBeforeSerialize()
        {
            m_SerializedPackages = m_Packages.Values.FilterByType<Package>().ToNewArray(m_Packages.Count);
            m_PackageUniqueIdToSampleCollectionsMap.Values.ToArray(ref m_SerializedSamples);
        }

        private void TriggerOnPackagesChanged(IList<IPackage> added = null, IList<IPackage> removed = null, IList<IPackage> updated = null, IList<IPackage> preUpdate = null, IList<IPackage> progressUpdated = null, PackagesChangedSource changedSource = PackagesChangedSource.Other)
        {
            added ??= Array.Empty<IPackage>();
            updated ??= Array.Empty<IPackage>();
            removed ??= Array.Empty<IPackage>();
            preUpdate ??= Array.Empty<IPackage>();
            progressUpdated ??= Array.Empty<IPackage>();

            if (added.Count + updated.Count + removed.Count + preUpdate.Count + progressUpdated.Count <= 0)
                return;

            onPackagesChanged?.Invoke(new PackagesChangeArgs { added = added, updated = updated, removed = removed, preUpdate = preUpdate, progressUpdated = progressUpdated, packagesChangedSource = changedSource});
        }

        public void OnPackagesModified(IList<IPackage> modified, bool isProgressUpdated = false)
        {
            TriggerOnPackagesChanged(updated: modified, progressUpdated: isProgressUpdated ? modified : null);
        }

        public void UpdatePackages(IReadOnlyCollection<IPackage> toAddOrUpdate = null, IReadOnlyCollection<string> toRemove = null, PackagesChangedSource changedSource = PackagesChangedSource.Other)
        {
            toAddOrUpdate ??= Array.Empty<IPackage>();
            toRemove ??= Array.Empty<string>();
            if (toAddOrUpdate.Count == 0 && toRemove.Count == 0)
                return;

            var featuresWithDependencyChange = new Dictionary<string, IPackage>();
            var packagesAdded = new List<IPackage>();
            var packagesRemoved = new List<IPackage>();

            var packagesPreUpdate = new List<IPackage>();
            var packagesUpdated = new List<IPackage>();

            var packageProgressUpdated = new List<IPackage>();

            foreach (var package in toAddOrUpdate)
            {
                foreach (var feature in EnumerateDirectReverseDependencies(package.versions.primary, true))
                {
                    if (!featuresWithDependencyChange.ContainsKey(feature.uniqueId))
                        featuresWithDependencyChange[feature.uniqueId] = feature.package;
                }

                var oldPackage = GetPackage(package.uniqueId);
                AddOrUpdatePackage(package, oldPackage);
                if (oldPackage != null)
                {
                    packagesPreUpdate.Add(oldPackage);
                    packagesUpdated.Add(package);

                    if (oldPackage.progress != package.progress)
                        packageProgressUpdated.Add(package);
                }
                else
                    packagesAdded.Add(package);
            }

            packagesUpdated.AddRange(featuresWithDependencyChange.Values.Filter(p => !packagesUpdated.Contains(p)));

            foreach (var packageUniqueId in toRemove)
            {
                var oldPackage = GetPackage(packageUniqueId);
                if (oldPackage != null)
                {
                    packagesRemoved.Add(oldPackage);
                    RemovePackage(oldPackage);
                }
            }

            LinkSamplesToPackage();
            TriggerOnPackagesChanged(added: packagesAdded, removed: packagesRemoved, preUpdate: packagesPreUpdate, updated: packagesUpdated, progressUpdated: packageProgressUpdated, changedSource: changedSource);
        }

        private void LinkSamplesToPackage()
        {
            var newSampleCollections = new List<SampleCollection>();
            foreach (var sampleCollection in m_PackageUniqueIdToSampleCollectionsMap.Values)
            {
                var package = GetPackage(sampleCollection.packageUniqueId);
                if (package == null)
                    continue;
                var newSamples = new Sample[sampleCollection.Count];
                for (var i = 0; i < sampleCollection.Count; i++)
                {
                    var sample = m_SampleModifier.SetPackage(sampleCollection[i], package);
                    newSamples[i] = sample;
                    m_SampleUniqueIdToSamplesMap[sample.uniqueId] = sample;
                }

                newSampleCollections.Add(new SampleCollection(package.uniqueId, newSamples));
            }

            if (newSampleCollections.Count == 0)
                return;

            foreach (var sampleCollection in newSampleCollections)
                m_PackageUniqueIdToSampleCollectionsMap[sampleCollection.packageUniqueId] = sampleCollection;
        }

        public void FinalizePackageUniqueId(string tempUniqueId, string finalizedUniqueId)
        {
            var packageWithTempId = GetPackage(tempUniqueId);
            if (packageWithTempId == null || !packageWithTempId.versions.primary.HasTag(PackageTag.SpecialInstall) || tempUniqueId == finalizedUniqueId)
                return;

            if (!string.IsNullOrEmpty(finalizedUniqueId))
                onPackageUniqueIdFinalize?.Invoke(tempUniqueId, finalizedUniqueId);

            RemovePackage(packageWithTempId);
            TriggerOnPackagesChanged(removed: new [] { packageWithTempId });
        }

        private void RemovePackage(IPackage package)
        {
            var packageName = package.name;
            var packageUniqueId = package.uniqueId;
            if (!string.IsNullOrEmpty(packageName))
                m_TechnicalNameToUniqueIdMap.Remove(packageName);
            m_Packages.Remove(packageUniqueId);
            m_Features.Remove(packageUniqueId);
        }

        private void AddOrUpdatePackage(IPackage package, IPackage oldPackage = null)
        {
            var packageUniqueId = package.uniqueId;
            m_Packages[packageUniqueId] = package;
            if (package.versions.AllMatches(v => v.HasTag(PackageTag.Feature)))
                m_Features[packageUniqueId] = package;

            var packageName = package.name;
            var oldPackageName = oldPackage?.name;
            if (!string.IsNullOrEmpty(packageName) && packageName != packageUniqueId)
                m_TechnicalNameToUniqueIdMap[packageName] = packageUniqueId;
            if (!string.IsNullOrEmpty(oldPackageName) && oldPackageName != packageName)
                m_TechnicalNameToUniqueIdMap.Remove(oldPackageName);
        }
    }
}
