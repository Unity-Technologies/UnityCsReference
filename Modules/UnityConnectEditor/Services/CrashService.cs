// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEditor.CrashReporting;
using System;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal class CrashService : SingleService
    {
        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; }
        public override string settingsProviderClassName => nameof(CloudDiagProjectSettings);
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.CrashService;
        public override string packageName { get; }
        public override string serviceFlagName { get; }
        public override bool shouldSyncOnProjectRebind => true;

        static readonly CrashService k_Instance;

        public static CrashService instance => k_Instance;

        private struct CrashServiceState { public bool crash_reporting; }

        static CrashService()
        {
            k_Instance = new CrashService();
        }

        CrashService()
        {
            name = "Game Performance";
            title = L10n.Tr("Cloud Diagnostics");
            description = L10n.Tr("Discover app errors and collect user feedback");
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Crash.png";
            projectSettingsPath = "Project/Services/Cloud Diagnostics";
            displayToggle = false;
            packageName = null;
            serviceFlagName = "gameperf";
            ServicesRepository.AddService(this);
        }

        public override bool IsServiceEnabled()
        {
            return CrashReportingSettings.enabled;
        }

        protected override void InternalEnableService(bool enable, bool shouldUpdateApiFlag)
        {
            if (CrashReportingSettings.enabled != enable)
            {
                CrashReportingSettings.SetEnabledServiceWindow(enable);
                EditorAnalytics.SendEventServiceInfo(new CrashServiceState() { crash_reporting = enable });
            }

            base.InternalEnableService(enable, shouldUpdateApiFlag);
        }
    }
}
