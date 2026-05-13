// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using Unity.IO.Archive;

namespace UnityEngine
{
    /// <summary>
    /// Represents a handle to an artifact directory registered via the CAHFileSystem.
    /// This handle is returned from RegisterArtifactDirectory and is used to unregister the directory.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEngine.ContentLoadModule", "ContentBuildLoadPreview")]
    internal struct CAHArtifactDirectoryHandle
    {
        internal UInt64 m_Handle;

        /// <summary>
        /// Returns true if the handle is valid.
        /// </summary>
        public readonly bool isValid => m_Handle != 0;
    }

    /// <summary>
    /// Provides methods to register and unregister artifact directories with the CAH (Content Addressable Hash) File System.
    /// When an artifact directory is registered, the system enumerates files with Hash128 filenames and makes them
    /// accessible through the CAHFileSystem.
    /// </summary>
    [NativeHeader("Runtime/VirtualFileSystem/CAHFileSystem/CAHFileSystem.h")]
    [NativeHeader("Runtime/VirtualFileSystem/CAHFileSystem/CAHFileSystemSingleton.h")]
    [StaticAccessor("GetCAHFileSystem()", StaticAccessorType.Dot)]
    [VisibleToOtherModules("UnityEngine.ContentLoadModule", "ContentBuildLoadPreview")]
    internal static class CAHFileSystem
    {
        /// <summary>
        /// Registers an artifact directory with the CAH File System.
        /// This enumerates all files in the directory with Hash128 filenames and registers them for content-addressable lookup.
        /// </summary>
        /// <param name="directoryPath">The path to the directory containing artifact files. Cannot be null or empty.</param>
        /// <returns>A handle that can be used to unregister the directory. Returns an invalid handle (isValid == false) if registration fails.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern CAHArtifactDirectoryHandle RegisterArtifactDirectory([NotNull] string directoryPath);

        /// <summary>
        /// Unregisters an artifact directory that was previously registered with RegisterArtifactDirectory.
        /// This removes the file collection mapping.
        /// </summary>
        /// <param name="handle">The handle returned from RegisterArtifactDirectory.</param>
        /// <returns>True if the directory was successfully unregistered, false if the handle is invalid. Logs an error on failure.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern bool UnregisterArtifactDirectory(CAHArtifactDirectoryHandle handle);

        /// <summary>
        /// Gets the VFS format path for a given content-addressable hash (cah:/hash).
        /// </summary>
        /// <param name="hash">The hash to convert to VFS path.</param>
        /// <returns>The VFS path.</returns>
        [NativeMethod(IsThreadSafe = true)]
        public static extern string GetVFSPath(Hash128 hash);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern void SetHasManagedHandlers(bool value);
    }

}
