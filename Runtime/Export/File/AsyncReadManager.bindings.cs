// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Scripting;
using System.Diagnostics;
using Unity.Jobs;

namespace Unity.IO.LowLevel.Unsafe
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct ReadCommand
    {
        public void* Buffer;
        public long Offset;
        public long Size;
    }

    // When adding a subsystem, keep in sync with AssetContext.h
    public enum AssetLoadingSubsystem
    {
        Other = 0,
        Texture,
        VirtualTexture,
        Mesh,
        Audio,
        Scripts,
        EntitiesScene,
        EntitiesStreamBinaryReader
    }

    public enum ReadStatus
    {
        Complete,
        InProgress,
        Failed
    }

    [RequiredByNativeCode]
    public enum Priority
    {
        PriorityLow = 0,
        PriorityHigh = 1
    }
    public struct ReadHandle : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        internal IntPtr ptr;
        internal int version;

        public bool IsValid()
        {
            return IsReadHandleValid(this);
        }

        public void Dispose()
        {
            if (!IsReadHandleValid(this))
                throw new InvalidOperationException("ReadHandle.Dispose cannot be called twice on the same ReadHandle");
            if (Status == ReadStatus.InProgress)
                throw new InvalidOperationException("ReadHandle.Dispose cannot be called until the read operation completes");
            ReleaseReadHandle(this);
        }

        public JobHandle JobHandle
        {
            get
            {
                if (!IsReadHandleValid(this))
                    throw new InvalidOperationException("ReadHandle.JobHandle cannot be called after the ReadHandle has been disposed");
                return GetJobHandle(this);
            }
        }
        public ReadStatus Status
        {
            get
            {
                if (!IsReadHandleValid(this))
                    throw new InvalidOperationException("ReadHandle.Status cannot be called after the ReadHandle has been disposed");
                return GetReadStatus(this);
            }
        }

        [ThreadAndSerializationSafe()]
        [FreeFunction("AsyncReadManagerManaged::GetReadStatus", IsThreadSafe = true)]
        private extern static ReadStatus GetReadStatus(ReadHandle handle);

        [ThreadAndSerializationSafe()]
        [FreeFunction("AsyncReadManagerManaged::ReleaseReadHandle", IsThreadSafe = true)]
        private extern static void ReleaseReadHandle(ReadHandle handle);

        [ThreadAndSerializationSafe()]
        [FreeFunction("AsyncReadManagerManaged::IsReadHandleValid", IsThreadSafe = true)]
        private extern static bool IsReadHandleValid(ReadHandle handle);

        [ThreadAndSerializationSafe()]
        [FreeFunction("AsyncReadManagerManaged::GetJobHandle", IsThreadSafe = true)]
        private extern static JobHandle GetJobHandle(ReadHandle handle);
    }

    [NativeHeader("Runtime/File/AsyncReadManagerManagedApi.h")]
    unsafe static public class AsyncReadManager
    {
        [ThreadAndSerializationSafe()]
        [FreeFunction("AsyncReadManagerManaged::Read", IsThreadSafe = true)]
        private extern unsafe static ReadHandle ReadInternal(string filename, void* cmds, uint cmdCount, string assetName, UInt64 typeID, AssetLoadingSubsystem subsystem);
        public static ReadHandle Read(string filename, ReadCommand* readCmds, uint readCmdCount, string assetName = "", UInt64 typeID = 0, AssetLoadingSubsystem subsystem =  AssetLoadingSubsystem.Scripts)
        {
            return ReadInternal(filename, readCmds, readCmdCount, assetName, typeID, subsystem);
        }
    }

    [NativeHeader("Runtime/File/AsyncReadManagerMetrics.h")]
    public enum ProcessingState
    {
        Unknown = 0,
        InQueue = 1,
        Reading = 2,
        Completed = 3,
        Failed = 4,
        Canceled = 5,
    };

    public enum FileReadType
    {
        Sync = 0,
        Async = 1
    };

    [NativeConditional("ENABLE_PROFILER")]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    public struct AsyncReadManagerRequestMetric
    {
        [NativeName("assetName")]
        public string   AssetName       { get; }
        [NativeName("fileName")]
        public string   FileName        {  get; }
        [NativeName("offsetBytes")]
        public UInt64   OffsetBytes     {  get; }
        [NativeName("sizeBytes")]
        public UInt64   SizeBytes       {  get; }
        [NativeName("assetTypeId")]
        public UInt64   AssetTypeId     {  get; }                // some are UIDs
        [NativeName("currentBytesRead")]
        public UInt64   CurrentBytesRead {  get; }
        [NativeName("batchReadCount")]
        public UInt32   BatchReadCount  {  get; }
        [NativeName("isBatchRead")]
        public bool     IsBatchRead     {  get; }
        [NativeName("state")]
        public ProcessingState State    {  get; }
        [NativeName("readType")]
        public FileReadType ReadType    {  get; }
        [NativeName("priorityLevel")]
        public Priority   PriorityLevel   {  get; }                // currently only low (0) and high (1)
        [NativeName("subsystem")]
        public AssetLoadingSubsystem Subsystem {  get; }
        [NativeName("requestTimeMicroseconds")]
        public double   RequestTimeMicroseconds     {  get; }    // Time request was made
        [NativeName("timeInQueueMicroseconds")]
        public double   TimeInQueueMicroseconds     {  get; }    // (if state is InQueue this is set at the point GetMetrics() call was made )
        [NativeName("totalTimeMicroseconds")]
        public double   TotalTimeMicroseconds       {  get; }    // if state is Completed or Failed this is : total time from request to completion (includes latency)
                                                                 // if stats is Processing or InQueue this is : total time (at the point GetMetrics() call was made)
                                                                 // ioTimeMicroSeconds = totalTimeMicroSeconds-timeInQueueMicroseconds
                                                                 // bandwidth = sizeBytes / ioTimeMicroSeconds
                                                                 // throughput = sizeBytes / totalTimeMicroSeconds
    }

    [NativeConditional("ENABLE_PROFILER")]
    static public class AsyncReadManagerMetrics
    {
        // Keep in Sync with AsyncReadManagerMetrics.h
        [Flags]
        public enum Flags
        {
            None = 0,
            ClearOnRead = 1 << 0
        };

        [FreeFunction("AreMetricsEnabled_Internal")]
        extern static public bool IsEnabled();

        [FreeFunction("GetAsyncReadManagerMetrics()->ClearMetrics"), ThreadSafe]
        extern static private void ClearMetrics_Internal();

        static public void ClearCompletedMetrics()
        {
            ClearMetrics_Internal();
        }

        [FreeFunction("GetAsyncReadManagerMetrics()->GetMarshalledMetrics"), ThreadSafe]
        extern static internal AsyncReadManagerRequestMetric[] GetMetrics_Internal(bool clear);

        [FreeFunction("GetAsyncReadManagerMetrics()->GetMetrics_NoAlloc"), ThreadSafe]
        extern static internal void GetMetrics_NoAlloc_Internal([NotNull] List<AsyncReadManagerRequestMetric> metrics, bool clear);

        [FreeFunction("GetAsyncReadManagerMetrics()->GetMarshalledMetrics_Filtered_Managed"), ThreadSafe]
        extern static internal AsyncReadManagerRequestMetric[] GetMetrics_Filtered_Internal(AsyncReadManagerMetricsFilters filters, bool clear);

        [FreeFunction("GetAsyncReadManagerMetrics()->GetMetrics_NoAlloc_Filtered_Managed"), ThreadSafe]
        extern static internal void GetMetrics_NoAlloc_Filtered_Internal([NotNull] List<AsyncReadManagerRequestMetric> metrics, AsyncReadManagerMetricsFilters filters, bool clear);

        static public AsyncReadManagerRequestMetric[] GetMetrics(AsyncReadManagerMetricsFilters filters, Flags flags)
        {
            bool clear = ((flags & Flags.ClearOnRead) == Flags.ClearOnRead) ? true : false;

            return GetMetrics_Filtered_Internal(filters, clear);
        }

        static public void GetMetrics(List<AsyncReadManagerRequestMetric> outMetrics, AsyncReadManagerMetricsFilters filters, Flags flags)
        {
            bool clear = ((flags & Flags.ClearOnRead) == Flags.ClearOnRead) ? true : false;

            GetMetrics_NoAlloc_Filtered_Internal(outMetrics, filters, clear);
        }

        static public AsyncReadManagerRequestMetric[] GetMetrics(Flags flags)
        {
            bool clear = ((flags & Flags.ClearOnRead) == Flags.ClearOnRead) ? true : false;

            return GetMetrics_Internal(clear);
        }

        static public void GetMetrics(List<AsyncReadManagerRequestMetric> outMetrics, Flags flags)
        {
            bool clear = ((flags & Flags.ClearOnRead) == Flags.ClearOnRead) ? true : false;

            GetMetrics_NoAlloc_Internal(outMetrics, clear);
        }

        [FreeFunction("GetAsyncReadManagerMetrics()->StartCollecting")]
        extern static public void StartCollectingMetrics();

        [FreeFunction("GetAsyncReadManagerMetrics()->StopCollecting")]
        extern static public void StopCollectingMetrics();

        [FreeFunction("GetAsyncReadManagerMetrics()->GetCurrentSummaryMetrics")]
        extern static internal AsyncReadManagerSummaryMetrics GetSummaryMetrics_Internal(bool clear);
        static public AsyncReadManagerSummaryMetrics GetCurrentSummaryMetrics(Flags flags)
        {
            bool clear = ((flags & Flags.ClearOnRead) == Flags.ClearOnRead) ? true : false;

            return GetSummaryMetrics_Internal(clear);
        }

        [FreeFunction("GetAsyncReadManagerMetrics()->GetCurrentSummaryMetricsWithFilters")]
        extern static internal AsyncReadManagerSummaryMetrics GetSummaryMetricsWithFilters_Internal(AsyncReadManagerMetricsFilters metricsFilters, bool clear);
        static public AsyncReadManagerSummaryMetrics GetCurrentSummaryMetrics(AsyncReadManagerMetricsFilters metricsFilters, Flags flags)
        {
            bool clear = ((flags & Flags.ClearOnRead) == Flags.ClearOnRead) ? true : false;

            return GetSummaryMetricsWithFilters_Internal(metricsFilters, clear);
        }

        [FreeFunction("GetAsyncReadManagerMetrics()->GetSummaryOfMetrics_Managed"), ThreadSafe]
        extern static internal AsyncReadManagerSummaryMetrics GetSummaryOfMetrics_Internal(AsyncReadManagerRequestMetric[] metrics);
        static public AsyncReadManagerSummaryMetrics GetSummaryOfMetrics(AsyncReadManagerRequestMetric[] metrics)
        {
            return GetSummaryOfMetrics_Internal(metrics);
        }

        [FreeFunction("GetAsyncReadManagerMetrics()->GetSummaryOfMetrics_FromContainer_Managed", ThrowsException = true), ThreadSafe]
        extern static internal AsyncReadManagerSummaryMetrics GetSummaryOfMetrics_FromContainer_Internal(List<AsyncReadManagerRequestMetric> metrics);
        static public AsyncReadManagerSummaryMetrics GetSummaryOfMetrics(List<AsyncReadManagerRequestMetric> metrics)
        {
            return GetSummaryOfMetrics_FromContainer_Internal(metrics);
        }

        [FreeFunction("GetAsyncReadManagerMetrics()->GetSummaryOfMetricsWithFilters_Managed"), ThreadSafe]
        extern static internal AsyncReadManagerSummaryMetrics GetSummaryOfMetricsWithFilters_Internal(AsyncReadManagerRequestMetric[] metrics, AsyncReadManagerMetricsFilters metricsFilters);

        static public AsyncReadManagerSummaryMetrics GetSummaryOfMetrics(AsyncReadManagerRequestMetric[] metrics, AsyncReadManagerMetricsFilters metricsFilters)
        {
            return GetSummaryOfMetricsWithFilters_Internal(metrics, metricsFilters);
        }

        [FreeFunction("GetAsyncReadManagerMetrics()->GetSummaryOfMetricsWithFilters_FromContainer_Managed", ThrowsException = true), ThreadSafe]
        extern static internal AsyncReadManagerSummaryMetrics GetSummaryOfMetricsWithFilters_FromContainer_Internal(List<AsyncReadManagerRequestMetric> metrics, AsyncReadManagerMetricsFilters metricsFilters);
        static public AsyncReadManagerSummaryMetrics GetSummaryOfMetrics(List<AsyncReadManagerRequestMetric> metrics, AsyncReadManagerMetricsFilters metricsFilters)
        {
            return GetSummaryOfMetricsWithFilters_FromContainer_Internal(metrics, metricsFilters);
        }

        [FreeFunction("GetAsyncReadManagerMetrics()->GetTotalSizeNonASRMReadsBytes"), ThreadSafe]
        extern static public UInt64 GetTotalSizeOfNonASRMReadsBytes(bool emptyAfterRead);
    }

    [NativeConditional("ENABLE_PROFILER")]
    [StructLayout(LayoutKind.Sequential)]
    [NativeAsStruct]
    public class AsyncReadManagerSummaryMetrics
    {
        [NativeName("totalBytesRead")]
        public UInt64 TotalBytesRead { get; }
        [NativeName("averageBandwidthMBPerSecond")]
        public float AverageBandwidthMBPerSecond { get; }
        [NativeName("averageReadSizeInBytes")]
        public float AverageReadSizeInBytes { get; }
        [NativeName("averageWaitTimeMicroseconds")]
        public float AverageWaitTimeMicroseconds { get; }
        [NativeName("averageReadTimeMicroseconds")]
        public float AverageReadTimeMicroseconds { get; }
        [NativeName("averageTotalRequestTimeMicroseconds")]
        public float AverageTotalRequestTimeMicroseconds { get; }
        [NativeName("averageThroughputMBPerSecond")]
        public float AverageThroughputMBPerSecond { get; } // Takes into account wait time as well as read time, whereas bandwidth is just file read

        [NativeName("longestWaitTimeMicroseconds")]
        public float LongestWaitTimeMicroseconds { get; }
        [NativeName("longestReadTimeMicroseconds")]
        public float LongestReadTimeMicroseconds { get; }
        [NativeName("longestReadAssetType")]
        public UInt64 LongestReadAssetType { get; }
        [NativeName("longestWaitAssetType")]
        public UInt64 LongestWaitAssetType { get; }

        [NativeName("longestReadSubsystem")]
        public AssetLoadingSubsystem LongestReadSubsystem { get; }
        [NativeName("longestWaitSubsystem")]
        public AssetLoadingSubsystem LongestWaitSubsystem { get; }

        [NativeName("numberOfInProgressRequests")]
        public int NumberOfInProgressRequests { get; }
        [NativeName("numberOfCompletedRequests")]
        public int NumberOfCompletedRequests { get; }
        [NativeName("numberOfFailedRequests")]
        public int NumberOfFailedRequests { get; }
        [NativeName("numberOfWaitingRequests")]
        public int NumberOfWaitingRequests { get; }
        [NativeName("numberOfCanceledRequests")]
        public int NumberOfCanceledRequests { get; }
        [NativeName("totalNumberOfRequests")]
        public int TotalNumberOfRequests { get; }

        [NativeName("numberOfCachedReads")]
        public int NumberOfCachedReads { get; }
        [NativeName("numberOfAsyncReads")]
        public int NumberOfAsyncReads { get; }
        [NativeName("numberOfSyncReads")]
        public int NumberOfSyncReads { get; }
    };

    [NativeConditional("ENABLE_PROFILER")]
    [StructLayout(LayoutKind.Sequential)]
    [NativeAsStruct]
    [RequiredByNativeCode]
    public class AsyncReadManagerMetricsFilters
    {
        [NativeName("typeIDs")]
        internal UInt64[] TypeIDs;
        [NativeName("states")]
        internal ProcessingState[] States;
        [NativeName("readTypes")]
        internal FileReadType[] ReadTypes;
        [NativeName("priorityLevels")]
        internal Priority[] PriorityLevels;
        [NativeName("subsystems")]
        internal AssetLoadingSubsystem[] Subsystems;

        // Constructors for common use cases, other combinations of filters can be created with Set Filter APIs.
        public AsyncReadManagerMetricsFilters()
        {
            ClearFilters();
        }

        public AsyncReadManagerMetricsFilters(UInt64 typeID)
        {
            ClearFilters();
            SetTypeIDFilter(typeID);
        }

        public AsyncReadManagerMetricsFilters(ProcessingState state)
        {
            ClearFilters();
            SetStateFilter(state);
        }

        public AsyncReadManagerMetricsFilters(FileReadType readType)
        {
            ClearFilters();
            SetReadTypeFilter(readType);
        }

        public AsyncReadManagerMetricsFilters(Priority priorityLevel)
        {
            ClearFilters();
            SetPriorityFilter(priorityLevel);
        }

        public AsyncReadManagerMetricsFilters(AssetLoadingSubsystem subsystem)
        {
            ClearFilters();
            SetSubsystemFilter(subsystem);
        }

        public AsyncReadManagerMetricsFilters(UInt64[] typeIDs)
        {
            ClearFilters();
            SetTypeIDFilter(typeIDs);
        }

        public AsyncReadManagerMetricsFilters(ProcessingState[] states)
        {
            ClearFilters();
            SetStateFilter(states);
        }

        public AsyncReadManagerMetricsFilters(FileReadType[] readTypes)
        {
            ClearFilters();
            SetReadTypeFilter(readTypes);
        }

        public AsyncReadManagerMetricsFilters(Priority[] priorityLevels)
        {
            ClearFilters();
            SetPriorityFilter(priorityLevels);
        }

        public AsyncReadManagerMetricsFilters(AssetLoadingSubsystem[] subsystems)
        {
            ClearFilters();
            SetSubsystemFilter(subsystems);
        }

        public AsyncReadManagerMetricsFilters(UInt64[] typeIDs, ProcessingState[] states, FileReadType[] readTypes, Priority[] priorityLevels, AssetLoadingSubsystem[] subsystems)
        {
            ClearFilters();
            SetTypeIDFilter(typeIDs);
            SetStateFilter(states);
            SetReadTypeFilter(readTypes);
            SetPriorityFilter(priorityLevels);
            SetSubsystemFilter(subsystems);
        }

        public void SetTypeIDFilter(UInt64[] _typeIDs) { TypeIDs = _typeIDs; }
        public void SetStateFilter(ProcessingState[] _states) { States = _states; }
        public void SetReadTypeFilter(FileReadType[] _readTypes) { ReadTypes = _readTypes; }
        public void SetPriorityFilter(Priority[] _priorityLevels) { PriorityLevels = _priorityLevels; }
        public void SetSubsystemFilter(AssetLoadingSubsystem[] _subsystems) { Subsystems = _subsystems; }

        public void SetTypeIDFilter(UInt64 _typeID) { TypeIDs = new UInt64[] { _typeID }; }
        public void SetStateFilter(ProcessingState _state) { States = new ProcessingState[] { _state }; }
        public void SetReadTypeFilter(FileReadType _readType) { ReadTypes = new FileReadType[] { _readType }; }
        public void SetPriorityFilter(Priority _priorityLevel) { PriorityLevels = new Priority[] { _priorityLevel }; }
        public void SetSubsystemFilter(AssetLoadingSubsystem _subsystem) { Subsystems = new AssetLoadingSubsystem[] { _subsystem };  }

        public void RemoveTypeIDFilter() { TypeIDs = null; }
        public void RemoveStateFilter() { States = null; }
        public void RemoveReadTypeFilter() { ReadTypes = null; }
        public void RemovePriorityFilter() { PriorityLevels = null; }
        public void RemoveSubsystemFilter() { Subsystems = null; }

        public void ClearFilters() { RemoveTypeIDFilter(); RemoveStateFilter(); RemoveReadTypeFilter(); RemovePriorityFilter(); RemoveSubsystemFilter(); }
    };
}
