// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements.StyleSheets;

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
    [Serializable]
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

        [SerializeField]
        private float m_Value;
        [SerializeField]
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

        internal static bool TryParseString(string str, out TimeValue timeValue)
        {
            timeValue = default;

            if (string.IsNullOrEmpty(str))
                return false;

            var s = str.AsSpan().Trim();

            // Find unit index
            int digitEndIndex = 0;
            int unitIndex = -1;
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];
                if (char.IsNumber(c) || c == '.')
                {
                    ++digitEndIndex;
                }
                else if (char.IsLetter(c))
                {
                    unitIndex = i;
                    break;
                }
                else
                {
                    // Invalid format
                    return false;
                }
            }

            var floatStr = s.Slice(0, digitEndIndex);
            var unitStr = new ReadOnlySpan<char>();
            if (unitIndex > 0)
                unitStr = s.Slice(unitIndex, s.Length - unitIndex);
            else
                unitStr = "s";

            // Note: ideally we would not specify NumberStyle settings, but there is no API that allows
            // it while also defining which culture to use. The value used here is the right default for float
            // (looking at source code from Mono & CoreCLR)
            if (StylePropertyUtil.TryParseFloat(floatStr, out var v))
                timeValue.value = v;

            if (unitStr.Equals("ms", StringComparison.OrdinalIgnoreCase))
            {
                timeValue.unit = TimeUnit.Millisecond;
            }
            else if (unitStr.Equals("s", StringComparison.OrdinalIgnoreCase))
            {
                timeValue.unit = TimeUnit.Second;
            }

            return true;
        }
    }
}
