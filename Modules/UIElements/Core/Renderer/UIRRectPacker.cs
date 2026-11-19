// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements.UIR
{
    // Packs rectangles into a fixed area that may already hold obstacles (regions that must not move), using a list of
    // maximal free rectangles (MaxRects) and the best short side fit. There is no Free: to reuse space, Reset and add
    // the regions that are still alive as obstacles.
    class RectPacker
    {
        // Edges rather than RectInt, whose min and max are computed on every read: the loops below compare them a lot
        struct Box
        {
            public int xMin;
            public int yMin;
            public int xMax;
            public int yMax;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Box(int x, int y, int width, int height)
            {
                xMin = x;
                yMin = y;
                xMax = x + width;
                yMax = y + height;
            }

            public int width
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => xMax - xMin;
            }

            public int height
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => yMax - yMin;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Overlaps(in Box other) => other.xMin < xMax && other.xMax > xMin && other.yMin < yMax && other.yMax > yMin;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Contains(in Box inner) => inner.xMin >= xMin && inner.yMin >= yMin && inner.xMax <= xMax && inner.yMax <= yMax;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(in Box other) => xMin == other.xMin && yMin == other.yMin && xMax == other.xMax && yMax == other.yMax;
        }

        Box[] m_FreeRects = new Box[16];
        int m_FreeCount;
        Box[] m_SplitRects = new Box[16];
        int m_SplitCount;
        int m_Width;
        int m_Height;

        public int width => m_Width;
        public int height => m_Height;

        public void Reset(int width, int height)
        {
            m_Width = width;
            m_Height = height;
            m_FreeCount = 0;
            if (width > 0 && height > 0)
                AddFree(new Box(0, 0, width, height));
        }

        // The obstacle may extend past the area and may overlap other obstacles or allocations
        public void AddObstacle(RectInt rect)
        {
            if (rect.width > 0 && rect.height > 0)
                Occupy(new Box(rect.x, rect.y, rect.width, rect.height));
        }

        public bool TryAllocate(int width, int height, out RectInt rect)
        {
            rect = default;
            if (width <= 0 || height <= 0)
                return false;

            int best = -1;
            int bestShortSide = int.MaxValue;
            int bestLongSide = int.MaxValue;
            for (int i = 0; i < m_FreeCount; ++i)
            {
                ref var free = ref m_FreeRects[i];
                int leftoverX = free.width - width;
                int leftoverY = free.height - height;
                if (leftoverX < 0 || leftoverY < 0)
                    continue;

                int shortSide = Math.Min(leftoverX, leftoverY);
                int longSide = Math.Max(leftoverX, leftoverY);
                if (shortSide < bestShortSide || (shortSide == bestShortSide && longSide < bestLongSide))
                {
                    bestShortSide = shortSide;
                    bestLongSide = longSide;
                    best = i;
                }
            }

            if (best < 0)
                return false;

            var used = new Box(m_FreeRects[best].xMin, m_FreeRects[best].yMin, width, height);
            Occupy(used);
            rect = new RectInt(used.xMin, used.yMin, width, height);
            return true;
        }

        void Occupy(in Box used)
        {
            m_SplitCount = 0;
            for (int i = m_FreeCount - 1; i >= 0; --i)
            {
                var free = m_FreeRects[i];
                if (!free.Overlaps(used))
                    continue;

                m_FreeRects[i] = m_FreeRects[--m_FreeCount];

                if (used.xMin > free.xMin)
                    AddSplit(new Box(free.xMin, free.yMin, used.xMin - free.xMin, free.height));
                if (used.xMax < free.xMax)
                    AddSplit(new Box(used.xMax, free.yMin, free.xMax - used.xMax, free.height));
                if (used.yMin > free.yMin)
                    AddSplit(new Box(free.xMin, free.yMin, free.width, used.yMin - free.yMin));
                if (used.yMax < free.yMax)
                    AddSplit(new Box(free.xMin, used.yMax, free.width, free.yMax - used.yMax));
            }

            // A rect that wasn't split was already maximal, so it can't lie inside a split rect: only splits are pruned
            int unsplitCount = m_FreeCount;
            for (int i = 0; i < m_SplitCount; ++i)
            {
                if (!IsContainedInFreeRect(i, unsplitCount) && !IsContainedInOtherSplit(i))
                    AddFree(m_SplitRects[i]);
            }
        }

        bool IsContainedInFreeRect(int splitIndex, int count)
        {
            ref var rect = ref m_SplitRects[splitIndex];
            for (int i = 0; i < count; ++i)
            {
                if (m_FreeRects[i].Contains(rect))
                    return true;
            }
            return false;
        }

        // Of two equal splits, only the first is kept
        bool IsContainedInOtherSplit(int index)
        {
            ref var rect = ref m_SplitRects[index];
            for (int i = 0; i < m_SplitCount; ++i)
            {
                if (i == index)
                    continue;
                ref var other = ref m_SplitRects[i];
                if (other.Contains(rect) && (i < index || !other.Equals(rect)))
                    return true;
            }
            return false;
        }

        void AddFree(in Box box)
        {
            if (m_FreeCount == m_FreeRects.Length)
                Array.Resize(ref m_FreeRects, m_FreeCount * 2);
            m_FreeRects[m_FreeCount++] = box;
        }

        void AddSplit(in Box box)
        {
            if (m_SplitCount == m_SplitRects.Length)
                Array.Resize(ref m_SplitRects, m_SplitCount * 2);
            m_SplitRects[m_SplitCount++] = box;
        }
    }
}
