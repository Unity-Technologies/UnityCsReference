// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;

namespace Unity.Timeline.Foundation.Widgets
{
    static class EventExtensions
    {
        public static TimeRange ApplyToTimeRange(this PanEvent panEvent, TimeRange range)
        {
            try
            {
                checked
                {
                    DiscreteTime timeDelta = range.duration * panEvent.factor.x;
                    TimeRange newRange = range + timeDelta;

                    if (newRange.start < DiscreteTime.Zero)
                        newRange = new TimeRange(DiscreteTime.Zero, newRange.duration);

                    return newRange;
                }
            }
            catch (OverflowException)
            {
                //bail out if we overflow by returning the original range
                return range;
            }
        }

        public static TimeRange ApplyToTimeRange(this ZoomEvent zoomEvent, TimeRange range)
        {
            if (zoomEvent.centerRatio < 0f)
                throw new ArgumentOutOfRangeException(nameof(zoomEvent.centerRatio), "Negative values are not supported");
            if (zoomEvent.scale < 0f)
                throw new ArgumentOutOfRangeException(nameof(zoomEvent.scale), "Negative values are not supported");

            try
            {
                checked
                {
                    DiscreteTime centerTime = range.duration * zoomEvent.centerRatio + range.start;
                    DiscreteTime newDuration = range.duration * zoomEvent.scale;
                    DiscreteTime newStart = centerTime - (newDuration * zoomEvent.centerRatio);

                    TimeRange newRange = newStart.Value + newDuration.Value < newStart.Value ?
                        new TimeRange(newStart, DiscreteTime.MaxValue) :
                        new TimeRange(newStart, newStart + newDuration);

                    if (newRange.start < DiscreteTime.Zero)
                        newRange += newRange.start.Abs();

                    return newRange;
                }
            }
            catch (OverflowException)
            {
                //bail out if we overflow by returning the original range
                return range;
            }
        }
    }
}
