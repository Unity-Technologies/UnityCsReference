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
        event Action<IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)>> onPackageInfosUpdated;
        event Action<PackageInfo> onExtraPackageInfoFetched;
        event Action onScopedRegistriesPotentiallyChanged;

        IEnumerable<PackageInfo> searchPackageInfos { get; }
        IEnumerable<PackageInfo> installedPackageInfos  { get; }

        void SetLoadAllVersions(string packageUniqueId, bool value);
        void AddExtraPackageInfo(PackageInfo packageInfo);
        PackageInfo GetExtraPackageInfo(string packageId);
        void RemoveInstalledPackageInfo(string packageName);
        PackageInfo GetInstalledPackageInfo(string packageName);
        PackageInfo GetInstalledPackageInfoById(string packageId);
        bool SetInstalledPackageInfo(PackageInfo info, string packageNamePreInstall);
        void SetInstalledPackageInfos(IEnumerable<PackageInfo> packageInfos, long timestamp = 0);
        PackageInfo GetSearchPackageInfo(string packageName);
        PackageInfo GetBestMatchPackageInfo(string packageName, bool isInstalled, string version = null);
        IUpmPackageData GetPackageData(string packageName);
        IUpmPackageData GetPackageData(long productId);
        void SetSearchPackageInfos(IEnumerable<PackageInfo> packageInfos, long timestamp);
        PackageInfo GetProductSearchPackageInfo(long productId);
        void SetProductSearchPackageInfo(long productId, PackageInfo info, long timestamp);
        Dictionary<string, object> ParseUpmReserved(PackageInfo packageInfo);
        void ClearCache();
        void ClearProductCache();
        void ClearExtraInfoCache();
    }

    [Serializable]
    internal class UpmCache : BaseService<IUpmCache>, IUpmCache, ISerializationCallbackReceiver
    {
        private Dictionary<string, PackageInfo> m_SearchPackageInfos = new();
        private Dictionary<string, PackageInfo> m_PackageNameToInstalledPackageInfosMap = new();
        private Dictionary<long, PackageInfo> m_ProductIdToInstalledPackageInfosMap = new();

        private Dictionary<long, (PackageInfo info, long timestamp)> m_ProductIdToProductSearchInfosMap = new();

        private Dictionary<string, Dictionary<string, PackageInfo>> m_ExtraPackageInfo = new();

        private readonly Dictionary<string, Dictionary<string, object>> m_ParsedUpmReserved = new();

        private HashSet<string> m_LoadAllVersions = new();

        [SerializeField]
        private long m_SearchPackageInfosTimestamp;

        [SerializeField]
        private long m_InstalledPackageInfosTimestamp;

        // arrays created to help serialize dictionaries
        [SerializeField]
        private PackageInfo[] m_SerializedInstalledPackageInfos;
        [SerializeField]
        private PackageInfo[] m_SerializedSearchPackageInfos;
        [SerializeField]
        private long[] m_SerializedProductSearchPackageInfoProductIds;
        [SerializeField]
        private PackageInfo[] m_SerializedProductSearchPackageInfos;
        [SerializeField]
        private long[] m_SerializedProductSearchPackageInfoTimestamps;
        [SerializeField]
        private PackageInfo[] m_SerializedExtraPackageInfos;
        [SerializeField]
        private string[] m_SerializedLoadAllVersions;

        public event Action<string, bool> onLoadAllVersionsChanged = delegate {};
        public event Action<IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)>> onPackageInfosUpdated;
        public event Action<PackageInfo> onExtraPackageInfoFetched;
        public event Action onScopedRegistriesPotentiallyChanged;

        public IEnumerable<PackageInfo> searchPackageInfos => m_SearchPackageInfos.Values;
        public IEnumerable<PackageInfo> installedPackageInfos => m_PackageNameToInstalledPackageInfosMap.Values;

        private readonly IUniqueIdMapper m_UniqueIdMapper;
        private readonly IProjectSettingsProxy m_SettingsProxy;
        public UpmCache(IUniqueIdMapper uniqueIdMapper, IProjectSettingsProxy settingsProxy)
        {
            m_UniqueIdMapper = RegisterDependency(uniqueIdMapper);
            m_SettingsProxy = RegisterDependency(settingsProxy);
        }

        private static IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> FindUpdatedPackageInfos(Dictionary<string, PackageInfo> oldInfos, Dictionary<string, PackageInfo> newInfos)
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
                p1.documentationUrl != p2.documentationUrl ||
                p1.changelogUrl != p2.changelogUrl ||
                p1.licensesUrl != p2.licensesUrl ||
                p1.assetStore?.productId != p2.assetStore?.productId ||
                !p1.registry.IsEquivalentTo(p2.registry) ||
                !p1.compliance.IsEquivalentTo(p2.compliance))
                return true;

            if (p1.source == PackageSource.BuiltIn || p1.source == PackageSource.Registry)
                return false;

            if (p1.source == PackageSource.Git)
                return p1.git.hash != p2.git?.hash || p1.git.revision != p2.git?.revision;

            return true;
        }
        public bool IsLoadAllVersions(string packageUniqueId)
        {
            return m_LoadAllVersions.Contains(packageUniqueId);
        }

        public void SetLoadAllVersions(string packageUniqueId, bool value)
        {
            if (string.IsNullOrEmpty(packageUniqueId) || value == IsLoadAllVersions(packageUniqueId))
                return;
            if (value)
                m_LoadAllVersions.Add(packageUniqueId);
            else
                m_LoadAllVersions.Remove(packageUniqueId);
            onLoadAllVersionsChanged?.Invoke(packageUniqueId, value);
        }

        public void OnBeforeSerialize()
        {
            m_SerializedInstalledPackageInfos = m_PackageNameToInstalledPackageInfosMap.Values.ToArray();
            m_SerializedSearchPackageInfos = m_SearchPackageInfos.Values.ToArray();
            m_SerializedExtraPackageInfos = m_ExtraPackageInfo.Values.SelectMany(p => p.Values).ToArray();

            m_SerializedProductSearchPackageInfoProductIds = m_ProductIdToProductSearchInfosMap.Keys.ToArray();
            m_SerializedProductSearchPackageInfos = m_ProductIdToProductSearchInfosMap.Values.Select(i => i.info).ToArray();
            m_SerializedProductSearchPackageInfoTimestamps = m_ProductIdToProductSearchInfosMap.Values.Select(i => i.timestamp).ToArray();

            m_SerializedLoadAllVersions = m_LoadAllVersions.ToArray();
        }

        public void OnAfterDeserialize()
        {
            m_PackageNameToInstalledPackageInfosMap = m_SerializedInstalledPackageInfos.ToDictionary(p => p.name, p => p);
            foreach (var info in m_SerializedInstalledPackageInfos)
            {
                m_PackageNameToInstalledPackageInfosMap[info.name] = info;
                UpdateProductIdToInstalledPackageInfoMap(null, info);
            }

            m_SearchPackageInfos = m_SerializedSearchPackageInfos.ToDictionary(p => p.name, p => p);

            foreach (var p in m_SerializedExtraPackageInfos)
                AddExtraPackageInfo(p);

            for (var i = 0; i < m_SerializedProductSearchPackageInfoProductIds.Length; i++)
            {
                var productId = m_SerializedProductSearchPackageInfoProductIds[i];
                var info = m_SerializedProductSearchPackageInfos[i];
                var timestamp = m_SerializedProductSearchPackageInfoTimestamps[i];
                m_ProductIdToProductSearchInfosMap[productId] = (info, timestamp);
            }

            m_LoadAllVersions = new HashSet<string>(m_SerializedLoadAllVersions);
        }

        public void AddExtraPackageInfo(PackageInfo packageInfo)
        {
            if (packageInfo == null)
                return;

            if (!m_ExtraPackageInfo.TryGetValue(packageInfo.name, out var dict))
            {
                dict = new Dictionary<string, PackageInfo>();
                m_ExtraPackageInfo[packageInfo.name] = dict;
            }
            dict[packageInfo.version] = packageInfo;
            onExtraPackageInfoFetched?.Invoke(packageInfo);
        }

        public Dictionary<string, PackageInfo> GetExtraPackageInfos(string packageName) => m_ExtraPackageInfo.Get(packageName);

        public PackageInfo GetExtraPackageInfo(string packageId)
        {
            var packageIdSplit = packageId?.Split(new[] { '@' }, 2);
            if (packageIdSplit?.Length == 2)
                return GetExtraPackageInfos(packageIdSplit[0])?.Get(packageIdSplit[1]);
            return null;
        }

        public void RemoveInstalledPackageInfo(string packageName)
        {
            var oldInfo = m_PackageNameToInstalledPackageInfosMap.Get(packageName);
            if (oldInfo == null)
                return;

            m_PackageNameToInstalledPackageInfosMap.Remove(packageName);
            UpdateProductIdToInstalledPackageInfoMap(oldInfo, null);
            TriggerOnPackageInfosUpdated(new [] { (oldInfo, (PackageInfo)null) });
        }

        public bool IsPackageInstalled(string packageName) => m_PackageNameToInstalledPackageInfosMap.ContainsKey(packageName);

        public PackageInfo GetInstalledPackageInfo(string packageName) => m_PackageNameToInstalledPackageInfosMap.Get(packageName);

        public PackageInfo GetProductInstalledPackageInfo(long productId) => m_ProductIdToInstalledPackageInfosMap.GetValueOrDefault(productId);

        public PackageInfo GetInstalledPackageInfoById(string packageId)
        {
            var idSplit = packageId?.Split(new[] { '@' }, 2);
            return idSplit?.Length == 2 ? GetInstalledPackageInfo(idSplit[0]) : null;
        }

        public bool SetInstalledPackageInfo(PackageInfo info, string packageNamePreInstall)
        {
            var oldInfo = m_PackageNameToInstalledPackageInfosMap.Get(info.name);
            m_PackageNameToInstalledPackageInfosMap[info.name] = info;
            UpdateProductIdToInstalledPackageInfoMap(oldInfo, info);
            m_UniqueIdMapper.MapTempIdAndFinalizedId(packageNamePreInstall, info.name);
            if (packageNamePreInstall != info.name || oldInfo == null || IsDifferent(oldInfo, info))
            {
                TriggerOnPackageInfosUpdated(new [] { (oldInfo, info) });
                return true;
            }
            return false;
        }

        private void UpdateProductIdToInstalledPackageInfoMap(PackageInfo oldInfo, PackageInfo newInfo)
        {
            var oldProductId = oldInfo?.ParseProductId() ?? 0;
            var newProductId = newInfo?.ParseProductId() ?? 0;
            if (oldProductId != newProductId && oldProductId > 0)
                m_ProductIdToInstalledPackageInfosMap.Remove(oldProductId);
            if (newProductId > 0)
                m_ProductIdToInstalledPackageInfosMap[newProductId] = newInfo;
        }

        public void SetInstalledPackageInfos(IEnumerable<PackageInfo> packageInfos, long timestamp = 0)
        {
            var newPackageInfos = packageInfos.ToDictionary(p => p.name, p => p);

            var oldPackageInfos = m_PackageNameToInstalledPackageInfosMap;
            m_PackageNameToInstalledPackageInfosMap = newPackageInfos;

            m_ProductIdToInstalledPackageInfosMap.Clear();
            foreach (var info in newPackageInfos.Values)
                UpdateProductIdToInstalledPackageInfoMap(null, info);

            var updatedInfos = FindUpdatedPackageInfos(oldPackageInfos, newPackageInfos);
            if (updatedInfos.Count > 0)
            {
                TriggerOnPackageInfosUpdated(updatedInfos);
                DetectScopedRegistriesChanges(updatedInfos, false);
            }
            m_InstalledPackageInfosTimestamp = timestamp;
        }

        public PackageInfo GetSearchPackageInfo(string packageName) => m_SearchPackageInfos.Get(packageName);

        public PackageInfo GetBestMatchPackageInfo(string packageName, bool isInstalled, string version = null)
        {
            if (string.IsNullOrEmpty(packageName))
                return null;
            if (isInstalled)
                return GetInstalledPackageInfo(packageName);
            var searchInfo = GetSearchPackageInfo(packageName) ?? GetInstalledPackageInfo(packageName);
            if (string.IsNullOrEmpty(version) || searchInfo?.version == version)
                return searchInfo;
            return GetExtraPackageInfos(packageName)?.Get(version) ?? searchInfo;
        }

        public IUpmPackageData GetPackageData(string packageName)
        {
            var installedInfo = GetInstalledPackageInfo(packageName);
            var searchInfo = GetSearchPackageInfo(packageName);
            if (installedInfo == null && searchInfo == null)
                return null;
            var isLoadAllVersion = IsLoadAllVersions(packageName);
            return new UpmPackageData(installedInfo, m_InstalledPackageInfosTimestamp, searchInfo, m_SearchPackageInfosTimestamp, isLoadAllVersion, GetExtraPackageInfos(packageName));
        }

        public IUpmPackageData GetPackageData(long productId)
        {
            var installedInfo = GetProductInstalledPackageInfo(productId);
            var searchInfo = GetProductSearchPackageInfo(productId);
            if (installedInfo == null && searchInfo == null)
                return null;
            // We check installed info first because when we switch between scoped registries, we would clear the product search infos immediately
            // while the installed info will remain until the next list result comes in
            var packageName = installedInfo?.name ?? searchInfo?.name;
            var isLoadAllVersion = IsLoadAllVersions(productId.ToString());
            return new UpmPackageData(installedInfo, m_InstalledPackageInfosTimestamp, searchInfo, m_SearchPackageInfosTimestamp, isLoadAllVersion, GetExtraPackageInfos(packageName));
        }

        public void SetSearchPackageInfos(IEnumerable<PackageInfo> packageInfos, long timestamp)
        {
            var newPackageInfos = packageInfos.ToDictionary(p => p.name, p => p);

            var oldPackageInfos = m_SearchPackageInfos;
            m_SearchPackageInfos = newPackageInfos;

            var updatedInfos = FindUpdatedPackageInfos(oldPackageInfos, newPackageInfos);
            if (updatedInfos.Count > 0)
            {
                TriggerOnPackageInfosUpdated(updatedInfos);
                DetectScopedRegistriesChanges(updatedInfos, true);
            }
            m_SearchPackageInfosTimestamp = timestamp;
        }

        public PackageInfo GetProductSearchPackageInfo(long productId) => GetProductSearchPackageInfoAndTimestamp(productId).info;

        private (PackageInfo info, long timestamp) GetProductSearchPackageInfoAndTimestamp(long productId)
            => productId > 0 && m_ProductIdToProductSearchInfosMap.TryGetValue(productId, out var result) ? result : (info: null, timestamp: 0);

        public void SetProductSearchPackageInfo(long productId, PackageInfo info, long timestamp)
        {
            var oldInfo = GetProductSearchPackageInfo(productId);
            m_ProductIdToProductSearchInfosMap[productId] = (info, timestamp);
            if (oldInfo == null || IsDifferent(oldInfo, info))
                TriggerOnPackageInfosUpdated(new [] { (oldInfo, newInfo: info) });
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

        private void TriggerOnPackageInfosUpdated(IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> packageInfos)
        {
            foreach (var (oldInfo, newInfo) in packageInfos)
            {
                if (!string.IsNullOrEmpty(oldInfo?.packageId))
                    m_ParsedUpmReserved.Remove(oldInfo.packageId);
                if (!string.IsNullOrEmpty(newInfo?.packageId))
                    m_ParsedUpmReserved.Remove(newInfo.packageId);
            }
            onPackageInfosUpdated?.Invoke(packageInfos);
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
            m_PackageNameToInstalledPackageInfosMap.Clear();
            m_ProductIdToInstalledPackageInfosMap.Clear();

            m_SearchPackageInfos.Clear();

            m_SerializedInstalledPackageInfos = new PackageInfo[0];
            m_SerializedSearchPackageInfos = new PackageInfo[0];

            ClearProductCache();
            ClearExtraInfoCache();
        }

        public void ClearProductCache()
        {
            m_ProductIdToProductSearchInfosMap.Clear();
            m_SerializedProductSearchPackageInfos = new PackageInfo[0];
        }

        public void ClearExtraInfoCache()
        {
            m_ExtraPackageInfo.Clear();
            m_SerializedExtraPackageInfos = new PackageInfo[0];
        }
    }
}
