using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.UIElements.UIR
{
    internal struct Alloc
    {
        public uint start, size;
        internal object handle;
        internal bool shortLived;
    }

    internal struct HeapStatistics
    {
        public uint numAllocs;
        public uint totalSize;
        public uint allocatedSize;
        public uint freeSize;
        public uint largestAvailableBlock;
        public uint availableBlocksCount;
        public uint blockCount;
        public uint highWatermark;
        public float fragmentation;
        public HeapStatistics[] subAllocators;
    }

    #region Internals
    class PoolItem { internal PoolItem poolNext; }
    class Pool<T> where T : PoolItem, new()
    {
        public T Get()
        {
            if (m_Pool == null)
                return new T();

            Debug.Assert(m_Pool != null);
            T block = (T)m_Pool;
            m_Pool = m_Pool.poolNext;
            block.poolNext = null;
            return block;
        }

        public void Return(T obj) { obj.poolNext = m_Pool; m_Pool = obj; }

        PoolItem m_Pool;
    }

    // Can handle up to 4GB due to uint, UIntPtr is just unusable
    // Maintains its available memory range to the best, giving practically a fast allocation time
    // but O(n) free time where n is a factor of fragmentation (number of available blocks)
    // It always coalesces free blocks on every free operation
    // For index buffers, go with immediate coalescing, but for vertex buffers we can
    // be very lazy and don't aim for adjacency or care for internal fragmentation much.
    // Instead we should improve allocation/free time more because vertex buffer content
    // will update often
    class BestFitAllocator
    {
        public BestFitAllocator(uint size)
        {
            totalSize = size;
            m_FirstBlock = m_FirstAvailableBlock = m_BlockPool.Get();
            m_FirstAvailableBlock.end = size;
        }

        public uint totalSize { get; }
        public uint highWatermark { get { return m_HighWatermark; } }
        public Alloc Allocate(uint size)
        {
            // Choose an allocation policy from below, each has performance/vs/fragmentation balance characteristics,
            // but remember to rename the class to reflect the policy chosen
            // All are O(n) where is n is fragmentation (number of available blocks)
            Block block = BestFitFindAvailableBlock(size);
            if (block == null)
                return new Alloc(); // Or throw an exception?

            Debug.Assert(block.size >= size);
            Debug.Assert(!block.allocated);

            if (size != block.size)
                SplitBlock(block, size);

            Debug.Assert(block.size == size);

            if (block.end > m_HighWatermark)
                m_HighWatermark = block.end;

            // Disconnect the block from the list of available blocks
            if (block == m_FirstAvailableBlock)
                m_FirstAvailableBlock = m_FirstAvailableBlock.nextAvailable;
            if (block.prevAvailable != null)
                block.prevAvailable.nextAvailable = block.nextAvailable;
            if (block.nextAvailable != null)
                block.nextAvailable.prevAvailable = block.prevAvailable;

            block.allocated = true;
            block.prevAvailable = block.nextAvailable = null;

            return new Alloc() { start = block.start, size = block.size, handle = block };
        }

        public void Free(Alloc alloc)
        {
            Block block = (Block)alloc.handle;

            if (!block.allocated)
            {
                Debug.Assert(false, "Severe error: UIR allocation double-free");
                return;
            }

            Debug.Assert(block.allocated);
            Debug.Assert(block.start == alloc.start);
            Debug.Assert(block.size == alloc.size);

            if (block.end == m_HighWatermark)
            {
                if (block.prev != null)
                    m_HighWatermark = block.prev.allocated ? block.prev.end : block.prev.start; // If the previous block is not allocated, then its start marks the high watermark (which can be 0 or the end of the previous allocated block)
                else m_HighWatermark = 0; // We're the first block and owning the high watermark and we got freed
            }

            block.allocated = false;

            // Scan availables to find the correct placement for us among the availables list
            // This is the loop that potentially makes this Free operation expensive
            Block availableIt = m_FirstAvailableBlock, availableBefore = null;
            while (availableIt != null && (availableIt.start < block.start))
            {
                availableBefore = availableIt;
                availableIt = availableIt.nextAvailable;
            }

            if (availableBefore == null)
            {
                Debug.Assert(block.prevAvailable == null);
                block.nextAvailable = m_FirstAvailableBlock;
                m_FirstAvailableBlock = block;
            }
            else
            {
                block.prevAvailable = availableBefore;
                block.nextAvailable = availableBefore.nextAvailable;
                availableBefore.nextAvailable = block;
            }
            if (block.nextAvailable != null)
                block.nextAvailable.prevAvailable = block;

            // Coalesce if possible
            if (block.prevAvailable == block.prev && block.prev != null)
                block = CoalesceBlockWithPrevious(block);
            if (block.nextAvailable == block.next && block.next != null)
                block = CoalesceBlockWithPrevious(block.next);
        }

        /// <summary>
        /// Coalesces the provided block with the previous block by expanding the previous block and discarding the
        /// provided block.
        /// </summary>
        /// <remarks>
        /// By keeping the first block, we don't need to change <c>firstAvailableBlock</c>. We need to keep this in
        /// mind if we ever to go the other way around.
        /// </remarks>
        private Block CoalesceBlockWithPrevious(Block block)
        {
            Debug.Assert(block.prevAvailable.end == block.start);
            Debug.Assert(block.prev.nextAvailable == block);
            Block prev = block.prev;
            prev.next = block.next;
            if (block.next != null)
                block.next.prev = prev;

            prev.nextAvailable = block.nextAvailable;
            if (block.nextAvailable != null)
                block.nextAvailable.prevAvailable = block.prevAvailable;
            prev.end = block.end;
            m_BlockPool.Return(block);
            return prev;
        }

        internal HeapStatistics GatherStatistics()
        {
            HeapStatistics stats = new HeapStatistics();

            Block block = m_FirstBlock;
            while (block != null)
            {
                if (block.allocated)
                {
                    stats.numAllocs++;
                    stats.allocatedSize += block.size;
                }
                else
                {
                    stats.freeSize += block.size;
                    stats.availableBlocksCount++;
                    stats.largestAvailableBlock = Math.Max(stats.largestAvailableBlock, block.size);
                }
                stats.blockCount++;
                block = block.next;
            }
            stats.totalSize = totalSize;
            stats.highWatermark = m_HighWatermark;
            if (stats.freeSize > 0)
                stats.fragmentation = (float)((double)(stats.freeSize - stats.largestAvailableBlock) / (double)stats.freeSize) * 100.0f;

            return stats;
        }

        #region Allocation policies
        Block BestFitFindAvailableBlock(uint size)
        {
            Block availableBlock = m_FirstAvailableBlock;
            Block bestFit = null;
            uint bestFitBlockSize = ~((uint)0);
            while (availableBlock != null)
            {
                if ((availableBlock.size >= size) && (bestFitBlockSize > availableBlock.size))
                {
                    bestFit = availableBlock;
                    bestFitBlockSize = availableBlock.size;
                }
                availableBlock = availableBlock.nextAvailable;
            }
            return bestFit;
        }

        #endregion

        void SplitBlock(Block block, uint size)
        {
            Debug.Assert(block.size > size);

            Block after = m_BlockPool.Get();


            after.next = block.next;
            after.nextAvailable = block.nextAvailable;
            after.prev = block;
            after.prevAvailable = block;
            after.start = block.start + size;
            after.end = block.end;

            if (after.next != null)
                after.next.prev = after;
            if (after.nextAvailable != null)
                after.nextAvailable.prevAvailable = after;

            block.next = after;
            block.nextAvailable = after;
            block.end = after.start;
        }

        class Block : PoolItem
        {
            public uint size { get { return end - start; } }
            public uint start, end; // end is exclusive
            public Block prev, next;
            public Block prevAvailable, nextAvailable; // Only valid when this block is not allocated
            public bool allocated;
        }

        Block m_FirstBlock, m_FirstAvailableBlock; // Sorted by address
        Pool<Block> m_BlockPool = new Pool<Block>();
        uint m_HighWatermark;
    }

    // The GPU buffer allocator supports allocations with 2 different types of lifetimes: permanent and temp
    // Permanent allocations grow from bottom to top, while temp grow from top to bottom
    class GPUBufferAllocator
    {
        BestFitAllocator m_Low, m_High;

        public GPUBufferAllocator(uint maxSize)
        {
            m_Low = new BestFitAllocator(maxSize);
            m_High = new BestFitAllocator(maxSize);
        }

        public Alloc Allocate(uint size, bool shortLived)
        {
            Alloc alloc;
            if (!shortLived)
                alloc = m_Low.Allocate(size);
            else
            {
                alloc = m_High.Allocate(size);
                alloc.start = m_High.totalSize - alloc.start - alloc.size; // Flip the address since we start from the high end
            }

            alloc.shortLived = shortLived;

            if (HighLowCollide() && alloc.size != 0)
            {
                Free(alloc);
                return new Alloc(); // OOM
            }

            return alloc;
        }

        public void Free(Alloc alloc)
        {
            if (!alloc.shortLived)
                m_Low.Free(alloc);
            else
            {
                alloc.start = m_High.totalSize - alloc.start - alloc.size; // Fix the flipped address by flipping it again
                m_High.Free(alloc);
            }
        }

        public bool isEmpty { get { return m_Low.highWatermark == 0 && m_High.highWatermark == 0; } }

        public HeapStatistics GatherStatistics()
        {
            HeapStatistics stats = new HeapStatistics();
            stats.subAllocators = new HeapStatistics[] { m_Low.GatherStatistics(), m_High.GatherStatistics() };

            stats.largestAvailableBlock = uint.MaxValue;
            for (int i = 0; i < 2; i++)
            {
                stats.numAllocs += stats.subAllocators[i].numAllocs;
                stats.totalSize = Math.Max(stats.totalSize, stats.subAllocators[i].totalSize);
                stats.allocatedSize += stats.subAllocators[i].allocatedSize;
                stats.largestAvailableBlock = Math.Min(stats.largestAvailableBlock, stats.subAllocators[i].largestAvailableBlock);
                stats.availableBlocksCount += stats.subAllocators[i].availableBlocksCount;
                stats.blockCount += stats.subAllocators[i].blockCount;
                stats.highWatermark = Math.Max(stats.highWatermark, stats.subAllocators[i].highWatermark);
                stats.fragmentation = Math.Max(stats.fragmentation, stats.subAllocators[i].fragmentation);
            }
            stats.freeSize = stats.totalSize - stats.allocatedSize;
            return stats;
        }

        bool HighLowCollide()
        {
            return (m_Low.highWatermark + m_High.highWatermark > m_Low.totalSize);
        }
    }

    internal unsafe class Page : IDisposable
    {
        public Page(uint vertexMaxCount, uint indexMaxCount, uint maxQueuedFrameCount, bool mockPage)
        {
            vertexMaxCount = Math.Min(vertexMaxCount, 1 << 16); // Because we use UInt16 as the index type
            vertices = new DataSet<Vertex>(Utility.GPUBufferType.Vertex, vertexMaxCount, maxQueuedFrameCount, 32, mockPage);
            indices = new DataSet<UInt16>(Utility.GPUBufferType.Index, indexMaxCount, maxQueuedFrameCount, 32, mockPage);
        }

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
                indices.Dispose();
                vertices.Dispose();
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        public bool isEmpty { get { return vertices.allocator.isEmpty && indices.allocator.isEmpty; } }

        public class DataSet<T> : IDisposable where T : struct
        {
            public DataSet(Utility.GPUBufferType bufferType, uint totalCount, uint maxQueuedFrameCount, uint updateRangePoolSize, bool mockBuffer)
            {
                if (!mockBuffer)
                    gpuData = new Utility.GPUBuffer<T>((int)totalCount, bufferType);
                cpuData = new NativeArray<T>((int)totalCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                allocator = new GPUBufferAllocator(totalCount);
                if (!mockBuffer)
                    m_ElemStride = (uint)gpuData.ElementStride;

                m_UpdateRangePoolSize = updateRangePoolSize;
                uint multipliedUpdateRangePoolSize = m_UpdateRangePoolSize * maxQueuedFrameCount;
                updateRanges = new NativeArray<GfxUpdateBufferRange>((int)multipliedUpdateRangePoolSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                m_UpdateRangeMin = uint.MaxValue;
                m_UpdateRangeMax = 0;
                m_UpdateRangesEnqueued = 0;
                m_UpdateRangesBatchStart = 0;
            }

            #region Dispose Pattern

            protected bool disposed { get; private set; }


            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            public void Dispose(bool disposing)
            {
                if (disposed)
                    return;

                if (disposing)
                {
                    gpuData?.Dispose();
                    cpuData.Dispose();
                    updateRanges.Dispose();
                }
                else
                    UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

                disposed = true;
            }

            #endregion // Dispose Pattern

            public void RegisterUpdate(uint start, uint size)
            {
                Debug.Assert(start + size <= cpuData.Length);

                int rangeIndex = (int)(m_UpdateRangesBatchStart + m_UpdateRangesEnqueued);

                if (m_UpdateRangesEnqueued > 0)
                {
                    // If this update chains with the previous one, just grow the previous one
                    int lastIndex = rangeIndex - 1;
                    var lastRange = updateRanges[lastIndex];
                    uint startBytes = start * m_ElemStride;
                    if (lastRange.offsetFromWriteStart + lastRange.size == startBytes)
                    {
                        updateRanges[lastIndex] = new GfxUpdateBufferRange() { source = lastRange.source, offsetFromWriteStart = lastRange.offsetFromWriteStart, size = lastRange.size + size * m_ElemStride };
                        m_UpdateRangeMax = Math.Max(m_UpdateRangeMax, start + size);
                        return;
                    }
                }

                m_UpdateRangeMin = Math.Min(m_UpdateRangeMin, start);
                m_UpdateRangeMax = Math.Max(m_UpdateRangeMax, start + size);
                if (m_UpdateRangesEnqueued == m_UpdateRangePoolSize)
                {
                    m_UpdateRangesSaturated = true;
                    return; // Reached the max for this frame, ignore any more notifications, and just upload the entire affected regions including any holes inbetween
                }

                var cpuDataSlice = new UIntPtr(cpuData.Slice((int)start, (int)size).GetUnsafeReadOnlyPtr());
                updateRanges[rangeIndex] = new GfxUpdateBufferRange() { source = cpuDataSlice, offsetFromWriteStart = start * m_ElemStride, size = size * m_ElemStride };
                m_UpdateRangesEnqueued++;
            }

            // This is expected to be called no more than once per frame
            public void SendUpdates()
            {
                if (m_UpdateRangesEnqueued == 0)
                    return;

                if (m_UpdateRangesSaturated)
                {
                    uint updateSize = m_UpdateRangeMax - m_UpdateRangeMin;
                    m_UpdateRangesEnqueued = 1;
                    updateRanges[(int)m_UpdateRangesBatchStart] = new GfxUpdateBufferRange()
                    {
                        source = new UIntPtr(cpuData.Slice((int)m_UpdateRangeMin, (int)updateSize).GetUnsafeReadOnlyPtr()),
                        offsetFromWriteStart = m_UpdateRangeMin * m_ElemStride,
                        size = updateSize * m_ElemStride
                    };
                }

                // Send to the GPU, if the minimum affected byte address is not zero, we need to adjust the range entries
                // to factor out that offset as the 'offsetFromWriteStart' member is refering to the buffer lock position
                // not the start of the GPU buffer
                uint minByte = m_UpdateRangeMin * m_ElemStride;
                uint maxByte = m_UpdateRangeMax * m_ElemStride;
                if (minByte > 0)
                {
                    for (uint i = 0; i < m_UpdateRangesEnqueued; i++)
                    {
                        int index = (int)(i + m_UpdateRangesBatchStart);
                        updateRanges[index] = new GfxUpdateBufferRange()
                        {
                            source = updateRanges[index].source,
                            offsetFromWriteStart = updateRanges[index].offsetFromWriteStart - minByte,
                            size = updateRanges[index].size
                        };
                    }
                }
                gpuData?.UpdateRanges(updateRanges.Slice((int)m_UpdateRangesBatchStart, (int)m_UpdateRangesEnqueued), (int)minByte, (int)maxByte);

                // Reset state for upcoming updates
                m_UpdateRangeMin = uint.MaxValue;
                m_UpdateRangeMax = 0;
                m_UpdateRangesEnqueued = 0;
                m_UpdateRangesBatchStart = (m_UpdateRangesBatchStart + m_UpdateRangePoolSize);
                if (m_UpdateRangesBatchStart >= updateRanges.Length)
                    m_UpdateRangesBatchStart = 0;
                m_UpdateRangesSaturated = false;
            }

            public Utility.GPUBuffer<T> gpuData;
            public NativeArray<T> cpuData;
            public NativeArray<GfxUpdateBufferRange> updateRanges; // Powers of two count
            public GPUBufferAllocator allocator;
            private readonly uint m_UpdateRangePoolSize;
            private uint m_ElemStride;
            private uint m_UpdateRangeMin;
            private uint m_UpdateRangeMax;
            private uint m_UpdateRangesEnqueued;
            private uint m_UpdateRangesBatchStart;
            private bool m_UpdateRangesSaturated;
        }

        public DataSet<Vertex> vertices;
        public DataSet<UInt16> indices;
        public Page next;
    }
    #endregion
}
