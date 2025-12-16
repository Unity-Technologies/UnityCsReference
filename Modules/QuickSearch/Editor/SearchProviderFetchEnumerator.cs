// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    /// <summary>
    /// Wrapper enumerator around a <see cref="SearchProvider"/> fetchItems enumerator object. Will stop the enumeration if the provider throws.
    /// </summary>
    class SearchProviderFetchEnumerator : IEnumerator<SearchItem>
    {
        // Real enumerator that supports nested IEnumerables/IEnumerators.
        readonly SearchEnumerator<SearchItem> m_Enumerator;
        readonly SearchProvider m_Provider;
        readonly System.Diagnostics.Stopwatch m_StopWatch;

        public SearchItem Current => m_Enumerator.Current;

        object IEnumerator.Current => Current;

        public SearchProviderFetchEnumerator(SearchProvider provider, object fetchEnumeratorObject)
        {
            m_Provider = provider ?? throw new ArgumentNullException(nameof(provider), "SearchProvider cannot be null");
            m_Enumerator = fetchEnumeratorObject != null ? new SearchEnumerator<SearchItem>(fetchEnumeratorObject) : new SearchEnumerator<SearchItem>();
            m_StopWatch = new System.Diagnostics.Stopwatch();
        }

        public bool MoveNext()
        {
            try
            {
                m_StopWatch.Start();
                var moveResult = m_Enumerator.MoveNext();
                m_StopWatch.Stop();
                m_Provider.IncrementFetchTime(m_StopWatch.Elapsed);
                m_StopWatch.Reset();
                return moveResult;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(new Exception($"Failed to fetch {m_Provider.name} provider items.", e));
                return false;
            }
        }

        public void Reset()
        {
            m_Enumerator.Reset();
        }

        public void Dispose()
        {
            m_Enumerator.Dispose();
        }
    }
}
