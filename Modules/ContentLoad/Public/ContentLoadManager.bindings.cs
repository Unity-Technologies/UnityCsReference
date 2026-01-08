// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;
using System.Runtime.InteropServices;

#pragma warning disable CS1574 // XML comment with cref attribute to types in UnityEditor namespace

namespace UnityEngine.Loading
{
    /// <summary>
    /// Represents a handle to a content directory registered via the ContentLoadManager. This struct is used to encapsulate
    /// information about the content directory, such as its path, and is returned from the RegisterContentDirectory operation
    /// in ContentLoadManager.
    /// </summary>
    /// <seealso cref="ContentLoadManager.RegisterContentDirectory"/>
    [StructLayout(LayoutKind.Sequential)]
    /*UCBP-PUBLIC*/ internal struct ContentDirectoryHandle
    {
        internal UInt64 m_Handle;

        /// <summary> Returns true if the handle is valid.</summary>
        /// <value>
        /// A bool representing that the content directory handle is valid.
        /// </value>
        public bool IsValid { get { return m_Handle != 0; } }
    }

    /// <summary>
    /// The ContentLoadManager offers APIs for accessing content that has been built. It is primarily used to register
    /// content directories and access root content.
    /// </summary>
    /// <remarks>
    /// In the Editor this is normally not used, because the content is available directly in the project using
    /// <see cref="UnityEditor.AssetDatabase"/> and <see cref="UnityEditor.SceneManagement.EditorSceneManager"/> calls.
    /// However, it can be useful in Editor play mode to run the same loading case as the runtime and to try out the
    /// output of your Content Directory build, directly inside the Editor.
    /// </remarks>
    /// <seealso cref="Loadable{T}"/>
    /// <seealso cref="LoadableScene"/>
    /// <seealso cref="UnityEditor.BuildPipeline.BuildContentDirectory"/>
    [NativeHeader("Modules/ContentLoad/Public/ContentLoadManager.bindings.h")]
    [StaticAccessor("ContentLoad", StaticAccessorType.DoubleColon)]
    /*UCBP-PUBLIC*/ internal static partial class ContentLoadManager
    {
        /// <summary>
        /// Event that is invoked on the main thread just when a content directory is registered.
        /// The path of the content directory is passed to the event.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<ContentDirectoryHandle> OnRegisterContentDirectory;

        /// <summary>
        /// Event that is invoked on the main thread just before a content directory is unregistered.
        /// The path of the registered content directory is passed to the event.
        /// </summary>
        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<ContentDirectoryHandle> OnUnregisterContentDirectory;

        /// <summary>
        /// Add the built-content in a directory to the ContentLoadManager. This makes it possible to load the contained Scenes
        /// and Assets.
        /// </summary>
        /// <remarks>
        /// In the runtime this is required when <see cref="BuildPipeline.BuildContentDirectory"/> has been used to build and
        /// distribute additional content.
        ///
        /// This can be called multiple times, with different paths, to expose the contents of additional content directories to
        /// the editor/runtime. For a clean shutdown each call to RegisterContentDirectory should be matched with a call to
        /// UnregisterContentDirectory.
        ///
        /// This method logs an error and returns an invalid handle if the specified path has already registered.
        /// </remarks>
        /// <param name="contentDirectoryPath">
        /// A local path pointing to a directory that contains the output from a call to
        /// <see cref="BuildPipeline.BuildContentDirectory"/>.
        /// </param>
        public static ContentDirectoryHandle RegisterContentDirectory(string contentDirectoryPath)
        {
            var handle = RegisterInternal(contentDirectoryPath, true);
            if (!handle.IsValid)
                throw new Exception($"Failed to register content directory at path {contentDirectoryPath}. See log for details.");
            OnRegisterContentDirectory?.Invoke(handle);
            return handle;
        }

        // Temporary function that we will migrate all tests to. See https://jira.unity3d.com/browse/CBD-1711
        internal static ContentDirectoryHandle RegisterContentDirectory_DontLoadRoots(string contentDirectoryPath)
        {
            var handle = RegisterInternal(contentDirectoryPath, false);
            if (!handle.IsValid)
                throw new Exception($"Failed to register content directory at path {contentDirectoryPath}. See log for details.");
            OnRegisterContentDirectory?.Invoke(handle);
            return handle;
        }

        [FreeFunction("ContentLoad::RegisterContentDirectory")]
        static extern ContentDirectoryHandle RegisterInternal(string contentDirectoryPath, bool forceLoadRoots);

        /// <summary>
        /// Remove access to content that had been loaded from a content directory.
        /// </summary>
        /// <remarks>
        /// All Loadable&lt;T&gt; referencing the content of a directory must be explicitly released prior to calling this.
        /// Similarly all LoadableScene must be Unloaded.
        /// </remarks>
        /// <param name="contentDirectory">
        /// Content directory handle to unregister
        /// </param>
        public static void UnregisterContentDirectory(ContentDirectoryHandle contentDirectory)
        {
            if (!contentDirectory.IsValid)
            {
                Debug.LogError("Cannot unregister invalid content directory handle");
                return;
            }

            OnUnregisterContentDirectory?.Invoke(contentDirectory);
            UnregisterInternal(contentDirectory);
        }

        [FreeFunction("ContentLoad::UnregisterContentDirectory")]
        static extern void UnregisterInternal(ContentDirectoryHandle handle);

        /// <summary>
        /// Retrieve a Loadable by its key.
        /// </summary>
        /// <remarks>
        /// This method is a somewhat lower-level equivalent to <see cref="RootAssetManager.GetRootAssetByKey"/>.
        ///
        /// Certain loadables can be loaded by string, e.g. the root assets. Typically the string would be the project-relative
        /// path of the Asset, but the concept is more generalized at this layer to permit "addressing" an Asset by any string,
        /// so long as it is unique within a specific Content Directory.
        ///
        /// In the runtime any roots available from registered Content Directories, can be retrieved through this API. When the
        /// same assetKey is present in multiple registered Content Directories then the one from the most recently registered
        /// Content Directory is used.
        ///
        /// In the Editor the only content that can be retrieved through this API is previously built content that has been
        /// explicitly registered (by calling <see cref="ContentLoadManager.RegisterContentDirectory"/>). Certain
        /// platform-specific formats or features may not be compatible with the Editor.
        ///
        /// </remarks>
        /// <typeparam name="T">
        /// Expected type for the referenced object within the Asset. For root assets, this must be a class derived from
        /// <see cref="ScriptableObject"/>.
        /// </typeparam>
        /// <param name="assetKey">
        /// String that uniquely identifies the Asset.
        /// </param>
        /// <returns>
        /// The returned object is a Loadable that is initialized to reference the correct built content. It is up to the caller
        /// to actually invoke a loading method. If the requested assetKey is not present in any registered Content Directory
        /// then the returned Loadable will be invalid.
        /// </returns>
        public static Loadable<T> GetLoadableByKey<T>(string assetKey) where T : UnityEngine.Object
        {
            LoadableReference weakRef = GetLoadableReferenceByKey(assetKey);
            return new Loadable<T>(weakRef);
        }

        /// <summary>
        /// Returns all the Assets that can be loaded by string from any of the loaded builds, e.g. the roots.
        /// </summary>
        /// <remarks>
        /// The returned strings can be converted to a Loadable using <see cref="ContentLoadManager.GetLoadableByKey"/>. Typically
        /// the string is the project-relative path of the Asset (as it was located when the build was performed).
        /// </remarks>
        /// <returns>List of all available roots.</returns>
        public static extern string[] GetLoadableKeys();

        public static extern Object[] GetRootAssets();
        extern private static Object[] GetRootAssetsFromRegisteredDirectory(ContentDirectoryHandle contentDirectory);
        public static Object[] GetRootAssets(ContentDirectoryHandle contentDirectory)
        {
            return GetRootAssetsFromRegisteredDirectory(contentDirectory);
        }

        /// <summary>
        /// Retrieves an ordered list of content directories.
        /// </summary>
        /// <returns>
        /// An array of ContentDirectoryHandle, where each handle represents a registered content directory. The content
        /// directories are sequenced according to their order of registration.
        /// </returns>
        [FreeFunction("ContentLoad::GetContentDirectories")]
        public static extern ContentDirectoryHandle[] GetContentDirectories();

        /// <summary>
        /// Retrieves a list of loadable keys from the specified registered content directory.
        /// </summary>
        /// <param name="contentDirectory">
        /// The registered content directory handle from which to retrieve loadable keys.
        /// </param>
        /// <returns>
        /// An array of strings listing the loadable keys available in the specified directory. These reference Assets in the
        /// Content Directory that can be loaded using the specific string. Typically these are Root Assets, and the strings are
        /// the project-relative paths of the Asset at the time it was built.
        /// </returns>
        public static string[] GetLoadableKeys(ContentDirectoryHandle contentDirectory)
        {
            return GetLoadableKeysFromRegisteredDirectory(contentDirectory);
        }
        extern private static string[] GetLoadableKeysFromRegisteredDirectory(ContentDirectoryHandle contentDirectory);

        /// <summary>
        /// Returns all the scene paths that can be loaded from any of the registered content directories.
        /// </summary>
        /// <remarks>
        /// The returned strings represent the project-relative paths of scenes available in registered content directories.
        /// These paths can be used with <see cref="ContentLoadManager.GetLoadableSceneByPath"/> to obtain a LoadableScene.
        /// </remarks>
        /// <returns>Array of scene paths available across all registered content directories.</returns>
        public static extern string[] GetLoadableScenePaths();

        /// <summary>
        /// Retrieves a list of loadable scene paths from the specified registered content directory.
        /// </summary>
        /// <param name="contentDirectory">
        /// The registered content directory handle from which to retrieve loadable scene paths.
        /// </param>
        /// <returns>
        /// An array of strings listing the scene paths available in the specified directory.
        /// </returns>
        public static string[] GetLoadableScenePaths(ContentDirectoryHandle contentDirectory)
        {
            return GetLoadableScenePathsFromRegisteredDirectory(contentDirectory);
        }
        extern private static string[] GetLoadableScenePathsFromRegisteredDirectory(ContentDirectoryHandle contentDirectory);

        /// <summary>
        /// This method processes a stack of content directories, which may contain duplicate keys. It returns all distinct root
        /// asset keys and the associated Loadable. In case of multiple content directories having the Loadables with the same key,
        /// the method prioritizes the content directory that is highest in the stack.
        /// </summary>
        /// <returns>A map of Loadable Key to Loadable.</returns>
        internal static Dictionary<string, Loadable<Object>> GetUniqueLoadablesWithKeys()
        {
            string[] rootAssetKeys = ContentLoadManager.GetLoadableKeys();

            Dictionary<string, Loadable<UnityEngine.Object>> uniqueRootAssetLoadables = new Dictionary<string, Loadable<Object>>();
            foreach (var key in rootAssetKeys)
            {
                // Currently contents of Resources folder are automatically built as roots. This enables support for legacy behaviors
                // and APIs, like Resources.Load(). But these should not be exposed through the RootAssetManager, especially because
                // that forced them into memory. CBD-12
                // Skip root assets that are in the ProjectSettings folder, as these are global game managers and are not meant to be
                // loaded as root assets.
                if (!uniqueRootAssetLoadables.ContainsKey(key) && !Regex.IsMatch(key, @"(?:/|\\)Resources(?:/|\\|$)") && !Regex.IsMatch(key, @"^ProjectSettings(?:/|\\)"))
                {
                    var loadable = ContentLoadManager.GetLoadableByKey<UnityEngine.Object>(key);

                    if (!loadable.isValid)
                    {
                        Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null,
                            $"ContentLoadManager: GetLoadablesWithKeys No valid loadable found for root asset with key:{key}");
                        continue;
                    }
                    uniqueRootAssetLoadables[key] = loadable;
                }
            }

            return uniqueRootAssetLoadables;
        }

