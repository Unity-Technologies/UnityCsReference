// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;
using static Unity.Mathematics.math;

namespace Unity.Mathematics.Geometry
{
    /// <summary>
    /// Axis aligned bounding box (AABB) stored in min and max form.
    /// </summary>
    /// <remarks>
    /// Axis aligned bounding boxes (AABB) are boxes where each side is parallel with one of the Cartesian coordinate axes
    /// X, Y, and Z. AABBs are useful for approximating the region an object (or collection of objects) occupies and quickly
    /// testing whether or not that object (or collection of objects) is relevant. Because they are axis aligned, they
    /// are very cheap to construct and perform overlap tests with them.
    /// </remarks>
    [System.Serializable]
    [Il2CppEagerStaticClassConstruction]
    public struct MinMaxAABB : IEquatable<MinMaxAABB>
    {
        /// <summary>
        /// The minimum point contained by the AABB.
        /// </summary>
        /// <remarks>
        /// If any component of <see cref="Min"/> is greater than <see cref="Max"/> then this AABB is invalid.
        /// </remarks>
        /// <seealso cref="IsValid"/>
        public float3 Min;

        /// <summary>
        /// The maximum point contained by the AABB.
        /// </summary>
        /// <remarks>
        /// If any component of <see cref="Max"/> is less than <see cref="Min"/> then this AABB is invalid.
        /// </remarks>
        /// <seealso cref="IsValid"/>
        public float3 Max;

        /// <summary>
        /// Constructs the AABB with the given minimum and maximum.
        /// </summary>
        /// <remarks>
        /// If you have a center and extents, you can call <see cref="CreateFromCenterAndExtents"/> or <see cref="CreateFromCenterAndHalfExtents"/>
        /// to create the AABB.
        /// </remarks>
        /// <param name="min">Minimum point inside AABB.</param>
        /// <param name="max">Maximum point inside AABB.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MinMaxAABB(float3 min, float3 max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Creates the AABB from a center and extents.
        /// </summary>
        /// <remarks>
        /// This function takes full extents. It is the distance between <see cref="Min"/> and <see cref="Max"/>.
        /// If you have half extents, you can call <see cref="CreateFromCenterAndHalfExtents"/>.
        /// </remarks>
        /// <param name="center">Center of AABB.</param>
        /// <param name="extents">Full extents of AABB.</param>
        /// <returns>AABB created from inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MinMaxAABB CreateFromCenterAndExtents(float3 center, float3 extents)
        {
            return CreateFromCenterAndHalfExtents(center, extents * 0.5f);
        }

        /// <summary>
        /// Creates the AABB from a center and half extents.
        /// </summary>
        /// <remarks>
        /// This function takes half extents. It is half the distance between <see cref="Min"/> and <see cref="Max"/>.
        /// If you have full extents, you can call <see cref="CreateFromCenterAndExtents"/>.
        /// </remarks>
        /// <param name="center">Center of AABB.</param>
        /// <param name="halfExtents">Half extents of AABB.</param>
        /// <returns>AABB created from inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MinMaxAABB CreateFromCenterAndHalfExtents(float3 center, float3 halfExtents)
        {
            return new MinMaxAABB(center - halfExtents, center + halfExtents);
        }

        /// <summary>
        /// Computes the extents of the AABB.
        /// </summary>
        /// <remarks>
        /// Extents is the componentwise distance between min and max.
        /// </remarks>
        public float3 Extents => Max - Min;

        /// <summary>
        /// Computes the half extents of the AABB.
        /// </summary>
        /// <remarks>
        /// HalfExtents is half of the componentwise distance between min and max. Subtracting HalfExtents from Center
        /// gives Min and adding HalfExtents to Center gives Max.
        /// </remarks>
        public float3 HalfExtents => (Max - Min) * 0.5f;

        /// <summary>
        /// Computes the center of the AABB.
        /// </summary>
        public float3 Center => (Max + Min) * 0.5f;

        /// <summary>
        /// Check if the AABB is valid.
        /// </summary>
        /// <remarks>
        /// An AABB is considered valid if <see cref="Min"/> is componentwise less than or equal to <see cref="Max"/>.
        /// </remarks>
        /// <returns>True if <see cref="Min"/> is componentwise less than or equal to <see cref="Max"/>.</returns>
        public bool IsValid => math.all(Min <= Max);

