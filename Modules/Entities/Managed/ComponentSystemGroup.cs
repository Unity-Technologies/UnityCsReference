// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    /// <summary>
    /// A special-case system that encapsulates an ordered list of other systems. When the group is updated, the group's
    /// member systems are updated in order.
    /// </summary>
    [DebuggerTypeProxy(typeof(ComponentSystemGroupDebugView))]
    public abstract unsafe partial class ComponentSystemGroup : SystemBase
    {
        private bool m_systemSortDirty = false;

        // Initial memory block size in bytes
        const int InitialSystemGroupAllocatorBlockSizeInBytes = 128 * 1024;    // 128k

        // If true (the default), calling SortSystems() will sort the system update list, respecting the constraints
        // imposed by [UpdateBefore] and [UpdateAfter] attributes. SortSystems() is called automatically during
        // DefaultWorldInitialization, as well as at the beginning of ComponentSystemGroup.OnUpdate(), but may also be
        // called manually.
        //
        // If false, calls to SortSystems() on this system group will have no effect on update order of systems in this
        // group (though SortSystems() will still be called recursively on any child system groups). The group's systems
        // will update in the order of the most recent sort operation, with any newly-added systems updating in
        // insertion order at the end of the list.
        //
        // Setting this value to false is not recommended unless you know exactly what you're doing, and you have full
        // control over the systems which will be updated in this group.
        private bool m_EnableSystemSorting = true;

        /// <summary>If true (the default), calling SortSystems() will sort the system update list, respecting the constraints
        /// imposed by [UpdateBefore] and [UpdateAfter] attributes.</summary>
        /// <remarks>SortSystems() is called automatically during
        /// DefaultWorldInitialization, as well as at the beginning of ComponentSystemGroup.OnUpdate(), but may also be
        /// called manually.
        ///
        /// If false, calls to SortSystems() on this system group will have no effect on update order of systems in this
        /// group (though SortSystems() will still be called recursively on any child system groups). The group's systems
        /// will update in the order of the most recent sort operation, with any newly-added systems updating in
        /// insertion order at the end of the list.
        ///
        /// Setting this value to false is not recommended unless you know exactly what you're doing, and you have full
        /// control over the systems which will be updated in this group.
        /// </remarks>
        public bool EnableSystemSorting
        {
            get => m_EnableSystemSorting;
            protected set
            {
                if (value && !m_EnableSystemSorting)
                    m_systemSortDirty = true; // force a sort after re-enabling sorting
                m_EnableSystemSorting = value;
            }
        }

        /// <summary>
        /// Checks if the system group is in a fully initialized and valid state
        /// </summary>
        public bool Created { get; private set; } = false;

        internal List<ComponentSystemBase> m_managedSystemsToUpdate = new List<ComponentSystemBase>();
        internal List<ComponentSystemBase> m_managedSystemsToRemove = new List<ComponentSystemBase>();

        internal UnsafeList<SystemHandle> m_UpdateList;
        internal UnsafeList<SystemHandle> m_UnmanagedSystemsToUpdate;
        internal UnsafeList<SystemHandle> m_UnmanagedSystemsToRemove;

        // function pointers used by netcode's tracing. Used to run the same tracing code before/after every systems in a given group
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
        internal delegate void SystemWrapperDelegate(SystemTypeIndex targetSystem, ref SystemState targetSystemState);
        // The assumption is that those function pointers won't touch the target system's state dependencies (and so those function pointers are run after those dependencies are updated)
        internal FunctionPointer<SystemWrapperDelegate> OnUpdateBefore;
        internal FunctionPointer<SystemWrapperDelegate> OnUpdateAfter;

        /// <summary>
        /// The list of managed systems in this group, sorted by update order.
        /// </summary>
        public virtual IReadOnlyList<ComponentSystemBase> ManagedSystems => m_managedSystemsToUpdate;

        internal UnsafeList<SystemHandle> UnmanagedSystems => m_UnmanagedSystemsToUpdate;
        /// <summary>
        /// Get the list of unmanaged systems in this group, sorted by update order.
        /// </summary>
        /// <param name="allocator">Which allocator to use to allocate the returned list.</param>
        /// <returns>A NativeList of systems</returns>
        public NativeList<SystemHandle> GetUnmanagedSystems(Allocator allocator = Allocator.Temp)
        {
            var ret = new NativeList<SystemHandle>(m_UnmanagedSystemsToUpdate.Length, allocator);
            ret.CopyFrom(m_UnmanagedSystemsToUpdate);
            return ret;
        }

        /// <summary>
        /// Get the list of all systems in this group, managed and unmanaged alike, sorted by update order.
        /// </summary>
        /// <param name="allocator">Which allocator to use to allocate the returned list.</param>
        /// <returns>A NativeList of systems</returns>
        public NativeList<SystemHandle> GetAllSystems(Allocator allocator = Allocator.Temp)
        {
            var ret = new NativeList<SystemHandle>(m_UpdateList.Length, allocator);
            ret.CopyFrom(m_UpdateList);
            return ret;
        }

        internal DoubleRewindableAllocators* m_RateGroupAllocators = null;
        internal byte RateGroupAllocatorsCreated { get; set; } = 0;

        private IRateManager m_RateManager = null;

        /// <inheritdoc cref="SystemBase.OnCreate"/>
        protected override void OnCreate()
        {
            base.OnCreate();
            m_UnmanagedSystemsToUpdate = new UnsafeList<SystemHandle>(0, Allocator.Persistent);
            m_UnmanagedSystemsToRemove = new UnsafeList<SystemHandle>(0, Allocator.Persistent);
            m_UpdateList = new UnsafeList<SystemHandle>(0, Allocator.Persistent);
            Created = true;
        }

        /// <inheritdoc cref="SystemBase.OnDestroy"/>
        protected override void OnDestroy()
        {
            if (RateGroupAllocatorsCreated == 1)
            {
                DestroyRateGroupAllocators();
            }
            m_UpdateList.Dispose();
            m_UnmanagedSystemsToRemove.Dispose();
            m_UnmanagedSystemsToUpdate.Dispose();
            base.OnDestroy();
            Created = false;
            // Adding this to help eliminate noise when hunting for memory leaks in the memory profiler tool. (DOTS-8683)
            m_managedSystemsToUpdate.Clear();
            m_managedSystemsToRemove.Clear();

        }

        private void CheckCreated()
        {
            if (!Created)
                throw new InvalidOperationException($"Group of type {GetType()} has not been created, either the derived class forgot to call base.OnCreate(), or it has been destroyed");
        }

        /// <summary>
        /// Appends a managed system to the group's update list. The list will be sorted the next time the group is updated.
        /// </summary>
        /// <param name="sys">The system to add.</param>
        /// <exception cref="ArgumentException">Thrown if a group is added to itself.</exception>
        public void AddSystemToUpdateList(ComponentSystemBase sys)
        {
            CheckCreated();

            if (sys != null)
            {
                if (this == sys)
                    throw new ArgumentException($"Can't add {TypeManager.GetSystemName(GetType())} to its own update list");

                // Check for duplicate Systems. Also see issue #1792
                if (m_managedSystemsToUpdate.IndexOf(sys) >= 0)
                {
                    if (m_managedSystemsToRemove.Contains(sys))
                    {
                        m_managedSystemsToRemove.Remove(sys);
                    }
                    return;
                }

                m_UpdateList.Add(sys.SystemHandle);
                m_managedSystemsToUpdate.Add(sys);
                m_systemSortDirty = true;
            }
        }

        private int UnmanagedSystemIndex(SystemHandle sysHandle)
        {
            int len = m_UnmanagedSystemsToUpdate.Length;
            var ptr = m_UnmanagedSystemsToUpdate.Ptr;
            for (int i = 0; i < len; ++i)
            {
                if (ptr[i] == sysHandle)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Appends an unmanaged system to the group's update list. The list will be sorted the next time the group is updated.
        /// </summary>
        /// <param name="sysHandle">The system to add</param>
        public void AddSystemToUpdateList(SystemHandle sysHandle)
        {
            CheckCreated();

            if (sysHandle == default)
                return;
            var state = World.Unmanaged.ResolveSystemState(sysHandle);

            bool isManaged = state->m_ManagedSystem.IsAllocated;
            if (isManaged)
            {
                var sys = (ComponentSystemBase)state->m_ManagedSystem.Target;
                this.AddSystemToUpdateList(sys);
                return;
            }
            else
            {
                if (-1 != UnmanagedSystemIndex(sysHandle))
                {
                    int index = m_UnmanagedSystemsToRemove.IndexOf(sysHandle);
                    if (-1 != index)
                        m_UnmanagedSystemsToRemove.RemoveAt(index);
                    return;
                }

                m_UpdateList.Add(sysHandle);
                m_UnmanagedSystemsToUpdate.Add(sysHandle);
            }

            m_systemSortDirty = true;
        }

        /// <summary>
        /// Requests that a managed system be removed from the group's update list. The system will be removed the next time the group is sorted.
        /// </summary>
        /// <param name="sys">The system to remove</param>
        public void RemoveSystemFromUpdateList(ComponentSystemBase sys)
        {
            CheckCreated();

            if (m_managedSystemsToUpdate.Contains(sys) && !m_managedSystemsToRemove.Contains(sys))
            {
                m_systemSortDirty = true;
                m_managedSystemsToRemove.Add(sys);
            }
        }

        /// <summary>
        /// Requests that an unmanaged system be removed from the group's update list. The system will be removed the next time the group is sorted.
        /// </summary>
        /// <param name="sys">The system to remove</param>
        public void RemoveSystemFromUpdateList(SystemHandle sys)
        {
            CheckCreated();

            if (m_UnmanagedSystemsToUpdate.Contains(sys) && !m_UnmanagedSystemsToRemove.Contains(sys))
            {
                m_systemSortDirty = true;
                m_UnmanagedSystemsToRemove.Add(sys);
            }
        }

        private void RemovePending()
        {
            if (m_managedSystemsToRemove.Count > 0)
            {
                foreach (var sys in m_managedSystemsToRemove)
                {
                    m_managedSystemsToUpdate.Remove(sys);
                }

                m_managedSystemsToRemove.Clear();
            }

            for(int i=0; i<m_UnmanagedSystemsToRemove.Length; ++i)
            {
                var sysHandle = m_UnmanagedSystemsToRemove[i];
                m_UnmanagedSystemsToUpdate.RemoveAt(m_UnmanagedSystemsToUpdate.IndexOf(sysHandle));
            }

            m_UnmanagedSystemsToRemove.Clear();
        }


        private void RemoveSystemsFromUnsortedUpdateList()
        {
            if (m_managedSystemsToRemove.Count <= 0 && m_UnmanagedSystemsToRemove.Length <= 0)
                return;

            var removedManaged = new HashSet<ComponentSystemBase>(m_managedSystemsToRemove);
            var removedHandles = new NativeHashSet<SystemHandle>(m_managedSystemsToRemove.Count + m_UnmanagedSystemsToRemove.Length, Allocator.Temp);
            foreach (var managedSystem in m_managedSystemsToRemove)
            {
                removedHandles.Add(managedSystem.CheckedState()->SystemHandle);
            }
            foreach (var unmanagedSystem in m_UnmanagedSystemsToRemove)
            {
                removedHandles.Add(unmanagedSystem);
            }

            m_managedSystemsToUpdate.RemoveAll(managedSystem => removedManaged.Contains(managedSystem));
            m_managedSystemsToRemove.Clear();

            RemoveHandles(ref m_UnmanagedSystemsToUpdate, removedHandles);
            m_UnmanagedSystemsToRemove.Clear();

            RemoveHandles(ref m_UpdateList, removedHandles);
            removedHandles.Dispose();
        }

        static void RemoveHandles(ref UnsafeList<SystemHandle> list, NativeHashSet<SystemHandle> removedHandles)
        {
            int kept = 0;
            for (int i = 0; i < list.Length; ++i)
            {
                if (!removedHandles.Contains(list[i]))
                    list[kept++] = list[i];
            }
            list.Length = kept;
        }

        private void RecurseUpdate()
        {
            if (!EnableSystemSorting)
            {
                RemoveSystemsFromUnsortedUpdateList();
            }
            else if (m_systemSortDirty)
            {
                GenerateMasterUpdateList();
            }
            m_systemSortDirty = false;

            foreach (var sys in m_managedSystemsToUpdate)
            {
                if (TypeManager.IsSystemAGroup(sys.GetType()))
                {
                    var childGroup = sys as ComponentSystemGroup;
                    childGroup.RecurseUpdate();
                }
            }
        }

        private void GenerateMasterUpdateList()
        {
            RemovePending();

            var groupTypeIndex = m_StatePtr->m_SystemTypeIndex;

            var nElems = m_managedSystemsToUpdate.Count + m_UnmanagedSystemsToUpdate.Length;
            var allElems =
                new UnsafeList<ComponentSystemSorter.SystemElement>(
                    nElems,
                    Allocator.Temp);
            allElems.Length = nElems;
            var systemsPerBucket = stackalloc int[] { 0, 0, 0 };
            for (int i = 0; i < m_managedSystemsToUpdate.Count; ++i)
            {
                var system = m_managedSystemsToUpdate[i];
                var sysTypeIndex = system.m_StatePtr->m_SystemTypeIndex;
                int orderingBucket = ComputeSystemOrdering(sysTypeIndex, groupTypeIndex);
                allElems[i] = new ComponentSystemSorter.SystemElement
                {
                    SystemTypeIndex = sysTypeIndex,
                    Handle = system.m_StatePtr->SystemHandle,
                    OrderingBucket = orderingBucket,
                    updateBefore = new NativeList<int>(16, Allocator.Temp),
                    nAfter = 0,
                };
                systemsPerBucket[orderingBucket]++;
            }
            for (int i = 0; i < m_UnmanagedSystemsToUpdate.Length; ++i)
            {
                var handle = m_UnmanagedSystemsToUpdate[i];
                var sysTypeIndex = World.Unmanaged.ResolveSystemState(handle)->m_SystemTypeIndex;
                int orderingBucket = ComputeSystemOrdering(sysTypeIndex, groupTypeIndex);
                allElems[m_managedSystemsToUpdate.Count + i] = new ComponentSystemSorter.SystemElement
                {
                    SystemTypeIndex = sysTypeIndex,
                    Handle = handle,
                    OrderingBucket = orderingBucket,
                    updateBefore = new NativeList<int>(16, Allocator.Temp),
                    nAfter = 0,
                };
                systemsPerBucket[orderingBucket]++;
            }

            var lookupDictionary = new NativeHashMap<int, int>(16, Allocator.Temp);

            var nativeHashMap =
                (NativeHashMap<SystemTypeIndex, int>*)UnsafeUtility.AddressOf(ref lookupDictionary);

            var badTypeIndices = new NativeHashSet<SystemTypeIndex>(16, Allocator.Temp);

            // Find & validate constraints between systems in the group
            var badTypeIndicesPtr = (NativeHashSet<SystemTypeIndex>*)UnsafeUtility.AddressOf(ref badTypeIndices);
            ComponentSystemSorter.FindConstraints(groupTypeIndex,
                (UnsafeList<ComponentSystemSorter.SystemElement>*)UnsafeUtility.AddressOf(ref allElems),
                nativeHashMap,
                TypeManager.SystemAttributeKind.UpdateAfter,
                TypeManager.SystemAttributeKind.UpdateBefore,
                badTypeIndicesPtr);

            if (badTypeIndices.Count > 0)
            {
                var enumerator = badTypeIndices.ToNativeArray(Allocator.Temp);

                for (int i=0; i<enumerator.Length; i++)
                {
                    var badTypeIndex = enumerator[i];
                    ComponentSystemSorter.WarnAboutAnySystemAttributeBadness(badTypeIndex, this);
                }
                enumerator.Dispose();
            }
            badTypeIndices.Clear();

            // Build three lists of systems
            var elemBuckets = stackalloc UnsafeList<ComponentSystemSorter.SystemElement>[]
            {
                new UnsafeList<ComponentSystemSorter.SystemElement>(systemsPerBucket[0], Allocator.Temp),
                new UnsafeList<ComponentSystemSorter.SystemElement>(systemsPerBucket[1], Allocator.Temp),
                new UnsafeList<ComponentSystemSorter.SystemElement>(systemsPerBucket[2], Allocator.Temp),
            };
            elemBuckets[0].Length = systemsPerBucket[0];
            elemBuckets[1].Length = systemsPerBucket[1];
            elemBuckets[2].Length = systemsPerBucket[2];
            var nextBucketIndex = stackalloc int[] { 0, 0, 0 };

            for(int i=0; i<allElems.Length; ++i)
            {
                int bucket = allElems[i].OrderingBucket;
                int index = nextBucketIndex[bucket]++;
                elemBuckets[bucket][index] = allElems[i];
            }
            // Perform the sort for each bucket.
            for (int i = 0; i < 3; ++i)
            {
                if (elemBuckets[i].Length > 0)
                {
                    ref var systemElements = ref elemBuckets[i];
                    ComponentSystemSorter.Sort(
                        (UnsafeList<ComponentSystemSorter.SystemElement>*)UnsafeUtility.AddressOf(ref systemElements),
                        nativeHashMap);
                }
            }

            // Because people can freely look at the list of managed systems, we need to put that part of list in order.
            var oldCount = m_managedSystemsToUpdate.Count;
            m_managedSystemsToUpdate = new List<ComponentSystemBase>(oldCount);
            m_UpdateList.Clear();
            m_UpdateList.SetCapacity(allElems.Length);
            for (int i = 0; i < 3; ++i)
            {
                foreach (var e in elemBuckets[i])
                {
                    m_UpdateList.Add(e.Handle);
                    var state = WorldOf(e.Handle).ResolveSystemState(e.Handle);
                    if (state->m_ManagedSystem.IsAllocated)
                        m_managedSystemsToUpdate.Add((ComponentSystemBase)state->m_ManagedSystem.Target);
                }
            }
        }

        // A managed system from another world can be added through AddSystemToUpdateList(ComponentSystemBase)
        WorldUnmanaged WorldOf(SystemHandle handle)
        {
            var world = World.Unmanaged;
            if (handle.m_WorldSeqNo == (uint)world.SequenceNumber)
                return world;

            var access = EntityComponentStore.GetEntityDataAccess(handle.m_Entity);
            return access != null ? access->m_WorldUnmanaged : world;
        }

        internal static int ComputeSystemOrdering(SystemTypeIndex sysType, SystemTypeIndex ourTypeIndex)
        {
            if (ourTypeIndex == -1 || sysType == -1)
                return 1;

            var attrs = TypeManager.GetSystemAttributes(sysType, TypeManager.SystemAttributeKind.UpdateInGroup);
            for (int i=0; i<attrs.Length; i++)
            {
                var attr = attrs[i];

                if (attr.TargetSystemTypeIndex == ourTypeIndex)
                {
                    if ((attr.Flags & TypeManager.SystemAttribute.kOrderFirstFlag) != 0)
                    {
                        return 0;
                    }

                    if ((attr.Flags & TypeManager.SystemAttribute.kOrderLastFlag) != 0)
                    {
                        return 2;
                    }
                }
            }

            return 1;
        }

        /// <summary>
        /// Update the component system's sort order.
        /// </summary>
        public void SortSystems()
        {
            CheckCreated();

            RecurseUpdate();
        }

        internal override void OnStopRunningInternal()
        {
            OnStopRunning();

            foreach (var sys in m_managedSystemsToUpdate)
            {
                if (sys == null)
                    continue;

                if (sys.m_StatePtr == null)
                    continue;

                if (!sys.m_StatePtr->PreviouslyEnabled)
                    continue;

                sys.m_StatePtr->PreviouslyEnabled = false;
                sys.OnStopRunningInternal();
            }

            for (int i = 0; i < m_UnmanagedSystemsToUpdate.Length; ++i)
            {
                var sys = World.Unmanaged.ResolveSystemState(m_UnmanagedSystemsToUpdate[i]);

                if (sys == null || !sys->PreviouslyEnabled)
                    continue;

                sys->PreviouslyEnabled = false;
                SystemBaseRegistry.CallOnStopRunning(sys);
            }
        }

        /// <summary>Obsolete. Use <see cref="RateManager"/> instead.</summary>
        [Obsolete("This property has been renamed to RateManager. (RemovedAfter Entities 1.0) (UnityUpgradable) -> RateManager")]
        public IRateManager FixedRateManager
        {
            get => m_RateManager;
            set => m_RateManager = value;
        }

        /// <summary>
        /// Optional field to control the update rate of this system group.
        /// </summary>
        /// <remarks>
        /// No group allocator is created when setting rate manager.
        /// </remarks>
        public IRateManager RateManager
        {
            get
            {
                return m_RateManager;
            }
            set
            {
                m_RateManager = value;
            }
        }

        /// <summary>
        /// Set optional rate manager for the system group and create group allocator.
        /// </summary>
        /// <remarks>
        /// Create a group allocator for this system group if not created already.
        /// </remarks>
        /// <param name="rateManager">The <see cref="IRateManager"/> to set the allocator to</param>
        public void SetRateManagerCreateAllocator(IRateManager rateManager)
        {
            CreateRateGroupAllocators();
            m_RateManager = rateManager;
        }

        /// <summary>
        /// Retrieve double rewindable allocators of this rate system group.
        /// </summary>
        public DoubleRewindableAllocators* RateGroupAllocators => m_RateGroupAllocators;

        internal DoubleRewindableAllocators* CurrentGroupAllocators { get; set; } = null;

        /// <summary>
        /// Updates the group's systems
        /// </summary>
        protected override void OnUpdate()
        {
            CheckCreated();

            if (RateManager == null)
            {
                UpdateAllSystems();
            }
            else
            {
                while (RateManager.ShouldGroupUpdate(this))
                {
                    UpdateAllSystems();
                }
            }
        }

        void UpdateAllSystems()
        {
            if (m_systemSortDirty)
                SortSystems();

            // Cache the update list length before updating; any new systems added mid-loop will change the length and
            // should not be processed until the subsequent group update, to give SortSystems() a chance to run.
            int updateListLength = m_UpdateList.Length;
            for (int i = 0; i < updateListLength; ++i)
            {
                try
                {
                    var handle = m_UpdateList[i];
                    var world = WorldOf(handle);
                    var state = (OnUpdateBefore.IsCreated || OnUpdateAfter.IsCreated) ? world.ResolveSystemState(handle) : null;
                    if (OnUpdateBefore.IsCreated && state != null) OnUpdateBefore.Invoke(state->m_SystemTypeIndex, ref *state);
                    world.GetImpl().UpdateSystem(handle);
                    if (OnUpdateAfter.IsCreated && state != null) OnUpdateAfter.Invoke(state->m_SystemTypeIndex, ref *state);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                if (World.QuitUpdate)
                    break;
            }
        }

        /// <summary>
        /// Create a double rewindable allocators for rate sytem groups.
        /// </summary>
        internal void CreateRateGroupAllocators()
        {
            if (RateGroupAllocatorsCreated == 0)
            {
                m_RateGroupAllocators = Memory.Unmanaged.Allocate<DoubleRewindableAllocators>(Allocator.Persistent);
                m_RateGroupAllocators->Initialize(Allocator.Persistent, InitialSystemGroupAllocatorBlockSizeInBytes);
                RateGroupAllocatorsCreated = 1;
            }
        }

        /// <summary>
        /// Destroy the double rewindable allocators for rate sytem groups.
        /// </summary>
        void DestroyRateGroupAllocators()
        {
            m_RateGroupAllocators->Dispose();
            Memory.Unmanaged.Free(m_RateGroupAllocators, Allocator.Persistent);
            m_RateGroupAllocators = null;
            RateGroupAllocatorsCreated = 0;
        }
    }

    /// <summary>
    /// Obsolete. This class only contains deprecated extension methods.
    /// </summary>
    [Obsolete("This class will soon be empty and will be removed. (RemovedAfter Entities 1.0)")]
    public static class ComponentSystemGroupExtensions
    {
        /// <summary> Obsolete. Use <see cref="ComponentSystemGroup.RemoveSystemFromUpdateList(ComponentSystemBase)"/> instead.</summary>
        /// <param name="self">The component system group.</param>
        /// <param name="sysHandle">TheSystemHandle</param>
        [Obsolete("RemoveUnmanagedSystemFromUpdateList has been deprecated. Please use RemoveSystemFromUpdateList. (RemovedAfter Entities 1.0)")]
        public static void RemoveUnmanagedSystemFromUpdateList(this ComponentSystemGroup self, SystemHandle sysHandle)
        {
            self.RemoveSystemFromUpdateList(sysHandle);
        }

        /// <summary> Obsolete. Use <see cref="ComponentSystemGroup.AddSystemToUpdateList(ComponentSystemBase)"/> instead.</summary>
        /// <param name="self">The component system group.</param>
        /// <param name="sysHandle">TheSystemHandle</param>
        [Obsolete("AddUnmanagedSystemToUpdateList has been deprecated. Please use AddSystemToUpdateList. (RemovedAfter Entities 1.0)")]
        public static void AddUnmanagedSystemToUpdateList(this ComponentSystemGroup self, SystemHandle sysHandle)
        {
            self.AddSystemToUpdateList(sysHandle);
        }
    }
}
