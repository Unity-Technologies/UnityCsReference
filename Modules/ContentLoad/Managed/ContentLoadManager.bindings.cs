// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;
using System.Runtime.InteropServices;
using Object = UnityEngine.Object;

#pragma warning disable CS1574 // XML comment with cref attribute to types in UnityEditor namespace

namespace Unity.Loading
{
    /// <summary>
    /// A handle that references a registered content directory.
    /// </summary>
    /// <remarks>
    /// This handle is returned from <see cref="ContentLoadManager.RegisterContentDirectory(string)"/>.
    /// Keep the handle to call <see cref="ContentLoadManager.UnregisterContentDirectory"/> after all loadables and scenes from that
    /// directory are released. Valid handles expose metadata such as <see cref="BuildName"/> from the content manifest, and can be
    /// used to query root assets from a specific content directory via <see cref="ContentLoadManager.GetRootAssets(ContentDirectoryHandle)"/>.
    /// </remarks>
    /// <example>
    /// <code source="../../ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/ContentDirectoryHandle_RegisterUnregister.cs"/>
    /// </example>
    /// <seealso cref="ContentLoadManager.RegisterContentDirectory(string)"/>
    [StructLayout(LayoutKind.Sequential)]
    public struct ContentDirectoryHandle
    {
        internal UInt64 m_Handle;

        /// <summary>True if the handle is valid.</summary>
        /// <value>
        /// A bool representing that the content directory handle is valid.
        /// </value>
        public readonly bool IsValid => m_Handle != 0;

        /// <summary> The build name of the content directory (e.g. from the Manifest build name).</summary>
        /// <remarks>This is the name set through <see cref="UnityEditor.BuildContentDirectoryParameters.name"/> when the content directory was built.</remarks>
        public string BuildName
        {
            get
            {
                return IsValid ? GetBuildNameFromContentDirectoryHandleInternal(this) : string.Empty;
            }
        }


        [FreeFunction("ContentLoad::GetBuildNameFromContentDirectoryHandle")]
        static extern string GetBuildNameFromContentDirectoryHandleInternal(ContentDirectoryHandle handle);
    }

    /// <summary>
    /// The ContentLoadManager offers APIs for accessing content that has been built. It is primarily used to register
    /// content directories and access root content.
    /// </summary>
    /// <remarks>
    /// In the Editor this is not typically used, because the content is available directly in the project using
    /// <see cref="UnityEditor.AssetDatabase"/> and <see cref="UnityEditor.SceneManagement.EditorSceneManager"/> calls.
    /// However, it can be useful in Editor play mode to run the same loading case as the runtime and to try out the
    /// output of your content directory build, directly inside the Editor.
    /// </remarks>
    /// <example>
    /// <code source="../../ContentBuild/Tests/local.test.build-examples/Editor/ContentLoad/ContentLoadManager_GetRootAssets.cs"/>
    /// </example>
    /// <seealso cref="Loadable{T}"/>
    /// <seealso cref="LoadableSceneId"/>
    /// <seealso cref="UnityEditor.BuildPipeline.BuildContentDirectory"/>
    [NativeHeader("Modules/ContentLoad/Public/ContentLoadManager.bindings.h")]
    [StaticAccessor("ContentLoad", StaticAccessorType.DoubleColon)]
    public static partial class ContentLoadManager
    {
        [AutoStaticsCleanupOnCodeReload]
        static int s_AllowEditModeRegistrationDepth;

        // Test-only override that suppresses the edit-mode registration check. Search for
        // AllowEditModeRegistrationForTesting to find tests still relying on edit-mode registration.
        // This override (and its remaining callers) will be removed once those tests are migrated to
        // Play Mode — see CBD-2011 (https://jira.unity3d.com/browse/CBD-2011).
        internal struct AllowEditModeRegistrationScope : IDisposable
        {
            bool m_Active;

            internal static AllowEditModeRegistrationScope Enter()
            {
                s_AllowEditModeRegistrationDepth++;
                return new AllowEditModeRegistrationScope { m_Active = true };
            }

            public void Dispose()
            {
                if (!m_Active)
                    return;
                m_Active = false;
                // Guard against decrementing below zero if the counter was reset under us
                // (e.g. by AutoStaticsCleanupOnCodeReload during a domain reload mid-scope).
                if (s_AllowEditModeRegistrationDepth > 0)
                    s_AllowEditModeRegistrationDepth--;
            }
        }

        internal static AllowEditModeRegistrationScope AllowEditModeRegistrationForTesting()
            => AllowEditModeRegistrationScope.Enter();

        static void ThrowIfEditModeRegistrationDisallowed()
        {
            // Use <= 0 so a stale Dispose() after a domain reload (which resets the counter via
            // AutoStaticsCleanupOnCodeReload) cannot leave the guard permanently suppressed.
            if (Application.isEditor && !Application.isPlaying && s_AllowEditModeRegistrationDepth <= 0)
                throw new InvalidOperationException("ContentLoadManager.RegisterContentDirectory is not supported in edit mode. Enter Play Mode before registering a content directory.");
        }

