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

            Material fallbackMaterial;
            if (s_FallbackMaterials.TryGetValue(key, out fallbackMaterial))
            {
                // Check if source material properties have changed.
                int sourceMaterialCRC = sourceMaterial.ComputeCRC();
                int fallbackMaterialCRC = fallbackMaterial.ComputeCRC();

                if (sourceMaterialCRC == fallbackMaterialCRC)
                    return fallbackMaterial;

                CopyMaterialPresetProperties(sourceMaterial, fallbackMaterial);

                return fallbackMaterial;
            }

            // Create new material from the source material and copy properties if using distance field shaders.
            if (sourceMaterial.HasProperty(TextShaderUtilities.ID_GradientScale) && targetMaterial.HasProperty(TextShaderUtilities.ID_GradientScale))
            {
                fallbackMaterial = new Material(sourceMaterial);
                fallbackMaterial.hideFlags = HideFlags.HideAndDontSave;

                fallbackMaterial.name += " + " + tex.name;

                fallbackMaterial.SetTexture(TextShaderUtilities.ID_MainTex, tex);
                // Retain material properties unique to target material.
                fallbackMaterial.SetFloat(TextShaderUtilities.ID_GradientScale, targetMaterial.GetFloat(TextShaderUtilities.ID_GradientScale));
                fallbackMaterial.SetFloat(TextShaderUtilities.ID_TextureWidth, targetMaterial.GetFloat(TextShaderUtilities.ID_TextureWidth));
                fallbackMaterial.SetFloat(TextShaderUtilities.ID_TextureHeight, targetMaterial.GetFloat(TextShaderUtilities.ID_TextureHeight));
                fallbackMaterial.SetFloat(TextShaderUtilities.ID_WeightNormal, targetMaterial.GetFloat(TextShaderUtilities.ID_WeightNormal));
                fallbackMaterial.SetFloat(TextShaderUtilities.ID_WeightBold, targetMaterial.GetFloat(TextShaderUtilities.ID_WeightBold));
            }
            else
            {
                fallbackMaterial = new Material(targetMaterial);
            }

            s_FallbackMaterials.Add(key, fallbackMaterial);


            return fallbackMaterial;
        }

        public static Material GetFallbackMaterial(FontAsset fontAsset, Material sourceMaterial, int atlasIndex)
        {
            int sourceMaterialID = sourceMaterial.GetInstanceID();
            Texture tex = fontAsset.atlasTextures[atlasIndex];
            int texID = tex.GetInstanceID();
            long key = (long)sourceMaterialID << 32 | (uint)texID;

            Material fallbackMaterial;
            if (s_FallbackMaterials.TryGetValue(key, out fallbackMaterial))
            {
                // Check if source material properties have changed.
                int sourceMaterialCRC = sourceMaterial.ComputeCRC();
                int fallbackMaterialCRC = fallbackMaterial.ComputeCRC();

                if (sourceMaterialCRC == fallbackMaterialCRC)
                    return fallbackMaterial;

                CopyMaterialPresetProperties(sourceMaterial, fallbackMaterial);

                return fallbackMaterial;
            }

            // Create new material from the source material and assign relevant atlas texture
            fallbackMaterial = new Material(sourceMaterial);
            fallbackMaterial.SetTexture(TextShaderUtilities.ID_MainTex, tex);

            fallbackMaterial.hideFlags = HideFlags.HideAndDontSave;

            fallbackMaterial.name += " + " + tex.name;

            s_FallbackMaterials.Add(key, fallbackMaterial);


            return fallbackMaterial;
        }

        /// <summary>
        /// Function to copy the properties of a source material preset to another while preserving the unique font asset properties of the destination material.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        static void CopyMaterialPresetProperties(Material source, Material destination)
        {
            if (!source.HasProperty(TextShaderUtilities.ID_GradientScale) || !destination.HasProperty(TextShaderUtilities.ID_GradientScale))
                return;

            // Save unique material properties
            Texture dst_texture = destination.GetTexture(TextShaderUtilities.ID_MainTex);
            float dst_gradientScale = destination.GetFloat(TextShaderUtilities.ID_GradientScale);
            float dst_texWidth = destination.GetFloat(TextShaderUtilities.ID_TextureWidth);
            float dst_texHeight = destination.GetFloat(TextShaderUtilities.ID_TextureHeight);
            float dst_weightNormal = destination.GetFloat(TextShaderUtilities.ID_WeightNormal);
            float dst_weightBold = destination.GetFloat(TextShaderUtilities.ID_WeightBold);

            // Make sure the same shader is used
            destination.shader = source.shader;

            // Copy all material properties
            destination.CopyPropertiesFromMaterial(source);

            // Copy shader keywords
            destination.shaderKeywords = source.shaderKeywords;

            // Restore unique material properties
            destination.SetTexture(TextShaderUtilities.ID_MainTex, dst_texture);
            destination.SetFloat(TextShaderUtilities.ID_GradientScale, dst_gradientScale);
            destination.SetFloat(TextShaderUtilities.ID_TextureWidth, dst_texWidth);
            destination.SetFloat(TextShaderUtilities.ID_TextureHeight, dst_texHeight);
            destination.SetFloat(TextShaderUtilities.ID_WeightNormal, dst_weightNormal);
            destination.SetFloat(TextShaderUtilities.ID_WeightBold, dst_weightBold);
        }
    }
}
