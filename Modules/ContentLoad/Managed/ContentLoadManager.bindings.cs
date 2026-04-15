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
    /// Represents a handle to a content directory registered via the ContentLoadManager. This struct is used to encapsulate
    /// information about the content directory, such as its path, and is returned from the RegisterContentDirectory operation
    /// in ContentLoadManager.
    /// </summary>
    /// <seealso cref="ContentLoadManager.RegisterContentDirectory(string)"/>
    /// <seealso cref="ContentLoadManager.RegisterContentDirectory(ContentManifest)"/>
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEditor.ContentLoadModule")]
    /*UCBP-PUBLIC*/ internal struct ContentDirectoryHandle
    {
        internal UInt64 m_Handle;

        /// <summary> Returns true if the handle is valid.</summary>
        /// <value>
        /// A bool representing that the content directory handle is valid.
        /// </value>
        public readonly bool isValid => m_Handle != 0;

        /// <summary> The build name of the content directory (e.g. from the Manifest build name).</summary>
        public string buildName
        {
            get
            {
                return isValid ? GetBuildNameFromContentDirectoryHandleInternal(this) : string.Empty;
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
    /// In the Editor this is normally not used, because the content is available directly in the project using
    /// <see cref="UnityEditor.AssetDatabase"/> and <see cref="UnityEditor.SceneManagement.EditorSceneManager"/> calls.
    /// However, it can be useful in Editor play mode to run the same loading case as the runtime and to try out the
    /// output of your Content Directory build, directly inside the Editor.
    /// </remarks>
    /// <seealso cref="Loadable{T}"/>
    /// <seealso cref="LoadableSceneId"/>
    /// <seealso cref="UnityEditor.BuildPipeline.BuildContentDirectory"/>
    [NativeHeader("Modules/ContentLoad/Public/ContentLoadManager.bindings.h")]
    [StaticAccessor("ContentLoad", StaticAccessorType.DoubleColon)]
    /*UCBP-REMOVE*/ [VisibleToOtherModules("UnityEditor.ContentLoadModule")]
    /*UCBP-PUBLIC*/ internal static partial class ContentLoadManager
    {
        /// <summary>
        /// Add the built-content in a directory to the ContentLoadManager. This makes it possible to load the contained Scenes
        /// and Assets.
        /// </summary>
        /// <remarks>
        /// In the runtime this is required when <see cref="BuildPipeline.BuildContentDirectory"/> has been used to build and
        /// distribute additional content.
        ///
        /// This can be called multiple times, with different paths, to expose the contents of additional content directories to
        /// the editor/runtime. However, this method is bound to a specific directory structure and limited to one build per directory.
        /// Use <see cref="RegisterContentDirectory(ContentManifest)"/> lower-level API when you need more flexibility in how content is
        /// organized or when registering multiple builds from custom locations.
        ///
        /// For a clean shutdown, each call to RegisterContentDirectory should be matched with a call to
        /// UnregisterContentDirectory.
        ///
        /// Throws <see cref="InvalidOperationException"/> if the path is already registered or if the content manifest cannot be loaded.
        /// Throws <see cref="FileNotFoundException"/> if the required manifest hash file is not found.
        /// Throws <see cref="DirectoryNotFoundException"/> if the directory does not exist.
        /// </remarks>
        /// <param name="contentDirectoryPath">
        /// A local path pointing to a directory that contains the output from a call to
        /// <see cref="BuildPipeline.BuildContentDirectory"/>.
        /// </param>
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
        /// this method does NOT automatically register CAH artifact directories or mount archives. The caller is responsible for
        /// registering any required CAH artifact directories and mounting any archives before calling this method.
        ///
        /// For most use cases, prefer <see cref="RegisterContentDirectory(string)"/> which handles these setup steps automatically.
        ///
        /// For a clean shutdown, each call to RegisterContentDirectory should be matched with a call to UnregisterContentDirectory.
        /// </remarks>
        /// <param name="manifest">The content manifest to register</param>
        /// <returns>Handle to the registered content directory</returns>
        internal static ContentDirectoryHandle RegisterContentDirectory(ContentManifest manifest)
        {
            var handle = RegisterInternalFromContentManifest(manifest);
            if (!handle.isValid)
                throw new InvalidOperationException("Failed to register content directory from manifest");

            return handle;
        }

        [FreeFunction("ContentLoad::RegisterContentDirectoryFromContentManifest")]
        static extern ContentDirectoryHandle RegisterInternalFromContentManifest([NotNull] ContentManifest contentManifest);

        /// <summary>
        /// Remove access to content that had been loaded from a content directory.
        /// </summary>
        /// <remarks>
        /// All Loadable&lt;T&gt; referencing the content of a directory must be explicitly released prior to calling this.
        /// Similarly all LoadableSceneId must be Unloaded.
        /// </remarks>
        /// <param name="contentDirectory">
        /// Content directory handle to unregister
        /// </param>
        public static void UnregisterContentDirectory(ContentDirectoryHandle contentDirectory)
        {
            if (!contentDirectory.isValid)
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
