// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// One record per registered component type, holding the type's store: an
    /// <see cref="UnmanagedDataStore"/> when the type carries no managed reference, a
    /// <see cref="ManagedComponentRegistry{T}"/> otherwise. Owned canonically by
    /// <see cref="ComponentManager"/> (for disposal and enumeration) and cached on
    /// <see cref="ComponentManager{T}"/> and on each <see cref="ComponentSlot"/> so the per-element
    /// teardown path can free a slot without a dictionary lookup (which matters on the GC finalizer thread).
    /// </summary>
    class PerTypeStore
    {
        public UnmanagedDataStore store;             // valid when managedRegistry == null
        public ManagedComponentRegistry managedRegistry;
        public RuntimeTypeHandle typeHandle;

        // Number of live slots for this type. Mutated on the main thread only: incremented by
        // AddComponent, decremented when the deferred free is drained by Collect().
        public int liveCount;

        // Trampoline into the type's [ReleaseComponentResources] method; null when it declares none.
        // Unmanaged types only — a managed component's box calls the typed handler with a ref into itself.
        public unsafe delegate* managed<void*, void> releaseResources;

        public bool isValid => managedRegistry != null ? managedRegistry.IsValid : store.IsValid;

        // Runs the type's [ReleaseComponentResources] method, then frees the slot — the free overwrites the
        // slot with the default value, so the hook has to see it first. False when the handle is already free.
        // Unmanaged types only: a managed component is released through its box, not through a handle.
        public unsafe bool ReleaseSlot(in UnmanagedDataHandle handle)
        {
            // The slot may already be gone if the store was disposed (domain reload); guard with Exists.
            if (!store.IsValid || !store.Exists(handle))
                return false;

            try
            {
                if (releaseResources != null)
                    releaseResources(store.GetComponentDataPtr(handle.Index, 0));
            }
            finally
            {
                store.Free(handle);   // free even when the method threw
            }

            return true;
        }

        // A slot is allocated exactly when it is not on the store's free list.
        public unsafe void ReleaseLiveSlots()
        {
            if (managedRegistry != null)
            {
                managedRegistry.ReleaseLiveBoxes();
                return;
            }

            if (releaseResources == null)
                return;

            var capacity = store.Capacity;
            for (var i = 0; i < capacity; i++)
            {
                if (store.IsFree(i))
                    continue;

                try
                {
                    releaseResources(store.GetComponentDataPtr(i, 0));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        // Unmanaged data is held by the store itself, so only the weakly-tracked managed boxes need this.
        public void PinLiveComponents()
        {
            managedRegistry?.PinLiveBoxes();
        }

        public void Dispose()
        {
            if (managedRegistry != null)
                managedRegistry.Dispose();
            else
                store.Dispose();
        }
    }

    /// <summary>
    /// Per-element slot: the per-type store the component lives in, and the handle indexing it.
    /// </summary>
    struct ComponentSlot
    {
        public UnmanagedDataHandle handle;
        public PerTypeStore record;

        // Storage for a component that carries a managed reference; null for an unmanaged one, whose
        // data lives in the per-type store addressed by handle. Element-held on purpose: see
        // ManagedComponentBox.
        public IManagedComponentBox managedBox;

        // The component's shared [OnComponentChanged] dispatcher, or null when it declares no handler.
        // Set at AddComponent from the generated hooks; travels with the slot through the shifts.
        public Action<VisualElement> onChanged;
        public ComponentBindingDispatcher bindingDispatcher;
    }

    /// <summary>
    /// Process-wide owner of the per-type component stores. Mirrors
    /// <c>LayoutManager.SharedManager</c>: a lazily created singleton with a concurrent free-queue
    /// drained on the main thread by <see cref="Collect"/>.
    /// </summary>
    partial class ComponentManager
    {
        // Small default: enough for one chunk's worth of slots without an immediate resize.
        internal const int k_DefaultInitialCapacity = 32;

        // Read [VisualElementComponent(initialCapacity)] by reflection (cold path): v1 has no source
        // generator to emit it into the caller yet.
        internal static int ResolveInitialCapacity(Type componentType)
        {
            var attribute = (VisualElementComponentAttribute)Attribute.GetCustomAttribute(componentType, typeof(VisualElementComponentAttribute));
            return attribute != null && attribute.initialCapacity > 0 ? attribute.initialCapacity : k_DefaultInitialCapacity;
        }

        // Three-state so teardown is one-way within a code-loaded scope: after Shutdown the manager is not
        // re-created. The code-reload cleanup resets both fields, so the next scope re-creates on demand.
        enum SharedManagerState
        {
            Uninitialized,
            Initialized,
            Shutdown
        }

        [AutoStaticsCleanupOnCodeReload]
        // Cleanup resets this to Uninitialized and SharedManager re-creates on demand, so a constructor
        // that trips the gate leaves nothing stale behind for the next code-loaded scope.
        [IgnoreForUAL0015("Initialization gate re-evaluated on demand by SharedManager after cleanup resets it")]
        static SharedManagerState s_State;

        [AutoStaticsCleanupOnCodeReload]
        // Initialize() recreates the shared manager on the next SharedManager access after cleanup nulls
        // this, so a constructor that forces the shared instance leaves nothing stale behind.
        [IgnoreForUAL0015("Shared instance recreated on demand by Initialize() after cleanup nulls it")]
        static ComponentManager s_SharedManager;

        public static ComponentManager SharedManager
        {
            get
            {
                Initialize();
                return s_SharedManager;
            }
        }

        public static bool IsSharedManagerCreated => s_State == SharedManagerState.Initialized;

        internal static bool IsSharedManagerShutDown => s_State == SharedManagerState.Shutdown;

        // Registration bookkeeping, populated by GetOrCreateStore and enumerated by DisposeAll on
        // shutdown. The hot path reads ComponentManager<T>.Record directly, never this dictionary.
        readonly Dictionary<RuntimeTypeHandle, PerTypeStore> m_StoresByComponentType = new();

        readonly ConcurrentQueue<FreeRequest> m_ToFree = new();

        // Non-zero while a [ReleaseComponentResources] method is running. See IsRunningReleaseHook.
        int m_ReleaseHookDepth;

        struct FreeRequest
        {
            public PerTypeStore record;
            public UnmanagedDataHandle handle;

            // Set instead of handle when the component's data lives in an element-held box.
            public IManagedComponentBox box;
        }

        ComponentManager() { }

        // First call is always main-thread (VisualElements are main-thread only), so the state
        // transitions aren't racy. Mirrors LayoutManager.Initialize.
        static void Initialize()
        {
            if (s_State != SharedManagerState.Uninitialized)
                return;

            // Shared managers have been created from the GC thread before in specific test setups;
            // catch that early instead of corrupting the state transitions below.
            Debug.Assert(UIR.Utility.DebugIsMainThread(), "The UI Toolkit ComponentManager must be created on the main thread.");

            s_State = SharedManagerState.Initialized;
            s_SharedManager = new ComponentManager();

            // Dispose native stores on unload via UnloadingUtility (AppDomain.DomainUnload is forbidden in engine code).
            UnloadingUtility.SubscribeToUnloading(UnloadingSubscriber.ComponentManager, s_SharedManager.DisposeAll);
        }

        /// <summary>
        /// Returns the per-type store for <typeparamref name="T"/>, creating and registering it on
        /// first use. Idempotent.
        /// </summary>
        internal PerTypeStore GetOrCreateStore<T>(bool isUnmanaged, int initialCapacity) where T : struct, IVisualElementComponent
        {
            var typeHandle = typeof(T).TypeHandle;

            // Distinct types register from their own static ctors, which the runtime serializes only
            // per-type — lock so concurrent first-use of two types can't corrupt the dictionary.
            lock (m_StoresByComponentType)
            {
                if (m_StoresByComponentType.TryGetValue(typeHandle, out var existing))
                    return existing;

                var record = new PerTypeStore { typeHandle = typeHandle };
                if (isUnmanaged)
                    record.store = CreateUnmanagedStore<T>(initialCapacity);
                else
                    record.managedRegistry = new ManagedComponentRegistry<T>();

                m_StoresByComponentType.Add(typeHandle, record);
                return record;
            }
        }

        static unsafe UnmanagedDataStore CreateUnmanagedStore<T>(int initialCapacity) where T : struct
        {
            var size = UnsafeUtility.SizeOf<T>();

            // Default-value template (real T bytes, zero padding) used to initialise and clear slots.
            var initial = stackalloc byte[Math.Max(size, sizeof(int))];
            UnsafeUtility.MemClear(initial, Math.Max(size, sizeof(int)));
            var template = default(T);
            UnsafeUtility.CopyStructureToPtr(ref template, initial);

            return CreateUnmanagedStore(typeof(T), size, UnsafeUtility.AlignOf<T>(), initial, initialCapacity);
        }

        static unsafe UnmanagedDataStore CreateUnmanagedStore(Type componentType, int size, int align, byte* initial, int initialCapacity)
        {
            // UnmanagedDataStore requires a minimum element size of sizeof(int); pad sub-4-byte
            // components up to a 4-byte storage stride. Real-field bytes are unaffected.
            var storeSize = Math.Max(size, sizeof(int));
            var storeAlign = Math.Max(align, sizeof(int));

            var initialData = stackalloc byte*[1];
            initialData[0] = initial;

            var components = new[] { new UnmanagedComponentType { Size = storeSize, Align = storeAlign } };
            var name = $"Components.{componentType.Name}";
            var labels = new[] { new MemoryLabel("UIElements", name, Allocator.Persistent) };

            return new UnmanagedDataStore(components, labels, initialData, initialCapacity, Allocator.Persistent, name);
        }

        /// <summary>
        /// Enqueues a slot handle for deferred freeing. Thread-safe: it is also called from
        /// <c>~VisualElement</c> on the GC finalizer thread, so it only enqueues — the actual
        /// <c>Free</c> and the <see cref="PerTypeStore.liveCount"/> decrement happen on the main
        /// thread in <see cref="Collect"/>.
        /// </summary>
        internal void EnqueueFree(PerTypeStore record, in UnmanagedDataHandle handle)
        {
            if (record == null || handle.IsUndefined)
                return;

            m_ToFree.Enqueue(new FreeRequest { record = record, handle = handle });
        }

        /// <summary>
        /// Enqueues an element-held box for deferred release. Thread-safe like
        /// <see cref="EnqueueFree(PerTypeStore, in UnmanagedDataHandle)"/>: the
        /// <c>[ReleaseComponentResources]</c> method runs on the main thread when
        /// <see cref="Collect"/> drains the queue.
        /// </summary>
        /// <remarks>
        /// Holding the box strongly on the queue cannot re-root its element. The two callers are
        /// <c>RemoveComponent</c>, where the element is alive and holds the box anyway, and
        /// <c>~VisualElement</c>, by which point the element is already unreachable.
        /// </remarks>
        internal void EnqueueFree(PerTypeStore record, IManagedComponentBox box)
        {
            if (record == null || box == null)
                return;

            m_ToFree.Enqueue(new FreeRequest { record = record, box = box });
        }

        // Per-call cap so a mass teardown doesn't spike a frame. Only safe because the AddComponent
        // allocate path recycles one pending free per allocation (TryRecycleSingleFree) — the same
        // pairing as LayoutManager.TryRecycleNodes + TryRecycleSingleNode. Without that backstop,
        // sustained churn above the cap grows the queue and the stores without bound.
        internal const int k_MaxFreesPerCollect = 100;

        /// <summary>
        /// Drains the deferred free-queue on the main thread, mirroring <c>LayoutManager.Collect()</c>.
        /// Wired into the same per-frame pump as the layout store. Work is capped per call
        /// (<see cref="k_MaxFreesPerCollect"/>); the rest drains on later calls or is recycled by
        /// <see cref="TryRecycleSingleFree"/> on the allocate path.
        /// </summary>
        public void Collect()
        {
            var iterations = 0;
            while (iterations < k_MaxFreesPerCollect && m_ToFree.TryDequeue(out var request))
            {
                FreeOne(in request);
                iterations++;
            }
        }

        /// <summary>
        /// Frees one pending slot, if any. Called once per slot allocated so an allocation pays down the
        /// deferred-free queue — the invariant that keeps <see cref="Collect"/>'s per-call cap from letting
        /// the queue grow unbounded under churn. Mirrors <c>LayoutManager.TryRecycleSingleNode</c>.
        /// </summary>
        internal void TryRecycleSingleFree()
        {
            if (m_ToFree.TryDequeue(out var request))
                FreeOne(in request);
        }

        /// <summary>
        /// True while a <c>[ReleaseComponentResources]</c> method is on the stack. The component mutators on
        /// <see cref="VisualElement"/> refuse to run then: a release is deferred work, driven by a frame
        /// pump, by an unrelated element's <c>AddComponent</c>, or by code unload, so it can land in the
        /// middle of another add — between that add's duplicate check and the point its slot is appended.
        /// </summary>
        internal bool IsRunningReleaseHook => m_ReleaseHookDepth > 0;

        // Releases every component of this type still attached at code unload. Guarded like the queued
        // path: an add from a hook here would register a store while DisposeAll enumerates them, and each
        // slot's failure is contained so the walk still reaches the rest.
        internal void ReleaseLiveSlots(PerTypeStore record)
        {
            m_ReleaseHookDepth++;
            try
            {
                record.ReleaseLiveSlots();
            }
            finally
            {
                m_ReleaseHookDepth--;
            }
        }

        void FreeOne(in FreeRequest request)
        {
            m_ReleaseHookDepth++;
            try
            {
                if (request.box != null)
                    FreeOneBox(request.box, request.record);
                else if (request.record.ReleaseSlot(request.handle))
                    request.record.liveCount--;
            }
            catch (Exception e)
            {
                // The drain runs from the frame pump and from unrelated AddComponent calls, neither of which
                // can carry a [ReleaseComponentResources] method's exception. ReleaseSlot recycles the slot
                // even when the method throws, and the throw can only come from the method itself — reached
                // only once the handle was found live — so the count still has to come down here.
                request.record.liveCount--;
                Debug.LogException(e);
            }
            finally
            {
                m_ReleaseHookDepth--;
            }
        }

        // Unregisters and pools the box whether or not the release method threw, so a throw reaches
        // FreeOne's handler with the box already recycled and the live count decremented exactly once.
        static void FreeOneBox(IManagedComponentBox box, PerTypeStore record)
        {
            try
            {
                box.ReleaseResources();
            }
            finally
            {
                box.Recycle();
            }

            record.liveCount--;
        }

        /// <summary>
        /// Test-only: pending deferred frees not yet drained. Lets tests drain deterministically and
        /// assert the queue stays bounded under churn.
        /// </summary>
        internal int PendingFreeCount => m_ToFree.Count;

        /// <summary>
        /// Test-only leak assertion helper: the number of live slots for <typeparamref name="T"/>.
        /// </summary>
        internal int LiveCount<T>() where T : struct, IVisualElementComponent
        {
            return ComponentManager<T>.Record?.liveCount ?? 0;
        }

        void DisposeAll()
        {
            // Don't unsubscribe (the dispatcher clears subscribers) and don't null s_SharedManager: this hook
            // runs before the code-reload statics cleanup, which resets both fields.
            s_State = SharedManagerState.Shutdown;

            // Pin the weakly-tracked managed boxes before any hook runs. From here on a finalizer drops its
            // slots without enqueueing (the state above closed that road), so a box's element is no longer
            // holding it, and the hooks below allocate — one collection would take a box the sweep has not
            // reached yet and silently skip its release.
            lock (m_StoresByComponentType)
            {
                foreach (var record in m_StoresByComponentType.Values)
                {
                    record.PinLiveComponents();
                }
            }

            // A component may own an allocation that only its [ReleaseComponentResources] method can free.
            while (m_ToFree.TryDequeue(out var request))
                FreeOne(in request);

            lock (m_StoresByComponentType)
            {
                foreach (var record in m_StoresByComponentType.Values)
                {
                    if (!record.isValid)
                        continue;

                    try
                    {
                        ReleaseLiveSlots(record);
                    }
                    finally
                    {
                        record.Dispose();   // the store owns native memory; never leave it behind
                    }
                }

                m_StoresByComponentType.Clear();
            }
        }
    }

    /// <summary>
    /// Per-type cache. The runtime creates one closed generic per component type, and its static
    /// constructor computes the type's facts: which storage shape it takes, and its per-type store
    /// record. The component API on <see cref="VisualElement"/> reads these statics directly, with
    /// no per-call dictionary lookup.
    /// </summary>
    /// <remarks>
    /// Registration is self-contained in this constructor and relies only on the single-type
    /// class-init guarantee (a type's static constructor runs before the first read of any of its
    /// static fields). It makes no assumption about the relative order of <typeparamref name="T"/>'s
    /// static constructor and this one.
    /// </remarks>
    static class ComponentManager<T> where T : struct, IVisualElementComponent
    {
        public static readonly bool IsUnmanaged;

        [NoAutoStaticsCleanup]
        static PerTypeStore s_Record;

        [NoAutoStaticsCleanup]
        static ManagedComponentRegistry<T> s_Registry;

        public static PerTypeStore Record
        {
            get
            {
                EnsureLiveRecord();
                return s_Record;
            }
        }

        /// <summary>Non-null only when <see cref="IsUnmanaged"/> is false.</summary>
        public static ManagedComponentRegistry<T> Registry
        {
            get
            {
                EnsureLiveRecord();
                return s_Registry;
            }
        }

        public static readonly RuntimeTypeHandle TypeHandle;

        // The component's [UxmlCreateInstanceMethod] factory, cached once per type; null when the
        // component has none. GetOrAddComponent uses it to create a new component.
        [NoAutoStaticsCleanup]
        public static readonly Func<T> CreateInstanceFactory;

        // The component's [ReleaseComponentResources] method, cached once per type; null when it declares none.
        [NoAutoStaticsCleanup]
        public static readonly ComponentResourceReleaseHandler<T> ReleaseResourcesHandler;

        static ComponentManager()
        {
            TypeHandle = typeof(T).TypeHandle;
            // The store is a raw byte copy between managed structs, never a marshaling boundary, so
            // the gate is "no managed references": bool and char fields stay in the native store.
            IsUnmanaged = UnsafeUtility.IsUnmanaged(typeof(T));
#pragma warning disable UAL0015 // the statics assigned here are already [NoAutoStaticsCleanup]: this closed generic's static ctor never re-runs after reload (see class remarks), so re-acquiring here would be unsafe, not the assignment itself
            // The hooks ignore instance state, so default(T) is a valid receiver; no boxing.
            CreateInstanceFactory = default(T).__GetComponentCreateInstanceFactory() as Func<T>;
            ReleaseResourcesHandler = default(T).__GetComponentResourceReleaseHandler() as ComponentResourceReleaseHandler<T>;

            AcquireRecord();
#pragma warning restore UAL0015

            // Register the required types for the editor-only RemoveComponent warning. Every
            // attached component initializes its manager first, so the registry covers all attached types.
#pragma warning disable UAL0015 // known, accepted gap documented on ComponentRequirementRegistry.s_RequiredTypes: a type surviving reload drops out of this editor-only warning aid, not a correctness issue
            ComponentRequirementRegistry.Register(TypeHandle, default(T).__GetRequiredComponentTypes());
#pragma warning restore UAL0015
        }

        static void EnsureLiveRecord()
        {
            // An engine-defined type's closed generic outlives a code reload, so the static constructor
            // never re-runs; re-acquiring while shut down would leak a store nothing will dispose.
            if (!s_Record.isValid && !ComponentManager.IsSharedManagerShutDown)
                AcquireRecord();
        }

        static void AcquireRecord()
        {
            var initialCapacity = ComponentManager.ResolveInitialCapacity(typeof(T));

            s_Record = ComponentManager.SharedManager.GetOrCreateStore<T>(IsUnmanaged, initialCapacity);
            s_Registry = s_Record.managedRegistry as ManagedComponentRegistry<T>;

            if (IsUnmanaged && ReleaseResourcesHandler != null)
                unsafe { s_Record.releaseResources = &ReleaseResourcesAt; }
        }

        static unsafe void ReleaseResourcesAt(void* data) => ReleaseResourcesHandler(ref UnsafeUtility.AsRef<T>(data));
    }

    /// <summary>
    /// Editor only: maps each component type to the types it requires, keyed by type handle.
    /// RemoveComponent uses it to warn when a still-required component is removed. Player builds
    /// compile it out and do no check.
    /// </summary>
    static partial class ComponentRequirementRegistry
    {
        // Re-registration runs from ComponentManager<T>'s static constructor, so a component type that
        // survives a code reload is no longer covered by the RemoveComponent warning.
        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<RuntimeTypeHandle, RuntimeTypeHandle[]> s_RequiredTypes = new();

        // Static constructors of two distinct component types can run concurrently, so lock.
        public static void Register(RuntimeTypeHandle type, RuntimeTypeHandle[] requiredTypes)
        {
            if (requiredTypes == null || requiredTypes.Length == 0)
                return;

            lock (s_RequiredTypes)
                s_RequiredTypes[type] = requiredTypes;
        }

        /// <summary>The required types of the component type, or null when it has none.</summary>
        public static RuntimeTypeHandle[] GetRequiredTypes(RuntimeTypeHandle type)
        {
            lock (s_RequiredTypes)
                return s_RequiredTypes.TryGetValue(type, out var required) ? required : null;
        }
    }
}
