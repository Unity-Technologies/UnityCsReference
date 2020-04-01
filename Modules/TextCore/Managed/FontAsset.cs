// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Bindings;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore
{
    /// <summary>
    /// Contains the font asset for the specified font weight styles.
    /// </summary>
    [Serializable]
    struct FontWeights
    {
        public FontAsset regularTypeface;
        public FontAsset italicTypeface;
    }

    // Structure which holds the font creation settings
    [Serializable]
    struct FontAssetCreationSettings
    {
        public string fontFileGUID;
        public int pointSizeSamplingMode;
        public int pointSize;
        public int padding;
        public int packingMode;
        public int atlasWidth;
        public int atlasHeight;
        public int characterSetSelectionMode;
        public string characterSequence;
        public string referencedFontAssetGUID;
        public string referencedTextAssetGUID;
        public int fontStyle;
        public float fontStyleModifier;
        public int renderMode;
        public bool includeFontFeatures;

        internal FontAssetCreationSettings(string fontFileGUID, int pointSize, int pointSizeSamplingMode, int padding, int packingMode, int atlasWidth, int atlasHeight, int characterSelectionMode, string characterSet, int renderMode)
        {
            this.fontFileGUID = fontFileGUID;
            this.pointSize = pointSize;
            this.pointSizeSamplingMode = pointSizeSamplingMode;
            this.padding = padding;
            this.packingMode = packingMode;
            this.atlasWidth = atlasWidth;
            this.atlasHeight = atlasHeight;
            this.characterSequence = characterSet;
            this.characterSetSelectionMode = characterSelectionMode;
            this.renderMode = renderMode;
            this.referencedFontAssetGUID = string.Empty;
            this.referencedTextAssetGUID = string.Empty;
            this.fontStyle = 0;
            this.fontStyleModifier = 0;
            this.includeFontFeatures = false;
        }
    }

    [Serializable]
    internal class FontAsset : ScriptableObject
    {
        /// <summary>
        /// The version of the font asset class.
        /// Version 1.1.0 adds support for the new TextCore.FontEngine and Dynamic SDF system.
        /// </summary>
        public string version
        {
            get { return m_Version; }
            set { m_Version = value; }
        }
        [SerializeField]
        string m_Version = "1.1.0";

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

        /// <summary>
        /// The general information about the font face.
        /// </summary>
        public FaceInfo faceInfo
        {
            get { return m_FaceInfo; }
            set { m_FaceInfo = value; }
        }
        [SerializeField]
        FaceInfo m_FaceInfo;

        /// <summary>
        /// This field is set when the font asset is first created.
        /// </summary>
        [SerializeField]
        internal string m_SourceFontFileGUID;

        /// <summary>
        /// Editor-Only internal reference to source font file.
        /// TODO: This should be enclosed in #if UNITY_EDITOR once Katana tests issue related to serialized properties in the Editor is resolved.
        /// </summary>
        [SerializeField]
        internal Font m_SourceFontFile_EditorRef;

        /// <summary>
        /// Source font file when atlas population mode is set to dynamic. Null when the atlas population mode is set to static.
        /// </summary>
        public Font sourceFontFile
        {
            get { return m_SourceFontFile; }
        }
        [SerializeField]
        internal Font m_SourceFontFile;

        internal enum AtlasPopulationMode
        {
            Static = 0x0,
            Dynamic = 0x1,
        }

        /// <summary>
        /// The atlas population mode.
        /// When set to Dynamic, the sourceFontFile is set to reference the source font file. Otherwise it is set to null.
        /// </summary>
        public AtlasPopulationMode atlasPopulationMode
        {
            get { return m_AtlasPopulationMode; }

            set
            {
                m_AtlasPopulationMode = value;

                //if (m_AtlasPopulationMode == AtlasPopulationMode.Static)
                //    m_SourceFontFile = null;
                //else if (m_AtlasPopulationMode == AtlasPopulationMode.Dynamic)
                //    m_SourceFontFile = m_SourceFontFile_EditorRef;
            }
        }
        [SerializeField]
        private AtlasPopulationMode m_AtlasPopulationMode;

        /// <summary>
        /// List of glyphs contained in the font asset.
        /// </summary>
        public List<Glyph> glyphTable
        {
            get { return m_GlyphTable; }
            set { m_GlyphTable = value; }
        }
        [SerializeField]
        List<Glyph> m_GlyphTable = new List<Glyph>();

        /// <summary>
        /// Dictionary used to lookup glyphs contained in the font asset by their index.
        /// </summary>
        public Dictionary<uint, Glyph> glyphLookupTable
        {
            get
            {
                if (m_GlyphLookupDictionary == null)
                    ReadFontAssetDefinition();

                return m_GlyphLookupDictionary;
            }
        }
        Dictionary<uint, Glyph> m_GlyphLookupDictionary;

        /// <summary>
        /// List containing the characters of the given font asset.
        /// </summary>
        public List<Character> characterTable
        {
            get { return m_CharacterTable; }
            set { m_CharacterTable = value; }
        }
        [SerializeField]
        List<Character> m_CharacterTable = new List<Character>();

        /// <summary>
        /// Dictionary used to lookup characters contained in the font asset by their unicode values.
        /// </summary>
        public Dictionary<uint, Character> characterLookupTable
        {
            get
            {
                if (m_CharacterLookupDictionary == null)
                    ReadFontAssetDefinition();


                return m_CharacterLookupDictionary;
            }
        }
        Dictionary<uint, Character> m_CharacterLookupDictionary;

        /// <summary>
        /// The font atlas used by this font asset.
        /// This is always the texture at index [0] of the fontAtlasTextures.
        /// </summary>
        public Texture2D atlasTexture
        {
            get
            {
                if (m_AtlasTexture == null)
                {
                    m_AtlasTexture = atlasTextures[0];
                }

                return m_AtlasTexture;
            }
        }
        Texture2D m_AtlasTexture;

        /// <summary>
        /// Array of atlas textures that contain the glyphs used by this font asset.
        /// </summary>
        public Texture2D[] atlasTextures
        {
            get
            {
                if (m_AtlasTextures == null)
                {
                    //
                }

                return m_AtlasTextures;
            }

            set
            {
                m_AtlasTextures = value;
            }
        }
        [SerializeField]
        Texture2D[] m_AtlasTextures;

        /// <summary>
        /// Index of the font atlas texture that still has available space to add new glyphs.
        /// Once an atlas texture is full, the index is increased to point to the next element in the m_AtlasTextures array.
        /// TODO:
        /// </summary>
        [SerializeField]
        internal int m_AtlasTextureIndex;

        /// <summary>
        /// The width of the atlas texture(s) used by this font asset.
        /// </summary>
        public int atlasWidth
        {
            get { return m_AtlasWidth; }
            set { m_AtlasWidth = value; }
        }
        [SerializeField]
        private int m_AtlasWidth;

        /// <summary>
        /// The height of the atlas texture(s) used by this font asset.
        /// </summary>
        public int atlasHeight
        {
            get { return m_AtlasHeight; }
            set { m_AtlasHeight = value; }
        }
        [SerializeField]
        private int m_AtlasHeight;

        /// <summary>
        /// The padding used between glyphs contained in the atlas texture(s) used by this font asset.
        /// </summary>
        public int atlasPadding
        {
            get { return m_AtlasPadding; }
            set { m_AtlasPadding = value; }
        }
        [SerializeField]
        private int m_AtlasPadding;

        public GlyphRenderMode atlasRenderMode
        {
            get { return m_AtlasRenderMode; }
            set { m_AtlasRenderMode = value; }
        }
        [SerializeField]
        private GlyphRenderMode m_AtlasRenderMode;

        /// <summary>
        /// List of spaces occupied by glyphs in a given texture.
        /// </summary>
        internal List<GlyphRect> usedGlyphRects
        {
            get { return m_UsedGlyphRects; }
            set { m_UsedGlyphRects = value; }
        }
        [SerializeField]
        List<GlyphRect> m_UsedGlyphRects;

        /// <summary>
        /// List of spaces available in a given texture to add new glyphs.
        /// </summary>
        internal List<GlyphRect> freeGlyphRects
        {
            get { return m_FreeGlyphRects; }
            set { m_FreeGlyphRects = value; }
        }
        [SerializeField]
        List<GlyphRect> m_FreeGlyphRects;

        /// <summary>
        /// Used in the process of adding new glyphs to the atlas texture.
        /// </summary>
        private List<uint> m_GlyphIndexes = new List<uint>();
        private Dictionary<uint, List<uint>> s_GlyphLookupMap = new Dictionary<uint, List<uint>>();

        /// <summary>
        /// The material used by this asset.
        /// </summary>
        public Material material
        {
            get { return m_Material; }
            set
            {
                m_Material = value;
                m_MaterialHashCode = TextUtilities.GetHashCodeCaseInSensitive(m_Material.name);
            }
        }
        [SerializeField]
        Material m_Material;

        /// <summary>
        /// HashCode based on the name of the material assigned to this asset.
        /// </summary>
        public int materialHashCode
        {
            get { return m_MaterialHashCode; }
            set
            {
                if (m_MaterialHashCode == 0)
                    m_MaterialHashCode = TextUtilities.GetHashCodeCaseInSensitive(m_Material.name);

                m_MaterialHashCode = value;
            }
        }
        [SerializeField]
        internal int m_MaterialHashCode;

        /// <summary>
        ///
        /// </summary>
        public KerningTable kerningTable
        {
            get { return m_KerningTable; }
            set { m_KerningTable = value; }
        }
        [SerializeField]
        internal KerningTable m_KerningTable = new KerningTable();

        /// <summary>
        /// Dictionary containing the kerning data
        /// </summary>
        public Dictionary<int, KerningPair> kerningLookupDictionary
        {
            get { return m_KerningLookupDictionary; }
        }
        Dictionary<int, KerningPair> m_KerningLookupDictionary;

        /// <summary>
        /// TODO: This is only used to display an empty kerning pair in the Editor. This should be implemented more cleanly.
        /// </summary>
        [SerializeField]
        internal KerningPair m_EmptyKerningPair;

        /// <summary>
        /// List containing the Fallback font assets for this font.
        /// </summary>
        public List<FontAsset> fallbackFontAssetTable
        {
            get { return m_FallbackFontAssetTable; }
            set { m_FallbackFontAssetTable = value; }
        }
        [SerializeField]
        internal List<FontAsset> m_FallbackFontAssetTable;


        public FontAssetCreationSettings fontAssetCreationSettings
        {
            get { return m_FontAssetCreationSettings; }
            set { m_FontAssetCreationSettings = value; }
        }

        [SerializeField]
        internal FontAssetCreationSettings m_FontAssetCreationSettings;

        /// <summary>
        /// Array containing font assets to be used as alternative typefaces for the various potential font weights of this font asset.
        /// </summary>
        public FontWeights[] fontWeightTable
        {
            get { return m_FontWeightTable; }
            set { m_FontWeightTable = value; }
        }
        [SerializeField]
        internal FontWeights[] m_FontWeightTable = new FontWeights[10];

        /// <summary>
        /// Defines the dilation of the text when using regular style.
        /// </summary>
        public float regularStyleWeight { get { return m_RegularStyleWeight; } set { m_RegularStyleWeight = value; } }
        [SerializeField]
        float m_RegularStyleWeight = 0;

        /// <summary>
        /// The spacing between characters when using regular style.
        /// </summary>
        public float regularStyleSpacing { get { return m_RegularStyleSpacing; } set { m_RegularStyleSpacing = value; } }
        [SerializeField]
        float m_RegularStyleSpacing = 0;

        /// <summary>
        /// Defines the dilation of the text when using bold style.
        /// </summary>
        public float boldStyleWeight { get { return m_BoldStyleWeight; } set { m_BoldStyleWeight = value; } }
        [SerializeField]
        float m_BoldStyleWeight = 0.75f;

        /// <summary>
        /// The spacing between characters when using regular style.
        /// </summary>
        public float boldStyleSpacing { get { return m_BoldStyleSpacing; } set { m_BoldStyleSpacing = value; } }
        [SerializeField]
        float m_BoldStyleSpacing = 7f;

        /// <summary>
        /// Defines the slant of the text when using italic style.
        /// </summary>
        public byte italicStyleSlant { get { return m_ItalicStyleSlant; } set { m_ItalicStyleSlant = value; } }
        [SerializeField]
        byte m_ItalicStyleSlant = 35;

        /// <summary>
        /// The number of spaces that a tab represents.
        /// </summary>
        public byte tabMultiple { get { return m_TabMultiple; } set { m_TabMultiple = value; } }
        [SerializeField]
        byte m_TabMultiple = 10;

        internal bool m_IsFontAssetLookupTablesDirty = false;


        public static FontAsset CreateFontAsset(Font font)
        {
            return CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
        }

        /// <summary>
        /// Create new instance of a font asset.
        /// </summary>
        /// <param name="font">The source font file.</param>
        /// <param name="samplingPointSize">The sampling point size.</param>
        /// <param name="atlasPadding">The padding / spread between individual glyphs in the font asset.</param>
        /// <param name="renderMode"></param>
        /// <param name="atlasWidth">The atlas texture width.</param>
        /// <param name="atlasHeight">The atlas texture height.</param>
        /// <param name="atlasPopulationMode"></param>
        /// <returns></returns>
        public static FontAsset CreateFontAsset(Font font, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight, AtlasPopulationMode atlasPopulationMode = AtlasPopulationMode.Dynamic)
        {
            FontAsset fontAsset = ScriptableObject.CreateInstance<FontAsset>();

            // Set face information
            FontEngine.InitializeFontEngine();
            FontEngine.LoadFontFace(font, samplingPointSize);

            fontAsset.faceInfo = FontEngine.GetFaceInfo();

            // Set font reference and GUID
            if (atlasPopulationMode == AtlasPopulationMode.Dynamic)
                fontAsset.m_SourceFontFile = font;

            // Set persistent reference to source font file in the Editor only.
            //string guid;
            //long localID;
            //UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(font, out guid, out localID);
            //fontAsset.m_SourceFontFileGUID = guid;
            fontAsset.m_SourceFontFile_EditorRef = font;

            fontAsset.atlasPopulationMode = atlasPopulationMode;

            fontAsset.atlasWidth = atlasWidth;
            fontAsset.atlasHeight = atlasHeight;
            fontAsset.atlasPadding = atlasPadding;
            fontAsset.atlasRenderMode = renderMode;

            // Initialize array for the font atlas textures.
            fontAsset.atlasTextures = new Texture2D[1];

            // Create and add font atlas texture.
            Texture2D texture = new Texture2D(0, 0, TextureFormat.Alpha8, false);

            //texture.name = assetName + " Atlas";
            fontAsset.atlasTextures[0] = texture;

            // Add free rectangle of the size of the texture.
            int packingModifier;
            if (((GlyphRasterModes)renderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP)
            {
                packingModifier = 0;

                // Optimize by adding static ref to shader.
                Material tmp_material = new Material(ShaderUtilities.ShaderRef_MobileBitmap);

                //tmp_material.name = texture.name + " Material";
                tmp_material.SetTexture(ShaderUtilities.ID_MainTex, texture);
                tmp_material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
                tmp_material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);

                fontAsset.material = tmp_material;
            }
            else
            {
                packingModifier = 1;

                // Optimize by adding static ref to shader.
                Material tmp_material = new Material(ShaderUtilities.ShaderRef_MobileSDF);

                //tmp_material.name = texture.name + " Material";
                tmp_material.SetTexture(ShaderUtilities.ID_MainTex, texture);
                tmp_material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
                tmp_material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);

                tmp_material.SetFloat(ShaderUtilities.ID_GradientScale, atlasPadding + packingModifier);

                tmp_material.SetFloat(ShaderUtilities.ID_WeightNormal, fontAsset.regularStyleWeight);
                tmp_material.SetFloat(ShaderUtilities.ID_WeightBold, fontAsset.boldStyleWeight);

                fontAsset.material = tmp_material;
            }

            fontAsset.freeGlyphRects = new List<GlyphRect>() { new GlyphRect(0, 0, atlasWidth - packingModifier, atlasHeight - packingModifier) };
            fontAsset.usedGlyphRects = new List<GlyphRect>();

            fontAsset.ReadFontAssetDefinition();

            return fontAsset;
        }

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
        }


        /// <summary>
        /// Read the various data tables of the font asset to populate its different dictionaries to allow for faster lookup of related font asset data.
        /// </summary>
        internal void InitializeDictionaryLookupTables()
        {
            // Create new instance of the glyph lookup dictionary or clear the existing one.
            if (m_GlyphLookupDictionary == null)
                m_GlyphLookupDictionary = new Dictionary<uint, Glyph>();
            else
                m_GlyphLookupDictionary.Clear();

            // Add the characters contained in the character table into the dictionary for faster lookup.
            for (int i = 0; i < m_GlyphTable.Count; i++)
            {
                Glyph glyph = m_GlyphTable[i];

                uint index = glyph.index;

                if (m_GlyphLookupDictionary.ContainsKey(index) == false)
                    m_GlyphLookupDictionary.Add(index, glyph);
            }

            // Create new instance of the character lookup dictionary or clear the existing one.
            if (m_CharacterLookupDictionary == null)
                m_CharacterLookupDictionary = new Dictionary<uint, Character>();
            else
                m_CharacterLookupDictionary.Clear();

            // Add the characters contained in the character table into the dictionary for faster lookup.
            for (int i = 0; i < m_CharacterTable.Count; i++)
            {
                Character character = m_CharacterTable[i];

                uint unicode = character.unicode;

                if (m_CharacterLookupDictionary.ContainsKey(unicode) == false)
                    m_CharacterLookupDictionary.Add(unicode, character);

                if (m_GlyphLookupDictionary.ContainsKey(character.glyphIndex))
                    character.glyph = m_GlyphLookupDictionary[character.glyphIndex];
            }

            // Read Font Features which will include kerning data.
            // TODO

            // Read Kerning pairs and update Kerning pair dictionary for faster lookup.
            if (m_KerningLookupDictionary == null)
                m_KerningLookupDictionary = new Dictionary<int, KerningPair>();
            else
                m_KerningLookupDictionary.Clear();

            List<KerningPair> glyphPairAdjustmentRecord = m_KerningTable.kerningPairs;
            if (glyphPairAdjustmentRecord != null)
            {
                for (int i = 0; i < glyphPairAdjustmentRecord.Count; i++)
                {
                    KerningPair pair = glyphPairAdjustmentRecord[i];

                    KerningPairKey uniqueKey = new KerningPairKey(pair.firstGlyph, pair.secondGlyph);

                    if (m_KerningLookupDictionary.ContainsKey((int)uniqueKey.key) == false)
                    {
                        m_KerningLookupDictionary.Add((int)uniqueKey.key, pair);
                    }
                    else
                    {
                        if (!TextSettings.warningsDisabled)
                            Debug.LogWarning("Kerning Key for [" + uniqueKey.ascii_Left + "] and [" + uniqueKey.ascii_Right + "] already exists.");
                    }
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        internal void ReadFontAssetDefinition()
        {
            // Initialize lookup tables for characters and glyphs.
            InitializeDictionaryLookupTables();

            // Add Tab char(9) to Dictionary.
            if (m_CharacterLookupDictionary.ContainsKey(9) == false)
            {
                Glyph glyph = new Glyph(0, new GlyphMetrics(0, 0, 0, 0, m_FaceInfo.tabWidth * tabMultiple), GlyphRect.zero, 1.0f, 0);
                m_CharacterLookupDictionary.Add(9, new Character(9, glyph));
            }

            // Add Linefeed LF char(10) and Carriage Return CR char(13)
            if (m_CharacterLookupDictionary.ContainsKey(10) == false)
            {
                Glyph glyph = new Glyph(0, new GlyphMetrics(10, 0, 0, 0, 0), GlyphRect.zero, 1.0f, 0);
                m_CharacterLookupDictionary.Add(10, new Character(10, glyph));

                if (!m_CharacterLookupDictionary.ContainsKey(13))
                    m_CharacterLookupDictionary.Add(13, new Character(13, glyph));
            }

            // Add Zero Width Space 8203 (0x200B)
            if (m_CharacterLookupDictionary.ContainsKey(8203) == false)
            {
                Glyph glyph = new Glyph(0, new GlyphMetrics(0, 0, 0, 0, 0), GlyphRect.zero, 1.0f, 0);
                m_CharacterLookupDictionary.Add(8203, new Character(8203, glyph));
            }

            // Add Zero Width Non-Breaking Space 8288 (0x2060)
            if (m_CharacterLookupDictionary.ContainsKey(8288) == false)
            {
                Glyph glyph = new Glyph(0, new GlyphMetrics(0, 0, 0, 0, 0), GlyphRect.zero, 1.0f, 0);
                m_CharacterLookupDictionary.Add(8288, new Character(8288, glyph));
            }

            // Set Cap Height
            if (m_FaceInfo.capLine == 0 && m_CharacterLookupDictionary.ContainsKey(72))
                m_FaceInfo.capLine = m_CharacterLookupDictionary[72].glyph.metrics.horizontalBearingY;

            // Adjust Font Scale for compatibility reasons
            if (m_FaceInfo.scale == 0)
                m_FaceInfo.scale = 1.0f;

            // Set Strikethrough Offset (if needed)
            if (m_FaceInfo.strikethroughOffset == 0)
                m_FaceInfo.strikethroughOffset = m_FaceInfo.capLine / 2.5f;

            // Set Padding value for legacy font assets.
            if (m_AtlasPadding == 0)
            {
                if (material.HasProperty(ShaderUtilities.ID_GradientScale))
                    m_AtlasPadding = (int)material.GetFloat(ShaderUtilities.ID_GradientScale) - 1;
            }

            // Compute Hashcode for the font asset name
            m_HashCode = TextUtilities.GetHashCodeCaseInSensitive(name);

            // Compute Hashcode for the material name
            m_MaterialHashCode = TextUtilities.GetHashCodeCaseInSensitive(material.name);
        }

        /// <summary>
        /// Sort the Character table by Unicode values.
        /// </summary>
        internal void SortCharacterTable()
        {
            if (m_CharacterTable != null && m_CharacterTable.Count > 0)
                m_CharacterTable = m_CharacterTable.OrderBy(c => c.unicode).ToList();
        }

        /// <summary>
        /// Sort the Glyph table by index values.
        /// </summary>
        internal void SortGlyphTable()
        {
            if (m_GlyphTable != null && m_GlyphTable.Count > 0)
                m_GlyphTable = m_GlyphTable.OrderBy(c => c.index).ToList();
        }

        /// <summary>
        /// Sort both glyph and character tables.
        /// </summary>
        internal void SortGlyphAndCharacterTables()
        {
            SortGlyphTable();
            SortCharacterTable();
        }

        /// <summary>
        /// Function to check if a certain character exists in the font asset.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        internal bool HasCharacter(int character)
        {
            if (m_CharacterLookupDictionary == null)
                return false;

            return m_CharacterLookupDictionary.ContainsKey((uint)character);
        }

        /// <summary>
        /// Function to check if a certain character exists in the font asset.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        internal bool HasCharacter(char character)
        {
            if (m_CharacterLookupDictionary == null)
                return false;

            return m_CharacterLookupDictionary.ContainsKey(character);
        }

        /// <summary>
        /// Function to check if a character is contained in the font asset with the option to also check through fallback font assets.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="searchFallbacks"></param>
        /// <returns></returns>
        internal bool HasCharacter(char character, bool searchFallbacks)
        {
            // Read font asset definition if it hasn't already been done.
            if (m_CharacterLookupDictionary == null)
            {
                ReadFontAssetDefinition();

                if (m_CharacterLookupDictionary == null)
                    return false;
            }

            // Check font asset
            if (m_CharacterLookupDictionary.ContainsKey(character))
                return true;

            if (searchFallbacks)
            {
                // Check font asset fallbacks
                if (fallbackFontAssetTable != null && fallbackFontAssetTable.Count > 0)
                {
                    for (int i = 0; i < fallbackFontAssetTable.Count && fallbackFontAssetTable[i] != null; i++)
                    {
                        if (fallbackFontAssetTable[i].HasCharacter_Internal(character, searchFallbacks))
                            return true;
                    }
                }

                // Check general fallback font assets.
                if (TextSettings.fallbackFontAssets != null && TextSettings.fallbackFontAssets.Count > 0)
                {
                    for (int i = 0; i < TextSettings.fallbackFontAssets.Count && TextSettings.fallbackFontAssets[i] != null; i++)
                    {
                        if (TextSettings.fallbackFontAssets[i].m_CharacterLookupDictionary == null)
                            TextSettings.fallbackFontAssets[i].ReadFontAssetDefinition();

                        if (TextSettings.fallbackFontAssets[i].m_CharacterLookupDictionary != null && TextSettings.fallbackFontAssets[i].HasCharacter_Internal(character, searchFallbacks))
                            return true;
                    }
                }

                // Check Text Settings Default Font Asset
                if (TextSettings.defaultFontAsset != null)
                {
                    if (TextSettings.defaultFontAsset.m_CharacterLookupDictionary == null)
                        TextSettings.defaultFontAsset.ReadFontAssetDefinition();

                    if (TextSettings.defaultFontAsset.m_CharacterLookupDictionary != null && TextSettings.defaultFontAsset.HasCharacter_Internal(character, searchFallbacks))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Function to check if a character is contained in a font asset with the option to also check through fallback font assets.
        /// This private implementation does not search the fallback font asset in the Text Settings file.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="searchFallbacks"></param>
        /// <returns></returns>
        bool HasCharacter_Internal(char character, bool searchFallbacks)
        {
            // Read font asset definition if it hasn't already been done.
            if (m_CharacterLookupDictionary == null)
            {
                ReadFontAssetDefinition();

                if (m_CharacterLookupDictionary == null)
                    return false;
            }

            // Check font asset
            if (m_CharacterLookupDictionary.ContainsKey(character))
                return true;

            if (searchFallbacks)
            {
                // Check Font Asset Fallback fonts.
                if (fallbackFontAssetTable != null && fallbackFontAssetTable.Count > 0)
                {
                    for (int i = 0; i < fallbackFontAssetTable.Count && fallbackFontAssetTable[i] != null; i++)
                    {
                        if (fallbackFontAssetTable[i].HasCharacter_Internal(character, searchFallbacks))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Function to check if certain characters exists in the font asset. Function returns a list of missing characters.
        /// </summary>
        /// <returns></returns>
        internal bool HasCharacters(string text, out List<char> missingCharacters)
        {
            if (m_CharacterLookupDictionary == null)
            {
                missingCharacters = null;
                return false;
            }

            missingCharacters = new List<char>();

            for (int i = 0; i < text.Length; i++)
            {
                if (!m_CharacterLookupDictionary.ContainsKey(text[i]))
                    missingCharacters.Add(text[i]);
            }

            return missingCharacters.Count == 0;
        }

        /// <summary>
        /// Function to check if certain characters exists in the font asset. Function returns false if any characters are missing.
        /// </summary>
        /// <param name="text">String containing the characters to check</param>
        /// <returns></returns>
        internal bool HasCharacters(string text)
        {
            if (m_CharacterLookupDictionary == null)
                return false;

            for (int i = 0; i < text.Length; i++)
            {
                if (!m_CharacterLookupDictionary.ContainsKey(text[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Function to extract all the characters from a font asset.
        /// </summary>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        internal static string GetCharacters(FontAsset fontAsset)
        {
            string characters = string.Empty;

            for (int i = 0; i < fontAsset.characterTable.Count; i++)
            {
                characters += (char)fontAsset.characterTable[i].unicode;
            }

            return characters;
        }

        /// <summary>
        /// Function which returns an array that contains all the characters from a font asset.
        /// </summary>
        /// <param name="fontAsset"></param>
        /// <returns></returns>
        internal static int[] GetCharactersArray(FontAsset fontAsset)
        {
            int[] characters = new int[fontAsset.characterTable.Count];

            for (int i = 0; i < fontAsset.characterTable.Count; i++)
            {
                characters[i] = (int)fontAsset.characterTable[i].unicode;
            }

            return characters;
        }

        // ================================================================================
        // Properties and functions related to character and glyph additions as well as
        // tacking glyphs that need to be added to various font asset atlas textures.
        // ================================================================================

        /// <summary>
        /// Determines if the font asset is already registered to be updated.
        /// </summary>
        //private bool m_IsAlreadyRegisteredForUpdate;

        /// <summary>
        /// List of glyphs that need to be added / packed in atlas texture.
        /// </summary>
        List<Glyph> m_GlyphsToPack = new List<Glyph>();

        /// <summary>
        /// List of glyphs that have been packed in the atlas texture and ready to be rendered.
        /// </summary>
        List<Glyph> m_GlyphsPacked = new List<Glyph>();

        /// <summary>
        ///
        /// </summary>
        List<Glyph> m_GlyphsToRender = new List<Glyph>();

        /// <summary>
        ///
        /// </summary>
        /// <param name="unicode"></param>
        /// <param name="glyph"></param>
        internal Character AddCharacter_Internal(uint unicode, Glyph glyph)
        {
            // Check if character is already contained in the character table.
            if (m_CharacterLookupDictionary.ContainsKey(unicode))
                return m_CharacterLookupDictionary[unicode];

            uint glyphIndex = glyph.index;

            // Check if glyph is already contained in the glyph table.
            if (m_GlyphLookupDictionary.ContainsKey(glyphIndex) == false)
            {
                if (glyph.glyphRect.width == 0 || glyph.glyphRect.width == 0)
                {
                    // Glyphs with zero width and / or height can be automatically added to font asset.
                    m_GlyphTable.Add(glyph);
                }
                else
                {
                    // Try packing new glyph
                    if (FontEngine.TryPackGlyphInAtlas(glyph, m_AtlasPadding, GlyphPackingMode.ContactPointRule, m_AtlasRenderMode, m_AtlasWidth, m_AtlasHeight, m_FreeGlyphRects, m_UsedGlyphRects) == false)
                    {
                        // TODO: Add handling to create new atlas texture to fit glyph.

                        return null;
                    }

                    m_GlyphsToRender.Add(glyph);
                }
            }

            // Add character to font asset.
            Character character = new Character(unicode, glyph);
            m_CharacterTable.Add(character);
            m_CharacterLookupDictionary.Add(unicode, character);

            //Debug.Log("Adding character [" + (char)unicode + "] with Unicode (" + unicode + ") to [" + this.name + "] font asset.");

            // Schedule glyph to be added to the font atlas texture
            //TM_FontAssetUpdateManager.RegisterFontAssetForUpdate(this);
            UpdateAtlasTexture(); // Temporary until callback system is revised.

            // Makes the changes to the font asset persistent.
            // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
            // Could also add some update registry to handle this.
            //SortGlyphTable();
            //UnityEditor.EditorUtility.SetDirty(this);

            return character;
        }

        /// <summary>
        /// Try adding character using Unicode value to font asset.
        /// </summary>
        /// <param name="unicode">The Unicode value of the character.</param>
        /// <param name="character">The character data if successfully added to the font asset. Null otherwise.</param>
        /// <returns>Returns true if the character has been added. False otherwise.</returns>
        internal bool TryAddCharacter(uint unicode, out Character character)
        {
            // Check if character is already contained in the character table.
            if (m_CharacterLookupDictionary.ContainsKey(unicode))
            {
                character = m_CharacterLookupDictionary[unicode];
                return true;
            }

            character = null;

            // Load font face.
            if (FontEngine.LoadFontFace(sourceFontFile, m_FaceInfo.pointSize) != FontEngineError.Success)
                return false;

            uint glyphIndex = FontEngine.GetGlyphIndex(unicode);
            if (glyphIndex == 0)
                return false;

            // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
            if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
            {
                character = new Character(unicode, m_GlyphLookupDictionary[glyphIndex]);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                // Makes the changes to the font asset persistent.
                // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
                // Could also add some update registry to handle this.
                //SortGlyphTable();
                //UnityEditor.EditorUtility.SetDirty(this);

                return true;
            }

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width == 0 || m_AtlasTextures[m_AtlasTextureIndex].height == 0)
            {
                m_AtlasTextures[m_AtlasTextureIndex].Resize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            Glyph glyph;
            if (FontEngine.TryAddGlyphToTexture(glyphIndex, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyph))
            {
                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                // Add new character
                character = new Character(unicode, glyph);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                // Makes the changes to the font asset persistent.
                // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
                // Could also add some update registry to handle this.
                //SortGlyphTable();
                //UnityEditor.EditorUtility.SetDirty(this);

                return true;
            }

            return false;
        }

        internal void UpdateAtlasTexture()
        {
            // Return if we don't have any glyphs to add to atlas texture.
            // This is possible if UpdateAtlasTexture() was called manually.
            //if (m_GlyphsToPack.Count == 0)
            //    return;

            if (m_GlyphsToRender.Count == 0)
                return;

            //Debug.Log("Updating [" + this.name + "]'s atlas texture.");

            // Pack glyphs in the given atlas texture size.
            // TODO: Packing and glyph render modes should be defined in the font asset.
            //FontEngine.PackGlyphsInAtlas(m_GlyphsToPack, m_GlyphsPacked, m_AtlasPadding, GlyphPackingMode.ContactPointRule, GlyphRenderMode.SDFAA, m_AtlasWidth, m_AtlasHeight, m_FreeGlyphRects, m_UsedGlyphRects);
            //FontEngine.RenderGlyphsToTexture(m_GlyphsPacked, m_AtlasPadding, GlyphRenderMode.SDFAA, m_AtlasTextures[m_AtlasTextureIndex]);

            FontEngine.RenderGlyphsToTexture(m_GlyphsToRender, m_AtlasPadding, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex]);

            // Apply changes to atlas texture
            m_AtlasTextures[m_AtlasTextureIndex].Apply(false, false);

            // Add glyphs that were successfully packed to the glyph table.
            for (int i = 0; i < m_GlyphsToRender.Count /* m_GlyphsPacked.Count */; i++)
            {
                Glyph glyph = m_GlyphsToRender[i]; // m_GlyphsPacked[i];

                // Update atlas texture index
                glyph.atlasIndex = m_AtlasTextureIndex;

                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyph.index, glyph);
            }

            // Clear list of glyphs
            m_GlyphsPacked.Clear();
            m_GlyphsToRender.Clear();

            // Add any remaining glyphs into new atlas texture if multi texture support if enabled.
            if (m_GlyphsToPack.Count > 0)
            {
                /*
                // Create new atlas texture
                Texture2D tex = new Texture2D(m_AtlasWidth, m_AtlasHeight, TextureFormat.Alpha8, false, true);
                tex.SetPixels32(new Color32[m_AtlasWidth * m_AtlasHeight]);
                tex.Apply();

                m_AtlasTextureIndex++;

                if (m_AtlasTextures.Length == m_AtlasTextureIndex)
                    Array.Resize(ref m_AtlasTextures, Mathf.NextPowerOfTwo(m_AtlasTextureIndex + 1));

                m_AtlasTextures[m_AtlasTextureIndex] = tex;
                */
            }

            // Makes the changes to the font asset persistent.
            SortGlyphAndCharacterTables();
            //UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Try adding the characters from the provided array of unicode values to the font asset.
        /// </summary>
        /// <param name="unicodes">Array that contains the unicode characters to be added.</param>
        /// <returns>Returns true if all the characters were successfully added to the font asset. Return false otherwise.</returns>
        public bool TryAddCharacters(uint[] unicodes)
        {
            bool isMissingCharacters = false;

            // Clear list of glyph indexes.
            m_GlyphIndexes.Clear();
            s_GlyphLookupMap.Clear();

            // Load the font face
            FontEngine.LoadFontFace(m_SourceFontFile, m_FaceInfo.pointSize);

            for (int i = 0; i < unicodes.Length; i++)
            {
                uint unicode = unicodes[i];

                // Check if character is already contained in the character table.
                if (m_CharacterLookupDictionary.ContainsKey(unicode))
                    continue;

                // Get the index of the glyph for this unicode value.
                uint glyphIndex = FontEngine.GetGlyphIndex(unicode);

                if (glyphIndex == 0)
                {
                    isMissingCharacters = true;
                    continue;
                }

                // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
                if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
                {
                    Character character = new Character(unicode, m_GlyphLookupDictionary[glyphIndex]);
                    m_CharacterTable.Add(character);
                    m_CharacterLookupDictionary.Add(unicode, character);

                    continue;
                }

                // Check if glyph is already on the list of glyphs to add
                if (s_GlyphLookupMap.ContainsKey(glyphIndex))
                {
                    s_GlyphLookupMap[glyphIndex].Add(unicode);
                    continue;
                }

                s_GlyphLookupMap.Add(glyphIndex, new List<uint> { unicode });
                m_GlyphIndexes.Add(glyphIndex);
            }

            if (m_GlyphIndexes == null || m_GlyphIndexes.Count == 0)
                return true;

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width == 0 || m_AtlasTextures[m_AtlasTextureIndex].height == 0)
            {
                m_AtlasTextures[m_AtlasTextureIndex].Resize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            Glyph[] glyphs;
            bool allCharactersAdded = FontEngine.TryAddGlyphsToTexture(m_GlyphIndexes, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyphs);

            for (int i = 0; i < glyphs.Length && glyphs[i] != null; i++)
            {
                Glyph glyph = glyphs[i];
                uint glyphIndex = glyph.index;

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                // Add new character(s)
                foreach (uint unicode in s_GlyphLookupMap[glyphIndex])
                {
                    Character character = new Character(unicode, glyph);
                    m_CharacterTable.Add(character);
                    m_CharacterLookupDictionary.Add(unicode, character);
                }
            }

            return allCharactersAdded && !isMissingCharacters;
        }

        /// <summary>
        /// Try adding the characters from the provided string to the font asset.
        /// </summary>
        /// <param name="characters">String containing the characters to add to the font asset.</param>
        /// <returns>Returns true if all the characters were successfully added to the font asset. Return false otherwise.</returns>
        public bool TryAddCharacters(string characters)
        {
            // Make sure font asset is set to dynamic and that we have a valid list of characters.
            if (string.IsNullOrEmpty(characters) || m_AtlasPopulationMode == AtlasPopulationMode.Static)
            {
                if (m_AtlasPopulationMode == AtlasPopulationMode.Static)
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because its AtlasPopulationMode is set to Static.", this);
                else
                {
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because the provided character list is Null or Empty.", this);
                }

                return false;
            }

            // Load font face.
            if (FontEngine.LoadFontFace(m_SourceFontFile, m_FaceInfo.pointSize) != FontEngineError.Success)
                return false;

            bool isMissingCharacters = false;
            int characterCount = characters.Length;

            // Clear list / dictionary used to track which glyph needs to be added to atlas texture.
            m_GlyphIndexes.Clear();
            s_GlyphLookupMap.Clear();

            // Iterate over each of the requested characters.
            for (int i = 0; i < characterCount; i++)
            {
                uint unicode = characters[i];

                // Check if character is already contained in the character table.
                if (m_CharacterLookupDictionary.ContainsKey(unicode))
                    continue;

                // Get the index of the glyph for this unicode value.
                uint glyphIndex = FontEngine.GetGlyphIndex(unicode);

                // Skip missing glyphs
                if (glyphIndex == 0)
                {
                    // Might want to keep track and report the missing characters.
                    isMissingCharacters = true;
                    continue;
                }

                // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
                if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
                {
                    Character character = new Character(unicode, m_GlyphLookupDictionary[glyphIndex]);
                    m_CharacterTable.Add(character);
                    m_CharacterLookupDictionary.Add(unicode, character);

                    continue;
                }

                // Check if glyph is already on the list of glyphs to added.
                if (s_GlyphLookupMap.ContainsKey(glyphIndex))
                {
                    // Exclude duplicates.
                    if (s_GlyphLookupMap[glyphIndex].Contains(unicode))
                        continue;

                    s_GlyphLookupMap[glyphIndex].Add(unicode);
                    continue;
                }

                // Add glyph to list of glyphs to add and glyph lookup map.
                s_GlyphLookupMap.Add(glyphIndex, new List<uint> { unicode });
                m_GlyphIndexes.Add(glyphIndex);
            }

            if (m_GlyphIndexes == null || m_GlyphIndexes.Count == 0)
            {
                Debug.LogWarning("No characters will be added to font asset [" + this.name + "] either because they are already present in the font asset or missing from the font file.");
                return true;
            }

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width == 0 || m_AtlasTextures[m_AtlasTextureIndex].height == 0)
            {
                m_AtlasTextures[m_AtlasTextureIndex].Resize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            Glyph[] glyphs;
            bool allCharactersAdded = FontEngine.TryAddGlyphsToTexture(m_GlyphIndexes, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyphs);

            for (int i = 0; i < glyphs.Length && glyphs[i] != null; i++)
            {
                Glyph glyph = glyphs[i];
                uint glyphIndex = glyph.index;

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                // Add new character(s)
                List<uint> unicodes = s_GlyphLookupMap[glyphIndex];
                int unicodeCount = unicodes.Count;

                for (int j = 0; j < unicodeCount; j++)
                {
                    uint unicode = unicodes[j];

                    Character character = new Character(unicode, glyph);
                    m_CharacterTable.Add(character);
                    m_CharacterLookupDictionary.Add(unicode, character);
                }
            }

            return allCharactersAdded && !isMissingCharacters;
        }

        /// <summary>
        /// Clears font asset data including the glyph and character tables and textures.
        /// Function might be changed to Internal and only used in tests.
        /// </summary>
        internal void ClearFontAssetData()
        {
            // Clear glyph and character tables
            if (m_GlyphTable != null)
                m_GlyphTable.Clear();

            if (m_CharacterTable != null)
                m_CharacterTable.Clear();

            // Clear glyph rectangles
            if (m_UsedGlyphRects != null)
                m_UsedGlyphRects.Clear();

            if (m_FreeGlyphRects != null)
            {
                int packingModifier = ((GlyphRasterModes)m_AtlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP ? 0 : 1;
                m_FreeGlyphRects = new List<GlyphRect>() { new GlyphRect(0, 0, m_AtlasWidth - packingModifier, m_AtlasHeight - packingModifier) };
            }

            if (m_GlyphsToPack != null)
                m_GlyphsToPack.Clear();

            if (m_GlyphsPacked != null)
                m_GlyphsPacked.Clear();

            // Clear Glyph Adjustment Table
            if (m_KerningTable != null && m_KerningTable.kerningPairs != null)
                m_KerningTable.kerningPairs.Clear();

            m_AtlasTextureIndex = 0;

            // Clear atlas textures
            if (m_AtlasTextures != null)
            {
                for (int i = 0; i < m_AtlasTextures.Length; i++)
                {
                    Texture2D texture = m_AtlasTextures[i];

                    if (texture == null)
                        continue;

                    // Verify texture size hasn't changed.
                    if (texture.width != m_AtlasWidth || texture.height != m_AtlasHeight)
                        texture.Resize(m_AtlasWidth, m_AtlasHeight, TextureFormat.Alpha8, false);

                    // Clear texture atlas
                    FontEngine.ResetAtlasTexture(texture);
                    texture.Apply();

                    if (i == 0)
                        m_AtlasTexture = texture;

                    m_AtlasTextures[i] = texture;
                }
            }

            ReadFontAssetDefinition();
        }
    }
}
