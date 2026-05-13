// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Sequential)]
    struct InternalManagedFileHandle : IEquatable<InternalManagedFileHandle>
    {
        internal int handle;

        public readonly bool isValid => handle != 0;

        public readonly bool Equals(InternalManagedFileHandle other) => handle == other.handle;
        public override readonly bool Equals(object obj) => obj is InternalManagedFileHandle other && Equals(other);
        public override readonly int GetHashCode() => handle;

        public static bool operator ==(InternalManagedFileHandle left, InternalManagedFileHandle right) => left.handle == right.handle;
        public static bool operator !=(InternalManagedFileHandle left, InternalManagedFileHandle right) => left.handle != right.handle;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ManagedReadAsyncCommand
    {
        private IntPtr m_Callback;
        private IntPtr m_Context;

        public void Complete(long bytesRead, bool success)
            => ManagedVFSNative.CompleteReadAsync(this, bytesRead, success);
    }

    [NativeHeader("Runtime/VirtualFileSystem/ManagedVFS/ManagedVirtualFileSystem.h")]
    static class ManagedVFSNative
    {
        [FreeFunction("ManagedVirtualFileSystem::CompleteReadAsync", IsThreadSafe = true)]
        internal static extern void CompleteReadAsync(ManagedReadAsyncCommand command, long bytesRead, bool success);
    }

    [VisibleToOtherModules("UnityEngine.ContentLoadModule", "ContentBuildLoadPreview")]
    internal interface IManagedVFSFileHandler
    {
        void ReadAsync(int handle, long offset, IntPtr buffer, int count, ManagedReadAsyncCommand command);
        long GetSize(int handle);
        void Close(int handle);
    }

    ref struct ReadLockScope
    {
        readonly ReaderWriterLockSlim m_Lock;
        public ReadLockScope(ReaderWriterLockSlim rwLock) { m_Lock = rwLock; m_Lock.EnterReadLock(); }
        public void Dispose() => m_Lock.ExitReadLock();
    }

    ref struct WriteLockScope
    {
        readonly ReaderWriterLockSlim m_Lock;
        public WriteLockScope(ReaderWriterLockSlim rwLock) { m_Lock = rwLock; m_Lock.EnterWriteLock(); }
        public void Dispose() => m_Lock.ExitWriteLock();
    }
}
