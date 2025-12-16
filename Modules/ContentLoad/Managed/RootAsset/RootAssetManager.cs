// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Loading;


namespace UnityEngine
{
    /// <summary>
    /// RootAssetManager is a utility class that provides access to root assets in the project.
    /// </summary>
    /// <remarks>
    /// It is available both in the Editor and Runtime. In the runtime the available objects are based on which roots have been
    /// included when performing the player build.
    /// </remarks>
    /*UCBP-PUBLIC*/ internal sealed partial class RootAssetManager
    {
        private static RootAssetProviderHandler providerHandler
        {
            get
            {
                if (m_ProviderHandler == null)
                {
                    m_ProviderHandler = new RootAssetProviderHandler();
                }
                return m_ProviderHandler;
            }
        }

        [AutoStaticsCleanupOnCodeReload]
        private static RootAssetProviderHandler m_ProviderHandler;

        /// <summary>
        /// Retrieve a root asset by type. This is suitable for use with root assets that are singletons within a project or
        /// build.
        /// </summary>
        /// <typeparam name="T">
        /// Requested type. Typically this would be a class derived from ScriptableObject.
        /// </typeparam>
        /// <returns>
        /// If a root asset exists of the requested type then it is returned. Otherwise it returns null.
        /// </returns>
        /// <seealso cref="ScriptableObject"/>
        public static T GetRootAsset<T>() where T : Object
        {
            if(Application.isPlaying)
            {
                return providerHandler.GetRootAssetByType<T>();
            }
            throw new InvalidOperationException("GetRootAsset should not be called from editor code. Use AssetDatabase APIs instead.");
        }

        /// <summary>
        /// Retrieves a root asset by its key.
        /// </summary>
        /// <remarks>
        /// By default the key is the project-relative path of the asset in the Unity project.
        /// </remarks>
        /// <typeparam name="T">
        /// Requested type. Typically this would be a class derived from <see cref="ScriptableObject"/>.
        /// </typeparam>
        /// <param name="key">The case sensitive key of the asset.</param>
        /// <returns>
        /// Will return the RootAsset with the specified key. Will return null if there is not root asset with that key, or if
        /// the type of the asset stored at that path does not match the Type passed as T
        /// </returns>
        public static T GetRootAssetByKey<T>(string key) where T : Object
        {
            if (Application.isPlaying)
            {
                return providerHandler.GetRootAssetByKey<T>(key);
            }
            throw new InvalidOperationException("GetRootAsset should not be called from editor code. Use AssetDatabase APIs instead.");
        }

        /// <summary>
        /// Retrieve a root asset filtered by type and name.
        /// </summary>
        /// <typeparam name="T">
        /// Requested type. Typically this would be a class derived from ScriptableObject.
        /// </typeparam>
        /// <param name="name"><see cref="Object.name"/></param>
        /// <returns>
        /// If a root asset exists that matches the requested type and name then it is returned. Otherwise it returns null.
        /// </returns>
        /// <seealso cref="ScriptableObject"/>
        public static T GetRootAsset<T>(string name) where T : Object
        {
            if(Application.isPlaying)
            {
                return providerHandler.GetRootAssetByName<T>(name);
            }
            throw new InvalidOperationException("GetRootAsset should not be called from editor code. Use AssetDatabase APIs instead.");
        }

        /// <summary>
        /// Retrieve all root assets that are instances of a certain type.
        /// </summary>
        /// <remarks>
        /// This is useful in cases where multiple instances of the same type may exist as root assets in a project or build.
        /// </remarks>
        /// <typeparam name="T">
        /// Requested type. Typically this would be a class derived from ScriptableObject.
        /// </typeparam>
        /// <seealso cref="ScriptableObject"/>
        public static List<T> GetRootAssets<T>() where T : Object
        {
            if (Application.isPlaying)
            {
                return providerHandler.GetAllRootAssetsOfType<T>();
            }
            throw new InvalidOperationException("GetRootAsset should not be called from editor code. Use AssetDatabase APIs instead.");
        }

        // Expose these methods to the ContentPipelineModule for use in the editor
        [VisibleToOtherModules("UnityEditor.ContentPipelineModule")]
        internal static void AddRootAssetProvider(IRootAssetProvider provider)
        {
            providerHandler.AddRootAssetProvider(provider);
        }
    }
}
