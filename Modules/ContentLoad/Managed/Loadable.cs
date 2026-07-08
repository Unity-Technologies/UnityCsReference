// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine;

#pragma warning disable CS1574 // XML comment with cref attribute to types in UnityEditor namespace

namespace Unity.Loading
{
    /// <summary>
    /// Describes the current loading phase of a <see cref="Loadable{T}"/> after <see cref="Loadable{T}.Load"/> or <see cref="Loadable{T}.LoadAsync"/> is used.
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
    /// <remarks>
    /// Add a <see cref="Loadable{T}"/> field to a <see cref="ScriptableObject"/> or <see cref="MonoBehaviour"/> to reference an
    /// asset without loading it immediately. The reference is stored as a <see cref="LoadableObjectId"/>, which you create in the
    /// Editor with <see cref="UnityEditor.LoadableObjectIdEditorUtility"/>. When the containing object is built into a content
    /// directory with <see cref="UnityEditor.BuildPipeline.BuildContentDirectory"/>, the referenced asset and its dependencies are
    /// pulled into the build output.
    ///
    /// At runtime, call <see cref="Load"/> or <see cref="LoadAsync"/> to load the asset on demand, read it from <see cref="Target"/>,
    /// and call <see cref="Release"/> when it is no longer needed. The asset must belong to a content directory that has been
    /// registered with <see cref="ContentLoadManager.RegisterContentDirectory(string)"/>.
    ///
    /// In Play mode you can load built content with much the same code and behavior as in the Player. Register the content
    /// directory with <see cref="ContentLoadManager.RegisterContentDirectory(string)"/>, get its root assets with
    /// <see cref="ContentLoadManager.GetRootAssets{T}()"/>, then read the <see cref="Loadable{T}"/> fields on those root assets or
    /// on any asset or scene they reference. A <see cref="Loadable{T}"/> reached from built content always loads its asset from that
    /// built content, never from the project.
    ///
    /// In Play mode you can also load the live project version of an asset, such as a scene loaded by path with
    /// <see cref="UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(string)"/> or a <see cref="ScriptableObject"/> loaded
    /// through <see cref="UnityEditor.AssetDatabase"/>. A <see cref="Loadable{T}"/> field on an asset loaded this way always resolves
    /// to the latest project version of the referenced asset through the <see cref="UnityEditor.AssetDatabase"/>, even when a
    /// registered content directory contains a built version of that asset.
    ///
    /// Content loaded from the <see cref="UnityEditor.AssetDatabase"/> in this way is not reference counted, so <see cref="Release"/>
    /// does not unload it. It unloads through the same garbage collection that handles other AssetDatabase content in the Editor. The
    /// asset unloads when nothing references it and a collection runs. Call <see cref="Resources.UnloadUnusedAssets"/> to force a collection.
    ///
    /// A <see cref="Loadable{T}"/> field is only supported in content built with <see cref="UnityEditor.BuildPipeline.BuildContentDirectory"/>.
    /// If a Loadable field is found in serialized data during a Player or AssetBundle build, the underlying reference is set to null
    /// in the build output and an error is logged. Suppress this error with <see cref="UnityEditor.BuildOptions.SuppressLoadableErrors"/>
    /// for Player builds or <see cref="UnityEditor.BuildAssetBundleOptions.SuppressLoadableErrors"/> for AssetBundle builds.
    /// </remarks>
    /// <typeparam name="T">
    /// Concrete Unity <see cref="UnityEngine.Object"/> type referenced by this field (for example <see cref="GameObject"/> for a prefab or a custom <see cref="ScriptableObject"/> type).
    /// </typeparam>
    /// <example>
    /// <code source="../../ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/Loadable_LoadAndRelease.cs"/>
    /// </example>
    /// <seealso cref="LoadableObjectId"/>
    /// <seealso cref="UnityEditor.LoadableObjectIdEditorUtility"/>
    /// <seealso cref="UnityEditor.BuildPipeline.BuildContentDirectory"/>
    /// <seealso cref="ContentLoadManager"/>
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
        /// <remarks>
        /// Load does not throw when the asset cannot be returned. Instead it returns <see langword="null"/>, logs an error, and
        /// sets <see cref="Status"/> to <see cref="LoadableStatus.Failed"/>. This happens when the asset is not present in any
        /// registered content directory, when it has already been released, or when the loaded object cannot be cast to
        /// <typeparamref name="T"/> because the <see cref="LoadableObjectId"/> points at an object of an incompatible type.
        /// </remarks>
        /// <returns>The loaded object, or <see langword="null"/> if loading failed.</returns>
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
