// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Bindings;
using UnityEngine.AI;

namespace UnityEngine.Experimental.AI
{
    ///<summary>Represents a compact identifier for the data of a NavMesh node.</summary>
    ///<remarks>It is used in <see cref="Experimental.AI.NavMeshQuery" /> operations for pinpointing and getting access to relevant nodes in the NavMesh. Each node can be used by only one type of agent.
    ///
    ///This identifier becomes invalid once the node gets removed from the NavMesh, either by completely removing the surface or by modifying the surface in the node's immediate vicinity.</remarks>
    ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshSurface.html"/>
    ///<seealso cref="Experimental.AI.NavMeshQuery.IsValid" />
    [Obsolete("The experimental PolygonId struct has been deprecated. Use NavNode instead.")]
    public struct PolygonId : IEquatable<PolygonId>
    {
        internal ulong polyRef;

        ///<summary>Returns <c>true</c> if the <see cref="Experimental.AI.PolygonId" /> has been created empty and has never pointed to any node in the NavMesh.</summary>
        public bool IsNull() { return polyRef == 0; }

        ///<summary>Returns <c>true</c> if two <see cref="Experimental.AI.PolygonId" /> objects refer to the same NavMesh node or if they are both null.</summary>
        ///<seealso cref="Experimental.AI.PolygonId.IsNull" />
        public static bool operator==(PolygonId x, PolygonId y) { return x.polyRef == y.polyRef; }
        ///<summary>Returns <c>true</c> if two <see cref="Experimental.AI.PolygonId" /> objects refer to different NavMesh nodes or if only one of them is null.</summary>
        ///<seealso cref="Experimental.AI.PolygonId.IsNull" />
        public static bool operator!=(PolygonId x, PolygonId y) { return x.polyRef != y.polyRef; }
        ///<summary>Returns the hash code for use in collections.</summary>
        public override int GetHashCode() { return polyRef.GetHashCode(); }
        ///<summary>Returns <c>true</c> if two <see cref="Experimental.AI.PolygonId" /> objects refer to the same NavMesh node.</summary>
        public bool Equals(PolygonId rhs) { return rhs == this; }

