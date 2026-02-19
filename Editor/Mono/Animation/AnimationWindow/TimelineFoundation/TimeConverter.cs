// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;

using FrameRate = UnityEngine.Playables.FrameRate;

namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    readonly struct TimeConverter
    {
        readonly TimeFormat m_TimeFormat;
        readonly FrameRate m_FrameRate;
        readonly TimeTransform m_TimeTransform;

        public TimeConverter(TimeFormat format, FrameRate frameRate, TimeTransform timeTransform)
        {
            if (!frameRate.IsValid())
                throw new ArgumentException("frame rate cannot be Invalid");
            m_TimeFormat = format;
            m_FrameRate = frameRate;
            m_TimeTransform = timeTransform;
        }

        public TimeFormat format => m_TimeFormat;
        public FrameRate frameRate => m_FrameRate;

        public string ToTimeString(DiscreteTime time, string format = "f2")
        {
            return m_TimeFormat.ToTimeString((double)m_TimeTransform.Transform(time), m_FrameRate, format);
        }

        public string ToTimeStringWithDelta(DiscreteTime time, DiscreteTime delta, string format = "f2")
        {
            return m_TimeFormat.ToTimeStringWithDelta((double)m_TimeTransform.Transform(time), m_FrameRate, (double)delta, format);
        }

        public DiscreteTime FromTimeString(string timeString, double defaultValue)
        {
            return m_TimeTransform.InverseTransform((DiscreteTime)m_TimeFormat.FromTimeString(timeString, m_FrameRate, defaultValue));
        }

        public double ToFrames(DiscreteTime time)
        {
            double frames = ToExactFrames(time);
            double previousFrame = Math.Floor(frames + TimeUtility.GetEpsilon((double)time, m_FrameRate.rate));
            double nextFrame = Math.Ceiling(frames - TimeUtility.GetEpsilon((double)time, m_FrameRate.rate));
            return Math.Abs(previousFrame - frames) >= Math.Abs(nextFrame - frames) ? nextFrame : previousFrame;
        }

        public double ToExactFrames(DiscreteTime time)
        {
            GetRational(m_FrameRate, out int numerator, out int denominator);
            return (double)(time * numerator / denominator);
        }

        public DiscreteTime ToDiscreteTime(double frames)
        {
            GetRational(m_FrameRate, out int numerator, out int denominator);
            return new DiscreteTime(frames * denominator / numerator);
        }

        public DiscreteTime RoundToFrame(DiscreteTime time)
        {
            return ToDiscreteTime(ToFrames(time));
        }

        public TimeRange SnapToFrame(TimeRange range)
        {
            DiscreteTime delta = RoundToFrame(range.start) - range.start;
            return range + delta;
        }

        static void GetRational(FrameRate frameRate, out int numerator, out int denominator)
        {
            int rate = FrameRate.FrameRateToInt(frameRate);
            numerator = frameRate.dropFrame ? -1000 * rate : rate;
            denominator = frameRate.dropFrame ? 1001 : 1;
        }
    }
}
