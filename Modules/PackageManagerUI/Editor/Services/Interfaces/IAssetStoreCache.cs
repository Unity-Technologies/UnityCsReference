// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal interface IAssetStoreCache
    {
        string GetLastETag(long productId);

        void SetLastETag(long productId, string etag);

        Texture2D LoadImage(long productId, string url);

        void SaveImage(long productId, string url, Texture2D texture);
    }
}
