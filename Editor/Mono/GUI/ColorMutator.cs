// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor
{
    internal enum RgbaChannel { R, G, B, A }
    internal enum HsvChannel { H, S, V }

    // includes color setters for RGB float, RGB byte, and HSV
    // each mode preserves maximum componential integrity until editing using a different mode
    // exposure and HDR interfaces assume that the supplied originalColor and all per-channel modifications are in linear space
    [Serializable]
    internal class ColorMutator
    {
        // specifies the max byte value to use when decomposing a float color into bytes with exposure
        // this is the value used by Photoshop
        private const byte k_MaxByteForOverexposedColor = 191;

        internal static void DecomposeHdrColor(Color linearColorHdr, out Color32 baseLinearColor, out float exposure)
        {
            baseLinearColor = linearColorHdr;
            var maxColorComponent = linearColorHdr.maxColorComponent;
            // replicate Photoshops's decomposition behaviour
            if (maxColorComponent == 0f || maxColorComponent <= 1f && maxColorComponent > 1 / 255f)
            {
                exposure = 0f;

                baseLinearColor.r = (byte)Mathf.RoundToInt(linearColorHdr.r * 255f);
                baseLinearColor.g = (byte)Mathf.RoundToInt(linearColorHdr.g * 255f);
                baseLinearColor.b = (byte)Mathf.RoundToInt(linearColorHdr.b * 255f);
            }
            else
            {
                // calibrate exposure to the max float color component
                var scaleFactor = k_MaxByteForOverexposedColor / maxColorComponent;
                exposure = Mathf.Log(255f / scaleFactor) / Mathf.Log(2f);

                // maintain maximal integrity of byte values to prevent off-by-one errors when scaling up a color one component at a time
                baseLinearColor.r = Math.Min(k_MaxByteForOverexposedColor, (byte)Mathf.CeilToInt(scaleFactor * linearColorHdr.r));
                baseLinearColor.g = Math.Min(k_MaxByteForOverexposedColor, (byte)Mathf.CeilToInt(scaleFactor * linearColorHdr.g));
                baseLinearColor.b = Math.Min(k_MaxByteForOverexposedColor, (byte)Mathf.CeilToInt(scaleFactor * linearColorHdr.b));
            }
        }

        public Color originalColor
        {
            get { return m_OriginalColor; }
        }
        [SerializeField] private Color m_OriginalColor;

        public Color32 color
        {
            get
            {
                return new Color32(
                    m_Color[(int)RgbaChannel.R],
                    m_Color[(int)RgbaChannel.G],
                    m_Color[(int)RgbaChannel.B],
                    m_Color[(int)RgbaChannel.A]
                    );
            }
        }
        [SerializeField] private byte[] m_Color = new byte[4];

        public byte GetColorChannel(RgbaChannel channel)
        {
            return m_Color[(int)channel];
        }

        public float GetColorChannelNormalized(RgbaChannel channel)
        {
            return m_Color[(int)channel] / 255f;
        }

        public void SetColorChannel(RgbaChannel channel, byte value)
        {
            if (m_Color[(int)channel] == value)
                return;
            m_Color[(int)channel] = value;
            m_ColorHdr[(int)channel] = (value / 255f);
            if (channel != RgbaChannel.A)
                m_ColorHdr[(int)channel] *= Mathf.Pow(2f, m_ExposureValue);
            Color.RGBToHSV(
                color,
                out m_Hsv[(int)HsvChannel.H],
                out m_Hsv[(int)HsvChannel.S],
                out m_Hsv[(int)HsvChannel.V]
                );
        }

        public void SetColorChannel(RgbaChannel channel, float normalizedValue)
        {
            SetColorChannel(channel, (byte)Mathf.RoundToInt(Mathf.Clamp01(normalizedValue) * 255f));
        }

        public Color exposureAdjustedColor
        {
            get
            {
                return new Color(
                    m_ColorHdr[(int)RgbaChannel.R],
                    m_ColorHdr[(int)RgbaChannel.G],
                    m_ColorHdr[(int)RgbaChannel.B],
                    m_ColorHdr[(int)RgbaChannel.A]
                    );
            }
        }
        [SerializeField] private float[] m_ColorHdr = new float[4];

        public float GetColorChannelHdr(RgbaChannel channel)
        {
            return m_ColorHdr[(int)channel];
        }

        public void SetColorChannelHdr(RgbaChannel channel, float value)
        {
            if (m_ColorHdr[(int)channel] == value)
                return;
            m_ColorHdr[(int)channel] = value;
            OnRgbaHdrChannelChanged((int)channel);
        }

        public Vector3 colorHsv
        {
            get
            {
                return new Vector3(
                    m_Hsv[(int)HsvChannel.H],
                    m_Hsv[(int)HsvChannel.S],
                    m_Hsv[(int)HsvChannel.V]
                    );
            }
        }
        [SerializeField] private float[] m_Hsv = new float[3];

        public float GetColorChannel(HsvChannel channel)
        {
            return m_Hsv[(int)channel];
        }

        public void SetColorChannel(HsvChannel channel, float value)
        {
            m_Hsv[(int)channel] = Mathf.Clamp01(value);

            var newColor = Color.HSVToRGB(
                    m_Hsv[(int)HsvChannel.H], m_Hsv[(int)HsvChannel.S], m_Hsv[(int)HsvChannel.V]
                    );
            m_Color[(int)RgbaChannel.R] = (byte)Mathf.CeilToInt(newColor.r * 255f);
            m_Color[(int)RgbaChannel.G] = (byte)Mathf.CeilToInt(newColor.g * 255f);
            m_Color[(int)RgbaChannel.B] = (byte)Mathf.CeilToInt(newColor.b * 255f);

            newColor *= Mathf.Pow(2f, m_ExposureValue);
            m_ColorHdr[(int)RgbaChannel.R] = newColor.r;
            m_ColorHdr[(int)RgbaChannel.G] = newColor.g;
            m_ColorHdr[(int)RgbaChannel.B] = newColor.b;
        }

        public float exposureValue
        {
            get { return m_ExposureValue; }
            set
            {
                if (m_ExposureValue == value)
                    return;
                m_ExposureValue = value;
                var newRgbFloat = (Color)color * Mathf.Pow(2f, m_ExposureValue);
                m_ColorHdr[(int)RgbaChannel.R] = newRgbFloat.r;
                m_ColorHdr[(int)RgbaChannel.G] = newRgbFloat.g;
                m_ColorHdr[(int)RgbaChannel.B] = newRgbFloat.b;
            }
        }
        [SerializeField] private float m_ExposureValue;

        public ColorMutator(Color originalColor)
        {
            m_OriginalColor = originalColor;
            Reset();
        }

        public void Reset()
        {
            if (m_ColorHdr == null || m_ColorHdr.Length != 4)
                m_ColorHdr = new float[4];

            m_ColorHdr[(int)RgbaChannel.R] = m_OriginalColor.r;
            m_ColorHdr[(int)RgbaChannel.G] = m_OriginalColor.g;
            m_ColorHdr[(int)RgbaChannel.B] = m_OriginalColor.b;
            m_ColorHdr[(int)RgbaChannel.A] = m_OriginalColor.a;

            if (m_Color == null || m_Color.Length != 4)
                m_Color = new byte[4];

            OnRgbaHdrChannelChanged(-1);
        }

        void OnRgbaHdrChannelChanged(int channel)
        {
            m_Color[(int)RgbaChannel.A] = (byte)Mathf.RoundToInt(Mathf.Clamp01(m_ColorHdr[(int)RgbaChannel.A]) * 255f);
            if (channel == (int)RgbaChannel.A)
                return;

            Color32 baseColor;
            DecomposeHdrColor(exposureAdjustedColor, out baseColor, out m_ExposureValue);
            m_Color[(int)RgbaChannel.R] = baseColor.r;
            m_Color[(int)RgbaChannel.G] = baseColor.g;
            m_Color[(int)RgbaChannel.B] = baseColor.b;
            Color.RGBToHSV(
                color,
                out m_Hsv[(int)HsvChannel.H],
                out m_Hsv[(int)HsvChannel.S],
                out m_Hsv[(int)HsvChannel.V]
                );
        }
    }
}
