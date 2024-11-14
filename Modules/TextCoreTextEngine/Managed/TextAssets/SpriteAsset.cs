// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;


namespace UnityEngine.TextCore.Text
{
    [HelpURL("https://docs.unity3d.com/2023.3/Documentation/Manual/UIE-sprite.html")]
    [ExcludeFromPresetAttribute]
    public class SpriteAsset : TextAsset
    {
        internal Dictionary<int, int> m_NameLookup;
        internal Dictionary<uint, int> m_GlyphIndexLookup;

        /// <summary>
        /// Information about the sprite asset's face.
        /// </summary>
        public FaceInfo faceInfo
        {
            get { return m_FaceInfo; }
            internal set { m_FaceInfo = value; }
        }
        [SerializeField]
        internal FaceInfo m_FaceInfo;

        /// <summary>
        /// The texture containing the sprites referenced by this sprite asset.
        /// </summary>
        public Texture spriteSheet
        {
            get { return m_SpriteAtlasTexture; }
            internal set
            {
                m_SpriteAtlasTexture = value;
                width = m_SpriteAtlasTexture.width;
                height = m_SpriteAtlasTexture.height;
            }
        }
        [FormerlySerializedAs("spriteSheet")][SerializeField]
        internal Texture m_SpriteAtlasTexture;

        internal float width { get; private set; }
        internal float height { get; private set; }

        /// <summary>
        /// List containing the sprite characters.
        /// </summary>
        public List<SpriteCharacter> spriteCharacterTable
        {
            get
            {
                if (m_GlyphIndexLookup == null)
                    UpdateLookupTables();

                return m_SpriteCharacterTable;
            }
            internal set { m_SpriteCharacterTable = value; }
        }
        [SerializeField]
        private List<SpriteCharacter> m_SpriteCharacterTable = new List<SpriteCharacter>();

        /// <summary>
        /// Dictionary used to lookup sprite characters by their unicode value.
        /// </summary>
        public Dictionary<uint, SpriteCharacter> spriteCharacterLookupTable
        {
            get
            {
                if (m_SpriteCharacterLookup == null)
                    UpdateLookupTables();

                return m_SpriteCharacterLookup;
            }
            internal set { m_SpriteCharacterLookup = value; }
        }
        internal Dictionary<uint, SpriteCharacter> m_SpriteCharacterLookup;

        public List<SpriteGlyph> spriteGlyphTable
        {
            get { return m_SpriteGlyphTable; }
            internal set { m_SpriteGlyphTable = value; }
        }
        [SerializeField]
        private List<SpriteGlyph> m_SpriteGlyphTable = new List<SpriteGlyph>();

        internal Dictionary<uint, SpriteGlyph> m_SpriteGlyphLookup;

        /// <summary>
        /// List which contains the Fallback font assets for this font.
        /// </summary>
        [SerializeField]
        public List<SpriteAsset> fallbackSpriteAssets;

        internal bool m_IsSpriteAssetLookupTablesDirty = false;


        void Awake() {}


        /// <summary>
        /// Create a material for the sprite asset.
        /// </summary>
        /// <returns></returns>
        /*Material GetDefaultSpriteMaterial()
        {
            //isEditingAsset = true;
            TextShaderUtilities.GetShaderPropertyIDs();

            // Add a new material
            Shader shader = Shader.Find("TextMeshPro/Sprite");
            Material tempMaterial = new Material(shader);
            tempMaterial.SetTexture(TextShaderUtilities.ID_MainTex, spriteSheet);
            tempMaterial.hideFlags = HideFlags.HideInHierarchy;

            #if UNITY_EDITOR
            UnityEditor.AssetDatabase.AddObjectToAsset(tempMaterial, this);
            UnityEditor.AssetDatabase.ImportAsset(UnityEditor.AssetDatabase.GetAssetPath(this));
            #endif
            //isEditingAsset = false;

            return tempMaterial;
        }*/


