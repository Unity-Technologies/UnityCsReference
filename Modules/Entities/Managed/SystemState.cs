// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using Debug = Unity.Scripting.LowLevel.Debug;
using ProfilerMarker = Unity.Entities.EntitiesProfilerMarker;

namespace Unity.Entities
{
    /// <summary>
    /// Contains raw entity system state. Used by unmanaged systems (ISystem) as well as managed systems behind the scenes.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    [DebuggerTypeProxy(typeof(SystemState_))]
    [DebuggerDisplay("{SystemState_.GetName(m_Handle, World)}")]
    public unsafe ref struct SystemState
    {
        struct SystemStateKey { }
        readonly static SharedStatic<IntPtr> s_SystemIdCellPtr = SharedStatic<IntPtr>.GetOrCreate<SystemStateKey>();
        [NoAutoStaticsCleanup] // System IDs stay unique across code reloads.
        static int                             ms_SystemIDAllocator;

#region Always accessed during every system OnUpdate (Hot)

        // For unmanaged systems, points to user struct that was allocated to front this system state
        internal void*                         m_SystemPtr;                     // 8
        internal JobHandle                     m_JobHandle;                     // 20 (+12)
        uint                                   m_Flags;                         // 24
        internal ComponentDependencyManager*   m_DependencyManager;             // 32
        internal EntityComponentStore*         m_EntityComponentStore;          // 40
        internal uint                          m_LastSystemVersion;             // 44
        internal ProfilerMarker                m_ProfilerMarker;
        internal ProfilerMarker                m_ProfilerMarkerBurst;
        private void*                          m_Self;
        private void* This
        {
            get
            {
                fixed (void* t = &m_EntityQueries)
                {
                    return t;
                }
            }
        }
        private void CheckThis()
        {
            if (m_Self != This)
                throw new InvalidOperationException("This is a value copy of the system state that will lead to memory corruption in the original system state data");
        }


        #endregion

        #region Rarely accessed during System.OnUpdate depending on what they do (Cold)
        internal SystemTypeIndex   m_SystemTypeIndex;

        internal int                           m_SystemID;

        internal EntityManager                 m_EntityManager;

        internal UnsafeList<TypeIndex>         m_JobDependencyForReadingSystems;
        internal UnsafeList<TypeIndex>         m_JobDependencyForWritingSystems;

        UnsafeList<EntityQuery>                m_EntityQueries;
        UnsafeList<EntityQuery>                m_RequiredEntityQueries;

        internal WorldUnmanaged                m_WorldUnmanaged;

        // a handle to this system state that can be used as a stable, safe reference but must be resolved via the
        // associated world.
        internal SystemHandle           m_Handle;

        int                                    m_UnmanagedMetaIndex;
        internal GCHandle                      m_World;
        // used by managed systems to store a reference to the actual system
        internal GCHandle                      m_ManagedSystem;

        NativeText.ReadOnly                    m_DebugName;
#endregion


        private const uint kEnabledMask = 0x1;
        private const uint kRequireMatchingQueriesForUpdateMask = 0x2;
        private const uint kPreviouslyEnabledMask = 0x4;
        private const uint kNeedToGetDependencyFromSafetyManagerMask = 0x8;
        private const uint kIsExecutingISystemOnUpdate = 0x10;
        private const uint kDidWarnIsExecutingISystemOnUpdate = 0x20;
        private const uint kWasUsingBurstProfilerMarker = 0x40;

        private void SetFlag(uint mask, bool value) => m_Flags = value ? m_Flags | mask : m_Flags & ~mask;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void DisableIsExecutingOnUpdate()
        {
            m_Flags &= ~kIsExecutingISystemOnUpdate;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void EnableIsExecutingISystemOnUpdate()
        {
            m_Flags |= kIsExecutingISystemOnUpdate;
        }

        internal void SetWasUsingBurstProfilerMarker(bool enabled)
        {
            SetFlag(kWasUsingBurstProfilerMarker, enabled);
        }

        internal bool WasUsingBurstProfilerMarker()
        {
            return (m_Flags & kWasUsingBurstProfilerMarker) != 0;
        }

        /// <summary>
        /// Return the unmanaged type index of the system (>= 0 for ISystem-type systems), or -1 for managed systems.
        /// </summary>
        public int UnmanagedMetaIndex => m_UnmanagedMetaIndex;

        /// <summary>
        /// Return a debug name for unmanaged systems.
        /// </summary>
        public NativeText.ReadOnly DebugName => m_DebugName;

        internal ref UnsafeList<EntityQuery> EntityQueries
        {
            get
            {
                fixed(void* ptr = &m_EntityQueries)
                {
                    return ref UnsafeUtility.AsRef<UnsafeList<EntityQuery>>(ptr);
                }
            }
        }

        internal ref UnsafeList<EntityQuery> RequiredEntityQueries
        {
            get
            {
                fixed(void* ptr = &m_RequiredEntityQueries)
                {
                    return ref UnsafeUtility.AsRef<UnsafeList<EntityQuery>>(ptr);
                }
            }
        }

        /// <summary>
        /// Controls whether this system executes when its OnUpdate function is called.
        /// </summary>
        /// <value>True, if the system is enabled.</value>
        /// <remarks>The Enabled property is intended for debugging so that you can easily turn on and off systems
        /// from the Entity Debugger window. A system with Enabled set to false will not update, even if its
        /// <see cref="ShouldRunSystem"/> function returns true.</remarks>
        public bool Enabled { get => (m_Flags & kEnabledMask) != 0; set => SetFlag(kEnabledMask, value); }

        internal bool RequireMatchingQueriesForUpdate { get => (m_Flags & kRequireMatchingQueriesForUpdateMask) != 0; set => SetFlag(kRequireMatchingQueriesForUpdateMask, value); }
        internal bool PreviouslyEnabled { get => (m_Flags & kPreviouslyEnabledMask) != 0; set => SetFlag(kPreviouslyEnabledMask, value); }
        private bool NeedToGetDependencyFromSafetyManager { get => (m_Flags & kNeedToGetDependencyFromSafetyManagerMask) != 0; set => SetFlag(kNeedToGetDependencyFromSafetyManagerMask, value); }

        /// <summary>
        /// The current change version number in this <see cref="World"/>.
        /// </summary>
        /// <remarks>The system updates the component version numbers inside any <see cref="ArchetypeChunk"/> instances
        /// that this system accesses with write permissions to this value.</remarks>
        public uint GlobalSystemVersion => m_EntityComponentStore->GlobalSystemVersion;

        /// <summary>
        /// The current version of this system.
        /// </summary>
        /// <remarks>
        /// LastSystemVersion is updated to match the <see cref="GlobalSystemVersion"/> whenever a system runs.
        ///
        /// When you use <seealso cref="EntityQuery.SetChangedVersionFilter(ComponentType)"/>
        /// or <seealso cref="ArchetypeChunk.DidChange"/>, LastSystemVersion provides the basis for determining
        /// whether a component could have changed since the last time the system ran.
        ///
        /// When a system accesses a component and has write permission, it updates the change version of that component
        /// type to the current value of LastSystemVersion. The system updates the component type's version whether or not
        /// it actually modifies data in any instances of the component type -- this is one reason why you should
        /// specify read-only access to components whenever possible.
        ///
        /// For efficiency, ECS tracks the change version of component types by chunks, not by individual entities. If a system
        /// updates the component of a given type for any entity in a chunk, then ECS assumes that the components of all
        /// entities in that chunk could have been changed. Change filtering allows you to save processing time by
        /// skipping all entities in an unchanged chunk, but does not support skipping individual entities in a chunk
        /// that does contain changes.
        /// </remarks>
        /// <value>The <see cref="GlobalSystemVersion"/> the last time this system ran.</value>
        public uint LastSystemVersion => m_LastSystemVersion;

        /// <summary>
        /// The EntityManager object of the <see cref="World"/> in which this system exists.
        /// </summary>
        /// <value>The EntityManager for this system.</value>
        public EntityManager EntityManager => m_EntityManager;

        /// <summary>
        /// The World in which this system exists.
        /// </summary>
        /// <value>The World of this system.</value>
        [ExcludeFromBurstCompatTesting("Returns managed World")]
        public World World => (World)m_World.Target;

        /// <summary>
        /// The unmanaged portion of the world in which this system exists.
        /// </summary>
        /// <value>The unmanaged world of this system.</value>
        public WorldUnmanaged WorldUnmanaged => m_WorldUnmanaged;

        /// <summary>
        /// Retrieve the world update allocator of the World in which this system exists.
        /// </summary>
        /// <value>The Allocator retrieved.</value>
        /// <remarks>Behind the world update allocator are double rewindable allocators, and the two allocators
        /// are switched in each world update.  Therefore user cannot cache the world update allocator.</remarks>
        public Allocator WorldUpdateAllocator => m_WorldUnmanaged.UpdateAllocator.ToAllocator;

        /// <summary>
        /// Retrieve the world update rewindable allocator of the World in which this system exists.
        /// </summary>
        /// <value>The RewindableAllocator retrieved.</value>
        internal ref RewindableAllocator WorldRewindableAllocator => ref m_WorldUnmanaged.UpdateAllocator;

        /// <summary>
        /// The untyped system's handle.
        /// </summary>
        public SystemHandle SystemHandle => m_Handle;

        /// <summary> Obsolete. Use <see cref="SystemHandle"/> instead.</summary>
        [Obsolete("SystemHandle has been renamed to SystemHandle. (UnityUpgradable) -> SystemHandle", true)]
        public SystemHandle SystemHandleUntyped => m_Handle;

        /// <summary>
        /// Obsolete. The current Time data for this system's world.
        /// </summary>
        /// <remarks> **Obsolete.** Use <see cref="SystemAPI.Time"/> or <see cref="WorldUnmanaged.Time"/> instead.</remarks>
        [Obsolete("Time has been deprecated as duplicate. Use SystemAPI.Time or WorldUnmanaged.Time instead (RemovedAfter 2023-08-08)", true)]
        public ref readonly TimeData Time => ref WorldUnmanaged.Time;

        [ExcludeFromBurstCompatTesting("Returns managed system")]
        internal ComponentSystemBase ManagedSystem => m_ManagedSystem.IsAllocated ? m_ManagedSystem.Target as ComponentSystemBase : null;

        // Managed systems call this function to initialize their backing system state
        [ExcludeFromBurstCompatTesting("Takes managed World")]
        internal void InitManaged(World world, SystemHandle handle, Type managedType, ComponentSystemBase system)
        {
            m_UnmanagedMetaIndex = -1;
            m_ManagedSystem = GCHandle.Alloc(system, GCHandleType.Normal);
            var systemTypeIndex = TypeManager.GetSystemTypeIndex(managedType);

            m_DebugName = TypeManager.GetSystemName(systemTypeIndex);

            CommonInit(world, handle, systemTypeIndex);

            if (managedType != null)
            {
                var requireAttributeType = typeof(RequireMatchingQueriesForUpdateAttribute);
                var attrs = TypeManager.GetSystemAttributes(managedType, requireAttributeType);
                if (attrs.Length > 0)
                    RequireMatchingQueriesForUpdate = true;
            }

            m_ProfilerMarker = new EntitiesProfilerMarker($"{world.Name} {m_DebugName}");
            m_ProfilerMarkerBurst = default;
        }


        // Initialization common to managed and unmanaged systems
        private void CommonInit(World world, SystemHandle handle, int systemTypeIndex)
        {
            Enabled = true;
            m_SystemTypeIndex = systemTypeIndex;
            m_SystemID = ++ms_SystemIDAllocator;
            m_World = GCHandle.Alloc(world);
            m_WorldUnmanaged = world.Unmanaged;
            m_EntityManager = world.EntityManager;
            m_EntityComponentStore = m_EntityManager.GetCheckedEntityDataAccess()->EntityComponentStore;
            m_DependencyManager = m_EntityManager.GetCheckedEntityDataAccess()->DependencyManager;
            m_Handle = handle;

            EntityQueries = new UnsafeList<EntityQuery>(0, Allocator.Persistent);
            RequiredEntityQueries = new UnsafeList<EntityQuery>(0, Allocator.Persistent);

            m_JobDependencyForReadingSystems = new UnsafeList<TypeIndex>(0, Allocator.Persistent);
            m_JobDependencyForWritingSystems = new UnsafeList<TypeIndex>(0, Allocator.Persistent);

            RequireMatchingQueriesForUpdate = false;
        }

        // Unmanaged systems call this function to initialize their backing system state
        [ExcludeFromBurstCompatTesting("Takes managed World")]
        internal void InitUnmanaged(World world, SystemHandle handle, int unmanagedMetaIndex, void* systemptr)
        {
            Enabled = true;
            m_UnmanagedMetaIndex = unmanagedMetaIndex;
            m_SystemPtr = systemptr;
            m_Self = This;
            var typeIndex = TypeManager.GetSystemTypeIndex(SystemBaseRegistry.GetStructType(unmanagedMetaIndex));
            CommonInit(world, handle, typeIndex);

            m_DebugName = TypeManager.GetSystemName(typeIndex);

            var attrs = TypeManager.GetSystemAttributes(typeIndex,
                TypeManager.SystemAttributeKind.RequireMatchingQueriesForUpdate);
            if (attrs.Length > 0)
                RequireMatchingQueriesForUpdate = true;

            var profilerName = GetProfilerMarkerName(world);
            m_ProfilerMarker = new EntitiesProfilerMarker(profilerName);
            m_ProfilerMarkerBurst = new EntitiesProfilerMarker(3, profilerName);
        }

        [ExcludeFromBurstCompatTesting("Returns managed string")]
        internal string GetProfilerMarkerName(World world)
        {
            return $"{world.Name} {m_DebugName}";
        }

        internal void Dispose()
        {
            DisposeQueries(ref EntityQueries);
            DisposeQueries(ref RequiredEntityQueries);

            EntityQueries.Dispose();
            EntityQueries = default;

            RequiredEntityQueries.Dispose();
            RequiredEntityQueries = default;

            if (m_World.IsAllocated)
            {
                m_World.Free();
            }

            if (m_ManagedSystem.IsAllocated)
            {
                m_ManagedSystem.Free();
            }

            m_JobDependencyForReadingSystems.Dispose();
            m_JobDependencyForWritingSystems.Dispose();
        }

        private void DisposeQueries(ref UnsafeList<EntityQuery> queries)
        {
            for (var i = 0; i < queries.Length; ++i)
            {
                var query = queries[i];

                if (m_EntityManager.IsQueryValid(query))
                {
                    query._GetImpl()->_DisallowDisposing = 0;
                    query.Dispose();
                }
            }
        }

        /// <summary>
        /// The ECS-related data dependencies of the system.
        /// </summary>
        /// <remarks>
        /// Before <see cref="SystemBase.OnUpdate"/> or <see cref="ISystem.OnUpdate(ref SystemState)"/>, the Dependency property represents the combined job handles of any job that
        /// writes to the same components that the current system reads -- or reads the same components that the current
        /// system writes to.
        ///
        /// The [JobHandle] objects of any jobs scheduled with explicit dependencies are not combined with
        /// the system’s Dependency property. You must set the Dependency property manually to make sure
        /// that later systems receive the correct job dependencies.
        ///
        /// [JobHandle]: https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html
        /// </remarks>
        /// <seealso cref="SystemBase.Dependency"/>
        public JobHandle Dependency
        {
            get
            {
                if (NeedToGetDependencyFromSafetyManager)
                {
                    var depMgr = m_DependencyManager;
                    NeedToGetDependencyFromSafetyManager = false;
                    m_JobHandle = depMgr->GetDependency(m_JobDependencyForReadingSystems.Ptr,
                        m_JobDependencyForReadingSystems.Length, m_JobDependencyForWritingSystems.Ptr,
                        m_JobDependencyForWritingSystems.Length, clearReadFencesAfterCombining:false);
                }

                return m_JobHandle;
            }
            set
            {
                NeedToGetDependencyFromSafetyManager = false;
                m_JobHandle = value;
            }
        }

        /// <summary>
        /// Completes combined job handles registered with this system. See <see cref="Dependency"/> for
        /// more information.
        /// </summary>
        public void CompleteDependency()
        {
            // Previous frame job
            m_JobHandle.Complete();

            // We need to get more job handles from other systems
            if (NeedToGetDependencyFromSafetyManager)
            {
                NeedToGetDependencyFromSafetyManager = false;
                CompleteDependencyInternal();
            }
        }

        internal void CompleteDependencyInternal()
        {
            m_DependencyManager->CompleteDependenciesNoChecks(m_JobDependencyForReadingSystems.Ptr,
                m_JobDependencyForReadingSystems.Length, m_JobDependencyForWritingSystems.Ptr,
                m_JobDependencyForWritingSystems.Length);
        }

        internal void BeforeUpdateVersioning()
        {
            m_EntityComponentStore->IncrementGlobalSystemVersion(in m_Handle);

            ref var qs = ref EntityQueries;
            for (int i = 0; i < qs.Length; ++i)
            {
                qs[i].SetChangedFilterRequiredVersion(m_LastSystemVersion);
            }
        }

        internal void AfterUpdateVersioning()
        {
            // Store global system version before incrementing it again
            m_LastSystemVersion = m_EntityComponentStore->GlobalSystemVersion;

            // Passing 'default' to mean that we are no longer within an executing system
            m_EntityComponentStore->IncrementGlobalSystemVersion(default);
        }

        [Conditional("UNITY_ENTITIES_RUNTIME_TOOLING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void BeforeUpdateResetRunTracker()
        {
        }

        [Conditional("UNITY_ENTITIES_RUNTIME_TOOLING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void BeforeUpdateRecordTiming()
        {
        }

        [Conditional("UNITY_ENTITIES_RUNTIME_TOOLING")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AfterUpdateRecordTiming()
        {
        }

        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static void InitSystemIdCell()
        {
            if (s_SystemIdCellPtr.Data == IntPtr.Zero)
            {
                s_SystemIdCellPtr.Data = JobsUtility.GetSystemIdCellPtr();
                SetCurrentSystemIdForJobDebugger(0);
            }
        }

        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal static int SetCurrentSystemIdForJobDebugger(int id)
        {
            var ptr = *(int**)s_SystemIdCellPtr.UnsafeDataPointer;
            var old = *ptr;
            *ptr = id;
            return old;
        }


        [ExcludeFromBurstCompatTesting("not present in players")]
        internal static SystemState* GetCurrentSystemFromJobDebugger()
        {
            var ptr = *(int**)s_SystemIdCellPtr.UnsafeDataPointer;
            if (ptr == null)
                return null;
            return World.FindSystemStateForId(*ptr);
        }

        [ExcludeFromBurstCompatTesting("not present in players")]
        internal static int GetCurrentSystemIDFromJobDebugger()
        {
            var ptr = *(int**)s_SystemIdCellPtr.UnsafeDataPointer;
            if (ptr == null)
                return 0;
            return *ptr;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void CheckOnUpdate_Query()
        {
            if (Burst.CompilerServices.Hint.Unlikely((m_Flags & (kIsExecutingISystemOnUpdate | kDidWarnIsExecutingISystemOnUpdate)) == kIsExecutingISystemOnUpdate))
            {
                Scripting.LowLevel.Debug.LogWarning($"'{new FixedString512Bytes(m_DebugName)}' creates a query during OnUpdate. Please create queries in OnCreate and store them in the system for use in OnUpdate instead. This is significantly faster.");
                m_Flags |= kDidWarnIsExecutingISystemOnUpdate;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void CheckOnUpdate_Handle()
        {
            if (Burst.CompilerServices.Hint.Unlikely((m_Flags & (kIsExecutingISystemOnUpdate | kDidWarnIsExecutingISystemOnUpdate)) == kIsExecutingISystemOnUpdate))
            {
                Scripting.LowLevel.Debug.LogWarning($"'{new FixedString512Bytes(m_DebugName)}' creates a type handle during OnUpdate. Please create the type handle in OnCreate instead and use type `_MyHandle.Update(ref systemState);` in OnUpdate to keep it up to date instead. This is significantly faster.");
                m_Flags |= kDidWarnIsExecutingISystemOnUpdate;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        void CheckOnUpdate_Lookup()
        {
           if (Burst.CompilerServices.Hint.Unlikely((m_Flags & (kIsExecutingISystemOnUpdate | kDidWarnIsExecutingISystemOnUpdate)) == kIsExecutingISystemOnUpdate))
            {
                Scripting.LowLevel.Debug.LogWarning($"'{new FixedString512Bytes(m_DebugName)}' creates a Lookup object (e.g. ComponentLookup) during OnUpdate. Please create this object in OnCreate instead and use type `_MyLookup.Update(ref systemState);` in OnUpdate to keep it up to date instead. This is significantly faster.");
                m_Flags |= kDidWarnIsExecutingISystemOnUpdate;
            }
        }

        internal void BeforeOnUpdate()
        {
            BeforeUpdateVersioning();

            // We need to wait on all previous frame dependencies, otherwise it is possible that we create infinitely long dependency chains
            // without anyone ever waiting on it
            m_JobHandle.Complete();
            NeedToGetDependencyFromSafetyManager = true;
        }

        internal void AfterOnUpdate()
        {
            AfterUpdateVersioning();
            // If m_JobHandle isn't the default we have scheduled some jobs (and they haven't been sync'd),
            // and need to batch them up or register them.
            // This is a big optimization if we only Run methods on main thread...
            if (!m_JobHandle.Equals(default(JobHandle)))
            {
                JobHandle.ScheduleBatchedJobs();
                m_JobHandle = m_DependencyManager->AddDependency(m_JobDependencyForReadingSystems.Ptr,
                    m_JobDependencyForReadingSystems.Length, m_JobDependencyForWritingSystems.Ptr,
                    m_JobDependencyForWritingSystems.Length, m_JobHandle);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        internal void LogSafetyErrors()
        {
            if (!JobsUtility.JobDebuggerEnabled)
                return;

            var depMgr = m_DependencyManager;
            if (SystemDependencySafetyUtility.FindSystemSchedulingErrors(m_SystemID, ref m_JobDependencyForReadingSystems, ref m_JobDependencyForWritingSystems, depMgr, out var details))
            {
                bool logged = false;
                LogSafetyDetails(details, ref logged);

                if (!logged)
                {
                    Debug.LogError("A system dependency error was detected but could not be logged accurately from Burst. Disable Burst compilation to see full error message.");
                }
            }
        }

        [BurstDiscard]
        private static void LogSafetyDetails(in SystemDependencySafetyUtility.SafetyErrorDetails details, ref bool logged)
        {
            Debug.LogError(details.FormatToString());
            logged = true;
        }


        /// <summary>
        /// Reports whether this system satisfies the criteria to update. This function is used
        /// internally to determine whether the system's OnUpdate function can be skipped.
        /// </summary>
        /// <remarks>
        /// <p>
        /// By default, systems will invoke OnUpdate every frame.
        /// </p>
        /// <p>
        /// If a system calls <see cref="RequireForUpdate{T}"/> or <see cref="RequireForUpdate(EntityQuery)"/>
        /// in OnCreate, it will only update if all of its required components exist and
        /// required queries match existing chunks. This check uses [IsEmptyIgnoreFilter], so the queries may
        /// still be empty if they use filters or [Enableable Components].
        /// </p>
        /// <p>
        /// If a system has the <see cref="RequireMatchingQueriesForUpdateAttribute"/> it will
        /// update if any EntityQuery it uses match existing chunks. This check also uses [IsEmptyIgnoreFilter],
        /// so all queries may still be empty if they use filters or [Enableable Components].
        /// </p>
        /// <p>
        /// Note: Other factors might prevent a system from updating, even if this method returns
        /// true. For example, a system will not be updated if its [Enabled] property is false.
        /// </p>
        ///
        /// [IsEmptyIgnoreFilter]: xref:Unity.Entities.EntityQuery.IsEmptyIgnoreFilter
        /// [Enableable Components]: xref:Unity.Entities.IEnableableComponent
        /// [Enabled]: xref:Unity.Entities.SystemState.Enabled
        /// </remarks>
        /// <returns>True if the system should be updated, or false if not.</returns>
        public bool ShouldRunSystem()
        {
            // If RequireForUpdate(...) was called, all those must match
            ref var required = ref RequiredEntityQueries;
            if (required.Length > 0)
            {
                for (int i = 0; i != required.Length; i++)
                {
                    EntityQuery query = required[i];
                    if (query.IsEmptyIgnoreFilter)
                        return false;
                }

                return true;
            }

            // If system has RequireMatchingQueriesForUpdate attribute, require at least one matching query
            if (RequireMatchingQueriesForUpdate)
            {
                // If all the queriesDesc are empty, skip it.
                // (There’s no way to know what the key value is without other markup)
                ref var eqs = ref EntityQueries;
                var length = EntityQueries.Length;
                for (int i = 0; i != length; i++)
                {
                    EntityQuery query = eqs[i];
                    if (!query.IsEmptyIgnoreFilter)
                        return true;
                }

                return false;
            }

            // Always update by default
            return true;
        }

        internal EntityQuery GetEntityQueryInternal(ComponentType* componentTypes, int count)
        {
            using var builder = new EntityQueryBuilder(Allocator.Temp, componentTypes, count);
            EntityQuery newQuery = GetEntityQueryInternal(builder);

            return newQuery;
        }

        internal void AddReaderWriter(ComponentType componentType)
        {
            if (CalculateReaderWriterDependency.Add(componentType, ref m_JobDependencyForReadingSystems, ref m_JobDependencyForWritingSystems))
            {
                CompleteDependencyInternal();
            }
        }

        internal void AddReaderWriters(EntityQuery query)
        {
            if (query.AddReaderWritersToLists(ref m_JobDependencyForReadingSystems, ref m_JobDependencyForWritingSystems))
            {
                CompleteDependencyInternal();
            }
        }

        private void AfterQueryCreated(EntityQuery query)
        {
            query.SetChangedFilterRequiredVersion(m_LastSystemVersion);
            query._GetImpl()->_DisallowDisposing = 1;

            EntityQueries.Add(query);
        }

        internal EntityQuery GetSingletonEntityQueryInternal(ComponentType type)
        {
            ref var handles = ref EntityQueries;

            for (var i = 0; i != handles.Length; i++)
            {
                var query = handles[i];
                var queryData = query._GetImpl()->_QueryData;

                // EntityQueries are constructed including the Entity ID
                if (2 != queryData->RequiredComponentsCount)
                    continue;

                if (queryData->RequiredComponents[1] != type)
                    continue;

                return query;
            }

            using (var builder = new EntityQueryBuilder(Allocator.Temp, &type, 1)
                .WithOptions(EntityQueryOptions.IncludeSystems))
            {
                var newQuery = EntityManager.CreateEntityQueryUnowned(builder);
                AddReaderWriters(newQuery);
                AfterQueryCreated(newQuery);

                return newQuery;
            }
        }

        [ExcludeFromBurstCompatTesting("Takes managed array")]
        internal EntityQuery GetEntityQueryInternal(EntityQueryDesc[] desc)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp);
            EntityQueryManager.ConvertToEntityQueryBuilder(ref builder, desc);
            EntityQuery result = GetEntityQueryInternal(builder);
            builder.Dispose();
            return result;
        }

        internal EntityQuery GetEntityQueryInternal(EntityQueryBuilder desc)
        {
            desc.FinalizeQueryInternal();
            ref var handles = ref EntityQueries;

            // TODO: https://jira.unity3d.com/browse/DOTS-6524 CompareQuery sorts each component array in the builder
            // every time it's called. That sort could be moved outside this loop, or into
            // EntityQueryBuilder.FinalizeQuery(), to avoid doing it every time.
            for (var i = 0; i != handles.Length; i++)
            {
                var query = handles[i];

                if (query.CompareQuery(desc))
                {
                    return query;
                }
            }

            var newQuery = EntityManager.CreateEntityQueryUnowned(desc);

            AddReaderWriters(newQuery);
            AfterQueryCreated(newQuery);

            return newQuery;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) })]
        internal DynamicBuffer<T> GetBuffer<T>(Entity entity, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return EntityManager.GetBuffer<T>(entity, isReadOnly);
        }

        /// <summary>
        /// Gets the cached query for the specified component types, if one exists; otherwise, creates a new query
        /// instance and caches it.
        /// </summary>
        /// <param name="componentTypes">An array or comma-separated list of component types.</param>
        /// <returns>The new or cached query.</returns>
        [ExcludeFromBurstCompatTesting("Takes managed array")]
        public EntityQuery GetEntityQuery(params ComponentType[] componentTypes)
        {
            CheckOnUpdate_Query();
            fixed (ComponentType* types = componentTypes)
            {
                return GetEntityQueryInternal(types, componentTypes.Length);
            }
        }

        /// <summary>
        /// Gets the cached query for the specified component type, if one exists; otherwise, creates a new query
        /// instance and caches it.
        /// </summary>
        /// <param name="componentType">The type of component to query.</param>
        /// <returns>The new or cached query.</returns>
        public EntityQuery GetEntityQuery(ComponentType componentType)
        {
            CheckOnUpdate_Query();
            return GetEntityQueryInternal(&componentType, 1);
        }

        /// <summary>
        /// Gets the cached query for the specified component types, if one exists; otherwise, creates a new query
        /// instance and caches it.
        /// </summary>
        /// <param name="componentTypes">An array of component types.</param>
        /// <returns>The new or cached query.</returns>
        public EntityQuery GetEntityQuery(NativeArray<ComponentType> componentTypes)
        {
            CheckOnUpdate_Query();
            return GetEntityQueryInternal((ComponentType*)componentTypes.GetUnsafeReadOnlyPtr(), componentTypes.Length);
        }

        /// <summary>
        /// Combines an array of query description objects into a single query.
        /// </summary>
        /// <remarks>This function looks for a cached query matching the combined query descriptions, and returns it
        /// if one exists; otherwise, the function creates a new query instance and caches it.</remarks>
        /// <returns>The new or cached query.</returns>
        /// <param name="queryDesc">An array of query description objects to be combined to define the query.</param>
        [ExcludeFromBurstCompatTesting("Takes managed array")]
        public EntityQuery GetEntityQuery(params EntityQueryDesc[] queryDesc)
        {
            CheckOnUpdate_Query();
            return GetEntityQueryInternal(queryDesc);
        }

        /// <summary>
        /// Create an entity query from a query description builder.
        /// </summary>
        /// <remarks>This function looks for a cached query matching the combined query descriptions, and returns it
        /// if one exists; otherwise, the function creates a new query instance and caches it.</remarks>
        /// <returns>The new or cached query.</returns>
        /// <param name="builder">The description builder</param>
        public EntityQuery GetEntityQuery(in EntityQueryBuilder builder)
        {
            CheckOnUpdate_Query();
            return GetEntityQueryInternal(builder);
        }

        /// <summary>
        /// Manually gets the run-time type information required to access an array of component data in a chunk.
        /// </summary>
        /// <remarks>Remember to call <see cref="ComponentTypeHandle{T}.Update(ref SystemState)"/>. </remarks>
        /// <param name="isReadOnly">Whether the component data is only read, not written. Access components as
        /// read-only whenever possible.</param>
        /// <typeparam name="T">A struct that implements <see cref="IComponentData"/>.</typeparam>
        /// <returns>An object representing the type information required to safely access component data stored in a
        /// chunk.</returns>
        /// <remarks>Pass an <see cref="ComponentTypeHandle{T}"/> instance to a job that has access to chunk data,
        /// such as an <see cref="IJobChunk"/> job, to access that type of component inside the job.</remarks>
        /// <remarks> Prefer using <see cref="SystemAPI.GetComponentTypeHandle{T}"/> in <see cref="SystemAPI"/> as it will cache in OnCreate for you
        /// and call .Update(ref SystemState) at the call-site.</remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public ComponentTypeHandle<T> GetComponentTypeHandle<T>(bool isReadOnly = false) where T : unmanaged, IComponentData
        {
            CheckOnUpdate_Handle();
            AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return EntityManager.GetComponentTypeHandle<T>(isReadOnly);
        }

        /// <summary>
        /// Manually gets the run-time type information required to access an array of component data in a chunk.
        /// </summary>
        /// <remarks>Remember to call <see cref="DynamicComponentTypeHandle.Update(ref SystemState)"/>.</remarks>
        /// <param name="componentType">Type of the component</param>
        /// <returns>An object representing the type information required to safely access component data stored in a
        /// chunk.</returns>
        /// <remarks>Pass an DynamicComponentTypeHandle instance to a job that has access to chunk data, such as an
        /// <see cref="IJobChunk"/> job, to access that type of component inside the job.</remarks>
        public DynamicComponentTypeHandle GetDynamicComponentTypeHandle(ComponentType componentType)
        {
            // NOTE: The primary use case of GetDynamicComponentTypeHandle is to get dynamic data access.
            //       in some cases for example in Netcode, this has to be done in OnUpdate. Hence we enable CheckOnUpdate_Handle warnings in this case.
            // CheckOnUpdate_Handle();

            AddReaderWriter(componentType);
            return EntityManager.GetDynamicComponentTypeHandle(componentType);
        }

        /// <summary>
        /// Manually gets the run-time type information required to access an array of buffer components in a chunk.
        /// </summary>
        /// <remarks>Remember to call <see cref="BufferTypeHandle{T}.Update(ref SystemState)"/>.</remarks>
        /// <param name="isReadOnly">Whether the data is only read, not written. Access data as
        /// read-only whenever possible.</param>
        /// <typeparam name="T">A struct that implements <see cref="IBufferElementData"/>.</typeparam>
        /// <returns>An object representing the type information required to safely access buffer components stored in a
        /// chunk.</returns>
        /// <remarks>Pass a BufferTypeHandle instance to a job that has access to chunk data, such as an
        /// <see cref="IJobChunk"/> job, to access that type of buffer component inside the job.</remarks>
        /// <remarks> Prefer using <see cref="SystemAPI.GetBufferTypeHandle{T}"/> in <see cref="SystemAPI"/> as it will cache in OnCreate for you
        /// and call .Update(ref SystemState) at the call-site.</remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) })]
        public BufferTypeHandle<T> GetBufferTypeHandle<T>(bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            CheckOnUpdate_Handle();
            AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return EntityManager.GetBufferTypeHandle<T>(isReadOnly);
        }

        /// <summary>
        /// Manually gets the run-time type information required to access transforms in a chunk.
        /// </summary>
        /// <remarks>Remember to call <see cref="TransformTypeHandle.Update(ref SystemState)"/>.</remarks>
        /// <param name="isReadOnly">True for read-only access, or false for read/write access.</param>
        /// <returns>An object representing the type information required to safely access transforms stored in a
        /// chunk.</returns>
        /// <remarks>Pass an TransformTypeHandle instance to a job that has access to chunk data, such as an
        /// <see cref="IJobChunk"/> job, to access transforms inside the job.</remarks>
        [GenerateTestsForBurstCompatibility]
        public TransformTypeHandle GetTransformTypeHandle(bool isReadOnly = false)
        {
            AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<TransformRef>() : ComponentType.ReadWrite<TransformRef>());
            return EntityManager.GetTransformTypeHandle(isReadOnly);
        }

        /// <summary>
        /// Gets the TransformRef of an entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="isReadOnly">True for read-only access, or false for read/write access.</param>
        /// <remarks>
        /// Using this method registers a read-only or read/write dependency for the system on TransformRef,
        /// based on the value passed to <paramref name="isReadOnly"/>.
        /// </remarks>
        /// <returns>The TransformRef to the entity.</returns>
        [GenerateTestsForBurstCompatibility]
        public TransformRef GetTransformRef(Entity entity, bool isReadOnly)
        {
            AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<TransformRef>() : ComponentType.ReadWrite<TransformRef>());
            return EntityManager.GetTransformRef(entity, isReadOnly);
        }

        /// <summary>
        /// Manually gets the run-time type information required to access a shared component data in a chunk.
        /// </summary>
        /// <remarks>Remember to call <see cref="SharedComponentTypeHandle{T}.Update(ref SystemState)"/>.</remarks>
        /// <typeparam name="T">A struct that implements <see cref="ISharedComponentData"/>.</typeparam>
        /// <returns>An object representing the type information required to safely access shared component data stored in a
        /// chunk.</returns>
        /// <remarks> Prefer using <see cref="SystemAPI.GetSharedComponentTypeHandle{T}"/> in <see cref="SystemAPI"/> as it will cache in OnCreate for you
        /// and call .Update(ref SystemState) at the call-site.</remarks>
        // NOTE: dual-use method — see ComponentSystemBase.GetSharedComponentTypeHandle for the
        // reason this is not marked [Obsolete] in step 1.
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSharedComponentData) })]
        public SharedComponentTypeHandle<T> GetSharedComponentTypeHandle<T>()
            where T : struct, ISharedComponentData
        {
            CheckOnUpdate_Handle();
            return EntityManager.GetSharedComponentTypeHandle<T>();
        }

        /// <summary>
        /// Manually gets the run-time type information required to access a shared component data in a chunk.
        /// </summary>
        /// <remarks>Remember to call <see cref="DynamicSharedComponentTypeHandle.Update(ref SystemState)"/>.</remarks>
        /// <param name="componentType">The component type to get access to.</param>
        /// <returns>An object representing the type information required to safely access shared component data stored in a
        /// chunk.</returns>
        public DynamicSharedComponentTypeHandle GetDynamicSharedComponentTypeHandle(ComponentType componentType)
        {
            // NOTE: The primary use case of GetDynamicSharedComponentTypeHandle is to get dynamic data access.
            //       in some cases for example in Netcode, this has to be done in OnUpdate. Hence we enable CheckOnUpdate_Handle warnings in this case.
            // CheckOnUpdate_Handle();

            return EntityManager.GetDynamicSharedComponentTypeHandle(componentType);
        }

        /// <summary>
        /// Manually gets the runtime type information required to access the array of <see cref="Entity"/> objects in a chunk.
        /// </summary>
        /// <remarks>To make sure the entity type handle is up to date, call  <see cref="EntityTypeHandle.Update(ref SystemState)"/> before you use this method.
        /// It's best practice to use <see cref="SystemAPI.GetEntityTypeHandle"/> instead of this method because `SystemAPI.GetEntityTypeHandle` caches in OnCreate for you
        /// and calls Update(ref SystemState) at the call-site.</remarks>
        /// <returns>An object that represents the type information required to safely access Entity instances stored in a
        /// chunk.</returns>
        public EntityTypeHandle GetEntityTypeHandle()
        {
            CheckOnUpdate_Handle();
            return EntityManager.GetEntityTypeHandle();
        }

        /// <summary>
        /// Manually gets a dictionary-like container containing all components of type T, keyed by Entity.
        /// </summary>
        /// <remarks>Remember to call <see cref="ComponentLookup{T}.Update(ref SystemState)"/>. </remarks>
        /// <param name="isReadOnly">Whether the data is only read, not written. Access data as
        /// read-only whenever possible.</param>
        /// <typeparam name="T">A struct that implements <see cref="IComponentData"/>.</typeparam>
        /// <returns>All component data of type T.</returns>
        /// <remarks> Prefer using <see cref="SystemAPI.GetComponentLookup{T}"/> as it will cache in OnCreate for you
        /// and call .Update(ref state) at the call-site.</remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        public ComponentLookup<T> GetComponentLookup<T>(bool isReadOnly = false)
            where T : unmanaged, IComponentData
        {
            CheckOnUpdate_Lookup();
            AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return EntityManager.GetComponentLookup<T>(isReadOnly);
        }
        /// <summary> Obsolete. Use <see cref="GetComponentLookup{T}"/> instead.</summary>
        /// <param name="isReadOnly">Whether the data is only read, not written. Access data as
        /// read-only whenever possible.</param>
        /// <typeparam name="T">A struct that implements <see cref="IComponentData"/>.</typeparam>
        /// <returns>All component data of type T.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleComponentData) })]
        [Obsolete("This method has been renamed to GetComponentLookup<T>(). (RemovedAFter Entities 1.0) (UnityUpgradable) -> GetComponentLookup<T>(*)", false)]
        public ComponentLookup<T> GetComponentDataFromEntity<T>(bool isReadOnly = false)
            where T : unmanaged, IComponentData
        {
            return GetComponentLookup<T>(isReadOnly);
        }

        /// <summary>
        /// Manually gets a BufferLookup&lt;T&gt; object that can access a <seealso cref="DynamicBuffer{T}"/>.
        /// </summary>
        /// <remarks>Remember to call <see cref="BufferLookup{T}.Update(ref SystemState)"/>. </remarks>
        /// <remarks>Assign the returned object to a field of your Job struct so that you can access the
        /// contents of the buffer in a Job.</remarks>
        /// <param name="isReadOnly">Whether the buffer data is only read or is also written. Access data in
        /// a read-only fashion whenever possible.</param>
        /// <typeparam name="T">The type of <see cref="IBufferElementData"/> stored in the buffer.</typeparam>
        /// <returns>An array-like object that provides access to buffers, indexed by <see cref="Entity"/>.</returns>
        /// <seealso cref="ComponentLookup{T}"/>
        /// <remarks> Prefer using <see cref="SystemAPI.GetBufferLookup{T}"/> as it will cache in OnCreate for you
        /// and call .Update(ref state) at the call-site.</remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) })]
        public BufferLookup<T> GetBufferLookup<T>(bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            CheckOnUpdate_Lookup();
            AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>());
            return EntityManager.GetBufferLookup<T>(isReadOnly);
        }
        /// <summary> Obsolete. Use <see cref="GetBufferLookup{T}"/> instead.</summary>
        /// <param name="isReadOnly">Whether the buffer data is only read or is also written. Access data in
        /// a read-only fashion whenever possible.</param>
        /// <typeparam name="T">The type of <see cref="IBufferElementData"/> stored in the buffer.</typeparam>
        /// <returns>An array-like object that provides access to buffers, indexed by <see cref="Entity"/>.</returns>
        /// <seealso cref="ComponentLookup{T}"/>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleBufferElement) })]
        [Obsolete("This method has been renamed to GetBufferLookup<T>(). (RemovedAFter Entities 1.0) (UnityUpgradable) -> GetBufferLookup<T>(*)", false)]
        public BufferLookup<T> GetBufferFromEntity<T>(bool isReadOnly = false) where T : unmanaged, IBufferElementData
        {
            return GetBufferLookup<T>(isReadOnly);
        }

        /// <summary>
        /// Manually gets a dictionary-like container containing information about how entities are stored.
        /// </summary>
        /// <remarks>Remember to call <see cref="EntityStorageInfoLookup.Update(ref SystemState)"/>. </remarks>
        /// <returns>A EntityStorageInfoLookup object.</returns>
        /// <seealso cref="EntityStorageInfoLookup"/>
        /// <remarks> Prefer using <see cref="SystemAPI.GetEntityStorageInfoLookup"/> as it will cache in OnCreate for you
        /// and call .Update(ref state) at the call-site.</remarks>
        [GenerateTestsForBurstCompatibility]
        public EntityStorageInfoLookup GetEntityStorageInfoLookup()
        {
            return EntityManager.GetEntityStorageInfoLookup();
        }

        /// <summary>
        /// Manually gets a <see cref="TransformLookup"/> object that can access a <see cref="TransformRef"/>.
        /// </summary>
        /// <remarks>Remember to call <see cref="TransformLookup.Update(ref SystemState)"/>. </remarks>
        /// <remarks>Assign the returned object to a field of your Job struct so that you can access the
        /// contents of the transform in a Job.</remarks>
        /// <param name="isReadOnly">Whether the transform data is only read or is also written. Access data in
        /// a read-only fashion whenever possible.</param>
        /// <returns>An array-like object that provides access to transform data, indexed by <see cref="Entity"/>.</returns>
        /// <seealso cref="ComponentLookup{T}"/>
        public TransformLookup GetTransformLookup(bool isReadOnly = false)
        {
            AddReaderWriter(isReadOnly ? ComponentType.ReadOnly<TransformRef>() : ComponentType.ReadWrite<TransformRef>());
            return EntityManager.GetTransformLookup(isReadOnly);
        }

        ///<summary> Obsolete. Use <see cref="GetEntityStorageInfoLookup"/> instead.</summary>
        /// <returns>A EntityStorageInfoLookup object.</returns>
        [GenerateTestsForBurstCompatibility]
        [Obsolete("This method has been renamed to GetEntityStorageInfoLookup(). (RemovedAFter Entities 1.0) (UnityUpgradable) -> GetEntityStorageInfoLookup(*)", false)]
        public EntityStorageInfoLookup GetStorageInfoFromEntity()
        {
            return EntityManager.GetEntityStorageInfoLookup();
        }

        /// <summary>
        /// Adds a query that must return entities for the system to run. You can add multiple required queries to a
        /// system; all of them must match at least one entity for the system to run.
        /// </summary>
        /// <param name="query">A query that must match entities this frame in order for this system to run.</param>
        /// <remarks>Any queries added through RequireForUpdate override all other queries cached by this system.
        /// In other words, if any required query does not find matching entities, the update is skipped even
        /// if another query created for the system (either explicitly or implicitly) does match entities and
        /// vice versa.
        ///
        /// Note that for components that implement <see cref="T:Unity.Entities.IEnableableComponent"/>
        /// this method ignores whether the component is enabled or not, it only checks whether it exists.
        /// It also ignores any other filters placed </remarks>
        /// <seealso cref="ShouldRunSystem"/>
        /// <seealso cref="RequireForUpdate{T}"/>
        /// <seealso cref="T:Unity.Entities.RequireMatchingQueriesForUpdateAttribute"/>
        public void RequireForUpdate(EntityQuery query)
        {
            CheckOnUpdate_Query();

            if (!RequiredEntityQueries.Contains(query))
                RequiredEntityQueries.Add(query);
        }

        /// <summary>
        /// Require that a specific component exist for this system to run.
        /// Also includes any components added to a system.
        /// See <see cref="Unity.Entities.SystemHandle"/> for more info on that.
        /// </summary>
        /// <typeparam name="T">The <see cref="IComponentData"/> subtype of the component.</typeparam>
        /// <remarks>Any queries added through RequireForUpdate override all other queries cached by this system.
        /// In other words, if any required query does not find matching entities, the update is skipped even
        /// if another query created for the system (either explicitly or implicitly) does match entities and
        /// vice versa.
        ///
        /// Note that for components that implement <see cref="T:Unity.Entities.IEnableableComponent"/>
        /// this method ignores whether the component is enabled or not, it only checks whether it exists.</remarks>
        /// <seealso cref="ShouldRunSystem"/>
        /// <seealso cref="RequireForUpdate(Unity.Entities.EntityQuery)"/>
        /// <seealso cref="T:Unity.Entities.RequireMatchingQueriesForUpdateAttribute"/>
        ///
        [ExcludeFromBurstCompatTesting("Eventually accesses managed World")]
        public void RequireForUpdate<T>()
        {
            var type = ComponentType.ReadOnly<T>();
            using var builder = new EntityQueryBuilder(Allocator.Temp, &type, 1).WithOptions(EntityQueryOptions.IncludeSystems);
            RequireForUpdate(GetEntityQueryInternal(builder));
        }

        /// <summary>
        /// Provide a set of queries, one of which must match entities for the system to run.
        /// </summary>
        /// <param name="queries">A set of queries, one of which must match entities this frame in order for
        /// this system to run.</param>
        /// <remarks>
        /// This method can only be called from a system's OnCreate method.
        ///
        /// You can call this method multiple times from the same system to add multiple sets of required
        /// queries. Each set must have at least one query that matches an entity for the system to run.
        ///
        /// Any queries added through RequireAnyForUpdate and [RequireForUpdate] override all other queries
        /// created by this system for the purposes of deciding whether to update. In other words, if any set
        /// of required queries does not find matching entities, the update is skipped even if another query
        /// created for the system (either explicitly or implicitly) does match entities and vice versa.
        ///
        /// [EntityQueries]: xref:Unity.Entities.EntityQuery
        /// [enableable components]: xref:T:Unity.Entities.IEnableableComponent
        /// </remarks>
        /// <seealso cref="ShouldRunSystem"/>
        /// <seealso cref="T:Unity.Entities.RequireMatchingQueriesForUpdateAttribute"/>
        [ExcludeFromBurstCompatTesting("Takes a managed array params")]
        public void RequireAnyForUpdate(params EntityQuery[] queries)
        {
            fixed(EntityQuery* queriesPtr = queries)
            {
                RequireAnyForUpdate(queriesPtr, queries.Length);
            }
        }

        /// <summary>
        /// Provide a set of queries, one of which must match entities for the system to run.
        /// </summary>
        /// <param name="queries">A set of queries, one of which must match entities this frame in order for
        /// this system to run.</param>
        /// <remarks>
        /// This method can only be called from a system's OnCreate method.
        ///
        /// You can call this method multiple times from the same system to add multiple sets of required
        /// queries. Each set must have at least one query that matches an entity for the system to run.
        ///
        /// Any queries added through RequireAnyForUpdate and [RequireForUpdate] override all other queries
        /// created by this system for the purposes of deciding whether to update. In other words, if any set
        /// of required queries does not find matching entities, the update is skipped even if another query
        /// created for the system (either explicitly or implicitly) does match entities and vice versa.
        ///
        /// [EntityQueries]: xref:Unity.Entities.EntityQuery
        /// [enableable components]: xref:T:Unity.Entities.IEnableableComponent
        /// </remarks>
        /// <seealso cref="ShouldRunSystem"/>
        /// <seealso cref="T:Unity.Entities.RequireMatchingQueriesForUpdateAttribute"/>
        [GenerateTestsForBurstCompatibility]
        public void RequireAnyForUpdate(NativeArray<EntityQuery> queries)
        {
            RequireAnyForUpdate((EntityQuery*)queries.GetUnsafeReadOnlyPtr(), queries.Length);
        }

        [GenerateTestsForBurstCompatibility]
        internal void RequireAnyForUpdate(EntityQuery* queries, int queryCount)
        {
            // We can support ORing required queries together (compared to RequireForUpdate's AND)
            // by taking each of the queries, converting them to an EntityQueryDesc, and adding
            // them all to a single EntityQuery. EntityQueries with multiple ArchetypeQueries
            // will be non-empty if any ArchetypeQueries match. Then we can add that EntityQuery
            // to the list of RequiredEntityQueries and don't need to change the logic of
            // ShouldRunSystem at all.
            //
            // This has the additional benefit of supporting more-expressive requirements
            //
            //      // ComponentA && (query1 || query2)
            //      RequireForUpdate<ComponentA>
            //      RequireAnyForUpdate(query1, query2);
            //
            //      // (query1 || query2) && (query3 || query4)
            //      RequireAnyForUpdate(query1, query2);
            //      RequireAnyForUpdate(query3, query4);
            //
            // This could be simplified by grabbing all the ArchetypeQuery pointers,
            // optionally de-duping them, then directly calling the second half of
            // EntityQueryManager.CreateEntityQuery(EntityDataAccess*,EntityQueryBuilder)

            var builder = new EntityQueryBuilder(Allocator.Temp);
            for (var q = 0; q < queryCount; q++)
            {
                var queryData = queries[q]._GetImpl()->_QueryData;
                var archetypeQueryCount = queryData->ArchetypeQueryCount;
                for (var a = 0; a < archetypeQueryCount; a++)
                {
                    var archetypeQuery = queryData->ArchetypeQueries[a];
                    for (var i = 0; i < archetypeQuery.AllCount; i++)
                    {
                        var componentType = new ComponentType{ TypeIndex = archetypeQuery.All[i], AccessModeType = (ComponentType.AccessMode)archetypeQuery.AllAccessMode[i] };
                        builder.WithAll(&componentType, 1);
                    }
                    for (var i = 0; i < archetypeQuery.AnyCount; i++)
                    {
                        var componentType = new ComponentType{ TypeIndex = archetypeQuery.Any[i], AccessModeType = (ComponentType.AccessMode)archetypeQuery.AnyAccessMode[i] };
                        builder.WithAny(&componentType, 1);
                    }
                    for (var i = 0; i < archetypeQuery.NoneCount; i++)
                    {
                        var componentType = new ComponentType{ TypeIndex = archetypeQuery.None[i], AccessModeType = (ComponentType.AccessMode)archetypeQuery.NoneAccessMode[i] };
                        builder.WithNone(&componentType, 1);
                    }
                    for (var i = 0; i < archetypeQuery.AbsentCount; i++)
                    {
                        var componentType = new ComponentType{ TypeIndex = archetypeQuery.Absent[i], AccessModeType = (ComponentType.AccessMode)archetypeQuery.AbsentAccessMode[i] };
                        builder.WithAbsent(&componentType, 1);
                    }
                    for (var i = 0; i < archetypeQuery.DisabledCount; i++)
                    {
                        var componentType = new ComponentType{ TypeIndex = archetypeQuery.Disabled[i], AccessModeType = (ComponentType.AccessMode)archetypeQuery.DisabledAccessMode[i] };
                        builder.WithDisabled(&componentType, 1);
                    }
                    for (var i = 0; i < archetypeQuery.PresentCount; i++)
                    {
                        var componentType = new ComponentType{ TypeIndex = archetypeQuery.Present[i], AccessModeType = (ComponentType.AccessMode)archetypeQuery.PresentAccessMode[i] };
                        builder.WithPresent(&componentType, 1);
                    }
                    builder.WithOptions(archetypeQuery.Options);
                    builder.FinalizeQueryInternal();
                }
            }

            var megaQuery = GetEntityQuery(builder);
            RequireForUpdate(megaQuery);
        }

        /// <summary> Obsolete. Use <see cref="RequireForUpdate{T}"/> instead.</summary>
        /// <typeparam name="T">The <see cref="IComponentData"/> subtype of the singleton component.</typeparam>
        [Obsolete("RequireSingletonForUpdate has been renamed. Use RequireForUpdate<T>() instead. (RemovedAFter Entities 1.0) (UnityUpgradable) -> RequireForUpdate<T>()", true)]
        public void RequireSingletonForUpdate<T>()
        {
            RequireForUpdate<T>();
        }
    }
}
