// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <undoc/>
    readonly struct PhysicsTransformWatcher
    {
        public static void RegisterWatcher(Transform transform, PhysicsCallbacks.ITransformChangedCallback callback)
        {
            if (transform == null)
                throw new NullReferenceException(nameof(transform));

            if (callback == null)
                throw new NullReferenceException(nameof(callback));

            // Create transform watchers if not initialized.
            if (s_TransformWatchers == null)
                s_TransformWatchers = new();

            // Add the callback if we already have a transform watcher.
            if (s_TransformWatchers.TryGetValue(transform, out var watcher))
            {
                watcher.Add(callback);
                return;
            }

            // Add a new watched/callback.
            var callbacks = HashSetPool<PhysicsCallbacks.ITransformChangedCallback>.Get();
            callbacks.Add(callback);
            s_TransformWatchers.Add(transform, callbacks);

            // Register the transform dispatch watcher.
            PhysicsCore2D_RegisterTransformWatcher(transform);
        }

        public static void UnregisterWatcher(Transform transform, PhysicsCallbacks.ITransformChangedCallback callback)
        {
            // The watcher store is dropped whole on subsystem teardown, but the native interest lives on the transform itself and survives that.
            // Clear it here rather than returning early, or a transform that outlives teardown is later destroyed with its interest still set.
            if (s_TransformWatchers == null)
            {
                if (transform != null)
                    PhysicsCore2D_UnregisterTransformWatcher(transform);

                return;
            }

            if (transform == null)
                throw new NullReferenceException(nameof(transform));

            if (callback == null)
                throw new NullReferenceException(nameof(callback));

            // No tracked watcher remains, so clear the native interest for the same reason as above.
            if (!s_TransformWatchers.TryGetValue(transform, out var watcher))
            {
                PhysicsCore2D_UnregisterTransformWatcher(transform);
                return;
            }

            // Remove the callback.
            watcher.Remove(callback);

            // Finish if callbacks exist.
            if (watcher.Count > 0)
                return;

            // Release the watcher.
            HashSetPool<PhysicsCallbacks.ITransformChangedCallback>.Release(watcher);

            // Remove from the watchers.
            s_TransformWatchers.Remove(transform);

            // Unregister the transform dispatch watcher.
            PhysicsCore2D_UnregisterTransformWatcher(transform);
        }

        public static void SendCallbacks(ref NativeArray<PhysicsEvents.TransformChangeEvent> transformChangeEvents)
        {
            // Finish if no transform watcher.
            if (s_TransformWatchers == null)
                return;

            // Iterate the transform change events.
            foreach (var callbackEvent in transformChangeEvents)
            {
                // Fetch the transform.
                var transform = callbackEvent.transform;
                if (transform != null)
                {
                    // Fetch the callbacks for this watcher.
                    if (s_TransformWatchers.TryGetValue(transform, out var changeCallbacks))
                    {
                        // Perform all the callbacks.
                        foreach (var callback in changeCallbacks)
                            callback?.OnTransformChanged(callbackEvent);

                        continue;
                    }

                    // We should never get here but if we somehow had a transform change without a watcher
                    // then unregister the transform dispatch watcher.
                    PhysicsCore2D_UnregisterTransformWatcher(transform);
                }
            }
        }

        // Watcher callbacks can point into reloadable code, but AutoStaticsCleanup codegen in this
        // player-shipped module forces the PhysicsCore2D module into stripped player builds
        // (TestStrippingDependencies). Persist as before until lifecycle registration is strip-safe.
        [NoAutoStaticsCleanup] // see stripping note above
        static Dictionary<Transform, HashSet<PhysicsCallbacks.ITransformChangedCallback>> s_TransformWatchers = null;

        #region Native Methods

        /// <undoc/>
        [RequiredByNativeCode]
        static void ClearAllWatchers()
        {
            // Called from native subsystem teardown (PhysicsWorldManager2D::DestroyScriptObjects).
            // Drops the watcher store so it does not span a scripting reload.
            // The native interest is left set on each transform, because a transform can outlive this teardown.
            // UnregisterWatcher clears it when the owning component disables.
            if (s_TransformWatchers == null)
                return;

            // Return each pooled callback set to the pool before dropping the dictionary.
            foreach (var watcher in s_TransformWatchers.Values)
                HashSetPool<PhysicsCallbacks.ITransformChangedCallback>.Release(watcher);

            s_TransformWatchers.Clear();
            s_TransformWatchers = null;
        }

        /// <undoc/>
        [RequiredByNativeCode]
        static void TransformChangedCallback(PhysicsBuffer physicsBuffer)
        {
            var transformChangeEvents = physicsBuffer.ToNativeArray<PhysicsEvents.TransformChangeEvent>();
            PhysicsTransformWatcher.SendCallbacks(ref transformChangeEvents);
            transformChangeEvents.Dispose();
        }

        /// <undoc/>
        [RequiredByNativeCode]
        static void TransformParentHierarchyChangedCallback(PhysicsBuffer physicsBuffer)
        {
            var transformChangeEvents = physicsBuffer.ToNativeArray<PhysicsEvents.TransformChangeEvent>();
            PhysicsTransformWatcher.SendCallbacks(ref transformChangeEvents);
            transformChangeEvents.Dispose();
        }

        #endregion
    }
}
