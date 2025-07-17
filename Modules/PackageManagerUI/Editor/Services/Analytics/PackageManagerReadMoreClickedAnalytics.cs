// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal;

[AnalyticInfo(k_EventName, k_VendorKey)]
internal class PackageManagerReadMoreClickedAnalytics : IAnalytic
{
    private const string k_EventName = "packageManagerReadMoreClicked";
    private const string k_VendorKey = "unity.package-manager-ui";

    [Serializable]
    internal class Data : IAnalytic.IData
    {
        public string id;
        public string link_url;
        public string[] parameters;
    }

    private readonly Data m_Data;

    private PackageManagerReadMoreClickedAnalytics(string analyticsId, string linkUrl, string[] parameters)
    {
        m_Data = new Data
        {
            id = analyticsId ?? string.Empty,
            link_url = linkUrl ?? string.Empty,
            parameters = parameters ?? Array.Empty<string>()
        };
    }

    public bool TryGatherData(out IAnalytic.IData data, out Exception error)
    {
        data = m_Data;
        error = null;
        return true;
    }

    public static void SendEvent(string analyticsId, string linkUrl, string[] parameters = null)
    {
        var analytics = ServicesContainer.instance.Resolve<IEditorAnalyticsProxy>();
        analytics.SendAnalytic(new PackageManagerReadMoreClickedAnalytics(analyticsId ?? "", linkUrl, parameters));
    }
}
