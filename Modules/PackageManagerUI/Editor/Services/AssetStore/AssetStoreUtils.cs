// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityAssetStoreUtils = UnityEditor.AssetStoreUtils;
using UnityAssetStorePackageInfo = UnityEditor.PackageInfo;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IAssetStoreUtils : IService
    {
        string BuildBaseDownloadPath(string publisher, string category);
        string BuildFinalDownloadPath(string basePath, string packageName);
        void Download(string id, string url, string[] destination, string key, string jsonData, bool resumeOK);
        string CheckDownload(string id, string url, string[] destination, string key);
        bool AbortDownload(string[] destination);
        void RegisterDownloadDelegate(ScriptableObject d);
        void UnRegisterDownloadDelegate(ScriptableObject d);
        UnityAssetStorePackageInfo[] GetLocalPackageList();
        UnityAssetStorePackageInfo GetLocalPackageInfo(string packagePath);
    }

    internal class AssetStoreUtils : BaseService<IAssetStoreUtils>, IAssetStoreUtils
    {
        public string BuildBaseDownloadPath(string publisher, string category)
        {
            return UnityAssetStoreUtils.BuildBaseDownloadPath(publisher, category);
        }

        public string BuildFinalDownloadPath(string basePath, string packageName)
        {
            return UnityAssetStoreUtils.BuildFinalDownloadPath(basePath, packageName);
        }

        public void Download(string id, string url, string[] destination, string key, string jsonData, bool resumeOK)
        {
            UnityAssetStoreUtils.Download(id, url, destination, key, jsonData, resumeOK);
        }

        public string CheckDownload(string id, string url, string[] destination, string key)
        {
            return UnityAssetStoreUtils.CheckDownload(id, url, destination, key);
        }

        public bool AbortDownload(string[] destination)
        {
            return UnityAssetStoreUtils.AbortDownload(destination);
        }

        public void RegisterDownloadDelegate(ScriptableObject d)
        {
            UnityAssetStoreUtils.RegisterDownloadDelegate(d);
        }

        public void UnRegisterDownloadDelegate(ScriptableObject d)
        {
            UnityAssetStoreUtils.UnRegisterDownloadDelegate(d);
        }

        public UnityAssetStorePackageInfo[] GetLocalPackageList()
        {
            return UnityAssetStorePackageInfo.GetPackageList();
        }

        public UnityAssetStorePackageInfo GetLocalPackageInfo(string packagePath)
        {
            return UnityAssetStorePackageInfo.GetPackageInfo(packagePath);
        }
    }
}
