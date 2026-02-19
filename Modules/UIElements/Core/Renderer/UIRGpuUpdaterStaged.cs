// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace UnityEngine.UIElements.UIR
{
    class GpuUpdaterStaged<T> : IDisposable where T : unmanaged
    {
        public GpuUpdaterStaged(Utility.GPUBufferType bufferType)
        {
            // Note that Vertex/Index have to be specified because some DX11 drivers reject Dynamic usage with bind flags set to 0.
            if (bufferType == Utility.GPUBufferType.Vertex)
            {
                m_StagingBufferFlags = GpuBufferFlags.BufferFlags_Target_Vertex | GpuBufferFlags.BufferFlags_Target_CopySrc | GpuBufferFlags.BufferFlags_Mode_SubUpdates;
                m_SupportedBufferSizes = new uint[] { 1 << 13, 1 << 16 };
            }
            else
            {
                m_StagingBufferFlags = GpuBufferFlags.BufferFlags_Target_Index | GpuBufferFlags.BufferFlags_Target_CopySrc | GpuBufferFlags.BufferFlags_Mode_SubUpdates;
                m_SupportedBufferSizes = new uint[] { 1 << 13, 1 << 18 };
            }
        }

        static readonly MemoryLabel k_MemoryLabel = new(nameof(UIElements), $"Renderer.{nameof(GpuUpdaterStaged<T>)}");

        readonly GpuBufferFlags m_StagingBufferFlags;
        readonly uint[] m_SupportedBufferSizes; // Must be sorted in ascending order

        // Stores the copy ranges provided to the gfx device
        CircularRangeBuffer<GfxCopyBufferRange> m_CopyRangesPool = new CircularRangeBuffer<GfxCopyBufferRange>(128);

        // Stores the update ranges for GPU uploads from staging buffer CPU data
        CircularRangeBuffer<GfxUpdateBufferRange> m_UpdateRangesPool = new CircularRangeBuffer<GfxUpdateBufferRange>(128);

        // Stores CPU copy ranges for jobified CPU-to-CPU copies
        CircularRangeBuffer<CpuCopyRange> m_CpuCopyRangesPool = new CircularRangeBuffer<CpuCopyRange>(128);

        // Represents a CPU-to-CPU copy operation for the job system
        [StructLayout(LayoutKind.Sequential)]
        unsafe struct CpuCopyRange
        {
            public byte* srcPtr;     // Source CPU buffer pointer
            public byte* dstPtr;     // Destination CPU buffer pointer (staging buffer)
            public int size;         // Size in bytes to copy
        }

        // Job for parallel CPU-to-CPU copies
        struct CpuCopyJob : IJobFor
        {
            [ReadOnly]
            public NativeSlice<CpuCopyRange> copyRanges;

            public unsafe void Execute(int index)
            {
                CpuCopyRange range = copyRanges[index];
                UnsafeUtility.MemCpy(range.dstPtr, range.srcPtr, range.size);
            }
        }

        // Per-frame data for tracking elements to free from pools
        struct PerFrameData
        {
            // Tracks the number of elements to free from the copy ranges pool per frame
            public int copyRangesToFree;
            // Tracks the number of elements to free from the update ranges pool per frame
            public int updateRangesToFree;
            // Tracks the number of CPU copy ranges to free per frame
            public int cpuCopyRangesToFree;
        }

        unsafe struct CopyInfo
        {
            public void* srcCpuBuffer; // The DataSet CPU buffer
            public Utility.GPUBuffer<T> dstGpuBuffer; // The DataSet GPU buffer
            public NativeSlice<GfxCopyBufferRange> pendingGpuCopies; // GPU copy ranges (from staging to destination)
        }

        // An update buffer can contain the source data for multiple DataSets.
        class StagingBufferInfo
        {
            public int frameUsed; // -1 means it wasn't used in a long time
            public int usedSize;
            public int capacity;

            // The data from the modified DataSets is staged here
            public NativeArray<T> cpuData;
            public Utility.GPUBuffer<T> gpuData;

            public List<CopyInfo> pendingDataSets = new List<CopyInfo>(4);
        }

        List<StagingBufferInfo> m_StagingBuffers = new List<StagingBufferInfo>(4);
        List<StagingBufferInfo> m_AvailableStagingBuffers = new List<StagingBufferInfo>(4);

        List<DataSet<T>> m_DirtyDataSets = new List<DataSet<T>>(4);

        // When negative, it means the number of frames since last used (decrements every unused frame)
        // When [0,1,2,...,k_MaxQueuedFrameCount[ it means the frame index where it is being used
        int m_CurrentFrameIndex;
        int m_TotalDirtyCount;
        int m_TotalCpuCopyRanges; // Total number of CPU copy ranges prepared

        // Job handle for the pending CPU copy job from the vertex/index buffers to the staging buffers
        JobHandle m_PendingCpuCopyJob;

        // Per-frame data array
        PerFrameData[] m_FrameDataArray = new PerFrameData[UIRenderDevice.k_MaxQueuedFrameCount];

        private ref PerFrameData currentFrameData
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref m_FrameDataArray[m_CurrentFrameIndex];
        }

        public void AddChanges(DataSet<T> dataSet)
        {
            int dirtyCount = dataSet.dirtyRanges.Count;

            if (dirtyCount > 0)
            {
                m_TotalDirtyCount += dirtyCount;
                m_DirtyDataSets.Add(dataSet);
            }
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

                    Debug.Assert(stagingBuffer.usedSize == 0);
                    Debug.Assert(stagingBuffer.pendingDataSets.Count == 0);
                }
            }
        }

        static readonly Comparison<DataSet<T>> s_DataSetSort = (a, b) =>
        {
            uint sizeA = a.totalDirtySize;
            uint sizeB = b.totalDirtySize;
            if (sizeB > sizeA) return 1;
            if (sizeB < sizeA) return -1;
            return 0;
        };

        public void SendUpdates()
        {
            if (m_DirtyDataSets.Count == 0)
                return;

            // Free the copy ranges used previously but that can now safely be reused
            m_CopyRangesPool.Free(currentFrameData.copyRangesToFree);
            currentFrameData.copyRangesToFree = 0;

            // Free the update ranges from previous frames
            m_UpdateRangesPool.Free(currentFrameData.updateRangesToFree);
            currentFrameData.updateRangesToFree = 0;

            // Free CPU copy ranges from previous frames
            m_CpuCopyRangesPool.Free(currentFrameData.cpuCopyRangesToFree);
            currentFrameData.cpuCopyRangesToFree = 0;

            // Find all the available staging buffers for this frame
            GatherAvailableStagingBuffers();

            // Sort the dirty data sets by total dirty size from largest to smallest
            m_DirtyDataSets.Sort(s_DataSetSort);

            // Reset the total CPU copy ranges counter
            m_TotalCpuCopyRanges = 0;

            // Process data sets, packing them into available buffers
            foreach (DataSet<T> dataSet in m_DirtyDataSets)
            {
                int totalDirtySize = (int)dataSet.totalDirtySize;

                // Find or allocate a suitable staging buffer
                StagingBufferInfo targetBuffer = FindOrAllocateBuffer(m_AvailableStagingBuffers, totalDirtySize);
                targetBuffer.frameUsed = m_CurrentFrameIndex;

                // Determine the required copy offsets between the staging buffer and the data set
                PrepareCopyRanges(dataSet, targetBuffer);
            }

            // Perform all CPU-to-CPU copies for each modified staging buffer using a parallel job
            UpdateStagingBuffersCpuData(m_AvailableStagingBuffers);

            // Upload the CPU data to the GPU buffer for each modified staging buffer
            UpdateStagingBuffersGpuData(m_AvailableStagingBuffers);

            // Issue the GPU copy commands for each staging buffer to all its destination buffers
            int totalRangesUsed = 0;
            foreach (StagingBufferInfo stagingBuffer in m_AvailableStagingBuffers)
            {
                if (stagingBuffer.usedSize > 0)
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
                    stagingBuffer.usedSize = 0;
                    stagingBuffer.pendingDataSets.Clear();
                }
            }

            // Track ranges to free in future frames
            currentFrameData.copyRangesToFree = totalRangesUsed;

            foreach (DataSet<T> dataSet in m_DirtyDataSets)
                dataSet.ResetDirtyRanges();
            m_DirtyDataSets.Clear();

            m_TotalDirtyCount = 0;

            // Clear the available buffers list to prevent memory retention
            m_AvailableStagingBuffers.Clear();

            PruneUnusedStagingBuffers();
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

        int FindSuitableBufferSize(int requiredSize)
        {
            // Find the smallest supported buffer size that fits the required size
            foreach (var size in m_SupportedBufferSizes)
            {
                if (size >= requiredSize)
                    return (int)size;
            }

            // Use a dedicated size for an oversized page
            return requiredSize;
        }

        StagingBufferInfo FindOrAllocateBuffer(List<StagingBufferInfo> availableBuffers, int requiredSize)
        {
            // Try to find an available buffer with enough space
            foreach (StagingBufferInfo stagingBuffer in availableBuffers)
            {
                if (stagingBuffer.capacity - stagingBuffer.usedSize >= requiredSize)
                    return stagingBuffer;
            }

            // If no suitable buffer found, allocate a new one
            var newBuffer = AllocateStagingBuffer(requiredSize);
            availableBuffers.Add(newBuffer);
            return newBuffer;
        }

        StagingBufferInfo AllocateStagingBuffer(int requiredSize)
        {
            int capacity = FindSuitableBufferSize(requiredSize);

            var stagingBuffer = new StagingBufferInfo
            {
                capacity = capacity,
                usedSize = 0,
                frameUsed = -1,
                cpuData = new NativeArray<T>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                gpuData = new Utility.GPUBuffer<T>(capacity, m_StagingBufferFlags)
            };

            m_StagingBuffers.Add(stagingBuffer);
            return stagingBuffer;
        }

        unsafe void PrepareCopyRanges(DataSet<T> dataSet, StagingBufferInfo stagingBuffer)
        {
            // Allocate ranges for this destination
            int rangeCount = dataSet.dirtyRanges.Count;
            NativeSlice<GfxCopyBufferRange> copyRanges = m_CopyRangesPool.Allocate(rangeCount);

            int elementStride = stagingBuffer.gpuData.ElementStride;
            int rangeIndex = 0;

            foreach (var dirtyRange in dataSet.dirtyRanges)
            {
                int stagingBufferOffset = stagingBuffer.usedSize;
                int size = (int)dirtyRange.size;

                // Setup the copy range for GPU-to-GPU copy
                // These ranges will also be used in reverse for CPU-to-CPU copies (if not using sub-updates)
                copyRanges[rangeIndex] = new GfxCopyBufferRange
                {
                    srcOffset = (uint)(stagingBufferOffset * elementStride),
                    dstOffset = (uint)((int)dirtyRange.start * elementStride),
                    size = (uint)((int)dirtyRange.size * elementStride)
                };

                stagingBuffer.usedSize += size;
                rangeIndex++;
            }

            Debug.Assert(dataSet.gpuData != null);
            Debug.Assert(dataSet.cpuData != null);

            var copyInfo = new CopyInfo
            {
                srcCpuBuffer = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(dataSet.cpuData),
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
                if (stagingBuffer.usedSize > 0)
                {
                    byte* stagingCpuPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(stagingBuffer.cpuData);

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
                                size = (int)copyRange.size
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
                if (stagingBuffer.usedSize > 0)
                {
                    int elementStride = stagingBuffer.gpuData.ElementStride;

                    // Allocate a single range to upload all the data from the beginning of the buffer
                    NativeSlice<GfxUpdateBufferRange> uploadRangeArray = m_UpdateRangesPool.Allocate(1);
                    T* source = (T*)NativeArrayUnsafeUtility.GetUnsafePtr(stagingBuffer.cpuData);
                    uploadRangeArray[0] = new GfxUpdateBufferRange
                    {
                        offsetFromWriteStart = 0, // Always write from the beginning
                        size = (uint)(stagingBuffer.usedSize * elementStride),
                        source = new UIntPtr(source)
                    };

                    // Update the GPU buffer from offset 0 to usedSize
                    int mappingEnd = stagingBuffer.usedSize * elementStride;
                    stagingBuffer.gpuData.UpdateRanges(uploadRangeArray.Slice(0, 1), 0, mappingEnd);

                    totalUpdateRangesUsed++;
                }
            }

            // Track update ranges to free in future frames
            currentFrameData.updateRangesToFree = totalUpdateRangesUsed;
        }

        public void AdvanceFrame()
        {
            // Complete any pending CPU copy jobs before advancing the frame
            // This ensures the job is done before source CPU buffers are modified
            m_PendingCpuCopyJob.Complete();

            m_CurrentFrameIndex = m_CurrentFrameIndex == UIRenderDevice.k_MaxQueuedFrameCount - 1 ? 0 : m_CurrentFrameIndex + 1;
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
                Debug.Assert(m_PendingCpuCopyJob.IsCompleted);
                m_CopyRangesPool?.Dispose();
                m_UpdateRangesPool?.Dispose();
                m_CpuCopyRangesPool?.Dispose();

                foreach (StagingBufferInfo stagingBuffer in m_StagingBuffers)
                {
                    stagingBuffer.cpuData.Dispose();
                    stagingBuffer.gpuData.Dispose();
                }
                m_StagingBuffers.Clear();
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion
    }
}
