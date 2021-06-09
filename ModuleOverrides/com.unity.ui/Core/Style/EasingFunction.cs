// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a mathematical function that describes the rate at which a numerical value changes.
    /// </summary>
    public enum EasingMode
    {
        /// <undoc/>
        Ease,
        /// <undoc/>
        EaseIn,
        /// <undoc/>
        EaseOut,
        /// <undoc/>
        EaseInOut,
        /// <undoc/>
        Linear,
        /// <undoc/>
        EaseInSine,
        /// <undoc/>
        EaseOutSine,
        /// <undoc/>
        EaseInOutSine,
        /// <undoc/>
        EaseInCubic,
        /// <undoc/>
        EaseOutCubic,
        /// <undoc/>
        EaseInOutCubic,
        /// <undoc/>
        EaseInCirc,
        /// <undoc/>
        EaseOutCirc,
        /// <undoc/>
        EaseInOutCirc,
        /// <undoc/>
        EaseInElastic,
        /// <undoc/>
        EaseOutElastic,
        /// <undoc/>
        EaseInOutElastic,
        /// <undoc/>
        EaseInBack,
        /// <undoc/>
        EaseOutBack,
        /// <undoc/>
        EaseInOutBack,
        /// <undoc/>
        EaseInBounce,
        /// <undoc/>
        EaseOutBounce,
        /// <undoc/>
        EaseInOutBounce
    }

    /// <summary>
    /// Determines how intermediate values are calculated for a transition.
    /// </summary>
    public struct EasingFunction : IEquatable<EasingFunction>
    {
        /// <summary>
        /// The value of the <see cref="EasingMode"/>.
        /// </summary>
        public EasingMode mode
        {
            get => m_Mode;
            set => m_Mode = value;
        }

        /// <summary>
        /// Creates from a <see cref="EasingMode"/>.
        /// </summary>
        public EasingFunction(EasingMode mode)
        {
            m_Mode = mode;
        }

        private EasingMode m_Mode;

        /// <undoc/>
        public static implicit operator EasingFunction(EasingMode easingMode)
        {
            return new EasingFunction(easingMode);
        }

        /// <undoc/>
        public static bool operator==(EasingFunction lhs, EasingFunction rhs)
        {
            return lhs.m_Mode == rhs.m_Mode;
        }

        /// <undoc/>
        public static bool operator!=(EasingFunction lhs, EasingFunction rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(EasingFunction other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is EasingFunction other && Equals(other);
        }

        public override string ToString()
        {
            return m_Mode.ToString();
        }

        public override int GetHashCode()
        {
            return (int)m_Mode;
        }
    }
}
