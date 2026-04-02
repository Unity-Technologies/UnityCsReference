// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.UIElements.UIR
{
    class GpuUpdaterMapped<T>: GpuUpdater<T> where T : unmanaged
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

        public override void ProcessDataSet(DataSet<T> dataSet)
        {
            if (dataSet.dirtyRanges.Count == 0)
                return;

            UploadDirtyRanges(dataSet);

            dataSet.ResetDirtyRanges();
        }

        public override void CompleteUpdate() {}

        unsafe void UploadDirtyRanges(DataSet<T> dataSet)
        {
            ref PerFrameData frameData = ref m_FrameDataArray[m_CurrentFrameIndex];

            dataSet.ConsolidateRanges();
            var dirtyRanges = dataSet.dirtyRanges;
            int rangeCount = dirtyRanges.Count;

            // Allocate ranges from the pool for this frame
            NativeSlice<GfxUpdateBufferRange> updateRanges = m_UpdateRangesPool.Allocate(rangeCount);

            // Track the allocated ranges so they can be freed later
            frameData.rangesToFree += rangeCount;

            T* source = (T*)NativeArrayUnsafeUtility.GetUnsafePtr(dataSet.cpuData);

            // Use the pre-computed bounds from the DataSet
            int writeStart = (int)(dataSet.dirtyRangeMin * dataSet.elemStride);
            int writeEnd = (int)(dataSet.dirtyRangeMax * dataSet.elemStride);

            // Populate each update range from the dirty ranges
            for (int i = 0; i < rangeCount; i++)
            {
                var dirtyRange = dirtyRanges[i];
                uint offsetBytes = dirtyRange.start * dataSet.elemStride;
                uint sizeBytes = dirtyRange.count * dataSet.elemStride;

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

        public override void AdvanceFrame()
        {
            ++m_CurrentFrameIndex;
            if (m_CurrentFrameIndex >= UIRenderDevice.k_MaxQueuedFrameCount)
                m_CurrentFrameIndex = 0;

            ref PerFrameData frameData = ref m_FrameDataArray[m_CurrentFrameIndex];
            m_UpdateRangesPool.Free(frameData.rangesToFree);
            frameData.rangesToFree = 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                m_UpdateRangesPool?.Dispose();
                m_UpdateRangesPool = null;
            }

            base.Dispose(disposing);
        }
    }
}
