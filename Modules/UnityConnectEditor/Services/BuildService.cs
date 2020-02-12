// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using System;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal class BuildService : SingleService
    {
        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; }
        public override string settingsProviderClassName => nameof(CloudBuildProjectSettings);
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.BuildService;
        public override string packageName { get; }
        public override string serviceFlagName { get; }
        public override bool shouldSyncOnProjectRebind => true;

        static readonly BuildService k_Instance;

        public static BuildService instance => k_Instance;

        static BuildService()
        {
            k_Instance = new BuildService();
        }

        struct BuildServiceState
        {
            public bool build;
        }

        BuildService()
        {
            name = "Build";
            title = L10n.Tr("Cloud Build");
            description = L10n.Tr("Build games faster");
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Build.png";
            projectSettingsPath = "Project/Services/Cloud Build";
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
