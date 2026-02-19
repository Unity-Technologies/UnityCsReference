// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.UIElements.UIR
{
    class GpuUpdaterMapped<T> : IDisposable where T : unmanaged
    {
        CircularRangeBuffer<GfxUpdateBufferRange> m_UpdateRangesPool;
        int m_CurrentFrameIndex;
        PerFrameData[] m_FrameDataArray = new PerFrameData[UIRenderDevice.k_MaxQueuedFrameCount];

        struct PerFrameData
        {
            public int rangesToFree;
        }

        public GpuUpdaterMapped()
        {
            m_UpdateRangesPool = new CircularRangeBuffer<GfxUpdateBufferRange>(128);
        }

        public void SendChanges(DataSet<T> dataSet)
        {
            if (dataSet.dirtyRanges.Count == 0)
                return;

            UploadDirtyRanges(dataSet);

            dataSet.ResetDirtyRanges();
        }

        unsafe void UploadDirtyRanges(DataSet<T> dataSet)
        {
            ref PerFrameData frameData = ref m_FrameDataArray[m_CurrentFrameIndex];

            int rangeCount = dataSet.dirtyRanges.Count;

            // Allocate ranges from the pool for this frame
            NativeSlice<GfxUpdateBufferRange> updateRanges = m_UpdateRangesPool.Allocate(rangeCount);

            // Track the allocated ranges so they can be freed later
            frameData.rangesToFree += rangeCount;

            T* source = (T*)NativeArrayUnsafeUtility.GetUnsafePtr(dataSet.cpuData);

            // Use the pre-computed bounds from the DataSet
            int writeStart = (int)(dataSet.updateRangeMin * dataSet.elemStride);
            int writeEnd = (int)(dataSet.updateRangeMax * dataSet.elemStride);

            // Populate each update range from the dirty ranges
            for (int i = 0; i < rangeCount; i++)
            {
                var dirtyRange = dataSet.dirtyRanges[i];
                uint offsetBytes = dirtyRange.start * dataSet.elemStride;
                uint sizeBytes = dirtyRange.size * dataSet.elemStride;

                updateRanges[i] = new GfxUpdateBufferRange
                {
                    offsetFromWriteStart = offsetBytes - (uint)writeStart, // Offset relative to writeStart
                    size = sizeBytes,
                    source = new UIntPtr(source + dirtyRange.start)
                };
            }

            // Update the GPU buffer with the dirty ranges
            dataSet.gpuData.UpdateRanges(updateRanges, writeStart, writeEnd);
        }

        public void AdvanceFrame()
        {
            ++m_CurrentFrameIndex;
            if (m_CurrentFrameIndex >= UIRenderDevice.k_MaxQueuedFrameCount)
                m_CurrentFrameIndex = 0;

            ref PerFrameData frameData = ref m_FrameDataArray[m_CurrentFrameIndex];
            m_UpdateRangesPool.Free(frameData.rangesToFree);
            frameData.rangesToFree = 0;
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
                m_UpdateRangesPool?.Dispose();
                m_UpdateRangesPool = null;
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion
    }
}
