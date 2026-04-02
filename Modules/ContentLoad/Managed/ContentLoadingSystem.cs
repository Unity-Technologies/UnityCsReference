// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// Uncomment to enable detailed debug logging for ContentLoadingSystem
//#define CONTENTLOAD_DEBUG_LOGGING

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Scripting;
using Unity.Loading.LowLevel;
using System.Collections;

namespace Unity.Loading
{
    /// <summary>
    /// Base class for asynchronous object load operations from content directories.
    /// </summary>
    /*UCBP-PUBLIC*/ internal abstract class ObjectLoadOperationBase : IEnumerator
    {
        protected ContentLoadingSystem.ResourceOperationHandle m_OperationHandle;
        protected LoadableReference m_loadableReferenceId;

        protected bool m_IsDone;
        protected bool m_Success;

        [ExcludeFromDocs]
        internal ObjectLoadOperationBase(LoadableReference loadableReferenceId, ContentLoadingSystem.ResourceOperationHandle operationHandle)
        {
            m_loadableReferenceId = loadableReferenceId;
            m_OperationHandle = operationHandle;
            m_IsDone = false;
            m_Success = false;
        }

        /// <summary>
        /// The handle for this load operation, used when releasing the loaded object.
        /// </summary>
        public ContentLoadingSystem.ResourceOperationHandle operationHandle => m_OperationHandle;

        /// <summary>
        /// Whether the load operation has completed, either successfully or with failure.
        /// </summary>
        public bool isDone => m_IsDone;

        /// <summary>
        /// Whether the load completed successfully. Only valid when <see cref="isDone"/> is true.
        /// </summary>
        public bool success => m_Success;

        /// <summary>
        /// The loaded object, or null if the load failed or is not yet complete.
        /// </summary>
        public UnityEngine.Object result => GetResultObject();

        [ExcludeFromDocs]
        protected abstract UnityEngine.Object GetResultObject();

        [ExcludeFromDocs]
        internal abstract bool Complete(EntityId entityId, bool logErrors);

        /// <summary>
        /// Blocks until the load completes or the timeout expires.
        /// </summary>
        /// <returns>True if the load completed; false if the timeout expired.</returns>
        public bool WaitForCompletion()
        {
            if (m_IsDone)
                return true;

            ContentLoadingSystem.DebugLog($"WaitForCompletion (Load): blocking on handle=0x{m_OperationHandle.value:X}");

            ContentLoadingSystem.WaitForLoadCompletion(m_OperationHandle);
            return m_IsDone;
        }

        [ExcludeFromDocs]
        public object Current => null;
        [ExcludeFromDocs]
        public bool MoveNext() => !m_IsDone;
        [ExcludeFromDocs]
        public void Reset() { }
    }

