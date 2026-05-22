// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;

using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// The geometry of a closed capsule which can be viewed as two semi-circles connected by a rectangle.
    /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(CapsuleGeometry, PhysicsShapeDefinition)"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct CapsuleGeometry
    {
        /// <summary>
        /// Create a default Capsule.
        /// See <see cref="LowLevelPhysics2D.CapsuleGeometry.defaultGeometry"/>.
        /// </summary>
        public CapsuleGeometry()
        {
            m_Center1 = Vector2.up * 0.5f;
            m_Center2 = Vector2.down * 0.5f;
            m_Radius = 0.5f;
        }

        /// <summary>
        /// Get the default Capsule.
        /// </summary>
        public static readonly CapsuleGeometry defaultGeometry = new()
        {
            center1 = Vector2.up * 0.5f,
            center2 = Vector2.down * 0.5f,
            radius = 0.5f
        };

        /// <summary>
        /// Create a Capsule.
        /// </summary>
        /// <param name="center1">The first local center of the capsule end.</param>
        /// <param name="center2">The second local center of the capsule end.</param>
        /// <param name="radius">The radius of the capsule.</param>
        /// <returns>The created geometry.</returns>
        public static CapsuleGeometry Create(Vector2 center1, Vector2 center2, float radius) => new() { center1 = center1, center2 = center2, radius = radius };

        /// <summary>
        /// Creates multiple <see cref="PolygonGeometry"/> from the geometry.
        /// A limit is imposed on small vertex distances so it is recommended that the geometry is scaled appropriately rather than on the returned geometry so geometry is not discarded due to it being invalid.
        /// </summary>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <param name="curveStride">The curve stride used when creating curves, in radians. Valid range is [<see cref="PhysicsComposer.MinCurveStride"/>, 1.0].</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created polygon geometries. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PolygonGeometry> ToPolygons(PhysicsTransform transform, float curveStride = PhysicsComposer.DefaultCurveStride, Allocator allocator = Allocator.Temp) => PhysicsComposer.ToPolygons(this, transform, curveStride, allocator);

        /// <summary>
        /// Check if the geometry is valid or not.
        /// </summary>
        public readonly bool isValid => CapsuleGeometry_IsValid(this);

        /// <summary>
        /// Local center of the first semi-circle.
        /// </summary>
        public Vector2 center1 { readonly get => m_Center1; set => m_Center1 = value; }

        /// <summary>
        /// Local center of the second semi-circle.
        /// </summary>
        public Vector2 center2 { readonly get => m_Center2; set => m_Center2 = value; }

        /// <summary>
        /// The radius of the semi-circles.
        /// </summary>        
        public float radius { readonly get => m_Radius; set => m_Radius = Mathf.Max(0f, value); }

        /// <summary>
        /// Calculate the mass configuration of the geometry.
        /// </summary>
        /// <param name="density">The density to use.</param>
        /// <returns>The calculated mass configuration.</returns>
        public readonly PhysicsBody.MassConfiguration CalculateMassConfiguration(float density = 1.0f) => CapsuleGeometry_CalculateMassConfiguration(this, density);

        /// <summary>
        /// Calculate the AABB of the geometry.
        /// </summary>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <returns>The bounds of the geometry.</returns>
        public readonly PhysicsAABB CalculateAABB(PhysicsTransform transform) => CapsuleGeometry_CalculateAABB(this, transform);

        /// <summary>
        /// Calculate if a point overlaps the geometry.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>If the point overlaps the geometry.</returns>
        public readonly bool OverlapPoint(Vector2 point) => CapsuleGeometry_OverlapPoint(this, point);

        /// <summary>
        /// Calculate the closest point on this geometry to the specified point.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>The closest point on the geometry to the specified point.</returns>
        public readonly Vector2 ClosestPoint(Vector2 point) => CapsuleGeometry_ClosestPoint(this, point);

        /// <summary>
        /// Calculate if a world ray intersects the geometry.
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput) => CapsuleGeometry_CastRay(this, castRayInput);

        /// <summary>
        /// Calculate if a cast shape intersects the geometry.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastShapeInput"/> and <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="input">The cast shape input used to check for intersection.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastShape(PhysicsQuery.CastShapeInput input) => CapsuleGeometry_CastShape(this, input);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, CircleGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.CapsuleAndCircle(this, transform, otherGeometry, otherTransform);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, CapsuleGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.CapsuleAndCapsule(otherGeometry, otherTransform, this, transform);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, PolygonGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.PolygonAndCapsule(otherGeometry, otherTransform, this, transform);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, SegmentGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.SegmentAndCapsule(otherGeometry, otherTransform, this, transform);

        /// <summary>
        /// Transform the geometry.
        /// </summary>
        /// <param name="transform">The geometry to transform with.</param>
        /// <returns>The transformed geometry.</returns>
        public readonly CapsuleGeometry Transform(PhysicsTransform transform)
        {
            return new CapsuleGeometry
            {
                center1 = transform.TransformPoint(center1),
                center2 = transform.TransformPoint(center2),
                radius = radius
            };
        }

        /// <summary>
        /// Inverse-Transform the geometry.
        /// </summary>
        /// <param name="transform">The geometry to transform with.</param>
        /// <returns>The inverse-transformed geometry.</returns>
        public readonly CapsuleGeometry InverseTransform(PhysicsTransform transform)
        {
            return new CapsuleGeometry
            {
                center1 = transform.InverseTransformPoint(center1),
                center2 = transform.InverseTransformPoint(center2),
                radius = radius
            };
        }

        /// <summary>
        /// Transform the geometry.
        /// The maximum absolute value component from the scale will be used to scale the <see cref="LowLevelPhysics2D.CapsuleGeometry.radius"/>.
        /// </summary>
        /// <param name="transform">The transform to be used on the geometry.</param>
        /// <param name="scaleRadius">Whether to scale the radius of the shape.</param>
        /// <returns>The transformed geometry.</returns>
        public readonly CapsuleGeometry Transform(Matrix4x4 transform, bool scaleRadius)
        {
            return new CapsuleGeometry
            {
                center1 = transform.MultiplyPoint3x4(center1),
                center2 = transform.MultiplyPoint3x4(center2),
                radius = scaleRadius ? PhysicsMath.MaxAbsComponent((Vector2)transform.lossyScale) * radius : radius
            };
        }

        /// <summary>
        /// Inverse-Transform the geometry.
        /// The maximum (minimum in the inverse) absolute value component from the scale will be used to scale the <see cref="LowLevelPhysics2D.CapsuleGeometry.radius"/>.
        /// </summary>
        /// <param name="transform">The transform to be used on the geometry.</param>
        /// <param name="scaleRadius">Whether to scale the radius of the shape.</param>
        /// <returns>The inverse-transformed geometry.</returns>
        public readonly CapsuleGeometry InverseTransform(Matrix4x4 transform, bool scaleRadius)
        {
            transform = transform.inverse;

            return new CapsuleGeometry
            {
                center1 = transform.MultiplyPoint3x4(center1),
                center2 = transform.MultiplyPoint3x4(center2),
                radius = scaleRadius ? PhysicsMath.MinAbsComponent((Vector2)transform.lossyScale) * radius : radius
            };
        }

        /// <summary>
        /// Get a validated version of the geometry, if possible.
        /// </summary>
        /// <returns>A validated copy of the geometry with an updated length and radius if required. See <see cref="LowLevelPhysics2D.CapsuleGeometry.isValid"/>.</returns>
        public readonly CapsuleGeometry Validate() => CapsuleGeometry_Validate(this);

        #region Internal

        [SerializeField] Vector2 m_Center1;
        [SerializeField] Vector2 m_Center2;
        [SerializeField] [Min(0.0f)] float m_Radius;

        #endregion
    }
}
