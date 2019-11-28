// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal interface IDownloadManager
    {
        event Action<IOperation, UIError> onDownloadError;
        event Action<IOperation> onDownloadFinalized;
        event Action<IOperation> onDownloadProgress;

        AssetStoreDownloadOperation GetDownloadOperation(string productId);

        void OnDownloadProgress(string productId, string message, ulong bytes, ulong total);

        bool IsAnyDownloadInProgress();

        void AbortAllDownloads();

        void AbortDownload(string productId);

        void Download(string productId);

        void DownloadImageAsync(long productID, string url, Action<long, Texture2D> doneCallbackAction = null);

        void RegisterEvents();

        void UnregisterEvents();

        void ClearCache();
    }
}
