// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal interface IAssetStoreUtils
    {
        void Download(string id, string url, string[] destination, string key, string jsonData, bool resumeOK);

        string CheckDownload(string id, string url, string[] destination, string key);

        bool AbortDownload(string id, string[] destination);

        void RegisterDownloadDelegate(ScriptableObject d);

        void UnRegisterDownloadDelegate(ScriptableObject d);

        UnityEditor.PackageInfo[] GetLocalPackageList();

        string assetStoreUrl { get; }
    }
}
