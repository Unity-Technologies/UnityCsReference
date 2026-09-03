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
    /// One record per registered unmanaged component type. Owned canonically by
    /// <see cref="ComponentManager"/> (for disposal and enumeration) and cached on
    /// <see cref="ComponentManager{T}"/> and on each native-stored <see cref="ComponentSlot"/> so the
    /// per-element teardown path can free a slot without a dictionary lookup (which matters on the
    /// GC finalizer thread).
    /// </summary>
    class PerTypeStore
    {
        public UnmanagedDataStore store;
        public RuntimeTypeHandle typeHandle;

        // Number of live native-store allocations for this type. Mutated on the main thread only:
        // incremented by AddComponent, decremented when the deferred free is drained by Collect().
        public int liveCount;
    }

    /// <summary>
    /// Per-element slot. A discriminated union: it holds one of two shapes, never both.
    /// For an unmanaged component, <see cref="record"/> is non-null and <see cref="handle"/> indexes
    /// the per-type store. For a component with any managed field, <see cref="managedBox"/> holds a
    /// <see cref="ManagedComponentBox{T}"/> and the other fields are default.
    /// </summary>
    struct ComponentSlot
    {
        public UnmanagedDataHandle handle;   // valid when record != null
        public PerTypeStore record;          // non-null => blittable
        public object managedBox;            // non-null => managed (ManagedComponentBox<T>)

        // The component's shared [OnComponentChanged] dispatcher, or null when it declares no handler.
        // Set at AddComponent from the generated hooks; travel with the slot through the remove-swap.
        public Action<VisualElement> onChanged;
        public ComponentBindingDispatcher bindingDispatcher;
    }

    /// <summary>
    /// Process-wide owner of the per-type unmanaged component stores. Mirrors
    /// <c>LayoutManager.SharedManager</c>: a lazily created singleton with a concurrent free-queue
    /// drained on the main thread by <see cref="Collect"/>.
    /// </summary>
    partial class ComponentManager
    {
        // Small default: enough for one chunk's worth of slots without an immediate resize.
        const int k_DefaultInitialCapacity = 32;

        // Three-state so teardown is one-way within a code-loaded scope: after Shutdown the manager is not
        // re-created. The code-reload cleanup resets both fields, so the next scope re-creates on demand.
        enum SharedManagerState
        {
            Uninitialized,
            Initialized,
            Shutdown
        }

        [AutoStaticsCleanupOnCodeReload]
        static SharedManagerState s_State;

        [AutoStaticsCleanupOnCodeReload]
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

        struct FreeRequest
        {
            public PerTypeStore record;
            public UnmanagedDataHandle handle;
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
        /// first use. Idempotent. Only called for unmanaged components (the caller checks
        /// <see cref="ComponentManager{T}.IsUnmanaged"/> first).
        /// </summary>
        internal unsafe PerTypeStore GetOrCreateStore<T>(int initialCapacity = k_DefaultInitialCapacity) where T : struct
        {
            var typeHandle = typeof(T).TypeHandle;

            // Distinct types register from their own static ctors, which the runtime serializes only
            // per-type — lock so concurrent first-use of two types can't corrupt the dictionary.
            lock (m_StoresByComponentType)
            {
                if (m_StoresByComponentType.TryGetValue(typeHandle, out var existing))
                    return existing;

                var size = UnsafeUtility.SizeOf<T>();
                var align = UnsafeUtility.AlignOf<T>();

                // UnmanagedDataStore requires a minimum element size of sizeof(int); pad sub-4-byte
                // components up to a 4-byte storage stride. Real-field bytes are unaffected.
                var storeSize = Math.Max(size, sizeof(int));
                var storeAlign = Math.Max(align, sizeof(int));

                // Default-value template (real T bytes, zero padding) used to initialise and clear slots.
                var initial = stackalloc byte[storeSize];
                UnsafeUtility.MemClear(initial, storeSize);
                var template = default(T);
                UnsafeUtility.CopyStructureToPtr(ref template, initial);
                var initialData = stackalloc byte*[1];
                initialData[0] = initial;

                var components = new[] { new UnmanagedComponentType { Size = storeSize, Align = storeAlign } };
                var labels = new[] { new MemoryLabel("UIElements", $"Components.{typeof(T).Name}", Allocator.Persistent) };

                var store = new UnmanagedDataStore(components, labels, initialData, initialCapacity, Allocator.Persistent, $"Components.{typeof(T).Name}");
                var record = new PerTypeStore { store = store, typeHandle = typeHandle };
                m_StoresByComponentType.Add(typeHandle, record);
                return record;
            }
        }

        /// <summary>
        /// Enqueues a native-store slot handle for deferred freeing. Thread-safe: it is also called from
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
        /// Frees one pending slot, if any. Called from the <c>AddComponent</c> allocate path so
        /// allocation pays down the deferred-free queue — the invariant that keeps
        /// <see cref="Collect"/>'s per-call cap from letting the queue grow unbounded under churn.
        /// Mirrors <c>LayoutManager.TryRecycleSingleNode</c>.
        /// </summary>
        internal void TryRecycleSingleFree()
        {
            if (m_ToFree.TryDequeue(out var request))
                FreeOne(in request);
        }

        void FreeOne(in FreeRequest request)
        {
            // The slot may already be gone if the store was disposed (domain reload); guard with Exists.
            if (request.record.store.IsValid && request.record.store.Exists(request.handle))
            {
                request.record.store.Free(request.handle);
                request.record.liveCount--;
            }
        }

        /// <summary>
        /// Test-only: pending deferred frees not yet drained. Lets tests drain deterministically and
        /// assert the queue stays bounded under churn.
        /// </summary>
        internal int PendingFreeCount => m_ToFree.Count;

        /// <summary>
        /// Test-only leak assertion helper: the number of live native-store allocations for
        /// <typeparamref name="T"/>. Returns 0 for managed component types (those are GC-tracked).
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

            // Drop pending frees; the stores own the memory and are about to be disposed wholesale.
            while (m_ToFree.TryDequeue(out _)) { }

            lock (m_StoresByComponentType)
            {
                foreach (var record in m_StoresByComponentType.Values)
                {
                    if (record.store.IsValid)
                        record.store.Dispose();
                }

                m_StoresByComponentType.Clear();
            }
        }
    }

    /// <summary>
    /// Per-type cache. The runtime creates one closed generic per component type, and its static
    /// constructor computes the type's facts: whether it can live in the unmanaged store, and
    /// (if so) its per-type store record. The component API on <see cref="VisualElement"/> reads
    /// these statics directly, with no per-call dictionary lookup.
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

        /// <summary>Non-null only when <see cref="IsUnmanaged"/>.</summary>
        public static PerTypeStore Record
        {
            get
            {
                // An engine-defined type's closed generic outlives a code reload, so the static constructor
                // never re-runs; re-acquiring while shut down would leak a store nothing will dispose.
                if (s_Record != null && !s_Record.store.IsValid && !ComponentManager.IsSharedManagerShutDown)
                    s_Record = CreateRecord();

                return s_Record;
            }
        }

        public static readonly RuntimeTypeHandle TypeHandle;

        // The component's [UxmlCreateInstanceMethod] factory, cached once per type; null when the
        // component has none. GetOrAddComponent uses it to create a new component.
        [NoAutoStaticsCleanup]
        public static readonly Func<T> CreateInstanceFactory;

        static ComponentManager()
        {
            TypeHandle = typeof(T).TypeHandle;
            // The store is a raw byte copy between managed structs, never a marshaling boundary, so
            // the gate is "no managed references": bool and char fields stay in the native store.
            IsUnmanaged = UnsafeUtility.IsUnmanaged(typeof(T));
#pragma warning disable UAL0015 // s_Record/CreateInstanceFactory are already [NoAutoStaticsCleanup]: this closed generic's static ctor never re-runs after reload (see class remarks), so re-acquiring here would be unsafe, not the assignment itself
            if (IsUnmanaged)
                s_Record = CreateRecord();

            // The hook ignores instance state, so default(T) is a valid receiver; no boxing.
            CreateInstanceFactory = default(T).__GetComponentCreateInstanceFactory() as Func<T>;
#pragma warning restore UAL0015

            // Register the required types for the editor-only RemoveComponent warning. Every
            // attached component initializes its manager first, so the registry covers all attached types.
#pragma warning disable UAL0015 // known, accepted gap documented on ComponentRequirementRegistry.s_RequiredTypes: a type surviving reload drops out of this editor-only warning aid, not a correctness issue
            ComponentRequirementRegistry.Register(TypeHandle, default(T).__GetRequiredComponentTypes());
#pragma warning restore UAL0015
        }

        static PerTypeStore CreateRecord()
        {
            // Read [VisualElementComponent(initialCapacity)] by reflection (cold path): v1 has no source
            // generator to emit it into this call yet.
            var attribute = (VisualElementComponentAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(VisualElementComponentAttribute));
            return attribute != null && attribute.initialCapacity > 0
                ? ComponentManager.SharedManager.GetOrCreateStore<T>(attribute.initialCapacity)
                : ComponentManager.SharedManager.GetOrCreateStore<T>();
        }
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
