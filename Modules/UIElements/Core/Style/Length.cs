// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.UIElements.Layout;
using UnityEngine.UIElements.StyleSheets;

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
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public partial struct Length : IEquatable<Length>
    {
        // Float clamping value (2 ^ 23).
        internal const float k_MaxValue = 8388608.0f;

        /// <summary>
        /// Creates a pixel <see cref="Length"/> from a float.
        /// </summary>
        /// <returns>The created length.</returns>
        public static Length Pixels(float value)
        {
            return new Length(value, LengthUnit.Pixel);
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
            return new Length(0.0f, LayoutUnit.Auto);
        }

        /// <summary>
        /// Creates a None Length <see cref="Length"/>.
        /// </summary>
        /// <returns>None length.</returns>
        public static Length None()
        {
            return new Length(0.0f, LayoutUnit.Undefined);
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
            set => m_Unit = (LayoutUnit)value;
        }

        /// <summary>
        /// The value property interpreted as a pixel value.
        /// </summary>
        /// <remarks>Used for border-width and for text-shadow uss properties.</remarks>
        // This is the way the computed style encodes the 4 borderWidth values at this moment.
        // Returns 0 for Auto or None, and assumes pixels even if the value is percents.
        // This might seem a bit weird but changing that could cause regressions.
        internal float pixelValue => m_Unit >= LayoutUnit.Auto ? 0f : m_Value;

        /// <summary>
        /// The internal layout unit of the value property.
        /// </summary>
        internal LayoutUnit layoutUnit => m_Unit;

        /// <summary>
        /// Check if Length is Auto.
        /// </summary>
        /// <returns>true if Length is Auto, false otherwise</returns>
        public bool IsAuto() => m_Unit == LayoutUnit.Auto;

        /// <summary>
        /// Check if Length is None.
        /// </summary>
        /// <returns>true if Length is None, false otherwise</returns>
        public bool IsNone() => m_Unit == LayoutUnit.Undefined;

        /// <summary>
        /// Creates from a float and an optional <see cref="LengthUnit"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="LengthUnit.Pixel"/> is the default unit.
        /// </remarks>
        public Length(float value) : this(value, LayoutUnit.Point)
        {
        }

        /// <summary>
        /// Creates from a float and an optional <see cref="LengthUnit"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="LengthUnit.Pixel"/> is the default unit.
        /// </remarks>
        public Length(float value, LengthUnit unit) : this(value, (LayoutUnit)unit)
        {
        }

        private Length(float value, LayoutUnit unit) : this()
        {
            this.value = value;
            m_Unit = unit;
        }

        [SerializeField]
        private float m_Value;
        [SerializeField]
        private LayoutUnit m_Unit;

        /// <undoc/>
        public static implicit operator Length(float value)
        {
            // Silently converting NaN to None here because there's no other way to get None from a float.
            return float.IsNaN(value) ? None() : new Length(value, LengthUnit.Pixel);
        }

        /// <undoc/>
        internal static bool Approximately(Length lhs, Length rhs)
        {
            return lhs.m_Unit == rhs.m_Unit && (lhs.m_Unit >= LayoutUnit.Auto || Mathf.Approximately(lhs.m_Value, rhs.m_Value));
        }

        /// <undoc/>
        public static bool operator ==(Length lhs, Length rhs)
        {
            return lhs.m_Unit == rhs.m_Unit && (lhs.m_Unit >= LayoutUnit.Auto || lhs.m_Value == rhs.m_Value);
        }

        /// <undoc/>
        public static bool operator !=(Length lhs, Length rhs)
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
                case LayoutUnit.Point:
                    if (!Mathf.Approximately(0, value))
                        unitStr = "px";
                    break;
                case LayoutUnit.Percent:
                    unitStr = "%";
                    break;
                case LayoutUnit.Auto:
                    valueStr = "auto";
                    break;
                case LayoutUnit.Undefined:
                    valueStr = "none";
                    break;
            }

            return $"{valueStr}{unitStr}";
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static Length ParseString(string str, Length defaultValue = default)
        {
            if (string.IsNullOrEmpty(str))
                return defaultValue;

            str = str.ToLowerInvariant().Trim();

            var result = defaultValue;
            if (char.IsLetter(str[0]))
            {
                if (str == "auto")
                    result = Auto();
                else if (str == "none")
                    result = None();
            }
            else
            {
                // Find unit index
                int digitEndIndex = 0;
                int unitIndex = -1;
                for (int i = 0; i < str.Length; i++)
                {
                    var c = str[i];
                    if (char.IsNumber(c) || c == '.' || c == '-')
                    {
                        ++digitEndIndex;
                    }
                    else if (char.IsLetter(c) || c == '%')
                    {
                        unitIndex = i;
                        break;
                    }
                    else
                    {
                        // Invalid format
                        return defaultValue;
                    }
                }

                var floatStr = str.Substring(0, digitEndIndex);
                var unitStr = string.Empty;
                if (unitIndex > 0)
                    unitStr = str.Substring(unitIndex, str.Length - unitIndex);
                else
                    unitStr = "px";

                float value = defaultValue.value;
                LengthUnit unit = defaultValue.unit;

                // Note: ideally we would not specify NumberStyle settings, but there is no API that allows
                // it while also defining which culture to use. The value used here is the right default for float
                // (looking at source code from Mono & CoreCLR)
                if (StylePropertyUtil.TryParseFloat(floatStr, out var v))
                    value = v;

                switch (unitStr)
                {
                    case "px":
                        unit = LengthUnit.Pixel;
                        break;
                    case "%":
                        unit = LengthUnit.Percent;
                        break;
                }

                result = new Length(value, unit);
            }

            return result;
        }
    }
}