        /// <summary>
        /// Retrieve a LoadableScene from built content.
        /// </summary>
        /// <remarks>
        /// Similar to <see cref="ContentLoadManager.GetLoadableByKey"/>, this API makes it possible to retrieve an object that can
        /// be used to load a Scene, based on its key. See <see cref="SceneManager.LoadSceneAsync(LoadableScene, LoadSceneParameters)"/>.
        ///
        /// In the runtime any Scene inside content that has been registered (through
        /// <see cref="ContentLoadManager.RegisterContentDirectory"/>), can be retrieved through this API.
        ///
        /// In the Editor Playmode it is possible to use this API to retrieve scenes that were built into a content directory,
        /// provided that <see cref="ContentLoadManager.RegisterContentDirectory"/> has been called.
        /// </remarks>
        /// <param name="path">
        /// The key would typically be the project-relative Asset path.
        /// </param>
        /// <returns>
        /// The returned object can be used to Load the scene.
        /// </returns>
        public static extern LoadableScene GetLoadableSceneByPath(string path);
        internal static extern LoadableReference GetLoadableReferenceByKey(string assetKey);

        // Exposed state of the ContentLoadManager, for testing and debugging
        internal static extern int GetRefCountByKey(string key);

        // For test and internal usage
        // This method loads the BuildManifest, which describe the content available inside a Content Directory.
        [ThreadSafe]
        internal static extern BuildManifest LoadBuildManifest(string path);
    }
}
