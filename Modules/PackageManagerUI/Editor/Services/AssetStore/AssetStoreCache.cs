// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class AssetStoreCache : ISerializationCallbackReceiver
    {
        private Dictionary<string, long> m_Categories = new Dictionary<string, long>();

        private Dictionary<long, AssetStorePurchaseInfo> m_PurchaseInfos = new Dictionary<long, AssetStorePurchaseInfo>();

        private Dictionary<long, AssetStoreProductInfo> m_ProductInfos = new Dictionary<long, AssetStoreProductInfo>();

        private Dictionary<long, AssetStoreLocalInfo> m_LocalInfos = new Dictionary<long, AssetStoreLocalInfo>();

        private Dictionary<long, AssetStoreUpdateInfo> m_UpdateInfos = new Dictionary<long, AssetStoreUpdateInfo>();

        // We use the path string as the key for each imported asset
        private Dictionary<string, Asset> m_ImportedAssets = new Dictionary<string, Asset>();
        private Dictionary<long, AssetStoreImportedPackage> m_ImportedPackages = new Dictionary<long, AssetStoreImportedPackage>();

        [SerializeField]
        private string[] m_SerializedCategories = new string[0];

        [SerializeField]
        private long[] m_SerializedCategoryCounts = new long[0];

        [SerializeField]
        private AssetStorePurchaseInfo[] m_SerializedPurchaseInfos = new AssetStorePurchaseInfo[0];

        [SerializeField]
        private AssetStoreProductInfo[] m_SerializedProductInfos = new AssetStoreProductInfo[0];

        [SerializeField]
        private AssetStoreLocalInfo[] m_SerializedLocalInfos = new AssetStoreLocalInfo[0];

        [SerializeField]
        private AssetStoreUpdateInfo[] m_SerializedUpdateInfos = new AssetStoreUpdateInfo[0];

        [SerializeField]
        private Asset[] m_SerializedImportedAssets = new Asset[0];

        public virtual event Action<IEnumerable<AssetStoreLocalInfo> /*addedOrUpdated*/, IEnumerable<AssetStoreLocalInfo> /*removed*/> onLocalInfosChanged;
        public virtual event Action<AssetStoreProductInfo> onProductInfoChanged;
        public virtual event Action<IEnumerable<AssetStorePurchaseInfo>> onPurchaseInfosChanged;
        public virtual event Action<IEnumerable<AssetStoreUpdateInfo>> onUpdateInfosChanged;
        public virtual event Action<IEnumerable<AssetStoreImportedPackage> /*addedOrUpdated*/, IEnumerable<AssetStoreImportedPackage> /*removed*/> onImportedPackagesChanged;

        public virtual IEnumerable<AssetStoreLocalInfo> localInfos => m_LocalInfos.Values;

        public virtual IEnumerable<AssetStoreImportedPackage> importedPackages => m_ImportedPackages.Values;
        public virtual IEnumerable<Asset> importedAssets => m_ImportedAssets.Values;

        [NonSerialized]
        private ApplicationProxy m_Application;
        [NonSerialized]
        private HttpClientFactory m_HttpClientFactory;
        [NonSerialized]
        private IOProxy m_IOProxy;
        [NonSerialized]
        private UniqueIdMapper m_UniqueIdMapper;
        public void ResolveDependencies(ApplicationProxy application,
            HttpClientFactory httpClientFactory,
            IOProxy iOProxy,
            UniqueIdMapper uniqueIdMapper)
        {
            m_Application = application;
            m_HttpClientFactory = httpClientFactory;
            m_IOProxy = iOProxy;
            m_UniqueIdMapper = uniqueIdMapper;
        }

        public void OnBeforeSerialize()
        {
            m_SerializedCategories = m_Categories.Keys.ToArray();
            m_SerializedCategoryCounts = m_Categories.Values.ToArray();

            m_SerializedPurchaseInfos = m_PurchaseInfos.Values.ToArray();
            m_SerializedProductInfos = m_ProductInfos.Values.ToArray();
            m_SerializedLocalInfos = m_LocalInfos.Values.ToArray();
            m_SerializedUpdateInfos = m_UpdateInfos.Values.ToArray();

            m_SerializedImportedAssets = m_ImportedAssets.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
            for (var i = 0; i < m_SerializedCategories.Length; i++)
                m_Categories[m_SerializedCategories[i]] = m_SerializedCategoryCounts[i];

            m_PurchaseInfos = m_SerializedPurchaseInfos.ToDictionary(info => info.productId, info => info);
            m_ProductInfos = m_SerializedProductInfos.ToDictionary(info => info.productId, info => info);
            m_LocalInfos = m_SerializedLocalInfos.ToDictionary(info => info.productId, info => info);
            m_UpdateInfos = m_SerializedUpdateInfos.ToDictionary(info => info.productId, info => info);

            m_ImportedAssets = m_SerializedImportedAssets.ToDictionary(asset => asset.importedPath, asset => asset);

            // We don't serialize imported packages, because the list of imported packages can be constructed from imported assets
            foreach (var asset in m_SerializedImportedAssets)
            {
                if (m_ImportedPackages.TryGetValue(asset.origin.productId, out var importedPackage))
                {
                    importedPackage.AddImportedAsset(asset);
                    continue;
                }

                m_ImportedPackages[asset.origin.productId] = new AssetStoreImportedPackage(new List<Asset>() { asset });
            }
        }

        public virtual void SetCategory(string category, long count)
        {
            m_Categories[category] = count;
        }

        public virtual Texture2D LoadImage(long productId, string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            var hash = Hash128.Compute(url);
            try
            {
                var path = m_IOProxy.PathsCombine(m_Application.userAppDataPath, "Asset Store", "Cache", "Images", productId.ToString(), hash.ToString());
                if (m_IOProxy.FileExists(path))
                {
                    var texture = new Texture2D(2, 2);
                    if (texture.LoadImage(m_IOProxy.FileReadAllBytes(path)))
                        return texture;
                }
            }
            catch (System.IO.IOException e)
            {
                Debug.Log($"[Package Manager Window] Cannot load image: {e.Message}");
            }

            return null;
        }

        public virtual void SaveImage(long productId, string url, Texture2D texture)
        {
            if (string.IsNullOrEmpty(url) || texture == null)
                return;

            try
            {
                var path = m_IOProxy.PathsCombine(m_Application.userAppDataPath, "Asset Store", "Cache", "Images", productId.ToString());
                if (!m_IOProxy.DirectoryExists(path))
                    m_IOProxy.CreateDirectory(path);

                var hash = Hash128.Compute(url);
                path = m_IOProxy.PathsCombine(path, hash.ToString());
                m_IOProxy.FileWriteAllBytes(path, texture.EncodeToJPG());
            }
            catch (System.IO.IOException e)
            {
                Debug.Log($"[Package Manager Window] Cannot save image: {e.Message}");
            }
        }

        public virtual void DownloadImageAsync(long productID, string url, Action<long, Texture2D> doneCallbackAction = null)
        {
            var texture = LoadImage(productID, url);
            if (texture != null)
            {
                doneCallbackAction?.Invoke(productID, texture);
                return;
            }

            var httpRequest = m_HttpClientFactory.GetASyncHTTPClient(url);
            httpRequest.doneCallback = httpClient =>
            {
                if (httpClient.IsSuccess() && httpClient.texture != null)
                {
                    SaveImage(productID, url, httpClient.texture);
                    doneCallbackAction?.Invoke(productID, httpClient.texture);
                    return;
                }

                doneCallbackAction?.Invoke(productID, null);
            };
            httpRequest.Begin();
        }

        public virtual void ClearOnlineCache()
        {
            m_Categories.Clear();
            m_PurchaseInfos.Clear();
            m_ProductInfos.Clear();
            m_UpdateInfos.Clear();
        }

        public virtual AssetStorePurchaseInfo GetPurchaseInfo(long? productId)
        {
            return productId > 0 ? m_PurchaseInfos.Get(productId.Value) : null;
        }

        public virtual AssetStoreProductInfo GetProductInfo(long? productId)
        {
            return productId > 0 ? m_ProductInfos.Get(productId.Value) : null;
        }

        public virtual AssetStoreLocalInfo GetLocalInfo(long? productId)
        {
            return productId > 0 ? m_LocalInfos.Get(productId.Value) : null;
        }

        public virtual AssetStoreUpdateInfo GetUpdateInfo(long? productId)
        {
            return productId > 0 ? m_UpdateInfos.Get(productId.Value) : null;
        }

        public virtual AssetStoreImportedPackage GetImportedPackage(long? productId)
        {
            return productId > 0 ? m_ImportedPackages.Get(productId.Value) : null;
        }

        public virtual void SetPurchaseInfos(IEnumerable<AssetStorePurchaseInfo> purchaseInfos)
        {
            var updatedPurchaseInfos = new List<AssetStorePurchaseInfo>();
            foreach (var purchaseInfo in purchaseInfos)
            {
                var oldPurchaseInfo = GetPurchaseInfo(purchaseInfo.productId);
                m_PurchaseInfos[purchaseInfo.productId] = purchaseInfo;
                if (!purchaseInfo.Equals(oldPurchaseInfo))
                    updatedPurchaseInfos.Add(purchaseInfo);
            }
            if (updatedPurchaseInfos.Count > 0)
                onPurchaseInfosChanged?.Invoke(updatedPurchaseInfos);
        }

        public virtual void SetProductInfo(AssetStoreProductInfo productInfo)
        {
            var oldProductInfo = GetProductInfo(productInfo.productId);
            m_ProductInfos[productInfo.productId] = productInfo;
            m_UniqueIdMapper.MapProductIdAndName(productInfo);
            if (!productInfo.Equals(oldProductInfo))
                onProductInfoChanged?.Invoke(productInfo);
        }

        public virtual void SetLocalInfos(IEnumerable<AssetStoreLocalInfo> localInfos)
        {
            var oldLocalInfos = m_LocalInfos;
            m_LocalInfos = new Dictionary<long, AssetStoreLocalInfo>();
            foreach (var info in localInfos)
            {
                var productId = info?.productId ?? 0;
                if (productId <= 0)
                    continue;

                if (m_LocalInfos.TryGetValue(productId, out var existingInfo))
                {
                    try
                    {
                        if (existingInfo.versionId >= info.versionId)
                            continue;
                    }
                    catch (Exception)
                    {
                        var warningMessage = L10n.Tr("Multiple versions of the same package found on disk and we could not determine which one to take. Please remove one of the following files:\n");
                        Debug.LogWarning($"{warningMessage}{existingInfo.packagePath}\n{info.packagePath}");
                        continue;
                    }
                }
                m_LocalInfos[productId] = info;
            }

            var addedOrUpdatedLocalInfos = new List<AssetStoreLocalInfo>();
            foreach (var info in m_LocalInfos.Values)
            {
                var oldInfo = oldLocalInfos.Get(info.productId);
                if (oldInfo != null)
                    oldLocalInfos.Remove(info.productId);

                if (!IsLocalInfoUpdated(oldInfo, info))
                    continue;

                addedOrUpdatedLocalInfos.Add(info);
                // When local info gets updated, we want to remove the cached update info so that we check update for the new local info
                m_UpdateInfos.Remove(info.productId);
            }
            if (addedOrUpdatedLocalInfos.Any() || oldLocalInfos.Any())
                onLocalInfosChanged?.Invoke(addedOrUpdatedLocalInfos, oldLocalInfos.Values);
        }

        public virtual void SetLocalInfo(AssetStoreLocalInfo localInfo)
        {
            var productId = localInfo?.productId ?? 0;
            if (productId <= 0)
                return;
            var oldInfo = m_LocalInfos.Get(productId);
            m_LocalInfos[productId] = localInfo;
            if (IsLocalInfoUpdated(oldInfo, localInfo))
                onLocalInfosChanged?.Invoke(new []{ localInfo }, Enumerable.Empty<AssetStoreLocalInfo>());
        }

        private static bool IsLocalInfoUpdated(AssetStoreLocalInfo oldInfo, AssetStoreLocalInfo newInfo)
        {
            return oldInfo == null
                   || oldInfo.versionId != newInfo.versionId
                   || oldInfo.uploadId != newInfo.uploadId
                   || oldInfo.versionString != newInfo.versionString
                   || oldInfo.packagePath != newInfo.packagePath;
        }

        public virtual void SetUpdateInfos(IEnumerable<AssetStoreUpdateInfo> updateInfos)
        {
            var updateInfosChanged = new List<AssetStoreUpdateInfo>();
            foreach (var info in updateInfos)
            {
                m_UpdateInfos.TryGetValue(info.productId, out var cachedUpdateInfo);
                if (info.recommendedUploadId != cachedUpdateInfo?.recommendedUploadId)
                    updateInfosChanged.Add(info);
                m_UpdateInfos[info.productId] = info;
            }

            if (updateInfosChanged.Any())
                onUpdateInfosChanged?.Invoke(updateInfosChanged);
        }

        public virtual void UpdateImportedAssets(IEnumerable<Asset> addedOrUpdatedAssets, IEnumerable<string> removedAssetPaths)
        {
            var modifiedProductIds = new HashSet<long>();
            foreach (var path in removedAssetPaths ?? Enumerable.Empty<string>())
            {
                if (!m_ImportedAssets.ContainsKey(path))
                    continue;

                modifiedProductIds.Add(m_ImportedAssets[path].origin.productId);
                m_ImportedAssets.Remove(path);
            }
            foreach (var asset in addedOrUpdatedAssets ?? Enumerable.Empty<Asset>())
            {
                modifiedProductIds.Add(asset.origin.productId);
                m_ImportedAssets[asset.importedPath] = asset;
            }

            if (modifiedProductIds.Any())
                RefreshImportedPackageList(modifiedProductIds);
        }

        private void RefreshImportedPackageList(HashSet<long> modifiedProductIds)
        {
            var addedOrUpdatedPackages = new Dictionary<long, AssetStoreImportedPackage>();
            foreach (var asset in m_ImportedAssets.Values)
            {
                var productId = asset.origin.productId;
                if (!modifiedProductIds.Contains(productId))
                    continue;

                if (addedOrUpdatedPackages.TryGetValue(asset.origin.productId, out var package))
                {
                    package.AddImportedAsset(asset);
                    continue;
                }
                addedOrUpdatedPackages[asset.origin.productId] = new AssetStoreImportedPackage(new List<Asset>() { asset });
            }

            var removedPackages = new List<AssetStoreImportedPackage>();
            foreach (var productId in modifiedProductIds)
            {
                if (addedOrUpdatedPackages.TryGetValue(productId, out var package))
                {
                    m_ImportedPackages[productId] = package;
                    continue;
                }

                if (m_ImportedPackages.TryGetValue(productId, out var removedPackage))
                {
                    m_ImportedPackages.Remove(productId);
                    removedPackages.Add(removedPackage);
                }
            }

            if (addedOrUpdatedPackages.Any() || removedPackages.Any())
                onImportedPackagesChanged?.Invoke(addedOrUpdatedPackages.Values, removedPackages);
        }
    }
}
