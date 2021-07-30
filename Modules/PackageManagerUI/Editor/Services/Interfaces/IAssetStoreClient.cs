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
        event Action<ProductList> onProductListFetched;

        event Action<long> onProductFetched;

        event Action<IEnumerable<IPackage>> onPackagesChanged;
        event Action<string, IPackageVersion> onPackageVersionUpdated;

        event Action<DownloadProgress> onDownloadProgress;

        event Action onListOperationStart;
        event Action onListOperationFinish;

        event Action onFetchDetailsStart;
        event Action onFetchDetailsFinish;

        event Action<Error> onOperationError;

        void List(int offset, int limit, string searchText = "");

        void Fetch(long productId);

        void FetchDetail(long productId, Action doneCallbackAction = null);

        void FetchDetails(IEnumerable<long> packageIds);

        void RefreshLocal();

        bool IsAnyDownloadInProgress();

        bool IsDownloadInProgress(string packageId);

        bool GetDownloadProgress(string packageId, out DownloadProgress progress);

        void AbortDownload(string packageId);

        void AbortAllDownloads();

        void Download(string packageId);

        void OnDownloadProgress(string packageId, string message, ulong bytes, ulong total);

        void RegisterEvents();

        void UnregisterEvents();

        void ClearCache();

        void CheckTermOfServiceAgreement(Action<TermOfServiceAgreementStatus> agreementStatusCallback);
    }
}