    /// <summary>
    /// Asynchronous operation for loading an object of a specific type from a content directory.
    /// </summary>
    /// <typeparam name="T">The type of object to load, must inherit from UnityEngine.Object.</typeparam>
    /*UCBP-PUBLIC*/ internal sealed class ObjectLoadOperation<T> : ObjectLoadOperationBase where T : UnityEngine.Object
    {
        private T m_Result;
        private Action<ObjectLoadOperation<T>> m_Completed;
        private AwaitableCompletionSource<T> m_CompletionSource;
        private bool m_HasBeenAwaited;

        [ExcludeFromDocs]
        internal ObjectLoadOperation(LoadableReference loadableReferenceId, ContentLoadingSystem.ResourceOperationHandle operationHandle)
            : base(loadableReferenceId, operationHandle)
        {
            m_Result = null;
            m_CompletionSource = new AwaitableCompletionSource<T>();
            m_HasBeenAwaited = false;
        }

        /// <summary>
        /// The loaded object cast to type T, or null if the load failed or is not yet complete.
        /// </summary>
        public new T result => m_Result;

        [ExcludeFromDocs]
        protected override UnityEngine.Object GetResultObject() => m_Result;

        /// <summary>
        /// Invoked when the load operation completes, whether successfully or with failure.
        /// </summary>
        public event Action<ObjectLoadOperation<T>> completed
        {
            add
            {
                if (m_IsDone)
                    value?.Invoke(this);
                else
                    m_Completed += value;
            }
            remove
            {
                m_Completed -= value;
            }
        }

        /// <summary>
        /// Returns an awaiter for use with async/await.
        /// </summary>
        /// <returns>The Awaiter struct.</returns>
        /// <exception cref="Exception">An exception is thrown if the operation is awaited multiple times.</exception>
        public Awaitable<T>.Awaiter GetAwaiter()
        {
            if (m_HasBeenAwaited)
            {
                throw new Exception($"ObjectLoadOperation<{typeof(T).Name}> for {m_loadableReferenceId} is being awaited multiple times. " +
                    "Awaiting the same operation more than once is not supported and may lead to unexpected behavior. " +
                    "Use the 'result' property to access the loaded object after the first await completes.");
            }
            m_HasBeenAwaited = true;

            return m_CompletionSource.Awaitable.GetAwaiter();
        }

        [ExcludeFromDocs]
        internal override bool Complete(EntityId entityId, bool logErrors)
        {
            UnityEngine.Object untypedResult = entityId.IsValid() ? Resources.EntityIdToObject(entityId) : null;
            m_Result = untypedResult as T;
            if (m_Result == null && logErrors)
            {
                if (untypedResult != null)
                {
                    Debug.LogError($"{nameof(ObjectLoadOperation<T>)}<{typeof(T)}> {m_loadableReferenceId} cannot cast the loaded object to <{typeof(T)}>. The loaded object has type {untypedResult.GetType()}");
                }
                else
                {
                    Debug.LogError($"{nameof(ObjectLoadOperation<T>)}<{typeof(T)}> {m_loadableReferenceId} cannot be loaded. The asset might not exist in the build or it has already been released.");
                }
            }

            m_IsDone = true;
            m_Success = m_Result != null;

            ContentLoadingSystem.DebugLog($"ObjectLoadOperation<{typeof(T).Name}> completed: handle=0x{m_OperationHandle.value:X}, success={m_Success}, ref={m_loadableReferenceId}");
            try
            {
                m_Completed?.Invoke(this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(ObjectLoadOperation<T>)}<{typeof(T)}> {m_loadableReferenceId} Exception thrown when invoking completed event: {ex}");
            }

            m_Completed = null;

            try
            {
                m_CompletionSource.TrySetResult(m_Result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(ObjectLoadOperation<T>)}<{typeof(T)}> {m_loadableReferenceId} Exception thrown when invoking continuation action: {ex}");
            }

            return m_Success;
        }
    }

    /// <summary>
    /// Asynchronous operation for releasing an object that was loaded from a content directory.
    /// </summary>
    /*UCBP-PUBLIC*/ internal sealed class ObjectReleaseOperation : IEnumerator
    {
        private ContentLoadingSystem.ResourceOperationHandle m_OperationHandle;
        private bool m_IsDone;
        private Action<ObjectReleaseOperation> m_Completed;
        private AwaitableCompletionSource m_CompletionSource;
        private bool m_HasBeenAwaited;

        [ExcludeFromDocs]
        internal ObjectReleaseOperation(ContentLoadingSystem.ResourceOperationHandle operationHandle)
        {
            m_OperationHandle = operationHandle;
            m_IsDone = false;
            m_CompletionSource = new AwaitableCompletionSource();
            m_HasBeenAwaited = false;
        }

        /// <summary>
        /// The handle for this release operation.
        /// </summary>
        public ContentLoadingSystem.ResourceOperationHandle operationHandle => m_OperationHandle;

        /// <summary>
        /// Whether the release operation has completed.
        /// </summary>
        public bool isDone => m_IsDone;

        /// <summary>
        /// Invoked when the release operation completes.
        /// </summary>
        public event Action<ObjectReleaseOperation> completed
        {
            add
            {
                if (m_IsDone)
                    value?.Invoke(this);
                else
                    m_Completed += value;
            }
            remove
            {
                m_Completed -= value;
            }
        }

        /// <summary>
        /// Returns an awaiter for use with async/await.
        /// </summary>
        /// <returns>The Awaiter struct.</returns>
        /// <exception cref="Exception">An exception is thrown if the operation is awaited multiple times.</exception>
        public Awaitable.Awaiter GetAwaiter()
        {
            if (m_HasBeenAwaited)
            {
                throw new Exception($"ObjectReleaseOperation for handle 0x{m_OperationHandle.value:X} is being awaited multiple times. " +
                    "Awaiting the same operation more than once is not supported and may lead to unexpected behavior.");
            }
            m_HasBeenAwaited = true;

            return m_CompletionSource.Awaitable.GetAwaiter();
        }

        /// <summary>
        /// Blocks until the release completes or the timeout expires.
        /// </summary>
        /// <returns>True if the release completed; false if the timeout expired.</returns>
        public bool WaitForCompletion()
        {
            if (m_IsDone)
                return true;

            ContentLoadingSystem.DebugLog($"WaitForCompletion (Release): blocking on handle=0x{m_OperationHandle.value:X}");
            ContentLoadingSystem.WaitForReleaseCompletion(m_OperationHandle);
            return m_IsDone;
        }

        [ExcludeFromDocs]
        internal void Complete()
        {
            m_IsDone = true;

            ContentLoadingSystem.DebugLog($"ObjectReleaseOperation completed: handle=0x{m_OperationHandle.value:X}");

            try
            {
                m_Completed?.Invoke(this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(ObjectReleaseOperation)} with handle=0x{m_OperationHandle.value:X} Exception thrown when invoking completed event: {ex}");
            }

            m_Completed = null;

            try
            {
                m_CompletionSource.TrySetResult();
            }
            catch (Exception ex)
            {
                Debug.LogError($"{nameof(ObjectReleaseOperation)} with handle=0x{m_OperationHandle.value:X} Exception thrown when invoking continuation action: {ex}");
            }
        }

        [ExcludeFromDocs]
        public object Current => null;
        [ExcludeFromDocs]
        public bool MoveNext() => !m_IsDone;
        [ExcludeFromDocs]
        public void Reset() { }
    }

