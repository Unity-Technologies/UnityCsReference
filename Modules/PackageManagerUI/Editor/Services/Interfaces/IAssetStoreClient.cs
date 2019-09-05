// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IAssetStoreClient
    {
        event Action<ProductList, bool> onProductListFetched;

        event Action<long> onProductFetched;

        event Action<IEnumerable<IPackage>> onPackagesChanged;
        event Action<string, IPackageVersion> onPackageVersionUpdated;

        event Action<DownloadProgress> onDownloadProgress;

        event Action<IOperation> onListOperation;

        event Action onFetchDetailsStart;
        event Action onFetchDetailsFinish;
        event Action<Error> onFetchDetailsError;

        void List(int offset, int limit, string searchText = "", bool fetchDetails = true);

        void Fetch(long productId);

        void FetchDetails(IEnumerable<long> productIds);

        void RefreshLocal();

        bool IsAnyDownloadInProgress();

        bool IsDownloadInProgress(string productId);

        bool GetDownloadProgress(string productId, out DownloadProgress progress);

        void AbortDownload(string productId);

        void AbortAllDownloads();

        void Download(string productId);

        void OnDownloadProgress(string productId, string message, ulong bytes, ulong total);

        void Setup();

        void Clear();

        void Reset();
    }
}
