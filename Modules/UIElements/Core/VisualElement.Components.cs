// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements.Unmanaged;

namespace UnityEngine.UIElements
{
    public partial class VisualElement
    {
        // Per-element component storage: two compact parallel arrays that grow only as components
        // are added. Both stay null until the first AddComponent call, so an element with no
        // components carries zero extra storage. A single element typically holds 1-5 components,
        // so lookup is a linear scan over the contiguous type-handle array.
        RuntimeTypeHandle[] m_ComponentTypes;
        ComponentSlot[] m_ComponentSlots;

        // Live component count. The arrays grow geometrically so capacity may exceed it; the tail past it is
        // kept cleared. This, not their .Length, is the count of components.
        int m_ComponentCount;

        // Introspection only (tests, debugger); a null array with count 0 yields a valid empty span.
        internal ReadOnlySpan<RuntimeTypeHandle> componentTypeHandles
            => new ReadOnlySpan<RuntimeTypeHandle>(m_ComponentTypes, 0, m_ComponentCount);

        // Per-frame dirty record over the component slots: bit i marks slot i changed, bit 31 is the
        // overflow sentinel ("some slot >= 31 changed"). Set by MarkComponentDirty, drained once per
        // frame by VisualTreeComponentUpdater so a component's [OnComponentChanged] handler runs at most
        // once even after a burst of writes. 31 precise slots is well past the typical 1-5 components.
        const uint k_ComponentOverflowBit = 1u << 31;
        uint m_DirtyComponentMask;

        /// <summary>
        /// Marks the element's resolved styles dirty so the next update re-resolves them, re-reading
        /// USS custom property values. The style counterpart of <see cref="MarkDirtyRepaint"/>.
        /// </summary>
        public void MarkDirtyStyles()
        {
            IncrementVersion(VersionChangeType.StyleSheet);
        }

        /// <summary>
        /// Notifies the data binding system that a field of an attached component changed, so a binding
        /// targeting that field (for example <c>${component:Tooltip}.text</c>) can publish the new value.
        /// </summary>
        /// <remarks>
        /// Writing through <c>ref GetComponent&lt;T&gt;()</c> changes data in place and fires nothing on
        /// its own; call this afterwards to drive bindings. The <c>UITKSG029</c> analyzer flags misses.
        /// </remarks>
        public void NotifyComponentChanged(in BindingId property)
        {
            NotifyPropertyChanged(property);
        }

        /// <summary>
        /// Marks the attached component of type <typeparamref name="T"/> as changed, so that its
        /// <c>[OnComponentChanged]</c> handler runs once during the next update.
        /// </summary>
        /// <remarks>
        /// Call this method after you change component data directly, for example by writing through
        /// <c>ref GetComponent&lt;T&gt;()</c>. Unity doesn't detect direct writes, so without this call
        /// the handler doesn't run. You don't need to call this method when you use the generated
        /// <c>SetXxx</c> setters or a data binding; both call it for you. If no component of type
        /// <typeparamref name="T"/> is attached to this element, this method does nothing.
        /// </remarks>
        /// <typeparam name="T">A component struct declared with <see cref="VisualElementComponentAttribute"/>.</typeparam>
        public void MarkComponentDirty<T>() where T : struct, IVisualElementComponent
        {
            var i = FindComponentIndex(typeof(T).TypeHandle);
            if (i < 0)
                return;

            // Bump the version only the first time this slot is flagged this frame. On a panel, a set bit
            // means the element is already enqueued for the next Component flush (the mask is cleared only
            // at flush), so further bumps in the same frame would fan out through every updater for nothing
            // — a write burst should cost one bump total. Off-panel the bump is a no-op and the bit alone
            // can't enqueue the element, so the dirty mask is re-armed on attach instead (see
            // ReArmDirtyComponentsOnAttach); that is also what makes this guard safe off-panel.
            var bit = i < 31 ? 1u << i : k_ComponentOverflowBit;
            if ((m_DirtyComponentMask & bit) == 0)
            {
                m_DirtyComponentMask |= bit;
                IncrementVersion(VersionChangeType.Component);
            }
        }

