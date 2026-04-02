// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Profiling.Editor.UI
{
    static class BoxPlotModelExtensions
    {
        // The input is an array of tuples, whereby the first value is the data value,
        // and the second is the frame index on which it occurred.
        public static BoxPlotModel BoxPlotModelFromSortedData((ulong, int)[] data, bool skipEmptyFrames)
        {
            var trueLength = data.Length;

            if (trueLength == 0)
                throw new ArgumentException("Cannot build box plot model from sorted data. Data array is empty.");

            var emptyFrameCount = 0;
            if (skipEmptyFrames)
            {
                while (emptyFrameCount < trueLength && data[emptyFrameCount].Item1 == 0)
                    emptyFrameCount++;
            }

            // If we've somehow got all empty frames, return to default behaviour to avoid negative array access
            if (emptyFrameCount >= trueLength)
                emptyFrameCount = 0;

            var dataLengthNoEmptyFrames = trueLength - emptyFrameCount;

            var minimumFrame = data[emptyFrameCount];
            var minimumFrameDurationNs = minimumFrame.Item1;
            var minimumFrameIndex = minimumFrame.Item2;

            var maximumFrame = data[^1];
            var maximumFrameDurationNs = maximumFrame.Item1;
            var maximumFrameIndex = maximumFrame.Item2;

            var medianIndex = emptyFrameCount + (uint)((dataLengthNoEmptyFrames - 1) * 0.5f);
            var medianFrame = data[medianIndex];
            var medianFrameDurationNs = medianFrame.Item1;
            var medianFrameIndex = medianFrame.Item2;

            var lowerQuartileIndex = emptyFrameCount + (uint)((dataLengthNoEmptyFrames - 1) * 0.25f);
            var lowerQuartileFrameDurationNs = data[lowerQuartileIndex].Item1;

            var upperQuartileIndex = emptyFrameCount + (uint)((dataLengthNoEmptyFrames - 1) * 0.75f);
            var upperQuartileFrameDurationNs = data[upperQuartileIndex].Item1;

            return new BoxPlotModel(
                minimumFrameDurationNs,
                maximumFrameDurationNs,
                medianFrameDurationNs,
                lowerQuartileFrameDurationNs,
                upperQuartileFrameDurationNs,
                minimumFrameIndex,
                maximumFrameIndex,
                medianFrameIndex);
        }
    }
}
