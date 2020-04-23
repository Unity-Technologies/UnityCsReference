using System;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    /// <summary>
    /// This class manages a vertical atlas of horizontal GradientSettings.
    /// </summary>
    class GradientSettingsAtlas : IDisposable
    {
        static ProfilerMarker s_MarkerWrite = new ProfilerMarker("UIR.GradientSettingsAtlas.Write");
        static ProfilerMarker s_MarkerCommit = new ProfilerMarker("UIR.GradientSettingsAtlas.Commit");

        readonly int m_Length;
        readonly int m_ElemWidth;

        internal int length { get { return m_Length; } }

        BestFitAllocator m_Allocator;
        Texture2D m_Atlas; // Should be accessed through the property
        RawTexture m_RawAtlas;

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                UIRUtility.Destroy(m_Atlas);
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        public GradientSettingsAtlas(int length = 4096)
        {
            m_Length = length;
            m_ElemWidth = 3;
            Reset();
        }

        public void Reset()
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            m_Allocator = new BestFitAllocator((uint)m_Length);
            UIRUtility.Destroy(m_Atlas);
            m_RawAtlas = new RawTexture();
            MustCommit = false;
        }

        public Texture2D atlas { get { return m_Atlas; } }

        public Alloc Add(int count)
        {
            Debug.Assert(count > 0);

            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return new Alloc();
            }

            Alloc alloc = m_Allocator.Allocate((uint)count);

            return alloc;
        }

        public void Remove(Alloc alloc)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            m_Allocator.Free(alloc);
        }

        public void Write(Alloc alloc, GradientSettings[] settings, GradientRemap remap)
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            if (m_RawAtlas.rgba == null)
            {
                m_RawAtlas = new RawTexture
                {
                    rgba = new Color32[m_ElemWidth * m_Length],
                    width = m_ElemWidth,
                    height = m_Length
                };

                int size = m_ElemWidth * m_Length;
                for (int i = 0; i < size; ++i)
                    m_RawAtlas.rgba[i] = Color.black;
            }

            s_MarkerWrite.Begin();

            int destY = (int)alloc.start;
            for (int i = 0, settingsCount = settings.Length; i < settingsCount; ++i)
            {
                int destX = 0;
                GradientSettings entry = settings[i];
                Debug.Assert(remap == null || destY == remap.destIndex);
                if (entry.gradientType == GradientType.Radial)
                {
                    var focus = entry.radialFocus;
                    focus += Vector2.one;
                    focus /= 2.0f;
                    focus.y = 1.0f - focus.y;
                    m_RawAtlas.WriteRawFloat4Packed((float)GradientType.Radial / 255, (float)entry.addressMode / 255, focus.x, focus.y, destX++, destY);
                }
                else if (entry.gradientType == GradientType.Linear)
                {
                    m_RawAtlas.WriteRawFloat4Packed(0.0f, (float)entry.addressMode / 255, 0.0f, 0.0f, destX++, destY);
                }

                Vector2Int pos = new Vector2Int(entry.location.x, entry.location.y);
                var size = new Vector2(entry.location.width - 1, entry.location.height - 1);
                if (remap != null)
                {
                    pos = new Vector2Int(remap.location.x, remap.location.y);
                    size = new Vector2(remap.location.width - 1, remap.location.height - 1);
                }
                m_RawAtlas.WriteRawInt2Packed(pos.x, pos.y, destX++, destY);
                m_RawAtlas.WriteRawInt2Packed((int)size.x, (int)size.y, destX++, destY);

                remap = remap?.next;
                ++destY;
            }

            MustCommit = true;

            s_MarkerWrite.End();
        }

        public bool MustCommit { get; private set; }

        public void Commit()
        {
            if (disposed)
            {
                DisposeHelper.NotifyDisposedUsed(this);
                return;
            }

            if (!MustCommit)
                return;

            PrepareAtlas();

            s_MarkerCommit.Begin();
            // TODO: This way of transferring is costly since it is a synchronous operation that flushes the pipeline.
            m_Atlas.SetPixels32(m_RawAtlas.rgba);
            m_Atlas.Apply();
            s_MarkerCommit.End();

            MustCommit = false;
        }

        void PrepareAtlas()
        {
            if (m_Atlas != null)
                return;

            m_Atlas = new Texture2D(m_ElemWidth, m_Length, TextureFormat.ARGB32, 0, true)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "GradientSettings " + Random.Range(int.MinValue, int.MaxValue),
                filterMode = FilterMode.Point
            };
        }

        struct RawTexture
        {
            public Color32[] rgba;
            public int width;
            public int height;

            public void WriteRawInt2Packed(int v0, int v1, int destX, int destY)
            {
                byte r = (byte)(v0 / 255);
                byte g = (byte)(v0 - r * 255);
                byte b = (byte)(v1 / 255);
                byte a = (byte)(v1 - b * 255);
                int offset = destY * width + destX;
                rgba[offset] = new Color32(r, g, b, a);
            }

            public void WriteRawFloat4Packed(float f0, float f1, float f2, float f3, int destX, int destY)
            {
                byte r = (byte)(f0 * 255f + 0.5f);
                byte g = (byte)(f1 * 255f + 0.5f);
                byte b = (byte)(f2 * 255f + 0.5f);
                byte a = (byte)(f3 * 255f + 0.5f);
                int offset = destY * width + destX;
                rgba[offset] = new Color32(r, g, b, a);
            }
        }
    }
}
