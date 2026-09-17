// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.UIElements
{
    // Lets the non-generic teardown and release paths reach a box without knowing the component type.
    interface IManagedComponentBox
    {
        void ReleaseResources();
        void Recycle();
    }

    /// <summary>
    /// Storage for one instance of a component that carries a managed reference, held by the owning
    /// element's <see cref="ComponentSlot"/>.
    /// </summary>
    /// <remarks>
    /// The element is the box's only strong root, which is what makes a component that points back at its
    /// own element a self-cycle the GC collects, rather than a leak. <see cref="StrongBox{T}"/> rather than
    /// a plain box is what lets <c>GetComponent</c> hand back a real <c>ref T</c> into the stored value.
    /// </remarks>
    sealed class ManagedComponentBox<T> : StrongBox<T>, IManagedComponentBox where T : struct, IVisualElementComponent
    {
        // The registry this box is registered in, and its slot there, or null and -1 while unregistered.
        // The registry is held rather than resolved per type so that a box outliving a code reload
        // unregisters from the registry that indexed it: a fresh one would apply the stale index to an
        // unrelated live box and drop that component from the next unload sweep.
        public ManagedComponentRegistry<T> registry;
        public int registryIndex = -1;

        bool m_Released;

        public void ReleaseResources()
        {
            // The unload sweep and the deferred free-queue can both reach a box — the queue accepts
            // finalizer enqueues while the sweep is pinning and running hooks — so releasing is once only.
            if (m_Released)
                return;

            m_Released = true;

            try
            {
                ComponentManager<T>.ReleaseResourcesHandler?.Invoke(ref Value);
            }
            finally
            {
                Value = default;   // clear even when the method threw
            }
        }

        public void Recycle()
        {
            registry?.Remove(this);
            ManagedComponentBoxPool<T>.Pool.Release(this);
        }

        // A pooled box must carry nothing from the instance that used it last.
        internal void ResetForReuse()
        {
            Value = default;
            registry = null;
            registryIndex = -1;
            m_Released = false;
        }
    }

    // Not GenericPool (nor the UIElements ObjectPool): their new T() constraints construct through
    // Activator.CreateInstance, while an explicit createFunc compiles to a direct ManagedComponentBox<T> ctor call.
    static class ManagedComponentBoxPool<T> where T : struct, IVisualElementComponent
    {
        // Cleanup would null a readonly field the class initializer never re-runs to restore.
        [NoAutoStaticsCleanup]
        public static readonly UnityEngine.Pool.ObjectPool<ManagedComponentBox<T>> Pool = new(
            () => new ManagedComponentBox<T>(),
            actionOnRelease: box => box.ResetForReuse());
    }
}
