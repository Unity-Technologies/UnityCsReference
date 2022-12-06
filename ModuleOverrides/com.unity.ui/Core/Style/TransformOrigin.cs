// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents the point of origin where the (<see cref="Scale"/>, <see cref="Translate"/>, <see cref="Rotate"/>) transformations are applied.
    /// </summary>
    public struct TransformOrigin : IEquatable<TransformOrigin>
    {
        /// <undoc/>
        public TransformOrigin(Length x, Length y , float z)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
        }

        /// <summary>
        /// Create a TransformOrigin data with two Lengths for the x and y axis.
        /// </summary>
        public TransformOrigin(Length x, Length y) : this(x, y, 0)
        { }

        /// <summary>
        /// Returns the initial value for the TransformOrigin property.
        /// </summary>
        public static TransformOrigin Initial()
        {
            return new TransformOrigin(Length.Percent(50), Length.Percent(50), 0);
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

        /// <undoc/>
        public static bool operator==(TransformOrigin lhs, TransformOrigin rhs)
        {
            return lhs.m_X == rhs.m_X && lhs.m_Y == rhs.m_Y && lhs.m_Z == rhs.m_Z;
        }

        /// <undoc/>
        public static bool operator!=(TransformOrigin lhs, TransformOrigin rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(TransformOrigin other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is TransformOrigin other && Equals(other);
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
