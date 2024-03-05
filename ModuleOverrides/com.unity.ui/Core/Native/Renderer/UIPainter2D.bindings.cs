// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    [NativeHeader("ModuleOverrides/com.unity.ui/Core/Native/Renderer/UIPainter2D.bindings.h")]
    internal static class UIPainter2D
    {
        public static extern IntPtr Create(float maxArcRadius);
        public static extern void Destroy(IntPtr handle);

        public static extern void Reset(IntPtr handle);

        public static extern float GetLineWidth(IntPtr handle);
        public static extern void SetLineWidth(IntPtr handle, float value);

        public static extern Color GetStrokeColor(IntPtr handle);
        public static extern void SetStrokeColor(IntPtr handle, Color value);

        [NativeName("GetStrokeGradientCopy")]
        public static extern Gradient GetStrokeGradient(IntPtr handle);
        public static extern void SetStrokeGradient(IntPtr handle, Gradient gradient);

        public static extern Color GetFillColor(IntPtr handle);
        public static extern void SetFillColor(IntPtr handle, Color value);

        public static extern LineJoin GetLineJoin(IntPtr handle);
        public static extern void SetLineJoin(IntPtr handle, LineJoin value);

        public static extern LineCap GetLineCap(IntPtr handle);
        public static extern void SetLineCap(IntPtr handle, LineCap value);

        public static extern float GetMiterLimit(IntPtr handle);
        public static extern void SetMiterLimit(IntPtr handle, float value);

        public static extern void BeginPath(IntPtr handle);
        public static extern void MoveTo(IntPtr handle, Vector2 pos);
        public static extern void LineTo(IntPtr handle, Vector2 pos);
        public static extern void ArcTo(IntPtr handle, Vector2 p1, Vector2 p2, float radius);
        public static extern void Arc(IntPtr handle, Vector2 center, float radius, float startAngleRads, float endAngleRads, ArcDirection direction);
        public static extern void BezierCurveTo(IntPtr handle, Vector2 p1, Vector2 p2, Vector2 p3);
        public static extern void QuadraticCurveTo(IntPtr handle, Vector2 p1, Vector2 p2);
        public static extern void ClosePath(IntPtr handle);

        public static extern MeshWriteDataInterface Stroke(IntPtr handle);
        public static extern MeshWriteDataInterface Fill(IntPtr handle, FillRule fillRule);

        public static extern Rect ComputeBBoxFromArcs(IntPtr meshes, int meshCount);
    }

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
}
