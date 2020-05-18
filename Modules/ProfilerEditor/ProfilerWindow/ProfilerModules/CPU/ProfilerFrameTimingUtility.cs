// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditorInternal.Profiling
{
    internal class ProfilerFrameTimingUtility
    {
        /// <summary>
        /// Calculates the time offset, in milliseconds, between the two specified frames.
        /// </summary>
        /// <param name="fromFrameIndex"></param>
        /// <param name="toFrameIndex"></param>
        /// <returns>The time offset in milliseconds.</returns>
        public static float TimeOffsetBetweenFrames(int fromFrameIndex, int toFrameIndex)
        {
            if (fromFrameIndex == toFrameIndex)
            {
                return 0f;
            }

            var timeOffset = 0f;
            if (toFrameIndex > fromFrameIndex)
            {
                timeOffset = TimeOffsetForLaterFrame(fromFrameIndex, toFrameIndex);
            }
            else
            {
                timeOffset = TimeOffsetForEarlierFrame(fromFrameIndex, toFrameIndex);
            }

            return timeOffset;
        }

        private static float TimeOffsetForEarlierFrame(int fromFrameIndex, int toFrameIndex)
        {
            var timeOffset = 0f;

            var frame = ProfilerDriver.GetPreviousFrameIndex(fromFrameIndex);
            while ((frame >= toFrameIndex) && (frame != -1))
            {
                using (var frameData = ProfilerDriver.GetRawFrameDataView(frame, 0))
                {
                    timeOffset -= frameData.frameTimeMs;

                    frame = ProfilerDriver.GetPreviousFrameIndex(frame);
                }
            }

            return timeOffset;
        }

        private static float TimeOffsetForLaterFrame(int fromFrameIndex, int toFrameIndex)
        {
            var timeOffset = 0f;

            var frame = fromFrameIndex;
            while ((frame < toFrameIndex) && (frame != -1))
            {
                using (var frameData = ProfilerDriver.GetRawFrameDataView(frame, 0))
                {
                    timeOffset += frameData.frameTimeMs;

                    frame = ProfilerDriver.GetNextFrameIndex(frame);
                }
            }

            return timeOffset;
        }
    }
}
