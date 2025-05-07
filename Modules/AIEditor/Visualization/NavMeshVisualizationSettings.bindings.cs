// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

[assembly:InternalsVisibleTo("Unity.AI.Navigation.Editor")]

namespace UnityEditor.AI
{
    [MovedFrom("UnityEditor")]
    [StaticAccessor("GetNavMeshVisualizationSettings()", StaticAccessorType.Dot)]
    public sealed class NavMeshVisualizationSettings
    {
        [Obsolete("showNavigation is no longer supported and will be removed.")]
        public static int showNavigation { get; set; }

        internal static extern bool showOnlySelectedSurfaces { get; set; }
        internal static extern Color32 heightMeshColor { get; set; }
        internal static extern float selectedSurfacesOpacity { get; set; }
        internal static extern float unselectedSurfacesOpacity { get; set; }
        internal static extern bool showNavMesh { get; set; }
        internal static extern bool showHeightMesh { get; set; }
        internal static extern bool showNavMeshPortals { get; set; }
        internal static extern bool showNavMeshLinks { get; set; }
        internal static extern bool showProximityGrid { get; set; }
        internal static extern bool showHeightMeshBVTree { get; set; }
        internal static extern bool showHeightMaps { get; set; }
        internal static extern bool showAgentPath { get; set; }
        internal static extern bool showAgentPathInfo { get; set; }
        internal static extern bool showAgentNeighbours { get; set; }
        internal static extern bool showAgentWalls { get; set; }
        internal static extern bool showAgentAvoidance { get; set; }
        internal static extern bool showObstacleCarveHull { get; set; }

        internal static extern void ResetHeightMeshColor();
        internal static extern void ResetSelectedSurfacesOpacity();
        internal static extern void ResetUnselectedSurfacesOpacity();
    }

    [NativeHeader("Modules/AIEditor/Visualization/NavMeshVisualizationSettings.bindings.h")]
    [StaticAccessor("NavMeshVisualizationSettingsScriptBindings", StaticAccessorType.DoubleColon)]
    public static partial class NavMeshEditorHelpers
    {
        [UnityEngine.Internal.ExcludeFromDocs]
        public static void DrawBuildDebug(NavMeshData navMeshData) { DrawBuildDebug(navMeshData, NavMeshBuildDebugFlags.All); }
        public static extern void DrawBuildDebug(NavMeshData navMeshData, [UnityEngine.Internal.DefaultValue(@"NavMeshBuildDebugFlags.All")] NavMeshBuildDebugFlags flags);

        [StaticAccessor("GetNavMeshManager()", StaticAccessorType.Dot)]
        internal static extern bool HasPendingAgentDebugInfoRequests();

        [StaticAccessor("GetNavMeshManager()", StaticAccessorType.Dot)]
        internal static extern void GetAgentsDebugInfoRejectedRequestsCount(out int rejected, out int allowed);

        internal static event Action<int, int> agentRejectedDebugInfoRequestsCountChanged;
        internal static event Action agentDebugRequestsPending;
        internal static event Action agentDebugRequestsProcessed;

        [RequiredByNativeCode]
        private static void OnAgentRejectedDebugInfoRequestsCountChanged(int rejected, int allowed)
        {
            if (agentRejectedDebugInfoRequestsCountChanged != null)
                agentRejectedDebugInfoRequestsCountChanged(rejected, allowed);
        }

        [RequiredByNativeCode]
        private static void OnAgentDebugInfoRequestsPending()
        {
            if (agentDebugRequestsPending != null)
                agentDebugRequestsPending();
        }

        [RequiredByNativeCode]
        private static void OnAgentDebugInfoRequestsProcessed()
        {
            if (agentDebugRequestsProcessed != null)
                agentDebugRequestsProcessed();
        }

        internal static void SetupLegacyNavigationWindow()
        {
            NavMeshEditorWindow.SetupWindow();
        }
    }
}
