// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Jobs;

namespace Unity.IO.LowLevel.Unsafe
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct ReadCommand
    {
        public void* Buffer;
        public long Offset;
        public long Size;
    }

    public enum ReadStatus
    {
        Complete,
        InProgress,
        Failed
    }
    public struct ReadHandle : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal IntPtr ptr;
        internal int version;

        public bool IsValid()
        {
            return IsReadHandleValid(this);
        }

        public void Dispose()
        {
            if (!IsReadHandleValid(this))
                throw new InvalidOperationException("ReadHandle.Dispose cannot be called twice on the same ReadHandle");
            if (Status == ReadStatus.InProgress)
                throw new InvalidOperationException("ReadHandle.Dispose cannot be called until the read operation completes");
            ReleaseReadHandle(this);
        }

        public JobHandle JobHandle
        {
            get
            {
                if (!IsReadHandleValid(this))
                    throw new InvalidOperationException("ReadHandle.JobHandle cannot be called after the ReadHandle has been disposed");
                return GetJobHandle(this);
            }
        }
        public ReadStatus Status
        {
            get
            {
                if (!IsReadHandleValid(this))
                    throw new InvalidOperationException("ReadHandle.Status cannot be called after the ReadHandle has been disposed");
                return GetReadStatus(this);
            }
        }

        [ThreadAndSerializationSafe()]
        [FreeFunction("AsyncReadManagerManaged::GetReadStatus", IsThreadSafe = true)]
        private extern static ReadStatus GetReadStatus(ReadHandle handle);

        [ThreadAndSerializationSafe()]
        [FreeFunction("AsyncReadManagerManaged::ReleaseReadHandle", IsThreadSafe = true)]
        private extern static void ReleaseReadHandle(ReadHandle handle);

        [ThreadAndSerializationSafe()]
        [FreeFunction("AsyncReadManagerManaged::IsReadHandleValid", IsThreadSafe = true)]
        private extern static bool IsReadHandleValid(ReadHandle handle);

        [ThreadAndSerializationSafe()]
        [FreeFunction("AsyncReadManagerManaged::GetJobHandle", IsThreadSafe = true)]
        private extern static JobHandle GetJobHandle(ReadHandle handle);
    }

    [NativeHeader("Runtime/File/AsyncReadManagerManagedApi.h")]
    unsafe static public class AsyncReadManager
    {
        [ThreadAndSerializationSafe()]
        [FreeFunction("AsyncReadManagerManaged::Read", IsThreadSafe = true)]
        private extern unsafe static ReadHandle ReadInternal(string filename, void *cmds, uint cmdCount);
        public static ReadHandle Read(string filename, ReadCommand *readCmds, uint readCmdCount)
        {
            return ReadInternal(filename, readCmds, readCmdCount);
        }
    }
}
