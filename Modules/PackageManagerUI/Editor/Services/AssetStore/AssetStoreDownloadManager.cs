// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    using CachePathConfig = AssetStoreCachePathManager.CachePathConfig;
    using ConfigStatus = AssetStoreCachePathManager.ConfigStatus;

    [Serializable]
    internal class AssetStoreDownloadManager : ISerializationCallbackReceiver
    {
        public virtual event Action<IOperation, UIError> onDownloadError = delegate {};
        public virtual event Action<IOperation> onDownloadFinalized = delegate {};
        public virtual event Action<IOperation> onDownloadProgress = delegate {};
        public virtual event Action<IOperation> onDownloadPaused = delegate {};

        private Dictionary<string, AssetStoreDownloadOperation> m_DownloadOperations = new Dictionary<string, AssetStoreDownloadOperation>();

        [SerializeField]
        private AssetStoreDownloadOperation[] m_SerializedDownloadOperations = new AssetStoreDownloadOperation[0];

        [NonSerialized]
        private ApplicationProxy m_Application;
        [NonSerialized]
        private HttpClientFactory m_HttpClientFactory;
        [NonSerialized]
        private UnityConnectProxy m_UnityConnect;
        [NonSerialized]
        private IOProxy m_IOProxy;
        [NonSerialized]
        private AssetStoreCache m_AssetStoreCache;
        [NonSerialized]
        private AssetStoreUtils m_AssetStoreUtils;
        [NonSerialized]
        private AssetStoreRestAPI m_AssetStoreRestAPI;
        [NonSerialized]
        private AssetStoreCachePathProxy m_AssetStoreCachePathProxy;
        public void ResolveDependencies(ApplicationProxy application,
            HttpClientFactory httpClientFactory,
            UnityConnectProxy unityConnect,
            IOProxy ioProxy,
            AssetStoreCache assetStoreCache,
            AssetStoreUtils assetStoreUtils,
            AssetStoreRestAPI assetStoreRestAPI,
            AssetStoreCachePathProxy assetStoreCachePathProxy)
        {
            m_Application = application;
            m_UnityConnect = unityConnect;
            m_IOProxy = ioProxy;
            m_HttpClientFactory = httpClientFactory;
            m_AssetStoreCache = assetStoreCache;
            m_AssetStoreUtils = assetStoreUtils;
            m_AssetStoreRestAPI = assetStoreRestAPI;
            m_AssetStoreCachePathProxy = assetStoreCachePathProxy;

            foreach (var operation in m_DownloadOperations.Values)
                operation.ResolveDependencies(assetStoreUtils, assetStoreRestAPI, m_AssetStoreCachePathProxy);
        }

        // The AssetStoreDownloadManager implementation requires the help of a ScriptableObject to dispatch download progress event.
        private class DownloadDelegateHandler : ScriptableObject
        {
            public AssetStoreDownloadManager downloadManager { get; set; }

            public void OnDownloadProgress(string downloadId, string message, ulong bytes, ulong total, int errorCode)
            {
                downloadManager?.OnDownloadProgress(downloadId, message, bytes, total, errorCode);
            }
        }

        [NonSerialized]
        private DownloadDelegateHandler m_DownloadDelegateHandler;
        [SerializeField]
        private int m_DownloadDelegateHandlerInstanceId = 0;
        [SerializeField]
        private bool m_DelegateRegistered = false;

        private DownloadDelegateHandler downloadDelegateHandler
        {
            get
            {
                if (m_DownloadDelegateHandler != null)
                    return m_DownloadDelegateHandler;

                if (m_DownloadDelegateHandlerInstanceId != 0)
                    m_DownloadDelegateHandler = UnityEngine.Object.FindObjectFromInstanceID(m_DownloadDelegateHandlerInstanceId) as DownloadDelegateHandler;

                if (m_DownloadDelegateHandler == null)
                {
                    m_DownloadDelegateHandler = ScriptableObject.CreateInstance<DownloadDelegateHandler>();
                    m_DownloadDelegateHandler.hideFlags = HideFlags.DontSave;
                }
                m_DownloadDelegateHandler.downloadManager = this;
                m_DownloadDelegateHandlerInstanceId = m_DownloadDelegateHandler.GetInstanceID();
                return m_DownloadDelegateHandler;
            }
        }

        private void RegisterDownloadDelegate()
        {
            if (m_DelegateRegistered)
                return;
            m_AssetStoreUtils.RegisterDownloadDelegate(downloadDelegateHandler);
            m_DelegateRegistered = true;
        }

        private void UnRegisterDownloadDelegate()
        {
            if (!m_DelegateRegistered)
                return;
            m_AssetStoreUtils.UnRegisterDownloadDelegate(downloadDelegateHandler);
            m_DelegateRegistered = false;
        }

        public virtual bool IsAnyDownloadInProgress()
        {
            return m_DownloadOperations.Values.Any(d => d.isInProgress);
        }

        public virtual bool IsAnyDownloadInProgressOrPause()
        {
            return m_DownloadOperations.Values.Any(d => d.isInProgress || d.isInPause);
        }

        public virtual int DownloadInProgressCount()
        {
            return m_DownloadOperations.Values.Count(d => d.isInProgress);
        }

        public virtual void Download(IPackage package)
        {
            var packageId = package?.uniqueId;
            if (string.IsNullOrEmpty(packageId))
                return;

            var operation = GetDownloadOperation(packageId);
            if (operation?.isInProgress ?? false)
                return;

            var localInfo = m_AssetStoreCache.GetLocalInfo(packageId);
            operation = new AssetStoreDownloadOperation(m_AssetStoreUtils, m_AssetStoreRestAPI, m_AssetStoreCachePathProxy, packageId, localInfo?.packagePath);
            SetupDownloadOperation(operation);
            operation.Download(false);
        }

        public virtual void ClearCache()
        {
            AbortAllDownloads();
        }

        public virtual AssetStoreDownloadOperation GetDownloadOperation(string productId)
        {
            return string.IsNullOrEmpty(productId) ? null : m_DownloadOperations.Get(productId);
        }

        private void SetupDownloadOperation(AssetStoreDownloadOperation operation)
        {
            m_DownloadOperations[operation.packageUniqueId] = operation;
            operation.onOperationError += (op, error) => onDownloadError?.Invoke(op, error);
            operation.onOperationFinalized += OnDownloadFinalized;
            operation.onOperationProgress += (op) => onDownloadProgress?.Invoke(op);
            operation.onOperationPaused += (op) => onDownloadPaused?.Invoke(op);
        }

        private void OnDownloadFinalized(IOperation operation)
        {
            onDownloadFinalized?.Invoke(operation);

            var downloadOperation = operation as AssetStoreDownloadOperation;
            if (downloadOperation == null)
                return;

            if (downloadOperation.state == DownloadState.Completed &&
                !string.IsNullOrEmpty(downloadOperation.packageOldPath) && downloadOperation.packageNewPath != downloadOperation.packageOldPath)
            {
                m_IOProxy.DeleteFile(downloadOperation.packageOldPath);
            }
        }

        private void RemoveDownloadOperation(string productId)
        {
            if (m_DownloadOperations.ContainsKey(productId))
                m_DownloadOperations.Remove(productId);
        }

        // This function will be called by AssetStoreUtils after the download delegate registration
        // assetStoreUtils.RegisterDownloadDelegate
        public virtual void OnDownloadProgress(string downloadId, string message, ulong bytes, ulong total, int errorCode)
        {
            // The download unique id we receive from the A$ download callback could be in the format of
            // `123456` or `content__123456` (depending on where the download starts).
            // therefore we have a progress that makes sure everything is in the format of `123456`.
            var productId = downloadId;
            if (downloadId.StartsWith(AssetStoreDownloadOperation.k_AssetStoreDownloadPrefix))
                productId = downloadId.Substring(AssetStoreDownloadOperation.k_AssetStoreDownloadPrefix.Length);

            // `GetDownloadOperation` could return null when the user starts the download in the legacy `Asset Store` window
            // in those cases, we create a download operation to track it
            // NOTE: Now that the CEF window doesn't exist anymore if we have a null operation we shouldn't manage it.
            var operation = GetDownloadOperation(productId);
            if (operation == null)
                return;
            operation.OnDownloadProgress(message, bytes, total, errorCode);

            if (!operation.isInProgress && !operation.isInPause)
                RemoveDownloadOperation(productId);
        }

        public virtual void AbortAllDownloads()
        {
            var operations = m_DownloadOperations.Values.ToList();
            m_DownloadOperations.Clear();
            foreach (var operation in operations)
                operation.Cancel();
        }

        public virtual void AbortDownload(string productId)
        {
            GetDownloadOperation(productId)?.Abort();
        }

        public virtual void PauseDownload(string productId)
        {
            GetDownloadOperation(productId)?.Pause();
        }

        public virtual void ResumeDownload(string productId)
        {
            var operation = GetDownloadOperation(productId);
            if (!operation?.isInPause ?? true)
                return;

            operation.Download(true);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (m_UnityConnect.isUserLoggedIn)
                RegisterDownloadDelegate();
        }

        public void OnEnable()
        {
            m_Application.onPlayModeStateChanged += OnPlayModeStateChanged;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_AssetStoreCachePathProxy.onConfigChanged += OnAssetStoreCacheConfigChange;
            if (m_UnityConnect.isUserLoggedIn)
                RegisterDownloadDelegate();
        }

        public void OnDisable()
        {
            m_Application.onPlayModeStateChanged -= OnPlayModeStateChanged;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            UnRegisterDownloadDelegate();
        }

        private void OnUserLoginStateChange(bool userInfoReady, bool loggedIn)
        {
            if (loggedIn)
            {
                RegisterDownloadDelegate();
            }
            else
            {
                UnRegisterDownloadDelegate();
                AbortAllDownloads();
            }
        }

        private void OnAssetStoreCacheConfigChange(CachePathConfig config)
        {
            if ((config.status == ConfigStatus.Success || config.status == ConfigStatus.ReadOnly) && IsAnyDownloadInProgressOrPause())
            {
                if (!m_Application.isBatchMode)
                    m_Application.DisplayDialog("assetCacheLocationChanged",
                        L10n.Tr("Assets Cache location changed"),
                        L10n.Tr("The Assets Cache location has been changed, all current downloads will be aborted."),
                        L10n.Tr("Ok"));

                AbortAllDownloads();
            }
        }

        public void OnBeforeSerialize()
        {
            m_SerializedDownloadOperations = m_DownloadOperations.Values.ToArray();
        }

        public void OnAfterDeserialize()
        {
            foreach (var operation in m_SerializedDownloadOperations)
                SetupDownloadOperation(operation);
        }
    }
}
