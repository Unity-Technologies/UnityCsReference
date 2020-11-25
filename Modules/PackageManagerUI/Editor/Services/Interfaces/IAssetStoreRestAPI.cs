// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    internal interface IAssetStoreRestAPI
    {
        void GetPurchases(string query, Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction = null);

        void GetCategories(Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction = null);

        void GetTaggings(Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction = null);

        void GetProductDetail(long productID, Action<Dictionary<string, object>> doneCallbackAction);

        void GetDownloadDetail(long productID, Action<AssetStoreDownloadInfo> doneCallbackAction);

        void GetProductUpdateDetail(IEnumerable<AssetStoreLocalInfo> localInfos, Action<Dictionary<string, object>> doneCallbackAction);

        void HandleHttpRequest(IAsyncHTTPClient httpRequest, Action<Dictionary<string, object>> doneCallbackAction, Action<UIError> errorCallbackAction);
    }
}
