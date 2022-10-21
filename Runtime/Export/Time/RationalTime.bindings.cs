// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using UnityEngine.Bindings;

namespace Unity.IntegerTime
{
    /// <summary>
    /// Data type that represents time as an integer count of a rational number.
    /// </summary>
    [NativeHeader("Runtime/Input/RationalTime.h")]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public readonly struct RationalTime
    {
        [UsedImplicitly] readonly long m_Count;
        [UsedImplicitly] readonly TicksPerSecond m_TicksPerSecond;

        /// <summary>
        /// Constructor that build a Rational time from a count and a rational number.
        /// </summary>
        /// <param name="count">The number of ticks.</param>
        /// <param name="ticks">The rational number of ticks per second.</param>
        public RationalTime(long count, TicksPerSecond ticks)
        {
            m_Count = count;
            m_TicksPerSecond = ticks;
        }

        /// <summary>
        /// Returns the number of ticks.
        /// </summary>
        public long Count => m_Count;

        /// <summary>
        /// Returns the number of ticks per second.
        /// </summary>
        public TicksPerSecond Ticks => m_TicksPerSecond;

        /// <summary>
        /// The number of ticks per second ( rate ) of the discrete time, expressed as a rational number.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        [Serializable]
        public readonly struct TicksPerSecond : IEquatable<TicksPerSecond>
        {
            const uint k_DefaultTicksPerSecond = 141120000;
            readonly uint m_Numerator;
            readonly uint m_Denominator;

            /// <summary>
            /// The default ticks per second is 141120000 which was chosen since it can represent most frame rates in a lossless way.
            /// </summary>
            public static readonly TicksPerSecond DefaultTicksPerSecond = new(k_DefaultTicksPerSecond);
            /// <summary>
            /// 24 Frames per second.
            /// </summary>
            public static readonly TicksPerSecond TicksPerSecond24 = new( 24 );
            /// <summary>
            /// 25 Frames per second.
            /// </summary>
            public static readonly TicksPerSecond TicksPerSecond25 = new( 25 );
            /// <summary>
            /// 30 Frames per second.
            /// </summary>
            public static readonly TicksPerSecond TicksPerSecond30 = new( 30 );
            /// <summary>
            /// 50 Frames per second.
            /// </summary>
            public static readonly TicksPerSecond TicksPerSecond50 = new( 50 );
            /// <summary>
            /// 60 Frames per second.
            /// </summary>
            public static readonly TicksPerSecond TicksPerSecond60 = new( 60 );
            /// <summary>
            /// 120 Frames per second.
            /// </summary>
            public static readonly TicksPerSecond TicksPerSecond120 = new( 120 );
            /// <summary>
            /// 24 Frames per second drop-frame.
            /// </summary>
            public static readonly TicksPerSecond TicksPerSecond2397 = new( 24 * 1000, 1001 );
            /// <summary>
            /// 25 Frames per second drop-frame.
            /// </summary>
            public static readonly TicksPerSecond TicksPerSecond2425 = new( 25 * 1000, 1001 );
            /// <summary>
            /// 30 Frames per second drop-frame.
            /// </summary>
            public static readonly TicksPerSecond TicksPerSecond2997 = new( 30 * 1000, 1001 );
            /// <summary>
            /// 60 Frames per second drop-frame.
            /// </summary>
            public static readonly TicksPerSecond TicksPerSecond5994 = new( 60 * 1000, 1001 );
            /// <summary>
            /// 120 Frames per second drop-frame.
            /// </summary>
            public static readonly TicksPerSecond TicksPerSecond11988 = new( 120 * 1000, 1001 );

            internal static readonly TicksPerSecond DiscreteTimeRate = new(DiscreteTime.TicksPerSecond);

            /// <summary>
            /// Constructor that builds a rational TicksPerSecond. The fraction is immediately reduced.
            /// </summary>
            /// <param name="num">The numerator.</param>
            /// <param name="den">The denominator.</param>
            public TicksPerSecond(uint num, uint den = 1)
            {
                m_Numerator = num;
                m_Denominator = den;
                Simplify(ref m_Numerator, ref m_Denominator);
            }

            /// <summary>
            /// Returns the numerator.
            /// </summary>
            public uint Numerator => m_Numerator;

            /// <summary>
            /// Returns the denominator.
            /// </summary>
            public uint Denominator => m_Denominator;

            /// <summary>
            /// Returns if the rate is valid. An invalid rate has a 0 denominator.
            /// </summary>
            public bool Valid => IsValid(this);

            /// <summary>
            /// Equality comparison.
            /// </summary>
            /// <param name="rhs">The right hand side.</param>
            /// <returns>true if the 2 values are equal and false otherwise.</returns>
            public bool Equals(TicksPerSecond rhs)
            {
                return m_Numerator == rhs.m_Numerator && m_Denominator == rhs.m_Denominator;
            }

            /// <summary>
            /// Equality comparison.
            /// </summary>
            /// <param name="rhs">The right hand side.</param>
            /// <returns>true if the 2 values are equal and false otherwise.</returns>
            public override bool Equals(object rhs)
            {
                return rhs is TicksPerSecond other && Equals(other);
            }

            /// <summary>
            /// Used by the equality comparison.
            /// </summary>
            /// <returns>The hash code.</returns>
            public override int GetHashCode()
            {
                return HashCode.Combine(m_Numerator, m_Denominator);
            }


            // This is the same as the C++ implementation.
            // Currently, Burst cannot call native methods from within static constructors.
            // These static constructors are triggered from all the static constant definitions
            static void Simplify(ref uint num, ref uint den)
            {
                if (den > 1 && num > 0) // 0 or 1
                {
                    var gcd = Gcd(num, den);
                    num /= gcd;
                    den /= gcd;
                }
            }

            // Greatest common divisor implementation
            static uint Gcd(uint a, uint b)
            {
                for (;;)
                {
                    if (a == 0)
                        return b;
                    b %= a;
                    if (b == 0)
                        return a;
                    a %= b;
                }
            }

            [FreeFunction("IntegerTime::TicksPerSecond::IsValid", IsFreeFunction = true,
                ThrowsException = false)]
            static extern bool IsValid(TicksPerSecond tps);
        }

        /// <summary>
        /// Converts a floating point number into a RationalTime with an explicit rate.
        /// </summary>
        /// <param name="t">the float time</param>
        /// <param name="ticksPerSecond">the desired rate</param>
        /// <returns>A new instance of RationalTime that best the floating point using floor.</returns>
        [FreeFunction("IntegerTime::RationalTime::FromDouble", IsFreeFunction = true, ThrowsException = true)]
        public static extern RationalTime FromDouble(double t, TicksPerSecond ticksPerSecond );

        /// <summary>
        /// Converts from a RationalTime to DiscreteTime representation. If the
        /// rate denominator is 1 and the DiscreteTime.TicksPerSecond is a
        /// multiple of the numerator, this conversion is lossless.
        /// </summary>
        /// <param name="r">the RationalTime parameter.</param>
        /// <returns>the DiscreteTime representation.</returns>
        public static explicit operator DiscreteTime(RationalTime t)
        {
            var timeInDiscreteTicks = t.Convert(RationalTime.TicksPerSecond.DiscreteTimeRate);
            return DiscreteTime.FromTicks(timeInDiscreteTicks.Count);
        }
    }

