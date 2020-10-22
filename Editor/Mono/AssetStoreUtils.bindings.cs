// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/AssetStore.bindings.h")]
    [StaticAccessor("AssetStoreScriptBindings", StaticAccessorType.DoubleColon)]
    internal class AssetStoreUtils
    {
        private const string kAssetStoreUrl = "https://shawarma.unity3d.com";
        public delegate void DownloadDoneCallback(string package_id, string message, int bytes, int total);

        extern public static string BuildBaseDownloadPath(string publisher, string category);
        extern public static string BuildFinalDownloadPath(string baseDir, string packageName);

        [NativeThrows]
        extern public static void Download(string id, string url, string[] destination, string key, string jsonData, bool resumeOK, DownloadDoneCallback doneCallback = null);
        [NativeThrows]
        extern public static string CheckDownload(string id, string url, string[] destination, string key);
        [NativeThrows]
        extern public static bool AbortDownload(string[] destination);
        extern public static void RegisterDownloadDelegate([NotNull] ScriptableObject d);
        extern public static void UnRegisterDownloadDelegate([NotNull] ScriptableObject d);
        extern public static string GetLoaderPath();
        extern public static void UpdatePreloading();

        public static string GetAssetStoreUrl()
        {
            return kAssetStoreUrl;
        }

        public static string GetAssetStoreSearchUrl()
        {
            return GetAssetStoreUrl().Replace("https", "http"); // Use use http (and not https) when searching because its faster
        }
    }
}
