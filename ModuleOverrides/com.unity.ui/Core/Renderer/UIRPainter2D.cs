// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        private static float k_Epsilon = 0.001f;
        private static float k_EdgeBuffer = 2.0f;
        private Vector2 m_Pen;
        private Vector2 m_SubPathStart;
        private bool m_AtSubPathStart = true;
        private bool m_PenIsSet = false;
        private bool m_IsPathClosed = false;
        private bool m_PathHasBegun = false;
        private int m_SubPathStartIndex;
        private List<SubPathEntry> m_SubPathEntries = new List<SubPathEntry>(100);

        internal enum EntryType
        {
            Move,
            Line,
            Arc,
            ArcTo,
            Bezier,
            StartCap,
            EndCap,
            Join
        }

        internal struct SubPathEntry
        {
            public EntryType type;
            public Vector2 p0;
            public Vector2 p1;
            public Vector2 p2;
            public Vector2 p3;
            public Vector2 startPos; // Used with ClosePath()
            public JoinInfo beginJoinInfo;
            public JoinInfo endJoinInfo;
            public int joinToIndex;
        }

        // JoinInfo is used to compute proper joins between sub-paths during stroke operations.
        // We store the vertex indices to be able to move them to avoid overlaps.
        //
        //  left0    left1
        //     +------+
        //     | \    |
        //     |   \  |
        //  p0 +------+ p1    p0/p1 can be the connect point
        //     |   /  |       depending if its the begin or end join
        //     | /    |
        //     +------+
        //  right0   right1

        internal struct JoinInfo
        {
            public Vector2 p0;
            public Vector2 p1;

            public int leftPointIndex0; // The mesh points on the left side of connect point
            public int leftPointIndex1;
            public int leftPointIndex2; // Next point to complete the left triangle

            public int rightPointIndex0; // The mesh points on the right side of connect point
            public int rightPointIndex1;
            public int rightPointIndex2; // Next point to complete the right triangle

            public bool useTangent; // Use tangent instead of mesh points, to work around hairy situations
            public Vector2 tangent;
        }

        internal Vector2 pen {
            get { return m_Pen; }
            set { m_Pen = value; m_PenIsSet = true; }
        }
        internal bool atSubPathStart => m_AtSubPathStart;
        internal List<SubPathEntry> subPathEntries => m_SubPathEntries;

        private MeshGenerationContext m_Ctx;
        private static Color32 s_ArcFlags = new Color32(0, 0, 1, 0);

        // Instantiated internally by UIR, users shouldn't derive from this class.
        internal Painter2D(MeshGenerationContext ctx)
        {
            m_Ctx = ctx;
            Reset();
        }

        /// <summary>
        /// The line width of draw paths when using <see cref="Stroke"/>.
        /// </summary>
        public float lineWidth
        {
            get { return m_LineWidth; }
            set { m_LineWidth = value; }
        }
        private float m_LineWidth = 1.0f;

        /// <summary>
        /// The color of draw paths when using <see cref="Stroke"/>.
        /// </summary>
        public Color strokeColor
        {
            get { return m_StrokeColor; }
            set { m_StrokeColor = value; }
        }
        private Color m_StrokeColor = Color.black;

        /// <summary>
        /// The color used for fill paths when using <see cref="Fill"/>.
        /// </summary>
        public Color fillColor
        {
            get { return m_FillColor; }
            set { m_FillColor = value; }
        }
        private Color m_FillColor = Color.black;

        /// <summary>
        /// The join to use when drawing paths using <see cref="Stroke"/>.
        /// </summary>
        public LineJoin lineJoin { get; set; }

        /// <summary>
        /// The cap to use when drawing paths using <see cref="Stroke"/>.
        /// </summary>
        public LineCap lineCap { get; set; }

        /// <summary>
        /// When using <see cref="LineJoin.Miter"/> joins, this defines the limit on the ratio of the miter length to the
        /// stroke width before converting the miter to a bevel.
        /// </summary>
        public float miterLimit
        {
            get { return m_MiterLimit; }
            set { m_MiterLimit = Mathf.Max(1.0f, value); }
        }
        private float m_MiterLimit = 10.0f;

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

            BeginPathInternal();
        }

        internal void BeginPathInternal()
        {
            m_SubPathEntries.Clear();
            m_AtSubPathStart = true;
            m_IsPathClosed = false;
            m_PathHasBegun = true;
            m_Pen = Vector2.zero;
            m_PenIsSet = false;
            m_SubPathStart = Vector2.zero;
            m_SubPathStartIndex = 0;
        }

        internal void Reset()
        {
            BeginPathInternal();
            strokeColor = Color.black;
            fillColor = Color.black;
            lineWidth = 1.0f;
            lineJoin = LineJoin.Miter;
            lineCap = LineCap.Butt;
            miterLimit = 10.0f;
            m_PathHasBegun = false;
        }

        /// <summary>
        /// Closes the current sub-path with a straight line. If the sub-path is already closed, this does nothing.
        /// </summary>
        public void ClosePath()
        {
            if (!ValidateState())
                return;

            if (m_IsPathClosed)
                return;

            // Attempt to close current sub-path with a straight line
            bool hasGraphics = false;
            int firstSubPathGraphicIndex = 0;
            for (int i = m_SubPathStartIndex; i < m_SubPathEntries.Count; ++i)
            {
                if (IsEntryTypeGraphic(m_SubPathEntries[i].type))
                {
                    hasGraphics = true;
                    LineTo(m_SubPathEntries[i].startPos);
                    firstSubPathGraphicIndex = i;
                    break;
                }
            }

            TerminateSubPath(false);

            if (hasGraphics)
            {
                if (firstSubPathGraphicIndex >= 1 && m_SubPathEntries[firstSubPathGraphicIndex - 1].type == EntryType.StartCap)
                {
                    // Remove the starting cap and fix the join-to indices
                    m_SubPathEntries.RemoveAt(firstSubPathGraphicIndex - 1);
                    for (int i = firstSubPathGraphicIndex; i < m_SubPathEntries.Count; ++i)
                    {
                        var entry = m_SubPathEntries[i];
                        if (entry.type == EntryType.Join)
                        {
                            --entry.joinToIndex;
                            m_SubPathEntries[i] = entry;
                        }
                    }
                }

                // Insert a join to link with the first sub-path entry
                m_SubPathEntries.Add(new SubPathEntry() { type = EntryType.Join, joinToIndex = m_SubPathStartIndex });
            }

            m_IsPathClosed = true;
        }

        /// <summary>
        /// Begins a new sub-path at the provied coordinate.
        /// </summary>
        /// <param name="pos">The position of the new sub-path.</param>
        public void MoveTo(Vector2 pos)
        {
            if (!ValidateState())
                return;

            if (!m_PathHasBegun)
            {
                Debug.LogError("Calling MoveTo() before BeginPath()");
                return;
            }

            TerminateSubPath();

            if (m_SubPathEntries.Count == 0 || m_SubPathEntries[m_SubPathEntries.Count - 1].type != EntryType.Move)
                m_SubPathEntries.Add(new SubPathEntry() { type = EntryType.Move });

            pen = pos;

            m_SubPathStart = pos;
            m_SubPathStartIndex = m_SubPathEntries.Count;
            m_AtSubPathStart = true;
            m_IsPathClosed = false;
        }

        /// <summary>
        /// Adds a straight line to the current sub-path to the provided position.
        /// </summary>
        /// <param name="pos">The end position of the line.</param>
        public void LineTo(Vector2 pos)
        {
            if (!ValidateState())
                return;

            if (!m_PathHasBegun)
            {
                Debug.LogError("Calling LineTo() before BeginPath()");
                return;
            }

            if (!m_PenIsSet)
            {
                // Basically a MoveTo() if the pen wasn't set
                pen = pos;
                return;
            }

            PrepareSubPath();

            if (pos != m_Pen)
                m_SubPathEntries.Add(new SubPathEntry() { type = EntryType.Line, p0 = m_Pen, p1 = pos, startPos = m_Pen });

            pen = pos;
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

            if (!m_PathHasBegun)
            {
                Debug.LogError("Calling ArcTo() before BeginPath()");
                return;
            }

            if (!m_PenIsSet)
                pen = p1;

            if (pen == p1 && pen == p2)
            {
                return; // Nothing to do
            }
            else if (pen == p1 || p1 == p2 || ArePointsColinear(pen, p1, p2))
            {
                // No need for an arc
                LineTo(p2);
            }
            else
            {
                var arcToEntry = new SubPathEntry() {
                    type = EntryType.ArcTo,
                    p0 = pen, p1 = p1, p2 = p2, p3 = new Vector2(radius, 0.0f),
                    startPos = pen
                };

                // The pen may not reach p2, depending on how the arc fits in the provided points.
                // We have to evaluate the arc to know where it will land
                Vector2 arcStart, arcEnd, newCenter;
                if (EvalArcTo(arcToEntry, out arcStart, out arcEnd, out newCenter))
                {
                    if (!AlmostEq(pen, arcStart))
                        LineTo(arcStart);
                    pen = arcEnd;
                }
                else
                    pen = p2;

                PrepareSubPath();

                m_SubPathEntries.Add(arcToEntry);
            }
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

            if (!m_PathHasBegun)
            {
                Debug.LogError("Calling Arc() before BeginPath()");
                return;
            }

            float startAngleRads = startAngle.ToRadians();
            float endAngleRads = endAngle.ToRadians();
            bool antiClockwise = (direction == ArcDirection.CounterClockwise);
            if (radius > k_Epsilon && !Mathf.Approximately(startAngleRads, endAngleRads))
            {
                var arcEntry = new SubPathEntry() {
                    type = EntryType.Arc,
                    p0 = center,
                    p1 = new Vector2(startAngleRads, endAngleRads),
                    p2 = new Vector2(radius, antiClockwise ? 1.0f : 0.0f),
                    startPos = pen
                };

                Vector2 p0, p1, pMid;
                EvalArcPositions(ref arcEntry, out p0, out p1, out pMid);

                if (!m_PenIsSet)
                    arcEntry.startPos = p0;
                else if (p0 != pen)
                    LineTo(p0); // Implicit line-to

                bool isFullCircle = (p0 == p1);
                PrepareSubPath(!isFullCircle); // Don't insert a cap if this is a full circle

                m_SubPathEntries.Add(arcEntry);

                pen = p1;
            }
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

            if (!m_PathHasBegun)
            {
                Debug.LogError("Calling BezierTo() before BeginPath()");
                return;
            }

            if (!m_PenIsSet)
                pen = p1;

            if (pen == p1 && pen == p2 && pen == p3)
                return;

            PrepareSubPath();
            m_SubPathEntries.Add(new SubPathEntry() { type = EntryType.Bezier, p0 = pen, p1 = p1, p2 = p2, p3 = p3, startPos = pen });
            pen = p3;
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

            if (!m_PathHasBegun)
            {
                Debug.LogError("Calling QuadraticBezierTo() before BeginPath()");
                return;
            }

            if (!m_PenIsSet)
                pen = p1;

            if (pen == p1 && pen == p2)
                return;

            PrepareSubPath();

            // Convert the quadratic curve to a cubic curve
            var cP0 = pen;
            var cP3 = p2;

            float t = 2.0f / 3.0f;
            var cP1 = cP0 + t * (p1 - cP0);
            var cP2 = p2 + t * (p1 - p2);

            m_SubPathEntries.Add(new SubPathEntry() { type = EntryType.Bezier, p0 = cP0, p1 = cP1, p2 = cP2, p3 = cP3 });
            pen = p2;
        }

        /// <summary>
        /// Strokes the currently defined path.
        /// </summary>
        public void Stroke()
        {
            if (!ValidateState())
                return;

            if (!m_PathHasBegun)
            {
                Debug.LogError("Calling Stroke() before BeginPath()");
                return;
            }

            TerminateSubPath();

            if (lineWidth < k_Epsilon || strokeColor == Color.clear)
                return;

            int vertexCount = 0;
            int indexCount = 0;

            // Stroking the entries using a null MeshWriteData will only count the required vertices/indices
            StrokeInternal(null, ref vertexCount, ref indexCount);

            var mwd = m_Ctx.Allocate(vertexCount, indexCount);
            vertexCount = 0;
            indexCount = 0;
            StrokeInternal(mwd, ref vertexCount, ref indexCount);
        }

        /// <summary>
        /// Fills the currently defined path.
        /// <param name="fillRule">The fill rule (non-zero or odd-even) to use. Default is non-zero.</param>
        /// </summary>
        public void Fill(FillRule fillRule = FillRule.NonZero)
        {
            if (!ValidateState())
                return;

            if (!m_PathHasBegun)
            {
                Debug.LogError("Calling Fill() before BeginPath()");
                return;
            }

            if (fillColor == Color.clear)
                return;

            // Filling the entries using a null MeshWriteData will only count the required vertices/indices
            GenerateFilledArcs();

            // Call libtess
            m_LibTessVertices.Clear();
            m_LibTessIndices.Clear();
            m_LibTessVertexIndices.Clear();
            m_LibTessContours.Clear();
            TessellateFillWithArcMappings(fillRule, m_LibTessVertices, m_LibTessIndices, m_LibTessVertexIndices, m_LibTessContours);

            // Mesh generation
            GenerateFilledMesh(m_LibTessVertices, m_LibTessIndices, m_LibTessVertexIndices);
        }

        private void PrepareSubPath(bool canInsertCap = true)
        {
            if (m_AtSubPathStart)
            {
                if (canInsertCap && (m_SubPathEntries.Count == 0 || m_SubPathEntries[m_SubPathEntries.Count - 1].type != EntryType.StartCap))
                    // Start the new sub-path with a cap
                    m_SubPathEntries.Add(new SubPathEntry() { type = EntryType.StartCap });

                m_AtSubPathStart = false;
            }
            else if (m_SubPathEntries.Count > 0)
            {
                // Attempt to insert a join
                var lastType = m_SubPathEntries[m_SubPathEntries.Count - 1].type;
                if (IsEntryTypeGraphic(lastType))
                    m_SubPathEntries.Add(new SubPathEntry() { type = EntryType.Join, joinToIndex = m_SubPathEntries.Count + 1 });
            }
        }

        private void TerminateSubPath(bool addEndCap = true)
        {
            if (m_AtSubPathStart)
                return; // Nothing to do

            int lastIndex = m_SubPathEntries.Count - 1;

            // Remove any pending joins
            while (lastIndex >= 0 && m_SubPathEntries[lastIndex].type == EntryType.Join)
            {
                m_SubPathEntries.RemoveAt(lastIndex);
                --lastIndex;
            }

            if (lastIndex >= 0)
            {
                var lastEntryType = m_SubPathEntries[lastIndex].type;
                if (lastEntryType == EntryType.StartCap)
                {
                    // A sub-path was started, but nothing was added to it, remove the starting cap
                    m_SubPathEntries.RemoveAt(lastIndex);
                    --lastIndex;
                }
                else if (addEndCap && lastEntryType != EntryType.EndCap)
                {
                    // If there's any sub-path to terminate, add the final cap
                    if (ShouldAddEndCap(m_SubPathEntries[lastIndex]))
                        m_SubPathEntries.Add(new SubPathEntry() { type = EntryType.EndCap });
                }
            }

            m_AtSubPathStart = true;
        }

        private static bool ShouldAddEndCap(SubPathEntry entry)
        {
            // There are situations where we don't want to end the path with an end-cap,
            // they are enumerated here.

            if (entry.type == EntryType.Arc)
            {
                Vector2 p0, p1, pMid;
                EvalArcPositions(ref entry, out p0, out p1, out pMid);
                if (p0 == p1)
                    // This is a circle, don't end the path
                    return false;
            }

            return true;
        }

        internal static bool ArePointsColinear(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            var v = p1 - p0;
            var w = p2 - p1;
            var crossZ = v.x * w.y - v.y * w.x;
            return Mathf.Abs(crossZ) <= k_Epsilon;
        }

        private static bool IsEntryTypeGraphic(EntryType type)
        {
            return type == EntryType.Line || type == EntryType.Arc || type == EntryType.ArcTo || type == EntryType.Bezier;
        }

        private void StrokeInternal(MeshWriteData mwd, ref int vertexCount, ref int indexCount)
        {
            int entryCount = m_SubPathEntries.Count;
            int bezierIndex = 0;

            // Stroke everything but the joins/caps, which requires join-info of future sub-paths.
            // Join-infos are computed during the stroke generation.
            for (int i = 0; i < entryCount; ++i)
            {
                var entry = m_SubPathEntries[i];
                switch (entry.type)
                {
                    case EntryType.Line:
                        StrokeLine(mwd, ref entry, ref vertexCount, ref indexCount);
                        break;
                    case EntryType.Arc:
                        StrokeArc(mwd, ref entry, ref vertexCount, ref indexCount);
                        break;
                    case EntryType.ArcTo:
                        StrokeArcTo(mwd, ref entry, ref vertexCount, ref indexCount);
                        break;
                    case EntryType.Bezier:
                        StrokeBezier(mwd, ref entry, bezierIndex++, ref vertexCount, ref indexCount);
                        break;
                    default:
                        break;
                }
                m_SubPathEntries[i] = entry;
            }

            // Stroke the joins/caps
            for (int i = 0; i < entryCount; ++i)
            {
                var entry = m_SubPathEntries[i];
                switch (entry.type)
                {
                    case EntryType.Join:
                    {
                        if (i > 0)
                        {
                            int prevIndex = i - 1;
                            int nextIndex = entry.joinToIndex;
                            if (prevIndex != nextIndex && prevIndex >= 0 && nextIndex >= 0 && nextIndex < entryCount)
                            {
                                var prevEntry = m_SubPathEntries[prevIndex];
                                var nextEntry = m_SubPathEntries[nextIndex];
                                if (IsEntryTypeGraphic(prevEntry.type) && IsEntryTypeGraphic(nextEntry.type))
                                    StrokeJoin(mwd, ref prevEntry.endJoinInfo, ref nextEntry.beginJoinInfo, ref vertexCount, ref indexCount);
                            }
                        }
                    }
                    break;
                    case EntryType.StartCap:
                    {
                        if (i < (entryCount - 1))
                        {
                            var nextEntry = m_SubPathEntries[i + 1];
                            if (IsEntryTypeGraphic(nextEntry.type))
                                StrokeCap(mwd, ref nextEntry.beginJoinInfo, true, ref vertexCount, ref indexCount);
                        }
                    }
                    break;
                    case EntryType.EndCap:
                    {
                        if (i > 0)
                        {
                            var prevEntry = m_SubPathEntries[i - 1];
                            if (IsEntryTypeGraphic(prevEntry.type))
                                StrokeCap(mwd, ref prevEntry.endJoinInfo, false, ref vertexCount, ref indexCount);
                        }
                    }
                    break;
                }
            }
        }

        private void StrokeLine(MeshWriteData mwd, ref SubPathEntry lineEntry, ref int vertexCount, ref int indexCount)
        {
            if (mwd == null)
            {
                vertexCount += 6;
                indexCount += 12;
                return;
            }

            float halfThickness = Mathf.Max(1.0f, lineWidth) / 2.0f;
            float geomHalfThickness = halfThickness + k_EdgeBuffer;
            var p0 = lineEntry.p0;
            var p1 = lineEntry.p1;
            var v = (p1 - p0);
            var perp = new Vector2(-v.y, v.x).normalized * geomHalfThickness;

            float ratio = geomHalfThickness / halfThickness;

            var circle0 = new Vector4(0.0f, 0.0f, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circle1 = new Vector4(0.0f, ratio, Tessellation.kUnusedArc, Tessellation.kUnusedArc);

            var color = strokeColor;
            color.a *= Mathf.Clamp01(lineWidth);

            mwd.SetNextVertex(new Vertex() { position = p0,        tint = color, flags = s_ArcFlags, circle = circle0 });
            mwd.SetNextVertex(new Vertex() { position = p0 + perp, tint = color, flags = s_ArcFlags, circle = circle1 });
            mwd.SetNextVertex(new Vertex() { position = p0 - perp, tint = color, flags = s_ArcFlags, circle = circle1 });

            mwd.SetNextVertex(new Vertex() { position = p1,        tint = color, flags = s_ArcFlags, circle = circle0 });
            mwd.SetNextVertex(new Vertex() { position = p1 + perp, tint = color, flags = s_ArcFlags, circle = circle1 });
            mwd.SetNextVertex(new Vertex() { position = p1 - perp, tint = color, flags = s_ArcFlags, circle = circle1 });

            mwd.SetNextIndex((UInt16)(vertexCount + 0));
            mwd.SetNextIndex((UInt16)(vertexCount + 3));
            mwd.SetNextIndex((UInt16)(vertexCount + 1));
            mwd.SetNextIndex((UInt16)(vertexCount + 1));
            mwd.SetNextIndex((UInt16)(vertexCount + 3));
            mwd.SetNextIndex((UInt16)(vertexCount + 4));

            mwd.SetNextIndex((UInt16)(vertexCount + 0));
            mwd.SetNextIndex((UInt16)(vertexCount + 2));
            mwd.SetNextIndex((UInt16)(vertexCount + 3));
            mwd.SetNextIndex((UInt16)(vertexCount + 3));
            mwd.SetNextIndex((UInt16)(vertexCount + 2));
            mwd.SetNextIndex((UInt16)(vertexCount + 5));

            lineEntry.beginJoinInfo = new JoinInfo() {
                p0 = p0, p1 = p1,
                leftPointIndex0 = vertexCount + 2,
                leftPointIndex1 = vertexCount + 5,
                leftPointIndex2 = vertexCount,
                rightPointIndex0 = vertexCount + 1,
                rightPointIndex1 = vertexCount + 4,
                rightPointIndex2 = vertexCount + 3,
                tangent = (p1 - p0).normalized
            };

            lineEntry.endJoinInfo = lineEntry.beginJoinInfo;

            vertexCount += 6;
            indexCount += 12;
        }

        internal static bool EvalArcTo(SubPathEntry arcToEntry, out Vector2 p0, out Vector2 p1, out Vector2 center)
        {
            // Computes the end point and center of an arc from an arc-to subpath entry.

            float radius = arcToEntry.p3.x;
            var v = (arcToEntry.p0 - arcToEntry.p1).normalized;
            var w = (arcToEntry.p2 - arcToEntry.p1).normalized;
            var bisect = ((v + w) * 0.5f).normalized;

            // Find the point along v and w where the radius fits between the two lines
            var theta = Vector2.Angle(v, bisect) * Mathf.Deg2Rad;
            if (theta < k_Epsilon)
            {
                p0 = p1 = center = Vector2.zero;
                return false; // Shouldn't happen, we test for colinearity before registering an arc
            }

            float sideDist = radius / Mathf.Tan(theta);
            float centerDist = radius / Mathf.Sin(theta);

            center = arcToEntry.p1 + bisect * centerDist;
            p0 = arcToEntry.p1 + v * sideDist;
            p1 = arcToEntry.p1 + w * sideDist;

            return true;
        }

        private void StrokeArcTo(MeshWriteData mwd, ref SubPathEntry arcToEntry,  ref int vertexCount, ref int indexCount)
        {
            SubPathEntry arcEntry;
            if (!ConvertArcToEntryToArcEntry(ref arcToEntry, out arcEntry))
                return;

            StrokeArc(mwd, ref arcEntry, ref vertexCount, ref indexCount);

            arcToEntry.beginJoinInfo = arcEntry.beginJoinInfo;
            arcToEntry.endJoinInfo = arcEntry.endJoinInfo;
        }

        internal static bool ConvertArcToEntryToArcEntry(ref SubPathEntry arcToEntry, out SubPathEntry outArcEntry)
        {
            // Arc-to entries are processed the same as normal arc entries. This method converts an arc-to entry
            // to an arc entry.

            float radius = arcToEntry.p3.x;
            Vector2 p0, p1, center;
            if (!EvalArcTo(arcToEntry, out p0, out p1, out center))
            {
                outArcEntry = new SubPathEntry();
                return false;
            }

            var v = (p0 - center);
            var w = (p1 - center);

            float startAngle = Vector2.SignedAngle(Vector2.right, v) * Mathf.Deg2Rad;
            float endAngle = Vector2.SignedAngle(Vector2.right, w) * Mathf.Deg2Rad;

            float crossZ = v.x * w.y - v.y * w.x;
            bool antiClockwise = crossZ < 0.0f;

            outArcEntry = new SubPathEntry() {
                type = EntryType.Arc,
                p0 = center,
                p1 = new Vector2(startAngle, endAngle),
                p2 = new Vector2(radius, antiClockwise ? 1.0f : 0.0f)
            };
            return true;
        }

        private static void GetArcData(SubPathEntry arcEntry, out Vector2 center, out float startAngle, out float endAngle, out float radius, out bool antiClockwise)
        {
            // Decodes the arc data stored in an arc entry

            center = arcEntry.p0;
            startAngle = arcEntry.p1.x;
            endAngle = arcEntry.p1.y;
            radius = arcEntry.p2.x;
            antiClockwise = arcEntry.p2.y == 1.0f;

            // Clamp angles between 0 - 2pi
            startAngle = NormalizeAngle(startAngle);
            endAngle = NormalizeAngle(endAngle);
        }

        private static float NormalizeAngle(float a)
        {
            /// Convert any angle to the (0 - 2pi) range
            const float twoPI = Mathf.PI * 2.0f;
            bool isNeg = a < 0.0f;
            a = Mathf.Abs(a);
            if (a > twoPI)
                a = Mathf.Repeat(a, twoPI);
            if (isNeg)
                a = twoPI - a;
            return a;
        }

        internal static void EvalArcPositions(ref SubPathEntry arcEntry, out Vector2 p0, out Vector2 p1, out Vector2 pMid)
        {
            Vector2 center;
            float startAngle, endAngle, radius;
            bool antiClockwise;
            GetArcData(arcEntry, out center, out startAngle, out endAngle, out radius, out antiClockwise);

            p0 = arcEntry.p0 + new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle)) * radius;
            p1 = arcEntry.p0 + new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle)) * radius;

            float halfSweep = SweepAngle(startAngle, endAngle, antiClockwise) * 0.5f;
            if (antiClockwise)
                halfSweep = -halfSweep;
            var v = (p0 - center).normalized;
            v = Quaternion.Euler(0.0f, 0.0f, halfSweep * Mathf.Rad2Deg) * v;
            pMid = center + v * radius;
        }

        internal static float SweepAngle(float startAngle, float endAngle, bool antiClockwise)
        {
            if (Mathf.Approximately(startAngle, endAngle))
                return 0.0f;

            startAngle = NormalizeAngle(startAngle);
            endAngle = NormalizeAngle(endAngle);

            const float twoPI = (Mathf.PI * 2.0f);
            float sweep = antiClockwise ? (startAngle - endAngle) : (endAngle - startAngle);
            if (sweep < 0.0f)
                sweep = twoPI + sweep;

            if (sweep < UIRUtility.k_Epsilon)
                return twoPI; // Can't have a sweep of 0, we validate that the start/end angles aren't the same

            return sweep;
        }

        unsafe private void StrokeArc(MeshWriteData mwd, ref SubPathEntry arcEntry, ref int vertexCount, ref int indexCount)
        {
            Vector2 center;
            float startAngle, endAngle, radius;
            bool antiClockwise;
            GetArcData(arcEntry, out center, out startAngle, out endAngle, out radius, out antiClockwise);

            Vector2 p0, p1, pMid;
            EvalArcPositions(ref arcEntry, out p0, out p1, out pMid);

            // Adjust the number of verts according to the subdivision steps
            float sweepAngle = SweepAngle(startAngle, endAngle, antiClockwise);

            float arcDelta = Mathf.PI / 6.0f;
            if (antiClockwise)
                arcDelta = -arcDelta;

            int subdivs = (Math.Abs(Mathf.CeilToInt(sweepAngle / arcDelta)) + 1);
            int maxVerts = subdivs * 2;

            if (mwd == null)
            {
                vertexCount += maxVerts;
                indexCount += (subdivs - 1) * 6;
                return;
            }

            var verts = stackalloc Vector2[maxVerts];
            int vCount = 0;

            float halfStrokeWidth = lineWidth * 0.5f;
            float outerDist = (radius * 1.1f) + halfStrokeWidth + k_EdgeBuffer;
            float innerDist = radius - halfStrokeWidth - k_EdgeBuffer;

            var p = p0;
            var rot = Quaternion.Euler(0.0f, 0.0f, arcDelta * Mathf.Rad2Deg);
            for (int i = 0; i < subdivs; ++i)
            {
                var w = Vector2.one;
                if (i == 0)
                    w = (p0 - center).normalized;
                else if (i == (subdivs - 1))
                    w = (p1 - center).normalized;
                else
                    w = (p - center).normalized;

                var q0 = center + w * outerDist;
                var q1 = center + w * innerDist;

                verts[vCount++] = q0;
                verts[vCount++] = q1;

                w = rot * w;
                p = center + w;
            }

            float halfThickness = Mathf.Max(1.0f, lineWidth) / 2.0f;
            var color = strokeColor;
            color.a *= Mathf.Clamp01(lineWidth);

            float invOuterRadius = 1.0f / (radius + halfThickness);
            float invInnerRadius = 1.0f / (radius - halfThickness);
            for (int i = 0; i < vCount; ++i)
            {
                var outerRatio = (verts[i] - center) * invOuterRadius;
                var innerRatio = (verts[i] - center) * invInnerRadius;
                var circle = new Vector4(outerRatio.x, outerRatio.y, innerRatio.x, innerRatio.y);
                mwd.SetNextVertex(new Vertex() { position = verts[i], tint = color, flags = s_ArcFlags, circle = circle });
            }

            // We may have allocated a bit more vertices than necessary, write degenerate triangles
            var lastVert = verts[vCount - 1];
            while (vCount < maxVerts)
            {
                mwd.SetNextVertex(new Vertex() { position = lastVert, tint = color });
                ++vCount;
            }

            int baseVertex = vertexCount;
            for (int i = 0; i < (subdivs - 1); ++i)
            {
                EmitTriangleCW(mwd, baseVertex + 0, baseVertex + 1, baseVertex + 2);
                EmitTriangleCW(mwd, baseVertex + 2, baseVertex + 1, baseVertex + 3);
                baseVertex += 2;
            }

            vertexCount += subdivs * 2;
            indexCount += (subdivs - 1) * 6;

            var tangent0 = (p0 - center).normalized;
            tangent0 = new Vector2(-tangent0.y, tangent0.x);

            var tangent1 = (p1 - center).normalized;
            tangent1 = new Vector2(-tangent1.y, tangent1.x);

            if (antiClockwise)
            {
                tangent0 = -tangent0;
                tangent1 = -tangent1;
            }

            arcEntry.beginJoinInfo = new JoinInfo() {
                p0 = p0, p1 = p1,
                tangent = tangent0,
                useTangent = true
            };
            arcEntry.endJoinInfo = new JoinInfo() {
                p0 = p0, p1 = p1,
                tangent = tangent1,
                useTangent = true
            };
        }

        internal struct BezierArcExtrusionData
        {
            // p0
            // +------------              p1
            //  \            `------------+
            //    \                      /
            //      \      [ Arc ]      /
            //        \                /
            //         +--------------+
            //        p3             p2
            //
            // The curve is always extruding towards p0-p1, regardless of arc direction

            public Vector2 p0;
            public Vector2 p1;
            public Vector2 p2;
            public Vector2 p3;
        }

        internal struct BezierArcSegment
        {
            public float t0;
            public float t1;
            public Vector2 p0;
            public Vector2 pMid;
            public Vector2 p1;
            public Vector2 center;
            public float radius;
            public bool isArc; // False for a straight segment
            public bool isSelfIntersecting;
            public bool isSingularity;
            public bool arcGoingRight;

            public BezierArcExtrusionData extrudeData;
        }

        private const int kMaxRecursion = 8;
        private List<List<BezierArcSegment>> m_BezierArcs = new List<List<BezierArcSegment>>(64);

        internal static void SubdivideBezierIntoArcs(ref SubPathEntry bezierEntry, List<BezierArcSegment> bezierArcs, float t0 = 0.0f, float t1 = 1.0f, int iteration = 1)
        {
            float midT = (t0 + t1) * 0.5f;
            var p0 = BezierEval(bezierEntry, t0);
            var pMid = BezierEval(bezierEntry, midT);
            var p1 = BezierEval(bezierEntry, t1);

            var center = Vector2.zero;
            float radius = 0.0f;
            CircleThroughThreePoints(p0, pMid, p1, out center, out radius);

            if (radius >= maxArcRadius)
            {
                // This is a straight segment
                bezierArcs.Add(new BezierArcSegment() { t0 = t0, t1 = t1, p0 = p0, p1 = p1, isArc = false });
                return;
            }

            var tan0 = BezierTangent(bezierEntry, t0).normalized;
            var tan1 = BezierTangent(bezierEntry, t1).normalized;
            if (Vector2.Dot(tan0, tan1) < 0.0f && iteration < kMaxRecursion)
            {
                // Tangents divergence, subdivide further
                SubdivideBezierIntoArcs(ref bezierEntry, bezierArcs, t0, midT, iteration + 1);
                SubdivideBezierIntoArcs(ref bezierEntry, bezierArcs, midT, t1, iteration + 1);
                return;
            }

            // Rough error evaluation by sampling a couple more points
            float u0 = (t0 + midT) * 0.5f;
            float u1 = (t1 + midT) * 0.5f;
            var q0 = BezierEval(bezierEntry, u0);
            var q1 = BezierEval(bezierEntry, u1);

            bool closeEnough = iteration >= kMaxRecursion;
            if (!closeEnough)
            {
                float err0 = (q0 - center).magnitude - radius;
                float err1 = (q1 - center).magnitude - radius;
                float err = err0 * err0 + err1 * err1;
                closeEnough = err < 0.01f;
            }

            if (closeEnough)
            {
                var arc = new BezierArcSegment() {
                    t0 = t0, t1 = t1,
                    p0 = p0, pMid = pMid, p1 = p1,
                    center = center, radius = radius,
                    isArc = true
                };
                SplitLargeArc(bezierEntry, arc, bezierArcs);
                return;
            }

            // Subdivide and conquer
            SubdivideBezierIntoArcs(ref bezierEntry, bezierArcs, t0, midT, iteration + 1);
            SubdivideBezierIntoArcs(ref bezierEntry, bezierArcs, midT, t1, iteration + 1);
        }

        static private void SplitLargeArc(SubPathEntry bezierEntry, BezierArcSegment arc, List<BezierArcSegment> bezierArcs)
        {
            var p = (arc.p0 + arc.p1) * 0.5f;
            var cordDist = arc.radius - (p - arc.center).magnitude;
            if (cordDist <= (k_EdgeBuffer - 1))
                bezierArcs.Add(arc);
            else
            {
                float midT = (arc.t0 + arc.t1) * 0.5f;
                var mid = BezierEval(bezierEntry, midT);

                var left = new BezierArcSegment() {
                    t0 = arc.t0, t1 = midT,
                    p0 = arc.p0, p1 = mid,
                    pMid = BezierEval(bezierEntry, (arc.t0 + midT) * 0.5f),
                    center = arc.center,
                    radius = arc.radius,
                    isArc = true
                };
                SplitLargeArc(bezierEntry, left, bezierArcs);

                var right = new BezierArcSegment() {
                    t0 = midT, t1 = arc.t1,
                    p0 = mid, p1 = arc.p1,
                    pMid = BezierEval(bezierEntry, (midT + arc.t1) * 0.5f),
                    center = arc.center,
                    radius = arc.radius,
                    isArc = true
                };
                SplitLargeArc(bezierEntry, right, bezierArcs);
            }
        }

        private void PopulateArcsExtrudeData(ref SubPathEntry bezierEntry, List<BezierArcSegment> bezierArcs, out int intersectionCount, out int singularityCount)
        {
            intersectionCount = 0;
            singularityCount = 0;

            float halfThickness = lineWidth * 0.5f;

            int arcCount = bezierArcs.Count;
            for (int i = 0; i < arcCount; ++i)
            {
                var arc = bezierArcs[i];

                // Compute normals at extremities
                var t0 = BezierTangent(bezierEntry, arc.t0).normalized;
                var t1 = BezierTangent(bezierEntry, arc.t1).normalized;
                var n0 = new Vector2(-t0.y, t0.x); // Rotate right 90 deg to find normals
                var n1 = new Vector2(-t1.y, t1.x);
                var n2 = -n1;
                var n3 = -n0;

                bool arcGoingRight = Vector2.Dot(n0, t1) >= 0.0f;
                arc.arcGoingRight = arcGoingRight;

                float dist = halfThickness + k_EdgeBuffer;
                n0 *= dist;
                n1 *= dist;
                n2 *= dist;
                n3 *= dist;

                var p0 = arc.p0 + n0;
                var p1 = arc.p1 + n1;
                var p2 = arc.p1 + n2;
                var p3 = arc.p0 + n3;

                if (arcGoingRight)
                {
                    // Swap p0/p3 and p1/p2 so that the outside arc is always defined by the p0-p1 segment
                    var tmp = p0;
                    p0 = p3;
                    p3 = tmp;

                    tmp = p1;
                    p1 = p2;
                    p2 = tmp;
                }

                if (arc.isArc)
                {
                    if (Vector2.Dot(t0, t1) < 0.0f)
                    {
                        // Tangents in opposite directions, this can happen at a singularity, non-differentiable point.
                        // Collapse that point.
                        arc.p0 = arc.p1 = arc.pMid;
                        arc.radius = 0.0f;
                        arc.center = arc.pMid;
                        ++singularityCount;
                        arc.isSingularity = true;
                    }
                    else
                    {
                        // Try to find self intersection
                        var q = Tessellation.IntersectLines(p0, p3, p1, p2);
                        if (!float.IsNaN(q.x))
                        {
                            var qP0 = (q - p0);
                            var qP1 = (q - p1);
                            var p2P1 = (p2 - p1);
                            var p3P0 = (p3 - p0);

                            float qP0Length = qP0.magnitude;
                            float qP1Length = qP1.magnitude;
                            float p2P1Length = p2P1.magnitude;
                            float p3P0Length = p3P0.magnitude;

                            if (qP0Length > UIRUtility.k_Epsilon) qP0 /= qP0Length;
                            else qP0 = Vector2.zero;

                            if (qP1Length > UIRUtility.k_Epsilon) qP1 /= qP1Length;
                            else qP1 = Vector2.zero;

                            if (p2P1Length > UIRUtility.k_Epsilon) p2P1 /= p2P1Length;
                            else p2P1 = Vector2.zero;

                            if (p3P0Length > UIRUtility.k_Epsilon) p3P0 /= p3P0Length;
                            else p3P0 = Vector2.zero;

                            float qt0 = (p3P0Length > UIRUtility.k_Epsilon) ? (qP0Length / p3P0Length) : 0.0f;
                            if (Vector2.Dot(p3P0, qP0) < 0.0f)
                                qt0 = -qt0;

                            float qt1 = (p2P1Length > UIRUtility.k_Epsilon) ? (qP1Length / p2P1Length) : 0.0f;
                            if (Vector2.Dot(p2P1, qP1) < 0.0f)
                                qt1 = -qt1;

                            if (qt0 >= 0.0f && qt0 <= 1.0f && qt1 >= 0.0f && qt1 <= 1.0f)
                            {
                                p2 = p3 = q; // Collapse at intersection point
                                ++intersectionCount;
                                arc.isSelfIntersecting = true;
                            }
                        }
                    }
                }

                arc.extrudeData = new BezierArcExtrusionData() { p0 = p0, p1 = p1, p2 = p2, p3 = p3 };
                bezierArcs[i] = arc; // Update with extrude data
            }
        }

        private void StrokeBezier(MeshWriteData mwd, ref SubPathEntry bezierEntry, int bezierIndex, ref int vertexCount, ref int indexCount)
        {
            if (mwd == null)
            {
                if (bezierIndex >= m_BezierArcs.Count)
                    m_BezierArcs.Add(new List<BezierArcSegment>(64));
                var arcs = m_BezierArcs[bezierIndex];
                arcs.Clear();

                SubdivideBezierIntoArcs(ref bezierEntry, arcs);

                vertexCount += arcs.Count * 4;
                indexCount += arcs.Count * 6;

                // More quads to deal with self-intersecting arcs
                int intersectionCount, singularityCount;
                PopulateArcsExtrudeData(ref bezierEntry, arcs, out intersectionCount, out singularityCount);
                vertexCount += intersectionCount * 4;
                indexCount += intersectionCount * 6;
                vertexCount += singularityCount * 5;
                indexCount += singularityCount * 12;

                return;
            }

            var bezierArcs = m_BezierArcs[bezierIndex];

            if (bezierArcs.Count == 0)
                return;

            float halfThickness = Mathf.Max(1.0f, lineWidth) / 2.0f;
            var color = strokeColor;
            color.a *= Mathf.Clamp01(lineWidth);

            for (int i = 0; i < bezierArcs.Count; ++i)
            {
                var arc = bezierArcs[i];
                var extrudeData = arc.extrudeData;

                var p0 = extrudeData.p0;
                var p1 = extrudeData.p1;
                var p2 = extrudeData.p2;
                var p3 = extrudeData.p3;

                var circle0 = Vector4.zero;
                var circle1 = Vector4.zero;
                var circle2 = Vector4.zero;
                var circle3 = Vector4.zero;

                if (arc.isArc)
                {
                    float outerRadius = 1.0f / (arc.radius + halfThickness);
                    float innerRadius = 1.0f / (arc.radius - halfThickness);

                    var edge0 = (p0 - arc.center);
                    var edge1 = (p1 - arc.center);
                    var edge2 = (p2 - arc.center);
                    var edge3 = (p3 - arc.center);

                    var outerRatio0 = edge0 * outerRadius;
                    var innerRatio0 = edge0 * innerRadius;
                    var outerRatio1 = edge1 * outerRadius;
                    var innerRatio1 = edge1 * innerRadius;
                    var outerRatio2 = edge2 * outerRadius;
                    var innerRatio2 = edge2 * innerRadius;
                    var outerRatio3 = edge3 * outerRadius;
                    var innerRatio3 = edge3 * innerRadius;

                    circle0 = new Vector4(outerRatio0.x, outerRatio0.y, innerRatio0.x, innerRatio0.y);
                    circle1 = new Vector4(outerRatio1.x, outerRatio1.y, innerRatio1.x, innerRatio1.y);
                    circle2 = new Vector4(outerRatio2.x, outerRatio2.y, innerRatio2.x, innerRatio2.y);
                    circle3 = new Vector4(outerRatio3.x, outerRatio3.y, innerRatio3.x, innerRatio3.y);

                    var intersectionToCircle = (arc.pMid - p2);
                    float intersectionToCircleLength = intersectionToCircle.magnitude;
                    if (arc.isSelfIntersecting && intersectionToCircleLength <= halfThickness)
                    {
                        edge0 = (p0 - p2);
                        edge1 = (p1 - p2);
                        float radius = intersectionToCircleLength + halfThickness;
                        var ratio0 = edge0 / radius;
                        var ratio1 = edge1 / radius;
                        circle0 = new Vector4(ratio0.x, ratio0.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                        circle1 = new Vector4(ratio1.x, ratio1.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                        circle2 = new Vector4(0.0f, 0.0f, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                        circle3 = new Vector4(0.0f, 0.0f, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                    }
                }
                else
                {
                    float outerRadius = 1.0f / (k_EdgeBuffer + lineWidth);
                    float innerRadius = 1.0f / k_EdgeBuffer;

                    var edge0 = (p0 - p3);
                    var edge1 = (p1 - p2);

                    var outerRatio0 = edge0 * outerRadius;
                    var innerRatio0 = edge0 * innerRadius;
                    var outerRatio1 = edge1 * outerRadius;
                    var innerRatio1 = edge1 * innerRadius;

                    circle0 = new Vector4(outerRatio0.x, outerRatio0.y, innerRatio0.x, innerRatio0.y);
                    circle1 = new Vector4(outerRatio1.x, outerRatio1.y, innerRatio1.x, innerRatio1.y);
                    circle2 = Vector4.zero;
                    circle3 = Vector4.zero;
                }

                mwd.SetNextVertex(new Vertex() { position = new Vector3(p0.x, p0.y, Vertex.nearZ), tint = color, flags = s_ArcFlags, circle = circle0 });
                mwd.SetNextVertex(new Vertex() { position = new Vector3(p1.x, p1.y, Vertex.nearZ), tint = color, flags = s_ArcFlags, circle = circle1 });
                mwd.SetNextVertex(new Vertex() { position = new Vector3(p2.x, p2.y, Vertex.nearZ), tint = color, flags = s_ArcFlags, circle = circle2 });
                mwd.SetNextVertex(new Vertex() { position = new Vector3(p3.x, p3.y, Vertex.nearZ), tint = color, flags = s_ArcFlags, circle = circle3 });

                EmitTriangleCW(mwd, vertexCount, vertexCount + 1, vertexCount + 2);
                EmitTriangleCW(mwd, vertexCount, vertexCount + 2, vertexCount + 3);

                if (i == 0)
                {
                    bezierEntry.beginJoinInfo = new JoinInfo() {
                        p0 = arc.p0, p1 = arc.p1,
                        useTangent = true,
                        tangent = BezierTangent(bezierEntry, 0.0f).normalized
                    };
                }
                else if (i == (bezierArcs.Count - 1))
                {
                    bezierEntry.endJoinInfo = new JoinInfo() {
                        p0 = arc.p0, p1 = arc.p1,
                        useTangent = true,
                        tangent = BezierTangent(bezierEntry, 1.0f).normalized
                    };
                }

                vertexCount += 4;
                indexCount += 6;

                if (arc.isSelfIntersecting)
                {
                    // Generate and extra extrusion quad to fill missing section above the intersection triangle.
                    // Intersection point is in p2 and p3.
                    var q0 = p0;
                    var q1 = p1;
                    var v = (q1 - q0).normalized;
                    var leftPerp = new Vector2(v.y, -v.x);
                    if (!arc.arcGoingRight)
                        leftPerp = -leftPerp;

                    var toCircle = (arc.p0 - p2);
                    var toCircleLength = toCircle.magnitude;
                    float radius0 = toCircleLength + halfThickness;

                    toCircle = (arc.p1 - p2);
                    toCircleLength = toCircle.magnitude;
                    float radius1 = toCircleLength + halfThickness;

                    var mid = (q0 + q1) * 0.5f;
                    float extrusionWidth = Mathf.Max(0.0f, Mathf.Max(radius0, radius1) - (mid - p2).magnitude);
                    if (extrusionWidth > Tessellation.kEpsilon)
                        extrusionWidth += k_EdgeBuffer;

                    var q2 = q1 + leftPerp * extrusionWidth;
                    var q3 = q0 + leftPerp * extrusionWidth;

                    var edge0 = (q0 - p2);
                    var edge1 = (q1 - p2);
                    var edge2 = (q2 - p2);
                    var edge3 = (q3 - p2);
                    float invRadius0 = 1.0f / radius0;
                    float invRadius1 = 1.0f / radius1;
                    var ratio0 = edge0 * invRadius0;
                    var ratio1 = edge1 * invRadius1;
                    var ratio2 = edge2 * invRadius1;
                    var ratio3 = edge3 * invRadius0;
                    circle0 = new Vector4(ratio0.x, ratio0.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                    circle1 = new Vector4(ratio1.x, ratio1.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                    circle2 = new Vector4(ratio2.x, ratio2.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                    circle3 = new Vector4(ratio3.x, ratio3.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);

                    mwd.SetNextVertex(new Vertex() { position = new Vector3(q0.x, q0.y, Vertex.nearZ), tint = color, flags = s_ArcFlags, circle = circle0 });
                    mwd.SetNextVertex(new Vertex() { position = new Vector3(q1.x, q1.y, Vertex.nearZ), tint = color, flags = s_ArcFlags, circle = circle1 });
                    mwd.SetNextVertex(new Vertex() { position = new Vector3(q2.x, q2.y, Vertex.nearZ), tint = color, flags = s_ArcFlags, circle = circle2 });
                    mwd.SetNextVertex(new Vertex() { position = new Vector3(q3.x, q3.y, Vertex.nearZ), tint = color, flags = s_ArcFlags, circle = circle3 });

                    EmitTriangleCW(mwd, vertexCount, vertexCount + 1, vertexCount + 2);
                    EmitTriangleCW(mwd, vertexCount, vertexCount + 2, vertexCount + 3);

                    vertexCount += 4;
                    indexCount += 6;
                }

                if (arc.isSingularity)
                {
                    // We can't determine a proper arc for singularities, just generate a full circle and hope for the best.
                    float extrusion = halfThickness + k_EdgeBuffer;
                    var q0 = arc.pMid;
                    var q1 = q0 + new Vector2(-1, -1) * extrusion;
                    var q2 = q0 + new Vector2(1, -1) * extrusion;
                    var q3 = q0 + new Vector2(1, 1) * extrusion;
                    var q4 = q0 + new Vector2(-1, 1) * extrusion;

                    var edge1 = (q1 - q0);
                    var edge2 = (q2 - q0);
                    var edge3 = (q3 - q0);
                    var edge4 = (q4 - q0);

                    float invRadius = 1.0f / halfThickness;
                    var ratio1 = edge1 * invRadius;
                    var ratio2 = edge2 * invRadius;
                    var ratio3 = edge3 * invRadius;
                    var ratio4 = edge4 * invRadius;

                    circle0 = new Vector4(0.0f, 0.0f, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                    circle1 = new Vector4(ratio1.x, ratio1.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                    circle2 = new Vector4(ratio2.x, ratio2.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                    circle3 = new Vector4(ratio3.x, ratio3.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                    var circle4 = new Vector4(ratio4.x, ratio4.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);

                    mwd.SetNextVertex(new Vertex() { position = new Vector3(q0.x, q0.y, Vertex.nearZ), tint = color, flags = s_ArcFlags, circle = circle0 });
                    mwd.SetNextVertex(new Vertex() { position = new Vector3(q1.x, q1.y, Vertex.nearZ), tint = color, flags = s_ArcFlags, circle = circle1 });
                    mwd.SetNextVertex(new Vertex() { position = new Vector3(q2.x, q2.y, Vertex.nearZ), tint = color, flags = s_ArcFlags, circle = circle2 });
                    mwd.SetNextVertex(new Vertex() { position = new Vector3(q3.x, q3.y, Vertex.nearZ), tint = color, flags = s_ArcFlags, circle = circle3 });
                    mwd.SetNextVertex(new Vertex() { position = new Vector3(q4.x, q4.y, Vertex.nearZ), tint = color, flags = s_ArcFlags, circle = circle4 });

                    EmitTriangleCW(mwd, vertexCount, vertexCount + 1, vertexCount + 2);
                    EmitTriangleCW(mwd, vertexCount, vertexCount + 2, vertexCount + 3);
                    EmitTriangleCW(mwd, vertexCount, vertexCount + 3, vertexCount + 4);
                    EmitTriangleCW(mwd, vertexCount, vertexCount + 4, vertexCount + 1);

                    vertexCount += 5;
                    indexCount += 12;
                }
            }
        }

        internal static Vector2 BezierEval(SubPathEntry bezierEntry, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return
                (bezierEntry.p3 - 3.0f * bezierEntry.p2 + 3.0f * bezierEntry.p1 - bezierEntry.p0) * t3
                + (3.0f * bezierEntry.p2 - 6.0f * bezierEntry.p1 + 3.0f * bezierEntry.p0) * t2
                + (3.0f * bezierEntry.p1 - 3.0f * bezierEntry.p0) * t
                + bezierEntry.p0;
        }

        internal static Vector2 BezierTangent(SubPathEntry bezierEntry, float t)
        {
            // The tangent is the first derivative
            float t2 = t * t;
            return
                3.0f * (bezierEntry.p3 - 3.0f * bezierEntry.p2 + 3.0f * bezierEntry.p1 - bezierEntry.p0) * t2
                + 2.0f * (3.0f * bezierEntry.p2 - 6.0f * bezierEntry.p1 + 3.0f * bezierEntry.p0) * t
                + (3.0f * bezierEntry.p1 - 3.0f * bezierEntry.p0);
        }

        internal static void CircleThroughThreePoints(Vector2 p0, Vector2 p1, Vector2 p2, out Vector2 center, out float radius)
        {
            float t = p1.x * p1.x + p1.y * p1.y;
            float bc = (p0.x * p0.x + p0.y * p0.y - t) / 2.0f;
            float cd = (t - p2.x * p2.x - p2.y * p2.y) / 2.0f;
            float det = (p0.x - p1.x) * (p1.y - p2.y) - (p1.x - p2.x) * (p0.y - p1.y);

            if (Math.Abs(det) < 1.0e-7f)
            {
                // Points are probably collapsed together
                center = p0;
                radius = 0.0f;
                return;
            }

            det = 1.0f / det;
            float x = ((p1.y - p2.y) * bc - (p0.y - p1.y) * cd) * det;
            float y = ((p0.x - p1.x) * cd - (p1.x - p2.x) * bc) * det;
            float r = Mathf.Sqrt((x - p0.x) * (x - p0.x) + (y - p0.y) * (y - p0.y));

            center = new Vector2(x, y);
            radius = r;
        }

        private void StrokeJoin(MeshWriteData mwd, ref JoinInfo joinInfoA, ref JoinInfo joinInfoB, ref int vertexCount, ref int indexCount)
        {
            switch (lineJoin)
            {
                case LineJoin.Miter:
                    StrokeMiteredJoin(mwd, ref joinInfoA, ref joinInfoB, ref vertexCount, ref indexCount);
                    break;
                case LineJoin.Bevel:
                    StrokeBeveledJoin(mwd, ref joinInfoA, ref joinInfoB, ref vertexCount, ref indexCount);
                    break;
                case LineJoin.Round:
                    StrokeRoundedJoin(mwd, ref joinInfoA, ref joinInfoB, ref vertexCount, ref indexCount);
                    break;
                default: break;
            }
        }

        private bool SkipIfContinuous(MeshWriteData mwd, ref JoinInfo joinInfoA, ref JoinInfo joinInfoB, int expectedVertices, int expectedIndices, ref int vertexCount, ref int indexCount)
        {
            var t0 = joinInfoA.tangent;
            var t1 = joinInfoB.tangent;
            if (Mathf.Abs(Vector2.Dot(t0, t1) - 1.0f) < k_Epsilon)
            {
                // No need for a join, sub-paths are continuous
                for (int i = 0; i < expectedVertices; ++i)
                    mwd.SetNextVertex(new Vertex());
                for (int i = 0; i < expectedIndices; ++i)
                    mwd.SetNextIndex((UInt16)vertexCount);
                vertexCount += expectedVertices;
                indexCount += expectedIndices;
                return true;
            }
            return false;
        }

        private void StrokeMiteredJoin(MeshWriteData mwd, ref JoinInfo joinInfoA, ref JoinInfo joinInfoB, ref int vertexCount, ref int indexCount)
        {
            const int kExpectedVertices = 6;
            const int kExpectedIndices = 6;
            if (mwd == null)
            {
                // This assumes StrokeBeveledJoin() uses 6 verts and 6 inds as well
                vertexCount += kExpectedVertices;
                indexCount += kExpectedIndices;
                return;
            }

            if (SkipIfContinuous(mwd, ref joinInfoA, ref joinInfoB, kExpectedVertices, kExpectedIndices, ref vertexCount, ref indexCount))
                return;

            float angle = Vector2.Angle(-joinInfoA.tangent, joinInfoB.tangent);
            float minMiterAngle = (2.0f * Mathf.Asin(1.0f / miterLimit)) * Mathf.Rad2Deg;
            bool useMiter = (angle > minMiterAngle);

            if (!useMiter)
            {
                StrokeBeveledJoin(mwd, ref joinInfoA, ref joinInfoB, ref vertexCount, ref indexCount);
                return;
            }

            ConnectInnerJoins(mwd, ref joinInfoA, ref joinInfoB);

            Vector2 outerPointA0, outerPointA1, outerPointB0, outerPointB1;
            GetOuterJoinPoints(mwd, ref joinInfoA, ref joinInfoB, out outerPointA0, out outerPointA1, out outerPointB0, out outerPointB1);

            // Find intersection point between outer edges.
            // Given the miter limit, the intersection should always be valid.
            var intersection = Tessellation.IntersectLines(outerPointA0, outerPointA1, outerPointB0, outerPointB1);

            var a0 = joinInfoA.p1;
            var a1 = outerPointA1;
            var a2 = intersection;
            var b0 = joinInfoB.p0;
            var b1 = outerPointB0;
            var b2 = intersection;

            float invHalfLineWidth = 1.0f / (lineWidth * 0.5f);
            var ratioA = (a1 - a0) * invHalfLineWidth;
            var ratioB = (b1 - b0) * invHalfLineWidth;

            var circleZ = new Vector4(0.0f, 0.0f, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circleA = new Vector4(ratioA.x, ratioA.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circleB = new Vector4(ratioB.x, ratioB.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);

            mwd.SetNextVertex(new Vertex() { position = new Vector3(a0.x, a0.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circleZ });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(a1.x, a1.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circleA });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(a2.x, a2.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circleA });

            mwd.SetNextVertex(new Vertex() { position = new Vector3(b0.x, b0.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circleZ });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(b1.x, b1.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circleB });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(b2.x, b2.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circleB });

            EmitTriangleCW(mwd, vertexCount + 0, vertexCount + 1, vertexCount + 2);
            EmitTriangleCW(mwd, vertexCount + 3, vertexCount + 4, vertexCount + 5);

            vertexCount += kExpectedVertices;
            indexCount += kExpectedIndices;
        }

        private void StrokeBeveledJoin(MeshWriteData mwd, ref JoinInfo joinInfoA, ref JoinInfo joinInfoB, ref int vertexCount, ref int indexCount)
        {
            const int kExpectedVertices = 6;
            const int kExpectedIndices = 6;
            if (mwd == null)
            {
                vertexCount += kExpectedVertices;
                indexCount += kExpectedIndices;
                return;
            }

            if (SkipIfContinuous(mwd, ref joinInfoA, ref joinInfoB, kExpectedVertices, kExpectedIndices, ref vertexCount, ref indexCount))
                return;

            ConnectInnerJoins(mwd, ref joinInfoA, ref joinInfoB);

            Vector2 outerPointA0, outerPointA1, outerPointB0, outerPointB1;
            GetOuterJoinPoints(mwd, ref joinInfoA, ref joinInfoB, out outerPointA0, out outerPointA1, out outerPointB0, out outerPointB1);

            float halfLineWidth = lineWidth * 0.5f;
            var v = (outerPointA1 - joinInfoA.p1).normalized;
            var w = (outerPointB0 - joinInfoB.p0).normalized;
            v *= halfLineWidth;
            w *= halfLineWidth;

            var bevelMid = ((joinInfoA.p1 + v) + (joinInfoB.p0 + w)) * 0.5f;
            bevelMid = (bevelMid - joinInfoA.p1);
            float bevelMidLength = bevelMid.magnitude;
            var bevelPoint = joinInfoA.p1 + bevelMid.normalized * (bevelMidLength + k_EdgeBuffer);

            var p = joinInfoA.p1;
            var a = outerPointA1;
            var b = bevelPoint;

            var ratioA = (a - p) / halfLineWidth;
            var ratioB = (b - p) / bevelMidLength;

            var circleZ = new Vector4(0.0f, 0.0f, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circleA = new Vector4(0.0f, ratioA.magnitude, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circleB = new Vector4(0.0f, ratioB.magnitude, Tessellation.kUnusedArc, Tessellation.kUnusedArc);

            mwd.SetNextVertex(new Vertex() { position = new Vector3(p.x, p.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circleZ });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(a.x, a.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circleA });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(b.x, b.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circleB });
            EmitTriangleCW(mwd, vertexCount + 0, vertexCount + 1, vertexCount + 2);

            p = joinInfoB.p0;
            a = outerPointB0;
            b = bevelPoint;

            ratioA = (a - p) / halfLineWidth;
            ratioB = (b - p) / bevelMidLength;

            circleZ = new Vector4(0.0f, 0.0f, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            circleA = new Vector4(0.0f, ratioA.magnitude, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            circleB = new Vector4(0.0f, ratioB.magnitude, Tessellation.kUnusedArc, Tessellation.kUnusedArc);

            mwd.SetNextVertex(new Vertex() { position = new Vector3(p.x, p.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circleZ });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(a.x, a.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circleA });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(b.x, b.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circleB });
            EmitTriangleCW(mwd, vertexCount + 3, vertexCount + 4, vertexCount + 5);

            vertexCount += kExpectedVertices;
            indexCount += kExpectedIndices;
        }

        private void StrokeRoundedJoin(MeshWriteData mwd, ref JoinInfo joinInfoA, ref JoinInfo joinInfoB, ref int vertexCount, ref int indexCount)
        {
            const int kExpectedVertices = 5;
            const int kExpectedIndices = 9;
            if (mwd == null)
            {
                vertexCount += kExpectedVertices;
                indexCount += kExpectedIndices;
                return;
            }

            if (SkipIfContinuous(mwd, ref joinInfoA, ref joinInfoB, kExpectedVertices, kExpectedIndices, ref vertexCount, ref indexCount))
                return;

            ConnectInnerJoins(mwd, ref joinInfoA, ref joinInfoB);

            Vector2 outerPointA0, outerPointA1, outerPointB0, outerPointB1;
            GetOuterJoinPoints(mwd, ref joinInfoA, ref joinInfoB, out outerPointA0, out outerPointA1, out outerPointB0, out outerPointB1);

            var v0 = (joinInfoA.p1 - joinInfoA.p0).normalized;
            var v1 = (joinInfoB.p1 - joinInfoB.p0).normalized;
            var rightPerp = new Vector2(-v0.y, v0.x);
            var dot = Vector2.Dot(v1, rightPerp);
            bool turningRight = dot >= 0.0f;

            if (outerPointA1 != outerPointB0)
            {
                float radius = lineWidth / 2.0f;

                var perp = outerPointB0 - outerPointA1;
                perp = new Vector2(perp.y, -perp.x).normalized * (radius + k_EdgeBuffer);
                if (!turningRight)
                    perp = -perp;

                var outerPointA1Perp = outerPointA1 + perp;
                var outerPointB0Perp = outerPointB0 + perp;

                var p0 = new Vector3(joinInfoA.p1.x, joinInfoA.p1.y, Vertex.nearZ);
                var p1 = new Vector3(outerPointA1.x, outerPointA1.y, Vertex.nearZ);
                var p2 = new Vector3(outerPointA1Perp.x, outerPointA1Perp.y, Vertex.nearZ);
                var p3 = new Vector3(outerPointB0Perp.x, outerPointB0Perp.y, Vertex.nearZ);
                var p4 = new Vector3(outerPointB0.x, outerPointB0.y, Vertex.nearZ);

                float invRadius = 1.0f / radius;
                var ratioP1 = (p1 - p0) * invRadius;
                var ratioP2 = (p2 - p0) * invRadius;
                var ratioP3 = (p3 - p0) * invRadius;
                var ratioP4 = (p4 - p0) * invRadius;

                var circle0 = new Vector4(0.0f, 0.0f, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                var circle1 = new Vector4(ratioP1.x, ratioP1.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                var circle2 = new Vector4(ratioP2.x, ratioP2.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                var circle3 = new Vector4(ratioP3.x, ratioP3.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
                var circle4 = new Vector4(ratioP4.x, ratioP4.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);

                mwd.SetNextVertex(new Vertex() { position = p0, tint = strokeColor, flags = s_ArcFlags, circle = circle0 });
                mwd.SetNextVertex(new Vertex() { position = p1, tint = strokeColor, flags = s_ArcFlags, circle = circle1 });
                mwd.SetNextVertex(new Vertex() { position = p2, tint = strokeColor, flags = s_ArcFlags, circle = circle2 });
                mwd.SetNextVertex(new Vertex() { position = p3, tint = strokeColor, flags = s_ArcFlags, circle = circle3 });
                mwd.SetNextVertex(new Vertex() { position = p4, tint = strokeColor, flags = s_ArcFlags, circle = circle4 });

                EmitTriangleCW(mwd, vertexCount, vertexCount + 1, vertexCount + 2);
                EmitTriangleCW(mwd, vertexCount, vertexCount + 2, vertexCount + 3);
                EmitTriangleCW(mwd, vertexCount, vertexCount + 3, vertexCount + 4);
            }
            else
            {
                // Make degenerate triangles
                var p = new Vector3(joinInfoA.p1.x, joinInfoA.p1.y, Vertex.nearZ);
                var vert = new Vertex() { position = p, tint = Color.clear };
                for (int i = 0; i < 5; ++i)
                    mwd.SetNextVertex(vert);
                for (int i = 0; i < 9; ++i)
                    mwd.SetNextIndex((UInt16)vertexCount);
            }

            vertexCount += kExpectedVertices;
            indexCount += kExpectedIndices;
        }

        private void ConnectInnerJoins(MeshWriteData mwd, ref JoinInfo joinInfoA, ref JoinInfo joinInfoB)
        {
            if (joinInfoA.useTangent || joinInfoB.useTangent)
                return;

            bool turningRight = IsJoinTurningRight(ref joinInfoA, ref joinInfoB);

            // Look if we need to move the inner vertices to avoid mesh overlap between the two sub-paths
            int innerIndexA0 = turningRight ? joinInfoA.rightPointIndex0 : joinInfoA.leftPointIndex0;
            int innerIndexA1 = turningRight ? joinInfoA.rightPointIndex1 : joinInfoA.leftPointIndex1;
            int innerIndexA2 = turningRight ? joinInfoA.rightPointIndex2 : joinInfoA.leftPointIndex2;
            int innerIndexB0 = turningRight ? joinInfoB.rightPointIndex0 : joinInfoB.leftPointIndex0;
            int innerIndexB1 = turningRight ? joinInfoB.rightPointIndex1 : joinInfoB.leftPointIndex1;
            int innerIndexB2 = turningRight ? joinInfoB.rightPointIndex2 : joinInfoB.leftPointIndex2;

            var verts = mwd.m_Vertices;
            var innerPointA0 = verts[innerIndexA0].position;
            var innerPointA1 = verts[innerIndexA1].position;
            var innerPointB0 = verts[innerIndexB0].position;
            var innerPointB1 = verts[innerIndexB1].position;

            var innerPoint = Tessellation.IntersectLines(innerPointA0, innerPointA1, innerPointB0, innerPointB1);
            if (float.IsNaN(innerPoint.x))
                return; // Lines are parallel

            // Check if the intersection is further than the length of the segments
            float t = TFromPointOnSegment(innerPoint, innerPointA0, innerPointA1);
            if (t < 0.0f || t > 1.0f)
                return;

            t = TFromPointOnSegment(innerPoint, innerPointB0, innerPointB1);
            if (t < 0.0f || t > 1.0f)
                return;

            // Okay, it's safe to move the inner vertices
            var vertA0 = verts[innerIndexA0];
            var vertA1 = verts[innerIndexA1];
            var vertA2 = verts[innerIndexA2];
            vertA1.circle = Tessellation.GetInterpolatedCircle(innerPoint, ref vertA0, ref vertA1, ref vertA2);
            vertA1.position = innerPoint;
            verts[innerIndexA1] = vertA1;

            var vertB0 = verts[innerIndexB0];
            var vertB1 = verts[innerIndexB1];
            var vertB2 = verts[innerIndexB2];
            vertB0.circle = Tessellation.GetInterpolatedCircle(innerPoint, ref vertB0, ref vertB1, ref vertB2);
            vertB0.position = innerPoint;
            verts[innerIndexB0] = vertB0;
        }

        internal float TFromPointOnSegment(Vector2 p, Vector2 p0, Vector2 p1)
        {
            var v = (p1 - p0);
            var w = (p - p0);

            float vLength = v.magnitude;
            float wLength = w.magnitude;

            if (vLength <= UIRUtility.k_Epsilon)
                return 0.0f;

            if (vLength > UIRUtility.k_Epsilon) v /= vLength;
            else v = Vector2.zero;

            if (wLength > UIRUtility.k_Epsilon) w /= wLength;
            else w = Vector2.zero;

            float t = wLength / vLength;
            if (Vector2.Dot(v, w) < 0.0f)
                return -t;
            return t;
        }

        private void GetOuterJoinPoints(MeshWriteData mwd, ref JoinInfo joinInfoA, ref JoinInfo joinInfoB, out Vector2 outerPointA0, out Vector2 outerPointA1, out Vector2 outerPointB0, out Vector2 outerPointB1)
        {
            bool turningRight = IsJoinTurningRight(ref joinInfoA, ref joinInfoB);

            if (joinInfoA.useTangent || joinInfoB.useTangent)
            {
                var tA = joinInfoA.tangent;
                var tB = joinInfoB.tangent;
                var perpA = new Vector2(-tA.y, tA.x);
                var perpB = new Vector2(-tB.y, tB.x);
                if (turningRight)
                {
                    perpA = -perpA;
                    perpB = -perpB;
                }

                float halfStrokeWidth = lineWidth * 0.5f;
                perpA *= halfStrokeWidth;
                perpB *= halfStrokeWidth;

                outerPointA0 = (joinInfoA.p1 - tA) + perpA;
                outerPointA1 = joinInfoA.p1 + perpA;
                outerPointB0 = joinInfoB.p0 + perpB;
                outerPointB1 = (joinInfoB.p0 + tB) + perpB;
            }
            else
            {
                int outerIndexA0 = turningRight ? joinInfoA.leftPointIndex0 : joinInfoA.rightPointIndex0;
                int outerIndexA1 = turningRight ? joinInfoA.leftPointIndex1 : joinInfoA.rightPointIndex1;
                int outerIndexB0 = turningRight ? joinInfoB.leftPointIndex0 : joinInfoB.rightPointIndex0;
                int outerIndexB1 = turningRight ? joinInfoB.leftPointIndex1 : joinInfoB.rightPointIndex1;

                var verts = mwd.m_Vertices;
                outerPointA0 = (Vector2)verts[outerIndexA0].position;
                outerPointA1 = (Vector2)verts[outerIndexA1].position;
                outerPointB0 = (Vector2)verts[outerIndexB0].position;
                outerPointB1 = (Vector2)verts[outerIndexB1].position;
            }
        }

        internal bool IsJoinTurningRight(ref JoinInfo joinInfoA, ref JoinInfo joinInfoB)
        {
            var v = (joinInfoA.p1 - joinInfoA.p0);
            var w = (joinInfoB.p1 - joinInfoB.p0);
            float crossZ = v.x * w.y - v.y * w.x;
            return crossZ >= 0.0f;
        }

        private void StrokeCap(MeshWriteData mwd, ref JoinInfo joinInfo, bool isStartingCap, ref int vertexCount, ref int indexCount)
        {
            switch (lineCap)
            {
                case LineCap.Butt:
                    // No antialiasing for now on  chopped caps
                    break;
                case LineCap.Round:
                    StrokeRoundedCap(mwd, ref joinInfo, isStartingCap, ref vertexCount, ref indexCount);
                    break;
                default: break;
            }
        }

        private void StrokeRoundedCap(MeshWriteData mwd, ref JoinInfo joinInfo, bool isStartingCap, ref int vertexCount, ref int indexCount)
        {
            if (mwd == null)
            {
                vertexCount += 5;
                indexCount += 9;
                return;
            }

            float radius = lineWidth * 0.5f;
            float dist = radius + k_EdgeBuffer;

            var p0 = Vector2.zero;
            var p1 = Vector2.zero;
            var p2 = Vector2.zero;
            var p3 = Vector2.zero;
            var p4 = Vector2.zero;

            if (joinInfo.useTangent)
            {
                var tan = joinInfo.tangent.normalized;
                if (isStartingCap)
                    tan = -tan;

                var nRight = tan;
                nRight = new Vector2(-nRight.y, nRight.x);
                nRight *= dist;
                tan *= dist;

                p0 = isStartingCap ? joinInfo.p0 : joinInfo.p1;
                p1 = p0 + nRight;
                p2 = p0 + nRight + tan;
                p3 = p0 - nRight + tan;
                p4 = p0 - nRight;
            }
            else
            {
                var verts = mwd.m_Vertices;
                var leftPoint0 = (Vector2)verts[joinInfo.leftPointIndex0].position;
                var leftPoint1 = (Vector2)verts[joinInfo.leftPointIndex1].position;
                var rightPoint0 = (Vector2)verts[joinInfo.rightPointIndex0].position;
                var rightPoint1 = (Vector2)verts[joinInfo.rightPointIndex1].position;

                var nLeft = (leftPoint1 - leftPoint0).normalized * dist;
                var nRight = (rightPoint1 - rightPoint0).normalized * dist;

                p0 = isStartingCap ? joinInfo.p0 : joinInfo.p1;
                p1 = isStartingCap ? leftPoint0 : leftPoint1;
                p2 = isStartingCap ? leftPoint0 - nLeft : leftPoint1 + nLeft;
                p3 = isStartingCap ? rightPoint0 - nRight : rightPoint1 + nRight;
                p4 = isStartingCap ? rightPoint0 : rightPoint1;
            }

            float invRadius = 1.0f / radius;
            var ratioP1 = (p1 - p0) * invRadius;
            var ratioP2 = (p2 - p0) * invRadius;
            var ratioP3 = (p3 - p0) * invRadius;
            var ratioP4 = (p4 - p0) * invRadius;

            var circle0 = new Vector4(0.0f, 0.0f, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circle1 = new Vector4(ratioP1.x, ratioP1.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circle2 = new Vector4(ratioP2.x, ratioP2.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circle3 = new Vector4(ratioP3.x, ratioP3.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circle4 = new Vector4(ratioP4.x, ratioP4.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);

            mwd.SetNextVertex(new Vertex() { position = new Vector3(p0.x, p0.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circle0 });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(p1.x, p1.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circle1 });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(p2.x, p2.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circle2 });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(p3.x, p3.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circle3 });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(p4.x, p4.y, Vertex.nearZ), tint = strokeColor, flags = s_ArcFlags, circle = circle4 });

            EmitTriangleCW(mwd, vertexCount, vertexCount + 1, vertexCount + 2);
            EmitTriangleCW(mwd, vertexCount, vertexCount + 2, vertexCount + 3);
            EmitTriangleCW(mwd, vertexCount, vertexCount + 3, vertexCount + 4);

            vertexCount += 5;
            indexCount += 9;
        }

        internal struct FillingArc
        {
            public Vector2 p0;
            public Vector2 pMid;
            public Vector2 p1;
            public Vector2 p2; // 3rd vertex of the triangle (not shared on the arc)
            public Vector2 center;
            public float radius;
            public bool isArc; // false for a straight segment
            public bool isConcave;
            public bool isContourBreaker; // true when starting a new contour
        }

        struct ContourInfo
        {
            public NativeArray<Vector2> verts;
            public int vertexCount;
        }

        struct IndexPair
        {
            public IndexPair(int f, int s) { first = f; second = s; }
            public int first;
            public int second;
        }

        private List<FillingArc> m_FillingArcs = new List<FillingArc>(64);
        private List<BezierArcSegment> m_FilledBezierArcs = new List<BezierArcSegment>(64);
        private List<IndexPair> m_ArcIndicesPerVertex = new List<IndexPair>(64);

        private void RecordArcAtVertexIndex(int arcIndex, int vertexIndex)
        {
            Debug.Assert(vertexIndex >= 0);
            while (vertexIndex >= m_ArcIndicesPerVertex.Count)
                m_ArcIndicesPerVertex.Add(new IndexPair(-1, -1));

            var pair = m_ArcIndicesPerVertex[vertexIndex];
            if (pair.first == arcIndex || pair.second == arcIndex)
                return; // Already recorded

            if (pair.first == -1)
                pair.first = arcIndex;
            else if (pair.second == -1)
                pair.second = arcIndex;
            m_ArcIndicesPerVertex[vertexIndex] = pair;
        }

        private void PopulateContoursWithArcMappings(List<ContourInfo> contours)
        {
            contours.Clear();

            int arcCount = m_FillingArcs.Count;
            if (arcCount == 0)
                return;

            m_ArcIndicesPerVertex.Clear();

            var currentContour = new ContourInfo() { verts = new NativeArray<Vector2>(m_FillingArcs.Count * 2, Allocator.Temp) };

            int nextVertexIndex = 0;
            int totalVertexIndex = 0;
            int contourStartArcIndex = 0;
            int contourStartVertexIndex = 0;
            bool atContourStart = true;
            for (int i = 0; i < arcCount; ++i)
            {
                var arc = m_FillingArcs[i];

                if (arc.isContourBreaker)
                {
                    if (currentContour.vertexCount > 0)
                        contours.Add(currentContour);
                    contourStartArcIndex = i + 1;
                    contourStartVertexIndex = totalVertexIndex;
                    atContourStart = true;
                    nextVertexIndex = 0;
                    currentContour = new ContourInfo() { verts = new NativeArray<Vector2>(m_FillingArcs.Count * 2, Allocator.Temp) };
                    continue;
                }

                if (atContourStart)
                {
                    RecordArcAtVertexIndex(i, totalVertexIndex++);
                    currentContour.verts[nextVertexIndex++] = arc.p0;
                    currentContour.vertexCount = nextVertexIndex;
                    atContourStart = false;
                }

                RecordArcAtVertexIndex(i, totalVertexIndex - 1);

                int nextI = i + 1;
                bool isClosingContour = nextI >= arcCount || m_FillingArcs[nextI].isContourBreaker;
                if (isClosingContour && AlmostEq(m_FillingArcs[i].p1, m_FillingArcs[contourStartArcIndex].p0))
                {
                    // Closing the contour
                    RecordArcAtVertexIndex(i, contourStartVertexIndex);
                }
                else
                {
                    RecordArcAtVertexIndex(i, totalVertexIndex++);
                    currentContour.verts[nextVertexIndex++] = arc.p1;
                    currentContour.vertexCount = nextVertexIndex;
                }
            }

            // Add the last contour
            contours.Add(currentContour);
        }

        private void GenerateFilledArcs()
        {
            m_FillingArcs.Clear();

            int entryCount = m_SubPathEntries.Count;
            for (int i = 0; i < entryCount; ++i)
            {
                var entry = m_SubPathEntries[i];
                switch (entry.type)
                {
                    case EntryType.Line:
                        GenerateLineFillArc(ref entry);
                        break;
                    case EntryType.Arc:
                        GenerateFillArc(ref entry);
                        break;
                    case EntryType.ArcTo:
                        GenerateFillArcTo(ref entry);
                        break;
                    case EntryType.Bezier:
                        GenerateBezierFillArcs(ref entry);
                        break;
                    case EntryType.Move:
                        // Insert contour breaker
                        if (m_FillingArcs.Count > 0 && !m_FillingArcs[m_FillingArcs.Count-1].isContourBreaker)
                            m_FillingArcs.Add(new FillingArc() { isContourBreaker = true });
                        break;
                    default: break;
                }
            }

            // Subdivide arcs when the ratio is higher than 10% (completely empiric value, nothing scientific)
            int arcCount = m_FillingArcs.Count;
            for (int i = 0; i < arcCount; ++i)
            {
                var arc = m_FillingArcs[i];
                if (!arc.isArc)
                    continue;

                var v = (arc.p1 - arc.p0);
                var mid = (arc.p0 + arc.p1) * 0.5f;
                float arcWidth = v.magnitude;
                float arcHeight = arc.radius - (mid - arc.center).magnitude;
                float heightRatio = arcHeight / arcWidth;

                if (heightRatio >= 0.1f || arcCount == 1)
                {
                    FillingArc arc0, arc1;
                    SplitArc(arc, out arc0, out arc1);

                    m_FillingArcs[i] = arc0;
                    m_FillingArcs.Insert(i + 1, arc1);
                    ++i;
                }
            }
        }

        internal static void SplitArc(FillingArc arc, out FillingArc arc0, out FillingArc arc1)
        {
            // This splitting method only works for sweep angle < pi/2
            var mid0 = (arc.p0 + arc.pMid) * 0.5f;
            var mid1 = (arc.p1 + arc.pMid) * 0.5f;
            var pMid0 = mid0 + (mid0 - arc.center).normalized * arc.radius;
            var pMid1 = mid1 + (mid1 - arc.center).normalized * arc.radius;
            arc0 = new FillingArc() { p0 = arc.p0, p1 = arc.pMid, pMid = pMid0, center = arc.center, radius = arc.radius, isArc = arc.isArc };
            arc1 = new FillingArc() { p0 = arc.pMid, p1 = arc.p1, pMid = pMid1, center = arc.center, radius = arc.radius, isArc = arc.isArc };
        }

        internal static void SplitArcMinSweep(FillingArc arc, float startAngle, float endAngle, bool antiClockwise, float minSweep, List<FillingArc> arcs)
        {
            if (Mathf.Abs(startAngle - endAngle) <= Tessellation.kEpsilon)
                return;

            float sweep = SweepAngle(startAngle, endAngle, antiClockwise);
            if (sweep <= minSweep)
            {
                arcs.Add(arc);
                return;
            }

            float sweepDelta = sweep * 0.5f;
            if (antiClockwise)
                sweepDelta = -sweepDelta;

            var q = Quaternion.Euler(0.0f, 0.0f, sweepDelta * Mathf.Rad2Deg);
            var qMid = Quaternion.Euler(0.0f, 0.0f, (sweepDelta * 0.5f) * Mathf.Rad2Deg);

            var p0 = arc.p0;
            var v = (p0 - arc.center).normalized;
            var p1 = arc.center + (Vector2)(q * v) * arc.radius;
            var pMid = arc.center + (Vector2)(qMid * v) * arc.radius;

            float newEndAngle = startAngle + sweepDelta;

            var subArc = new FillingArc() { p0 = p0, p1 = p1, pMid = pMid, center = arc.center, radius = arc.radius, isArc = arc.isArc };
            SplitArcMinSweep(subArc, startAngle, newEndAngle, antiClockwise, minSweep, arcs);

            p0 = p1;
            v = (p0 - arc.center).normalized;
            p1 = arc.center + (Vector2)(q * v) * arc.radius;
            pMid = arc.center + (Vector2)(qMid * v) * arc.radius;

            float newStartAngle = newEndAngle;

            subArc = new FillingArc() { p0 = p0, p1 = p1, pMid = pMid, center = arc.center, radius = arc.radius, isArc = arc.isArc };
            SplitArcMinSweep(subArc, newStartAngle, endAngle, antiClockwise, minSweep, arcs);
        }

        private void GenerateLineFillArc(ref SubPathEntry entry)
        {
            var mid = (entry.p0 + entry.p1) * 0.5f;
            m_FillingArcs.Add(new FillingArc() { p0 = entry.p0, pMid = mid, p1 = entry.p1, isArc = false });
        }

        private void GenerateFillArc(ref SubPathEntry arcEntry)
        {
            Vector2 p0, p1, pMid;
            EvalArcPositions(ref arcEntry, out p0, out p1, out pMid);

            Vector2 center;
            float startAngle, endAngle, radius;
            bool antiClockwise;
            GetArcData(arcEntry, out center, out startAngle, out endAngle, out radius, out antiClockwise);

            int prevCount = m_FillingArcs.Count;

            var arc = new FillingArc() { p0 = p0, pMid = pMid, p1 = p1, radius = radius, center = center, isArc = true };
            SplitArcMinSweep(arc, startAngle, endAngle, antiClockwise, Mathf.PI / 2, m_FillingArcs);

            if ((m_FillingArcs.Count - prevCount) == 1)
            {
                // Only one arc was added, we'll split it to avoid sending only two vertices to libtess
                var lastArc = m_FillingArcs[m_FillingArcs.Count - 1];

                FillingArc arc0, arc1;
                SplitArc(lastArc, out arc0, out arc1);

                m_FillingArcs[m_FillingArcs.Count - 1] = arc0;
                m_FillingArcs.Add(arc1);
            }
        }

        private void GenerateFillArcTo(ref SubPathEntry arcToEntry)
        {
            SubPathEntry arcEntry;
            if (!ConvertArcToEntryToArcEntry(ref arcToEntry, out arcEntry))
                return;

            GenerateFillArc(ref arcEntry);
        }

        private void GenerateBezierFillArcs(ref SubPathEntry entry)
        {
            m_FilledBezierArcs.Clear();
            SubdivideBezierIntoArcs(ref entry, m_FilledBezierArcs);

            foreach (var ba in m_FilledBezierArcs)
                m_FillingArcs.Add(new FillingArc() { p0 = ba.p0, pMid = ba.pMid, p1 = ba.p1, center = ba.center, radius = ba.radius, isArc = ba.isArc });
        }

        private List<Vector2> m_LibTessVertices = new List<Vector2>(64);
        private List<int> m_LibTessIndices = new List<int>(64);
        private List<int> m_LibTessVertexIndices = new List<int>(64);
        private List<ContourInfo> m_LibTessContours = new List<ContourInfo>(8);

        private unsafe void TessellateFillWithArcMappings(FillRule fillRule, List<Vector2> vertices, List<int> indices, List<int> vertexIndices, List<ContourInfo> contours)
        {
            PopulateContoursWithArcMappings(contours);

            var tess = UITessellation.BeginTess();

            try
            {
                foreach (var c in contours)
                    UITessellation.AddContour(tess, c.verts.GetUnsafePtr(), c.vertexCount, sizeof(Vector2));

                if (UITessellation.Tessellate(tess, fillRule == FillRule.OddEven))
                {
                    var tessVertCount = UITessellation.GetVertexCount(tess);
                    var tessVerts = UITessellation.GetVertices(tess);
                    var tessElementCount = UITessellation.GetElementCount(tess);
                    var tessElements = UITessellation.GetElements(tess);
                    var tessVertIndices = UITessellation.GetVertexIndices(tess);

                    for (int i = 0; i < tessVertCount; ++i)
                    {
                        var x = tessVerts[i * 2];
                        var y = tessVerts[i * 2 + 1];
                        vertices.Add(new Vector2(x, y));
                    }
                    for (int i = 0; i < tessElementCount; ++i)
                    {
                        indices.Add(tessElements[i * 3]);
                        indices.Add(tessElements[i * 3 + 1]);
                        indices.Add(tessElements[i * 3 + 2]);
                    }
                    for (int i = 0; i < tessVertCount; ++i)
                    {
                        vertexIndices.Add(tessVertIndices[i]);
                    }
                }
            }
            finally
            {
                UITessellation.EndTess(tess);

                foreach (var c in contours)
                    c.verts.Dispose();
            }
        }

        struct TriangleInfo
        {
            public Vector2 p0;
            public Vector2 p1;
            public Vector2 p2;
            public FillingArc arc; // Arc joining p0 and p1, p2 is used to complete the triangle
            public bool isConcaveFilled;
            public bool isFilled;
        }

        private List<int> m_MatchedArcs = new List<int>(64);
        private List<TriangleInfo> m_TriangleInfo = new List<TriangleInfo>(256);

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        bool AlmostEq(Vector2 p, Vector2 q)
        {
            return Mathf.Abs(p.x - q.x) <= Tessellation.kEpsilon &&
                Mathf.Abs(p.y - q.y) <= Tessellation.kEpsilon;
        }

        private void GenerateFilledMesh(List<Vector2> vertices, List<int> indices, List<int> vertexIndices)
        {
            m_TriangleInfo.Clear();

            int vertexCount = 0;
            int indexCount = 0;

            for (int i = 0; i < indices.Count; i += 3)
            {
                // Find matching pair to locate edge arc
                m_MatchedArcs.Clear();

                var i0 = indices[i];
                var i1 = indices[i + 1];
                var i2 = indices[i + 2];

                var v0 = vertices[i0];
                var v1 = vertices[i1];
                var v2 = vertices[i2];

                var vi0 = vertexIndices[i0];
                var vi1 = vertexIndices[i1];
                var vi2 = vertexIndices[i2];

                var pair0 = vi0 >= 0 ? m_ArcIndicesPerVertex[vi0] : new IndexPair(-1, -1);
                var pair1 = vi1 >= 0 ? m_ArcIndicesPerVertex[vi1] : new IndexPair(-1, -1);
                var pair2 = vi2 >= 0 ? m_ArcIndicesPerVertex[vi2] : new IndexPair(-1, -1);

                if (pair0.first != -1 && (pair0.first == pair1.first || pair0.first == pair1.second))
                    m_MatchedArcs.Add(pair0.first);
                if (pair0.second != -1 && (pair0.second == pair1.first || pair0.second == pair1.second))
                    m_MatchedArcs.Add(pair0.second);

                if (pair1.first != -1 && (pair1.first == pair2.first || pair1.first == pair2.second))
                    m_MatchedArcs.Add(pair1.first);
                if (pair1.second != -1 && (pair1.second == pair2.first || pair1.second == pair2.second))
                    m_MatchedArcs.Add(pair1.second);

                if (pair2.first != -1 && (pair2.first == pair0.first || pair2.first == pair0.second))
                    m_MatchedArcs.Add(pair2.first);
                if (pair2.second != -1 && (pair2.second == pair0.first || pair2.second == pair0.second))
                    m_MatchedArcs.Add(pair2.second);

                // Check if we have more than one concave arc for a given triangle
                int concaveArcCount = 0;
                foreach (var arcIndex in m_MatchedArcs)
                {
                    var arc = m_FillingArcs[arcIndex];

                    // Determine the vertex that completes the triangle (not on the arc)
                    var p2 = Vector2.zero;
                    if ((AlmostEq(arc.p0, v0) && AlmostEq(arc.p1, v1)) || (AlmostEq(arc.p0, v1) && AlmostEq(arc.p1, v0)))
                        p2 = v2;
                    else if ((AlmostEq(arc.p0, v1) && AlmostEq(arc.p1, v2)) || (AlmostEq(arc.p0, v2) && AlmostEq(arc.p1, v1)))
                        p2 = v0;
                    else
                        p2 = v1;

                    bool isConcave = false;
                    if (arc.isArc)
                    {
                        var mid = (arc.p0 + arc.p1) * 0.5f;
                        isConcave = Vector2.Dot((arc.center - mid), (p2 - mid)) <= 0.0f;
                        if (isConcave)
                            ++concaveArcCount;
                    }
                    else
                    {
                        isConcave = true;
                        ++concaveArcCount; // Straight lines are processed as concave
                    }

                    arc.isConcave = isConcave;
                    arc.p2 = p2;
                    m_FillingArcs[arcIndex] = arc;
                }

                // Cannot have more than one concave arc per triangle, so we'll have to split this triangle.
                if (concaveArcCount == 1)
                {
                    foreach (var arcIndex in m_MatchedArcs)
                    {
                        var arc = m_FillingArcs[arcIndex];
                        if (!arc.isConcave)
                            continue;

                        m_TriangleInfo.Add(new TriangleInfo() { p0 = arc.p0, p1 = arc.p1, p2 = arc.p2, arc = arc, isConcaveFilled = true });
                        vertexCount += 3;
                        indexCount += 3;
                        break;
                    }
                }
                else if (concaveArcCount == 2)
                {
                    TriangleInfo a, b;
                    SplitDoubleConcaveTriangle(v0, v1, v2, out a, out b);
                    m_TriangleInfo.Add(a);
                    m_TriangleInfo.Add(b);
                    vertexCount += 6;
                    indexCount += 6;
                }

                foreach (var arcIndex in m_MatchedArcs)
                {
                    var arc = m_FillingArcs[arcIndex];
                    if (!arc.isConcave)
                    {
                        m_TriangleInfo.Add(new TriangleInfo() { p0 = arc.p0, p1 = arc.p1, p2 = arc.p2, arc = arc, isConcaveFilled = false });
                        vertexCount += 4;
                        indexCount += 6;
                    }
                }

                if (concaveArcCount == 0)
                {
                    m_TriangleInfo.Add(new TriangleInfo() { p0 = v0, p1 = v1, p2 = v2, isFilled = true });
                    vertexCount += 3;
                    indexCount += 3;
                }
            }

            var mwd = m_Ctx.Allocate(vertexCount, indexCount);
            vertexCount = 0;
            indexCount = 0;

            foreach (var tri in m_TriangleInfo)
            {
                var arc = tri.arc;
                if (tri.isFilled)
                    FillTriangle(mwd, tri, ref vertexCount, ref indexCount);
                else if (tri.isConcaveFilled)
                {
                    if (tri.arc.isArc)
                        FillConcaveArc(mwd, tri, ref vertexCount, ref indexCount);
                    else
                        FillConcaveLine(mwd, tri, ref vertexCount, ref indexCount);
                }
                else
                    ExtrudeFillArc(mwd, tri, ref vertexCount, ref indexCount);
            }
        }

        unsafe private void SplitDoubleConcaveTriangle(Vector2 v0, Vector2 v1, Vector2 v2, out TriangleInfo a, out TriangleInfo b)
        {
            var usedEdge = stackalloc int[2];
            var arcs = stackalloc FillingArc[2];

            int concaveIndex = 0;
            foreach (var arcIndex in m_MatchedArcs)
            {
                var arc = m_FillingArcs[arcIndex];
                if (!arc.isConcave)
                    continue;

                if ((v0 == arc.p0 && v1 == arc.p1) || (v0 == arc.p1 && v1 == arc.p0))
                    usedEdge[concaveIndex] = 0; // v0-v1
                else if ((v0 == arc.p0 && v2 == arc.p1) || (v0 == arc.p1 && v2 == arc.p0))
                    usedEdge[concaveIndex] = 1; // v0-v2
                else
                    usedEdge[concaveIndex] = 2; // v1-v2

                arcs[concaveIndex] = arc;

                ++concaveIndex;
            }

            // Identify the unused edge, this is the one that will be split in half
            if ((usedEdge[0] == 0 && usedEdge[1] == 1) || usedEdge[0] == 1 && usedEdge[1] == 0)
            {
                // Split v1-v2
                var mid = (v1 + v2) * 0.5f;
                a = new TriangleInfo() {
                    p0 = v0,
                    p1 = usedEdge[0] == 0 ? v1 : v2,
                    p2 = mid,
                    arc = arcs[0],
                    isConcaveFilled = true
                };
                b = new TriangleInfo() {
                    p0 = v0,
                    p1 = usedEdge[1] == 0 ? v1 : v2,
                    p2 = mid,
                    arc = arcs[1],
                    isConcaveFilled = true
                };
            }
            else if ((usedEdge[0] == 0 && usedEdge[1] == 2) || (usedEdge[0] == 2 && usedEdge[1] == 0))
            {
                // Split v0-v2
                var mid = (v0 + v2) * 0.5f;
                a = new TriangleInfo() {
                    p0 = v1,
                    p1 = usedEdge[0] == 0 ? v0 : v2,
                    p2 = mid,
                    arc = arcs[0],
                    isConcaveFilled = true
                };
                b = new TriangleInfo() {
                    p0 = v1,
                    p1 = usedEdge[1] == 0 ? v0 : v2,
                    p2 = mid,
                    arc = arcs[1],
                    isConcaveFilled = true
                };
            }
            else
            {
                // Split v0-v1
                var mid = (v0 + v1) * 0.5f;
                a = new TriangleInfo() {
                    p0 = v2,
                    p1 = usedEdge[0] == 1 ? v0 : v1,
                    p2 = mid,
                    arc = arcs[0],
                    isConcaveFilled = true
                };
                b = new TriangleInfo() {
                    p0 = v2,
                    p1 = usedEdge[1] == 1 ? v0 : v1,
                    p2 = mid,
                    arc = arcs[1],
                    isConcaveFilled = true
                };
            }
        }

        private void FillTriangle(MeshWriteData mwd, TriangleInfo tri, ref int vertexCount, ref int indexCount)
        {
            // Filled triangle
            mwd.SetNextVertex(new Vertex() { position = new Vector3(tri.p0.x, tri.p0.y, Vertex.nearZ), tint = fillColor });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(tri.p1.x, tri.p1.y, Vertex.nearZ), tint = fillColor });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(tri.p2.x, tri.p2.y, Vertex.nearZ), tint = fillColor });
            EmitTriangleCW(mwd, vertexCount + 0, vertexCount + 1, vertexCount + 2);

            vertexCount += 3;
            indexCount += 3;
        }

        private void FillConcaveArc(MeshWriteData mwd, TriangleInfo tri, ref int vertexCount, ref int indexCount)
        {
            var arc = tri.arc;
            var p0 = tri.p0;
            var p1 = tri.p1;
            var p2 = tri.p2;

            // Concave arc digging inside the triangle
            float invRadius = 1.0f / arc.radius;
            var innerRatio0 = (p0 - arc.center) * invRadius;
            var innerRatio1 = (p1 - arc.center) * invRadius;
            var innerRatio2 = (p2 - arc.center) * invRadius;

            var circle0 = new Vector4(Tessellation.kUnusedArc, Tessellation.kUnusedArc, innerRatio0.x, innerRatio0.y);
            var circle1 = new Vector4(Tessellation.kUnusedArc, Tessellation.kUnusedArc, innerRatio1.x, innerRatio1.y);
            var circle2 = new Vector4(Tessellation.kUnusedArc, Tessellation.kUnusedArc, innerRatio2.x, innerRatio2.y);

            mwd.SetNextVertex(new Vertex() { position = new Vector3(p0.x, p0.y, Vertex.nearZ), tint = fillColor, flags = s_ArcFlags, circle = circle0 });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(p1.x, p1.y, Vertex.nearZ), tint = fillColor, flags = s_ArcFlags, circle = circle1 });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(p2.x, p2.y, Vertex.nearZ), tint = fillColor, flags = s_ArcFlags, circle = circle2 });
            EmitTriangleCW(mwd, vertexCount + 0, vertexCount + 1, vertexCount + 2);

            vertexCount += 3;
            indexCount += 3;
        }

        private void FillConcaveLine(MeshWriteData mwd, TriangleInfo tri, ref int vertexCount, ref int indexCount)
        {
            var arc = tri.arc;
            var p0 = tri.p0;
            var p1 = tri.p1;
            var p2 = tri.p2;

            var circle0 = new Vector4(0.0f, 1.0f, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circle1 = new Vector4(0.0f, 1.0f, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circle2 = new Vector4(0.0f, 0.0f, Tessellation.kUnusedArc, Tessellation.kUnusedArc);

            mwd.SetNextVertex(new Vertex() { position = new Vector3(p0.x, p0.y, Vertex.nearZ), tint = fillColor, flags = s_ArcFlags, circle = circle0 });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(p1.x, p1.y, Vertex.nearZ), tint = fillColor, flags = s_ArcFlags, circle = circle1 });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(p2.x, p2.y, Vertex.nearZ), tint = fillColor, flags = s_ArcFlags, circle = circle2 });
            EmitTriangleCW(mwd, vertexCount + 0, vertexCount + 1, vertexCount + 2);

            vertexCount += 3;
            indexCount += 3;
        }

        private void ExtrudeFillArc(MeshWriteData mwd, TriangleInfo tri, ref int vertexCount, ref int indexCount)
        {
            var arc = tri.arc;

            var p0 = arc.p0;
            var p1 = arc.p1;
            var mid = (p0 + p1) * 0.5f;
            var midCenter = (mid - arc.center);
            var arcHeight = arc.radius - midCenter.magnitude;

            var perp = (p1 - p0).normalized;
            perp = new Vector2(perp.y, -perp.x);
            if (Vector2.Dot(perp, midCenter) < 0.0f)
                perp = -perp;

            var p2 = p1 + perp * (arcHeight + 2.0f);
            var p3 = p0 + perp * (arcHeight + 2.0f);

            float invRadius = 1.0f / arc.radius;
            var outerRatio0 = (p0 - arc.center) * invRadius;
            var outerRatio1 = (p1 - arc.center) * invRadius;
            var outerRatio2 = (p2 - arc.center) * invRadius;
            var outerRatio3 = (p3 - arc.center) * invRadius;

            var circle0 = new Vector4(outerRatio0.x, outerRatio0.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circle1 = new Vector4(outerRatio1.x, outerRatio1.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circle2 = new Vector4(outerRatio2.x, outerRatio2.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);
            var circle3 = new Vector4(outerRatio3.x, outerRatio3.y, Tessellation.kUnusedArc, Tessellation.kUnusedArc);

            mwd.SetNextVertex(new Vertex() { position = new Vector3(p0.x, p0.y, Vertex.nearZ), tint = fillColor, flags = s_ArcFlags, circle = circle0 });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(p1.x, p1.y, Vertex.nearZ), tint = fillColor, flags = s_ArcFlags, circle = circle1 });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(p2.x, p2.y, Vertex.nearZ), tint = fillColor, flags = s_ArcFlags, circle = circle2 });
            mwd.SetNextVertex(new Vertex() { position = new Vector3(p3.x, p3.y, Vertex.nearZ), tint = fillColor, flags = s_ArcFlags, circle = circle3 });
            EmitTriangleCW(mwd, vertexCount + 0, vertexCount + 1, vertexCount + 2);
            EmitTriangleCW(mwd, vertexCount + 2, vertexCount + 0, vertexCount + 3);

            vertexCount += 4;
            indexCount += 6;
        }

        private void EmitTriangleCW(MeshWriteData mwd, int i, int j, int k)
        {
            var verts = mwd.m_Vertices;
            var v0 = verts[i].position;
            var v1 = verts[j].position;
            var v2 = verts[k].position;
            var v = (v1 - v0);
            var w = (v2 - v0);
            float crossZ = v.x * w.y - v.y * w.x;
            if (crossZ >= 0.0f)
            {
                mwd.SetNextIndex((UInt16)i);
                mwd.SetNextIndex((UInt16)j);
                mwd.SetNextIndex((UInt16)k);
            }
            else
            {
                mwd.SetNextIndex((UInt16)i);
                mwd.SetNextIndex((UInt16)k);
                mwd.SetNextIndex((UInt16)j);
            }
        }
    }
}
