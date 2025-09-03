// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.InAppPurchasing
{
    [Serializable]
    internal class InAppPurchasingQuickInstallData : IAnalytic.IData
    {
        public string installationMethod;
    }

    [AnalyticInfo(eventName: "iap_quickinstall_packageInstalled", vendorKey: "unity.quickInstall")]
    internal class InAppPurchasingQuickInstallAnalytic : IAnalytic
    {
        private InAppPurchasingQuickInstallData m_data = new InAppPurchasingQuickInstallData();

        public InAppPurchasingQuickInstallAnalytic(string installationMethod)
        {
            m_data.installationMethod = installationMethod;
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_data;
            return data != null;
        }

        public static void SendEvent(string installationMethod)
        {
            InAppPurchasingQuickInstallAnalytic analytic = new InAppPurchasingQuickInstallAnalytic(installationMethod);
            EditorAnalytics.SendAnalytic(analytic);
        }
    }
}
