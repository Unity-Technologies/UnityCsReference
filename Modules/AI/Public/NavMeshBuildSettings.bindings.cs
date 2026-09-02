// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.AI
{
    // Keep this struct in sync with the one defined in "NavMeshBuildSettings.h"
    ///<summary>The NavMeshBuildSettings struct allows you to specify a collection of settings which describe the dimensions and limitations of a particular agent type.</summary>
    ///<remarks>You might want to define multiple NavMeshBuildSettings if your game involves characters with large differences in height, width or climbing ability.
    ///
    ///You can also use this struct to control the precision and granularity of the build process, by setting the voxel and tile sizes. Some of the values are coupled, meaning there are constraints on the values based on other values. For example, it’s not valid for <see cref="agentClimb" /> to be larger than <see cref="agentHeight" />.
    ///To help diagnose violations of these rules, a special method <see cref="ValidationReport" /> can be evaluated.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/AI/Public/NavMeshBuildSettings.h")]
    public struct NavMeshBuildSettings
    {
        ///<summary>The agent type ID the NavMesh will be baked for.</summary>
        ///<remarks>Each <see cref="NavMeshAgent" /> or <see cref="NavMeshQueryFilter" /> can only use a NavMesh which is built for its agent type; it is the ID that is matched.</remarks>
        public int agentTypeID { get { return m_AgentTypeID; } set { m_AgentTypeID = value; } }
        ///<summary>The radius of the agent for baking in world units.</summary>
        ///<remarks>The resulting NavMesh will be shrunk by this radius to make sure that agents do not clip to walls when close to obstacles, in some scenarios it can be useful to reduce this radius.</remarks>
        public float agentRadius { get { return m_AgentRadius; } set { m_AgentRadius = value; } }
        ///<summary>The height of the agent for baking in world units.</summary>
        ///<remarks>NavMesh will be removed from areas with a ceiling  lower than this height. The build process does some quantization, so make sure that spaces you intend to be walkable have some extra head room.</remarks>
        public float agentHeight { get { return m_AgentHeight; } set { m_AgentHeight = value; } }
        ///<summary>The maximum slope angle which is walkable (angle in degrees).</summary>
        ///<remarks>The valid range is 0–60 degrees. Steep slopes will be excluded from the resulting NavMesh. Please note that setting the slope higher than 45 can give artifacts due to the voxelization process - i.e. a steep slope cannot be distinguished from a wall.</remarks>
        public float agentSlope { get { return m_AgentSlope; } set { m_AgentSlope = value; } }
        ///<summary>The maximum vertical step size an agent can take.</summary>
        ///<remarks>Must be less than agent height. This parameter is used to detect sharp discontinuities in the level (i.e. stairs, steps), and allow the agent to pass them.</remarks>
        public float agentClimb { get { return m_AgentClimb; } set { m_AgentClimb = value; } }
        ///<summary>Maximum agent drop height.</summary>
        ///<remarks>Drop-Down link generation is controlled by the Drop Height parameter. The parameter controls what is the highest drop that will be connected, setting the value to 0 will disable the generation.
        ///
        ///The trajectory of the drop-down link is defined so that the horizontal travel is: 2 x agentRadius + 4 x voxelSize. That is, the drop will land just beyond the edge of the platform. In addition the vertical travel needs to be more than bake settings’ Step Height (otherwise we could just step down) and less than Drop Height. The adjustment by voxel size is done so that any round off errors during voxelization does not prevent the links being generated. You should set the Drop Height to a bit larger value than what you measure in your level, so that the links will connect properly.</remarks>
        public float ledgeDropHeight { get { return m_LedgeDropHeight; } set { m_LedgeDropHeight = value; } }
        ///<summary>Maximum agent jump distance.</summary>
        ///<remarks>Jump-Across link generation is controlled by the Jump Distance parameter. The parameter controls what is the furthest distance that will be connected. Setting the value to 0 will disable the generation.
        ///
        ///The trajectory of the jump-across link is defined so that the horizontal travel is more than 2 x agentRadius and less than Jump Distance. In addition the landing location must not be further than voxelSize from the level of the start location.</remarks>
        public float maxJumpAcrossDistance { get { return m_MaxJumpAcrossDistance; } set { m_MaxJumpAcrossDistance = value; } }
        ///<summary>The approximate minimum area of individual NavMesh regions.</summary>
        ///<remarks>This property allows you to cull away small non-connected NavMesh regions. NavMesh regions whose surface area is smaller than the specified value, will be removed.
        ///
        ///Note: some regions may not get removed. The NavMesh is built in parallel as a grid of tiles. If a region straddles a tile boundary, the region is not removed. The reason for this is that the region pruning happens at a stage in the build process where surrounding tiles are not available.</remarks>
        public float minRegionArea { get { return m_MinRegionArea; } set { m_MinRegionArea = value; } }
        ///<summary>Enables overriding the default voxel size.</summary>
        ///<seealso cref="voxelSize" />
        public bool overrideVoxelSize { get { return m_OverrideVoxelSize != 0; } set { m_OverrideVoxelSize = value ? 1 : 0; } }
        ///<summary>Sets the voxel size in world length units.</summary>
        ///<remarks>The NavMesh is built by first voxelizing the Scene, and then figuring out walkable spaces from the voxelized representation of the Scene. The voxel size controls how closely the NavMesh fits the geometry of your Scene, and is defined in world units.
        ///
        ///If you require a more detail so that the NavMesh more closely fits your Scene’s geometry, you can reduce the voxel size. An increase in detail will also cause your game to consume more memory and take more time to calculate the NavMesh data. The scaling is roughly quadratic, so doubling the resolution will result in an approximate quadrupling of the build time.
        ///
        ///In general you should aim to have 4-6 voxels per character diameter. For example, if you have a Scene with characters that have a radius of 0.3, a good voxel size is 0.1. The default value is set to a third of the agentRadius.
        ///
        ///Note: If you want to use this setting, you must also set <see cref="overrideVoxelSize" /> to true.</remarks>
        public float voxelSize { get { return m_VoxelSize; } set { m_VoxelSize = value; } }
        ///<summary>Enables overriding the default tile size.</summary>
        ///<seealso cref="tileSize" />
        public bool overrideTileSize { get { return m_OverrideTileSize != 0; } set { m_OverrideTileSize = value ? 1 : 0; } }
        ///<summary>Sets the tile size in voxel units.</summary>
        ///<remarks>The NavMesh is built in square tiles in order to build the mesh in parallel and to control maximum memory usage. It also helps to make the carving changes more local. If you plan to update NavMesh at runtime, a good tile size is around 32–128 voxels (roughly 5 to 20 meters for human size characters). 64 is good value to start, and you can use the [profiler window](xref:Profiler) to find a good trade off. Default value is 256, which is good for static baking. If you use a lot of <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/CreateNavMeshObstacle.html">carving obstacles</see> you can try a smaller size if you see in the profiler that a lot of time is being spent on carving.
        ///
        ///The tile size is specified in units of voxels per tile side length.
        ///
        ///Note: if you want to use this setting, you must also set <see cref="overrideTileSize" /> to true.</remarks>
        public int tileSize { get { return m_TileSize; } set { m_TileSize = value; } }
        ///<summary>The maximum number of worker threads that the build process can utilize when building a NavMesh with these settings.</summary>
        ///<remarks>A value between 1 and <see cref="Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount" /> (inclusive) causes the build process to schedule all of the work within that number of jobs when building a NavMesh. Each job computes as many NavMesh tiles as it can grab, after it has finished computing the previous tiles.
        ///
        ///A value of 0 or higher than <c>JobsUtility.JobWorkerCount</c> causes the build process to use all of the available worker threads. In this case, it computes each tile in its own separate job. The build process also computes each tile in a separate job when the number of tiles that need computing is less than the number of worker threads.
        ///
        ///The default value is 0.</remarks>
        ///<seealso cref="tileSize" />
        public uint maxJobWorkers { get { return m_MaxJobWorkers; } set { m_MaxJobWorkers = value; } }
        ///<summary>Specifies whether to keep the NavMesh unchanged in the sections outside the build bounds during a NavMesh update.</summary>
        ///<remarks>With this property enabled, a NavMesh update recomputes only the NavMesh tiles that fall completely inside the specified local bounds. All other tiles, such as those fully outside the bounds or those that only partially intersect them, remain unchanged. Unity rebuilds the recomputed tiles as usual from the provided <see cref="NavMeshBuildSource" /> objects.
        ///
        ///The default value is false, which means all tiles are rebuilt during a NavMesh update regardless of whether they fall inside the bounds.
        ///
        ///This property is useful when you need to update the NavMesh in a limited volume at runtime without affecting the rest of the NavMesh. Use this property to clear tiles in a specific area. Unity removes any tile inside the bounds that has no overlapping sources.
        ///
        ///The spatial dimensions of a NavMesh tile equal <see cref="tileSize" /> multiplied by <see cref="voxelSize" />. The world position of the tile grid origin depends on the positions and rotations used to build and then to instantiate the <see cref="NavMeshData" />. The bounds passed to <see cref="NavMeshBuilder.UpdateNavMeshData" /> or <see cref="NavMeshBuilder.UpdateNavMeshDataAsync" /> are in local space relative to that origin.
        ///
        ///When this property is true, a NavMesh update has the following additional effects:
        ///
        ///- Unity doesn't create the height mesh, and if one already exists, Unity removes it.
        ///- Unity removes all automatically generated off-mesh links and doesn't regenerate them. This is because generated links cannot be modified for only one section of the NavMesh. Manually placed &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html"&gt;NavMeshLink&lt;/a&gt; components are unaffected.
        ///- The NavMesh builder carries over most build settings from the existing <see cref="NavMeshData" /> in order to preserve consistency. The exceptions are <see cref="minRegionArea" />, <see cref="maxJobWorkers" />, <see cref="debug" />, and <see cref="buildHeightMesh" />, which are taken from the settings you provide. Unity ignores changes to other settings such as <see cref="agentTypeID" /> or <see cref="agentHeight" />. The values already stored in the <see cref="NavMeshData" /> are used instead.
        ///
        ///Known issue: If you call <see cref="NavMeshBuilder.UpdateNavMeshDataAsync" /> a second time for the same <see cref="NavMeshData" /> object, the first operation is canceled. Therefore, you can't start operations with <c>preserveTilesOutsideBounds</c> in parallel to affect different parts of the NavMesh. Each call must wait for the previous operation's <see cref="AsyncOperation.isDone" /> to be true before starting.
        ///
        ///This property is available as of Unity 2020.1.</remarks>
        public bool preserveTilesOutsideBounds { get { return m_PreserveTilesOutsideBounds != 0; } set { m_PreserveTilesOutsideBounds = value ? 1 : 0; } }
        ///<summary>Enables the creation of additional data needed to determine the height at any position on the NavMesh more accurately.</summary>
        ///<remarks>The NavMesh Agent is constrained to the surface of the NavMesh as it navigates. Since the NavMesh is an approximation of the walkable space, some features are evened out when the NavMesh is built. For example, stairs may appear as a slope in the NavMesh. If you need accurate placement of the agent for your game, enable height mesh building when you build the NavMesh. Note that building the height mesh will take up memory and processing at runtime, and it increases the time needed to bake the NavMesh.
        ///
        ///The current implementation of the height mesh has the following limitations:
        ///
        ///- It can construct height data for a Terrain only when its horizontal plane is parallel to the XZ plane of the NavMesh.
        ///- During a NavMesh update, if the build setting "preserveTilesOutsideBounds" is true the height mesh will not be created and if it already exists, will be removed.
        ///
        ///This property is available as of Unity 2022.2. It will be correctly compiled in scripts when the <c>UNITY_2022_2_OR_NEWER</c> symbol is [defined by the engine](xref:platform-dependent-compilation).</remarks>
        ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@1.0/manual/NavMeshSurface.html#advanced-settings">NavMeshSurface Advanced Settings</seealso>
        public bool buildHeightMesh { get { return m_BuildHeightMesh != 0; } set { m_BuildHeightMesh = value ? 1 : 0; } }
        ///<summary>Options for collecting debug data during the build process.</summary>
        ///<seealso cref="NavMeshBuildDebugSettings" />
        ///<seealso cref="M:UnityEditor.AI.NavMeshEditorHelpers.DrawBuildDebug" />
        public NavMeshBuildDebugSettings debug { get { return m_Debug; } set { m_Debug = value; } }

        int m_AgentTypeID;
        float m_AgentRadius;
        float m_AgentHeight;
        float m_AgentSlope;
        float m_AgentClimb;
        float m_LedgeDropHeight;
        float m_MaxJumpAcrossDistance;
        float m_MinRegionArea;
        int m_OverrideVoxelSize;
        float m_VoxelSize;
        int m_OverrideTileSize;
        int m_TileSize;
        int m_BuildHeightMesh;
        uint m_MaxJobWorkers;
        int m_PreserveTilesOutsideBounds;

        NavMeshBuildDebugSettings m_Debug;

        ///<summary>Validates the properties of NavMeshBuildSettings.</summary>
        ///<remarks>Returns a string of violated constraints. - and suggestions for changes for the current values in the build settings and the provided bounds for building the NavMesh.
        ///
        ///An empty array is returned if all internal constraints are satisfied.
        ///
        ///Some of the settings which you can specify in the <see cref="NavMeshBuildSettings" /> struct are coupled to each other, meaning there are constraints on the values based on other values. For example, it’s not valid for <see cref="agentClimb" /> to be larger than <see cref="agentHeight" />. Another invalid case is when the vertical size of the buildBounds exceeds the height of 65535 voxel units.
        ///
        ///You can use this function to check if the values in <see cref="NavMeshBuildSettings" /> violate any of the constraints, before starting the NavMesh building process.
        ///
        ///Ignoring the violated constraints might give unexpected results when building a NavMesh, but will still produce a NavMesh.</remarks>
        ///<param name="buildBounds">Describes the volume to build NavMesh for.</param>
        ///<returns>The list of violated constraints.</returns>
        public String[] ValidationReport(Bounds buildBounds)
        {
            return InternalValidationReport(this, buildBounds);
        }

        [FreeFunction]
        [NativeHeader("Modules/AI/Public/NavMeshBuildSettings.h")]
        static extern String[] InternalValidationReport(NavMeshBuildSettings buildSettings, Bounds buildBounds);

        // Consider exposing a "Validate" method to modify the BuildSettings in-place
    }

    ///<summary>Specify which of the temporary data generated while building the NavMesh should be retained in memory after the process has completed.</summary>
    ///<remarks>It is possible to collect and display in the Editor the intermediate data used in the process of building the navigation mesh using the <see cref="UnityEngine.AI.NavMeshBuilder" />. This can help with diagnosing those situations when the resulting NavMesh isn’t of the expected shape.
    ///
    /// <img src="NavMeshBuildDebug.png"/>
    ///
    ///Input Geometry, Regions, Polygonal Mesh Detail and Raw Contours shown after building the NavMesh with debug options
    ///
    ///The process for computing a NavMesh comprises of several sequential steps:
    ///
    ///i. decomposing the Scene's terrain and meshes into triangles;
    ///
    ///ii. rasterizing the input triangles into a 3D voxel representation and finding ledges;
    ///
    ///iii. partitioning the voxels lying at the surface into simpler horizontal regions;
    ///
    ///iv. finding a tight-fitting contour for each of these regions;
    ///
    ///v. simplifying the contours into polygonal shapes;
    ///
    ///vi. creating a mesh of convex polygons based on all the contours combined;
    ///
    ///vii. refining the polygonal mesh into a triangulated version that approximates better the Scene's original geometry.
    ///
    ///Through the use of the debug functionality the results from each stage can be captured and displayed separately, whereas normally they would get discarded when the NavMesh construction is completed.
    ///
    ///Depending on the Scene composition this debug data can be considerably large in size. It is stored in memory in a compressed manner but gets further expanded when being displayed.
    ///
    ///**Notes: **
    ///
    ///1. Unity does not save Debug visualizations - they are only available during the session in which Unity is building the NavMesh.
    ///
    ///2. Debug data is neither displayed nor collected when the system recomputes local patches of the NavMesh due to the presence of <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/CreateNavMeshObstacle.html">NavMesh Obstacles</see>.</remarks>
    ///<seealso cref="NavMeshBuildSettings" />
    ///<seealso cref="NavMeshBuilder.BuildNavMeshData" />
    ///<seealso cref="M:UnityEditor.AI.NavMeshEditorHelpers.DrawBuildDebug" />
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/AI/Public/NavMeshBuildDebugSettings.h")]
    public struct NavMeshBuildDebugSettings
    {
        ///<summary>Specify which types of debug data to collect when building the NavMesh.</summary>
        ///<remarks>Default value is <see cref="NavMeshBuildDebugFlags.None" />.</remarks>
        ///<seealso cref="NavMeshBuildDebugFlags" />
        ///<seealso cref="NavMeshBuildSettings.debug" />
        ///<seealso cref="M:UnityEditor.AI.NavMeshEditorHelpers.DrawBuildDebug" />
        public NavMeshBuildDebugFlags flags { get { return (NavMeshBuildDebugFlags)m_Flags; } set { m_Flags = (byte)value; } }

        byte m_Flags;
    }
}
