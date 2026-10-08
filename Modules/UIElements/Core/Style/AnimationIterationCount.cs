// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents the number of times an animation repeats, used by the animation-iteration-count style
    /// property. The count is either a finite number of iterations or <see cref="Infinite"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    readonly public partial struct AnimationIterationCount : IEquatable<AnimationIterationCount>
    {
        internal const string k_InfiniteKeyword = "infinite";

        private readonly float m_Value;

        /// <summary>
        /// Creates a finite iteration count. Negative values clamp to 0 (0 is valid: the animation is applied
        /// but does not play).
        /// </summary>
        /// <param name="value">The number of iterations.</param>
        public AnimationIterationCount(float value)
        {
            m_Value = value < 0f ? 0f : value;
        }

        /// <summary>
        /// A count that repeats the animation forever.
        /// </summary>
        public static AnimationIterationCount Infinite()
        {
            return new AnimationIterationCount(float.PositiveInfinity);
        }

        /// <summary>
        /// The number of iterations. Is <see cref="float.PositiveInfinity"/> when the count is infinite.
        /// </summary>
        public float value
        {
            get => m_Value;
        }

        /// <summary>
        /// Whether the animation repeats forever.
        /// </summary>
        public bool IsInfinite()
        {
            return float.IsPositiveInfinity(m_Value);
        }

        /// <undoc/>
        public static implicit operator AnimationIterationCount(float value)
        {
            return new AnimationIterationCount(value);
        }

        /// <undoc/>
        public static implicit operator float(AnimationIterationCount value)
        {
            return value.value;
        }

        /// <undoc/>
        public static bool operator==(AnimationIterationCount lhs, AnimationIterationCount rhs)
        {
            if (lhs.IsInfinite() && rhs.IsInfinite())
                return true;
            return lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(AnimationIterationCount lhs, AnimationIterationCount rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(AnimationIterationCount other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is AnimationIterationCount other && Equals(other);
        }

        public override int GetHashCode()
        {
            return m_Value.GetHashCode();
        }

        public override string ToString()
        {
            return IsInfinite() ? k_InfiniteKeyword : m_Value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }
    }
}
