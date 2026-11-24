// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.PackageManager.UI.Internal
{
    [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey)]
    internal class PackageManagerTrustWindowAnalytics : IAnalytic
    {
        private const string k_EventName = "packageManagerTrustWindowUserAction";
        private const string k_VendorKey = "unity.package-manager-ui";

        [Serializable]
        private class Data : IAnalytic.IData
        {
            public string action;
            public string operation_type;
            public string window_type;
            public string user_security_level;
            public string[] invalid_signature_package_ids;
            public string[] missing_signature_package_ids;
            public string[] limited_trust_package_ids;
        }

        private Data m_Data;
        private PackageManagerTrustWindowAnalytics(
            string action,
            string operationType,
            string windowType,
            string userSecurityLevel,
            string[] invalidSignaturePackageIds,
            string[] missingSignaturePackageIds,
            string[] limitedTrustPackageIds)
        {
            var analyticsScrubber = ServicesContainer.instance.Resolve<IAnalyticsScrubberProxy>();

            m_Data = new Data
            {
                action = action,
                operation_type = operationType,
                window_type = windowType,
                user_security_level = userSecurityLevel,
                invalid_signature_package_ids = ScrubPackageIds(invalidSignaturePackageIds, analyticsScrubber),
                missing_signature_package_ids = ScrubPackageIds(missingSignaturePackageIds, analyticsScrubber),
                limited_trust_package_ids = ScrubPackageIds(limitedTrustPackageIds, analyticsScrubber)
            };
        }

        private static string[] ScrubPackageIds(string[] packageIds, IAnalyticsScrubberProxy analyticsScrubber)
        {
            if (packageIds == null)
                return Array.Empty<string>();

            var scrubbed = new string[packageIds.Length];
            for (var i = 0; i < packageIds.Length; ++i)
                scrubbed[i] = analyticsScrubber.ScrubPackageId(packageIds[i]);
            return scrubbed;
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_Data;
            return data != null;
        }

        public static void SendEvent(TrustAnalyticsData analyticsData)
        {
            var servicesContainer = ServicesContainer.instance;
            var editorAnalyticsProxy = servicesContainer.Resolve<IEditorAnalyticsProxy>();
            var projectSettingsProxy = servicesContainer.Resolve<IProjectSettingsProxy>();

            editorAnalyticsProxy.SendAnalytic(new PackageManagerTrustWindowAnalytics(
                analyticsData.action,
                analyticsData.operationType,
                analyticsData.windowType,
                projectSettingsProxy.trustPolicyLevel.ToString(),
                analyticsData.invalidSignaturePackageIds,
                analyticsData.missingSignaturePackageIds,
                analyticsData.limitedTrustPackageIds));
        }
    }
}
