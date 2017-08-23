// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    class WaveformPreview : IDisposable
    {
        static int s_BaseTextureWidth = 4096;
        static Material s_Material;

        public double start { get { return m_Start; } }
        public double length { get { return m_Length; } }

        public Color backgroundColor { get; set; }
        public Color waveColor { get; set; }
        public event Action updated;
        public UnityEngine.Object presentedObject;

        public bool optimized
        {
            get { return m_Optimized; }
            set
            {
                if (m_Optimized != value)
                {
                    if (value)
                        m_Dirty = true;

                    m_Optimized = value;
                    m_Flags |= MessageFlags.Optimization;
                }
            }
        }

        public bool looping
        {
            get { return m_Looping; }
            set
            {
                if (m_Looping != value)
                {
                    m_Dirty = true;
                    m_Looping = value;
                    m_Flags |= MessageFlags.Looping;
                }
            }
        }

        public enum ChannelMode
        {
            MonoSum,
            Separate,
            SpecificChannel
        }

        [Flags]
        protected enum MessageFlags
        {
            None = 0,
            Size = 1 << 0,
            Length = 1 << 1,
            Start = 1 << 2,
            Optimization = 1 << 3,
            TextureChanged = 1 << 4,
            ContentsChanged = 1 << 5,
            Looping = 1 << 6
        }

        protected static bool HasFlag(MessageFlags flags, MessageFlags test)
        {
            return (flags & test) != 0;
        }

        protected double m_Start;
        protected double m_Length;
        protected bool m_ClearTexture = true;
        protected Vector2 Size { get { return m_Size; } }

        Texture2D m_Texture;
        Vector2 m_Size;
        int m_Channels;
        int m_Samples;
        int m_SpecificChannel;
        ChannelMode m_ChannelMode;

        bool m_Looping;
        bool m_Optimized;
        bool m_Dirty;
        bool m_Disposed;
        MessageFlags m_Flags;

        protected WaveformPreview(UnityEngine.Object presentedObject, int samplesAndWidth, int channels)
        {
            this.presentedObject = presentedObject;
            optimized = true;
            m_Samples = samplesAndWidth;
            m_Channels = channels;
            backgroundColor = new Color(40 / 255.0f, 40 / 255.0f, 40 / 255.0f, 1.0f);
            waveColor = new Color(255.0f / 255.0f, 140.0f / 255.0f, 0 / 255.0f, 1.0f);
            UpdateTexture(samplesAndWidth, channels);
        }

        protected internal WaveformPreview(UnityEngine.Object presentedObject, int samplesAndWidth, int channels, bool deferTextureCreation)
        {
            this.presentedObject = presentedObject;
            optimized = true;
            m_Samples = samplesAndWidth;
            m_Channels = channels;
            backgroundColor = new Color(40 / 255.0f, 40 / 255.0f, 40 / 255.0f, 1.0f);
            waveColor = new Color(255.0f / 255.0f, 140.0f / 255.0f, 0 / 255.0f, 1.0f);
            if (!deferTextureCreation)
                UpdateTexture(samplesAndWidth, channels);
        }

        public void Dispose()
        {
            if (!m_Disposed)
            {
                m_Disposed = true;
                InternalDispose();

                if (m_Texture != null)
                    UnityEngine.Object.Destroy(m_Texture);

                m_Texture = null;
            }
        }

        protected virtual void InternalDispose() {}

        public void Render(Rect rect)
        {
            if (s_Material == null)
            {
                s_Material = EditorGUIUtility.LoadRequired("Previews/PreviewAudioClipWaveform.mat") as Material;
            }

            s_Material.SetTexture("_WavTex", m_Texture);
            s_Material.SetFloat("_SampCount", m_Samples);
            s_Material.SetFloat("_ChanCount", m_Channels);
            s_Material.SetFloat("_RecPixelSize", 1.0f / rect.height);
            s_Material.SetColor("_BacCol", backgroundColor);
            s_Material.SetColor("_ForCol", waveColor);

            int mode = -2;

            if (m_ChannelMode == ChannelMode.Separate)
                mode = -1;
            else if (m_ChannelMode == ChannelMode.SpecificChannel)
                mode = m_SpecificChannel;

            s_Material.SetInt("_ChanDrawMode", mode);

            Graphics.DrawTexture(rect, m_Texture, s_Material);
        }

        public bool ApplyModifications()
        {
            if (m_Dirty || m_Texture == null)
            {
                m_Flags |= UpdateTexture((int)m_Size.x, m_Channels) ? MessageFlags.TextureChanged : MessageFlags.None;
                OnModifications(m_Flags);
                m_Flags = MessageFlags.None;

                m_Texture.Apply();
                m_Dirty = false;
                return true;
            }

            return false;
        }

        public void SetChannelMode(ChannelMode mode, int specificChannelToRender)
        {
            m_ChannelMode = mode;
            m_SpecificChannel = specificChannelToRender;
        }

        public void SetChannelMode(ChannelMode mode)
        {
            SetChannelMode(mode, 0);
        }

        bool UpdateTexture(int width, int channels)
        {
            int widthWithChannels = width * channels;
            int textureHeight = 1 + widthWithChannels / s_BaseTextureWidth;

            Action<bool> updateTexture =
                clear =>
                {
                    if (m_Texture == null)
                    {
                        m_Texture = new Texture2D(s_BaseTextureWidth, textureHeight, TextureFormat.RGBAHalf, false, true);
                        m_Texture.filterMode = FilterMode.Point;
                        clear = false;
                    }
                    else
                    {
                        m_Texture.Resize(s_BaseTextureWidth, textureHeight);
                    }

                    if (!clear)
                        return;

                    var fillColorArray = m_Texture.GetPixels();
                    for (var i = 0; i < fillColorArray.Length; ++i)
                        fillColorArray[i] = Color.black;

                    m_Texture.SetPixels(fillColorArray);
                };

            if (width == m_Samples && channels == m_Channels && m_Texture != null)
            {
                return false;
            }

            // resample texture here
            updateTexture(m_ClearTexture);

            m_Samples = width;
            m_Channels = channels;

            return m_Dirty = true;
        }

        public void OptimizeForSize(Vector2 newSize)
        {
            newSize = new Vector2(Mathf.Ceil(newSize.x), Mathf.Ceil(newSize.y));

            if (newSize.x != m_Size.x)
            {
                m_Size = newSize;
                m_Flags |= MessageFlags.Size;
                m_Dirty = true;
            }
        }

        protected virtual void OnModifications(MessageFlags changedFlags) {}

        public void SetTimeInfo(double start, double length)
        {
            if (start != m_Start || length != m_Length)
            {
                m_Start = start;
                m_Length = length;
                m_Dirty = true;
                m_Flags |= MessageFlags.Start | MessageFlags.Length;
            }
        }

        public virtual void SetMMWaveData(int interleavedOffset, float[] data)
        {
            // could be optimized, but profiling shows it isn't the bottleneck at all
            for (int i = 0; i < data.Length; interleavedOffset++, i += 2)
            {
                int x = interleavedOffset % s_BaseTextureWidth;
                int y = interleavedOffset / s_BaseTextureWidth;
                m_Texture.SetPixel(x, y, new Color(data[i], data[i + 1], 0.0f, 0.0f));
            }

            m_Dirty = true;
            m_Flags |= MessageFlags.ContentsChanged;

            if (updated != null)
                updated();
        }
    }
}
