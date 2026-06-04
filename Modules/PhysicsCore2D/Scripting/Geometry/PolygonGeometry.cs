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
    /// The geometry of a closed convex polygon.
    ///
    /// The geometry has a fixed maximum number of vertices as defined by the constant <see cref="PhysicsConstants.MaxPolygonVertices"/>.
    /// Polygon regions that require a larger quantity of vertices or are concave are defined by multiple polygon geometry using the <see cref="PhysicsComposer"/> or the <see cref="PolygonGeometry.CreatePolygons(ReadOnlySpan{Vector2}, PhysicsTransform, Vector2, Allocator)"/> utility.
    /// 
    /// See <see cref="PhysicsBody.CreateShape(PolygonGeometry, PhysicsShapeDefinition)"/>.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public struct PolygonGeometry
    {
        /// <summary>
        /// Create a default Polygon.
        /// See <see cref="PolygonGeometry.defaultGeometry"/>.
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
                vertex2 = Vector2.up,
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
        /// Create multiple <see cref="PolygonGeometry"/> from a set of vertices.
        /// The vertices are assumed to produce a closed loop but can describe a concave shape if required.
        /// There must be at least 3 vertices.
        /// A limit is imposed on small vertex distances so be aware that this overload uses a vertex scale of <see cref="Vector2.one"/> so consider using the overload which allows you to increase this if required.
        /// </summary>
        /// <param name="vertices">The vertices to create the polygons from.</param>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created polygon geometries. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public static NativeArray<PolygonGeometry> CreatePolygons(ReadOnlySpan<Vector2> vertices, PhysicsTransform transform, Allocator allocator = Unity.Collections.Allocator.Temp) => CreatePolygons(vertices, transform, vertexScale: Vector2.one, radius: 0.0f, useDelaunay: true, allocator);

        /// <summary>
        /// Create multiple <see cref="PolygonGeometry"/> from a set of vertices.
        /// The vertices are assumed to produce a closed loop but can describe a concave shape if required.
        /// There must be at least 3 vertices.
        /// A limit is imposed on small vertex distances so it is recommended that scaling is applied here rather than on the returned geometry so geometry is not discarded due to it being invalid.
        /// </summary>
        /// <param name="vertices">The vertices to create the polygons from.</param>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <param name="vertexScale">The scaling to be applied to the vertices.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created polygon geometries. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public static NativeArray<PolygonGeometry> CreatePolygons(ReadOnlySpan<Vector2> vertices, PhysicsTransform transform, Vector2 vertexScale, Allocator allocator = Unity.Collections.Allocator.Temp) => CreatePolygons(vertices, transform, vertexScale, radius: 0.0f, useDelaunay: true, allocator);

        /// <summary>
        /// Create multiple <see cref="PolygonGeometry"/> from a set of vertices.
        /// The vertices are assumed to produce a closed loop but can describe a concave shape if required.
        /// There must be at least 3 vertices.
        /// A limit is imposed on small vertex distances so it is recommended that scaling is applied here rather than on the returned geometry so geometry is not discarded due to it being invalid.
        /// </summary>
        /// <param name="vertices">The vertices to create the polygons from.</param>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <param name="vertexScale">The scaling to be applied to the vertices.</param>
        /// <param name="radius">The radius to apply to all generated polygons. Note that this will likely mean that the same polygon region defined by the vertices will not match.</param>
        /// <param name="useDelaunay">Whether Delaunay tessellation will be used.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created polygon geometries. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public static NativeArray<PolygonGeometry> CreatePolygons(ReadOnlySpan<Vector2> vertices, PhysicsTransform transform, Vector2 vertexScale, float radius, bool useDelaunay = true, Allocator allocator = Unity.Collections.Allocator.Temp) => PolygonGeometry_CreatePolygons(vertices, transform, vertexScale, radius, useDelaunay, allocator).ToNativeArray<PolygonGeometry>();

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
        /// The number of vertices must be in the range 3 to <see cref="PhysicsConstants.MaxPolygonVertices"/>.
        /// </summary>
        /// <param name="vertices">The vertices to use.</param>
        /// <param name="radius">The radius to use.</param>
        /// <returns>The created geometry.</returns>
        public static PolygonGeometry Create(ReadOnlySpan<Vector2> vertices, float radius = 0.0f) => PolygonGeometry_Create_WithPhysicsTransform(vertices, radius, PhysicsTransform.identity);

        /// <summary>
        /// Create a Polygon from the specified vertices.
        /// The number of vertices must be in the range 3 to <see cref="PhysicsConstants.MaxPolygonVertices"/>.
        /// </summary>
        /// <param name="vertices">The vertices to use.</param>
        /// <param name="radius">The radius to use.</param>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <returns>The created geometry.</returns>
        public static PolygonGeometry Create(ReadOnlySpan<Vector2> vertices, float radius, PhysicsTransform transform) => PolygonGeometry_Create_WithPhysicsTransform(vertices, radius, transform);

        /// <summary>
        /// Create a Polygon from the specified vertices.
        /// The number of vertices must be in the range 3 to <see cref="PhysicsConstants.MaxPolygonVertices"/>.
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
        /// The maximum absolute value component from the scale will be used to scale the radius when <paramref name="scaleRadius"/> is true.
        /// </summary>
        /// <param name="transform">The transform used to position the geometry.</param>
        /// <param name="scaleRadius">Whether to scale the radius of the shape.</param>
        /// <exception cref="System.ArgumentException">Thrown if the geometry is not valid.</exception>
        public readonly PhysicsShape.ShapeProxy CreateShapeProxy(Matrix4x4 transform, bool scaleRadius)
        {
            if (isValid)
                return new PhysicsShape.ShapeProxy(Transform(transform, scaleRadius));

            throw new ArgumentException("Geometry is not valid.");
        }

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
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the vertex count is already at maximum or the index is out of range. <see cref="PhysicsConstants.MaxPolygonVertices"/>.</exception>
        public static PolygonGeometry InsertVertex(PolygonGeometry geometry, int index, Vector2 vertex)
        {
            // Validate.
            if (geometry.count == PhysicsConstants.MaxPolygonVertices || index < 0 || index >= geometry.count)
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
            if (geometry.count == 3 || index < 0 || index >= geometry.count)
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
        /// The geometry vertices stored in a <see cref="PhysicsShape.ShapeArray"/>.
        /// </summary>
        /// <remarks>
        /// This is exposed directly as a field rather than a property as it is extremely unlikely to ever change and causes issues when changing values as a property.
        /// </remarks>
        public PhysicsShape.ShapeArray vertices;

        /// <summary>
        /// The geometry normal stored in a <see cref="PhysicsShape.ShapeArray"/>.
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
        /// Get the polygon vertices as a span.
        /// </summary>
        /// <returns>The span representing the vertices in the geometry.</returns>
        public unsafe Span<Vector2> AsSpan() => vertices.AsSpan(m_Count);

        /// <summary>
        /// Get the polygon vertices as a read-only span.
        /// </summary>
        /// <returns>The read-only span representing the vertices in the geometry.</returns>
        public unsafe ReadOnlySpan<Vector2> AsReadOnlySpan() => AsSpan();

        /// <summary>
        /// Get a validated version of the geometry, if possible.
        /// </summary>
        /// <returns>A validated copy of the geometry with updated normals, centroid etc. Depending on the current geometry, the returned geometry may not be valid. See <see cref="PolygonGeometry.isValid"/>.</returns>
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
        /// See <see cref="PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput) => PolygonGeometry_CastRay(this, castRayInput);

        /// <summary>
        /// Calculate if a cast shape intersects the geometry.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// See <see cref="PhysicsQuery.CastShapeInput"/> and <see cref="PhysicsQuery.CastResult"/>.
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
        /// The maximum absolute value component from the scale will be used to scale the <see cref="PolygonGeometry.radius"/>.
        /// </summary>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <param name="scaleRadius">Whether to scale the radius of the shape.</param>
        /// <returns>The transformed geometry.</returns>
        public readonly PolygonGeometry Transform(Matrix4x4 transform, bool scaleRadius) => PolygonGeometry_Transform_WithMatrix(this, transform, scaleRadius);

        /// <summary>
        /// Inverse-Transform the geometry.
        /// The maximum (minimum in the inverse) absolute value component from the scale will be used to scale the <see cref="PolygonGeometry.radius"/>.
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
            /// The geometry vertices stored in a <see cref="PhysicsShape.ShapeArray"/>.
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
            /// Get the convex-hull vertices as a span.
            /// </summary>
            /// <returns>The span representing the vertices in the geometry.</returns>
            public Span<Vector2> AsSpan() => vertices.AsSpan(m_Count);

            /// <summary>
            /// Get the convex-hull vertices as a read-only span.
            /// </summary>
            /// <returns>The read-only span representing the vertices in the geometry.</returns>
            public ReadOnlySpan<Vector2> AsReadOnlySpan() => AsSpan();

            #region Internal

            [SerializeField][Range(3, PhysicsConstants.MaxPolygonVertices)] internal int m_Count;

            #endregion
        }

        /// <undoc/>
        public static implicit operator PhysicsShape.ShapeProxy(PolygonGeometry geometry) => geometry.CreateShapeProxy();

        #region Internal

        [SerializeField] internal Vector2 m_Centroid;
        [SerializeField] [Min(0.0f)] internal float m_Radius;
        [SerializeField] [Range(3, PhysicsConstants.MaxPolygonVertices)] internal int m_Count;

        #endregion
    }
}
