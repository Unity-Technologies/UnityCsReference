// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Connect;

namespace UnityEditor.Advertisements
{
    public static class AdvertisementSettings
    {
        public static bool enabled
        {
            get
            {
                return UnityAdsSettings.enabled;
            }
            set
            {
                UnityAdsSettings.enabled = value;
            }
        }

        public static bool IsPlatformEnabled(RuntimePlatform platform)
        {
            return UnityAdsSettings.IsPlatformEnabled(platform);
        }

        public static void SetPlatformEnabled(RuntimePlatform platform, bool value)
        {
            UnityAdsSettings.SetPlatformEnabled(platform, value);
        }

        public static bool initializeOnStartup
        {
            get
            {
                return UnityAdsSettings.initializeOnStartup;
            }
            set
            {
                UnityAdsSettings.initializeOnStartup = value;
            }
        }

        public static bool testMode
        {
            get
            {
                return UnityAdsSettings.testMode;
            }
            set
            {
                UnityAdsSettings.testMode = value;
            }
        }

        public static string GetGameId(RuntimePlatform platform)
        {
            return UnityAdsSettings.GetGameId(platform);
        }

        public static void SetGameId(RuntimePlatform platform, string gameId)
        {
            UnityAdsSettings.SetGameId(platform, gameId);
        }
    }
}
