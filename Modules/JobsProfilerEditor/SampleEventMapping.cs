// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEditorInternal.Profiling;

namespace UnityEditor.JobsProfiling;

internal static class SampleEventMapping
{
    internal static int FindEventIndex(in FrameData frameData, int rawThreadIndex, int rawSampleIndex)
    {
        if (rawSampleIndex < 0)
            return -1;

        for (int threadIndex = 0; threadIndex < frameData.threads.Length; ++threadIndex)
        {
            ThreadInfo thread = frameData.threads[threadIndex];
            if (thread.rawThreadIndex != rawThreadIndex)
                continue;

            for (int eventIndex = thread.eventStart; eventIndex < thread.eventEnd; ++eventIndex)
            {
                if (frameData.events[eventIndex].rawSampleIndex == rawSampleIndex)
                    return eventIndex;
            }

            return -1;
        }

        return -1;
    }

    internal static bool TryFindRawSample(in FrameData frameData, int eventIndex, out int rawThreadIndex, out int rawSampleIndex)
    {
        rawThreadIndex = -1;
        rawSampleIndex = -1;

        if (eventIndex < 0 || eventIndex >= frameData.events.Length)
            return false;

        ProfilingEvent profilingEvent = frameData.events[eventIndex];
        if (profilingEvent.rawSampleIndex < 0)
            return false;

        rawThreadIndex = frameData.threads[profilingEvent.threadIndex].rawThreadIndex;
        rawSampleIndex = profilingEvent.rawSampleIndex;
        return true;
    }

    internal static bool TryFindRawSample(ProfilerTimeSampleSelection selection, IProfilerSampleNameProvider sampleNameProvider, int frameIndex, out int rawThreadIndex, out int rawSampleIndex)
    {
        rawSampleIndex = RawFrameDataView.invalidSampleIndex;
        rawThreadIndex = selection.GetThreadIndex(frameIndex);

        int markerPathDepth = selection.markerPathDepth;
        if (rawThreadIndex == FrameDataView.invalidThreadIndex || markerPathDepth <= 0)
            return false;

        using (var frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, rawThreadIndex))
        {
            if (frameData == null || !frameData.valid)
                return false;

            var markerNamePath = selection.markerNamePath;
            var markerIdPath = new List<int>(markerPathDepth);
            for (int i = 0; i < markerPathDepth; ++i)
            {
                markerIdPath.Add(frameData.GetMarkerId(markerNamePath[i]));
            }

            if (selection.safeFrameIndex == frameIndex &&
                selection.rawSampleIndex < frameData.sampleCount &&
                frameData.GetSampleMarkerId(selection.rawSampleIndex) == markerIdPath[markerPathDepth - 1])
            {
                rawSampleIndex = selection.rawSampleIndex;
                return true;
            }

            string name = null;
            var longestMatchingPath = new List<int>(markerPathDepth);
            rawSampleIndex = ProfilerTimelineGUI.FindFirstSampleThroughMarkerPath(
                frameData, sampleNameProvider, markerIdPath, markerPathDepth, ref name,
                longestMatchingPath: longestMatchingPath);
            if (rawSampleIndex == RawFrameDataView.invalidSampleIndex && longestMatchingPath.Count > 0)
                rawSampleIndex = longestMatchingPath[longestMatchingPath.Count - 1];
            return rawSampleIndex != RawFrameDataView.invalidSampleIndex;
        }
    }
}
