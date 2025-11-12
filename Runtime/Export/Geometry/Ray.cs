// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace UnityEngine
{
    // Representation of rays.
    public partial struct Ray : IFormattable
    {
        private Vector3 m_Origin;
        private Vector3 m_Direction;

        // Creates a ray starting at /origin/ along /direction/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Ray(Vector3 origin, Vector3 direction)
        {
            m_Origin = origin;
            m_Direction = direction;
            m_Direction.Normalize();
        }

        // Creates a ray starting at /origin/ along /direction/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Ray(in Vector3 origin, in Vector3 direction)
        {
            m_Origin = origin;
            m_Direction = direction;
            m_Direction.Normalize();
        }

        // The origin point of the ray.
        public Vector3 origin
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Origin;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Origin = value;
        }

        // The direction of the ray.
        public Vector3 direction
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Direction;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Direction = value; m_Direction.Normalize(); }
        }

        // Returns a point at /distance/ units along the ray.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly Vector3 GetPoint(float distance) => new Vector3() {
            x = m_Origin.x + m_Direction.x * distance,
            y = m_Origin.y + m_Direction.y * distance,
            z = m_Origin.z + m_Direction.z * distance
        };

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly string ToString() => ToString(null, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format) => ToString(format, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F2";
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("Origin: {0}, Dir: {1}", m_Origin.ToString(format, formatProvider), m_Direction.ToString(format, formatProvider));
        }
    }
}
