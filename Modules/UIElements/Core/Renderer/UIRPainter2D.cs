// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine.Profiling;
using UnityEngine.UIElements.UIR;
using UnityEngine.Bindings;

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
        static readonly MemoryLabel k_MemoryLabel = new (nameof(UIElements), $"Renderer.{nameof(Painter2D)}");

        private MeshGenerationContext m_Ctx;
        internal DetachedAllocator m_DetachedAllocator;
        internal SafeHandleAccess m_Handle;

        // Cached values mirroring native state. These must be reset in Reset() to stay
        // in sync with the native side.
        FillGradient m_CachedFillGradient;
        Texture2D m_CachedFillTexture;
        FillGradient m_CachedStrokeFillGradient;
        List<float> m_CachedDashPattern = new(); // To enable alloc-free getting of the pattern

        internal bool isDetached => m_DetachedAllocator != null;

        List<Painter2DJobData> m_JobSnapshots = null;
        List<VectorImage> m_VectorImageToRelease = null;
        NativeArray<Painter2DJobData> m_JobParameters;

        // Instantiated internally by UIR, users shouldn't derive from this class.
        internal Painter2D(MeshGenerationContext ctx)
        {
            m_Handle = new SafeHandleAccess(UIPainter2D.Create());
            m_Ctx = ctx;
            m_JobSnapshots = new(32);
            m_VectorImageToRelease = new(16);
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
            m_CachedFillGradient = default;
            m_CachedFillTexture = null;
            m_CachedStrokeFillGradient = default;
            m_CachedDashPattern.Clear();
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
                if (m_VectorImageToRelease != null)
                {
                    foreach (var vi in m_VectorImageToRelease)
                    {
                        if (vi != null)
                        {
                            UIRUtility.Destroy(vi.atlas);
                            UIRUtility.Destroy(vi);
                        }
                    }
                    m_VectorImageToRelease.Clear();
                }
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

        internal Matrix4x4 fillTransform
        {
            get => UIPainter2D.GetFillTransform(m_Handle);
            [VisibleToOtherModules("UnityEngine.VectorGraphicsModule")]
            set => UIPainter2D.SetFillTransform(m_Handle, value);
        }

        internal float opacity
        {
            [VisibleToOtherModules("UnityEngine.VectorGraphicsModule")]
            set => UIPainter2D.SetOpacity(m_Handle, value);
            get => UIPainter2D.GetOpacity(m_Handle);
        }

        /// <summary>
        /// The fill gradient to use when using. <see cref="Fill"/>.
        /// </summary>
        /// <remarks>
        /// Setting a fill gradient will override the currently set <see cref="fillColor"/>.
        /// </remarks>
        public FillGradient fillGradient
        {
            set
            {
                m_CachedFillGradient = value;
                UIPainter2D.SetFillGradient(m_Handle, value);
            }
            get => m_CachedFillGradient;
        }

        /// <summary>
        /// The stroke fill gradient to use when using. <see cref="Stroke"/>.
        /// </summary>
        /// <remarks>
        /// Setting a stroke fill gradient will override the currently set <see cref="strokeColor"/>.
        /// </remarks>
        public FillGradient strokeFillGradient
        {
            set
            {
                m_CachedStrokeFillGradient = value;
                UIPainter2D.SetStrokeFillGradient(m_Handle, value);
            }
            get => m_CachedStrokeFillGradient;
        }

        private bool hasStrokeFillGradient
        {
            get => UIPainter2D.HasStrokeFillGradient(m_Handle);
        }

        private bool hasFillGradient
        {
            get => UIPainter2D.HasFillGradient(m_Handle);
        }

        private bool hasFillTexture
        {
            get => UIPainter2D.HasFillTexture(m_Handle);
        }

        /// <summary>
        /// Texture2D to use when filling paths using <see cref="Fill"/>.
        /// </summary>
        public Texture2D fillTexture
        {
            set
            {
                m_CachedFillTexture = value;
                UIPainter2D.SetHasFillTexture(m_Handle, value != null);
            }
            get => m_CachedFillTexture;
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

        /// <summary>
        /// Sets the dash pattern to use when drawing paths using <see cref="Stroke"/>.
        /// </summary>
        [Obsolete("Use SetDashPattern(ReadOnlySpan<float>) and GetDashPattern(Span<float>) instead.", false)]
        public ReadOnlySpan<float> dashPattern
        {
            set => SetDashPattern(value);
        }

        /// <summary>
        /// Sets the dash pattern to use when drawing paths using <see cref="Stroke"/>.
        /// Use <see cref="GetDashPattern"/> to retrieve the current pattern.
        /// </summary>
        /// <param name="pattern">The dash pattern values to set.</param>
        public void SetDashPattern(ReadOnlySpan<float> pattern)
        {
            NoAllocHelpers.EnsureListElemCount(m_CachedDashPattern, pattern.Length);
            pattern.CopyTo(NoAllocHelpers.CreateSpan(m_CachedDashPattern));
            UIPainter2D.SetDashPattern(m_Handle, pattern);
        }

        /// <summary>
        /// Copies the current dash pattern into the provided span.
        /// </summary>
        /// <param name="values">
        /// The destination span. Must have a length greater than or equal to the current dash pattern length.
        /// Pass an empty span to query the required length without copying.
        /// </param>
        /// <returns>
        /// The number of elements in the dash pattern. If <paramref name="values"/> is too short to hold the
        /// pattern, returns 0 and no data is copied.
        /// </returns>
        public int GetDashPattern(Span<float> values)
        {
            int count = m_CachedDashPattern.Count;

            if (values.Length == 0)
                return count;

            if (values.Length < count)
            {
                Debug.LogError($"GetDashPattern: provided span length ({values.Length}) is too small to hold the dash pattern ({count} elements).");
                return 0;
            }

            NoAllocHelpers.CreateReadOnlySpan(m_CachedDashPattern).CopyTo(values);
            return count;
        }

        /// <summary>
        /// Sets a simple dash-gap pattern to use when drawing paths using <see cref="Stroke"/>.
        /// </summary>
        public void SetDashPattern(float dash, float gap)
        {
            NoAllocHelpers.EnsureListElemCount(m_CachedDashPattern, 2);
            var span = NoAllocHelpers.CreateSpan(m_CachedDashPattern);
            span[0] = dash;
            span[1] = gap;
            UIPainter2D.SetDashGapPattern(m_Handle, dash, gap);
        }

        /// <summary>
        /// The offset to the first dash to use when drawing paths using <see cref="Stroke"/>.
        /// </summary>
        public float dashOffset
        {
            get => UIPainter2D.GetDashOffset(m_Handle);
            set => UIPainter2D.SetDashOffset(m_Handle, value);
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

        private static readonly ProfilerMarker s_StrokeMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, "Painter2D.Stroke");

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
                    var meshData = UIPainter2D.Stroke(m_Handle, true);
                    if (meshData.vertexCount == 0)
                        return;

                    // transfer all data in a single batch
                    var meshWrite = Allocate(meshData.vertexCount, meshData.indexCount);

                    if (hasStrokeFillGradient)
                    {
                        m_DetachedAllocator.AddGradient(m_CachedStrokeFillGradient);
                    }

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
                    IntPtr vectorImagePtr = IntPtr.Zero;

                    if (hasStrokeFillGradient)
                    {
                        VectorImage vi = ScriptableObject.CreateInstance<VectorImage>();
                        vi.hideFlags = HideFlags.HideAndDontSave;
                        m_VectorImageToRelease.Add(vi);
                        CreateTextureAndGradientSettings(ref m_CachedStrokeFillGradient, out Texture2D texture, out GradientSettings gradientSettings);
                        vi.atlas = texture;
                        vi.settings = new GradientSettings[] { gradientSettings };
                        vectorImagePtr = m_Ctx.renderData.parent.renderTree.m_GCHandlePool.GetIntPtr(vi);
                    }

                    // Take a snapshot for the job system
                    m_Ctx.InsertUnsafeMeshGenerationNode(out var unsafeNode);
                    int snapshotIndex = UIPainter2D.TakeStrokeSnapshot(m_Handle);
                    m_JobSnapshots.Add(new Painter2DJobData() { node = unsafeNode, snapshotIndex = snapshotIndex, vectorImagePtr = vectorImagePtr, texturePtr = IntPtr.Zero });
                }
            }
        }

        private static readonly ProfilerMarker s_FillMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, "Painter2D.Fill");

        private static void SetSolidTextureData(Texture2D targetTexture, Color color, int width, int height)
        {
            NativeArray<Color32> texels = targetTexture.GetRawTextureData<Color32>();
            var textureWidth = targetTexture.width;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                    texels[x + y * textureWidth] = color;
            }
        }

        private static void SetGradientTextureData(Texture2D texture, int width, int x, int y, Gradient gradient, bool duplicateOnBorder = false)
        {
            NativeArray<Color32> texels = texture.GetRawTextureData<Color32>();
            float scale = 1.0f / (float)(Math.Max(1, width - 1));
            for (int i = 0; i < width; i++)
            {
                float t = i * scale;
                Color color = gradient.Evaluate(t);
                texels[x + i + y * texture.width] = color;

                if (duplicateOnBorder)
                {
                    if (i == 0)
                    {
                        texels[x + width + y * texture.width] = color;
                    }
                    else if (i == width - 1)
                    {
                        texels[x - 1 + y * texture.width] = color;
                    }

                    texels[x + i + (y + 1) * texture.width] = color;
                    texels[x + i + (y - 1) * texture.width] = color;
                }
            }
        }

        private static void SetupSolidColor(int width, int height, int x, int y, out GradientSettings gradientSettings)
        {
            gradientSettings = new GradientSettings();
            gradientSettings.gradientType = GradientType.Linear;
            gradientSettings.addressMode = AddressMode.Clamp;
            gradientSettings.location.x = x;
            gradientSettings.location.y = y;
            gradientSettings.location.width = width;
            gradientSettings.location.height = height;
            gradientSettings.radialFocus = Vector2.zero;
        }

        private static void SetupGradient(ref FillGradient fillGradient, int width, int x, int y, out GradientSettings gradientSettings)
        {
            gradientSettings = new GradientSettings();
            gradientSettings.gradientType = fillGradient.gradientType;
            gradientSettings.addressMode = fillGradient.addressMode;
            gradientSettings.location.x = x;
            gradientSettings.location.y = y;
            gradientSettings.location.width = width;
            gradientSettings.location.height = 1;

            var focus = fillGradient.radius > UIRUtility.k_Epsilon ? (fillGradient.focus - fillGradient.center) / fillGradient.radius : Vector2.zero;
            gradientSettings.radialFocus = focus;
        }

        private static void SetupGradientForTexture(int width, int height, int x, int y, out GradientSettings gradientSettings)
        {
            gradientSettings = new GradientSettings();
            gradientSettings.gradientType = GradientType.Linear;
            gradientSettings.addressMode = AddressMode.Clamp;
            gradientSettings.location.x = x;
            gradientSettings.location.y = y;
            gradientSettings.location.width = width;
            gradientSettings.location.height = height;
            gradientSettings.radialFocus = Vector2.zero;
        }

        private static void CreateTextureAndGradientSettings(ref FillGradient fillGradient, out Texture2D texture, out GradientSettings gradientSettings)
        {
            const int width = 64;
            texture = new Texture2D(width, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            SetGradientTextureData(texture, width, 0, 0, fillGradient.gradient);
            texture.Apply(false, true);
            SetupGradient(ref fillGradient, width, 0, 0, out gradientSettings);
        }

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

                    if (hasFillGradient)
                    {
                        m_DetachedAllocator.AddGradient(m_CachedFillGradient);
                    }

                    if (hasFillTexture)
                    {
                        m_DetachedAllocator.AddTexture(m_CachedFillTexture);
                    }

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
                    IntPtr vectorImagePtr = IntPtr.Zero;
                    IntPtr texturePtr = IntPtr.Zero;

                    if (hasFillGradient)
                    {
                        VectorImage vi = ScriptableObject.CreateInstance<VectorImage>();
                        vi.hideFlags = HideFlags.HideAndDontSave;
                        m_VectorImageToRelease.Add(vi);
                        CreateTextureAndGradientSettings(ref m_CachedFillGradient, out Texture2D texture, out GradientSettings gradientSettings);
                        vi.atlas = texture;
                        vi.settings = new GradientSettings[] { gradientSettings };
                        vectorImagePtr = m_Ctx.renderData.parent.renderTree.m_GCHandlePool.GetIntPtr(vi);
                    }

                    if (hasFillTexture)
                    {
                        texturePtr = m_Ctx.renderData.parent.renderTree.m_GCHandlePool.GetIntPtr(m_CachedFillTexture);
                    }

                    // Take a snapshot for the job system
                    m_Ctx.InsertUnsafeMeshGenerationNode(out var unsafeNode);
                    int jobIndex = UIPainter2D.TakeFillSnapshot(m_Handle, fillRule);
                    m_JobSnapshots.Add(new Painter2DJobData() { node = unsafeNode, snapshotIndex = jobIndex, vectorImagePtr = vectorImagePtr, texturePtr = texturePtr });
                }
            }
        }

        private static readonly ProfilerMarker s_ClipMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, "Painter2D.Clip");

        /// <summary>
        /// Uses the current path as a clipping region. This region clips subsequent `Fill()` and `Stroke()` operations.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The clipping process is very expensive, as it involves per-triangle clipping on the CPU. It is recommended to minimize
        /// the complexity of both the clipping path and the clipped content. Also avoid nested clipping regions when possible,
        /// as each additional clipping region adds more overhead to the clipping process.
        /// </para>
        /// <para>
        /// Note that the clipped shapes will not have antialiased edges since the clipping path is applied by doing a per-triangle
        /// clipping process. Use PopClip() to remove the most recently added clipping region.
        /// </para>
        /// </remarks>
        public void PushClip()
        {
            ValidateState();

            using (s_ClipMarker.Auto())
            {
                UIPainter2D.PushClip(m_Handle);
            }
        }

        /// <summary>
        /// Removes the most recently added clipping region.
        /// </summary>
        public void PopClip()
        {
            ValidateState();

            if (UIPainter2D.GetClipCount(m_Handle) == 0)
            {
                Debug.LogError("PopClip() called without a matching PushClip().");
                return;
            }

            UIPainter2D.PopClip(m_Handle);
        }

        struct Painter2DJobData
        {
            public UnsafeMeshGenerationNode node;
            public int snapshotIndex;
            public IntPtr vectorImagePtr;
            public IntPtr texturePtr;
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

                if (data.vectorImagePtr != IntPtr.Zero)
                {
                    GCHandle handle = GCHandle.FromIntPtr(data.vectorImagePtr);
                    VectorImage vi = (VectorImage)handle.Target;
                    data.node.DrawGradientsInternal(vertices, indices, vi);
                }
                else if (data.texturePtr != IntPtr.Zero)
                {
                    GCHandle handle = GCHandle.FromIntPtr(data.texturePtr);
                    Texture texture = handle.Target as Texture;
                    data.node.DrawMesh(vertices, indices, texture);
                }
                else
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
                m_JobParameters = new NativeArray<Painter2DJobData>(snapshotCount, k_MemoryLabel, NativeArrayOptions.UninitializedMemory);
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
        public bool SaveToVectorImage(VectorImage vectorImage)
        {
            return SaveToVectorImage(vectorImage, Rect.zero);
        }

        [VisibleToOtherModules("UnityEngine.VectorGraphicsModule")]
        internal unsafe bool SaveToVectorImage(VectorImage vectorImage, Rect viewport)
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

            int padding = 1;
            UIRAtlasAllocator atlasAllocator = new UIRAtlasAllocator(64, 4096);
            List<RectInt> textureAllocations = new List<RectInt>();
            List<int> gradientMeshIndex = new List<int>();

            bool hasRect = viewport != Rect.zero;

            if (m_DetachedAllocator.HasGradientsOrTextures())
            {
                // Reserve the first gradient entry to a white texture for solid geometry
                atlasAllocator.TryAllocate(1 + 2 * padding, 1 + 2 * padding, out RectInt location);
                textureAllocations.Add(location);
            }

            for (int meshIndex = 0; meshIndex < meshes.Count; ++meshIndex)
            {
                var mwd = meshes[meshIndex];
                var verts = mwd.m_Vertices;

                bool hasGradient = false;
                if (m_DetachedAllocator.HasGradientAtMeshIndex(meshIndex))
                {
                    if (!atlasAllocator.TryAllocate(64 + 2 * padding, 1 + 2 * padding, out RectInt location))
                    {
                        Debug.LogError("SaveToVectorImage cannot save VectorImage since texture atlas has no space left.");
                        return false;
                    }
                    textureAllocations.Add(location);
                    gradientMeshIndex.Add(meshIndex);
                    hasGradient = true;
                }

                if (m_DetachedAllocator.HasTextureAtMeshIndex(meshIndex))
                {
                    Texture texture = m_DetachedAllocator.GetTextureFromMeshIndex(meshIndex);
                    if (!atlasAllocator.TryAllocate(texture.width, texture.height, out RectInt location))
                    {
                        Debug.LogError("SaveToVectorImage cannot save VectorImage since texture atlas has no space left.");
                        return false;
                    }
                    textureAllocations.Add(location);
                    gradientMeshIndex.Add(-1);
                    hasGradient = true;
                }

                for (int i = 0; i < verts.Length; ++i)
                {
                    var v = verts[i];
                    var p = v.position;

                    if (!hasRect)
                    {
                        // No viewport: offset the content to the origin
                        p.x -= bbox.x;
                        p.y -= bbox.y;
                    }

                    allVerts[vCount++] = new VectorImageVertex() {
                        position = new Vector3(p.x, p.y, Vertex.nearZ),
                        tint = v.tint,
                        uv = v.uv,
                        flags = v.flags,
                        settingIndex = hasGradient ? (UInt32)(textureAllocations.Count - 1) : 0,
                        circle = v.circle
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
            vectorImage.size = hasRect ? viewport.size : bbox.size;

            if (textureAllocations.Count > 0)
            {
                RenderTexture atlasTexture = new RenderTexture(atlasAllocator.physicalWidth, atlasAllocator.physicalHeight, 0, RenderTextureFormat.ARGB32);
                List<GradientSettings> gradientSettingsList = new List<GradientSettings>(textureAllocations.Count);

                for (int i = 0; i < textureAllocations.Count; ++i)
                {
                    var r = textureAllocations[i];
                    int gradIndex = (i > 0) ? gradientMeshIndex[i-1] : -1;

                    Texture2D tempTexture = null;

                    // First gradient is reserved for solid geometry
                    if (i == 0)
                    {
                        tempTexture = new Texture2D(r.width, r.height, TextureFormat.RGBA32, false);
                        SetSolidTextureData(tempTexture, Color.white, r.width, r.height);
                        tempTexture.Apply(false, true);

                        SetupSolidColor(r.width, r.height, r.x + padding, r.y + padding, out GradientSettings gradientSettings);
                        gradientSettingsList.Add(gradientSettings);
                    }
                    else if (gradIndex != -1)
                    {
                        // Fill Gradient
                        FillGradient fillGradient = m_DetachedAllocator.GetGradientFromMeshIndex(gradIndex);

                        tempTexture = new Texture2D(r.width, r.height, TextureFormat.RGBA32, false);
                        SetGradientTextureData(tempTexture, r.width - 2 * padding, padding, padding, fillGradient.gradient, true);
                        tempTexture.Apply(false, true);
                        SetupGradient(ref fillGradient, r.width - 2 * padding, r.x + padding, r.y + padding, out GradientSettings gradientSettings);
                        gradientSettingsList.Add(gradientSettings);
                    }
                    else
                    {
                        RenderTexture.active = atlasTexture;
                        Rect destRect = new Rect(r.x, atlasTexture.height - r.height - r.y, r.width, r.height);
                        Texture texture = m_DetachedAllocator.GetTextureFromMeshIndex(i-1);
                        GL.PushMatrix();
                        GL.LoadPixelMatrix(0, atlasTexture.width, atlasTexture.height, 0);
                        Graphics.DrawTexture(destRect, texture);
                        GL.PopMatrix();
                        RenderTexture.active = null;

                        SetupGradientForTexture(r.width, r.height, r.x, r.y, out GradientSettings gradientSettings);
                        gradientSettingsList.Add(gradientSettings);
                    }

                    if (tempTexture != null)
                    {
                        // Copy temp texture into atlas
                        Graphics.CopyTexture(
                            tempTexture, 0, 0, 0, 0, r.width, r.height,
                            atlasTexture, 0, 0, r.x, r.y);

                        UIRUtility.Destroy(tempTexture);
                    }
                }

                RenderTexture.active = atlasTexture;
                Texture2D atlasTexture2D = new Texture2D(atlasTexture.width, atlasTexture.height, TextureFormat.RGBA32, false);
                atlasTexture2D.ReadPixels(new Rect(0, 0, atlasTexture.width, atlasTexture.height), 0, 0);
                atlasTexture2D.Apply();
                RenderTexture.active = null;
                vectorImage.atlas = atlasTexture2D;
                vectorImage.settings = gradientSettingsList.ToArray();
                atlasTexture.Release();
            }

            return true;
        }
    }
}
