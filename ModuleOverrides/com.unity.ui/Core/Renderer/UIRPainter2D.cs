// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
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
    /// The fill rule to use when filling shapes with <see cref="Painter2D.Fill(FillRule)"/>.
    /// </summary>
    public enum FillRule
    {
        /// <summary>The "non-zero" winding rule.</summary>
        NonZero,

        /// <summary>The "odd-even" winding rule.</summary>
        OddEven
    }

    /// <summary>
    /// Join types connecting two sub-paths (see <see cref="Painter2D.lineJoin"/>).
    /// </summary>
    public enum LineJoin
    {
        /// <summary>
        /// Joins the sub-paths with a sharp corner.
        /// The join converts to a beveled join when the <see cref="Painter2D.miterLimit"/> ratio is reached.
        /// </summary>
        Miter,

        /// <summary>Joins the sub-paths with a beveled corner.</summary>
        Bevel,

        /// <summary>Joins the sub-paths with a round corner.</summary>
        Round
    }

    /// <summary>
    /// Cap types for the beginning and end of paths (see <see cref="Painter2D.lineCap"/>).
    /// </summary>
    public enum LineCap
    {
        /// <summary>Terminates the path with no tip.</summary>
        Butt,

        /// <summary>Terminates the path with a round tip.</summary>
        Round
    }

    /// <summary>
    /// Direction to use when defining an arc (see <see cref="Painter2D.Arc(Vector2, float, Angle, Angle, ArcDirection)"/>).
    /// </summary>
    public enum ArcDirection
    {
        /// <summary>A clockwise direction.</summary>
        Clockwise,

        /// <summary>A counter-clockwise direction.</summary>
        CounterClockwise
    }

    /// <summary>
    /// Object to draw 2D vector graphics. Do not instantiate this class directly. Access it
    /// from the <see cref="MeshGenerationContext.painter2D"/> property.
    /// </summary>
    public class Painter2D
    {
        private MeshGenerationContext m_Ctx;
        internal SafeHandleAccess m_Handle;

        // Instantiated internally by UIR, users shouldn't derive from this class.
        internal Painter2D(MeshGenerationContext ctx)
        {
            m_Handle = new SafeHandleAccess(UIPainter2D.Create(maxArcRadius));
            m_Ctx = ctx;
            Reset();
        }

        internal void Reset()
        {
            UIPainter2D.Reset(m_Handle);
        }

        internal void Destroy()
        {
            UIPainter2D.Destroy(m_Handle);
            m_Handle = new SafeHandleAccess(IntPtr.Zero);
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
        public Color strokeColor
        {
            get => UIPainter2D.GetStrokeColor(m_Handle);
            set => UIPainter2D.SetStrokeColor(m_Handle, value);
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
            get => (LineJoin)UIPainter2D.GetLineJoin(m_Handle);
            set => UIPainter2D.SetLineJoin(m_Handle, (int)value);
        }

        /// <summary>
        /// The cap to use when drawing paths using <see cref="Stroke"/>.
        /// </summary>
        public LineCap lineCap
        {
            get => (LineCap)UIPainter2D.GetLineCap(m_Handle);
            set => UIPainter2D.SetLineCap(m_Handle, (int)value);
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
        private static bool ValidateState()
        {
            if (!isPainterActive)
                Debug.LogError("Cannot issue vector graphics commands outside of generateVisualContent callback");

            return isPainterActive;
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

            UIPainter2D.Arc(m_Handle, center, radius, startAngle.ToRadians(), endAngle.ToRadians(), (int)direction);
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
                var meshWrite = m_Ctx.Allocate(meshData.vertexCount, meshData.indexCount);
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
        /// <param name="fillRule">The fill rule (non-zero or odd-even) to use. Default is non-zero.</param>
        /// </summary>
        public void Fill(FillRule fillRule = FillRule.NonZero)
        {
            using (s_FillMarker.Auto())
            {
                if (!ValidateState())
                    return;

                var meshData = UIPainter2D.Fill(m_Handle, (int)fillRule);
                if (meshData.vertexCount == 0)
                    return;

                // transfer all data in a single batch
                var meshWrite = m_Ctx.Allocate(meshData.vertexCount, meshData.indexCount);
                unsafe
                {
                    var vertices = UIRenderDevice.PtrToSlice<Vertex>((void*)meshData.vertices, meshData.vertexCount);
                    var indices = UIRenderDevice.PtrToSlice<UInt16>((void*)meshData.indices, meshData.indexCount);
                    meshWrite.SetAllVertices(vertices);
                    meshWrite.SetAllIndices(indices);
                }
            }
        }
    }
}