        // Runs the [OnComponentChanged] handler of each component flagged dirty this frame, then clears
        // the mask. Called once per frame by VisualTreeComponentUpdater. The mask is captured and cleared
        // up front, so a handler that changes another field just re-flags for the next frame (the
        // "at most once per frame" guarantee). Only dirty slots are touched, never the whole component set.
        internal void FlushComponentChanges()
        {
            var mask = m_DirtyComponentMask;
            m_DirtyComponentMask = 0;
            if (mask == 0)
                return;

            // Capture the slots array up front. A handler may remove another component mid-flush, which
            // reassigns m_ComponentSlots to a new (resized) array; iterating the captured old array stays
            // safe — it is still a valid array, and a removed component's dispatcher early-returns via its
            // HasComponent<T> guard, so a now-stale slot just no-ops.
            var slots = m_ComponentSlots;
            var count = slots?.Length ?? 0;

            // Precise slots 0-30: invoke only the ones whose bit is set.
            var precise = count < 31 ? count : 31;
            for (var i = 0; i < precise; i++)
            {
                if ((mask & (1u << i)) != 0)
                    slots[i].onChanged?.Invoke(this);
            }

            // Overflow tail (>= 31 components, rare): the sentinel can't say which, so notify the tail.
            if ((mask & k_ComponentOverflowBit) != 0)
            {
                for (var i = 31; i < count; i++)
                    slots[i].onChanged?.Invoke(this);
            }
        }

        // Called when the element (re)attaches to a panel. A change made while detached only set the dirty
        // bit: IncrementVersion was a no-op with no panel, so the element was never enqueued, and the set
        // bit would otherwise block re-arming (the guard in MarkComponentDirty bumps only on a clean bit).
        // Re-enqueue here so an off-panel change's [OnComponentChanged] handler runs once in the panel.
        internal void ReArmDirtyComponentsOnAttach()
        {
            if (m_DirtyComponentMask != 0)
                IncrementVersion(VersionChangeType.Component);
        }

        /// <summary>
        /// Attaches a component of type <typeparamref name="T"/> to this element.
        /// </summary>
        /// <param name="value">Initial component value.</param>
        /// <typeparam name="T">A component struct declared with <see cref="VisualElementComponentAttribute"/>.</typeparam>
        /// <exception cref="InvalidOperationException">
        /// Thrown if a component of the same type is already attached (an element holds at most one
        /// component of each type), or if this element's resources have been released.
        /// </exception>
        public void AddComponent<T>(in T value) where T : struct, IVisualElementComponent
        {
            if ((m_Flags & VisualElementFlags.Released) != 0)
                throw new InvalidOperationException(k_ElementReleaseExceptionMessage);

            value.__ValidateOwnerType(this);

            var typeHandle = typeof(T).TypeHandle;
            if (FindComponentIndex(typeHandle) >= 0)
                throw new InvalidOperationException(
                    $"VisualElement already has a component of type '{typeof(T).Name}'. Only one component of a given type is allowed per element.");

            AttachComponent(typeHandle, in value);
        }

