// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEngine.Profiling;
using UnityEngine.UIElements.UIR;

// Overview of the Vector Graphics API
// ===================================
//
// Use the Painter2D object is to issue vector drawing commands with the Mesh API backend.
// When calling the drawing methods, such as `LineTo()` or `BezierTo()`, the commands
// are registered as `SubPathEntry` values.
//
// Use either `Stroke()` or `Fill()` on the recorded sub-paths. The methods have the
// following behavior:
//
//  1. Calling `Stroke()`
//     The process generates triangle strips for the sub-paths. How the strips
//     are generated depends on the sub-path types. The stroking methods are `StrokeLine()`,
//     `StrokeArc()`, `StrokeArcTo()` and `StrokeBezier()`.
//
//     When the strip is built, it also generates JoinInfo structures. These are used to join
//     the triangle strips of connected sub-paths together (miter, bevel or rounded) and to generate
//     the path caps (butt or rounded). The joins and caps generate after the sub-path triangle
//     strips, since they need the JoinInfo data to function.
//
//     Joins might move the vertices of connected sub-paths to avoid triangle overlaps.
//     Triangle overlap might still occur in some situations. The joins or caps might also
//     use the tangent information of the curve (stored in the `JoinInfo` structure) to generate
//     the geometry. This is used in more complex situations, such as Bezier or Arcs, where
//     strip connections are more error prone due to high curvature.
//
//     Finally, the generated triangle strip is inflated by `k_EdgeBuffer` pixels to
//     accomodate the arc rendering.
//
//  2. Calling `Fill()`
//     The filling process does the following:
//        a. Generates arcs for each sub-paths: `GenerateFilledArcs()`
//        b. Sends the arc endpoints to LibTess to generate a rough tessellation of the
//           shape: `TessellateFillWithArcMappings()`
//        c. Builds the actual mesh from the vertices computed in step b.: `GenerateFilledMesh()`
//
//     After the LibTess process in step b., build a mapping between the triangle edges
//     that maps to the arcs from step a. This gives enough information to compute
//     arc data when building the actual mesh in step c.

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Object to draw 2D vector graphics.
    /// </summary>
    public class Painter2D : IDisposable
    {
        private MeshGenerationContext m_Ctx;
        internal DetachedAllocator m_DetachedAllocator;
        internal SafeHandleAccess m_Handle;

        internal bool isDetached => m_DetachedAllocator != null;

        // Instantiated internally by UIR, users shouldn't derive from this class.
        internal Painter2D(MeshGenerationContext ctx)
        {
            m_Handle = new SafeHandleAccess(UIPainter2D.Create(maxArcRadius));
            m_Ctx = ctx;
            Reset();
        }

        /// <summary>
        /// Initializes an instance of Painter2D.
        /// </summary>
        public Painter2D()
        {
            m_Handle = new SafeHandleAccess(UIPainter2D.Create(maxArcRadius));
            m_DetachedAllocator = new DetachedAllocator();
            isPainterActive = true;
            Reset();
        }

        internal void Reset()
        {
            UIPainter2D.Reset(m_Handle);
        }

        internal MeshWriteData Allocate(int vertexCount, int indexCount)
        {
            if (isDetached)
                return m_DetachedAllocator.Alloc(vertexCount, indexCount);
            else
                return m_Ctx.Allocate(vertexCount, indexCount);
        }

        /// <summary>
        /// When created as a detached painter, clears the current content. Does nothing otherwise.
        /// </summary>
        public void Clear()
        {
            if (!isDetached)
            {
                Debug.LogError("Clear() cannot be called on a Painter2D associated with a MeshGenerationContext. You should create your own instance of Painter2D instead.");
                return;
            }

            m_DetachedAllocator.Clear();
        }


        /// <summary>
        /// Dispose the Painter2D object and free its internal unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool m_Disposed;
        void Dispose(bool disposing)
        {
            if(m_Disposed)
                return;

            if (disposing)
            {
                if (!m_Handle.IsNull())
                {
                    UIPainter2D.Destroy(m_Handle);
                    m_Handle = new SafeHandleAccess(IntPtr.Zero);
                }

                if (m_DetachedAllocator != null)
                    m_DetachedAllocator.Dispose();
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            m_Disposed = true;
        }

        /// <summary>
        /// The line width of draw paths when using <see cref="Stroke"/>.
        /// </summary>
        public float lineWidth
        {
            get => UIPainter2D.GetLineWidth(m_Handle);
            set => UIPainter2D.SetLineWidth(m_Handle, value);
        }

        /// <summary>
        /// The color of draw paths when using <see cref="Stroke"/>.
        /// </summary>
        /// <remarks>
        /// Setting a stroke color will override the currently set <see cref="strokeGradient"/>.
        /// </remarks>
        public Color strokeColor
        {
            get => UIPainter2D.GetStrokeColor(m_Handle);
            set => UIPainter2D.SetStrokeColor(m_Handle, value);
        }

        /// <summary>
        /// The stroke gradient to use when using <see cref="Stroke"/>.
        /// </summary>
        /// <remarks>
        /// Setting a stroke gradient will override the currently set <see cref="strokeColor"/>.
        /// Setting a null stroke gradient will remove it and fall back on the currently set <see cref="strokeColor"/>.
        /// </remarks>
        public Gradient strokeGradient
        {
            get => UIPainter2D.GetStrokeGradient(m_Handle);
            set => UIPainter2D.SetStrokeGradient(m_Handle, value);
        }

        /// <summary>
        /// The color used for fill paths when using <see cref="Fill"/>.
        /// </summary>
        public Color fillColor
        {
            get => UIPainter2D.GetFillColor(m_Handle);
            set => UIPainter2D.SetFillColor(m_Handle, value);
        }

        /// <summary>
        /// The join to use when drawing paths using <see cref="Stroke"/>.
        /// </summary>
        public LineJoin lineJoin
        {
            get => UIPainter2D.GetLineJoin(m_Handle);
            set => UIPainter2D.SetLineJoin(m_Handle, value);
        }

        /// <summary>
        /// The cap to use when drawing paths using <see cref="Stroke"/>.
        /// </summary>
        public LineCap lineCap
        {
            get => UIPainter2D.GetLineCap(m_Handle);
            set => UIPainter2D.SetLineCap(m_Handle, value);
        }

        /// <summary>
        /// When using <see cref="LineJoin.Miter"/> joins, this defines the limit on the ratio of the miter length to the
        /// stroke width before converting the miter to a bevel.
        /// </summary>
        public float miterLimit
        {
            get => UIPainter2D.GetMiterLimit(m_Handle);
            set => UIPainter2D.SetMiterLimit(m_Handle, value);
        }

        internal static bool isPainterActive { get; set; }
        private bool ValidateState()
        {
            bool isValid = isDetached || isPainterActive;
            if (!isValid)
                Debug.LogError("Cannot issue vector graphics commands outside of generateVisualContent callback");

            return isValid;
        }

        private static float s_MaxArcRadius = -1.0f;
        private static float maxArcRadius
        {
            get {
                if (s_MaxArcRadius < 0.0f)
                {
                    if (!UIRenderDevice.vertexTexturingIsAvailable)
                        // If vertexTexturingIsAvailable is false, we probably are on a low-end
                        // device which may have fp16 fragment shader float precision. We limit
                        // the max arc radius even more in this case.
                        s_MaxArcRadius = 1.0e3f;
                    else
                        s_MaxArcRadius = 1.0e5f;
                }
                return s_MaxArcRadius;
            }
        }

        /// <summary>
        /// Begins a new path and empties the list of recorded sub-paths and resets the pen position to (0,0).
        /// </summary>
        public void BeginPath()
        {
            if (!ValidateState())
                return;

            UIPainter2D.BeginPath(m_Handle);
        }

        /// <summary>
        /// Closes the current sub-path with a straight line. If the sub-path is already closed, this does nothing.
        /// </summary>
        public void ClosePath()
        {
            if (!ValidateState())
                return;

            UIPainter2D.ClosePath(m_Handle);
        }

        /// <summary>
        /// Begins a new sub-path at the provied coordinate.
        /// </summary>
        /// <param name="pos">The position of the new sub-path.</param>
        public void MoveTo(Vector2 pos)
        {
            if (!ValidateState())
                return;

            UIPainter2D.MoveTo(m_Handle, pos);
        }

        /// <summary>
        /// Adds a straight line to the current sub-path to the provided position.
        /// </summary>
        /// <param name="pos">The end position of the line.</param>
        public void LineTo(Vector2 pos)
        {
            if (!ValidateState())
                return;

            UIPainter2D.LineTo(m_Handle, pos);
        }

        /// <summary>
        /// Adds an arc to the current sub-path to the provided position using a control point.
        /// </summary>
        /// <param name="p1">The first control point of the arc.</param>
        /// <param name="p2">The final point of the arc.</param>
        /// <param name="radius">The radius of the arc.</param>
        public void ArcTo(Vector2 p1, Vector2 p2, float radius)
        {
            if (!ValidateState())
                return;

            UIPainter2D.ArcTo(m_Handle, p1, p2, radius);
        }

        /// <summary>
        /// Adds an arc to the current sub-path to the provided position, radius and angles.
        /// </summary>
        /// <param name="center">The center position of the arc.</param>
        /// <param name="radius">The radius of the arc.</param>
        /// <param name="startAngle">The starting angle the arc.</param>
        /// <param name="endAngle">The ending angle of the arc.</param>
        /// <param name="antiClockwise">Whether the arc should draw in the anti-clockwise direction (default=false).</param>
        public void Arc(Vector2 center, float radius, Angle startAngle, Angle endAngle, ArcDirection direction = ArcDirection.Clockwise)
        {
            if (!ValidateState())
                return;

            UIPainter2D.Arc(m_Handle, center, radius, startAngle.ToRadians(), endAngle.ToRadians(), direction);
        }

        /// <summary>
        /// Adds a cubic bezier curve to the current sub-path to the provided position using two control points.
        /// </summary>
        /// <param name="p1">The first control point of the cubic bezier.</param>
        /// <param name="p2">The second control point of the cubic bezier.</param>
        /// <param name="p3">The final position of the cubic bezier.</param>
        public void BezierCurveTo(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            if (!ValidateState())
                return;

            UIPainter2D.BezierCurveTo(m_Handle, p1, p2, p3);
        }

        /// <summary>
        /// Adds a quadratic bezier curve to the current sub-path to the provided position using a control point.
        /// </summary>
        /// <param name="p1">The control point of the quadratic bezier.</param>
        /// <param name="p2">The final position of the quadratic bezier.</param>
        public void QuadraticCurveTo(Vector2 p1, Vector2 p2)
        {
            if (!ValidateState())
                return;

            UIPainter2D.QuadraticCurveTo(m_Handle, p1, p2);
        }

        private static readonly ProfilerMarker s_StrokeMarker = new ProfilerMarker("Painter2D.Stroke");

        /// <summary>
        /// Strokes the currently defined path.
        /// </summary>
        public void Stroke()
        {
            using (s_StrokeMarker.Auto())
            {
                if (!ValidateState())
                    return;

                var meshData = UIPainter2D.Stroke(m_Handle);

                if (meshData.vertexCount == 0)
                    return;

                // transfer all data in a single batch
                var meshWrite = Allocate(meshData.vertexCount, meshData.indexCount);
                unsafe
                {
                    var vertices = UIRenderDevice.PtrToSlice<Vertex>((void*)meshData.vertices, meshData.vertexCount);
                    var indices = UIRenderDevice.PtrToSlice<UInt16>((void*)meshData.indices, meshData.indexCount);
                    meshWrite.SetAllVertices(vertices);
                    meshWrite.SetAllIndices(indices);
                }
            }
        }

        private static readonly ProfilerMarker s_FillMarker = new ProfilerMarker("Painter2D.Fill");

        /// <summary>
        /// Fills the currently defined path.
        /// </summary>
        /// <param name="fillRule">The fill rule (non-zero or odd-even) to use. Default is non-zero.</param>
        public void Fill(FillRule fillRule = FillRule.NonZero)
        {
            using (s_FillMarker.Auto())
            {
                if (!ValidateState())
                    return;

                var meshData = UIPainter2D.Fill(m_Handle, fillRule);
                if (meshData.vertexCount == 0)
                    return;

                // transfer all data in a single batch
                var meshWrite = Allocate(meshData.vertexCount, meshData.indexCount);
                unsafe
                {
                    var vertices = UIRenderDevice.PtrToSlice<Vertex>((void*)meshData.vertices, meshData.vertexCount);
                    var indices = UIRenderDevice.PtrToSlice<UInt16>((void*)meshData.indices, meshData.indexCount);
                    meshWrite.SetAllVertices(vertices);
                    meshWrite.SetAllIndices(indices);
                }
            }
        }

        /// <summary>
        /// Saves the content of this <see cref="Painter2D"/> to a <see cref="VectorImage"/> object.
        /// </summary>
        /// <remarks>
        /// The size and content of the vector image will be determined from the bounding-box of the visible content of the painter object.
        /// Any offset of the visible content will not be saved in the vector image.
        /// </remarks>
        /// <param name="vectorImage">The <see cref="VectorImage"/> object that will be initialized with this painter. This object should not be null.</param>
        /// <returns>True if the VectorImage initialization succeeded. False otherwise.</returns>
        public bool SaveToVectorImage(VectorImage vectorImage)
        {
            if (!isDetached)
            {
                Debug.LogError("SaveToVectorImage cannot be called on a Painter2D associated with a MeshGenerationContext. You should create your own instance of Painter2D instead.");
                return false;
            }

            if (vectorImage == null)
                throw new NullReferenceException("The provided vectorImage is null");

            var meshes = m_DetachedAllocator.meshes;

            // Count the total number of vertices/indices.
            int vertCount = 0, indCount = 0;
            foreach (var mwd in meshes)
            {
                vertCount += mwd.m_Vertices.Length;
                indCount += mwd.m_Indices.Length;
            }

            var bboxMin = new Vector2(float.MaxValue, float.MaxValue);
            var bboxMax = new Vector2(-float.MaxValue, -float.MaxValue);
            foreach (var mwd in meshes)
            {
                var vs = mwd.m_Vertices;
                for (int i = 0; i < vs.Length; ++i)
                {
                    var v = vs[i];
                    bboxMin = Vector2.Min(bboxMin, v.position);
                    bboxMax = Vector2.Max(bboxMax, v.position);
                }
            }

            // Allocate + copy
            var allVerts = new VectorImageVertex[vertCount];
            var allInds = new UInt16[indCount];
            int vCount = 0;
            int iCount = 0;
            int baseVertex = 0;
            foreach (var mwd in meshes)
            {
                var verts = mwd.m_Vertices;
                for (int i = 0; i < verts.Length; ++i)
                {
                    var v = verts[i];
                    var p = v.position;
                    p.x -= bboxMin.x;
                    p.y -= bboxMin.y;
                    allVerts[vCount++] = new VectorImageVertex() {
                        position = new Vector3(p.x, p.y, Vertex.nearZ),
                        tint = v.tint,
                        uv = v.uv,
                        flags = v.flags,
                        circle = v.circle,
                    };
                }

                var inds = mwd.m_Indices;
                for (int i = 0; i < inds.Length; ++i)
                    allInds[iCount++] = (UInt16)(inds[i] + baseVertex);

                baseVertex += verts.Length;
            }

            vectorImage.version = 0;
            vectorImage.vertices = allVerts;
            vectorImage.indices = allInds;
            vectorImage.size = bboxMax - bboxMin;

            return true;
        }
    }
}
