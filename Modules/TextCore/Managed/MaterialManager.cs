// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.TextCore
{
    static class MaterialManager
    {
        static Dictionary<long, Material> s_FallbackMaterials = new Dictionary<long, Material>();

        /// <summary>
        /// This function returns a material instance using the material properties of a previous material but using the font atlas texture of the new font asset.
        /// </summary>
        /// <param name="sourceMaterial">The material containing the source material properties to be copied to the new material.</param>
        /// <param name="targetMaterial">The font atlas texture that should be assigned to the new material.</param>
        /// <returns></returns>
        public static Material GetFallbackMaterial(Material sourceMaterial, Material targetMaterial)
        {
            int sourceId = sourceMaterial.GetInstanceID();
            Texture tex = targetMaterial.GetTexture(ShaderUtilities.ID_MainTex);
            int texId = tex.GetInstanceID();
            long key = (long)sourceId << 32 | (uint)texId;

            Material fallback;
            if (s_FallbackMaterials.TryGetValue(key, out fallback))
            {
                return fallback;
            }

            // Create new material from the source material and copy properties if using distance field shaders.
            if (sourceMaterial.HasProperty(ShaderUtilities.ID_GradientScale) && targetMaterial.HasProperty(ShaderUtilities.ID_GradientScale))
            {
                fallback = new Material(sourceMaterial);
                fallback.hideFlags = HideFlags.HideAndDontSave;

                fallback.name += " + " + tex.name;

                fallback.SetTexture(ShaderUtilities.ID_MainTex, tex);
                // Retain material properties unique to target material.
                fallback.SetFloat(ShaderUtilities.ID_GradientScale, targetMaterial.GetFloat(ShaderUtilities.ID_GradientScale));
                fallback.SetFloat(ShaderUtilities.ID_TextureWidth, targetMaterial.GetFloat(ShaderUtilities.ID_TextureWidth));
                fallback.SetFloat(ShaderUtilities.ID_TextureHeight, targetMaterial.GetFloat(ShaderUtilities.ID_TextureHeight));
                fallback.SetFloat(ShaderUtilities.ID_WeightNormal, targetMaterial.GetFloat(ShaderUtilities.ID_WeightNormal));
                fallback.SetFloat(ShaderUtilities.ID_WeightBold, targetMaterial.GetFloat(ShaderUtilities.ID_WeightBold));
            }
            else
            {
                fallback = new Material(targetMaterial);
            }

            s_FallbackMaterials.Add(key, fallback);

            return fallback;
        }
    }
}
