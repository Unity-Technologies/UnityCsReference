// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Provides rotation information for visual elements that rotates around the <see cref="TransformOrigin"/>. Positive values represent clockwise rotation.
    /// </summary>
    public partial struct Rotate : IEquatable<Rotate>
    {
        internal Rotate(Angle angle, Vector3 axis)
        {
            m_Angle = angle;
            m_Axis = axis;
            m_IsNone = false;
        }

        /// <summary>
        /// Create a Rotate struct that correspond to a rotation around the z axis by the provided <see cref="Angle"/>.
        /// </summary>
        public Rotate(Angle angle)
        {
            m_Angle = angle;
            m_Axis = Vector3.forward;
            m_IsNone = false;
        }

        internal static Rotate Initial()
        {
            return new Rotate(0);
        }

        /// <summary>
        /// Return a value of <see cref="Rotate"/> that applies no rotation
        /// </summary>
        public static Rotate None()
        {
            Rotate none = Initial();
            none.m_IsNone = true;
            return none;
        }

        /// <summary>
        /// The angle applied by the rotation. Positive values represent clockwise rotation and negative values represent counterclockwise rotation.
        /// </summary>
        public Angle angle
        {
            get => m_Angle;
            set => m_Angle = value;
        }
        internal Vector3 axis
        {
            get => m_Axis;
            set => m_Axis = value;
        }

        private Angle m_Angle;
        private Vector3 m_Axis;
        private bool m_IsNone;

        internal bool IsNone() => m_IsNone;

        /// <undoc/>
        public static bool operator==(Rotate lhs, Rotate rhs)
        {
            return lhs.m_Angle == rhs.m_Angle && lhs.m_Axis == rhs.m_Axis && lhs.m_IsNone == rhs.m_IsNone;
        }

        /// <undoc/>
        public static bool operator!=(Rotate lhs, Rotate rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(Rotate other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is Rotate other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Angle.GetHashCode() * 793) ^ (m_Axis.GetHashCode() * 791) ^ (m_IsNone.GetHashCode() * 197);
            }
        }

        public override string ToString()
        {
            return $"{m_Angle.ToString()} {m_Axis.ToString()}";
        }

        internal Quaternion ToQuaternion()
        {
            return Quaternion.AngleAxis(m_Angle.ToDegrees(), m_Axis);
        }
    }
}
