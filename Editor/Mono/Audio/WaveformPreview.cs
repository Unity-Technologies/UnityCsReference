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

        public double Start { get; private set; }
        public double Length { get; private set; }

        public Color BackgroundColor { get; set; }
        public Color WaveColor { get; set; }
        public event Action Updated;
        public UnityEngine.Object PresentedObject { get; private set; }

        public bool Optimized
        {
            get => _optimized;
            set
            {
                if (_optimized != value)
                {
                    if (value)
                        _dirty = true;

                    _optimized = value;
                    SetFlag(MessageFlags.Optimization, true);
                }
            }
        }

        public bool Looping
        {
            get => _looping;
            set
            {
                if (_looping != value)
                {
                    _dirty = true;
                    _looping = value;
                    SetFlag(MessageFlags.Looping, true);
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

        private double _start;
        private double _length;
        private bool _clearTexture = true;
        private Vector2 _size;
        private Texture2D _texture;
        private int _channels;
        private int _samples;
        private int _specificChannel;
        private ChannelMode _channelMode;

        private bool _looping;
        private bool _optimized;
        private bool _dirty;
        private bool _disposed;
        private MessageFlags _flags;

        protected WaveformPreview(UnityEngine.Object presentedObject, int sampleCount, int channels)
        {
            PresentedObject = presentedObject;
            Optimized = true;
            _samples = sampleCount;
            _channels = channels;
            BackgroundColor = new Color(40 / 255.0f, 40 / 255.0f, 40 / 255.0f, 1.0f);
            WaveColor = new Color(255.0f / 255.0f, 140.0f / 255.0f, 0 / 255.0f, 1.0f);
            UpdateTexture(sampleCount, channels);
        }

        protected internal WaveformPreview(UnityEngine.Object presentedObject, int sampleCount, int channels, bool deferTextureCreation)
        {
            PresentedObject = presentedObject;
            Optimized = true;
            _samples = sampleCount;
            _channels = channels;
            BackgroundColor = new Color(40 / 255.0f, 40 / 255.0f, 40 / 255.0f, 1.0f);
            WaveColor = new Color(255.0f / 255.0f, 140.0f / 255.0f, 0 / 255.0f, 1.0f);

            if (!deferTextureCreation)
                UpdateTexture(sampleCount, channels);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                InternalDispose();

                if (_texture != null)
                {
                    if (Application.isPlaying)
                        UnityEngine.Object.Destroy(_texture);
                    else
                        UnityEngine.Object.DestroyImmediate(_texture);
                }

                _texture = null;
            }
        }

        protected virtual void InternalDispose() { }

        public void Render(Rect rect)
        {
            if (s_Material == null)
            {
                s_Material = EditorGUIUtility.LoadRequired("Previews/PreviewAudioClipWaveform.mat") as Material;
            }

            s_Material.SetTexture("_WavTex", _texture);
            s_Material.SetFloat("_SampCount", _samples);
            s_Material.SetFloat("_ChanCount", _channels);
            s_Material.SetFloat("_RecPixelSize", 1.0f / rect.height);
            s_Material.SetColor("_BacCol", BackgroundColor);
            s_Material.SetColor("_ForCol", WaveColor);

            int mode = _channelMode switch
            {
                ChannelMode.Separate => -1,
                ChannelMode.SpecificChannel => _specificChannel,
                _ => -2
            };

            s_Material.SetFloat("_ChanDrawMode", mode);

            Graphics.DrawTexture(rect, _texture, s_Material);
        }

        public bool ApplyModifications()
        {
            if (_dirty || _texture == null)
            {
                SetFlag(UpdateTexture((int)_size.x, _channels) ? MessageFlags.TextureChanged : MessageFlags.None, true);
                OnModifications(_flags);
                _flags = MessageFlags.None;

                _texture.Apply();
                _dirty = false;
                return true;
            }

            return false;
        }

        public void SetChannelMode(ChannelMode mode, int specificChannelToRender = 0)
        {
            _channelMode = mode;
            _specificChannel = specificChannelToRender;
        }

        private bool UpdateTexture(int width, int channels)
        {
            int widthWithChannels = width * channels;
            int textureHeight = 1 + widthWithChannels / s_BaseTextureWidth;

            EnsureTextureExists(textureHeight);

            if (width == _samples && channels == _channels && _texture != null)
                return false;

            _samples = width;
            _channels = channels;

            _dirty = true;
            return true;
        }

        private void EnsureTextureExists(int textureHeight)
        {
            if (_texture == null)
            {
                _texture = new Texture2D(s_BaseTextureWidth, textureHeight, TextureFormat.RGBAHalf, false, true)
                {
                    filterMode = FilterMode.Point
                };
            }
            else
            {
                _texture.Reinitialize(s_BaseTextureWidth, textureHeight);
            }
        }

        public void OptimizeForSize(Vector2 newSize)
        {
            newSize = new Vector2(Mathf.Ceil(newSize.x), Mathf.Ceil(newSize.y));

            if (newSize.x != _size.x)
            {
                _size = newSize;
                SetFlag(MessageFlags.Size, true);
                _dirty = true;
            }
        }

        protected virtual void OnModifications(MessageFlags changedFlags) { }

        public void SetTimeInfo(double start, double length)
        {
            if (start != Start || length != Length)
            {
                Start = start;
                Length = length;
                _dirty = true;
                SetFlag(MessageFlags.Start | MessageFlags.Length, true);
            }
        }

        public virtual void SetMMWaveData(int interleavedOffset, float[] data)
        {
            Color[] colors = new Color[data.Length / 2];

            for (int i = 0, j = 0; i < data.Length; i += 2, j++)
            {
                colors[j] = new Color(data[i], data[i + 1], 0.0f, 0.0f);
            }

            int x = interleavedOffset % s_BaseTextureWidth;
            int y = interleavedOffset / s_BaseTextureWidth;
            _texture.SetPixels(x, y, colors.Length, 1, colors);

            _dirty = true;
            SetFlag(MessageFlags.ContentsChanged, true);

            Updated?.Invoke();
        }

        private void SetFlag(MessageFlags flag, bool value)
        {
            if (value)
                _flags |= flag;
            else
                _flags &= ~flag;
        }
    }
}
