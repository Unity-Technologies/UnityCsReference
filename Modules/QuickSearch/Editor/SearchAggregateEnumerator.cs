// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    enum SearchAggregateEnumerationStyle
    {
        Sequential,
        RoundRobin
    }

    class SearchAggregateEnumerator<T> : IEnumerator<T>
    {
        const int k_NoEnumerationIndex = -1;

        struct EnumeratorData
        {
            public IEnumerator<T> Enumerator;
            public bool Active;
        }

        readonly List<EnumeratorData> m_EnumeratorsData;
        int m_CurrentIndex;
        T m_Current;
        int m_Remaining;
        readonly SearchAggregateEnumerationStyle m_EnumerationStyle;

        public T Current
        {
            get
            {
                if (m_CurrentIndex < 0 || m_CurrentIndex >= m_EnumeratorsData.Count)
                    throw new InvalidOperationException();
                return m_Current;
            }
        }

        object IEnumerator.Current => Current;

        public int Count => m_EnumeratorsData.Count;

        public SearchAggregateEnumerator(params IEnumerator<T>[] enumerators)
            : this(SearchAggregateEnumerationStyle.Sequential, enumerators)
        {}

        public SearchAggregateEnumerator(SearchAggregateEnumerationStyle enumerationStyle, params IEnumerator<T>[] enumerators)
        {
            if (enumerators == null)
                throw new ArgumentNullException(nameof(enumerators), "A non null enumerator array must be provided.");

            m_EnumerationStyle = enumerationStyle;
            m_EnumeratorsData = new List<EnumeratorData>(enumerators.Length);
            foreach (var enumerator in enumerators)
            {
                if (enumerator != null)
                    m_EnumeratorsData.Add(new EnumeratorData { Active = true, Enumerator = enumerator });
                else
                    throw new ArgumentNullException(nameof(enumerators), "Null enumerator is not allowed.");
            }
            m_Remaining = m_EnumeratorsData.Count;
            m_CurrentIndex = k_NoEnumerationIndex;
        }

        public void Clear()
        {
            Dispose();
            m_EnumeratorsData.Clear();
            m_Remaining = 0;
            m_CurrentIndex = k_NoEnumerationIndex;
            m_Current = default;
        }

        public void Dispose()
        {
            foreach (var enumeratorData in m_EnumeratorsData)
            {
                if (enumeratorData.Enumerator is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        public void AddEnumerator(IEnumerator<T> enumerator)
        {
            if (enumerator == null)
                throw new ArgumentNullException(nameof(enumerator), "Null enumerator is not allowed.");
            m_EnumeratorsData.Add(new EnumeratorData { Active = true, Enumerator = enumerator });
            m_Remaining++;
        }

        public bool MoveNext()
        {
            if (m_Remaining == 0)
                return false;

            var startIndex = GetCurrentEnumeratorIndex();
            var checkedCount = 0;

            while (checkedCount < m_EnumeratorsData.Count)
            {
                var idx = (startIndex + checkedCount) % m_EnumeratorsData.Count;
                var enumeratorData = m_EnumeratorsData[idx];
                if (enumeratorData.Active)
                {
                    if (enumeratorData.Enumerator.MoveNext())
                    {
                        m_CurrentIndex = idx;
                        m_Current = enumeratorData.Enumerator.Current;
                        return true;
                    }
                    else
                    {
                        enumeratorData.Active = false;
                        m_EnumeratorsData[idx] = enumeratorData;
                        m_Remaining--;
                    }
                }
                checkedCount++;
            }
            return false;
        }

        public void Reset()
        {
            for (var i = 0; i < m_EnumeratorsData.Count; ++i)
            {
                var enumeratorData = m_EnumeratorsData[i];
                enumeratorData.Enumerator.Reset();
                enumeratorData.Active = true;
                m_EnumeratorsData[i] = enumeratorData;
            }
            m_Remaining = m_EnumeratorsData.Count;
            m_CurrentIndex = k_NoEnumerationIndex;
            m_Current = default;
        }

        int GetCurrentEnumeratorIndex()
        {
            switch (m_EnumerationStyle)
            {
                case SearchAggregateEnumerationStyle.Sequential: return m_CurrentIndex < 0 ? 0 : m_CurrentIndex;
                case SearchAggregateEnumerationStyle.RoundRobin: return (m_CurrentIndex + 1) % m_EnumeratorsData.Count;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
