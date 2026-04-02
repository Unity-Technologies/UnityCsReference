// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using Unity.Loading;

namespace UnityEngine
{
    /// <summary>
    /// Status for a Loadable
    /// </summary>
    [VisibleToOtherModules]
    /*UCBP-PUBLIC*/ internal enum LoadableStatus
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
    /// Helper class to manage the content loading process.  
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    [VisibleToOtherModules]
    /*UCBP-PUBLIC*/ internal sealed class Loadable<T> where T : UnityEngine.Object
    {
        [SerializeField]
        private LoadableReference m_LoadableRef;
        private ObjectLoadOperation<T> m_loadOperation;

        /// <summary>
        /// Creates a new Loadable with the specified loadable reference.
        /// </summary>
        /// <param name="id">The loadable reference identifying the asset to load.</param>
        public Loadable(in LoadableReference id)
        {
            m_LoadableRef = id;
        }

        /// <summary>
        /// The underlying loadable reference.
        /// </summary>
        public LoadableReference loadableReference => m_LoadableRef;

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
        /// Returns true if the load operation has completed successfully.
        /// </summary>
        public bool isLoaded => Status == LoadableStatus.Loaded;


        /// <summary>
        /// Returns the result of the load operation.  If the operation is not complete or has failed, this will return null.
        /// Use <see cref="Load"/> to force the operation to complete synchronously.
        /// </summary>
        public T target => m_loadOperation?.result;


        /// <summary>
        /// Returns a string representation of the loadable reference.
        /// </summary>
        /// <returns>A string representation of the loadable reference.</returns>
        public override string ToString() => $"{nameof(Loadable<T>)}<{typeof(T)}> {m_LoadableRef}";


        /// <summary>
        /// Loads the object synchronously. Blocks until loading is complete.
        /// </summary>
        /// <returns>The loaded object, or null if loading failed.</returns>
        public T Load()
        {
            LoadAsyncInternal().WaitForCompletion();
            return target;
        }

        /// <summary>
        /// Loads the object asynchronously. Returns an awaitable that completes when loading is finished.
        /// </summary>
        /// <returns>An awaitable that yields the loaded object, or null if loading failed.</returns>
        public async Awaitable<T> LoadAsync() => await LoadAsyncInternal();

        private ObjectLoadOperation<T> LoadAsyncInternal()
        {
            if (m_loadOperation == null)
                m_loadOperation = ContentLoadingSystem.LoadObjectAsync<T>(m_LoadableRef, out var _);
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
