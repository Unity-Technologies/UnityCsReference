// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
        IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> SetInstalledPackageInfos(IEnumerable<PackageInfo> packageInfos, long timestamp = 0, PackagesChangedSource changedSource = PackagesChangedSource.Other);
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

            m_LoadAllVersions.ToArray(ref m_SerializedLoadAllVersions);
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
            if (m_SettingsProxy.scopedRegistries == null || m_SettingsProxy.scopedRegistries.Count == 0)
                return;

            var registriesToCheck = m_SettingsProxy.scopedRegistries.ToNewDictionary(r => r.name);
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

        private void TriggerOnPackageInfosUpdated(IReadOnlyCollection<(PackageInfo oldInfo, PackageInfo newInfo)> packageInfos, PackagesChangedSource changedSource = PackagesChangedSource.Other)
        {
            foreach (var (oldInfo, newInfo) in packageInfos)
            {
                if (!string.IsNullOrEmpty(oldInfo?.packageId))
                    m_ParsedUpmReserved.Remove(oldInfo.packageId);
                if (!string.IsNullOrEmpty(newInfo?.packageId))
                    m_ParsedUpmReserved.Remove(newInfo.packageId);
            }
            onPackageInfosUpdated?.Invoke(packageInfos, changedSource);
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
