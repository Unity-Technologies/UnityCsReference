// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    // Keep this struct in sync with the one defined in "NavMeshBindingTypes.h"
    // Result information for NavMesh queries.
    [MovedFrom("UnityEngine")]
    public struct NavMeshHit
    {
        Vector3 m_Position;
        Vector3 m_Normal;
        float m_Distance;
        int m_Mask;
        int m_Hit;

        // Position of hit.
        public Vector3 position { get { return m_Position; } set { m_Position = value; } }

        // Normal at the point of hit.
        public Vector3 normal { get { return m_Normal; } set { m_Normal = value; } }

        // Distance to the point of hit.
        public float distance { get { return m_Distance; } set { m_Distance = value; } }

        // Mask specifying NavMesh area index at point of hit.
        public int mask { get { return m_Mask; } set { m_Mask = value; } }

        // Flag set when hit.
        public bool hit { get { return m_Hit != 0; } set { m_Hit = value ? 1 : 0; } }
    }

    // Keep this struct in sync with the one defined in "NavMeshBindingTypes.h"
    // Contains data describing a triangulation of the navmesh
    [UsedByNativeCode]
    [MovedFrom("UnityEngine")]
    public struct NavMeshTriangulation
    {
        public Vector3[] vertices;
        public int[] indices;
        public int[] areas;

        [Obsolete("Use areas instead.")]
        public int[] layers => areas;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Stub class for NavMeshData passing
    [NativeHeader("Modules/AI/NavMesh/NavMesh.bindings.h")]
    public sealed class NavMeshData : Object
    {
        public NavMeshData()
        {
            Internal_Create(this, 0);
        }

        public NavMeshData(int agentTypeID)
        {
            Internal_Create(this, agentTypeID);
        }

        [StaticAccessor("NavMeshDataBindings", StaticAccessorType.DoubleColon)]
        static extern void Internal_Create([Writable] NavMeshData mono, int agentTypeID);

        public extern Bounds sourceBounds { get; }
        public extern Vector3 position { get; set; }
        public extern Quaternion rotation { get; set; }
    }

    public struct NavMeshDataInstance
    {
        public bool valid => id != 0 && NavMesh.IsValidNavMeshDataHandle(id);
        internal int id { get; set; }

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

    // Keep this struct in sync with the one defined in "NavMeshBindingTypes.h"
    public struct NavMeshLinkData
    {
        Vector3 m_StartPosition;
        Vector3 m_EndPosition;
        float m_CostModifier;
        int m_Bidirectional;
        float m_Width;
        int m_Area;
        int m_AgentTypeID;

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
        public bool valid => id != 0 && NavMesh.IsValidLinkHandle(id);
        internal int id { get; set; }

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
        const int k_AreaCostElementCount = 32;

        internal float[] costs { get; private set; }

        public int areaMask { get; set; }
        public int agentTypeID { get; set; }

        public float GetAreaCost(int areaIndex)
        {
            if (costs == null)
            {
                if (areaIndex < 0 || areaIndex >= k_AreaCostElementCount)
                {
                    var msg = string.Format("The valid range is [0:{0}]", k_AreaCostElementCount - 1);
                    throw new IndexOutOfRangeException(msg);
                }
                return 1.0f;
            }
            return costs[areaIndex];
        }

        public void SetAreaCost(int areaIndex, float cost)
        {
            if (costs == null)
            {
                costs = new float[k_AreaCostElementCount];
                for (int j = 0; j < k_AreaCostElementCount; ++j)
                    costs[j] = 1.0f;
            }
            costs[areaIndex] = cost;
        }
    }

    [NativeHeader("Modules/AI/NavMeshManager.h")]
    [NativeHeader("Modules/AI/NavMesh/NavMesh.bindings.h")]
    [StaticAccessor("NavMeshBindings", StaticAccessorType.DoubleColon)]
    [MovedFrom("UnityEngine")]
    public static class NavMesh
    {
        public const int AllAreas = ~0;

        public delegate void OnNavMeshPreUpdate();
        public static OnNavMeshPreUpdate onPreUpdate;

        [RequiredByNativeCode]
        static void Internal_CallOnNavMeshPreUpdate()
        {
            if (onPreUpdate != null)
                onPreUpdate();
        }

        // Trace a ray between two points on the NavMesh.
        public static extern bool Raycast(Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, int areaMask);

        // Calculate a path between two points and store the resulting path.
        public static bool CalculatePath(Vector3 sourcePosition, Vector3 targetPosition, int areaMask, NavMeshPath path)
        {
            path.ClearCorners();
            return CalculatePathInternal(sourcePosition, targetPosition, areaMask, path);
        }

        static extern bool CalculatePathInternal(Vector3 sourcePosition, Vector3 targetPosition, int areaMask, NavMeshPath path);

        // Locate the closest NavMesh edge from a point on the NavMesh.
        public static extern bool FindClosestEdge(Vector3 sourcePosition, out NavMeshHit hit, int areaMask);

        // Sample the NavMesh closest to the point specified.
        public static extern bool SamplePosition(Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, int areaMask);

        [Obsolete("Use SetAreaCost instead.")]
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("SetAreaCost")]
        public static extern void SetLayerCost(int layer, float cost);

        [Obsolete("Use GetAreaCost instead.")]
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaCost")]
        public static extern float GetLayerCost(int layer);

        [Obsolete("Use GetAreaFromName instead.")]
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaFromName")]
        public static extern int GetNavMeshLayerFromName(string layerName);

        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("SetAreaCost")]
        public static extern void SetAreaCost(int areaIndex, float cost);

        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaCost")]
        public static extern float GetAreaCost(int areaIndex);

        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaFromName")]
        public static extern int GetAreaFromName(string areaName);

        public static extern NavMeshTriangulation CalculateTriangulation();

        //*undocumented* DEPRECATED
        [Obsolete("use NavMesh.CalculateTriangulation() instead.")]
        public static void Triangulate(out Vector3[] vertices, out int[] indices)
        {
            NavMeshTriangulation results = CalculateTriangulation();
            vertices = results.vertices;
            indices = results.indices;
        }

        [Obsolete("AddOffMeshLinks has no effect and is deprecated.")]
        public static void AddOffMeshLinks() {}

        [Obsolete("RestoreNavMesh has no effect and is deprecated.")]
        public static void RestoreNavMesh() {}

        [StaticAccessor("GetNavMeshManager()")]
        public static extern float avoidancePredictionTime { get; set; }

        [StaticAccessor("GetNavMeshManager()")]
        public static extern int pathfindingIterationsPerFrame { get; set; }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

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

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("IsValidSurfaceID")]
        internal static extern bool IsValidNavMeshDataHandle(int handle);

        [StaticAccessor("GetNavMeshManager()")]
        internal static extern bool IsValidLinkHandle(int handle);

        internal static extern Object InternalGetOwner(int dataID);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("SetSurfaceUserID")]
        internal static extern bool InternalSetOwner(int dataID, int ownerID);

        internal static extern Object InternalGetLinkOwner(int linkID);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("SetLinkUserID")]
        internal static extern bool InternalSetLinkOwner(int linkID, int ownerID);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("LoadData")]
        internal static extern int AddNavMeshDataInternal(NavMeshData navMeshData);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("LoadData")]
        internal static extern int AddNavMeshDataTransformedInternal(NavMeshData navMeshData, Vector3 position, Quaternion rotation);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("UnloadData")]
        internal static extern void RemoveNavMeshDataInternal(int handle);

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

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("AddLink")]
        internal static extern int AddLinkInternal(NavMeshLinkData link, Vector3 position, Quaternion rotation);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("RemoveLink")]
        internal static extern void RemoveLinkInternal(int handle);

        public static bool SamplePosition(Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, NavMeshQueryFilter filter)
        {
            return SamplePositionFilter(sourcePosition, out hit, maxDistance, filter.agentTypeID, filter.areaMask);
        }

        // a CUSTOM "SamplePosition" exists elsewhere. We need to pick unique name here to compile generated code in batch-builds
        static extern bool SamplePositionFilter(Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, int type, int mask);

        public static bool FindClosestEdge(Vector3 sourcePosition, out NavMeshHit hit, NavMeshQueryFilter filter)
        {
            return FindClosestEdgeFilter(sourcePosition, out hit, filter.agentTypeID, filter.areaMask);
        }

        // a CUSTOM "FindClosestEdge" exists elsewhere. We need to pick unique name here to compile generated code in batch-builds
        static extern bool FindClosestEdgeFilter(Vector3 sourcePosition, out NavMeshHit hit, int type, int mask);

        public static bool Raycast(Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, NavMeshQueryFilter filter)
        {
            return RaycastFilter(sourcePosition, targetPosition, out hit, filter.agentTypeID, filter.areaMask);
        }

        // a CUSTOM "Raycast" exists elsewhere. We need to pick unique name here to compile generated code in batch-builds
        static extern bool RaycastFilter(Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, int type, int mask);

        public static bool CalculatePath(Vector3 sourcePosition, Vector3 targetPosition, NavMeshQueryFilter filter, NavMeshPath path)
        {
            path.ClearCorners();
            return CalculatePathFilterInternal(sourcePosition, targetPosition, path, filter.agentTypeID, filter.areaMask, filter.costs);
        }

        static extern bool CalculatePathFilterInternal(Vector3 sourcePosition, Vector3 targetPosition, NavMeshPath path, int type, int mask, float[] costs);

        [StaticAccessor("GetNavMeshProjectSettings()")]
        public static extern NavMeshBuildSettings CreateSettings();

        //[StaticAccessor("GetNavMeshProjectSettings()")]
        //public static extern void UpdateSettings(NavMeshBuildSettings buildSettings);

        [StaticAccessor("GetNavMeshProjectSettings()")]
        public static extern void RemoveSettings(int agentTypeID);

        public static extern NavMeshBuildSettings GetSettingsByID(int agentTypeID);

        [StaticAccessor("GetNavMeshProjectSettings()")]
        public static extern int GetSettingsCount();

        public static extern NavMeshBuildSettings GetSettingsByIndex(int index);

        public static extern string GetSettingsNameFromID(int agentTypeID);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("CleanupAfterCarving")]
        public static extern void RemoveAllNavMeshData();
    }
}