    /// <summary>
    /// Extension method operations for RationalTime.
    /// </summary>
    public static class RationalTimeExtensions
    {
        /// <summary>
        /// Conversion to double.
        /// </summary>
        /// <param name="value">the RationalTime value.</param>
        /// <returns>The double representation for rational time (potentially lossy).</returns>
        [FreeFunction("IntegerTime::RationalTime::ToDouble", IsFreeFunction = true, ThrowsException = true)]
        public static extern double ToDouble(this RationalTime value);

        /// <summary>
        /// Validity check.
        /// </summary>
        /// <param name="value">the value to check</param>
        /// <returns>true if the TicksPerSecond is valid and false otherwise.</returns>
        [FreeFunction("IntegerTime::RationalTime::IsValid", IsFreeFunction = true, ThrowsException = false)]
        public static extern bool IsValid(this RationalTime value);

        /// <summary>
        /// Converts a RationalTime from one rate to another. This is lossless unless it would trigger an overflow.
        /// </summary>
        /// <param name="time">the value to convert.</param>
        /// <param name="rate">the new rate.</param>
        /// <returns>A new instance of RationalTime representing as closely as possible the same number of seconds.</returns>
        [FreeFunction("IntegerTime::RationalTime::ConvertRate", IsFreeFunction = true, ThrowsException = true)]
        public static extern RationalTime Convert(this RationalTime time, RationalTime.TicksPerSecond rate);

        /// <summary>
        /// Adds two Rational Times together.
        /// </summary>
        /// <param name="lhs">the left hand side.</param>
        /// <param name="rhs">the right hand size.</param>
        /// <returns>a new instance that is the sum of the two operands. </returns>
        /// <exception cref="System.ArgumentException">If the TicksPerSecond do not match.</exception>
        [FreeFunction("IntegerTime::RationalTime::Add", IsFreeFunction = true, ThrowsException = true)]
        public static extern RationalTime Add(this RationalTime lhs, RationalTime rhs);

        /// <summary>
        /// Subtracts two Rational Times together.
        /// </summary>
        /// <param name="lhs">the left hand side.</param>
        /// <param name="rhs">the right hand size.</param>
        /// <returns>a new instance that is the difference of the two operands. </returns>
        /// <exception cref="System.ArgumentException">If the TicksPerSecond do not match.</exception>
        [FreeFunction("IntegerTime::RationalTime::Subtract", IsFreeFunction = true, ThrowsException = true)]
        public static extern RationalTime Subtract(this RationalTime lhs, RationalTime rhs);

        /// <summary>
        /// Multiplies two Rational Times together.
        /// </summary>
        /// <param name="lhs">the left hand side.</param>
        /// <param name="rhs">the right hand size.</param>
        /// <returns>a new instance that is the multiplication of the two operands. </returns>
        /// <exception cref="System.ArgumentException">If the TicksPerSecond do not match.</exception>
        [FreeFunction("IntegerTime::RationalTime::Multiply", IsFreeFunction = true, ThrowsException = true)]
        public static extern RationalTime Multiply(this RationalTime lhs, RationalTime rhs);

        /// <summary>
        /// Divides two Rational Times together.
        /// </summary>
        /// <param name="lhs">the left hand side.</param>
        /// <param name="rhs">the right hand size.</param>
        /// <returns>a new instance that is the division of the two operands. </returns>
        /// <exception cref="System.ArgumentException">If the TicksPerSecond do not match.</exception>
        [FreeFunction("IntegerTime::RationalTime::Divide", IsFreeFunction = true, ThrowsException = true)]
        public static extern RationalTime Divide(this RationalTime lhs, RationalTime rhs);
    }
}
