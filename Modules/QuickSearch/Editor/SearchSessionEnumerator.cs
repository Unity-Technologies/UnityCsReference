// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace UnityEditor.Search
{
    /// <summary>
    /// An enumerator for a <see cref="SearchSession"/>. Enumerates the results of a <see cref="SearchSession"/>.
    /// </summary>
    /// <remarks>This will enumerate a <see cref="SearchSession"/> from the start until its end. It assumes that <see cref="SearchSession.Start"/> has not been called yet. It does not restart the session. If the enumeration reaches the end, the session is ended.</remarks>
    class SearchSessionEnumerator : IEnumerator<SearchItem>
    {
        SearchSession m_Session;
        SearchItem m_Current;
        CancellationToken m_SessionCancellationToken;

        public SearchItem Current => m_Current;
        object IEnumerator.Current => Current;

        public SearchSessionEnumerator(SearchSession session)
        {
            m_Session = session ?? throw new ArgumentNullException(nameof(session), "A non null SearchSession must be provided to enumerate it.");
            m_Current = null;
            m_Session.Start();
            m_SessionCancellationToken = session.cancelToken;
        }

        public void Dispose()
        {
            m_Session.Stop();
            m_Session = null;
            m_SessionCancellationToken = CancellationToken.None;
        }

        public bool MoveNext()
        {
            // In case the enumerator was disposed or the session was cancelled.
            if (m_Session == null || m_SessionCancellationToken.IsCancellationRequested)
                return false;

            if (m_Session.context.options.HasAny(SearchFlags.Synchronous))
                Dispatcher.ProcessOne();

            var success = m_Session.FetchOne(out m_Current);
            if (!success)
                m_Session.Stop();
            return success;
        }

        public void Reset()
        {
            throw new NotSupportedException($"{nameof(SearchSession)} can not be enumerated multiple times.");
        }
    }
}
