// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Profiling.Editor.UI
{
    static class BuilderUtility
    {
        public static Range RangeForAllProfilerData(IProfilerCaptureDataService dataService)
        {
            var frameCount = dataService.FrameCount;
            if (frameCount == 0)
                return new Range(0, 0); // No profiler data; return a zero length range.

            var firstFrameIndex = dataService.FirstFrameIndex;
            return new Range(firstFrameIndex, firstFrameIndex + frameCount);
        }

        public static void ThrowIfFrameRangeIsOutOfBounds(Range frameRange, IProfilerCaptureDataService dataService)
        {
            if (frameRange.Equals(Range.All))
                frameRange = RangeForAllProfilerData(dataService);

            var startIndex = frameRange.Start.Value;
            ThrowIfFrameIndexIsOutOfBounds(startIndex, dataService);

            var inclusiveEndIndex = frameRange.End.Value - 1;
            ThrowIfFrameIndexIsOutOfBounds(inclusiveEndIndex, dataService);
        }

        public static void ThrowIfFrameIndexIsOutOfBounds(int frameIndex, IProfilerCaptureDataService dataService)
        {
            var frameCount = dataService.FrameCount;
            if (frameCount == 0)
                throw new ProfilerFrameIndexOutOfBounds();

            var isValidFrameIndex =
                frameIndex >= dataService.FirstFrameIndex
                && frameIndex < dataService.FirstFrameIndex + frameCount;
            if (isValidFrameIndex == false)
                throw new ProfilerFrameIndexOutOfBounds();
        }
    }
}
