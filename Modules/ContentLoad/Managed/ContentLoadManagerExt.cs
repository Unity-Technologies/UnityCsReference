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

namespace Unity.Loading
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
            string normalizedPath = Path.GetFullPath(contentDirectoryPath).TrimEnd('/', '\\');
            if (!Directory.Exists(normalizedPath))
                throw new DirectoryNotFoundException($"Content directory not found: {normalizedPath}");

            if (s_RegisteredPaths.Contains(normalizedPath))
                throw new InvalidOperationException($"Content directory is already registered: {normalizedPath}");

            var registration = new ContentDirectoryRegistration
            {
                NormalizedPath = normalizedPath,
                CAHHandles = new List<CAHArtifactDirectoryHandle>(),
                ArchiveHandles = new List<ArchiveHandle>()
            };

            try
            {
                MountAndRegisterArchives(ref registration);
                ContentManifest manifest = LoadManifestFromDirectory(normalizedPath);
                registration.ContentHandle = ContentLoadManager.RegisterContentDirectory(manifest);

                s_RegisteredPaths.Add(normalizedPath);
                s_RegistrationsByHandle[registration.ContentHandle.m_Handle] = registration;

                return registration.ContentHandle;
            }
            catch
            {
                CleanupRegisteredResources(registration);
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

        static string[] GetArchivePathsForFolder(string normalizedPath)
        {
            var numberedPaths = new List<string>();
            for (int i = 0; i < kMaxArchiveCount; i++)
            {
                string numberedPath = Path.Combine(normalizedPath, string.Format(NumberedArchiveFileNameFormat, i));
                if (!File.Exists(numberedPath))
                    break;
                numberedPaths.Add(numberedPath);
            }

            return numberedPaths.ToArray();
        }

        static void MountAndRegisterArchives(ref ContentDirectoryRegistration registration)
        {
            string[] archivePaths = GetArchivePathsForFolder(registration.NormalizedPath);

            if (archivePaths.Length == 0)
            {
                registration.CAHHandles.Add(CAHFileSystem.RegisterArtifactDirectory(registration.NormalizedPath));
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