        /// <summary>
        /// Returns a reference to the component of type <typeparamref name="T"/> attached to this
        /// element. If the component is not attached yet, it is added first.
        /// </summary>
        /// <remarks>
        /// When the component is already attached, this method behaves exactly like
        /// <see cref="GetComponent{T}"/> and the component data is not changed. When it is not attached,
        /// a new component is created and added the same way as <see cref="AddComponent{T}"/>. The new
        /// component is created with the type's <see cref="UxmlCreateInstanceMethodAttribute"/> method
        /// when the type has one, otherwise with default values.
        /// </remarks>
        /// <typeparam name="T">A struct decorated with <see cref="VisualElementComponentAttribute"/>.</typeparam>
        /// <returns>
        /// A reference to the live component data. Like <see cref="GetComponent{T}"/>, the reference is
        /// valid until a component is added to or removed from this element.
        /// </returns>
        public ref T GetOrAddComponent<T>() where T : struct, IVisualElementComponent
        {
            var typeHandle = typeof(T).TypeHandle;
            var index = FindComponentIndex(typeHandle);
            if (index >= 0)
                return ref GetComponentRefAt<T>(index);

            var factory = ComponentManager<T>.CreateInstanceFactory;
            var value = factory != null ? factory() : default;
            value.__ValidateOwnerType(this);

            // AttachComponent stores the new component at the end of the list, so no second scan is
            // needed — unless an [OnComponentAdded] method ran and may have changed the list.
            if (!AttachComponent(typeHandle, in value))
                return ref GetComponentRefAt<T>(m_ComponentCount - 1);

            return ref GetComponent<T>();
        }

        // Shared by AddComponent and GetOrAddComponent: stores the component and runs the add side
        // effects. Callers have already validated the owner type and rejected duplicates. Returns
        // whether an [OnComponentAdded] method ran (it may have added or removed other components).
        unsafe bool AttachComponent<T>(RuntimeTypeHandle typeHandle, in T value) where T : struct, IVisualElementComponent
        {
            // Add the [RequiresComponentOfType] components first. This can grow the component arrays,
            // so nothing must be cached across this call.
            value.__ResolveComponentRequirements(this);

            var slot = new ComponentSlot();

            if (ComponentManager<T>.IsUnmanaged)
            {
                var record = ComponentManager<T>.Record;

                if (!record.store.IsValid)   // shut down between the code unload and the statics cleanup
                    throw new InvalidOperationException(
                        $"The UI Toolkit component store for '{typeof(T).Name}' has been torn down (code/domain unload). Components cannot be added until it is reinitialized.");

                // Free one pending slot before allocating one — the backstop that makes Collect()'s
                // per-call cap safe under churn (mirrors LayoutManager.CreateNodeInternal).
                ComponentManager.SharedManager.TryRecycleSingleFree();

                var handle = record.store.Allocate();
                var local = value;
                UnsafeUtility.CopyStructureToPtr(ref local, record.store.GetComponentDataPtr(handle.Index, 0));
                record.liveCount++;
                slot.record = record;
                slot.handle = handle;
            }
            else
            {
                // A managed field forces a boxed slot. StrongBox<T> (not a plain box) lets GetComponent
                // hand back a real `ref T`; pool it so add/remove churn is allocation-free.
                var box = ManagedComponentBoxPool<T>.Pool.Get();
                box.Value = value;
                slot.managedBox = box;
            }

            // Cache the component's shared per-type dispatchers ([OnComponentChanged]; binding dispatch),
            // so later code can invoke them without knowing T. Constrained generic calls, no boxing.
            slot.onChanged = value.__GetComponentChangedDispatcher();
            slot.bindingDispatcher = value.__GetComponentBindingDispatcher();

            AppendComponentSlot(typeHandle, slot);

            value.__RegisterComponentCallbacks(this);

            // [OnComponentAdded] runs last, so it sees the component stored and its callbacks
            // registered. A required component's own method already ran while it was added above.
            return value.__InvokeComponentAdded(this);
        }

        /// <summary>
        /// Returns a reference to the attached component of type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>
        /// The reference points at the component's live storage: assigning to its fields updates the
        /// element directly, with no need to write the value back. The reference stays valid until
        /// the component is removed.
        /// </remarks>
        /// <typeparam name="T">A component struct declared with <see cref="VisualElementComponentAttribute"/>.</typeparam>
        /// <returns>A reference to the live component data.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no such component is attached, or if this element's resources have been released.
        /// </exception>
        public ref T GetComponent<T>() where T : struct, IVisualElementComponent
        {
            if ((m_Flags & VisualElementFlags.Released) != 0)
                throw new InvalidOperationException(k_ElementReleaseExceptionMessage);

            var index = FindComponentIndex(typeof(T).TypeHandle);
            if (index < 0)
                throw new InvalidOperationException(
                    $"VisualElement does not have a component of type '{typeof(T).Name}'.");

            return ref GetComponentRefAt<T>(index);
        }

