// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IUpmClient
    {
        event Action<IOperation> onListOperation;
        event Action<IOperation> onSearchAllOperation;
        event Action<IOperation> onRemoveOperation;
        event Action<IOperation> onAddOperation;
        event Action<IOperation> onEmbedOperation;

        event Action<IEnumerable<IPackage>> onPackagesChanged;
        event Action<string, IPackage> onProductPackageChanged;
        event Action<string, IPackageVersion> onPackageVersionUpdated;
        event Action<string, IPackageVersion> onProductPackageVersionUpdated;
        event Action<string, Error> onProductPackageFetchError;

        bool isAddRemoveOrEmbedInProgress { get; }

        bool IsEmbedInProgress(string packageName);
        bool IsRemoveInProgress(string packageName);
        bool IsAddInProgress(string packageId);

        void SearchAll(bool offlineMode = false);
        void ExtraFetch(string packageId);
        void ProcessExtraFetchResult(PackageInfo packageInfo);

        void FetchForProduct(string productId, string packageName);

        void List(bool offlineMode = false);
        void AddById(string packageId);
        void AddByPath(string path);
        void AddByUrl(string url);

        void RemoveByName(string packageName);
        void RemoveEmbeddedByName(string packageName);

        void EmbedByName(string packageName);

        void RegisterEvents();

        void UnregisterEvents();

        void ClearCache();

        void ClearProductCache();

        bool IsUnityPackage(PackageInfo packageInfo);
        bool IsUnityUrl(string url);
    }
}
