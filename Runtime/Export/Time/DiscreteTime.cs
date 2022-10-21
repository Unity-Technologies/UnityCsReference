// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

// Requirements:
//
//   - Contain consistent precision in the fractional component no matter how large/small the integral value
//        - timeline clips start/end points should not lose fractional precision at larger time values.
//        - i.e. sub-frame accuracy is the same at t = 0 and t = 1000000
//   - Discrete to avoid numerical imprecision when evaluating ranges.
//         - use case: adjacent clips in timeline should never evaluate both on, or both off due to numerical issues
//   - Compatible with common frame rates
//        - 24,25,30,50,60,120,240, 1000, 44kHz, 192KHz plus 'drop frame' like 2997 (which is 30 * 1000/1001)
//        - i.e. no error is introduced when advancing by a fixed frame rate
//   - Hold an absolute or delta time value, and a reasonably large range of time for cinematic production
//         - implementation below is about +/- 2072 years
//   - Performant.
//         - intended to be used in game engine
//   - Serializable without data loss in text formats.
//   - Reasonably compatible with floating point times (single and double precision).
//        - A lot of game code uses float to represent time.
//        - e.g. 1.0f/30.0f should convert to EXACTLY the number of ticks in a 30 fps frame
//        -      void Update() { time += 1f/30; } should be exactly time = frames / 30.f after N frames
//
// References: TimeRef, which uses a similar constant https://pdfs.semanticscholar.org/a048/a7b6137a298290cd37cf8288c6e422f565b7.pdf (of by a factor of 10)
//             Flick: https://github.com/OculusVR/Flicks
//             Flick value does not convert to/from float well. (Flick/5 does, which is actually the suggested TimeRef value)


