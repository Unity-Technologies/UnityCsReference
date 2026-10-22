// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Assertions;
using Unity.Burst;
using static Unity.Burst.BurstRuntime;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using Unity.Mathematics;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Entities
{
    /// <summary> Obsolete. Use <see cref="SystemHandle"/> instead.</summary>
    [Obsolete("(UnityUpgradable) -> SystemHandle", true)]
    public struct SystemHandleUntyped : IEquatable<SystemHandleUntyped>, IComparable<SystemHandleUntyped>
    {
        internal Entity m_Entity;
        internal ushort m_Handle;
        internal ushort m_Version;
        internal uint m_WorldSeqNo;

        internal SystemHandleUntyped(Entity systemEntity, ushort handle, ushort version, uint worldSeqNo)
        {
            m_Entity = systemEntity;
            m_Handle = handle;
            m_Version = version;
            m_WorldSeqNo = worldSeqNo;
        }
        /// <inheritdoc cref="SystemHandle.CompareTo(SystemHandle)"/>
        public int CompareTo(SystemHandleUntyped other) => 0;
        /// <inheritdoc cref="SystemHandle.Equals(object)"/>
        public override bool Equals(object obj) => false;
        /// <inheritdoc cref="SystemHandle.Equals(SystemHandle)"/>
        public bool Equals(SystemHandleUntyped other) => false;
        /// <inheritdoc cref="SystemHandle.GetHashCode"/>
        public override int GetHashCode() => 0;
        /// <inheritdoc cref="SystemHandle.operator=="/>
        public static bool operator ==(SystemHandleUntyped lhs, SystemHandleUntyped rhs) => false;
        /// <inheritdoc cref="SystemHandle.operator!="/>
        public static bool operator !=(SystemHandleUntyped lhs, SystemHandleUntyped rhs) => false;
        /// <inheritdoc cref="SystemHandle.Update(WorldUnmanaged)"/>
        public void Update(WorldUnmanaged world) { }
    }

    /// <summary>
    /// An identifier representing a system instance in a particular world.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerTypeProxy(typeof(SystemDebugView))]
    public struct SystemHandle : IEquatable<SystemHandle>, IComparable<SystemHandle>
    {
        internal Entity m_Entity;
        internal ushort m_Handle;
        internal ushort m_Version;
        internal uint m_WorldSeqNo;

        ulong ToUlong()
        {
            return ((ulong)m_WorldSeqNo << 32) | ((ulong)m_Handle << 16) | (ulong)m_Version;
        }

        internal SystemHandle(Entity systemEntity, ushort handle, ushort version, uint worldSeqNo)
        {
            m_Entity = systemEntity;
            m_Handle = handle;
            m_Version = version;
            m_WorldSeqNo = worldSeqNo;
        }

        /// <summary>
        /// Implements IComparable interface for usage in generic sorted containers
        /// </summary>
        /// <param name="other">The SystemHandle being compared against</param>
        /// <returns>Value representing relative order compared to another SystemHandle</returns>
        public int CompareTo(SystemHandle other)
        {
            ulong a = ToUlong();
            ulong b = other.ToUlong();
            if (a < b)
                return -1;
            else if (a > b)
                return 1;
            return 0;
        }

        /// <summary>
        /// SystemHandle instances are equal if they refer to the same system instance.
        /// </summary>
        /// <param name="obj">The object to compare to this SystemHandle.</param>
        /// <returns>True, if the obj is a SystemHandle object and both SystemHandles represent the same system instance.</returns>
        public override bool Equals(object obj)
        {
            if (obj is SystemHandle foo)
                return Equals(foo);
            return false;
        }

        /// <summary>
        /// Implements IEquatable interface for usage in generic containers.
        /// SystemHandle instances are equal if they refer to the same system instance.
        /// </summary>
        /// <param name="other">Another SystemHandle instance.</param>
        /// <returns>True, if both SystemHandles represent the same system instance.</returns>
        public bool Equals(SystemHandle other)
        {
            return ToUlong() == other.ToUlong();
        }

        /// <summary>
        /// A hash used for comparisons.
        /// </summary>
        /// <returns>A reproducable hash code for this SystemHandle's contents</returns>
        public override int GetHashCode()
        {
            int hashCode = -116238775;
            hashCode = hashCode * -1521134295 + m_Handle.GetHashCode();
            hashCode = hashCode * -1521134295 + m_Version.GetHashCode();
            hashCode = hashCode * -1521134295 + m_WorldSeqNo.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// SystemHandle instances are equal if they refer to the same system instance.
        /// </summary>
        /// <param name="lhs">A SystemHandle instance.</param>
        /// <param name="rhs">Another SystemHandle instance.</param>
        /// <returns>True, if both SystemHandles represent the same system instance.</returns>
        public static bool operator==(SystemHandle lhs, SystemHandle rhs)
        {
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// SystemHandle instances are equal if they refer to the same system instance.
        /// </summary>
        /// <param name="lhs">A SystemHandle instance.</param>
        /// <param name="rhs">Another SystemHandle instance.</param>
        /// <returns>True, if the SystemHandles represent different system instances.</returns>
        public static bool operator!=(SystemHandle lhs, SystemHandle rhs)
        {
            return !lhs.Equals(rhs);
        }

        /// <summary>
        /// A "blank" SystemHandle that does not refer to an actual system.
        /// </summary>
        public static SystemHandle Null => default;

        /// <summary>
        /// Update the system manually.
        /// </summary>
        /// <remarks>
        /// If a system manually calls another system's <see cref="Update(WorldUnmanaged)"/> method from inside its own
        /// <see cref="ISystem.OnUpdate(ref SystemState)"/> method, <see cref="EntityQuery"/> objects in the caller
        /// system might see unexpected and incorrect change version numbers based on the processing performed in the
        /// target system. For this reason, you shouldn't manually update one system from another if both systems are
        /// processing entity data, especially if either uses <see cref="EntityQuery.SetChangedVersionFilter(ComponentType[])"/>.
        /// This guidance doesn't apply to <see cref="ComponentSystemGroup"/> or other "pass-through" systems which only
        /// update other systems without manipulating entity data.
        /// </remarks>
        /// <param name="world">The <see cref="WorldUnmanaged"/> for the <see cref="World"/> instance this handle belongs to.</param>
        /// <exception cref="InvalidOperationException">Thrown if this SystemHandle is invalid or does not belong to this world.</exception>
        public void Update(WorldUnmanaged world)
        {
            world.GetImpl().UpdateSystem(this);
        }
    }

    internal struct PerWorldSystemInfo
    {
        public SystemHandle handle;
        public int systemTypeIndex;
    }

    [BurstCompile]
    // Need access to AutoFreeAllocator for the ctor to compile in an external project
    //[GenerateTestsForBurstCompatibility]
    internal unsafe partial struct WorldUnmanagedImpl
    {
        internal AllocatorHelper<AutoFreeAllocator> m_WorldAllocatorHelper;
        internal NativeParallelHashMap<int, IntPtr> m_SystemStatePtrMap;
        private StateAllocator _stateMemory;
        private UnsafeParallelMultiHashMap<long, ushort> _unmanagedSlotByTypeHash;
        internal UnsafeList<PerWorldSystemInfo> sysHandlesInCreationOrder;
        internal readonly ulong SequenceNumber;
        public WorldFlags Flags;
        public TimeData CurrentTime;
        public FixedString128Bytes Name;
        public SystemHandle ExecutingSystem;
        public DoubleRewindableAllocators *DoubleUpdateAllocators;
        internal DoubleRewindableAllocators* GroupUpdateAllocators;
        internal EntityManager m_EntityManager;

        /// <summary>
        /// The maximum DeltaTime that will be applied to a World in a single call to Update().
        /// If the actual elapsed time since the previous frame exceeds this value, it will be clamped.
        /// This helps maintain a minimum frame rate after a large frame time spike, by spreading out the recovery over
        /// multiple frames.
        /// The value is expressed in seconds. The default value is 1/3rd seconds. Recommended values are 1/10th and 1/3rd seconds.
        /// </summary>
        public float MaximumDeltaTime;

        byte m_AllowGetSystem;

        internal delegate void UnmanagedUpdateSignature(void* pSystemState);
        [NoAutoStaticsCleanup] // Points at module code.
        private static UnmanagedUpdateSignature s_UnmanagedUpdateFn;

        // Initial memory block size in bytes
        const int InitialUpdateAllocatorBlockSizeInBytes = 128 * 1024;    // 128k


        public int Version { get; private set; }
        internal void BumpVersion() => Version++;

        public bool AllowGetSystem
        {
            get => m_AllowGetSystem != 0;
            internal set => m_AllowGetSystem = value ? (byte)1 : (byte)0;
        }

        internal WorldUnmanagedImpl(
            World worldManaged,
            ulong sequenceNumber,
            WorldFlags flags,
            AllocatorHelper<AutoFreeAllocator> worldAllocatorHelper,
            string worldName)
        {
            m_WorldAllocatorHelper = worldAllocatorHelper;
            var allocatorHandle = worldAllocatorHelper.Allocator.Handle;
            CurrentTime = default;
            Name = new FixedString128Bytes();
            Name.CopyFromTruncated(worldName);
            m_AllowGetSystem = 1;
            SequenceNumber = sequenceNumber;
            MaximumDeltaTime = 1.0f / 3.0f;
            Flags = flags;
            _unmanagedSlotByTypeHash = new UnsafeParallelMultiHashMap<long, ushort>(32, allocatorHandle);
            _stateMemory = default;
            _stateMemory.Init();
            Version = 0;
            ExecutingSystem = default;

            if (s_UnmanagedUpdateFn == null)
                s_UnmanagedUpdateFn = BurstCompiler.CompileFunctionPointer<UnmanagedUpdateSignature>(UnmanagedUpdate).Invoke;

            GroupUpdateAllocators = (DoubleRewindableAllocators*)Memory.Unmanaged.Allocate(sizeof(DoubleRewindableAllocators),
                                                                                                       JobsUtility.CacheLineSize,
                                                                                                       allocatorHandle);

            GroupUpdateAllocators[0] = new DoubleRewindableAllocators(allocatorHandle, InitialUpdateAllocatorBlockSizeInBytes);
            DoubleUpdateAllocators = GroupUpdateAllocators;

            m_SystemStatePtrMap = new NativeParallelHashMap<int, IntPtr>(32, Allocator.Persistent);
            sysHandlesInCreationOrder = new UnsafeList<PerWorldSystemInfo>(1, Allocator.Persistent);

            m_EntityManager = default;
        }

        internal bool UpdateAllocatorEnableBlockFree
        {
            get => DoubleUpdateAllocators->EnableBlockFree;
            set => DoubleUpdateAllocators->EnableBlockFree = value;
        }

        internal void SetGroupAllocator(DoubleRewindableAllocators* newGroupAllocators)
        {
            // Group allocators provided
            if (newGroupAllocators != null)
            {
                DoubleUpdateAllocators = newGroupAllocators;
                return;
            }
        }

        internal void RestoreGroupAllocator(DoubleRewindableAllocators* oldGroupAllocators)
        {
            // Group allocators provided
            if (oldGroupAllocators != null)
            {
                // Not the same as the old rate group allocator
                if (DoubleUpdateAllocators != oldGroupAllocators)
                {
                    DoubleUpdateAllocators->Update();
                }
                DoubleUpdateAllocators = oldGroupAllocators;
                return;
            }
        }

        [ExcludeFromBurstCompatTesting("DestroyInstance disposes managed lists")]
        internal void Dispose()
        {
            var allocatorHandle = m_WorldAllocatorHelper.Allocator.Handle;
            GroupUpdateAllocators[0].Dispose();
            Memory.Unmanaged.Free(GroupUpdateAllocators, allocatorHandle);

            m_EntityManager.DestroyInstance();
            m_EntityManager = default;

            _unmanagedSlotByTypeHash.Dispose();
            _stateMemory.Dispose();
            m_SystemStatePtrMap.Dispose();
            sysHandlesInCreationOrder.Dispose();
        }



        private void InvalidateSlot(ushort handle)
        {
            var block = _stateMemory.GetBlock(handle, out var subIndex);
            StateAllocator.IncVersion(ref block->Version[subIndex]);
            ++Version;
        }

        internal void FreeSlotWithoutOnDestroy(ushort handle, SystemState* statePtr)
        {
            var sysHandle = statePtr->SystemHandle;
            m_EntityManager.DestroyEntityInternal(&sysHandle.m_Entity, 1);
            Memory.Unmanaged.Free(statePtr->m_SystemPtr, Allocator.Persistent);
            m_SystemStatePtrMap.Remove(statePtr->m_SystemID);
            statePtr->Dispose();
            _stateMemory.Free(handle);
        }

        private void FreeSlot(ushort handle, SystemState* statePtr)
        {
            var systemPtr = statePtr->m_SystemPtr;
            // If the system ptr is not null, this is an unmanaged system and we need to actually destroy it.
            if (systemPtr != null)
            {
                PreviousSystemGlobalState state = new PreviousSystemGlobalState(ref this, statePtr);

                try
                {
                    if (statePtr->PreviouslyEnabled)
                    {
                        SystemBaseRegistry.CallOnStopRunning(statePtr);
                    }
                    SystemBaseRegistry.CallOnDestroy(statePtr);
                    state.Restore(ref this, statePtr);
                }
                finally
                {
                    state.Restore(ref this, statePtr);
                    FreeSlotWithoutOnDestroy(handle, statePtr);
                }
            }
            else
            {
                FreeSlotWithoutOnDestroy(handle, statePtr);
            }
        }

        private void FreeSlot(ushort handle)
        {
            var statePtr = _stateMemory.ResolveNoCheck(handle);
            FreeSlot(handle, statePtr);
        }

        private SystemHandle GetExistingUnmanagedSystem(long typeHash)
        {
            if (_unmanagedSlotByTypeHash.TryGetFirstValue(typeHash, out ushort handle, out _))
            {
                var block = _stateMemory.GetBlock(handle, out var subIndex);
                return new SystemHandle(block->States[subIndex].SystemHandle.m_Entity, handle, block->Version[subIndex], (uint)SequenceNumber);
            }
            return SystemHandle.Null;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSystem) })]
        internal SystemHandle GetExistingUnmanagedSystem<T>() where T : unmanaged, ISystem
            => GetExistingUnmanagedSystem(GetHashCode64<T>());

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSystem) })]
        internal ref SystemState GetExistingSystemState<T>()
        {
            var statePtr = ResolveSystemState(GetExistingUnmanagedSystem(TypeManager.GetSystemTypeHash<T>()));
            if (statePtr == null)
            {
                var name = TypeManager.GetSystemName(TypeManager.GetSystemTypeIndex<T>());
                throw new InvalidOperationException($"System {name} did not exist when calling GetExistingSystemState<{name}>. Please create it first.");
            }
            return ref *statePtr;
        }

        internal SystemHandle GetExistingUnmanagedSystem(Type t)
        {
            var hash = GetHashCode64(t);
            if (_unmanagedSlotByTypeHash.ContainsKey(hash))
            {
                return GetExistingUnmanagedSystem(hash);
            }
            return SystemHandle.Null;
        }

        internal SystemHandle GetExistingUnmanagedSystem(SystemTypeIndex t)
        {
            var hash = TypeManager.GetSystemTypeHash(t);
            if (_unmanagedSlotByTypeHash.ContainsKey(hash))
            {
                return GetExistingUnmanagedSystem(hash);
            }
            return SystemHandle.Null;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private void CheckSysHandleVersion(SystemHandle sysHandle)
        {
            if (sysHandle.m_Version == 0)
                throw new ArgumentException("sysHandle is invalid (default constructed)");
        }

        internal void InvalidateSystemHandle(SystemHandle sysHandle)
        {
            CheckSysHandleVersion(sysHandle);
            long typeHash = _stateMemory.GetTypeHashNoCheck(sysHandle.m_Handle);

            // TODO: Find other systems of same type in creation order, restore type lookup. Needed?
            _unmanagedSlotByTypeHash.Remove(typeHash, sysHandle.m_Handle);

            // Invalidate the slot so handles no longer resolve, but don't free the storage yet
            // as there could be live references in the parent scope. We can only free after the
            // calling system has updated
            InvalidateSlot(sysHandle.m_Handle);
        }

        internal void DestroyManagedSystem(SystemState* state)
        {
            InvalidateSystemHandle(state->m_Handle);
            FreeSlot(state->m_Handle.m_Handle, state);
        }

        internal void DestroyUnmanagedSystem(SystemHandle sysHandle)
        {
            InvalidateSystemHandle(sysHandle);
            FreeSlot(sysHandle.m_Handle);
        }

        private SystemHandle CreateUnmanagedSystemInternal(World self, int structSize, long typeHash, int typeIndex, out void* systemPtr, bool callOnCreate)
        {
            var metaIndex = SystemBaseRegistry.GetSystemTypeMetaIndex(typeHash);
            if (metaIndex == -1)
                throw new ArgumentException($"Type {TypeManager.GetSystemName(typeIndex)} couldn't be found in the SystemRegistry. (This is likely a bug in an ILPostprocessor.)");

            var statePtr = _stateMemory.Alloc(out var handle, out var version, typeHash);
            var systemEntity = CreateSystemEntity(self, typeIndex, statePtr);

            var systemHandle = new SystemHandle(systemEntity, handle, version, (uint) SequenceNumber);

            systemPtr = Memory.Unmanaged.Allocate(structSize, 16, Allocator.Persistent);
            UnsafeUtility.MemClear(systemPtr, structSize);
            statePtr->InitUnmanaged(self, systemHandle, metaIndex, systemPtr);

            ++Version;

            _unmanagedSlotByTypeHash.Add(typeHash, handle);
            m_SystemStatePtrMap.Add(statePtr->m_SystemID, (IntPtr)statePtr);

            if (callOnCreate)
                CallSystemOnCreateWithCleanup(statePtr);

            sysHandlesInCreationOrder.Add(new PerWorldSystemInfo
            {
                handle = systemHandle,
                systemTypeIndex = typeIndex
            });

            return systemHandle;
        }

        Entity CreateSystemEntity(World self, int typeIndex, SystemState* statePtr)
        {
            var componentTypes = new NativeArray<ComponentType>(2, Allocator.Temp);
            componentTypes[0] = ComponentType.ReadWrite<SystemInstance>();
            componentTypes[1] = ComponentType.ReadWrite<HideInHierarchy>();
            Entity systemEntity = self.EntityManager.CreateEntity(self.EntityManager.CreateArchetype(componentTypes));
            FixedString64Bytes systemName = default;
            systemName.CopyFromTruncated(TypeManager.GetSystemName(typeIndex));
            self.EntityManager.SetName(systemEntity, systemName);
            self.EntityManager.SetComponentData(systemEntity, new SystemInstance { state = statePtr });
            return systemEntity;
        }

        internal SystemState* AllocateSystemStateForManagedSystem(World self, ComponentSystemBase system)
        {
            var type = system.GetType();
            long typeHash = GetHashCode64(type);

            SystemState* statePtr = _stateMemory.Alloc(out var handle, out var version, typeHash);

            Entity systemEntity = default;
            if (!typeof(ComponentSystemGroup).IsAssignableFrom(type))
                systemEntity = CreateSystemEntity(self, TypeManager.GetSystemTypeIndex(type), statePtr);

            var safeHandle = new SystemHandle(systemEntity, handle, version, (uint) SequenceNumber);
            statePtr->InitManaged(self, safeHandle, type, system);
            _unmanagedSlotByTypeHash.Add(typeHash, handle);
            m_SystemStatePtrMap.Add(statePtr->m_SystemID, (IntPtr) statePtr);
            ++Version;

            return statePtr;
        }

        [ExcludeFromBurstCompatTesting("Takes managed World")]

        internal SystemHandle CreateUnmanagedSystem<T>(World self, bool callOnCreate) where T : unmanaged, ISystem
        {
            int systemTypeIndex = TypeManager.GetSystemTypeIndex<T>();

            if (!UnsafeUtility.IsUnmanaged<T>())
                throw new ArgumentException($"The system {TypeManager.GetSystemName(systemTypeIndex)} cannot contain managed fields. If you need have to store managed fields in your system, please use SystemBase instead.");

            var systemHandle = CreateUnmanagedSystemInternal(self, UnsafeUtility.SizeOf<T>(), GetHashCode64<T>(), systemTypeIndex, out var systemPtr, callOnCreate);

            EntitiesProfilerPackageCalls.OnSystemCreated(TypeManager.GetSystemTypeIndex<T>(), in systemHandle);

            return systemHandle;
        }

        [ExcludeFromBurstCompatTesting("Uses managed World")]
        private SystemHandle CreateUnmanagedSystem(SystemTypeIndex t, long typeHash, bool callOnCreate)
        {

            var systemType = TypeManager.GetSystemType(t);
            if (TypeManager.IsSystemManaged(systemType))
            {
                throw new ArgumentException($"The system {t} cannot contain managed fields. If you need have to store managed fields in your system, please use SystemBase instead. Reason: {UnsafeUtility.GetReasonForTypeNonBlittable(systemType)}");
            }


            var untypedHandle = CreateUnmanagedSystemInternal(m_EntityManager.World, TypeManager.GetSystemTypeSize(t), typeHash, t, out _, callOnCreate);

            EntitiesProfilerPackageCalls.OnSystemCreated(t, in untypedHandle);

            return untypedHandle;
        }

        internal void CallSystemOnCreateWithCleanup(SystemState* statePtr)
        {
            PreviousSystemGlobalState state = new PreviousSystemGlobalState(ref this, statePtr);

            try
            {
                // Bump global system version to mean that this system OnCreate begins
                statePtr->m_EntityComponentStore->IncrementGlobalSystemVersion(in statePtr->m_Handle);

                SystemBaseRegistry.CallOnCreateForCompiler(statePtr);
                SystemBaseRegistry.CallOnCreate(statePtr);
                state.Restore(ref this, statePtr);

                // Bump global system version again to mean that this system OnCreate ends
                statePtr->m_EntityComponentStore->IncrementGlobalSystemVersion();
            }
            catch
            {
                state.Restore(ref this, statePtr);
                var handle = statePtr->m_Handle.m_Handle;
                var typeHash = TypeManager.GetSystemTypeHash(SystemBaseRegistry.GetStructType(statePtr->UnmanagedMetaIndex));
                _unmanagedSlotByTypeHash.Remove(typeHash, handle);
                for (int i = 0; i < sysHandlesInCreationOrder.Length; i++)
                {
                    if (sysHandlesInCreationOrder[i].handle == statePtr->m_Handle)
                    {
                        sysHandlesInCreationOrder.RemoveAt(i);
                        break;
                    }
                }
                FreeSlotWithoutOnDestroy(handle, statePtr);
                throw;
            }
        }

        [ExcludeFromBurstCompatTesting("Uses managed World")]
        internal SystemHandle GetOrCreateUnmanagedSystem<T>(bool callOnCreate = true) where T : unmanaged, ISystem
        {
            if (_unmanagedSlotByTypeHash.ContainsKey(GetHashCode64<T>()))
            {
                return GetExistingUnmanagedSystem<T>();
            }
            else
            {
                return CreateUnmanagedSystem<T>(m_EntityManager.World, callOnCreate);
            }
        }

        /// <summary>
        /// Retrieves the SystemHandle for the specified SystemTypeIndex, creating the unmanaged system if it does not already exist.
        /// </summary>
        /// <param name="t">The SystemTypeIndex.</param>
        /// <returns>A SystemHandle.</returns>
        /// <remarks>In the case that the specified system is created, the system's OnCreate will be called automatically.</remarks>
        [ExcludeFromBurstCompatTesting("Uses managed World")]
        public SystemHandle GetOrCreateUnmanagedSystem(SystemTypeIndex t)
        {
            if (t.IsManaged)
            {
                throw new ArgumentException("The provided SystemTypeIndex targets a managed system.");
            }

            return GetOrCreateUnmanagedSystem(t, true);

        }

        [ExcludeFromBurstCompatTesting("Uses managed World")]
        internal SystemHandle GetOrCreateUnmanagedSystem(SystemTypeIndex t, bool callOnCreate = true)
        {
            long hash = TypeManager.GetSystemTypeHash(t);
            if (_unmanagedSlotByTypeHash.ContainsKey(hash))
            {
                return GetExistingUnmanagedSystem(hash);
            }
            else
            {
                return CreateUnmanagedSystem(t, hash, callOnCreate);
            }
        }

        internal SystemHandle CreateUnmanagedSystem(Type t, bool callOnCreate)
        {
            return CreateUnmanagedSystem(TypeManager.GetSystemTypeIndex(t), callOnCreate);
        }

        /// <summary>
        /// Creates an unmanaged system with the specified SystemTypeIndex and returns its SystemHandle.
        /// </summary>
        /// <param name="t">The target SystemTypeIndex for the new system.</param>
        /// <returns>A SystemHandle.</returns>
        /// <remarks>In the case that the specified system is created, the system's OnCreate will be called automatically.</remarks>
        public SystemHandle CreateUnmanagedSystem(SystemTypeIndex t)
        {
            if (t.IsManaged)
            {
                throw new ArgumentException("The provided SystemTypeIndex targets a managed system.");
            }

            return CreateUnmanagedSystem(t, true);
        }

        internal SystemHandle CreateUnmanagedSystem(SystemTypeIndex t, bool callOnCreate)
        {
            long hash = TypeManager.GetSystemTypeHash(t);
            return CreateUnmanagedSystem(t, hash, callOnCreate);
        }

        internal bool IsSystemValid(SystemHandle id) => ResolveSystemState(id) != null;

        internal SystemState* ResolveSystemState(SystemHandle id)
        {
            // Nothing can resolve while we're shutting down.
            if (m_AllowGetSystem == 0)
                return null;
            // System ID is for a different world.
            if (id.m_WorldSeqNo != (uint)SequenceNumber)
                return null;

            ushort handle = id.m_Handle;
            ushort version = id.m_Version;

            // System ID is out of bounds.
            if (handle >= 64 * 64)
                return null;

            return _stateMemory.Resolve(handle, version);
        }

        internal SystemState* ResolveSystemStateChecked(SystemHandle id)
        {
            // Nothing can resolve while we're shutting down.
            if (m_AllowGetSystem == 0)
                throw new InvalidOperationException("Shutdown in progress. Can not resolve systems.");
            // System ID is for a different world.
            if (id.m_WorldSeqNo != (uint)SequenceNumber)
                throw new InvalidOperationException("This system handle belongs to a different world.");

            ushort handle = id.m_Handle;
            ushort version = id.m_Version;

            // System ID is out of bounds.
            if (handle >= 64 * 64)
                throw new InvalidOperationException("System handle is invalid (ID out of bounds).");

            return _stateMemory.Resolve(handle, version);
        }

        internal NativeArray<SystemHandle> GetAllUnmanagedSystems(AllocatorManager.AllocatorHandle a)
        {
            int totalCount = 0;
            for (int i = 0; i < 64; ++i)
            {
                var blockPtr = _stateMemory.m_Level1 + i;
                totalCount += math.countbits(~blockPtr->FreeBits);
            }

            var outputIndex = 0;
            var result = CollectionHelper.CreateNativeArray<SystemHandle>(totalCount, a);

            for (int i = 0; i < 64; ++i)
            {
                var blockPtr = _stateMemory.m_Level1 + i;

                var allocBits = ~blockPtr->FreeBits;

                while (allocBits != 0)
                {
                    int bit = math.tzcnt(allocBits);

                    var systemState = _stateMemory.GetState((ushort) (i * 64 + bit));
                    if (systemState->m_SystemPtr != null)
                        result[outputIndex++] = systemState->m_Handle;

                    allocBits &= ~(1ul << bit);
                }
            }

            result.m_Length = outputIndex;

            return result;
        }

        internal NativeArray<SystemHandle> GetAllSystems(AllocatorManager.AllocatorHandle a)
        {
            int totalCount = 0;
            for (int i = 0; i < 64; ++i)
            {
                var blockPtr = _stateMemory.m_Level1 + i;
                totalCount += math.countbits(~blockPtr->FreeBits);
            }

            var outputIndex = 0;
            var result = CollectionHelper.CreateNativeArray<SystemHandle>(totalCount, a);

            for (int i = 0; i < 64; ++i)
            {
                var blockPtr = _stateMemory.m_Level1 + i;

                var allocBits = ~blockPtr->FreeBits;

                while (allocBits != 0)
                {
                    int bit = math.tzcnt(allocBits);

                    var systemState = _stateMemory.GetState((ushort)(i * 64 + bit));
                    result[outputIndex++] = systemState->m_Handle;

                    allocBits &= ~(1ul << bit);
                }
            }

            return result;
        }

        internal struct PreviousSystemGlobalState
        {
            public SystemHandle OldExecutingSystem;
            public int                 OldIsInForEachDisallowStructuralChange;
            public int                 OldPreviousSystemID;
            public PreviousSystemGlobalState(ref WorldUnmanagedImpl world, SystemState* state)
            {
                OldExecutingSystem = world.ExecutingSystem;
                world.ExecutingSystem = state->m_Handle;
                OldIsInForEachDisallowStructuralChange = state->m_DependencyManager->ForEachStructuralChange.Depth;
                OldPreviousSystemID = SystemState.SetCurrentSystemIdForJobDebugger(state->m_SystemID);
            }

            public void Restore(ref WorldUnmanagedImpl world, SystemState* state)
            {
                world.ExecutingSystem = OldExecutingSystem;
                state->m_DependencyManager->ForEachStructuralChange.SetIsInForEachDisallowStructuralChangeCounter(OldIsInForEachDisallowStructuralChange);
                SystemState.SetCurrentSystemIdForJobDebugger(OldPreviousSystemID);
                state->DisableIsExecutingOnUpdate();
            }
        }

        [BurstCompile]
        internal static void UnmanagedUpdate(void* pSystemState)
        {
            SystemState* state = (SystemState*) pSystemState;

            if (SystemBaseRegistry.IsOnUpdateUsingBurst(state))
            {
                state->SetWasUsingBurstProfilerMarker(true);
                state->m_ProfilerMarkerBurst.Begin();
            }
            else
            {
                state->SetWasUsingBurstProfilerMarker(false);
                state->m_ProfilerMarker.Begin();
            }
            state->BeforeUpdateResetRunTracker();

            if (state->Enabled && state->ShouldRunSystem())
            {
                state->BeforeOnUpdate();

                if (!state->PreviouslyEnabled)
                {
                    state->PreviouslyEnabled = true;
                    SystemBaseRegistry.CallOnStartRunning(state);
                }

                state->EnableIsExecutingISystemOnUpdate();
                SystemBaseRegistry.CallOnUpdate(state);

                state->AfterOnUpdate();
            }
            else if (state->PreviouslyEnabled)
            {
                state->PreviouslyEnabled = false;
                state->DisableIsExecutingOnUpdate();
                state->BeforeOnUpdate();
                SystemBaseRegistry.CallOnStopRunning(state);
                state->AfterOnUpdate();
            }
            if (state->WasUsingBurstProfilerMarker())
                state->m_ProfilerMarkerBurst.End();
            else
                state->m_ProfilerMarker.End();
        }

        internal void UpdateSystem(SystemHandle sh)
        {
            if (sh == SystemHandle.Null)
                throw new NullReferenceException("The system couldn't be updated. The SystemHandle is default/null, so was never assigned.");

            SystemState* sys = ResolveSystemStateChecked(sh);
            if (sys == null)
                throw new NullReferenceException("The system couldn't be resolved. The System has been destroyed.");

            if (sys->m_ManagedSystem.IsAllocated)
            {
                sys->ManagedSystem.Update();
                return;
            }

            var previousState = new PreviousSystemGlobalState(ref this, sys);

            try
            {
                UnmanagedUpdate(sys);
            }
            catch
            {
                sys->AfterOnUpdate();

                previousState.Restore(ref this, sys);
                if (sys->WasUsingBurstProfilerMarker())
                    sys->m_ProfilerMarkerBurst.Begin();
                else
                    sys->m_ProfilerMarker.Begin();

                // Limit follow up errors if we arrived here due to a job related exception by syncing all jobs
                sys->m_DependencyManager->Safety.PanicSyncAll();

                throw;
            }

            previousState.Restore(ref this, sys);
        }
    }

    /// <summary>
    /// A pointer-to-implementation for the unmanaged representation of a World.
    /// </summary>
    /// <remarks>This is intended to stay small (8 bytes without collections checks, 32 bytes with padding with
    /// collections checks), because it is intended to be cheaply passed around by value.
    /// </remarks>
    [GenerateTestsForBurstCompatibility]
    public unsafe struct WorldUnmanaged : IEquatable<WorldUnmanaged>, IComparable<WorldUnmanaged>
    {
        private AtomicSafetyHandle m_Safety;
        internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<WorldUnmanaged>();
        private WorldUnmanagedImpl* m_Impl;

        /// <summary>
        /// An interface to manipulate the World's entity data from the main thread
        /// </summary>
        public EntityManager EntityManager => GetImpl().m_EntityManager;

        /// <summary>
        /// The name of the World
        /// </summary>
        public FixedString128Bytes Name => GetImpl().Name;

        internal SystemHandle ExecutingSystem
        {
            get => GetImpl().ExecutingSystem;
            set => GetImpl().ExecutingSystem = value;
        }

        /// <summary>
        /// The world's simulated time, include elapsed time since World creation and the delta time since the previous update.
        /// </summary>
        public ref TimeData Time => ref GetImpl().CurrentTime;

        /// <summary>
        /// The <see cref="WorldFlags"/> settings for this World
        /// </summary>
        public WorldFlags Flags => GetImpl().Flags;

        /// <summary>
        /// The maximum single-frame delta time permitted in this world.
        /// </summary>
        /// <remarks>If a frame takes longer than this to simulate and render, the delta time reported for the next
        /// frame will be clamped to this value. This can prevent an out-of-control negative feedback loop.</remarks>
        public float MaximumDeltaTime
        {
            get => GetImpl().MaximumDeltaTime;
            set => GetImpl().MaximumDeltaTime = value;
        }

        /// <summary>
        /// The World's current sequence number
        /// </summary>
        public ulong SequenceNumber => GetImpl().SequenceNumber;

        /// <summary>
        /// The World's current version number
        /// </summary>
        public int Version => GetImpl().Version;

        internal void BumpVersion() => GetImpl().BumpVersion();

        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        internal bool AllowGetSystem
        {
            get => GetImpl().AllowGetSystem;
            set => GetImpl().AllowGetSystem = value;
        }

        /// <summary>Increments whenever a world is created or destroyed, or when an EntityQuery is created.</summary>
        internal static readonly SharedStatic<ulong> NextSequenceNumber = SharedStatic<ulong>.GetOrCreate<World>();

        internal bool UpdateAllocatorEnableBlockFree
        {
            get => m_Impl->UpdateAllocatorEnableBlockFree;
            set => m_Impl->UpdateAllocatorEnableBlockFree = value;
        }

        /// <summary>
        /// Has this World been successfully initialized?
        /// </summary>
        public bool IsCreated => m_Impl != null;

        [ExcludeFromBurstCompatTesting("Takes managed World")]
        internal void Create(World world, WorldFlags flags, AllocatorManager.AllocatorHandle backingAllocatorHandle)
        {
            var worldAllocatorHelper = new AllocatorHelper<AutoFreeAllocator>(backingAllocatorHandle);
            worldAllocatorHelper.Allocator.Initialize(backingAllocatorHandle);
            var allocatorHandle = worldAllocatorHelper.Allocator.Handle;

            var ptr = allocatorHandle.Allocate(sizeof(WorldUnmanagedImpl), 16, 1);
            m_Impl = (WorldUnmanagedImpl*)ptr;
            m_Safety = AtomicSafetyHandle.Create();
            CollectionHelper.SetStaticSafetyId(ref m_Safety, ref s_staticSafetyId.Data, "Unity.Entities.WorldUnmanaged");
            UnsafeUtility.AsRef<WorldUnmanagedImpl>(m_Impl) = new WorldUnmanagedImpl(world,
                NextSequenceNumber.Data++,
                flags,
                worldAllocatorHelper,
                world.Name);

            /*
             * if we init the entitymanager inside the WorldUnmanagedImpl ctor, m_Impl will not be set, and so when the
             * EM asks for the sequence number, it will ask for GetImpl().SequenceNumber and get uninitialized data.
             * so, init it here instead.
             */
            m_Impl->m_EntityManager.Initialize(world);
        }

        [ExcludeFromBurstCompatTesting("AllocatorManager accesses managed delegates")]
        internal void Dispose()
        {
            AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
            AtomicSafetyHandle.Release(m_Safety);
            var allocatorHandle = m_Impl->m_WorldAllocatorHelper.Allocator.Handle;
            var worldAllocatorHelper = m_Impl->m_WorldAllocatorHelper;

            UnsafeUtility.AsRef<WorldUnmanagedImpl>(m_Impl).Dispose();
            allocatorHandle.Free(m_Impl, sizeof(WorldUnmanagedImpl), 16, 1);
            m_Impl = null;

            worldAllocatorHelper.Allocator.Dispose();
            worldAllocatorHelper.Dispose();

            ++NextSequenceNumber.Data;
        }

        internal ref WorldUnmanagedImpl GetImpl()
        {
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            return ref UnsafeUtility.AsRef<WorldUnmanagedImpl>(m_Impl);
        }
        internal WorldUnmanagedImpl* GetImplPtr()
        {
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            return m_Impl;
        }

        /// <summary>
        /// Rewindable allocator instance for this World.
        /// </summary>
        /// <remarks>Useful for fast, thread-safe temporary memory allocations which do not need to be explicitly disposed.
        /// All allocations from this allocator are automatically disposed en masse after two full World updates.  Behind
        /// the world update allocator are double rewindable allocators, and the two allocators are switched in each world
        /// update.  Therefore user should not cache the world update allocator. </remarks>
        public ref RewindableAllocator UpdateAllocator => ref GetImpl().DoubleUpdateAllocators->Allocator;

        /// <summary>
        /// Manually update the double update allocators.  This switches to the other allocator and rewinds the newly switched allocator.
        /// </summary>
        public void ResetUpdateAllocator()
        {
            GetImpl().DoubleUpdateAllocators->Update();
        }

        /// <returns>Null if not found.</returns>
        [GenerateTestsForBurstCompatibility]
        internal SystemState* TryGetSystemStateForId(int systemId)
        {
            return GetImpl().m_SystemStatePtrMap.TryGetValue(systemId, out var systemStatePtr) ? (SystemState*) systemStatePtr : null;
        }

        [ExcludeFromBurstCompatTesting("Takes managed system")]
        internal SystemState* AllocateSystemStateForManagedSystem(World self, ComponentSystemBase system) =>
            GetImpl().AllocateSystemStateForManagedSystem(self, system);

        [ExcludeFromBurstCompatTesting("Takes managed World")]
        internal SystemHandle CreateUnmanagedSystem<T>(World self, bool callOnCreate) where T : unmanaged, ISystem =>
            GetImpl().CreateUnmanagedSystem<T>(self, callOnCreate);

        [ExcludeFromBurstCompatTesting("Uses managed World under the hood")]
        internal SystemHandle GetOrCreateUnmanagedSystem<T>() where T : unmanaged, ISystem =>
            GetImpl().GetOrCreateUnmanagedSystem<T>();

        [ExcludeFromBurstCompatTesting("Uses managed World under the hood")]
        internal SystemHandle GetOrCreateUnmanagedSystem(SystemTypeIndex unmanagedType) =>
            GetImpl().GetOrCreateUnmanagedSystem(unmanagedType);

        [ExcludeFromBurstCompatTesting("Uses managed World under the hood")]
        internal SystemHandle CreateUnmanagedSystem(SystemTypeIndex unmanagedType, bool callOnCreate) =>
            GetImpl().CreateUnmanagedSystem(unmanagedType, callOnCreate);

        [ExcludeFromBurstCompatTesting("accesses managed World")]
        internal void DestroyUnmanagedSystem(SystemHandle sysHandle)
        {
            if (ExecutingSystem != default)
                throw new ArgumentException("A system can not be disposed while another system in that world is executing");
            GetImpl().DestroyUnmanagedSystem(sysHandle);
        }


        [ExcludeFromBurstCompatTesting("accesses managed World")]
        internal void DestroyManagedSystemState(SystemState* state) =>
            GetImpl().DestroyManagedSystem(state);

        internal SystemState* ResolveSystemStateChecked(SystemHandle id) => GetImpl().ResolveSystemStateChecked(id);
        internal SystemState* ResolveSystemState(SystemHandle id) => GetImpl().ResolveSystemState(id);

        /// <summary>
        /// Resolves the system handle to a reference to its underlying system state.
        /// </summary>
        /// <param name="id">The system handle</param>
        /// <returns>A reference to the <see cref="SystemState"/> for this system</returns>
        /// <exception cref="InvalidOperationException">Thrown if the system handle is invalid or does not belong to this world.</exception>
        public ref SystemState ResolveSystemStateRef(SystemHandle id)
            => ref *(GetImpl().ResolveSystemStateChecked(id));

        /// <summary>
        /// Resolves the system handle to a reference to its underlying system instantiation.
        /// </summary>
        /// <remarks>
        /// This system reference is not guaranteed to be safe to use. If the system or world is destroyed then the reference
        /// becomes invalid and will likely lead to corrupted program state if used.
        ///
        /// Generally, instead of public member data, prefer using component data for system level data that needs to be
        /// shared between systems or externally to them. This defines a data protocol for the system which is separated
        /// from the system functionality.
        ///
        /// Private member data which is only used internally to the system is recommended.
        ///
        /// `GetUnsafeSystemRef`, though provided as a backdoor to system instance data, is not recommended for
        /// usage in production or any non-throwaway code, as it
        /// - encourages coupling of data and functionality
        /// - couples data to the system type with no direct path to decouple
        /// - does not provide lifetime or thread safety guarantees for data
        /// - does not provide lifetime or thread safety guarantees for system access through the returned ref
        /// </remarks>
        /// <typeparam name="T">The system type</typeparam>
        /// <param name="id">The system handle</param>
        /// <returns>A reference to the concrete <see cref="ISystem"/> compatible instance for this system</returns>
        /// <exception cref="InvalidOperationException">Thrown if the system handle is invalid or does not belong to this world.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(BurstCompatibleSystem) })]
        public ref T GetUnsafeSystemRef<T>(SystemHandle id) where T : unmanaged, ISystem
        {
            ref var systemState = ref ResolveSystemStateRef(id);
            return ref UnsafeUtility.AsRef<T>(systemState.m_SystemPtr);
        }

        [ExcludeFromBurstCompatTesting("Returns managed Type")]
        internal Type GetTypeOfSystem(SystemHandle SystemHandle)
        {
            SystemState* s = ResolveSystemState(SystemHandle);
            if (s != null)
            {
                if (s->m_ManagedSystem.IsAllocated)
                {
                    return s->m_ManagedSystem.Target.GetType();
                }
                return SystemBaseRegistry.GetStructType(s->UnmanagedMetaIndex);
            }

            return null;
        }

        /// <summary>
        /// Gets the SystemTypeIndex for the specified SystemHandle.
        /// </summary>
        /// <param name="SystemHandle">The SystemHandle</param>
        /// <returns>A SystemTypeIndex</returns>
        /// <exception cref="InvalidOperationException">Thrown if the system handle could not be resolved to a SystemTypeIndex.</exception>
        public SystemTypeIndex GetSystemTypeIndex(SystemHandle SystemHandle)
        {
            SystemState* s = ResolveSystemStateChecked(SystemHandle);
            return s->m_SystemTypeIndex;
        }

        /// <summary>
        /// Return an existing instance of a system of type <typeparamref name="T"/> in this World.
        /// </summary>
        /// <typeparam name="T">The system type</typeparam>
        /// <returns>The existing instance of system type <typeparamref name="T"/> in this World. If no such instance exists, the method returns default.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[]{typeof(BurstCompatibleSystem)})]
        public SystemHandle GetExistingUnmanagedSystem<T>() where T : unmanaged, ISystem =>
            GetImpl().GetExistingUnmanagedSystem<T>();

        /// <summary>
        /// Return the system state for an existing instance of a system of type <typeparamref name="T"/> in this World.
        /// </summary>
        /// <typeparam name="T">The system type</typeparam>
        /// <returns>The system state for the existing instance of system type <typeparamref name="T"/> in this World. If no such instance exists, throws.</returns>
        /// <exception cref="InvalidOperationException">Thrown if no such system of type <typeparamref name="T"/> exists in the World.</exception>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[]{typeof(BurstCompatibleSystem)})]
        public ref SystemState GetExistingSystemState<T>() =>
            ref GetImpl().GetExistingSystemState<T>();

        /// <summary>
        /// Return an existing instance of a system of type <paramref name="type"/> in this World. Prefer the version
        /// that takes a SystemTypeIndex to avoid unnecessary reflection.
        /// </summary>
        /// <param name="type">The system type</param>
        /// <returns>The existing instance of system type <paramref name="type"/> in this World. If no such instance exists, the method returns SystemHandle.Null.</returns>
        [ExcludeFromBurstCompatTesting("Takes System.Type")]
        public SystemHandle GetExistingUnmanagedSystem(Type type) =>
            GetImpl().GetExistingUnmanagedSystem(type);

        /// <summary>
        /// Return an existing instance of a system of type <paramref name="type"/> in this World. This avoids
        /// unnecessary reflection.
        /// </summary>
        /// <param name="type">The system type</param>
        /// <returns>The existing instance of system type <paramref name="type"/> in this World. If no such instance exists, the method returns SystemHandle.Null.</returns>
        public SystemHandle GetExistingUnmanagedSystem(SystemTypeIndex type) =>
            GetImpl().GetExistingUnmanagedSystem(type);

        /// <summary>
        /// Checks whether a system identified by its system handle exists and is in a valid state
        /// </summary>
        /// <param name="id">The system handle</param>
        /// <returns>True if the system handle identifies a valid system, false otherwise</returns>
        public bool IsSystemValid(SystemHandle id) =>
            GetImpl().IsSystemValid(id);

        /// <summary> Obsolete. Use <see cref="WorldUnmanaged.GetUnsafeSystemRef{T}(SystemHandle)"/> instead.</summary>
        /// <param name="systemHandle">The system handle</param>
        /// <typeparam name="T">The unmanaged system</typeparam>
        /// <returns></returns>
        [Obsolete("Use GetUnsafeSystemRef (UnityUpgradable) -> GetUnsafeSystemRef<T>(*)", true)]
        public ref T ResolveSystem<T>(SystemHandle systemHandle) where T : unmanaged, ISystem
        {
            var ptr = ResolveSystemState(systemHandle);
            CheckSystemReference(ptr);
            return ref UnsafeUtility.AsRef<T>(ptr->m_SystemPtr);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        internal void CheckSystemReference(SystemState* ptr)
        {
            if (ptr == null)
                throw new InvalidOperationException("System reference is not valid");
        }

        /// <summary>
        /// Retrieve an array of all unmanaged systems in this world
        /// </summary>
        /// <param name="a">Allocator used for the returned container</param>
        /// <returns>An array of system instances</returns>
        public NativeArray<SystemHandle> GetAllUnmanagedSystems(AllocatorManager.AllocatorHandle a)
        {
            return GetImpl().GetAllUnmanagedSystems(a);
        }

        /// <summary>
        /// Retrieve an array of all systems in this world, both unmanaged and managed
        /// </summary>
        /// <param name="a">Allocator used for the returned container</param>
        /// <returns>An array of system instances</returns>
        public NativeArray<SystemHandle> GetAllSystems(AllocatorManager.AllocatorHandle a)
        {
            return GetImpl().GetAllSystems(a);
        }

        [ExcludeFromBurstCompatTesting("Accesses managed World under the hood")]
        unsafe internal NativeArray<SystemHandle> GetOrCreateUnmanagedSystems(NativeList<SystemTypeIndex> unmanagedTypes)
        {
            int count = unmanagedTypes.Length;
            var result = new NativeArray<SystemHandle>(count, Allocator.Temp);

            var impl = GetImplPtr();
            for (int i = 0; i < count; ++i)
                result[i] = impl->GetOrCreateUnmanagedSystem(unmanagedTypes[i], false);

            for (int i = 0; i < count; ++i)
            {
                var systemState = impl->ResolveSystemState(result[i]);
                if (systemState != null)
                    impl->CallSystemOnCreateWithCleanup(systemState);
            }

            return result;
        }

        internal void GetInfo(out ulong sequenceNumber, out SystemHandle executingSystem)
        {
            var impl = GetImpl();
            sequenceNumber = impl.SequenceNumber;
            executingSystem = impl.ExecutingSystem;
        }
        public override bool Equals(object compare)
        {
            return compare is WorldUnmanaged compareWorld && Equals(compareWorld);
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = SequenceNumber.GetHashCode();
                hashCode = (hashCode * 397) ^ Version;
                return hashCode;
            }
        }        
        
        public bool Equals(WorldUnmanaged world)
        {
            return world.SequenceNumber == SequenceNumber && world.Version == Version;
        }

        /// <summary>
        /// Compares this world with another by sequence number, then by version.
        /// </summary>
        /// <param name="other">The world to compare with.</param>
        /// <returns>A negative value, zero or a positive value if this world sorts before, the same as or after <paramref name="other"/>.</returns>
        public int CompareTo(WorldUnmanaged other)
        {
            var compareVal = SequenceNumber.CompareTo(other.SequenceNumber);
            return compareVal != 0 ? compareVal : Version.CompareTo(other.Version);
        }
        
        /// <summary>
        /// Whether two references point to the same world.
        /// </summary>
        /// <param name="lhs">The first world.</param>
        /// <param name="rhs">The second world.</param>
        /// <returns>True if both have the same sequence number and version.</returns>
        public static bool operator==(WorldUnmanaged lhs, WorldUnmanaged rhs)
        {
            return lhs.SequenceNumber == rhs.SequenceNumber && lhs.Version == rhs.Version;
        }
        
        /// <summary>
        /// Whether two references point to different worlds.
        /// </summary>
        /// <param name="lhs">The first world.</param>
        /// <param name="rhs">The second world.</param>
        /// <returns>True if the sequence numbers or versions differ.</returns>
        public static bool operator!=(WorldUnmanaged lhs, WorldUnmanaged rhs)
        {
            return !(lhs == rhs);
        }        
    }
}
