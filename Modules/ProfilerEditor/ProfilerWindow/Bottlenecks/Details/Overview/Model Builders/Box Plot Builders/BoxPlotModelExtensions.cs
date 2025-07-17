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
        public static BoxPlotModel BoxPlotModelFromSortedData((ulong, int)[] data)
        {
            if (data.Length == 0)
                throw new ArgumentException("Cannot build box plot model from sorted data. Data array is empty.");

            var minimumFrame = data[0];
            var minimumFrameDurationNs = minimumFrame.Item1;
            var minimumFrameIndex = minimumFrame.Item2;

            var maximumFrame = data[^1];
            var maximumFrameDurationNs = maximumFrame.Item1;
            var maximumFrameIndex = maximumFrame.Item2;

            var medianIndex = (uint)((data.Length - 1) * 0.5f);
            var medianFrame = data[medianIndex];
            var medianFrameDurationNs = medianFrame.Item1;
            var medianFrameIndex = medianFrame.Item2;

            var lowerQuartileIndex = (uint)((data.Length - 1) * 0.25f);
            var lowerQuartileFrameDurationNs = data[lowerQuartileIndex].Item1;

            var upperQuartileIndex = (uint)((data.Length - 1) * 0.75f);
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
