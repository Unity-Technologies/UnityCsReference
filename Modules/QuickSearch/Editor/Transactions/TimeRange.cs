// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Search
{
    enum LimitType
    {
        Inferior,
        Superior
    }

    readonly struct TimeRangeLimit : IComparable<DateTime>, IEquatable<DateTime>
    {
        public readonly DateTime timeStamp;
        public readonly bool exclusive;
        public readonly LimitType limitType;

        public TimeRangeLimit(DateTime timeStamp, bool exclusive, LimitType limitType)
        {
            this.timeStamp = timeStamp;
            this.exclusive = exclusive;
            this.limitType = limitType;
        }

        public int CompareTo(DateTime other)
        {
            return timeStamp.CompareTo(other);
        }

        public bool Equals(DateTime other)
        {
            return timeStamp == other;
        }

        public bool Equals(TimeRangeLimit other)
        {
            return timeStamp.Equals(other.timeStamp) && exclusive == other.exclusive;
        }

        public override bool Equals(object obj)
        {
            return obj is TimeRangeLimit other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (timeStamp.GetHashCode() * 397) ^ exclusive.GetHashCode();
            }
        }

        public static bool operator<(TimeRangeLimit lhs, DateTime other)
        {
            return lhs.timeStamp < other;
        }

        public static bool operator>(TimeRangeLimit lhs, DateTime other)
        {
            return lhs.timeStamp > other;
        }

        public static bool operator<=(TimeRangeLimit lhs, DateTime other)
        {
            return lhs.timeStamp <= other;
        }

        public static bool operator>=(TimeRangeLimit lhs, DateTime other)
        {
            return lhs.timeStamp >= other;
        }

        public static bool operator==(TimeRangeLimit lhs, DateTime other)
        {
            return lhs.timeStamp == other;
        }

        public static bool operator!=(TimeRangeLimit lhs, DateTime other)
        {
            return lhs.timeStamp != other;
        }

        public bool InRange(DateTime time)
        {
            if (limitType == LimitType.Inferior)
            {
                if (this < time)
                    return true;
            }
            else
            {
                if (this > time)
                    return true;
            }

            if (exclusive)
                return false;

            if (this == time)
                return true;

            return false;
        }
    }

    struct TimeRange
    {
        public TimeRangeLimit first;
        public TimeRangeLimit last;

        public static TimeRange Until(DateTime date, bool exclusive)
        {
            return new TimeRange
            {
                first = new TimeRangeLimit(DateTime.MinValue, false, LimitType.Inferior),
                last = new TimeRangeLimit(date, exclusive, LimitType.Superior)
            };
        }

        public static TimeRange From(DateTime date, bool exclusive)
        {
            return new TimeRange
            {
                first = new TimeRangeLimit(date, exclusive, LimitType.Inferior),
                last = new TimeRangeLimit(DateTime.MaxValue, false, LimitType.Superior),
            };
        }

        public static TimeRange Between(DateTime first, bool firstExclusive, DateTime last, bool lastExclusive)
        {
            return new TimeRange
            {
                first = new TimeRangeLimit(first, firstExclusive, LimitType.Inferior),
                last = new TimeRangeLimit(last, lastExclusive, LimitType.Superior),
            };
        }

        public static TimeRange All()
        {
            return new TimeRange
            {
                first = new TimeRangeLimit(DateTime.MinValue, false, LimitType.Inferior),
                last = new TimeRangeLimit(DateTime.MaxValue, false, LimitType.Superior)
            };
        }

        public bool InRange(DateTime time)
        {
            return first.InRange(time) && last.InRange(time);
        }

        public override string ToString()
        {
            return $"{(first.exclusive ? "]" : "[")}{first.timeStamp:u}, {last.timeStamp:u}{(last.exclusive ? "[" : "]")}";
        }
    }
}
