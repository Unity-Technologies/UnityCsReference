// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UnityConnectHub not yet converted
using Unity.Scripting.LifecycleManagement;
using UnityEditor.CrashReporting;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal partial class CrashService : SingleService
    {
        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; } = "Project/Services/Cloud Diagnostics";
        public override string settingsProviderClassName => nameof(CloudDiagProjectSettings);
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.CrashService;
        public override string packageName { get; }

        public override string editorGamePackageName { get; } = "com.unity.services.cloud-diagnostics";

        public override string serviceFlagName { get; }
        public override bool shouldSyncOnProjectRebind => true;

        [AutoStaticsCleanupOnCodeReload]
        static CrashService k_Instance;

        public static CrashService instance
        {
            get
            {
                EnsureInstanceInitialized();
                return k_Instance;
            }
        }

        private struct CrashServiceState { public bool crash_reporting; }

        // [OnCodeLoaded] re-registers this service with ServicesRepository after a reload;
        // the null check in `instance` covers consumers that reach us before it has run
        // (ordering across classes is not guaranteed).
        [OnCodeLoaded]
        static void EnsureInstanceInitialized()
        {
            if (k_Instance == null)
            {
                k_Instance = new CrashService();
            }
        }

        CrashService()
        {
            name = "Game Performance";
            title = L10n.Tr("Cloud Diagnostics", null);
            description = L10n.Tr("Discover app errors and collect user feedback", null);
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Crash.png";
            displayToggle = false;
            packageName = null;
            serviceFlagName = "gameperf";
            ServicesRepository.AddService(this);
        }

        public override bool IsServiceEnabled()
        {
            return CrashReportingSettings.canUploadReports;
        }

        protected override void InternalEnableService(bool enable, bool shouldUpdateApiFlag)
        {
            if (CrashReportingSettings.canUploadReports != enable)
            {
                CrashReportingSettings.SetEnabledServiceWindow(enable);
                EditorAnalytics.SendEventServiceInfo(new CrashServiceState() { crash_reporting = enable });
            }

            base.InternalEnableService(enable, shouldUpdateApiFlag);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
