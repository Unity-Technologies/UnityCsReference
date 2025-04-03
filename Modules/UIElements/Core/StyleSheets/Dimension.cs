// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements.StyleSheets
{
    // Used by StyleSheet to store all kind of CSS dimension
    // https://developer.mozilla.org/en-US/docs/Web/CSS/dimension
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal struct Dimension : IEquatable<Dimension>
    {
        public enum Unit
        {
            Unitless,
            Pixel,
            Percent,
            Second,
            Millisecond,
            Degree,
            Gradian,
            Radian,
            Turn,
        }

        public Unit unit;
        public float value;

        public Dimension(float value, Unit unit)
        {
            this.unit = unit;
            this.value = value;
        }

        public Length ToLength()
        {
            var lengthUnit = unit == Unit.Percent ? LengthUnit.Percent : LengthUnit.Pixel;
            return new Length(value, lengthUnit);
        }

        public TimeValue ToTime()
        {
            var timeUnit = unit == Unit.Millisecond ? TimeUnit.Millisecond : TimeUnit.Second;
            return new TimeValue(value, timeUnit);
        }

        public Angle ToAngle()
        {
            switch (unit)
            {
                case Unit.Degree:  return new Angle(value, AngleUnit.Degree);
                case Unit.Gradian: return new Angle(value, AngleUnit.Gradian);
                case Unit.Radian: return new Angle(value, AngleUnit.Radian);
                case Unit.Turn: return new Angle(value, AngleUnit.Turn);
            }

            return new Angle(value, AngleUnit.Degree);
        }

        public static bool operator==(Dimension lhs, Dimension rhs)
        {
            return lhs.value == rhs.value && lhs.unit == rhs.unit;
        }

        public static bool operator!=(Dimension lhs, Dimension rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(Dimension other)
        {
            return other == this;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Dimension))
            {
                return false;
            }

            var v = (Dimension)obj;
            return v == this;
        }

        public override int GetHashCode()
        {
            var hashCode = -799583767;
            hashCode = hashCode * -1521134295 + unit.GetHashCode();
            hashCode = hashCode * -1521134295 + value.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            string unitStr = string.Empty;
            switch (unit)
            {
                case Unit.Pixel:
                    unitStr = "px";
                    break;
                case Unit.Percent:
                    unitStr = "%";
                    break;
                case Unit.Second:
                    unitStr = "s";
                    break;
                case Unit.Millisecond:
                    unitStr = "ms";
                    break;
                case Unit.Degree:
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
                case Unit.Unitless:
                    unitStr = string.Empty;
                    break;
            }
            return $"{value.ToString(CultureInfo.InvariantCulture.NumberFormat)}{unitStr}";
        }

        public bool IsLength()
        {
            return unit is Unit.Pixel or Unit.Percent;
        }

        public bool IsTimeValue()
        {
            return unit is Unit.Millisecond or Unit.Second;
        }

        public bool IsAngle()
        {
            return unit is Unit.Degree or Unit.Gradian or Unit.Radian or Unit.Turn;
        }

        public bool IsUnitless()
        {
            return unit is Unit.Unitless;
        }
    }
}
