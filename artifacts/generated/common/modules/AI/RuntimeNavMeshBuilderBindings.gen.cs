// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Collections.Generic;

namespace UnityEngine.AI
{


[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct NavMeshBuildSettings
{
            public int agentTypeID { get { return m_AgentTypeID; } set { m_AgentTypeID = value; } }
            public float agentRadius { get { return m_AgentRadius; } set { m_AgentRadius = value; } }
            public float agentHeight { get { return m_AgentHeight; } set { m_AgentHeight = value; } }
            public float agentSlope { get { return m_AgentSlope; } set { m_AgentSlope = value; } }
            public float agentClimb { get { return m_AgentClimb; } set { m_AgentClimb = value; } }
            public bool overrideVoxelSize { get { return m_OverrideVoxelSize != 0; } set { m_OverrideVoxelSize = value ? 1 : 0; } }
            public float voxelSize { get { return m_VoxelSize; } set { m_VoxelSize = value; } }
            public bool overrideTileSize { get { return m_OverrideTileSize != 0; } set { m_OverrideTileSize = value ? 1 : 0; } }
            public int tileSize { get { return m_TileSize; } set { m_TileSize = value; } }
    
            private int m_AgentTypeID;
            private float m_AgentRadius;
            private float m_AgentHeight;
            private float m_AgentSlope;
            private float m_AgentClimb;
            private float m_LedgeDropHeight;    
            private float m_MaxJumpAcrossDistance; 
            private float m_MinRegionArea;      
            private int m_OverrideVoxelSize;
            private float m_VoxelSize;
            private int m_OverrideTileSize;
            private int m_TileSize;
            private int m_AccuratePlacement;    
    
    
    
    public String[] ValidationReport(Bounds buildBounds)
        {
            return InternalValidationReport(this, buildBounds);
        }
    
    
    private static String[] InternalValidationReport (NavMeshBuildSettings buildSettings, Bounds buildBounds) {
        return INTERNAL_CALL_InternalValidationReport ( ref buildSettings, ref buildBounds );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static String[] INTERNAL_CALL_InternalValidationReport (ref NavMeshBuildSettings buildSettings, ref Bounds buildBounds);
}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
internal partial struct NavMeshBuildDebugSettings
{
            public bool showInputGeom { get { return m_ShowInputGeom != 0; } set { m_ShowInputGeom = value ? 1 : 0; } }
            public bool showVoxels { get { return m_ShowVoxels != 0; } set { m_ShowVoxels = value ? 1 : 0; } }
            public bool showRegions { get { return m_ShowRegions != 0; } set { m_ShowRegions = value ? 1 : 0; } }
            public bool showRawContours { get { return m_ShowRawContours != 0; } set { m_ShowRawContours = value ? 1 : 0; } }
            public bool showContours { get { return m_ShowContours != 0; } set { m_ShowContours = value ? 1 : 0; } }
            public bool showPolyMesh { get { return m_ShowPolyMesh != 0; } set { m_ShowPolyMesh = value ? 1 : 0; } }
            public bool showPolyMeshDetail { get { return m_ShowPolyMeshDetail != 0; } set { m_ShowPolyMeshDetail = value ? 1 : 0; } }
            public bool useFocus { get { return m_UseFocus != 0; } set { m_UseFocus = value ? 1 : 0; } }
            public Vector3 focusPoint { get { return m_FocusPoint; } set { m_FocusPoint = value; } }
    
            private int m_ShowInputGeom;
            private int m_ShowVoxels;
            private int m_ShowRegions;
            private int m_ShowRawContours;
            private int m_ShowContours;
            private int m_ShowPolyMesh;
            private int m_ShowPolyMeshDetail;
            private int m_UseFocus;
            private Vector3 m_FocusPoint;
}

public enum NavMeshBuildSourceShape
{
    Mesh = 0,
    Terrain = 1,
    Box = 2,
    Sphere = 3,
    Capsule = 4,
    ModifierBox = 5
}

public enum NavMeshCollectGeometry
{
    RenderMeshes = 0,
    PhysicsColliders = 1
}

[UsedByNativeCode]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct NavMeshBuildSource
{
            public Matrix4x4 transform { get { return m_Transform; } set { m_Transform = value; } }
            public Vector3 size { get { return m_Size; } set { m_Size = value; } }
            public NavMeshBuildSourceShape shape { get { return m_Shape; } set { m_Shape = value; } }
            public int area { get { return m_Area; } set { m_Area = value; } }
            public Object sourceObject { get { return InternalGetObject(m_InstanceID); } set { m_InstanceID = value != null ? value.GetInstanceID() : 0; } }
            public Component component { get { return InternalGetComponent(m_ComponentID); } set { m_ComponentID = value != null ? value.GetInstanceID() : 0; } }
    
            private Matrix4x4 m_Transform;
            private Vector3 m_Size;
            private NavMeshBuildSourceShape m_Shape;
            private int m_Area;
            private int m_InstanceID;
            private int m_ComponentID;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private Component InternalGetComponent (int instanceID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private Object InternalGetObject (int instanceID) ;

}

[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct NavMeshBuildMarkup
{
            public bool overrideArea { get { return m_OverrideArea != 0; } set { m_OverrideArea = value ? 1 : 0; } }
            public int area { get { return m_Area; } set { m_Area = value; } }
            public bool ignoreFromBuild { get { return m_IgnoreFromBuild != 0; } set { m_IgnoreFromBuild = value ? 1 : 0; } }
            public Transform root { get { return InternalGetRootGO(m_InstanceID); } set { m_InstanceID = value != null ? value.GetInstanceID() : 0; } }
    
            private int m_OverrideArea;
            private int m_Area;
            private int m_IgnoreFromBuild;
            private int m_InstanceID;
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private Transform InternalGetRootGO (int instanceID) ;

}

public static partial class NavMeshBuilder
{
    public static void CollectSources(Bounds includedWorldBounds, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea, List<NavMeshBuildMarkup> markups, List<NavMeshBuildSource> results)
        {
            if (markups == null) throw new ArgumentNullException("markups");
            if (results == null) throw new ArgumentNullException("results");

            includedWorldBounds.extents = Vector3.Max(includedWorldBounds.extents, 0.001f * Vector3.one);
            NavMeshBuildSource[] resultsArray = CollectSourcesInternal(includedLayerMask, includedWorldBounds, null, true, geometry, defaultArea, markups.ToArray());
            results.Clear();
            results.AddRange(resultsArray);
        }
    
    
    public static void CollectSources(Transform root, int includedLayerMask, NavMeshCollectGeometry geometry, int defaultArea, List<NavMeshBuildMarkup> markups, List<NavMeshBuildSource> results)
        {
            if (markups == null) throw new ArgumentNullException("markups");
            if (results == null) throw new ArgumentNullException("results");

            var empty = new Bounds();
            NavMeshBuildSource[] resultsArray = CollectSourcesInternal(includedLayerMask, empty, root, false, geometry, defaultArea, markups.ToArray());
            results.Clear();
            results.AddRange(resultsArray);
        }
    
    
    private static NavMeshBuildSource[] CollectSourcesInternal (int includedLayerMask, Bounds includedWorldBounds, Transform root, bool useBounds, NavMeshCollectGeometry geometry, int defaultArea, NavMeshBuildMarkup[] markups) {
        return INTERNAL_CALL_CollectSourcesInternal ( includedLayerMask, ref includedWorldBounds, root, useBounds, geometry, defaultArea, markups );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static NavMeshBuildSource[] INTERNAL_CALL_CollectSourcesInternal (int includedLayerMask, ref Bounds includedWorldBounds, Transform root, bool useBounds, NavMeshCollectGeometry geometry, int defaultArea, NavMeshBuildMarkup[] markups);
    public static NavMeshData BuildNavMeshData(NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources, Bounds localBounds, Vector3 position, Quaternion rotation)
        {
            if (sources == null) throw new ArgumentNullException("sources");

            var debug = new NavMeshBuildDebugSettings();
            return BuildNavMeshData(buildSettings, sources, localBounds, position, rotation, debug);
        }
    
    
    private static NavMeshData BuildNavMeshData(NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources, Bounds localBounds, Vector3 position, Quaternion rotation, NavMeshBuildDebugSettings debug)
        {
            if (sources == null) throw new ArgumentNullException("sources");

            var data = new NavMeshData(buildSettings.agentTypeID);
            data.position = position;
            data.rotation = rotation;
            UpdateNavMeshDataListInternal(data, buildSettings, sources, localBounds, debug);
            return data;
        }
    
    
    public static bool UpdateNavMeshData(NavMeshData data, NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources, Bounds localBounds)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (sources == null) throw new ArgumentNullException("sources");

            var debug = new NavMeshBuildDebugSettings();
            return UpdateNavMeshData(data, buildSettings, sources, localBounds, debug);
        }
    
    
    private static bool UpdateNavMeshData(NavMeshData data, NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources, Bounds localBounds, NavMeshBuildDebugSettings debug)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (sources == null) throw new ArgumentNullException("sources");

            return UpdateNavMeshDataListInternal(data, buildSettings, sources, localBounds, debug);
        }
    
    
    private static bool UpdateNavMeshDataListInternal (NavMeshData data, NavMeshBuildSettings buildSettings, object sources, Bounds localBounds, NavMeshBuildDebugSettings debug) {
        return INTERNAL_CALL_UpdateNavMeshDataListInternal ( data, ref buildSettings, sources, ref localBounds, ref debug );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_UpdateNavMeshDataListInternal (NavMeshData data, ref NavMeshBuildSettings buildSettings, object sources, ref Bounds localBounds, ref NavMeshBuildDebugSettings debug);
    public static AsyncOperation UpdateNavMeshDataAsync(NavMeshData data, NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources, Bounds localBounds)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (sources == null) throw new ArgumentNullException("sources");

            var debug = new NavMeshBuildDebugSettings();
            return UpdateNavMeshDataAsync(data, buildSettings, sources, localBounds, debug);
        }
    
    
    private static AsyncOperation UpdateNavMeshDataAsync(NavMeshData data, NavMeshBuildSettings buildSettings, List<NavMeshBuildSource> sources, Bounds localBounds, NavMeshBuildDebugSettings debug)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (sources == null) throw new ArgumentNullException("sources");

            return UpdateNavMeshDataAsyncListInternal(data, buildSettings, sources, localBounds, debug);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Cancel (NavMeshData data) ;

    private static AsyncOperation UpdateNavMeshDataAsyncListInternal (NavMeshData data, NavMeshBuildSettings buildSettings, object sources, Bounds localBounds, NavMeshBuildDebugSettings debug) {
        return INTERNAL_CALL_UpdateNavMeshDataAsyncListInternal ( data, ref buildSettings, sources, ref localBounds, ref debug );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static AsyncOperation INTERNAL_CALL_UpdateNavMeshDataAsyncListInternal (NavMeshData data, ref NavMeshBuildSettings buildSettings, object sources, ref Bounds localBounds, ref NavMeshBuildDebugSettings debug);
}

}
