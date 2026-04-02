// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Timeline.Foundation.Time
{
    [Serializable]
    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal readonly struct TimeRange : IEquatable<TimeRange>
    {
        public static TimeRange Empty = new TimeRange(DiscreteTime.Zero, DiscreteTime.Zero);
        public static TimeRange MaxRange = new TimeRange(DiscreteTime.MinValue, DiscreteTime.MaxValue);

        public TimeRange(DiscreteTime start, DiscreteTime end)
        {
            if (end < start)
                throw new ArgumentException($"Range end cannot be smaller than start", nameof(end));

            this.start = start;
            this.end = end;
        }

        public TimeRange(double start, double end)
            : this(new DiscreteTime(start), new DiscreteTime(end)) { }

        public TimeRange(DiscreteTime start, double end)
            : this(start, new DiscreteTime(end)) { }

        public TimeRange(double start, DiscreteTime end)
            : this(new DiscreteTime(start), end) { }

        public readonly DiscreteTime start;
        public readonly DiscreteTime end;

        public DiscreteTime duration => end - start;

        public static bool operator ==(TimeRange c1, TimeRange c2)
        {
            return c1.start == c2.start && c1.end == c2.end;
        }

        public static bool operator !=(TimeRange c1, TimeRange c2)
        {
            return c1.start != c2.start || c1.end != c2.end;
        }

        public bool Equals(TimeRange rhs) { return this == rhs; }

        public override bool Equals(object o)
        {
            return o is TimeRange timeRange && Equals(timeRange);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (start.GetHashCode() * 397) ^ end.GetHashCode();
            }
        }

        public static TimeRange operator +(TimeRange range, DiscreteTime offset)
        {
            return new TimeRange(range.start + offset, range.end + offset);
        }

        public static TimeRange operator +(DiscreteTime offset, TimeRange range)
        {
            return range + offset;
        }

        public static TimeRange operator -(TimeRange range, DiscreteTime offset)
        {
            return new TimeRange(range.start - offset, range.end - offset);
        }

        public static TimeRange operator -(DiscreteTime offset, TimeRange range)
        {
            return range - offset;
        }

        public static implicit operator Vector2(TimeRange range)
        {
            return new Vector2((float)range.start, (float)range.end);
        }

        public static implicit operator TimeRange(Vector2 vec)
        {
            return new TimeRange(new DiscreteTime(vec.x), new DiscreteTime(vec.y));
        }

        public override string ToString()
        {
            return $"Start: {start:F3} End: {end:F3}";
        }
    }

    [VisibleToOtherModules("UnityEditor.TimelineFoundationModule")]
    internal static class RangeExtensions
    {
        /// <summary>
        /// Returns true if <paramref name="time"/> is located within <paramref name="range"/>.
        /// </summary>
        public static bool Intersects(this TimeRange range, DiscreteTime time)
        {
            return range.start <= time && range.end >= time;
        }

        /// <summary>
        /// Returns true if <paramref name="a"/> and <paramref name="b"/> have an overlap of duration greater than 0.
        /// </summary>
        public static bool Overlaps(this TimeRange a, TimeRange b)
        {
            return a.start < b.end && b.start < a.end;
        }

        /// <summary>
        /// Returns true if <paramref name="a"/> and <paramref name="b"/> have an overlap of duration greater or equal to 0.
        /// </summary>
        public static bool Intersects(this TimeRange a, TimeRange b)
        {
            if (a.start == b.start || b.end == a.end)
                return true;

            return a.start < b.end && b.start < a.end;
        }

        public static bool CompletelyOverlapsStrict(this TimeRange a, TimeRange b)
        {
            if (a == b)
                return false;

            return a.start < b.start && a.end > b.end;
        }

        public static bool CompletelyOverlaps(this TimeRange a, TimeRange b)
        {
            if (a == b)
                return true;

            return a.start <= b.start && a.end >= b.end;
        }

        public static TimeRange OverlapWith(this TimeRange a, TimeRange b)
        {
            if (!a.Overlaps(b))
                return TimeRange.Empty;

            DiscreteTime left = DiscreteTimeTimeExtensions.Max(a.start, b.start);
            DiscreteTime right = DiscreteTimeTimeExtensions.Min(a.end, b.end);

            return new TimeRange(left, right);
        }

        public static TimeRange Union(this TimeRange a, TimeRange b)
        {
            return new TimeRange(
                DiscreteTimeTimeExtensions.Min(a.start, b.start),
                DiscreteTimeTimeExtensions.Max(a.end, b.end));
        }

        public static TimeRange WithPadding(this TimeRange a, DiscreteTime padding)
        {
            return new TimeRange(a.start - padding, a.end + padding);
        }

        public static TimeRange Clamp(this TimeRange range, DiscreteTime minStart, DiscreteTime maxEnd)
        {
            var bounds = new TimeRange(minStart, maxEnd);
            return new TimeRange(range.start.Clamp(bounds), range.end.Clamp(bounds));
        }

        public static TimeRange Clamp(this TimeRange range, TimeRange b)
        {
            return Clamp(range, b.start, b.end);
        }

        public static TimeRange ClampStart(this TimeRange range, DiscreteTime minStart)
        {
            return Clamp(range, minStart, DiscreteTime.MaxValue);
        }

        public static TimeRange ClampEnd(this TimeRange range, DiscreteTime maxEnd)
        {
            return Clamp(range, DiscreteTime.MinValue, maxEnd);
        }
    }
}