        // No existence/bounds check: callers must resolve the index via FindComponentIndex first.
        unsafe ref T GetComponentRefAt<T>(int index) where T : struct, IVisualElementComponent
        {
            ref var slot = ref m_ComponentSlots[index];
            if (slot.record != null)
                return ref UnsafeUtility.AsRef<T>(slot.record.store.GetComponentDataPtr(slot.handle.Index, 0));

            return ref ((StrongBox<T>)slot.managedBox).Value;
        }

        /// <summary>
        /// Returns a reference to the component of type <typeparamref name="T"/> attached to this
        /// element, or a null reference when the component is not attached. This method never adds
        /// a component.
        /// </summary>
        /// <remarks>
        /// Always check the returned reference with
        /// <c>System.Runtime.CompilerServices.Unsafe.IsNullRef</c> before using it. Reading or
        /// writing through a null reference throws <see cref="NullReferenceException"/>.
        /// When the component is attached, this method behaves exactly like
        /// <see cref="GetComponent{T}"/>.
        /// </remarks>
        /// <typeparam name="T">A struct decorated with <see cref="VisualElementComponentAttribute"/>.</typeparam>
        /// <returns>
        /// A reference to the live component data, or a null reference when the component is not
        /// attached. A valid reference stays valid until a component is added to or removed from
        /// this element.
        /// </returns>
        public ref T GetComponentRefOrNullRef<T>() where T : struct, IVisualElementComponent
        {
            var index = FindComponentIndex(typeof(T).TypeHandle);
            if (index < 0)
                return ref UnsafeUtilityInternal.NullRef<T>();

            return ref GetComponentRefAt<T>(index);
        }

        /// <summary>
        /// Checks whether a component of type <typeparamref name="T"/> is attached to this element.
        /// </summary>
        /// <typeparam name="T">A component struct declared with <see cref="VisualElementComponentAttribute"/>.</typeparam>
        /// <returns><see langword="true"/> if the component is attached; otherwise <see langword="false"/>.</returns>
        public bool HasComponent<T>() where T : struct, IVisualElementComponent
        {
            return FindComponentIndex(typeof(T).TypeHandle) >= 0;
        }

        /// <summary>
        /// Removes the attached component of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">A component struct declared with <see cref="VisualElementComponentAttribute"/>.</typeparam>
        /// <exception cref="InvalidOperationException">
        /// Thrown if no such component is attached, or if this element's resources have been released.
        /// </exception>
        public void RemoveComponent<T>() where T : struct, IVisualElementComponent
        {
            if (!TryRemoveComponent<T>())
                throw new InvalidOperationException(
                    $"VisualElement does not have a component of type '{typeof(T).Name}'.");
        }

