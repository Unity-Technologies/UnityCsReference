// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Profiling.Editor.UI
{
    readonly struct BoxPlotModel
    {
        public static BoxPlotModel Empty = default;

        public BoxPlotModel(
            ulong minimum,
            ulong maximum,
            ulong median,
            ulong lowerQuartile,
            ulong upperQuartile,
            int frameIndexOfMinimum,
            int frameIndexOfMaximum,
            int frameIndexOfMedian)
        {
            Minimum = new FrameValue(minimum, frameIndexOfMinimum);
            Maximum = new FrameValue(maximum, frameIndexOfMaximum);
            Median = new FrameValue(median, frameIndexOfMedian);
            LowerQuartile = lowerQuartile;
            UpperQuartile = upperQuartile;
        }

        public FrameValue Minimum { get; }
        public FrameValue Maximum { get; }
        public FrameValue Median { get; }
        public ulong LowerQuartile { get; }
        public ulong UpperQuartile { get; }

        public readonly struct FrameValue
        {
            public FrameValue(ulong value, int frameIndex)
            {
                Value = value;
                FrameIndex = frameIndex;
            }

            public ulong Value { get; }
            public int FrameIndex { get; }
        }
    }
}
