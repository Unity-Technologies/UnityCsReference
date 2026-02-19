// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    // This is used to store vertices or indices. We track up to a certain number of dirty ranges. Beyond that point,
    // we track a single range that covers the entire range that has been marked dirty. This class is not thread-safe.
    class DataSet<T> : IDisposable where T : unmanaged
    {
        static readonly MemoryLabel s_CpuMemoryLabel = new(nameof(UIElements), "Renderer.RendererCpuData");

        [StructLayout(LayoutKind.Sequential)]
        public struct Range
        {
            public uint start;
            public uint size;
        }

        public Utility.GPUBuffer<T> gpuData;
        public NativeArray<T> cpuData;
        public GPUBufferAllocator allocator;
        public List<Range> dirtyRanges;
        public readonly uint elemStride;

        readonly uint m_MaxRangesPerFrame;

        // Overflow tracking:
        // - When false: dirtyRanges contains individual ranges (up to m_MaxRangesPerFrame)
        // - When true: dirtyRanges contains exactly 1 consolidated range covering [m_UpdateRangeMin, m_UpdateRangeMax)
        bool m_RangesOverflow;

        // These track the overall bounds of all dirty ranges added since the last reset.
        // Used to compute the consolidated range when we exceed m_MaxRangesPerFrame individual ranges.
        uint m_UpdateRangeMin;
        uint m_UpdateRangeMax;

        // Tracks the total size of all dirty ranges
        uint m_TotalDirtySize;

        public uint totalDirtySize => m_TotalDirtySize;
        public uint updateRangeMin => m_UpdateRangeMin;
        public uint updateRangeMax => m_UpdateRangeMax;

        public DataSet(Utility.GPUBufferType bufferType, bool mapped, uint totalCount, uint maxQueuedFrameCount, uint maxRangesPerFrame)
        {
            GpuBufferFlags bufferFlags = 0;
            bufferFlags |= (bufferType == Utility.GPUBufferType.Vertex) ? GpuBufferFlags.BufferFlags_Target_Vertex : GpuBufferFlags.BufferFlags_Target_Index;
            bufferFlags |= mapped ? GpuBufferFlags.BufferFlags_Mode_SubUpdates : GpuBufferFlags.BufferFlags_Mode_Immutable | GpuBufferFlags.BufferFlags_Target_CopyDst;

            gpuData = new Utility.GPUBuffer<T>((int)totalCount, bufferFlags);
            cpuData = new NativeArray<T>((int)totalCount, s_CpuMemoryLabel, NativeArrayOptions.UninitializedMemory);
            allocator = new GPUBufferAllocator(totalCount);
            elemStride = (uint)gpuData.ElementStride;

            m_MaxRangesPerFrame = maxRangesPerFrame;
            dirtyRanges = new List<Range>((int)maxRangesPerFrame);
            ResetDirtyRanges();
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
                gpuData.Dispose();
                cpuData.Dispose();

                allocator = null;
                dirtyRanges = null;
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        public void AddDirtyRange(uint start, uint size)
        {
            Debug.Assert(size > 0 && start + size <= cpuData.Length);

            // Update the overall bounds to track the min/max of all ranges added this frame.
            m_UpdateRangeMin = Math.Min(m_UpdateRangeMin, start);
            m_UpdateRangeMax = Math.Max(m_UpdateRangeMax, start + size);

            if (m_RangesOverflow)
            {
                // We're already in overflow mode (too many ranges were added previously).
                // dirtyRanges contains exactly 1 consolidated range.
                // Update that single range to reflect the new overall bounds.
                Debug.Assert(dirtyRanges.Count == 1);
                uint consolidatedSize = m_UpdateRangeMax - m_UpdateRangeMin;
                dirtyRanges[0] = new Range { start = m_UpdateRangeMin, size = consolidatedSize };
                m_TotalDirtySize = consolidatedSize;
                return;
            }

            if (dirtyRanges.Count > 0)
            {
                // If this range chains with the previous one, just grow the previous one.
                Range lastRange = dirtyRanges[^1];
                if (lastRange.start + lastRange.size == start)
                {
                    lastRange.size += size;
                    dirtyRanges[^1] = lastRange;
                    m_TotalDirtySize += size;
                    return;
                }
            }

            if (dirtyRanges.Count == m_MaxRangesPerFrame)
            {
                // We already have m_MaxRangesPerFrame ranges stored, so adding this new range would exceed the limit.
                // We've reached capacity for individual ranges. Transition to overflow mode. Replace all individual
                // ranges with a single consolidated range covering [m_UpdateRangeMin, m_UpdateRangeMax).
                m_RangesOverflow = true;
                dirtyRanges.Clear();
                uint consolidatedSize = m_UpdateRangeMax - m_UpdateRangeMin;
                dirtyRanges.Add(new Range { start = m_UpdateRangeMin, size = consolidatedSize });
                m_TotalDirtySize = consolidatedSize;
                return;
            }

            // The usual case: we have room for more individual ranges
            dirtyRanges.Add(new Range { start = start, size = size });
            m_TotalDirtySize += size;
        }

        public void ResetDirtyRanges()
        {
            dirtyRanges.Clear();
            m_RangesOverflow = false;

            // Reset bounds to prepare for tracking new ranges
            m_UpdateRangeMin = uint.MaxValue;
            m_UpdateRangeMax = 0;
            m_TotalDirtySize = 0;
        }
    }
}
