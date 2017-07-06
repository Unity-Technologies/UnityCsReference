// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEditor.Connect;
using UnityEditor.Advertisements;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.Web
{
    [InitializeOnLoad]
    internal class AdsAccess : CloudServiceAccess
    {
        private const string kServiceName = "Unity Ads";
        private const string kServiceDisplayName = "Ads";
        private const string kServicePackageName = "com.unity.ads";
        private const string kServiceUrl = "https://public-cdn.cloud.unity3d.com/editor/production/cloud/ads";

        public override string GetServiceName()
        {
            return kServiceName;
        }

        public override string GetServiceDisplayName()
        {
            return kServiceDisplayName;
        }

        public override string GetPackageName()
        {
            return kServicePackageName;
        }

        override public bool IsServiceEnabled()
        {
            return AdvertisementSettings.enabled;
        }

        [Serializable]
        public struct AdsServiceState { public bool ads; }
        override public void EnableService(bool enabled)
        {
            if (AdvertisementSettings.enabled != enabled)
            {
                AdvertisementSettings.enabled = enabled;
                EditorAnalytics.SendEventServiceInfo(new AdsServiceState() { ads = enabled });
            }
        }

        override public void OnProjectUnbound()
        {
            AdvertisementSettings.enabled = false;
            AdvertisementSettings.SetGameId(RuntimePlatform.IPhonePlayer, "");
            AdvertisementSettings.SetGameId(RuntimePlatform.Android, "");
            AdvertisementSettings.testMode = false;
        }

        public bool IsInitializedOnStartup()
        {
            return AdvertisementSettings.initializeOnStartup;
        }

        public void SetInitializedOnStartup(bool enabled)
        {
            AdvertisementSettings.initializeOnStartup = enabled;
        }

        public string GetIOSGameId()
        {
            return AdvertisementSettings.GetGameId(RuntimePlatform.IPhonePlayer);
        }

        public void SetIOSGameId(string value)
        {
            AdvertisementSettings.SetGameId(RuntimePlatform.IPhonePlayer, value);
        }

        public string GetAndroidGameId()
        {
            return AdvertisementSettings.GetGameId(RuntimePlatform.Android);
        }

        public void SetAndroidGameId(string value)
        {
            AdvertisementSettings.SetGameId(RuntimePlatform.Android, value);
        }

        public string GetGameId(string platformName)
        {
            return AdvertisementSettings.GetPlatformGameId(platformName);
        }

        public void SetGameId(string platformName, string value)
        {
            AdvertisementSettings.SetPlatformGameId(platformName, value);
        }

        public bool IsTestModeEnabled()
        {
            return AdvertisementSettings.testMode;
        }

        public void SetTestModeEnabled(bool enabled)
        {
            AdvertisementSettings.testMode = enabled;
        }

        static AdsAccess()
        {
            var serviceData = new UnityConnectServiceData(kServiceName, kServiceUrl, new AdsAccess(), "unity/project/cloud/ads");
            UnityConnectServiceCollection.instance.AddService(serviceData);
        }
    }
}

