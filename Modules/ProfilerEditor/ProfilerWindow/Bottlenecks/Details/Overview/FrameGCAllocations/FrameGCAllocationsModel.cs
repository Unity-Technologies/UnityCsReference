// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Profiling.Editor.UI
{
    readonly struct FrameGCAllocationsModel
    {
        public static FrameGCAllocationsModel Empty = default;

        public FrameGCAllocationsModel(ulong totalCount, long totalSize)
        {
            TotalCount = totalCount;
            TotalSize = totalSize;
        }

        public ulong TotalCount { get; }
        public long TotalSize { get; }
    }
}
