using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements.StyleSheets
{
    // Used by StyleSheet to store all kind of CSS dimension
    // https://developer.mozilla.org/en-US/docs/Web/CSS/dimension
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal struct Dimension : IEquatable<Dimension>
    {
        public enum Unit
        {
            Unitless,
            Pixel,
            Percent
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
                default:
                    break;
            }
            return $"{value.ToString(CultureInfo.InvariantCulture.NumberFormat)}{unitStr}";
        }
    }
}
