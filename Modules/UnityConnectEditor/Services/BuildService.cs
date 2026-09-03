// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UnityConnectHub not yet converted
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal partial class BuildService : SingleService
    {
        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; } = "Project/Services/Build Automation";
        public override string settingsProviderClassName => nameof(CloudBuildProjectSettings);
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.BuildService;
        public override string packageName { get; }

        public override string editorGamePackageName { get; } = "com.unity.services.cloud-build";

        public override string serviceFlagName { get; }
        public override bool shouldSyncOnProjectRebind => true;

        [AutoStaticsCleanupOnCodeReload]
        static BuildService k_Instance;

        public static BuildService instance
        {
            get
            {
                EnsureInstanceInitialized();
                return k_Instance;
            }
        }

        // [OnCodeLoaded] re-registers this service with ServicesRepository after a reload;
        // the null check in `instance` covers consumers that reach us before it has run
        // (ordering across classes is not guaranteed).
        [OnCodeLoaded]
        static void EnsureInstanceInitialized()
        {
            if (k_Instance == null)
            {
                k_Instance = new BuildService();
            }
        }

        struct BuildServiceState
        {
            public bool build;
        }

        BuildService()
        {
            name = "Build";
            title = L10n.Tr("Build Automation", null);
            description = L10n.Tr("Build games faster", null);
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Build.png";
            displayToggle = true;
            packageName = null;
            serviceFlagName = "build";
            ServicesRepository.AddService(this);
        }

        protected override void InternalEnableService(bool enable, bool shouldUpdateApiFlag)
        {
            if (IsServiceEnabled() != enable)
            {
                EditorAnalytics.SendEventServiceInfo(new BuildServiceState() { build = enable });
            }
            base.InternalEnableService(enable, shouldUpdateApiFlag);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
