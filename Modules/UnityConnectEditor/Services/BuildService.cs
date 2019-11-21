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
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.BuildService;
        public override string packageId { get; }

        static readonly BuildService k_Instance;

        public static BuildService instance => k_Instance;

        static BuildService()
        {
            k_Instance = new BuildService();
        }

        BuildService()
        {
            name = "Build";
            title = L10n.Tr("Cloud Build");
            description = L10n.Tr("Build games faster");
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Build.png";
            projectSettingsPath = "Project/Services/Cloud Build";
            displayToggle = true;
            packageId = null;
            ServicesRepository.AddService(this);
        }
    }
}
