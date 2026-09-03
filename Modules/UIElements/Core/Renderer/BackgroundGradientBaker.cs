// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Bindings;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    // Bakes a BackgroundGradient into a transient VectorImage that flows through the same
    // GradientSettingsAtlas / VectorImageManager path Painter2D uses. Per-panel instance,
    // owned by RenderTreeManager (mirrors UIRVectorImageManager's shape). Cached by
    // gradient hash; refcounted; unused VIs are destroyed at the end of each ProcessChanges.
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal class BackgroundGradientBaker : IDisposable
    {
        const int k_AtlasWidth = 64;

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
        bool m_Disposed;

        public VectorImage Bake(in BackgroundGradient gradient)
        {
            if (gradient.IsEmpty())
                return null;

            if (m_Cache.TryGetValue(gradient, out var cached) && cached != null)
            {
                m_PendingEviction.Remove(cached); // rescue: a fresh reference is inbound
                return cached;
            }

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
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal int CacheCount => m_Cache.Count;
        internal int PendingEvictionCount => m_PendingEviction.Count;
        internal int GetRefCount(VectorImage vi)
            => vi != null && m_Entries.TryGetValue(vi, out var e) ? e.refCount : 0;

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
                float t = (i + 0.5f) / k_AtlasWidth;
                pixels[i] = SampleStops(gradient, t);
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
                    ? RadialUV(corners[i], gradient.position, gradient.size)
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
        // Computed in element-fraction space [0,1]², so non-square elements get a slight aspect stretch.

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

        internal static Vector2 RadialUV(Vector2 corner, Vector2 center, BackgroundGradientSize sizeMode)
        {
            EllipseAxes(center, sizeMode, out float Rx, out float Ry);
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
            Vector2 uv = RadialUV(new Vector2(u, v), gradient.position, gradient.size);
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

        static Color32 SampleStops(in BackgroundGradient gradient, float t)
        {
            t = Mathf.Clamp01(t);
            var stops = gradient.stops;
            if (stops == null || stops.Length == 0)
                return new Color32(0, 0, 0, 0);
            if (stops.Length == 1)
                return stops[0].color;

            int prev = 0, next = stops.Length - 1;
            for (int i = 0; i < stops.Length; ++i)
            {
                float pi = NormalizePosition(stops[i].position, stops[i].positionIsPercent);
                if (pi >= t)
                {
                    next = i;
                    prev = Mathf.Max(0, i - 1);
                    break;
                }
                prev = i;
            }

            float prevT = NormalizePosition(stops[prev].position, stops[prev].positionIsPercent);
            float nextT = NormalizePosition(stops[next].position, stops[next].positionIsPercent);
            float span = nextT - prevT;
            float localT = span > 1e-6f ? Mathf.Clamp01((t - prevT) / span) : 0f;
            return Color.Lerp(stops[prev].color, stops[next].color, localT);
        }

        static float NormalizePosition(float pos, bool isPercent)
        {
            // Pixel stops have no canonical mapping without element bounds; approximate against atlas width.
            return isPercent ? pos : Mathf.Clamp01(pos / k_AtlasWidth);
        }
    }
}
