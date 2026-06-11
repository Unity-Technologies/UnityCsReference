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
    class DataSet : IDisposable
    {
        static readonly MemoryLabel s_CpuMemoryLabel = new(nameof(UIElements), "Renderer.RendererCpuData");

        [StructLayout(LayoutKind.Sequential)]
        public struct Range
        {
            public uint start;
            public uint count;
        }

        public readonly Utility.GPUBufferType bufferType;
        public Utility.GPUBuffer gpuData;
        public RawArray cpuData;
        public GPUBufferAllocator allocator;
        public List<Range> dirtyRanges;

        // These track the overall bounds of all dirty ranges added since the last reset.
        uint m_DirtyRangeMin;
        uint m_DirtyRangeMax;

        // Tracks the total size of all dirty ranges
        uint m_TotalDirtyCount;

        public uint totalDirtyCount => m_TotalDirtyCount;
        public uint dirtyRangeMin => m_DirtyRangeMin;
        public uint dirtyRangeMax => m_DirtyRangeMax;

        public DataSet(Utility.GPUBufferType bufferType, bool mapped, uint totalElemCount, uint elemStride)
        {
            this.bufferType = bufferType;

            GpuBufferFlags bufferFlags = 0;
            bufferFlags |= (bufferType == Utility.GPUBufferType.Vertex) ? GpuBufferFlags.BufferFlags_Target_Vertex : GpuBufferFlags.BufferFlags_Target_Index;
            bufferFlags |= mapped ? GpuBufferFlags.BufferFlags_Mode_SubUpdates : GpuBufferFlags.BufferFlags_Mode_Immutable | GpuBufferFlags.BufferFlags_Target_CopyDst;

            gpuData = new Utility.GPUBuffer((int)totalElemCount, (int)elemStride, bufferFlags);
            cpuData = new RawArray(checked((int)totalElemCount), checked((int)elemStride), s_CpuMemoryLabel);
            allocator = new GPUBufferAllocator(totalElemCount);

            dirtyRanges = new List<Range>(32);
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

        public void AddDirtyRange(uint start, uint count)
        {
            Debug.Assert(count > 0 && (start + count) <= (uint)cpuData.Length);

            // Update the overall bounds to track the min/max of all ranges added this frame.
            m_DirtyRangeMin = Math.Min(m_DirtyRangeMin, start);
            m_DirtyRangeMax = Math.Max(m_DirtyRangeMax, start + count);
            m_TotalDirtyCount += count;

            if (dirtyRanges.Count > 0)
            {
                Range lastRange = dirtyRanges[^1];
                if (lastRange.start + lastRange.count == start)
                {
                    // The new range is right after the last added range, grow the previous range.
                    lastRange.count += count;
                    dirtyRanges[^1] = lastRange;
                    return;
                }
                else if(start + count == lastRange.start)
                {
                    // The new range is right before the last added range, grow the previous range.
                    lastRange.count += count;
                    lastRange.start = start;
                    dirtyRanges[^1] = lastRange;
                    return;
                }
            }

            // Add the new range
            dirtyRanges.Add(new Range { start = start, count = count });
        }

        public void ConsolidateRanges(float threshold = 0.9f)
        {
            if (dirtyRanges.Count > 1)
            {
                uint totalRangeCount = m_DirtyRangeMax - m_DirtyRangeMin;
                if (totalRangeCount > 0 && m_TotalDirtyCount >= totalRangeCount * threshold)
                {
                    dirtyRanges.Clear();
                    dirtyRanges.Add(new Range { start = m_DirtyRangeMin, count = totalRangeCount });
                    m_TotalDirtyCount = totalRangeCount;
                }
            }
        }

        public void ResetDirtyRanges()
        {
            dirtyRanges.Clear();

            // Reset bounds to prepare for tracking new ranges
            m_DirtyRangeMin = uint.MaxValue;
            m_DirtyRangeMax = 0;
            m_TotalDirtyCount = 0;
        }
    }
}
