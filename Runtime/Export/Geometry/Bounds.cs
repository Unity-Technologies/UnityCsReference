// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;
using scm = System.ComponentModel;
using uei = UnityEngine.Internal;
using UnityEngine.Bindings;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

namespace UnityEngine
{
    [NativeHeader("Runtime/Geometry/AABB.h")]
    [NativeClass("AABB")]
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    public partial struct Bounds : IEquatable<Bounds>, IFormattable
    {
        private Vector3 m_Center;
        [NativeName("m_Extent")]
        private Vector3 m_Extents;

        // Creates new Bounds with a given /center/ and total /size/. Bound ::ref::extents will be half the given size.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Bounds(Vector3 center, Vector3 size)
        {
            m_Center = center;
            m_Extents = size * 0.5F;
        }

        // used to allow Bounds to be used as keys in hash tables
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override int GetHashCode()
        {
            return center.GetHashCode() ^ (extents.GetHashCode() << 2);
        }

        // also required for being able to use Vector4s as keys in hash tables
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override bool Equals(object other)
        {
            if (!(other is Bounds)) return false;

            return Equals((Bounds)other);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Equals(Bounds other)
        {
            return center.Equals(other.center) && extents.Equals(other.extents);
        }

        // The center of the bounding box.
        public Vector3 center
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_Center; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Center = value; }
        }

        // The total size of the box. This is always twice as large as the ::ref::extents.
        public Vector3 size
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_Extents * 2.0F; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Extents = value * 0.5F; }
        }

        // The extents of the box. This is always half of the ::ref::size.
        public Vector3 extents
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return m_Extents; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Extents = value; }
        }

        // The minimal point of the box. This is always equal to ''center-extents''.
        public Vector3 min
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return center - extents; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { SetMinMax(value, max); }
        }

        // The maximal point of the box. This is always equal to ''center+extents''.
        public Vector3 max
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] get { return center + extents; }
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { SetMinMax(min, value); }
        }

        //*undoc*
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(Bounds lhs, Bounds rhs)
        {
            // Returns false in the presence of NaN values.
            return (lhs.center == rhs.center && lhs.extents == rhs.extents);
        }

        //*undoc*
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(Bounds lhs, Bounds rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        // Sets the bounds to the /min/ and /max/ value of the box.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void SetMinMax(Vector3 min, Vector3 max)
        {
            extents = (max - min) * 0.5F;
            center = min + extents;
        }

        // Grows the Bounds to include the /point/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Encapsulate(Vector3 point)
        {
            SetMinMax(Vector3.Min(min, point), Vector3.Max(max, point));
        }

        // Grows the Bounds to include the /Bounds/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Encapsulate(Bounds bounds)
        {
            Encapsulate(bounds.center - bounds.extents);
            Encapsulate(bounds.center + bounds.extents);
        }

        // Expand the bounds by increasing its /size/ by /amount/ along each side.
        public void Expand(float amount)
        {
            amount *= .5f;
            extents += new Vector3(amount, amount, amount);
        }

        // Expand the bounds by increasing its /size/ by /amount/ along each side.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Expand(Vector3 amount)
        {
            extents += amount * .5f;
        }

        // Does another bounding box intersect with this bounding box?
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool Intersects(Bounds bounds)
        {
            return (min.x <= bounds.max.x) && (max.x >= bounds.min.x) &&
                (min.y <= bounds.max.y) && (max.y >= bounds.min.y) &&
                (min.z <= bounds.max.z) && (max.z >= bounds.min.z);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool IntersectRay(Ray ray) { float dist; return IntersectRayAABB(ray, this, out dist); }
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public bool IntersectRay(Ray ray, out float distance) { return IntersectRayAABB(ray, this, out distance); }


        /// *listonly*
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        override public string ToString()
        {
            return ToString(null, null);
        }

        // Returns a nicely formatted string for the bounds.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public string ToString(string format)
        {
            return ToString(format, null);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F2";
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return UnityString.Format("Center: {0}, Extents: {1}", m_Center.ToString(format, formatProvider), m_Extents.ToString(format, formatProvider));
        }
    }
} //namespace
