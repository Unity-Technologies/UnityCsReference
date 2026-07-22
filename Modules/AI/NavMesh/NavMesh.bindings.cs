// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.AI
{
    // Keep this struct in sync with the one defined in "NavMeshBindingTypes.h"
    ///<summary>Information about a position that is the result of a query ran on the NavMesh.</summary>
    ///<remarks>
    ///  <para>The object represents a valid result if the <c>distance</c> and <c>position</c> properties have finite values. Otherwise, the object represents a result that could not be calculated from the input data provided to the query. Refer to the documentation of each query method for details about the situations that produce invalid results.
    ///
    ///**Note:** You can use <c>float.isFinite()</c> to determine whether a value is finite.</para>
    ///  <para />
    ///</remarks>
    ///<example nocheck="true">
    ///  <code><![CDATA[{code Modules/AI/Tests/UTFTests/Playmode/CodeExamples/ShowLineOfSightToTarget.cs}]]></code>
    ///</example>
    ///<seealso cref="NavMesh.SamplePosition" />
    ///<seealso cref="NavMesh.FindClosestEdge" />
    ///<seealso cref="NavMesh.Raycast" />
    ///<seealso cref="NavMeshAgent.FindClosestEdge" />
    ///<seealso cref="NavMeshAgent.Raycast" />
    ///<seealso cref="NavMeshAgent.SamplePathPosition" />
    [MovedFrom("UnityEngine")]
    public struct NavMeshHit
    {
        Vector3 m_Position;
        Vector3 m_Normal;
        float m_Distance;
        int m_Mask;
        int m_Hit;

        ///<summary>Position of hit.</summary>
        ///<remarks>It is a position that a **NavMesh Agent** can move to, if it has a <see cref="NavMeshAgent.agentTypeID">agentTypeID</see> value that matches the <see cref="NavMeshBuildSettings.agentTypeID">agentTypeID</see> of the NavMesh at that position. The position lies inside a NavMesh polygon. When the NavMesh also contains  <see cref="NavMeshBuildSettings.buildHeightMesh">HeightMesh</see> data, the position aligns to the HeightMesh polygon that is closest on the vertical axis.
        ///
        ///If the position coordinates are not finite, the entire NavMeshHit object represents the result of an invalid query.</remarks>
        ///<seealso cref="NavMeshAgent.SetDestination" />
        ///<seealso cref="NavMeshAgent.Warp" />
        ///<seealso cref="NavMeshAgent.Move" />
        public Vector3 position { get => m_Position; set => m_Position = value; }

        ///<summary>Normal of the polygon edge where the query terminates.</summary>
        ///<remarks>The vector points towards the inner side of the last NavMesh polygon that the query traverses.
        ///
        ///If the query terminates inside a polygon, and is therefore not blocked by an edge, the normal is <see cref="Vector3.zero" />.
        ///
        ///**Note:** None of the query methods returns the normal of the polygon itself.</remarks>
        public Vector3 normal { get => m_Normal; set => m_Normal = value; }

        ///<summary>Distance to the point of hit.</summary>
        ///<remarks>If the value is not finite, the entire NavMeshHit object represents the result of an invalid query.</remarks>
        public float distance { get => m_Distance; set => m_Distance = value; }

        ///<summary>Bitmask that specifies the NavMesh area type at the point of hit.</summary>
        ///<remarks>The index at which the binary representation of the integer contains a bit turned on is the number of the area type.
        ///
        ///                    If the query proceeds uninterrupted to the target position, the <c>mask</c> represents the area type of the NavMesh polygon where the resulting <see cref="position" /> lies.
        ///
        ///                    When the query terminates at the edge of a NavMesh polygon that is of a different type than the ones allowed by the input parameters, the <c>mask</c> represents the area type of the polygon that blocks the query.
        ///
        ///                    When the query terminates at an edge of the NavMesh the <c>mask</c> is 0, to signify that there is no polygon beyond that position.</remarks>
        public int mask { get => m_Mask; set => m_Mask = value; }

        ///<summary>Flag set when the query encounters a particular valid situation.</summary>
        ///<remarks>The queries set this flag differently. <see cref="NavMesh.SamplePosition" /> reports <c>hit</c> as true every time it returns a valid position on the NavMesh. The rest of the methods report <c>hit</c> as true when the edge of a NavMesh polygon blocks the query before it can reach the target position. In all other cases <c>hit</c> is false.</remarks>
        public bool hit { get => m_Hit != 0; set => m_Hit = value ? 1 : 0; }
    }

    // Keep this struct in sync with the one defined in "NavMeshBindingTypes.h"
    ///<summary>Contains data describing a triangulation of a navmesh.</summary>
    [UsedByNativeCode]
    [MovedFrom("UnityEngine")]
    public struct NavMeshTriangulation
    {
        ///<summary>Vertices for the navmesh triangulation.</summary>
        ///<remarks>Vertices are referenced by the indices.</remarks>
        public Vector3[] vertices;
        ///<summary>Triangle indices for the navmesh triangulation.</summary>
        ///<remarks>Contains 3 integers for each triangle. These integers refer to the vertices array.</remarks>
        public int[] indices;
        ///<summary>NavMesh area indices for the navmesh triangulation.</summary>
        ///<remarks>Contains one element for each triangle.</remarks>
        public int[] areas;

        ///<summary>NavMeshLayer values for the navmesh triangulation.</summary>
        ///<remarks>Contains one element for each triangle.</remarks>
        [Obsolete("Use areas instead.")]
        public int[] layers => areas;
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    // Stub class for NavMeshData passing
    ///<summary>Contains and represents NavMesh data.</summary>
    ///<remarks>An object of this class can be used for creating instances of NavMeshes. See <see cref="NavMesh.AddNavMeshData" />. The contained NavMesh can be built and updated using the build API. See <see cref="UnityEngine.AI.NavMeshBuilder" /> and methods therein.</remarks>
    [NativeHeader("Modules/AI/NavMesh/NavMesh.bindings.h")]
    public sealed class NavMeshData : Object
    {
        ///<summary>Constructs a new object for representing a NavMesh for the default agent type.</summary>
        ///<remarks>At construction this NavMesh is empty, i.e. there are no polygons. You can use this class to create, build and add a NavMesh at runtime.</remarks>
        public NavMeshData()
        {
            Internal_Create(this, 0);
        }

        ///<summary>Constructs a new object representing a NavMesh for the specified agent type.</summary>
        ///<param name="agentTypeID">The agent type ID to create a NavMesh for.</param>
        public NavMeshData(int agentTypeID)
        {
            Internal_Create(this, agentTypeID);
        }

        [StaticAccessor("NavMeshDataBindings", StaticAccessorType.DoubleColon)]
        static extern void Internal_Create([Writable] NavMeshData mono, int agentTypeID);

        ///<summary>Returns the bounding volume of the input geometry used to build this NavMesh (RO).</summary>
        ///<remarks>If the NavMesh data has not been built, the bounds will have zero values.</remarks>
        public extern Bounds sourceBounds { get; }
        ///<summary>Gets or sets the world space position of the NavMesh data.</summary>
        ///<remarks>The default value is zero - that is, the world space origin.</remarks>
        public extern Vector3 position { get; set; }
        ///<summary>Gets or sets the orientation of the NavMesh data.</summary>
        ///<remarks>The default value is <see cref="Quaternion.identity" /> - that is, the NavMesh up axis is the same as the world space y-axis.</remarks>
        public extern Quaternion rotation { get; set; }
        internal extern bool hasHeightMeshData { [NativeMethod("HasHeightMeshData")] get; }

        internal extern NavMeshBuildSettings buildSettings { get; }
    }

    ///<summary>The instance is returned when adding NavMesh data.</summary>
    ///<remarks>A valid NavMesh data instance is available to the navigation system. This means you can calculate paths etc. using that instance. You also need the instance if you want to remove the NavMesh data at a later time.</remarks>
    ///<seealso cref="Remove" />
    ///<seealso cref="NavMesh.AddNavMeshData" />
    ///<seealso cref="NavMesh.RemoveNavMeshData" />
    public struct NavMeshDataInstance
    {
        ///<summary>True if the NavMesh data is added to the navigation system - otherwise false (RO).</summary>
        public bool valid => id != 0 && NavMesh.IsValidNavMeshDataHandle(id);
        internal int id { get; set; }

        ///<summary>Removes this instance from the NavMesh system.</summary>
        ///<remarks>An identical but convenient alternative to calling <see cref="NavMesh.RemoveNavMeshData" />. If the instance is not valid, e.g. has been removed before, the call has no effect.</remarks>
        public void Remove()
        {
            NavMesh.RemoveNavMeshDataInternal(id);
        }

        ///<summary>Get or set the owning Object.</summary>
        ///<remarks>If the instance is invalid: setting the owner has no effect and getting it will return null.</remarks>
        ///<seealso cref="NavMeshAgent.navMeshOwner" />
        public Object owner
        {
            get => NavMesh.InternalGetOwner(id);
            set
            {
                var ownerID = value != null ? value.GetEntityId() : EntityId.None;
                if (!NavMesh.InternalSetOwner(id, ownerID))
                    Debug.LogError("Cannot set 'owner' on an invalid NavMeshDataInstance");
            }
        }

        internal void FlagAsInSelectionHierarchy()
        {
            if (valid)
                FlagSurfaceAsInSelectionHierarchy(id);
        }

        [StaticAccessor("GetNavMeshManager()", StaticAccessorType.Dot)]
        static extern void FlagSurfaceAsInSelectionHierarchy(int id);
    }

    // Keep this struct in sync with the one defined in "NavMeshBindingTypes.h"
    ///<summary>Used for runtime manipulation of links connecting polygons of the NavMesh.</summary>
    ///<remarks>A typical use case is to connect different navigation meshes. Use the <see cref="NavMesh.AddLink" /> method to instantiate a link with these properties in the navigation system. The <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshLink.html">NavMesh Link</see> component creates its runtime data in this way.</remarks>
    public struct NavMeshLinkData
    {
        Vector3 m_StartPosition;
        Vector3 m_EndPosition;
        float m_CostModifier;
        int m_Bidirectional;
        float m_Width;
        int m_Area;
        int m_AgentTypeID;

        ///<summary>Start position of the link.</summary>
        ///<remarks>If the <see cref="width" /> is positive, this position specifies the midpoint of the starting edge.</remarks>
        public Vector3 startPosition { get => m_StartPosition; set => m_StartPosition = value; }
        ///<summary>End position of the link.</summary>
        ///<remarks>If the <see cref="width" /> is positive, this position specifies the midpoint of the ending edge.</remarks>
        public Vector3 endPosition { get => m_EndPosition; set => m_EndPosition = value; }
        ///<summary>If positive, overrides the pathfinder cost to traverse the link.</summary>
        ///<remarks>When searching for a path this cost multiplies the Euclidean distance between the link end points when scoring the link. If the value is negative, the default cost based on area type is used. The value must be &gt;= 1.0.</remarks>
        public float costModifier { get => m_CostModifier; set => m_CostModifier = value; }
        ///<summary>If true, the link can be traversed in both directions, otherwise only from start to end position.</summary>
        public bool bidirectional { get => m_Bidirectional != 0; set => m_Bidirectional = value ? 1 : 0; }
        ///<summary>If positive, the link will be rectangle aligned along the line from start to end.</summary>
        ///<remarks>This allows paths to enter the link at any location along the end sides. If not positive, the link endpoints will be represented as points.</remarks>
        public float width { get => m_Width; set => m_Width = value; }
        ///<summary>Area type of the link.</summary>
        ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas And Costs</seealso>
        public int area { get => m_Area; set => m_Area = value; }
        ///<summary>Specifies which agent type this link is available for.</summary>
        public int agentTypeID { get => m_AgentTypeID; set => m_AgentTypeID = value; }
    }

    ///<summary>Represents a link available for pathfinding.</summary>
    ///<remarks>You obtain a valid object when you call <see cref="NavMesh.AddLink" /> to create one specific link in the navigation system. Conversely, you need to pass it into <see cref="NavMesh.RemoveLink" /> to remove that instance of the link from the system. Use this object to check or modify the state of the link instance by calling the following methods: <see cref="NavMesh.IsLinkValid" />, <see cref="NavMesh.IsLinkOccupied" />, <see cref="NavMesh.IsLinkActive" />, <see cref="NavMesh.SetLinkActive" />, <see cref="NavMesh.GetLinkOwner" /> and <see cref="NavMesh.SetLinkOwner" />.
    ///
    ///
    ///
    ///            Empty objects created when you instantiate this struct do not represent any link that exists in the navigation system. The <see cref="NavMesh.IsLinkValid" /> and <see cref="NavMesh.IsLinkActive" /> methods return a value of <c>false</c> for objects created in this manner.</remarks>
    public partial struct NavMeshLinkInstance
    {
        internal int id { get; set; }
    }

    ///<summary>Specifies which agent type and areas to consider when searching the NavMesh.</summary>
    ///<remarks>This struct is used with the NavMesh query methods overloaded with the query filter argument.</remarks>
    ///<seealso cref="NavMesh.CalculatePath" />
    ///<seealso cref="NavMesh.Raycast" />
    ///<seealso cref="NavMesh.FindClosestEdge" />
    ///<seealso cref="NavMesh.SamplePosition" />
    public struct NavMeshQueryFilter
    {
        const int k_AreaCostElementCount = 32;

        internal float[] costs { get; private set; }

        ///<summary>A bitmask representing the traversable area types.</summary>
        public int areaMask { get; set; }
        ///<summary>The agent type ID, specifying which navigation meshes to consider for the query functions.</summary>
        public int agentTypeID { get; set; }

        ///<summary>Returns the area cost multiplier for the given area type for this filter.</summary>
        ///<remarks>The default value is 1.</remarks>
        ///<param name="areaIndex">Index to retrieve the cost for.</param>
        ///<returns>The cost multiplier for the supplied area index.</returns>
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

        ///<summary>Sets the pathfinding cost multiplier for this filter for a given area type.</summary>
        ///<remarks>Calling SetAreaCost the first time on a NavMeshQueryFilter object causes an internal allocation of the maximum 32 cost modifiers.</remarks>
        ///<param name="areaIndex">The area index to set the cost for.</param>
        ///<param name="cost">The cost for the supplied area index.</param>
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

    ///<summary>Singleton class to access the baked NavMesh.</summary>
    ///<remarks>Use the NavMesh class to perform spatial queries such as pathfinding and walkability tests. This class also lets you set the pathfinding cost for specific area types, and tweak the global behavior of pathfinding and avoidance.
    ///
    ///Before you can use spatial queries, you must first bake the NavMesh to your scene.
    ///
    ///See also:
    ///
    ///• &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/CreateNavMesh.html"&gt;Create a NavMesh&lt;/a&gt; – for more information on how to setup and bake NavMesh
    ///• &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html"&gt;Areas and Costs&lt;/a&gt; – to learn how to use different Area types.
    ///• <see cref="NavMeshAgent" /> – to learn how to control and move NavMesh Agents.
    ///• <see cref="NavMeshObstacle" /> – to learn how to control NavMesh Obstacles using scripting.
    ///• &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html"&gt;NavMeshLink&lt;/a&gt; – to learn how to control Off-Mesh Links using scripting.</remarks>
    [NativeHeader("Modules/AI/NavMeshManager.h")]
    [NativeHeader("Modules/AI/NavMesh/NavMesh.bindings.h")]
    [StaticAccessor("NavMeshBindings", StaticAccessorType.DoubleColon)]
    [MovedFrom("UnityEngine")]
    public static partial class NavMesh
    {
        ///<summary>Area mask constant that includes all NavMesh areas.</summary>
        ///<remarks>
        ///  Use the mask in query functions, such as <see cref="Raycast" />, to indicate that all NavMesh area types are accepted.
        ///
        ///  See <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</see> to learn how to use different Area types.
        ///</remarks>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///
        ///public class TargetReachable : MonoBehaviour
        ///{
        ///    public Transform target;
        ///    private NavMeshHit hit;
        ///    private bool blocked = false;
        ///
        ///    void Update()
        ///    {
        ///        // Allow pass through all area types when testing if the target position
        ///        // is reachable from the transform location.
        ///        blocked = NavMesh.Raycast(transform.position, target.position, out hit, NavMesh.AllAreas);
        ///        Debug.DrawLine(transform.position, target.position, blocked ? Color.red : Color.green);
        ///        if (blocked)
        ///            Debug.DrawRay(hit.position, Vector3.up, Color.red);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public const int AllAreas = ~0;

        ///<summary>Registers callback methods to be invoked before the NavMesh system updates.</summary>
        ///<remarks>This is useful for updating the NavMesh and links before the agents are simulated during the update cycle.</remarks>
        ///<seealso cref="onPreUpdate" />
        public delegate void OnNavMeshPreUpdate();
        ///<summary>Set a function to be called before the NavMesh is updated during the frame update execution.</summary>
        ///<remarks>This lets you set a delegate function to be called every frame, right before the NavMesh system gets updated.</remarks>
        ///<seealso cref="OnNavMeshPreUpdate" />
        [AutoStaticsCleanupOnCodeReload] // holds user-registered pre-update callbacks
        public static OnNavMeshPreUpdate onPreUpdate;

        [RequiredByNativeCode]
        static void ClearPreUpdateListeners()
        {
            onPreUpdate = null;
        }

        [RequiredByNativeCode]
        static void Internal_CallPreUpdateListeners()
        {
            if (onPreUpdate != null)
                onPreUpdate();
        }

        ///<summary>Trace a line between two points on the NavMesh.</summary>
        ///<remarks>
        ///  <para>The source and destination points are first mapped on the NavMesh, then a ray is traced from the source point towards the target. If the ray hits a NavMesh boundary, the function returns true and the hit data is filled. If the path from the source to target is unobstructed, the function returns false.
        ///
        ///If the raycast terminates on an outer edge, <c>hit.mask</c> is 0; otherwise it contains the area mask of the blocking polygon.
        ///
        ///This function can be used to check if an agent can walk unobstructed between two points on the NavMesh. For example if you character has an evasive dodge move which needs space, you can shoot a ray from the characters location to multiple directions to find a spot where the character can dodge to.
        ///
        ///The Raycast is different from physics ray cast because it works on “2.5D”, on the NavMesh. The difference to physics ray casts is that NavMesh ray casts can detect all kinds of navigation obstructions, such as holes in the ground, and it can also climb up slopes, if the area is navigable.</para>
        ///  <para>If you want to find the nearest point on the NavMesh, use physics ray cast to find a point in the world. For more information, refer to the <see href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMoveToClickPoint.html">Move an Agent to a Position Clicked by the Mouse</see> example.</para>
        ///</remarks>
        ///<param name="sourcePosition">The origin of the ray.</param>
        ///<param name="targetPosition">The end of the ray.</param>
        ///<param name="hit">Holds the properties of the ray cast resulting location.</param>
        ///<param name="areaMask">A bitfield mask specifying which NavMesh areas can be passed when tracing the ray.</param>
        ///<returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///
        ///public class TargetReachable : MonoBehaviour
        ///{
        ///    public Transform target;
        ///    private NavMeshHit hit;
        ///    private bool blocked = false;
        ///
        ///    void Update()
        ///    {
        ///        blocked = NavMesh.Raycast(transform.position, target.position, out hit, NavMesh.AllAreas);
        ///        Debug.DrawLine(transform.position, target.position, blocked ? Color.red : Color.green);
        ///
        ///        if (blocked)
        ///            Debug.DrawRay(hit.position, Vector3.up, Color.red);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static extern bool Raycast(Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, int areaMask);

        ///<summary>Calculate a path between two points and store the resulting path.</summary>
        ///<remarks>Use this function to avoid gameplay delays by planning a path before it is needed. You can also use this function to check if a target position is reachable before moving the agent.
        ///
        ///This function is synchronous. It performs path finding immediately which can adversely affect the frame rate when processing very long paths. It is recommended to only perform a few path finds per frame when, for example, evaluating distances to cover points.
        ///
        ///Use the returned path to set the path for an agent with <see cref="NavMeshAgent.SetPath" />. For SetPath to work, the agent must be close to the starting point.</remarks>
        ///<param name="sourcePosition">The initial position of the path requested.</param>
        ///<param name="targetPosition">The final position of the path requested.</param>
        ///<param name="areaMask">A bitfield mask specifying which NavMesh areas can be passed when calculating a path.</param>
        ///<param name="path">The resulting path.</param>
        ///<returns>True if either a complete or partial path is found. False otherwise.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///
        ///public class ShowGoldenPath : MonoBehaviour
        ///{
        ///    public Transform target;
        ///    private NavMeshPath path;
        ///    private float elapsed = 0.0f;
        ///    void Start()
        ///    {
        ///        path = new NavMeshPath();
        ///        elapsed = 0.0f;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        // Update the way to the goal every second.
        ///        elapsed += Time.deltaTime;
        ///        if (elapsed > 1.0f)
        ///        {
        ///            elapsed -= 1.0f;
        ///            NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path);
        ///        }
        ///        for (int i = 0; i < path.corners.Length - 1; i++)
        ///            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static bool CalculatePath(Vector3 sourcePosition, Vector3 targetPosition, int areaMask, NavMeshPath path)
        {
            path.ClearCorners();
            return CalculatePathInternal(sourcePosition, targetPosition, areaMask, path);
        }

        static extern bool CalculatePathInternal(Vector3 sourcePosition, Vector3 targetPosition, int areaMask, NavMeshPath path);

        ///<summary>Locate the closest NavMesh edge from a point on the NavMesh.</summary>
        ///<remarks>The returned <see cref="NavMeshHit" /> object contains the position
        ///and details of the nearest point on the nearest edge of the
        ///navmesh. This can be used to query how much extra space there is around the agent.</remarks>
        ///<param name="sourcePosition">The origin of the distance query.</param>
        ///<param name="hit">Holds the properties of the resulting location.</param>
        ///<param name="areaMask">A bitfield mask specifying which NavMesh areas can be passed when finding the nearest edge.</param>
        ///<returns>True if the nearest edge is found.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///
        ///public class MeasureSpace : MonoBehaviour
        ///{
        ///    void DrawCircle(Vector3 center, float radius, Color color)
        ///    {
        ///        Vector3 prevPos = center + new Vector3(radius, 0, 0);
        ///        for (int i = 0; i < 30; i++)
        ///        {
        ///            float angle = (float)(i + 1) / 30.0f * Mathf.PI * 2.0f;
        ///            Vector3 newPos = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        ///            Debug.DrawLine(prevPos, newPos, color);
        ///            prevPos = newPos;
        ///        }
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        NavMeshHit hit;
        ///        if (NavMesh.FindClosestEdge(transform.position, out hit, NavMesh.AllAreas))
        ///        {
        ///            DrawCircle(transform.position, hit.distance, Color.red);
        ///            Debug.DrawRay(hit.position, Vector3.up, Color.red);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static extern bool FindClosestEdge(Vector3 sourcePosition, out NavMeshHit hit, int areaMask);

        ///<summary>Finds the nearest point based on the NavMesh within a specified range.</summary>
        ///<remarks>The nearest point is found by projecting the input point onto nearby NavMesh instances along the vertical axis. This vertical axis has been chosen for each instance at the time of <see cref="NavMesh.AddNavMeshData">creation</see>. If this step does not find a projected point within the specified distance, then sampling is extended to surrounding NavMesh positions.
        ///
        ///Finds the nearest point based on the distance to the query point. This function does not consider obstructions. For example, in a two-story structure, if the sourcePosition is set to a point on the ceiling on the first floor, the nearest point might be found on the second floor rather than the first floor. The ceiling is not considered as an obstruction.
        ///
        ///This function may reduce the frame rate if a large search radius is specified. To avoid frame rate issues, it is recommended that you specify a maxDistance of twice the agent height.
        ///
        ///If you are trying to find a random point on the NavMesh, you should use the recommended radius and perform the find multiple times instead of using a very large radius.</remarks>
        ///<param name="sourcePosition">The origin of the sample query.</param>
        ///<param name="hit">Holds the properties of the resulting location. The value of <c>hit.normal</c> is never computed. It is always (0,0,0).</param>
        ///<param name="maxDistance">Sample within this distance from sourcePosition.</param>
        ///<param name="areaMask">A mask that specifies the NavMesh areas allowed when finding the nearest point.</param>
        ///<returns>True if the nearest point is found.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///
        ///public class RandomPointOnNavMesh : MonoBehaviour
        ///{
        ///    public float range = 10.0f;
        ///
        ///    bool RandomPoint(Vector3 center, float range, out Vector3 result)
        ///    {
        ///        for (int i = 0; i < 30; i++)
        ///        {
        ///            Vector3 randomPoint = center + Random.insideUnitSphere * range;
        ///            NavMeshHit hit;
        ///            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
        ///            {
        ///                result = hit.position;
        ///                return true;
        ///            }
        ///        }
        ///        result = Vector3.zero;
        ///        return false;
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        Vector3 point;
        ///        if (RandomPoint(transform.position, range, out point))
        ///        {
        ///            Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static extern bool SamplePosition(Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, int areaMask);

        ///<summary>Sets the cost for traversing over geometry of the layer type on all agents.</summary>
        ///<remarks>This will replace any custom layer costs on all agents.</remarks>
        [Obsolete("Use SetAreaCost instead.")]
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("SetAreaCost")]
        public static extern void SetLayerCost(int layer, float cost);

        ///<summary>Gets the cost for traversing over geometry of the layer type on all agents.</summary>
        [Obsolete("Use GetAreaCost instead.")]
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaCost")]
        public static extern float GetLayerCost(int layer);

        ///<summary>Returns the layer index for a named layer.</summary>
        ///<remarks>If the named layer does not exist returns -1.</remarks>
        [Obsolete("Use GetAreaFromName instead.")]
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaFromName")]
        public static extern int GetNavMeshLayerFromName(string layerName);

        ///<summary>Sets the cost for finding path over geometry of the area type on all agents.</summary>
        ///<remarks>
        ///  <para>This will replace any custom area costs on all agents, and set the default cost for new agents that are created after calling the function. The cost must be larger than 1.0.
        ///
        ///Use <see cref="GetAreaFromName" /> to find the area index based on the name of the NavMesh area type.</para>
        ///  <para />
        ///</remarks>
        ///<param name="areaIndex">Index of the area to set.</param>
        ///<param name="cost">New cost.</param>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///
        ///public class ToggleWaterCost : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        if (Input.anyKeyDown)
        ///        {
        ///            // Make the water area 10x more costly to traverse.
        ///            NavMesh.SetAreaCost(NavMesh.GetAreaFromName("water"), 10.0f);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</seealso>
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("SetAreaCost")]
        public static extern void SetAreaCost(int areaIndex, float cost);

        ///<summary>Gets the cost for path finding over geometry of the area type.</summary>
        ///<remarks>The value applies to all agents unless you the value has been customized per agent by calling <see cref="NavMeshAgent.SetAreaCost" />.
        ///
        ///Use <see cref="GetAreaFromName" /> to find the area index based on the name of the <see cref="NavMesh" /> area type.</remarks>
        ///<param name="areaIndex">Index of the area to get.</param>
        ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</seealso>
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaCost")]
        public static extern float GetAreaCost(int areaIndex);

        ///<summary>Returns the area index for a named NavMesh area type.</summary>
        ///<param name="areaName">Name of the area to look up.</param>
        ///<returns>Index if the specified area name exists, or -1 if no area type has the specified name.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///
        ///public class NearestPointOnWater : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        // Find the nearest point on water.
        ///        int waterMask = 1 << NavMesh.GetAreaFromName("water");
        ///        NavMeshHit hit;
        ///        if (NavMesh.SamplePosition(transform.position, out hit, 2.0f, waterMask))
        ///        {
        ///            Debug.DrawRay(hit.position, Vector3.up, Color.blue);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</seealso>
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaFromName")]
        public static extern int GetAreaFromName(string areaName);

        ///<summary>Get all the NavMesh area names.</summary>
        ///<returns>Names of all the NavMesh areas.</returns>
        [StaticAccessor("GetNavMeshProjectSettings()")]
        [NativeName("GetAreaNames")]
        public static extern string[] GetAreaNames();

        ///<summary>Calculates a triangulation of all the NavMeshes that are present in the scene at the time of the call.</summary>
        ///<remarks>Calculates and returns a simple triangulation of all the NavMeshes that are currently active. The resulting object contains vertices, triangle indices and NavMesh <see cref="NavMesh.GetAreaFromName">area types</see>. The triangles from each NavMesh instance are grouped together in the array. These triangle groups are further sorted in the array based on the <see cref="NavMeshBuildSettings.agentTypeID">agent types</see> that their originating NavMeshes were built for.
        ///
        ///The triangulation captures the current shape of the NavMeshes, which can include temporary holes <see cref="NavMeshObstacle.carving">carved</see> by NavMeshObstacles.
        ///
        ///
        ///The returned mesh contains only the triangles used for pathfinding. It does not contain the detail that is used to place the agents on the walkable surface. This is noticeable on locations with curved surfaces.</remarks>
        ///<returns>Object that contains a list of vertices and a list of indices that describe the triangles of the active NavMeshes.</returns>
        ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</seealso>
        public static extern NavMeshTriangulation CalculateTriangulation();

        ///<exclude />
        [Obsolete("use NavMesh.CalculateTriangulation() instead.")]
        public static void Triangulate(out Vector3[] vertices, out int[] indices)
        {
            NavMeshTriangulation results = CalculateTriangulation();
            vertices = results.vertices;
            indices = results.indices;
        }

        ///<exclude />
        [Obsolete("AddOffMeshLinks has no effect and is deprecated.")]
        public static void AddOffMeshLinks() {}

        ///<exclude />
        [Obsolete("RestoreNavMesh has no effect and is deprecated.")]
        public static void RestoreNavMesh() {}

        ///<summary>Describes how far in the future the agents predict collisions for avoidance.</summary>
        ///<remarks>The larger the value, the earlier the agents will start to avoid each other if they are on collision course. The value is measured in seconds. Default value is 2.0, a good range for tuning is between 0.5 and 5.0.</remarks>
        [StaticAccessor("GetNavMeshManager()")]
        public static extern float avoidancePredictionTime { get; set; }

        ///<summary>The maximum number of nodes processed for each frame during the asynchronous pathfinding process.</summary>
        ///<remarks>During the pathfinding process, the pathfinder expands only a certain number of nodes (NavMesh polygons) for each frame. This allows for smoother gameplay when processing long paths or when processing a large number of requests concurrently. However, the path request might take many frames to process.
        ///
        ///The iteration count only affects asynchronous pathfinding. This method of pathfinding is used when the NavMesh Agent destination is set with <see cref="AI.NavMeshAgent.SetDestination" /> or <see cref="AI.NavMeshAgent.destination" />.
        ///
        ///Increasing this value causes faster path processing but it might also cause frame rate issues. The default value is 100. An ideal value is between 50 and 500.</remarks>
        [StaticAccessor("GetNavMeshManager()")]
        public static extern int pathfindingIterationsPerFrame { get; set; }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        ///<summary>Adds the specified NavMeshData to the game.</summary>
        ///<remarks>This makes the NavMesh data available for agents and NavMesh queries. Returns an instance for later removing the NavMesh data from the runtime.
        ///
        ///The instance returned will be valid unless the NavMesh data could not be added - e.g. due to running out of memory or navmesh data being loaded from a corrupted file.</remarks>
        ///<param name="navMeshData">Contains the data for the navmesh.</param>
        ///<returns>Representing the added navmesh.</returns>
        ///<seealso cref="NavMeshDataInstance" />
        ///<seealso cref="RemoveNavMeshData" />
        public static NavMeshDataInstance AddNavMeshData(NavMeshData navMeshData)
        {
            if (navMeshData == null) throw new ArgumentNullException(nameof(navMeshData));

            var handle = new NavMeshDataInstance();
            handle.id = AddNavMeshDataInternal(navMeshData);
            return handle;
        }

        ///<summary>Adds the specified NavMeshData to the game.</summary>
        ///<remarks>This function is similar to <see cref="AddNavMeshData" /> above, but the position and rotation specified is applied in addition to the position and rotation where the NavMesh data was baked.</remarks>
        ///<param name="navMeshData">Contains the data for the navmesh.</param>
        ///<param name="position">Translate the navmesh to this position.</param>
        ///<param name="rotation">Rotate the navmesh to this orientation.</param>
        ///<returns>Representing the added navmesh.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///
        ///class Example : MonoBehaviour
        ///{
        ///    public NavMeshData data;
        ///    NavMeshDataInstance[] instances = new NavMeshDataInstance[2];
        ///
        ///    public void OnEnable()
        ///    {
        ///        // Add an instance of navmesh data
        ///        instances[0] = NavMesh.AddNavMeshData(data);
        ///
        ///        // Add another instance of the same navmesh data - displaced and rotated
        ///        instances[1] = NavMesh.AddNavMeshData(data, new Vector3(0, 5, 0), Quaternion.AngleAxis(90, Vector3.up));
        ///    }
        ///
        ///    public void OnDisable()
        ///    {
        ///        instances[0].Remove();
        ///        instances[1].Remove();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public static NavMeshDataInstance AddNavMeshData(NavMeshData navMeshData, Vector3 position, Quaternion rotation)
        {
            if (navMeshData == null) throw new ArgumentNullException(nameof(navMeshData));

            var handle = new NavMeshDataInstance();
            handle.id = AddNavMeshDataTransformedInternal(navMeshData, position, rotation);
            return handle;
        }

        ///<summary>Removes the specified <see cref="NavMeshDataInstance" /> from the game, making it unavailable for agents and queries.</summary>
        ///<remarks>Use the instance returned by <see cref="AddNavMeshData" /> to remove the corresponding NavMesh data. If the instance is not valid, e.g. has been removed before, the call has no effect.</remarks>
        ///<param name="handle">The instance of a NavMesh to remove.</param>
        ///<seealso cref="NavMeshDataInstance.Remove" />
        ///<seealso cref="RemoveAllNavMeshData" />
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
        internal static extern bool InternalSetOwner(int dataID, UnityEngine.EntityId ownerID);

        internal static extern Object InternalGetLinkOwner(int linkID);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("SetLinkUserID")]
        internal static extern bool InternalSetLinkOwner(int linkID, UnityEngine.EntityId ownerID);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("LoadData")]
        internal static extern int AddNavMeshDataInternal(NavMeshData navMeshData);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("LoadData")]
        internal static extern int AddNavMeshDataTransformedInternal(NavMeshData navMeshData, Vector3 position, Quaternion rotation);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("UnloadData")]
        internal static extern void RemoveNavMeshDataInternal(int handle);

        ///<summary>Adds a link to the NavMesh. The link is described by the NavMeshLinkData struct.</summary>
        ///<remarks>Returns an instance identifier for the added link.
        ///
        ///The returned instance is valid if the link was successfully added. The instance can be used to later remove the link using RemoveLink().
        ///
        ///**Note:** If the area is set to Not Walkable, or if adding a link would exceed the maximum number of active links (65535) the link will fail to be added – and the valid property will be false.</remarks>
        ///<param name="link">Object that describes the properties of the link.</param>
        ///<returns>Object that identifies the added link.</returns>
        ///<seealso cref="NavMeshLinkInstance" />
        ///<seealso cref="RemoveLink" />
        public static NavMeshLinkInstance AddLink(NavMeshLinkData link)
        {
            var handle = new NavMeshLinkInstance();
            handle.id = AddLinkInternal(link, Vector3.zero, Quaternion.identity);
            return handle;
        }

        ///<summary>Adds a link to the NavMesh. The link is described by the NavMeshLinkData struct.</summary>
        ///<remarks>Returns an instance identifier for the added link.
        ///
        ///This function is similar to AddLink above, but the position and rotation specified is applied to the start and end positions of the link. The rotation also specifies the local up-axis of the link.</remarks>
        ///<param name="link">Object that describes the properties of the link.</param>
        ///<param name="position">Translate the link to this position.</param>
        ///<param name="rotation">Rotate the link to this orientation.</param>
        ///<returns>Object that identifies the added link.</returns>
        ///<seealso cref="NavMeshLinkInstance" />
        ///<seealso cref="RemoveLink" />
        public static NavMeshLinkInstance AddLink(NavMeshLinkData link, Vector3 position, Quaternion rotation)
        {
            var handle = new NavMeshLinkInstance();
            handle.id = AddLinkInternal(link, position, rotation);
            return handle;
        }

        ///<summary>Removes a link from the NavMesh.</summary>
        ///<remarks>Use the instance returned by <see cref="AddLink" /> to remove the corresponding link.</remarks>
        ///<param name="handle">The instance of a link to remove.</param>
        ///<seealso cref="RemoveAllNavMeshData" />
        public static void RemoveLink(NavMeshLinkInstance handle)
        {
            RemoveLinkInternal(handle.id);
        }

        ///<summary>Determines whether the instance of the link can be used to <see cref="NavMesh.CalculatePath">calculate</see> paths, and if NavMesh agents can move over it.</summary>
        ///<remarks>Use this method to determine if paths for the assigned <see cref="NavMeshLinkData.agentTypeID">agent type</see> can traverse this link or not.
        ///
        ///A link instance is active by default regardless of whether the ends connect to NavMesh surfaces or not. To change the link's state, call <see cref="SetLinkActive" />. After you <see cref="NavMesh.RemoveLink">remove</see> the link from the running navigation system this method always returns <c>false</c>.
        ///
        ///
        ///
        ///This method is available as of 2023.2.</remarks>
        ///<param name="handle">The link instance whose state to query.</param>
        ///<returns>True if agents can plan paths through, and traverse, this instance of the link, otherwise false.</returns>
        ///<seealso cref="SetLinkActive" />
        public static bool IsLinkActive(NavMeshLinkInstance handle)
        {
            return IsOffMeshConnectionActive(handle.id);
        }

        ///<summary>Activates or deactivates the link instance. An active link instance can be traversed by agents and used to plan paths, but a deactivated link cannot.</summary>
        ///<remarks>This method changes the state of the link instance immediately. Any path that you <see cref="NavMesh.CalculatePath">calculate</see> afterwards takes into account the new state of the link. When you disable the link instance any paths that have already been calculated through it get a <see cref="NavMeshPath.status">status</see> value of <see cref="NavMeshPathStatus.PathInvalid">invalid</see>.
        ///
        ///You can call this method at any time to deactivate the link and prevent agents from moving through a section of the game level, for example through a door that connects two rooms. Conversely, you can activate the link and allow the agents to move between the respective game level sections.
        ///
        ///Deactivated links remain connected to the NavMesh surfaces and they do not need to find the connection points again when they are reactivated.
        ///
        ///Any link instance created with the <see cref="AddLink" /> method is active by default.
        ///
        ///
        ///
        ///This method is available as of 2023.2.</remarks>
        ///<param name="handle">The link instance whose active state to modify.</param>
        ///<param name="value">Whether agents can plan paths through, and traverse, the link. When the value is true, agents can plan paths through, and traverse, the link. Otherwise, no paths can use the link instance.</param>
        ///<seealso cref="IsLinkActive" />
        public static void SetLinkActive(NavMeshLinkInstance handle, bool value)
        {
            SetOffMeshConnectionActive(handle.id, value);
        }

        ///<summary>Determines whether or not a NavMesh agent is currently using this link.</summary>
        ///<remarks>Use this method to determine if your NavMesh agent can move onto the specified NavMesh link instance. Only one NavMesh agent can traverse a NavMesh link instance at any one time, so your agent can't move onto a NavMesh link instance that is already occupied. A NavMesh link instance is occupied when any NavMesh agent moves onto the link as part of the path the agent has calculated to the <see cref="NavMeshAgent.destination">destination</see>. When the agent moves off of the link, either automatically or through a call to <see cref="NavMeshAgent.CompleteOffMeshLink" />, the link instance is no longer occupied.
        ///
        ///This method is available as of 2023.2.</remarks>
        ///<param name="handle">The link instance whose state to query.</param>
        ///<returns>True if an agent is currently traversing the link, otherwise false.</returns>
        ///<seealso cref="NavMeshAgent.isOnOffMeshLink" />
        public static bool IsLinkOccupied(NavMeshLinkInstance handle)
        {
            return IsOffMeshConnectionOccupied(handle.id);
        }

        ///<summary>Determines whether the link instance is part of the current data used for navigation.</summary>
        ///<param name="handle">The identifier of the link instance to check.</param>
        ///<returns>True if the NavMesh link is added to the navigation system - otherwise false.</returns>
        public static bool IsLinkValid(NavMeshLinkInstance handle)
        {
            return IsValidLinkHandle(handle.id);
        }

        ///<summary>Gets the object, if any, that is associated with the link instance.</summary>
        ///<remarks>Use this method to obtain a reference to the component that created the link, or more generally, to any object that contains useful information about this specific link that is active in the navigation system. We refer to that object as the "owner". The owner is null for any new link instance created with <see cref="AddLink" />. Therefore you need to first call <see cref="SetLinkOwner" /> in order to retrieve the same object later. This "owner" is also referenced by the <see cref="OffMeshLinkData.owner" /> property when you query for the next link on a NavMesh agent's path.
        ///
        ///When the link instance is <see cref="NavMeshLinkInstance.Remove">removed</see> the owner property returns null once again.</remarks>
        ///<param name="handle">The identifier of the link instance whose owner needs to be retrieved.</param>
        ///<returns>The object that was passed into <see cref="SetLinkOwner" /> for the specified link instance.
        ///
        ///Returns <c>null</c> when no owner object has been assigned or when the link instance is not valid.</returns>
        ///<seealso cref="OffMeshLinkData.owner" />
        public static Object GetLinkOwner(NavMeshLinkInstance handle)
        {
            return InternalGetLinkOwner(handle.id);
        }

        ///<summary>Associates an object with the instance of a link.</summary>
        ///<remarks>Call <see cref="GetLinkOwner" /> to retrieve a reference to the assigned object. The <see cref="OffMeshLinkData.owner" /> property obtained from the path of an agent also points to the object that has been assigned to the link.
        ///
        ///If the instance of the link is not valid, setting the owner has no effect and getting it returns null.</remarks>
        ///<param name="handle">The identifier of the link instance for which you assign an owner.</param>
        ///<param name="owner">An object that carries useful information in relation to the instance of the link.</param>
        ///<seealso cref="OffMeshLinkData.owner" />
        public static void SetLinkOwner(NavMeshLinkInstance handle, Object owner)
        {
            var ownerID = owner != null ? owner.GetEntityId() : EntityId.None;
            if (!InternalSetLinkOwner(handle.id, ownerID))
                Debug.LogError("Cannot set 'owner' on an invalid NavMeshLinkInstance");
        }

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("AddLink")]
        internal static extern int AddLinkInternal(NavMeshLinkData link, Vector3 position, Quaternion rotation);

        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("RemoveLink")]
        internal static extern void RemoveLinkInternal(int handle);

        [StaticAccessor("GetNavMeshManager()")]
        internal static extern bool IsOffMeshConnectionOccupied(int handle);

        [StaticAccessor("GetNavMeshManager()")]
        internal static extern bool IsOffMeshConnectionActive(int linkHandle);

        [StaticAccessor("GetNavMeshManager()")]
        internal static extern void SetOffMeshConnectionActive(int linkHandle, bool activated);

        ///<summary>Samples the position nearest the sourcePosition on any NavMesh built for the agent type specified by the filter.</summary>
        ///<remarks>Consider only positions on areas defined in the <see cref="NavMeshQueryFilter.areaMask" />.
        ///A maximum search radius is set by maxDistance. The information of any found position is returned in the hit argument.</remarks>
        ///<param name="sourcePosition">The origin of the sample query.</param>
        ///<param name="hit">Holds the properties of the resulting location. The value of <c>hit.normal</c> is never computed. It is always (0,0,0).</param>
        ///<param name="maxDistance">Sample within this distance from sourcePosition.</param>
        ///<param name="filter">A filter specifying which NavMesh areas are allowed when finding the nearest point.</param>
        ///<returns>True if the nearest point is found.</returns>
        public static bool SamplePosition(Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, NavMeshQueryFilter filter)
        {
            return SamplePositionFilter(sourcePosition, out hit, maxDistance, filter.agentTypeID, filter.areaMask);
        }

        // a CUSTOM "SamplePosition" exists elsewhere. We need to pick unique name here to compile generated code in batch-builds
        static extern bool SamplePositionFilter(Vector3 sourcePosition, out NavMeshHit hit, float maxDistance, int type, int mask);

        ///<summary>Locate the closest NavMesh edge from a point on the NavMesh, subject to the constraints of the filter argument.</summary>
        ///<remarks>The returned NavMeshHit object contains the position and details of the nearest point on the nearest edge of the NavMesh. This can be used to query how much extra space there is around the agent.</remarks>
        ///<param name="sourcePosition">The origin of the distance query.</param>
        ///<param name="hit">Holds the properties of the resulting location.</param>
        ///<param name="filter">A filter specifying which NavMesh areas can be passed when finding the nearest edge.</param>
        ///<returns>True if the nearest edge is found.</returns>
        public static bool FindClosestEdge(Vector3 sourcePosition, out NavMeshHit hit, NavMeshQueryFilter filter)
        {
            return FindClosestEdgeFilter(sourcePosition, out hit, filter.agentTypeID, filter.areaMask);
        }

        // a CUSTOM "FindClosestEdge" exists elsewhere. We need to pick unique name here to compile generated code in batch-builds
        static extern bool FindClosestEdgeFilter(Vector3 sourcePosition, out NavMeshHit hit, int type, int mask);

        ///<summary>Traces a line between two positions on the NavMesh, subject to the constraints defined by the filter argument.</summary>
        ///<remarks>The line is terminated on outer edges or a non-passable area.</remarks>
        ///<param name="sourcePosition">The origin of the ray.</param>
        ///<param name="targetPosition">The end of the ray.</param>
        ///<param name="hit">Holds the properties of the ray cast resulting location.</param>
        ///<param name="filter">A filter specifying which NavMesh areas can be passed when tracing the ray.</param>
        ///<returns>True if the ray is terminated before reaching target position. Otherwise returns false.</returns>
        public static bool Raycast(Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, NavMeshQueryFilter filter)
        {
            return RaycastFilter(sourcePosition, targetPosition, out hit, filter.agentTypeID, filter.areaMask);
        }

        // a CUSTOM "Raycast" exists elsewhere. We need to pick unique name here to compile generated code in batch-builds
        static extern bool RaycastFilter(Vector3 sourcePosition, Vector3 targetPosition, out NavMeshHit hit, int type, int mask);

        ///<summary>Calculates a path between two positions mapped to the NavMesh, subject to the constraints and costs defined by the filter argument.</summary>
        ///<param name="sourcePosition">The initial position of the path requested.</param>
        ///<param name="targetPosition">The final position of the path requested.</param>
        ///<param name="filter">A filter specifying the cost of NavMesh areas that can be passed when calculating a path.</param>
        ///<param name="path">The resulting path.</param>
        ///<returns>True if a either a complete or partial path is found and false otherwise.</returns>
        public static bool CalculatePath(Vector3 sourcePosition, Vector3 targetPosition, NavMeshQueryFilter filter, NavMeshPath path)
        {
            path.ClearCorners();
            return CalculatePathFilterInternal(sourcePosition, targetPosition, path, filter.agentTypeID, filter.areaMask, filter.costs);
        }

        static extern bool CalculatePathFilterInternal(Vector3 sourcePosition, Vector3 targetPosition, NavMeshPath path, int type, int mask, float[] costs);

        ///<summary>Creates and returns a new entry of NavMesh build settings available for runtime NavMesh building.</summary>
        ///<remarks>This is useful for creating and storing settings to use for building NavMeshes for different sized characters.
        ///
        ///
        ///
        ///The <see cref="NavMeshBuildSettings.agentTypeID" /> will be positive and unique for the created settings.</remarks>
        ///<returns>The created settings.</returns>
        ///<seealso cref="NavMeshBuildSettings" />
        [StaticAccessor("GetNavMeshProjectSettings()")]
        public static extern NavMeshBuildSettings CreateSettings();

        //[StaticAccessor("GetNavMeshProjectSettings()")]
        //public static extern void UpdateSettings(NavMeshBuildSettings buildSettings);

        ///<summary>Removes the build settings matching the agent type ID.</summary>
        ///<remarks>If no matching settings are found or the agentTypeID is the default value 0, nothing is removed.</remarks>
        ///<param name="agentTypeID">The ID of the entry to remove.</param>
        [StaticAccessor("GetNavMeshProjectSettings()")]
        public static extern void RemoveSettings(int agentTypeID);

        ///<summary>Returns an existing entry of NavMesh build settings.</summary>
        ///<remarks>If no previously-created settings match the provided agent type ID, the returned <see cref="NavMeshBuildSettings" /> struct will have the agentTypeID set to -1. See also: <see cref="NavMeshBuildSettings" />.
        ///
        ///**Note:** A default entry will always exist for the agentTypeID being 0.</remarks>
        ///<param name="agentTypeID">The ID to look for.</param>
        ///<returns>The settings found.</returns>
        public static extern NavMeshBuildSettings GetSettingsByID(int agentTypeID);

        ///<summary>Returns the number of registered NavMesh build settings.</summary>
        ///<remarks>This will always be at least one available, namely the default setting.</remarks>
        ///<returns>The number of registered entries.</returns>
        ///<seealso cref="NavMeshBuildSettings" />
        [StaticAccessor("GetNavMeshProjectSettings()")]
        public static extern int GetSettingsCount();

        ///<summary>Returns an existing entry of NavMesh build settings by its ordered index.</summary>
        ///<remarks>If the index is outside the valid range (0, GetSettingsCount-1), the returned NavMeshBuildSettings struct will have the agentTypeID set to -1.</remarks>
        ///<param name="index">The index to retrieve from.</param>
        ///<returns>The found settings.</returns>
        ///<seealso cref="NavMeshBuildSettings" />
        ///<seealso cref="GetSettingsCount" />
        public static extern NavMeshBuildSettings GetSettingsByIndex(int index);

        ///<summary>Returns the name associated with the NavMesh build settings matching the provided agent type ID.</summary>
        ///<remarks>If no settings are found, the result is an empty string.</remarks>
        ///<param name="agentTypeID">The ID to look for.</param>
        ///<returns>The name associated with the ID found.</returns>
        public static extern string GetSettingsNameFromID(int agentTypeID);

        ///<summary>Removes all NavMesh surfaces and links from the game.</summary>
        ///<remarks>Unloads all surfaces and links that have been loaded from the Scene or added with <see cref="AddNavMeshData" /> or <see cref="AddLink" /> and frees all the internal resources associated with the NavMesh.</remarks>
        ///<seealso cref="RemoveNavMeshData" />
        ///<seealso cref="RemoveLink" />
        [StaticAccessor("GetNavMeshManager()")]
        [NativeName("CleanupAfterCarving")]
        public static extern void RemoveAllNavMeshData();
    }
}
