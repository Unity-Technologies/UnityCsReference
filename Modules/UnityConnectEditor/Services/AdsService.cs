// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEditor.Advertisements;
using System;

namespace UnityEditor.Connect
{
    [InitializeOnLoad]
    internal class AdsService : SingleService
    {
        public override string name { get; }
        public override string title { get; }
        public override string description { get; }
        public override string pathTowardIcon { get; }
        public override string projectSettingsPath { get; }
        public override bool displayToggle { get; }
        public override Notification.Topic notificationTopic => Notification.Topic.AdsService;
        public override bool requiresCoppaCompliance => true;
        public override string packageId { get; }
        static readonly AdsService k_Instance;

        public static AdsService instance => k_Instance;

        static AdsService()
        {
            k_Instance = new AdsService();
        }

        AdsService()
        {
            name = "Unity Ads";
            title = L10n.Tr("Ads");
            description = L10n.Tr("Monetize your games");
            pathTowardIcon = @"Builtin Skins\Shared\Images\ServicesWindow-ServiceIcon-Ads.png";
            projectSettingsPath = "Project/Services/Ads";
            displayToggle = true;
            packageId = "com.unity.ads";
            ServicesRepository.AddService(this);
        }

        public override bool IsServiceEnabled()
        {
            return AdvertisementSettings.enabled;
        }

        private struct AdsServiceState
        {
            public bool ads;
        }

        protected override void InternalEnableService(bool enabled)
        {
            if (AdvertisementSettings.enabled != enabled)
            {
                AdvertisementSettings.SetEnabledServiceWindow(enabled);
                EditorAnalytics.SendEventServiceInfo(new AdsServiceState() { ads = enabled });
            }

            base.InternalEnableService(enabled);
        }
    }
}
