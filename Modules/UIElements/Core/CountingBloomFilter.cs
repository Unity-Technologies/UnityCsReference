// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
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

        private bool IsSlotEmpty(uint index)
        {
            return m_Counters[index] == 0;
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

        public bool ContainsHash(uint hash)
        {
            return !IsSlotEmpty(Hash1(hash)) && !IsSlotEmpty(Hash2(hash));
        }
    }

    class AncestorFilter
    {
        CountingBloomFilter m_CountingBloomFilter;

        Stack<int> m_HashStack = new Stack<int>(100);

        public AncestorFilter() {}

        private void AddHash(int hash)
        {
            m_HashStack.Push(hash);
            m_CountingBloomFilter.InsertHash((uint)hash);
        }

        unsafe public bool IsCandidate(StyleComplexSelector complexSel)
        {
            // We traverse the hash values for the complex selector parts to detect if any part isn't found
            // in the Bloom filter, in which case the selector is not a candidate for the exhaustive search.
            // Also, if a value of 0 is found during the search, then all parts have been visited without a
            // Bloom filter rejection, in which case we must proceed with the exhaustive search.
            for (int i = 0; i < Hashes.kSize; i++)
            {
                // A default part hash value means that all parts have been visited without a Bloom filter rejection.
                if (complexSel.ancestorHashes.hashes[i] == 0)
                    return true;

                // A negative search in the Bloom filter means that the exhaustive search can be skipped for this selector.
                if (!m_CountingBloomFilter.ContainsHash((uint)complexSel.ancestorHashes.hashes[i]))
                    return false;
            }

            return true;
        }

        // Overload for flattened data path (takes ancestor hashes directly)
        unsafe public bool IsCandidate(int* ancestorHashes)
        {
            for (int i = 0; i < Hashes.kSize; i++)
            {
                if (ancestorHashes[i] == 0)
                    return true;

                if (!m_CountingBloomFilter.ContainsHash((uint)ancestorHashes[i]))
                    return false;
            }

            return true;
        }

        public void PushElement(VisualElement element)
        {
            int rememberCount = m_HashStack.Count;

            // Use cached type ID with bit mixing to distribute bits across all 32 bits
            // This ensures Hash2 (upper 14 bits) is non-zero, preventing slot 0 collisions
            AddHash(Hashes.MixBits(element.typeNameId) * (int)Salt.TagNameSalt);

            // Use cached name ID if available
            // nameId >= 0 means valid ID (negative values are sentinels: not initialized or not found)
            int nameId = element.nameId;
            if (nameId >= 0)
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
