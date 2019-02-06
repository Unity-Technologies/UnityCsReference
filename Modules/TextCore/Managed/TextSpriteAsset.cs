// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;


namespace UnityEngine.TextCore
{
    /// <summary>
    ///
    /// </summary>
    [Serializable]
    internal class TextSpriteAsset : ScriptableObject
    {
        internal Dictionary<uint, int> m_UnicodeLookup;
        internal Dictionary<int, int> m_NameLookup;
        internal Dictionary<uint, int> m_GlyphIndexLookup;

        /// <summary>
        /// The version of the sprite asset class.
        /// Version 1.1.0 updates the asset data structure to be compatible with new font asset structure.
        /// </summary>
        public string version
        {
            get { return m_Version; }
            set { m_Version = value; }
        }
        [SerializeField]
        string m_Version;


        /// <summary>
        /// HashCode based on the name of the asset.
        /// TODO: Need to handle renaming of font asset.
        /// </summary>
        public int hashCode
        {
            get { return m_HashCode; }
            set { m_HashCode = value; }
        }
        [SerializeField]
        int m_HashCode;

        // The texture which contains the sprites.
        [SerializeField]
        public Texture spriteSheet;


        /// <summary>
        /// The material used by this text sprite asset.
        /// </summary>
        public Material material
        {
            get { return m_Material; }
            set { m_Material = value; }
        }
        [SerializeField]
        Material m_Material;

        /// <summary>
        /// Hash code derived from the name of the material used by this TextSpriteAsset.
        /// </summary>
        public int materialHashCode
        {
            get { return m_MaterialHashCode; }
        }
        [SerializeField]
        int m_MaterialHashCode;

        /// <summary>
        /// List containing the sprite characters.
        /// </summary>
        public List<SpriteCharacter> spriteCharacterTable
        {
            get { return m_SpriteCharacterTable; }
            internal set { m_SpriteCharacterTable = value; }
        }
        [SerializeField]
        private List<SpriteCharacter> m_SpriteCharacterTable = new List<SpriteCharacter>();

        /// <summary>
        /// List containing the sprite glyphs.
        /// </summary>
        public List<SpriteGlyph> spriteGlyphTable
        {
            get { return m_SpriteGlyphTable; }
            internal set { m_SpriteGlyphTable = value; }
        }
        [SerializeField]
        private List<SpriteGlyph> m_SpriteGlyphTable = new List<SpriteGlyph>();

        /// <summary>
        /// List which contains the Fallback font assets for this font.
        /// </summary>
        [SerializeField]
        public List<TextSpriteAsset> fallbackSpriteAssets;

        internal bool m_IsSpriteAssetLookupTablesDirty = false;

        void Awake()
        {
            // Update the name and material hash codes.
            m_HashCode = TextUtilities.GetHashCodeCaseInSensitive(this.name);
            if (m_Material != null)
                m_MaterialHashCode = TextUtilities.GetHashCodeCaseInSensitive(m_Material.name);
        }

        void OnValidate()
        {
            // Update name hash code.
            m_HashCode = TextUtilities.GetHashCodeCaseInSensitive(this.name);
            if (m_Material != null)
                m_MaterialHashCode = TextUtilities.GetHashCodeCaseInSensitive(m_Material.name);

            UpdateLookupTables();
        }


        /// <summary>
        /// Create a material for the sprite asset.
        /// </summary>
        /// <returns></returns>
        Material GetDefaultSpriteMaterial()
        {
            ShaderUtilities.GetShaderPropertyIDs();

            // Add a new material
            Shader shader = Shader.Find("TextMeshPro/Sprite");
            Material tempMaterial = new Material(shader);
            tempMaterial.SetTexture(ShaderUtilities.ID_MainTex, spriteSheet);
            tempMaterial.hideFlags = HideFlags.HideInHierarchy;

            return tempMaterial;
        }

