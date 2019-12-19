// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal interface IAssetStoreCache
    {
        string GetLastETag(string key);

        void SetLastETag(string key, string etag);

        void SetCategory(string category, long count);

        Texture2D LoadImage(long productId, string url);

        void SaveImage(long productId, string url, Texture2D texture);

        void ClearCache();
    }
}