        /// <summary>
        /// Removes the attached component of type <typeparamref name="T"/>, if one is attached.
        /// </summary>
        /// <typeparam name="T">A component struct declared with <see cref="VisualElementComponentAttribute"/>.</typeparam>
        /// <returns><see langword="true"/> if a component was removed; <see langword="false"/> if none was attached.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this element's resources have been released.</exception>
        public bool TryRemoveComponent<T>() where T : struct, IVisualElementComponent
        {
            if ((m_Flags & VisualElementFlags.Released) != 0)
                throw new InvalidOperationException(k_ElementReleaseExceptionMessage);

            var typeHandle = typeof(T).TypeHandle;
            var index = FindComponentIndex(typeHandle);
            if (index < 0)
                return false;

            // The editor warns when another attached component still requires this one; player
            // builds do no check.
            WarnIfComponentStillRequired(typeHandle, index);

            // [OnComponentRemoved] runs first, while the data is live and the [RegisterCallback]
            // handlers are still registered, so it can read its own state and clean up the owner.
            if (default(T).__InvokeComponentRemoved(this))
            {
                // The method may have added or removed components, so resolve the slot again.
                index = FindComponentIndex(typeHandle);
                if (index < 0)
                    return true;
            }

            // The hook forwards to shared per-type registrations and ignores instance state, so default(T) is a valid receiver.
            default(T).__UnregisterComponentCallbacks(this);

            // Pool the managed box for reuse. Safe here because RemoveComponent is main-thread; the pool
            // is not thread-safe, so the finalizer teardown (ReleaseComponentStorage) doesn't pool.
            if (!ComponentManager<T>.IsUnmanaged && m_ComponentSlots[index].managedBox is ManagedComponentBox<T> box)
                ManagedComponentBoxPool<T>.Pool.Release(box);

            FreeComponentSlot(ref m_ComponentSlots[index]);
            RemoveComponentSlotAt(index);
            return true;
        }

        // Logs a warning when another attached component declares the removed component as required
        // with [RequiresComponentOfType]. Editor only; compiled out of player builds.
        void WarnIfComponentStillRequired(RuntimeTypeHandle removedType, int removedIndex)
        {
            for (var i = 0; i < m_ComponentCount; i++)
            {
                if (i == removedIndex)
                    continue;

                var required = ComponentRequirementRegistry.GetRequiredTypes(m_ComponentTypes[i]);
                if (required == null)
                    continue;

                for (var r = 0; r < required.Length; r++)
                {
                    if (required[r].Equals(removedType))
                    {
                        Debug.LogWarning(
                            $"[UI Toolkit] Removing component '{Type.GetTypeFromHandle(removedType).Name}' from this element, but the attached component '{Type.GetTypeFromHandle(m_ComponentTypes[i]).Name}' declares it required with [RequiresComponentOfType]. The dependent component may no longer work.");
                        break;
                    }
                }
            }
        }

        // Frees every component's storage as part of element teardown. Called from both
        // ~VisualElement (GC finalizer thread) and ReleaseResourcesNoChecks (main thread), so it
        // must stay safe to run off the main thread: it only enqueues blittable handles on the
        // manager's concurrent free-queue and drops managed references. The box pool is not
        // thread-safe, so only the main-thread caller opts into returnManagedBoxesToPool.
        internal void ReleaseComponentStorage(bool returnManagedBoxesToPool = false)
        {
            var slots = m_ComponentSlots;
            if (slots == null)
                return;

            var manager = ComponentManager.IsSharedManagerCreated ? ComponentManager.SharedManager : null;
            for (var i = 0; i < m_ComponentCount; i++)
            {
                ref var slot = ref slots[i];
                if (slot.record != null && manager != null)
                    manager.EnqueueFree(slot.record, slot.handle);
                if (returnManagedBoxesToPool && slot.managedBox is IManagedComponentBox box)
                    box.ReturnToPool();
                slot.managedBox = null;
            }

            m_ComponentTypes = null;
            m_ComponentSlots = null;
            m_ComponentCount = 0;
            m_DirtyComponentMask = 0;
        }

        internal ComponentBindingDispatcher GetComponentBindingDispatcher(RuntimeTypeHandle typeHandle)
        {
            var index = FindComponentIndex(typeHandle);
            return index >= 0 ? m_ComponentSlots[index].bindingDispatcher : null;
        }

        int FindComponentIndex(RuntimeTypeHandle typeHandle)
        {
            var types = m_ComponentTypes;
            if (types == null)
                return -1;

            var count = m_ComponentCount;
            for (var i = 0; i < count; i++)
            {
                if (types[i].Equals(typeHandle))
                    return i;
            }

            return -1;
        }

