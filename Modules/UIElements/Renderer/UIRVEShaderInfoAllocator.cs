// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    internal struct UIRVEShaderInfoAllocator
    {
        internal const int kPageWidth = 32; // Must match bit count of the type of m_AllocMap
        internal const int kPageHeight = 16;
        struct Page
        {
            public UInt16 x, y; // Location of this page in the atlas. These coordinates are the top-left corner of the page.
        }

        List<Page> m_Pages;
        List<UInt32> m_AllocMap; // Each page takes kPageHeight sequential entries/lines in this array, 0 is allocated, 1 is available

        public struct Allocation
        {
            public bool Equals(Allocation other) { return line == other.line && bitIndex == other.bitIndex; }
            public bool IsValid() { return line >= 0; }

            public int line;
            public byte bitIndex;
            public byte owned;
            public ushort x, y;
        }

        public void Construct()
        {
            m_Pages = new List<Page>(1);
            m_AllocMap = new List<UInt32>(kPageHeight * m_Pages.Capacity);
        }

        public Allocation Allocate(UIRAtlasManager atlasManager)
        {
            if (atlasManager == null)
                return new Allocation() { line = -1 }; // Not possible

            int linesAvailable = m_AllocMap.Count;
            for (int i = 0; i < linesAvailable; i++)
            {
                var allocBits = m_AllocMap[i];
                if (allocBits == 0)
                    continue;
                byte allocIndex = CountTrailingZeroes(allocBits);
                m_AllocMap[i] = allocBits & (~(1U << allocIndex));
                int pageIndex = i / kPageHeight;
                var pageInfo = m_Pages[pageIndex];
                return new Allocation() { line = i, bitIndex = allocIndex, x = (UInt16)(allocIndex + pageInfo.x), y = (UInt16)(i - pageIndex * kPageHeight + pageInfo.y), owned = 1 };
            }

            RectInt uvRect;
            if (!atlasManager.AllocateRect(kPageWidth, kPageHeight, out uvRect))
                return new Allocation() { line = -1 }; // Failed

            var alloc = new Allocation() { line = m_AllocMap.Count, bitIndex = 0, x = 0, y = 0, owned = 1 };
            m_AllocMap.Capacity += kPageHeight;
            m_AllocMap.Add(0xFFFFFFFE); // Reserve first slot
            for (int i = 1; i < kPageHeight; i++)
                m_AllocMap.Add(0xFFFFFFFF);

            var newPage = new Page();
            newPage.x = (UInt16)uvRect.xMin;
            newPage.y = (UInt16)uvRect.yMin;

            m_Pages.Add(newPage);
            alloc.x += newPage.x;
            alloc.y += newPage.y;
            return alloc;
        }

        public void Free(Allocation alloc)
        {
            Debug.Assert(alloc.owned == 1);
            m_AllocMap[alloc.line] = m_AllocMap[alloc.line] | (1U << alloc.bitIndex);
        }

        static byte CountTrailingZeroes(UInt32 val)
        {
            // This entire function is implemented on hardware in one instruction... sigh
            byte trailingZeroes = 0;
            if ((val & 0xFFFF) == 0)
            {
                val >>= 16;
                trailingZeroes = 16;
            }

            if ((val & 0xFF) == 0)
            {
                val >>= 8;
                trailingZeroes += 8;
            }

            if ((val & 0xF) == 0)
            {
                val >>= 4;
                trailingZeroes += 4;
            }

            if ((val & 3) == 0)
            {
                val >>= 2;
                trailingZeroes += 2;
            }

            if ((val & 1) == 0)
                trailingZeroes += 1;

            return trailingZeroes;
        }
    }
}
