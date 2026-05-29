// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// The geometry of a closed circle.
    /// See <see cref="PhysicsBody.CreateShape(CircleGeometry, PhysicsShapeDefinition)"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public struct CircleGeometry
    {
        /// <summary>
        /// Create a default Circle.
        /// See <see cref="CircleGeometry.defaultGeometry"/>.
        /// </summary>
        public CircleGeometry()
        {
            m_Center = Vector2.zero;
            m_Radius = 0.5f;
        }

        /// <summary>
        /// Get the default Circle.
        /// </summary>
        public static readonly CircleGeometry defaultGeometry = new()
        {
            center = Vector2.zero,
            radius = 0.5f
        };

        /// <summary>
        /// Create Circle.
        /// </summary>
        /// <param name="radius">The radius to use.</param>
        /// <returns>The created geometry.</returns>
        public static CircleGeometry Create(float radius) => new() { center = Vector2.zero, radius = radius };

        /// <summary>
        /// Create a Circle.
        /// </summary>
        /// <param name="radius">The radius to use.</param>
        /// <param name="center">The local center of the circle.</param>
        /// <returns>The created geometry.</returns>
        public static CircleGeometry Create(float radius, Vector2 center) => new() { center = center, radius = radius };

        /// <summary>
        /// Create a shape proxy from the geometry.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown if the geometry is not valid.</exception>
        public readonly PhysicsShape.ShapeProxy CreateShapeProxy()
        {
            if (isValid)
                return new PhysicsShape.ShapeProxy(this);

            throw new ArgumentException("Geometry is not valid.");
        }

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
        public readonly bool isValid => CircleGeometry_IsValid(this);

        /// <summary>
        /// The local center.
        /// </summary>
        public Vector2 center { readonly get => m_Center; set => m_Center = value; }

        /// <summary>
        /// The radius.
        /// </summary>
        public float radius { readonly get => m_Radius; set => m_Radius = Mathf.Max(0f, value); }

        /// <summary>
        /// Calculate the mass configuration of the geometry.
        /// </summary>
        /// <param name="density">The density to use.</param>
        /// <returns>The calculated mass configuration.</returns>
        public readonly PhysicsBody.MassConfiguration CalculateMassConfiguration(float density = 1.0f) => CircleGeometry_CalculateMassConfiguration(this, density);

        /// <summary>
        /// Calculate the AABB of the geometry.
        /// </summary>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <returns>The bounds of the geometry.</returns>
        public readonly PhysicsAABB CalculateAABB(PhysicsTransform transform) => CircleGeometry_CalculateAABB(this, transform);

        /// <summary>
        /// Calculate if a point overlaps the geometry.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>If the point overlaps the geometry.</returns>
        public readonly bool OverlapPoint(Vector2 point) => CircleGeometry_OverlapPoint(this, point);

        /// <summary>
        /// Calculate the closest point on this geometry to the specified point.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>The closest point on the geometry to the specified point.</returns>
        public readonly Vector2 ClosestPoint(Vector2 point) => CircleGeometry_ClosestPoint(this, point);

        /// <summary>
        /// Calculate if a world ray intersects the geometry.
        /// See <see cref="PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput) => CircleGeometry_CastRay(this, castRayInput);

        /// <summary>
        /// Calculate if a cast shape intersects the geometry.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// See <see cref="PhysicsQuery.CastShapeInput"/> and <see cref="PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="input">The cast shape input used to check for intersection.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastShape(PhysicsQuery.CastShapeInput input) => CircleGeometry_CastShape(this, input);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, CircleGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.CircleAndCircle(this, transform, otherGeometry, otherTransform);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, CapsuleGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.CapsuleAndCircle(otherGeometry, otherTransform, this, transform);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, PolygonGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.PolygonAndCircle(otherGeometry, otherTransform, this, transform);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, SegmentGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.SegmentAndCircle(otherGeometry, otherTransform, this, transform);

        /// <summary>
        /// Transform the geometry.
        /// </summary>
        /// <param name="transform">The geometry to transform with.</param>
        /// <returns>The transformed geometry.</returns>
        public readonly CircleGeometry Transform(PhysicsTransform transform)
        {
            return new CircleGeometry
            {
                center = transform.TransformPoint(center),
                radius = radius
            };
        }

        /// <summary>
        /// Inverse-Transform the geometry.
        /// </summary>
        /// <param name="transform">The geometry to transform with.</param>
        /// <returns>The inverse-transformed geometry.</returns>
        public readonly CircleGeometry InverseTransform(PhysicsTransform transform)
        {
            return new CircleGeometry
            {
                center = transform.InverseTransformPoint(center),
                radius = radius
            };
        }

        /// <summary>
        /// Transform the geometry.
        /// The maximum absolute value component from the scale will be used to scale the <see cref="CircleGeometry.radius"/>.
        /// </summary>
        /// <param name="transform">The transform to be used on the geometry.</param>
        /// <param name="scaleRadius">Whether to scale the radius of the shape.</param>
        /// <returns>The transformed geometry.</returns>
        public readonly CircleGeometry Transform(Matrix4x4 transform, bool scaleRadius)
        {
            return new CircleGeometry
            {
                center = transform.MultiplyPoint3x4(center),
                radius = scaleRadius ? PhysicsMath.MaxAbsComponent((Vector2)transform.lossyScale) * radius : radius
            };
        }

        /// <summary>
        /// Inverse-Transform the geometry.
        /// The maximum (minimum in the inverse) absolute value component from the scale will be used to scale the <see cref="CircleGeometry.radius"/>.
        /// </summary>
        /// <param name="transform">The transform to be used on the geometry.</param>
        /// <param name="scaleRadius">Whether to scale the radius of the shape.</param>
        /// <returns>The inverse-transformed geometry.</returns>
        public readonly CircleGeometry InverseTransform(Matrix4x4 transform, bool scaleRadius)
        {
            transform = transform.inverse;

            return new CircleGeometry
            {
                center = transform.MultiplyPoint3x4(center),
                radius = scaleRadius ? PhysicsMath.MinAbsComponent((Vector2)transform.lossyScale) * radius : radius
            };
        }

        /// <undoc/>
        public static implicit operator PhysicsShape.ShapeProxy(CircleGeometry geometry) => geometry.CreateShapeProxy();

        #region Internal

        [SerializeField] Vector2 m_Center;
        [SerializeField] [Min(0.0f)] float m_Radius;

        #endregion
    }
}