        /// <summary>
        /// Function to update the sprite name and unicode lookup tables.
        /// This function should be called when a sprite's name or unicode value changes or when a new sprite is added.
        /// </summary>
        public void UpdateLookupTables()
        {
            width = m_SpriteAtlasTexture.width;
            height = m_SpriteAtlasTexture.height;
            //Debug.Log("Updating [" + this.name + "] Lookup tables.");

            // Initialize / Clear glyph index lookup dictionary.
            if (m_GlyphIndexLookup == null)
                m_GlyphIndexLookup = new Dictionary<uint, int>();
            else
                m_GlyphIndexLookup.Clear();

            //
            if (m_SpriteGlyphLookup == null)
                m_SpriteGlyphLookup = new Dictionary<uint, SpriteGlyph>();
            else
                m_SpriteGlyphLookup.Clear();

            // Initialize SpriteGlyphLookup
            for (int i = 0; i < m_SpriteGlyphTable.Count; i++)
            {
                SpriteGlyph spriteGlyph = m_SpriteGlyphTable[i];
                uint glyphIndex = spriteGlyph.index;

                if (m_GlyphIndexLookup.ContainsKey(glyphIndex) == false)
                    m_GlyphIndexLookup.Add(glyphIndex, i);

                if (m_SpriteGlyphLookup.ContainsKey(glyphIndex) == false)
                    m_SpriteGlyphLookup.Add(glyphIndex, spriteGlyph);
            }

            // Initialize name lookup
            if (m_NameLookup == null)
                m_NameLookup = new Dictionary<int, int>();
            else
                m_NameLookup.Clear();


            // Initialize character lookup
            if (m_SpriteCharacterLookup == null)
                m_SpriteCharacterLookup = new Dictionary<uint, SpriteCharacter>();
            else
                m_SpriteCharacterLookup.Clear();


            // Populate Sprite Character lookup tables
            for (int i = 0; i < m_SpriteCharacterTable.Count; i++)
            {
                SpriteCharacter spriteCharacter = m_SpriteCharacterTable[i];

                // Make sure sprite character is valid
                if (spriteCharacter == null)
                    continue;

                uint glyphIndex = spriteCharacter.glyphIndex;

                // Lookup the glyph for this character
                if (m_SpriteGlyphLookup.ContainsKey(glyphIndex) == false)
                    continue;

                // Assign glyph and text asset to this character
                spriteCharacter.glyph = m_SpriteGlyphLookup[glyphIndex];
                spriteCharacter.textAsset = this;

                int nameHashCode = TextUtilities.GetHashCodeCaseInSensitive(m_SpriteCharacterTable[i].name);

                if (m_NameLookup.ContainsKey(nameHashCode) == false)
                    m_NameLookup.Add(nameHashCode, i);

                uint unicode = m_SpriteCharacterTable[i].unicode;

                if (unicode != 0xFFFE && m_SpriteCharacterLookup.ContainsKey(unicode) == false)
                    m_SpriteCharacterLookup.Add(unicode, spriteCharacter);
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
            if (m_SpriteCharacterLookup == null)
                UpdateLookupTables();

            SpriteCharacter spriteCharacter;

            if (m_SpriteCharacterLookup.TryGetValue(unicode, out spriteCharacter))
                return (int)spriteCharacter.glyphIndex;

            return -1;
        }

        /// <summary>
        /// Returns the index of the sprite for the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public int GetSpriteIndexFromName(string name)
        {
            if (m_NameLookup == null)
                UpdateLookupTables();

            int hashCode = TextUtilities.GetHashCodeCaseInSensitive(name);

            return GetSpriteIndexFromHashcode(hashCode);
        }

        /// <summary>
        /// Search through the given sprite asset and its fallbacks for the specified sprite matching the given unicode character.
        /// </summary>
        /// <param name="spriteAsset">The sprite asset asset to search for the given unicode.</param>
        /// <param name="unicode">The unicode character to find.</param>
        /// <param name="includeFallbacks">Include fallback sprite assets in the search?</param>
        /// <param name="spriteIndex">The index of the sprite in the sprite asset (if found)</param>
        /// <returns></returns>
        public static SpriteAsset SearchForSpriteByUnicode(SpriteAsset spriteAsset, uint unicode, bool includeFallbacks, out int spriteIndex)
        {
            // Check to make sure sprite asset is not null
            if (spriteAsset == null) { spriteIndex = -1; return null; }

            // Get sprite index for the given unicode
            spriteIndex = spriteAsset.GetSpriteIndexFromUnicode(unicode);
            if (spriteIndex != -1)
                return spriteAsset;

            // Initialize list to track instance of Sprite Assets that have already been searched.
            HashSet<int> searchedSpriteAssets = new HashSet<int>();

            // Get instance ID of sprite asset and add to list.
            int id = spriteAsset.GetInstanceID();
            searchedSpriteAssets.Add(id);

            // Search potential fallback sprite assets if includeFallbacks is true.
            if (includeFallbacks && spriteAsset.fallbackSpriteAssets != null && spriteAsset.fallbackSpriteAssets.Count > 0)
                return SearchForSpriteByUnicodeInternal(spriteAsset.fallbackSpriteAssets, unicode, true, searchedSpriteAssets, out spriteIndex);

            // Search default sprite asset potentially assigned in the TMP Settings.
            //if (includeFallbacks && TMP_Settings.defaultSpriteAsset != null)
            //    return SearchForSpriteByUnicodeInternal(TMP_Settings.defaultSpriteAsset, unicode, true, out spriteIndex);

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
        private static SpriteAsset SearchForSpriteByUnicodeInternal(List<SpriteAsset> spriteAssets, uint unicode, bool includeFallbacks, HashSet<int> searchedSpriteAssets, out int spriteIndex)
        {
            for (int i = 0; i < spriteAssets.Count; i++)
            {
                SpriteAsset temp = spriteAssets[i];
                if (temp == null) continue;

                int id = temp.GetInstanceID();

                // Skip sprite asset if it has already been searched.
                if (searchedSpriteAssets.Add(id) == false)
                    continue;

                temp = SearchForSpriteByUnicodeInternal(temp, unicode, includeFallbacks, searchedSpriteAssets, out spriteIndex);

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
        private static SpriteAsset SearchForSpriteByUnicodeInternal(SpriteAsset spriteAsset, uint unicode, bool includeFallbacks, HashSet<int> searchedSpriteAssets, out int spriteIndex)
        {
            // Get sprite index for the given unicode
            spriteIndex = spriteAsset.GetSpriteIndexFromUnicode(unicode);

            if (spriteIndex != -1)
                return spriteAsset;

            if (includeFallbacks && spriteAsset.fallbackSpriteAssets != null && spriteAsset.fallbackSpriteAssets.Count > 0)
                return SearchForSpriteByUnicodeInternal(spriteAsset.fallbackSpriteAssets, unicode, true, searchedSpriteAssets, out spriteIndex);

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
        public static SpriteAsset SearchForSpriteByHashCode(SpriteAsset spriteAsset, int hashCode, bool includeFallbacks, out int spriteIndex, TextSettings textSettings = null)
        {
            // Make sure sprite asset is not null
            if (spriteAsset == null) { spriteIndex = -1; return null; }

            spriteIndex = spriteAsset.GetSpriteIndexFromHashcode(hashCode);
            if (spriteIndex != -1)
                return spriteAsset;

            // Initialize or clear list to Sprite Assets that have already been searched.
            HashSet<int> searchedSpriteAssets = new HashSet<int>();

            int id = spriteAsset.GetHashCode();

            // Add to list of font assets already searched.
            searchedSpriteAssets.Add(id);

            SpriteAsset tempSpriteAsset;

            // Search potential local fallbacks assigned to the sprite asset.
            if (includeFallbacks && spriteAsset.fallbackSpriteAssets != null && spriteAsset.fallbackSpriteAssets.Count > 0)
            {
                tempSpriteAsset = SearchForSpriteByHashCodeInternal(spriteAsset.fallbackSpriteAssets, hashCode, true, searchedSpriteAssets, out spriteIndex);

                if (spriteIndex != -1)
                    return tempSpriteAsset;
            }

            // Early exist if text settings is null
            if (textSettings == null)
            {
                spriteIndex = -1;
                return null;
            }

            // Search default sprite asset potentially assigned in the Text Settings.
            if (includeFallbacks && textSettings.defaultSpriteAsset != null)
            {
                tempSpriteAsset = SearchForSpriteByHashCodeInternal(textSettings.defaultSpriteAsset, hashCode, true, searchedSpriteAssets, out spriteIndex);

                if (spriteIndex != -1)
                    return tempSpriteAsset;
            }

            // Clear search list since we are now looking for the missing sprite character.
            searchedSpriteAssets.Clear();

            uint missingSpriteCharacterUnicode = textSettings.missingSpriteCharacterUnicode;

            // Get sprite index for the given unicode
            spriteIndex = spriteAsset.GetSpriteIndexFromUnicode(missingSpriteCharacterUnicode);
            if (spriteIndex != -1)
                return spriteAsset;

            // Add current sprite asset to list of assets already searched.
            searchedSpriteAssets.Add(id);

            // Search for the missing sprite character in the local sprite asset and potential fallbacks.
            if (includeFallbacks && spriteAsset.fallbackSpriteAssets != null && spriteAsset.fallbackSpriteAssets.Count > 0)
            {
                tempSpriteAsset = SearchForSpriteByUnicodeInternal(spriteAsset.fallbackSpriteAssets, missingSpriteCharacterUnicode, true, searchedSpriteAssets, out spriteIndex);

                if (spriteIndex != -1)
                    return tempSpriteAsset;
            }

            // Search for the missing sprite character in the default sprite asset and potential fallbacks.
            if (includeFallbacks && textSettings.defaultSpriteAsset != null)
            {
                tempSpriteAsset = SearchForSpriteByUnicodeInternal(textSettings.defaultSpriteAsset, missingSpriteCharacterUnicode, true, searchedSpriteAssets,out spriteIndex);
                if (spriteIndex != -1)
                    return tempSpriteAsset;
            }

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
        private static SpriteAsset SearchForSpriteByHashCodeInternal(List<SpriteAsset> spriteAssets, int hashCode, bool searchFallbacks, HashSet<int> searchedSpriteAssets, out int spriteIndex)
        {
            // Search through the list of sprite assets
            for (int i = 0; i < spriteAssets.Count; i++)
            {
                SpriteAsset temp = spriteAssets[i];
                if (temp == null) continue;

                int id = temp.GetHashCode();

                // Skip sprite asset if it has already been searched.
                if (searchedSpriteAssets.Add(id) == false)
                    continue;

                temp = SearchForSpriteByHashCodeInternal(temp, hashCode, searchFallbacks, searchedSpriteAssets, out spriteIndex);

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
        private static SpriteAsset SearchForSpriteByHashCodeInternal(SpriteAsset spriteAsset, int hashCode, bool searchFallbacks, HashSet<int> searchedSpriteAssets, out int spriteIndex)
        {
            // Get the sprite for the given hash code.
            spriteIndex = spriteAsset.GetSpriteIndexFromHashcode(hashCode);
            if (spriteIndex != -1)
                return spriteAsset;

            if (searchFallbacks && spriteAsset.fallbackSpriteAssets != null && spriteAsset.fallbackSpriteAssets.Count > 0)
                return SearchForSpriteByHashCodeInternal(spriteAsset.fallbackSpriteAssets, hashCode, true, searchedSpriteAssets, out spriteIndex);

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