        /// <summary>
        /// Function to update the sprite name and unicode lookup tables.
        /// This function should be called when a sprite's name or unicode value changes or when a new sprite is added.
        /// </summary>
        public void UpdateLookupTables()
        {
            // Initialize / Clear glyph index lookup dictionary.
            if (m_GlyphIndexLookup == null)
                m_GlyphIndexLookup = new Dictionary<uint, int>();
            else
                m_GlyphIndexLookup.Clear();

            for (int i = 0; i < m_SpriteGlyphTable.Count; i++)
            {
                uint glyphIndex = m_SpriteGlyphTable[i].index;

                if (m_GlyphIndexLookup.ContainsKey(glyphIndex) == false)
                    m_GlyphIndexLookup.Add(glyphIndex, i);
            }

            if (m_NameLookup == null)
                m_NameLookup = new Dictionary<int, int>();
            else
                m_NameLookup.Clear();

            if (m_UnicodeLookup == null)
                m_UnicodeLookup = new Dictionary<uint, int>();
            else
                m_UnicodeLookup.Clear();

            for (int i = 0; i < m_SpriteCharacterTable.Count; i++)
            {
                int nameHashCode = m_SpriteCharacterTable[i].hashCode;

                if (m_NameLookup.ContainsKey(nameHashCode) == false)
                    m_NameLookup.Add(nameHashCode, i);

                uint unicode = m_SpriteCharacterTable[i].unicode;

                if (m_UnicodeLookup.ContainsKey(unicode) == false)
                    m_UnicodeLookup.Add(unicode, i);

                // Update glyph reference which is not serialized
                uint glyphIndex = m_SpriteCharacterTable[i].glyphIndex;

                int index;
                if (m_GlyphIndexLookup.TryGetValue(glyphIndex, out index))
                    m_SpriteCharacterTable[i].glyph = m_SpriteGlyphTable[index];
            }

            m_IsSpriteAssetLookupTablesDirty = false;
        }

        /// <summary>
        /// Function which returns the sprite index using the hashcode of the name
        /// </summary>
        /// <param name="hashCode"></param>
        /// <returns></returns>
        public int GetSpriteIndexFromHashcode(int hashCode)
        {
            if (m_NameLookup == null)
                UpdateLookupTables();

            int index;
            if (m_NameLookup.TryGetValue(hashCode, out index))
                return index;

            return -1;
        }

        /// <summary>
        /// Returns the index of the sprite for the given unicode value.
        /// </summary>
        /// <param name="unicode"></param>
        /// <returns></returns>
        public int GetSpriteIndexFromUnicode(uint unicode)
        {
            if (m_UnicodeLookup == null)
                UpdateLookupTables();

            int index;
            if (m_UnicodeLookup.TryGetValue(unicode, out index))
                return index;

            return -1;
        }

        /// <summary>
        /// Returns the index of the sprite for the given name.
        /// </summary>
        /// <param name="spriteName"></param>
        /// <returns></returns>
        public int GetSpriteIndexFromName(string spriteName)
        {
            if (m_NameLookup == null)
                UpdateLookupTables();

            int hashCode = TextUtilities.GetHashCodeCaseInSensitive(spriteName);

            return GetSpriteIndexFromHashcode(hashCode);
        }

        /// <summary>
        /// Used to keep track of which Sprite Assets have been searched.
        /// </summary>
        static List<int> s_SearchedSpriteAssets;

        /// <summary>
        /// Search through the given sprite asset and its fallbacks for the specified sprite matching the given unicode character.
        /// </summary>
        /// <param name="spriteAsset">The font asset to search for the given character.</param>
        /// <param name="unicode">The character to find.</param>
        /// <param name="includeFallbacks"></param>
        /// <param name="spriteIndex"></param>
        /// <returns></returns>
        public static TextSpriteAsset SearchForSpriteByUnicode(TextSpriteAsset spriteAsset, uint unicode, bool includeFallbacks, out int spriteIndex)
        {
            // Check to make sure sprite asset is not null
            if (spriteAsset == null) { spriteIndex = -1; return null; }

            // Get sprite index for the given unicode
            spriteIndex = spriteAsset.GetSpriteIndexFromUnicode(unicode);
            if (spriteIndex != -1)
                return spriteAsset;

            // Initialize list to track instance of Sprite Assets that have already been searched.
            if (s_SearchedSpriteAssets == null)
                s_SearchedSpriteAssets = new List<int>();

            s_SearchedSpriteAssets.Clear();

            // Get instance ID of sprite asset and add to list.
            int id = spriteAsset.GetInstanceID();
            s_SearchedSpriteAssets.Add(id);

            // Search potential fallback sprite assets if includeFallbacks is true.
            if (includeFallbacks && spriteAsset.fallbackSpriteAssets != null && spriteAsset.fallbackSpriteAssets.Count > 0)
                return SearchForSpriteByUnicodeInternal(spriteAsset.fallbackSpriteAssets, unicode, includeFallbacks, out spriteIndex);

            // Search default sprite asset potentially assigned in the Text Settings.
            if (includeFallbacks && TextSettings.defaultSpriteAsset != null)
                return SearchForSpriteByUnicodeInternal(TextSettings.defaultSpriteAsset, unicode, includeFallbacks, out spriteIndex);

            spriteIndex = -1;
            return null;
        }