namespace Unity.IntegerTime
{
    /// <summary>
    /// Data-type representing a discrete time value.
    /// </summary>
    public readonly struct DiscreteTime : IEquatable<DiscreteTime>, IFormattable, IComparable<DiscreteTime>
    {
        /// <summary> The underlying value. It represents the number of discrete ticks </summary>
        public readonly long Value;

        /// <summary>The zero value.</summary>
        public static readonly DiscreteTime Zero = new();

        /// <summary>
        /// The minimum representable time
        /// </summary>
        public static readonly DiscreteTime MinValue = new(long.MinValue, 0);

        /// <summary>
        /// The maximum representable time
        /// </summary>
        public static readonly DiscreteTime MaxValue = new(long.MaxValue, 0);


        // 2^9 * 3^2* 5^3 * 7^2  supports frame rates listed below
        const int Pow2Exp = 9;
        const uint Pow2Tps = (1 << Pow2Exp);
        const uint NonPow2Tps = 3 * 3 * (5 * 5 * 5 * 5) * 7 * 7;

        static readonly int TicksPerSecondBits = (int)Mathf.Ceil(Mathf.Log(TicksPerSecond, 2));
        static readonly int NonPow2TpsBits = (int)Mathf.Ceil(Mathf.Log(NonPow2Tps, 2));

        public const uint TicksPerSecond =  Pow2Tps * NonPow2Tps;
        public const double Tick = 1.0 / TicksPerSecond;
        public const long MaxValueSeconds = long.MaxValue / TicksPerSecond;
        public const long MinValueSeconds = long.MinValue / TicksPerSecond;

        // Constants for supported frame rates
        public const uint Tick5Fps = TicksPerSecond / 5;
        public const uint Tick10Fps = TicksPerSecond / 10;
        public const uint Tick12Fps = TicksPerSecond / 12;
        public const uint Tick15Fps = TicksPerSecond / 15;
        public const uint Tick2397Fps = (uint)((ulong)Tick24Fps * 1001 / 1000);
        public const uint Tick24Fps = TicksPerSecond / 24;
        public const uint Tick25Fps = TicksPerSecond / 25;
        public const uint Tick2997Fps = (uint)((ulong)Tick30Fps * 1001 / 1000);
        public const uint Tick30Fps = TicksPerSecond / 30;
        public const uint Tick48Fps = TicksPerSecond / 48;
        public const uint Tick50Fps = TicksPerSecond / 50;
        public const uint Tick5995Fps = Tick60Fps * 1001 / 1000;
        public const uint Tick60Fps = TicksPerSecond / 60;
        public const uint Tick90Fps = TicksPerSecond / 90;
        public const uint Tick11988Fps = Tick120Fps * 1001 / 1000;
        public const uint Tick120Fps = TicksPerSecond / 120;
        public const uint Tick240Fps = TicksPerSecond / 240;
        public const uint Tick1000Fps = TicksPerSecond / 1000;
        public const uint Tick8Khz = TicksPerSecond / 8000;
        public const uint Tick16Khz = TicksPerSecond / 16000;
        public const uint Tick22Khz = TicksPerSecond / 22050;
        public const uint Tick44Khz = TicksPerSecond / 44100;
        public const uint Tick48Khz = TicksPerSecond / 48000;
        public const uint Tick88Khz = TicksPerSecond / 88200;
        public const uint Tick96Khz = TicksPerSecond / 96000;
        public const uint Tick192Khz = TicksPerSecond / 192000;


        /// <summary>Constructs a time value from a time value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DiscreteTime(DiscreteTime x) { Value = x.Value; }

        /// <summary>Constructs a time value from a float value in seconds.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DiscreteTime(float v) { Value = (long)Math.Round((double)v * TicksPerSecond); }

        /// <summary>Constructs a time value from a double value in seconds.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DiscreteTime(double v) { Value = (long)Math.Round(v * TicksPerSecond); }

        /// <summary>Constructs a time value from a long value representing an integral value in seconds.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DiscreteTime(long v) { Value = v * TicksPerSecond; }

        /// <summary>Constructs a time value from a long value representing an integral value in seconds.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DiscreteTime(int v) { Value = v * TicksPerSecond; }

        /// <summary>Private implementation for FromTicks method</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        DiscreteTime(long v, int _) { Value = v; }

        /// <summary>Explicitly converts a tick value to a fixedTime value</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime FromTicks(long v) { return new DiscreteTime(v, 0);}

        /// <summary>Explicitly converts a float value to a fixedTime value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator DiscreteTime(float v) { return new DiscreteTime(v); }

        /// <summary>Explicitly converts a double value to a fixedTime value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator DiscreteTime(double v) { return new DiscreteTime(v); }

        /// <summary>Explicitly converts a time value to a float value in seconds.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator float(DiscreteTime d) { return (float)((double)d.Value / TicksPerSecond); }

        /// <summary>Explicitly converts a time value to a double value in seconds.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator double(DiscreteTime d) { return (double)d.Value / TicksPerSecond; }

        /// <summary>
        /// Converts from a DiscreteTime to a RationalTime representation. This conversion is always lossless.
        /// </summary>
        /// <param name="t">the DiscreteTime parameter.</param>
        /// <returns>the RationalTime representation.</returns>
        public static implicit operator RationalTime(DiscreteTime t)
        {
            return new RationalTime(t.Value, RationalTime.TicksPerSecond.DiscreteTimeRate);
        }

        /// <summary>Returns whether two time values are equal.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator==(DiscreteTime lhs, DiscreteTime rhs) { return lhs.Value == rhs.Value; }

        /// <summary>Returns whether two time values are different.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator!=(DiscreteTime lhs, DiscreteTime rhs) { return lhs.Value != rhs.Value; }

        /// <summary>Returns whether one time value is less than the other.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator<(DiscreteTime lhs, DiscreteTime rhs) { return lhs.Value < rhs.Value; }

        /// <summary>Returns whether one time value is greater than the other.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator>(DiscreteTime lhs, DiscreteTime rhs) { return lhs.Value > rhs.Value; }

        /// <summary>Returns whether one time value is less than or equal to the other.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator<=(DiscreteTime lhs, DiscreteTime rhs) { return lhs.Value <= rhs.Value; }

        /// <summary>Returns whether one time value is greater than or equal to the other.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator>=(DiscreteTime lhs, DiscreteTime rhs) { return lhs.Value >= rhs.Value; }

        /// <summary>Returns the addition of two time values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime operator+(DiscreteTime lhs, DiscreteTime rhs) { return FromTicks(lhs.Value + rhs.Value); }

        /// <summary>Returns the subtraction of two time values.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime operator-(DiscreteTime lhs, DiscreteTime rhs) { return FromTicks(lhs.Value - rhs.Value); }

        /// <summary>Returns a time value scaled by an integral amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime operator*(DiscreteTime lhs, long rhs) { return FromTicks(lhs.Value * rhs); }

        /// <summary>Returns a time value scaled by an floating amount.</summary>
        public static DiscreteTime operator*(DiscreteTime lhs, double s)
        {
            // result is lhs * rhs / TicksPerSecond
            //     but rhs is s * TicksPerSecond, so split between integral and fraction components to help
            //         prevent overflow

            var x = Modf(s, out var i);
            var ticks = lhs.Value * (long)i;

            // fractional component
            if (Math.Abs(x) >= Tick)
            {
                // use as much precision as we can get away with without overflow
                var bits = Lzcnt(Math.Abs(lhs.Value)) - 1;

                long num = 1 << bits;          // by default use as much scale as available
                if (bits >= TicksPerSecondBits) // scale by entire ticks per second
                    num = TicksPerSecond;
                else if (bits >= NonPow2TpsBits) // chop off power of 2 bits as needed from ticks per second constant
                    num = NonPow2Tps << (bits - NonPow2TpsBits);

                var denum = (long)Math.Round(num / x);
                ticks += lhs.Value * num / denum;
            }

            return FromTicks(ticks);
        }

        static double Modf(double x, out double i) { i = Math.Truncate(x); return x - i; }

        [StructLayout(LayoutKind.Explicit)]
        struct LongDoubleUnion
        {
            [FieldOffset(0)]
            public long longValue;
            [FieldOffset(0)]
            public double doubleValue;
        }

        // Taken from: https://github.cds.internal.unity3d.com/unity/dots/blob/81dc924b0953b172765f7488fd04bc537eb79428/Packages/com.unity.mathematics/Unity.Mathematics/math.cs#L5052
        // workaround for a burst inlining issue calling math.lzcnt() - 1.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Lzcnt(long x)
        {
            if (x == 0)
                return 64;

            var xh = (uint)(x >> 32);
            var bits = xh != 0 ? xh : (uint)x;
            var offset = xh != 0 ? 0x41E : 0x43E;

            LongDoubleUnion u;
            u.doubleValue = 0.0;
            u.longValue = 0x4330000000000000L + bits;
            u.doubleValue -= 4503599627370496.0;
            return offset - (int)(u.longValue >> 52);
        }

        /// <summary>Returns a time value scaled by an floating point amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime operator*(DiscreteTime lhs, float s) { return lhs * (double)s; }

        /// <summary>Returns a time value divided by an floating point amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime operator/(DiscreteTime lhs, double s) { return lhs  * (1 / s); }

        /// <summary>Returns a time value divided by an integral amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime operator/(DiscreteTime lhs, long s) { return FromTicks(lhs.Value / s); }

        /// <summary>Returns a time value modulo another value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime operator%(DiscreteTime lhs, DiscreteTime rhs) {  return FromTicks((lhs.Value) % (rhs.Value));}

        /// <summary> Returns the negation of a time value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime operator-(DiscreteTime lhs) { return FromTicks(-lhs.Value); }

        /// <summary>Returns true if the time is equal to a given time, false otherwise.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(DiscreteTime rhs) { return Value == rhs.Value; }

        /// <summary>Returns true if the time is equal to a given time, false otherwise.</summary>
        public override bool Equals(object o) { return Equals((DiscreteTime)o); }

        /// <summary>Returns a hash code for the time.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() { return Value.GetHashCode(); }

        /// <summary>Returns a string representation of the time.</summary>
        public override string ToString() { return ((double)this).ToString(); }

        /// <summary>Returns a string representation of the time using a specified format and culture-specific format information.</summary>
        public string ToString(string format, IFormatProvider formatProvider) { return ((double)this).ToString(format, formatProvider); }

        /// <summary> Implementation of IComparable </summary>
        public int CompareTo(DiscreteTime other) { return Value.CompareTo(other.Value); }
    }

