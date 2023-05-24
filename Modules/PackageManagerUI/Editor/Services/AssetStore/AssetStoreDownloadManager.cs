// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEditor.Utils;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    using CachePathConfig = AssetStoreCachePathManager.CachePathConfig;
    using ConfigStatus = AssetStoreCachePathManager.ConfigStatus;

    [Serializable]
    internal class AssetStoreDownloadManager : ISerializationCallbackReceiver
    {
        private const string k_TermsOfServicesURL = "https://assetstore.unity.com/account/term";

        public virtual event Action<AssetStoreDownloadOperation, UIError> onDownloadError = delegate {};
        public virtual event Action<AssetStoreDownloadOperation> onDownloadFinalized = delegate {};
        public virtual event Action<AssetStoreDownloadOperation> onDownloadProgress = delegate {};
        public virtual event Action<AssetStoreDownloadOperation> onDownloadStateChanged = delegate {};
        public virtual event Action<long> onBeforeDownloadStart = delegate {};

        private Dictionary<long, AssetStoreDownloadOperation> m_DownloadOperations = new Dictionary<long, AssetStoreDownloadOperation>();

        [SerializeField]
        private bool m_TermsOfServiceAccepted = false;

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
                operation.ResolveDependencies(assetStoreUtils, assetStoreRestAPI, m_AssetStoreCache, m_AssetStoreCachePathProxy);
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

        private void Download(long productId)
        {
            if (productId <= 0)
                return;

            var operation = GetDownloadOperation(productId);
            if (operation?.isInProgress ?? false)
                return;

            onBeforeDownloadStart?.Invoke(productId);

            var localInfo = m_AssetStoreCache.GetLocalInfo(productId);
            operation = new AssetStoreDownloadOperation(m_AssetStoreUtils, m_AssetStoreRestAPI, m_AssetStoreCache, m_AssetStoreCachePathProxy, productId, localInfo?.packagePath);
            SetupDownloadOperation(operation);
            operation.Download(false);
        }

        public virtual bool Download(IEnumerable<long> productIds)
        {
            return CheckTermsOfServiceAgreement(
                () =>
                {
                    foreach (var productId in productIds)
                        Download(productId);
                },
                error =>
                {
                    // ToS error are not related to any specific package, and we don't really have a good place to
                    // show the error in the UI. It is to be addressed in https://jira.unity3d.com/browse/PAX-1994.
                    Debug.Log(error.message);
                });
        }

        public virtual void ClearCache()
        {
            AbortAllDownloads();
        }

        public virtual AssetStoreDownloadOperation GetDownloadOperation(long? productId)
        {
            return productId > 0 ? m_DownloadOperations.Get(productId.Value) : null;
        }

        private void SetupDownloadOperation(AssetStoreDownloadOperation operation)
        {
            m_DownloadOperations[operation.productId] = operation;
            operation.onOperationError += (_, error) => onDownloadError?.Invoke(operation, error);
            operation.onOperationFinalized += (_) => OnDownloadFinalized(operation);
            operation.onOperationProgress += (_) => onDownloadProgress?.Invoke(operation);
            operation.onDownloadStateChanged += (_) => onDownloadStateChanged?.Invoke(operation);
        }

        private void OnDownloadFinalized(AssetStoreDownloadOperation operation)
        {
            onDownloadFinalized?.Invoke(operation);
            if (operation.state == DownloadState.Completed &&
                !string.IsNullOrEmpty(operation.packageOldPath) && operation.packageNewPath.NormalizePath() != operation.packageOldPath.NormalizePath())
            {
                m_IOProxy.DeleteFile(operation.packageOldPath);
            }
        }

        private void RemoveDownloadOperation(long productId)
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
            if (downloadId.StartsWith(AssetStoreDownloadOperation.k_AssetStoreDownloadPrefix))
                downloadId = downloadId.Substring(AssetStoreDownloadOperation.k_AssetStoreDownloadPrefix.Length);

            // `GetDownloadOperation` could return null when the user starts the download in the legacy `Asset Store` window
            // in those cases, we create a download operation to track it
            // NOTE: Now that the CEF window doesn't exist anymore if we have a null operation we shouldn't manage it.
            if (!long.TryParse(downloadId, out var productId))
                return;

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

        public virtual void AbortDownload(long? productId)
        {
            GetDownloadOperation(productId)?.Abort();
        }

        public virtual void PauseDownload(long? productId)
        {
            GetDownloadOperation(productId)?.Pause();
        }

        public virtual void ResumeDownload(long? productId)
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
            m_TermsOfServiceAccepted = false;
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

        // Returns true if Terms of Service agreement has already been accepted
        private bool CheckTermsOfServiceAgreement(Action onTosAccepted, Action<UIError> onError)
        {
            if (!m_TermsOfServiceAccepted)
            {
                m_AssetStoreRestAPI.CheckTermsAndConditions(tosAccepted =>
                {
                    m_TermsOfServiceAccepted = tosAccepted;
                    if (m_TermsOfServiceAccepted)
                        onTosAccepted?.Invoke();
                    else
                    {
                        var result = m_Application.DisplayDialog("acceptToS",
                            L10n.Tr("Accepting Terms of Service and EULA"),
                            L10n.Tr("You need to accept Asset Store Terms of Service and EULA before you can download/update any package."),
                            L10n.Tr("Read and accept"), L10n.Tr("Close"));

                        if (result)
                            m_UnityConnect.OpenAuthorizedURLInWebBrowser(k_TermsOfServicesURL);
                    }

                }, error => onError?.Invoke(error));
                return false;
            }
            onTosAccepted?.Invoke();
            return true;
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
