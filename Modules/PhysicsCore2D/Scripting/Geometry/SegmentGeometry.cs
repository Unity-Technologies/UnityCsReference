// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics
{
    /// <summary>
    /// The geometry of a line segment.
    /// See <see cref="PhysicsBody.CreateShape(SegmentGeometry, PhysicsShapeDefinition)"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public partial struct SegmentGeometry
    {
        /// <summary>
        /// Create a default Segment.
        /// See <see cref="SegmentGeometry.defaultGeometry"/>.
        /// </summary>
        public SegmentGeometry()
        {
            m_Point1 = Vector2.right * 0.5f;
            m_Point2 = Vector2.left * 0.5f;
        }

        /// <summary>
        /// Get the default Segment. The line segment is directed towards the left.
        /// </summary>
        public static readonly SegmentGeometry defaultGeometry = new()
        {
            point1 = Vector2.right * 0.5f,
            point2 = Vector2.left * 0.5f
        };

        /// <summary>
        /// Create a Segment.
        /// </summary>
        /// <param name="point1">The first local point.</param>
        /// <param name="point2">The second local point.</param>
        /// <returns>The created geometry.</returns>
        public static SegmentGeometry Create(Vector2 point1, Vector2 point2) => new() { point1 = point1, point2 = point2 };

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
        /// Create a shape proxy from the geometry, transformed by the specified transform.
        /// </summary>
        /// <param name="transform">The transform used to position the geometry.</param>
        /// <exception cref="System.ArgumentException">Thrown if the geometry is not valid.</exception>
        public readonly PhysicsShape.ShapeProxy CreateShapeProxy(PhysicsTransform transform)
        {
            if (isValid)
                return new PhysicsShape.ShapeProxy(Transform(transform));

            throw new ArgumentException("Geometry is not valid.");
        }

        /// <summary>
        /// Create a shape proxy from the geometry, transformed by the specified transform.
        /// </summary>
        /// <param name="transform">The transform used to position the geometry.</param>
        /// <exception cref="System.ArgumentException">Thrown if the geometry is not valid.</exception>
        public readonly PhysicsShape.ShapeProxy CreateShapeProxy(Matrix4x4 transform)
        {
            if (isValid)
                return new PhysicsShape.ShapeProxy(Transform(transform));

            throw new ArgumentException("Geometry is not valid.");
        }

        /// <summary>
        /// Check if the geometry is valid or not.
        /// </summary>
        public readonly bool isValid => SegmentGeometry_IsValid(this);

        /// <summary>
        /// The first point.
        /// </summary>
        public Vector2 point1 { readonly get => m_Point1; set => m_Point1 = value; }

        /// <summary>
        /// The second point.
        /// </summary>
        public Vector2 point2 { readonly get => m_Point2; set => m_Point2 = value; }

        /// <summary>
        /// The mid-point between <see cref="SegmentGeometry.point1"/> and <see cref="SegmentGeometry.point2"/>.
        /// </summary>
        public readonly Vector2 midPoint => (point1 + point2) * 0.5f;

        /// <summary>
        /// Calculate the vector from <see cref="SegmentGeometry.point1"/> to <see cref="SegmentGeometry.point2"/>.
        /// See <see cref="SegmentGeometry.backward"/>.
        /// </summary>
        public readonly Vector2 forward => point2 - point1;

        /// <summary>
        /// Calculate the vector from <see cref="SegmentGeometry.point2"/> to <see cref="SegmentGeometry.point1"/>.
        /// See <see cref="SegmentGeometry.forward"/>.
        /// </summary>
        public readonly Vector2 backward => point1 - point2;

        /// <summary>
        /// Calculate the AABB of the geometry.
        /// </summary>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <returns>The bounds of the geometry.</returns>
        public readonly PhysicsAABB CalculateAABB(PhysicsTransform transform) => SegmentGeometry_CalculateAABB(this, transform);

        /// <summary>
        /// Calculate the closest point on this geometry to the specified point.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>The closest point on the geometry to the specified point.</returns>
        public readonly Vector2 ClosestPoint(Vector2 point) => SegmentGeometry_ClosestPoint(this, point);

        /// <summary>
        /// Calculate if a world ray intersects the geometry.
        /// See <see cref="PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <param name="oneSided">Whether to treat the segment as having one-sided collision. The "left" side collision is ignored.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput, bool oneSided = false) => SegmentGeometry_CastRay(this, castRayInput, oneSided);

        /// <summary>
        /// Calculate if a cast shape intersects the geometry.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// See <see cref="PhysicsQuery.CastShapeInput"/> and <see cref="PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="input">The cast shape input used to check for intersection.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastShape(PhysicsQuery.CastShapeInput input) => SegmentGeometry_CastShape(this, input);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, CircleGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.SegmentAndCircle(this, transform, otherGeometry, otherTransform);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, CapsuleGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.SegmentAndCapsule(this, transform, otherGeometry, otherTransform);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, PolygonGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.SegmentAndPolygon(this, transform, otherGeometry, otherTransform);

        /// <summary>
        /// Transform the geometry.
        /// </summary>
        /// <param name="transform">The geometry to transform with.</param>
        /// <returns>The transformed geometry.</returns>
        public readonly SegmentGeometry Transform(PhysicsTransform transform)
        {
            return new SegmentGeometry
            {
                point1 = transform.TransformPoint(point1),
                point2 = transform.TransformPoint(point2)
            };
        }

        /// <summary>
        /// Inverse-Transform the geometry.
        /// </summary>
        /// <param name="transform">The geometry to transform with.</param>
        /// <returns>The inverse-transformed geometry.</returns>
        public readonly SegmentGeometry InverseTransform(PhysicsTransform transform)
        {
            return new SegmentGeometry
            {
                point1 = transform.InverseTransformPoint(point1),
                point2 = transform.InverseTransformPoint(point2)
            };
        }

        /// <summary>
        /// Transform the geometry.
        /// </summary>
        /// <param name="transform">The transform to be used on the geometry.</param>
        /// <returns>The transformed geometry.</returns>
        public readonly SegmentGeometry Transform(Matrix4x4 transform)
        {
            return new SegmentGeometry
            {
                point1 = transform.MultiplyPoint3x4(point1),
                point2 = transform.MultiplyPoint3x4(point2)
            };
        }

        /// <summary>
        /// Inverse-Transform the geometry.
        /// </summary>
        /// <param name="transform">The transform to be used on the geometry.</param>
        /// <returns>The inverse-transformed geometry.</returns>
        public readonly SegmentGeometry InverseTransform(Matrix4x4 transform)
        {
            transform = transform.inverse;

            return new SegmentGeometry
            {
                point1 = transform.MultiplyPoint3x4(point1),
                point2 = transform.MultiplyPoint3x4(point2)
            };
        }

        /// <summary>
        /// Transform a batch of geometry in place.
        /// </summary>
        /// <param name="geometry">The geometry to transform in place.</param>
        /// <param name="transform">The transform to apply.</param>
        public static void Transform(Span<SegmentGeometry> geometry, PhysicsTransform transform)
        {
            for (var i = 0; i < geometry.Length; ++i)
                geometry[i] = geometry[i].Transform(transform);
        }

        /// <summary>
        /// Inverse-Transform a batch of geometry in place.
        /// </summary>
        /// <param name="geometry">The geometry to inverse-transform in place.</param>
        /// <param name="transform">The transform to apply.</param>
        public static void InverseTransform(Span<SegmentGeometry> geometry, PhysicsTransform transform)
        {
            for (var i = 0; i < geometry.Length; ++i)
                geometry[i] = geometry[i].InverseTransform(transform);
        }

        /// <summary>
        /// Transform a batch of geometry in place.
        /// </summary>
        /// <param name="geometry">The geometry to transform in place.</param>
        /// <param name="transform">The transform to apply.</param>
        public static void Transform(Span<SegmentGeometry> geometry, Matrix4x4 transform)
        {
            for (var i = 0; i < geometry.Length; ++i)
                geometry[i] = geometry[i].Transform(transform);
        }

        /// <summary>
        /// Inverse-Transform a batch of geometry in place.
        /// </summary>
        /// <param name="geometry">The geometry to inverse-transform in place.</param>
        /// <param name="transform">The transform to apply.</param>
        public static void InverseTransform(Span<SegmentGeometry> geometry, Matrix4x4 transform)
        {
            // No radius, so the inverse is the forward transform by the inverted matrix; invert once.
            transform = transform.inverse;
            for (var i = 0; i < geometry.Length; ++i)
                geometry[i] = geometry[i].Transform(transform);
        }

        /// <summary>
        /// Scale the geometry along the <see cref="SegmentGeometry.forward"/> and <see cref="SegmentGeometry.backward"/> direction.
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        public readonly SegmentGeometry Scale(float scale)
        {
            var centroid = midPoint;
            var extension = forward * 0.5f * scale;

            return new SegmentGeometry
            {
                point1 = centroid - extension,
                point2 = centroid + extension
            };
        }

        /// <undoc/>
        public static implicit operator PhysicsShape.ShapeProxy(SegmentGeometry geometry) => geometry.CreateShapeProxy();

        #region Internal

        [SerializeField] Vector2 m_Point1;
        [SerializeField] Vector2 m_Point2;

        #endregion
    }
}
