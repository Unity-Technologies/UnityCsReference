// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UnityConnectHub not yet converted
using System;
using UnityEditor.Analytics;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal partial class AnalyticsService : SingleService
    {
        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; } = "Project/Services/Analytics";
        public override string settingsProviderClassName => nameof(AnalyticsProjectSettings);
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.AnalyticsService;
        public override string packageName { get; }

        public override string editorGamePackageName { get; } = "com.unity.services.analytics";

        public override string serviceFlagName { get; }
        public override bool shouldEnableOnProjectCreation => false;
        public override bool shouldSyncOnProjectRebind => true;

        [AutoStaticsCleanupOnCodeReload]
        static AnalyticsService k_Instance;

        public static AnalyticsService instance
        {
            get
            {
                EnsureInstanceInitialized();
                return k_Instance;
            }
        }

        private struct AnalyticsServiceState { public bool analytics; }

        // [OnCodeLoaded] re-registers this service with ServicesRepository after a reload;
        // the null check in `instance` covers consumers that reach us before it has run
        // (ordering across classes is not guaranteed).
        [OnCodeLoaded]
        static void EnsureInstanceInitialized()
        {
            if (k_Instance == null)
            {
                k_Instance = new AnalyticsService();
            }
        }

        AnalyticsService()
        {
            const string serviceName = "Legacy Analytics";
            name = serviceName;
            title = L10n.Tr(serviceName, null);
            description = L10n.Tr("Discover player insights", null);
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Analytics.png";
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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
