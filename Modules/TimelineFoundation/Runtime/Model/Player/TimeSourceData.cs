// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;

namespace Unity.Timeline.Foundation.Model
{
    readonly struct TimeSourceData : IEquatable<TimeSourceData>
    {
        public static readonly TimeSourceData Zero = new(TimeTransform.Identity);

        public readonly DiscreteTime localTime;
        public readonly TimeTransform timeTransform;
        public DiscreteTime globalTime => timeTransform.Transform(localTime);

        public TimeSourceData(TimeTransform timeTransform, DiscreteTime localTime = default)
            : this()
        {
            this.timeTransform = timeTransform;
            this.localTime = localTime;
        }

        public void Deconstruct(out DiscreteTime globalTime, out DiscreteTime localTime, out TimeTransform timeTransform)
        {
            globalTime = this.globalTime;
            localTime = this.localTime;
            timeTransform = this.timeTransform;
        }

        public void Deconstruct(out DiscreteTime localTime, out TimeTransform timeTransform)
        {
            localTime = this.localTime;
            timeTransform = this.timeTransform;
        }

        public bool Equals(TimeSourceData other)
        {
            return localTime.Equals(other.localTime)
                && timeTransform.Equals(other.timeTransform);
        }

        public override bool Equals(object obj)
        {
            return obj is TimeSourceData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(localTime, timeTransform);
        }

        public static TimeSourceData CreateFromGlobalTime(TimeTransform timeTransform, DiscreteTime globalTime = default)
        {
            return new TimeSourceData(timeTransform, timeTransform.InverseTransform(globalTime));
        }

        public override string ToString()
        {
            return $"{nameof(TimeSourceData)}: {nameof(globalTime)}: {globalTime}, {nameof(localTime)}: {localTime}, {nameof(timeTransform)}: {timeTransform}";
        }
    }

    static class TimeSourceDataExtensions
    {
        public static TimeSourceData CopyWithLocalTime(this TimeSourceData previous, DiscreteTime localTime)
        {
            return new TimeSourceData(previous.timeTransform, localTime);
        }

        public static TimeSourceData CopyWithGlobalTime(this TimeSourceData previous, DiscreteTime globalTime)
        {
            return TimeSourceData.CreateFromGlobalTime(previous.timeTransform, globalTime);
        }

        public static TimeSourceData CopyWithTimeTransform(this TimeSourceData previous, TimeTransform tr)
        {
            return new TimeSourceData(tr, previous.localTime);
        }
    }
}
