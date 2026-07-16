// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Threading;

namespace UnityEditor.Search
{
    /// <summary>
    /// Coordinates access to a <see cref="SearchIndexer"/>'s native storage between readers (typically
    /// background search threads) and the thread that disposes the index.
    ///
    /// A reader registers for the duration of its access by calling <see cref="TryAcquire"/>. Disposal
    /// calls <see cref="CloseAndWait"/>, which cancels the disposal token and then blocks until every
    /// outstanding lease has been released. This guarantees the native storage is never freed while a
    /// reader is still using it.
    ///
    /// IMPORTANT: a lease must be released by the same work that uses the index (e.g. the worker
    /// thread/task), never by code pumped on the thread that calls <see cref="CloseAndWait"/>. That
    /// thread is blocked for the duration of the wait, so a lease released from it would deadlock.
    /// </summary>
    sealed class SearchIndexAccessGuard
    {
        readonly object m_SyncRoot = new object();
        // Never disposed on purpose: leases hand out copies of this token and external callers may link
        // it into their own token sources that can outlive the wait. The source holds no OS handle
        // (we never access its WaitHandle), so letting the GC reclaim it is safe and avoids
        // ObjectDisposedException ordering hazards with linked token sources.
        readonly CancellationTokenSource m_DisposalCts = new CancellationTokenSource();
        int m_ActiveLeases;
        bool m_Closing;

        /// <summary>
        /// Token that is canceled when the guarded index starts being disposed. Readers should observe
        /// it and stop touching the index promptly so <see cref="CloseAndWait"/> can return.
        /// </summary>
        public CancellationToken disposalToken => m_DisposalCts.Token;

        /// <summary>
        /// Registers a reader. The returned lease must be disposed when the reader is done. Returns
        /// <c>null</c> if the index is already closing or disposed, in which case the caller must not
        /// touch the index.
        /// </summary>
        public IDisposable TryAcquire(out CancellationToken disposalToken)
        {
            lock (m_SyncRoot)
            {
                if (m_Closing)
                {
                    disposalToken = new CancellationToken(canceled: true);
                    return null;
                }

                ++m_ActiveLeases;
                disposalToken = m_DisposalCts.Token;
                return new Lease(this);
            }
        }

        void Release()
        {
            lock (m_SyncRoot)
            {
                if (--m_ActiveLeases == 0)
                    Monitor.Pulse(m_SyncRoot);
            }
        }

        /// <summary>
        /// Marks the guard as closing, cancels the disposal token and blocks until all outstanding
        /// leases have been released. After this returns, no new lease can be acquired and it is safe
        /// to free the resources the leases were protecting. Idempotent.
        /// </summary>
        public void CloseAndWait()
        {
            lock (m_SyncRoot)
            {
                m_Closing = true;
            }

            // Cancel outside the lock: cancellation callbacks run synchronously on this thread and may
            // execute arbitrary code registered by readers.
            m_DisposalCts.Cancel();

            lock (m_SyncRoot)
            {
                while (m_ActiveLeases > 0)
                    Monitor.Wait(m_SyncRoot);
            }
        }

        sealed class Lease : IDisposable
        {
            SearchIndexAccessGuard m_Guard;

            public Lease(SearchIndexAccessGuard guard)
            {
                m_Guard = guard;
            }

            public void Dispose()
            {
                // Guard against double-dispose; release exactly once.
                var guard = Interlocked.Exchange(ref m_Guard, null);
                guard?.Release();
            }
        }
    }
}