        ///<summary>Returns <c>true</c> if two <see cref="Experimental.AI.PolygonId" /> objects refer to the same NavMesh node.</summary>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is PolygonId))
                return false;
            var rhs = (PolygonId)obj;
            return rhs == this;
        }
    }

    ///<summary>A world position that is guaranteed to be on the surface of the NavMesh.</summary>
    ///<remarks>The NavMeshLocation stores the position on the NavMesh surface together with the <see cref="Experimental.AI.PolygonId" /> of the NavMesh node containing that position. Using NavMeshLocations with <see cref="Experimental.AI.NavMeshQuery" /> operations remove the need to project the desired world position onto the NavMesh at the beginning of each and every operation.
    ///
    ///A NavMeshLocation can be invalid in two situations:
    ///1. When it has been created empty, instead of being the result of a <see cref="Experimental.AI.NavMeshQuery" /> operation.
    ///2. When the NavMesh has been removed or modified at the indicated position or in its close vicinity.
    ///
    ///If a NavMeshLocation is made invalid by a <see cref="NavMeshObstacle" /> carving the NavMesh in its vicinity the NavMeshLocation returns to a valid state if the <see cref="NavMeshObstacle" /> is removed. This is because removing a <see cref="NavMeshObstacle" /> restores the NavMesh to its original form without regenerating it.</remarks>
    ///<seealso cref="Experimental.AI.NavMeshQuery.MapLocation" />
    ///<seealso cref="Experimental.AI.NavMeshQuery.IsValid" />
    ///<seealso cref="Experimental.AI.PolygonId" />
    [Obsolete("The experimental NavMeshLocation struct has been deprecated. Use NavLocation instead.")]
    public struct NavMeshLocation
    {
        ///<summary>Unique identifier for the node in the NavMesh to which the world position has been mapped.</summary>
        ///<seealso cref="Experimental.AI.NavMeshPolyTypes" />
        public PolygonId polygon { get; }
        ///<summary>A world position that sits precisely on the surface of the NavMesh or along its links.</summary>
        public Vector3 position { get; }

        internal NavMeshLocation(Vector3 position, PolygonId polygon)
        {
            this.position = position;
            this.polygon = polygon;
        }
    }

    //public struct NavMeshHit
    //{
    //    public NavMeshLocation  position;
    //    public Vector3          normal;
    //    public float            distance;

    //    public int              area; //Think if this should be a struct etc

    //    public bool             hit;
    //}

    //public struct NavMeshPolyData
    //{
    //    internal unsafe fixed ulong      neighbors[6];
    //    internal unsafe fixed float      vertices[6 * 3];
    //    internal int                     areaType;
    //    internal int                     vertexCount;
    //}

    //public struct NavMeshSegment
    //{
    //    public Vector3  begin;
    //    public Vector3  end;
    //}

    // Keep in sync with the values in NavMeshTypes.h
    ///<summary>Bit flags representing the resulting state of <see cref="Experimental.AI.NavMeshQuery" /> operations.</summary>
    ///<remarks>The main values are <c>Success</c>, <c>Failure</c> and <c>InProgress</c>. A status will usually have only one of these main flags set. The secondary flags (details) are set when specific issues have been encountered during the operation. <c>StatusDetailMask</c> is a bit mask that can be used to filter out these secondary flags.
    ///
    ///**Note:** Issues highlighted by the presence of certain detail flags in certain situations might refer to internal structures outside the control of users, thus they will not always be able to mitigate them by taking the necessary actions in their code. Ways for handling these situations will be made available in the future.</remarks>
    [Obsolete("The experimental PathQueryStatus enum has been deprecated. Use NavQueryStatus instead.")]
    [Flags]
    public enum PathQueryStatus
    {
        // High level status.
        ///<summary>The operation has failed.</summary>
        ///<remarks>Check the status for secondary flags that might provide more details about the issue causing the failure.</remarks>
        Failure = 1 << 31,
        ///<summary>The operation was successful.</summary>
        Success = 1 << 30,
        ///<summary>The operation is in progress.</summary>
        InProgress = 1 << 29,

        // Detail information for status.
        ///<summary>Bitmask that has 0 set for the <c>Success</c>, <c>Failure</c> and <c>InProgress</c> bits and 1 set for all the other flags.</summary>
        ///<remarks>It can be used to separate the detail flags from the main status flags.</remarks>
        StatusDetailMask = 0x0ffffff,
        ///<summary>Data in the NavMesh cannot be recognized and used.</summary>
        WrongMagic = 1 << 0,
        ///<summary>Data in the NavMesh world has a wrong version.</summary>
        WrongVersion = 1 << 1,
        ///<summary>Operation ran out of memory.</summary>
        ///<remarks>**Known issue, will be fixed:** This flag is not currently reported when memory fails to be allocated because the <see cref="Experimental.AI.NavMeshQuery" /> is created with a <c>pathNodePoolSize</c> value too large. The NavMeshQuery will then be silently defective and might produce a crash.</remarks>
        OutOfMemory = 1 << 2,
        ///<summary>A parameter did not contain valid information, useful for carring out the NavMesh query.</summary>
        InvalidParam = 1 << 3,
        ///<summary>The node buffer of the query was too small to store all results.</summary>
        ///<remarks>Creating a different <see cref="Experimental.AI.NavMeshQuery" /> with a larger <c>pathNodePoolSize</c> parameter might solve the issue.</remarks>
        BufferTooSmall = 1 << 4,
        ///<summary>Query ran out of node stack space during a search.</summary>
        ///<remarks>This happens when the query has visited more nodes than there is room in the <see cref="Experimental.AI.NavMeshQuery" />. To fix this issue try a larger value for the <c>pathNodePoolSize</c> parameter when creating the <see cref="Experimental.AI.NavMeshQuery" />.</remarks>
        OutOfNodes = 1 << 5,
        ///<summary>Query did not reach the end location, returning best guess.</summary>
        PartialResult = 1 << 6
    }

    // Flags describing polygon properties. Keep in sync with the enum declared in NavMesh.h
    ///<summary>The types of nodes in the navigation data.</summary>
    ///<remarks>Navigation data is comprised geometrically of polygons and segments connected together.</remarks>
    ///<seealso cref="Experimental.AI.NavMeshQuery.GetPolygonType" />
    ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshSurface.html">NavMeshSurface</seealso>
    ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html">NavMeshLink</seealso>
    ///<seealso cref="OffMeshLink">Off-mesh Link</seealso>
    [Obsolete("The experimental NavMeshPolyTypes enum has been deprecated. Use NavNodeType instead.")]
    public enum NavMeshPolyTypes
    {
        ///<summary>Type of node in the NavMesh representing one surface polygon.</summary>
        ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshSurface.html">NavMeshSurface</seealso>
        Ground = 0,
        ///<summary>Type of node in the NavMesh representing a point-to-point connection between two positions on the NavMesh surface.</summary>
        ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshLink.html">NavMeshLink</seealso>
        ///<seealso cref="OffMeshLink">Off-mesh Link</seealso>
        OffMeshConnection = 1
    }

    ///<summary>Assembles together a collection of NavMesh surfaces and links that are used as a whole for performing navigation operations.</summary>
    ///<remarks>Operations are initialized against one world, can use only the NavMeshes inside that world and are not aware of the existence of any other NavMeshWorld.
    ///
    ///Copying this object only produces a new reference to the same NavMesh data, it does not duplicate the data in memory.
    ///
    ///**Important note:** Currently only a single NavMesh world can be used and a reference to it can be obtained through the <see cref="Experimental.AI.NavMeshWorld.GetDefaultWorld" /> method. In the future, multiple NavMesh worlds will be able to be created and any two of them will be completely isolated from each other.</remarks>
    ///<seealso cref="Experimental.AI.NavMeshQuery" />
    [Obsolete("The experimental NavMeshWorld struct has been deprecated. Use NavWorld instead.")]
    [StaticAccessor("NavMeshWorldBindingsExperimental", StaticAccessorType.DoubleColon)]
    [NativeHeader("Modules/AI/NavMeshExperimental.bindings.h")]
    [NativeType(CodegenOptions.Auto, "NavMeshWorldExp")]
    public struct NavMeshWorld
    {
        internal IntPtr world;

        ///<summary>Returns <c>true</c> if the NavMeshWorld has been properly initialized.</summary>
        ///<remarks>Currently the only way to obtain the single possible valid NavMesh world is through a call to <see cref="Experimental.AI.NavMeshWorld.GetDefaultWorld" />.</remarks>
        public bool IsValid()
        {
            return world != IntPtr.Zero;
        }

        static extern NavMeshWorld GetDefaultWorldExp();
        ///<summary>Returns a reference to the single <see cref="Experimental.AI.NavMeshWorld" /> that can currently exist and be used in Unity.</summary>
        ///<remarks>The returned world comprises of all the NavMeshes and connections that are also used through the <see cref="NavMesh" />-related structures.</remarks>
        public static NavMeshWorld GetDefaultWorld()
        {
            return GetDefaultWorldExp();
        }

        static extern void AddDependencyInternalExp(IntPtr navmesh, JobHandle handle);

        ///<summary>Tells the NavMesh world to halt any changes until the specified job is completed.</summary>
        ///<remarks>When jobs process <see cref="Experimental.AI.NavMeshQuery" /> operations, it is essential that the NavMesh data does not change. Thus, every time a job of that type is scheduled its <see cref="JobHandle" /> must be passed to the NavMeshWorld using this method. Otherwise, an exception will be thrown when the project is running in the Editor.</remarks>
        ///<param name="job">The job that needs to be completed before the NavMesh world can be modified in any way.</param>
        ///<seealso cref="IJob" />
        ///<seealso cref="IJobParallelFor" />
        public void AddDependency(JobHandle job)
        {
            if (!IsValid())
                throw new InvalidOperationException("The NavMesh world is invalid.");
            AddDependencyInternalExp(world, job);
        }
    }

    ///<summary>Object used for doing navigation operations in a <see cref="Experimental.AI.NavMeshWorld" />.</summary>
    ///<remarks>NavMeshQuery operations can be executed inside jobs (<see cref="IJob" />, <see cref="IJobParallelFor" />), as opposed to the operations in the <see cref="NavMesh" />-related structures.
    ///
    ///To obtain a path between two locations on the NavMesh, you must create a NavMeshQuery with a <c>pathNodePoolSize</c> value in the range from 1 to 65,535. After creating a NavMeshQuery, you must call the following methods in this order: <c>BeginFindPath</c>, <c>UpdateFindPath</c> (can be repeated), <c>EndFindPath</c>, <c>GetPathResult</c>. These methods store state data within the NavMeshQuery. Other methods can be called in any order since they do not change state data.
    ///
    ///All methods throw exceptions if any of their parameters are not valid when executed in the Editor.
    ///
    ///**Note:** The intended feature set for NavMeshQuery is not yet fully complete.</remarks>
    [Obsolete("The experimental NavMeshQuery struct has been deprecated. Use NavWorld instead.")]
    [NativeContainer]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/AI/NavMeshExperimental.bindings.h")]
    [NativeHeader("Modules/AI/Public/NavMeshBindingTypes.h")]
    [NativeHeader("Runtime/Math/Matrix4x4.h")]
    [StaticAccessor("NavMeshQueryBindingsExperimental", StaticAccessorType.DoubleColon)]
    [NativeType(CodegenOptions.Auto, "NavMeshQueryExp")]
    public struct NavMeshQuery : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal IntPtr             m_NavMeshQuery;

        const string                k_NoBufferAllocatedErrorMessage = "This query has no buffer allocated for pathfinding operations. " +
            "Create a different NavMeshQuery with an explicit node pool size.";
        internal AtomicSafetyHandle m_Safety;

        // Each node in the pool stores an index to the next node anywhere in the pool.
        // To save memory, indices stored in the node pool are of type unsigned short.
        // Keep in sync with kMaxNavMeshNodePoolSize = USHRT_MAX from NavMeshNode.h
        const int k_MaxNavMeshNodePoolSize = ushort.MaxValue;

        ///<summary>Creates the <see cref="Experimental.AI.NavMeshQuery" /> object and allocates memory to store NavMesh node information, if required.</summary>
        ///<remarks>You must specify a pathNodePoolSize greater than 0 to use the NavMeshQuery object for pathfinding methods (<c>BeginFindPath</c>, <c>UpdateFindPath</c>, <c>EndFindPath</c>, <c>GetPathResult</c> ). If the node pool size for the NavMeshQuery object is too small, the pathfinding method returns a <see cref="Experimental.AI.PathQueryStatus.OutOfNodes" /> status. The range of pathNodePoolSize is 0 through 65,535.</remarks>
        ///<param name="world">NavMeshWorld object used as an entry point to the collection of NavMesh objects. This object that can be used by query operations.</param>
        ///<param name="allocator">Label indicating the desired life time of the object. (**Known issue:** Currently <c>allocator</c> has no effect).</param>
        ///<param name="pathNodePoolSize">The number of nodes temporarily stored in the query during search operations. The maximum number of nodes is 65,535. By default, if unspecified, the number of nodes is set to 0.</param>
        public NavMeshQuery(NavMeshWorld world, Allocator allocator, int pathNodePoolSize = 0)
        {
            if (!world.IsValid())
                throw new ArgumentNullException("world", "Invalid world");

            if (pathNodePoolSize < 0 || pathNodePoolSize > k_MaxNavMeshNodePoolSize)
                throw new ArgumentException(
                    $"The path node pool size ({pathNodePoolSize}) must be greater than or equal to 0 and less than {k_MaxNavMeshNodePoolSize + 1}.",
                    nameof(pathNodePoolSize));
            m_NavMeshQuery = CreateExp(world, pathNodePoolSize);

            UnsafeUtility.LeakRecord(m_NavMeshQuery, LeakCategory.NavMeshQuery, 0);
            AtomicSafetyHandle.CreateHandle(out m_Safety, allocator);
            AddQuerySafetyExp(m_NavMeshQuery, m_Safety);
        }

        ///<summary>Destroys the NavMeshQuery and deallocates all memory used by it.</summary>
        public void Dispose()
        {

            // When the NavMesh destroys itself it will disable read or write access.
            // Since it has been deallocated, we shouldn't deregister the query from it...
            // We need to extract removeQuery before disposing the handle,
            // because the atomic safety handle stores that state.
            var removeQuery = AtomicSafetyHandle.GetAllowReadOrWriteAccess(m_Safety);

            AtomicSafetyHandle.DisposeHandle(ref m_Safety);

            if (removeQuery)
                RemoveQuerySafetyExp(m_NavMeshQuery, m_Safety);
            UnsafeUtility.LeakErase(m_NavMeshQuery, LeakCategory.NavMeshQuery);
            DestroyExp(m_NavMeshQuery);
            m_NavMeshQuery = IntPtr.Zero;
        }

        static extern IntPtr CreateExp(NavMeshWorld world, int nodePoolSize);

        static extern void DestroyExp(IntPtr navMeshQuery);

        static extern void AddQuerySafetyExp(IntPtr navMeshQuery, AtomicSafetyHandle handle);
        static extern void RemoveQuerySafetyExp(IntPtr navMeshQuery, AtomicSafetyHandle handle);

        [NativeMethod(IsThreadSafe = true)]
        static extern bool HasNodePoolExp(IntPtr navMeshQuery);

        ///<summary>Initiates a pathfinding operation between two locations on the NavMesh.</summary>
        ///<remarks>The path always begins at the specified location. If the desired end location is not directly accessible, the search algorithm tries to find a valid location nearby.
        ///
        ///Calling this method overrides the progress made by this <see cref="Experimental.AI.NavMeshQuery" /> in the previous pathfinding operation.
        ///
        ///<see cref="Experimental.AI.NavMeshQuery.UpdateFindPath" /> should be called after this method to process the path search.</remarks>
        ///<param name="costs">Array of custom cost values for all of the 32 possible area types. Each value must be at least <c>1.0f</c>. This parameter is optional and defaults to the area costs configured in the project settings.</param>
        ///<param name="areaMask">Bitmask with values of 1 set at the indices for areas that can be traversed, and values of 0 for areas that are not traversable. This parameter is optional and defaults to <see cref="NavMesh.AllAreas" />, if omitted.</param>
        ///<param name="start">The start location on the NavMesh for the path.</param>
        ///<param name="end">The location on the NavMesh where the path ends.</param>
        ///<returns>
        ///  <c>InProgress</c> if the operation was successful and the query is ready to search for a path.
        ///
        ///<c>Failure</c> if the query's NavMeshWorld or any of the received parameters are no longer valid.</returns>
        ///<seealso cref="Experimental.AI.PathQueryStatus" />
        ///<seealso cref="NavMesh.GetAreaCost" />
        ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</seealso>
        public unsafe PathQueryStatus BeginFindPath(NavMeshLocation start, NavMeshLocation end,
            int areaMask = NavMesh.AllAreas, NativeArray<float> costs = new NativeArray<float>())
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (!HasNodePoolExp(m_NavMeshQuery))
                throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);

            const int kAreaCount = 32;
            if (costs.Length != 0)
            {
                if (costs.Length != kAreaCount)
                    throw new ArgumentException(
                        string.Format("The number of costs ({0}) must be exactly {1}, one for each possible area type.", costs.Length, kAreaCount)
                        , "costs");

                for (var i = 0; i < costs.Length; i++)
                {
                    if (costs[i] < 1.0f)
                        throw new ArgumentException(
                            string.Format("The area cost ({0}) at index ({1}) must be greater or equal to 1.", costs[i], i), "costs");
                }
            }

            if (!IsValid(start.polygon))
                throw new ArgumentException("The start location doesn't belong to any active NavMesh surface.", "start");


            if (!IsValid(end.polygon))
                throw new ArgumentException("The end location doesn't belong to any active NavMesh surface.", "end");

            var agentTypeStart = GetAgentTypeIdForPolygon(start.polygon);
            var agentTypeEnd = GetAgentTypeIdForPolygon(end.polygon);
            if (agentTypeStart != agentTypeEnd)
                throw new ArgumentException(string.Format(
                    "The start and end locations belong to different NavMesh surfaces, with agent type IDs {0} and {1}.",
                    agentTypeStart, agentTypeEnd));
            void* costsPtr = costs.Length > 0 ? costs.GetUnsafePtr() : null;
            return BeginFindPathExp(m_NavMeshQuery, start, end, areaMask, costsPtr);
        }

        ///<summary>Continues a path search that is in progress.</summary>
        ///<remarks>The operation needs to have been initialized previously with <see cref="Experimental.AI.NavMeshQuery.BeginFindPath" /> and it will run until the entire route is found or the specified number of iterations have been executed.
        ///
        ///As long as the previous call returned a state of <c>InProgress</c> this method can be called repeatedly, across different frames, until the operation is successful. Use <see cref="Experimental.AI.NavMeshQuery.EndFindPath" /> afterwards to prepare the path data for retrieval, along with the number of contained nodes.</remarks>
        ///<param name="iterations">Maximum number of nodes to be traversed by the search algorithm during this call.</param>
        ///<param name="iterationsPerformed">Outputs the actual number of nodes that have been traversed during this call.</param>
        ///<returns>
        ///  <c>InProgress</c> if the search needs to continue further by calling <c>UpdateFindPath</c> again.
        ///
        ///<c>Success</c> if the search is completed and a path has been found or not.
        ///
        ///<c>Failure</c> if the search for the desired position could not be completed because the NavMesh has changed significantly since the search was initiated.
        ///
        ///Additionally the returned value can contain the <c>OutOfNodes</c> flag when the <c>pathNodePoolSize</c> parameter for the NavMeshQuery initialization was not large enough to accommodate the search space.</returns>
        ///<seealso cref="Experimental.AI.PathQueryStatus" />
        public PathQueryStatus UpdateFindPath(int iterations, out int iterationsPerformed)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (!HasNodePoolExp(m_NavMeshQuery))
                throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);
            return UpdateFindPathExp(m_NavMeshQuery, iterations, out iterationsPerformed);
        }

        ///<summary>Obtains the number of nodes in the path that has been computed during a successful <see cref="Experimental.AI.NavMeshQuery.UpdateFindPath" /> operation.</summary>
        ///<remarks>This method prepares the path data so that <see cref="Experimental.AI.NavMeshQuery.GetPathResult" /> can be used afterward to retrieve the actual array of <see cref="Experimental.AI.PolygonId" /> values that make up the path.
        ///
        ///**Important:** This method should only be called once at the end of the pathfinding operation. Calling it multiple times may ruin the stored path.</remarks>
        ///<param name="pathSize">A reference to an int which will be set to the number of NavMesh nodes in the found path.</param>
        ///<returns>
        ///  <c>Success</c> when the number of nodes in the path was retrieved correctly.
        ///
        ///<c>PartialPath</c> when a path was found but it falls short of the desired end location.
        ///
        ///<c>Failure</c> when the path size can not be evaluated because the preceding call to <c>UpdateFindPath</c> was not successful.</returns>
        ///<seealso cref="Experimental.AI.PathQueryStatus" />
        public PathQueryStatus EndFindPath(out int pathSize)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (!HasNodePoolExp(m_NavMeshQuery))
                throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);
            return EndFindPathExp(m_NavMeshQuery, out pathSize);
        }

        ///<summary>Copies into the provided array the list of NavMesh nodes that form the path found by the NavMeshQuery operation.</summary>
        ///<remarks>Must be called at the end of a successful <see cref="Experimental.AI.NavMeshQuery.BeginFindPath" /> - <see cref="Experimental.AI.NavMeshQuery.UpdateFindPath" /> - <see cref="Experimental.AI.NavMeshQuery.EndFindPath" /> sequence in order to obtain the resulting path.
        ///
        ///Can be called multiple times as long as <see cref="Experimental.AI.NavMeshQuery.BeginFindPath" /> has not been called for that same query.
        ///
        ///If the resulting path, stored in the query, is longer than the length of the provided array, the nodes are still copied (from the beginning of the path up to the array's length).
        ///
        ///**Important:** If the start NavMesh node of the path has been removed by a NavMesh modification since the initial <c>BeginFindPath</c> call of the pathfinding operation, the returned path will be empty.</remarks>
        ///<param name="path">Data array to be filled with the sequence of NavMesh nodes that comprises the found path.</param>
        ///<returns>Number of path nodes successfully copied into the provided array.</returns>
        public unsafe int GetPathResult(NativeSlice<PolygonId> path)
        {
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);

            if (!HasNodePoolExp(m_NavMeshQuery))
                throw new InvalidOperationException(k_NoBufferAllocatedErrorMessage);
            return GetPathResultExp(m_NavMeshQuery, path.GetUnsafePtr(), path.Length);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe PathQueryStatus BeginFindPathExp(IntPtr navMeshQuery, NavMeshLocation start, NavMeshLocation end, int areaMask, void* costs);

        [NativeMethod(IsThreadSafe = true)]
        static extern PathQueryStatus UpdateFindPathExp(IntPtr navMeshQuery, int iterations, out int iterationsPerformed);

        [NativeMethod(IsThreadSafe = true)]
        static extern PathQueryStatus EndFindPathExp(IntPtr navMeshQuery, out int pathSize);

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe int GetPathResultExp(IntPtr navMeshQuery, void* path, int maxPath);

        // If BeginFindPath/UpdateFindPath/EndFindPath existing NativeArray become invalid...
