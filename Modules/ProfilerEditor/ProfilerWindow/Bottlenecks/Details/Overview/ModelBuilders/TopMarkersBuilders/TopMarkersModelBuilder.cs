// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor.Profiling;
using static Unity.Profiling.Editor.UI.TopMarkersModel;

namespace Unity.Profiling.Editor.UI
{
    // Abstract base type for TopMarkersModel builders containing data/functionality shared across builders.
    abstract class TopMarkersModelBuilder
    {
        const int k_DefaultStackCapacity = 16;
        static readonly string[] k_IgnoredMarkerNames = new string[]
        {
            "Semaphore.WaitForSignal",
            "Idle"
        };

        readonly protected IProfilerCaptureDataService m_DataService;
        readonly protected uint m_NumberOfTopMarkers;

        protected TopMarkersModelBuilder(
            IProfilerCaptureDataService dataService,
            uint numberOfTopMarkers)
        {
            m_DataService = dataService;
            m_NumberOfTopMarkers = numberOfTopMarkers;
        }

        protected Dictionary<int, CombinedMarkerData> ComputeCombinedExclusiveTimeAndGCAllocationForAllMarkersInFrame(
            int frameIndex,
            CancellationToken cancellationToken)
        {
            var combinedMarkers = new Dictionary<int, CombinedMarkerData>();

            // Iterate over all markers, computing exclusive time and direct child
            // GC allocation bytes. Combine markers with the same marker ID.
            var dataService = m_DataService;
            var markerDataReusePool = new Stack<MarkerData>(k_DefaultStackCapacity);
            int gcAllocMarkerId = FrameDataView.invalidMarkerId;
            int[] ignoredMarkerIds = null;
            for (var threadIndex = 0; ; ++threadIndex)
            {
                using var threadData = dataService.GetRawFrameDataView(frameIndex, threadIndex);

                // Profiler API has no way to obtain thread count in a frame.
                // This is the only way to know we have processed all threads.
                if (threadData.valid == false)
                    break;

                // Discover relevant marker IDs on first iteration.
                const string k_GCAllocMarkerName = "GC.Alloc";
                if (gcAllocMarkerId == FrameDataView.invalidMarkerId)
                    gcAllocMarkerId = threadData.GetMarkerId(k_GCAllocMarkerName);

                if (ignoredMarkerIds == null)
                {
                    var ignoredMarkersCount = k_IgnoredMarkerNames.Length;
                    ignoredMarkerIds = new int[ignoredMarkersCount];
                    for (var i = 0; i < ignoredMarkersCount; ++i)
                    {
                        var markerId = threadData.GetMarkerId(k_IgnoredMarkerNames[i]);
                        ignoredMarkerIds[i] = markerId;
                    }
                }

                // Walk the marker array as a tree hierarchy.
                var stack = new Stack<MarkerData>(k_DefaultStackCapacity);
                for (var sampleIndex = 0; sampleIndex < threadData.sampleCount; ++sampleIndex)
                {
                    // The Profiler's in-memory data (e.g. ProfilerFrameData etc.) uses floating point
                    // to represent a sample's start time and duration, rather than uint64. This causes
                    // issues with samples overlapping by small amounts due to precision errors and
                    // therefore causes issues when trying to compute the sample hierarchy here. When
                    // the .raw Profiler data stream is loaded, a 'child count' is computed per
                    // profiler sample using the original 64-bit time value. This is why this algorithm
                    // uses the child count property to traverse the sample stack, rather than the more
                    // obvious approach of using sample timings; it allows us to avoid these precision
                    // errors when computing sample hierarchy with lower resolution timings.
                    var duration = threadData.GetSampleTimeNs(sampleIndex);
                    var childCount = threadData.GetSampleChildrenCount(sampleIndex);
                    while (stack.Count > 0 &&
                        stack.Peek().NumberOfChildrenToVisitRemaining == 0)
                    {
                        // The top marker on the stack has completed. This marker is not a
                        // child of it. Pop top marker off the stack and process it. Repeat
                        // until this marker is a child of the top marker on the stack.
                        var completedMarker = stack.Pop();
                        ProcessCompletedMarker(
                            completedMarker,
                            ignoredMarkerIds,
                            threadData,
                            threadIndex,
                            frameIndex,
                            combinedMarkers);

                        // Save this MarkerData for reuse to save significant managed allocations.
                        markerDataReusePool.Push(completedMarker);
                    }

                    // This marker is a child of the top sample on the stack.
                    if (stack.Count > 0)
                    {
                        // Add this marker's duration to its parent's total child duration.
                        var parent = stack.Peek();
                        parent.TotalChildDurationNs += duration;

                        // If this marker is a GC.Alloc, add its allocation size to its
                        // parent's total child GC allocation size.
                        if (gcAllocMarkerId != FrameDataView.invalidMarkerId)
                        {
                            var markerId = threadData.GetSampleMarkerId(sampleIndex);
                            if (markerId == gcAllocMarkerId)
                            {
                                var allocationSize = 0UL;
                                if (threadData.GetSampleMetadataCount(sampleIndex) > 0)
                                    allocationSize = Convert.ToUInt64(threadData.GetSampleMetadataAsLong(sampleIndex, 0));
                                parent.TotalChildGCAllocationBytes += allocationSize;
                            }
                        }

                        parent.NumberOfChildrenToVisitRemaining--;
                    }

                    // Push MarkerData onto the stack. Reuse one if possible to save significant managed allocations.
                    MarkerData markerData = null;
                    if (markerDataReusePool.Count > 0)
                    {
                        markerData = markerDataReusePool.Pop();
                        markerData.Reset(sampleIndex, childCount, duration);
                    }
                    else
                    {
                        markerData = new MarkerData(sampleIndex, childCount, duration);
                    }
                    stack.Push(markerData);

                    // Check for cancellation after each sample is processed.
                    cancellationToken.ThrowIfCancellationRequested();
                }

                // Process remaining markers (parents of the last begun marker).
                while (stack.Count > 0)
                {
                    var completedMarker = stack.Pop();
                    ProcessCompletedMarker(
                        completedMarker,
                        ignoredMarkerIds,
                        threadData,
                        threadIndex,
                        frameIndex,
                        combinedMarkers);
                }

                // Check for cancellation after each thread is processed.
                cancellationToken.ThrowIfCancellationRequested();
            }

            return combinedMarkers;
        }

