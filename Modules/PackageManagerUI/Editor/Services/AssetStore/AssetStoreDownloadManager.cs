// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    internal sealed class AssetStoreDownloadManager
    {
        static IDownloadManager s_Instance = null;
        public static IDownloadManager instance => s_Instance ?? AssetStoreDownloadManagerInternal.instance;

        [Serializable]
        private class AssetStoreDownloadManagerInternal : ScriptableSingleton<AssetStoreDownloadManagerInternal>, IDownloadManager, ISerializationCallbackReceiver
        {
            public event Action<IOperation, Error> onDownloadError = delegate {};
            public event Action<IOperation> onDownloadFinalized = delegate {};
            public event Action<IOperation> onDownloadProgress = delegate {};

            private Texture2D m_MissingTexture;

            private Dictionary<string, AssetStoreDownloadOperation> m_DownloadOperations = new Dictionary<string, AssetStoreDownloadOperation>();

            [SerializeField]
            private AssetStoreDownloadOperation[] m_SerializedDownloadOperations = new AssetStoreDownloadOperation[0];

            [NonSerialized]
            private bool m_EventsRegistered;

            public bool IsAnyDownloadInProgress()
            {
                return m_DownloadOperations.Values.Any(d => d.isInProgress);
            }

            public void Download(string productId)
            {
                var operation = GetDownloadOperation(productId);
                if (operation?.isInProgress ?? false)
                    return;

                operation = new AssetStoreDownloadOperation(productId);
                SetupDownloadOperation(operation);
                operation.Download();
            }

            public void DownloadImageAsync(long productID, string url, Action<long, Texture2D> doneCallbackAction = null)
            {
                if (m_MissingTexture == null)
                {
                    m_MissingTexture = (Texture2D)EditorGUIUtility.LoadRequired("Icons/UnityLogo.png");
                }

                var texture = AssetStoreCache.instance.LoadImage(productID, url);
                if (texture != null)
                {
                    doneCallbackAction?.Invoke(productID, texture);
                    return;
                }

                var httpRequest = ApplicationUtil.instance.GetASyncHTTPClient(url);
                httpRequest.doneCallback = httpClient =>
                {
                    if (httpClient.IsSuccess() && httpClient.texture != null)
                    {
                        AssetStoreCache.instance.SaveImage(productID, url, httpClient.texture);
                        doneCallbackAction?.Invoke(productID, httpClient.texture);
                        return;
                    }

                    doneCallbackAction?.Invoke(productID, m_MissingTexture);
                };
                httpRequest.Begin();
            }

            public void ClearCache()
            {
                m_MissingTexture = null;

                AbortAllDownloads();
            }

            public AssetStoreDownloadOperation GetDownloadOperation(string productId)
            {
                return string.IsNullOrEmpty(productId) ? null : m_DownloadOperations.Get(productId);
            }

            private void SetupDownloadOperation(AssetStoreDownloadOperation operation)
            {
                m_DownloadOperations[operation.packageUniqueId] = operation;
                operation.onOperationError += (op, error) => onDownloadError?.Invoke(op, error);
                operation.onOperationFinalized += (op) => onDownloadFinalized?.Invoke(op);
                operation.onOperationProgress += (op) => onDownloadProgress?.Invoke(op);
            }

            private void RemoveDownloadOperation(string productId)
            {
                if (m_DownloadOperations.ContainsKey(productId))
                    m_DownloadOperations.Remove(productId);
            }

            // This function will be called by AssetStoreUtils after the download delegate registration
            // AssetStoreUtils.instance.RegisterDownloadDelegate
            public void OnDownloadProgress(string downloadId, string message, ulong bytes, ulong total)
            {
                // The download unique id we receive from the A$ download callback could be in the format of
                // `123456` or `content__123456` (depending on where the download starts).
                // therefore we have a progress that makes sure everything is in the format of `123456`.
                var productId = downloadId;
                if (downloadId.StartsWith(AssetStoreDownloadOperation.k_AssetStoreDownloadPrefix))
                    productId = downloadId.Substring(AssetStoreDownloadOperation.k_AssetStoreDownloadPrefix.Length);

                // `GetDownloadOperation` could return null when the user starts the download in the legacy `Asset Store` window
                // in those cases, we create a download operation to track it
                var operation = GetDownloadOperation(productId);
                if (operation == null)
                {
                    operation = new AssetStoreDownloadOperation(productId);
                    SetupDownloadOperation(operation);
                }
                operation.OnDownloadProgress(message, bytes, total);

                if (!operation.isInProgress)
                    RemoveDownloadOperation(productId);
            }

            public void AbortAllDownloads()
            {
                foreach (var operation in m_DownloadOperations.Values)
                    operation.Abort();
                m_DownloadOperations.Clear();
            }

            public void AbortDownload(string productId)
            {
                GetDownloadOperation(productId)?.Abort();
            }

            public void RegisterEvents()
            {
                if (m_EventsRegistered)
                    return;
                m_EventsRegistered = true;

                ApplicationUtil.instance.onUserLoginStateChange += OnUserLoginStateChange;
                if (ApplicationUtil.instance.isUserLoggedIn)
                    AssetStoreUtils.instance.RegisterDownloadDelegate(this);
            }

            public void UnregisterEvents()
            {
                if (!m_EventsRegistered)
                    return;
                m_EventsRegistered = false;

                ApplicationUtil.instance.onUserLoginStateChange -= OnUserLoginStateChange;
                AssetStoreUtils.instance.UnRegisterDownloadDelegate(this);
            }

            private void OnUserLoginStateChange(bool loggedIn)
            {
                if (!loggedIn)
                {
                    AssetStoreUtils.instance.UnRegisterDownloadDelegate(this);
                    AbortAllDownloads();
                }
                else
                {
                    AssetStoreUtils.instance.RegisterDownloadDelegate(this);
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
}
