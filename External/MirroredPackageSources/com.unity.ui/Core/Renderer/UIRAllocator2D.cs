using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnityEngine.UIElements.UIR
{
    class Allocator2D
    {
        readonly Vector2Int m_MinSize, m_MaxSize, m_MaxAllocSize;
        readonly int m_RowHeightBias; // Rows have a height of PoT + Bias
        readonly Row[] m_Rows;
        readonly List<Area> m_Areas = new List<Area>();

        public class Area
        {
            public RectInt rect;
            public BestFitAllocator allocator; // Allocates vertically within the area

            public Area(RectInt rect)
            {
                this.rect = rect;
                allocator = new BestFitAllocator((uint)rect.height);
            }
        }

        public class Row : LinkedPoolItem<Row>
        {
            public RectInt rect;
            public Area area;
            public BestFitAllocator allocator; // Allocates horizontally within the row
            public Alloc alloc; // Provided by the area
            public Row next; // The next row MUST have the same height

            public static readonly LinkedPool<Row> pool = new LinkedPool<Row>(Create, Reset, 256);

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            static Row Create() => new Row();

            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            static void Reset(Row row)
            {
                row.rect = new RectInt();
                row.area = null;
                row.allocator = null;
                row.alloc = new Alloc();
                row.next = null;
            }
        }

        public struct Alloc2D
        {
            public RectInt rect;
            public Row row;
            public Alloc alloc; // Provided by the row

            public Alloc2D(Row row, Alloc alloc, int width, int height)
            {
                this.alloc = alloc;
                this.row = row;
                rect = new RectInt(
                    row.rect.xMin + (int)alloc.start,
                    row.rect.yMin,
                    width,
                    height);
            }
        }

        public Vector2Int minSize => m_MinSize;
        public Vector2Int maxSize => m_MaxSize;
        public Vector2Int maxAllocSize => m_MaxAllocSize;

        public Allocator2D(int minSize, int maxSize, int rowHeightBias)
            : this(new Vector2Int(minSize, minSize), new Vector2Int(maxSize, maxSize), rowHeightBias) {}

        // minSize and maxSize xy must be POT (256x256 and 2048x1024 are two valid examples)
        public Allocator2D(Vector2Int minSize, Vector2Int maxSize, int rowHeightBias)
        {
            Debug.Assert(
                minSize.x > 0 && minSize.x <= maxSize.x &&
                minSize.y > 0 && minSize.y <= maxSize.y);
            Debug.Assert(
                minSize.x == UIRUtility.GetNextPow2(minSize.x) &&
                minSize.y == UIRUtility.GetNextPow2(minSize.y) &&
                maxSize.x == UIRUtility.GetNextPow2(maxSize.x) &&
                maxSize.y == UIRUtility.GetNextPow2(maxSize.y));
            Debug.Assert(rowHeightBias >= 0);

            m_MinSize = minSize;
            m_MaxSize = maxSize;
            m_RowHeightBias = rowHeightBias;

            BuildAreas(m_Areas, minSize, maxSize);
            m_MaxAllocSize = ComputeMaxAllocSize(m_Areas, rowHeightBias);
            m_Rows = BuildRowArray(m_MaxAllocSize.y, rowHeightBias);
        }

        public bool TryAllocate(int width, int height, out Alloc2D alloc2D)
        {
            if (width < 1 || width > m_MaxAllocSize.x ||
                height < 1 || height > m_MaxAllocSize.y)
            {
                alloc2D = new Alloc2D();
                return false;
            }

            int n = UIRUtility.GetNextPow2Exp(Mathf.Max(height - m_RowHeightBias, 1));

            // Try to allocate from existing rows.
            Row row = m_Rows[n];
            while (row != null)
            {
                if (row.rect.width >= width)
                {
                    Alloc alloc = row.allocator.Allocate((uint)width);
                    if (alloc.size > 0)
                    {
                        alloc2D = new Alloc2D(row, alloc, width, height);
                        return true;
                    }
                }

                row = row.next;
            }

            // Try to open a new row.
            int rowHeight = (1 << n) + m_RowHeightBias;
            Debug.Assert(rowHeight >= height);
            for (int i = 0; i < m_Areas.Count; ++i)
            {
                Area area = m_Areas[i];
                if (area.rect.height >= rowHeight && area.rect.width >= width)
                {
                    Alloc rowAlloc = area.allocator.Allocate((uint)rowHeight);
                    if (rowAlloc.size > 0)
                    {
                        row = Row.pool.Get();
                        row.alloc = rowAlloc;
                        row.allocator = new BestFitAllocator((uint)area.rect.width);
                        row.area = area;
                        row.next = m_Rows[n];
                        row.rect = new RectInt(area.rect.xMin, area.rect.yMin + (int)rowAlloc.start, area.rect.width, rowHeight);
                        m_Rows[n] = row;
                        Alloc alloc = row.allocator.Allocate((uint)width);
                        Debug.Assert(alloc.size > 0);
                        alloc2D = new Alloc2D(row, alloc, width, height);
                        return true;
                    }
                }
            }

            alloc2D = new Alloc2D();
            return false;
        }

        public void Free(Alloc2D alloc2D)
        {
            if (alloc2D.alloc.size == 0)
                return;

            // Free the alloc2D in the row
            Row row = alloc2D.row;
            row.allocator.Free(alloc2D.alloc);
            if (row.allocator.highWatermark == 0)
            {
                // Free the row in the area
                row.area.allocator.Free(row.alloc);
                int n = UIRUtility.GetNextPow2Exp(row.rect.height - m_RowHeightBias);
                Row first = m_Rows[n];
                if (first == row)
                    m_Rows[n] = row.next;
                else
                {
                    Row prev = first;
                    while (prev.next != row)
                        prev = prev.next;
                    prev.next = row.next;
                }

                Row.pool.Return(row);
            }
        }

        /// <summary>
        /// Expands the atlas from its initial size to its maximum size.
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
        static void BuildAreas(List<Area> areas, Vector2Int minSize, Vector2Int maxSize)
        {
            int xMax = Mathf.Min(minSize.x, minSize.y);
            int yMax = xMax;
            areas.Add(new Area(new RectInt(0, 0, xMax, yMax)));

            while (xMax < maxSize.x || yMax < maxSize.y)
            {
                // Double Horizontally
                if (xMax < maxSize.x)
                {
                    areas.Add(new Area(new RectInt(xMax, 0, xMax, yMax)));
                    xMax *= 2;
                }

                // Double Vertically
                if (yMax < maxSize.y)
                {
                    areas.Add(new Area(new RectInt(0, yMax, xMax, yMax)));
                    yMax *= 2;
                }
            }
        }

        static Vector2Int ComputeMaxAllocSize(List<Area> areas, int rowHeightBias)
        {
            int maxWidth = 0;
            int maxHeight = 0;
            for (int i = 0; i < areas.Count; ++i)
            {
                var area = areas[i];
                maxWidth = Mathf.Max(area.rect.width, maxWidth);
                maxHeight = Mathf.Max(area.rect.height, maxHeight);
            }

            return new Vector2Int(maxWidth, UIRUtility.GetPrevPow2(maxHeight - rowHeightBias) + rowHeightBias);
        }

        static Row[] BuildRowArray(int maxRowHeight, int rowHeightBias)
        {
            int n = UIRUtility.GetNextPow2Exp(maxRowHeight - rowHeightBias) + 1;
            return new Row[n];
        }
    }
}
