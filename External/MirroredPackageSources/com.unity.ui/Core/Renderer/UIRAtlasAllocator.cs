using System;
using UnityEngine.Assertions;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    internal class UIRAtlasAllocator : IDisposable
    {
        private class Row
        {
            private static ObjectPool<Row> s_Pool = new ObjectPool<Row>();

            /// <summary>
            /// Distance from the left of the texture to left side of the row.
            /// </summary>
            public int offsetX { get; private set; }

            /// <summary>
            /// Distance between the bottom of the texture to the bottom of the row.
            /// </summary>
            public int offsetY { get; private set; }

            public int width { get; private set; }

            public int height { get; private set; }

            /// <summary>
            /// Distance between the left of the row and the horizontal insertion point.
            /// </summary>
            public int Cursor;

            public static Row Acquire(int offsetX, int offsetY, int width, int height)
            {
                Row row = s_Pool.Get();
                row.offsetX = offsetX;
                row.offsetY = offsetY;
                row.width = width;
                row.height = height;
                row.Cursor = 0;
                return row;
            }

            public void Release()
            {
                s_Pool.Release(this);
                offsetX = -1;
                offsetY = -1;
                width = -1;
                height = -1;
                Cursor = -1;
            }
        }

        private class AreaNode
        {
            private static ObjectPool<AreaNode> s_Pool = new ObjectPool<AreaNode>();

            public RectInt rect;
            public AreaNode previous;
            public AreaNode next;

            public static AreaNode Acquire(RectInt rect)
            {
                AreaNode node = s_Pool.Get();
                node.rect = rect;
                node.previous = null;
                node.next = null;
                return node;
            }

            public void Release()
            {
                s_Pool.Release(this);
            }

            public void RemoveFromChain()
            {
                if (previous != null)
                    previous.next = next;

                if (next != null)
                    next.previous = previous;

                previous = null;
                next = null;
            }

            public void AddAfter(AreaNode previous)
            {
                Assert.IsNull(this.previous);
                Assert.IsNull(next);

                this.previous = previous;

                if (previous != null)
                {
                    next = previous.next;
                    previous.next = this;
                }

                if (next != null)
                    next.previous = this;
            }
        }

        public int maxAtlasSize { get; }
        public int maxImageWidth { get; }
        public int maxImageHeight { get; }

        // ONLY POT.
        public int virtualWidth { get; private set; }
        public int virtualHeight { get; private set; }

        public int physicalWidth { get; private set; }
        public int physicalHeight { get; private set; }

        AreaNode m_FirstUnpartitionedArea;
        Row[] m_OpenRows;
        int m_1SidePadding, m_2SidePadding;

        static ProfilerMarker s_MarkerTryAllocate = new ProfilerMarker("UIRAtlasAllocator.TryAllocate");

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                for (int i = 0; i < m_OpenRows.Length; ++i)
                {
                    Row row = m_OpenRows[i];
                    if (row != null)
                        row.Release();
                }
                m_OpenRows = null;

                AreaNode area = m_FirstUnpartitionedArea;
                while (area != null)
                {
                    AreaNode temp = area.next;
                    area.Release();
                    area = temp;
                }
                m_FirstUnpartitionedArea = null;
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        private static int GetLog2OfNextPower(int n)
        {
            float next = Mathf.NextPowerOfTwo(n);
            float pow = Mathf.Log(next, 2);
            return Mathf.RoundToInt(pow);
        }

        /// <param name="initialAtlasSize">Initial size of the atlas (POT).</param>
        /// <param name="maxAtlasSize">Maximum size of the atlas (POT).</param>
        public UIRAtlasAllocator(int initialAtlasSize, int maxAtlasSize, int sidePadding = 1)
        {
            // Validate Size Coherence.
            Assert.IsTrue(initialAtlasSize > 0 && initialAtlasSize <= maxAtlasSize);

            // Validate POT
            Assert.IsTrue(initialAtlasSize == Mathf.NextPowerOfTwo(initialAtlasSize));
            Assert.IsTrue(maxAtlasSize == Mathf.NextPowerOfTwo(maxAtlasSize));

            m_1SidePadding = sidePadding;
            m_2SidePadding = sidePadding << 1;
            this.maxAtlasSize = maxAtlasSize;
            maxImageWidth = maxAtlasSize;
            maxImageHeight = (initialAtlasSize == maxAtlasSize) ? maxAtlasSize / 2 + m_2SidePadding : maxAtlasSize / 4 + m_2SidePadding;
            virtualWidth = initialAtlasSize;
            virtualHeight = initialAtlasSize;

            int maxOpenRows = GetLog2OfNextPower(maxAtlasSize) + 1;
            m_OpenRows = new Row[maxOpenRows];

            var area = new RectInt(0, 0, initialAtlasSize, initialAtlasSize);
            m_FirstUnpartitionedArea = AreaNode.Acquire(area);
            BuildAreas();
        }

        public bool TryAllocate(int width, int height, out RectInt location)
        {
            using (s_MarkerTryAllocate.Auto())
            {
                location = new RectInt();

                if (disposed)
                    return false;

                if (width < 1 || height < 1)
                    return false;

                if (width > maxImageWidth || height > maxImageHeight)
                    return false;

                // Get a row in which we will allocate.
                // Rows have height of POT+2. The reason for the +2 is that textures are generally POT, but the border adds
                // 2 pixels. Accounting for the border produced better packing because it avoids, for example, fitting a
                // 16x16 pixel (which turns into 18x18) into a row with a height of 32. So the actual heights of the rows
                // are: 3, 4, 6, 10, 18, 34, 66, etc...
                int rowIndex = GetLog2OfNextPower(Mathf.Max(height - m_2SidePadding, 1));
                int rowHeight = (1 << rowIndex) + m_2SidePadding;
                Row row = m_OpenRows[rowIndex];

                // Ensure that there is enough place
                if (row != null && row.width - row.Cursor < width)
                    row = null;

                if (row == null)
                {
                    // Attempt to partition a new row.
                    AreaNode areaNode = m_FirstUnpartitionedArea;
                    while (areaNode != null)
                    {
                        if (TryPartitionArea(areaNode, rowIndex, rowHeight, width))
                        {
                            row = m_OpenRows[rowIndex];
                            break;
                        }

                        areaNode = areaNode.next;
                    }

                    if (row == null)
                        return false;
                }

                // Allocate in the row.
                location = new RectInt(row.offsetX + row.Cursor, row.offsetY, width, height);
                row.Cursor += width;
                Assert.IsTrue(row.Cursor <= row.width);
                physicalWidth = Mathf.NextPowerOfTwo(Mathf.Max(physicalWidth, location.xMax));
                physicalHeight = Mathf.NextPowerOfTwo(Mathf.Max(physicalHeight, location.yMax));

                return true;
            }
        }

        /// <remarks>
        /// If the provided area is large enough, the lower part of the area is partitioned to host the row and the
        /// lower bound of the area moves upwards. When the area becomes empty, it is removed from the chain of
        /// unpartitioned areas to avoid spending time on it during future allocations.
        /// </remarks>
        private bool TryPartitionArea(AreaNode areaNode, int rowIndex, int rowHeight, int minWidth)
        {
            RectInt area = areaNode.rect;
            if (area.height < rowHeight || area.width < minWidth)
                return false;

            var row = m_OpenRows[rowIndex];
            if (row != null)
                row.Release();
            row = Row.Acquire(area.x, area.y, area.width, rowHeight);
            m_OpenRows[rowIndex] = row;
            area.y += rowHeight;
            area.height -= rowHeight;

            if (area.height == 0)
            {
                if (areaNode == m_FirstUnpartitionedArea)
                    m_FirstUnpartitionedArea = areaNode.next;
                areaNode.RemoveFromChain();
                areaNode.Release();
            }
            else
                areaNode.rect = area;
            return true;
        }

        /// <summary>
        /// Virtually expands the atlas from its initial size to its maximum size.
        /// </summary>
        /// <remarks>
        /// The areas are built by doubling horizontally and vertically, over and over. Consider the following example,
        /// where the initial atlas size is 64x64 and the maximum size is 256x256. The resulting 5 areas would be:
        /// 1: First area determined by the initial atlas size
        /// 2: Area generated by horizontal expansion
        /// 3: Area generated by vertical expansion
        /// 4: Area generated by horizontal expansion
        /// 5: Area generated by vertical expansion - final expansion
        ///  _______________________
        /// |                       |
        /// |                       |
        /// |           5           |
        /// |                       |
        /// |                       |
        /// |_______________________|
        /// |           |           |
        /// |     3     |           |
        /// |___________|     4     |
        /// |     |     |           |
        /// |  1  |  2  |           |
        /// |_____|_____|___________|
        ///
        /// Because of the exponential nature of this process, we don't need to worry about very large textures. For
        /// example, from 64x64 to 4096x4096, only 12 iterations are required, leading to 13 areas.
        /// </remarks>
        private void BuildAreas()
        {
            AreaNode current = m_FirstUnpartitionedArea;
            while (virtualWidth < maxAtlasSize || virtualHeight < maxAtlasSize)
            {
                RectInt newArea;
                if (virtualWidth > virtualHeight)
                {
                    // Double Vertically.
                    newArea = new RectInt(0, virtualHeight, virtualWidth, virtualHeight);
                    virtualHeight *= 2;
                }
                else
                {
                    // Double Horizontally.
                    newArea = new RectInt(virtualWidth, 0, virtualWidth, virtualHeight);
                    virtualWidth *= 2;
                }

                var newAreaNode = AreaNode.Acquire(newArea);
                newAreaNode.AddAfter(current);
                current = newAreaNode;
            }
        }
    }
}