        /// <summary>
        /// Search through the given list of sprite assets and fallbacks for a sprite whose unicode value matches the target unicode.
        /// </summary>
        /// <param name="spriteAssets"></param>
        /// <param name="unicode"></param>
        /// <param name="includeFallbacks"></param>
        /// <param name="spriteIndex"></param>
        /// <returns></returns>
        static TextSpriteAsset SearchForSpriteByUnicodeInternal(List<TextSpriteAsset> spriteAssets, uint unicode, bool includeFallbacks, out int spriteIndex)
        {
            for (int i = 0; i < spriteAssets.Count; i++)
            {
                TextSpriteAsset temp = spriteAssets[i];
                if (temp == null) continue;

                int id = temp.GetInstanceID();

                // Skip over the fallback sprite asset if it has already been searched.
                if (s_SearchedSpriteAssets.Contains(id)) continue;

                // Add to list of font assets already searched.
                s_SearchedSpriteAssets.Add(id);

                temp = SearchForSpriteByUnicodeInternal(temp, unicode, includeFallbacks, out spriteIndex);

                if (temp != null)
                    return temp;
            }

            spriteIndex = -1;
            return null;
        }

        /// <summary>
        /// Search the given sprite asset and fallbacks for a sprite whose unicode value matches the target unicode.
        /// </summary>
        /// <param name="spriteAsset"></param>
        /// <param name="unicode"></param>
        /// <param name="includeFallbacks"></param>
        /// <param name="spriteIndex"></param>
        /// <returns></returns>
        static TextSpriteAsset SearchForSpriteByUnicodeInternal(TextSpriteAsset spriteAsset, uint unicode, bool includeFallbacks, out int spriteIndex)
        {
            // Get sprite index for the given unicode
            spriteIndex = spriteAsset.GetSpriteIndexFromUnicode(unicode);
            if (spriteIndex != -1)
                return spriteAsset;

            if (includeFallbacks && spriteAsset.fallbackSpriteAssets != null && spriteAsset.fallbackSpriteAssets.Count > 0)
                return SearchForSpriteByUnicodeInternal(spriteAsset.fallbackSpriteAssets, unicode, includeFallbacks, out spriteIndex);

            spriteIndex = -1;
            return null;
        }

        /// <summary>
        /// Search the given sprite asset and fallbacks for a sprite whose hash code value of its name matches the target hash code.
        /// </summary>
        /// <param name="spriteAsset">The Sprite Asset to search for the given sprite whose name matches the hashcode value</param>
        /// <param name="hashCode">The hash code value matching the name of the sprite</param>
        /// <param name="includeFallbacks">Include fallback sprite assets in the search</param>
        /// <param name="spriteIndex">The index of the sprite matching the provided hash code</param>
        /// <returns>The Sprite Asset that contains the sprite</returns>
        public static TextSpriteAsset SearchForSpriteByHashCode(TextSpriteAsset spriteAsset, int hashCode, bool includeFallbacks, out int spriteIndex)
        {
            // Make sure sprite asset is not null
            if (spriteAsset == null) { spriteIndex = -1; return null; }

            spriteIndex = spriteAsset.GetSpriteIndexFromHashcode(hashCode);
            if (spriteIndex != -1)
                return spriteAsset;

            // Initialize list to track instance of Sprite Assets that have already been searched.
            if (s_SearchedSpriteAssets == null)
                s_SearchedSpriteAssets = new List<int>();

            s_SearchedSpriteAssets.Clear();

            int id = spriteAsset.GetInstanceID();
            // Add to list of font assets already searched.
            s_SearchedSpriteAssets.Add(id);

            if (includeFallbacks && spriteAsset.fallbackSpriteAssets != null && spriteAsset.fallbackSpriteAssets.Count > 0)
                return SearchForSpriteByHashCodeInternal(spriteAsset.fallbackSpriteAssets, hashCode, includeFallbacks, out spriteIndex);

            // Search default sprite asset potentially assigned in the Text Settings.
            if (includeFallbacks && TextSettings.defaultSpriteAsset != null)
                return SearchForSpriteByHashCodeInternal(TextSettings.defaultSpriteAsset, hashCode, includeFallbacks, out spriteIndex);

            spriteIndex = -1;
            return null;
        }

