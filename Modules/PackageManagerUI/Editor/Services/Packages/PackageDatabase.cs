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
    internal interface IPackageDatabase : IService
    {
        event Action<string, string> onPackageUniqueIdFinalize;
        event Action<PackagesChangeArgs> onPackagesChanged;

        bool isEmpty { get; }
        IEnumerable<IPackage> allPackages { get; }

        IPackage GetPackage(string uniqueId);
        IPackage GetPackage(long productId);
        void GetPackageAndVersionByIdOrName(string idOrName, out IPackage package, out IPackageVersion version, bool bruteForceSearch);
        IPackage GetPackageByIdOrName(string idOrName);
        void GetPackageAndVersion(DependencyInfo info, out IPackage package, out IPackageVersion version);
        IEnumerable<IPackageVersion> GetDirectReverseDependencies(IPackageVersion version);
        IEnumerable<IPackageVersion> GetFeaturesThatUseThisPackage(IPackageVersion version);
        IPackage[] GetCustomizedDependencies(IPackageVersion version, CustomizedDependencyType dependencyType);
        IReadOnlyCollection<Sample> GetSamples(IPackageVersion version);
        void OnPackagesModified(IList<IPackage> modified, bool isProgressUpdated = false);
        void UpdatePackages(IReadOnlyCollection<IPackage> toAddOrUpdate = null, IReadOnlyCollection<string> toRemove = null, PackagesChangedSource changedSource = PackagesChangedSource.Other);
        void FinalizePackageUniqueId(string tempUniqueId, string finalizedUniqueId);

        void ClearSamplesCache();

        PackageInUseState GetPackagesInUseState();
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
        // Normally package unique Id never changes for a package, but when we are installing a package from git or a tarball
        // we only had a temporary unique id at first. For example, for `com.unity.a` is a unique id for a package, but when
        // we are installing from git, the only identifier we know is something like `git@example.com/com.unity.a.git`.
        // We only know the id `com.unity.a` after the package has been successfully installed, and we'll trigger an event for that.
        public event Action<string, string> onPackageUniqueIdFinalize = delegate {};

        public event Action<PackagesChangeArgs> onPackagesChanged = delegate {};

        private readonly Dictionary<string, IPackage> m_Packages = new();
        // we added m_Feature to speed up reverse dependencies lookup
        private readonly Dictionary<string, IPackage> m_Features = new();

        private readonly Dictionary<string, IReadOnlyCollection<Sample>> m_ParsedSamples = new();

        [SerializeField]
        private Package[] m_SerializedPackages = Array.Empty<Package>();

        private readonly IAssetDatabaseProxy m_AssetDatabase;
        private readonly IUpmCache m_UpmCache;
        private readonly IIOProxy m_IOProxy;
        public PackageDatabase(IAssetDatabaseProxy assetDatabase,
            IUpmCache upmCache,
            IIOProxy ioProxy)
        {
            m_AssetDatabase = RegisterDependency(assetDatabase);
            m_UpmCache = RegisterDependency(upmCache);
            m_IOProxy = RegisterDependency(ioProxy);
        }

        public bool isEmpty => m_Packages.Count == 0;

        public IEnumerable<IPackage> allPackages => m_Packages.Values;

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

        // In some situations, we only know an id (could be package unique id, or version unique id) or just a name (package Name, or display name)
        // but we still might be able to find a package and a version that matches the criteria
        public void GetPackageAndVersionByIdOrName(string idOrName, out IPackage package, out IPackageVersion version, bool bruteForceSearch)
        {
            // GetPackage by packageUniqueId itself is not an expensive operation, so we want to try and see if the input string is a packageUniqueId first.
            package = GetPackage(idOrName);
            if (package != null)
            {
                version = null;
                return;
            }

            // if we are able to break the string into two by looking at '@' sign, it's possible that the input idOrDisplayName is a versionId
            var idOrDisplayNameSplit = idOrName?.Split(new[] { '@' }, 2);
            if (idOrDisplayNameSplit?.Length == 2)
            {
                var packageUniqueId = idOrDisplayNameSplit[0];
                package = GetPackage(packageUniqueId);
                if (package != null)
                {
                    #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                    version = package.versions.FirstOrDefault(v => v.uniqueId == idOrName);
#pragma warning restore RS0030
                    return;
                }
            }

            // If none of those find-by-index options work, we'll just have to find it the brute force way by matching the name & display name
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            package = bruteForceSearch ? m_Packages.Values.FirstOrDefault(p => p.name == idOrName || p.displayName == idOrName) : null;
#pragma warning restore RS0030
            version = null;
        }

        public IPackage GetPackageByIdOrName(string idOrName)
        {
            GetPackageAndVersionByIdOrName(idOrName, out var package, out _, false);
            return package;
        }

        public void GetPackageAndVersion(DependencyInfo info, out IPackage package, out IPackageVersion version)
        {
            package = GetPackage(info.name);
            if (package == null)
            {
                version = null;
                return;
            }

            // the versionIdentifier could either be SemVersion or file, git or ssh reference
            // and the two cases are handled differently.
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (!string.IsNullOrEmpty(info.version) && char.IsDigit(info.version.First()))
#pragma warning restore RS0030
            {
                SemVersionParser.TryParse(info.version, out var parsedVersion);
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                version = package.versions.FirstOrDefault(v => v.version == parsedVersion);
#pragma warning restore RS0030
            }
            else
            {
                var packageId = UpmPackageVersion.FormatPackageId(info.name, info.version);
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                version = package.versions.FirstOrDefault(v => v.uniqueId == packageId);
#pragma warning restore RS0030
            }
        }

        public IEnumerable<IPackageVersion> GetDirectReverseDependencies(IPackageVersion version)
        {
            if (version == null)
                return null;
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return allPackages.Select(p => p.versions.installed).Where(p => p?.dependencies?.Any(d =>d.name == version.name) ?? false);
#pragma warning restore RS0030
        }

        public IEnumerable<IPackageVersion> GetFeaturesThatUseThisPackage(IPackageVersion version)
        {
            if (version?.dependencies == null)
                #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                return Enumerable.Empty<IPackageVersion>();
#pragma warning restore RS0030

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            var installedFeatures = m_Features.Values.Select(p => p.versions.installed)
#pragma warning restore RS0030
                .Where(p => p?.isDirectDependency ?? false);
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return installedFeatures.Where(f => f.dependencies?.Any(r => r.name == version.name) ?? false);
#pragma warning restore RS0030
        }

        public IPackage[] GetCustomizedDependencies(IPackageVersion version, CustomizedDependencyType dependencyType)
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return version?.dependencies?.Select(d => GetPackage(d.name)).Where(p =>
#pragma warning restore RS0030
            {
                var installed = p?.versions.installed;
                var isCustomized = installed is { isDirectDependency: true } && p.versions.recommended?.isInstalled == false;
                if (!isCustomized)
                    return false;
                var isResettable = !installed.HasTag(PackageTag.Custom) && installed.versionString == installed.versionInManifest;
                return (isResettable && (dependencyType & CustomizedDependencyType.Resettable) != 0) ||
                       (!isResettable && (dependencyType & CustomizedDependencyType.NonResettable) != 0);
            }).ToArray() ?? Array.Empty<IPackage>();
        }

        public IReadOnlyCollection<Sample> GetSamples(IPackageVersion version)
        {
            // Null check for version.package is necessary for domain reload test that uses the UpmPackageVersion directly without a package without mocking it
            var packageInfo = version != null ? m_UpmCache.GetBestMatchPackageInfo(version.name, version.package?.product?.id ?? 0, version.isInstalled, version.versionString) : null;
            if (packageInfo == null || packageInfo.version != version.version?.ToString())
                return Array.Empty<Sample>();

            if (m_ParsedSamples.TryGetValue(version.uniqueId, out var parsedSamples))
                return parsedSamples;

            var samples = Sample.FindByPackage(packageInfo, m_UpmCache, m_IOProxy, m_AssetDatabase);
            m_ParsedSamples[version.uniqueId] = samples;
            return samples;
        }

        public void OnAfterDeserialize()
        {
            foreach (var p in m_SerializedPackages)
                AddPackage(p.uniqueId, p);
        }

        public void OnBeforeSerialize()
        {
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            m_SerializedPackages = m_Packages.Values.Cast<Package>().ToArray();
#pragma warning restore RS0030
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
                foreach (var feature in GetFeaturesThatUseThisPackage(package.versions.primary))
                {
                    if (!featuresWithDependencyChange.ContainsKey(feature.uniqueId))
                        featuresWithDependencyChange[feature.uniqueId] = feature.package;
                }

                var packageUniqueId = package.uniqueId;
                var oldPackage = GetPackage(packageUniqueId);

                AddPackage(packageUniqueId, package);
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

            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            packagesUpdated.AddRange(featuresWithDependencyChange.Values.Where(p => !packagesUpdated.Contains(p)));
#pragma warning restore RS0030

            foreach (var packageUniqueId in toRemove)
            {
                var oldPackage = GetPackage(packageUniqueId);
                if (oldPackage != null)
                {
                    packagesRemoved.Add(oldPackage);
                    RemovePackage(packageUniqueId);
                }
            }
            TriggerOnPackagesChanged(added: packagesAdded, removed: packagesRemoved, preUpdate: packagesPreUpdate, updated: packagesUpdated, progressUpdated: packageProgressUpdated, changedSource: changedSource);
        }

        public void FinalizePackageUniqueId(string tempUniqueId, string finalizedUniqueId)
        {
            var packageWithTempId = GetPackage(tempUniqueId);
            if (packageWithTempId == null || !packageWithTempId.versions.primary.HasTag(PackageTag.SpecialInstall) || tempUniqueId == finalizedUniqueId)
                return;

            if (!string.IsNullOrEmpty(finalizedUniqueId))
                onPackageUniqueIdFinalize?.Invoke(tempUniqueId, finalizedUniqueId);

            RemovePackage(tempUniqueId);
            TriggerOnPackagesChanged(removed: new [] { packageWithTempId });
        }

        private void RemovePackage(string packageUniqueId)
        {
            m_Packages.Remove(packageUniqueId);
            m_Features.Remove(packageUniqueId);
        }

        private void AddPackage(string packageUniqueId, IPackage package)
        {
            m_Packages[packageUniqueId] = package;
            #pragma warning disable RS0030 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            if (package.versions.All(v => v.HasTag(PackageTag.Feature)))
#pragma warning restore RS0030
                m_Features[packageUniqueId] = package;
        }

        public void ClearSamplesCache()
        {
            m_ParsedSamples.Clear();
        }
    }
}
