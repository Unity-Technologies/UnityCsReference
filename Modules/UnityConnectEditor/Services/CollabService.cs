// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEditor.Collaboration;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal class CollabService : SingleService
    {
        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; }
        public override string settingsProviderClassName => nameof(CollabProjectSettings);
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.CollabService;
        public override string packageName { get; }
        public override string serviceFlagName { get; }
        public override bool shouldSyncOnProjectRebind => true;

        static readonly CollabService k_Instance;

        public static CollabService instance => k_Instance;

        struct CollabServiceState
        {
            public bool collaborate;
        }

        static CollabService()
        {
            k_Instance = new CollabService();
        }

        protected override void InternalEnableService(bool enable, bool shouldUpdateApiFlag)
        {
            if (IsServiceEnabled() != enable)
            {
                EditorAnalytics.SendEventServiceInfo(new CollabServiceState() { collaborate = enable });
            }

            base.InternalEnableService(enable, shouldUpdateApiFlag);

            Collab.instance.SetCollabEnabledForCurrentProject(enable);

            AssetDatabase.Refresh();                    // If auto-refresh was off, make sure we refresh when setting it back on
        }

        CollabService()
        {
            name = "Collab";
            title = L10n.Tr("Collaborate");
            description = L10n.Tr("Create together seamlessly");
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Collab.png";
            projectSettingsPath = "Project/Services/Collaborate";
            displayToggle = true;
            packageName = "com.unity.collab-proxy";
            serviceFlagName = "collab";
            ServicesRepository.AddService(this);
        }
    }
}
