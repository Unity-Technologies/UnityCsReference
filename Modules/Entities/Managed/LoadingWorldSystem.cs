// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Debug = Unity.Scripting.LowLevel.Debug;

namespace Unity.Entities
{
    // Lives in the World a load moves into and owns the idle streaming loading Worlds, the way SceneSectionStreamingSystem
    // owns its streaming Worlds. It keeps a few between loads and disposes them with itself. A load owns its loading World
    // until it gives it back, so the World outlives a destroyed system. Such a load's entities are destroyed, never moved
    // into another World, and its loading World is disposed when the load gives it back.
    // Everything here runs on the main thread.
    [DisableAutoCreation]
    unsafe partial struct LoadingWorldSystem : ISystem
    {
        struct InUseLoadingWorld
        {
            public WorldUnmanaged Loading;
            public WorldUnmanaged Target;
            public ulong TargetSequenceNumber;
            public bool TargetDestroyed;
        }

        const int k_KeptIdleCount = 2;

        internal const string k_WorldName = "Loading World";

        const string k_NotEmptyAfterSyncError = "Loading World '{0}' still had entities after its sync. They were destroyed.";

        const string k_InUseAtShutdownError = "{0} loading World(s) are still in use while Entities shuts down. They are disposed now.";

        // Loads in progress for every World, since a load gives back only its loading World. Disposed when Entities shuts down.
        static readonly SharedStatic<UnsafeList<InUseLoadingWorld>> s_InUse = SharedStatic<UnsafeList<InUseLoadingWorld>>.GetOrCreate<InUseLoadingWorld>();

        NativeList<WorldUnmanaged> m_Idle;

        internal static int InUseCount => s_InUse.Data.IsCreated ? s_InUse.Data.Length : 0;

        internal int IdleCount => m_Idle.Length;

        public void OnCreate(ref SystemState state)
        {
            m_Idle = new NativeList<WorldUnmanaged>(k_KeptIdleCount + 1, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            foreach (var world in m_Idle)
            {
                DisposeWorld(world);
            }
            m_Idle.Dispose();

            var target = state.WorldUnmanaged.SequenceNumber;
            ref var inUse = ref s_InUse.Data;
            for (int i = 0; i < InUseCount; ++i)
            {
                ref var entry = ref inUse.ElementAt(i);
                if (entry.TargetSequenceNumber == target)
                    entry.TargetDestroyed = true;
            }
        }

        // A loading thread changes the loading World only through the returned transaction
        internal static WorldUnmanaged Acquire(WorldUnmanaged target, bool forLoadingThread, out ExclusiveEntityTransaction transaction)
        {
            transaction = default;
            ref var system = ref GetSystem(target, target.GetImpl().GetOrCreateUnmanagedSystem<LoadingWorldSystem>());
            var loading = system.TakeIdle();
            if (!loading.IsCreated)
                loading = new World(k_WorldName, WorldFlags.Streaming, target).Unmanaged;
            try
            {
                if (forLoadingThread)
                    transaction = GlobalEntityManager.BeginExclusiveEntityTransaction(loading);
            }
            catch
            {
                DisposeWorld(loading);
                throw;
            }

            // Owning itself, the World stays until the load gives it back
            loading.SetOwner(loading);
            if (!s_InUse.Data.IsCreated)
                s_InUse.Data = new UnsafeList<InUseLoadingWorld>(k_KeptIdleCount, Allocator.Persistent);
            s_InUse.Data.Add(new InUseLoadingWorld { Loading = loading, Target = target, TargetSequenceNumber = target.SequenceNumber });
            return loading;
        }

        static ref LoadingWorldSystem GetSystem(WorldUnmanaged target, SystemHandle handle) =>
            ref target.GetUnsafeSystemRef<LoadingWorldSystem>(handle);

        WorldUnmanaged TakeIdle()
        {
            if (m_Idle.Length == 0)
                return default;
            var world = m_Idle[m_Idle.Length - 1];
            m_Idle.RemoveAt(m_Idle.Length - 1);
            return world;
        }

        internal static bool EndLoadingThreadAccess(WorldUnmanaged loading)
        {
            var index = FindInUse(loading);
            if (index < 0)
                return false;
            EndLoadingThreadAccessOf(loading);
            return true;
        }

        static void EndLoadingThreadAccessOf(WorldUnmanaged loading)
        {
            if (loading.EntityManager.GetUncheckedEntityDataAccess()->IsInExclusiveTransaction)
                GlobalEntityManager.EndExclusiveEntityTransaction(loading);
        }

        // Moved entities keep their ids, which GameObjects hold
        internal static bool SyncAndRelease(WorldUnmanaged loading) => Release(loading, true);

        internal static bool Release(WorldUnmanaged loading) => Release(loading, false);

        static bool Release(WorldUnmanaged handle, bool sync)
        {
            var index = FindInUse(handle);
            if (index < 0)
                return false;

            var entry = s_InUse.Data[index];
            s_InUse.Data.RemoveAtSwapBack(index);
            var loading = entry.Loading;
            try
            {
                EndLoadingThreadAccessOf(loading);
                var entityManager = loading.EntityManager;
                if (!entityManager.UniversalQuery.IsEmptyIgnoreFilter)
                {
                    // A load only ever syncs into the World it started in. When that World is gone, so are its entities.
                    if (sync && !entry.TargetDestroyed)
                    {
                        entry.Target.EntityManager.MoveEntitiesFromWithoutRemapping(entityManager);
                        if (!entityManager.UniversalQuery.IsEmptyIgnoreFilter)
                            Debug.LogError(string.Format(k_NotEmptyAfterSyncError, loading.Name));
                    }

                    // PrepareForDeserialize throws if an entity survived the destroy, for example one with a cleanup component
                    entityManager.DestroyEntity(entityManager.UniversalQuery);
                    entityManager.PrepareForDeserialize();
                }

                if (entry.TargetDestroyed)
                {
                    DisposeWorld(loading);
                    return true;
                }

                loading.SetOwner(entry.Target);
                ref var idle = ref GetSystem(entry.Target, entry.Target.GetExistingUnmanagedSystem<LoadingWorldSystem>()).m_Idle;
                idle.Add(loading);
                while (idle.Length > k_KeptIdleCount)
                {
                    var extra = idle[idle.Length - 1];
                    idle.RemoveAt(idle.Length - 1);
                    DisposeWorld(extra);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                if (loading.IsCreated)
                    DisposeWorld(loading);
            }
            return true;
        }

        static int FindInUse(WorldUnmanaged loading)
        {
            for (int i = 0; i < InUseCount; ++i)
            {
                var handle = s_InUse.Data[i].Loading;
                if (UnsafeUtility.MemCmp(&handle, &loading, sizeof(WorldUnmanaged)) == 0)
                    return i;
            }
            return -1;
        }

        // Disposing a World still goes through the managed World
        static void DisposeWorld(WorldUnmanaged world) => world.EntityManager.World.Dispose();

        // Every load has normally given its loading World back by now
        internal static void DisposeInUseAtShutdown()
        {
            if (!s_InUse.Data.IsCreated)
                return;

            if (s_InUse.Data.Length > 0)
                Debug.LogError(string.Format(k_InUseAtShutdownError, s_InUse.Data.Length));
            foreach (var entry in s_InUse.Data)
            {
                if (entry.Loading.IsCreated)
                    DisposeWorld(entry.Loading);
            }
            s_InUse.Data.Dispose();
            s_InUse.Data = default;
        }
    }
}
