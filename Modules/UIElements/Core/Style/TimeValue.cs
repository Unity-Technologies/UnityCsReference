// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Describes how to interpret a <see cref="TimeValue"/>.
    /// </summary>
    public enum TimeUnit
    {
        /// <summary>
        /// Interprets the time value as seconds.
        /// </summary>
        Second,
        /// <summary>
        /// Interprets the time value as milliseconds.
        /// </summary>
        Millisecond
    }

    /// <summary>
    /// Represents a time value.
    /// </summary>
    public partial struct TimeValue : IEquatable<TimeValue>
    {
        /// <summary>
        /// Creates a second <see cref="TimeValue"/> from a float.
        /// </summary>
        /// <returns>The created time value.</returns>
        public static TimeValue Seconds(float value)
        {
            return new TimeValue(value, TimeUnit.Second);
        }

        /// <summary>
        /// Creates a millisecond <see cref="TimeValue"/> from a float.
        /// </summary>
        /// <returns>The created time value.</returns>
        public static TimeValue Milliseconds(float value)
        {
            return new TimeValue(value, TimeUnit.Millisecond);
        }

        /// <summary>
        /// The time value.
        /// </summary>
        public float value
        {
            get => m_Value;
            set => m_Value = value;
        }

        /// <summary>
        /// The unit of the value property.
        /// </summary>
        public TimeUnit unit
        {
            get => m_Unit;
            set => m_Unit = value;
        }

        /// <summary>
        /// Creates from a float and an optional <see cref="TimeUnit"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="TimeUnit.Second"/> is the default unit.
        /// </remarks>
        public TimeValue(float value) : this(value, TimeUnit.Second)
        {}

        /// <summary>
        /// Creates from a float and an optional <see cref="TimeUnit"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="TimeUnit.Second"/> is the default unit.
        /// </remarks>
        public TimeValue(float value, TimeUnit unit)
        {
            m_Value = value;
            m_Unit = unit;
        }

        private float m_Value;
        private TimeUnit m_Unit;

        /// <undoc/>
        public static implicit operator TimeValue(float value)
        {
            return new TimeValue(value, TimeUnit.Second);
        }

        /// <undoc/>
        public static bool operator==(TimeValue lhs, TimeValue rhs)
        {
            return lhs.m_Value == rhs.m_Value && lhs.m_Unit == rhs.m_Unit;
        }

        /// <undoc/>
        public static bool operator!=(TimeValue lhs, TimeValue rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(TimeValue other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is TimeValue other && Equals(other);
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
            switch (unit)
            {
                case TimeUnit.Second:
                    unitStr = "s";
                    break;
                case TimeUnit.Millisecond:
                    unitStr = "ms";
                    break;
            }
            return $"{valueStr}{unitStr}";
        }
    }
}
