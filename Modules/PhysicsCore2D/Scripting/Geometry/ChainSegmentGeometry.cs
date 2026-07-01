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
    /// The geometry of a chain line segment with one-sided collision which only collides on the "right" side.
    /// Several of these are generated for a chain, connected as ghost1 -> point1 -> point2 -> ghost2.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [MovedFrom(autoUpdateAPI: ScriptUpdateConstants.AutoUpdateAPI, sourceNamespace: ScriptUpdateConstants.SourceNamespace, sourceAssembly: ScriptUpdateConstants.SourceAssembly)]
    public partial struct ChainSegmentGeometry
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
            m_ChainId = -1;
        }

        /// <summary>
        /// Create a default ChainSegment.
        /// </summary>
        /// <param name="segmentGeometry">The segment geometry.</param>
        /// <param name="ghost1">The 'ghost' vertex preceding <see cref="SegmentGeometry.point1"/>.</param>
        /// <param name="ghost2">The 'ghost' vertex following <see cref="SegmentGeometry.point2"/>.</param>
        public ChainSegmentGeometry(SegmentGeometry segmentGeometry, Vector2 ghost1, Vector2 ghost2)
        {
            m_Segment = segmentGeometry;
            m_Ghost1 = ghost1;
            m_Ghost2 = ghost2;
            m_ChainId = -1;
        }

        /// <summary>
        /// Create multiple <see cref="ChainSegmentGeometry"/> from a set of vertices.
        /// The rules for interpreting the specified vertices when creating segments is described in <see cref="PhysicsChain"/>.
        /// </summary>
        /// <param name="vertices">The vertices to create the ChainSegmentGeometry from.</param>
        /// <param name="transform">The transform used to specify where the geometry is positioned.</param>
        /// <param name="isLoop">Indicates a closed chain formed by connecting the first and last vertices specified. This changes how the vertices are interpreted.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The created ChainSegment geometry. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public static NativeArray<ChainSegmentGeometry> CreateSegments(ReadOnlySpan<Vector2> vertices, PhysicsTransform transform, bool isLoop, Unity.Collections.Allocator allocator = Unity.Collections.Allocator.Temp) => ChainSegmentGeometry_CreateSegments(vertices, transform, isLoop, allocator).ToNativeArray<ChainSegmentGeometry>();

        /// <summary>
        /// Get the default Chain Segment.
        /// </summary>
        public static readonly ChainSegmentGeometry defaultGeometry = new()
        {
            // Segment is direction left so contact is from above.
            segment = SegmentGeometry.defaultGeometry,
            ghost1 = SegmentGeometry.defaultGeometry.point1 * 2f,
            ghost2 = SegmentGeometry.defaultGeometry.point2 * 2f,
            m_ChainId = -1
        };

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
        public readonly bool isValid => ChainSegmentGeometry_IsValid(this);

        /// <summary>
        /// The tail ghost vertex.
        /// A ghost vertex is used by the solver to define how a collision response should be handled when a contact with the vertex occurs.
        /// </summary>
        public Vector2 ghost1 { readonly get => m_Ghost1; set => m_Ghost1 = value; }

        /// <summary>
        /// The Segment.
        /// </summary>
        public SegmentGeometry segment { readonly get => m_Segment; set => m_Segment = value; }

        /// <summary>
        /// The head ghost vertex
        /// A ghost vertex is used by the solver to define how a collision response should be handled when a contact with the vertex occurs.
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
        /// <param name="point">The point to check.</param>
        /// <returns>The closest point on the geometry to the specified point.</returns>
        public readonly Vector2 ClosestPoint(Vector2 point) => ChainSegmentGeometry_ClosestPoint(this, point);

        /// <summary>
        /// Calculate if a world ray intersects the geometry.
        /// See <see cref="PhysicsQuery.CastResult"/>.
        /// </summary>
        /// <param name="castRayInput">The configuration of the ray to cast.</param>
        /// <param name="oneSided">Whether to treat the segment as having one-sided collision. The "left" side collision is ignored.</param>
        /// <returns>The results of the intersection test.</returns>
        public readonly PhysicsQuery.CastResult CastRay(PhysicsQuery.CastRayInput castRayInput, bool oneSided) => ChainSegmentGeometry_CastRay(this, castRayInput, oneSided);

        /// <summary>
        /// Calculate if a cast shape intersects the geometry.
        /// Initially touching shapes are treated as a miss. You should check for overlap first if initial overlap is required.
        /// See <see cref="PhysicsQuery.CastShapeInput"/> and <see cref="PhysicsQuery.CastResult"/>.
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
                segment = segment.InverseTransform(transform),
                ghost1 = transform.InverseTransformPoint(ghost1),
                ghost2 = transform.InverseTransformPoint(ghost2)
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

        /// <summary>
        /// Transform a batch of geometry in place.
        /// </summary>
        /// <param name="geometry">The geometry to transform in place.</param>
        /// <param name="transform">The transform to apply.</param>
        public static void Transform(Span<ChainSegmentGeometry> geometry, PhysicsTransform transform)
        {
            for (var i = 0; i < geometry.Length; ++i)
                geometry[i] = geometry[i].Transform(transform);
        }

        /// <summary>
        /// Inverse-Transform a batch of geometry in place.
        /// </summary>
        /// <param name="geometry">The geometry to inverse-transform in place.</param>
        /// <param name="transform">The transform to apply.</param>
        public static void InverseTransform(Span<ChainSegmentGeometry> geometry, PhysicsTransform transform)
        {
            for (var i = 0; i < geometry.Length; ++i)
                geometry[i] = geometry[i].InverseTransform(transform);
        }

        /// <summary>
        /// Transform a batch of geometry in place.
        /// </summary>
        /// <param name="geometry">The geometry to transform in place.</param>
        /// <param name="transform">The transform to apply.</param>
        public static void Transform(Span<ChainSegmentGeometry> geometry, Matrix4x4 transform)
        {
            for (var i = 0; i < geometry.Length; ++i)
                geometry[i] = geometry[i].Transform(transform);
        }

        /// <summary>
        /// Inverse-Transform a batch of geometry in place.
        /// </summary>
        /// <param name="geometry">The geometry to inverse-transform in place.</param>
        /// <param name="transform">The transform to apply.</param>
        public static void InverseTransform(Span<ChainSegmentGeometry> geometry, Matrix4x4 transform)
        {
            // No radius, so the inverse is the forward transform by the inverted matrix; invert once.
            transform = transform.inverse;
            for (var i = 0; i < geometry.Length; ++i)
                geometry[i] = geometry[i].Transform(transform);
        }

        /// <undoc/>
        public static implicit operator PhysicsShape.ShapeProxy(ChainSegmentGeometry geometry) => geometry.CreateShapeProxy();

        #region Internal

        [SerializeField] SegmentGeometry m_Segment;
        [SerializeField] Vector2 m_Ghost1;
        [SerializeField] Vector2 m_Ghost2;
        int m_ChainId;

        #endregion
    };
}
