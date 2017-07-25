// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // Representation of planes. Uses the formula Ax + By + Cz + D = 0.
    [UsedByNativeCode]
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct Plane
    {
        Vector3 m_Normal;
        float m_Distance;

        // Normal vector of the plane.
        public Vector3 normal
        {
            get { return m_Normal; }
            set { m_Normal = value; }
        }
        // Distance from the origin to the plane.
        public float distance
        {
            get { return m_Distance; }
            set { m_Distance = value; }
        }

        // Creates a plane.
        public Plane(Vector3 inNormal, Vector3 inPoint)
        {
            m_Normal = Vector3.Normalize(inNormal);
            m_Distance = -Vector3.Dot(m_Normal, inPoint);
        }

        // Creates a plane.
        public Plane(Vector3 inNormal, float d)
        {
            m_Normal = Vector3.Normalize(inNormal);
            m_Distance = d;
        }

        // Creates a plane.
        public Plane(Vector3 a, Vector3 b, Vector3 c)
        {
            m_Normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            m_Distance = -Vector3.Dot(m_Normal, a);
        }

        // Sets a plane using a point that lies within it plus a normal to orient it (note that the normal must be a normalized vector).
        public void SetNormalAndPosition(Vector3 inNormal, Vector3 inPoint)
        {
            m_Normal = Vector3.Normalize(inNormal);
            m_Distance = -Vector3.Dot(inNormal, inPoint);
        }

        // Sets a plane using three points that lie within it.  The points go around clockwise as you look down on the top surface of the plane.
        public void Set3Points(Vector3 a, Vector3 b, Vector3 c)
        {
            m_Normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            m_Distance = -Vector3.Dot(m_Normal, a);
        }

        // Make the plane face the opposite direction
        public void Flip() { m_Normal = -m_Normal; m_Distance = -m_Distance; }

        // Return a version of the plane that faces the opposite direction
        public Plane flipped { get { return new Plane(-m_Normal, -m_Distance); } }

        // Translates the plane into a given direction
        public void Translate(Vector3 translation) { m_Distance += Vector3.Dot(m_Normal, translation); }

        // Creates a plane that's translated into a given direction
        public static Plane Translate(Plane plane, Vector3 translation) { return new Plane(plane.m_Normal, plane.m_Distance += Vector3.Dot(plane.m_Normal, translation)); }

        // Calculates the closest point on the plane.
        public Vector3 ClosestPointOnPlane(Vector3 point)
        {
            var pointToPlaneDistance = Vector3.Dot(m_Normal, point) + m_Distance;
            return point - (m_Normal * pointToPlaneDistance);
        }

        // Returns a signed distance from plane to point.
        public float GetDistanceToPoint(Vector3 point) { return Vector3.Dot(m_Normal, point) + m_Distance; }

        // Is a point on the positive side of the plane?
        public bool GetSide(Vector3 point) { return Vector3.Dot(m_Normal, point) + m_Distance > 0.0F; }

        // Are two points on the same side of the plane?
        public bool SameSide(Vector3 inPt0, Vector3 inPt1)
        {
            float d0 = GetDistanceToPoint(inPt0);
            float d1 = GetDistanceToPoint(inPt1);
            return (d0 >  0.0f && d1 >  0.0f) ||
                (d0 <= 0.0f && d1 <= 0.0f);
        }

        // Intersects a ray with the plane.
        public bool Raycast(Ray ray, out float enter)
        {
            float vdot = Vector3.Dot(ray.direction, m_Normal);
            float ndot = -Vector3.Dot(ray.origin, m_Normal) - m_Distance;

            if (Mathf.Approximately(vdot, 0.0f))
            {
                enter = 0.0F;
                return false;
            }

            enter = ndot / vdot;

            return enter > 0.0F;
        }

        public override string ToString()
        {
            return UnityString.Format("(normal:({0:F1}, {1:F1}, {2:F1}), distance:{3:F1})", m_Normal.x, m_Normal.y, m_Normal.z, m_Distance);
        }

        public string ToString(string format)
        {
            return UnityString.Format("(normal:({0}, {1}, {2}), distance:{3})", m_Normal.x.ToString(format), m_Normal.y.ToString(format), m_Normal.z.ToString(format), m_Distance.ToString(format));
        }
    }
}