        /// <summary>
        ///  Search through the given list of sprite assets and fallbacks for a sprite whose hash code value of its name matches the target hash code.
        /// </summary>
        /// <param name="spriteAssets"></param>
        /// <param name="hashCode"></param>
        /// <param name="searchFallbacks"></param>
        /// <param name="spriteIndex"></param>
        /// <returns></returns>
        static TextSpriteAsset SearchForSpriteByHashCodeInternal(List<TextSpriteAsset> spriteAssets, int hashCode, bool searchFallbacks, out int spriteIndex)
        {
            // Search through the list of sprite assets
            for (int i = 0; i < spriteAssets.Count; i++)
            {
                TextSpriteAsset temp = spriteAssets[i];
                if (temp == null) continue;

                int id = temp.GetInstanceID();

                // Skip over the fallback sprite asset if it has already been searched.
                if (s_SearchedSpriteAssets.Contains(id)) continue;

                // Add to list of font assets already searched.
                s_SearchedSpriteAssets.Add(id);

                temp = SearchForSpriteByHashCodeInternal(temp, hashCode, searchFallbacks, out spriteIndex);

                if (temp != null)
                    return temp;
            }

            spriteIndex = -1;
            return null;
        }

        /// <summary>
        /// Search through the given sprite asset and fallbacks for a sprite whose hash code value of its name matches the target hash code.
        /// </summary>
        /// <param name="spriteAsset"></param>
        /// <param name="hashCode"></param>
        /// <param name="searchFallbacks"></param>
        /// <param name="spriteIndex"></param>
        /// <returns></returns>
        static TextSpriteAsset SearchForSpriteByHashCodeInternal(TextSpriteAsset spriteAsset, int hashCode, bool searchFallbacks, out int spriteIndex)
        {
            // Get the sprite for the given hash code.
            spriteIndex = spriteAsset.GetSpriteIndexFromHashcode(hashCode);
            if (spriteIndex != -1)
                return spriteAsset;

            if (searchFallbacks && spriteAsset.fallbackSpriteAssets != null && spriteAsset.fallbackSpriteAssets.Count > 0)
                return SearchForSpriteByHashCodeInternal(spriteAsset.fallbackSpriteAssets, hashCode, searchFallbacks, out spriteIndex);

            spriteIndex = -1;
            return null;
        }

        /// <summary>
        /// Sort the sprite glyph table by glyph index.
        /// </summary>
        public void SortGlyphTable()
        {
            if (m_SpriteGlyphTable == null || m_SpriteGlyphTable.Count == 0) return;

            m_SpriteGlyphTable = m_SpriteGlyphTable.OrderBy(item => item.index).ToList();
        }

        /// <summary>
        /// Sort the sprite character table by Unicode values.
        /// </summary>
        internal void SortCharacterTable()
        {
            if (m_SpriteCharacterTable != null && m_SpriteCharacterTable.Count > 0)
                m_SpriteCharacterTable = m_SpriteCharacterTable.OrderBy(c => c.unicode).ToList();
        }

        /// <summary>
        /// Sort both sprite glyph and character tables.
        /// </summary>
        internal void SortGlyphAndCharacterTables()
        {
            SortGlyphTable();
            SortCharacterTable();
        }
    }
}
