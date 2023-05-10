// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Profiling;

namespace Unity.Profiling.Editor
{
    interface IProfilerCaptureDataService : IDisposable
    {
        // The number of frames stored in the capture data.
        int FrameCount { get; }

        // The index of the first frame stored in the capture data.
        int FirstFrameIndex { get; }

        // Obtain the frame data for the specified frame and thread.
        RawFrameDataView GetRawFrameDataView(int frameIndex, int threadIndex);

        // Obtain the specified counter's values, as floats, over a range of frames beginning at firstFrameIndex to firstFrameIndex + buffer.Length.
        void GetCounterValues(
            string categoryName,
            string counterName,
            int firstFrameIndex,
            Span<float> buffer,
            out float maxValue);

        public event Action NewDataLoadedOrCleared;
    }
}
