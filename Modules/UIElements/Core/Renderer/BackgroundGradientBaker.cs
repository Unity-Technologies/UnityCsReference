// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine.Bindings;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    // Bakes a BackgroundGradient into a transient VectorImage that flows through the same
    // GradientSettingsAtlas / VectorImageManager path Painter2D uses. Per-panel instance,
    // owned by RenderTreeManager (mirrors UIRVectorImageManager's shape). Cached by
    // gradient hash; refcounted; unused VIs are destroyed at the end of each ProcessChanges.
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    internal class BackgroundGradientBaker : IDisposable
    {
        internal const int k_AtlasWidth = 64;

        // Keyed by BackgroundGradient itself (not just its hash) so hash collisions on the
        // 32-bit GetHashCode land in different buckets rather than aliasing the cache.
        readonly Dictionary<BackgroundGradient, VectorImage> m_Cache = new();

        sealed class Entry
        {
            public BackgroundGradient sourceKey;
            public int refCount;
        }

        readonly Dictionary<VectorImage, Entry> m_Entries = new();
        readonly HashSet<VectorImage> m_PendingEviction = new();
        readonly List<VectorImage> m_PurgeScratch = new();
        // Indexed by stop count, so cache hits for pixel-stop gradients don't allocate.
        BackgroundGradientStop[][] m_ResolveScratch = new BackgroundGradientStop[5][];
        bool m_Disposed;

        // uvScale multiplies the element's gradient UVs (see MeshGenerator.GradientUVParams.UVAt).
        public VectorImage Bake(in BackgroundGradient source, Vector2 rectSize, out float uvScale)
        {
            uvScale = 1f;
            if (source.IsEmpty())
                return null;

            var gradient = ResolveForElement(source, rectSize, GetResolveScratch(source.stops), out uvScale);
            if (m_Cache.TryGetValue(gradient, out var cached) && cached != null)
            {
                m_PendingEviction.Remove(cached); // rescue: a fresh reference is inbound
                return cached;
            }

            // The cache key must not alias the scratch buffer.
            if (gradient.stops != source.stops)
                gradient.stops = (BackgroundGradientStop[])gradient.stops.Clone();

            var vi = ScriptableObject.CreateInstance<VectorImage>();
            vi.hideFlags = HideFlags.HideAndDontSave;
            vi.name = "BackgroundGradient";
            vi.size = new Vector2(1f, 1f);
            vi.atlas = CreateAtlas(gradient);
            vi.settings = new[]
            {
                new GradientSettings
                {
                    gradientType = gradient.type,
                    addressMode = AddressMode.Clamp,
                    location = new RectInt(0, 0, k_AtlasWidth, 1),
                    radialFocus = Vector2.zero,
                },
            };
            ComputeQuadMesh(gradient, out vi.vertices, out vi.indices);

            m_Cache[gradient] = vi;
            m_Entries[vi] = new Entry { sourceKey = gradient, refCount = 0 };
            return vi;
        }

        internal void AddUser(VectorImage vi)
        {
            if (vi == null || !m_Entries.TryGetValue(vi, out var entry))
                return;
            entry.refCount++;
            m_PendingEviction.Remove(vi);
        }

        internal void RemoveUser(VectorImage vi)
        {
            if (vi == null || !m_Entries.TryGetValue(vi, out var entry))
                return;
            if (entry.refCount > 0)
                entry.refCount--;
            if (entry.refCount == 0)
                m_PendingEviction.Add(vi);
        }

        // Called at the end of each ProcessChanges — after all Reset+Insert cycles have
        // run, so any refcount that hit 0 mid-cycle and was rescued by a later Insert has
        // already been removed from pending.
        internal void PurgePending()
        {
            if (m_PendingEviction.Count == 0)
                return;

            m_PurgeScratch.Clear();
            foreach (var vi in m_PendingEviction)
                m_PurgeScratch.Add(vi);
            m_PendingEviction.Clear();

            foreach (var vi in m_PurgeScratch)
            {
                if (!m_Entries.TryGetValue(vi, out var entry) || entry.refCount > 0)
                    continue; // rescued or cleared out from under us
                m_Cache.Remove(entry.sourceKey);
                m_Entries.Remove(vi);
                if (vi != null)
                    UIRUtility.Destroy(vi); // VectorImage.OnDestroy tears down its atlas
            }
            m_PurgeScratch.Clear();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            foreach (var kv in m_Cache)
            {
                if (kv.Value != null)
                    UIRUtility.Destroy(kv.Value); // VectorImage.OnDestroy tears down its atlas
            }
            m_Cache.Clear();
            m_Entries.Clear();
            m_PendingEviction.Clear();
            m_PurgeScratch.Clear();
            m_Disposed = true;
        }

        // Test-only.
        internal int CacheCount => m_Cache.Count;
        internal int PendingEvictionCount => m_PendingEviction.Count;
        internal int GetRefCount(VectorImage vi)
            => vi != null && m_Entries.TryGetValue(vi, out var e) ? e.refCount : 0;

        internal static class Testing
        {
            public static BackgroundGradient ResolvePixelStops(in BackgroundGradient gradient, Vector2 rectSize)
                => BackgroundGradientBaker.ResolvePixelStops(gradient, rectSize, new BackgroundGradientStop[gradient.stops?.Length ?? 0]);

            public static BackgroundGradient ResolveForElement(in BackgroundGradient gradient, Vector2 rectSize)
                => BackgroundGradientBaker.ResolveForElement(gradient, rectSize, new BackgroundGradientStop[gradient.stops.Length], out _);

            public static Vector2 UVOnElement(in BackgroundGradient gradient, Vector2 rectSize, Vector2 frac)
            {
                BackgroundGradientBaker.ResolveForElement(gradient, rectSize, new BackgroundGradientStop[gradient.stops.Length], out float uvScale);
                return new MeshGenerator.GradientUVParams(gradient, new Rect(Vector2.zero, rectSize), uvScale).UVAt(frac);
            }

            // The color drawn at frac of an element of rectSize, through the same stops and UVs as the renderer.
            public static Color32 SampleLinearOnElement(in BackgroundGradient gradient, Vector2 rectSize, Vector2 frac)
            {
                var baked = BackgroundGradientBaker.ResolveForElement(gradient, rectSize, new BackgroundGradientStop[gradient.stops.Length], out _);
                return SampleStops(baked, UVOnElement(gradient, rectSize, frac).x);
            }

            // Same as SampleLinearOnElement, but read from the baked ramp texels with clamped bilinear filtering, as the GPU does.
            public static Color32 SampleBakedLinearOnElement(in BackgroundGradient gradient, Vector2 rectSize, Vector2 frac)
            {
                var baked = BackgroundGradientBaker.ResolveForElement(gradient, rectSize, new BackgroundGradientStop[gradient.stops.Length], out _);
                float u = Mathf.Clamp01(UVOnElement(gradient, rectSize, frac).x) * (k_AtlasWidth - 1);
                int i = Mathf.Min((int)u, k_AtlasWidth - 2);
                return Color32.Lerp(RampTexel(baked, i), RampTexel(baked, i + 1), u - i);
            }
        }

        BackgroundGradientStop[] GetResolveScratch(BackgroundGradientStop[] stops)
        {
            int count = stops?.Length ?? 0;
            if (count >= m_ResolveScratch.Length)
                Array.Resize(ref m_ResolveScratch, count + 1);
            return m_ResolveScratch[count] ??= new BackgroundGradientStop[count];
        }

        // Converts pixel and auto stops into fractions of the gradient line for rectSize. Returns the gradient
        // unchanged when it has neither; otherwise its stops are written to (and alias) scratch.
        static BackgroundGradient ResolvePixelStops(in BackgroundGradient gradient, Vector2 rectSize, BackgroundGradientStop[] scratch)
        {
            var stops = gradient.stops;
            if (!NeedsResolve(stops))
                return gradient;

            // Whole pixels, so subpixel resizes keep hitting the same cache entry.
            float length = Mathf.Round(GradientLength(gradient, rectSize));
            int lastResolved = -1;
            for (int i = 0; i < stops.Length; ++i)
            {
                var stop = stops[i];
                if (stop.positionIsAuto && lastResolved >= 0 && i < stops.Length - 1)
                    continue;

                float position;
                if (stop.positionIsAuto)
                    position = i == 0 ? 0f : 1f; // as the parser anchors an unpositioned first or last stop
                else
                    position = stop.positionIsPercent ? stop.position : (length > 0f ? stop.position / length : 0f);
                // CSS moves a stop placed before its predecessor up to it before spreading the auto stops in between.
                if (lastResolved >= 0)
                    position = Mathf.Max(position, scratch[lastResolved].position);
                scratch[i] = BackgroundGradientStop.Percent(stop.color, position);
                for (int j = lastResolved + 1; j < i; ++j)
                {
                    float t = (float)(j - lastResolved) / (i - lastResolved);
                    scratch[j] = BackgroundGradientStop.Percent(stops[j].color, Mathf.Lerp(scratch[lastResolved].position, position, t));
                }
                lastResolved = i;
            }

            var resolved = gradient;
            resolved.stops = scratch;
            return resolved;
        }

        static BackgroundGradient ResolveForElement(in BackgroundGradient source, Vector2 rectSize,
            BackgroundGradientStop[] scratch, out float uvScale)
        {
            uvScale = 1f;
            if (!TryNormalizeLengthStops(source, scratch, out var normalized, out float pixelExtent))
                return ResolvePixelStops(source, rectSize, scratch);

            float length = GradientLength(source, rectSize);
            // A gradient line shorter than half the extent would see under half the ramp's texels and blur close stops.
            if (length * 2f < pixelExtent)
                return ResolvePixelStops(source, rectSize, scratch);

            uvScale = length / pixelExtent;
            return normalized;
        }

        // Length stops become fractions of the farthest stop, not of the gradient line, so one ramp fits every size.
        static bool TryNormalizeLengthStops(in BackgroundGradient gradient, BackgroundGradientStop[] scratch,
            out BackgroundGradient normalized, out float pixelExtent)
        {
            normalized = gradient;
            pixelExtent = 0f;
            var stops = gradient.stops;
            bool hasLength = false;
            float extent = 0f;
            for (int i = 0; i < stops.Length; ++i)
            {
                var stop = stops[i];
                if (stop.positionIsAuto || (stop.positionIsPercent && stop.position != 0f))
                    return false;
                hasLength |= !stop.positionIsPercent;
                extent = Mathf.Max(extent, stop.position);
            }

            if (!hasLength)
                return false;
            if (extent <= 0f)
                extent = 1f; // every stop is at or before 0px: the ramp is solid, so any extent draws it
            // The ramp reaches one texel past the farthest stop, so its last texel holds the color after that stop.
            extent *= k_AtlasWidth / (k_AtlasWidth - 1f);

            for (int i = 0; i < stops.Length; ++i)
                scratch[i] = BackgroundGradientStop.Percent(stops[i].color, stops[i].position / extent);
            normalized.stops = scratch;
            pixelExtent = extent;
            return true;
        }

        static bool NeedsResolve(BackgroundGradientStop[] stops)
        {
            if (stops == null)
                return false;
            for (int i = 0; i < stops.Length; ++i)
            {
                if (!stops[i].positionIsPercent || stops[i].positionIsAuto)
                    return true;
            }
            return false;
        }

        // Pixels along the element per unit of the ramp parameter t, as LinearUV / RadialUV map it.
        static float GradientLength(in BackgroundGradient gradient, Vector2 rectSize)
        {
            if (rectSize.x < 1e-6f || rectSize.y < 1e-6f)
                return 0f;

            if (gradient.type == GradientType.Radial)
            {
                if (gradient.shape == BackgroundGradientShape.Circle)
                    return CircleRadius(gradient.position, gradient.size, rectSize);

                // Ellipse t runs along the horizontal radius, as in CSS.
                EllipseAxes(gradient.position, gradient.size, out float rx, out _);
                return rx * rectSize.x;
            }

            // The renderer's fraction-space t, not the CSS gradient line, so pixel stops land where the ramp is drawn.
            float t0 = LinearUV(Vector2.zero, gradient.angle).x;
            float tx = (LinearUV(new Vector2(1f, 0f), gradient.angle).x - t0) / rectSize.x;
            float ty = (LinearUV(new Vector2(0f, 1f), gradient.angle).x - t0) / rectSize.y;
            float magnitude = Mathf.Sqrt(tx * tx + ty * ty);
            return magnitude > 1e-6f ? 1f / magnitude : 0f;
        }

        static Texture2D CreateAtlas(in BackgroundGradient gradient)
        {
            var atlas = new Texture2D(k_AtlasWidth, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0, // LUT: aniso taps would blend unrelated ramp positions
                name = "BackgroundGradientAtlas",
            };
            var pixels = atlas.GetRawTextureData<Color32>();
            for (int i = 0; i < k_AtlasWidth; ++i)
            {
                pixels[i] = RampTexel(gradient, i);
            }
            atlas.Apply(false, true);
            return atlas;
        }

        // Unit quad in the VectorImage's local space; per-vertex UVs encode the gradient
        // geometry (linear parameter t, or [0,1] position in the radial disc).
        static void ComputeQuadMesh(in BackgroundGradient gradient,
                                    out VectorImageVertex[] vertices,
                                    out ushort[] indices)
        {
            var corners = new Vector2[]
            {
                new(0f, 0f), // top-left
                new(1f, 0f), // top-right
                new(1f, 1f), // bottom-right
                new(0f, 1f), // bottom-left
            };

            vertices = new VectorImageVertex[4];
            for (int i = 0; i < 4; ++i)
            {
                Vector2 uv = gradient.type == GradientType.Radial
                    ? RadialUV(corners[i], gradient.position, gradient.size, gradient.shape, Vector2.one)
                    : LinearUV(corners[i], gradient.angle);

                vertices[i] = new VectorImageVertex
                {
                    position = new Vector3(corners[i].x, corners[i].y, Vertex.nearZ),
                    tint = Color.white,
                    uv = uv,
                    settingIndex = 0u,
                    vertexFlags = VertexFlags.None,
                    circle = Vector4.zero,
                };
            }

            indices = new ushort[] { 0, 1, 2, 2, 3, 0 }; // two triangles, CCW, +y down
        }

        // Per-vertex UV math mirrors shader semantics in Shaders/Includes/Internal/UnityUIE.cginc:447-486.
        // Computed in element-fraction space [0,1]²; circles need the element's pixel size to undo the aspect stretch.

        internal static Vector2 LinearUV(Vector2 corner, float angleRadians)
        {
            // CSS: 0 rad = "to top", clockwise. Direction in vertex space (+y down): (sin θ, -cos θ).
            float dx = Mathf.Sin(angleRadians);
            float dy = -Mathf.Cos(angleRadians);

            float nx = corner.x - 0.5f;
            float ny = corner.y - 0.5f;
            float proj = nx * dx + ny * dy;

            // Half-projection of the [0,1]² box onto dir — makes t=0 and t=1 land on the touching corners.
            float halfExtent = 0.5f * (Mathf.Abs(dx) + Mathf.Abs(dy));
            float invSpan = halfExtent > 1e-6f ? 1f / (2f * halfExtent) : 0f;
            float t = (proj + halfExtent) * invSpan;

            return new Vector2(t, 0f);
        }

        internal static Vector2 RadialUV(Vector2 corner, Vector2 center, BackgroundGradientSize sizeMode,
            BackgroundGradientShape shape, Vector2 rectSize)
        {
            float Rx, Ry;
            if (shape == BackgroundGradientShape.Circle)
            {
                // Pixel radius back to per-axis fractions, so the element-rect stretch yields a circle.
                float r = CircleRadius(center, sizeMode, rectSize);
                Rx = rectSize.x > 1e-6f ? r / rectSize.x : 0f;
                Ry = rectSize.y > 1e-6f ? r / rectSize.y : 0f;
            }
            else
                EllipseAxes(center, sizeMode, out Rx, out Ry);

            if (Rx < 1e-6f || Ry < 1e-6f)
            {
                // Degenerate ellipse: encode past t=1 so the shader clamps to the last stop.
                return new Vector2(1.5f, 1.5f);
            }

            // Shader does (uv-0.5)*2 and tests against the unit circle; independent Rx/Ry map
            // the CSS ellipse onto that circle.
            return new Vector2(
                0.5f + (corner.x - center.x) / (2f * Rx),
                0.5f + (corner.y - center.y) / (2f * Ry));
        }

        // Test-only per-pixel samplers — same math as the shader, in C#, so tests avoid the GPU.

        internal static Color32 SampleLinearAt(in BackgroundGradient gradient, float u, float v)
        {
            float t = LinearUV(new Vector2(u, v), gradient.angle).x;
            return SampleStops(gradient, Mathf.Clamp01(t));
        }

        internal static Color32 SampleRadialAt(in BackgroundGradient gradient, float u, float v)
        {
            return SampleRadialAt(gradient, u, v, Vector2.one);
        }

        internal static Color32 SampleRadialAt(in BackgroundGradient gradient, float u, float v, Vector2 rectSize)
        {
            Vector2 uv = RadialUV(new Vector2(u, v), gradient.position, gradient.size, gradient.shape, rectSize);
            // Mirror shader (uv-0.5)*2 with focus=0: t is the magnitude of the remapped uv.
            float rx = (uv.x - 0.5f) * 2f;
            float ry = (uv.y - 0.5f) * 2f;
            float t = Mathf.Sqrt(rx * rx + ry * ry);
            return SampleStops(gradient, Mathf.Clamp01(t));
        }

        // CSS ellipse semi-axes in normalized [0,1]² space (CSS Images L3 §3.3). Axes must be
        // per-axis (not scalarized) so the element-rect stretch produces the correct pixel ellipse.
        internal static void EllipseAxes(Vector2 center, BackgroundGradientSize sizeMode, out float Rx, out float Ry)
        {
            float top = center.y;
            float bottom = 1f - center.y;
            float left = center.x;
            float right = 1f - center.x;

            float closestX = Mathf.Min(left, right);
            float closestY = Mathf.Min(top, bottom);
            float farthestX = Mathf.Max(left, right);
            float farthestY = Mathf.Max(top, bottom);

            switch (sizeMode)
            {
                case BackgroundGradientSize.ClosestSide:    Rx = closestX;           Ry = closestY;           return;
                case BackgroundGradientSize.FarthestSide:   Rx = farthestX;          Ry = farthestY;          return;
                case BackgroundGradientSize.ClosestCorner:  Rx = Mathf.Sqrt(2f) * closestX;  Ry = Mathf.Sqrt(2f) * closestY;  return;
                case BackgroundGradientSize.FarthestCorner: Rx = Mathf.Sqrt(2f) * farthestX; Ry = Mathf.Sqrt(2f) * farthestY; return;
                default:                                    Rx = Mathf.Sqrt(2f) * farthestX; Ry = Mathf.Sqrt(2f) * farthestY; return;
            }
        }

        // CSS circle radius in pixels (CSS Images L3 §3.3); center is a fraction of rectSize.
        internal static float CircleRadius(Vector2 center, BackgroundGradientSize sizeMode, Vector2 rectSize)
        {
            float top = center.y * rectSize.y;
            float bottom = (1f - center.y) * rectSize.y;
            float left = center.x * rectSize.x;
            float right = (1f - center.x) * rectSize.x;

            float closestX = Mathf.Min(left, right);
            float closestY = Mathf.Min(top, bottom);
            float farthestX = Mathf.Max(left, right);
            float farthestY = Mathf.Max(top, bottom);

            switch (sizeMode)
            {
                case BackgroundGradientSize.ClosestSide:    return Mathf.Min(closestX, closestY);
                case BackgroundGradientSize.FarthestSide:   return Mathf.Max(farthestX, farthestY);
                case BackgroundGradientSize.ClosestCorner:  return Mathf.Sqrt(closestX * closestX + closestY * closestY);
                case BackgroundGradientSize.FarthestCorner: return Mathf.Sqrt(farthestX * farthestX + farthestY * farthestY);
                default:                                    return Mathf.Sqrt(farthestX * farthestX + farthestY * farthestY);
            }
        }

        // GradientSettingsAtlas maps t=0..1 onto the centers of the first and last texels.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Color32 RampTexel(in BackgroundGradient gradient, int i) => SampleStops(gradient, i / (k_AtlasWidth - 1f));

        static Color32 SampleStops(in BackgroundGradient gradient, float t)
        {
            t = Mathf.Clamp01(t);
            var stops = gradient.stops;
            if (stops == null || stops.Length == 0)
                return new Color32(0, 0, 0, 0);
            if (stops.Length == 1)
                return stops[0].color;

            int prev = 0, next = -1;
            float prevT = 0f, nextT = 0f;
            float position = float.NegativeInfinity;
            for (int i = 0; i < stops.Length; ++i)
            {
                // CSS color-stop fix-up: a stop placed before its predecessor moves up to it.
                position = Mathf.Max(position, stops[i].position);
                if (position >= t)
                {
                    next = i;
                    nextT = position;
                    if (i == 0)
                        prevT = position;
                    break;
                }
                prev = i;
                prevT = position;
            }
            if (next < 0)
            {
                next = prev;
                nextT = prevT;
            }

            float span = nextT - prevT;
            float localT = span > 1e-6f ? Mathf.Clamp01((t - prevT) / span) : 0f;
            return LerpPremultiplied(stops, prev, next, localT);
        }

        // CSS interpolates color stops in premultiplied alpha; the ramp stores the result straight.
        static Color LerpPremultiplied(BackgroundGradientStop[] stops, int prev, int next, float t)
        {
            Color a = stops[prev].color;
            Color b = stops[next].color;
            float weightA = a.a * (1f - t);
            float weightB = b.a * t;
            float alpha = weightA + weightB;
            if (alpha <= 0f)
                return TransparentNearVisibleStop(stops, prev, next);

            float invAlpha = 1f / alpha;
            return new Color(
                (a.r * weightA + b.r * weightB) * invAlpha,
                (a.g * weightA + b.g * weightB) * invAlpha,
                (a.b * weightA + b.b * weightB) * invAlpha,
                alpha);
        }

        // Bilinear filtering blends ramp texels straight, so a transparent texel takes the color of
        // the nearest visible stop to keep its rgb out of the neighboring texels.
        static Color TransparentNearVisibleStop(BackgroundGradientStop[] stops, int prev, int next)
        {
            for (int offset = 0; prev - offset >= 0 || next + offset < stops.Length; ++offset)
            {
                if (prev - offset >= 0 && stops[prev - offset].color.a > 0f)
                    return WithoutAlpha(stops[prev - offset].color);
                if (next + offset < stops.Length && stops[next + offset].color.a > 0f)
                    return WithoutAlpha(stops[next + offset].color);
            }
            return Color.clear;
        }

        static Color WithoutAlpha(Color color) => new Color(color.r, color.g, color.b, 0f);
    }
}
