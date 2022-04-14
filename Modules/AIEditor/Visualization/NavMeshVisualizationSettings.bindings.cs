// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.AI;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Bindings;

namespace UnityEditor.AI
{
    [MovedFrom("UnityEditor")]
    [StaticAccessor("GetNavMeshVisualizationSettings()", StaticAccessorType.Dot)]
    public sealed class NavMeshVisualizationSettings
    {
        public static extern int showNavigation { get; set; }
        internal static extern bool showNavMesh { get; set; }
        internal static extern bool showHeightMesh { get; set; }
        internal static extern bool showNavMeshPortals { get; set; }
        internal static extern bool showNavMeshLinks { get; set; }
        internal static extern bool showProximityGrid { get; set; }
        internal static extern bool showHeightMeshBVTree { get; set; }
        internal static extern bool showHeightMaps { get; set; }
        internal static extern bool hasHeightMesh
        {
            [NativeName("HasHeightMesh")]
            get;
        }
        internal static extern bool showAgentPath { get; set; }
        internal static extern bool showAgentPathInfo { get; set; }
        internal static extern bool showAgentNeighbours { get; set; }
        internal static extern bool showAgentWalls { get; set; }
        internal static extern bool showAgentAvoidance { get; set; }
        internal static extern bool showObstacleCarveHull { get; set; }
        internal static extern bool hasPendingAgentDebugInfo
        {
            [StaticAccessor("GetNavMeshManager()", StaticAccessorType.Dot)]
            [NativeName("HasPendingAgentDebugInfo")]
            get;
        }
    }

    [NativeHeader("Modules/AIEditor/Visualization/NavMeshVisualizationSettings.bindings.h")]
    [StaticAccessor("NavMeshVisualizationSettingsScriptBindings", StaticAccessorType.DoubleColon)]
    public static partial class NavMeshEditorHelpers
    {
        [UnityEngine.Internal.ExcludeFromDocs]
        public static void DrawBuildDebug(NavMeshData navMeshData) { DrawBuildDebug(navMeshData, NavMeshBuildDebugFlags.All); }
        public static extern void DrawBuildDebug(NavMeshData navMeshData, [UnityEngine.Internal.DefaultValue(@"NavMeshBuildDebugFlags.All")] NavMeshBuildDebugFlags flags);
    }
}
