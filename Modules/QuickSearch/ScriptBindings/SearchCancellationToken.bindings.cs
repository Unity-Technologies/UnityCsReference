// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine.Bindings;

namespace UnityEditor.Search
{
    /// <summary>
    /// Wrapper around a CancellationToken to be used when native methods need to be cancelled.
    /// </summary>
    [NativeHeader("Modules/QuickSearch/SearchCancellationToken.h")]
    [NativeHeader("Modules/QuickSearch/SearchCancellationTokenBindings.h")]
    class SearchCancellationToken : IDisposable
    {
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToUnmanaged(SearchCancellationToken token) => token.m_Ptr;
        }

        IntPtr m_Ptr;
        CancellationTokenRegistration? m_CancellationTokenRegistration;
        ReaderWriterLockSlim m_Lock;

        public static readonly SearchCancellationToken None = new(IntPtr.Zero);

        public bool IsCreated => m_Ptr != IntPtr.Zero;

        public bool IsCancellationRequested
        {
            get
            {
                // Early out if the token is not created. This avoid taking a lock in the common case where no cancellation is needed.
                if (!IsCreated)
                    return false;

                m_Lock.EnterReadLock();
                try
                {
                    if (IsCreated)
                        return IsCancellationRequested_Internal();
                }
                finally
                {
                    m_Lock.ExitReadLock();
                }
                return false;
            }
        }

        public SearchCancellationToken()
        {
            m_Ptr = Create(GCHandle.ToIntPtr(GCHandle.Alloc(this)));
            m_Lock = new ReaderWriterLockSlim();
        }

        public SearchCancellationToken(CancellationToken token)
            : this()
        {
            m_CancellationTokenRegistration = token.Register(Cancel);
        }

        SearchCancellationToken(IntPtr ptr)
        {
            m_Ptr = ptr;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            // Early out if the token is not created. This avoid taking a lock in the common case where no cancellation is needed.
            if (!IsCreated)
                return;

            m_Lock.EnterWriteLock();
            try
            {
                m_CancellationTokenRegistration?.Dispose();
                m_CancellationTokenRegistration = null;
                if (IsCreated)
                {
                    Destroy(m_Ptr);
                    m_Ptr = IntPtr.Zero;
                }
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
        }

        public void Cancel()
        {
            // Early out if the token is not created. This avoid taking a lock in the common case where no cancellation is needed.
            if (!IsCreated)
                return;

            m_Lock.EnterReadLock();
            try
            {
                if (IsCreated)
                    Cancel_Internal();
            }
            finally
            {
                m_Lock.ExitReadLock();
            }
        }

        // Native calls
        [FreeFunction("SearchCancellationTokenBindings::Create", IsThreadSafe = true)]
        static extern IntPtr Create(IntPtr handlePtr);

        [FreeFunction("SearchCancellationTokenBindings::Destroy", IsThreadSafe = true)]
        static extern void Destroy(IntPtr nativePtr);

        [NativeMethod("IsCancellationRequested", IsThreadSafe = true)]
        extern bool IsCancellationRequested_Internal();

        [NativeMethod("Cancel", IsThreadSafe = true)]
        extern void Cancel_Internal();
    }
}
