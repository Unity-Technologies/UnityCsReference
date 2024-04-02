// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Describes how to interpret a <see cref="Length"/> value.
    /// </summary>
    public enum LengthUnit
    {
        /// <summary>
        /// Interprets length as pixel.
        /// </summary>
        Pixel,
        /// <summary>
        /// Interprets length as a percentage, with 100 representing 100%.
        /// The value is not constrained and can range from negative numbers to values greater than 100.
        /// </summary>
        Percent,
    }

    /// <summary>
    /// Represents a distance value.
    /// </summary>
    [Serializable]
    public struct Length : IEquatable<Length>
    {
        // Float clamping value (2 ^ 23).
        internal const float k_MaxValue = 8388608.0f;

        // Extension of the LengthUnit to include keywords that can be used with StyleLength
        private enum Unit
        {
            Pixel = LengthUnit.Pixel,
            Percent = LengthUnit.Percent,
            Auto,
            None
        }

        /// <summary>
        /// Creates a percentage <see cref="Length"/> from a float.
        /// </summary>
        /// <returns>The created length.</returns>
        public static Length Percent(float value)
        {
            return new Length(value, LengthUnit.Percent);
        }

        /// <summary>
        /// Creates an Auto Length <see cref="Length"/>.
        /// </summary>
        /// <returns>Auto length.</returns>
        public static Length Auto()
        {
            return new Length(0f, Unit.Auto);
        }

        /// <summary>
        /// Creates a None Length <see cref="Length"/>.
        /// </summary>
        /// <returns>None length.</returns>
        public static Length None()
        {
            return new Length(0f, Unit.None);
        }

        /// <summary>
        /// The length value.
        /// </summary>
        public float value
        {
            get => m_Value;

            // Clamp values to prevent floating point calculation inaccuracies in Yoga.
            set => m_Value = Mathf.Clamp(value, -k_MaxValue, k_MaxValue);
        }

        /// <summary>
        /// The unit of the value property.
        /// </summary>
        public LengthUnit unit
        {
            get => (LengthUnit)m_Unit;
            set => m_Unit = (Unit)value;
        }

        /// <summary>
        /// Check if Length is Auto.
        /// </summary>
        /// <returns>true if Length is Auto, false otherwise</returns>
        public bool IsAuto() => m_Unit == Unit.Auto;

        /// <summary>
        /// Check if Length is None.
        /// </summary>
        /// <returns>true if Length is None, false otherwise</returns>
        public bool IsNone() => m_Unit == Unit.None;

        /// <summary>
        /// Creates from a float and an optional <see cref="LengthUnit"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="LengthUnit.Pixel"/> is the default unit.
        /// </remarks>
        public Length(float value) : this(value, Unit.Pixel)
        {}

        /// <summary>
        /// Creates from a float and an optional <see cref="LengthUnit"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="LengthUnit.Pixel"/> is the default unit.
        /// </remarks>
        public Length(float value, LengthUnit unit) : this(value, (Unit)unit)
        {}

        private Length(float value, Unit unit) : this()
        {
            this.value = value;
            m_Unit = unit;
        }

        [SerializeField]
        private float m_Value;
        [SerializeField]
        private Unit m_Unit;

        /// <undoc/>
        public static implicit operator Length(float value)
        {
            return new Length(value, LengthUnit.Pixel);
        }

        /// <undoc/>
        public static bool operator==(Length lhs, Length rhs)
        {
            return lhs.m_Value == rhs.m_Value && lhs.m_Unit == rhs.m_Unit;
        }

        /// <undoc/>
        public static bool operator!=(Length lhs, Length rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(Length other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is Length other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Value.GetHashCode() * 397) ^ (int)m_Unit;
            }
        }

        public override string ToString()
        {
            var valueStr = value.ToString(CultureInfo.InvariantCulture.NumberFormat);
            var unitStr = string.Empty;
            switch (m_Unit)
            {
                case Unit.Pixel:
                    if (!Mathf.Approximately(0, value))
                        unitStr = "px";
                    break;
                case Unit.Percent:
                    unitStr = "%";
                    break;
                case Unit.Auto:
                    valueStr = "auto";
                    break;
                case Unit.None:
                    valueStr = "none";
                    break;
            }
            return $"{valueStr}{unitStr}";
        }
    }
}
