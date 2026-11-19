// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor.TerrainTools
{
    // The kind of area a TerrainRegion describes.
    internal enum TerrainRegionShape
    {
        Whole = 0,
        Rectangle = 1,
        Circle = 2,
        Path = 3,       // a corridor of a given width along a polyline
        Intersection = 4,
    }

    ///<summary>An area of a Terrain that <see cref="TerrainEditUtility" /> edits or measures.</summary>
    ///<remarks>Positions are in meters on the X and Z axes of the Terrain's local space: (0, 0) is the corner at the Terrain's position and (<see cref="TerrainData.size" />.x, <see cref="TerrainData.size" />.z) is the opposite corner. <see cref="Vector2.y" /> and <see cref="Rect.y" /> hold the Z coordinate. A region can extend past the Terrain; only the part on the Terrain is used.
    ///
    ///A region is immutable. Create one with <see cref="Rectangle" />, <see cref="Circle" />, <see cref="Path" /> or <see cref="Intersect" />, or use <see cref="whole" />.</remarks>
    public sealed class TerrainRegion
    {
        // Half the diagonal of a square cell, rounded up: a disc or corridor at least this wide touches every cell it crosses.
        const float k_CoverCellFactor = 0.71f;

        [NoAutoStaticsCleanup] // immutable singleton with no reference to user code
        static readonly TerrainRegion s_Whole = new TerrainRegion(TerrainRegionShape.Whole);

        readonly TerrainRegionShape m_Shape;
        readonly Rect m_Rect;
        readonly Vector2 m_Center;
        readonly float m_Radius;
        readonly Vector2[] m_PathPoints;
        readonly Rect[] m_SegmentBounds;
        readonly Rect m_PathBounds;   // of the polyline, without the width
        readonly float m_PathWidth;
        readonly TerrainRegion m_First;
        readonly TerrainRegion m_Second;

        TerrainRegion(TerrainRegionShape shape)
        {
            m_Shape = shape;
        }

        TerrainRegion(Rect rect) : this(TerrainRegionShape.Rectangle)
        {
            m_Rect = rect;
        }

        TerrainRegion(Vector2 center, float radius) : this(TerrainRegionShape.Circle)
        {
            m_Center = center;
            m_Radius = radius;
        }

        TerrainRegion(Vector2[] pathPoints, float pathWidth) : this(TerrainRegionShape.Path)
        {
            m_PathPoints = pathPoints;
            m_PathWidth = pathWidth;
            m_SegmentBounds = new Rect[pathPoints.Length - 1];
            m_PathBounds = Rect.MinMaxRect(pathPoints[0].x, pathPoints[0].y, pathPoints[0].x, pathPoints[0].y);
            for (int i = 0; i < m_SegmentBounds.Length; i++)
            {
                var a = pathPoints[i];
                var b = pathPoints[i + 1];
                m_SegmentBounds[i] = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                m_PathBounds.min = Vector2.Min(m_PathBounds.min, b);
                m_PathBounds.max = Vector2.Max(m_PathBounds.max, b);
            }
        }

        TerrainRegion(TerrainRegion first, TerrainRegion second) : this(TerrainRegionShape.Intersection)
        {
            m_First = first;
            m_Second = second;
        }

        ///<summary>The whole Terrain.</summary>
        public static TerrainRegion whole => s_Whole;

        ///<summary>Creates a rectangular region.</summary>
        ///<param name="rect">The rectangle in meters. Its width and height must be greater than zero.</param>
        ///<returns>The new region.</returns>
        public static TerrainRegion Rectangle(Rect rect)
        {
            if (!float.IsFinite(rect.xMin) || !float.IsFinite(rect.yMin) || !float.IsFinite(rect.width) || !float.IsFinite(rect.height))
                throw new ArgumentException("The rectangle must have finite coordinates.", nameof(rect));
            if (!(rect.width > 0f) || !(rect.height > 0f))
                throw new ArgumentException($"The rectangle must have a positive width and height; got {rect.width} x {rect.height}.", nameof(rect));
            return new TerrainRegion(rect);
        }

        ///<summary>Creates a disc-shaped region.</summary>
        ///<param name="center">The center of the disc in meters.</param>
        ///<param name="radius">The radius of the disc in meters. Must be greater than zero.</param>
        ///<returns>The new region.</returns>
        public static TerrainRegion Circle(Vector2 center, float radius)
        {
            if (!float.IsFinite(center.x) || !float.IsFinite(center.y))
                throw new ArgumentException("The center must have finite coordinates.", nameof(center));
            if (!(radius > 0f && float.IsFinite(radius)))
                throw new ArgumentOutOfRangeException(nameof(radius), radius, "The radius must be a finite number greater than zero.");
            return new TerrainRegion(center, radius);
        }

        ///<summary>Creates a corridor of a given width along a polyline, such as a road or a river.</summary>
        ///<remarks>A position belongs to the corridor when it lies closer than half of <paramref name="width" /> to any segment of the polyline, so the ends and the bends of the corridor are rounded. Repeated points are allowed.</remarks>
        ///<param name="points">The points of the polyline in meters. At least two. The region keeps its own copy.</param>
        ///<param name="width">The full width of the corridor in meters. Must be greater than zero.</param>
        ///<returns>The new region.</returns>
        public static TerrainRegion Path(ReadOnlySpan<Vector2> points, float width)
        {
            if (points.Length < 2)
                throw new ArgumentException($"A path needs at least two points; got {points.Length}.", nameof(points));
            if (!(width > 0f && float.IsFinite(width)))
                throw new ArgumentOutOfRangeException(nameof(width), width, "The width must be a finite number greater than zero.");
            var copy = points.ToArray();
            for (int i = 0; i < copy.Length; i++)
                if (!float.IsFinite(copy[i].x) || !float.IsFinite(copy[i].y))
                    throw new ArgumentException($"Path point {i} must have finite coordinates.", nameof(points));
            return new TerrainRegion(copy, width);
        }

        ///<summary>Creates the region where two regions overlap.</summary>
        ///<remarks>The weight of the intersection at a position is the smaller of the weights of the two regions there. An edit's blend band around an intersection is measured from the farther of the two regions, so it can reach past the distance it was given near the corners of the overlap.</remarks>
        ///<param name="first">The first region.</param>
        ///<param name="second">The second region.</param>
        ///<returns>The intersection. When one of the regions is <see cref="whole" />, returns the other one.</returns>
        public static TerrainRegion Intersect(TerrainRegion first, TerrainRegion second)
        {
            if (first == null)
                throw new ArgumentNullException(nameof(first));
            if (second == null)
                throw new ArgumentNullException(nameof(second));
            if (first.m_Shape == TerrainRegionShape.Whole)
                return second;
            if (second.m_Shape == TerrainRegionShape.Whole)
                return first;
            return new TerrainRegion(first, second);
        }

        // The parameters of the shape, for tests. Zero or empty when they do not apply.
        internal TerrainRegionShape shape => m_Shape;
        internal Rect rect => m_Rect;
        internal float radius => m_Radius;
        internal float pathWidth => m_PathWidth;

        ///<summary>Checks whether a position lies inside the region.</summary>
        ///<param name="position">The position in meters.</param>
        ///<returns>True when the position lies inside the region.</returns>
        public bool Contains(Vector2 position)
        {
            Evaluate(position, 0f, 0f, out bool inside, out _);
            return inside;
        }

        ///<summary>Gets how strongly an edit of this region applies at a position.</summary>
        ///<remarks>The weight is 1 inside the region and 0 outside it. With a falloff, it fades smoothly (smoothstep) from 1 to 0 towards the edge of the region, over that fraction of the half size of a rectangle, the radius of a disc, or the half width of a corridor.</remarks>
        ///<param name="position">The position in meters.</param>
        ///<param name="falloff">The fraction of the region, from 0 to 1, over which the weight fades at its edge. 0 gives a hard edge.</param>
        ///<returns>The weight, from 0 to 1.</returns>
        public float GetWeight(Vector2 position, float falloff)
        {
            CheckFalloff(falloff);
            return Evaluate(position, falloff, 0f, out _, out _);
        }

        internal static void CheckFalloff(float falloff)
        {
            if (!(falloff >= 0f && falloff <= 1f))
                throw new ArgumentOutOfRangeException(nameof(falloff), falloff, "The falloff must be between 0 and 1.");
        }

        // Weight (with falloff), whether the position is inside, and the Euclidean distance outside the region (0 inside).
        // The distance is exact up to 'maxDistance'; farther positions report at least 'maxDistance'. For an intersection
        // it is the larger of the two distances, a lower bound of the exact one.
        internal float Evaluate(Vector2 p, float falloff, float maxDistance, out bool inside, out float outside)
        {
            switch (m_Shape)
            {
                case TerrainRegionShape.Whole:
                    inside = true;
                    outside = 0f;
                    return 1f;
                case TerrainRegionShape.Rectangle:
                {
                    var r = m_Rect;
                    float dx = Mathf.Max(0f, Mathf.Max(r.xMin - p.x, p.x - r.xMax));
                    float dz = Mathf.Max(0f, Mathf.Max(r.yMin - p.y, p.y - r.yMax));
                    inside = dx == 0f && dz == 0f;
                    if (!inside)
                    {
                        outside = Mathf.Sqrt(dx * dx + dz * dz);
                        return 0f;
                    }
                    outside = 0f;
                    if (falloff <= 0f)
                        return 1f;
                    float ex = Mathf.Min(p.x - r.xMin, r.xMax - p.x) / Mathf.Max(r.width * 0.5f, 1e-6f);
                    float ez = Mathf.Min(p.y - r.yMin, r.yMax - p.y) / Mathf.Max(r.height * 0.5f, 1e-6f);
                    return Mathf.SmoothStep(0f, 1f, Mathf.Min(ex, ez) / falloff);
                }
                case TerrainRegionShape.Circle:
                {
                    float d = Vector2.Distance(p, m_Center);
                    inside = d < m_Radius;
                    outside = Mathf.Max(0f, d - m_Radius);
                    if (!inside)
                        return 0f;
                    return falloff <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, (1f - d / m_Radius) / falloff);
                }
                case TerrainRegionShape.Path:
                {
                    float half = m_PathWidth * 0.5f;
                    float d = DistanceToPolyline(p, half + Mathf.Max(maxDistance, 0f));
                    inside = d < half;
                    outside = Mathf.Max(0f, d - half);
                    if (!inside)
                        return 0f;
                    return falloff <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, (1f - d / half) / falloff);
                }
                default:
                {
                    float w0 = m_First.Evaluate(p, falloff, maxDistance, out bool in0, out float out0);
                    float w1 = m_Second.Evaluate(p, falloff, maxDistance, out bool in1, out float out1);
                    inside = in0 && in1;
                    outside = Mathf.Max(out0, out1);
                    return Mathf.Min(w0, w1);
                }
            }
        }

        // Returns a copy wide enough to touch every grid cell it crosses: a corridor and a disc grow to at least half a
        // cell diagonal, a rectangle to at least one cell, keeping their centers. Returns this when nothing changes.
        internal TerrainRegion ExpandToCoverCells(float cellSize)
        {
            float minimum = cellSize * k_CoverCellFactor;
            switch (m_Shape)
            {
                case TerrainRegionShape.Circle:
                    return m_Radius < minimum ? new TerrainRegion(m_Center, minimum) : this;
                case TerrainRegionShape.Path:
                    return m_PathWidth * 0.5f < minimum ? new TerrainRegion(m_PathPoints, minimum * 2f) : this;
                case TerrainRegionShape.Rectangle:
                {
                    if (m_Rect.width >= cellSize && m_Rect.height >= cellSize)
                        return this;
                    var c = m_Rect.center;
                    float w = Mathf.Max(m_Rect.width, cellSize), h = Mathf.Max(m_Rect.height, cellSize);
                    return new TerrainRegion(new Rect(c.x - w * 0.5f, c.y - h * 0.5f, w, h));
                }
                case TerrainRegionShape.Intersection:
                {
                    var first = m_First.ExpandToCoverCells(cellSize);
                    var second = m_Second.ExpandToCoverCells(cellSize);
                    return ReferenceEquals(first, m_First) && ReferenceEquals(second, m_Second) ? this : new TerrainRegion(first, second);
                }
                default:
                    return this;
            }
        }

        // Bounds in meters, clipped to a terrain of the given size.
        internal Rect GetBounds(Vector3 terrainSize)
        {
            Rect b;
            switch (m_Shape)
            {
                case TerrainRegionShape.Rectangle:
                    b = m_Rect;
                    break;
                case TerrainRegionShape.Circle:
                    b = new Rect(m_Center.x - m_Radius, m_Center.y - m_Radius, m_Radius * 2f, m_Radius * 2f);
                    break;
                case TerrainRegionShape.Path:
                {
                    float half = m_PathWidth * 0.5f;
                    b = Rect.MinMaxRect(m_PathBounds.xMin - half, m_PathBounds.yMin - half, m_PathBounds.xMax + half, m_PathBounds.yMax + half);
                    break;
                }
                case TerrainRegionShape.Intersection:
                {
                    // The overlap of the two bounds; empty (zero size at its minimum corner) when they do not overlap.
                    var a = m_First.GetBounds(terrainSize);
                    var c = m_Second.GetBounds(terrainSize);
                    float x0 = Mathf.Max(a.xMin, c.xMin), z0 = Mathf.Max(a.yMin, c.yMin);
                    return Rect.MinMaxRect(x0, z0, Mathf.Max(Mathf.Min(a.xMax, c.xMax), x0), Mathf.Max(Mathf.Min(a.yMax, c.yMax), z0));
                }
                default:
                    b = new Rect(0f, 0f, terrainSize.x, terrainSize.z);
                    break;
            }
            return Rect.MinMaxRect(
                Mathf.Clamp(b.xMin, 0f, terrainSize.x), Mathf.Clamp(b.yMin, 0f, terrainSize.z),
                Mathf.Clamp(b.xMax, 0f, terrainSize.x), Mathf.Clamp(b.yMax, 0f, terrainSize.z));
        }

        // Grid points covering the region plus 'pad' points per side, for grids whose edge points lie on the terrain
        // boundary (point i at i * size / (resolution - 1)): heightmap samples and alphamap texels.
        internal RectInt GetPointWindow(Vector3 terrainSize, int resolution, int pad)
        {
            var b = GetBounds(terrainSize);
            float sx = (resolution - 1) / Mathf.Max(terrainSize.x, 1e-6f), sz = (resolution - 1) / Mathf.Max(terrainSize.z, 1e-6f);
            int x0 = Mathf.Clamp(Mathf.FloorToInt(b.xMin * sx) - pad, 0, resolution - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt(b.yMin * sz) - pad, 0, resolution - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(b.xMax * sx) + pad, x0, resolution - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt(b.yMax * sz) + pad, z0, resolution - 1);
            return new RectInt(x0, z0, x1 - x0 + 1, z1 - z0 + 1);
        }

        // Distance to the nearest segment, exact below 'limit'; returns 'limit' when every segment is at least that far.
        // A segment whose bounding box is already farther than the best distance so far is skipped.
        float DistanceToPolyline(Vector2 p, float limit)
        {
            float best = limit, best2 = limit * limit;
            for (int i = 0; i < m_SegmentBounds.Length; i++)
            {
                var s = m_SegmentBounds[i];
                float bx = Mathf.Max(0f, Mathf.Max(s.xMin - p.x, p.x - s.xMax));
                float bz = Mathf.Max(0f, Mathf.Max(s.yMin - p.y, p.y - s.yMax));
                if (bx * bx + bz * bz >= best2)
                    continue;
                var a = m_PathPoints[i];
                var ab = m_PathPoints[i + 1] - a;
                float len2 = ab.sqrMagnitude;
                float t = len2 > 1e-12f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
                float d2 = (p - (a + ab * t)).sqrMagnitude;
                if (d2 < best2)
                {
                    best2 = d2;
                    best = Mathf.Sqrt(d2);
                }
            }
            return best;
        }
    }
}
