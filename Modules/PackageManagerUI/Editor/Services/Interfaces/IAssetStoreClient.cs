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
        event Action<ProductList, bool> onProductListFetched;

        event Action<long> onProductFetched;

        event Action<IEnumerable<IPackage>> onPackagesChanged;
        event Action<string, IPackageVersion> onPackageVersionUpdated;

        event Action<DownloadProgress> onDownloadProgress;

        event Action onListOperationStart;
        event Action onListOperationFinish;

        event Action onFetchDetailsStart;
        event Action onFetchDetailsFinish;

        event Action<Error> onOperationError;

        void List(int offset, int limit, string searchText = "", bool fetchDetails = true);

        void Fetch(long productId);

        void FetchDetails(IEnumerable<long> packageIds);

        void RefreshLocal();

        bool IsAnyDownloadInProgress();

        bool IsDownloadInProgress(string packageId);

        bool GetDownloadProgress(string packageId, out DownloadProgress progress);

        void AbortDownload(string packageId);

        void AbortAllDownloads();

        void Download(string packageId);

        void OnDownloadProgress(string packageId, string message, ulong bytes, ulong total);

        void Setup();

        void Clear();

        void Reload();
    }
}