        /// <summary>
        /// Computes the surface area for this axis aligned bounding box.
        /// </summary>
        public float SurfaceArea
        {
            get
            {
                float3 diff = Max - Min;
                return 2 * math.dot(diff, diff.yzx);
            }
        }

        /// <summary>
        /// Tests if the input point is contained by the AABB.
        /// </summary>
        /// <param name="point">Point to test.</param>
        /// <returns>True if AABB contains the input point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(float3 point) => math.all(point >= Min & point <= Max);

        /// <summary>
        /// Tests if the input AABB is contained entirely by this AABB.
        /// </summary>
        /// <param name="aabb">AABB to test.</param>
        /// <returns>True if input AABB is contained entirely by this AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(MinMaxAABB aabb) => math.all((Min <= aabb.Min) & (Max >= aabb.Max));

        /// <summary>
        /// Tests if the input AABB overlaps this AABB.
        /// </summary>
        /// <param name="aabb">AABB to test.</param>
        /// <returns>True if input AABB overlaps with this AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Overlaps(MinMaxAABB aabb)
        {
            return math.all(Max >= aabb.Min & Min <= aabb.Max);
        }

        /// <summary>
        /// Expands the AABB by the given signed distance.
        /// </summary>
        /// <remarks>
        /// Positive distance expands the AABB while negative distance shrinks the AABB.
        /// </remarks>
        /// <param name="signedDistance">Signed distance to expand the AABB with.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Expand(float signedDistance)
        {
            Min -= signedDistance;
            Max += signedDistance;
        }

        /// <summary>
        /// Encapsulates the given AABB.
        /// </summary>
        /// <remarks>
        /// Modifies this AABB so that it contains the given AABB. If the given AABB is already contained by this AABB,
        /// then this AABB doesn't change.
        /// </remarks>
        /// <seealso cref="Contains(Unity.Mathematics.Geometry.MinMaxAABB)"/>
        /// <param name="aabb">AABB to encapsulate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(MinMaxAABB aabb)
        {
            Min = math.min(Min, aabb.Min);
            Max = math.max(Max, aabb.Max);
        }

        /// <summary>
        /// Encapsulate the given point.
        /// </summary>
        /// <remarks>
        /// Modifies this AABB so that it contains the given point. If the given point is already contained by this AABB,
        /// then this AABB doesn't change.
        /// </remarks>
        /// <seealso cref="Contains(Unity.Mathematics.float3)"/>
        /// <param name="point">Point to encapsulate.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Encapsulate(float3 point)
        {
            Min = math.min(Min, point);
            Max = math.max(Max, point);
        }
        /// <summary>
        /// Determines whether the specified other AABB is equal to the current AABB.
        /// </summary>
        /// <param name="other">The other AABB to compare this one with.</param>
        /// <returns>True if the specified other AABB is equal to this AABB; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(MinMaxAABB other)
        {
            return Min.Equals(other.Min) && Max.Equals(other.Max);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return string.Format("MinMaxAABB({0}, {1})", Min, Max);
        }
    }

    /// <summary>
    /// Class containing math functions for <see cref="MinMaxAABB"/>.
    /// </summary>
    /// <remarks>
    /// Use the static methods in this class to transform AABBs.
    /// </remarks>
    public static partial class Math
    {
        /// <summary>
        /// Transforms the AABB with the given transform.
        /// </summary>
        /// <remarks>
        /// The resulting AABB encapsulates the transformed AABB which may not be axis aligned after the transformation.
        /// </remarks>
        /// <param name="transform">Transform to apply to AABB.</param>
        /// <param name="aabb">AABB to be transformed.</param>
        /// <returns>Transformed AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MinMaxAABB Transform(RigidTransform transform, MinMaxAABB aabb)
        {
            float3 halfExtentsInA = aabb.HalfExtents;

            // Rotate each axis individually and find their new positions in the rotated space.
            float3 x = math.rotate(transform.rot, new float3(halfExtentsInA.x, 0, 0));
            float3 y = math.rotate(transform.rot, new float3(0, halfExtentsInA.y, 0));
            float3 z = math.rotate(transform.rot, new float3(0, 0, halfExtentsInA.z));

            // Find the new max corner by summing the rotated axes.  Absolute value of each axis
            // since we are trying to find the max corner.
            float3 halfExtentsInB = math.abs(x) + math.abs(y) + math.abs(z);
            float3 centerInB = math.transform(transform, aabb.Center);

            return new MinMaxAABB(centerInB - halfExtentsInB, centerInB + halfExtentsInB);
        }

