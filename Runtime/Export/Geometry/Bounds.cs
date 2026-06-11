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
    [Serializable]
    public partial struct Bounds : IEquatable<Bounds>, IFormattable
    {
        private Vector3 m_Center;
        [NativeName("m_Extent")]
        private Vector3 m_Extents;

        // Creates new Bounds with a given /center/ and total /size/. Bound ::ref::extents will be half the given size.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Bounds(Vector3 center, Vector3 size)
        {
            m_Center.x = center.x;
            m_Center.y = center.y;
            m_Center.z = center.z;
            m_Extents.x = size.x * 0.5F;
            m_Extents.y = size.y * 0.5F;
            m_Extents.z = size.z * 0.5F;
        }

        // Creates new Bounds with a given /center/ and total /size/. Bound ::ref::extents will be half the given size.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public Bounds(in Vector3 center, in Vector3 size)
        {
            m_Center.x = center.x;
            m_Center.y = center.y;
            m_Center.z = center.z;
            m_Extents.x = size.x * 0.5F;
            m_Extents.y = size.y * 0.5F;
            m_Extents.z = size.z * 0.5F;
        }

        // used to allow Bounds to be used as keys in hash tables
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly int GetHashCode() => m_Center.GetHashCode() ^ (m_Extents.GetHashCode() << 2);

        // also required for being able to use Vector4s as keys in hash tables
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public override readonly bool Equals(object other)
        {
            if (other is Bounds bounds)
                return Equals(in bounds);

            return false;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(Bounds other) => m_Center.Equals(in other.m_Center) && m_Extents.Equals(in other.m_Extents);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Equals(in Bounds other) => m_Center.Equals(in other.m_Center) && m_Extents.Equals(in other.m_Extents);

        // The center of the bounding box.
        public Vector3 center
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Center;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Center = value;
        }

        // The total size of the box. This is always twice as large as the ::ref::extents.
        public Vector3 size
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector3() { x = m_Extents.x * 2.0F, y = m_Extents.y * 2.0F, z = m_Extents.z * 2.0F };
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set { m_Extents.x = value.x * 0.5F; m_Extents.y = value.y * 0.5F; m_Extents.z = value.z * 0.5F; }
        }

        // The extents of the box. This is always half of the ::ref::size.
        public Vector3 extents
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => m_Extents;
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => m_Extents = value;
        }

        // The minimal point of the box. This is always equal to ''center-extents''.
        public Vector3 min
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector3() { x = m_Center.x - m_Extents.x, y = m_Center.y - m_Extents.y, z = m_Center.z - m_Extents.z };
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => SetMinMax(in value, max);
        }

        // The maximal point of the box. This is always equal to ''center+extents''.
        public Vector3 max
        {
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] readonly get => new Vector3() { x = m_Center.x + m_Extents.x, y = m_Center.y + m_Extents.y, z = m_Center.z + m_Extents.z };
            [MethodImpl(MethodImplOptionsEx.AggressiveInlining)] set => SetMinMax(min, in value);
        }

        /// <undoc/>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator==(Bounds lhs, Bounds rhs) =>
            // Returns false in the presence of NaN values.
            (lhs.m_Center == rhs.m_Center && lhs.m_Extents == rhs.m_Extents);

        /// <undoc/>
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public static bool operator!=(Bounds lhs, Bounds rhs) =>
            // Returns true in the presence of NaN values.
            !(lhs.m_Center == rhs.m_Center && lhs.m_Extents == rhs.m_Extents);


        // Sets the bounds to the /min/ and /max/ value of the box.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void SetMinMax(Vector3 min, Vector3 max)
        {
            m_Extents.x = (max.x - min.x) * 0.5F;
            m_Extents.y = (max.y - min.y) * 0.5F;
            m_Extents.z = (max.z - min.z) * 0.5F;
            m_Center.x = min.x + m_Extents.x;
            m_Center.y = min.y + m_Extents.y;
            m_Center.z = min.z + m_Extents.z;
        }

        // Sets the bounds to the /min/ and /max/ value of the box.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void SetMinMax(in Vector3 min, in Vector3 max)
        {
            m_Extents.x = (max.x - min.x) * 0.5F;
            m_Extents.y = (max.y - min.y) * 0.5F;
            m_Extents.z = (max.z - min.z) * 0.5F;
            m_Center.x = min.x + m_Extents.x;
            m_Center.y = min.y + m_Extents.y;
            m_Center.z = min.z + m_Extents.z;
        }

        // Grows the Bounds to include the /point/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Encapsulate(Vector3 point)
        {
            Vector3 mmin = Vector3.Min(min, in point);
            Vector3 mmax = Vector3.Max(max, in point);
            SetMinMax(in mmin, in mmax);
        }

        // Grows the Bounds to include the /point/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Encapsulate(in Vector3 point)
        {
            Vector3 mmin = Vector3.Min(min, in point);
            Vector3 mmax = Vector3.Max(max, in point);
            SetMinMax(in mmin, in mmax);
        }

        // Grows the Bounds to include the /Bounds/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Encapsulate(Bounds bounds)
        {
            Vector3 boundsMin = bounds.min;
            Vector3 boundsMax = bounds.max;
            Encapsulate(in boundsMin);
            Encapsulate(in boundsMax);
        }

        // Grows the Bounds to include the /Bounds/.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Encapsulate(in Bounds bounds)
        {
            Vector3 boundsMin = bounds.min;
            Vector3 boundsMax = bounds.max;
            Encapsulate(in boundsMin);
            Encapsulate(in boundsMax);
        }

        // Expand the bounds by increasing its /size/ by /amount/ along each side.
        public void Expand(float amount)
        {
            amount *= 0.5f;
            m_Extents.x += amount;
            m_Extents.y += amount;
            m_Extents.z += amount;
        }

        // Expand the bounds by increasing its /size/ by /amount/ along each side.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Expand(Vector3 amount)
        {
            m_Extents.x += amount.x * 0.5f;
            m_Extents.y += amount.y * 0.5f;
            m_Extents.z += amount.z * 0.5f;
        }

        // Expand the bounds by increasing its /size/ by /amount/ along each side.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public void Expand(in Vector3 amount)
        {
            m_Extents.x += amount.x * 0.5f;
            m_Extents.y += amount.y * 0.5f;
            m_Extents.z += amount.z * 0.5f;
        }

        // Does another bounding box intersect with this bounding box?
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Intersects(Bounds bounds)
        {
            Vector3 thisMin = min;
            Vector3 thisMax = max;
            Vector3 otherMin = bounds.min;
            Vector3 otherMax = bounds.max;
            return (thisMin.x <= otherMax.x) && (thisMax.x >= otherMin.x) &&
                   (thisMin.y <= otherMax.y) && (thisMax.y >= otherMin.y) &&
                   (thisMin.z <= otherMax.z) && (thisMax.z >= otherMin.z);
        }

        // Does another bounding box intersect with this bounding box?
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool Intersects(in Bounds bounds)
        {
            Vector3 thisMin = min;
            Vector3 thisMax = max;
            Vector3 otherMin = bounds.min;
            Vector3 otherMax = bounds.max;
            return (thisMin.x <= otherMax.x) && (thisMax.x >= otherMin.x) &&
                   (thisMin.y <= otherMax.y) && (thisMax.y >= otherMin.y) &&
                   (thisMin.z <= otherMax.z) && (thisMax.z >= otherMin.z);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool IntersectRay(Ray ray) => IntersectRayAABB(in ray, in this, out float _);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool IntersectRay(in Ray ray) => IntersectRayAABB(in ray, in this, out float _);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool IntersectRay(Ray ray, out float distance) => IntersectRayAABB(in ray, in this, out distance);
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly bool IntersectRay(in Ray ray, out float distance) => IntersectRayAABB(in ray, in this, out distance);

        /// *listonly*
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        override public readonly string ToString() => ToString(null, null);

        // Returns a nicely formatted string for the bounds.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format) => ToString(format, null);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        public readonly string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F2";
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return string.Format("Center: {0}, Extents: {1}", m_Center.ToString(format, formatProvider), m_Extents.ToString(format, formatProvider));
        }
    }
} //namespace
