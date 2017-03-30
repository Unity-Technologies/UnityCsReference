// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm=System.ComponentModel;
using uei=UnityEngine.Internal;
using RequiredByNativeCodeAttribute=UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute=UnityEngine.Scripting.UsedByNativeCodeAttribute;


using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{


[MovedFrom("UnityEngine")]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct NavMeshHit
{
    private Vector3 m_Position;
    private Vector3 m_Normal;
    private float   m_Distance;
    private int     m_Mask;
    private int     m_Hit;
    
    
    public Vector3 position { get { return m_Position; } set { m_Position = value; } }
    
    
    public Vector3 normal   { get { return m_Normal; } set { m_Normal = value; } }
    
    
    public float   distance { get { return m_Distance; } set { m_Distance = value; } }
    
    
    public int     mask     { get { return m_Mask; } set { m_Mask = value; } }
    
    
    public bool    hit      { get { return m_Hit != 0; } set { m_Hit = value ? 1 : 0; } }
}

[UsedByNativeCode]
[MovedFrom("UnityEngine")]
[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct NavMeshTriangulation
{
    public Vector3[] vertices;
    public int[] indices;
    public int[] areas;
    
    
    [System.Obsolete ("Use areas instead.")]
    public int[] layers { get { return areas; } }
}

public sealed partial class NavMeshData : Object
{
    public NavMeshData()
        {
            Internal_Create(this, 0);
        }
    
    
    public NavMeshData(int agentTypeID)
        {
            Internal_Create(this, agentTypeID);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private static  void Internal_Create ([Writable] NavMeshData mono, int agentTypeID) ;

    public Bounds sourceBounds
    {
        get { Bounds tmp; INTERNAL_get_sourceBounds(out tmp); return tmp;  }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private void INTERNAL_get_sourceBounds (out Bounds value) ;


    public  Vector3 position
    {
        get { Vector3 tmp; INTERNAL_get_position(out tmp); return tmp;  }
        set { INTERNAL_set_position(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_position (out Vector3 value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_position (ref Vector3 value) ;

    public  Quaternion rotation
    {
        get { Quaternion tmp; INTERNAL_get_rotation(out tmp); return tmp;  }
        set { INTERNAL_set_rotation(ref value); }
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_get_rotation (out Quaternion value) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern private  void INTERNAL_set_rotation (ref Quaternion value) ;

}

public struct NavMeshDataInstance
    {
        private int m_Handle;
        public bool valid { get { return m_Handle != 0 && NavMesh.IsValidNavMeshDataHandle(m_Handle); } }
        internal int id { get { return m_Handle; } set { m_Handle = value; } }

        public void Remove()
        {
            NavMesh.RemoveNavMeshDataInternal(id);
        }

        public Object owner
        {
            get
            {
                return NavMesh.InternalGetOwner(id);
            }
            set
            {
                var ownerID = value != null ? value.GetInstanceID() : 0;
                if (!NavMesh.InternalSetOwner(id, ownerID))
                    Debug.LogError("Cannot set 'owner' on an invalid NavMeshDataInstance");
            }
        }
    }


[System.Runtime.InteropServices.StructLayout (System.Runtime.InteropServices.LayoutKind.Sequential)]
public partial struct NavMeshLinkData
{
    private Vector3 m_StartPosition;
    private Vector3 m_EndPosition;
    private float m_CostModifier;
    private int m_Bidirectional;
    private float m_Width;
    private int m_Area;
    private int m_AgentTypeID;
    
    
    public Vector3 startPosition { get { return m_StartPosition; } set { m_StartPosition = value; } }
    public Vector3 endPosition { get { return m_EndPosition; } set { m_EndPosition = value; } }
    public float costModifier { get { return m_CostModifier; } set { m_CostModifier = value; } }
    public bool bidirectional { get { return m_Bidirectional != 0; } set { m_Bidirectional = value ? 1 : 0; } }
    public float width { get { return m_Width; } set { m_Width = value; } }
    public int area { get { return m_Area; } set { m_Area = value; } }
    public int agentTypeID { get { return m_AgentTypeID; } set { m_AgentTypeID = value; } }
}

public struct NavMeshLinkInstance
    {
        private int m_Handle;
        public bool valid { get { return m_Handle != 0 && NavMesh.IsValidLinkHandle(m_Handle); } }
        internal int id { get { return m_Handle; } set { m_Handle = value; } }

        public void Remove()
        {
            NavMesh.RemoveLinkInternal(id);
        }

        public Object owner
        {
            get
            {
                return NavMesh.InternalGetLinkOwner(id);
            }
            set
            {
                var ownerID = value != null ? value.GetInstanceID() : 0;
                if (!NavMesh.InternalSetLinkOwner(id, ownerID))
                    Debug.LogError("Cannot set 'owner' on an invalid NavMeshLinkInstance");
            }
        }
    }


public struct NavMeshQueryFilter
    {
        private const int AREA_COST_ELEMENT_COUNT = 32;
        private int m_AreaMask;
        private int m_AgentTypeID;
        private float[] m_AreaCost;
        internal float[] costs { get { return m_AreaCost; } }

        public int areaMask { get { return m_AreaMask; } set { m_AreaMask = value; } }
        public int agentTypeID { get { return m_AgentTypeID; } set { m_AgentTypeID = value; } }

        public float GetAreaCost(int areaIndex)
        {
            if (m_AreaCost == null)
            {
                if (areaIndex < 0 || areaIndex >= AREA_COST_ELEMENT_COUNT)
                {
                    var msg = string.Format("The valid range is [0:{0}]", AREA_COST_ELEMENT_COUNT - 1);
                    throw new IndexOutOfRangeException(msg);
                }
                return 1.0f;
            }
            return m_AreaCost[areaIndex];
        }

        public void SetAreaCost(int areaIndex, float cost)
        {
            if (m_AreaCost == null)
            {
                m_AreaCost = new float[AREA_COST_ELEMENT_COUNT];
                for (int j = 0; j < AREA_COST_ELEMENT_COUNT; ++j)
                    m_AreaCost[j] = 1.0f;
            }
            m_AreaCost[areaIndex] = cost;
        }

    }


[MovedFrom("UnityEngine")]
public static partial class NavMesh
{
    public const int AllAreas = ~0;
    
    
    public static bool Raycast (Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, int areaMask) {
        return INTERNAL_CALL_Raycast ( ref sourcePosition, ref targetPosition, out hit, areaMask );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_Raycast (ref Vector3 sourcePosition, ref Vector3 targetPosition, out NavMeshHit hit, int areaMask);
    public static bool CalculatePath(Vector3 sourcePosition, Vector3 targetPosition, int areaMask, NavMeshPath path)
        {
            path.ClearCorners();
            return CalculatePathInternal(sourcePosition, targetPosition, areaMask, path);
        }
    
    
    internal static bool CalculatePathInternal (Vector3 sourcePosition, Vector3 targetPosition, int areaMask, NavMeshPath path) {
        return INTERNAL_CALL_CalculatePathInternal ( ref sourcePosition, ref targetPosition, areaMask, path );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CalculatePathInternal (ref Vector3 sourcePosition, ref Vector3 targetPosition, int areaMask, NavMeshPath path);
    public static bool FindClosestEdge (Vector3 sourcePosition, out NavMeshHit hit, int areaMask) {
        return INTERNAL_CALL_FindClosestEdge ( ref sourcePosition, out hit, areaMask );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_FindClosestEdge (ref Vector3 sourcePosition, out NavMeshHit hit, int areaMask);
    public static bool SamplePosition (Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, int areaMask) {
        return INTERNAL_CALL_SamplePosition ( ref sourcePosition, out hit, maxDistance, areaMask );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_SamplePosition (ref Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, int areaMask);
    [System.Obsolete ("Use SetAreaCost instead.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetLayerCost (int layer, float cost) ;

    [System.Obsolete ("Use GetAreaCost instead.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GetLayerCost (int layer) ;

    [System.Obsolete ("Use GetAreaFromName instead.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetNavMeshLayerFromName (string layerName) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void SetAreaCost (int areaIndex, float cost) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  float GetAreaCost (int areaIndex) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetAreaFromName (string areaName) ;

    public static NavMeshTriangulation CalculateTriangulation()
        {
            NavMeshTriangulation tri = (NavMeshTriangulation)TriangulateInternal();
            return tri;
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  object TriangulateInternal () ;

    [System.Obsolete ("use NavMesh.CalculateTriangulation () instead.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void Triangulate (out Vector3[] vertices, out int[] indices) ;

    [System.Obsolete ("AddOffMeshLinks has no effect and is deprecated.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void AddOffMeshLinks () ;

    [System.Obsolete ("RestoreNavMesh has no effect and is deprecated.")]
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RestoreNavMesh () ;

    public static float avoidancePredictionTime { get { return GetAvoidancePredictionTime(); } set { SetAvoidancePredictionTime(value); } }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetAvoidancePredictionTime (float t) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  float GetAvoidancePredictionTime () ;

    public static int pathfindingIterationsPerFrame { get { return GetPathfindingIterationsPerFrame(); } set { SetPathfindingIterationsPerFrame(value); } }
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void SetPathfindingIterationsPerFrame (int iter) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int GetPathfindingIterationsPerFrame () ;

    public static NavMeshDataInstance AddNavMeshData(NavMeshData navMeshData)
        {
            if (navMeshData == null) throw new ArgumentNullException("navMeshData");

            var handle = new NavMeshDataInstance();
            handle.id = AddNavMeshDataInternal(navMeshData);
            return handle;
        }
    
    
    public static NavMeshDataInstance AddNavMeshData(NavMeshData navMeshData, Vector3 position, Quaternion rotation)
        {
            if (navMeshData == null) throw new ArgumentNullException("navMeshData");

            var handle = new NavMeshDataInstance();
            handle.id = AddNavMeshDataTransformedInternal(navMeshData, position, rotation);
            return handle;
        }
    
    
    public static void RemoveNavMeshData(NavMeshDataInstance handle)
        {
            RemoveNavMeshDataInternal(handle.id);
        }
    
    
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsValidNavMeshDataHandle (int handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool IsValidLinkHandle (int handle) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Object InternalGetOwner (int dataID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool InternalSetOwner (int dataID, int ownerID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  Object InternalGetLinkOwner (int linkID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  bool InternalSetLinkOwner (int linkID, int ownerID) ;

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  int AddNavMeshDataInternal (NavMeshData navMeshData) ;

    internal static int AddNavMeshDataTransformedInternal (NavMeshData navMeshData, Vector3 position, Quaternion rotation) {
        return INTERNAL_CALL_AddNavMeshDataTransformedInternal ( navMeshData, ref position, ref rotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_AddNavMeshDataTransformedInternal (NavMeshData navMeshData, ref Vector3 position, ref Quaternion rotation);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void RemoveNavMeshDataInternal (int handle) ;

    public static NavMeshLinkInstance AddLink(NavMeshLinkData link)
        {
            var handle = new NavMeshLinkInstance();
            handle.id = AddLinkInternal(link, Vector3.zero, Quaternion.identity);
            return handle;
        }
    
    
    public static NavMeshLinkInstance AddLink(NavMeshLinkData link, Vector3 position, Quaternion rotation)
        {
            var handle = new NavMeshLinkInstance();
            handle.id = AddLinkInternal(link, position, rotation);
            return handle;
        }
    
    
    public static void RemoveLink(NavMeshLinkInstance handle)
        {
            RemoveLinkInternal(handle.id);
        }
    
    
    internal static int AddLinkInternal (NavMeshLinkData link, Vector3 position, Quaternion rotation) {
        return INTERNAL_CALL_AddLinkInternal ( ref link, ref position, ref rotation );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static int INTERNAL_CALL_AddLinkInternal (ref NavMeshLinkData link, ref Vector3 position, ref Quaternion rotation);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern internal static  void RemoveLinkInternal (int handle) ;

    public static bool SamplePosition(Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, NavMeshQueryFilter filter)
        {
            return SamplePositionFilter(sourcePosition, out hit, maxDistance, filter.agentTypeID, filter.areaMask);
        }
    
    
    private static bool SamplePositionFilter (Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, int type, int mask) {
        return INTERNAL_CALL_SamplePositionFilter ( ref sourcePosition, out hit, maxDistance, type, mask );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_SamplePositionFilter (ref Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, int type, int mask);
    public static bool FindClosestEdge(Vector3 sourcePosition, out NavMeshHit hit, NavMeshQueryFilter filter)
        {
            return FindClosestEdgeFilter(sourcePosition, out hit, filter.agentTypeID, filter.areaMask);
        }
    
    
    private static bool FindClosestEdgeFilter (Vector3 sourcePosition, out NavMeshHit hit, int type, int mask) {
        return INTERNAL_CALL_FindClosestEdgeFilter ( ref sourcePosition, out hit, type, mask );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_FindClosestEdgeFilter (ref Vector3 sourcePosition, out NavMeshHit hit, int type, int mask);
    public static bool Raycast(Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, NavMeshQueryFilter filter)
        {
            return RaycastFilter(sourcePosition, targetPosition, out hit, filter.agentTypeID, filter.areaMask);
        }
    
    
    private static bool RaycastFilter (Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, int type, int mask) {
        return INTERNAL_CALL_RaycastFilter ( ref sourcePosition, ref targetPosition, out hit, type, mask );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_RaycastFilter (ref Vector3 sourcePosition, ref Vector3 targetPosition, out NavMeshHit hit, int type, int mask);
    public static bool CalculatePath(Vector3 sourcePosition, Vector3 targetPosition, NavMeshQueryFilter filter, NavMeshPath path)
        {
            path.ClearCorners();
            return CalculatePathFilterInternal(sourcePosition, targetPosition, path, filter.agentTypeID, filter.areaMask, filter.costs);
        }
    
    
    internal static bool CalculatePathFilterInternal (Vector3 sourcePosition, Vector3 targetPosition, NavMeshPath path, int type, int mask, float[] costs) {
        return INTERNAL_CALL_CalculatePathFilterInternal ( ref sourcePosition, ref targetPosition, path, type, mask, costs );
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static bool INTERNAL_CALL_CalculatePathFilterInternal (ref Vector3 sourcePosition, ref Vector3 targetPosition, NavMeshPath path, int type, int mask, float[] costs);
    public static NavMeshBuildSettings CreateSettings () {
        NavMeshBuildSettings result;
        INTERNAL_CALL_CreateSettings ( out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_CreateSettings (out NavMeshBuildSettings value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  void RemoveSettings (int agentTypeID) ;

    public static NavMeshBuildSettings GetSettingsByID (int agentTypeID) {
        NavMeshBuildSettings result;
        INTERNAL_CALL_GetSettingsByID ( agentTypeID, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSettingsByID (int agentTypeID, out NavMeshBuildSettings value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  int GetSettingsCount () ;

    public static NavMeshBuildSettings GetSettingsByIndex (int index) {
        NavMeshBuildSettings result;
        INTERNAL_CALL_GetSettingsByIndex ( index, out result );
        return result;
    }

    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    private extern static void INTERNAL_CALL_GetSettingsByIndex (int index, out NavMeshBuildSettings value);
    [UnityEngine.Scripting.GeneratedByOldBindingsGeneratorAttribute] // Temporarily necessary for bindings migration
    [System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)0x1000)]
    extern public static  string GetSettingsNameFromID (int agentTypeID) ;

}

}