        /// <summary>
        /// Transforms the AABB with the given transform.
        /// </summary>
        /// <remarks>
        /// The resulting AABB encapsulates the transformed AABB which may not be axis aligned after the transformation.
        /// </remarks>
        /// <param name="transform">Transform to apply to AABB.</param>
        /// <param name="aabb">AABB to be transformed.</param>
        /// <returns>Transformed AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MinMaxAABB Transform(float4x4 transform, MinMaxAABB aabb)
        {
            var transformed = Transform(new float3x3(transform), aabb);
            transformed.Min += transform.c3.xyz;
            transformed.Max += transform.c3.xyz;
            return transformed;
        }

        /// <summary>
        /// Transforms the AABB with the given transform.
        /// </summary>
        /// <remarks>
        /// The resulting AABB encapsulates the transformed AABB which may not be axis aligned after the transformation.
        /// </remarks>
        /// <param name="transform">Transform to apply to AABB.</param>
        /// <param name="aabb">AABB to be transformed.</param>
        /// <returns>Transformed AABB.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MinMaxAABB Transform(float3x3 transform, MinMaxAABB aabb)
        {
            // From Christer Ericson's Real-Time Collision Detection on page 86 and 87.
            // We want the transformed minimum and maximums of the AABB. Multiplying a 3x3 matrix on the left of a
            // column vector looks like so:
            //
            // [ c0.x c1.x c2.x ] [ x ]   [ c0.x * x + c1.x * y + c2.x * z ]
            // [ c0.y c1.y c2.y ] [ y ] = [ c0.y * x + c1.y * y + c2.y * z ]
            // [ c0.z c1.z c2.z ] [ z ]   [ c0.z * x + c1.z * y + c2.z * z ]
            //
            // The column vectors we will use are the input AABB's min and max. Simply multiplying those two vectors
            // with the transformation matrix won't guarantee we get the new min and max since those are only two
            // points out of eight in the AABB and one of the other six may set the new min or max.
            //
            // To ensure we get the correct min and max, we must transform all eight points. But it's not necessary
            // to actually perform eight matrix multiplies to get our final result. Instead, we can build the min and
            // max incrementally by computing each term in the above matrix multiply separately then summing the min
            // (or max). For instance, to find the new minimum contributed by the original min and max x component, we
            // compute this:
            //
            // newMin.x = min(c0.x * Min.x, c0.x * Max.x);
            // newMin.y = min(c0.y * Min.x, c0.y * Max.x);
            // newMin.z = min(c0.z * Min.x, c0.z * Max.x);
            //
            // Then we add minimum contributed by the original min and max y components:
            //
            // newMin.x += min(c1.x * Min.y, c1.x * Max.y);
            // newMin.y += min(c1.y * Min.y, c1.y * Max.y);
            // newMin.z += min(c1.z * Min.y, c1.z * Max.y);
            //
            // And so on. Translation can be handled by simply initializing the new min and max with the translation
            // amount since it does not affect the min and max bounds in local space.
            var t1 = transform.c0.xyz * aabb.Min.xxx;
            var t2 = transform.c0.xyz * aabb.Max.xxx;
            var minMask = t1 < t2;
            var transformed = new MinMaxAABB(select(t2, t1, minMask), select(t2, t1, !minMask));
            t1 = transform.c1.xyz * aabb.Min.yyy;
            t2 = transform.c1.xyz * aabb.Max.yyy;
            minMask = t1 < t2;
            transformed.Min += select(t2, t1, minMask);
            transformed.Max += select(t2, t1, !minMask);
            t1 = transform.c2.xyz * aabb.Min.zzz;
            t2 = transform.c2.xyz * aabb.Max.zzz;
            minMask = t1 < t2;
            transformed.Min += select(t2, t1, minMask);
            transformed.Max += select(t2, t1, !minMask);
            return transformed;
        }
    }
}
