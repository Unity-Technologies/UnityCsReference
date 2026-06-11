// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    enum StagingMode
    {
        CpuGpu, // We allocate a CPU and GPU staging buffer
        GpuOnly // We allocate a GPU staging buffer only (Actual mapping must be supported OR GfxDevice implementation must create a CPU staging buffer)
    }

    class GpuUpdaterStaged : GpuUpdater
    {
        public GpuUpdaterStaged(Utility.GPUBufferType bufferType, StagingMode stagingMode, int elementStride)
        {
            m_BufferType = bufferType;
            m_StagingElementStride = elementStride;

            // Note that Vertex/Index have to be specified because some DX11 drivers reject Dynamic usage with bind flags set to 0.
            if (bufferType == Utility.GPUBufferType.Vertex)
            {
                Debug.Assert((elementStride & 3) == 0, "Vertex buffer element size must be a multiple of 4 bytes");
                m_StagingBufferFlags = GpuBufferFlags.BufferFlags_Target_Vertex | GpuBufferFlags.BufferFlags_Target_CopySrc | GpuBufferFlags.BufferFlags_Mode_SubUpdates;
                m_SupportedBufferLengths = [1u << 13, 1u << 16];
            }
            else
            {
                Debug.Assert(elementStride == sizeof(ushort), "Index buffer element size must be 2 bytes");
                m_StagingBufferFlags = GpuBufferFlags.BufferFlags_Target_Index | GpuBufferFlags.BufferFlags_Target_CopySrc | GpuBufferFlags.BufferFlags_Mode_SubUpdates;
                m_SupportedBufferLengths = [1u << 13, 1u << 18];
            }

            if (stagingMode == StagingMode.GpuOnly && !Utility.HasMappedBufferRange())
            {
                Debug.LogError("Failed to use Gpu-Only staging with GpuUpdaterStaged where sub-updates aren't supported. Reverting to Cpu/Gpu staging");
                stagingMode = StagingMode.CpuGpu;
            }

            this.stagingMode = stagingMode;
        }

        static readonly MemoryLabel k_MemoryLabel = new(nameof(UIElements), $"Renderer.{nameof(GpuUpdaterStaged)}");
        static ProfilerMarker s_MarkerGpuMappingFence = new ProfilerMarker("UIR.WaitOnGpuMappingFence");

        readonly Utility.GPUBufferType m_BufferType;
        readonly GpuBufferFlags m_StagingBufferFlags;
        readonly int m_StagingElementStride;
        readonly uint[] m_SupportedBufferLengths; // Element counts, sorted in ascending order.

        // When true, the GPU staging buffer is updated directly from the CPU buffer.
        // When false, the source CPU data will be packed into the staging buffer CPU data, which will then be used as
        // the source for the update of the staging buffer GPU data update.
        public StagingMode stagingMode { get; }

        CircularRangeBuffer<GfxCopyBufferRange> m_GpuCopyRangesPool = new CircularRangeBuffer<GfxCopyBufferRange>(128);
        CircularRangeBuffer<GfxUpdateBufferRange> m_UpdateRangesPool = new CircularRangeBuffer<GfxUpdateBufferRange>(128);
        CircularRangeBuffer<CpuCopyRange> m_CpuCopyRangesPool = new CircularRangeBuffer<CpuCopyRange>(128);

        // Represents a CPU-to-CPU copy operation for the job system
        [StructLayout(LayoutKind.Sequential)]
        unsafe struct CpuCopyRange
        {
            public byte* srcPtr;     // Source CPU buffer pointer
            public byte* dstPtr;     // Destination CPU buffer pointer (staging buffer)
            public int byteSize;         // Size in bytes to copy
        }

        // Job for parallel CPU-to-CPU copies
        struct CpuCopyJob : IJobFor
        {
            [ReadOnly]
            public NativeSlice<CpuCopyRange> copyRanges;

            public unsafe void Execute(int index)
            {
                CpuCopyRange range = copyRanges[index];
                UnsafeUtility.MemCpy(range.dstPtr, range.srcPtr, range.byteSize);
            }
        }

        // Per-frame data for tracking elements to free from pools
        struct PerFrameData
        {
            // Tracks the number of elements to free from the copy ranges pool per frame
            public int gpuCopyRangesToFree;
            // Tracks the number of elements to free from the update ranges pool per frame
            public int gpuUpdateRangesToFree;
            // Tracks the number of CPU copy ranges to free per frame
            public int cpuCopyRangesToFree;
        }

        unsafe struct CopyInfo
        {
            public void* srcCpuBuffer; // The DataSet CPU buffer
            public Utility.GPUBuffer dstGpuBuffer; // The DataSet GPU buffer
            public NativeSlice<GfxCopyBufferRange> pendingGpuCopies; // GPU copy ranges (from staging to destination)
        }

        // An update buffer can contain the source data for multiple DataSets.
        class StagingBufferInfo
        {
            public int frameUsed; // -1 means it wasn't used in a long time
            public int usedCount;
            public int capacity;

            // The data from the modified DataSets is staged here
            public RawArray cpuData;
            public Utility.GPUBuffer gpuData;

            public List<CopyInfo> pendingDataSets = new List<CopyInfo>(4);
        }

        List<StagingBufferInfo> m_StagingBuffers = new List<StagingBufferInfo>(4);
        List<StagingBufferInfo> m_AvailableStagingBuffers = new List<StagingBufferInfo>(4);

        List<DataSet> m_DirtyDataSets = new List<DataSet>(4);

        // When negative, it means the number of frames since last used (decrements every unused frame)
        // When [0,1,2,...,k_MaxQueuedFrameCount[ it means the frame index where it is being used
        int m_CurrentFrameIndex;
        int m_TotalDirtyCount;
        int m_TotalCpuCopyRanges; // Total number of CPU copy ranges prepared

        // Job handle for the pending CPU copy job from the vertex/index buffers to the staging buffers
        JobHandle m_PendingCpuCopyJob;

        // CPU fence for the render thread to complete GPU mapping operations
        // Only used with GpuStagingMode.GpuOnly
        uint m_UpdateFence;

        // Per-frame data array
        PerFrameData[] m_FrameDataArray = new PerFrameData[UIRenderDevice.k_MaxQueuedFrameCount];

        private ref PerFrameData currentFrameData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_FrameDataArray[m_CurrentFrameIndex];
        }

        public override void ProcessDataSet(DataSet dataSet)
        {
            Debug.Assert(dataSet.bufferType == m_BufferType);
            Debug.Assert(dataSet.cpuData.Stride == m_StagingElementStride, "DataSet stride must match the updater's element stride");

            int dirtyCount = dataSet.dirtyRanges.Count;

            if (dirtyCount > 0)
            {
                m_TotalDirtyCount += dirtyCount;
                m_DirtyDataSets.Add(dataSet);
            }
        }

        public override void CompleteUpdate()
        {
            if (m_DirtyDataSets.Count == 0)
                return;

            m_CpuCopyRangesPool.Free(currentFrameData.cpuCopyRangesToFree);
            currentFrameData.cpuCopyRangesToFree = 0;

            m_GpuCopyRangesPool.Free(currentFrameData.gpuCopyRangesToFree);
            currentFrameData.gpuCopyRangesToFree = 0;

            m_UpdateRangesPool.Free(currentFrameData.gpuUpdateRangesToFree);
            currentFrameData.gpuUpdateRangesToFree = 0;

            // Find all the available staging buffers for this frame
            GatherAvailableStagingBuffers();

            // Sort the dirty data sets by total dirty size from largest to smallest
            m_DirtyDataSets.Sort(k_DataSetSort);

            // Reset the total CPU copy ranges counter
            m_TotalCpuCopyRanges = 0;

            // Process data sets, packing them into available buffers
            foreach (DataSet dataSet in m_DirtyDataSets)
            {
                StagingBufferInfo targetBuffer = FindOrAllocateBuffer(m_AvailableStagingBuffers, (int)dataSet.totalDirtyCount);
                targetBuffer.frameUsed = m_CurrentFrameIndex;

                // Determine the required copy offsets between the staging buffer and the data set
                PrepareCopyRanges(dataSet, targetBuffer);
            }

            // Choose the update path based on whether sub-updates are supported
            if (stagingMode == StagingMode.GpuOnly)
            {
                // Direct update from DataSet CPU buffers to staging GPU buffers
                UpdateStagingBuffersDirectly(m_AvailableStagingBuffers);
            }
            else
            {
                // Perform all CPU-to-CPU copies for each modified staging buffer using a parallel job
                UpdateStagingBuffersCpuData(m_AvailableStagingBuffers);

                // Upload the CPU data to the GPU buffer for each modified staging buffer
                UpdateStagingBuffersGpuData(m_AvailableStagingBuffers);
            }

            // Issue the GPU copy commands for each staging buffer to all its destination buffers
            int totalRangesUsed = 0;
            foreach (StagingBufferInfo stagingBuffer in m_AvailableStagingBuffers)
            {
                if (stagingBuffer.usedCount > 0)
                {
                    foreach (var destInfo in stagingBuffer.pendingDataSets)
                    {
                        // Issue the GPU-to-GPU copy command
                        unsafe
                        {
                            Utility.CopyBufferRanges(
                                stagingBuffer.gpuData.BufferPointer,
                                destInfo.dstGpuBuffer.BufferPointer,
                                new IntPtr(NativeSliceUnsafeUtility.GetUnsafePtr(destInfo.pendingGpuCopies)),
                                destInfo.pendingGpuCopies.Length,
                                GfxCopyBufferRangesFlags.AcquiredPointer);
                        }

                        totalRangesUsed += destInfo.pendingGpuCopies.Length;
                    }

                    // Clear the buffer for next frame
                    stagingBuffer.usedCount = 0;
                    stagingBuffer.pendingDataSets.Clear();
                }
            }

            // Track ranges to free in future frames
            currentFrameData.gpuCopyRangesToFree = totalRangesUsed;

            foreach (DataSet dataSet in m_DirtyDataSets)
                dataSet.ResetDirtyRanges();
            m_DirtyDataSets.Clear();

            m_TotalDirtyCount = 0;

            // Clear the available buffers list to prevent memory retention
            m_AvailableStagingBuffers.Clear();

            PruneUnusedStagingBuffers();
        }

        void GatherAvailableStagingBuffers()
        {
            m_AvailableStagingBuffers.Clear();

            foreach (StagingBufferInfo stagingBuffer in m_StagingBuffers)
            {
                if (stagingBuffer.frameUsed == m_CurrentFrameIndex || stagingBuffer.frameUsed < 0)
                {
                    m_AvailableStagingBuffers.Add(stagingBuffer);

                    // Advance pruning by decrementing frameUsed or starting at -1
                    if (stagingBuffer.frameUsed < 0)
                        --stagingBuffer.frameUsed;
                    else
                        stagingBuffer.frameUsed = -1;

                    Debug.Assert(stagingBuffer.usedCount == 0);
                    Debug.Assert(stagingBuffer.pendingDataSets.Count == 0);
                }
            }
        }

        static readonly Comparison<DataSet> k_DataSetSort = (a, b) =>
        {
            uint countA = a.totalDirtyCount;
            uint countB = b.totalDirtyCount;
            if (countB > countA) return 1;
            if (countB < countA) return -1;
            return 0;
        };

        const uint k_IndexAlignment = 2;

        // Aligns an index range to k_IndexAlignment index boundaries (2 indices = 4 bytes).
        // The start is aligned down and the end is aligned up to ensure the original range is fully covered.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AlignIndexRange(ref uint start, ref uint count)
        {
            uint end = start + count;
            start = start & ~(k_IndexAlignment - 1);
            end = (end + k_IndexAlignment - 1) & ~(k_IndexAlignment - 1);
            count = end - start;
        }

        void PruneUnusedStagingBuffers()
        {
            int writeIndex = 0;
            int count = m_StagingBuffers.Count;

            for (int readIndex = 0; readIndex < count; ++readIndex)
            {
                var bufferInfo = m_StagingBuffers[readIndex];
                if (bufferInfo.frameUsed <= -UIRenderDevice.k_PruneEmptyPageFrameCount)
                {
                    // Dispose the staging buffer resources
                    bufferInfo.cpuData.Dispose();
                    bufferInfo.gpuData.Dispose();
                }
                else
                {
                    // Keep this buffer by moving it to the write position
                    if (writeIndex != readIndex)
                        m_StagingBuffers[writeIndex] = bufferInfo;
                    ++writeIndex;
                }
            }

            // Remove all pruned elements
            int elementsToRemove = count - writeIndex;
            if (elementsToRemove > 0)
                m_StagingBuffers.RemoveRange(writeIndex, elementsToRemove);
        }

        int FindSuitableBufferLength(int requiredLength)
        {
            // Find the smallest supported buffer length that fits the required length
            foreach (var length in m_SupportedBufferLengths)
            {
                if (length >= requiredLength)
                    return (int)length;
            }

            // Use a dedicated length for an oversized page
            return requiredLength;
        }

        StagingBufferInfo FindOrAllocateBuffer(List<StagingBufferInfo> availableBuffers, int requiredLength)
        {
            // Try to find an available buffer with enough space
            foreach (StagingBufferInfo stagingBuffer in availableBuffers)
            {
                if (stagingBuffer.capacity - stagingBuffer.usedCount >= requiredLength)
                    return stagingBuffer;
            }

            // If no suitable buffer found, allocate a new one
            var newBuffer = AllocateStagingBuffer(requiredLength);
            availableBuffers.Add(newBuffer);
            return newBuffer;
        }

        StagingBufferInfo AllocateStagingBuffer(int requiredLength)
        {
            int capacity = FindSuitableBufferLength(requiredLength);

            var stagingBuffer = new StagingBufferInfo
            {
                capacity = capacity,
                usedCount = 0,
                frameUsed = -1,
                cpuData = new RawArray(capacity, m_StagingElementStride, k_MemoryLabel),
                gpuData = new Utility.GPUBuffer(capacity, m_StagingElementStride, m_StagingBufferFlags)
            };

            m_StagingBuffers.Add(stagingBuffer);
            return stagingBuffer;
        }

        unsafe void PrepareCopyRanges(DataSet dataSet, StagingBufferInfo stagingBuffer)
        {
            // Allocate ranges for this destination
            dataSet.ConsolidateRanges();
            var dirtyRanges = dataSet.dirtyRanges;
            int rangeCount = dirtyRanges.Count;
            NativeSlice<GfxCopyBufferRange> copyRanges = m_GpuCopyRangesPool.Allocate(rangeCount);

            uint elementStride = (uint)m_StagingElementStride;
            int rangeIndex = 0;

            foreach (var dirtyRange in dirtyRanges)
            {
                int stagingBufferOffset = stagingBuffer.usedCount;
                uint start = dirtyRange.start;
                uint count = dirtyRange.count;

                if (m_BufferType == Utility.GPUBufferType.Index)
                    AlignIndexRange(ref start, ref count);

                // Setup the copy range for GPU-to-GPU copy
                // These ranges will also be used in reverse for CPU-to-CPU copies (if not using sub-updates)
                copyRanges[rangeIndex] = new GfxCopyBufferRange
                {
                    srcOffset = (uint)stagingBufferOffset * elementStride,
                    dstOffset = start * elementStride,
                    size = count * elementStride
                };

                stagingBuffer.usedCount += (int)count;
                rangeIndex++;
            }

            Debug.Assert(dataSet.gpuData != null);

            var copyInfo = new CopyInfo
            {
                srcCpuBuffer = (void*)dataSet.cpuData.GetUnsafePtr(),
                dstGpuBuffer = dataSet.gpuData,
                pendingGpuCopies = copyRanges
            };

            stagingBuffer.pendingDataSets.Add(copyInfo);

            // Increment the total CPU copy ranges counter
            m_TotalCpuCopyRanges += rangeCount;
        }

        unsafe void UpdateStagingBuffersCpuData(List<StagingBufferInfo> stagingBuffers)
        {
            if (m_TotalCpuCopyRanges == 0)
                return;

            // Allocate a single buffer to hold all CPU copy ranges
            NativeSlice<CpuCopyRange> allCpuCopyRanges = m_CpuCopyRangesPool.Allocate(m_TotalCpuCopyRanges);
            int writeIndex = 0;

            // Build all CPU copy ranges from all staging buffers
            foreach (StagingBufferInfo stagingBuffer in stagingBuffers)
            {
                if (stagingBuffer.usedCount > 0)
                {
                    byte* stagingCpuPtr = (byte*)stagingBuffer.cpuData.GetUnsafePtr();

                    // Copy data from each source DataSet CPU buffer to the staging CPU buffer
                    foreach (CopyInfo copyInfo in stagingBuffer.pendingDataSets)
                    {
                        byte* dataSetCpuPtr = (byte*)copyInfo.srcCpuBuffer;

                        // The GPU copy ranges describe staging->destination mapping
                        // For CPU copies, we need the inverse: source->staging mapping
                        foreach (var copyRange in copyInfo.pendingGpuCopies)
                        {
                            allCpuCopyRanges[writeIndex++] = new CpuCopyRange
                            {
                                srcPtr = dataSetCpuPtr + copyRange.dstOffset,
                                dstPtr = stagingCpuPtr + copyRange.srcOffset,
                                byteSize = (int)copyRange.size
                            };
                        }
                    }
                }
            }

            Debug.Assert(writeIndex == m_TotalCpuCopyRanges);

            // Schedule the parallel job to perform all CPU copies
            var copyJob = new CpuCopyJob
            {
                copyRanges = allCpuCopyRanges
            };

            m_PendingCpuCopyJob = copyJob.ScheduleParallelByRef(m_TotalCpuCopyRanges, 1, new JobHandle());
            JobHandle.ScheduleBatchedJobs();

            // Let the render thread wait for the job to complete via Utility.SyncJobFence
            Utility.SyncJobFence(m_PendingCpuCopyJob);

            // Track CPU copy ranges to free in future frames
            currentFrameData.cpuCopyRangesToFree = m_TotalCpuCopyRanges;
        }

        unsafe void UpdateStagingBuffersGpuData(List<StagingBufferInfo> stagingBuffers)
        {
            int totalUpdateRangesUsed = 0;

            foreach (StagingBufferInfo stagingBuffer in stagingBuffers)
            {
                if (stagingBuffer.usedCount > 0)
                {
                    // Allocate a single range to upload all the data from the beginning of the buffer
                    NativeSlice<GfxUpdateBufferRange> uploadRangeArray = m_UpdateRangesPool.Allocate(1);
                    byte* source = (byte*)stagingBuffer.cpuData.GetUnsafePtr();
                    int mappingEnd = stagingBuffer.usedCount * m_StagingElementStride;
                    uploadRangeArray[0] = new GfxUpdateBufferRange
                    {
                        offsetFromWriteStart = 0, // Always write from the beginning
                        size = (uint)mappingEnd,
                        source = new UIntPtr(source)
                    };

                    stagingBuffer.gpuData.UpdateRanges(uploadRangeArray.Slice(0, 1), 0, mappingEnd);

                    totalUpdateRangesUsed++;
                }
            }

            // Track update ranges to free in future frames
            currentFrameData.gpuUpdateRangesToFree += totalUpdateRangesUsed;
        }

        unsafe void UpdateStagingBuffersDirectly(List<StagingBufferInfo> stagingBuffers)
        {
            int totalUpdateRangesUsed = 0;

            foreach (StagingBufferInfo stagingBuffer in stagingBuffers)
            {
                if (stagingBuffer.usedCount > 0)
                {
                    // Count total update ranges needed for this staging buffer
                    int totalRangeCount = 0;
                    foreach (var dataSetInfo in stagingBuffer.pendingDataSets)
                        totalRangeCount += dataSetInfo.pendingGpuCopies.Length;

                    if (totalRangeCount == 0)
                        continue;

                    // Allocate a single array to hold all update ranges from this staging buffer
                    NativeSlice<GfxUpdateBufferRange> allUpdateRanges = m_UpdateRangesPool.Allocate(totalRangeCount);

                    int writeIndex = 0;

                    foreach (var dataSetInfo in stagingBuffer.pendingDataSets)
                    {
                        byte* srcCpuPtr = (byte*)dataSetInfo.srcCpuBuffer;

                        // For each copy range, create a corresponding update range
                        // The copy ranges describe staging->destination mapping
                        // For update ranges, we need source->staging mapping which are basically the reverse.
                        foreach (var copyRange in dataSetInfo.pendingGpuCopies)
                        {
                            allUpdateRanges[writeIndex++] = new GfxUpdateBufferRange
                            {
                                offsetFromWriteStart = copyRange.srcOffset, // Offset in staging buffer
                                size = copyRange.size,
                                source = new UIntPtr(srcCpuPtr + copyRange.dstOffset) // Source data from dataset CPU buffer
                            };
                        }
                    }

                    Debug.Assert(writeIndex == totalRangeCount);

                    // Single call to UpdateRanges for the entire staging buffer
                    int mappingEnd = stagingBuffer.usedCount * m_StagingElementStride;
                    stagingBuffer.gpuData.UpdateRanges(allUpdateRanges, 0, mappingEnd);

                    totalUpdateRangesUsed += totalRangeCount;
                }
            }

            // Insert a CPU fence to ensure the render thread completes GPU mapping
            // before we allow source data to be modified in the next frame
            if (totalUpdateRangesUsed > 0)
                m_UpdateFence = Utility.InsertCPUFence();

            // Track update ranges to free in future frames
            currentFrameData.gpuUpdateRangesToFree += totalUpdateRangesUsed;
        }

        public override void AdvanceFrame()
        {
            // Complete any pending CPU copy jobs before advancing the frame
            // This ensures the job is done before source CPU buffers are modified
            if (stagingMode == StagingMode.CpuGpu)
            {
                m_PendingCpuCopyJob.Complete();
            }
            else
            {
                // When using direct mapping, wait for the render thread to complete GPU mapping operations
                // before advancing the frame, as source CPU buffers may be modified after this point
                if (m_UpdateFence != 0 && !Utility.CPUFencePassed(m_UpdateFence))
                {
                    using (s_MarkerGpuMappingFence.Auto())
                    {
                        Utility.WaitForCPUFencePassed(m_UpdateFence);
                    }
                }
                m_UpdateFence = 0;
            }

            m_CurrentFrameIndex = m_CurrentFrameIndex == UIRenderDevice.k_MaxQueuedFrameCount - 1 ? 0 : m_CurrentFrameIndex + 1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                Debug.Assert(m_PendingCpuCopyJob.IsCompleted);
                m_GpuCopyRangesPool?.Dispose();
                m_UpdateRangesPool?.Dispose();
                m_CpuCopyRangesPool?.Dispose();

                foreach (StagingBufferInfo stagingBuffer in m_StagingBuffers)
                {
                    stagingBuffer.cpuData.Dispose();
                    stagingBuffer.gpuData.Dispose();
                }
                m_StagingBuffers.Clear();
            }

            base.Dispose(disposing);
        }
    }
}
