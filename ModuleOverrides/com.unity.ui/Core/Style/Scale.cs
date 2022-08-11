// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents the scale applied as element transformations. The center point that doesn't move when the scaling is applied is the <see cref="TransformOrigin"/>.
    /// </summary>
    public partial struct Scale : IEquatable<Scale>
    {
        /// <summary>
        /// Creates a new Scale from a <see cref="Vector2"/>.
        /// </summary>
        public Scale(Vector2 scale)
        {
            m_Scale = new Vector3(scale.x, scale.y, 1.0f);
            m_IsNone = false;
        }

        /// <summary>
        /// Creates a new Scale from a <see cref="Vector3"/>.
        /// </summary>
        /// <remarks>
        /// Scaling in the Z axis is currently unsupported. Consequently, if a Z value different
        /// than 1.0f is provided, a warning is issued and the Z value is forced to 1.0f.
        /// </remarks>
        public Scale(Vector3 scale)
        {
            if (!Mathf.Approximately(1.0f, scale.z))
            {
                Debug.LogWarning("Assigning Z scale different than 1.0f, this is not yet supported. Forcing the value to 1.0f.");
                scale.z = 1.0f;
            }
            m_Scale = scale;
            m_IsNone = false;
        }

        internal static Scale Initial()
        {
            return new Scale(Vector3.one);
        }

        /// <summary>
        /// Returns a value of Scale without any scaling applied.
        /// </summary>
        public static Scale None()
        {
            Scale none = Initial();
            none.m_IsNone = true;
            return none;
        }

        /// <undoc/>
        public Vector3 value
        {
            get => m_Scale;
            set => m_Scale = value;
        }

        Vector3 m_Scale;
        private bool m_IsNone;

        internal bool IsNone() => m_IsNone;

        /// <undoc/>
        public static implicit operator Scale(Vector2 scale)
        {
            return new Scale(scale);
        }

        /// <undoc/>
        public static bool operator==(Scale lhs, Scale rhs)
        {
            return lhs.m_Scale == rhs.m_Scale;
        }

        /// <undoc/>
        public static bool operator!=(Scale lhs, Scale rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(Scale other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is Scale other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_Scale.GetHashCode() * 793);
            }
        }

        public override string ToString()
        {
            return m_Scale.ToString();
        }
    }
}
