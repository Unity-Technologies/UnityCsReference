// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal
{
    [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey)]
    internal class AssetSelectionWindowAnalytics : IAnalytic
    {
        private const string k_EventName = "assetSelectionWindow";
        private const string k_VendorKey = "unity.package-manager-ui";

        [Serializable]
        private class Data : IAnalytic.IData
        {
            public string window_type;
            public string action;
            public int product_id;
            public int num_selected_assets;
            public int num_total_assets;
        }

        private Data m_Data;
        private AssetSelectionWindowAnalytics(SelectionWindowData windowData, string action)
        {
            var productId = windowData.assets.Count > 0 ? windowData.assets[0].origin.productId : 0;
            m_Data = new Data
            {
                window_type = "removeWindow",
                action = action,
                product_id = productId,
                num_selected_assets = windowData.selectedAssets.Count,
                num_total_assets = windowData.assets.Count
            };
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_Data;
            return data != null;
        }

        public static void SendEvent(SelectionWindowData data, string action)
        {
            var editorAnalyticsProxy = ServicesContainer.instance.Resolve<IEditorAnalyticsProxy>();
            editorAnalyticsProxy.SendAnalytic(new AssetSelectionWindowAnalytics(data, action));
        }
    }
}
