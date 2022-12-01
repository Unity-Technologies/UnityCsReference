// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a translation of the object. Percentage values in X and Y are relative to the width and height of the visual element where the style value is applied.
    /// </summary>
    public struct Translate : IEquatable<Translate>
    {
        /// <undoc/>
        public Translate(Length x, Length y , float z)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
            m_isNone = false;
        }

        /// <summary>
        /// Create a Translate data with two Lengths for the x and y axis.
        /// </summary>
        public Translate(Length x, Length y) : this(x, y, 0)
        { }

        /// <summary>
        /// Returns the value of a Translate object with no translation applied.
        /// </summary>
        public static Translate None()
        {
            Translate translate = default;
            translate.m_isNone = true;
            return translate;
        }

        /// <undoc/>
        public Length x
        {
            get => m_X;
            set => m_X = value;
        }

        /// <undoc/>
        public Length y
        {
            get => m_Y;
            set => m_Y = value;
        }

        /// <undoc/>
        public float z
        {
            get => m_Z;
            set => m_Z = value;
        }
        private Length m_X;
        private Length m_Y;
        private float m_Z;
        private bool m_isNone;

        internal bool IsNone() => m_isNone;

        /// <undoc/>
        public static bool operator==(Translate lhs, Translate rhs)
        {
            return lhs.m_X == rhs.m_X && lhs.m_Y == rhs.m_Y && lhs.m_Z == rhs.m_Z && lhs.m_isNone == rhs.m_isNone;
        }

        /// <undoc/>
        public static bool operator!=(Translate lhs, Translate rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(Translate other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is Translate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_X.GetHashCode() * 793) ^ (m_Y.GetHashCode() * 791) ^ (m_Z.GetHashCode() * 571);
            }
        }

        public override string ToString()
        {
            var zStr = m_Z.ToString(CultureInfo.InvariantCulture.NumberFormat);

            return $"{m_X.ToString()} {m_Y.ToString()} {zStr}";
        }
    }
}
