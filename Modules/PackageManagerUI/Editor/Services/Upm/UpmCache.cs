// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class UpmCache
    {
        static IUpmCache s_Instance = null;
        public static IUpmCache instance { get { return s_Instance ?? UpmCacheInternal.instance; } }

        [Serializable]
        internal class UpmCacheInternal : ScriptableSingleton<UpmCacheInternal>, IUpmCache, ISerializationCallbackReceiver
        {
            private Dictionary<string, PackageInfo> m_SearchPackageInfos = new Dictionary<string, PackageInfo>();
            private Dictionary<string, PackageInfo> m_InstalledPackageInfos = new Dictionary<string, PackageInfo>();
            private Dictionary<string, PackageInfo> m_ProductPackageInfos = new Dictionary<string, PackageInfo>();

            private Dictionary<string, Dictionary<string, PackageInfo>> m_ExtraPackageInfo = new Dictionary<string, Dictionary<string, PackageInfo>>();

            // the mapping between package name (key) to asset store product id (value)
            private Dictionary<string, string> m_ProductIdMap = new Dictionary<string, string>();

            // arrays created to help serialize dictionaries
            [SerializeField]
            private PackageInfo[] m_SerializedInstalledPackageInfos;
            [SerializeField]
            private PackageInfo[] m_SerializedSearchPackageInfos;
            [SerializeField]
            private PackageInfo[] m_SerializedProductPackageInfos;
            [SerializeField]
            private PackageInfo[] m_SerializedExtraPackageInfos;
            [SerializeField]
            private string[] m_SerializedProductIdMapKeys;
            [SerializeField]
            private string[] m_SerializedProductIdMapValues;

            public event Action<IEnumerable<PackageInfo>> onPackageInfosUpdated;

            public IEnumerable<PackageInfo> searchPackageInfos => m_SearchPackageInfos.Values;
            public IEnumerable<PackageInfo> installedPackageInfos => m_InstalledPackageInfos.Values;
            public IEnumerable<PackageInfo> productPackageInfos => m_ProductPackageInfos.Values;

            private static List<PackageInfo> FindUpdatedPackageInfos(Dictionary<string, PackageInfo> oldInfos, Dictionary<string, PackageInfo> newInfos)
            {
                PackageInfo info;
                return newInfos.Values.Where(p => { return !oldInfos.TryGetValue(p.name, out info) || IsDifferent(info, p); })
                    .Concat(oldInfos.Values.Where(p => { return !newInfos.TryGetValue(p.name, out info); })).ToList();
            }

            // we want to only compare a subset of PackageInfo attributes
            // because most attributes never change if their PackageId is the same.
            private static bool IsDifferent(PackageInfo oldInfo, PackageInfo newInfo)
            {
                if (oldInfo.packageId != newInfo.packageId ||
                    oldInfo.source != newInfo.source ||
                    oldInfo.resolvedPath != newInfo.resolvedPath ||
                    oldInfo.isDirectDependency != newInfo.isDirectDependency ||
                    oldInfo.entitlements.isAllowed != newInfo.entitlements.isAllowed)
                    return true;

                var oldVersions = oldInfo.versions.compatible;
                var newVersions = newInfo.versions.compatible;
                if (oldVersions.Length != newVersions.Length || !oldVersions.SequenceEqual(newVersions))
                    return true;

                if (oldInfo.errors.Length != newInfo.errors.Length || !oldInfo.errors.SequenceEqual(newInfo.errors))
                    return true;

                return false;
            }

            public void OnBeforeSerialize()
            {
                m_SerializedInstalledPackageInfos = m_InstalledPackageInfos.Values.ToArray();
                m_SerializedSearchPackageInfos = m_SearchPackageInfos.Values.ToArray();
                m_SerializedProductPackageInfos = m_ProductPackageInfos.Values.ToArray();
                m_SerializedExtraPackageInfos = m_ExtraPackageInfo.Values.SelectMany(p => p.Values).ToArray();
                m_SerializedProductIdMapKeys = m_ProductIdMap.Keys.ToArray();
                m_SerializedProductIdMapValues = m_ProductIdMap.Values.ToArray();
            }

            public void OnAfterDeserialize()
            {
                foreach (var p in m_SerializedInstalledPackageInfos)
                    m_InstalledPackageInfos[p.name] = p;

                foreach (var p in m_SerializedSearchPackageInfos)
                    m_SearchPackageInfos[p.name] = p;

                m_ProductPackageInfos = m_SerializedProductPackageInfos.ToDictionary(p => p.name, p => p);

                foreach (var p in m_SerializedExtraPackageInfos)
                    AddExtraPackageInfo(p);

                for (var i = 0; i < m_SerializedProductIdMapKeys.Length; i++)
                    m_ProductIdMap[m_SerializedProductIdMapKeys[i]] = m_SerializedProductIdMapValues[i];
            }

            public void AddExtraPackageInfo(PackageInfo packageInfo)
            {
                Dictionary<string, PackageInfo> dict;
                if (!m_ExtraPackageInfo.TryGetValue(packageInfo.name, out dict))
                {
                    dict = new Dictionary<string, PackageInfo>();
                    m_ExtraPackageInfo[packageInfo.name] = dict;
                }
                dict[packageInfo.version] = packageInfo;
            }

            public Dictionary<string, PackageInfo> GetExtraPackageInfos(string packageName) => m_ExtraPackageInfo.Get(packageName);

            public void RemoveInstalledPackageInfo(string packageName)
            {
                var oldInfo = m_InstalledPackageInfos.Get(packageName);
                if (oldInfo == null)
                    return;

                m_InstalledPackageInfos.Remove(packageName);
                onPackageInfosUpdated?.Invoke(new PackageInfo[] { oldInfo });

                if (IsPreviewInstalled(oldInfo))
                    OnInstalledPreviewPackagesChanged();
            }

            public bool IsPackageInstalled(string packageName) => m_InstalledPackageInfos.ContainsKey(packageName);

            public PackageInfo GetInstalledPackageInfo(string packageName) => m_InstalledPackageInfos.Get(packageName);

            public void SetInstalledPackageInfo(PackageInfo info)
            {
                var oldInfo = m_InstalledPackageInfos.Get(info.name);
                m_InstalledPackageInfos[info.name] = info;
                if (oldInfo == null || IsDifferent(oldInfo, info))
                    onPackageInfosUpdated?.Invoke(new PackageInfo[] { info });

                if (IsPreviewInstalled(oldInfo) || IsPreviewInstalled(info))
                    OnInstalledPreviewPackagesChanged();
            }

            public void SetInstalledPackageInfos(IEnumerable<PackageInfo> packageInfos)
            {
                var newPackageInfos = packageInfos.ToDictionary(p => p.name, p => p);

                var oldPackageInfos = m_InstalledPackageInfos;
                m_InstalledPackageInfos = newPackageInfos;

                var updatedInfos = FindUpdatedPackageInfos(oldPackageInfos, newPackageInfos);

                if (updatedInfos.Any())
                {
                    onPackageInfosUpdated?.Invoke(updatedInfos);
                    OnInstalledPreviewPackagesChanged();
                }
            }

            public PackageInfo GetSearchPackageInfo(string packageName) => m_SearchPackageInfos.Get(packageName);
            public void SetSearchPackageInfos(IEnumerable<PackageInfo> packageInfos)
            {
                var newPackageInfos = packageInfos.ToDictionary(p => p.name, p => p);

                var oldPackageInfos = m_SearchPackageInfos;
                m_SearchPackageInfos = newPackageInfos;

                var updatedInfos = FindUpdatedPackageInfos(oldPackageInfos, newPackageInfos);
                if (updatedInfos.Any())
                    onPackageInfosUpdated?.Invoke(updatedInfos);
            }

            public PackageInfo GetProductPackageInfo(string packageName) => m_ProductPackageInfos.Get(packageName);
            public void SetProductPackageInfo(string productId, PackageInfo info)
            {
                m_ProductIdMap[info.name] = productId;
                var oldInfo = m_ProductPackageInfos.Get(info.name);
                m_ProductPackageInfos[info.name] = info;
                if (oldInfo == null || IsDifferent(oldInfo, info))
                    onPackageInfosUpdated?.Invoke(new PackageInfo[] { info });
            }

            public string GetProductId(string packageName) => m_ProductIdMap.Get(packageName);

            private static bool IsPreviewInstalled(PackageInfo packageInfo)
            {
                SemVersion? packageInfoVersion;
                bool isPackageInfoVersionParsed = SemVersionParser.TryParse(packageInfo?.version, out packageInfoVersion);

                return packageInfo?.isDirectDependency == true &&
                    packageInfo.source == PackageSource.Registry && isPackageInfoVersionParsed && !((SemVersion)packageInfoVersion).IsRelease();
            }

            private void OnInstalledPreviewPackagesChanged()
            {
                if (!PackageManagerPrefs.instance.hasShowPreviewPackagesKey)
                    PackageManagerPrefs.instance.showPreviewPackagesFromInstalled = UpmCache.instance.installedPackageInfos.Any(p => {
                        SemVersion? version;
                        bool isVersionParsed = SemVersionParser.TryParse(p.version, out version);
                        return isVersionParsed && !((SemVersion)version).IsRelease();
                    });
            }

            public void ClearCache()
            {
                m_InstalledPackageInfos.Clear();
                m_SearchPackageInfos.Clear();
                m_ExtraPackageInfo.Clear();
                m_ProductIdMap.Clear();

                m_SerializedInstalledPackageInfos = new PackageInfo[0];
                m_SerializedSearchPackageInfos = new PackageInfo[0];
                m_SerializedExtraPackageInfos = new PackageInfo[0];

                ClearProductCache();
            }

            public void ClearProductCache()
            {
                m_ProductPackageInfos.Clear();
                m_ProductIdMap.Clear();

                m_SerializedProductPackageInfos = new PackageInfo[0];
                m_SerializedProductIdMapKeys = new string[0];
                m_SerializedProductIdMapValues = new string[0];
            }
        }
    }
}
