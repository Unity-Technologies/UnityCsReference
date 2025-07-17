// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.UIElements.HierarchyV2
{
    internal sealed class CollectionViewSelection
    {
        readonly HashSet<int> m_IndexLookup = new();

        // We cache the min/max index
        int m_MinIndex = -1;
        int m_MaxIndex = -1;

        public readonly List<int> indices = new();
        public int indexCount => indices.Count;

        public int minIndex
        {
            get
            {
                if (m_MinIndex == -1)
                {
                    m_MinIndex = int.MaxValue;
                    foreach (var index in indices)
                    {
                        if (index < m_MinIndex)
                            m_MinIndex = index;
                    }
                }
                return m_MinIndex;
            }
        }

        public int maxIndex
        {
            get
            {
                if (m_MaxIndex == -1)
                {
                    foreach(var index in indices)
                    {
                        if (index > m_MaxIndex)
                            m_MaxIndex = index;
                    }
                }

                return m_MaxIndex;
            }
        }

        public int capacity
        {
            get => indices.Capacity;
            set => indices.Capacity = value;
        }

        public int FirstIndex() => indices.Count > 0 ? indices[0] : -1;

        public bool ContainsIndex(int index) => m_IndexLookup.Contains(index);

        public void AddIndex(int index)
        {
            m_IndexLookup.Add(index);
            indices.Add(index);

            if (index < m_MinIndex)
                m_MinIndex = index;

            if (index > m_MaxIndex)
                m_MaxIndex = index;
        }

        public bool TryRemove(int index)
        {
            if (!m_IndexLookup.Remove(index))
                return false;

            var i = indices.IndexOf(index);
            if (i >= 0)
            {
                indices.RemoveAt(i);

                if (index == m_MinIndex)
                    m_MinIndex = -1;
                if (index == m_MaxIndex)
                    m_MaxIndex = -1;
            }

            return true;
        }

        public void ClearIndices()
        {
            m_IndexLookup.Clear();
            indices.Clear();
            m_MinIndex = -1;
            m_MaxIndex = -1;
        }
    }
}