        /// <summary>
        /// Add the built-content in a directory to the ContentLoadManager. This makes it possible to load the contained Scenes
        /// and Assets.
        /// </summary>
        /// <remarks>
        /// At runtime, call this for each content directory that <see cref="BuildPipeline.BuildContentDirectory"/> produced and
        /// that you distribute alongside the Player, so that the scenes and assets it contains can be loaded. You can call it multiple
        /// times with different paths to register several content directories, but each directory is limited to a single build.
        ///
        /// Match every call with a corresponding call to <see cref="UnregisterContentDirectory"/> once all loadables and scenes
        /// from the directory are released.
        ///
        /// In the Editor, registration is only supported in Play mode, where it lets you load and test built content the same way
        /// the runtime does. Outside of Play mode the content is already available through <see cref="UnityEditor.AssetDatabase"/>,
        /// <see cref="UnityEditor.LoadableObjectIdEditorUtility"/> and <see cref="UnityEditor.LoadableSceneIdEditorUtility"/>, so registration is unnecessary and throws <see cref="InvalidOperationException"/>.
        /// </remarks>
        /// <param name="contentDirectoryPath">
        /// A local path pointing to a directory that contains the output from a call to
        /// <see cref="BuildPipeline.BuildContentDirectory"/>.
        /// </param>
        /// <returns>A handle to the registered content directory, used to unregister it and to query its content.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when called from the Editor outside of Play mode, when the path is already registered, or when the content
        /// manifest cannot be loaded.
        /// </exception>
        /// <exception cref="FileNotFoundException">Thrown when the required manifest hash file is not found.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
        public static ContentDirectoryHandle RegisterContentDirectory(string contentDirectoryPath)
        {
            return RegisterContentDirectoryFromPath(contentDirectoryPath);
        }

        /// <summary>
        /// Register a content manifest with the ContentLoadManager. This makes it possible to load the contained Scenes
        /// and Assets.
        /// </summary>
        /// <remarks>
        /// This is a lower-level API that registers only the content manifest. Unlike <see cref="RegisterContentDirectory(string)"/>,
        /// this method does NOT automatically register CAH artifact directories or mount archives. The caller must
        /// register any required CAH artifact directories and mount any archives before calling this method.
        ///
        /// For most use cases, prefer <see cref="RegisterContentDirectory(string)"/> which handles these setup steps automatically.
        ///
        /// For a clean shutdown, each call to RegisterContentDirectory should be matched with a call to UnregisterContentDirectory.
        /// </remarks>
        /// <param name="manifest">The content manifest to register</param>
        /// <returns>Handle to the registered content directory</returns>
        internal static ContentDirectoryHandle RegisterContentDirectory(ContentManifest manifest)
        {
            ThrowIfEditModeRegistrationDisallowed();
            if (Application.isEditor && !manifest.BuiltWithTypeTrees)
            {
                throw new InvalidOperationException(
                    "Cannot register a content directory in the Editor whose content was built without type trees.");
            }

            var handle = RegisterInternalFromContentManifest(manifest);
            if (!handle.IsValid)
                throw new InvalidOperationException("Failed to register content directory from manifest");

            return handle;
        }

        [FreeFunction("ContentLoad::RegisterContentDirectoryFromContentManifest")]
        static extern ContentDirectoryHandle RegisterInternalFromContentManifest([NotNull] ContentManifest contentManifest);

        /// <summary>
        /// Remove access to content that had been loaded from a content directory.
        /// </summary>
        /// <remarks>
        /// Before calling this, release everything that was loaded from the content directory. Release each
        /// <see cref="Loadable{T}"/> with <see cref="Loadable{T}.Release"/>, and unload each scene that was loaded from a
        /// <see cref="LoadableSceneId"/>. Use <see cref="SceneManager.GetSceneByLoadableSceneId(LoadableSceneId)"/> to get the
        /// loaded scene and unload it through the <see cref="SceneManager"/> API.
        ///
        /// For content directories registered in Play mode, call this before exiting Play mode.
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

            UnregisterInternal(contentDirectory);
            CleanupTrackedRegistration(contentDirectory);
        }

        [FreeFunction("ContentLoad::UnregisterContentDirectory")]
        internal static extern void UnregisterInternal(ContentDirectoryHandle handle);

        public static extern Object[] GetRootAssets();
        extern private static Object[] GetRootAssetsFromRegisteredDirectory(ContentDirectoryHandle contentDirectory);
        public static Object[] GetRootAssets(ContentDirectoryHandle contentDirectory)
        {
            return GetRootAssetsFromRegisteredDirectory(contentDirectory);
        }

        /// <summary>
        /// Retrieve all root assets of a specific type from all registered content directories.
        /// </summary>
        /// <typeparam name="T">The type to filter root assets by.</typeparam>
        /// <returns>An array of root assets that match the specified type.</returns>
        public static T[] GetRootAssets<T>() where T : Object
            => FilterByType<T>(GetRootAssets());

        /// <summary>
        /// Retrieve all root assets of a specific type from the specified content directory.
        /// </summary>
        /// <typeparam name="T">The type to filter root assets by.</typeparam>
        /// <param name="contentDirectory">The registered content directory handle from which to retrieve root assets.</param>
        /// <returns>An array of root assets that match the specified type.</returns>
        public static T[] GetRootAssets<T>(ContentDirectoryHandle contentDirectory) where T : Object
            => FilterByType<T>(GetRootAssetsFromRegisteredDirectory(contentDirectory));

        private static T[] FilterByType<T>(Object[] assets) where T : Object
        {
            var typedList = new List<T>();
            foreach (var obj in assets)
            {
                if (typeof(T).IsAssignableFrom(obj.GetType()))
                    typedList.Add((T)obj);
            }
            return typedList.ToArray();
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

        // For test and internal usage
        // This method loads the BuildManifest, which describe the content available inside a Content Directory.
        [NativeMethod(IsThreadSafe = true)]
        internal static extern BuildManifest LoadBuildManifest(string path);
    }
}
