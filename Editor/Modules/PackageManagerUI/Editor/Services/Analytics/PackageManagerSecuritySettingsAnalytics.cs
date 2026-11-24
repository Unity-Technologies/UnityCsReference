// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal;

[AnalyticInfo(k_EventName, k_VendorKey)]
internal class PackageManagerSecuritySettingsAnalytics : IAnalytic
{
    private const string k_EventName = "packageManagerSecuritySettings";
    private const string k_VendorKey = "unity.package-manager-ui";

    [Serializable]
    internal class Data : IAnalytic.IData
    {
        public string action;
        public string previous_security_level;
        public string new_security_level;
    }

    private readonly Data m_Data;

    private PackageManagerSecuritySettingsAnalytics(string action, TrustPolicyLevel previousSecurityLevel, TrustPolicyLevel newSecurityLevel)
    {
        m_Data = new Data
        {
            action = action,
            previous_security_level = previousSecurityLevel.ToString(),
            new_security_level = newSecurityLevel.ToString()
        };
    }

    public bool TryGatherData(out IAnalytic.IData data, out Exception error)
    {
        data = m_Data;
        error = null;
        return true;
    }

    public static void SendEvent(string action, TrustPolicyLevel previousSecurityLevel, TrustPolicyLevel newSecurityLevel)
    {
        var editorAnalyticsProxy = ServicesContainer.instance.Resolve<IEditorAnalyticsProxy>();
        editorAnalyticsProxy.SendAnalytic(new PackageManagerSecuritySettingsAnalytics(action, previousSecurityLevel, newSecurityLevel));
    }
}
