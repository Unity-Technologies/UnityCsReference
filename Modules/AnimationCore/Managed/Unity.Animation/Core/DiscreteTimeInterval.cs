// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.IntegerTime;

namespace Unity.Animations
{
    /// <summary>
    /// Structure representing a time interval.
    /// </summary>
    public struct DiscreteTimeInterval : System.IEquatable<DiscreteTimeInterval>, IFormattable
    {
        /// <summary>The largest representable time interval, contains an invalid duration (due to overflow) </summary>
        public static readonly DiscreteTimeInterval MaxRange = new DiscreteTimeInterval(DiscreteTime.MinValue, DiscreteTime.MaxValue);

        ///<summary>Unit time interval [0, 1]</summary>
        public static readonly DiscreteTimeInterval UnitRange = new DiscreteTimeInterval(DiscreteTime.Zero, DiscreteTimeExtensions.One);

        /// <summary>The starting time of the interval</summary>
        public readonly DiscreteTime Start;

        /// <summary>The ending time of the interval</summary>
        public readonly DiscreteTime End;

        /// <summary>Constructor that takes two time values to create an interval. </summary>
        /// <param name="time0"></param>
        /// <param name="time1"></param>
        public DiscreteTimeInterval(DiscreteTime time0, DiscreteTime time1)
        {
            Start = time0.Min(time1);
            End = time0.Max(time1);
        }

        /// <summary>The duration of the interval</summary>
        /// <remarks>
        /// The duration of the interval will be capped at DiscreteTime.MaxValue, so very large intervals may not satisfy
        ///   end = start + duration
        /// </remarks>
        public DiscreteTime Duration
        {
            get
            {
                // overflow
                if (unchecked(End.Value - Start.Value) < 0)
                    return DiscreteTime.MaxValue;
                return DiscreteTime.FromTicks(End.Value - Start.Value);
            }
        }

        /// <summary> The duration of the interval, expressed as a unsigned long </summary>
        /// <remarks>Use this version to if the duration is possibly larger than DiscreteTime.MaxValue</remarks>
        public ulong DurationAsTick => (ulong)End.Value - (ulong)Start.Value;

        /// <summary>Returns true only if a given time value is inside the interval.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(DiscreteTime t) => t >= Start && t <= End;

        /// <summary>Returns true only if another interval overlaps.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(DiscreteTimeInterval other) => Start <= other.End && other.Start <= End;

        /// <summary>Returns the smallest interval containing both this interval and another one.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DiscreteTimeInterval Union(DiscreteTimeInterval other) => new DiscreteTimeInterval(Start.Min(other.Start), End.Max(other.End));

        /// <summary>Limits a time value to the interval.</summary>
        /// <param name="time">The time value to limit to the interval.</param>
        /// <returns><see cref="Start"/> if <paramref name="time"/> is before the interval, <see cref="End"/> if it is after the interval, and <paramref name="time"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DiscreteTime Clamp(DiscreteTime time) => End.Min(Start.Max(time));

        /// <summary>Returns true if the time is equal to a given time, false otherwise.</summary>
        public override bool Equals(object o) => Equals((DiscreteTimeInterval)o);

        /// <summary>Returns true if the same range is represented.</summary>
        public bool Equals(DiscreteTimeInterval other) =>  Start == other.Start && End == other.End;

        /// <summary>Returns a hash code for the time.</summary>
        public override int GetHashCode() => (Start.GetHashCode() * 397) ^ End.GetHashCode();

        /// <summary>Returns a string representation of the time.</summary>
        public override string ToString()
        {
            return $"{nameof(DiscreteTimeInterval)}({Start}, {End})";
        }

        /// <summary>Returns a string representation of the time using a specified format and culture-specific format information.</summary>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return $"{nameof(DiscreteTimeInterval)}({Start.ToString(format,formatProvider)}, {End.ToString(format, formatProvider)})";
        }
    }
}
