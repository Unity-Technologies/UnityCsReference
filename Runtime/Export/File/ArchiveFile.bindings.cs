// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Content;
using Unity.Jobs;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.IO.Archive
{
    [RequiredByNativeCode]
    public enum ArchiveStatus
    {
        InProgress,
        Complete,
        Failed
    }

    [NativeHeader("Runtime/VirtualFileSystem/ArchiveFileSystem/ArchiveFileHandle.h")]
    [RequiredByNativeCode]
    public struct ArchiveFileInfo
    {
        public string Filename;
        public ulong FileSize;
    }

    [NativeHeader("Runtime/VirtualFileSystem/ArchiveFileSystem/ArchiveFileHandle.h")]
    [RequiredByNativeCode]
    public struct ArchiveHandle
    {
        internal UInt64 Handle;

        public ArchiveStatus Status
        {
            get
            {
                ThrowIfInvalid();
                return ArchiveFileInterface.Archive_GetStatus(this);
            }
        }

        public JobHandle JobHandle
        {
            get
            {
                ThrowIfInvalid();
                return ArchiveFileInterface.Archive_GetJobHandle(this);
            }
        }

        public JobHandle Unmount()
        {
            ThrowIfInvalid();
            return ArchiveFileInterface.Archive_UnmountAsync(this);
        }

        void ThrowIfInvalid()
        {
            if (!ArchiveFileInterface.Archive_IsValid(this))
                throw new InvalidOperationException("The archive has already been unmounted.");
        }

        public string GetMountPath()
        {
            ThrowIfInvalid();
            return ArchiveFileInterface.Archive_GetMountPath(this);
        }

        public UnityEngine.CompressionType Compression
        {
            get
            {
                ThrowIfInvalid();
                return ArchiveFileInterface.Archive_GetCompression(this);
            }
        }

        public bool IsStreamed
        {
            get
            {
                ThrowIfInvalid();
                return ArchiveFileInterface.Archive_IsStreamed(this);
            }
        }

        public ArchiveFileInfo[] GetFileInfo()
        {
            ThrowIfInvalid();
            return ArchiveFileInterface.Archive_GetFileInfo(this);
        }
    }

    [RequiredByNativeCode]
    [NativeHeader("Runtime/VirtualFileSystem/ArchiveFileSystem/ArchiveFileHandle.h")]
    [StaticAccessor("GetManagedArchiveSystem()", StaticAccessorType.Dot)]
    public static class ArchiveFileInterface
    {
        public static extern ArchiveHandle MountAsync(ContentNamespace namespaceId, string filePath, string prefix);
        public static extern ArchiveHandle[] GetMountedArchives(ContentNamespace namespaceId);

        internal static extern ArchiveStatus Archive_GetStatus(ArchiveHandle handle);
        internal static extern JobHandle Archive_GetJobHandle(ArchiveHandle handle);
        internal static extern bool Archive_IsValid(ArchiveHandle handle);
        internal static extern JobHandle Archive_UnmountAsync(ArchiveHandle handle);
        internal static extern string Archive_GetMountPath(ArchiveHandle handle);
        internal static extern UnityEngine.CompressionType Archive_GetCompression(ArchiveHandle handle);
        internal static extern bool Archive_IsStreamed(ArchiveHandle handle);
        internal static extern ArchiveFileInfo[] Archive_GetFileInfo(ArchiveHandle handle);
    }
}
