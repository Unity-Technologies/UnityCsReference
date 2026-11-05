// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using System;
using UnityEngine.Bindings;
using System.Collections.Generic;
using Unity.Scripting.AssemblyManagement;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    public partial class AsyncOperation : YieldInstruction
    {
        [VisibleToOtherModules]
        internal IntPtr m_Ptr;

#pragma warning disable UA5000 // The Avoid Finalizer Analyzer produces compile errors for any new finalizers. This pre-existing finalizer declaration has been suppressed, but should be rewritten if possible.
        ~AsyncOperation()
        {
            InternalDestroy(m_Ptr);
        }
#pragma warning restore UA5000

        private System.Action<AsyncOperation> m_completeCallback;

        [RequiredByNativeCode]
        internal void InvokeCompletionEvent()
        {
            if (m_completeCallback != null)
                m_completeCallback = WithoutCallbacksIntoUnloadingCode(m_completeCallback);
            if (m_completeCallback != null)
            {
                m_completeCallback(this);
                m_completeCallback = null;
            }
        }

        public event System.Action<AsyncOperation> completed
        {
            add
            {
                if (isDone)
                {
                    value(this);
                }
                else
                {
                    m_completeCallback += value;
                    TrackOperationWithCallbacks(this);
                }
            }
            remove
            {
                m_completeCallback -= value;
            }
        }

        // Native code keeps an AsyncOperation alive until it completes, and the operation lives in the Unity ALC, which a
        // code reload does not unload. An operation started before a reload can therefore complete after it, holding
        // callbacks into an ALC that the reload unloads. Running them mixes the old generation of types with the new one
        // (UnloadingAssembly errors, duplicate ScriptableSingletons, InvalidCastException between the two generations),
        // and holding them keeps the unloading ALC alive. So drop them once the new code is loaded, and never run them on
        // completion.
        [NoAutoStaticsCleanup] // pruned by [OnCodeLoaded] DropCallbacksIntoUnloadingCode; holds only weak references
        static readonly List<WeakReference<AsyncOperation>> s_OperationsWithCallbacks = new List<WeakReference<AsyncOperation>>();
        [NoAutoStaticsCleanup] // reset with s_OperationsWithCallbacks
        static int s_PruneOperationsWithCallbacksAt = k_MinPruneOperationsWithCallbacksAt;
        const int k_MinPruneOperationsWithCallbacksAt = 64;

        static void TrackOperationWithCallbacks(AsyncOperation operation)
        {
            lock (s_OperationsWithCallbacks)
            {
                if (s_OperationsWithCallbacks.Count >= s_PruneOperationsWithCallbacksAt)
                    PruneOperationsWithCallbacks();

                s_OperationsWithCallbacks.Add(new WeakReference<AsyncOperation>(operation));
            }
        }

        [OnCodeLoaded]
        static void DropCallbacksIntoUnloadingCode()
        {
            lock (s_OperationsWithCallbacks)
            {
                foreach (var weak in s_OperationsWithCallbacks)
                {
                    if (weak.TryGetTarget(out var operation) && operation.m_completeCallback != null)
                        operation.m_completeCallback = WithoutCallbacksIntoUnloadingCode(operation.m_completeCallback);
                }

                PruneOperationsWithCallbacks();
            }
        }

        static void PruneOperationsWithCallbacks()
        {
            s_OperationsWithCallbacks.RemoveAll(weak => !weak.TryGetTarget(out var operation) || operation.m_completeCallback == null);
            s_PruneOperationsWithCallbacksAt = Math.Max(k_MinPruneOperationsWithCallbacksAt, s_OperationsWithCallbacks.Count * 2);
        }

        static Action<AsyncOperation> WithoutCallbacksIntoUnloadingCode(Action<AsyncOperation> callback)
        {
            var currentAssemblyLoadContext = ICurrentAssemblyLoadContext.Instance;
            if (currentAssemblyLoadContext == null)
                return callback;

            // Where AsyncOperation is reloaded together with the code it calls, an operation from before a reload holds
            // callbacks typed against the previous AsyncOperation, which cannot be cast to this one to remove them one by one.
            var canRemoveOneByOne = callback.GetType() == typeof(Action<AsyncOperation>);
            var invocations = callback.GetInvocationList();
            var remaining = invocations.Length;
            foreach (var invocation in invocations)
            {
                if (!IsInUnloadingCode(currentAssemblyLoadContext, invocation))
                    continue;

                remaining--;
                if (canRemoveOneByOne)
                    callback -= (Action<AsyncOperation>)invocation;
            }

            return remaining == 0 ? null : callback;
        }

        static bool IsInUnloadingCode(ICurrentAssemblyLoadContext currentAssemblyLoadContext, Delegate invocation)
        {
            var declaringType = invocation.Method.DeclaringType;
            var target = invocation.Target;
            return (declaringType != null && currentAssemblyLoadContext.IsUnloading(declaringType.Assembly))
                || (target != null && currentAssemblyLoadContext.IsUnloading(target.GetType().Assembly));
        }

    }
}