    /// <summary>
    /// Extension methods for the DiscreteTime dataType
    /// </summary>
    public static class DiscreteTimeTimeExtensions
    {
        /// <summary> Returns the absolute value of a time value. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime Abs(this DiscreteTime lhs) { return DiscreteTime.FromTicks(Math.Abs(lhs.Value)); }

        /// <summary> Returns the minimum of two time values. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime Min(this DiscreteTime lhs, DiscreteTime rhs) { return DiscreteTime.FromTicks(Math.Min(lhs.Value, rhs.Value)); }

        /// <summary> Returns the maximum of two time values. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime Max(this DiscreteTime lhs, DiscreteTime rhs) { return DiscreteTime.FromTicks(Math.Max(lhs.Value, rhs.Value)); }

        /// <summary>Returns the result of clamping the value x into the interval [a, b].</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime Clamp(this DiscreteTime x, DiscreteTime a, DiscreteTime b) { return Max(a, Min(b, x)); }

        /// <summary>Returns the result of rounding a DiscreteTime value up to the nearest integral value less or equal to the original value.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime Floor(this DiscreteTime x) { return (DiscreteTime)Math.Floor((double)x); }

        /// <summary>Returns b if c is true, a otherwise.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DiscreteTime Select(this DiscreteTime a, DiscreteTime b, bool c) { return DiscreteTime.FromTicks( c ? b.Value : a.Value); }
    }
}
