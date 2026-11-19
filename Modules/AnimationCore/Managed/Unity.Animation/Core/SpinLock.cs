// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Animations
{
    /// <summary>
    /// Spin lock structure for short timed locking of threads
    /// You can rely on the auto <see cref="Scope"/> structure for safe Acquisition/Release of the lock
    /// </summary>
    unsafe struct SpinLock : IDisposable
    {
        int* m_InternalLock;
        Allocator m_Allocator;

        public bool IsCreated => m_InternalLock != null;

        public SpinLock(Allocator allocator)
        {
            m_Allocator = allocator;
            m_InternalLock = Malloc<int>(allocator);
            *m_InternalLock = 0;
        }

        public void Dispose()
        {
            UnsafeUtility.Free(m_InternalLock, m_Allocator);
            this = default;
        }

        public Scope Lock() => new Scope(this);

        /// <summary>
        /// Acquires the lock
        /// </summary>
        void Acquire()
        {
            while (Interlocked.CompareExchange(ref *m_InternalLock, 1, 0) != 0)
            {
                // Wait for the lock to be released without generating cache misses
                while (Volatile.Read(ref *m_InternalLock) == 1)
                    continue;
            }
        }

        /// <summary>
        /// Releases the lock
        /// </summary>
        void Release() => Volatile.Write(ref *m_InternalLock, 0);

        public ref struct Scope
        {
            SpinLock m_Lock;

            public bool IsCreated => m_Lock.IsCreated;

            /// <summary>
            /// Acquires the lock
            /// </summary>
            /// <param name="lock"></param>
            public Scope(SpinLock @lock)
            {
                Core.ValidateIsTrue(@lock.IsCreated);
                m_Lock = @lock;
                m_Lock.Acquire();
            }

            /// <summary>
            /// Releases the lock
            /// </summary>
            public void Dispose()
            {
                m_Lock.Release();
                m_Lock = default;
            }
        }

        static T* Malloc<T>(Allocator allocator) where T : unmanaged =>
            (T*)UnsafeUtility.Malloc(sizeof(T), UnsafeUtility.AlignOf<T>(), allocator);
    }
}
