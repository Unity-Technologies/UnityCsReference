// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace Unity.Properties.Internal
{
    readonly struct ReadOnlyAdapterCollection
    {
        public struct Enumerator
        {
            List<IPropertyVisitorAdapter> m_Adapters;
            int m_Index;
            public IPropertyVisitorAdapter Current { get; private set; }

            public Enumerator(ReadOnlyAdapterCollection collection)
            {
                m_Adapters = collection.m_Adapters;
                m_Index = 0;
                Current = default;
            }

            public bool MoveNext()
            {
                if (null == m_Adapters)
                    return false;

                if (m_Index >= m_Adapters.Count)
                    return false;

                Current = m_Adapters[m_Index];
                ++m_Index;
                return true;
            }

            void Reset()
            {
                m_Index = 0;
                Current = default;
            }
        }

        readonly List<IPropertyVisitorAdapter> m_Adapters;

        public ReadOnlyAdapterCollection(List<IPropertyVisitorAdapter> adapters)
        {
            m_Adapters = adapters;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
    }
}
