// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class DownloadInformation
    {
        public string categoryName;
        public string packageName;
        public string publisherName;
        public string productId;
        public string key;
        public string url;
        public bool isValid;
        public string errorMessage;

        public string[] destination => new string[]
        {
            publisherName.Replace(".", ""),
            categoryName.Replace(".", ""),
            packageName.Replace(".", "")
        };
    }

    [Serializable]
    internal class ProductList
    {
        public long total;
        public int startIndex;
        public bool isValid;
        public string searchText;
        public string errorMessage;
        public List<long> list = new List<long>();
    }

    internal interface IAssetStoreRestAPI
    {
        void GetProductIDList(int startIndex, int limit, string searchText, Action<ProductList> doneCallbackAction);

        void GetProductDetail(long productID, Action<Dictionary<string, object>> doneCallbackAction);

        void GetDownloadDetail(long productID, Action<DownloadInformation> doneCallbackAction);

        void GetProductUpdateDetail(IEnumerable<AssetStoreLocalInfo> localInfos, Action<Dictionary<string, object>> doneCallbackAction);
    }
}
