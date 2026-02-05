// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// The geometry of a closed circle.
    /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(CircleGeometry, PhysicsShapeDefinition)"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct CircleGeometry
    {
        /// <summary>
        /// Create a default Circle.
        /// See <see cref="LowLevelPhysics2D.CircleGeometry.defaultGeometry"/>.
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
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput) => CircleGeometry_CastRay(this, castRayInput);

        /// <summary>
        /// Calculate if a cast shape intersects the geometry.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastShapeInput"/> and <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
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
        /// The maximum absolute value component from the scale will be used to scale the <see cref="LowLevelPhysics2D.CircleGeometry.radius"/>.
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
        /// The maximum (minimum in the inverse) absolute value component from the scale will be used to scale the <see cref="LowLevelPhysics2D.CircleGeometry.radius"/>.
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

        #region Internal
        
        [SerializeField] Vector2 m_Center;
        [SerializeField] [Min(0.0f)] float m_Radius;

        #endregion
    }

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

        #region Internal

        [SerializeField] Vector2 m_Center1;
        [SerializeField] Vector2 m_Center2;
        [SerializeField] [Min(0.0f)] float m_Radius;

        #endregion
    }

    /// <summary>
    /// The geometry of a closed convex polygon.
    /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(PolygonGeometry, PhysicsShapeDefinition)"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PolygonGeometry
    {
        /// <summary>
        /// Create a default Polygon.
        /// See <see cref="LowLevelPhysics2D.PolygonGeometry.defaultGeometry"/>.
        /// </summary>
        public PolygonGeometry()
        {
            vertices = new PhysicsShape.ShapeArray
            {
                vertex0 = new Vector2(-0.5f, -0.5f),
                vertex1 = new Vector2(0.5f, -0.5f),
                vertex2 = new Vector2(0.5f, 0.5f),
                vertex3 = new Vector2(-0.5f, 0.5f)
            };

            normals = new PhysicsShape.ShapeArray
            {
                vertex0 = Vector2.down,
                vertex1 = Vector2.right,
                vertex2 = Vector2.right,
                vertex3 = Vector2.left
            };

            m_Count = 4;
            m_Centroid = Vector2.zero;
            m_Radius = 0f;
        }

        /// <summary>
        /// Get the default Polygon.
        /// </summary>
        public static readonly PolygonGeometry defaultGeometry = CreateBox(Vector2.one);

        /// <summary>
        /// Create a Polygon as a four-sided box.
        /// </summary>
        /// <param name="size">The full size of the box.</param>
        /// <param name="radius">The radius to use.</param>
        /// <param name="inscribe">When true, the specified size will be inclusive of the specified radius. If true, a warning will be produced if the radius is greater-than or equal-to twice the specified size.</param>
        /// <returns>The created geometry.</returns>
        public static PolygonGeometry CreateBox(Vector2 size, float radius = 0.0f, bool inscribe = false) => PolygonGeometry_CreateBox(size, radius, PhysicsTransform.identity, inscribe);

        /// <summary>
        /// Create multiple Polygons from a set of vertices.
        /// The vertices are assumed to produce a closed loop but can describe a concave shape if required.
        /// There must be at least 3 vertices.
        /// A limit is imposed on small vertex distances so it is recommended that scaling is applied here rather than on the returned geometry so geometry is not discarded due to it being invalid.
        /// </summary>
        /// <param name="vertices">The vertices to create the polygons from..</param>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <param name="vertexScale">The scaling to be applied to the vertices.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created polygon geometries. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public static NativeArray<PolygonGeometry> CreatePolygons(ReadOnlySpan<Vector2> vertices, PhysicsTransform transform, Vector2 vertexScale, Allocator allocator = Unity.Collections.Allocator.Temp) => PolygonGeometry_CreatePolygons(vertices, transform, vertexScale, allocator).ToNativeArray<PolygonGeometry>();

        /// <summary>
        /// Create a Polygon as a four-sided box.
        /// </summary>
        /// <param name="size">The full size of the box.</param>
        /// <param name="radius">The radius to use.</param>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <param name="inscribe">When true, the specified size will be inclusive of the specified radius. If true, a warning will be produced if the radius is greater-than or equal-to twice the specified size.</param>
        /// <returns>The created geometry.</returns>
        public static PolygonGeometry CreateBox(Vector2 size, float radius, PhysicsTransform transform, bool inscribe = false) => PolygonGeometry_CreateBox(size, radius, transform, inscribe);

        /// <summary>
        /// Create a Polygon from the specified vertices.
        /// The number of vertices must be in the range 3 to <see cref="LowLevelPhysics2D.PhysicsConstants.MaxPolygonVertices"/>.
        /// </summary>
        /// <param name="vertices">The vertices to use.</param>
        /// <param name="radius">The radius to use.</param>
        /// <returns>The created geometry.</returns>
        public static PolygonGeometry Create(ReadOnlySpan<Vector2> vertices, float radius = 0.0f) => PolygonGeometry_Create_WithPhysicsTransform(vertices, radius, PhysicsTransform.identity);

        /// <summary>
        /// Create a Polygon from the specified vertices.
        /// The number of vertices must be in the range 3 to <see cref="LowLevelPhysics2D.PhysicsConstants.MaxPolygonVertices"/>.
        /// </summary>
        /// <param name="vertices">The vertices to use.</param>
        /// <param name="radius">The radius to use.</param>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <returns>The created geometry.</returns>
        public static PolygonGeometry Create(ReadOnlySpan<Vector2> vertices, float radius, PhysicsTransform transform) => PolygonGeometry_Create_WithPhysicsTransform(vertices, radius, transform);

        /// <summary>
        /// Create a Polygon from the specified vertices.
        /// The number of vertices must be in the range 3 to <see cref="LowLevelPhysics2D.PhysicsConstants.MaxPolygonVertices"/>.
        /// </summary>
        /// <param name="vertices">The vertices to use.</param>
        /// <param name="radius">The radius to use.</param>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <returns>The created geometry.</returns>
        public static PolygonGeometry Create(ReadOnlySpan<Vector2> vertices, float radius, Matrix4x4 transform) => PolygonGeometry_Create_WithMatrix(vertices, radius, transform);

        /// <summary>
        /// Create a Polygon from the specified convex hull.
        /// </summary>
        /// <param name="convexHull">The convex hull to create the polygon from.</param>
        /// <param name="radius">The radius to use.</param>
        /// <returns>The created geometry.</returns>
        public static PolygonGeometry Create(ref ConvexHull convexHull, float radius) => new PolygonGeometry
        {
            vertices = convexHull.vertices,
            count = convexHull.count,
            radius = radius

        }.Validate();

        /// <summary>
        /// Check if the geometry is valid or not.
        /// </summary>
        public readonly bool isValid => PolygonGeometry_IsValid(this);

        /// <summary>
        /// Insert a vertex into the geometry returning a new geometry with updated normals and centroid.
        /// </summary>
        /// <param name="geometry">The geometry to adjust.</param>
        /// <param name="index">The vertex index to insert at.</param>
        /// <param name="vertex">The vertex to insert.</param>
        /// <returns>The new geometry with the inserted vertex.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the vertex count is already at maximum or the index is out of range. <see cref="LowLevelPhysics2D.PhysicsConstants.MaxPolygonVertices"/>.</exception>
        public static PolygonGeometry InsertVertex(PolygonGeometry geometry, int index, Vector2 vertex)
        {
            // Validate.
            if (geometry.count == PhysicsConstants.MaxPolygonVertices || index < 0 && index >= PhysicsConstants.MaxPolygonVertices)
                throw new ArgumentOutOfRangeException(nameof(index), "Invalid index.");

            // Adjust the vertices.
            geometry.count++;
            ref var vertices = ref geometry.vertices;
            for (var i = geometry.count - 1; i > index; --i)
            {
                vertices[i] = vertices[i - 1];
            }
            vertices[index] = vertex;

            // Create the new validated geometry.
            return geometry.Validate();
        }

        /// <summary>
        /// Delete a vertex from the geometry returning a new geometry with updated normals and centroid.
        /// </summary>
        /// <param name="geometry">The geometry to adjust.</param>
        /// <param name="index">The vertex index to delete.</param>
        /// <returns>The new geometry with the deleted vertex.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public static PolygonGeometry DeleteVertex(PolygonGeometry geometry, int index)
        {
            // Validate.
            if (geometry.count == 3 || index < 0 && index >= PhysicsConstants.MaxPolygonVertices)
                throw new ArgumentOutOfRangeException(nameof(index), "Invalid index.");

            // Adjust the vertices.
            --geometry.count;
            ref var vertices = ref geometry.vertices;
            for (var i = index; i < geometry.count; ++i)
            {
                vertices[i] = vertices[i + 1];
            }

            // Create the new validated geometry.
            return geometry.Validate();
        }

        /// <summary>
        /// The geometry vertices stored in a <see cref="LowLevelPhysics2D.PhysicsShape.ShapeArray"/>.
        /// </summary>
        /// <remarks>
        /// This is exposed directly as a field rather than a property as it is extremely unlikely to ever change and causes issues when changing values as a property.
        /// </remarks>
        public PhysicsShape.ShapeArray vertices;

        /// <summary>
        /// The geometry normal stored in a <see cref="LowLevelPhysics2D.PhysicsShape.ShapeArray"/>.
        /// </summary>
        /// <remarks>
        /// This is exposed directly as a field rather than a property as it is extremely unlikely to ever change and causes usability issues as a property.
        /// </remarks>
        public PhysicsShape.ShapeArray normals;

        /// <summary>
        /// The centroid of the polygon.
        /// </summary>
        public Vector2 centroid { readonly get => m_Centroid; set => m_Centroid = value; }

        /// <summary>
        /// The external radius for rounded polygons.
        /// </summary>
        public float radius { readonly get => m_Radius; set => m_Radius = Mathf.Max(0f, value); }

        /// <summary>
        /// The number of polygon vertices.
        /// </summary>
        public int count { readonly get => m_Count; set => m_Count = Mathf.Clamp(value, 3, PhysicsConstants.MaxPolygonVertices); }

        /// <summary>
        /// Get the polygon vertices as a read-only span.
        /// </summary>
        /// <returns>The read-only span representing the vertices in the convex hull.</returns>
        public unsafe ReadOnlySpan<Vector2> AsReadOnlySpan()
        {
            ref Vector2 vertex0 = ref vertices[0];
            fixed (Vector2* pThis = &vertex0)
            {
                return new ReadOnlySpan<Vector2>(pThis, m_Count);
            }
        }

        /// <summary>
        /// Get a validated version of the geometry, if possible.
        /// </summary>
        /// <returns>A validated copy of the geometry with updated normals, centroid etc. Depending on the current geometry, the returned geometry may not be valid. See <see cref="LowLevelPhysics2D.PolygonGeometry.isValid"/>.</returns>
        public readonly PolygonGeometry Validate() => PolygonGeometry_Validate(this);

        /// <summary>
        /// Calculate the mass configuration of the geometry.
        /// </summary>
        /// <param name="density">The density to use.</param>
        /// <returns>The calculated mass configuration.</returns>
        public readonly PhysicsBody.MassConfiguration CalculateMassConfiguration(float density = 1.0f) => PolygonGeometry_CalculateMassConfiguration(this, density);

        /// <summary>
        /// Calculate the AABB of the geometry.
        /// </summary>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <returns>The bounds of the geometry.</returns>
        public readonly PhysicsAABB CalculateAABB(PhysicsTransform transform) => PolygonGeometry_CalculateAABB(this, transform);

        /// <summary>
        /// Calculate if a point overlaps the geometry.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>If the point overlaps the geometry.</returns>
        public readonly bool OverlapPoint(Vector2 point) => PolygonGeometry_OverlapPoint(this, point);

        /// <summary>
        /// Calculate the closest point on this geometry to the specified point.
        /// </summary>
        /// <param name="point">The point to check.</param>
        /// <returns>The closest point on the geometry to the specified point.</returns>
        public readonly Vector2 ClosestPoint(Vector2 point) => PolygonGeometry_ClosestPoint(this, point);

        /// <summary>
        /// Calculate if a world ray intersects the geometry.
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput) => PolygonGeometry_CastRay(this, castRayInput);

        /// <summary>
        /// Calculate if a cast shape intersects the geometry.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastShapeInput"/> and <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="input">The cast shape input used to check for intersection.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastShape(PhysicsQuery.CastShapeInput input) => PolygonGeometry_CastShape(this, input);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, CircleGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.PolygonAndCircle(this, transform, otherGeometry, otherTransform);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, CapsuleGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.PolygonAndCapsule(this, transform, otherGeometry, otherTransform);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, PolygonGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.PolygonAndPolygon(this, transform, otherGeometry, otherTransform);

        /// <summary>
        /// Check the intersection between this geometry and another.
        /// </summary>
        /// <param name="transform">The transform used to specify where this geometry is positioned.</param>
        /// <param name="otherGeometry">The other geometry used to check intersection against.</param>
        /// <param name="otherTransform">The transform used to specify where the other geometry is positioned.</param>
        /// <returns>The contact manifold fully detailing the intersection.</returns>
        public readonly PhysicsShape.ContactManifold Intersect(PhysicsTransform transform, SegmentGeometry otherGeometry, PhysicsTransform otherTransform) => PhysicsQuery.SegmentAndPolygon(otherGeometry, otherTransform, this, transform);

        /// <summary>
        /// Transform the specified geometry.
        /// </summary>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <returns>The transformed geometry.</returns>
        public readonly PolygonGeometry Transform(PhysicsTransform transform) => PolygonGeometry_Transform_WithPhysicsTransform(this, transform);

        /// <summary>
        /// Inverse-Transform the geometry.
        /// </summary>
        /// <param name="transform">The geometry to transform with.</param>
        /// <returns>The inverse-transformed geometry.</returns>
        public readonly PolygonGeometry InverseTransform(PhysicsTransform transform) => PolygonGeometry_InverseTransform_WithPhysicsTransform(this, transform);

        /// <summary>
        /// Transform the specified geometry.
        /// The maximum absolute value component from the scale will be used to scale the <see cref="LowLevelPhysics2D.PolygonGeometry.radius"/>.
        /// </summary>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <param name="scaleRadius">Whether to scale the radius of the shape.</param>
        /// <returns>The transformed geometry.</returns>
        public readonly PolygonGeometry Transform(Matrix4x4 transform, bool scaleRadius) => PolygonGeometry_Transform_WithMatrix(this, transform, scaleRadius);

        /// <summary>
        /// Inverse-Transform the geometry.
        /// The maximum (minimum in the inverse) absolute value component from the scale will be used to scale the <see cref="LowLevelPhysics2D.PolygonGeometry.radius"/>.
        /// </summary>
        /// <param name="transform">The transform to be used on the geometry.</param>
        /// <param name="scaleRadius">Whether to scale the radius of the shape.</param>
        /// <returns>The inverse-transformed geometry.</returns>
        public readonly PolygonGeometry InverseTransform(Matrix4x4 transform, bool scaleRadius) => PolygonGeometry_InverseTransform_WithMatrix(this, transform, scaleRadius);

        /// <summary>
        /// A simple convex hull.
        /// The hull is not validated by physics so cannot be used directly for shapes.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct ConvexHull
        {
            /// <summary>
            /// The geometry vertices stored in a <see cref="LowLevelPhysics2D.PhysicsShape.ShapeArray"/>.
            /// </summary>
            /// <remarks>
            /// This is exposed directly as a field rather than a property as it is extremely unlikely to ever change and causes issues when changing values as a property.
            /// </remarks>
            public PhysicsShape.ShapeArray vertices;

            /// <summary>
            /// The number of polygon vertices.
            /// </summary>
            public int count { readonly get => m_Count; set => m_Count = Mathf.Clamp(value, 3, PhysicsConstants.MaxPolygonVertices); }

            /// <summary>
            /// Get the convex hull vertices as a read-only span.
            /// </summary>
            /// <returns>The read-only span representing the vertices in the convex hull.</returns>
            public unsafe ReadOnlySpan<Vector2> AsReadOnlySpan()
            {
                ref Vector2 vertex0 = ref vertices[0];
                fixed (Vector2* pThis = &vertex0)
                {
                    return new ReadOnlySpan<Vector2>(pThis, m_Count);
                }
            }

            #region Internal

            [SerializeField][Range(3, PhysicsConstants.MaxPolygonVertices)] internal int m_Count;

            #endregion
        }

        #region Internal

        [SerializeField] internal Vector2 m_Centroid;
        [SerializeField] [Min(0.0f)] internal float m_Radius;
        [SerializeField] [Range(3, PhysicsConstants.MaxPolygonVertices)] internal int m_Count;

        #endregion
    }

    /// <summary>
    /// The geometry of a line segment.
    /// See <see cref="LowLevelPhysics2D.PhysicsBody.CreateShape(SegmentGeometry, PhysicsShapeDefinition)"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct SegmentGeometry
    {
        /// <summary>
        /// Create a default Segment.
        /// See <see cref="LowLevelPhysics2D.SegmentGeometry.defaultGeometry"/>.
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
        /// The mid-point between <see cref="LowLevelPhysics2D.SegmentGeometry.point1"/> and <see cref="SegmentGeometry.point2"/>.
        /// </summary>
        public readonly Vector2 midPoint => (point1 + point2) * 0.5f;

        /// <summary>
        /// Calculate the vector from <see cref="LowLevelPhysics2D.SegmentGeometry.point1"/> to <see cref="LowLevelPhysics2D.SegmentGeometry.point2"/>.
        /// See <see cref="LowLevelPhysics2D.SegmentGeometry.backward"/>.
        /// </summary>
        public readonly Vector2 forward => point2 - point1;

        /// <summary>
        /// Calculate the vector from <see cref="LowLevelPhysics2D.SegmentGeometry.point2"/> to <see cref="LowLevelPhysics2D.SegmentGeometry.point1"/>.
        /// See <see cref="LowLevelPhysics2D.SegmentGeometry.forward"/>.
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
        /// <param name="transform">The Transform to use on this geometry.</param>
        /// <param name="point">The point to check.</param>
        /// <returns>The closest point on the geometry to the specified point.</returns>
        public readonly Vector2 ClosestPoint(PhysicsTransform transform, Vector2 point) => SegmentGeometry_ClosestPoint(this, transform.TransformPoint(point));

        /// <summary>
        /// Calculate if a world ray intersects the geometry.
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <param name="oneSided">Whether to treat the segment as having one-sided collision. The "left" side collision is ignored.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput, bool oneSided = false) => SegmentGeometry_CastRay(this, castRayInput, oneSided);

        /// <summary>
        /// Calculate if a cast shape intersects the geometry.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastShapeInput"/> and <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
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
        /// Scale the geometry along the <see cref="LowLevelPhysics2D.SegmentGeometry.forward"/> and <see cref="LowLevelPhysics2D.SegmentGeometry.backward"/> direction.
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        public readonly SegmentGeometry Scale(float scale)
        {
            var centroid = midPoint;
            var extension = forward * 0.5f * scale;

            return new SegmentGeometry
            {
                point1 = midPoint - extension,
                point2 = midPoint + extension
            };
        }

        #region Internal

        [SerializeField] Vector2 m_Point1;
        [SerializeField] Vector2 m_Point2;

        #endregion
    }

    /// <summary>
    /// The geometry of a chain line segment with one-sided collision which only collides on the "right" side.
    /// Several of these are generated for a chain, connected as ghost1 -> point1 -> point2 -> ghost2.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ChainSegmentGeometry
    {
        /// <summary>
        /// Create a default ChainSegment.
        /// See <see cref="ChainSegmentGeometry.defaultGeometry"/>.
        /// </summary>
        public ChainSegmentGeometry()
        {
            // Segment is direction left so contact is from above.
            m_Segment = new SegmentGeometry();
            m_Ghost1 = m_Segment.point1 * 2f;
            m_Ghost2 = m_Segment.point2 * 2f;
            m_ChainId = default;
        }

        /// <summary>
        /// Create a default ChainSegment.
        /// </summary>
        /// <param name="segmentGeometry">The segment geometry.</param>
        /// <param name="ghost1">The 'ghost' vertex preceding <see cref="LowLevelPhysics2D.SegmentGeometry.point1"/>.</param>
        /// <param name="ghost2">The 'ghost' vertex following <see cref="LowLevelPhysics2D.SegmentGeometry.point2"/>.</param>
        public ChainSegmentGeometry(SegmentGeometry segmentGeometry, Vector2 ghost1, Vector2 ghost2)
        {
            m_Segment = segmentGeometry;
            m_Ghost1 = ghost1;
            m_Ghost2 = ghost2;
            m_ChainId = default;
        }

        /// <summary>
        /// Get the default Chain Segment.
        /// </summary>
        public static readonly ChainSegmentGeometry defaultGeometry = new()
        {
            // Segment is direction left so contact is from above.
            segment = SegmentGeometry.defaultGeometry,
            ghost1 = SegmentGeometry.defaultGeometry.point1 * 2f,
            ghost2 = SegmentGeometry.defaultGeometry.point2 * 2f
        };

        /// <summary>
        /// Check if the geometry is valid or not.
        /// </summary>
        public readonly bool isValid => ChainSegmentGeometry_IsValid(this);

        /// <summary>
        /// The tail ghost vertex
        /// </summary>
        public Vector2 ghost1 { readonly get => m_Ghost1; set => m_Ghost1 = value; }

        /// <summary>
        /// The Segment.
        /// </summary>
        public SegmentGeometry segment { readonly get => m_Segment; set => m_Segment = value; }

        /// <summary>
        /// The head ghost vertex
        /// </summary>
        public Vector2 ghost2 { readonly get => m_Ghost2; set => m_Ghost2 = value; }

        /// <summary>
        /// Calculate the AABB of the geometry.
        /// </summary>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <returns>The bounds of the geometry.</returns>
        public readonly PhysicsAABB CalculateAABB(PhysicsTransform transform) => ChainSegmentGeometry_CalculateAABB(this, transform);

        /// <summary>
        /// Calculate the closest point on this geometry to the specified point.
        /// </summary>
        /// <param name="transform">The Transform to use on this geometry.</param>
        /// <param name="point">The point to check.</param>
        /// <returns>The closest point on the geometry to the specified point.</returns>
        public readonly Vector2 ClosestPoint(PhysicsTransform transform, Vector2 point) => ChainSegmentGeometry_ClosestPoint(this, transform.TransformPoint(point));

        /// <summary>
        /// Calculate if a world ray intersects the geometry.
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <param name="oneSided">Whether to treat the segment as having one-sided collision. The "left" side collision is ignored.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput, bool oneSided) => ChainSegmentGeometry_CastRay(this, castRayInput, oneSided);

        /// <summary>
        /// Calculate if a cast shape intersects the geometry.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastShapeInput"/> and <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="input">The cast shape input used to check for intersection.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastShape(PhysicsQuery.CastShapeInput input) => ChainSegmentGeometry_CastShape(this, input);

        /// <summary>
        /// Transform the geometry.
        /// </summary>
        /// <param name="transform">The geometry to transform with.</param>
        /// <returns>The transformed geometry.</returns>
        public readonly ChainSegmentGeometry Transform(PhysicsTransform transform)
        {
            return new ChainSegmentGeometry
            {
                ghost1 = transform.TransformPoint(ghost1),
                segment = segment.Transform(transform),
                ghost2 = transform.TransformPoint(ghost2),
            };
        }

        /// <summary>
        /// Inverse-Transform the geometry.
        /// </summary>
        /// <param name="transform">The geometry to transform with.</param>
        /// <returns>The inverse-transformed geometry.</returns>
        public readonly ChainSegmentGeometry InverseTransform(PhysicsTransform transform)
        {
            return new ChainSegmentGeometry
            {
                ghost1 = transform.InverseTransformPoint(ghost1),
                segment = segment.InverseTransform(transform),
                ghost2 = transform.TransformPoint(ghost2),
            };
        }

        /// <summary>
        /// Transform the geometry.
        /// </summary>
        /// <param name="transform">The transform to be used on the geometry.</param>
        /// <returns>The transformed geometry.</returns>
        public readonly ChainSegmentGeometry Transform(Matrix4x4 transform)
        {
            return new ChainSegmentGeometry
            {
                ghost1 = transform.MultiplyPoint3x4(ghost1),
                segment = segment.Transform(transform),
                ghost2 = transform.MultiplyPoint3x4(ghost2)
            };
        }

        /// <summary>
        /// Inverse-Transform the geometry.
        /// </summary>
        /// <param name="transform">The transform to be used on the geometry.</param>
        /// <returns>The inverse-transformed geometry.</returns>
        public readonly ChainSegmentGeometry InverseTransform(Matrix4x4 transform)
        {
            transform = transform.inverse;

            return new ChainSegmentGeometry
            {
                ghost1 = transform.MultiplyPoint3x4(ghost1),
                segment = segment.Transform(transform),
                ghost2 = transform.MultiplyPoint3x4(ghost2)
            };
        }

        #region Internal

        [SerializeField] Vector2 m_Ghost1;
        [SerializeField] SegmentGeometry m_Segment;
        [SerializeField] Vector2 m_Ghost2;
        readonly int m_ChainId;

        #endregion
    };

    /// <summary>
    /// The geometry of a chain of ChainSegment.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ChainGeometry
    {
        /// <summary>
        /// Create the geometry of a Chain using the specified vertices.
        /// </summary>
        /// <param name="vertices">The vertices that will create the ChainSegment shapes.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the number of vertices is less than 4.</exception>
        public unsafe ChainGeometry(NativeArray<Vector2> vertices)
        {
            // Validate.
            if (vertices.Length < 4)
                throw new ArgumentOutOfRangeException(nameof(vertices), "Chain Geometry must contain a minimum of 4 vertices.");

            // Assign the buffer data.
            m_Points = new IntPtr(vertices.GetUnsafeReadOnlyPtr());
            m_Count = vertices.Length;
        }

        /// <summary>
        /// Create the geometry of a chain using the specified vertices.
        /// </summary>
        /// <param name="vertices">The vertices that will create the ChainSegment shapes.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the number of vertices is less than 4.</exception>
        public unsafe ChainGeometry(ReadOnlySpan<Vector2> vertices)
        {
            // Validate.
            if (vertices.Length < 4)
                throw new ArgumentOutOfRangeException(nameof(vertices), "Chain Geometry must contain a minimum of 4 vertices.");

            fixed (Vector2* addr = vertices)
            {
                // Assign the buffer data.
                m_Points = new IntPtr(addr);
                m_Count = vertices.Length;
            }
        }

        /// <summary>
        /// Check if the geometry is valid or not.
        /// </summary>
        public readonly bool isValid => ChainGeometry_IsValid(this);

        /// <summary>
        /// Get the geometry vertices.
        /// </summary>
        public readonly unsafe ReadOnlySpan<Vector2> vertices => new(m_Points.ToPointer(), m_Count);

        /// <summary>
        /// Calculate the AABB of the geometry.
        /// </summary>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <returns>The bounds of the geometry.</returns>
        public readonly PhysicsAABB CalculateAABB(PhysicsTransform transform) => ChainGeometry_CalculateAABB(this, transform);

        /// <summary>
        /// Calculate the closest point on this geometry to the specified point.
        /// </summary>
        /// <param name="transform">The Transform to use on this geometry.</param>
        /// <param name="point">The point to check.</param>
        /// <returns>The closest point on the geometry to the specified point.</returns>
        public readonly Vector2 ClosestPoint(PhysicsTransform transform, Vector2 point) => ChainGeometry_ClosestPoint(this, transform.TransformPoint(point));

        /// <summary>
        /// Calculate if a world ray intersects the geometry.
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <param name="oneSided">Whether to treat the segment as having one-sided collision. The "left" side collision is ignored.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput, bool oneSided = true) => ChainGeometry_CastRay(this, castRayInput, oneSided);

        /// <summary>
        /// Calculate if a cast shape intersects the geometry.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// See <see cref="LowLevelPhysics2D.PhysicsQuery.CastShapeInput"/> and <see cref="LowLevelPhysics2D.PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="input">The cast shape input used to check for intersection.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastShape(PhysicsQuery.CastShapeInput input) => ChainGeometry_CastShape(this, input);

        #region Internal

        IntPtr m_Points;
        int m_Count;

        #endregion
    }
}
