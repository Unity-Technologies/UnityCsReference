// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEngine.TextCore
{
    class MaterialReferenceManager
    {
        static MaterialReferenceManager s_Instance;

        // Dictionaries used to track Asset references.
        Dictionary<int, Material> m_FontMaterialReferenceLookup = new Dictionary<int, Material>();
        Dictionary<int, FontAsset> m_FontAssetReferenceLookup = new Dictionary<int, FontAsset>();
        Dictionary<int, TextSpriteAsset> m_SpriteAssetReferenceLookup = new Dictionary<int, TextSpriteAsset>();
        Dictionary<int, TextGradientPreset> m_ColorGradientReferenceLookup = new Dictionary<int, TextGradientPreset>();

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
        public static void AddSpriteAsset(TextSpriteAsset spriteAsset)
        {
            instance.AddSpriteAssetInternal(spriteAsset);
        }

        /// <summary>
        /// Internal method to add a new sprite asset to the dictionary.
        /// </summary>
        /// <param name="spriteAsset"></param>
        void AddSpriteAssetInternal(TextSpriteAsset spriteAsset)
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
        public static void AddSpriteAsset(int hashCode, TextSpriteAsset spriteAsset)
        {
            instance.AddSpriteAssetInternal(hashCode, spriteAsset);
        }

        /// <summary>
        /// Internal method to add a new sprite asset to the dictionary.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="spriteAsset"></param>
        void AddSpriteAssetInternal(int hashCode, TextSpriteAsset spriteAsset)
        {
            if (m_SpriteAssetReferenceLookup.ContainsKey(hashCode)) return;

            // Add reference to Sprite Asset.
            m_SpriteAssetReferenceLookup.Add(hashCode, spriteAsset);

            // Add reference to Sprite Asset using the asset hashcode.
            m_FontMaterialReferenceLookup.Add(hashCode, spriteAsset.material);

            // Compatibility check
            if (spriteAsset.hashCode == 0)
                spriteAsset.hashCode = hashCode;
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
        public static void AddColorGradientPreset(int hashCode, TextGradientPreset spriteAsset)
        {
            instance.AddColorGradientPreset_Internal(hashCode, spriteAsset);
        }

        /// <summary>
        /// Internal method to add a new Color Gradient Preset to the dictionary.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="spriteAsset"></param>
        void AddColorGradientPreset_Internal(int hashCode, TextGradientPreset spriteAsset)
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
            if (m_FontAssetReferenceLookup.ContainsKey(font.hashCode))
                return true;

            return false;
        }

        /// <summary>
        /// Function to check if the sprite asset is already referenced.
        /// </summary>
        /// <param name="sprite"></param>
        /// <returns></returns>
        public bool Contains(TextSpriteAsset sprite)
        {
            if (m_FontAssetReferenceLookup.ContainsKey(sprite.hashCode))
                return true;

            return false;
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

            if (m_FontAssetReferenceLookup.TryGetValue(hashCode, out fontAsset))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Function returning the Sprite Asset corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="spriteAsset"></param>
        /// <returns></returns>
        public static bool TryGetSpriteAsset(int hashCode, out TextSpriteAsset spriteAsset)
        {
            return instance.TryGetSpriteAssetInternal(hashCode, out spriteAsset);
        }

        /// <summary>
        /// Internal function returning the Sprite Asset corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="spriteAsset"></param>
        /// <returns></returns>
        bool TryGetSpriteAssetInternal(int hashCode, out TextSpriteAsset spriteAsset)
        {
            spriteAsset = null;

            if (m_SpriteAssetReferenceLookup.TryGetValue(hashCode, out spriteAsset))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Function returning the Color Gradient Preset corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="gradientPreset"></param>
        /// <returns></returns>
        public static bool TryGetColorGradientPreset(int hashCode, out TextGradientPreset gradientPreset)
        {
            return instance.TryGetColorGradientPresetInternal(hashCode, out gradientPreset);
        }

        /// <summary>
        /// Internal function returning the Color Gradient Preset corresponding to the provided hash code.
        /// </summary>
        /// <param name="hashCode"></param>
        /// <param name="gradientPreset"></param>
        /// <returns></returns>
        bool TryGetColorGradientPresetInternal(int hashCode, out TextGradientPreset gradientPreset)
        {
            gradientPreset = null;

            if (m_ColorGradientReferenceLookup.TryGetValue(hashCode, out gradientPreset))
            {
                return true;
            }

            return false;
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

            if (m_FontMaterialReferenceLookup.TryGetValue(hashCode, out material))
            {
                return true;
            }

            return false;
        }
    }
}
