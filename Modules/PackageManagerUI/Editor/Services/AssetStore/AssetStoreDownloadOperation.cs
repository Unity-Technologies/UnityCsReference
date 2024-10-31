// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    using ConfigStatus = UnityEditorInternal.AssetStoreCachePathManager.ConfigStatus;

    [Serializable]
    internal class AssetStoreDownloadOperation : IOperation
    {
        internal static readonly string k_DownloadErrorMessage = L10n.Tr("The download could not be completed. See details in console.");
        internal static readonly string k_AbortErrorMessage = L10n.Tr("The download could not be canceled. Please try again.");
        internal static readonly string k_AssetStoreDownloadPrefix = "content__";
        internal static readonly string k_NotPurchasedErrorMessage = L10n.Tr("The Asset Store package you are trying to download is not available to the current Unity account. If you purchased this asset from the Asset Store using a different account, use that Unity account to sign into the Editor.");
        internal static readonly string k_ForbiddenErrorMessage = L10n.Tr("The Asset Store package couldn't be downloaded at this time. Please try again later. Should the issue persist, please contact our <a href=\"https://support.unity.com\">customer support</a> for assistance.");
        private static readonly string k_ConsoleLogPrefix = L10n.Tr("[Package Manager Window]");

        [SerializeField]
        private long m_ProductId;
        public long productId => m_ProductId;
        public string packageUniqueId => m_ProductId.ToString();

        [SerializeField]
        private string m_ProductOldPath;
        public string packageOldPath => m_ProductOldPath;

        [SerializeField]
        private string m_ProductNewPath;
        public string packageNewPath => m_ProductNewPath;


        // timestamps are usually used for refresh operations, for download operations it is not used, so we won't bother setting them for now
        public long timestamp => 0;
        public long lastSuccessTimestamp => 0;

        public bool isOfflineMode => false;

        public virtual bool isInProgress => (state & DownloadState.InProgress) != 0;

        public bool isInPause => (state & DownloadState.InPause) != 0;

        public virtual bool isProgressVisible => (state & (DownloadState.DownloadRequested | DownloadState.InPause | DownloadState.InProgress)) != 0;

        public bool isProgressTrackable => true;

        public float progressPercentage => m_TotalBytes > 0 ? m_DownloadedBytes / (float)m_TotalBytes : 0.0f;

        public RefreshOptions refreshOptions => RefreshOptions.None;

        public event Action<IOperation, UIError> onOperationError = delegate {};
        public event Action<IOperation> onOperationSuccess = delegate {};
        public event Action<IOperation> onOperationFinalized = delegate {};
        public event Action<IOperation> onOperationProgress = delegate {};
        public event Action<AssetStoreDownloadOperation> onDownloadStateChanged = delegate {};

        [SerializeField]
        private ulong m_DownloadedBytes;
        [SerializeField]
        private ulong m_TotalBytes;

        [SerializeField]
        private DownloadState m_State;
        public virtual DownloadState state
        {
            get => m_State;
            private set
            {
                if (m_State == value)
                    return;
                m_State = value;
                onDownloadStateChanged?.Invoke(this);
            }
        }

        [SerializeField]
        private string m_ErrorMessage;
        public string errorMessage => m_ErrorMessage;

        [SerializeField]
        private AssetStoreDownloadInfo m_DownloadInfo;
        public AssetStoreDownloadInfo downloadInfo => m_DownloadInfo;

        [NonSerialized]
        private IIOProxy m_IOProxy;
        [NonSerialized]
        private IAssetStoreUtils m_AssetStoreUtils;
        [NonSerialized]
        private IAssetStoreRestAPI m_AssetStoreRestAPI;
        [NonSerialized]
        private IAssetStoreCache m_AssetStoreCache;
        [NonSerialized]
        private IAssetStoreCachePathProxy m_AssetStoreCachePathProxy;
        [NonSerialized]
        private ILocalInfoHandler m_LocalInfoHandler;
        public void ResolveDependencies(IIOProxy ioProxy,
            IAssetStoreUtils assetStoreUtils,
            IAssetStoreRestAPI assetStoreRestAPI,
            IAssetStoreCache assetStoreCache,
            IAssetStoreCachePathProxy assetStoreCachePathProxy,
            ILocalInfoHandler localInfoHandler)
        {
            m_IOProxy = ioProxy;
            m_AssetStoreUtils = assetStoreUtils;
            m_AssetStoreRestAPI = assetStoreRestAPI;
            m_AssetStoreCache = assetStoreCache;
            m_AssetStoreCachePathProxy = assetStoreCachePathProxy;
            m_LocalInfoHandler = localInfoHandler;
        }

        public AssetStoreDownloadOperation(IIOProxy ioProxy, IAssetStoreUtils assetStoreUtils, IAssetStoreRestAPI assetStoreRestAPI, IAssetStoreCache assetStoreCache, IAssetStoreCachePathProxy assetStoreCachePathProxy, ILocalInfoHandler localInfoHandler, long productId, string oldPath)
        {
            ResolveDependencies(ioProxy, assetStoreUtils, assetStoreRestAPI, assetStoreCache, assetStoreCachePathProxy, localInfoHandler);

            m_ProductId = productId;
            m_ProductOldPath = oldPath;
        }

        public void OnDownloadProgress(string message, ulong bytes, ulong total, int errorCode)
        {
            switch (message)
            {
                case "ok":
                    state = DownloadState.Completed;
                    ProcessDownloadResult();
                    onOperationSuccess?.Invoke(this);
                    onOperationFinalized?.Invoke(this);
                    break;
                case "connecting":
                    state = DownloadState.Connecting;
                    break;
                case "downloading":
                    if (!isInPause)
                        state = DownloadState.Downloading;
                    m_DownloadedBytes = Math.Max(m_DownloadedBytes, bytes);
                    m_TotalBytes = Math.Max(m_TotalBytes, total);
                    break;
                case "decrypt":
                    state = DownloadState.Decrypting;
                    break;
                case "aborted":
                    if (!isInPause)
                    {
                        m_DownloadedBytes = 0;
                        state = DownloadState.Aborted;
                        onOperationFinalized?.Invoke(this);
                    }
                    else
                    {
                        state = DownloadState.Paused;
                    }
                    break;
                default:
                    OnErrorMessage(message, errorCode);
                    break;
            }

            onOperationProgress?.Invoke(this);
        }

        private void ProcessDownloadResult()
        {
            m_LocalInfoHandler.UpdateExtraInfoInCacheIfNeeded(m_ProductNewPath, m_DownloadInfo);
            if (!string.IsNullOrEmpty(m_ProductOldPath) && m_ProductNewPath?.NormalizePath() != m_ProductOldPath.NormalizePath())
                m_IOProxy.DeleteIfExists(m_ProductOldPath);
            m_AssetStoreCache.SetLocalInfo(m_LocalInfoHandler.GetParsedLocalInfo(m_ProductNewPath));
        }

        private void OnErrorMessage(string message, int operationErrorCode = -1, UIError.Attribute attr = UIError.Attribute.None)
        {
            state = DownloadState.Error;

            if ((attr & UIError.Attribute.Warning) != 0)
                Debug.LogWarning($"{k_ConsoleLogPrefix} {message}");
            else
                Debug.LogError($"{k_ConsoleLogPrefix} {message}");

            attr |= UIError.Attribute.DetailInConsole;
            if (operationErrorCode == 403)
            {
                var purchaseInfo = m_AssetStoreCache.GetPurchaseInfo(m_ProductId);
                m_ErrorMessage = purchaseInfo == null ? k_NotPurchasedErrorMessage : k_ForbiddenErrorMessage;
            }
            else
            {
                attr |= UIError.Attribute.Clearable;
                m_ErrorMessage = k_DownloadErrorMessage;
            }

            var error = new UIError(UIErrorCode.AssetStoreOperationError, m_ErrorMessage, attr, operationErrorCode);
            onOperationError?.Invoke(this, error);
            onOperationFinalized?.Invoke(this);

            PackageManagerOperationErrorAnalytics.SendEvent(GetType().Name, error);
        }

        public void Pause()
        {
            if (m_DownloadInfo == null || m_DownloadInfo.productId <= 0)
                return;

            if (state == DownloadState.Aborted || state == DownloadState.Completed || state == DownloadState.Error || state == DownloadState.Paused)
                return;

            state = DownloadState.Pausing;

            // Pause here is the same as aborting the download, but we don't delete the file so we can resume from where we paused it from
            if (!m_AssetStoreUtils.AbortDownload(downloadInfo.destination))
                Debug.LogError($"{k_ConsoleLogPrefix} {k_AbortErrorMessage}");
        }

        public void Cancel()
        {
            if (m_DownloadInfo == null || m_DownloadInfo.productId <= 0)
                return;

            m_AssetStoreUtils.AbortDownload(downloadInfo.destination);
            m_DownloadedBytes = 0;
            state = DownloadState.None;
            onOperationFinalized?.Invoke(this);
        }

        public void Abort()
        {
            if (!isInProgress && !isInPause)
                return;

            // We reset everything if we cancel after pausing a download
            if (state == DownloadState.Paused)
            {
                m_DownloadedBytes = 0;
                state = DownloadState.Aborted;
                onOperationFinalized?.Invoke(this);
                return;
            }

            state = DownloadState.AbortRequested;
            // the actual download state change from `downloading` to `aborted` happens in `OnDownloadProgress` callback
            if (m_DownloadInfo?.productId > 0 && !m_AssetStoreUtils.AbortDownload(m_DownloadInfo.destination))
                Debug.LogError($"{k_ConsoleLogPrefix} {k_AbortErrorMessage}");
        }

        public void Download(bool resume)
        {
            var config = m_AssetStoreCachePathProxy.GetConfig();
            if (config.status == ConfigStatus.ReadOnly)
            {
                OnErrorMessage("The Assets Cache location is read-only, see configuration in Preferences | Package Manager", -1, UIError.Attribute.Warning);
                return;
            }
            if (config.status == ConfigStatus.InvalidPath)
            {
                OnErrorMessage("The Assets Cache location is invalid or inaccessible, see configuration in Preferences | Package Manager", -1, UIError.Attribute.Warning);
                return;
            }

            state = resume ? DownloadState.ResumeRequested : DownloadState.DownloadRequested;
            m_AssetStoreRestAPI.GetDownloadDetail(m_ProductId, downloadInfo =>
            {
                // if the user requested to abort before receiving the download details, we can simply discard the download info and do nothing
                if (state == DownloadState.AbortRequested)
                    return;

                m_DownloadInfo = downloadInfo;
                var dest = downloadInfo.destination;

                var publisher = string.Empty;
                var category = string.Empty;
                var packageName = string.Empty;

                if (dest.Length >= 1)
                    publisher = dest[0];
                if (dest.Length >= 2)
                    category = dest[1];
                if (dest.Length >= 3)
                    packageName = dest[2];

                var basePath = m_AssetStoreUtils.BuildBaseDownloadPath(publisher, category);
                m_ProductNewPath = m_AssetStoreUtils.BuildFinalDownloadPath(basePath, packageName);

                var json = m_AssetStoreUtils.CheckDownload(
                    $"{k_AssetStoreDownloadPrefix}{downloadInfo.productId}",
                    downloadInfo.url, dest,
                    downloadInfo.key);

                var resumeOK = false;
                try
                {
                    var current = Json.Deserialize(json) as IDictionary<string, object>;
                    if (current == null)
                        throw new ArgumentException("Invalid JSON");

                    var inProgress = current.Get("in_progress", false);
                    if (inProgress)
                    {
                        if (!isInPause)
                            state = DownloadState.Downloading;
                        return;
                    }

                    var download = current.GetDictionary("download");
                    resumeOK = download != null && download.GetString("url") == downloadInfo.url && download.GetString("key") == downloadInfo.key;
                }
                catch (Exception e)
                {
                    OnErrorMessage(e.Message);
                    return;
                }

                json = $"{{\"download\":{{\"url\":\"{downloadInfo.url}\",\"key\":\"{downloadInfo.key}\"}}}}";
                m_AssetStoreUtils.Download(
                    $"{k_AssetStoreDownloadPrefix}{downloadInfo.productId}",
                    downloadInfo.url,
                    dest,
                    downloadInfo.key,
                    json,
                    resumeOK && resume);

                state = DownloadState.Connecting;
            },
            error =>
            {
                m_DownloadInfo = null;
                OnErrorMessage(error.message, error.operationErrorCode, error.attribute);
            });
        }
    }
}
