// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal interface IAssetStoreCache
    {
        IEnumerable<AssetStoreLocalInfo> localInfos { get; }

        event Action<IEnumerable<AssetStoreLocalInfo> /*addedOrUpdated*/, IEnumerable<AssetStoreLocalInfo> /*removed*/> onLocalInfosChanged;

        string GetLastETag(string key);

        void SetLastETag(string key, string etag);

        void SetCategory(string category, long count);

        AssetStorePurchaseInfo GetPurchaseInfo(string productIdString);
        AssetStoreProductInfo GetProductInfo(string productIdString);
        AssetStoreLocalInfo GetLocalInfo(string productIdString);

        void SetPurchaseInfo(AssetStorePurchaseInfo info);
        void SetProductInfo(AssetStoreProductInfo info);
        void SetLocalInfos(IEnumerable<AssetStoreLocalInfo> localInfos);

        void RemoveProductInfo(string productIdString);

        Texture2D LoadImage(long productId, string url);

        void SaveImage(long productId, string url, Texture2D texture);

        void ClearCache();
    }
}
