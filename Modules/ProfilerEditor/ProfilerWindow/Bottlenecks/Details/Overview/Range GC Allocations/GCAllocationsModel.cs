// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Profiling.Editor.UI
{
    readonly struct GCAllocationsModel
    {
        public static GCAllocationsModel Empty = default;

        public GCAllocationsModel(
            ulong total,
            ulong maximumCallsInFrame,
            ulong maximumSizeInFrame,
            int frameIndexOfMaximumCalls,
            int frameIndexOfMaximumSize)
        {
            Total = total;
            MaximumCallsInFrame = new FrameValue(maximumCallsInFrame, frameIndexOfMaximumCalls);
            MaximumSizeInFrame = new FrameValue(maximumSizeInFrame, frameIndexOfMaximumSize);
        }

        public ulong Total { get; }
        public FrameValue MaximumCallsInFrame { get; }
        public FrameValue MaximumSizeInFrame { get; }

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
