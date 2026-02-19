// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.IntegerTime;

namespace UnityEditor.Animations.AnimationWindow.TimelineFoundation
{
    static class TimeViewUtility
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FrameToPixel(int frame, double frameRate, float width, in TimeRange displayRange, float pixelOffset = 0f)
        {
            float time = frameRate == 0f ? 0f : (float)(frame / frameRate);
            return TimeToPixel(time, width, displayRange, pixelOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TimeToPixel(double time, float viewWidth, in TimeRange displayRange, float pixelOffset = 0f)
        {
            if (viewWidth - pixelOffset == 0f || displayRange == TimeRange.Empty) return 0f;
            float pixelsPerSecond = (viewWidth - pixelOffset) / (float)displayRange.duration;
            return TimeToPixel(time, pixelsPerSecond, (double)displayRange.start, pixelOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TimeToPixel(DiscreteTime time, float viewWidth, in TimeRange displayRange, float pixelOffset = 0f)
        {
            return TimeToPixel((double)time, viewWidth, displayRange, pixelOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TimeToPixel(double timeInSeconds, float pixelsPerSecond, double offsetInSeconds, float pixelOffset = 0f)
        {
            return (float)(timeInSeconds * pixelsPerSecond - offsetInSeconds * pixelsPerSecond) + (pixelOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime PixelToTime(float pixel, float viewWidth, in TimeRange displayRange, float pixelOffset = 0f)
        {
            if (viewWidth - pixelOffset == 0f || displayRange == TimeRange.Empty) return DiscreteTime.Zero;
            float pixelsPerSecond = (viewWidth - pixelOffset) / (float)displayRange.duration;
            return PixelToTime(pixel, pixelsPerSecond, (double)displayRange.start, pixelOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime PixelToTime(float pixel, float pixelsPerSecond, double offsetInSeconds, float pixelOffset = 0f)
        {
            if (pixelsPerSecond == 0f) return DiscreteTime.Zero;
            pixel -= pixelOffset;
            return new DiscreteTime(pixel / pixelsPerSecond + offsetInSeconds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime PixelWidthToDuration(float pixelWidth, float viewWidth, in TimeRange displayRange, float pixelOffset = 0f)
        {
            if (displayRange == TimeRange.Empty) return DiscreteTime.Zero;
            float pixelsPerSecond = (viewWidth - pixelOffset) / (float)displayRange.duration;
            return PixelWidthToDuration(pixelWidth, pixelsPerSecond);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime PixelWidthToDuration(float pixelWidth, float pixelsPerSecond)
        {
            if (pixelsPerSecond == 0f) return DiscreteTime.Zero;
            return new DiscreteTime(pixelWidth / pixelsPerSecond);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DurationToPixelWidth(double durationInSeconds, float viewWidth, in TimeRange displayRange, float pixelOffset = 0f)
        {
            if (displayRange == TimeRange.Empty || float.IsNaN(viewWidth)) return 0f;
            float pixelsPerSecond = (viewWidth - pixelOffset) / (float)displayRange.duration;
            return DurationToPixelWidth(durationInSeconds, pixelsPerSecond);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DurationToPixelWidth(double durationInSeconds, float pixelsPerSecond)
        {
            return (float)(durationInSeconds * pixelsPerSecond);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DurationToPixelWidth(DiscreteTime duration, float viewWidth, in TimeRange displayRange, float pixelOffset = 0f)
        {
            return DurationToPixelWidth((double)duration, viewWidth, displayRange, pixelOffset);
        }
    }
}
