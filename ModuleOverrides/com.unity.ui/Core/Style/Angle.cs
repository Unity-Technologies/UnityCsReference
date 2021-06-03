// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Unit of measurement used to express the value of an <see cref="Angle"/>.
    /// </summary>
    public enum AngleUnit
    {
        /// <summary>
        /// Interprets an angle as degrees.
        /// </summary>
        Degree,
        /// <summary>
        /// Interprets the measurement of an angle in gradians. One full circle is 400 gradians
        /// </summary>
        Gradian,
        /// <summary>
        /// Interprets the measurement of an angle in radians. One full circle is 2Pi radians which approximates to 6.2832 radians
        /// </summary>
        Radian,
        /// <summary>
        /// Interprets the measurement of an angle, expressed as a number of turns. One full circle is one turn.
        /// </summary>
        Turn,
    }

    /// <summary>
    /// Represents an angle value.
    /// </summary>
    public struct Angle : IEquatable<Angle>
    {
        // Extension of the AngleUnit to include keywords that can be used with StyleAngle
        private enum Unit
        {
            Degree = AngleUnit.Degree,
            Gradian = AngleUnit.Gradian,
            Radian = AngleUnit.Radian,
            Turn = AngleUnit.Turn,
            None
        }

        /// <summary>
        /// Creates a percentage <see cref="Angle"/> from a float.
        /// </summary>
        /// <returns>The created angle.</returns>
        public static Angle Degrees(float value)
        {
            return new Angle(value, AngleUnit.Degree);
        }

        internal static Angle None()
        {
            return new Angle(0f, Unit.None);
        }

        /// <summary>
        /// The angle value.
        /// </summary>
        /// <remarks>
        /// Positive values represent a clockwise rotation. Negative values represent counterclockwise rotation.
        /// </remarks>
        public float value
        {
            get => m_Value;
            set => m_Value = value;
        }

        /// <summary>
        /// The unit of the value property.
        /// </summary>
        public AngleUnit unit
        {
            get => (AngleUnit)m_Unit;
            set => m_Unit = (Unit)value;
        }

        internal bool IsNone() => m_Unit == Unit.None;

        /// <summary>
        /// Creates an Angle from a float and an optionnal <see cref="AngleUnit"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="AngleUnit.Degree"/> is the default unit.
        /// </remarks>
        public Angle(float value) : this(value, Unit.Degree)
        {}

        /// <summary>
        /// Creates an Angle from a float and an optionnal <see cref="AngleUnit"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="AngleUnit.Degree"/> is the default unit.
        /// </remarks>
        public Angle(float value, AngleUnit unit) : this(value, (Unit)unit)
        {}

        private Angle(float value, Unit unit)
        {
            m_Value = value;
            m_Unit = unit;
        }

        private float m_Value;
        private Unit m_Unit;

        /// <summary>
        /// Returns the value of the angle, expressed in degrees.
        /// </summary>
        public float ToDegrees()
        {
            switch (m_Unit)
            {
                case Unit.Degree:
                    return m_Value;
                case Unit.Gradian:
                    return m_Value * 360 / 400;
                case Unit.Radian:
                    return m_Value * 180 / Mathf.PI;
                case Unit.Turn:
                    return m_Value * 360;
                case Unit.None:
                    return 0;
            }
            return 0;
        }

        /// <undoc/>
        public static implicit operator Angle(float value)
        {
            return new Angle(value, AngleUnit.Degree);
        }

        /// <undoc/>
        public static bool operator==(Angle lhs, Angle rhs)
        {
            return lhs.m_Value == rhs.m_Value && lhs.m_Unit == rhs.m_Unit;
        }

        /// <undoc/>
        public static bool operator!=(Angle lhs, Angle rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(Angle other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is Angle other && Equals(other);
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
                case Unit.Degree:
                    if (!Mathf.Approximately(0, value))
                        unitStr = "deg";
                    break;
                case Unit.Gradian:
                    unitStr = "grad";
                    break;
                case Unit.Radian:
                    unitStr = "rad";
                    break;
                case Unit.Turn:
                    unitStr = "turn";
                    break;
                case Unit.None:
                    valueStr = "";
                    break;
            }
            return $"{valueStr}{unitStr}";
        }
    }
}