    /// <summary>
    /// High-level API for loading and releasing objects from content directories.
    /// </summary>
    /// <remarks>
    /// Use <see cref="LoadObjectAsync{T}"/> to load objects asynchronously and
    /// <see cref="ReleaseObjectAsync"/> to release them when no longer needed.
    /// </remarks>
    /*UCBP-PUBLIC*/ internal static partial class ContentLoadingSystem
    {
        /// <summary>
        /// Represents a handle to a resource operation in the L1 loading system.
        /// </summary>
        public struct ResourceOperationHandle : IEquatable<ResourceOperationHandle>
        {
            [ExcludeFromDocs]
            internal ulong value;

            [ExcludeFromDocs]
            public bool Equals(ResourceOperationHandle other) => value == other.value;
            [ExcludeFromDocs]
            public override bool Equals(object obj) => obj is ResourceOperationHandle other && Equals(other);
            [ExcludeFromDocs]
            public override int GetHashCode() => value.GetHashCode();
            [ExcludeFromDocs]
            public static bool operator ==(ResourceOperationHandle lhs, ResourceOperationHandle rhs) => lhs.value == rhs.value;
            [ExcludeFromDocs]
            public static bool operator !=(ResourceOperationHandle lhs, ResourceOperationHandle rhs) => lhs.value != rhs.value;

            /// <summary>
            /// Whether this handle refers to an active operation.
            /// </summary>
            public bool isValid => value != 0;
        }

        private static LoadingResponseQueue s_ResultBuffer;
        private static Dictionary<ulong, ObjectLoadOperationBase> s_PendingLoadOperations;
        private static Dictionary<ulong, ObjectReleaseOperation> s_PendingReleaseOperations;
        private static bool s_Initialized;
        private const int kResultsBatchSize = 32;

        static ContentLoadingSystem()
        {
            Initialize();
        }

        private static void Initialize()
        {
            if (!s_Initialized)
            {
                s_ResultBuffer = new LoadingResponseQueue();
                s_PendingLoadOperations = new Dictionary<ulong, ObjectLoadOperationBase>();
                s_PendingReleaseOperations = new Dictionary<ulong, ObjectReleaseOperation>();
                s_Initialized = true;
                DebugLog("ContentLoadingSystem initialized");
            }
        }

        [System.Diagnostics.Conditional("CONTENTLOAD_DEBUG_LOGGING")]
        internal static void DebugLog(string message)
        {
            Debug.Log($"[ContentLoadingSystem] {message}");
        }

        internal static ResourceHandle ToL0Handle(ResourceOperationHandle operationHandle)
        {
            return new ResourceHandle { value = operationHandle.value };
        }

        internal static void WaitForLoadCompletion(ResourceOperationHandle operationHandle)
        {
            if (!s_Initialized)
            {
                DebugLog($"WaitForLoadCompletion called after the {nameof(ContentLoadingSystem)} has shut down, handle=0x{operationHandle.value:X}.");
                return;
            }

            unsafe
            {
                ResourceHandle l0Handle = ToL0Handle(operationHandle);
                NativeLoadingSystem.WaitForLoadCompletion(&l0Handle, 1);
            }
            ProcessResults();
        }

        internal static void WaitForReleaseCompletion(ResourceOperationHandle operationHandle)
        {
            if (!s_Initialized)
            {
                DebugLog($"WaitForReleaseCompletion called after the {nameof(ContentLoadingSystem)} has shut down, handle=0x{operationHandle.value:X}.");
                return;
            }

            unsafe
            {
                ResourceHandle l0Handle = ToL0Handle(operationHandle);
                NativeLoadingSystem.WaitForReleaseCompletion(&l0Handle, 1);
            }
            ProcessResults();
        }

        /// <summary>
        /// Starts an asynchronous load of an object from a content directory.
        /// </summary>
        /// <typeparam name="T">The type of object to load, must inherit from UnityEngine.Object.</typeparam>
        /// <param name="objectId">Reference to the object to load.</param>
        /// <param name="operationHandle">Receives the handle for this operation, used when releasing the object.</param>
        /// <returns>An operation that completes when the load finishes. Use <see cref="ObjectLoadOperation{T}.result"/>, <see cref="ObjectLoadOperation{T}.completed"/>, or await to get the result.</returns>
        public static ObjectLoadOperation<T> LoadObjectAsync<T>(LoadableReference objectId, out ResourceOperationHandle operationHandle) where T : UnityEngine.Object
        {
            ObjectLoadOperation<T> operation = null;
            if (!s_Initialized)
            {
                DebugLog($"LoadObjectAsync<{typeof(T).Name}> called after the {nameof(ContentLoadingSystem)} has shut down, ref={objectId}.");
                operationHandle = default;
                operation = new ObjectLoadOperation<T>(objectId, default);
                operation.Complete(default, false);
            }
            else
            {
                unsafe
                {
                    ResourceHandle l0Handle;
                    LoadableReference reference = objectId;
                    NativeLoadingSystem.LoadAsync(&reference, &l0Handle, 1, s_ResultBuffer);

                    // L1 handle uses the same value as L0 handle
                    ResourceOperationHandle opHandle = new ResourceOperationHandle { value = l0Handle.value };
                    operationHandle = opHandle;

                    operation = new ObjectLoadOperation<T>(objectId, opHandle);
                    s_PendingLoadOperations[opHandle.value] = operation;

                    DebugLog($"LoadObjectAsync<{typeof(T).Name}> started: handle=0x{opHandle.value:X}, ref={objectId}, pending={s_PendingLoadOperations.Count}");
                }
            }
            return operation;
        }

        /// <summary>
        /// Starts an asynchronous release of a previously loaded object.
        /// </summary>
        /// <param name="operationHandle">The handle from the load operation that loaded the object.</param>
        /// <returns>An operation that completes when the release finishes.</returns>
        public static ObjectReleaseOperation ReleaseObjectAsync(ResourceOperationHandle operationHandle)
        {
            ObjectReleaseOperation operation = null;
            if (!s_Initialized)
            {
                DebugLog($"ReleaseObjectAsync called after the {nameof(ContentLoadingSystem)} has shut down, handle=0x{operationHandle.value:X}.");
                operation = new ObjectReleaseOperation(operationHandle);
                operation.Complete();
            }
            else
            {
                unsafe
                {
                    ResourceHandle l0Handle = ToL0Handle(operationHandle);
                    NativeLoadingSystem.ReleaseAsync(&l0Handle, 1, s_ResultBuffer);
                }

                operation = new ObjectReleaseOperation(operationHandle);
                if (!s_PendingReleaseOperations.TryAdd(operationHandle.value, operation))
                {
                    DebugLog($"ReleaseObjectAsync called multiple times with handle=0x{operationHandle.value:X}, pending={s_PendingReleaseOperations.Count}");
                    operation.Complete();
                }

                DebugLog($"ReleaseObjectAsync started: handle=0x{operationHandle.value:X}, pending={s_PendingReleaseOperations.Count}");
            }
            return operation;
        }

        [ExcludeFromDocs]
        [RequiredByNativeCode(Optional = true)]
        internal unsafe static void ProcessResults()
        {
            if (!s_Initialized)
                return;

            AsyncResult* results = stackalloc AsyncResult[kResultsBatchSize];
            int numResults;
            int totalProcessed = 0;
            List<ResourceHandle> handlesToRelease = null;
            while ((numResults = s_ResultBuffer.ConsumeResults(results, kResultsBatchSize)) > 0)
            {
                totalProcessed += numResults;
                for (int i = 0; i < numResults; i++)
                {
                    if (!ProcessResult(results[i]))
                    {
                        if(handlesToRelease == null)
                            handlesToRelease = new List<ResourceHandle>();
                        handlesToRelease.Add(results[i].handle);
                    }
                }
            }

            if (totalProcessed > 0)
                DebugLog($"ProcessResults: processed {totalProcessed} results, pendingLoads={s_PendingLoadOperations.Count}, pendingReleases={s_PendingReleaseOperations.Count}");

            //there may have been some failed loads that triggered a release - these must be completed synchronously to preserve expected behavior
            if (handlesToRelease != null)
            {
                DebugLog($"ProcessResults: performing {handlesToRelease.Count} internal cleanup releases");
                var waitHandles = handlesToRelease.ToArray();
                fixed (ResourceHandle* waitHandlesPtr = waitHandles)
                {
                    NativeLoadingSystem.ReleaseAsync(waitHandlesPtr, waitHandles.Length, s_ResultBuffer);
                    NativeLoadingSystem.WaitForReleaseCompletion(waitHandlesPtr, waitHandles.Length);
                }
            }
        }

        private static bool ProcessResult(AsyncResult result)
        {
            // L1 handle value is the same as L0 handle value
            ulong handleValue = result.handle.value;

            switch (result.type)
            {
                case AsyncResultType.Load:
                    if (s_PendingLoadOperations.Remove(handleValue, out ObjectLoadOperationBase loadOp))
                    {
                        if (result.resultCode == ReturnCode.Completed)
                        {
                            DebugLog($"ProcessResult: Load completed for handle=0x{handleValue:X}, objectId={result.objectId}");
                            return loadOp.Complete(result.objectId, true);
                        }
                        else if (result.resultCode == ReturnCode.Failed)
                        {
                            DebugLog($"ProcessResult: Load failed for handle=0x{handleValue:X}");
                            loadOp.Complete(default, true);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"ContentLoadingSystem received load result for unknown handle 0x{handleValue:X}. This may indicate a system error or operation completed during shutdown.");
                    }
                    break;
                case AsyncResultType.Release:
                    if (s_PendingReleaseOperations.Remove(handleValue, out ObjectReleaseOperation releaseOp))
                    {
                        DebugLog($"ProcessResult: Release completed for handle=0x{handleValue:X}");
                        releaseOp.Complete();
                    }
                    // Note: Release operations from internal cleanup are not tracked, so missing operations are expected
                    break;
            }
            return true;
        }

        [RequiredByNativeCode(Optional = true)]
        static void Shutdown()
        {
            if (s_Initialized)
            {
                int pendingLoads = s_PendingLoadOperations?.Count ?? 0;
                int pendingReleases = s_PendingReleaseOperations?.Count ?? 0;

                if (pendingLoads > 0 || pendingReleases > 0)
                    DebugLog($"Shutdown: completing {pendingLoads} pending loads and {pendingReleases} pending releases");

                // Complete all pending operations before clearing to avoid orphaned awaiters and event subscribers
                if (s_PendingLoadOperations != null)
                {
                    foreach (var loadOp in s_PendingLoadOperations.Values)
                    {
                        if (!loadOp.isDone)
                        {
                            // Complete with failure, don't log errors during shutdown
                            loadOp.Complete(default, false);
                        }
                    }
                    s_PendingLoadOperations.Clear();
                }

                if (s_PendingReleaseOperations != null)
                {
                    foreach (var releaseOp in s_PendingReleaseOperations.Values)
                    {
                        if (!releaseOp.isDone)
                        {
                            releaseOp.Complete();
                        }
                    }
                    s_PendingReleaseOperations.Clear();
                }

                s_ResultBuffer.Dispose();
                s_ResultBuffer = default;
                s_Initialized = false;

                DebugLog("ContentLoadingSystem shutdown complete");
            }
        }
    }
}
