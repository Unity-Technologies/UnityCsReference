// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine;

namespace Unity.Loading
{
    /// <summary>
    /// Describes the current asynchronous loading phase of a `Loadable{T}` after <see cref="Loadable{T}.Load"/> or <see cref="Loadable{T}.LoadAsync"/> is used.
    /// </summary>
    /// <remarks>
    /// Query <see cref="Loadable{T}.Status"/> to drive UI or to assert that a load finished before reading
    /// <see cref="Loadable{T}.Target"/>. <see cref="LoadableStatus.Failed"/> indicates the asynchronous load completed without a usable object. This often occurs due to missing built content.
    /// </remarks>
    /// <example>
    /// <code source="../../ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/Loadable_LoadAndRelease.cs"/>
    /// </example>
    public enum LoadableStatus
    {
        /// <summary>
        /// The Loadable has not begun loading.
        /// </summary>
        None,
        /// <summary>
        /// The loading operation has begun.
        /// </summary>
        Loading,
        /// <summary>
        /// The loading operation completed successfully.
        /// </summary>
        Loaded,
        /// <summary>
        /// The loading operation completed but failed.
        /// </summary>
        Failed
    };

    /// <summary>
    /// Serialized reference that loads a specific <typeparamref name="T"/> asset from registered built content on demand instead of pulling it in with direct references.
    /// </summary>
    /// <typeparam name="T">
    /// Concrete Unity <see cref="UnityEngine.Object"/> type referenced by this field (for example <see cref="GameObject"/> for a prefab or a custom <see cref="ScriptableObject"/> type).
    /// </typeparam>
    /// <example>
    /// <code source="../../ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/Loadable_LoadAndRelease.cs"/>
    /// </example>
    [Serializable]
    public sealed class Loadable<T> where T : UnityEngine.Object
    {
        [SerializeField]
        private LoadableObjectId m_LoadableObjectId;
        private ObjectLoadOperation<T> m_loadOperation;

        /// <summary>
        /// Creates a new Loadable with the specified loadable object id.
        /// </summary>
        /// <param name="id">The loadable object id identifying the asset to load.</param>
        public Loadable(in LoadableObjectId id)
        {
            m_LoadableObjectId = id;
        }

        /// <summary>
        /// The underlying loadable object id.
        /// </summary>
        public LoadableObjectId LoadableObjectId => m_LoadableObjectId;

        /// <summary>
        /// The current status of the loading operation.
        /// </summary>
        public LoadableStatus Status
        {
            get
            {
                if(m_loadOperation == null) return LoadableStatus.None;
                if(!m_loadOperation.isDone) return LoadableStatus.Loading;
                return m_loadOperation.success ? LoadableStatus.Loaded : LoadableStatus.Failed;
            }
        }

        /// <summary>
        /// The result of the load operation. If the operation is not complete or has failed, this returns null.
        /// Use <see cref="Load"/> to force the operation to complete synchronously.
        /// </summary>
        public T Target => m_loadOperation?.result;


        /// <summary>
        /// Returns a string representation of the loadable object id.
        /// </summary>
        /// <returns>A string representation of the loadable object id.</returns>
        public override string ToString() => $"{nameof(Loadable<T>)}<{typeof(T)}> {m_LoadableObjectId}";


        /// <summary>
        /// Loads the object synchronously. Blocks until loading is complete.
        /// </summary>
        /// <returns>The loaded object, or null if loading failed.</returns>
        public T Load()
        {
            LoadAsyncInternal().WaitForCompletion();
            return Target;
        }

        /// <summary>
        /// Loads the object asynchronously. Returns an awaitable that completes when loading is finished.
        /// </summary>
        /// <returns>An awaitable that yields the loaded object, or null if loading failed.</returns>
        public async Awaitable<T> LoadAsync() => await LoadAsyncInternal();

        private ObjectLoadOperation<T> LoadAsyncInternal()
        {
            if (m_loadOperation == null)
                m_loadOperation = ContentLoadingSystem.LoadObjectAsync<T>(m_LoadableObjectId, out var _);
            return m_loadOperation;
        }

        /// <summary>
        /// Releases the loaded object.
        /// </summary>
        public void Release()
        {
            if (m_loadOperation != null)
            {
                ContentLoadingSystem.ReleaseObjectAsync(m_loadOperation.operationHandle);
                m_loadOperation = null;
            }
        }

        //kept for tests
        internal void ReleaseSync()
        {
            if (m_loadOperation != null)
            {
                ContentLoadingSystem.ReleaseObjectAsync(m_loadOperation.operationHandle).WaitForCompletion();
                m_loadOperation = null;
            }
        }
    }
}
