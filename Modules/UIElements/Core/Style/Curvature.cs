// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Bi-axial curvature applied to a visual element's generated mesh. The element bends around its
    /// X axis by <see cref="x"/> (curving the vertical extent) and around its Y axis by <see cref="y"/>
    /// (curving the horizontal extent); the sign of each angle selects concave vs. convex.
    /// </summary>
    /// <remarks>Curvature describes surface shape only — placement and orientation still come from the
    /// element's transform. It is only visually meaningful on world-space panels.</remarks>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public partial struct Curvature : IEquatable<Curvature>
    {
        /// <summary>
        /// Create a Curvature that bends around the X axis by the given angle (curving the vertical extent),
        /// flat around the Y axis.
        /// </summary>
        public Curvature(Angle x)
        {
            m_X = x;
            m_Y = new Angle(0);
            m_IsNone = false;
        }

        /// <summary>
        /// Create a Curvature that bends by the given angles around the X (x) and Y (y) axes.
        /// </summary>
        public Curvature(Angle x, Angle y)
        {
            m_X = x;
            m_Y = y;
            m_IsNone = false;
        }

        internal static Curvature Initial()
        {
            return new Curvature(new Angle(0), new Angle(0));
        }

        /// <summary>
        /// Returns a Curvature that applies no bend (flat).
        /// </summary>
        public static Curvature None()
        {
            Curvature none = Initial();
            none.m_IsNone = true;
            return none;
        }

        /// <summary>
        /// The bend angle around the X axis. The vertical extent curves; the sign selects concave vs. convex.
        /// </summary>
        public Angle x
        {
            get => m_X;
            set => m_X = value;
        }

        /// <summary>
        /// The bend angle around the Y axis. The horizontal extent curves; the sign selects concave vs. convex.
        /// </summary>
        public Angle y
        {
            get => m_Y;
            set => m_Y = value;
        }

        [SerializeField]
        private Angle m_X;
        [SerializeField]
        private Angle m_Y;
        [SerializeField]
        private bool m_IsNone;

        internal bool IsNone() => m_IsNone;

        /// <undoc/>
        public static bool operator==(Curvature lhs, Curvature rhs)
        {
            return lhs.m_X == rhs.m_X && lhs.m_Y == rhs.m_Y && lhs.m_IsNone == rhs.m_IsNone;
        }

        /// <undoc/>
        public static bool operator!=(Curvature lhs, Curvature rhs)
        {
            return !(lhs == rhs);
        }

        /// <undoc/>
        public bool Equals(Curvature other)
        {
            return other == this;
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is Curvature other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (m_X.GetHashCode() * 793) ^ (m_Y.GetHashCode() * 791) ^ (m_IsNone.GetHashCode() * 197);
            }
        }

        public override string ToString()
        {
            return $"{m_X.ToString()} {m_Y.ToString()}";
        }
    }
}
