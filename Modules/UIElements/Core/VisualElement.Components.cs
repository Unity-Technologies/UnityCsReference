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
        // The types live in a shared record; m_ComponentSlots holds this element's state, positionally parallel.
        ComponentTypeSet m_ComponentTypeSet = ComponentTypeSet.Empty;
        ComponentSlot[] m_ComponentSlots;

        // Introspection only (tests, debugger); the record's array is exact-sized.
        internal ReadOnlySpan<RuntimeTypeHandle> componentTypeHandles
            => new ReadOnlySpan<RuntimeTypeHandle>(m_ComponentTypeSet.types);

        // Introspection only (tests, diagnostics): the shared record holding this element's component types.
        internal ComponentTypeSet componentTypeSet => m_ComponentTypeSet;

        // Per-frame dirty record over the component slots: bit i marks slot i changed, bit 31 is the
        // overflow sentinel ("some slot >= 31 changed"). Set by MarkComponentDirty, drained once per
        // frame by VisualTreeComponentUpdater so a component's [OnComponentChanged] handler runs at most
        // once even after a burst of writes. 31 precise slots is well past the typical 1-5 components.
        const uint k_ComponentOverflowBit = 1u << 31;
        uint m_DirtyComponentMask;

        const string k_ReleaseHookMutationExceptionMessage =
            "A [ReleaseComponentResources] method can't add or remove components. It receives only the component being released, runs at a deferred point that can fall in the middle of an unrelated AddComponent, and also runs during code unload. Release the resources the component owns and nothing else.";

        // The guards and messages below are shared by the generic component methods. IL2CPP emits a
        // separate native copy of every generic body per component struct, so anything that needs only
        // the type handle is kept out of them.

        void ThrowIfComponentMutationForbidden()
        {
            if ((m_Flags & VisualElementFlags.Released) != 0)
                throw new InvalidOperationException(k_ElementReleaseExceptionMessage);

            ThrowIfRunningReleaseHook();
        }

        static void ThrowIfRunningReleaseHook()
        {
            if (ComponentManager.SharedManager.IsRunningReleaseHook)
                throw new InvalidOperationException(k_ReleaseHookMutationExceptionMessage);
        }

        void ThrowIfComponentAttached(RuntimeTypeHandle typeHandle)
        {
            if (FindComponentIndex(typeHandle) >= 0)
                throw new InvalidOperationException(
                    $"VisualElement already has a component of type '{Type.GetTypeFromHandle(typeHandle).Name}'. Only one component of a given type is allowed per element.");
        }

        static void ThrowComponentNotAttached(RuntimeTypeHandle typeHandle)
        {
            throw new InvalidOperationException(
                $"VisualElement does not have a component of type '{Type.GetTypeFromHandle(typeHandle).Name}'.");
        }

        static void ThrowComponentStoreTornDown(RuntimeTypeHandle typeHandle)
        {
            throw new InvalidOperationException(
                $"The UI Toolkit component store for '{Type.GetTypeFromHandle(typeHandle).Name}' has been torn down (code/domain unload). Components cannot be added until it is reinitialized.");
        }

        int GetAttachedComponentIndex(RuntimeTypeHandle typeHandle)
        {
            if ((m_Flags & VisualElementFlags.Released) != 0)
                throw new InvalidOperationException(k_ElementReleaseExceptionMessage);

            var index = FindComponentIndex(typeHandle);
            if (index < 0)
                ThrowComponentNotAttached(typeHandle);

            return index;
        }

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
            MarkComponentDirty(typeof(T).TypeHandle);
        }

        internal void MarkComponentDirty(RuntimeTypeHandle typeHandle)
        {
            var i = FindComponentIndex(typeHandle);
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

            // A handler may add or remove components mid-flush, shifting entries inside these very
            // arrays; InvokeComponentChangedAt resolves each dirty bit against this snapshot.
            var set = m_ComponentTypeSet;
            var slots = m_ComponentSlots;
            var count = slots?.Length ?? 0;

            // Precise slots 0-30: invoke only the ones whose bit is set.
            var precise = count < 31 ? count : 31;
            for (var i = 0; i < precise; i++)
            {
                if ((mask & (1u << i)) != 0)
                    InvokeComponentChangedAt(set, slots, i);
            }

            // Overflow tail (>= 31 components, rare): the sentinel can't say which, so notify the tail.
            if ((mask & k_ComponentOverflowBit) != 0)
            {
                for (var i = 31; i < count; i++)
                    InvokeComponentChangedAt(set, slots, i);
            }
        }

        // Dispatches for the component at `index` in the flush snapshot. When the live fields differ,
        // positions have shifted: re-resolve the captured type against live storage; removed ones skip.
        void InvokeComponentChangedAt(ComponentTypeSet capturedSet, ComponentSlot[] capturedSlots, int index)
        {
            if (ReferenceEquals(capturedSet, m_ComponentTypeSet) && ReferenceEquals(capturedSlots, m_ComponentSlots))
            {
                capturedSlots[index].onChanged?.Invoke(this);
                return;
            }

            var types = capturedSet.types;
            if (index >= types.Length)
                return;

            var current = FindComponentIndex(types[index]);
            if (current >= 0)
                m_ComponentSlots[current].onChanged?.Invoke(this);
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
        /// component of each type), if this element's resources have been released, or if called from a
        /// <see cref="ReleaseComponentResourcesAttribute">[ReleaseComponentResources]</see> method.
        /// </exception>
        public void AddComponent<T>(in T value) where T : struct, IVisualElementComponent
        {
            ThrowIfComponentMutationForbidden();

            value.__ValidateOwnerType(this);

            var typeHandle = typeof(T).TypeHandle;
            ThrowIfComponentAttached(typeHandle);

            AttachComponent(typeHandle, in value, out _);
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
            ThrowIfRunningReleaseHook();

            var typeHandle = typeof(T).TypeHandle;
            var index = FindComponentIndex(typeHandle);
            if (index >= 0)
                return ref GetComponentRefAt<T>(index);

            var factory = ComponentManager<T>.CreateInstanceFactory;
            var value = factory != null ? factory() : default;
            value.__ValidateOwnerType(this);

            // When an [OnComponentAdded] method ran it may have moved the slot; resolve again.
            if (!AttachComponent(typeHandle, in value, out var insertIndex))
                return ref GetComponentRefAt<T>(insertIndex);

            return ref GetComponent<T>();
        }

        // Shared by AddComponent and GetOrAddComponent: stores the component and runs the add side
        // effects. Callers have already validated the owner type and rejected duplicates. Returns
        // whether an [OnComponentAdded] method ran (it may have added or removed other components);
        // insertIndex is the slot the component landed in, only reliable when that method did not run.
        unsafe bool AttachComponent<T>(RuntimeTypeHandle typeHandle, in T value, out int insertIndex) where T : struct, IVisualElementComponent
        {
            // Add the [RequiresComponentOfType] components first. This can grow the component arrays,
            // so nothing must be cached across this call.
            value.__ResolveComponentRequirements(this);

            var record = ComponentManager<T>.Record;

            if (!record.isValid)   // shut down between the code unload and the statics cleanup
                ThrowComponentStoreTornDown(typeHandle);

            var slot = new ComponentSlot { record = record };

            // Free one pending slot per slot allocated — the backstop that keeps Collect()'s per-call cap
            // from letting the queue grow under churn.
            ComponentManager.SharedManager.TryRecycleSingleFree();

            if (ComponentManager<T>.IsUnmanaged)
            {
                slot.handle = record.store.Allocate();
                var local = value;
                UnsafeUtility.CopyStructureToPtr(ref local, record.store.GetComponentDataPtr(slot.handle.Index, 0));
            }
            else
            {
                var box = ManagedComponentBoxPool<T>.Pool.Get();
                box.Value = value;
                ComponentManager<T>.Registry.Add(box);
                slot.managedBox = box;
            }

            record.liveCount++;

            // Cache the component's shared per-type dispatchers ([OnComponentChanged]; binding dispatch),
            // so later code can invoke them without knowing T. Constrained generic calls, no boxing.
            slot.onChanged = value.__GetComponentChangedDispatcher();
            slot.bindingDispatcher = value.__GetComponentBindingDispatcher();

            insertIndex = InsertComponentSlot(typeHandle, slot);

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
            return ref GetComponentRefAt<T>(GetAttachedComponentIndex(typeof(T).TypeHandle));
        }

        // No existence/bounds check: callers must resolve the index via FindComponentIndex first.
        unsafe ref T GetComponentRefAt<T>(int index) where T : struct, IVisualElementComponent
        {
            ref var slot = ref m_ComponentSlots[index];
            if (ComponentManager<T>.IsUnmanaged)
                return ref UnsafeUtility.AsRef<T>(slot.record.store.GetComponentDataPtr(slot.handle.Index, 0));

            return ref ((ManagedComponentBox<T>)slot.managedBox).Value;
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
                ThrowComponentNotAttached(typeof(T).TypeHandle);
        }

        /// <summary>
        /// Removes the attached component of type <typeparamref name="T"/>, if one is attached.
        /// </summary>
        /// <typeparam name="T">A component struct declared with <see cref="VisualElementComponentAttribute"/>.</typeparam>
        /// <returns><see langword="true"/> if a component was removed; <see langword="false"/> if none was attached.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this element's resources have been released, or if called from a
        /// <see cref="ReleaseComponentResourcesAttribute">[ReleaseComponentResources]</see> method.
        /// </exception>
        public bool TryRemoveComponent<T>() where T : struct, IVisualElementComponent
        {
            ThrowIfComponentMutationForbidden();

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

            FreeComponentSlot(ref m_ComponentSlots[index]);
            RemoveComponentSlotAt(index);
            return true;
        }

        // Logs a warning when another attached component declares the removed component as required
        // with [RequiresComponentOfType]. Editor only; compiled out of player builds.
        void WarnIfComponentStillRequired(RuntimeTypeHandle removedType, int removedIndex)
        {
            var types = m_ComponentTypeSet.types;
            for (var i = 0; i < types.Length; i++)
            {
                if (i == removedIndex)
                    continue;

                var required = ComponentRequirementRegistry.GetRequiredTypes(types[i]);
                if (required == null)
                    continue;

                for (var r = 0; r < required.Length; r++)
                {
                    if (required[r].Equals(removedType))
                    {
                        Debug.LogWarning(
                            $"[UI Toolkit] Removing component '{Type.GetTypeFromHandle(removedType).Name}' from this element, but the attached component '{Type.GetTypeFromHandle(types[i]).Name}' declares it required with [RequiresComponentOfType]. The dependent component may no longer work.");
                        break;
                    }
                }
            }
        }

        // Frees every component's storage as part of element teardown. Called from both
        // ~VisualElement (GC finalizer thread) and ReleaseResourcesNoChecks (main thread), so it
        // must stay safe to run off the main thread: it only enqueues on the manager's concurrent
        // free-queue and drops the slot arrays.
        internal void ReleaseComponentStorage(bool fromFinalizer = false)
        {
            var slots = m_ComponentSlots;
            if (slots == null)
                return;

            if (ComponentManager.IsSharedManagerCreated)
            {
                var manager = ComponentManager.SharedManager;
                var count = m_ComponentTypeSet.Count;
                for (var i = 0; i < count; i++)
                {
                    ref var slot = ref slots[i];
                    if (slot.managedBox != null)
                        manager.EnqueueFree(slot.record, slot.managedBox);
                    else
                        manager.EnqueueFree(slot.record, slot.handle);
                }
            }

            // ReleaseRef is main-thread only; the finalizer thread must enqueue.
            if (fromFinalizer)
                ComponentTypeSet.EnqueueRelease(m_ComponentTypeSet);
            else
                m_ComponentTypeSet.ReleaseRef();

            m_ComponentTypeSet = ComponentTypeSet.Empty;
            m_ComponentSlots = null;
            m_DirtyComponentMask = 0;
        }

        internal ComponentBindingDispatcher GetComponentBindingDispatcher(RuntimeTypeHandle typeHandle)
        {
            var index = FindComponentIndex(typeHandle);
            return index >= 0 ? m_ComponentSlots[index].bindingDispatcher : null;
        }

        int FindComponentIndex(RuntimeTypeHandle typeHandle)
        {
            var types = m_ComponentTypeSet.types;
            for (var i = 0; i < types.Length; i++)
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

            var types = m_ComponentTypeSet.types;
            var matchIndex = -1;
            for (var i = 0; i < types.Length; i++)
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

        internal int componentCountForDebug => m_ComponentTypeSet.Count;

        internal IEnumerable<(Type type, object value)> EnumerateComponentsForDebug()
        {
            var types = m_ComponentTypeSet.types;
            for (var i = 0; i < types.Length; i++)
            {
                // Cold path: re-resolve per yield so a removal between MoveNexts skips, never faults.
                var index = FindComponentIndex(types[i]);
                if (index < 0)
                    continue;

                yield return (Type.GetTypeFromHandle(types[i]), BoxComponentForDebug(index));
            }
        }

        internal IEnumerable<Type> EnumerateComponentTypesForDebug()
        {
            var types = m_ComponentTypeSet.types;
            for (var i = 0; i < types.Length; i++)
                yield return Type.GetTypeFromHandle(types[i]);
        }

        unsafe object BoxComponentForDebug(int index)
        {
            ref var slot = ref m_ComponentSlots[index];
            if (slot.managedBox != null)
                return ((IStrongBox)slot.managedBox).Value;

            // Copy the live bytes with the store's own stride (UnsafeUtility.SizeOf), not Marshal, so the read
            // can't diverge from how the value was written. Unmanaged-only here (no references), so pin + raw copy is valid.
            var type = Type.GetTypeFromHandle(m_ComponentTypeSet.types[index]);
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

        // Transitions the record and inserts the slot at the type's canonical index; returns that index.
        int InsertComponentSlot(RuntimeTypeHandle typeHandle, ComponentSlot slot)
        {
            var set = m_ComponentTypeSet;
            var found = set.Find(typeHandle, out var index);
            // Callers reject duplicates before requirements resolution; a hit here means that broke.
            Debug.Assert(!found, "Inserting a component type that is already in the element's type set.");
            var successor = ComponentTypeSet.GetWithAdded(set, typeHandle, index);

            var count = set.Count;
            var capacity = m_ComponentSlots?.Length ?? 0;
            if (count == capacity)
            {
                // Start at 1 (most elements carry a single component), then double.
                Array.Resize(ref m_ComponentSlots, capacity == 0 ? 1 : capacity * 2);
            }

            for (var i = count; i > index; i--)
                m_ComponentSlots[i] = m_ComponentSlots[i - 1];
            m_ComponentSlots[index] = slot;

            m_DirtyComponentMask = ShiftDirtyMaskForInsert(m_DirtyComponentMask, index);

            successor.AddRef();
            set.ReleaseRef();
            m_ComponentTypeSet = successor;
            return index;
        }

        void FreeComponentSlot(ref ComponentSlot slot)
        {
            if (slot.managedBox != null)
                ComponentManager.SharedManager.EnqueueFree(slot.record, slot.managedBox);
            else
                ComponentManager.SharedManager.EnqueueFree(slot.record, slot.handle);

            slot.record = null;
            slot.handle = UnmanagedDataHandle.Undefined;
            slot.managedBox = null;
        }

        // Order-preserving removal: the successor record must depend only on content, never on element history.
        void RemoveComponentSlotAt(int index)
        {
            var set = m_ComponentTypeSet;
            var successor = ComponentTypeSet.GetWithRemoved(set, index);

            var last = set.Count - 1;
            m_DirtyComponentMask = ShiftDirtyMaskForRemove(m_DirtyComponentMask, index, last);

            for (var i = index; i < last; i++)
                m_ComponentSlots[i] = m_ComponentSlots[i + 1];

            // Clear the freed tail slot so it can't root a dispatcher past the live range. Capacity is kept,
            // even at count 0, so steady-state add/remove churn never reallocates the array.
            m_ComponentSlots[last] = default;

            successor.AddRef();
            set.ReleaseRef();
            m_ComponentTypeSet = successor;
        }

        // Pending bits at and above the insertion index move up one. Precise bits only: bit 30 merges
        // into the bit-31 overflow sentinel, the only representation for slots past 30.
        static uint ShiftDirtyMaskForInsert(uint mask, int index)
        {
            if (mask == 0 || index >= 31)
                return mask;

            var below = mask & ((1u << index) - 1);
            var atOrAbove = mask & ~((1u << index) - 1) & ~k_ComponentOverflowBit;
            return below | (atOrAbove << 1) | (mask & k_ComponentOverflowBit);
        }

        // The removed index's bit clears and precise bits above it move down one. Slot 31 lands on 30,
        // which the sentinel no longer covers, so a set sentinel also sets bit 30 and only survives while
        // a slot >= 31 remains. Coarse either way: the sentinel names no single slot.
        static uint ShiftDirtyMaskForRemove(uint mask, int index, int remainingCount)
        {
            if (mask == 0)
                return mask;

            // Removing from the tail moves no precise bit, but a sentinel left with no slot >= 31 would
            // name whatever lands in slot 31 next. Keeping it retired maintains "sentinel set => count > 31",
            // which is also what lets ShiftDirtyMaskForInsert pass a tail insertion straight through.
            if (index >= 31)
                return remainingCount > 31 ? mask : mask & ~k_ComponentOverflowBit;

            var below = mask & ((1u << index) - 1);
            var above = mask & ~((2u << index) - 1) & ~k_ComponentOverflowBit;
            var shifted = below | (above >> 1);
            if ((mask & k_ComponentOverflowBit) == 0)
                return shifted;

            shifted |= 1u << 30;
            return remainingCount > 31 ? shifted | k_ComponentOverflowBit : shifted;
        }
    }
}
