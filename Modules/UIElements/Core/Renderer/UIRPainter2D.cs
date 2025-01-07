// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
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
    /// <remarks>
    /// The example below demonstrates how to use the Painter2D class to draw content in a <see cref="VisualElement"/> with
    /// the <see cref="VisualElement.generateVisualContent"/> callback.
    /// 
    /// 
    /// You can also create a standalone <see cref="Painter2D.Painter2D"/> object to draw content offscreen,
    ///  and use the <see cref="Painter2D.SaveToVectorImage"/> method to save the painter content in a <see cref="VectorImage"/> asset.
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// using UnityEngine;
    /// using UnityEngine.UIElements;
    ///
    /// [RequireComponent(typeof(UIDocument))]
    /// public class Painter2DExample : MonoBehaviour
    /// {
    ///     public void OnEnable()
    ///     {
    ///         var doc = GetComponent<UIDocument>();
    ///         doc.rootVisualElement.generateVisualContent += Draw;
    ///     }
    ///
    ///     void Draw(MeshGenerationContext ctx)
    ///     {
    ///         var painter = ctx.painter2D;
    ///         painter.lineWidth = 10.0f;
    ///         painter.lineCap = LineCap.Round;
    ///         painter.strokeGradient = new Gradient() {
    ///             colorKeys = new GradientColorKey[] {
    ///                 new GradientColorKey() { color = Color.red, time = 0.0f },
    ///                 new GradientColorKey() { color = Color.blue, time = 1.0f }
    ///             }
    ///         };
    ///         painter.BeginPath();
    ///         painter.MoveTo(new Vector2(10, 10));
    ///         painter.BezierCurveTo(new Vector2(100, 100), new Vector2(200, 0), new Vector2(300, 100));
    ///         painter.Stroke();
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public class Painter2D : IDisposable
    {
        private MeshGenerationContext m_Ctx;
        internal DetachedAllocator m_DetachedAllocator;
        internal SafeHandleAccess m_Handle;

        internal bool isDetached => m_DetachedAllocator != null;

        List<Painter2DJobData> m_JobSnapshots = null;
        NativeArray<Painter2DJobData> m_JobParameters;

        // Instantiated internally by UIR, users shouldn't derive from this class.
        internal Painter2D(MeshGenerationContext ctx)
        {
            m_Handle = new SafeHandleAccess(UIPainter2D.Create());
            m_Ctx = ctx;
            m_JobSnapshots = new(32);
            m_OnMeshGenerationDelegate = OnMeshGeneration;
            Reset();
        }

        /// <summary>
        /// Initializes an instance of Painter2D.
        /// </summary>
        public Painter2D()
        {
            // Create the Painter2D with computeBBox flag set to true,
            // This allows other APIs (such as SaveToVectorImage) to know the size of the content.
            m_Handle = new SafeHandleAccess(UIPainter2D.Create(true));
            m_DetachedAllocator = new DetachedAllocator();
            isPainterActive = true;
            m_OnMeshGenerationDelegate = OnMeshGeneration;
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
            Reset();
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

                m_JobParameters.Dispose();
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

        /// <summary>
        /// Begins a new path and empties the list of recorded sub-paths.
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
        /// <param name="pos">The position of the new sub-path in the local space of the VisualElement or the VectorImage.</param>
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
        /// <param name="direction">The direction of the arc (default=clock-wise).</param>
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

                if (isDetached)
                {
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
                else
                {
                    // Take a snapshot for the job system
                    m_Ctx.InsertUnsafeMeshGenerationNode(out var unsafeNode);
                    int snapshotIndex = UIPainter2D.TakeStrokeSnapshot(m_Handle);
                    m_JobSnapshots.Add(new Painter2DJobData() { node = unsafeNode, snapshotIndex = snapshotIndex });                }
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

                if (isDetached)
                {
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
                else
                {
                    // Take a snapshot for the job system
                    m_Ctx.InsertUnsafeMeshGenerationNode(out var unsafeNode);
                    int jobIndex = UIPainter2D.TakeFillSnapshot(m_Handle, fillRule);
                    m_JobSnapshots.Add(new Painter2DJobData() { node = unsafeNode, snapshotIndex = jobIndex });
                }
            }
        }

        struct Painter2DJobData
        {
            public UnsafeMeshGenerationNode node;
            public int snapshotIndex;
        }

        struct Painter2DJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public IntPtr painterHandle;
            [ReadOnly] public TempMeshAllocator allocator;
            [ReadOnly] public NativeSlice<Painter2DJobData> jobParameters;

            public void Execute(int i)
            {
                var data = jobParameters[i];
                var meshData = UIPainter2D.ExecuteSnapshotFromJob(painterHandle, data.snapshotIndex);

                NativeSlice<Vertex> nativeVertices;
                NativeSlice<UInt16> nativeIndices;
                unsafe
                {
                    nativeVertices = UIRenderDevice.PtrToSlice<Vertex>((void*)meshData.vertices, meshData.vertexCount);
                    nativeIndices = UIRenderDevice.PtrToSlice<UInt16>((void*)meshData.indices, meshData.indexCount);
                }
                if (nativeVertices.Length == 0 || nativeIndices.Length == 0)
                    return;

                allocator.AllocateTempMesh(nativeVertices.Length, nativeIndices.Length, out var vertices, out var indices);

                Debug.Assert(vertices.Length == nativeVertices.Length);
                Debug.Assert(indices.Length == nativeIndices.Length);
                vertices.CopyFrom(nativeVertices);
                indices.CopyFrom(nativeIndices);

                data.node.DrawMesh(vertices, indices);
            }
        }

        internal void ScheduleJobs(MeshGenerationContext mgc)
        {
            int snapshotCount = m_JobSnapshots.Count;
            if (snapshotCount == 0)
                return;

            if (m_JobParameters.Length < snapshotCount)
            {
                m_JobParameters.Dispose();
                m_JobParameters = new NativeArray<Painter2DJobData>(snapshotCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            }

            for (int i = 0; i < snapshotCount; ++i)
                m_JobParameters[i] = m_JobSnapshots[i];
            m_JobSnapshots.Clear();

            var job = new Painter2DJob { painterHandle = m_Handle, jobParameters = m_JobParameters.Slice(0, snapshotCount) };
            mgc.GetTempMeshAllocator(out job.allocator);

            var jobHandle = job.Schedule(snapshotCount, 1);

            mgc.AddMeshGenerationJob(jobHandle);
            mgc.AddMeshGenerationCallback(m_OnMeshGenerationDelegate, null, MeshGenerationCallbackType.Work, true);
        }

        UIR.MeshGenerationCallback m_OnMeshGenerationDelegate;
        void OnMeshGeneration(MeshGenerationContext ctx, object data)
        {
            UIPainter2D.ClearSnapshots(m_Handle);
        }

        /// <summary>
        /// Saves the content of this <see cref="Painter2D"/> to a <see cref="VectorImage"/> object.
        /// </summary>
        /// <remarks>
        /// The size and content of the vector image will be determined from the bounding-box of the visible content of the painter object.
        /// Any offset of the visible content will not be saved in the vector image.
        /// </remarks>
        /// <param name="vectorImage">The VectorImage object that will be initialized with this painter. This object should not be null.</param>
        /// <returns>True if the VectorImage initialization succeeded. False otherwise.</returns>
        public unsafe bool SaveToVectorImage(VectorImage vectorImage)
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

            // Case UUM-41589: We cannot simply compute the bbox from the vertices because
            // of the additional buffer around the shapes used for anti-aliasing. The
            // ComputeBoundingBoxFromArcs() method peeks into the arc data for more precise measurements.
            // This is a native method for performance reasons.
            Rect bbox = UIPainter2D.GetBBox(m_Handle);

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
                    p.x -= bbox.x;
                    p.y -= bbox.y;
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
            vectorImage.size = bbox.size;

            return true;
        }
    }
}
