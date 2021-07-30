// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal class DownloadInformation
    {
        public string CategoryName;
        public string PackageName;
        public string PublisherName;
        public string PackageId;
        public string Key;
        public string Url;
        public bool isValid;
        public string errorMessage;
    }

    [Serializable]
    internal class ProductInfo
    {
        public long productId;
        public string displayName;
    }

    [Serializable]
    internal class ProductList
    {
        public long total;
        public int startIndex;
        public bool isValid;
        public string searchText;
        public string errorMessage;
        public List<ProductInfo> list = new List<ProductInfo>();
    }

    internal interface IAssetStoreRestAPI
    {
        void GetProductIDList(int startIndex, int limit, string searchText, Action<ProductList> doneCallbackAction);

        void GetProductDetail(long productID, Action<Dictionary<string, object>> doneCallbackAction);

        void GetDownloadDetail(long productID, Action<DownloadInformation> doneCallbackAction);

        void GetProductUpdateDetail(IEnumerable<AssetStore.LocalInfo> localInfos, Action<Dictionary<string, object>> doneCallbackAction);

        void CheckTermsAndConditions(Action<Dictionary<string, object>> doneCallbackAction);

        void HandleHttpRequest(IAsyncHTTPClient httpRequest, Action<Dictionary<string, object>> doneCallbackAction, Action<string> errorCallbackAction);
    }
}
