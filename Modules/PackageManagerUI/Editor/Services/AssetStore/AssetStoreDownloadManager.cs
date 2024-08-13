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

    internal interface IAssetStoreDownloadManager : IService
    {
        event Action<AssetStoreDownloadOperation, UIError> onDownloadError;
        event Action<AssetStoreDownloadOperation> onDownloadFinalized;
        event Action<AssetStoreDownloadOperation> onDownloadProgress;
        event Action<AssetStoreDownloadOperation> onDownloadStateChanged;
        event Action<long> onBeforeDownloadStart;

        bool IsAnyDownloadInProgress();
        bool IsAnyDownloadInProgressOrPause();
        int DownloadInProgressCount();
        bool Download(IEnumerable<long> productIds);
        void ClearCache();
        AssetStoreDownloadOperation GetDownloadOperation(long? productId);
        void OnDownloadProgress(string downloadId, string message, ulong bytes, ulong total, int errorCode);
        void AbortAllDownloads();
        void AbortDownload(long? productId);
        void PauseDownload(long? productId);
        void ResumeDownload(long? productId);
    }

    [Serializable]
    internal class AssetStoreDownloadManager : BaseService<IAssetStoreDownloadManager>, IAssetStoreDownloadManager, ISerializationCallbackReceiver
    {
        private const string k_TermsOfServicesURL = "https://assetstore.unity.com/account/term";

        public event Action<AssetStoreDownloadOperation, UIError> onDownloadError = delegate {};
        public event Action<AssetStoreDownloadOperation> onDownloadFinalized = delegate {};
        public event Action<AssetStoreDownloadOperation> onDownloadProgress = delegate {};
        public event Action<AssetStoreDownloadOperation> onDownloadStateChanged = delegate {};
        public event Action<long> onBeforeDownloadStart = delegate {};

        private readonly Dictionary<long, AssetStoreDownloadOperation> m_DownloadOperations = new();

        [SerializeField]
        private bool m_TermsOfServiceAccepted = false;

        [SerializeField]
        private AssetStoreDownloadOperation[] m_SerializedDownloadOperations = new AssetStoreDownloadOperation[0];

        private readonly IApplicationProxy m_Application;
        private readonly IUnityConnectProxy m_UnityConnect;
        private readonly IIOProxy m_IOProxy;
        private readonly IAssetStoreCache m_AssetStoreCache;
        private readonly IAssetStoreUtils m_AssetStoreUtils;
        private readonly IAssetStoreRestAPI m_AssetStoreRestAPI;
        private readonly IAssetStoreCachePathProxy m_AssetStoreCachePathProxy;
        private readonly ILocalInfoHandler m_LocalInfoHandler;
        public AssetStoreDownloadManager(IApplicationProxy application,
            IUnityConnectProxy unityConnect,
            IIOProxy ioProxy,
            IAssetStoreCache assetStoreCache,
            IAssetStoreUtils assetStoreUtils,
            IAssetStoreRestAPI assetStoreRestAPI,
            IAssetStoreCachePathProxy assetStoreCachePathProxy,
            ILocalInfoHandler localInfoHandler)
        {
            m_Application = RegisterDependency(application);
            m_UnityConnect = RegisterDependency(unityConnect);
            m_IOProxy = RegisterDependency(ioProxy);
            m_AssetStoreCache = RegisterDependency(assetStoreCache);
            m_AssetStoreUtils = RegisterDependency(assetStoreUtils);
            m_AssetStoreRestAPI = RegisterDependency(assetStoreRestAPI);
            m_AssetStoreCachePathProxy = RegisterDependency(assetStoreCachePathProxy);
            m_LocalInfoHandler = RegisterDependency(localInfoHandler);
        }

        // The AssetStoreDownloadManager implementation requires the help of a ScriptableObject to dispatch download progress event.
        private class DownloadDelegateHandler : ScriptableObject
        {
            public void OnDownloadProgress(string downloadId, string message, ulong bytes, ulong total, int errorCode)
            {
                ServicesContainer.instance.Resolve<AssetStoreDownloadManager>().OnDownloadProgress(downloadId, message, bytes, total, errorCode);
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

        public bool IsAnyDownloadInProgress()
        {
            return m_DownloadOperations.Values.Any(d => d.isInProgress);
        }

        public bool IsAnyDownloadInProgressOrPause()
        {
            return m_DownloadOperations.Values.Any(d => d.isInProgress || d.isInPause);
        }

        public int DownloadInProgressCount()
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
            operation = new AssetStoreDownloadOperation(m_IOProxy, m_AssetStoreUtils, m_AssetStoreRestAPI, m_AssetStoreCache, m_AssetStoreCachePathProxy, m_LocalInfoHandler, productId, localInfo?.packagePath);
            SetupDownloadOperation(operation);
            operation.Download(false);
        }

        public bool Download(IEnumerable<long> productIds)
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

        public void ClearCache()
        {
            AbortAllDownloads();
        }

        public AssetStoreDownloadOperation GetDownloadOperation(long? productId)
        {
            return productId > 0 ? m_DownloadOperations.Get(productId.Value) : null;
        }

        private void SetupDownloadOperation(AssetStoreDownloadOperation operation)
        {
            m_DownloadOperations[operation.productId] = operation;
            operation.onOperationError += (_, error) => onDownloadError?.Invoke(operation, error);
            operation.onOperationFinalized += _ => OnDownloadFinalized(operation);
            operation.onOperationProgress += _ => onDownloadProgress?.Invoke(operation);
            operation.onDownloadStateChanged += _ => onDownloadStateChanged?.Invoke(operation);
        }

        private void OnDownloadFinalized(AssetStoreDownloadOperation operation)
        {
            onDownloadFinalized?.Invoke(operation);
        }

        private void RemoveDownloadOperation(long productId)
        {
            if (m_DownloadOperations.ContainsKey(productId))
                m_DownloadOperations.Remove(productId);
        }

        // This function will be called by AssetStoreUtils after the download delegate registration
        // assetStoreUtils.RegisterDownloadDelegate
        public void OnDownloadProgress(string downloadId, string message, ulong bytes, ulong total, int errorCode)
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

        public void AbortAllDownloads()
        {
            var operations = m_DownloadOperations.Values.ToList();
            m_DownloadOperations.Clear();
            foreach (var operation in operations)
                operation.Cancel();
        }

        public void AbortDownload(long? productId)
        {
            GetDownloadOperation(productId)?.Abort();
        }

        public void PauseDownload(long? productId)
        {
            GetDownloadOperation(productId)?.Pause();
        }

        public void ResumeDownload(long? productId)
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

        public override void OnEnable()
        {
            m_Application.onPlayModeStateChanged += OnPlayModeStateChanged;
            m_UnityConnect.onUserLoginStateChange += OnUserLoginStateChange;
            m_AssetStoreCachePathProxy.onConfigChanged += OnAssetStoreCacheConfigChange;
            if (m_UnityConnect.isUserLoggedIn)
                RegisterDownloadDelegate();
        }

        public override void OnDisable()
        {
            m_Application.onPlayModeStateChanged -= OnPlayModeStateChanged;
            m_UnityConnect.onUserLoginStateChange -= OnUserLoginStateChange;
            m_AssetStoreCachePathProxy.onConfigChanged -= OnAssetStoreCacheConfigChange;
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
                        L10n.Tr("The Assets Cache location has been changed, all current downloads will be canceled."),
                        L10n.Tr("OK"));

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
            {
                operation.ResolveDependencies(m_IOProxy, m_AssetStoreUtils, m_AssetStoreRestAPI, m_AssetStoreCache, m_AssetStoreCachePathProxy, m_LocalInfoHandler);
                SetupDownloadOperation(operation);
            }
        }
    }
}
