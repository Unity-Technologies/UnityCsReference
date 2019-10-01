// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.AssetStore
{
    internal class DownloadResult
    {
        public DownloadProgress.State downloadState;
        public string errorMessage;
    }

    internal interface IDownloadOperation
    {
        void ClearDownloadInformation(string productID);

        void AbortDownloadPackage(long productID, Action<DownloadResult> doneCallbackAction = null);

        void DownloadUnityPackageAsync(long productID, Action<DownloadResult> doneCallbackAction = null);

        void DownloadImageAsync(long productID, string url, Action<long, Texture2D> doneCallbackAction = null);

        void ClearCache();
    }
}
