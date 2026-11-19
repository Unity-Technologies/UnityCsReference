// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements.UIR
{
    // Packs rectangles in rows (shelves) stacked from the top. Each allocation costs one pass over the rows, whatever the
    // mix of sizes, and the rows stay tight when the rectangles arrive tallest first. There are no obstacles and no Free:
    // to reuse space, Reset.
    class ShelfPacker
    {
        struct Shelf
        {
            public int y;
            public int height;
            public int usedWidth;
        }

        Shelf[] m_Shelves = new Shelf[16];
        int m_ShelfCount;
        int m_Width;
        int m_Height;
        int m_UsedHeight;

        public int width => m_Width;
        public int height => m_Height;

        public void Reset(int width, int height)
        {
            m_Width = width;
            m_Height = height;
            m_ShelfCount = 0;
            m_UsedHeight = 0;
        }

        public bool TryAllocate(int width, int height, out RectInt rect)
        {
            rect = default;
            if (width <= 0 || height <= 0 || width > m_Width)
                return false;

            int best = -1;
            int bestWaste = int.MaxValue;
            for (int i = 0; i < m_ShelfCount; ++i)
            {
                ref var shelf = ref m_Shelves[i];
                if (shelf.height < height || m_Width - shelf.usedWidth < width)
                    continue;

                int waste = shelf.height - height;
                if (waste < bestWaste)
                {
                    best = i;
                    bestWaste = waste;
                    if (waste == 0)
                        break;
                }
            }

            // The space above the rectangle is lost: a much taller row is only used when no new row fits
            bool canAddShelf = m_Height - m_UsedHeight >= height;
            if (best < 0 || (bestWaste > height / 2 && canAddShelf))
            {
                if (!canAddShelf)
                    return false;
                best = AddShelf(height);
            }

            ref var target = ref m_Shelves[best];
            rect = new RectInt(target.usedWidth, target.y, width, height);
            target.usedWidth += width;
            return true;
        }

        int AddShelf(int height)
        {
            if (m_ShelfCount == m_Shelves.Length)
                Array.Resize(ref m_Shelves, m_ShelfCount * 2);
            m_Shelves[m_ShelfCount] = new Shelf { y = m_UsedHeight, height = height };
            m_UsedHeight += height;
            return m_ShelfCount++;
        }
    }
}
