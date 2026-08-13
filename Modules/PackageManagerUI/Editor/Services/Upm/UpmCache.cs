// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IUpmCache : IService
    {
        event Action<string, bool> onLoadAllVersionsChanged;
        event Action<IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)>, PackagesChangedSource> onPackageInfosUpdated;
        event Action<PackageInfo> onExtraPackageInfoFetched;
        event Action onScopedRegistriesPotentiallyChanged;

        IReadOnlyCollection<PackageInfo> discoverableSearchPackageInfos { get; }
        IReadOnlyCollection<PackageInfo> nonDiscoverableSearchPackageInfos { get; }
        IReadOnlyCollection<PackageInfo> installedPackageInfos { get; }
        bool installedPackageInfosReady { get; }

        void SetLoadAllVersions(string packageName, bool value);
        void AddSearchNonDiscoverableResult(string packageName, PackageInfo packageInfo, long timestamp);
        void AddExtraFetchResult(PackageInfo packageInfo);
        PackageInfo GetExtraPackageInfo(string packageId);
        PackageInfo GetInstalledPackageInfo(string packageName);
        IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> SetInstalledPackageInfos(IEnumerable<PackageInfo> packageInfos, long timestamp = 0, PackagesChangedSource changeSource = PackagesChangedSource.Other);
        PackageInfo GetSearchPackageInfo(string packageName);
        PackageInfo GetBestMatchPackageInfo(string packageName, bool isInstalled, string version = null);
        IUpmPackageData GetPackageData(string packageName);
        IUpmPackageData GetPackageData(long productId);
        void SetSearchPackageInfos(IEnumerable<PackageInfo> packageInfos, long timestamp);
        Dictionary<string, object> ParseUpmReserved(PackageInfo packageInfo);
        void ClearCache();
        void ClearNonDiscoverableSearchInfos();
        void ClearExtraInfoCache();
    }

    [Serializable]
    internal class PackageInfoDictionary : ISerializationCallbackReceiver
    {
        private Dictionary<string, PackageInfo> m_ByName = new();
        private readonly Dictionary<long, PackageInfo> m_ByProductId = new();
        private Dictionary<string, long> m_TimestampByName = new();

        [SerializeField]
        private PackageInfo[] m_Serialized = Array.Empty<PackageInfo>();

        [SerializeField]
        private long[] m_SerializedTimestamps = Array.Empty<long>();

        [SerializeField]
        private long m_Timestamp = -1;

        public long timestamp => m_Timestamp;
        public IReadOnlyCollection<PackageInfo> values => m_ByName.Values;

        public PackageInfo GetByName(string packageName) => m_ByName.GetValueOrDefault(packageName);
        public PackageInfo GetByProductId(long productId) => m_ByProductId.GetValueOrDefault(productId);

        public (PackageInfo info, long timestamp)? GetByNameWithTimestamp(string packageName)
        {
            var info = m_ByName.GetValueOrDefault(packageName);
            return info == null ? null : (info, m_TimestampByName.GetValueOrDefault(packageName, m_Timestamp));
        }

        public List<(PackageInfo oldInfo, PackageInfo newInfo)> ReplaceAll(IEnumerable<PackageInfo> packageInfos, long timestamp)
        {
            m_Timestamp = timestamp;
            m_TimestampByName.Clear();
            var old = m_ByName;
            m_ByName = packageInfos.ToDictionary(p => p.name);
            RebuildByProductId();
            return FindUpdatedPackageInfos(old, m_ByName);
        }

        public (PackageInfo oldInfo, PackageInfo newInfo)? AddOrUpdate(string packageName, PackageInfo packageInfo, long timestamp)
        {
            var oldInfo = m_ByName.GetValueOrDefault(packageName);
            var oldProductId = oldInfo?.ParseProductId() ?? 0;
            if (oldProductId > 0)
                m_ByProductId.Remove(oldProductId);
            m_ByName[packageName] = packageInfo;
            m_TimestampByName[packageName] = timestamp;
            var newProductId = packageInfo.ParseProductId();
            if (newProductId > 0)
                m_ByProductId[newProductId] = packageInfo;
            if (oldInfo == null || IsDifferent(oldInfo, packageInfo))
                return (oldInfo, packageInfo);
            return null;
        }

        private static List<(PackageInfo oldInfo, PackageInfo newInfo)> FindUpdatedPackageInfos(Dictionary<string, PackageInfo> oldInfos, Dictionary<string, PackageInfo> newInfos)
        {
            var result = new List<(PackageInfo oldInfo, PackageInfo newInfo)>();
            foreach (var oldInfo in oldInfos.Values)
            {
                if (newInfos.TryGetValue(oldInfo.name, out var newInfo) && !IsDifferent(oldInfo, newInfo))
                    continue;
                result.Add((oldInfo, newInfo));
            }
            foreach (var newInfo in newInfos.Values.Where(p => !oldInfos.ContainsKey(p.name)))
                result.Add((null, newInfo));
            return result;
        }

        // For BuiltIn and Registry packages, we want to only compare a subset of PackageInfo attributes,
        // as most attributes never change if their PackageId is the same. For other types of packages, always consider them different
        private static bool IsDifferent(PackageInfo p1, PackageInfo p2)
        {
            if (p1.packageId != p2.packageId ||
                p1.isDirectDependency != p2.isDirectDependency ||
                p1.version != p2.version ||
                p1.source != p2.source ||
                p1.resolvedPath != p2.resolvedPath ||
                p1.entitlements.isAllowed != p2.entitlements.isAllowed ||
                p1.entitlements.licensingModel != p2.entitlements.licensingModel ||
                p1.versions.recommended != p2.versions.recommended ||
                p1.versions.compatible.Length != p2.versions.compatible.Length || !p1.versions.compatible.SequenceEqual(p2.versions.compatible) ||
                p1.versions.all.Length != p2.versions.all.Length || !p1.versions.all.SequenceEqual(p2.versions.all) ||
                p1.errors.Length != p2.errors.Length || !p1.errors.SequenceEqual(p2.errors) ||
                p1.dependencies.Length != p2.dependencies.Length || !p1.dependencies.SequenceEqual(p2.dependencies) ||
                p1.resolvedDependencies.Length != p2.resolvedDependencies.Length || !p1.resolvedDependencies.SequenceEqual(p2.resolvedDependencies) ||
                p1.projectDependenciesEntry != p2.projectDependenciesEntry ||
                p1.signature.status != p2.signature.status ||
                p1.trustLevel != p2.trustLevel ||
                p1.documentationUrl != p2.documentationUrl ||
                p1.changelogUrl != p2.changelogUrl ||
                p1.licensesUrl != p2.licensesUrl ||
                p1.assetStore?.productId != p2.assetStore?.productId ||
                !p1.registry.IsEquivalentTo(p2.registry) ||
                !p1.compliance.IsEquivalentTo(p2.compliance))
                return true;

            if (p1.source is PackageSource.BuiltIn or PackageSource.Registry)
                return false;

            if (p1.source == PackageSource.Git)
                return p1.git.hash != p2.git?.hash || p1.git.revision != p2.git?.revision;

            return true;
        }

        public void Clear()
        {
            m_ByName.Clear();
            m_ByProductId.Clear();
            m_TimestampByName.Clear();
            m_Serialized = Array.Empty<PackageInfo>();
            m_SerializedTimestamps = Array.Empty<long>();
            m_Timestamp = -1;
        }

        private void RebuildByProductId()
        {
            m_ByProductId.Clear();
            foreach (var info in m_ByName.Values)
            {
                var productId = info.ParseProductId();
                if (productId > 0)
                    m_ByProductId[productId] = info;
            }
        }

        public void OnBeforeSerialize()
        {
            m_Serialized = m_ByName.Values.ToArray();
            m_SerializedTimestamps = m_TimestampByName.Count > 0
                ? m_Serialized.Select(p => m_TimestampByName.GetValueOrDefault(p.name)).ToArray()
                : Array.Empty<long>();
        }

        public void OnAfterDeserialize()
        {
            m_ByName = m_Serialized.ToDictionary(p => p.name);
            m_TimestampByName = new Dictionary<string, long>();
            for (var i = 0; i < m_SerializedTimestamps.Length && i < m_Serialized.Length; i++)
                m_TimestampByName[m_Serialized[i].name] = m_SerializedTimestamps[i];
            RebuildByProductId();
        }
    }

    [Serializable]
    internal class UpmCache : BaseService<IUpmCache>, IUpmCache, ISerializationCallbackReceiver
    {
        [SerializeField]
        private PackageInfoDictionary m_SearchPackageInfos = new();
        [SerializeField]
        private PackageInfoDictionary m_InstalledPackageInfos = new();
        [SerializeField]
        private PackageInfoDictionary m_NonDiscoverableSearchInfos = new();

        private Dictionary<string, Dictionary<string, PackageInfo>> m_ExtraPackageInfosByVersion = new();

        private readonly Dictionary<string, Dictionary<string, object>> m_ParsedUpmReserved = new();

        private HashSet<string> m_LoadAllVersions = new();

        // arrays created to help serialize dictionaries
        [SerializeField]
        private List<PackageInfo> m_SerializedExtraPackageInfosByVersion = new ();
        [SerializeField]
        private string[] m_SerializedLoadAllVersions;

        public event Action<string, bool> onLoadAllVersionsChanged = delegate {};
        public event Action<IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)>, PackagesChangedSource> onPackageInfosUpdated;
        public event Action<PackageInfo> onExtraPackageInfoFetched;
        public event Action onScopedRegistriesPotentiallyChanged;

        public IReadOnlyCollection<PackageInfo> discoverableSearchPackageInfos => m_SearchPackageInfos.values;
        public IReadOnlyCollection<PackageInfo> nonDiscoverableSearchPackageInfos => m_NonDiscoverableSearchInfos.values;
        public IReadOnlyCollection<PackageInfo> installedPackageInfos => m_InstalledPackageInfos.values;
        public bool installedPackageInfosReady => m_InstalledPackageInfos.timestamp >= 0;

        private readonly IProjectSettingsProxy m_SettingsProxy;
        public UpmCache(IProjectSettingsProxy settingsProxy)
        {
            m_SettingsProxy = RegisterDependency(settingsProxy);
        }

        public bool IsLoadAllVersions(string packageName)
        {
            return m_LoadAllVersions.Contains(packageName);
        }

        public void SetLoadAllVersions(string packageName, bool value)
        {
            if (string.IsNullOrEmpty(packageName) || value == IsLoadAllVersions(packageName))
                return;
            if (value)
                m_LoadAllVersions.Add(packageName);
            else
                m_LoadAllVersions.Remove(packageName);
            onLoadAllVersionsChanged?.Invoke(packageName, value);
        }

        public void OnBeforeSerialize()
        {
            m_SerializedExtraPackageInfosByVersion.Clear();
            foreach (var infoDictionary in m_ExtraPackageInfosByVersion.Values)
                m_SerializedExtraPackageInfosByVersion.AddRange(infoDictionary.Values);

            m_SerializedLoadAllVersions = m_LoadAllVersions.ToArray();
        }

        public void OnAfterDeserialize()
        {
            foreach (var p in m_SerializedExtraPackageInfosByVersion)
                AddExtraPackageInfoByVersion(p, false);

            m_LoadAllVersions = new HashSet<string>(m_SerializedLoadAllVersions);
        }

        public void AddSearchNonDiscoverableResult(string packageName, PackageInfo packageInfo, long timestamp)
        {
            if (packageInfo == null)
                return;
            var change = m_NonDiscoverableSearchInfos.AddOrUpdate(packageName, packageInfo, timestamp);
            if (change.HasValue)
                TriggerOnPackageInfosUpdated(new[] { change.Value });
        }

        public void AddExtraFetchResult(PackageInfo packageInfo)
        {
            AddExtraPackageInfoByVersion(packageInfo, true);
        }

        private void AddExtraPackageInfoByVersion(PackageInfo packageInfo, bool triggerEvent)
        {
            if (packageInfo == null)
                return;

            if (!m_ExtraPackageInfosByVersion.TryGetValue(packageInfo.name, out var dict))
            {
                dict = new Dictionary<string, PackageInfo>();
                m_ExtraPackageInfosByVersion[packageInfo.name] = dict;
            }
            dict[packageInfo.version] = packageInfo;
            if (triggerEvent)
                onExtraPackageInfoFetched?.Invoke(packageInfo);
        }

        public Dictionary<string, PackageInfo> GetExtraPackageInfos(string packageName) => m_ExtraPackageInfosByVersion.Get(packageName);

        public PackageInfo GetExtraPackageInfo(string packageId)
        {
            var packageIdSplit = packageId?.Split(new[] { '@' }, 2);
            if (packageIdSplit?.Length == 2)
                return GetExtraPackageInfos(packageIdSplit[0])?.Get(packageIdSplit[1]);
            return null;
        }

        public bool IsPackageInstalled(string packageName) => m_InstalledPackageInfos.GetByName(packageName) != null;

        public PackageInfo GetInstalledPackageInfo(string packageName) => m_InstalledPackageInfos.GetByName(packageName);

        public IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> SetInstalledPackageInfos(IEnumerable<PackageInfo> packageInfos, long timestamp = 0, PackagesChangedSource changedSource = PackagesChangedSource.Other)
        {
            var updatedInfos = m_InstalledPackageInfos.ReplaceAll(packageInfos, timestamp);
            if (updatedInfos.Count > 0)
            {
                TriggerOnPackageInfosUpdated(updatedInfos, changedSource);
                DetectScopedRegistriesChanges(updatedInfos, false);
            }
            return updatedInfos;
        }

        public PackageInfo GetSearchPackageInfo(string packageName) => m_SearchPackageInfos.GetByName(packageName) ?? m_NonDiscoverableSearchInfos.GetByName(packageName);

        public PackageInfo GetBestMatchPackageInfo(string packageName, bool isInstalled, string version = null)
        {
            if (string.IsNullOrEmpty(packageName))
                return null;
            if (isInstalled)
                return GetInstalledPackageInfo(packageName);
            var result = GetSearchPackageInfo(packageName) ?? GetInstalledPackageInfo(packageName);
            if (string.IsNullOrEmpty(version) || result?.version == version)
                return result;
            return GetExtraPackageInfos(packageName)?.Get(version) ?? result;
        }

        public IUpmPackageData GetPackageData(string packageName)
        {
            var installedInfo = GetInstalledPackageInfo(packageName);
            var (searchInfo, searchTimestamp) = m_SearchPackageInfos.GetByNameWithTimestamp(packageName) ?? m_NonDiscoverableSearchInfos.GetByNameWithTimestamp(packageName) ?? (null, -1);
            if (installedInfo == null && searchInfo == null)
                return null;
            var isLoadAllVersion = IsLoadAllVersions(packageName);
            return new UpmPackageData(installedInfo, m_InstalledPackageInfos.timestamp, searchInfo, searchTimestamp, isLoadAllVersion, GetExtraPackageInfos(packageName));
        }

        public IUpmPackageData GetPackageData(long productId)
        {
            // We check non-discoverable search info first because UpmOnAssetStorePackage are currently not discoverable, but that could change in the future
            // so we still fall back to looking at discoverable search infos
            var packageName = m_NonDiscoverableSearchInfos.GetByProductId(productId)?.name ??
                              m_SearchPackageInfos.GetByProductId(productId)?.name ??
                              m_InstalledPackageInfos.GetByProductId(productId)?.name;
            return string.IsNullOrEmpty(packageName) ? null : GetPackageData(packageName);
        }

        public void SetSearchPackageInfos(IEnumerable<PackageInfo> packageInfos, long timestamp)
        {
            var updatedInfos = m_SearchPackageInfos.ReplaceAll(packageInfos, timestamp);
            if (updatedInfos.Count > 0)
            {
                TriggerOnPackageInfosUpdated(updatedInfos);
                DetectScopedRegistriesChanges(updatedInfos, true);
            }
        }

        // This is to detected changes to the scoped registry compliance data, as that is something that will change without the users modifying the project manifest.
        // We don't want to call the API to get the registry list all the time, instead, we want to take a look at the packages we receive from List and Search calls and
        // detect changes to the scoped registries that way.
        private void DetectScopedRegistriesChanges(IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> packageInfos, bool isSearchResult)
        {
            if (m_SettingsProxy.registries == null || m_SettingsProxy.registries.Count < 2)
                return;

            var registriesToCheck = m_SettingsProxy.scopedRegistries.ToDictionary(r => r.name, r => r);
            // We use a HashSet to make sure we only check each registry once, because packages from the same registry will share the same RegistryInfo
            var registriesChecked = new HashSet<string>();
            foreach (var (_, newInfo) in packageInfos)
            {
                var registry = newInfo?.registry;
                if (!string.IsNullOrEmpty(registry?.name) && !registry.isDefault && !registriesChecked.Contains(registry.name) && registriesToCheck.TryGetValue(registry.name, out var result))
                {
                    if (!result.IsEquivalentTo(registry))
                    {
                        onScopedRegistriesPotentiallyChanged?.Invoke();
                        return;
                    }
                    registriesChecked.Add(registry.name);
                }
            }

            // In the case where the result is from a search request, we need to do an additional check because it's possible that some packages disappeared from the search result
            // due to the scoped registry becoming non-compliant. Whereas List request results are not affected by this.
            if (registriesToCheck.Count == registriesChecked.Count || !isSearchResult)
                return;

            foreach (var registry in registriesChecked)
                registriesToCheck.Remove(registry);
            registriesChecked.Clear();

            foreach (var (oldInfo, newInfo) in packageInfos)
            {
                if (newInfo != null || oldInfo == null || registriesChecked.Contains(oldInfo.registry.name))
                    continue;

                // When we find a package that used to be in the search request but not anymore, and their scoped registry is still in the list of registries, we flag this as a potential change
                // to the scoped registry compliance data. There could be other cause of this, such as packages removed from the server directly.
                if (registriesToCheck.ContainsKey(oldInfo.registry.name))
                {
                    onScopedRegistriesPotentiallyChanged?.Invoke();
                    return;
                }
                registriesChecked.Add(oldInfo.registry.name);
            }
        }

        private void TriggerOnPackageInfosUpdated(IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> packageInfos, PackagesChangedSource changeSource = PackagesChangedSource.Other)
        {
            foreach (var (oldInfo, newInfo) in packageInfos)
            {
                if (!string.IsNullOrEmpty(oldInfo?.packageId))
                    m_ParsedUpmReserved.Remove(oldInfo.packageId);
                if (!string.IsNullOrEmpty(newInfo?.packageId))
                    m_ParsedUpmReserved.Remove(newInfo.packageId);
            }
            onPackageInfosUpdated?.Invoke(packageInfos, changeSource);
        }

        public Dictionary<string, object> ParseUpmReserved(PackageInfo packageInfo)
        {
            if (string.IsNullOrEmpty(packageInfo?.upmReserved))
                return null;

            if (!m_ParsedUpmReserved.TryGetValue(packageInfo.packageId, out var result))
            {
                result = Json.Deserialize(packageInfo.upmReserved) as Dictionary<string, object>;
                m_ParsedUpmReserved[packageInfo.packageId] = result;
            }
            return result;
        }

        public void ClearCache()
        {
            m_InstalledPackageInfos.Clear();

            m_SearchPackageInfos.Clear();

            ClearNonDiscoverableSearchInfos();
            ClearExtraInfoCache();
        }

        public void ClearNonDiscoverableSearchInfos()
        {
            m_NonDiscoverableSearchInfos.Clear();
        }

        public void ClearExtraInfoCache()
        {
            m_ExtraPackageInfosByVersion.Clear();
        }
    }
}
