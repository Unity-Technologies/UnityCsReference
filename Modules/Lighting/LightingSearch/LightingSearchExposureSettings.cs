// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Lighting.Utilities;
using UnityEngine;

namespace UnityEditor.Lighting.LightingSearch
{
    static class LightingSearchExposureSettings
    {
        internal const float k_MinExposure = -16.0f;
        internal const float k_MaxExposure = 16.0f;
        const float k_DefaultExposure = 0.0f;
        const float k_AutoExposureTargetLuminance = 0.4f;

        static float? s_CurrentExposure;
        internal static event System.Action ExposureChanged;

        internal static float CurrentExposure
        {
            get
            {
                if (!s_CurrentExposure.HasValue)
                {
                    var lightmaps = LightmapSettings.lightmaps;
                    if (lightmaps.Length > 0)
                    {
                        // Only store the calculated exposure if we have lightmaps, so we recalculate when lightmaps load
                        s_CurrentExposure = CalculateAutoExposure();
                        return s_CurrentExposure.Value;
                    }
                    return k_DefaultExposure;
                }
                return s_CurrentExposure.Value;
            }
            set
            {
                var clampedValue = Mathf.Clamp(value, k_MinExposure, k_MaxExposure);

                if (!s_CurrentExposure.HasValue || !Mathf.Approximately(s_CurrentExposure.Value, clampedValue))
                {
                    s_CurrentExposure = clampedValue;
                    ExposureChanged?.Invoke();
                }
            }
        }

        static float CalculateAutoExposure()
        {
            var lightmaps = LightmapSettings.lightmaps;
            if (lightmaps.Length == 0)
                return k_DefaultExposure;

            var textures = new Texture2D[lightmaps.Length];
            for (int i = 0; i < lightmaps.Length; i++)
            {
                textures[i] = lightmaps[i].lightmapColor;
            }

            var textureArray = TextureExposure.GetUniformSizeTextureArray(textures);
            return TextureExposure.GetAutoExposure(textureArray, k_AutoExposureTargetLuminance);
        }

        internal static void ResetToDefault()
        {
            // Set via property setter to recalculate and trigger ExposureChanged event
            CurrentExposure = CalculateAutoExposure();
        }
    }
}