        // Warn-once per clashing short name, so an ambiguous binding doesn't spam every update.
        [AutoStaticsCleanupOnCodeReload]
        static readonly System.Collections.Generic.HashSet<string> s_AmbiguousComponentNamesWarned =
            new System.Collections.Generic.HashSet<string>();

        // Resolves a component by short type name for "${component:TypeName}.field" bindings. Two attached
        // components sharing a short name (different namespaces) are ambiguous, so report unresolved and
        // warn once rather than silently picking one.
        internal bool TryResolveComponentHandleByName(string shortTypeName, out RuntimeTypeHandle handle)
        {
            handle = default;

            var types = m_ComponentTypes;
            if (types == null)
                return false;

            var matchIndex = -1;
            for (var i = 0; i < m_ComponentCount; i++)
            {
                if (Type.GetTypeFromHandle(types[i]).Name != shortTypeName)
                    continue;

                if (matchIndex >= 0)
                {
                    if (s_AmbiguousComponentNamesWarned.Add(shortTypeName))
                        Debug.LogWarning($"[UI Toolkit] The component data binding selector '${{component:{shortTypeName}}}' is ambiguous: more than one component named '{shortTypeName}' is attached to this element. The binding is skipped. Give the component types distinct names.");
                    return false;
                }

                matchIndex = i;
            }

            if (matchIndex < 0)
                return false;

            handle = types[matchIndex];
            return true;
        }

        // Debugger support: non-generic entry points for the editor UI Toolkit Debugger, which introspects a
        // live element's components without knowing their types (the public API is all generic). Cold paths,
        // so they box a copy rather than expose the unsafe storage; route edits back through SetComponentForDebug.

        internal int componentCountForDebug => m_ComponentCount;

        internal IEnumerable<(Type type, object value)> EnumerateComponentsForDebug()
        {
            var types = m_ComponentTypes;
            for (var i = 0; i < m_ComponentCount; i++)
                yield return (Type.GetTypeFromHandle(types[i]), BoxComponentForDebug(i));
        }

        internal IEnumerable<Type> EnumerateComponentTypesForDebug()
        {
            var types = m_ComponentTypes;
            for (var i = 0; i < m_ComponentCount; i++)
                yield return Type.GetTypeFromHandle(types[i]);
        }

        unsafe object BoxComponentForDebug(int index)
        {
            ref var slot = ref m_ComponentSlots[index];
            if (slot.managedBox != null)
                return ((IStrongBox)slot.managedBox).Value;

            // Copy the live bytes with the store's own stride (UnsafeUtility.SizeOf), not Marshal, so the read
            // can't diverge from how the value was written. Unmanaged-only here (no references), so pin + raw copy is valid.
            var type = Type.GetTypeFromHandle(m_ComponentTypes[index]);
            var box = Activator.CreateInstance(type);
            var handle = GCHandle.Alloc(box, GCHandleType.Pinned);
            try
            {
                UnsafeUtility.MemCpy((void*)handle.AddrOfPinnedObject(),
                    slot.record.store.GetComponentDataPtr(slot.handle.Index, 0), UnsafeUtility.SizeOf(type));
            }
            finally
            {
                handle.Free();
            }
            return box;
        }

        internal unsafe void SetComponentForDebug(Type type, object value)
        {
            if (type == null || value == null)
                return;
            var index = FindComponentIndex(type.TypeHandle);
            if (index < 0)
                return;

            ref var slot = ref m_ComponentSlots[index];
            if (slot.managedBox != null)
            {
                ((IStrongBox)slot.managedBox).Value = value;
            }
            else
            {
                var handle = GCHandle.Alloc(value, GCHandleType.Pinned);
                try
                {
                    UnsafeUtility.MemCpy(slot.record.store.GetComponentDataPtr(slot.handle.Index, 0),
                        (void*)handle.AddrOfPinnedObject(), UnsafeUtility.SizeOf(type));
                }
                finally
                {
                    handle.Free();
                }
            }

            // A debug write changes the bytes but fires no invalidation on its own: repaint for painter
            // components, and a style re-resolve to re-apply [StyleProperty] values. Bindings are handled by
            // the caller's NotifyComponentChanged.
            IncrementVersion(VersionChangeType.Repaint | VersionChangeType.StyleSheet);
        }

