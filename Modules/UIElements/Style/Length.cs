// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace UnityEngine.UIElements
{
    public enum LengthUnit
    {
        Pixel,
        Percent,
        // Em
    }

    public struct Length : IEquatable<Length>
    {
        public static Length Percent(float value)
        {
            return new Length(value, LengthUnit.Percent);
        }

        public float value
        {
            get { return m_Value; }
            set { m_Value = value; }
        }

        public LengthUnit unit
        {
            get { return m_Unit; }
            set { m_Unit = value; }
        }

        public Length(float value) : this(value, LengthUnit.Pixel)
        {}

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
            if (!Mathf.Approximately(0, value))
            {
                switch (unit)
                {
                    case LengthUnit.Pixel:
                        unitStr = "px";
                        break;
                    case LengthUnit.Percent:
                        unitStr = "%";
                        break;
                    default:
                        break;
                }
            }
            return $"{value.ToString(CultureInfo.InvariantCulture.NumberFormat)}{unitStr}";
        }
    }
}
