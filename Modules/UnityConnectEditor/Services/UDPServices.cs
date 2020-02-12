// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal class UDPService : SingleService
    {
        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; }
        public override string settingsProviderClassName => "AppStoreProjectSettingsEditor";
        public override bool displayToggle { get; }
        public override bool isPackage { get; }
        public override string packageName { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.UDPService;
        static readonly UDPService k_Instance;
        public static UDPService instance => k_Instance;
        static UDPService()
        {
            k_Instance = new UDPService();
        }

        UDPService()
        {
            name = "UDP";
            title = L10n.Tr("Unity Distribution Portal");
            description = L10n.Tr("Distribute to multiple app stores through a single hub.");
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-UDP.png";
            projectSettingsPath = "Project/Services/Unity Distribution Portal";
            displayToggle = false;
            isPackage = true;
            packageName = "com.unity.purchasing.udp";
            // Due to really bad user experience, this service cannot be included in the services window.
            // See fogbugz case 1215216, with this entry removed the user experience is not worst then on previous Unity Version
            // ServicesRepository.AddService(this);
        }
    }
}

