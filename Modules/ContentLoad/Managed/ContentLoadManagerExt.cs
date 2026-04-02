// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.ContentLoad;
using Unity.IO.Archive;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEngine.Loading
{
    /// <summary>
    /// Partial extension class providing high-level content directory registration with path-based management,
    /// archive mounting, and CAH filesystem integration.
    /// </summary>
    /// <remarks>
    /// Registration and unregistration are main-thread only. The static state (s_RegisteredPaths, s_RegistrationsByHandle)
    /// is not synchronized; do not call RegisterContentDirectory(string) or UnregisterContentDirectory from worker threads.
    /// </remarks>
    /*UCBP-PUBLIC*/ internal static partial class ContentLoadManager
    {
        struct ContentDirectoryRegistration
        {
            public string NormalizedPath;
            public ContentDirectoryHandle ContentHandle;
            public CAHArtifactDirectoryHandle? CAHHandle;
            public ArchiveHandle? ArchiveHandle;
        }

        const string ArchiveFileName = "content.archive";
        const string ManifestHashFileName = "BuildManifestHash.txt";
        const string ContentNamespaceName = "contentload";
        const string ArchiveMountPrefixFormat = "contentdirectory{0}";

        [AutoStaticsCleanupOnCodeReload]
        static ulong s_NextArchiveMountId = 0;

        [AutoStaticsCleanupOnCodeReload]
        static readonly HashSet<string> s_RegisteredPaths = new HashSet<string>();

        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<ulong, ContentDirectoryRegistration> s_RegistrationsByHandle = new Dictionary<ulong, ContentDirectoryRegistration>();

        private static ContentDirectoryHandle RegisterContentDirectoryFromPath(string contentDirectoryPath)
        {
            string normalizedPath = Path.GetFullPath(contentDirectoryPath).TrimEnd('/', '\\');
            if (!Directory.Exists(normalizedPath))
                throw new DirectoryNotFoundException($"Content directory not found: {normalizedPath}");

            if (s_RegisteredPaths.Contains(normalizedPath))
                throw new InvalidOperationException($"Content directory is already registered: {normalizedPath}");

            ArchiveHandle? archiveHandle = null;

            CAHArtifactDirectoryHandle cahHandle = RegisterArtifactDirectory(normalizedPath, ref archiveHandle);
            if (!cahHandle.isValid)
                throw new InvalidOperationException($"Failed to register CAH artifact directory: {contentDirectoryPath}");

            try
            {
                ContentManifest manifest = LoadManifestFromDirectory(normalizedPath);
                ContentDirectoryHandle contentHandle = ContentLoadManager.RegisterContentDirectory(manifest);

                TrackRegistration(normalizedPath, contentHandle, cahHandle, archiveHandle);

                return contentHandle;
            }
            catch
            {
                if (cahHandle.isValid)
                    CAHFileSystem.UnregisterArtifactDirectory(cahHandle);

                if (archiveHandle.HasValue)
                    archiveHandle.Value.Unmount();

                throw;
            }
        }

        internal static void CleanupTrackedRegistration(ContentDirectoryHandle contentDirectory)
        {
            if (!s_RegistrationsByHandle.TryGetValue(contentDirectory.m_Handle, out ContentDirectoryRegistration registration))
            {
                // Not tracked by Ext - no cleanup needed (e.g., RegisterContentDirectory(ContentManifest) path)
                return;
            }

            CleanupRegisteredResources(registration);
            s_RegisteredPaths.Remove(registration.NormalizedPath);
            s_RegistrationsByHandle.Remove(contentDirectory.m_Handle);
        }

        static ContentManifest LoadManifestFromDirectory(string normalizedPath)
        {
            string manifestHashPath = Path.Combine(normalizedPath, ManifestHashFileName);

            if (!File.Exists(manifestHashPath))
                throw new FileNotFoundException($"Required manifest hash file not found: {manifestHashPath}");

            string hashText = File.ReadAllText(manifestHashPath).Trim();
            Hash128 manifestHash = Hash128.Parse(hashText);

            string vfsPath = CAHFileSystem.GetVFSPath(manifestHash);

            if (!ContentManifest.LoadFromFile(vfsPath, out ContentManifest manifest))
                throw new InvalidOperationException($"Failed to load content manifest from VFS path: {vfsPath}");

            return manifest;
        }

        static CAHArtifactDirectoryHandle RegisterArtifactDirectory(string normalizedPath, ref ArchiveHandle? archiveHandle)
        {
            string archivePath = Path.Combine(normalizedPath, ArchiveFileName);
            if (File.Exists(archivePath))
            {
                var contentNamespace = Unity.Content.ContentNamespace.GetOrCreateNamespace(ContentNamespaceName);
                string mountPrefix = string.Format(ArchiveMountPrefixFormat, s_NextArchiveMountId++);

                archiveHandle = ArchiveFileInterface.MountAsync(
                    contentNamespace,
                    archivePath,
                    mountPrefix);

                archiveHandle.Value.JobHandle.Complete();

                return CAHFileSystem.RegisterArtifactDirectory(archiveHandle.Value.GetMountPath());
            }
            else
            {
                return CAHFileSystem.RegisterArtifactDirectory(normalizedPath);
            }
        }

        private static void TrackRegistration(
            string normalizedPath,
            ContentDirectoryHandle contentHandle,
            CAHArtifactDirectoryHandle? cahHandle,
            ArchiveHandle? archiveHandle)
        {
            var registration = new ContentDirectoryRegistration
            {
                NormalizedPath = normalizedPath,
                ContentHandle = contentHandle,
                CAHHandle = cahHandle,
                ArchiveHandle = archiveHandle
            };

            s_RegisteredPaths.Add(normalizedPath);
            s_RegistrationsByHandle[contentHandle.m_Handle] = registration;
        }

        static void CleanupRegisteredResources(ContentDirectoryRegistration registration)
        {
            if (registration.CAHHandle.HasValue)
                CAHFileSystem.UnregisterArtifactDirectory(registration.CAHHandle.Value);

            if (registration.ArchiveHandle.HasValue)
                registration.ArchiveHandle.Value.Unmount();
        }
    }
}
