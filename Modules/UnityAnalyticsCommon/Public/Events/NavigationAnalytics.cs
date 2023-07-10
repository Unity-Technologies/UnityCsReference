// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;


namespace UnityEditor.Analytics
{
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class NavmeshBakingAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public NavmeshBakingAnalytic() : base("navigation_navmesh_baking", 1) { }

        [UsedByNativeCode]
        public static NavmeshBakingAnalytic CreateNavmeshBakingAnalytic() { return new NavmeshBakingAnalytic(); }

        bool new_nav_api;
        bool bake_at_runtime;
        int height_meshes_count;
        int offmesh_links_count;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class ProjectSettingsInformationAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public ProjectSettingsInformationAnalytic() : base("navigation_project_settings_info", 1) { }

        [UsedByNativeCode]
        public static ProjectSettingsInformationAnalytic CreateProjectSettingsInformationAnalytic() { return new ProjectSettingsInformationAnalytic(); }

        int agent_types_count;
        int areas_count;
    }

    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class SendGameBuildAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public SendGameBuildAnalytic() : base("navigation_gamebuild_info", 1, UnityEngine.Analytics.SendEventOptions.kAppendBuildGuid) { }

        [UsedByNativeCode]
        public static SendGameBuildAnalytic CreateSendGameBuildAnalytic() { return new SendGameBuildAnalytic(); }

        int navmesh_count;
    }
    
}
