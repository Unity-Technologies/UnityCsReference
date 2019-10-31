// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IAssetStoreClient
    {
        event Action<AssetStorePurchases, bool> onProductListFetched;

        event Action<long> onProductFetched;

        event Action<IEnumerable<IPackage>> onPackagesChanged;
        event Action<string, IPackageVersion> onPackageVersionUpdated;

        event Action<IOperation> onListOperation;

        event Action onFetchDetailsStart;
        event Action onFetchDetailsFinish;
        event Action<Error> onFetchDetailsError;

        void ListPurchases(PurchasesQueryArgs queryArgs, bool fetchDetails = true);

        void Fetch(long productId);

        void FetchDetails(IEnumerable<long> productIds);

        void RefreshLocal();

        void RegisterEvents();

        void UnregisterEvents();

        void ClearCache();
    }
}
