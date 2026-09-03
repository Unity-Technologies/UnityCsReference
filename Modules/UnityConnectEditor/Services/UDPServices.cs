// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UnityConnectHub not yet converted
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal partial class UDPService : SingleService
    {
        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; } = "Project/Services/Unity Distribution Portal";
        public override string settingsProviderClassName => "AppStoreProjectSettingsEditor";
        public override bool displayToggle { get; }
        public override bool isPackage { get; }
        public override string packageName { get; }

        public override string editorGamePackageName { get; } = "com.unity.purchasing.udp";

        public override Notification.Topic notificationTopic => Notification.Topic.UDPService;
        [AutoStaticsCleanupOnCodeReload]
        static UDPService k_Instance;
        public static UDPService instance
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
                k_Instance = new UDPService();
            }
        }

        UDPService()
        {
            name = "UDP";
            title = L10n.Tr("Unity Distribution Portal", null);
            description = L10n.Tr("Distribute to multiple app stores through a single hub.", null);
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-UDP.png";
            displayToggle = false;
            isPackage = true;
            packageName = "com.unity.purchasing.udp";
            // Due to really bad user experience, this service cannot be included in the services window.
            // See fogbugz case 1215216, with this entry removed the user experience is not worst then on previous Unity Version
            ServicesRepository.AddService(this);
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
