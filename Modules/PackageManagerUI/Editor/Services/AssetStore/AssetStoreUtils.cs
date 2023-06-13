// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityAssetStoreUtils = UnityEditor.AssetStoreUtils;
using UnityAssetStorePackageInfo = UnityEditor.PackageInfo;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class AssetStoreUtils
    {
        public virtual string BuildBaseDownloadPath(string publisher, string category)
        {
            return UnityAssetStoreUtils.BuildBaseDownloadPath(publisher, category);
        }

        public virtual string BuildFinalDownloadPath(string basePath, string packageName)
        {
            return UnityAssetStoreUtils.BuildFinalDownloadPath(basePath, packageName);
        }

        public virtual void Download(string id, string url, string[] destination, string key, string jsonData, bool resumeOK)
        {
            UnityAssetStoreUtils.Download(id, url, destination, key, jsonData, resumeOK);
        }

        public virtual string CheckDownload(string id, string url, string[] destination, string key)
        {
            return UnityAssetStoreUtils.CheckDownload(id, url, destination, key);
        }

        public virtual bool AbortDownload(string[] destination)
        {
            return UnityAssetStoreUtils.AbortDownload(destination);
        }

        public virtual void RegisterDownloadDelegate(ScriptableObject d)
        {
            UnityAssetStoreUtils.RegisterDownloadDelegate(d);
        }

        public virtual void UnRegisterDownloadDelegate(ScriptableObject d)
        {
            UnityAssetStoreUtils.UnRegisterDownloadDelegate(d);
        }

        public virtual UnityAssetStorePackageInfo[] GetLocalPackageList()
        {
            return UnityAssetStorePackageInfo.GetPackageList();
        }

        public virtual UnityAssetStorePackageInfo GetLocalPackageInfo(string packagePath)
        {
            return UnityAssetStorePackageInfo.GetPackageInfo(packagePath);
        }
    }
}
