// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Scripting;

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
            if (s_TransformWatchers == null)
                return;

            if (transform == null)
                throw new NullReferenceException(nameof(transform));

            if (callback == null)
                throw new NullReferenceException(nameof(callback));

            // Finish if there's no watcher found.
            if (!s_TransformWatchers.TryGetValue(transform, out var watcher))
                return;

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

        static Dictionary<Transform, HashSet<PhysicsCallbacks.ITransformChangedCallback>> s_TransformWatchers = null;

        #region Native Methods

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
