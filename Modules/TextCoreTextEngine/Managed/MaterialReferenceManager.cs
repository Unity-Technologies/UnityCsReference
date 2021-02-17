// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.TextCore.Text
{
    class MaterialReferenceManager
    {
        static MaterialReferenceManager s_Instance;

        // Dictionaries used to track Asset references.
        Dictionary<int, Material> m_FontMaterialReferenceLookup = new Dictionary<int, Material>();
        Dictionary<int, FontAsset> m_FontAssetReferenceLookup = new Dictionary<int, FontAsset>();
        Dictionary<int, SpriteAsset> m_SpriteAssetReferenceLookup = new Dictionary<int, SpriteAsset>();
        Dictionary<int, TextColorGradient> m_ColorGradientReferenceLookup = new Dictionary<int, TextColorGradient>();

        /// <summary>
        /// Get a singleton instance of the registry
        /// </summary>
        public static MaterialReferenceManager instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new MaterialReferenceManager();
                return s_Instance;
            }
        }

        /// <summary>
        /// Add new font asset reference to dictionary.
        /// </summary>
        /// <param name="fontAsset"></param>
        public static void AddFontAsset(FontAsset fontAsset)
        {
            instance.AddFontAssetInternal(fontAsset);
        }

        /// <summary>
        ///  Add new Font Asset reference to dictionary.
        /// </summary>
        /// <param name="fontAsset"></param>
        void AddFontAssetInternal(FontAsset fontAsset)
        {
            if (m_FontAssetReferenceLookup.ContainsKey(fontAsset.hashCode)) return;

            // Add reference to the font asset.
            m_FontAssetReferenceLookup.Add(fontAsset.hashCode, fontAsset);

            // Add reference to the font material.
            m_FontMaterialReferenceLookup.Add(fontAsset.materialHashCode, fontAsset.material);
        }

        /// <summary>
        /// Add new Sprite Asset to dictionary.
        /// </summary>
        /// <param name="spriteAsset"></param>
        public static void AddSpriteAsset(SpriteAsset spriteAsset)
        {
            instance.AddSpriteAssetInternal(spriteAsset);
        }

        /// <summary>
        /// Internal method to add a new sprite asset to the dictionary.
        /// </summary>
        /// <param name="spriteAsset"></param>
        void AddSpriteAssetInternal(SpriteAsset spriteAsset)
        {
            if (m_SpriteAssetReferenceLookup.ContainsKey(spriteAsset.hashCode)) return;

            // Add reference to sprite asset.
            m_SpriteAssetReferenceLookup.Add(spriteAsset.hashCode, spriteAsset);

            // Adding reference to the sprite asset material as well
            m_FontMaterialReferenceLookup.Add(spriteAsset.hashCode, spriteAsset.material);
        }

        /// <summary>
        /// Add new Sprite Asset to dictionary.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="spriteAsset"></param>
        public static void AddSpriteAsset(int hashCode, SpriteAsset spriteAsset)
        {
            instance.AddSpriteAssetInternal(hashCode, spriteAsset);
        }

        /// <summary>
        /// Internal method to add a new sprite asset to the dictionary.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="spriteAsset"></param>
        void AddSpriteAssetInternal(int hashCode, SpriteAsset spriteAsset)
        {
            if (m_SpriteAssetReferenceLookup.ContainsKey(hashCode)) return;

            // Add reference to Sprite Asset.
            m_SpriteAssetReferenceLookup.Add(hashCode, spriteAsset);

            // Add reference to Sprite Asset using the asset hashcode.
            m_FontMaterialReferenceLookup.Add(hashCode, spriteAsset.material);
        }

        /// <summary>
        /// Add new Material reference to dictionary.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="material"></param>
        public static void AddFontMaterial(int hashCode, Material material)
        {
            instance.AddFontMaterialInternal(hashCode, material);
        }

        /// <summary>
        /// Add new material reference to dictionary.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="material"></param>
        void AddFontMaterialInternal(int hashCode, Material material)
        {
            // Since this function is called after checking if the material is
            // contained in the dictionary, there is no need to check again.
            m_FontMaterialReferenceLookup.Add(hashCode, material);
        }

        /// <summary>
        /// Add new Color Gradient Preset to dictionary.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="spriteAsset"></param>
        public static void AddColorGradientPreset(int hashCode, TextColorGradient spriteAsset)
        {
            instance.AddColorGradientPreset_Internal(hashCode, spriteAsset);
        }

        /// <summary>
        /// Internal method to add a new Color Gradient Preset to the dictionary.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="spriteAsset"></param>
        void AddColorGradientPreset_Internal(int hashCode, TextColorGradient spriteAsset)
        {
            if (m_ColorGradientReferenceLookup.ContainsKey(hashCode)) return;

            // Add reference to Color Gradient Preset Asset.
            m_ColorGradientReferenceLookup.Add(hashCode, spriteAsset);
        }

        /// <summary>
        /// Function to check if the font asset is already referenced.
        /// </summary>
        /// <param name="font"></param>
        /// <returns></returns>
        public bool Contains(FontAsset font)
        {
            return m_FontAssetReferenceLookup.ContainsKey(font.hashCode);
        }

        /// <summary>
        /// Function to check if the sprite asset is already referenced.
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public bool Contains(SpriteAsset sprite)
        {
            return m_FontAssetReferenceLookup.ContainsKey(sprite.hashCode);
        }

        /// <summary>
        /// Function returning the Font Asset corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        public static bool TryGetFontAsset(int hashCode, out FontAsset fontAsset)
        {
            return instance.TryGetFontAssetInternal(hashCode, out fontAsset);
        }

        /// <summary>
        /// Internal Function returning the Font Asset corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        bool TryGetFontAssetInternal(int hashCode, out FontAsset fontAsset)
        {
            fontAsset = null;

            return m_FontAssetReferenceLookup.TryGetValue(hashCode, out fontAsset);
        }

        /// <summary>
        /// Function returning the Sprite Asset corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="spriteAsset"></param>
        /// <returns></returns>
        public static bool TryGetSpriteAsset(int hashCode, out SpriteAsset spriteAsset)
        {
            return instance.TryGetSpriteAssetInternal(hashCode, out spriteAsset);
        }

        /// <summary>
        /// Internal function returning the Sprite Asset corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="spriteAsset"></param>
        /// <returns></returns>
        bool TryGetSpriteAssetInternal(int hashCode, out SpriteAsset spriteAsset)
        {
            spriteAsset = null;

            return m_SpriteAssetReferenceLookup.TryGetValue(hashCode, out spriteAsset);
        }

        /// <summary>
        /// Function returning the Color Gradient Preset corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="gradientPreset"></param>
        /// <returns></returns>
        public static bool TryGetColorGradientPreset(int hashCode, out TextColorGradient gradientPreset)
        {
            return instance.TryGetColorGradientPresetInternal(hashCode, out gradientPreset);
        }

        /// <summary>
        /// Internal function returning the Color Gradient Preset corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="gradientPreset"></param>
        /// <returns></returns>
        bool TryGetColorGradientPresetInternal(int hashCode, out TextColorGradient gradientPreset)
        {
            gradientPreset = null;

            return m_ColorGradientReferenceLookup.TryGetValue(hashCode, out gradientPreset);
        }

        /// <summary>
        /// Function returning the Font Material corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="material"></param>
        /// <returns></returns>
        public static bool TryGetMaterial(int hashCode, out Material material)
        {
            return instance.TryGetMaterialInternal(hashCode, out material);
        }

        /// <summary>
        /// Internal function returning the Font Material corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="material"></param>
        /// <returns></returns>
        bool TryGetMaterialInternal(int hashCode, out Material material)
        {
            material = null;

            return m_FontMaterialReferenceLookup.TryGetValue(hashCode, out material);
        }
    }
}
