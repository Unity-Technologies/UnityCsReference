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
        IPackage[] GetCustomizedDependencies(IPackageVersion version, bool? rootDependenciesOnly = null);
        IEnumerable<Sample> GetSamples(IPackageVersion version);
        void OnPackagesModified(IList<IPackage> modified, bool isProgressUpdated = false);
        void UpdatePackages(IList<IPackage> toAddOrUpdate = null, IList<string> toRemove = null);

        void ClearSamplesCache();
    }

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

        private readonly Dictionary<string, IEnumerable<Sample>> m_ParsedSamples = new();

        [SerializeField]
        private Package[] m_SerializedPackages = Array.Empty<Package>();

        private readonly IUniqueIdMapper m_UniqueIdMapper;
        private readonly IAssetDatabaseProxy m_AssetDatabase;
        private readonly IUpmCache m_UpmCache;
        private readonly IIOProxy m_IOProxy;
        public PackageDatabase(IUniqueIdMapper uniqueIdMapper,
            IAssetDatabaseProxy assetDatabase,
            IUpmCache upmCache,
            IIOProxy ioProxy)
        {
            m_UniqueIdMapper = RegisterDependency(uniqueIdMapper);
            m_AssetDatabase = RegisterDependency(assetDatabase);
            m_UpmCache = RegisterDependency(upmCache);
            m_IOProxy = RegisterDependency(ioProxy);
        }

        public bool isEmpty => !m_Packages.Any();

        public IEnumerable<IPackage> allPackages => m_Packages.Values;

        public IPackage GetPackage(long productId)
        {
            return GetPackage(productId.ToString());
        }

        public IPackage GetPackage(string uniqueId) => m_Packages.Get(uniqueId);

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
                    version = package.versions.FirstOrDefault(v => v.uniqueId == idOrName);
                    return;
                }
            }

            // If none of those find-by-index options work, we'll just have to find it the brute force way by matching the name & display name
            package = bruteForceSearch ? m_Packages.Values.FirstOrDefault(p => p.name == idOrName || p.displayName == idOrName) : null;
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

        public IEnumerable<IPackageVersion> GetDirectReverseDependencies(IPackageVersion version)
        {
            if (version == null)
                return null;
            return allPackages.Select(p => p.versions.installed).Where(p => p?.dependencies?.Any(d =>d.name == version.name) ?? false);
        }

        public IEnumerable<IPackageVersion> GetFeaturesThatUseThisPackage(IPackageVersion version)
        {
            if (version?.dependencies == null)
                return Enumerable.Empty<IPackageVersion>();

            var installedFeatures = m_Features.Values.Select(p => p.versions.installed)
                .Where(p => p?.isDirectDependency ?? false);
            return installedFeatures.Where(f => f.dependencies?.Any(r => r.name == version.name) ?? false);
        }

        public IPackage[] GetCustomizedDependencies(IPackageVersion version, bool? rootDependenciesOnly = null)
        {
            return version?.dependencies?.Select(d => GetPackage(d.name)).Where(p =>
            {
                var installed = p?.versions.installed;
                return installed != null && p.versions.recommended?.isInstalled == false
                       && (rootDependenciesOnly == null || installed.isDirectDependency == rootDependenciesOnly);
            }).ToArray() ?? new IPackage[0];
        }

        public IEnumerable<Sample> GetSamples(IPackageVersion version)
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

        public void OnPackagesModified(IList<IPackage> modified, bool isProgressUpdated = false)
        {
            TriggerOnPackagesChanged(updated: modified, progressUpdated: isProgressUpdated ? modified : null);
        }

        public void UpdatePackages(IList<IPackage> toAddOrUpdate = null, IList<string> toRemove = null)
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