//      extern NavMeshPathStatus GetPath(out NativeArray<PolygonId> outputPath);

        //void DidScheduleJob(JobHandle handle);


        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsValidPolygonExp(IntPtr navMeshQuery, PolygonId polygon);

        ///<summary>Returns true if the node referenced by the specified <see cref="Experimental.AI.PolygonId" /> is active in the NavMesh.</summary>
        ///<remarks>You can make NavMesh nodes invalid when you remove the NavMesh surface or the links they belong to, or when you modify the NavMesh in their region, replacing them. You can remove the NavMesh surface and links with calls to <see cref="NavMesh.RemoveNavMeshData" />, <see cref="NavMesh.RemoveLink" />. To modify the NavMesh, call  <see cref="NavMeshBuilder.UpdateNavMeshData" /> or use a &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshObstacle.html"&gt;NavMeshObstacle&lt;/a&gt; to carve it.</remarks>
        ///<param name="polygon">Identifier of the NavMesh node to be checked.</param>
        public bool IsValid(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return polygon.polyRef != 0 && IsValidPolygonExp(m_NavMeshQuery, polygon);
        }

        ///<summary>Returns <c>true</c> if the node referenced by the <see cref="Experimental.AI.PolygonId" /> contained in the <see cref="Experimental.AI.NavMeshLocation" /> is active in the NavMesh.</summary>
        ///<param name="location">Location on the NavMesh to be checked. Same as checking <c>location.polygon</c> directly.</param>
        public bool IsValid(NavMeshLocation location)
        {
            return IsValid(location.polygon);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern int GetAgentTypeIdForPolygonExp(IntPtr navMeshQuery, PolygonId polygon);
        ///<summary>Returns the identifier of the agent type the NavMesh was baked for or for which the link has been configured.</summary>
        ///<remarks>When NavMesh surfaces are baked or links are configured the **Agent Type** allowed to use them needs to be specified. Each **Agent Type** is identified by a unique integer. Operations such as <see cref="Experimental.AI.NavMeshQuery.MapLocation" />, <see cref="NavMesh.GetSettingsByID" />, <see cref="NavMesh.GetSettingsNameFromID" />, <see cref="NavMeshBuilder.BuildNavMeshData" />, and <see cref="NavMesh.CalculatePath" /> all require an agent type to be specified to distinguish between NavMeshes built for different agent configurations.</remarks>
        ///<param name="polygon">Identifier of a node from a NavMesh surface or link.</param>
        ///<returns>Agent type identifier.</returns>
        public int GetAgentTypeIdForPolygon(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return GetAgentTypeIdForPolygonExp(m_NavMeshQuery, polygon);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern bool IsPositionInPolygonExp(IntPtr navMeshQuery, Vector3 position, PolygonId polygon);

        [NativeMethod(IsThreadSafe = true)]
        static extern PathQueryStatus GetClosestPointOnPolyExp(IntPtr navMeshQuery, PolygonId polygon, Vector3 position, out Vector3 nearest);

        ///<summary>Returns a valid <see cref="Experimental.AI.NavMeshLocation" /> for a position and a polygon provided by the user.</summary>
        ///<remarks>The returned position will be the point on the surface of the required NavMesh polygon that is closest to the specified position.
        ///
        ///Other methods for obtaining reliable positions on the NavMesh are: <see cref="Experimental.AI.NavMeshQuery.MapLocation" />, <see cref="Experimental.AI.NavMeshQuery.MoveLocation" /> and <see cref="Experimental.AI.NavMeshQuery.GetPortalPoints" />.</remarks>
        ///<param name="position">World position of the <see cref="Experimental.AI.NavMeshLocation" /> to be created.</param>
        ///<param name="polygon">Valid identifier for the NavMesh node.</param>
        ///<returns>Object containing the desired position and NavMesh node.</returns>
        public NavMeshLocation CreateLocation(Vector3 position, PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            Vector3 nearest;
            var status = GetClosestPointOnPolyExp(m_NavMeshQuery, polygon, position, out nearest);
            return (status & PathQueryStatus.Success) != 0 ? new NavMeshLocation(nearest, polygon) : new NavMeshLocation();
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern NavMeshLocation MapLocationExp(IntPtr navMeshQuery, Vector3 position, Vector3 extents, int agentTypeID, int areaMask = NavMesh.AllAreas);
        ///<summary>Finds the closest point and <see cref="Experimental.AI.PolygonId" /> on the NavMesh for a given world position.</summary>
        ///<remarks>The search only applies to the specified type of NavMesh surface, for one or more desired area types and is limited to within the specified search area. It does not search for positions on &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html"&gt;NavMeshLinks&lt;/a&gt; or <see cref="OffMeshLink">OffMeshLinks</see>.
        ///
        ///Nearby NavMesh surfaces directly above or below the specified position are preferred. When there are none up or down within the specified search extents the surfaces closest sideways are sampled.</remarks>
        ///<param name="position">World position for which the closest point on the NavMesh needs to be found.</param>
        ///<param name="extents">Maximum distance, from the specified <c>position</c>, expanding along all three axes, within which NavMesh surfaces are searched.</param>
        ///<param name="agentTypeID">Identifier for the agent type whose NavMesh surfaces should be selected for this operation. The Humanoid agent type exists for all NavMeshes and has an ID of 0. Other agent types can be defined manually through the Editor. A separate NavMesh surface needs to be baked for each agent type.</param>
        ///<param name="areaMask">Bitmask used to represent areas of the NavMesh that should (value of 1) or shouldn't (values of 0) be sampled. This parameter is optional and defaults to <see cref="NavMesh.AllAreas" /> if unspecified.</param>
        ///<returns>An object with position and valid <see cref="Experimental.AI.PolygonId" />  - when a point on the NavMesh has been found.
        ///
        ///An invalid object - when no NavMesh surface with the desired features has been found within the search area.</returns>
        ///<seealso cref="NavMesh.SamplePosition" />
        ///<seealso cref="Experimental.AI.NavMeshQuery.IsValid" />
        ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</seealso>
        public NavMeshLocation MapLocation(Vector3 position, Vector3 extents, int agentTypeID, int areaMask = NavMesh.AllAreas)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return MapLocationExp(m_NavMeshQuery, position, extents, agentTypeID, areaMask);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe void MoveLocationsExp(IntPtr navMeshQuery, void* locations, void* targets, void* areaMasks, int count);
        ///<summary>Translates a series of NavMesh locations to other positions without losing contact with the surface.</summary>
        ///<remarks>Does the exact same thing as <see cref="Experimental.AI.NavMeshQuery.MoveLocation" /> only it acts sequentially on a batch of locations, given their respective targets and area filters. All three array parameters must have the same length.
        ///
        ///The results are returned in-place in the <c>locations</c> array.</remarks>
        ///<param name="locations">Array of positions to be moved across the NavMesh surface. At the end of the method call this array contains the resulting locations.</param>
        ///<param name="targets">World positions to be used as movement targets by the agent.</param>
        ///<param name="areaMasks">Filters for the areas which can be traversed during the movement to each of the locations.</param>
        ///<seealso cref="Experimental.AI.NavMeshQuery.MoveLocationsInSameAreas" />
        ///<seealso cref="Experimental.AI.NavMeshLocation" />
        public unsafe void MoveLocations(NativeSlice<NavMeshLocation> locations, NativeSlice<Vector3> targets, NativeSlice<int> areaMasks)
        {
            if (locations.Length != targets.Length || locations.Length != areaMasks.Length)
                throw new ArgumentException("locations.Length, targets.Length and areaMasks.Length must be equal");

            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            MoveLocationsExp(m_NavMeshQuery, locations.GetUnsafePtr(), targets.GetUnsafeReadOnlyPtr(), areaMasks.GetUnsafeReadOnlyPtr(), locations.Length);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe void MoveLocationsInSameAreasExp(IntPtr navMeshQuery, void* locations, void* targets, int count, int areaMask);
        ///<summary>Translates a series of NavMesh locations to other positions without losing contact with the surface, given one common area filter for all of them.</summary>
        ///<remarks>Does the exact same thing as <see cref="Experimental.AI.NavMeshQuery.MoveLocations" /> only it applies the same area filter to all the movements.</remarks>
        ///<param name="locations">Array of positions to be moved across the NavMesh surface. At the end of the method call this array contains the resulting locations.</param>
        ///<param name="targets">World positions you want the agent to reach when moving to each of the locations.</param>
        ///<param name="areaMask">Filters for the areas which can be traversed during the movement to each of the locations.</param>
        ///<seealso cref="Experimental.AI.NavMeshQuery.MoveLocation" />
        ///<seealso cref="Experimental.AI.NavMeshLocation" />
        public unsafe void MoveLocationsInSameAreas(NativeSlice<NavMeshLocation> locations, NativeSlice<Vector3> targets, int areaMask = NavMesh.AllAreas)
        {
            if (locations.Length != targets.Length)
                throw new ArgumentException("locations.Length and targets.Length must be equal");

            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            MoveLocationsInSameAreasExp(m_NavMeshQuery, locations.GetUnsafePtr(), targets.GetUnsafeReadOnlyPtr(), locations.Length, areaMask);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern NavMeshLocation MoveLocationExp(IntPtr navMeshQuery, NavMeshLocation location, Vector3 target, int areaMask);
        ///<summary>Translates a NavMesh location to another position without losing contact with the surface.</summary>
        ///<remarks>Returns the location on the NavMesh that is closest to the <c>target</c> position and that also has a continuous connection on the NavMesh surface through the allowed area types all the way to the start position specified by the <c>location</c> parameter. If the <c>target</c> position is outside the edges of the surface or of its allowed areas, a position at the edge is returned.
        ///
        ///The movement does not cross &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html"&gt;NavMeshLinks&lt;/a&gt; or <see cref="OffMeshLink">OffMeshLinks</see>.
        ///
        ///The result might not be accurate (the closest) if the <c>pathNodePoolSize</c> value in the NavMeshQuery initialization was not large enough to accommodate all the nodes that needed to be traversed in order to find a connection between <c>location.position</c> and <c>target</c>.</remarks>
        ///<param name="location">Position to be moved across the NavMesh surface.</param>
        ///<param name="target">World position you require the agent to move to.</param>
        ///<param name="areaMask">Bitmask with values of 1 set at the indices corresponding to areas that can be traversed, and with values of 0 for areas that should not be traversed. This parameter can be omitted, in which case it defaults to <see cref="NavMesh.AllAreas" />.</param>
        ///<returns>A new location on the NavMesh placed as closely as possible to the specified <c>target</c> position.
        ///
        ///The start <c>location</c> is returned when that start is inside an area which is not allowed by the <c>areaMask</c>.</returns>
        ///<seealso cref="Experimental.AI.NavMeshQuery.MoveLocations" />
        ///<seealso cref="Experimental.AI.NavMeshQuery.MoveLocationsInSameAreas" />
        ///<seealso href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html">Areas and Costs</seealso>
        public NavMeshLocation MoveLocation(NavMeshLocation location, Vector3 target, int areaMask = NavMesh.AllAreas)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return MoveLocationExp(m_NavMeshQuery, location, target, areaMask);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern bool GetPortalPointsExp(IntPtr navMeshQuery, PolygonId polygon, PolygonId neighbourPolygon, out Vector3 left, out Vector3 right);
        ///<summary>Obtains the end points of the line segment common to two adjacent NavMesh nodes.</summary>
        ///<remarks>For two polygons that are part of a NavMesh surface, this method returns the edge where both polygons meet. If the two polygons are in different NavMesh tiles the connected edges can be of different length or have different start and end positions from each other. If this happens the resulting separation edge is the overlapping part of the edges, which may be shorter than either of the individual edges.
        ///
        ///When one node is a link and the other is a polygon, the returned points are placed where the link intersects the polygon.
        ///
        ///The resulting positions are expressed in world space and can be transformed into a NavMesh's local space by the use of <see cref="Experimental.AI.NavMeshQuery.PolygonWorldToLocalMatrix" />.</remarks>
        ///<param name="polygon">First NavMesh node.</param>
        ///<param name="neighbourPolygon">Second NavMesh node.</param>
        ///<param name="left">One of the world points for the resulting separation edge which must be passed through when traversing between the two specified nodes. This point is the left side of the edge when traversing from the first node to the second.</param>
        ///<param name="right">One of the world points for the resulting separation edge which must be passed through when traversing between the two specified nodes. This point is the right side of the edge when traversing from the first node to the second.</param>
        ///<returns>
        ///  <c>True</c> if a connection exists between the two NavMesh nodes.
        ///<c>False</c> if no connection exists between the two NavMesh nodes.</returns>
        ///<seealso cref="Experimental.AI.NavMeshQuery.GetEdgesAndNeighbors" />
        public bool GetPortalPoints(PolygonId polygon, PolygonId neighbourPolygon, out Vector3 left, out Vector3 right)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return GetPortalPointsExp(m_NavMeshQuery, polygon, neighbourPolygon, out left, out right);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern Matrix4x4 PolygonLocalToWorldMatrixExp(IntPtr navMeshQuery, PolygonId polygon);
        ///<summary>Returns the transformation matrix of the NavMesh surface that contains the specified NavMesh node.</summary>
        ///<remarks>
        ///  <see cref="NavMeshData" /> surfaces have their transforms defined by the <c>position</c> and <c>rotation</c> values declared at the moment when they were baked with <see cref="NavMeshBuilder.BuildNavMeshData" />, or as part of a &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshSurface.html"&gt;NavMeshSurface&lt;/a&gt;, or by explicitly setting the values for <see cref="NavMeshData.position" /> and <see cref="NavMeshData.rotation" />.
        ///
        ///Custom transforms for <see cref="NavMeshDataInstance" />s can further be specified when they are created with explicit <c>position</c> and <c>rotation</c> values passed to the <see cref="NavMesh.AddNavMeshData" />(data, position, rotation) method.
        ///
        ///**Important:** This method does not return the position and orientation of a single NavMesh polygon. It returns the position of the surface that owns the polygon.
        ///
        ///**Known issue:** Identity matrix is returned instead of the actual transform for NavMeshLinks that have been instantiated with a call to <see cref="NavMesh.AddLink" />(link, position, rotation).</remarks>
        ///<param name="polygon">NavMesh node for which its owner's transform must be determined.</param>
        ///<returns>Transformation matrix for the surface owning the specified polygon.
        ///
        ///<see cref="Matrix4x4.identity" /> when the NavMesh node is a &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html"&gt;NavMeshLink&lt;/a&gt; or an <see cref="OffMeshLink" />.</returns>
        ///<seealso cref="Experimental.AI.NavMeshQuery.PolygonWorldToLocalMatrix" />
        ///<seealso cref="Experimental.AI.NavMeshQuery.GetPolygonType" />
        public Matrix4x4 PolygonLocalToWorldMatrix(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return PolygonLocalToWorldMatrixExp(m_NavMeshQuery, polygon);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern Matrix4x4 PolygonWorldToLocalMatrixExp(IntPtr navMeshQuery, PolygonId polygon);
        ///<summary>Returns the inverse transformation matrix of the NavMesh surface that contains the specified NavMesh node.</summary>
        ///<remarks>In contrast to <see cref="Experimental.AI.NavMeshQuery.PolygonLocalToWorldMatrix" /> the returned matrix can be used for transforming a world-coordinates position into the local coordinate system of the NavMesh surface owning the specified polygon.
        ///
        ///**Important:** This method does not return the inverse position and orientation of a single NavMesh polygon. It returns the inverse position and orientation of the surface that owns the polygon.
        ///
        ///**Known issue:** Identity matrix is returned instead of the actual inverse transform for NavMeshLinks that have been instantiated with a call to <see cref="NavMesh.AddLink" />(linkData, position, rotation).</remarks>
        ///<param name="polygon">NavMesh node for which its owner's inverse transform must be determined.</param>
        ///<returns>Inverse transformation matrix of the surface owning the specified polygon.
        ///
        ///<see cref="Matrix4x4.identity" /> when the NavMesh node is a &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html"&gt;NavMeshLink&lt;/a&gt; or an <see cref="OffMeshLink" />.</returns>
        ///<seealso cref="Experimental.AI.NavMeshQuery.GetPolygonType" />
        public Matrix4x4 PolygonWorldToLocalMatrix(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return PolygonWorldToLocalMatrixExp(m_NavMeshQuery, polygon);
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern NavMeshPolyTypes GetPolygonTypeExp(IntPtr navMeshQuery, PolygonId polygon);
        ///<summary>Returns whether the NavMesh node is a polygon or a link.</summary>
        ///<remarks>The type can be determined even after the specified node has become invalid in the query's NavMeshWorld.
        ///
        ///**Known issue, to be fixed:** If the query's <see cref="Experimental.AI.NavMeshWorld" /> is invalid for any reason, the method returns <see cref="Experimental.AI.NavMeshPolyTypes.OffMeshConnection" />.</remarks>
        ///<param name="polygon">Identifier of a node from a NavMesh surface or link.</param>
        ///<returns>
        ///  <c>Ground</c> when the node is a polygon on a NavMesh surface.
        ///
        ///<c>OffMeshConnection</c> when the node is a &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html"&gt;NavMeshLink&lt;/a&gt; or an <see cref="OffMeshLink" />.</returns>
        ///<seealso cref="Experimental.AI.NavMeshPolyTypes" />
        ///<seealso cref="Experimental.AI.NavMeshQuery.IsValid" />
        ///<seealso cref="Experimental.AI.NavMeshWorld.IsValid" />
        public NavMeshPolyTypes GetPolygonType(PolygonId polygon)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
            return GetPolygonTypeExp(m_NavMeshQuery, polygon);
        }

        //NavMeshStatus MoveAlongSurface(NavMeshLocation location, Vector3 targetPosition, int agentTypeID, int areaMask,
        //    out NavMeshLocation outputLocation, NativeArray<PolygonId> visitedBuffer, out int actualVisited);

        // Trace a ray between two points on the NavMesh.
        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe PathQueryStatus RaycastExp(IntPtr navMeshQuery, NavMeshLocation start, Vector3 targetPosition,
            int areaMask, void* costs, out NavMeshHit hit, void* path, out int pathCount, int maxPath);

        ///<summary>Trace a line between two points on the NavMesh.</summary>
        ///<remarks>This method is similar to <see cref="NavMesh.Raycast" />, both of them sharing the same underlying implementation.
        ///
        ///The properties that make this one different are:
        ///
        ///- it can be used in parallel [jobs](xref:JobSystem);
        ///
        ///- it returns status flags indicating whether the operation succeeded or failed;
        ///
        ///- the reported <c>hit.distance</c> is affected by the area costs;
        ///
        ///- the resulting <c>hit.position</c> is not adjusted on the vertical axis according to the &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/HeightMesh.html"&gt;HeightMesh&lt;/a&gt;, if that exists;
        ///
        ///- it has the variant described below that returns also the list of polygons through which the ray passes.
        ///
        ///
        ///
        ///The returned <c>hit.distance</c> represents the straight line between the start and termination point. It also takes into account the list of the provided area costs. It is the result of summing up all the distances covered by the ray over each separate area, multiplied by the cost of that respective area.
        ///
        ///
        ///
        ///First, the start location is verified to be valid in the NavMeshWorld, and the target point is mapped on the NavMesh. Then, a ray is traced from the start point towards the target. If the computation is successful, the <c>hit</c> data is filled with information about the furthest point that the ray has reached. This happens regardless of whether the path from the source to target has been obstructed.
        ///
        ///If the computation fails, the returned <c>hit</c> is filled with invalid data. Most notably, the <c>hit.distance</c> field gets the value <c>positiveInfinity</c>.
        ///
        ///If the raycast terminates on an outer edge, <c>hit.mask</c> is 0; otherwise it contains the area mask of the blocking polygon.
        ///
        ///You can use this function to check if an agent can walk unobstructed between two points on the NavMesh. For example, if your character has an evasive dodge move that needs space, you can shoot a ray from the character's location to multiple directions. This finds a spot where the character can dodge to.
        ///
        ///The <see cref="Experimental.AI.NavMeshQuery.Raycast" /> is different from the Physics raycast. The NavMeshQuery.Raycast can detect all kinds of navigation obstructions, for example holes in the ground. It can also climb up slopes, if the area is navigable.</remarks>
        ///<param name="hit">Holds the properties of the raycast resulting location.</param>
        ///<param name="start">The start location of the ray on the NavMesh. <c>start.polygon</c> must be of the type <see cref="Experimental.AI.NavMeshPolyTypes.Ground" />.</param>
        ///<param name="targetPosition">The desired end of the ray, in world coordinates.</param>
        ///<param name="areaMask">Bitmask that correlates index positions with area types.  The index goes from 0 to 31. In each relevant index position, you have to set the value to either 1 or 0. 1 indicates area types that the ray can pass through. 0 indicates area types that block the ray. This parameter is optional. If you leave out this parameter, it defaults to <see cref="NavMesh.AllAreas" />. To learn more, see: &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/AreasAndCosts.html"&gt;Areas and Costs&lt;/a&gt;.</param>
        ///<param name="costs">Array of custom cost values for all of the 32 possible area types. They act as multipliers to the distance reported by the ray when crossing various areas. This parameter is optional. If you omit it, it defaults to the area costs that you configured in the Project settings. To learn more, see <see cref="NavMesh.GetAreaCost" />.</param>
        ///<returns>
        ///  <c>Success</c> if the ray can be correctly traced using the provided arguments.
        ///
        ///<c>Failure</c> if the <c>start</c> location is not valid in the query's NavMeshWorld, or if it is inside an area not permitted by the <c>areaMask</c> argument, or when it is on a &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html"&gt;NavMeshLink&lt;/a&gt;/<see cref="OffMeshLink" />.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // TargetReachable
        ///using Unity.Collections;
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///using UnityEngine.Experimental.AI;
        ///
        ///public class TargetReachable : MonoBehaviour
        ///{
        ///    public Transform target;
        ///    NavMeshQuery m_NavQuery;
        ///    NavMeshHit m_Hit;
        ///
        ///    void OnEnable()
        ///    {
        ///        m_NavQuery = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent);
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        var startLocation = m_NavQuery.MapLocation(transform.position, Vector3.one, 0);
        ///        var status = m_NavQuery.Raycast(out m_Hit, startLocation, target.position, NavMesh.AllAreas, new NativeArray<float>());
        ///        if ((status & PathQueryStatus.Success) != 0)
        ///        {
        ///            Debug.DrawLine(transform.position, target.position, m_Hit.hit ? Color.red : Color.green);
        ///
        ///            if (m_Hit.hit)
        ///                Debug.DrawRay(m_Hit.position, Vector3.up, Color.red);
        ///        }
        ///    }
        ///
        ///    void OnDisable()
        ///    {
        ///        m_NavQuery.Dispose();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        public unsafe PathQueryStatus Raycast(out NavMeshHit hit, NavMeshLocation start, Vector3 targetPosition,
            int areaMask = NavMesh.AllAreas, NativeArray<float> costs = new NativeArray<float>())
        {
            const int kAreaCount = 32;
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

            if (costs.Length != 0)
            {
                if (costs.Length != kAreaCount)
                    throw new ArgumentException(
                        string.Format("The number of costs ({0}) must be exactly {1}, one for each possible area type.", costs.Length, kAreaCount), "costs");
            }
            int pathCount;
            void* costsPtr = costs.Length == kAreaCount ? costs.GetUnsafePtr() : null;
            var status = RaycastExp(m_NavMeshQuery, start, targetPosition, areaMask, costsPtr, out hit, null, out pathCount, 0);
            status &= ~PathQueryStatus.BufferTooSmall;
            return status;
        }

        ///<summary>Trace a line between two points on the NavMesh, and return the list of polygons through which it passed.</summary>
        ///<remarks>Even if the <c>path</c> buffer is too small it will still hold as many polygons as it has room for, starting from the ray's origin location.</remarks>
        ///<param name="hit">Holds the properties of the raycast resulting location.</param>
        ///<param name="path">A buffer that will be filled with the sequence of polygons through which the ray passes.</param>
        ///<param name="pathCount">The reported number of polygons through which the ray has passed, all stored in the <c>path</c> buffer. It will not be greater than <c>path.Length</c>.</param>
        ///<param name="start">The start location of the ray on the NavMesh. <c>start.polygon</c> must be of the type <see cref="Experimental.AI.NavMeshPolyTypes.Ground" />.</param>
        ///<param name="targetPosition">The desired end of the ray, in world coordinates.</param>
        ///<param name="areaMask">A bitfield that specifies which NavMesh areas can be traversed when the ray is traced. This parameter is optional. If you do not fill out this parameter, it defaults to <see cref="NavMesh.AllAreas" />.</param>
        ///<param name="costs">Cost multipliers that affect the distance reported by the ray over different area types. This parameter is optional. If you omit it, it defaults to the area costs that you configured in the Project settings.</param>
        ///<returns>
        ///  <c>Success</c> if the ray can be correctly traced using the provided arguments.
        ///
        ///<c>Failure</c> if the <c>start</c> location is not valid in the query's NavMeshWorld, or if it is inside an area not permitted by the <c>areaMask</c> argument, or when it is on a &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html"&gt;NavMeshLink&lt;/a&gt;/<see cref="OffMeshLink" />.
        ///
        ///<c>BufferTooSmall</c> is part of the returned flags when the provided <c>path</c> buffer is not large enough to hold all the polygons that the ray passed through.</returns>
        ///<example>
        ///  <code><![CDATA[
        /// // StraightPathFromRay
        ///using Unity.Collections;
        ///using UnityEngine;
        ///using UnityEngine.AI;
        ///using UnityEngine.Experimental.AI;
        ///
        ///public class StraightPathFromRay : MonoBehaviour
        ///{
        ///    public Transform target;
        ///    NavMeshQuery m_NavQuery;
        ///    NavMeshHit m_Hit;
        ///    NativeArray<PolygonId> m_Path;
        ///    int m_PathCount;
        ///
        ///    void OnEnable()
        ///    {
        ///        m_Path = new NativeArray<PolygonId>(3, Allocator.Persistent);
        ///        m_NavQuery = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent);
        ///    }
        ///
        ///    void Update()
        ///    {
        ///        var startLocation = m_NavQuery.MapLocation(transform.position, Vector3.one, 0);
        ///        PathQueryStatus status = m_NavQuery.Raycast(out m_Hit, m_Path, out m_PathCount, startLocation, target.position, NavMesh.AllAreas, new NativeArray<float>());
        ///        if ((status & PathQueryStatus.Success) != 0)
        ///        {
        ///            var bufferTooSmall = (status & PathQueryStatus.BufferTooSmall) != 0;
        ///            Debug.DrawLine(transform.position, m_Hit.position, bufferTooSmall ? Color.black : Color.green);
        ///
        ///            if (m_Hit.hit)
        ///                Debug.DrawRay(m_Hit.position, Vector3.up, Color.red);
        ///        }
        ///    }
        ///
        ///    void OnDisable()
        ///    {
        ///        m_NavQuery.Dispose();
        ///        m_Path.Dispose();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Experimental.AI.PolygonId" />
        public unsafe PathQueryStatus Raycast(out NavMeshHit hit, NativeSlice<PolygonId> path, out int pathCount,
            NavMeshLocation start, Vector3 targetPosition,
            int areaMask = NavMesh.AllAreas, NativeArray<float> costs = new NativeArray<float>())
        {
            const int kAreaCount = 32;
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

            if (costs.Length != 0)
            {
                if (costs.Length != kAreaCount)
                    throw new ArgumentException(
                        string.Format("The number of costs ({0}) must be exactly {1}, one for each possible area type.", costs.Length, kAreaCount), "costs");
            }
            void* costsPtr = costs.Length == kAreaCount ? costs.GetUnsafePtr() : null;
            void* pathPtr = path.Length > 0 ? path.GetUnsafePtr() : null;
            var maxPath = pathPtr != null ? path.Length : 0;
            var status = RaycastExp(m_NavMeshQuery, start, targetPosition, areaMask, costsPtr, out hit, pathPtr, out pathCount, maxPath);
            return status;
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern unsafe PathQueryStatus GetEdgesAndNeighborsExp(IntPtr navMeshQuery, PolygonId node, int maxVerts, int maxNei,
            void* verts, void* neighbors, void* edgeIndices,
            out int vertCount, out int neighborsCount);

        ///<summary>Retrieves the vertices of a given <c>node</c> and the <see cref="Experimental.AI.PolygonId">identifiers</see> of all the navigation nodes to which it connects.</summary>
        ///<remarks>A <see cref="Experimental.AI.NavMeshPolyTypes.Ground">polygon</see> of a NavMesh surface connects to all other neighboring polygons with which it shares an edge as well as all the <see cref="Experimental.AI.NavMeshPolyTypes.OffMeshConnection">OffMeshLinks or NavMeshLinks</see> that leave from anywhere on its surface. The polygon does not connect to other polygons with which it shares only a vertex.
        ///
        ///Each point returned in the <c>edgeVertices</c> array represents the start of a <c>node</c>'s edge and the subsequent element in the array is the end point of that edge. All vertices form a closed polygonal line. The last and first elements define the last edge.
        ///
        ///
        ///
        ///An <see cref="Experimental.AI.NavMeshPolyTypes.OffMeshConnection">off-mesh link</see> connects to all the NavMesh polygons that each end of the link intersects with, regardless of whether the link is unidirectional.
        ///
        ///For link nodes the returned <c>edgeVertices</c> array contains two pairs of points at indices [0]-[1] and [2]-[3] that define the end points of the start and end edges of the link, in this order. These are the world positions established at the moment when the link is instantiated in the NavMesh world. For nodes added through &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshLink.html"&gt;NavMesh Link&lt;/a&gt; or &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/OffMeshLink.html"&gt;OffMesh Link&lt;/a&gt; components the pairs contain the same value in both of their elements.
        ///
        ///
        ///
        ///A node from the <c>neighbors</c> array lies at the edge returned in <c>edgeIndices</c> at the same index.
        ///
        ///If both the given <c>node</c> and its neighbor are NavMesh <see cref="Experimental.AI.NavMeshPolyTypes.Ground">polygons</see>, then the corresponding <c>edgeIndices</c> value represents the index of the polygon edge that leads from <c>node</c> to the neighbor.  E.g. <c>edgeVertices[edgeIndices[2]]</c> represents the start point of the edge that is common between <c>node</c> and the <c>neighbors[2]</c> node, and <c>edgeVertices[edgeIndices[2] + 1]</c> is the end point of that edge.
        ///
        ///A NavMesh polygon can have a maximum of 6 edges. This means the <c>edgeIndices</c> value corresponding to a polygon-polygon connection is between 0 and 5, inclusive. An edge usually connects only the two polygons that share it, but edges that sit at a tile border can connect one polygon in the first tile to multiple polygons in the second tile. In this case, <c>edgeIndices</c> report the same value for all of those neighbors.
        ///
        ///If either the given <c>node</c> or the <c>neighbor</c> is a <see cref="Experimental.AI.NavMeshPolyTypes.OffMeshConnection">link</see>, then the corresponding <c>edgeIndices</c> value represents the side on the link where the connection is made: 0 for <see cref="NavMeshLinkData.startPosition">start</see> and 2 for <see cref="NavMeshLinkData.endPosition">end</see>. When the <c>node</c> is a polygon and the <c>neighbor</c> is a link the value acts only as information about the side of the link where the two nodes connect and should not be used as an index in the <c>edgeVertices</c> array.
        ///
        ///When the <c>neighbors</c> and <c>edgeIndices</c> buffers both have positive capacity, they must be the same size, otherwise you will encounter an <c>ArgumentException</c> when this method executes in the Editor.
        ///
        ///
        ///
        ///You can set any of the buffers to have zero capacity for the cases when you do not need the results.
        ///
        ///
        ///
        ///The returned <c>verticesCount</c> and <c>neighborsCount</c> values express the number of elements that comprise the result in the output buffers of sufficient size. Buffers that are not large enough are still filled with valid nodes up to their full capacity.
        ///
        ///
        ///
        ///The five result parameters (<c>edgeVertices</c>, <c>neighbors</c>, <c>edgeIndices</c>, <c>verticesCount</c> and <c>neighborsCount</c>) do not act as input and do not change the internal navigation data in any way. Unity only modifies them in the case when the operation returns a <c>Success</c> status.</remarks>
        ///<param name="node">Identifier of a node from a NavMesh surface, &lt;a href="https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/api/Unity.AI.Navigation.NavMeshLink.html"&gt;NavMeshLink&lt;/a&gt; or <see cref="OffMeshLink" /> for which the vertices and neighbors need to be retrieved.</param>
        ///<param name="edgeVertices">The result buffer that contains the world positions describing the geometry of the input navigation <c>node</c>. It can have zero capacity.
        ///
        ///<see cref="Experimental.AI.NavMeshPolyTypes.Ground">Polygonal</see> nodes of the NavMesh have a minimum of 3 and a maximum of 6 vertices.
        ///
        ///<see cref="Experimental.AI.NavMeshPolyTypes.OffMeshConnection">OffMeshConnection</see> nodes are always represented by 4 vertices, regardless of their width.</param>
        ///<param name="neighbors">The result buffer that holds the identifiers of all the navigation nodes immediately reachable from the given <c>node</c>. It can have zero capacity.</param>
        ///<param name="edgeIndices">The helper result buffer that maps each neighbor node to an edge of the given <c>node</c>.  It can have zero capacity.
        ///
        ///The index of an element in <c>edgeIndices</c> is also an index in the <c>neighbors</c> array and the value of that <c>edgeIndices</c> element is an index in the <c>edgeVertices</c> array.</param>
        ///<param name="verticesCount">The total number of vertices that describe the geometry of the input <c>node</c>. This is independent of the capacity of the <c>vertices</c> result buffer.</param>
        ///<param name="neighborsCount">The total number of navigation nodes the input <c>node</c> connects to. This is independent of the capacity of the result buffers (<c>neighbors</c> and <c>edgeIndices</c>).</param>
        ///<returns>
        ///  <c>Success</c> if Unity can evaluate the neighbors and vertices of the specified node, regardless of the result. The <c>verticesCount</c> and <c>neighborsCount</c> are always valid in this case.
        ///
        ///<c>Failure</c> if Unity can not use the <c>node</c> identifier to retrieve the neighbors or geometry information. Unity does not modify any of the five result parameters (<c>edgeVertices</c>, <c>neighbors</c>, <c>edgeIndices</c>, <c>verticesCount</c> or <c>neighborsCount</c>) in this case.
        ///
        ///<c>InvalidParam</c> is part of the returned flags if the specified navigation node is not <see cref="Experimental.AI.NavMeshQuery.IsValid">valid</see> in the query's NavMeshWorld.
        ///
        ///<c>BufferTooSmall</c> is part of the PathQueryStatus flags, that Unity returns from this function, when any of the result buffers you provide are not large enough to hold all the neighbor nodes the input <c>node</c> connects to or all of its edge vertices.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using Unity.Collections;
        ///using UnityEngine;
        ///using UnityEngine.Experimental.AI;
        ///
        ///public class NavMeshNodeEdgesDrawer : MonoBehaviour
        ///{
        ///    void Update()
        ///    {
        ///        var query = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Temp);
        ///        var vertices = new NativeArray<Vector3>(6, Allocator.Temp);
        ///        var neighbors = new NativeArray<PolygonId>(10, Allocator.Temp);
        ///        var edgeIndices = new NativeArray<byte>(neighbors.Length, Allocator.Temp);
        ///        int totalVertices;
        ///        int totalNeighbors;
        ///
        ///        var location = query.MapLocation(transform.position, Vector3.one, 0);
        ///
        ///        var queryStatus = query.GetEdgesAndNeighbors(
        ///            location.polygon, vertices, neighbors, edgeIndices,
        ///            out totalVertices, out totalNeighbors);
        ///
        ///        var color = (queryStatus & PathQueryStatus.Success) != 0 ? Color.green : Color.red;
        ///        Debug.DrawLine(transform.position - Vector3.up, transform.position + Vector3.up, color);
        ///
        ///        for (int i = 0, j = totalVertices - 1; i < totalVertices; j = i++)
        ///        {
        ///            Debug.DrawLine(vertices[i], vertices[j], Color.grey);
        ///        }
        ///
        ///        for (var i = 0; i < totalNeighbors; i++)
        ///        {
        ///            if (query.GetPolygonType(neighbors[i]) == NavMeshPolyTypes.OffMeshConnection)
        ///            {
        ///                // The link neighbor is not connected through any of the polygon's edges.
        ///                // Call GetEdgesAndNeighbors() on this specific neighbor in order to retrieve its edges.
        ///                continue;
        ///            }
        ///
        ///            var start = edgeIndices[i];
        ///            var end = (start + 1) % totalVertices;
        ///            var neighborColor = Color.Lerp(Color.yellow, Color.magenta, 1f * start / (totalVertices - 1));
        ///            Debug.DrawLine(vertices[start], vertices[end], neighborColor);
        ///        }
        ///
        ///        query.Dispose();
        ///        vertices.Dispose();
        ///        neighbors.Dispose();
        ///        edgeIndices.Dispose();
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Experimental.AI.NavMeshQuery.GetPolygonType" />
        ///<seealso cref="Experimental.AI.NavMeshQuery.GetPortalPoints" />
        public unsafe PathQueryStatus GetEdgesAndNeighbors(PolygonId node,
            NativeSlice<Vector3> edgeVertices, NativeSlice<PolygonId> neighbors, NativeSlice<byte> edgeIndices,
            out int verticesCount, out int neighborsCount)
        {
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);

            if (edgeIndices.Length != neighbors.Length && neighbors.Length > 0 && edgeIndices.Length > 0)
            {
                throw new ArgumentException($"The length of the {nameof(edgeIndices)} buffer ({edgeIndices.Length}) " +
                    $"needs to be the same as that of the {nameof(neighbors)} buffer ({neighbors.Length}) " +
                    "because the elements from the two arrays will pair up at the same index.");
            }
            void* vertPtr = edgeVertices.Length > 0 ? edgeVertices.GetUnsafePtr() : null;
            void* neiPtr = neighbors.Length > 0 ? neighbors.GetUnsafePtr() : null;
            void* edgesPtr = edgeIndices.Length > 0 ? edgeIndices.GetUnsafePtr() : null;
            var maxVertices = edgeVertices.Length;
            var maxNeighbors = neighbors.Length > 0 ? neighbors.Length : edgeIndices.Length;
            var status = GetEdgesAndNeighborsExp(m_NavMeshQuery, node, maxVertices, maxNeighbors,
                vertPtr, neiPtr, edgesPtr,
                out verticesCount, out neighborsCount);
            return status;
        }

        //// Polygon Queries
        //public NavMeshPolyData GetPolygon(PolygonId poly);
        //public void GetPolygon(NativeArray<PolygonId> polygonIDs, NativeArray<NavMeshPolyData> polygons);
        //public void GetPolygons(MappedPosition position, float distance, NativeList<NavMeshPolyData> polygons);

        //public static void LocalizePolygonIndices(NativeArray<NavMeshPolyData> polygons);

        //// Segments
        //public NativeArray<NavMeshSegment> FindBoundarySegments (MappedPosition position, float distance, Allocator allocator);

        //// Voxel rasterize
        //public void Rasterize (MappedPosition position, Quaternion orientation, float cellWidth, float cellHeight, NativeArray2D<bool> grid);

        //// DetailMesh queries
        //void ProjectToDetailMesh(NativeArray<MappedPosition> positions, NativeArray<Vector3> outputPositions);
    }
}

