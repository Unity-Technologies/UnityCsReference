// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEditor.Connect;
using UnityEngine;
using UnityEditor.Analytics;

namespace UnityEditor.Web
{
    [InitializeOnLoad]
    class AnalyticsAccess : CloudServiceAccess
    {
        private const string kServiceName = "Analytics";
        private const string kServiceDisplayName = "Analytics";
        private const string kServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/production/cloud/analytics";

        public override string GetServiceName()
        {
            return kServiceName;
        }

        public override string GetServiceDisplayName()
        {
            return kServiceDisplayName;
        }

        override public bool IsServiceEnabled()
        {
            return AnalyticsSettings.enabled;
        }

        override public void EnableService(bool enabled)
        {
            AnalyticsSettings.enabled = enabled;
        }

        public bool IsTestModeEnabled()
        {
            return AnalyticsSettings.testMode;
        }

        public void SetTestModeEnabled(bool enabled)
        {
            AnalyticsSettings.testMode = enabled;
        }

        static AnalyticsAccess()
        {
            var serviceData = new UnityConnectServiceData(kServiceName, kServiceUrl, new AnalyticsAccess(), "unity/project/cloud/analytics");
            UnityConnectServiceCollection.instance.AddService(serviceData);
        }
    }
}

