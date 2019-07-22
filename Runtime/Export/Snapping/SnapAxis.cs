// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine
{
    [Flags]
    public enum SnapAxis : byte
    {
        None = 0 << 0,
        X = 1 << 0,
        Y = 1 << 1,
        Z = 1 << 2,
        All = X | Y | Z
    }

    struct SnapAxisFilter : IEquatable<SnapAxisFilter>
    {
        const SnapAxis X = SnapAxis.X;
        const SnapAxis Y = SnapAxis.Y;
        const SnapAxis Z = SnapAxis.Z;

        public static readonly SnapAxisFilter all = new SnapAxisFilter(X | Y | Z);

        SnapAxis m_Mask;

        public float x
        {
            get { return (m_Mask & X) == X ? 1f : 0f; }
        }

        public float y
        {
            get { return (m_Mask & Y) == Y ? 1f : 0f; }
        }

        public float z
        {
            get { return (m_Mask & Z) == Z ? 1f : 0f; }
        }

        public SnapAxisFilter(Vector3 v)
        {
            m_Mask = 0x0;
            var epsilon = 0.000001f;

            if (Mathf.Abs(v.x) > epsilon)
                m_Mask |= X;
            if (Mathf.Abs(v.y) > epsilon)
                m_Mask |= Y;
            if (Mathf.Abs(v.z) > epsilon)
                m_Mask |= Z;
        }

        public SnapAxisFilter(SnapAxis axis)
        {
            m_Mask = 0x0;

            if ((axis & SnapAxis.X) == SnapAxis.X)
                m_Mask |= X;
            if ((axis & SnapAxis.Y) == SnapAxis.Y)
                m_Mask |= Y;
            if ((axis & SnapAxis.Z) == SnapAxis.Z)
                m_Mask |= Z;
        }

        public override string ToString()
        {
            return string.Format("{{{0}, {1}, {2}}}", x, y, z);
        }

        /// <summary>
        /// The number of toggled axes.
        /// </summary>
        public int active
        {
            get
            {
                int count = 0;
                if ((m_Mask & X) > 0)
                    count++;
                if ((m_Mask & Y) > 0)
                    count++;
                if ((m_Mask & Z) > 0)
                    count++;
                return count;
            }
        }

        public static implicit operator Vector3(SnapAxisFilter mask)
        {
            return new Vector3(mask.x, mask.y, mask.z);
        }

        public static explicit operator SnapAxisFilter(Vector3 v)
        {
            return new SnapAxisFilter(v);
        }

        public static explicit operator SnapAxis(SnapAxisFilter mask)
        {
            return mask.m_Mask;
        }

        public static SnapAxisFilter operator|(SnapAxisFilter left, SnapAxisFilter right)
        {
            return new SnapAxisFilter(left.m_Mask | right.m_Mask);
        }

        public static SnapAxisFilter operator&(SnapAxisFilter left, SnapAxisFilter right)
        {
            return new SnapAxisFilter(left.m_Mask & right.m_Mask);
        }

        public static SnapAxisFilter operator^(SnapAxisFilter left, SnapAxisFilter right)
        {
            return new SnapAxisFilter(left.m_Mask ^ right.m_Mask);
        }

        public static SnapAxisFilter operator~(SnapAxisFilter left)
        {
            return new SnapAxisFilter(~left.m_Mask);
        }

        public static Vector3 operator*(SnapAxisFilter mask, float value)
        {
            return new Vector3(mask.x * value, mask.y * value, mask.z * value);
        }

        public static Vector3 operator*(SnapAxisFilter mask, Vector3 right)
        {
            return new Vector3(mask.x * right.x, mask.y * right.y, mask.z * right.z);
        }

        public static Vector3 operator*(Quaternion rotation, SnapAxisFilter mask)
        {
            var active = mask.active;

            if (active > 2)
                return mask;

            var rotated = (rotation * (Vector3)mask);
            rotated = new Vector3(Mathf.Abs(rotated.x), Mathf.Abs(rotated.y), Mathf.Abs(rotated.z));

            if (active > 1)
            {
                return new Vector3(
                    rotated.x > rotated.y || rotated.x > rotated.z ? 1 : 0,
                    rotated.y > rotated.x || rotated.y > rotated.z ? 1 : 0,
                    rotated.z > rotated.x || rotated.z > rotated.y ? 1 : 0
                );
            }

            return new Vector3(
                rotated.x > rotated.y && rotated.x > rotated.z ? 1 : 0,
                rotated.y > rotated.z && rotated.y > rotated.x ? 1 : 0,
                rotated.z > rotated.x && rotated.z > rotated.y ? 1 : 0);
        }

        public static bool operator==(SnapAxisFilter left, SnapAxisFilter right)
        {
            return left.m_Mask == right.m_Mask;
        }

        public static bool operator!=(SnapAxisFilter left, SnapAxisFilter right)
        {
            return !(left == right);
        }

        public float this[int i]
        {
            get
            {
                if (i < 0 || i > 2)
                    throw new IndexOutOfRangeException();

                return (1 & ((int)m_Mask >> i)) * 1f;
            }

            set
            {
                if (i < 0 || i > 2)
                    throw new IndexOutOfRangeException();

                m_Mask &= (SnapAxis) ~(1 << i);
                m_Mask |= (SnapAxis)((value > 0f ? 1 : 0) << i);
            }
        }

        public bool Equals(SnapAxisFilter other)
        {
            return m_Mask == other.m_Mask;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is SnapAxisFilter && Equals((SnapAxisFilter)obj);
        }

        public override int GetHashCode()
        {
            return m_Mask.GetHashCode();
        }
    }
}
