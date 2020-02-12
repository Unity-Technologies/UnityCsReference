// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEditor.Analytics;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal class AnalyticsService : SingleService
    {
        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; }
        public override string settingsProviderClassName => nameof(AnalyticsProjectSettings);
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.AnalyticsService;
        public override string packageName { get; }
        public override string serviceFlagName { get; }
        public override bool shouldEnableOnProjectCreation => true;
        public override bool shouldSyncOnProjectRebind => true;

        static readonly AnalyticsService k_Instance;

        public static AnalyticsService instance => k_Instance;

        private struct AnalyticsServiceState { public bool analytics; }

        static AnalyticsService()
        {
            k_Instance = new AnalyticsService();
        }

        AnalyticsService()
        {
            const string serviceName = "Analytics";
            name = serviceName;
            title = L10n.Tr(serviceName);
            description = L10n.Tr("Discover player insights");
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Analytics.png";
            projectSettingsPath = "Project/Services/Analytics";
            displayToggle = true;
            packageName = "com.unity.analytics";
            serviceFlagName = "analytics";
            ServicesRepository.AddService(this);
        }

        public override bool IsServiceEnabled()
        {
            return AnalyticsSettings.enabled;
        }

        protected override void InternalEnableService(bool enable, bool shouldUpdateApiFlag)
        {
            if (AnalyticsSettings.enabled != enable)
            {
                AnalyticsSettings.SetEnabledServiceWindow(enable);
                EditorAnalytics.SendEventServiceInfo(new AnalyticsServiceState() { analytics = enable });
                if (!enable && PurchasingService.instance.IsServiceEnabled())
                {
                    PurchasingService.instance.EnableService(false, shouldUpdateApiFlag);
                }
            }

            base.InternalEnableService(enable, shouldUpdateApiFlag);
        }

        public void RequestValidationData(Action<AsyncOperation> onGet, string authSignature, out UnityWebRequest request)
        {
            request = UnityWebRequest.Get(String.Format(AnalyticsConfiguration.instance.validatorUrl, UnityConnect.instance.projectInfo.projectGUID));
            request.suppressErrorsToConsole = true;
            var encodedAuthToken = ServicesUtils.Base64Encode((UnityConnect.instance.projectInfo.projectGUID + ":" + authSignature));
            request.SetRequestHeader("Authorization", $"Basic {encodedAuthToken}");
            var operation = request.SendWebRequest();
            operation.completed += onGet;
        }

        //TODO: Consider moving to 'core' of services:
        public void RequestAuthSignature(Action<AsyncOperation> onGet, out UnityWebRequest request)
        {
            request = UnityWebRequest.Get(String.Format(AnalyticsConfiguration.instance.coreProjectsUrl, UnityConnect.instance.projectInfo.projectGUID));
            request.suppressErrorsToConsole = true;
            request.SetRequestHeader("AUTHORIZATION", $"Bearer {UnityConnect.instance.GetUserInfo().accessToken}");
            var operation = request.SendWebRequest();
            operation.completed += onGet;
        }
    }
}
