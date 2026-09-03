// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    // Layout and hash constants must remain in sync with the read-only C++ mirror in
    // Modules/UIElements/Core/Native/StyleSheets/CountingBloomFilter.h
    unsafe struct CountingBloomFilter
    {
        private const int KEY_SIZE = 14;
        private const uint ARRAY_SIZE = 1 << KEY_SIZE;
        private const int KEY_MASK = (1 << KEY_SIZE) - 1;

        private fixed byte m_Counters[(int)ARRAY_SIZE];

        private void AdjustSlot(uint index, bool increment)
        {
            if (increment)
            {
                if (m_Counters[index] != 0xff) // once a slot is full, it can't be increased anymore
                    m_Counters[index]++;
            }
            else
            {
                if (m_Counters[index] != 0x00) // once a slot is empty, it can't be decreased anymore
                    m_Counters[index]--;
            }
        }

        private uint Hash1(uint hash)
        {
            return hash & KEY_MASK;
        }

        private uint Hash2(uint hash)
        {
            return (hash >> KEY_SIZE) & KEY_MASK;
        }

        public void InsertHash(uint hash)
        {
            AdjustSlot(Hash1(hash), true);
            AdjustSlot(Hash2(hash), true);
        }

        public void RemoveHash(uint hash)
        {
            AdjustSlot(Hash1(hash), false);
            AdjustSlot(Hash2(hash), false);
        }
    }

    class AncestorFilter
    {
        CountingBloomFilter m_CountingBloomFilter;

        // The native matcher reads the filter in place (ContainsHash only); exposed by ref so
        // the caller can pin it for the duration of the call. Push/pop maintenance stays managed.
        internal ref CountingBloomFilter filter => ref m_CountingBloomFilter;

        Stack<int> m_HashStack = new Stack<int>(100);

        public AncestorFilter() {}

        private void AddHash(int hash)
        {
            m_HashStack.Push(hash);
            m_CountingBloomFilter.InsertHash((uint)hash);
        }

        public void PushElement(VisualElement element)
        {
            int rememberCount = m_HashStack.Count;

            // Use cached type ID with bit mixing to distribute bits across all 32 bits
            // This ensures Hash2 (upper 14 bits) is non-zero, preventing slot 0 collisions
            AddHash(Hashes.MixBits(element.typeNameId) * (int)Salt.TagNameSalt);

            // Only add the name hash when the element has a non-empty, non-null name.
            int nameId = element.nameId;
            if (!UniqueStyleString.IsNullOrEmpty(nameId))
                AddHash(Hashes.MixBits(nameId) * (int)Salt.IdSalt);

            var classList = element.GetClassesForIteration();
            var classIds = classList.GetClassIds();
            for (int i = 0; i < classIds.Length; i++)
            {
                // Classes use UniqueStyleString IDs with bit mixing
                AddHash(Hashes.MixBits(classIds[i]) * (int)Salt.ClassSalt);
            }

            m_HashStack.Push(m_HashStack.Count - rememberCount);
        }

        public void PopElement()
        {
            int elemCount = m_HashStack.Peek();
            m_HashStack.Pop();
            while (elemCount > 0)
            {
                int hash = m_HashStack.Peek();
                m_CountingBloomFilter.RemoveHash((uint)hash);
                m_HashStack.Pop();
                elemCount--;
            }
        }
    }
}
