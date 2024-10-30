// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Event for when building an active build profile.
    /// </summary>
    [AnalyticInfo(eventName: "buildProfileBuildTime", vendorKey: "unity.buildprofile")]
    internal class BuildProfileBuildTimeEvent : IAnalytic
    {
        [Serializable]
        internal struct Payload : IAnalytic.IData
        {
            /// <summary>
            /// Platform ID of the target build profile.
            /// </summary>
            public GUID platformId;

            /// <summary>
            /// Platform display name of the target build profile.
            /// </summary>
            public string platformDisplayName;

            /// <summary>
            /// Unique identifier of the profile asset. Not available
            /// for classic platforms.
            /// </summary>
            public string profileAssetGUID;
        }

        Payload m_Payload;

        public BuildProfileBuildTimeEvent(Payload payload)
        {
            this.m_Payload = payload;
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_Payload;
            return data != null;
        }

        [RequiredByNativeCode]
        public static void SendBuildProfile()
        {
            if (!EditorUserBuildSettings.isBuildProfileAvailable)
                return;

            var profile = BuildProfile.GetActiveBuildProfile();
            if (profile == null)
                return;

            EditorAnalytics.SendAnalytic(new BuildProfileBuildTimeEvent(new BuildProfileBuildTimeEvent.Payload
            {
                platformId = profile.platformGuid,
                platformDisplayName = BuildProfileModuleUtil.GetClassicPlatformDisplayName(profile.platformGuid),
                profileAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile))
            }));
        }
    }
}
