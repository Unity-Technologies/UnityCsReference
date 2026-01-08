// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace Unity.Mathematics.Geometry
{
    /// <summary>
    /// A plane represented by a normal vector and a distance along the normal from the origin.
    /// </summary>
    /// <remarks>
    /// A plane splits the 3D space in half.  The normal vector points to the positive half and the other half is
    /// considered negative.
    /// </remarks>
    [DebuggerDisplay("{Normal}, {Distance}")]
    [Serializable]
    [Il2CppEagerStaticClassConstruction]
    public struct Plane
    {
        /// <summary>
        /// A plane in the form Ax + By + Cz + Dw = 0.
        /// </summary>
        /// <remarks>
        /// Stores the plane coefficients A, B, C, D where (A, B, C) is a unit normal vector and D is the distance
        /// from the origin.  A plane stored with a unit normal vector is called a normalized plane.
        /// </remarks>
        public float4 NormalAndDistance;

        /// <summary>
        /// Constructs a Plane from arbitrary coefficients A, B, C, D of the plane equation Ax + By + Cz + Dw = 0.
        /// </summary>
        /// <remarks>
        /// The constructed plane will be the normalized form of the plane specified by the given coefficients.
        /// </remarks>
        /// <param name="coefficientA">Coefficient A from plane equation.</param>
        /// <param name="coefficientB">Coefficient B from plane equation.</param>
        /// <param name="coefficientC">Coefficient C from plane equation.</param>
        /// <param name="coefficientD">Coefficient D from plane equation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Plane(float coefficientA, float coefficientB, float coefficientC, float coefficientD)
        {
            NormalAndDistance = Normalize(new float4(coefficientA, coefficientB, coefficientC, coefficientD));
        }

        /// <summary>
        /// Constructs a plane with a normal vector and distance from the origin.
        /// </summary>
        /// <remarks>
        /// The constructed plane will be the normalized form of the plane specified by the inputs.
        /// </remarks>
        /// <param name="normal">A non-zero vector that is perpendicular to the plane.  It may be any length.</param>
        /// <param name="distance">Distance from the origin along the normal.  A negative value moves the plane in the
        /// same direction as the normal while a positive value moves it in the opposite direction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Plane(float3 normal, float distance)
        {
            NormalAndDistance = Normalize(new float4(normal, distance));
        }

        /// <summary>
        /// Constructs a plane with a normal vector and a point that lies in the plane.
        /// </summary>
        /// <remarks>
        /// The constructed plane will be the normalized form of the plane specified by the inputs.
        /// </remarks>
        /// <param name="normal">A non-zero vector that is perpendicular to the plane.  It may be any length.</param>
        /// <param name="pointInPlane">A point that lies in the plane.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Plane(float3 normal, float3 pointInPlane)
        : this(normal, -math.dot(normal, pointInPlane))
        {
        }

        /// <summary>
        /// Constructs a plane with two vectors and a point that all lie in the plane.
        /// </summary>
        /// <remarks>
        /// The constructed plane will be the normalized form of the plane specified by the inputs.
        /// </remarks>
        /// <param name="vector1InPlane">A non-zero vector that lies in the plane.  It may be any length.</param>
        /// <param name="vector2InPlane">A non-zero vector that lies in the plane.  It may be any length and must not be a scalar multiple of <paramref name="vector1InPlane"/>.</param>
        /// <param name="pointInPlane">A point that lies in the plane.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Plane(float3 vector1InPlane, float3 vector2InPlane, float3 pointInPlane)
        : this(math.cross(vector1InPlane, vector2InPlane), pointInPlane)
        {
        }

        /// <summary>
        /// Creates a normalized Plane directly without normalization cost.
        /// </summary>
        /// <remarks>
        /// If you have a unit length normal vector, you can create a Plane faster than using one of its constructors
        /// by calling this function.
        /// </remarks>
        /// <param name="unitNormal">A non-zero vector that is perpendicular to the plane.  It must be unit length.</param>
        /// <param name="distance">Distance from the origin along the normal.  A negative value moves the plane in the
        /// same direction as the normal while a positive value moves it in the opposite direction.</param>
        /// <returns>Normalized Plane constructed from given inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Plane CreateFromUnitNormalAndDistance(float3 unitNormal, float distance)
        {
            return new Plane { NormalAndDistance = new float4(unitNormal, distance) };
        }

        /// <summary>
        /// Creates a normalized Plane without normalization cost.
        /// </summary>
        /// <remarks>
        /// If you have a unit length normal vector, you can create a Plane faster than using one of its constructors
        /// by calling this function.
        /// </remarks>
        /// <param name="unitNormal">A non-zero vector that is perpendicular to the plane.  It must be unit length.</param>
        /// <param name="pointInPlane">A point that lies in the plane.</param>
        /// <returns>Normalized Plane constructed from given inputs.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Plane CreateFromUnitNormalAndPointInPlane(float3 unitNormal, float3 pointInPlane)
        {
            return new Plane { NormalAndDistance = new float4(unitNormal, -math.dot(unitNormal, pointInPlane)) };
        }

        /// <summary>
        /// Get/set the normal vector of the plane.
        /// </summary>
        /// <remarks>
        /// It is assumed that the normal is unit length.  If you set a new plane such that Ax + By + Cz + Dw = 0 but
        /// (A, B, C) is not unit length, then you must normalize the plane by calling <see cref="Normalize(Plane)"/>.
        /// </remarks>
        public float3 Normal
        {
            get => NormalAndDistance.xyz;
            set => NormalAndDistance.xyz = value;
        }

        /// <summary>
        /// Get/set the distance of the plane from the origin.  May be a negative value.
        /// </summary>
        /// <remarks>
        /// It is assumed that the normal is unit length.  If you set a new plane such that Ax + By + Cz + Dw = 0 but
        /// (A, B, C) is not unit length, then you must normalize the plane by calling <see cref="Normalize(Plane)"/>.
        /// </remarks>
        public float Distance
        {
            get => NormalAndDistance.w;
            set => NormalAndDistance.w = value;
        }

        /// <summary>
        /// Normalizes the given Plane.
        /// </summary>
        /// <param name="plane">Plane to normalize.</param>
        /// <returns>Normalized Plane.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Plane Normalize(Plane plane)
        {
            return new Plane { NormalAndDistance = Normalize(plane.NormalAndDistance) };
        }

        /// <summary>
        /// Normalizes the plane represented by the given plane coefficients.
        /// </summary>
        /// <remarks>
        /// The plane coefficients are A, B, C, D and stored in that order in the <see cref="float4"/>.
        /// </remarks>
        /// <param name="planeCoefficients">Plane coefficients A, B, C, D stored in x, y, z, w (respectively).</param>
        /// <returns>Normalized plane coefficients.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 Normalize(float4 planeCoefficients)
        {
            float recipLength = math.rsqrt(math.lengthsq(planeCoefficients.xyz));
            return new Plane { NormalAndDistance = planeCoefficients * recipLength };
        }

        /// <summary>
        /// Get the signed distance from the point to the plane.
        /// </summary>
        /// <remarks>
        /// Plane must be normalized prior to calling this function.  Distance is positive if point is on side of the
        /// plane the normal points to, negative if on the opposite side and zero if the point lies in the plane.
        /// Avoid comparing equality with 0.0f when testing if a point lies exactly in the plane and use an approximate
        /// comparison instead.
        /// </remarks>
        /// <param name="point">Point to find the signed distance with.</param>
        /// <returns>Signed distance of the point from the plane.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float SignedDistanceToPoint(float3 point)
        {
            CheckPlaneIsNormalized();
            return math.dot(NormalAndDistance, new float4(point, 1.0f));
        }

        /// <summary>
        /// Projects the given point onto the plane.
        /// </summary>
        /// <remarks>
        /// Plane must be normalized prior to calling this function.  The result is the position closest to the point
        /// that still lies in the plane.
        /// </remarks>
        /// <param name="point">Point to project onto the plane.</param>
        /// <returns>Projected point that's inside the plane.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 Projection(float3 point)
        {
            CheckPlaneIsNormalized();
            return point - Normal * SignedDistanceToPoint(point);
        }

        /// <summary>
        /// Flips the plane so the normal points in the opposite direction.
        /// </summary>
        public Plane Flipped => new Plane { NormalAndDistance = -NormalAndDistance };

        /// <summary>
        /// Implicitly converts a <see cref="Plane"/> to <see cref="float4"/>.
        /// </summary>
        /// <param name="plane">Plane to convert.</param>
        /// <returns>A <see cref="float4"/> representing the plane.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float4(Plane plane) => plane.NormalAndDistance;

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckPlaneIsNormalized()
        {
            float ll = math.lengthsq(Normal.xyz);
            const float lowerBound = 0.999f * 0.999f;
            const float upperBound = 1.001f * 1.001f;

            if (ll < lowerBound || ll > upperBound)
            {
                throw new System.ArgumentException("Plane must be normalized. Call Plane.Normalize() to normalize plane.");
            }
        }
    }
}