        void AppendComponentSlot(RuntimeTypeHandle typeHandle, ComponentSlot slot)
        {
            var capacity = m_ComponentTypes?.Length ?? 0;
            if (m_ComponentCount == capacity)
            {
                // Start at 1 (most elements carry a single component), then double.
                var newCapacity = capacity == 0 ? 1 : capacity * 2;
                Array.Resize(ref m_ComponentTypes, newCapacity);
                Array.Resize(ref m_ComponentSlots, newCapacity);
            }

            m_ComponentTypes[m_ComponentCount] = typeHandle;
            m_ComponentSlots[m_ComponentCount] = slot;
            m_ComponentCount++;
        }

        void FreeComponentSlot(ref ComponentSlot slot)
        {
            if (slot.record != null)
                ComponentManager.SharedManager.EnqueueFree(slot.record, slot.handle);

            slot.managedBox = null;
            slot.record = null;
            slot.handle = UnmanagedDataHandle.Undefined;
        }

        void RemoveComponentSlotAt(int index)
        {
            var last = m_ComponentCount - 1;

            // Realign the per-slot dirty bits with the swap below, so a mid-frame removal can't misroute a
            // pending [OnComponentChanged]. Precise bits (< 31) only; the overflow sentinel stays coarse.
            // The removed slot's bit clears; if the last slot moves into `index`, its bit moves with it.
            if (m_DirtyComponentMask != 0)
            {
                if (index < 31)
                    m_DirtyComponentMask &= ~(1u << index);
                if (index != last)
                {
                    var lastDirty = last < 31 && (m_DirtyComponentMask & (1u << last)) != 0;
                    if (last < 31)
                        m_DirtyComponentMask &= ~(1u << last);
                    if (lastDirty && index < 31)
                        m_DirtyComponentMask |= 1u << index;
                }
            }

            // Swap-remove: the last entry fills the gap.
            if (index != last)
            {
                m_ComponentTypes[index] = m_ComponentTypes[last];
                m_ComponentSlots[index] = m_ComponentSlots[last];
            }

            // Clear the freed tail slot so it can't root a managed box past the live range. Capacity is
            // kept, even at count 0, so steady-state add/remove churn never reallocates the arrays.
            m_ComponentTypes[last] = default;
            m_ComponentSlots[last] = default;
            m_ComponentCount = last;
        }
    }

    // Lets the non-generic teardown path (ReleaseComponentStorage) return a box to its typed pool
    // without knowing T.
    interface IManagedComponentBox
    {
        void ReturnToPool();
    }

    // StrongBox<T> (not a plain box) lets GetComponent hand back a real `ref T` into the boxed value.
    sealed class ManagedComponentBox<T> : StrongBox<T>, IManagedComponentBox where T : struct, IVisualElementComponent
    {
        public void ReturnToPool() => ManagedComponentBoxPool<T>.Pool.Release(this);
    }

    // Not GenericPool (nor the UIElements ObjectPool): their new T() constraints construct through
    // Activator.CreateInstance, while an explicit createFunc compiles to a direct ManagedComponentBox<T> ctor call.
    static class ManagedComponentBoxPool<T> where T : struct, IVisualElementComponent
    {
        [NoAutoStaticsCleanup] // pooled boxes root nothing (see actionOnRelease)
        public static readonly UnityEngine.Pool.ObjectPool<ManagedComponentBox<T>> Pool = new(
            () => new ManagedComponentBox<T>(),
            actionOnRelease: box => box.Value = default); // don't keep removed managed fields rooted while pooled
    }
}
