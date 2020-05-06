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
        /// Interprets length as a percentage.
        /// </summary>
        Percent,
        // Em
    }

    /// <summary>
    /// Reprensents a distance value.
    /// </summary>
    public struct Length : IEquatable<Length>
    {
        /// <summary>
        /// Creates a percentage <see cref="Length"/> from a float.
        /// </summary>
        /// <returns>The created length.</returns>
        public static Length Percent(float value)
        {
            return new Length(value, LengthUnit.Percent);
        }

        /// <summary>
        /// The length value.
        /// </summary>
        public float value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        /// <summary>
        /// The unit of the value property.
        /// </summary>
        public LengthUnit unit
        {
            get { return m_Unit; }
            set { m_Unit = value; }
        }

        /// <summary>
        /// Creates from a float and an optionnal <see cref="LengthUnit"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="LengthUnit.Pixel"/> is the default unit.
        /// </remarks>
        public Length(float value) : this(value, LengthUnit.Pixel)
        {}

        /// <summary>
        /// Creates from a float and an optionnal <see cref="LengthUnit"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="LengthUnit.Pixel"/> is the default unit.
        /// </remarks>
        public Length(float value, LengthUnit unit)
        {
            m_Value = value;
            m_Unit = unit;
        }

        private float m_Value;
        private LengthUnit m_Unit;
        public static implicit operator Length(float value)
        {
            return new Length(value, LengthUnit.Pixel);
        }

        public static bool operator==(Length lhs, Length rhs)
        {
            return lhs.m_Value == rhs.m_Value && lhs.m_Unit == rhs.m_Unit;
        }

        public static bool operator!=(Length lhs, Length rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(Length other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Length))
            {
                return false;
            }

            var v = (Length)obj;
            return v == this;
        }

        public override int GetHashCode()
        {
            var hashCode = 851985039;
            hashCode = hashCode * -1521134295 + m_Value.GetHashCode();
            hashCode = hashCode * -1521134295 + m_Unit.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            string unitStr = string.Empty;
            switch (unit)
            {
                case LengthUnit.Pixel:
                    if (!Mathf.Approximately(0, value))
                        unitStr = "px";
                    break;
                case LengthUnit.Percent:
                    unitStr = "%";
                    break;
                default:
                    break;
            }
            return $"{value.ToString(CultureInfo.InvariantCulture.NumberFormat)}{unitStr}";
        }
    }
}
