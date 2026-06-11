// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Representation of planes. Uses the formula Ax + By + Cz + D = 0.
    [UsedByNativeCode]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Plane : IEquatable<Plane>, IFormattable
    {
        // sizeof(Plane) is not const in C# and so cannot be used in fixed arrays, so we define it here
        internal const int size = 16;

        Vector3 m_Normal;
        float m_Distance;

        // Normal vector of the plane.
        public Vector3 normal
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Normal;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Normal = value;
        }
        // Distance from the origin to the plane.
        public float distance
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Distance;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Distance = value;
        }

        // Creates a plane.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Plane(Vector3 inNormal, Vector3 inPoint)
        {
            m_Normal.x = inNormal.x;
            m_Normal.y = inNormal.y;
            m_Normal.z = inNormal.z;
            m_Normal.Normalize();

            m_Distance = -Vector3.Dot(in m_Normal, in inPoint);
        }

        // Creates a plane.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Plane(in Vector3 inNormal, in Vector3 inPoint)
        {
            m_Normal.x = inNormal.x;
            m_Normal.y = inNormal.y;
            m_Normal.z = inNormal.z;
            m_Normal.Normalize();

            m_Distance = -Vector3.Dot(in m_Normal, in inPoint);
        }

        // Creates a plane.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Plane(Vector3 inNormal, float d)
        {
            m_Normal.x = inNormal.x;
            m_Normal.y = inNormal.y;
            m_Normal.z = inNormal.z;
            m_Normal.Normalize();

            m_Distance = d;
        }

        // Creates a plane.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Plane(in Vector3 inNormal, float d)
        {
            m_Normal.x = inNormal.x;
            m_Normal.y = inNormal.y;
            m_Normal.z = inNormal.z;
            m_Normal.Normalize();

            m_Distance = d;
        }

        // Creates a plane.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Plane(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ba;
            ba.x = b.x - a.x;
            ba.y = b.y - a.y;
            ba.z = b.z - a.z;

            Vector3 ca;
            ca.x = c.x - a.x;
            ca.y = c.y - a.y;
            ca.z = c.z - a.z;

            Vector3 cross = Vector3.Cross(in ba, in ca);
            m_Normal.x = cross.x;
            m_Normal.y = cross.y;
            m_Normal.z = cross.z;
            m_Normal.Normalize();

            m_Distance = -Vector3.Dot(in m_Normal, in a);
        }

        // Creates a plane.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Plane(in Vector3 a, in Vector3 b, in Vector3 c)
        {
            Vector3 ba;
            ba.x = b.x - a.x;
            ba.y = b.y - a.y;
            ba.z = b.z - a.z;

            Vector3 ca;
            ca.x = c.x - a.x;
            ca.y = c.y - a.y;
            ca.z = c.z - a.z;

            Vector3 cross = Vector3.Cross(in ba, in ca);
            m_Normal.x = cross.x;
            m_Normal.y = cross.y;
            m_Normal.z = cross.z;
            m_Normal.Normalize();

            m_Distance = -Vector3.Dot(in m_Normal, in a);
        }

        // Sets a plane using a point that lies within it plus a normal to orient it
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void SetNormalAndPosition(Vector3 inNormal, Vector3 inPoint)
        {
            m_Normal.x = inNormal.x;
            m_Normal.y = inNormal.y;
            m_Normal.z = inNormal.z;
            m_Normal.Normalize();

            m_Distance = -Vector3.Dot(in m_Normal, in inPoint);
        }

        // Sets a plane using a point that lies within it plus a normal to orient it
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void SetNormalAndPosition(in Vector3 inNormal, in Vector3 inPoint)
        {
            m_Normal.x = inNormal.x;
            m_Normal.y = inNormal.y;
            m_Normal.z = inNormal.z;
            m_Normal.Normalize();

            m_Distance = -Vector3.Dot(in m_Normal, in inPoint);
        }

        // Sets a plane using three points that lie within it.  The points go around clockwise as you look down on the top surface of the plane.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Set3Points(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ba;
            ba.x = b.x - a.x;
            ba.y = b.y - a.y;
            ba.z = b.z - a.z;

            Vector3 ca;
            ca.x = c.x - a.x;
            ca.y = c.y - a.y;
            ca.z = c.z - a.z;

            Vector3 cross = Vector3.Cross(in ba, in ca);
            m_Normal.x = cross.x;
            m_Normal.y = cross.y;
            m_Normal.z = cross.z;
            m_Normal.Normalize();

            m_Distance = -Vector3.Dot(in m_Normal, in a);
        }

        // Sets a plane using three points that lie within it.  The points go around clockwise as you look down on the top surface of the plane.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Set3Points(in Vector3 a, in Vector3 b, in Vector3 c)
        {
            Vector3 ba;
            ba.x = b.x - a.x;
            ba.y = b.y - a.y;
            ba.z = b.z - a.z;

            Vector3 ca;
            ca.x = c.x - a.x;
            ca.y = c.y - a.y;
            ca.z = c.z - a.z;

            Vector3 cross = Vector3.Cross(in ba, in ca);
            m_Normal.x = cross.x;
            m_Normal.y = cross.y;
            m_Normal.z = cross.z;
            m_Normal.Normalize();

            m_Distance = -Vector3.Dot(in m_Normal, in a);
        }

        // Make the plane face the opposite direction
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Flip()
        {
            m_Normal.x = -m_Normal.x;
            m_Normal.y = -m_Normal.y;
            m_Normal.z = -m_Normal.z;
            m_Distance = -m_Distance;
        }

        // Return a version of the plane that faces the opposite direction
        public readonly Plane flipped
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
            get => new Plane(-m_Normal, -m_Distance);
        }

        // Translates the plane into a given direction
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Translate(Vector3 translation) => m_Distance += Vector3.Dot(in m_Normal, in translation);

        // Translates the plane into a given direction
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Translate(in Vector3 translation) => m_Distance += Vector3.Dot(in m_Normal, in translation);

        // Creates a plane that's translated into a given direction
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Plane Translate(Plane plane, Vector3 translation) => new Plane(in plane.m_Normal, plane.m_Distance + Vector3.Dot(in plane.m_Normal, in translation));

        // Creates a plane that's translated into a given direction
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static Plane Translate(in Plane plane, in Vector3 translation) => new Plane(in plane.m_Normal, plane.m_Distance + Vector3.Dot(in plane.m_Normal, in translation));

        // Calculates the closest point on the plane.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly Vector3 ClosestPointOnPlane(Vector3 point)
        {
            var pointToPlaneDistance = Vector3.Dot(in m_Normal, in point) + m_Distance;

            Vector3 closest;
            closest.x = point.x - (m_Normal.x * pointToPlaneDistance);
            closest.y = point.y - (m_Normal.y * pointToPlaneDistance);
            closest.z = point.z - (m_Normal.z * pointToPlaneDistance);

            return closest;
        }

        // Calculates the closest point on the plane.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly Vector3 ClosestPointOnPlane(in Vector3 point)
        {
            var pointToPlaneDistance = Vector3.Dot(in m_Normal, in point) + m_Distance;

            Vector3 closest;
            closest.x = point.x - (m_Normal.x * pointToPlaneDistance);
            closest.y = point.y - (m_Normal.y * pointToPlaneDistance);
            closest.z = point.z - (m_Normal.z * pointToPlaneDistance);

            return closest;
        }

        // Returns a signed distance from plane to point.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly float GetDistanceToPoint(Vector3 point) => Vector3.Dot(in m_Normal, in point) + m_Distance;

        // Returns a signed distance from plane to point.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly float GetDistanceToPoint(in Vector3 point) => Vector3.Dot(in m_Normal, in point) + m_Distance;

        // Is a point on the positive side of the plane?
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool GetSide(Vector3 point) => Vector3.Dot(in m_Normal, in point) + m_Distance > 0.0F;

        // Is a point on the positive side of the plane?
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool GetSide(in Vector3 point) => Vector3.Dot(in m_Normal, in point) + m_Distance > 0.0F;

        // Are two points on the same side of the plane?
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool SameSide(Vector3 inPt0, Vector3 inPt1)
        {
            float d0 = GetDistanceToPoint(in inPt0);
            float d1 = GetDistanceToPoint(in inPt1);
            return (d0 >  0.0f && d1 >  0.0f) ||
                (d0 <= 0.0f && d1 <= 0.0f);
        }

        // Are two points on the same side of the plane?
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool SameSide(in Vector3 inPt0, in Vector3 inPt1)
        {
            float d0 = GetDistanceToPoint(in inPt0);
            float d1 = GetDistanceToPoint(in inPt1);
            return (d0 >  0.0f && d1 >  0.0f) ||
                (d0 <= 0.0f && d1 <= 0.0f);
        }

        // Intersects a ray with the plane.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Raycast(Ray ray, out float enter)
        {
            float vdot = Vector3.Dot(ray.direction, in m_Normal);
            float ndot = -Vector3.Dot(ray.origin, in m_Normal) - m_Distance;

            if (Mathf.Approximately(vdot, 0.0f))
            {
                enter = 0.0F;
                return false;
            }

            enter = ndot / vdot;

            return enter > 0.0F;
        }

        // Intersects a ray with the plane.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Raycast(in Ray ray, out float enter)
        {
            float vdot = Vector3.Dot(ray.direction, in m_Normal);
            float ndot = -Vector3.Dot(ray.origin, in m_Normal) - m_Distance;

            if (Mathf.Approximately(vdot, 0.0f))
            {
                enter = 0.0F;
                return false;
            }

            enter = ndot / vdot;

            return enter > 0.0F;
        }

        /// <undoc/>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(Plane lhs, Plane rhs) => lhs.m_Normal == rhs.m_Normal && lhs.m_Distance == rhs.m_Distance;

        /// <undoc/>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(Plane lhs, Plane rhs) => !(lhs.m_Normal == rhs.m_Normal && lhs.m_Distance == rhs.m_Distance);


        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly bool Equals(object other)
        {
            if (other is Plane plane)
                return Equals(in plane);
            return false;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(Plane other) => this == other;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(in Plane other) => this == other;

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly int GetHashCode() => m_Distance.GetHashCode() ^ (m_Normal.GetHashCode() << 2);

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
            return string.Format("(normal:{0}, distance:{1})", m_Normal.ToString(format, formatProvider), m_Distance.ToString(format, formatProvider));
        }
    }
}
