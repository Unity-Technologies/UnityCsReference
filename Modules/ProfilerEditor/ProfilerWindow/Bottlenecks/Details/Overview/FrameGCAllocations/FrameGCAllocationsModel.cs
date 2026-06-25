// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Profiling.Editor.UI
{
    readonly struct FrameGCAllocationsModel
    {
        public static readonly FrameGCAllocationsModel Empty = default;

        public FrameGCAllocationsModel(int frameIndex, ulong totalCount, long totalSize)
        {
            FrameIndex = frameIndex;
            TotalCount = totalCount;
            TotalSize = totalSize;
        }

        public int FrameIndex { get; }
        public ulong TotalCount { get; }
        public long TotalSize { get; }
    }
}
