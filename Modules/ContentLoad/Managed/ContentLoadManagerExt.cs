// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.IO.Archive;
using Unity.Jobs;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Scripting;
using Unity.IO;

namespace Unity.Loading
{
    // Additional implementation for ContentLoadManager providing path-based registration,
    // archive mounting, and CAH filesystem integration.
    // Registration and unregistration are main-thread only; do not call from worker threads.
    public static partial class ContentLoadManager
    {
        struct ContentDirectoryRegistration
        {
            public string ContentDirectoryPath;
            public ContentDirectoryHandle ContentHandle;
            public List<CAHArtifactDirectoryHandle> CAHHandles;
            public List<ArchiveHandle> ArchiveHandles;
        }

        const string NumberedArchiveFileNameFormat = "content{0}.archive";
        const string ManifestHashFileName = "BuildManifestHash.txt";
        const string ContentNamespaceName = "contentload";
        const string ArchiveMountPrefixFormat = "contentdirectory{0}";
        const int kMaxArchiveCount = 1000;

        [AutoStaticsCleanupOnCodeReload]
        static ulong s_NextArchiveMountId = 0;

        [AutoStaticsCleanupOnCodeReload]
        static readonly HashSet<string> s_RegisteredPaths = new HashSet<string>();

        [AutoStaticsCleanupOnCodeReload]
        static readonly Dictionary<ulong, ContentDirectoryRegistration> s_RegistrationsByHandle = new Dictionary<ulong, ContentDirectoryRegistration>();

        private static ContentDirectoryHandle RegisterContentDirectoryFromPath(string contentDirectoryPath)
        {
            string sanitizedPath = contentDirectoryPath.TrimEnd('/', '\\');

            if (s_RegisteredPaths.Contains(sanitizedPath))
                throw new InvalidOperationException($"Content directory is already registered: {sanitizedPath}");

            var registration = new ContentDirectoryRegistration
            {
                ContentDirectoryPath = sanitizedPath,
                CAHHandles = new List<CAHArtifactDirectoryHandle>(),
                ArchiveHandles = new List<ArchiveHandle>()
            };

            ContentManifest manifest;
            try
            {
                MountAndRegisterArchives(ref registration);
                manifest = LoadManifestFromDirectory(sanitizedPath);
            }
            catch (Exception ex)
            {
                CleanupRegisteredResources(registration);
                throw new InvalidOperationException($"Failed to register ContentDirectory at {contentDirectoryPath}.", ex);
            }

            try
            {
                // The manifest overload is the canonical validation entry point and may throw
                // InvalidOperationException (edit-mode, future platform / has-type-trees checks, etc.).
                // Those already name the reason — propagate unwrapped.
                registration.ContentHandle = ContentLoadManager.RegisterContentDirectory(manifest);

                s_RegisteredPaths.Add(sanitizedPath);
                s_RegistrationsByHandle[registration.ContentHandle.m_Handle] = registration;
            }
            catch
            {
                CleanupRegisteredResources(registration);
                throw;
            }

            return registration.ContentHandle;
        }

        internal static void CleanupTrackedRegistration(ContentDirectoryHandle contentDirectory)
        {
            if (!s_RegistrationsByHandle.TryGetValue(contentDirectory.m_Handle, out ContentDirectoryRegistration registration))
            {
                // Not tracked by Ext - no cleanup needed (e.g., RegisterContentDirectory(ContentManifest) path)
                return;
            }

            CleanupRegisteredResources(registration);
            s_RegisteredPaths.Remove(registration.ContentDirectoryPath);
            s_RegistrationsByHandle.Remove(contentDirectory.m_Handle);
        }

        static ContentManifest LoadManifestFromDirectory(string contentDirectoryPath)
        {
            string manifestHashPath = contentDirectoryPath + "/" + ManifestHashFileName;

            byte[] loaded = UnityFile.ReadAllBytes(manifestHashPath);
            string hashText = System.Text.Encoding.UTF8.GetString(loaded).Trim();
            Hash128 manifestHash = Hash128.Parse(hashText);

            string vfsPath = CAHFileSystem.GetVFSPath(manifestHash);

            if (!ContentManifest.LoadFromFile(vfsPath, out ContentManifest manifest))
                throw new InvalidOperationException($"Failed to load content manifest from VFS path: {vfsPath}");

            return manifest;
        }

        static string[] GetArchivePathsForFolder(string contentDirectoryPath)
        {
            var numberedPaths = new List<string>();
            for (int i = 0; i < kMaxArchiveCount; i++)
            {
                // Use string concatenation instead of Path.Combine to support Android URI paths (e.g., jar:file://...)
                string numberedPath = contentDirectoryPath + "/" + string.Format(NumberedArchiveFileNameFormat, i);
                if (!UnityFile.Exists(numberedPath))
                    break;
                numberedPaths.Add(numberedPath);
            }

            return numberedPaths.ToArray();
        }

        static void MountAndRegisterArchives(ref ContentDirectoryRegistration registration)
        {
            string[] archivePaths = GetArchivePathsForFolder(registration.ContentDirectoryPath);

            if (archivePaths.Length == 0)
            {
                registration.CAHHandles.Add(CAHFileSystem.RegisterArtifactDirectory(registration.ContentDirectoryPath));
                return;
            }

            var contentNamespace = Unity.Content.ContentNamespace.GetOrCreateNamespace(ContentNamespaceName);

            for (int i = 0; i < archivePaths.Length; i++)
            {
                string mountPrefix = string.Format(ArchiveMountPrefixFormat, s_NextArchiveMountId++);
                registration.ArchiveHandles.Add(ArchiveFileInterface.MountAsync(contentNamespace, archivePaths[i], mountPrefix));
            }

            foreach (var archive in registration.ArchiveHandles)
                archive.JobHandle.Complete();

            for (int i = 0; i < registration.ArchiveHandles.Count; i++)
            {
                if (registration.ArchiveHandles[i].Status != ArchiveStatus.Complete)
                    throw new InvalidOperationException($"Failed to mount archive: {archivePaths[i]} (status: {registration.ArchiveHandles[i].Status})");
            }

            for (int i = 0; i < registration.ArchiveHandles.Count; i++)
            {
                var cahHandle = CAHFileSystem.RegisterArtifactDirectory(registration.ArchiveHandles[i].GetMountPath());
                registration.CAHHandles.Add(cahHandle);

                if (!cahHandle.isValid)
                    throw new InvalidOperationException($"Failed to register CAH artifact directory for archive: {archivePaths[i]}");
            }
        }

        static void CleanupRegisteredResources(ContentDirectoryRegistration registration)
        {
            foreach (var cahHandle in registration.CAHHandles)
            {
                if (cahHandle.isValid)
                    CAHFileSystem.UnregisterArtifactDirectory(cahHandle);
            }

            var unmountJobs = new JobHandle[registration.ArchiveHandles.Count];
            for (int i = 0; i < registration.ArchiveHandles.Count; i++)
                unmountJobs[i] = registration.ArchiveHandles[i].Unmount();
            for (int i = 0; i < unmountJobs.Length; i++)
                unmountJobs[i].Complete();
        }
    }
}
