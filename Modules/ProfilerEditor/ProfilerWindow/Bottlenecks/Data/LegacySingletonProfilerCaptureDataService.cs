// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Profiling;
using UnityEditorInternal;

namespace Unity.Profiling.Editor
{
    // A wrapper around the existing singleton API for Profiler capture data (ProfilerDriver). This exists to improve
    // the testability of components that depend upon Profiler data (so data can be mocked up), and makes it easier
    // going forward to migrate away from the singleton.
    class LegacySingletonProfilerCaptureDataService : IProfilerCaptureDataService
    {
        public LegacySingletonProfilerCaptureDataService()
        {
            ProfilerDriver.profileLoaded += OnProfileLoaded;
            ProfilerDriver.profileCleared += OnProfileCleared;
            ProfilerDriver.NewProfilerFrameRecorded += OnNewProfilerFrameRecorded;
        }

        public int FrameCount
        {
            get
            {
                if (!HasData)
                    return 0;

                if (ProfilerHasMoreFramesLoadedThanIsReportedByMaxHistoryLength)
                    return ProfilerUserSettings.frameCount;

                return (ProfilerDriver.lastFrameIndex - ProfilerDriver.firstFrameIndex) + 1;
            }
        }

        public int FirstFrameIndex
        {
            get
            {
                if (ProfilerHasMoreFramesLoadedThanIsReportedByMaxHistoryLength)
                    return (ProfilerDriver.lastFrameIndex - ProfilerUserSettings.frameCount) + 1;

                return ProfilerDriver.firstFrameIndex;
            }
        }

        bool HasData
        {
            get
            {
                // Work around the legacy ProfilerDriver API to detect no data.
                return ProfilerDriver.firstFrameIndex != -1;
            }
        }

        bool ProfilerHasMoreFramesLoadedThanIsReportedByMaxHistoryLength
        {
            get
            {
                if (!HasData)
                    return false;

                // There is an existing behaviour whereby there can be more frames loaded in the Profiler
                // than the Profiler's maxHistoryLength states. We should be able to trust the Profiler's
                // maxHistoryLength to be the maximum length of Profiler history, so we adjust the
                // FirstFrameIndex to account for that. In this case, we assume the first loaded index
                // is the last index minus the history length because this is what the existing charts do.
                // Note: maxHistoryLength is called ProfilerUserSettings.frameCount in managed code.
                var loadedFrameCount = (ProfilerDriver.lastFrameIndex - ProfilerDriver.firstFrameIndex) + 1;
                return loadedFrameCount > ProfilerUserSettings.frameCount;
            }
        }

        public event Action DataCleared;
        public event Action DataLoaded;
        public event Action<int, int> NewFrameRecorded;

        public void GetCounterValues(
            string categoryName,
            string counterName,
            int firstFrameIndex,
            Span<float> buffer,
            out float maxValue)
        {
            ProfilerDriver.GetCounterValuesBatchByCategoryFast(
                categoryName,
                counterName,
                firstFrameIndex,
                buffer,
                out maxValue);
        }

        public RawFrameDataView GetRawFrameDataView(int frameIndex, int threadIndex)
        {
            if (frameIndex < ProfilerDriver.firstFrameIndex)
                throw new ArgumentOutOfRangeException(nameof(frameIndex), $"frameIndex is out of range of loaded profiler data. Must be greater or equal than {ProfilerDriver.firstFrameIndex}");
            if (frameIndex > ProfilerDriver.lastFrameIndex)
                throw new ArgumentOutOfRangeException(nameof(frameIndex), $"frameIndex is out of range of loaded profiler data. Must be smaller or equal than {ProfilerDriver.lastFrameIndex}");

            return ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex);
        }

        public HierarchyFrameDataView GetHierarchyFrameDataView(int frameIndex, int threadIndex, HierarchyFrameDataView.ViewModes viewMode, int sortColumn, bool sortAscending)
        {
            if (frameIndex < ProfilerDriver.firstFrameIndex)
                throw new ArgumentOutOfRangeException(nameof(frameIndex), $"frameIndex is out of range of loaded profiler data. Must be greater or equal than {ProfilerDriver.firstFrameIndex}");
            if (frameIndex > ProfilerDriver.lastFrameIndex)
                throw new ArgumentOutOfRangeException(nameof(frameIndex), $"frameIndex is out of range of loaded profiler data. Must be smaller or equal than {ProfilerDriver.lastFrameIndex}");

            return ProfilerDriver.GetHierarchyFrameDataView(frameIndex, threadIndex, viewMode, sortColumn, sortAscending);
        }

        public void Dispose()
        {
            ProfilerDriver.profileCleared -= OnProfileCleared;
            ProfilerDriver.profileLoaded -= OnProfileLoaded;
            ProfilerDriver.NewProfilerFrameRecorded -= OnNewProfilerFrameRecorded;
        }

        void OnProfileCleared()
        {
            DataCleared?.Invoke();
        }

        void OnProfileLoaded()
        {
            DataLoaded?.Invoke();
        }

        void OnNewProfilerFrameRecorded(int connectionId, int newFrameIndex)
        {
            NewFrameRecorded?.Invoke(connectionId, newFrameIndex);
        }
    }
}
