// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine.Bindings;
using UnityEngine.Loading;
using UnityEngine.Serialization;

namespace UnityEngine
{
    /// <summary>
    /// Loadable can be used as a field type on a <see cref="MonoBehaviour"/> or <see cref="ScriptableObject"/> to reference an
    /// Asset that is loaded on-demand. Loadable works both in the Editor and in the built player.  The presence of Loadables
    /// helps determine what content from a project is brought into a <see cref="BuildPipeline.BuildContentDirectory"/> build.
    /// </summary>
    /// <remarks>
    /// To populate a Loadable in the Editor use <see cref="UnityEditor.LoadableReferenceEditorUtility"/> to create a
    /// LoadableReference, then construct a Loadable from it. These are saved as part of the scripting object's serialized state
    /// and then can be accessed in the Player to load the content.
    /// <see cref="Loading.ContentLoadManager.GetLoadableByKey"/> is another way to create a Loadable in the Player.
    ///
    /// Typically the Main Object of the Asset is referenced, but it is possible to reference a Sub-Asset (e.g. a Mesh or Sprite),
    /// or even a <see cref="Component"/>. The specific Object that is referenced can influence how much of an Asset makes it
    /// into the build. For example, if just a single Mesh inside an FBX is referenced by a Loadable then other Meshes and the
    /// GameObject hierarchy will not be included in the build.
    /// 
    /// Player and AssetBundle builds do not pull assets referenced by Loadable into the build.  
    /// But a Loadable field in built player content can be used to load the asset at runtime if it is available inside a
    /// registered ContentDirectory.
    /// 
    /// </remarks>
    /// <typeparam name="T">
    /// Type of object that is referenced, for example <see cref="Shader"/>, <see cref="GameObject"/>, or a C# class derived from
    /// <see cref="ScriptableObject"/>.
    /// </typeparam>
    /// <seealso cref="LoadableScene"/>
    /// <seealso cref="UnityEditor.BuildPipeline.BuildContentDirectory"/>
    /*UCBP-REMOVE*/[VisibleToOtherModules]
    [Serializable, StructLayout(LayoutKind.Sequential)]
    /*UCBP-PUBLIC*/ internal sealed class Loadable<T> where T : Object
    {
        [SerializeField]
        LoadableReference m_LoadableRef;

        /// <summary>
        /// Constructs a Loadable from a LoadableReference.
        /// </summary>
        /// <remarks>
        /// This constructor is typically used when creating a Loadable in the Editor from a reference obtained via
        /// <see cref="UnityEditor.LoadableReferenceEditorUtility"/>.
        /// </remarks>
        /// <param name="loadableRef">
        /// The LoadableReference that identifies the asset to be loaded on-demand.
        /// </param>
        public Loadable(in LoadableReference loadableRef)
        {
            m_LoadableRef = loadableRef;
        }

        /// <summary>
        /// Creates a clone of the Loadable object.
        /// </summary>
        /// <remarks>
        /// This method can be useful to share a reference to a Loadable between multiple components, without requiring central
        /// tracking of its lifetime. <see cref="Load"/> and <see cref="Release"/> can be called independently on each clone of the
        /// Loadable while still ensuring the referenced object is only loaded once and then unloaded when the final reference is
        /// released.
        ///
        /// It is possible to clone an object that is already loaded, but not while a <see cref="LoadAsync"/> operation is
        /// actively in progress.
        /// </remarks>
        /// <returns>A new instance of the Loadable object with the same asset reference.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if a <see cref="LoadAsync"/> operation is in progress.
        /// </exception>
        public Loadable<T> Clone()
        {
            if (m_LoadOperation != null)
                throw new InvalidOperationException("Cannot clone a Loadable while a LoadAsync operation is in progress.");

            Loadable<T> clone = new Loadable<T>(m_LoadableRef);

            if (m_Target != null)
            {
                // This should be a low cost operation because the object is already loaded.
                // It will increase the internal ref-count so that the clone can be independently released.
                clone.Load();
            }
            return clone;
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// This is useful for advanced scenarios such as comparing references or passing to low-level APIs.
        /// </remarks>
        /// <seealso cref="LoadableReference"/>
        public LoadableReference loadableReference => m_LoadableRef;

        /// <summary>
        /// Gets the loaded target object.
        /// </summary>
        /// <remarks>
        /// If a LoadAsync operation was used, this property will wait for the load to complete before returning the
        /// target object.
        /// </remarks>
        public T target
        {
            get
            {
                // Will wait for the load to complete if LoadAsync was used
                if (m_Target == null && m_LoadOperation != null)
                    m_Target = m_LoadOperation.asset as T;

                return m_Target;
            }
        }

        /// <summary>
        /// Return a string representation of the reference information inside the Loadable.
        /// </summary>
        /// <returns>
        /// A string containing the fileID, GUID, type, and object ID hash of the referenced asset.
        /// Returns "{ Invalid }" if the Loadable is not valid.
        /// </returns>
        public override string ToString()
        {
            return m_LoadableRef.ToString();
        }

        private T m_Target;
        private ContentLoadOperation m_LoadOperation;

        /// <summary>
        /// Similar to a null check, this returns true when the Loadable is initialized with a reference to an Asset, but it
        /// doesn't guarantee a valid type T
        /// </summary>
        public bool isValid => m_LoadableRef.isValid;

        /// <summary>
        /// Returns true if the object referenced by the Loadable has been loaded.
        /// </summary>
        public bool isLoaded
        {
            get
            {
                return m_Target != null;
            }
        }

        /// <summary>
        /// Load the Asset from the output of a build.
        /// </summary>
        /// <remarks>
        /// This can be called in both Runtime and Editor. When the loaded Asset is no longer needed, call <see cref="Release"/>
        /// to decrement the reference count and allow Unity to unload the asset and reclaim memory. If the asset is already
        /// loaded, this returns the existing reference without reloading and increments the reference count.
        /// </remarks>
        /// <returns>
        /// The object inside the loaded asset. Null is returned if the load fails, if the Loadable is invalid, or if the object
        /// is not compatible with T.
        /// </returns>
        /// <seealso cref="UnityEditor.BuildPipeline.BuildContentDirectory"/>
        /// <seealso cref="ContentLoadInterface.LoadContentFileAsync"/>
        /// <seealso cref="Release"/>
        public T Load()
        {
            if (!isValid)
            {
                Debug.LogError($"Loadable<{typeof(T)}> has no asset set.");
                return null;
            }

            if (m_LoadOperation != null)
            {
                Debug.LogError($"Loadable<{typeof(T)}> {nameof(LoadAsync)} already in progress.");
                return null;
            }

            if (m_Target == null)
            {
                Object obj = LoadableManager.LoadObjectFromRef(m_LoadableRef);

                m_Target = obj as T;

                if (m_Target == null && obj != null)
                {
                    Debug.LogError($"The Loadable cannot cast the loaded object to <{typeof(T)}>. The loaded object has type {obj.GetType().ToString()}");
                    LoadableManager.ReleaseObjectFromRef(m_LoadableRef);
                }
            }

            return m_Target;
        }

        /// <summary>
        /// Launch an asynchronous load for the referenced Asset.
        /// </summary>
        /// <param name="ct">Optional CancellationToken to enable cancellation during the load.</param>
        /// <returns><see cref="UnityEngine.Awaitable"/> object.</returns>
        public async Awaitable LoadAsync(CancellationToken ct = default)
        {
            if (!isValid)
            {
                Debug.LogError($"Loadable<{typeof(T)}> has no asset set.");
                return;
            }

            if (m_LoadOperation != null)
            {
                Debug.LogError($"Loadable<{typeof(T)}> {nameof(LoadAsync)} already in progress.");
                return;
            }

            if (m_Target != null)
                return;

            m_LoadOperation = LoadableManager.LoadObjectAsyncFromRef(m_LoadableRef);

            if (m_LoadOperation == null)
            {
                Debug.LogError($"Loadable<{typeof(T)}> asset with GUID '{m_LoadableRef.m_GUID}' is unknown.");
                return;
            }

            await Awaitable.FromAsyncOperation(m_LoadOperation, ct);

            if (ct.IsCancellationRequested)
            {
                m_LoadOperation = null;
                return;
            }

            m_Target = m_LoadOperation?.asset as T;

            if (m_Target == null && m_LoadOperation?.asset != null)
            {
                LoadableManager.ReleaseObjectFromRef(m_LoadableRef);
            }

            m_LoadOperation = null;
        }

        /// <summary>
        /// Decrement the reference count to the Asset that was previously loaded by a call to Load().
        /// </summary>
        /// <remarks>
        /// Calling Release() permits Unity to unload files, and reclaim memory, once all reference counts reach zero. The
        /// number of times this is called should exactly match the number of times Load() was called.
        /// </remarks>
        public void Release()
        {
            if (m_LoadOperation == null && (m_Target == null || !isValid))
                return;

            LoadableManager.ReleaseObjectFromRef(m_LoadableRef);

            m_LoadOperation = null;
            m_Target = null;
        }

        /// <summary>
        /// Returns true if the two loadables have the same asset reference.
        /// </summary>
        /// <remarks>
        /// This method compares the underlying LoadableReference values to determine if two Loadable instances refer to the
        /// same asset. Note that this performs an exact comparison of the reference data, so two Loadables that point to the
        /// same asset but were created differently (e.g., one with full reference data and one with only an object hash) will
        /// return false.
        /// </remarks>
        /// <param name="rhs">The other Loadable to compare against.</param>
        /// <returns>
        /// True if both Loadables reference the same asset with identical reference data; otherwise, false.
        /// </returns>
        public bool IsReferenceEqual(Loadable<T> rhs)
        {
            return m_LoadableRef == rhs.m_LoadableRef;
        }
    }
}
