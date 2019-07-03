// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.PackageManager.UI.AssetStore;

namespace UnityEditor.PackageManager.UI
{
    internal interface IAssetStoreClient
    {
        event Action<IEnumerable<IPackage>> onPackagesChanged;

        event Action<DownloadProgress> onDownloadProgress;

        event Action onOperationStart;
        event Action onOperationFinish;
        event Action<Error> onOperationError;

        void List(int offset, int limit);

        void Refresh(IPackage package);

        bool IsAnyDownloadInProgress();

        bool IsDownloadInProgress(string packageId);

        bool GetDownloadInProgress(string packageId, out DownloadProgress progress);

        void AbortDownload(string packageId);

        void AbortAllDownloads();

        void Download(string packageId);

        void OnDownloadProgress(string packageId, string message, ulong bytes, ulong total);

        void Setup();

        void Clear();
    }
}
