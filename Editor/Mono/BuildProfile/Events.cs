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
            /// Module name.
            /// </summary>
            public string moduleName;

            /// <summary>
            /// Build target of the built profile.
            /// </summary>
            public BuildTarget buildTarget;

            /// <summary>
            /// Build target string for the build profile.
            /// </summary>
            public string buildTargetString;

            /// <summary>
            /// StandaloneBuildSubtarget of the built profile.
            /// </summary>
            public StandaloneBuildSubtarget standaloneSubtarget;

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
                moduleName = profile.moduleName,
                buildTarget = profile.buildTarget,
                buildTargetString = profile.buildTarget.ToString(),
                standaloneSubtarget = profile.subtarget,
                profileAssetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile))
            }));
        }
    }
}
