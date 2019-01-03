// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine
{
    // Representation of rays.
    public partial struct Ray
    {
        private Vector3 m_Origin;
        private Vector3 m_Direction;

        // Creates a ray starting at /origin/ along /direction/.
        public Ray(Vector3 origin, Vector3 direction)
        {
            m_Origin = origin;
            m_Direction = direction.normalized;
        }

        // The origin point of the ray.
        public Vector3 origin
        {
            get { return m_Origin; }
            set { m_Origin = value; }
        }

        // The direction of the ray.
        public Vector3 direction
        {
            get { return m_Direction; }
            set { m_Direction = value.normalized; }
        }

        // Returns a point at /distance/ units along the ray.
        public Vector3 GetPoint(float distance)
        {
            return m_Origin + m_Direction * distance;
        }

        public override string ToString()
        {
            return UnityString.Format("Origin: {0}, Dir: {1}", m_Origin, m_Direction);
        }

        public string ToString(string format)
        {
            return UnityString.Format("Origin: {0}, Dir: {1}", m_Origin.ToString(format), m_Direction.ToString(format));
        }
    }
}
