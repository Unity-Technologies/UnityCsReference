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
    internal class PackagesChangeArgs
    {
        public IList<IPackage> added = Array.Empty<IPackage>();
        public IList<IPackage> removed = Array.Empty<IPackage>();
        public IList<IPackage> updated = Array.Empty<IPackage>();

        // To avoid unnecessary cloning of packages, preUpdate is now set to be optional, the list is either empty or the same size as the the postUpdate list
        public IList<IPackage> preUpdate = Array.Empty<IPackage>();
        public IList<IPackage> progressUpdated = Array.Empty<IPackage>();
    }

    [Serializable]
    internal class PackageDatabase : ISerializationCallbackReceiver
    {
        // Normally package unique Id never changes for a package, but when we are installing a package from git or a tarball
        // we only had a temporary unique id at first. For example, for `com.unity.a` is a unique id for a package, but when
        // we are installing from git, the only identifier we know is something like `git@example.com/com.unity.a.git`.
        // We only know the id `com.unity.a` after the package has been successfully installed, and we'll trigger an event for that.
        public virtual event Action<string, string> onPackageUniqueIdFinalize = delegate {};

        public virtual event Action<PackagesChangeArgs> onPackagesChanged = delegate {};

        private readonly Dictionary<string, IPackage> m_Packages = new();
        // we added m_Feature to speed up reverse dependencies lookup
        private readonly Dictionary<string, IPackage> m_Features = new();

        private readonly Dictionary<string, IEnumerable<Sample>> m_ParsedSamples = new();

        [SerializeField]
        private Package[] m_SerializedPackages = Array.Empty<Package>();

        [NonSerialized]
        private UniqueIdMapper m_UniqueIdMapper;
        [NonSerialized]
        private AssetDatabaseProxy m_AssetDatabase;
        [NonSerialized]
        private UpmCache m_UpmCache;
        [NonSerialized]
        private IOProxy m_IOProxy;
        public void ResolveDependencies(UniqueIdMapper uniqueIdMapper,
            AssetDatabaseProxy assetDatabase,
            UpmCache upmCache,
            IOProxy ioProxy)
        {
            m_UniqueIdMapper = uniqueIdMapper;
            m_AssetDatabase = assetDatabase;
            m_UpmCache = upmCache;
            m_IOProxy = ioProxy;
        }

        public virtual bool isEmpty => !m_Packages.Any();

        public virtual IEnumerable<IPackage> allPackages => m_Packages.Values;

        public virtual IPackage GetPackage(string uniqueId)
        {
            return GetPackage(uniqueId, false);
        }

        public virtual IPackage GetPackage(long productId)
        {
            return GetPackage(productId.ToString(), false);
        }

        private IPackage GetPackage(string uniqueId, bool retryWithIdMapper)
        {
            if (string.IsNullOrEmpty(uniqueId))
                return null;
            var package = m_Packages.Get(uniqueId);
            if (package != null || !retryWithIdMapper)
                return package;

            // We only retry with productId now because in the case where productId and package Name both exist for a package
            // we use productId as the primary key.
            var productId = m_UniqueIdMapper.GetProductIdByName(uniqueId);
            return productId > 0 ? m_Packages.Get(productId.ToString()) : null;
        }

        // In some situations, we only know an id (could be package unique id, or version unique id) or just a name (package Name, or display name)
        // but we still might be able to find a package and a version that matches the criteria
        public virtual void GetPackageAndVersionByIdOrName(string idOrName, out IPackage package, out IPackageVersion version, bool bruteForceSearch)
        {
            // GetPackage by packageUniqueId itself is not an expensive operation, so we want to try and see if the input string is a packageUniqueId first.
            package = GetPackage(idOrName, true);
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
                GetPackageAndVersion(packageUniqueId, idOrName, out package, out version);
                if (package != null)
                    return;
            }

            // If none of those find-by-index options work, we'll just have to find it the brute force way by matching the name & display name
            package = bruteForceSearch ? m_Packages.Values.FirstOrDefault(p => p.name == idOrName || p.displayName == idOrName) : null;
            version = null;
        }

        public virtual IPackage GetPackageByIdOrName(string idOrName)
        {
            GetPackageAndVersionByIdOrName(idOrName, out var package, out _, false);
            return package;
        }

        public virtual void GetPackageAndVersion(string packageUniqueId, string versionUniqueId, out IPackage package, out IPackageVersion version)
        {
            package = GetPackage(packageUniqueId, true);
            version = package?.versions.FirstOrDefault(v => v.uniqueId == versionUniqueId);
        }

        public virtual void GetPackageAndVersion(PackageAndVersionIdPair pair, out IPackage package, out IPackageVersion version)
        {
            GetPackageAndVersion(pair?.packageUniqueId, pair?.versionUniqueId, out package, out version);
        }

        public virtual void GetPackageAndVersion(DependencyInfo info, out IPackage package, out IPackageVersion version)
        {
            package = GetPackage(info.name);
            if (package == null)
            {
                version = null;
                return;
            }

            // the versionIdentifier could either be SemVersion or file, git or ssh reference
            // and the two cases are handled differently.
            if (!string.IsNullOrEmpty(info.version) && char.IsDigit(info.version.First()))
            {
                SemVersionParser.TryParse(info.version, out var parsedVersion);
                version = package.versions.FirstOrDefault(v => v.version == parsedVersion);
            }
            else
            {
                var packageId = UpmPackageVersion.FormatPackageId(info.name, info.version);
                version = package.versions.FirstOrDefault(v => v.uniqueId == packageId);
            }
        }

        public virtual IEnumerable<IPackageVersion> GetReverseDependencies(IPackageVersion version, bool directDependenciesOnly = false)
        {
            if (version?.dependencies == null)
                return null;

            var installedRoots = allPackages.Select(p => p.versions.installed).Where(p => p?.isDirectDependency ?? false);
            return installedRoots.Where(p
                => (directDependenciesOnly ? p.dependencies : p.resolvedDependencies)?.Any(r => r.name == version.name) ?? false);
        }

        public virtual IEnumerable<IPackageVersion> GetFeaturesThatUseThisPackage(IPackageVersion version)
        {
            if (version?.dependencies == null)
                return Enumerable.Empty<IPackageVersion>();

            var installedFeatures = m_Features.Values.Select(p => p.versions.installed)
                .Where(p => p?.isDirectDependency ?? false);
            return installedFeatures.Where(f => f.dependencies?.Any(r => r.name == version.name) ?? false);
        }

        public virtual IPackage[] GetCustomizedDependencies(IPackageVersion version, bool? rootDependenciesOnly = null)
        {
            return version?.dependencies?.Select(d => GetPackage(d.name)).Where(p =>
            {
                return p?.versions.isNonLifecycleVersionInstalled == true
                && (rootDependenciesOnly == null || p.versions.installed.isDirectDependency == rootDependenciesOnly);
            }).ToArray() ?? new IPackage[0];
        }

        public virtual IEnumerable<Sample> GetSamples(IPackageVersion version)
        {
            var packageInfo = version != null ? m_UpmCache.GetBestMatchPackageInfo(version.name, version.isInstalled, version.versionString) : null;
            if (packageInfo == null || packageInfo.version != version.version?.ToString())
                return Enumerable.Empty<Sample>();

            if (m_ParsedSamples.TryGetValue(version.uniqueId, out var parsedSamples))
                return parsedSamples;

            var samples = Sample.FindByPackage(packageInfo, m_UpmCache, m_IOProxy, m_AssetDatabase);
            m_ParsedSamples[version.uniqueId] = samples;
            return samples;
        }

        public virtual IPackageVersion GetLifecycleOrPrimaryVersion(string packageUniqueId)
        {
            var versions = GetPackage(packageUniqueId)?.versions;
            return versions?.lifecycleVersion ?? versions?.primary;
        }

        public void OnAfterDeserialize()
        {
            foreach (var p in m_SerializedPackages)
                AddPackage(p.uniqueId, p);
        }

        public void OnBeforeSerialize()
        {
            m_SerializedPackages = m_Packages.Values.Cast<Package>().ToArray();
        }

        private void TriggerOnPackagesChanged(IList<IPackage> added = null, IList<IPackage> removed = null, IList<IPackage> updated = null, IList<IPackage> preUpdate = null, IList<IPackage> progressUpdated = null)
        {
            added ??= Array.Empty<IPackage>();
            updated ??= Array.Empty<IPackage>();
            removed ??= Array.Empty<IPackage>();
            preUpdate ??= Array.Empty<IPackage>();
            progressUpdated ??= Array.Empty<IPackage>();

            if (added.Count + updated.Count + removed.Count + preUpdate.Count + progressUpdated.Count <= 0)
                return;

            onPackagesChanged?.Invoke(new PackagesChangeArgs { added = added, updated = updated, removed = removed, preUpdate = preUpdate, progressUpdated = progressUpdated });
        }

        public virtual void OnPackagesModified(IList<IPackage> modified, bool isProgressUpdated = false)
        {
            TriggerOnPackagesChanged(updated: modified, progressUpdated: isProgressUpdated ? modified : null);
        }

        public virtual void UpdatePackages(IList<IPackage> toAddOrUpdate = null, IList<string> toRemove = null)
        {
            toAddOrUpdate ??= Array.Empty<IPackage>();
            toRemove ??= Array.Empty<string>();
            if (!toAddOrUpdate.Any() && !toRemove.Any())
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

                // It could happen that before the productId info was available, another package was created with packageName as the uniqueId
                // Once the productId becomes available it should be the new uniqueId, we want to old package such that there won't be two
                // entries of the same package with different uniqueIds (one productId, one with packageName)
                if (package.product != null && !string.IsNullOrEmpty(package.name))
                {
                    {
                        var packageWithNameAsUniqueId = GetPackage(package.name);
                        if (packageWithNameAsUniqueId != null)
                        {
                            packagesRemoved.Add(packageWithNameAsUniqueId);
                            RemovePackage(package.name);
                            onPackageUniqueIdFinalize?.Invoke(package.name, package.uniqueId);
                        }
                    }
                }

                var tempId = m_UniqueIdMapper.GetTempIdByFinalizedId(package.uniqueId);
                if (!string.IsNullOrEmpty(tempId))
                {
                    var packageWithTempId = GetPackage(tempId);
                    if (packageWithTempId != null)
                    {
                        packagesRemoved.Add(packageWithTempId);
                        RemovePackage(tempId);
                    }
                    onPackageUniqueIdFinalize?.Invoke(tempId, package.uniqueId);
                    m_UniqueIdMapper.RemoveTempId(package.uniqueId);
                }
            }

            packagesUpdated.AddRange(featuresWithDependencyChange.Values.Where(p => !packagesUpdated.Contains(p)));

            foreach (var packageUniqueId in toRemove)
            {
                var oldPackage = GetPackage(packageUniqueId);
                if (oldPackage != null)
                {
                    packagesRemoved.Add(oldPackage);
                    RemovePackage(packageUniqueId);
                }
            }
            TriggerOnPackagesChanged(added: packagesAdded, removed: packagesRemoved, preUpdate: packagesPreUpdate, updated: packagesUpdated, progressUpdated: packageProgressUpdated);
        }

        private void RemovePackage(string packageUniqueId)
        {
            m_Packages.Remove(packageUniqueId);
            m_Features.Remove(packageUniqueId);
        }

        private void AddPackage(string packageUniqueId, IPackage package)
        {
            m_Packages[packageUniqueId] = package;
            if (package.versions.All(v => v.HasTag(PackageTag.Feature)))
                m_Features[packageUniqueId] = package;
        }

        public void ClearSamplesCache()
        {
            m_ParsedSamples.Clear();
        }
    }
}
