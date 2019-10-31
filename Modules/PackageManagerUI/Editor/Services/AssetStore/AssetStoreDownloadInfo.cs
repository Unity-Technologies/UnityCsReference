// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class AssetStoreDownloadInfo
    {
        public bool isValid;
        public string categoryName;
        public string packageName;
        public string publisherName;
        public string productId;
        public string key;
        public string url;
        public string errorMessage;

        public string[] destination => new string[]
        {
            publisherName.Replace(".", ""),
            categoryName.Replace(".", ""),
            packageName.Replace(".", "")
        };

        public static AssetStoreDownloadInfo ParseDownloadInfo(IDictionary<string, object> rawInfo)
        {
            if (rawInfo?.Any() != true)
                return null;

            try
            {
                var download = rawInfo.GetDictionary("result").GetDictionary("download");
                return new AssetStoreDownloadInfo
                {
                    isValid = true,
                    categoryName = download.GetString("filename_safe_category_name"),
                    packageName = download.GetString("filename_safe_package_name"),
                    publisherName = download.GetString("filename_safe_publisher_name"),
                    productId = download.GetString("id"),
                    key = download.GetString("key"),
                    url = download.GetString("url")
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
