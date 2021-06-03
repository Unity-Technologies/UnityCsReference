// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Reprensents the scale applied as an element's transformations. The center point that will not move when the scaling is applied is the <see cref="TransformOrigin"/>.
    /// </summary>
    public struct Scale : IEquatable<Scale>
    {
        /// <undoc/>
        public Scale(Vector3 scale)
        {
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
