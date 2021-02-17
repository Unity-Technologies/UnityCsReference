// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.TextCore.Text
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
            Texture tex = targetMaterial.GetTexture(TextShaderUtilities.ID_MainTex);
            int texId = tex.GetInstanceID();
            long key = (long)sourceId << 32 | (uint)texId;

            Material fallback;
            if (s_FallbackMaterials.TryGetValue(key, out fallback))
            {
                return fallback;
            }

            // Create new material from the source material and copy properties if using distance field shaders.
            if (sourceMaterial.HasProperty(TextShaderUtilities.ID_GradientScale) && targetMaterial.HasProperty(TextShaderUtilities.ID_GradientScale))
            {
                fallback = new Material(sourceMaterial);
                fallback.hideFlags = HideFlags.HideAndDontSave;

                fallback.name += " + " + tex.name;

                fallback.SetTexture(TextShaderUtilities.ID_MainTex, tex);
                // Retain material properties unique to target material.
                fallback.SetFloat(TextShaderUtilities.ID_GradientScale, targetMaterial.GetFloat(TextShaderUtilities.ID_GradientScale));
                fallback.SetFloat(TextShaderUtilities.ID_TextureWidth, targetMaterial.GetFloat(TextShaderUtilities.ID_TextureWidth));
                fallback.SetFloat(TextShaderUtilities.ID_TextureHeight, targetMaterial.GetFloat(TextShaderUtilities.ID_TextureHeight));
                fallback.SetFloat(TextShaderUtilities.ID_WeightNormal, targetMaterial.GetFloat(TextShaderUtilities.ID_WeightNormal));
                fallback.SetFloat(TextShaderUtilities.ID_WeightBold, targetMaterial.GetFloat(TextShaderUtilities.ID_WeightBold));
            }
            else
            {
                fallback = new Material(targetMaterial);
            }

            s_FallbackMaterials.Add(key, fallback);

            return fallback;
        }

        public static Material GetFallbackMaterial(FontAsset fontAsset, Material sourceMaterial, int atlasIndex)
        {
            int sourceMaterialID = sourceMaterial.GetInstanceID();
            Texture tex = fontAsset.atlasTextures[atlasIndex];
            int texID = tex.GetInstanceID();
            long key = (long)sourceMaterialID << 32 | (long)(uint)texID;
            Material fallback;

            if (s_FallbackMaterials.TryGetValue(key, out fallback))
                return fallback;

            // Create new material from the source material and assign relevant atlas texture
            Material fallbackMaterial = new Material(sourceMaterial);
            fallbackMaterial.SetTexture(TextShaderUtilities.ID_MainTex, tex);

            fallbackMaterial.hideFlags = HideFlags.HideAndDontSave;

            fallbackMaterial.name += " + " + tex.name;

            s_FallbackMaterials.Add(key, fallbackMaterial);


            return fallbackMaterial;
        }
    }
}
