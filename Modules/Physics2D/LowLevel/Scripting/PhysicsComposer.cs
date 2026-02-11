// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;

using Unity.Collections;

using static UnityEngine.LowLevelPhysics2D.PhysicsShape;
using static UnityEngine.LowLevelPhysics2D.PhysicsLowLevelScripting2D;
using static UnityEngine.LowLevelPhysics2D.PhysicsComposerScripting2D;

namespace UnityEngine.LowLevelPhysics2D
{
    /// <summary>
    /// Provides the ability to compose geometry using specific operations on layers in a specific order.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PhysicsComposer : IEquatable<PhysicsComposer>
    {
        #region Id

        readonly Int32 m_Index1;
        readonly UInt16 m_Generation;

        /// <undoc/>
        public override readonly string ToString() => isValid ? $"index={m_Index1}, generation={m_Generation}" : "<INVALID>";

        #endregion

        #region Equality

        /// <undoc/>
        public override bool Equals(object obj) { return base.Equals(obj); }

        /// <undoc/>
        public bool Equals(PhysicsComposer other) { return m_Index1 == other.m_Index1 && m_Generation == other.m_Generation; }

        /// <undoc/>
        public static bool operator ==(PhysicsComposer lhs, PhysicsComposer rhs) => lhs.Equals(rhs);

        /// <undoc/>
        public static bool operator !=(PhysicsComposer lhs, PhysicsComposer rhs) => !(lhs == rhs);

        /// <undoc/>
        public override int GetHashCode() { return HashCode.Combine(m_Index1, m_Generation); }

        #endregion

        /// <summary>
        /// A composer layer containing individual or spans of shape geometries, active world shapes (from which geometry will be extracted) or contiguous set of vertices.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal readonly struct Layer
        {
            /// <undoc/>
            internal Layer(ReadOnlySpan<CircleGeometry> geometry, PhysicsTransform transform, Operation operation, int order, float curveStride, bool reverseWinding)
            {
                // Validate.
                if (geometry.Length < 1)
                    throw new ArgumentOutOfRangeException(nameof(geometry), "At least a single geometry must be specified.");

                if (curveStride < MinCurveStride || curveStride > 1.0f)
                    throw new ArgumentOutOfRangeException(nameof(curveStride), $"Curve Stride must be in the range [{PhysicsComposer.MinCurveStride}, 1.0]");

                m_LayerType = LayerType.Geometry;
                m_GeometryType = ShapeType.Circle;
                m_DataBuffer = PhysicsBuffer.FromSpan(geometry);
                m_Transform = transform;
                m_Operation = operation;
                m_Order = order;
                m_CurveStride = curveStride;
                m_ReverseWinding = reverseWinding;
            }

            /// <undoc/>
            internal Layer(ReadOnlySpan<CapsuleGeometry> geometry, PhysicsTransform transform, Operation operation, int order, float curveStride, bool reverseWinding)
            {
                // Validate.
                if (geometry.Length < 1)
                    throw new ArgumentOutOfRangeException(nameof(geometry), "At least a single geometry must be specified.");

                if (curveStride < MinCurveStride || curveStride > 1.0f)
                    throw new ArgumentOutOfRangeException(nameof(curveStride), $"Curve Stride must be in the range [{PhysicsComposer.MinCurveStride}, 1.0]");

                m_LayerType = LayerType.Geometry;
                m_GeometryType = ShapeType.Capsule;
                m_DataBuffer = PhysicsBuffer.FromSpan(geometry);
                m_Transform = transform;
                m_Operation = operation;
                m_Order = order;
                m_CurveStride = curveStride;
                m_ReverseWinding = reverseWinding;
            }

            /// <undoc/>
            internal Layer(ReadOnlySpan<PolygonGeometry> geometry, PhysicsTransform transform, Operation operation, int order, float curveStride, bool reverseWinding)
            {
                // Validate.
                if (geometry.Length < 1)
                    throw new ArgumentOutOfRangeException(nameof(geometry), "At least a single geometry must be specified.");

                if (curveStride < MinCurveStride || curveStride > 1.0f)
                    throw new ArgumentOutOfRangeException(nameof(curveStride), $"Curve Stride must be in the range [{PhysicsComposer.MinCurveStride}, 1.0]");

                m_LayerType = LayerType.Geometry;
                m_GeometryType = ShapeType.Polygon;
                m_DataBuffer = PhysicsBuffer.FromSpan(geometry);
                m_Transform = transform;
                m_Operation = operation;
                m_Order = order;
                m_CurveStride = curveStride;
                m_ReverseWinding = reverseWinding;
            }

            /// <undoc/>
            internal Layer(ReadOnlySpan<PhysicsShape> shapes, PhysicsTransform transform, Operation operation, int order, float curveStride, bool reverseWinding)
            {
                // Validate.
                if (shapes.Length < 1)
                    throw new ArgumentOutOfRangeException(nameof(shapes), "At least a single PhysicsShape must be specified.");

                foreach (var shape in shapes)
                    if (!shape.isValid)
                        throw new ArgumentException(nameof(shapes), "At least one of the shapes was invalid.");

                if (curveStride < MinCurveStride || curveStride > 1.0f)
                    throw new ArgumentOutOfRangeException(nameof(curveStride), $"Curve Stride must be in the range [{PhysicsComposer.MinCurveStride}, 1.0]");

                m_LayerType = LayerType.Shape;
                m_DataBuffer = PhysicsBuffer.FromSpan(shapes);
                m_Transform = transform;
                m_Operation = operation;
                m_Order = order;
                m_CurveStride = curveStride;
                m_ReverseWinding = reverseWinding;

                // Unused.
                m_GeometryType = default;
            }

            /// <undoc/>
            internal Layer(PhysicsBuffer vertices, PhysicsTransform transform, Operation operation, int order, bool reverseWinding)
            {
                // Validate.
                if (vertices.size < 3)
                    throw new ArgumentOutOfRangeException(nameof(vertices), "A minimum of 3 vertices must be specified.");

                m_LayerType = LayerType.Vertex;
                m_DataBuffer = vertices;
                m_Transform = transform;
                m_Operation = operation;
                m_Order = order;
                m_CurveStride = 1.0f;
                m_ReverseWinding = reverseWinding;

                // Unused.
                m_GeometryType = default;
            }

            /// <summary>
            /// Indicates the layer type.
            /// </summary>
            public enum LayerType
            {
                /// <summary>
                /// The layer is composed of a geometry span indicated by the geometry type.
                /// </summary>
                Geometry = 0,

                /// <summary>
                /// The layer is composed of a shape span.
                /// </summary>
                Shape = 1,

                /// <summary>
                /// The layer is composed of a vertex span.
                /// </summary>
                Vertex = 2
            }

            /// <summary>
            /// The layer type indicating which buffer contains the layer information.
            /// </summary>
            public readonly LayerType layerType => m_LayerType;

            /// <summary>
            /// The geometry type in composition.
            /// </summary>
            public readonly ShapeType geometryType => m_GeometryType;

            /// <summary>
            /// The buffer that contains the geometry, shape or vertex data.
            /// </summary>
            public readonly PhysicsBuffer dataBuffer => m_DataBuffer;

            /// <summary>
            /// The transform for the shape.
            /// </summary>
            public readonly PhysicsTransform transform => m_Transform;

            /// <summary>
            /// The composition operation.
            /// </summary>
            public readonly Operation operation => m_Operation;

            /// <summary>
            /// The composition order.
            /// </summary>
            public readonly int order => m_Order;

            /// <summary>
            /// The curve stride used when creating curves, in radians.
            /// </summary>
            public readonly float curveStride => m_CurveStride;

            /// <summary>
            /// Whether the winding should be reversed.
            /// Generated shape geometry are normally generated with an anti-clockwise winding however this option will reverse the winding to be clockwise.
            /// When tessellation occurs, opposite windings result in holes being generated.
            /// </summary>
            public readonly bool reverseWinding => m_ReverseWinding;

            #region Internal

            readonly LayerType m_LayerType;
            readonly ShapeType m_GeometryType;
            readonly PhysicsBuffer m_DataBuffer;
            readonly PhysicsTransform m_Transform;
            readonly Operation m_Operation;
            readonly int m_Order;
            readonly float m_CurveStride;
            readonly bool m_ReverseWinding;

            #endregion
        }

        /// <summary>
        /// A composer layer handle.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct LayerHandle
        {
            /// <undoc/>
            public override readonly string ToString() => $"index={m_IndexId}, composer={m_Composer}, generation={m_Revision}";

            #region Internal

            readonly Int32 m_IndexId;
            readonly Int32 m_Composer;
            readonly UInt16 m_Revision;

            #endregion
        }

        /// <summary>
        /// A composer operation.
        /// </summary>
        public enum Operation
        {
            /// <summary>
            /// Perform an OR operation (geometric merge).
            /// </summary>
            OR = 0,

            /// <summary>
            /// Perform an AND operation (geometric intersection).
            /// </summary>
            AND = 1,

            /// <summary>
            /// Perform a NOT operation (geometric difference).
            /// </summary>
            NOT = 2,

            /// <summary>
            /// Perform an XOR operation (geometric flip).
            /// </summary>
            XOR = 3
        }

        /// <summary>
        /// Create a Physics Composer.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>The new Physics Composer.</returns>
        public static PhysicsComposer Create(Allocator allocator = Allocator.Temp) => PhysicsComposer_Create(allocator);

        /// <summary>
        /// Destroy the Physics Composer.
        /// </summary>
        /// <returns>If the composer was destroyed or not.</returns>
        public readonly bool Destroy() => PhysicsComposer_Destroy(this);

        /// <summary>
        /// Check if a Physics Composer is valid.
        /// </summary>
        public readonly bool isValid => Composer_IsValid(this);

        /// <summary>
        /// The default curve stride used when composing geometry with curves, in radians. Lower values produce more vertices, larger values fewer vertices.
        /// </summary>
        public const float DefaultCurveStride = 0.06f;

        /// <summary>
        /// The minimum curve stride, in radians.
        /// </summary>
        public const float MinCurveStride = 0.01f;

        /// <summary>
        /// Add a Circle Geometry layer to the Physics Composer.
        /// </summary>
        /// <param name="geometry">The Circle Geometry to use.</param>
        /// <param name="transform">The transform to use on the geometry.</param>
        /// <param name="operation">The composer operation to use.</param>
        /// <param name="order">The order to perform the composer operation.</param>
        /// <param name="curveStride">The curve stride used when creating curves, in radians. Valid range is [<see cref="LowLevelPhysics2D.PhysicsComposer.MinCurveStride"/>, 1.0].</param>
        /// <param name="reverseWinding">Whether the winding should be reversed. Typically winding is generated anti-clockwise, reversed winding is therefore clockwise.</param>
        /// <returns>A handle to the new layer.</returns>
        public unsafe readonly LayerHandle AddLayer(CircleGeometry geometry, PhysicsTransform transform, Operation operation = Operation.OR, int order = 0, float curveStride = DefaultCurveStride, bool reverseWinding = false)
        {
            return AddLayer(new ReadOnlySpan<CircleGeometry>(&geometry, 1), transform, operation, order, curveStride, reverseWinding);
        }

        /// <summary>
        /// Add multiple Circle Geometry layer to the Physics Composer.
        /// </summary>
        /// <param name="geometry">The Circle Geometry to use. This geometry will be copied so the geometry the span is referring to can be disposed of afterwards if required.</param>
        /// <param name="transform">The transform to use on the geometry.</param>
        /// <param name="operation">The composer operation to use.</param>
        /// <param name="order">The order to perform the composer operation.</param>
        /// <param name="curveStride">The curve stride used when creating curves, in radians. Valid range is [<see cref="LowLevelPhysics2D.PhysicsComposer.MinCurveStride"/>, 1.0].</param>
        /// <param name="reverseWinding">Whether the winding should be reversed. Typically winding is generated anti-clockwise, reversed winding is therefore clockwise.</param>
        /// <returns>A handle to the new layer.</returns>
        public readonly LayerHandle AddLayer(ReadOnlySpan<CircleGeometry> geometry, PhysicsTransform transform, Operation operation = Operation.OR, int order = 0, float curveStride = DefaultCurveStride, bool reverseWinding = false)
        {
            return PhysicsComposer_AddLayer(this, new Layer(geometry, transform, operation, order, curveStride, reverseWinding));
        }

        /// <summary>
        /// Add a Capsule Geometry layer to the Physics Composer.
        /// </summary>
        /// <param name="geometry">The Capsule Geometry to use.</param>
        /// <param name="transform">The transform to use on the geometry.</param>
        /// <param name="operation">The composer operation to use.</param>
        /// <param name="order">The order to perform the composer operation.</param>
        /// <param name="curveStride">The curve stride used when creating curves, in radians. Valid range is [<see cref="LowLevelPhysics2D.PhysicsComposer.MinCurveStride"/>, 1.0].</param>
        /// <param name="reverseWinding">Whether the winding should be reversed. Typically winding is generated anti-clockwise, reversed winding is therefore clockwise.</param>
        /// <returns>A handle to the new layer.</returns>
        public unsafe readonly LayerHandle AddLayer(CapsuleGeometry geometry, PhysicsTransform transform, Operation operation = Operation.OR, int order = 0,  float curveStride = DefaultCurveStride, bool reverseWinding = false)
        {
            return AddLayer(new ReadOnlySpan<CapsuleGeometry>(&geometry, 1), transform, operation, order, curveStride, reverseWinding);
        }

        /// <summary>
        /// Add multiple Capsule Geometry layer to the Physics Composer.
        /// </summary>
        /// <param name="geometry">The Capsule Geometry to use. This geometry will be copied so the geometry the span is referring to can be disposed of afterwards if required.</param>
        /// <param name="transform">The transform to use on the geometry.</param>
        /// <param name="operation">The composer operation to use.</param>
        /// <param name="order">The order to perform the composer operation.</param>
        /// <param name="curveStride">The curve stride used when creating curves, in radians. Valid range is [<see cref="LowLevelPhysics2D.PhysicsComposer.MinCurveStride"/>, 1.0].</param>
        /// <param name="reverseWinding">Whether the winding should be reversed. Typically winding is generated anti-clockwise, reversed winding is therefore clockwise.</param>
        /// <returns>A handle to the new layer.</returns>
        public readonly LayerHandle AddLayer(ReadOnlySpan<CapsuleGeometry> geometry, PhysicsTransform transform, Operation operation = Operation.OR, int order = 0, float curveStride = DefaultCurveStride, bool reverseWinding = false)
        {
            return PhysicsComposer_AddLayer(this, new Layer(geometry, transform, operation, order, curveStride, reverseWinding));
        }

        /// <summary>
        /// Add a Polygon Geometry layer to the Physics Composer.
        /// </summary>
        /// <param name="geometry">The Polygon Geometry to use.</param>
        /// <param name="transform">The transform to use on the geometry.</param>
        /// <param name="operation">The composer operation to use.</param>
        /// <param name="order">The order to perform the composer operation.</param>
        /// <param name="curveStride">The curve stride used when creating curves, in radians. Valid range is [<see cref="LowLevelPhysics2D.PhysicsComposer.MinCurveStride"/>, 1.0].</param>
        /// <param name="reverseWinding">Whether the winding should be reversed. Typically winding is generated anti-clockwise, reversed winding is therefore clockwise.</param>
        /// <returns>A handle to the new layer.</returns>
        public unsafe readonly LayerHandle AddLayer(PolygonGeometry geometry, PhysicsTransform transform, Operation operation = Operation.OR, int order = 0, float curveStride = DefaultCurveStride, bool reverseWinding = false)
        {
            return AddLayer(new ReadOnlySpan<PolygonGeometry>(&geometry, 1), transform, operation, order, curveStride, reverseWinding);
        }

        /// <summary>
        /// Add multiple PhysicsShape layer to the Physics Composer.
        /// </summary>
        /// <param name="geometry">The Polygon Geometry to use. This geometry will be copied so the geometry the span is referring to can be disposed of afterwards if required.</param>
        /// <param name="transform">The transform to use on the geometry.</param>
        /// <param name="operation">The composer operation to use.</param>
        /// <param name="order">The order to perform the composer operation.</param>
        /// <param name="curveStride">The curve stride used when creating curves, in radians. Valid range is [<see cref="LowLevelPhysics2D.PhysicsComposer.MinCurveStride"/>, 1.0].</param>
        /// <param name="reverseWinding">Whether the winding should be reversed. Typically winding is generated anti-clockwise, reversed winding is therefore clockwise.</param>
        /// <returns>A handle to the new layer.</returns>
        public unsafe readonly LayerHandle AddLayer(ReadOnlySpan<PolygonGeometry> geometry, PhysicsTransform transform, Operation operation = Operation.OR, int order = 0, float curveStride = DefaultCurveStride, bool reverseWinding = false)
        {
            return PhysicsComposer_AddLayer(this, new Layer(geometry, transform, operation, order, curveStride, reverseWinding));
        }

        /// <summary>
        /// Add a PhysicsShape layer to the Physics Composer.
        /// Only PhysicsShape with a geometry of <see cref="LowLevelPhysics2D.PhysicsShape.ShapeType.Circle"/>, <see cref="LowLevelPhysics2D.PhysicsShape.ShapeType.Capsule"/> or <see cref="LowLevelPhysics2D.PhysicsShape.ShapeType.Polygon"/> will be used. All other types will be ignored.
        /// </summary>
        /// <param name="shape">The PhysicsShape to use.</param>
        /// <param name="transform">The transform to use on the geometry.</param>
        /// <param name="operation">The composer operation to use.</param>
        /// <param name="order">The order to perform the composer operation.</param>
        /// <param name="curveStride">The curve stride used when creating curves, in radians. Valid range is [<see cref="LowLevelPhysics2D.PhysicsComposer.MinCurveStride"/>, 1.0].</param>
        /// <param name="reverseWinding">Whether the winding should be reversed. Typically winding is generated anti-clockwise, reversed winding is therefore clockwise.</param>
        /// <returns>A handle to the new layer.</returns>
        public unsafe readonly LayerHandle AddLayer(PhysicsShape shape, PhysicsTransform transform, Operation operation = Operation.OR, int order = 0, float curveStride = DefaultCurveStride, bool reverseWinding = false)
        {
            return AddLayer(new ReadOnlySpan<PhysicsShape>(&shape, 1), transform, operation, order, curveStride, reverseWinding);
        }

        /// <summary>
        /// Add a Polygon Geometry layer to the Physics Composer.
        /// Only PhysicsShape with a geometry of <see cref="LowLevelPhysics2D.PhysicsShape.ShapeType.Circle"/>, <see cref="LowLevelPhysics2D.PhysicsShape.ShapeType.Capsule"/> or <see cref="LowLevelPhysics2D.PhysicsShape.ShapeType.Polygon"/> will be used. All other types will be ignored.
        /// </summary>
        /// <param name="shapes">The PhysicsShapes to use. The geometry these shapes used will be copied so the geometry the span is referring to can changed afterwards if required.</param>
        /// <param name="transform">The transform to use on the geometry.</param>
        /// <param name="operation">The composer operation to use.</param>
        /// <param name="order">The order to perform the composer operation.</param>
        /// <param name="curveStride">The curve stride used when creating curves, in radians. Valid range is [<see cref="LowLevelPhysics2D.PhysicsComposer.MinCurveStride"/>, 1.0].</param>
        /// <param name="reverseWinding">Whether the winding should be reversed. Typically winding is generated anti-clockwise, reversed winding is therefore clockwise.</param>
        /// <returns>A handle to the new layer.</returns>
        public unsafe readonly LayerHandle AddLayer(ReadOnlySpan<PhysicsShape> shapes, PhysicsTransform transform, Operation operation = Operation.OR, int order = 0, float curveStride = DefaultCurveStride, bool reverseWinding = false)
        {
            return PhysicsComposer_AddLayer(this, new Layer(shapes, transform, operation, order, curveStride, reverseWinding));
        }

        /// <summary>
        /// Add a vertices layer to the Physics Composer.
        /// </summary>
        /// <param name="vertices">A span of vertices. This geometry will be copied so the geometry the span is referring to can be disposed of afterwards if required.</param>
        /// <param name="transform">The transform to use on the geometry.</param>
        /// <param name="operation">The composer operation to use.</param>
        /// <param name="order">The order to perform the composer operation.</param>
        /// <param name="reverseWinding">Whether the winding should be reversed. Typically winding is generated anti-clockwise, reversed winding is therefore clockwise.</param>
        /// <returns>A handle to the new layer.</returns>
        public readonly LayerHandle AddLayer(ReadOnlySpan<Vector2> vertices, PhysicsTransform transform, Operation operation = Operation.OR, int order = 0, bool reverseWinding = false)
        {
            var layer = new Layer(PhysicsBuffer.FromSpan(vertices), transform, operation, order, reverseWinding);

            return PhysicsComposer_AddLayer(this, layer);
        }

        /// <summary>
        /// Remove a layer from the Physics Composer.
        /// </summary>
        /// <param name="layerHandle">The layer to remove.</param>
        public readonly void RemoveLayer(LayerHandle layerHandle) => PhysicsComposer_RemoveLayer(this, layerHandle);

        /// <summary>
        /// Remove all layers from the Physics Composer.
        /// </summary>
        public readonly void ClearLayers() => PhysicsComposer_ClearLayers(this);

        /// <summary>
        /// Get/Set if Delaunay tessellation is to be used.
        /// </summary>
        public readonly bool useDelaunay { get => PhysicsComposer_GetDelaunay(this); set => PhysicsComposer_SetDelaunay(this, value); }

        /// <summary>
        /// Get/Set the maximum number of polygon vertices to be used when composing polygon output.
        /// This should be in the range of 3 to <see cref="UnityEngine.LowLevelPhysics2D.PhysicsConstants.MaxPolygonVertices"/>.
        /// The default is <see cref="UnityEngine.LowLevelPhysics2D.PhysicsConstants.MaxPolygonVertices"/>.
        /// </summary>
        public readonly int maxPolygonVertices { get => PhysicsComposer_GetMaxPolygonVertices(this); set => PhysicsComposer_SetMaxPolygonVertices(this, value); }

        /// <summary>
        /// Get the number of layers currently added to the Physics Composer.
        /// </summary>
        public readonly int layerCount => PhysicsComposer_GetLayerCount(this);

        /// <summary>
        /// Get the layer handles added to the Physics Composer.
        /// </summary>
        /// <returns>A NativeArray of all the layer handles added to the composer. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<LayerHandle> layerHandles => PhysicsComposer_GetLayerHandles(this).ToNativeArray<LayerHandle>();

        /// <summary>
        /// Get the number of geometries that were rejected during the last Geometry Composition.
        /// Geometry can be rejected for a number of reasons such as vertices being collinear or too close etc.
        /// Whilst "pure" geometry is always valid, this geometry is meant to be used by physics which has constraints on what it can accept.
        /// All geometry successfully created will always be valid when used by physics.
        /// If you notice thin/small gaps in the composition, this is likely to be rejected geometry. Checking this property will help determine that.
        /// </summary>
        public readonly int rejectedGeometryCount => PhysicsComposer_GetRejectedGeometryCount(this);

        /// <summary>
        /// Get the geometry islands from a previous polygon geometry composition i.e. a call to <see cref="LowLevelPhysics2D.PhysicsComposer.CreatePolygonGeometry(Vector2, Allocator)"/> or <see cref="LowLevelPhysics2D.PhysicsComposer.CreateConvexHulls(Vector2, Allocator)"/>.
        /// Each generated polygon or convex-hull belongs to a unique island defining a set of polygons that are connected together as they share edges.
        /// The array returned contains a series of ranges where each range is a unique connected island where the range indicates both the start index and length of the original polygon indices.
        /// The number of discovered unique islands is defined by the size of the returned array.
        /// </summary>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>A NativeArray containing a series of ranges where each range is a uniquely connected island where the range indicates both the start and end indices of the original polygon indices.</returns>
        public readonly NativeArray<RangeInt> GetGeometryIslands(Allocator allocator) => PhysicsComposer_GetGeometryIslands(this, allocator).ToNativeArray<RangeInt>();

        /// <summary>
        /// Create <see cref="LowLevelPhysics2D.PolygonGeometry"/> from the composition by iterating all the layers added to the composition in the layer order specified, applying each operation specified.
        /// A limit is imposed on small vertex distances so it is recommended that scaling is applied here rather than on the returned geometry so geometry is not discarded due to it being invalid.
        /// </summary>
        /// <param name="vertexScale">The scaling to be applied to the composer vertices.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>A NativeArray containing the Polygon Geometry created from the composer. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PolygonGeometry> CreatePolygonGeometry(Vector2 vertexScale, Allocator allocator) => PhysicsComposer_CreatePolygonGeometry(this, vertexScale, allocator).ToNativeArray<PolygonGeometry>();

        /// <summary>
        /// Create <see cref="LowLevelPhysics2D.PolygonGeometry.ConvexHull"/> from the composition by iterating all the layers added to the composition in the layer order specified, applying each operation specified.
        /// </summary>
        /// <param name="vertexScale">The scaling to be applied to the composer vertices.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>A NativeArray containing the Polygon Geometry convex hull created from the composer. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<PolygonGeometry.ConvexHull> CreateConvexHulls(Vector2 vertexScale, Allocator allocator) => PhysicsComposer_CreateConvexHulls(this, vertexScale, allocator).ToNativeArray<PolygonGeometry.ConvexHull>();

        /// <summary>
        /// Create <see cref="LowLevelPhysics2D.ChainGeometry"/> from the composition by iterating all the layers added to the composition in the layer order specified, applying each operation specified.
        /// A limit is imposed on small vertex distances so it is recommended that scaling is applied here rather than on the returned geometry so geometry is not discarded due to it being invalid.
        /// </summary>
        /// <param name="vertices">The total set of vertices that the chain geometry uses. This must be disposed.</param>
        /// <param name="vertexScale">The scaling to be applied to the composer vertices.</param>
        /// <param name="allocator">The memory allocator to use for the results. This can only be <see cref="Unity.Collections.Allocator.Temp"/>, <see cref="Unity.Collections.Allocator.TempJob"/> or <see cref="Unity.Collections.Allocator.Persistent"/>.</param>
        /// <returns>A NativeArray containing the Chain Geometry created from the composer. This NativeArray must be disposed of after use otherwise leaks will occur. The exception to this is if the array is empty.</returns>
        public readonly NativeArray<ChainGeometry> CreateChainGeometry(out NativeArray<Vector2> vertices, Vector2 vertexScale, Allocator allocator)
        {
            // Create the chain geometry.
            var resultsBufferPair = PhysicsComposer_CreateChainGeometry(this, vertexScale, allocator);

            // Assign the vertices.
            vertices = resultsBufferPair.buffer1.ToNativeArray<Vector2>();

            // Create the chain geometry.
            using var physicsBuffers = resultsBufferPair.buffer2.ToNativeArray<PhysicsBuffer>();

            // If either of the buffers are empty then dispose appropriate and finish.
            if (vertices.Length == 0 || physicsBuffers.Length == 0)
            {
                if (vertices.Length > 0)
                    vertices.Dispose();

                return new NativeArray<ChainGeometry>();
            }

            // Create the chain geometry.
            var chainGeometry = new NativeArray<ChainGeometry>(physicsBuffers.Length, resultsBufferPair.buffer2.allocator, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < physicsBuffers.Length; ++i)
            {
                var buffer = physicsBuffers[i];
                chainGeometry[i] = new ChainGeometry(buffer.ToSpan<Vector2>());
            }

            return chainGeometry;
        }
    }
}

