// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// UI Event when attempting through create build profiles through
    /// the platform browser. This event is sent when the platform browser closes.
    /// </summary>
    [AnalyticInfo(eventName: "buildProfilePlatformBrowserClosed", vendorKey: "unity.buildprofile")]
    internal class BuildProfilePlatformBrowserClosed : IAnalytic
    {
        [Serializable]
        internal struct Payload : IAnalytic.IData
        {
            /// <summary>
            /// Flag set if a profile was created during the session.
            /// </summary>
            public bool wasProfileCreated;

            /// <summary>
            /// Module name of the created profile.
            /// </summary>
            public string moduleName;

            /// <summary>
            /// Build target of the created profile.
            /// </summary>
            public BuildTarget buildTarget;

            /// <summary>
            /// Build target string for the build profile.
            /// </summary>
            public string buildTargetString;

            /// <summary>
            /// Standalone subtarget of the created profile.
            /// </summary>
            public StandaloneBuildSubtarget standaloneSubtarget;
        }

        Payload m_Payload;

        public BuildProfilePlatformBrowserClosed()
        {
            m_Payload = new Payload()
            {
                buildTarget = BuildTarget.NoTarget,
                buildTargetString = string.Empty,
                standaloneSubtarget = StandaloneBuildSubtarget.Default,
                wasProfileCreated = false
            };
        }

        public BuildProfilePlatformBrowserClosed(Payload payload)
        {
            this.m_Payload = payload;
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_Payload;
            return data != null;
        }
    }

    /// <summary>
    /// UI Event when opening the build profile window. Captures generic
    /// platform usage information.
    /// </summary>
    [AnalyticInfo(eventName: "buildProfileWorkflowReport", vendorKey: "unity.buildprofile")]
    internal class BuildProfileWorkflowReport : IAnalytic
    {
        [Serializable]
        internal struct Payload : IAnalytic.IData
        {
            /// <summary>
            /// Build target of the built profile.
            /// </summary>
            public BuildTarget buildTarget;

            /// <summary>
            /// Build target as string.
            /// </summary>
            public string buildTargetString;

            /// <summary>
            /// Standalone subtarget of the built profile.
            /// </summary>
            public StandaloneBuildSubtarget standaloneSubtarget;

            /// <summary>
            /// Count of profiles in project matching the current build target.
            /// </summary>
            public int count;
        }

        Payload m_Payload;

        public BuildProfileWorkflowReport()
        {
            this.m_Payload = new Payload()
            {
                count = 0,
                buildTarget = BuildTarget.NoTarget,
                buildTargetString = string.Empty,
                standaloneSubtarget = StandaloneBuildSubtarget.Default
            };
        }

        public BuildProfileWorkflowReport(Payload payload)
        {
            this.m_Payload = payload;
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_Payload;
            return data != null;
        }

        public void Increment()
        {
            m_Payload.count++;
        }
    }

    /// <summary>
    /// UI Event for when a Build Profile asset is created.
    /// </summary>
    [AnalyticInfo(eventName: "buildProfileCreated", vendorKey: "unity.buildprofile")]
    internal class BuildProfileCreatedEvent : IAnalytic
    {
        internal enum CreationType
        {
            PlatformBrowser,
            DuplicateClassic,
            DuplicateProfile
        }

        [Serializable]
        internal struct Payload : IAnalytic.IData
        {
            /// <summary>
            /// Module name of the created profile.
            /// </summary>
            public string moduleName;

            /// <summary>
            /// Build target of the created profile.
            /// </summary>
            public BuildTarget buildTarget;

            /// <summary>
            /// Build target as string.
            /// </summary>
            public string buildTargetString;

            /// <summary>
            /// Standalone subtarget of the created profile.
            /// </summary>
            public StandaloneBuildSubtarget standaloneSubtarget;

            /// <summary>
            /// Source of build profile creation.
            /// </summary>
            public CreationType creationType;
        }

        Payload m_Payload;

        public BuildProfileCreatedEvent(Payload payload)
        {
            this.m_Payload = payload;
        }

        public bool TryGatherData(out IAnalytic.IData data, out Exception error)
        {
            error = null;
            data = m_Payload;
            return data != null;
        }
    }
}
