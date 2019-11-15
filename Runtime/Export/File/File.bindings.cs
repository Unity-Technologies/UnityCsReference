// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine.IO
{
    [NativeHeader("Runtime/VirtualFileSystem/VirtualFileSystem.h")]
    internal enum ThreadIORestrictionMode
    {
        Allowed = 0,
        TreatAsError = 1,
    }

    [NativeHeader("Runtime/VirtualFileSystem/VirtualFileSystem.h")]
    [NativeConditional("ENABLE_PROFILER")]
    [StaticAccessor("FileAccessor", StaticAccessorType.DoubleColon)]
    internal static class File
    {
        internal static ulong totalOpenCalls
        {
            get { return GetTotalOpenCalls(); }
        }
        internal static ulong totalCloseCalls
        {
            get { return GetTotalCloseCalls(); }
        }
        internal static ulong totalReadCalls
        {
            get { return GetTotalReadCalls(); }
        }
        internal static ulong totalWriteCalls
        {
            get { return GetTotalWriteCalls(); }
        }
        internal static ulong totalSeekCalls
        {
            get { return GetTotalSeekCalls(); }
        }
        internal static ulong totalZeroSeekCalls
        {
            get { return GetTotalZeroSeekCalls(); }
        }

        internal static ulong totalFilesOpened
        {
            get { return GetTotalFilesOpened(); }
        }
        internal static ulong totalFilesClosed
        {
            get { return GetTotalFilesClosed(); }
        }
        internal static ulong totalBytesRead
        {
            get { return GetTotalBytesRead(); }
        }
        internal static ulong totalBytesWritten
        {
            get { return GetTotalBytesWritten(); }
        }

        internal static bool recordZeroSeeks
        {
            set { SetRecordZeroSeeks(value); }
            get { return GetRecordZeroSeeks(); }
        }

        // This can be used to print errors when when I/O is performed on the main thread. This is useful for testing async operations
        // to ensure they don't block the main thread with file I/O.
        internal static ThreadIORestrictionMode MainThreadIORestrictionMode
        {
            get { return GetMainThreadFileIORestriction(); }
            set { SetMainThreadFileIORestriction(value); }
        }

        internal extern static void SetRecordZeroSeeks(bool enable);
        internal extern static bool GetRecordZeroSeeks();

        internal extern static ulong GetTotalOpenCalls();
        internal extern static ulong GetTotalCloseCalls();
        internal extern static ulong GetTotalReadCalls();
        internal extern static ulong GetTotalWriteCalls();
        internal extern static ulong GetTotalSeekCalls();
        internal extern static ulong GetTotalZeroSeekCalls();

        internal extern static ulong GetTotalFilesOpened();
        internal extern static ulong GetTotalFilesClosed();
        internal extern static ulong GetTotalBytesRead();
        internal extern static ulong GetTotalBytesWritten();

        private extern unsafe static void SetMainThreadFileIORestriction(ThreadIORestrictionMode mode);
        private extern unsafe static ThreadIORestrictionMode GetMainThreadFileIORestriction();
    }
}
