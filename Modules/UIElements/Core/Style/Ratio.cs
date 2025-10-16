// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a ratio, used for Aspect Ratio style property.
    /// </summary>
    [Serializable]
    readonly public partial struct Ratio : IEquatable<Ratio>
    {
        /// <summary>
        /// Create a ratio from a float value.
        /// </summary>
        /// <param name="value"> represent the actual ratio. A values of 0 and is not valid for use with the aspect-ratio property.</param>
        public Ratio(float value)
        {
            m_Value = value;
        }

        /// <summary>
        /// Create a special ratio value that indicate no specific ratio should be used.
        /// </summary>
        public static Ratio Auto()
        {
            Ratio aspectRatio = new Ratio(float.NaN);
            return aspectRatio;
        }

        private readonly float m_Value;

        /// <summary>
        /// The float value of the ratio. Will be float.NaN if the ratio is set to auto.
        /// </summary>
        public float value
        {
            get => m_Value;
        }

        /// <summary>
        /// Property indicating that no specific ratio should be used.
        /// </summary>
        public bool IsAuto()
        {
            return float.IsNaN(value);
        }

        /// <undoc/>
        public static implicit operator Ratio(float value)
        {
            return new Ratio(value);
        }

        /// <undoc/>
        public static implicit operator float(Ratio value)
        {
            return value.value;
        }

        /// <undoc/>
        public static bool operator==(Ratio lhs, Ratio rhs)
        {
            if (lhs.IsAuto() && rhs.IsAuto())
                return true;
            return lhs.m_Value == rhs.m_Value;
        }

        /// <undoc/>
        public static bool operator!=(Ratio lhs, Ratio rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(Ratio other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is Ratio other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Value.GetHashCode() * 793);
            }
        }

        public override string ToString()
        {
            return IsAuto() ? StyleValueKeywordExtension.ToUssString(StyleValueKeyword.Auto)
                : m_Value.ToString(CultureInfo.InvariantCulture.NumberFormat);
        }
    }
}
