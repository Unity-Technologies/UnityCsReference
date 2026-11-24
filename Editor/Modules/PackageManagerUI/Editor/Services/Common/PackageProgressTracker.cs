// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IPackageProgressTracker : IService
    {
        event Action<IEnumerable<(string packageNameOrProductId, PackageProgress progress)>> onPackagesProgressChanged;
        PackageProgress GetProgress(string packageNameOrProductId);
        PackageProgress GetProgress(string packageName, long productId);
    }

    [Serializable]
    internal class PackageProgressTracker : BaseService<IPackageProgressTracker>, IPackageProgressTracker, ISerializationCallbackReceiver
    {
        public event Action<IEnumerable<(string packageNameOrProductId, PackageProgress progress)>> onPackagesProgressChanged;

        [SerializeField]
        private Dictionary<string, PackageProgress> m_OperationProgressMap = new();
        private readonly HashSet<string> m_RefreshProgress = new();

        [SerializeField]
        private string[] m_SerializedRefreshProgress = Array.Empty<string>();

        private readonly IUpmClient m_UpmClient;
        private readonly IAssetStoreDownloadManager m_AssetStoreDownloadManager;
        private readonly IFetchStatusTracker m_FetchStatusTracker;

        public PackageProgressTracker(IUpmClient upmClient, IAssetStoreDownloadManager assetStoreDownloadManager, IFetchStatusTracker fetchStatusTracker)
        {
            m_UpmClient = RegisterDependency(upmClient);
            m_AssetStoreDownloadManager = RegisterDependency(assetStoreDownloadManager);
            m_FetchStatusTracker = RegisterDependency(fetchStatusTracker);
        }

        public override void OnEnable()
        {
            m_UpmClient.onPackagesProgressChange += OnUpmPackagesProgressChange;
            m_AssetStoreDownloadManager.onDownloadProgress += OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadStateChanged += OnDownloadStateChanged;
            m_AssetStoreDownloadManager.onDownloadFinalized += OnDownloadFinalized;
            m_FetchStatusTracker.onProductInfoFetchStatusChanged += OnProductInfoFetchStatusChanged;
            m_FetchStatusTracker.onSearchInfoFetchStatusChanged += OnSearchInfoFetchStatusChanged;
        }

        public override void OnDisable()
        {
            m_UpmClient.onPackagesProgressChange -= OnUpmPackagesProgressChange;
            m_AssetStoreDownloadManager.onDownloadProgress -= OnDownloadProgress;
            m_AssetStoreDownloadManager.onDownloadStateChanged -= OnDownloadStateChanged;
            m_AssetStoreDownloadManager.onDownloadFinalized -= OnDownloadFinalized;
            m_FetchStatusTracker.onProductInfoFetchStatusChanged -= OnProductInfoFetchStatusChanged;
            m_FetchStatusTracker.onSearchInfoFetchStatusChanged -= OnSearchInfoFetchStatusChanged;
        }

        public PackageProgress GetProgress(string packageNameOrProductId)
        {
            if (m_OperationProgressMap.TryGetValue(packageNameOrProductId, out var progress))
                return progress;
            return m_RefreshProgress.Contains(packageNameOrProductId) ? PackageProgress.Refreshing : PackageProgress.None;
        }

        // We need to check for both packageName and productId because UpmOnAssetStore packages can have progress stored under either key:
        // packageName for UPM operations / searchInfo refreshes, productId for downloads / productInfo refreshes.
        public PackageProgress GetProgress(string packageName, long productId)
        {
            if (m_OperationProgressMap.TryGetValue(packageName, out var progress))
                return progress;
            var productIdString = productId.ToString();
            if (m_OperationProgressMap.TryGetValue(productIdString, out progress))
                return progress;
            return m_RefreshProgress.Contains(packageName) || m_RefreshProgress.Contains(productIdString) ? PackageProgress.Refreshing : PackageProgress.None;
        }

        private void SetOperationProgress(IEnumerable<(string packageIdOrName, PackageProgress progress)> progressUpdates, bool normalizePackageUniqueId)
        {
            var changedUpdates = new List<(string packageUniqueId, PackageProgress progress)>();
            foreach (var (packageIdOrName, progress) in progressUpdates)
            {
                var packageUniqueId = packageIdOrName;
                if (normalizePackageUniqueId)
                {
                    var atIndex = packageIdOrName.IndexOf('@');
                    packageUniqueId = atIndex >= 0 ? packageIdOrName[..atIndex] : packageIdOrName;
                }

                var previousEffective = GetProgress(packageUniqueId);
                if (progress == PackageProgress.None)
                    m_OperationProgressMap.Remove(packageUniqueId);
                else
                    m_OperationProgressMap[packageUniqueId] = progress;
                var newEffective = GetProgress(packageUniqueId);
                if (previousEffective != newEffective)
                    changedUpdates.Add((packageUniqueId, newEffective));
            }
            if (changedUpdates.Count > 0)
                onPackagesProgressChanged?.Invoke(changedUpdates);
        }

        private void SetOperationProgress(string packageUniqueId, PackageProgress progress)
        {
            SetOperationProgress(new[] { (packageUniqueId, progress) }, false);
        }

        private void SetRefreshProgress(string packageUniqueId, bool isRefreshing)
        {
            var previousEffective = GetProgress(packageUniqueId);
            if (!isRefreshing)
                m_RefreshProgress.Remove(packageUniqueId);
            else
                m_RefreshProgress.Add(packageUniqueId);
            var newEffective = GetProgress(packageUniqueId);
            if (previousEffective != newEffective)
                onPackagesProgressChanged?.Invoke(new[] { (packageUniqueId, newEffective) });
        }

        private void OnUpmPackagesProgressChange(IEnumerable<(string packageIdOrName, PackageProgress progress)> progressUpdates)
        {
            SetOperationProgress(progressUpdates, true);
        }

        private void OnDownloadProgress(AssetStoreDownloadOperation operation)
        {
            SetOperationProgress(operation.packageUniqueId, operation.isInProgress ? PackageProgress.Downloading : PackageProgress.None);
        }

        private void OnDownloadStateChanged(AssetStoreDownloadOperation operation)
        {
            switch (operation.state)
            {
                case DownloadState.Pausing:
                    SetOperationProgress(operation.packageUniqueId, PackageProgress.Pausing);
                    break;
                case DownloadState.ResumeRequested:
                    SetOperationProgress(operation.packageUniqueId, PackageProgress.Resuming);
                    break;
                case DownloadState.Paused:
                case DownloadState.AbortRequested:
                case DownloadState.Aborted:
                    SetOperationProgress(operation.packageUniqueId, PackageProgress.None);
                    break;
            }
        }

        private void OnDownloadFinalized(AssetStoreDownloadOperation operation)
        {
            SetOperationProgress(operation.packageUniqueId, PackageProgress.None);
        }

        private void OnProductInfoFetchStatusChanged(long productId)
        {
            var status = m_FetchStatusTracker.GetProductInfoFetchStatus(productId);
            SetRefreshProgress(productId.ToString(), status is { inProgress: true });
        }

        private void OnSearchInfoFetchStatusChanged(string packageName)
        {
            var status = m_FetchStatusTracker.GetSearchInfoFetchStatus(packageName);
            SetRefreshProgress(packageName, status is { inProgress: true });
        }

        public void OnBeforeSerialize()
        {
            m_RefreshProgress.ToArray(ref m_SerializedRefreshProgress);
        }

        public void OnAfterDeserialize()
        {
            foreach (var packageUniqueId in m_SerializedRefreshProgress)
                m_RefreshProgress.Add(packageUniqueId);
        }
    }
}