        void ProcessCompletedMarker(
            MarkerData marker,
            int[] ignoredMarkerIds,
            RawFrameDataView threadData,
            int cachedThreadIndex,
            int frameIndex,
            Dictionary<int, CombinedMarkerData> combinedMarkers)
        {
            // The Profiler data has a hidden 'root' sample on each thread, which is
            // always at index 0. We don't consider these samples when computing the
            // 'top markers'. Firstly because they are hidden from users in the
            // Profiler UI. And secondly because we have many threads with just a
            // root sample and no other samples, which would always have an
            // exclusive time equal to the duration of the frame/thread and a GC
            // allocation size of zero.
            if (marker.SampleIndex == 0)
                return;

            // Filter out markers that we don't want to direct the user to.
            var markerId = threadData.GetSampleMarkerId(marker.SampleIndex);
            foreach (var ignoredMarkerId in ignoredMarkerIds)
            {
                if (markerId == ignoredMarkerId)
                    return;
            }

            // At the point a marker is processed, all of its (direct) child GC.Alloc markers
            // have accumulated their GC allocation size in TotalChildGcAllocationBytes.
            var totalChildGCAllocationBytes = marker.TotalChildGCAllocationBytes;

            // At the point a marker is processed, all of its (direct) children
            // have accumulated their duration in TotalChildDurationNs.
            ulong exclusiveTimeNs;
            if (marker.TotalChildDurationNs > marker.DurationNs)
            {
                // It is actually possible for a marker's own duration to be less than the
                // summed durations of its children, due to some edge cases I have discovered.
                // These are:
                //
                // 1. The 64-bit precision of timing values in the raw profiler data stream is
                // lost when loaded into the ProfilerFrameData construct, where it is stored
                // as 32-bit floating point. This means that where timing values are very
                // close, the loss of precision can cause this to occur. This most commonly
                // occurs when long-running markers are cut and restarted across frame
                // boundaries.
                //
                // 2. When the process being profiled crashes, or is interrupted in some way,
                // the parent marker may not be terminated and will have a duration of zero.
                // Child markers that were correctly terminated will have their durations
                // correctly set, again causing this to occur.
                //
                // We handle this by simply treating these markers as having no exclusive time.
                exclusiveTimeNs = 0UL;
            }
            else
                exclusiveTimeNs = marker.DurationNs - marker.TotalChildDurationNs;

            // Add or update combined markers collection.
            if (combinedMarkers.TryGetValue(markerId, out CombinedMarkerData combinedMarker))
            {
                combinedMarker.AddInstance(exclusiveTimeNs, totalChildGCAllocationBytes);
            }
            else
            {
                combinedMarker = new CombinedMarkerData(cachedThreadIndex, frameIndex);
                combinedMarker.AddInstance(exclusiveTimeNs, totalChildGCAllocationBytes);
                combinedMarkers.Add(markerId, combinedMarker);
            }
        }

