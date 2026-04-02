// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;

namespace Unity.Timeline.Foundation.Widgets
{
    readonly struct CanvasTransform
    {
        /// <summary>
        /// Canvas elements in the Foundation canvas are offset by 12 pixels to leave a gap when the display range begins at 0
        /// </summary>
        public const float foundationCanvasPixelsBeforeZero = 12f;

        public readonly TimeRange displayRange;
        public readonly float displayWidth;

        public CanvasTransform(TimeRange displayRange, float displayWidth)
        {
            this.displayRange = displayRange;
            this.displayWidth = displayWidth;
        }

        public DiscreteTime PixelToTime(float pixel)
        {
            return TimeViewUtility.PixelToTime(pixel, displayWidth, displayRange, foundationCanvasPixelsBeforeZero);
        }

        public float TimeToPixel(double time)
        {
            return TimeToPixel(new DiscreteTime(time));
        }

        public float TimeToPixel(DiscreteTime time)
        {
            return TimeViewUtility.TimeToPixel(time, displayWidth, displayRange, foundationCanvasPixelsBeforeZero);
        }

        public float DurationToPixelWidth(DiscreteTime duration)
        {
            return TimeViewUtility.DurationToPixelWidth(duration, displayWidth, displayRange, foundationCanvasPixelsBeforeZero);
        }

        public DiscreteTime PixelWidthToDuration(float width)
        {
            return TimeViewUtility.PixelWidthToDuration(width, displayWidth, displayRange, foundationCanvasPixelsBeforeZero);
        }
    }
}
