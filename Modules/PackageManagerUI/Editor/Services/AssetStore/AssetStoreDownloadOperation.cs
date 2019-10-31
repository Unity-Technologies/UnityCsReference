// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class AssetStoreDownloadOperation : IOperation
    {
        internal static readonly string k_LocalizedDownloadErrorMessage = L10n.Tr("The download could not be completed. Please try again. See console for more details.");
        internal static readonly string k_LocalizedAbortErrorMessage = L10n.Tr("The download could not be aborted. Please try again.");
        internal static readonly string k_AssetStoreDownloadPrefix = "content__";

        [SerializeField]
        private string m_ProductId;
        public string packageUniqueId => m_ProductId;

        public string specialUniqueId => string.Empty;
        public string versionUniqueId => string.Empty;

        // a timestamp is added to keep track of how `fresh` the result is
        // it doesn't apply in the case of download operations
        public long timestamp => 0;
        public long lastSuccessTimestamp => 0;

        public bool isOfflineMode => false;

        public bool isInProgress => state == DownloadState.Connecting || state == DownloadState.Downloading || state == DownloadState.Decrypting;

        public bool isProgressTrackable => true;

        public float progressPercentage => m_TotalBytes > 0 ? m_DownloadedBytes / (float)m_TotalBytes : 0.0f;

        public RefreshOptions refreshOptions => RefreshOptions.None;

        public event Action<IOperation, Error> onOperationError = delegate {};
        public event Action<IOperation> onOperationSuccess = delegate {};
        public event Action<IOperation> onOperationFinalized = delegate {};
        public event Action<IOperation> onOperationProgress = delegate {};

        [SerializeField]
        private ulong m_DownloadedBytes;
        [SerializeField]
        private ulong m_TotalBytes;

        [SerializeField]
        private DownloadState m_State;
        public DownloadState state => m_State;

        [SerializeField]
        private string m_ErrorMessage;
        public string errorMessage => m_ErrorMessage;

        [SerializeField]
        private AssetStoreDownloadInfo m_DownloadInfo;
        public AssetStoreDownloadInfo downloadInfo => m_DownloadInfo;

        public void OnDownloadProgress(string message, ulong bytes, ulong total)
        {
            m_DownloadedBytes = bytes;
            m_TotalBytes = total;

            switch (message)
            {
                case "ok":
                    m_State = DownloadState.Completed;
                    onOperationSuccess?.Invoke(this);
                    onOperationFinalized?.Invoke(this);
                    break;
                case "connecting":
                    m_State = DownloadState.Connecting;
                    break;
                case "downloading":
                    m_State = DownloadState.Downloading;
                    break;
                case "decrypt":
                    m_State = DownloadState.Decrypting;
                    break;
                case "aborted":
                    m_DownloadedBytes = 0;
                    m_State = DownloadState.Aborted;
                    m_ErrorMessage = L10n.Tr("Download aborted");
                    onOperationError?.Invoke(this, new Error(NativeErrorCode.Unknown, m_ErrorMessage));
                    onOperationFinalized?.Invoke(this);
                    break;
                default:
                    OnErrorMessage(message);
                    break;
            }

            onOperationProgress?.Invoke(this);
        }

        private void OnErrorMessage(string errorMessage)
        {
            m_State = DownloadState.Error;
            m_ErrorMessage = k_LocalizedDownloadErrorMessage;
            Debug.LogError(errorMessage);
            onOperationError?.Invoke(this, new Error(NativeErrorCode.Unknown, m_ErrorMessage));
            onOperationFinalized?.Invoke(this);
        }

        public void Abort()
        {
            if (downloadInfo?.isValid != true)
                return;

            if (state == DownloadState.Aborted || state == DownloadState.Completed || state == DownloadState.Error)
                return;

            // the actual download state change from `downloading` to `aborted` happens in `OnDownloadProgress` callback
            if (!AssetStoreUtils.instance.AbortDownload($"{k_AssetStoreDownloadPrefix}{m_ProductId}", downloadInfo.destination))
                Debug.LogError(k_LocalizedAbortErrorMessage);
        }

        public void Download()
        {
            var productId = long.Parse(m_ProductId);
            AssetStoreRestAPI.instance.GetDownloadDetail(productId, downloadInfo =>
            {
                m_DownloadInfo = downloadInfo;
                if (!downloadInfo.isValid)
                {
                    OnErrorMessage(downloadInfo.errorMessage);
                    return;
                }

                var dest = downloadInfo.destination;

                var json = AssetStoreUtils.instance.CheckDownload(
                    $"{k_AssetStoreDownloadPrefix}{downloadInfo.productId}",
                    downloadInfo.url, dest,
                    downloadInfo.key);

                var resumeOK = false;
                try
                {
                    json = Regex.Replace(json, "\"url\":(?<url>\"?[^,]+\"?),\"", "\"url\":\"${url}\",\"");
                    json = Regex.Replace(json, "\"key\":(?<key>\"?[0-9a-zA-Z]*\"?)\\}", "\"key\":\"${key}\"}");
                    json = Regex.Replace(json, "\"+(?<value>[^\"]+)\"+", "\"${value}\"");

                    var current = Json.Deserialize(json) as IDictionary<string, object>;
                    if (current == null)
                        throw new ArgumentException("Invalid JSON");

                    var inProgress = current.ContainsKey("in_progress") && (current["in_progress"] is bool? (bool)current["in_progress"] : false);
                    if (inProgress)
                    {
                        m_State = DownloadState.Downloading;
                        return;
                    }

                    if (current.ContainsKey("download") && current["download"] is IDictionary<string, object>)
                    {
                        var download = (IDictionary<string, object>)current["download"];
                        var existingUrl = download.ContainsKey("url") ? download["url"] as string : string.Empty;
                        var existingKey = download.ContainsKey("key") ? download["key"] as string : string.Empty;
                        resumeOK = (existingUrl == downloadInfo.url && existingKey == downloadInfo.key);
                    }
                }
                catch (Exception e)
                {
                    OnErrorMessage(e.Message);
                    return;
                }

                json = $"{{\"download\":{{\"url\":\"{downloadInfo.url}\",\"key\":\"{downloadInfo.key}\"}}}}";
                AssetStoreUtils.instance.Download(
                    $"{k_AssetStoreDownloadPrefix}{downloadInfo.productId}",
                    downloadInfo.url,
                    dest,
                    downloadInfo.key,
                    json,
                    resumeOK);

                m_State = DownloadState.Connecting;
            });
        }

        public AssetStoreDownloadOperation(string productId)
        {
            m_ProductId = productId;
        }
    }
}