        protected Result BuildTopMarkersModelsWithCombinedMarkers(
            Dictionary<int, CombinedMarkerData> combinedMarkers,
            IProfilerCaptureDataService dataService,
            CancellationToken cancellationToken)
        {
            // Iterate over all combined markers to discover the top X markers by
            // exclusive time and GC allocation.
            var numberOfTopMarkers = m_NumberOfTopMarkers;
            var topMarkersByExclusiveTimeCollection = new TopMarkersCollection(numberOfTopMarkers);
            var topMarkersByGCAllocationCollection = new TopMarkersCollection(numberOfTopMarkers);
            AddCombinedMarkersIntoCollectionsIfNecessary(
                combinedMarkers,
                ref topMarkersByExclusiveTimeCollection,
                ref topMarkersByGCAllocationCollection,
                cancellationToken);

            return BuildTopMarkersModelsFromCollections(
                dataService,
                topMarkersByExclusiveTimeCollection,
                topMarkersByGCAllocationCollection,
                cancellationToken);
        }

        // Iterates over all provided combined markers, adding them if necessary to the
        // top marker collections for exclusive time and GC allocations.
        protected void AddCombinedMarkersIntoCollectionsIfNecessary(
            Dictionary<int, CombinedMarkerData> combinedMarkers,
            ref TopMarkersCollection topMarkersByExclusiveTimeCollection,
            ref TopMarkersCollection topMarkersByGCAllocationCollection,
            CancellationToken cancellationToken)
        {
            foreach (var kvp in combinedMarkers)
            {
                var markerId = kvp.Key;
                var combinedMarker = kvp.Value;
                var numberOfInstances = combinedMarker.NumberOfInstances;
                var anyFrameIndex = combinedMarker.AnyFrameIndex;
                var anyThreadIndex = combinedMarker.AnyThreadIndex;
                topMarkersByExclusiveTimeCollection.AddMarkerIfNecessary(
                    markerId,
                    combinedMarker.ExclusiveTimeNs,
                    Marker.Unit.TimeNanoseconds,
                    numberOfInstances,
                    anyFrameIndex,
                    anyThreadIndex);
                topMarkersByGCAllocationCollection.AddMarkerIfNecessary(
                    markerId,
                    combinedMarker.TotalChildGCAllocationBytes,
                    Marker.Unit.Bytes,
                    numberOfInstances,
                    anyFrameIndex,
                    anyThreadIndex);

                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        protected Result BuildTopMarkersModelsFromCollections(
            IProfilerCaptureDataService dataService,
            TopMarkersCollection topMarkersByExclusiveTimeCollection,
            TopMarkersCollection topMarkersByGCAllocationCollection,
            CancellationToken cancellationToken)
        {
            // Resolve the highest X markers that were discovered to sorted arrays.
            var topMarkersByExclusiveTime = topMarkersByExclusiveTimeCollection.ToSortedArray();
            var topMarkersByGCAllocation = topMarkersByGCAllocationCollection.ToSortedArray();

            // Resolve marker names for only the highest X markers. This is an
            // expensive operation, so it is only done once we have the final
            // list of top markers for the frame.
            ResolveAllMarkerNamesInCollection(topMarkersByExclusiveTime, dataService, cancellationToken);
            ResolveAllMarkerNamesInCollection(topMarkersByGCAllocation, dataService, cancellationToken);

            return new Result()
            {
                TopMarkersByExclusiveTime = new TopMarkersModel(topMarkersByExclusiveTime),
                TopMarkersByGCAllocation = new TopMarkersModel(topMarkersByGCAllocation),
            };
        }

        void ResolveAllMarkerNamesInCollection(
            Marker[] markers,
            IProfilerCaptureDataService dataService,
            CancellationToken cancellationToken)
        {
            for (var i = 0U; i < markers.Length; ++i)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ref var marker = ref markers[i];

                var markerId = marker.MarkerId;
                var frameIndex = marker.FrameIndex;
                var threadIndex = marker.ThreadIndex;

                // Fetch thread data from native memory.
                using (var threadData = dataService.GetRawFrameDataView(frameIndex, threadIndex))
                {
                    if (threadData.valid == false)
                        continue;

                    // Fetch names from native memory.
                    marker.Name = threadData.GetMarkerName(markerId);
                }
            }
        }

        public struct Result
        {
            public TopMarkersModel TopMarkersByExclusiveTime;
            public TopMarkersModel TopMarkersByGCAllocation;
        }

        protected class CombinedMarkerData
        {
            public CombinedMarkerData(
                int anyThreadIndex,
                int anyFrameIndex)
            {
                AnyThreadIndex = anyThreadIndex;
                AnyFrameIndex = anyFrameIndex;
                NumberOfInstances = 0U;
            }

            public int AnyThreadIndex { get; }
            public int AnyFrameIndex { get; }

            public ulong ExclusiveTimeNs { get; private set; }
            public ulong TotalChildGCAllocationBytes { get; private set; }
            public uint NumberOfInstances { get; private set; }

            public void AddInstance(
                ulong exclusiveTimeNs,
                ulong totalChildGCAllocationBytes)
            {
                ExclusiveTimeNs += exclusiveTimeNs;
                TotalChildGCAllocationBytes += totalChildGCAllocationBytes;
                NumberOfInstances++;
            }

            public void CombineWith(CombinedMarkerData other)
            {
                ExclusiveTimeNs += other.ExclusiveTimeNs;
                TotalChildGCAllocationBytes += other.TotalChildGCAllocationBytes;
                NumberOfInstances += other.NumberOfInstances;
            }
        }

        class MarkerData
        {
            public MarkerData(int sampleIndex, int childCount, ulong durationNs)
            {
                Reset(sampleIndex, childCount, durationNs);
            }

            public int SampleIndex { get; private set; }
            public ulong DurationNs { get; private set; }
            public ulong TotalChildDurationNs { get; set; }
            public ulong TotalChildGCAllocationBytes { get; set; }
            public int NumberOfChildrenToVisitRemaining { get; set; }

            public void Reset(int sampleIndex, int childCount, ulong durationNs)
            {
                SampleIndex = sampleIndex;
                DurationNs = durationNs;
                TotalChildDurationNs = 0UL;
                TotalChildGCAllocationBytes = 0UL;
                NumberOfChildrenToVisitRemaining = childCount;
            }
        }
    }
}
