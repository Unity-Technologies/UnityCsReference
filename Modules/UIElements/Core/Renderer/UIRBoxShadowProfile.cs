// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.UIElements.UIR
{
    // The falloff of a blurred box-shadow edge, sampled by every box-shadow of a panel. U goes across the edge, from
    // k_ExtentInSigmas standard deviations inside the shadow shape to as many outside (see UIRTessellationBoxShadow.cpp).
    class BoxShadowProfile : IDisposable
    {
        // Keep in sync with kBoxShadowExtentInSigmas
        const float k_ExtentInSigmas = 3.0f;
        const int k_Width = 128;

        Texture2D m_Texture;

        public Texture2D texture
        {
            get
            {
                if (m_Texture == null)
                    m_Texture = CreateTexture();
                return m_Texture;
            }
        }

        public void Dispose()
        {
            UIRUtility.Destroy(m_Texture);
            m_Texture = null;
        }

        static Texture2D CreateTexture()
        {
            var texture = new Texture2D(k_Width, 1, TextureFormat.RGBA32, false, true)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 0,
                name = "BoxShadowProfile",
            };

            var pixels = texture.GetRawTextureData<Color32>();
            for (int i = 0; i < k_Width; ++i)
            {
                float u = (i + 0.5f) / k_Width;
                float distanceInSigmas = (u - 0.5f) * 2.0f * k_ExtentInSigmas;
                pixels[i] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Evaluate(distanceInSigmas) * 255.0f));
            }
            texture.Apply(false, true);
            return texture;
        }

        // Keep in sync with GetBoxShadowProfile in UIRTessellationBoxShadow.cpp
        internal static float Evaluate(float distanceInSigmas)
        {
            float inner = Coverage(-k_ExtentInSigmas);
            float outer = Coverage(k_ExtentInSigmas);
            return Mathf.Clamp01((Coverage(distanceInSigmas) - outer) / (inner - outer));
        }

        // Fraction of a Gaussian beyond a distance, in standard deviations
        static float Coverage(float distanceInSigmas)
        {
            return 0.5f * Erfc(distanceInSigmas / Mathf.Sqrt(2.0f));
        }

        // Abramowitz and Stegun 7.1.26, with an absolute error below 1.5e-7
        static float Erfc(float x)
        {
            float z = Mathf.Abs(x);
            float t = 1.0f / (1.0f + 0.3275911f * z);
            float polynomial = t * (0.254829592f + t * (-0.284496736f + t * (1.421413741f + t * (-1.453152027f + t * 1.061405429f))));
            float erfc = polynomial * Mathf.Exp(-z * z);
            return x >= 0.0f ? erfc : 2.0f - erfc;
        }
    }
}
