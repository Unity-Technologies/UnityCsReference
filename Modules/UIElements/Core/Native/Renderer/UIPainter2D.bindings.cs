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
    [NativeHeader("Modules/UIElements/Core/Native/Renderer/UIPainter2D.bindings.h")]
    internal static class UIPainter2D
    {
        public static extern IntPtr Create(bool computeBBox = false);
        public static extern void Destroy(IntPtr handle);

        public static extern void Reset(IntPtr handle);

        public static extern float GetLineWidth(IntPtr handle);
        public static extern void SetLineWidth(IntPtr handle, float value);

        public static extern Color GetStrokeColor(IntPtr handle);
        public static extern void SetStrokeColor(IntPtr handle, Color value);

        [NativeName("GetStrokeGradientCopy")]
        public static extern Gradient GetStrokeGradient(IntPtr handle);
        public static extern void SetStrokeGradient(IntPtr handle, Gradient gradient);

        public static extern FillGradient GetFillGradient(IntPtr handle);
        public static extern void SetFillGradient(IntPtr handle, FillGradient gradient);
        public static extern bool HasFillGradient(IntPtr handle);

        public static extern void SetStrokeFillGradient(IntPtr handle, FillGradient gradient);
        public static extern bool HasStrokeFillGradient(IntPtr handle);

        public static extern void SetHasFillTexture(IntPtr handle, bool hasFillTexture);
        public static extern bool HasFillTexture(IntPtr handle);

        internal static extern Matrix4x4 GetFillTransform(IntPtr handle);
        internal static extern void SetFillTransform(IntPtr handle, Matrix4x4 fillTransform);

        internal static extern float GetOpacity(IntPtr handle);
        internal static extern void SetOpacity(IntPtr handle, float opacity);

        public static extern Color GetFillColor(IntPtr handle);
        public static extern void SetFillColor(IntPtr handle, Color value);

        public static extern LineJoin GetLineJoin(IntPtr handle);
        public static extern void SetLineJoin(IntPtr handle, LineJoin value);

        public static extern LineCap GetLineCap(IntPtr handle);
        public static extern void SetLineCap(IntPtr handle, LineCap value);

        public static extern float GetMiterLimit(IntPtr handle);
        public static extern void SetMiterLimit(IntPtr handle, float value);

        public static extern void SetDashPattern(IntPtr handle, ReadOnlySpan<float> value);
        public static extern void SetDashGapPattern(IntPtr handle, float dash, float gap);

        public static extern float GetDashOffset(IntPtr handle);
        public static extern void SetDashOffset(IntPtr handle, float value);

        public static extern void BeginPath(IntPtr handle);
        public static extern void MoveTo(IntPtr handle, Vector2 pos);
        public static extern void LineTo(IntPtr handle, Vector2 pos);
        public static extern void ArcTo(IntPtr handle, Vector2 p1, Vector2 p2, float radius);
        public static extern void Arc(IntPtr handle, Vector2 center, float radius, float startAngleRads, float endAngleRads, ArcDirection direction);
        public static extern void BezierCurveTo(IntPtr handle, Vector2 p1, Vector2 p2, Vector2 p3);
        public static extern void QuadraticCurveTo(IntPtr handle, Vector2 p1, Vector2 p2);
        public static extern void ClosePath(IntPtr handle);

        public static extern void PushClip(IntPtr handle);
        public static extern void PopClip(IntPtr handle);
        public static extern int GetClipCount(IntPtr handle);

        public static extern Rect GetBBox(IntPtr handle);

        public static extern MeshWriteDataInterface Stroke(IntPtr handle, bool isDetached);
        public static extern MeshWriteDataInterface Fill(IntPtr handle, FillRule fillRule);

        public static extern int TakeStrokeSnapshot(IntPtr handle);
        public static extern int TakeFillSnapshot(IntPtr handle, FillRule fillRule);
        public static extern void ClearSnapshots(IntPtr handle);

        [NativeMethod(IsThreadSafe = true)] public static extern MeshWriteDataInterface ExecuteSnapshotFromJob(IntPtr painterHandle, int i);
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

    /// <summary>
    /// Describes a fill gradient used for rendering filled shapes in <see cref="Painter2D"/>.
    /// The start, end , center, focus, and radius properties are pixel coordinate relative to the painter's coordinate system.
    /// </summary>
    /// <remarks>
    /// This struct encapsulates the data required to define a gradient fill, including the gradient itself,
    /// the type of gradient (linear or radial), and the parameters that control the direction or focus of the gradient.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct FillGradient
    {
        /// <summary>The color gradient used for the fill.</summary>
        public Gradient gradient { get; set; }

        /// <summary>The type of gradient to use (linear or radial).</summary>
        public GradientType gradientType { get; set; }

        /// <summary>Specifies how the gradient is sampled when UV coordinates are outside the [0, 1] range.</summary>
        public AddressMode addressMode { get; set; }

        /// <summary>The start point for a linear gradient.</summary>
        public Vector2 start { get; set; }

        /// <summary>The end point for a linear gradient.</summary>
        public Vector2 end { get; set; }

        /// <summary>The center point for a radial gradient.</summary>
        public Vector2 center { get; set; }

        /// <summary>The Focus point for radial gradient.</summary>
        public Vector2 focus { get; set; }

        /// <summary>The radius for a radial gradient.</summary>
        public float radius { get; set; }

        /// <summary>
        /// Helper method to create a linear gradient fill, using startColor and endColor to define the gradient.
        /// </summary>
        /// <param name="startColor"></param>
        /// <param name="endColor"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="addressMode"></param>
        static public FillGradient MakeLinearGradient(
            Color startColor,
            Color endColor,
            Vector2 start,
            Vector2 end,
            AddressMode addressMode = AddressMode.Clamp)
        {
            Gradient gradient = new Gradient()
            {
                colorKeys = new GradientColorKey[] {
                        new GradientColorKey() { color = startColor, time = 0.0f },
                        new GradientColorKey() { color = endColor, time = 1.0f }
                    }
            };

            return MakeLinearGradient(gradient, 
            start,
            end,
            addressMode);
        }

        /// <summary>
        /// Helper method to create a linear gradient fill.
        /// </summary>
        /// <param name="gradient"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="addressMode"></param>
        static public FillGradient MakeLinearGradient(
           Gradient gradient,
           Vector2 start,
           Vector2 end,
           AddressMode addressMode = AddressMode.Clamp)
        {
            FillGradient fillGradient = new FillGradient();
            fillGradient.gradient = gradient;
            fillGradient.gradientType = GradientType.Linear;
            fillGradient.addressMode = addressMode;
            fillGradient.start = start;
            fillGradient.end = end;
            fillGradient.center = Vector2.zero;
            fillGradient.focus = Vector2.zero;
            fillGradient.radius = 0f;
            return fillGradient;
        }

        /// <summary>
        /// Helper method to create a radial gradient fill, using startColor and endColor to define the gradient.
        /// </summary>
        /// <param name="startColor"></param>
        /// <param name="endColor"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="focus"></param>
        /// <param name="addressMode"></param>
        static public FillGradient MakeRadialGradient(
            Color startColor,
            Color endColor,
            Vector2 center,
            float radius,
            Vector2 focus,
            AddressMode addressMode = AddressMode.Clamp)
        {
            Gradient gradient = new Gradient()
            {
                colorKeys = new GradientColorKey[] {
                        new GradientColorKey() { color = startColor, time = 0.0f },
                        new GradientColorKey() { color = endColor, time = 1.0f }
                    }
            };

            return MakeRadialGradient(gradient, center, radius, focus, addressMode);
        }


        /// <summary>
        /// Helper method to create a radial gradient fill.
        /// </summary>
        /// <param name="gradient"></param>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <param name="focus"></param>
        /// <param name="addressMode"></param>
        static public FillGradient MakeRadialGradient(
            Gradient gradient,
            Vector2 center,
            float radius,
            Vector2 focus,
            AddressMode addressMode = AddressMode.Clamp)
        {
            FillGradient fillGradient = new FillGradient();
            fillGradient.gradient = gradient;
            fillGradient.gradientType = GradientType.Radial;
            fillGradient.addressMode = addressMode;
            fillGradient.start = Vector2.zero;
            fillGradient.end = Vector2.zero;
            fillGradient.center = center;
            fillGradient.focus = focus;
            fillGradient.radius = radius;
            return fillGradient;
        }
    }
}
