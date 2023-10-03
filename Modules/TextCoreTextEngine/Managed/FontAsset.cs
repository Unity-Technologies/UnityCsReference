// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Profiling;
using UnityEngine.Serialization;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// Defines the potential font weights of a given font asset.
    /// </summary>
    public enum TextFontWeight
    {
        Thin        = 100,
        ExtraLight  = 200,
        Light       = 300,
        Regular     = 400,
        Medium      = 500,
        SemiBold    = 600,
        Bold        = 700,
        Heavy       = 800,
        Black       = 900,
    }

    /// <summary>
    /// Defines a pair of font assets for the regular and italic styles associated with a given font weight.
    /// </summary>
    [Serializable]
    public struct FontWeightPair
    {
        public FontAsset regularTypeface;
        public FontAsset italicTypeface;
    }

    //Structure which holds the font creation settings
    [Serializable]
    public struct FontAssetCreationEditorSettings
    {
        //public string sourceFontFileName;
        public string sourceFontFileGUID;
        public int faceIndex;
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

        internal FontAssetCreationEditorSettings(string sourceFontFileGUID, int pointSize, int pointSizeSamplingMode, int padding, int packingMode, int atlasWidth, int atlasHeight, int characterSelectionMode, string characterSet, int renderMode)
        {
            this.sourceFontFileGUID = sourceFontFileGUID;
            this.faceIndex = 0;
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

    public enum AtlasPopulationMode
    {
        Static = 0x0,
        Dynamic = 0x1,
        DynamicOS = 0x2
    }

    [Serializable][ExcludeFromPresetAttribute]
    public class FontAsset : TextAsset
    {
        /// <summary>
        /// This field is set when the font asset is first created.
        /// </summary>
        [SerializeField]
        internal string m_SourceFontFileGUID;

        /// <summary>
        /// Persistent reference to the source font file maintained in the editor.
        /// </summary>
        internal Font SourceFont_EditorRef
        {
            get
            {
                if (m_SourceFontFile_EditorRef == null)
                    m_SourceFontFile_EditorRef = GetSourceFontRef?.Invoke(m_SourceFontFileGUID);

                return m_SourceFontFile_EditorRef;
            }

            set
            {
                m_SourceFontFile_EditorRef = value;
                m_SourceFontFileGUID = SetSourceFontGUID?.Invoke(m_SourceFontFile_EditorRef);

                if (m_AtlasPopulationMode == AtlasPopulationMode.Static || m_AtlasPopulationMode == AtlasPopulationMode.DynamicOS)
                    m_SourceFontFile = null;
                else
                    m_SourceFontFile = m_SourceFontFile_EditorRef;
            }
        }
        internal Font m_SourceFontFile_EditorRef;

        /// <summary>
        /// Source font file when atlas population mode is set to dynamic. Null when the atlas population mode is set to static.
        /// </summary>
        public Font sourceFontFile
        {
            get { return m_SourceFontFile; }
            internal set { m_SourceFontFile = value; }
        }
        [SerializeField]
        private Font m_SourceFontFile;

        public AtlasPopulationMode atlasPopulationMode
        {
            get { return m_AtlasPopulationMode; }

            set
            {
                m_AtlasPopulationMode = value;

                if (m_AtlasPopulationMode == AtlasPopulationMode.Static || m_AtlasPopulationMode == AtlasPopulationMode.DynamicOS)
                    m_SourceFontFile = null;
                else if (m_AtlasPopulationMode == AtlasPopulationMode.Dynamic)
                    m_SourceFontFile = m_SourceFontFile_EditorRef;
            }
        }
        [SerializeField]
        private AtlasPopulationMode m_AtlasPopulationMode;

        /// <summary>
        /// Field used to identify dynamic OS font assets used internally.
        /// </summary>
        [SerializeField]
        internal bool InternalDynamicOS;

        /// <summary>
        /// Information about the font's face.
        /// </summary>
        public FaceInfo faceInfo
        {
            get { return m_FaceInfo; }
            set { m_FaceInfo = value; }
        }
        [SerializeField]
        internal FaceInfo m_FaceInfo;

        /// <summary>
        ///
        /// </summary>
        internal int familyNameHashCode
        {
            get
            {
                if (m_FamilyNameHashCode == 0)
                    m_FamilyNameHashCode = TextUtilities.GetHashCodeCaseInSensitive(m_FaceInfo.familyName);

                return m_FamilyNameHashCode;
            }
            set => m_FamilyNameHashCode = value;
        }
        private int m_FamilyNameHashCode;

        /// <summary>
        ///
        /// </summary>
        internal int styleNameHashCode
        {
            get
            {
                if (m_StyleNameHashCode == 0)
                    m_StyleNameHashCode = TextUtilities.GetHashCodeCaseInSensitive(m_FaceInfo.styleName);

                return m_StyleNameHashCode;
            }
            set => m_StyleNameHashCode = value;
        }
        private int m_StyleNameHashCode;


        /// <summary>
        /// Array containing font assets to be used as alternative typefaces for the various potential font weights of this font asset.
        /// </summary>
        public FontWeightPair[] fontWeightTable
        {
            get { return m_FontWeightTable; }
            internal set { m_FontWeightTable = value; }
        }
        [SerializeField]
        private FontWeightPair[] m_FontWeightTable = new FontWeightPair[10];

        /// <summary>
        /// List of glyphs contained in the font asset.
        /// </summary>
        public List<Glyph> glyphTable
        {
            get { return m_GlyphTable; }
            internal set { m_GlyphTable = value; }
        }
        [SerializeField]
        internal List<Glyph> m_GlyphTable = new List<Glyph>();

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
        internal Dictionary<uint, Glyph> m_GlyphLookupDictionary;


        /// <summary>
        /// List containing the characters of the given font asset.
        /// </summary>
        public List<Character> characterTable
        {
            get { return m_CharacterTable; }
            internal set { m_CharacterTable = value; }
        }
        [SerializeField]
        internal List<Character> m_CharacterTable = new List<Character>();

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
        internal Dictionary<uint, Character> m_CharacterLookupDictionary;


        /// <summary>
        /// Determines if the font asset is using a shared atlas texture(s)
        /// </summary>
        //public bool isUsingDynamicTextures
        //{
        //    get { return m_IsUsingDynamicTextures; }
        //    set { m_IsUsingDynamicTextures = value; }
        //}
        //[SerializeField]
        //private bool m_IsUsingDynamicTextures;

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
        internal Texture2D m_AtlasTexture;

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

                //if (m_IsUsingDynamicTextures)
                //    return TMP_DynamicAtlasTextureGroup.managedDynamicTextures;

                return m_AtlasTextures;
            }

            set
            {
                m_AtlasTextures = value;
            }
        }
        [SerializeField]
        internal Texture2D[] m_AtlasTextures;

        /// <summary>
        /// Index of the font atlas texture that still has available space to add new glyphs.
        /// </summary>
        [SerializeField]
        internal int m_AtlasTextureIndex;

        /// <summary>
        /// Number of atlas textures used by this font asset.
        /// </summary>
        public int atlasTextureCount { get { return m_AtlasTextureIndex + 1; } }

        /// <summary>
        /// Enables the font asset to create additional atlas textures as needed.
        /// </summary>
        public bool isMultiAtlasTexturesEnabled
        {
            get { return m_IsMultiAtlasTexturesEnabled; }
            set { m_IsMultiAtlasTexturesEnabled = value; }
        }

        [SerializeField]
        private bool m_IsMultiAtlasTexturesEnabled;

        /// <summary>
        /// Determines if dynamic font asset data should be cleared before builds.
        /// </summary>
        internal bool clearDynamicDataOnBuild
        {
            get { return m_ClearDynamicDataOnBuild; }
            set { m_ClearDynamicDataOnBuild = value; }
        }
        [SerializeField]
        private bool m_ClearDynamicDataOnBuild;

        /// <summary>
        /// The width of the atlas texture(s) used by this font asset.
        /// </summary>
        public int atlasWidth
        {
            get { return m_AtlasWidth; }
            internal set { m_AtlasWidth = value; }
        }
        [SerializeField]
        internal int m_AtlasWidth;

        /// <summary>
        /// The height of the atlas texture(s) used by this font asset.
        /// </summary>
        public int atlasHeight
        {
            get { return m_AtlasHeight; }
            internal set { m_AtlasHeight = value; }
        }
        [SerializeField]
        internal int m_AtlasHeight;

        /// <summary>
        /// The padding used between glyphs contained in the atlas texture(s) used by this font asset.
        /// </summary>
        public int atlasPadding
        {
            get { return m_AtlasPadding; }
            internal set { m_AtlasPadding = value; }
        }
        [SerializeField]
        internal int m_AtlasPadding;

        public GlyphRenderMode atlasRenderMode
        {
            get { return m_AtlasRenderMode; }
            internal set { m_AtlasRenderMode = value; }
        }
        [SerializeField]
        internal GlyphRenderMode m_AtlasRenderMode;

        /// <summary>
        /// List of spaces occupied by glyphs in a given texture.
        /// </summary>
        internal List<GlyphRect> usedGlyphRects
        {
            get { return m_UsedGlyphRects; }
            set { m_UsedGlyphRects = value; }
        }
        [SerializeField]
        private List<GlyphRect> m_UsedGlyphRects;

        /// <summary>
        /// List of spaces available in a given texture to add new glyphs.
        /// </summary>
        internal List<GlyphRect> freeGlyphRects
        {
            get { return m_FreeGlyphRects; }
            set { m_FreeGlyphRects = value; }
        }
        [SerializeField]
        private List<GlyphRect> m_FreeGlyphRects;

        /// <summary>
        /// Table containing the various font features of this font asset.
        /// </summary>
        public FontFeatureTable fontFeatureTable
        {
            get { return m_FontFeatureTable; }
            internal set { m_FontFeatureTable = value; }
        }
        [SerializeField]
        internal FontFeatureTable m_FontFeatureTable = new FontFeatureTable();

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

        //#if UNITY_EDITOR
        /// <summary>
        /// The settings used in the Font Asset Creator when this font asset was created or edited.
        /// </summary>
        public FontAssetCreationEditorSettings fontAssetCreationEditorSettings
        {
            get { return m_fontAssetCreationEditorSettings; }
            set { m_fontAssetCreationEditorSettings = value; }
        }
        [SerializeField]
        internal FontAssetCreationEditorSettings m_fontAssetCreationEditorSettings;
        //#endif

        /// <summary>
        /// Defines the dilation of the text when using regular style.
        /// </summary>
        public float regularStyleWeight { get { return m_RegularStyleWeight; } set { m_RegularStyleWeight = value; } }
        [FormerlySerializedAs("normalStyle")][SerializeField]
        internal float m_RegularStyleWeight = 0;

        /// <summary>
        /// The spacing between characters when using regular style.
        /// </summary>
        public float regularStyleSpacing { get { return m_RegularStyleSpacing; } set { m_RegularStyleSpacing = value; } }
        [FormerlySerializedAs("normalSpacingOffset")][SerializeField]
        internal float m_RegularStyleSpacing = 0;

        /// <summary>
        /// Defines the dilation of the text when using bold style.
        /// </summary>
        public float boldStyleWeight { get { return m_BoldStyleWeight; } set { m_BoldStyleWeight = value; } }
        [FormerlySerializedAs("boldStyle")][SerializeField]
        internal float m_BoldStyleWeight = 0.75f;

        /// <summary>
        /// The spacing between characters when using regular style.
        /// </summary>
        public float boldStyleSpacing { get { return m_BoldStyleSpacing; } set { m_BoldStyleSpacing = value; } }
        [FormerlySerializedAs("boldSpacing")][SerializeField]
        internal float m_BoldStyleSpacing = 7f;

        /// <summary>
        /// Defines the slant of the text when using italic style.
        /// </summary>
        public byte italicStyleSlant { get { return m_ItalicStyleSlant; } set { m_ItalicStyleSlant = value; } }
        [FormerlySerializedAs("italicStyle")][SerializeField]
        internal byte m_ItalicStyleSlant = 35;

        /// <summary>
        /// The number of spaces that a tab represents.
        /// </summary>
        public byte tabMultiple { get { return m_TabMultiple; } set { m_TabMultiple = value; } }
        [FormerlySerializedAs("tabSize")][SerializeField]
        internal byte m_TabMultiple = 10;

        internal bool IsFontAssetLookupTablesDirty;

        // ================================================================================
        // Functions used to create font asset at runtime
        // ================================================================================

        /// <summary>
        /// Creates a new font asset instance from the given family name and style.
        /// </summary>
        /// <param name="familyName">The family name of the source font.</param>
        /// <param name="styleName">The style name of the source font face.</param>
        /// <param name="pointSize">Optional point size.</param>
        /// <returns>An instance of the newly created font asset.</returns>
        public static FontAsset CreateFontAsset(string familyName, string styleName, int pointSize = 90)
        {
            if (FontEngine.TryGetSystemFontReference(familyName, styleName, out FontReference fontRef))
                return CreateFontAsset(fontRef.filePath, fontRef.faceIndex, pointSize, 9, GlyphRenderMode.SDFAA, 1024, 1024);

            Debug.Log("Unable to find a font file with the specified Family Name [" + familyName + "] and Style [" + styleName + "].");

            return null;
        }

        static FontAsset CreateFontAsset(string fontFilePath, int faceIndex, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight, AtlasPopulationMode atlasPopulationMode = AtlasPopulationMode.DynamicOS, bool enableMultiAtlasSupport = true)
        {
            // Load Font Face
            if (FontEngine.LoadFontFace(fontFilePath, samplingPointSize, faceIndex) != FontEngineError.Success)
            {
                Debug.Log("Unable to load font face for [" + fontFilePath + "].");
                return null;
            }

            return CreateFontAssetInstance(null, atlasPadding, renderMode, atlasWidth, atlasHeight, atlasPopulationMode, enableMultiAtlasSupport);
        }

        /// <summary>
        /// Creates a new font asset instance from the provided font object.
        /// </summary>
        /// <param name="font">The source font object.</param>
        /// <returns>An instance of the newly created font asset.</returns>
        public static FontAsset CreateFontAsset(Font font)
        {
            return CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
        }

        /// <summary>
        /// Creates a new font asset instance from the provided font object.
        /// </summary>
        /// <param name="font">The source font object.</param>
        /// <param name="samplingPointSize">The sampling point size.</param>
        /// <param name="atlasPadding">The padding between individual glyphs in the font atlas texture.</param>
        /// <param name="renderMode">The atlas render mode.</param>
        /// <param name="atlasWidth">The atlas texture width.</param>
        /// <param name="atlasHeight">The atlas texture height.</param>
        /// <param name="atlasPopulationMode">The atlas population mode.</param>
        /// <param name="enableMultiAtlasSupport">Enable multi atlas texture.</param>
        /// <returns>An instance of the newly created font asset.</returns>
        public static FontAsset CreateFontAsset(Font font, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight, AtlasPopulationMode atlasPopulationMode = AtlasPopulationMode.Dynamic, bool enableMultiAtlasSupport = true)
        {
            return CreateFontAsset(font, 0, samplingPointSize, atlasPadding, renderMode, atlasWidth, atlasHeight, atlasPopulationMode, enableMultiAtlasSupport);
        }

        static FontAsset CreateFontAsset(Font font, int faceIndex, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight, AtlasPopulationMode atlasPopulationMode = AtlasPopulationMode.Dynamic, bool enableMultiAtlasSupport = true)
        {
            // Load Font Face
            if (FontEngine.LoadFontFace(font, samplingPointSize, faceIndex) != FontEngineError.Success)
            {
                if (font.name == "Arial")
                {
                    return FontAsset.CreateFontAsset("Arial", "Regular");
                }
                Debug.LogWarning("Unable to load font face for [" + font.name + "]. Make sure \"Include Font Data\" is enabled in the Font Import Settings.", font);
                return null;
            }

            return CreateFontAssetInstance(font, atlasPadding, renderMode, atlasWidth, atlasHeight, atlasPopulationMode, enableMultiAtlasSupport);
        }

        static FontAsset CreateFontAssetInstance(Font font, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight, AtlasPopulationMode atlasPopulationMode, bool enableMultiAtlasSupport)
        {
            // Create new font asset
            FontAsset fontAsset = CreateInstance<FontAsset>();

            fontAsset.m_Version = "1.1.0";
            fontAsset.faceInfo = FontEngine.GetFaceInfo();

            if (atlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                fontAsset.sourceFontFile = font;

                fontAsset.m_SourceFontFileGUID = SetSourceFontGUID?.Invoke(font);
                fontAsset.m_SourceFontFile_EditorRef = font;
            }

            fontAsset.atlasPopulationMode = atlasPopulationMode;
            // Need to eventually add support for setting default state related to Clear Dynamic Data on Build.

            fontAsset.atlasWidth = atlasWidth;
            fontAsset.atlasHeight = atlasHeight;
            fontAsset.atlasPadding = atlasPadding;
            fontAsset.atlasRenderMode = renderMode;

            // Initialize array for the font atlas textures.
            fontAsset.atlasTextures = new Texture2D[1];

            // Create and add font atlas texture.
            Texture2D texture = new Texture2D(1, 1, TextureFormat.Alpha8, false);
            fontAsset.atlasTextures[0] = texture;

            fontAsset.isMultiAtlasTexturesEnabled = enableMultiAtlasSupport;

            // Add free rectangle of the size of the texture.
            int packingModifier;
            if (((GlyphRasterModes)renderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP)
            {
                packingModifier = 0;

                // Optimize by adding static ref to shader.
                Material tmp_material = new Material(TextShaderUtilities.ShaderRef_MobileBitmap);

                //tmp_material.name = texture.name + " Material";
                tmp_material.SetTexture(TextShaderUtilities.ID_MainTex, texture);
                tmp_material.SetFloat(TextShaderUtilities.ID_TextureWidth, atlasWidth);
                tmp_material.SetFloat(TextShaderUtilities.ID_TextureHeight, atlasHeight);

                fontAsset.material = tmp_material;
            }
            else
            {
                packingModifier = 1;

                // Optimize by adding static ref to shader.
                Material tmp_material = new Material(TextShaderUtilities.ShaderRef_MobileSDF);

                //tmp_material.name = texture.name + " Material";
                tmp_material.SetTexture(TextShaderUtilities.ID_MainTex, texture);
                tmp_material.SetFloat(TextShaderUtilities.ID_TextureWidth, atlasWidth);
                tmp_material.SetFloat(TextShaderUtilities.ID_TextureHeight, atlasHeight);

                tmp_material.SetFloat(TextShaderUtilities.ID_GradientScale, atlasPadding + packingModifier);

                tmp_material.SetFloat(TextShaderUtilities.ID_WeightNormal, fontAsset.regularStyleWeight);
                tmp_material.SetFloat(TextShaderUtilities.ID_WeightBold, fontAsset.boldStyleWeight);

                fontAsset.material = tmp_material;
            }

            fontAsset.freeGlyphRects = new List<GlyphRect>(8) { new GlyphRect(0, 0, atlasWidth - packingModifier, atlasHeight - packingModifier) };
            fontAsset.usedGlyphRects = new List<GlyphRect>(8);

            // Set the name of the font asset resources for tracking in the profiler
            string fontName = fontAsset.faceInfo.familyName + " - " + fontAsset.faceInfo.styleName;
            fontAsset.material.name = fontName + " Material";
            fontAsset.atlasTextures[0].name = fontName + " Atlas";

            // TODO: Consider adding support for extracting glyph positioning data

            fontAsset.ReadFontAssetDefinition();

            return fontAsset;
        }

        // Editor Only Callbacks
        internal static Action<Texture, FontAsset> OnFontAssetTextureChanged;
        internal static Action<FontAsset> RegisterResourceForUpdate;
        internal static Action<FontAsset> RegisterResourceForReimport;
        internal static Action<Texture2D, bool> SetAtlasTextureIsReadable;
        internal static Func<string, Font> GetSourceFontRef;
        internal static Func<Font, string> SetSourceFontGUID;

        // Profiler Marker declarations
        private static ProfilerMarker k_ReadFontAssetDefinitionMarker = new ProfilerMarker("FontAsset.ReadFontAssetDefinition");
        private static ProfilerMarker k_AddSynthesizedCharactersMarker = new ProfilerMarker("FontAsset.AddSynthesizedCharacters");
        private static ProfilerMarker k_TryAddCharacterMarker = new ProfilerMarker("FontAsset.TryAddCharacter");
        private static ProfilerMarker k_TryAddCharactersMarker = new ProfilerMarker("FontAsset.TryAddCharacters");
        private static ProfilerMarker k_UpdateGlyphAdjustmentRecordsMarker = new ProfilerMarker("FontAsset.UpdateGlyphAdjustmentRecords");
        private static ProfilerMarker k_ClearFontAssetDataMarker = new ProfilerMarker("FontAsset.ClearFontAssetData");
        private static ProfilerMarker k_UpdateFontAssetDataMarker = new ProfilerMarker("FontAsset.UpdateFontAssetData");

        // ================================================================================
        //
        // ================================================================================

        void Awake() {}

        private void OnDestroy()
        {
            DestroyAtlasTextures();

            DestroyImmediate(m_Material);
        }

        private void OnValidate()
        {
            // Make sure our lookup dictionary have been initialized.
            if (m_CharacterLookupDictionary == null || m_GlyphLookupDictionary == null)
                ReadFontAssetDefinition();
        }


        private static string k_InternalErrorShader = "Hidden/InternalErrorShader";
        private static string s_DefaultMaterialSuffix = " Atlas Material";

        /// <summary>
        /// Reads the various data tables of the font asset and populates various data structures to allow for faster lookup of related font asset data.
        /// </summary>
        public void ReadFontAssetDefinition()
        {
            k_ReadFontAssetDefinitionMarker.Begin();

            //Debug.Log("Reading Font Asset Definition for " + this.name + ".");

            // Initialize lookup tables for characters and glyphs.
            InitializeDictionaryLookupTables();

            // Add synthesized characters and adjust face metrics
            AddSynthesizedCharactersAndFaceMetrics();

            // Set Cap Line using the capital letter 'X'
            if (m_FaceInfo.capLine == 0 && m_CharacterLookupDictionary.ContainsKey('X'))
            {
                uint glyphIndex = m_CharacterLookupDictionary['X'].glyphIndex;
                m_FaceInfo.capLine = m_GlyphLookupDictionary[glyphIndex].metrics.horizontalBearingY;
            }

            // Set Mean Line using the lowercase letter 'x'
            if (m_FaceInfo.meanLine == 0 && m_CharacterLookupDictionary.ContainsKey('x'))
            {
                uint glyphIndex = m_CharacterLookupDictionary['x'].glyphIndex;
                m_FaceInfo.meanLine = m_GlyphLookupDictionary[glyphIndex].metrics.horizontalBearingY;
            }

            // Adjust Font Scale for compatibility reasons
            if (m_FaceInfo.scale == 0)
                m_FaceInfo.scale = 1.0f;

            // Set Strikethrough Offset (if needed)
            if (m_FaceInfo.strikethroughOffset == 0)
                m_FaceInfo.strikethroughOffset = m_FaceInfo.capLine / 2.5f;

            // Set Padding value for legacy font assets.
            if (m_AtlasPadding == 0)
            {
                if (material.HasProperty(TextShaderUtilities.ID_GradientScale))
                    m_AtlasPadding = (int)material.GetFloat(TextShaderUtilities.ID_GradientScale) - 1;
            }

            // Special handling to replace shaders referencing the InternalErrorShader by the appropriate TextCore internal shaders.
            // This is necessary to make sure font assets and materials created in a project the includes the TextCore package
            // continue to work as expected when the package is removed and using the TextCore module.
            if (m_Material.shader.name == k_InternalErrorShader)
            {
                if (((GlyphRasterModes)m_AtlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP)
                    m_Material.shader = TextShaderUtilities.ShaderRef_MobileBitmap;
                else
                    m_Material.shader = TextShaderUtilities.ShaderRef_MobileSDF;

                Debug.LogWarning("Replacing missing shader on font asset [" + name + "] with [" + m_Material.shader.name + "] shader.", m_Material);
            }

            // Compute hash codes for various properties of the font asset used for lookup.
            hashCode = TextUtilities.GetHashCodeCaseInSensitive(name);
            familyNameHashCode = TextUtilities.GetHashCodeCaseInSensitive(m_FaceInfo.familyName);
            styleNameHashCode = TextUtilities.GetHashCodeCaseInSensitive(m_FaceInfo.styleName);
            materialHashCode = TextUtilities.GetHashCodeCaseInSensitive(this.name + s_DefaultMaterialSuffix);

            // Add reference to font asset in TMP Resource Manager
            TextResourceManager.AddFontAsset(this);

            IsFontAssetLookupTablesDirty = false;

            k_ReadFontAssetDefinitionMarker.End();
        }

        /// <summary>
        /// Read the various data tables of the font asset to populate its different dictionaries to allow for faster lookup of related font asset data.
        /// </summary>
        internal void InitializeDictionaryLookupTables()
        {
            // Initialize and populate glyph lookup dictionary
            InitializeGlyphLookupDictionary();

            // Initialize and populate character lookup dictionary
            InitializeCharacterLookupDictionary();

            // Initialize and populate glyph pair adjustment records
            InitializeGlyphPaidAdjustmentRecordsLookupDictionary();
        }

        internal void InitializeGlyphLookupDictionary()
        {
            // Create new instance of the glyph lookup dictionary or clear the existing one.
            if (m_GlyphLookupDictionary == null)
                m_GlyphLookupDictionary = new Dictionary<uint, Glyph>();
            else
                m_GlyphLookupDictionary.Clear();

            // Initialize or clear list of glyph indexes.
            if (m_GlyphIndexList == null)
                m_GlyphIndexList = new List<uint>();
            else
                m_GlyphIndexList.Clear();

            // Initialize or clear list of glyph indexes.
            if (m_GlyphIndexListNewlyAdded == null)
                m_GlyphIndexListNewlyAdded = new List<uint>();
            else
                m_GlyphIndexListNewlyAdded.Clear();

            //
            int glyphCount = m_GlyphTable.Count;

            // Add glyphs contained in the glyph table to dictionary for faster lookup.
            for (int i = 0; i < glyphCount; i++)
            {
                Glyph glyph = m_GlyphTable[i];

                uint index = glyph.index;

                // TODO: Not sure it is necessary to check here.
                if (m_GlyphLookupDictionary.ContainsKey(index) == false)
                {
                    m_GlyphLookupDictionary.Add(index, glyph);
                    m_GlyphIndexList.Add(index);
                }
            }
        }

        internal void InitializeCharacterLookupDictionary()
        {
            // Create new instance of the character lookup dictionary or clear the existing one.
            if (m_CharacterLookupDictionary == null)
                m_CharacterLookupDictionary = new Dictionary<uint, Character>();
            else
                m_CharacterLookupDictionary.Clear();

            // Add the characters contained in the character table to the dictionary for faster lookup.
            for (int i = 0; i < m_CharacterTable.Count; i++)
            {
                Character character = m_CharacterTable[i];

                uint unicode = character.unicode;
                uint glyphIndex = character.glyphIndex;

                // Add character along with reference to text asset and glyph
                if (m_CharacterLookupDictionary.ContainsKey(unicode) == false)
                {
                    m_CharacterLookupDictionary.Add(unicode, character);
                    character.textAsset = this;
                    character.glyph = m_GlyphLookupDictionary[glyphIndex];
                }
            }

            // Clear internal fallback references
            /*
            if (FallbackSearchQueryLookup == null)
                FallbackSearchQueryLookup = new HashSet<int>();
            else
                FallbackSearchQueryLookup.Clear();
            */
        }

        internal void InitializeGlyphPaidAdjustmentRecordsLookupDictionary()
        {
            // Read Font Features which will include kerning data.
            if (m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup == null)
                m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup = new Dictionary<uint, GlyphPairAdjustmentRecord>();
            else
                m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.Clear();

            List<GlyphPairAdjustmentRecord> glyphPairAdjustmentRecords = m_FontFeatureTable.m_GlyphPairAdjustmentRecords;
            if (glyphPairAdjustmentRecords != null)
            {
                for (int i = 0; i < glyphPairAdjustmentRecords.Count; i++)
                {
                    GlyphPairAdjustmentRecord record = glyphPairAdjustmentRecords[i];

                    uint key = record.secondAdjustmentRecord.glyphIndex << 16 | record.firstAdjustmentRecord.glyphIndex;

                    if (!m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.ContainsKey(key))
                        m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.Add(key, record);
                }
            }
        }

        internal void AddSynthesizedCharactersAndFaceMetrics()
        {
            k_AddSynthesizedCharactersMarker.Begin();

            bool isFontFaceLoaded = false;

            if (m_AtlasPopulationMode == AtlasPopulationMode.Dynamic || m_AtlasPopulationMode == AtlasPopulationMode.DynamicOS)
            {
                isFontFaceLoaded = LoadFontFace() == FontEngineError.Success;

                if (!isFontFaceLoaded && !InternalDynamicOS)
                    Debug.LogWarning("Unable to load font face for [" + this.name + "] font asset.", this);
            }

            // Only characters not present in the source font file will be synthesized.

            // Non visible and control characters with no metrics
            // Add End of Text \u0003
            AddSynthesizedCharacter(0x03, isFontFaceLoaded, true);

            // Add Tab \u0009
            AddSynthesizedCharacter(0x09, isFontFaceLoaded, true);

            // Add Line Feed (LF) \u000A
            AddSynthesizedCharacter(0x0A, isFontFaceLoaded);

            // Add Vertical Tab (VT) \u000B
            AddSynthesizedCharacter(0x0B, isFontFaceLoaded);

            // Add Carriage Return (CR) \u000D
            AddSynthesizedCharacter(0x0D, isFontFaceLoaded);

            // Add Arabic Letter Mark \u061C
            AddSynthesizedCharacter(0x061C, isFontFaceLoaded);

            // Add Zero Width Space <ZWSP> \u2000B
            AddSynthesizedCharacter(0x200B, isFontFaceLoaded);

            // Add Left-To-Right Mark \u200E
            AddSynthesizedCharacter(0x200E, isFontFaceLoaded);

            // Add Right-To-Left Mark \u200F
            AddSynthesizedCharacter(0x200F, isFontFaceLoaded);

            // Add Line Separator \u2028
            AddSynthesizedCharacter(0x2028, isFontFaceLoaded);

            // Add Paragraph Separator \u2029
            AddSynthesizedCharacter(0x2029, isFontFaceLoaded);

            // Add Word Joiner <WJ> / Zero Width Non-Breaking Space \u2060
            AddSynthesizedCharacter(0x2060, isFontFaceLoaded);

            k_AddSynthesizedCharactersMarker.End();
        }

        void AddSynthesizedCharacter(uint unicode, bool isFontFaceLoaded, bool addImmediately = false)
        {
            // Check if unicode is already present in the font asset
            if (m_CharacterLookupDictionary.ContainsKey(unicode))
                return;

            Glyph glyph;

            if (isFontFaceLoaded)
            {
                // Check if unicode is present in font file
                if (FontEngine.GetGlyphIndex(unicode) != 0)
                {
                    if (addImmediately == false)
                        return;

                    //Debug.Log("Adding Unicode [" + unicode.ToString("X4") + "].");

                    GlyphLoadFlags glyphLoadFlags = ((GlyphRasterModes)m_AtlasRenderMode & GlyphRasterModes.RASTER_MODE_NO_HINTING) == GlyphRasterModes.RASTER_MODE_NO_HINTING
                        ? GlyphLoadFlags.LOAD_NO_BITMAP | GlyphLoadFlags.LOAD_NO_HINTING
                        : GlyphLoadFlags.LOAD_NO_BITMAP;

                    if (FontEngine.TryGetGlyphWithUnicodeValue(unicode, glyphLoadFlags, out glyph))
                        m_CharacterLookupDictionary.Add(unicode, new Character(unicode, this, glyph));

                    return;
                }
            }

            //Debug.Log("Synthesizing Unicode [" + unicode.ToString("X4") + "].");

            // Synthesize and add missing glyph and character
            glyph = new Glyph(0, new GlyphMetrics(0, 0, 0, 0, 0), GlyphRect.zero, 1.0f, 0);
            m_CharacterLookupDictionary.Add(unicode, new Character(unicode, this, glyph));
        }

        //internal HashSet<int> FallbackSearchQueryLookup = new HashSet<int>();

        internal void AddCharacterToLookupCache(uint unicode, Character character)
        {
            m_CharacterLookupDictionary.Add(unicode, character);

            // Add font asset to fallback references.
            //FallbackSearchQueryLookup.Add(character.textAsset.instanceID);
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        FontEngineError LoadFontFace()
        {
            if (m_AtlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                // Font Asset should have a valid reference to a font in the Editor.
                if (m_SourceFontFile == null)
                    m_SourceFontFile = SourceFont_EditorRef;

                return FontEngine.LoadFontFace(m_SourceFontFile, m_FaceInfo.pointSize, m_FaceInfo.faceIndex);
            }

            // Font Asset is Dynamic OS
            if (SourceFont_EditorRef != null)
            {
                // Try loading the font face from the referenced source font
                if (FontEngine.LoadFontFace(m_SourceFontFile_EditorRef, m_FaceInfo.pointSize, m_FaceInfo.faceIndex) == FontEngineError.Success)
                    return FontEngineError.Success;
            }

            return FontEngine.LoadFontFace(m_FaceInfo.familyName, m_FaceInfo.styleName, m_FaceInfo.pointSize);
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

        internal void SortFontFeatureTable()
        {
            m_FontFeatureTable.SortGlyphPairAdjustmentRecords();
        }

        /// <summary>
        /// Sort both glyph and character tables.
        /// </summary>
        internal void SortAllTables()
        {
            SortGlyphTable();
            SortCharacterTable();
            SortFontFeatureTable();
        }

        /// <summary>
        /// HashSet of font asset instance ID used in the process of searching for through fallback font assets for a given character or characters.
        /// </summary>
        private static HashSet<int> k_SearchedFontAssetLookup;

        /// <summary>
        /// Function to check if a certain character exists in the font asset.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        public bool HasCharacter(int character)
        {
            if (m_CharacterLookupDictionary == null)
                return false;

            return m_CharacterLookupDictionary.ContainsKey((uint)character);
        }

        /// <summary>
        /// Function to check if a character is contained in the font asset with the option to also check potential local fallbacks.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="searchFallbacks"></param>
        /// <param name="tryAddCharacter"></param>
        /// <returns></returns>
        public bool HasCharacter(char character, bool searchFallbacks = false, bool tryAddCharacter = false)
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

            // Check if font asset is dynamic and if so try to add the requested character to it.
            if (tryAddCharacter && (m_AtlasPopulationMode == AtlasPopulationMode.Dynamic || m_AtlasPopulationMode == AtlasPopulationMode.DynamicOS))
            {
                Character returnedCharacter;

                if (TryAddCharacterInternal(character, out returnedCharacter))
                    return true;
            }

            if (searchFallbacks)
            {
                // Initialize or clear font asset lookup
                if (k_SearchedFontAssetLookup == null)
                    k_SearchedFontAssetLookup = new HashSet<int>();
                else
                    k_SearchedFontAssetLookup.Clear();

                // Add current font asset to lookup
                k_SearchedFontAssetLookup.Add(GetInstanceID());

                // Check font asset fallbacks
                if (fallbackFontAssetTable != null && fallbackFontAssetTable.Count > 0)
                {
                    for (int i = 0; i < fallbackFontAssetTable.Count && fallbackFontAssetTable[i] != null; i++)
                    {
                        FontAsset fallback = fallbackFontAssetTable[i];
                        int fallbackID = fallback.GetInstanceID();

                        // Search fallback if not already contained in lookup
                        if (k_SearchedFontAssetLookup.Add(fallbackID))
                        {
                            if (fallback.HasCharacter_Internal(character, true, tryAddCharacter))
                                return true;
                        }
                    }
                }

                // Check general fallback font assets.
                // if (TMP_Settings.fallbackFontAssets != null && TMP_Settings.fallbackFontAssets.Count > 0)
                // {
                //     for (int i = 0; i < TMP_Settings.fallbackFontAssets.Count && TMP_Settings.fallbackFontAssets[i] != null; i++)
                //     {
                //         TMP_FontAsset fallback = TMP_Settings.fallbackFontAssets[i];
                //         int fallbackID = fallback.GetInstanceID();
                //
                //         // Search fallback if not already contained in lookup
                //         if (k_SearchedFontAssetLookup.Add(fallbackID))
                //         {
                //             if (fallback.HasCharacter_Internal(character, true, tryAddCharacter))
                //                 return true;
                //         }
                //     }
                // }

                // Check TMP Settings Default Font Asset
                // if (TMP_Settings.defaultFontAsset != null)
                // {
                //     TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
                //     int fallbackID = fallback.GetInstanceID();
                //
                //     // Search fallback if it has not already been searched
                //     if (k_SearchedFontAssetLookup.Add(fallbackID))
                //     {
                //         if (fallback.HasCharacter_Internal(character, true, tryAddCharacter))
                //             return true;
                //     }
                // }
            }

            return false;
        }

        /// <summary>
        /// Function to check if a character is contained in a font asset with the option to also check through fallback font assets.
        /// This private implementation does not search the fallback font asset in the TMP Settings file.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="searchFallbacks"></param>
        /// <param name="tryAddCharacter"></param>
        /// <returns></returns>
        bool HasCharacter_Internal(uint character, bool searchFallbacks = false, bool tryAddCharacter = false)
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

            // Check if fallback is dynamic and if so try to add the requested character to it.
            if (tryAddCharacter && (atlasPopulationMode == AtlasPopulationMode.Dynamic || m_AtlasPopulationMode == AtlasPopulationMode.DynamicOS))
            {
                Character returnedCharacter;

                if (TryAddCharacterInternal(character, out returnedCharacter))
                    return true;
            }

            if (searchFallbacks)
            {
                // Check Font Asset Fallback fonts.
                if (fallbackFontAssetTable == null || fallbackFontAssetTable.Count == 0)
                    return false;

                for (int i = 0; i < fallbackFontAssetTable.Count && fallbackFontAssetTable[i] != null; i++)
                {
                    FontAsset fallback = fallbackFontAssetTable[i];
                    int fallbackID = fallback.GetInstanceID();

                    // Search fallback if it has not already been searched
                    if (k_SearchedFontAssetLookup.Add(fallbackID))
                    {
                        if (fallback.HasCharacter_Internal(character, true, tryAddCharacter))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Function to check if certain characters exists in the font asset. Function returns a list of missing characters.
        /// </summary>
        /// <param name="text">String containing the characters to check.</param>
        /// <param name="missingCharacters">List of missing characters.</param>
        /// <returns></returns>
        public bool HasCharacters(string text, out List<char> missingCharacters)
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

            if (missingCharacters.Count == 0)
                return true;

            return false;
        }

        /// <summary>
        /// Function to check if the characters in the given string are contained in the font asset with the option to also check its potential local fallbacks.
        /// </summary>
        /// <param name="text">String containing the characters to check.</param>
        /// <param name="missingCharacters">Array containing the unicode values of the missing characters.</param>
        /// <param name="searchFallbacks">Determines if fallback font assets assigned to this font asset should be searched.</param>
        /// <param name="tryAddCharacter"></param>
        /// <returns></returns>
        public bool HasCharacters(string text, out uint[] missingCharacters, bool searchFallbacks = false, bool tryAddCharacter = false)
        {
            missingCharacters = null;

            // Read font asset definition if it hasn't already been done.
            if (m_CharacterLookupDictionary == null)
            {
                ReadFontAssetDefinition();

                if (m_CharacterLookupDictionary == null)
                    return false;
            }

            // Clear internal list of
            s_MissingCharacterList.Clear();

            for (int i = 0; i < text.Length; i++)
            {
                bool isMissingCharacter = true;
                uint character = text[i];

                if (m_CharacterLookupDictionary.ContainsKey(character))
                    continue;

                // Check if fallback is dynamic and if so try to add the requested character to it.
                if (tryAddCharacter && (atlasPopulationMode == AtlasPopulationMode.Dynamic || m_AtlasPopulationMode == AtlasPopulationMode.DynamicOS))
                {
                    Character returnedCharacter;

                    if (TryAddCharacterInternal(character, out returnedCharacter))
                        continue;
                }

                if (searchFallbacks)
                {
                    // Initialize or clear font asset lookup
                    if (k_SearchedFontAssetLookup == null)
                        k_SearchedFontAssetLookup = new HashSet<int>();
                    else
                        k_SearchedFontAssetLookup.Clear();

                    // Add current font asset to lookup
                    k_SearchedFontAssetLookup.Add(GetInstanceID());

                    // Check font asset fallbacks
                    if (fallbackFontAssetTable != null && fallbackFontAssetTable.Count > 0)
                    {
                        for (int j = 0; j < fallbackFontAssetTable.Count && fallbackFontAssetTable[j] != null; j++)
                        {
                            FontAsset fallback = fallbackFontAssetTable[j];
                            int fallbackID = fallback.GetInstanceID();

                            // Search fallback if it has not already been searched
                            if (k_SearchedFontAssetLookup.Add(fallbackID))
                            {
                                if (fallback.HasCharacter_Internal(character, true, tryAddCharacter) == false)
                                    continue;

                                isMissingCharacter = false;
                                break;
                            }
                        }
                    }

                    // Check general fallback font assets.
                    // if (isMissingCharacter && TMP_Settings.fallbackFontAssets != null && TMP_Settings.fallbackFontAssets.Count > 0)
                    // {
                    //     for (int j = 0; j < TMP_Settings.fallbackFontAssets.Count && TMP_Settings.fallbackFontAssets[j] != null; j++)
                    //     {
                    //         TMP_FontAsset fallback = TMP_Settings.fallbackFontAssets[j];
                    //         int fallbackID = fallback.GetInstanceID();
                    //
                    //         // Search fallback if it has not already been searched
                    //         if (k_SearchedFontAssetLookup.Add(fallbackID))
                    //         {
                    //             if (fallback.HasCharacter_Internal(character, true, tryAddCharacter) == false)
                    //                 continue;
                    //
                    //             isMissingCharacter = false;
                    //             break;
                    //         }
                    //     }
                    // }

                    // Check TMP Settings Default Font Asset
                    // if (isMissingCharacter && TMP_Settings.defaultFontAsset != null)
                    // {
                    //     TMP_FontAsset fallback = TMP_Settings.defaultFontAsset;
                    //     int fallbackID = fallback.GetInstanceID();
                    //
                    //     // Search fallback if it has not already been searched
                    //     if (k_SearchedFontAssetLookup.Add(fallbackID))
                    //     {
                    //         if (fallback.HasCharacter_Internal(character, true, tryAddCharacter))
                    //             isMissingCharacter = false;
                    //     }
                    // }
                }

                if (isMissingCharacter)
                    s_MissingCharacterList.Add(character);
            }

            if (s_MissingCharacterList.Count > 0)
            {
                missingCharacters = s_MissingCharacterList.ToArray();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Function to check if certain characters exists in the font asset. Function returns false if any characters are missing.
        /// </summary>
        /// <param name="text">String containing the characters to check</param>
        /// <returns></returns>
        public bool HasCharacters(string text)
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
        public static string GetCharacters(FontAsset fontAsset)
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
        public static int[] GetCharactersArray(FontAsset fontAsset)
        {
            int[] characters = new int[fontAsset.characterTable.Count];

            for (int i = 0; i < fontAsset.characterTable.Count; i++)
            {
                characters[i] = (int)fontAsset.characterTable[i].unicode;
            }

            return characters;
        }

        /// <summary>
        /// Internal function used to get the glyph index for the given Unicode.
        /// </summary>
        /// <param name="unicode"></param>
        /// <returns></returns>
        internal uint GetGlyphIndex(uint unicode)
        {
            // Check if glyph already exists in font asset.
            if (m_CharacterLookupDictionary.ContainsKey(unicode))
                return m_CharacterLookupDictionary[unicode].glyphIndex;

            // Load font face.
            return LoadFontFace() == FontEngineError.Success ? FontEngine.GetGlyphIndex(unicode) : 0;
        }

        // ================================================================================
        // Properties and functions related to character and glyph additions as well as
        // tacking glyphs that need to be added to various font asset atlas textures.
        // ================================================================================

        // List and HashSet used for tracking font assets whose font atlas texture and character data needs updating.
        private static List<FontAsset> k_FontAssets_FontFeaturesUpdateQueue = new List<FontAsset>();
        private static HashSet<int> k_FontAssets_FontFeaturesUpdateQueueLookup = new HashSet<int>();

        private static List<Texture2D> k_FontAssets_AtlasTexturesUpdateQueue = new List<Texture2D>();
        private static HashSet<int> k_FontAssets_AtlasTexturesUpdateQueueLookup = new HashSet<int>();

        /// <summary>
        ///
        /// </summary>
        /// <param name="fontAsset"></param>
        internal static void RegisterFontAssetForFontFeatureUpdate(FontAsset fontAsset)
        {
            int instanceID = fontAsset.instanceID;

            if (k_FontAssets_FontFeaturesUpdateQueueLookup.Add(instanceID))
                k_FontAssets_FontFeaturesUpdateQueue.Add(fontAsset);
        }

        /// <summary>
        /// Function called to update the font atlas texture and character data of font assets to which
        /// new characters were added.
        /// </summary>
        internal static void UpdateFontFeaturesForFontAssetsInQueue()
        {
            int count = k_FontAssets_FontFeaturesUpdateQueue.Count;

            for (int i = 0; i < count; i++)
                k_FontAssets_FontFeaturesUpdateQueue[i].UpdateGlyphAdjustmentRecords();

            if (count > 0)
            {
                k_FontAssets_FontFeaturesUpdateQueue.Clear();
                k_FontAssets_FontFeaturesUpdateQueueLookup.Clear();
            }
        }

        /// <summary>
        /// Register Atlas Texture for Apply()
        /// </summary>
        /// <param name="texture">The texture on which to call Apply().</param>
        internal static void RegisterAtlasTextureForApply(Texture2D texture)
        {
            int instanceID = texture.GetInstanceID();

            if (k_FontAssets_AtlasTexturesUpdateQueueLookup.Add(instanceID))
                k_FontAssets_AtlasTexturesUpdateQueue.Add(texture);
        }

        /// <summary>
        ///
        /// </summary>
        internal static void UpdateAtlasTexturesInQueue()
        {
            int count = k_FontAssets_AtlasTexturesUpdateQueueLookup.Count;

            for (int i = 0; i < count; i++)
                k_FontAssets_AtlasTexturesUpdateQueue[i].Apply(false, false);

            if (count > 0)
            {
                k_FontAssets_AtlasTexturesUpdateQueue.Clear();
                k_FontAssets_AtlasTexturesUpdateQueueLookup.Clear();
            }
        }

        /// <summary>
        ///
        /// </summary>
        internal static void UpdateFontAssetInUpdateQueue()
        {
            UpdateAtlasTexturesInQueue();

            UpdateFontFeaturesForFontAssetsInQueue();
        }

        /// <summary>
        ///
        /// </summary>
        //private bool m_IsAlreadyRegisteredForUpdate;

        /// <summary>
        /// List of glyphs that need to be rendered and added to an atlas texture.
        /// </summary>
        private List<Glyph> m_GlyphsToRender = new List<Glyph>();

        /// <summary>
        /// List of glyphs that we just rendered and added to an atlas texture.
        /// </summary>
        private List<Glyph> m_GlyphsRendered = new List<Glyph>();

        /// <summary>
        /// List of all the glyph indexes contained in the font asset.
        /// </summary>
        private List<uint> m_GlyphIndexList = new List<uint>();

        /// <summary>
        /// List of glyph indexes newly added to the font asset.
        /// This list is used in the process of retrieving font features.
        /// </summary>
        private List<uint> m_GlyphIndexListNewlyAdded = new List<uint>();

        /// <summary>
        ///
        /// </summary>
        internal List<uint> m_GlyphsToAdd = new List<uint>();
        internal HashSet<uint> m_GlyphsToAddLookup = new HashSet<uint>();

        internal List<Character> m_CharactersToAdd = new List<Character>();
        internal HashSet<uint> m_CharactersToAddLookup = new HashSet<uint>();

        /// <summary>
        /// Internal list used to track characters that could not be added to the font asset.
        /// </summary>
        internal List<uint> s_MissingCharacterList = new List<uint>();

        /// <summary>
        /// Hash table used to track characters that are known to be missing from the font file.
        /// </summary>
        internal HashSet<uint> m_MissingUnicodesFromFontFile = new HashSet<uint>();

        /// <summary>
        /// Internal static array used to avoid allocations when using the GetGlyphPairAdjustmentTable().
        /// </summary>
        internal static uint[] k_GlyphIndexArray;

        /// <summary>
        /// Try adding the characters from the provided string to the font asset.
        /// </summary>
        /// <param name="unicodes">Array that contains the characters to add to the font asset.</param>
        /// <param name="includeFontFeatures"></param>
        /// <returns>Returns true if all the characters were successfully added to the font asset. Return false otherwise.</returns>
        public bool TryAddCharacters(uint[] unicodes, bool includeFontFeatures = false)
        {
            uint[] missingUnicodes;

            return TryAddCharacters(unicodes, out missingUnicodes, includeFontFeatures);
        }

        /// <summary>
        /// Try adding the characters from the provided string to the font asset.
        /// </summary>
        /// <param name="unicodes">Array that contains the characters to add to the font asset.</param>
        /// <param name="missingUnicodes">Array containing the characters that could not be added to the font asset.</param>
        /// <param name="includeFontFeatures"></param>
        /// <returns>Returns true if all the characters were successfully added to the font asset. Return false otherwise.</returns>
        public bool TryAddCharacters(uint[] unicodes, out uint[] missingUnicodes, bool includeFontFeatures = false)
        {
            k_TryAddCharactersMarker.Begin();

            // Make sure font asset is set to dynamic and that we have a valid list of characters.
            if (unicodes == null || unicodes.Length == 0 || m_AtlasPopulationMode == AtlasPopulationMode.Static)
            {
                if (m_AtlasPopulationMode == AtlasPopulationMode.Static)
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because its AtlasPopulationMode is set to Static.", this);
                else
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because the provided Unicode list is Null or Empty.", this);

                missingUnicodes = null;
                k_TryAddCharactersMarker.End();
                return false;
            }

            // Load font face.
            if (LoadFontFace() != FontEngineError.Success)
            {
                missingUnicodes = unicodes.ToArray();
                k_TryAddCharactersMarker.End();
                return false;
            }

            // Make sure font asset has been initialized
            if (m_CharacterLookupDictionary == null || m_GlyphLookupDictionary == null)
                ReadFontAssetDefinition();

            // Clear lists used to track which character and glyphs were added or missing.
            m_GlyphsToAdd.Clear();
            m_GlyphsToAddLookup.Clear();
            m_CharactersToAdd.Clear();
            m_CharactersToAddLookup.Clear();
            s_MissingCharacterList.Clear();

            bool isMissingCharacters = false;
            int unicodeCount = unicodes.Length;

            for (int i = 0; i < unicodeCount; i++)
            {
                uint unicode = unicodes[i];

                // Check if character is already contained in the character table.
                if (m_CharacterLookupDictionary.ContainsKey(unicode))
                    continue;

                // Get the index of the glyph for this Unicode value.
                uint glyphIndex = FontEngine.GetGlyphIndex(unicode);

                // Skip missing glyphs
                if (glyphIndex == 0)
                {
                    // Special handling for characters with potential alternative glyph representations
                    switch (unicode)
                    {
                        case 0xA0: // Non Breaking Space <NBSP>
                            // Use Space
                            glyphIndex = FontEngine.GetGlyphIndex(0x20);
                            break;
                        case 0xAD: // Soft Hyphen <SHY>
                        case 0x2011: // Non Breaking Hyphen
                            // Use Hyphen Minus
                            glyphIndex = FontEngine.GetGlyphIndex(0x2D);
                            break;
                    }

                    // Skip to next character if no potential alternative glyph representation is present in font file.
                    if (glyphIndex == 0)
                    {
                        // Add character to list of missing characters.
                        s_MissingCharacterList.Add(unicode);

                        isMissingCharacters = true;
                        continue;
                    }
                }

                Character character = new Character(unicode, glyphIndex);

                // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
                if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
                {
                    // Add a reference to the source text asset and glyph
                    character.glyph = m_GlyphLookupDictionary[glyphIndex];
                    character.textAsset = this;

                    m_CharacterTable.Add(character);
                    m_CharacterLookupDictionary.Add(unicode, character);
                    continue;
                }

                // Make sure glyph index has not already been added to list of glyphs to add.
                if (m_GlyphsToAddLookup.Add(glyphIndex))
                    m_GlyphsToAdd.Add(glyphIndex);

                // Make sure unicode / character has not already been added.
                if (m_CharactersToAddLookup.Add(unicode))
                    m_CharactersToAdd.Add(character);
            }

            if (m_GlyphsToAdd.Count == 0)
            {
                //Debug.LogWarning("No characters will be added to font asset [" + this.name + "] either because they are already present in the font asset or missing from the font file.");
                missingUnicodes = unicodes;
                k_TryAddCharactersMarker.End();
                return false;
            }

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width != m_AtlasWidth || m_AtlasTextures[m_AtlasTextureIndex].height != m_AtlasHeight)
            {
                m_AtlasTextures[m_AtlasTextureIndex].Reinitialize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            Glyph[] glyphs;
            bool allGlyphsAddedToTexture = FontEngine.TryAddGlyphsToTexture(m_GlyphsToAdd, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyphs);

            // Add new glyphs to relevant font asset data structure
            for (int i = 0; i < glyphs.Length && glyphs[i] != null; i++)
            {
                Glyph glyph = glyphs[i];
                uint glyphIndex = glyph.index;

                glyph.atlasIndex = m_AtlasTextureIndex;

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                m_GlyphIndexListNewlyAdded.Add(glyphIndex);
                m_GlyphIndexList.Add(glyphIndex);
            }

            // Clear glyph index list to allow
            m_GlyphsToAdd.Clear();

            // Add new characters to relevant data structures as well as track glyphs that could not be added to the current atlas texture.
            for (int i = 0; i < m_CharactersToAdd.Count; i++)
            {
                Character character = m_CharactersToAdd[i];
                Glyph glyph;

                if (m_GlyphLookupDictionary.TryGetValue(character.glyphIndex, out glyph) == false)
                {
                    m_GlyphsToAdd.Add(character.glyphIndex);
                    continue;
                }

                // Add a reference to the source text asset and glyph
                character.glyph = glyph;
                character.textAsset = this;

                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(character.unicode, character);

                // Remove character from list to add
                m_CharactersToAdd.RemoveAt(i);
                i -= 1;
            }

            // Try adding missing glyphs to
            if (m_IsMultiAtlasTexturesEnabled && allGlyphsAddedToTexture == false)
            {
                while (allGlyphsAddedToTexture == false)
                    allGlyphsAddedToTexture = TryAddGlyphsToNewAtlasTexture();
            }

            // Get Font Features for the given characters
            if (includeFontFeatures)
                UpdateGlyphAdjustmentRecords();

            // Makes the changes to the font asset persistent.
            RegisterResourceForUpdate?.Invoke(this);

            // Populate list of missing characters
            for (int i = 0; i < m_CharactersToAdd.Count; i++)
            {
                Character character = m_CharactersToAdd[i];
                s_MissingCharacterList.Add(character.unicode);
            }

            missingUnicodes = null;

            if (s_MissingCharacterList.Count > 0)
                missingUnicodes = s_MissingCharacterList.ToArray();

            k_TryAddCharactersMarker.End();

            return allGlyphsAddedToTexture && !isMissingCharacters;
        }

        /// <summary>
        /// Try adding the characters from the provided string to the font asset.
        /// </summary>
        /// <param name="characters">String containing the characters to add to the font asset.</param>
        /// <param name="includeFontFeatures"></param>
        /// <returns>Returns true if all the characters were successfully added to the font asset. Return false otherwise.</returns>
        public bool TryAddCharacters(string characters, bool includeFontFeatures = false)
        {
            string missingCharacters;

            return TryAddCharacters(characters, out missingCharacters, includeFontFeatures);
        }

        /// <summary>
        /// Try adding the characters from the provided string to the font asset.
        /// </summary>
        /// <param name="characters">String containing the characters to add to the font asset.</param>
        /// <param name="missingCharacters">String containing the characters that could not be added to the font asset.</param>
        /// <param name="includeFontFeatures"></param>
        /// <returns>Returns true if all the characters were successfully added to the font asset. Return false otherwise.</returns>
        public bool TryAddCharacters(string characters, out string missingCharacters, bool includeFontFeatures = false)
        {
            k_TryAddCharactersMarker.Begin();

            // Make sure font asset is set to dynamic and that we have a valid list of characters.
            if (string.IsNullOrEmpty(characters) || m_AtlasPopulationMode == AtlasPopulationMode.Static)
            {
                if (m_AtlasPopulationMode == AtlasPopulationMode.Static)
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because its AtlasPopulationMode is set to Static.", this);
                else
                {
                    Debug.LogWarning("Unable to add characters to font asset [" + this.name + "] because the provided character list is Null or Empty.", this);
                }

                missingCharacters = characters;
                k_TryAddCharactersMarker.End();
                return false;
            }

            // Load font face.
            if (LoadFontFace() != FontEngineError.Success)
            {
                missingCharacters = characters;
                k_TryAddCharactersMarker.End();
                return false;
            }

            // Make sure font asset has been initialized
            if (m_CharacterLookupDictionary == null || m_GlyphLookupDictionary == null)
                ReadFontAssetDefinition();

            // Clear data structures used to track which glyph needs to be added to atlas texture.
            m_GlyphsToAdd.Clear();
            m_GlyphsToAddLookup.Clear();
            m_CharactersToAdd.Clear();
            m_CharactersToAddLookup.Clear();
            s_MissingCharacterList.Clear();

            bool isMissingCharacters = false;
            int characterCount = characters.Length;

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
                    // Special handling for characters with potential alternative glyph representations
                    switch (unicode)
                    {
                        case 0xA0: // Non Breaking Space <NBSP>
                            // Use Space
                            glyphIndex = FontEngine.GetGlyphIndex(0x20);
                            break;
                        case 0xAD: // Soft Hyphen <SHY>
                        case 0x2011: // Non Breaking Hyphen
                            // Use Hyphen Minus
                            glyphIndex = FontEngine.GetGlyphIndex(0x2D);
                            break;
                    }

                    // Skip to next character if no potential alternative glyph representation is present in font file.
                    if (glyphIndex == 0)
                    {
                        // Add character to list of missing characters.
                        s_MissingCharacterList.Add(unicode);

                        isMissingCharacters = true;
                        continue;
                    }
                }

                Character character = new Character(unicode, glyphIndex);

                // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
                if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
                {
                    // Add a reference to the source text asset and glyph
                    character.glyph = m_GlyphLookupDictionary[glyphIndex];
                    character.textAsset = this;

                    m_CharacterTable.Add(character);
                    m_CharacterLookupDictionary.Add(unicode, character);
                    continue;
                }

                // Make sure glyph index has not already been added to list of glyphs to add.
                if (m_GlyphsToAddLookup.Add(glyphIndex))
                    m_GlyphsToAdd.Add(glyphIndex);

                // Make sure unicode / character has not already been added.
                if (m_CharactersToAddLookup.Add(unicode))
                    m_CharactersToAdd.Add(character);
            }

            if (m_GlyphsToAdd.Count == 0)
            {
                missingCharacters = characters;
                k_TryAddCharactersMarker.End();
                return false;
            }

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width != m_AtlasWidth || m_AtlasTextures[m_AtlasTextureIndex].height != m_AtlasHeight)
            {
                //Debug.Log("Setting initial size of atlas texture used by font asset [" + this.name + "].");
                m_AtlasTextures[m_AtlasTextureIndex].Reinitialize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            Glyph[] glyphs;

            bool allGlyphsAddedToTexture = FontEngine.TryAddGlyphsToTexture(m_GlyphsToAdd, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyphs);

            for (int i = 0; i < glyphs.Length && glyphs[i] != null; i++)
            {
                Glyph glyph = glyphs[i];
                uint glyphIndex = glyph.index;

                glyph.atlasIndex = m_AtlasTextureIndex;

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                m_GlyphIndexListNewlyAdded.Add(glyphIndex);
                m_GlyphIndexList.Add(glyphIndex);
            }

            // Clear glyph index list to track glyphs that were not added to the atlas texture
            m_GlyphsToAdd.Clear();

            // Add new characters to relevant data structures.
            for (int i = 0; i < m_CharactersToAdd.Count; i++)
            {
                Character character = m_CharactersToAdd[i];
                Glyph glyph;

                if (m_GlyphLookupDictionary.TryGetValue(character.glyphIndex, out glyph) == false)
                {
                    m_GlyphsToAdd.Add(character.glyphIndex);
                    continue;
                }

                // Add a reference to the source text asset and glyph
                character.glyph = glyph;
                character.textAsset = this;

                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(character.unicode, character);

                // Remove character from list to add
                m_CharactersToAdd.RemoveAt(i);
                i -= 1;
            }

            // Try adding glyphs that didn't fit in the current atlas texture to new atlas texture
            if (m_IsMultiAtlasTexturesEnabled && allGlyphsAddedToTexture == false)
            {
                while (allGlyphsAddedToTexture == false)
                    allGlyphsAddedToTexture = TryAddGlyphsToNewAtlasTexture();
            }

            // Get Font Features for the given characters
            if (includeFontFeatures)
                UpdateGlyphAdjustmentRecords();

            // Makes the changes to the font asset persistent.
            RegisterResourceForUpdate?.Invoke(this);

            missingCharacters = string.Empty;

            // Populate list of missing characters
            for (int i = 0; i < m_CharactersToAdd.Count; i++)
            {
                Character character = m_CharactersToAdd[i];
                s_MissingCharacterList.Add(character.unicode);
            }

            if (s_MissingCharacterList.Count > 0)
                missingCharacters = s_MissingCharacterList.UintToString();

            k_TryAddCharactersMarker.End();
            return allGlyphsAddedToTexture && !isMissingCharacters;
        }

        // Functions to remove...
        /*
        /// <summary>
        /// NOT USED CURRENTLY - Try adding character using Unicode value to font asset.
        /// </summary>
        /// <param name="unicode">The Unicode value of the character.</param>
        /// <param name="character">The character data if successfully added to the font asset. Null otherwise.</param>
        /// <returns>Returns true if the character has been added. False otherwise.</returns>
        internal bool TryAddCharacter_Internal(uint unicode)
        {
            TMP_Character character = null;

            // Check if character is already contained in the character table.
            if (m_CharacterLookupDictionary.ContainsKey(unicode))
                return true;

            uint glyphIndex = FontEngine.GetGlyphIndex(unicode);
            if (glyphIndex == 0)
                return false;

            // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
            if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
            {
                character = new TMP_Character(unicode, m_GlyphLookupDictionary[glyphIndex]);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                //#if UNITY_EDITOR
                // Makes the changes to the font asset persistent.
                // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
                // Could also add some update registry to handle this.
                //SortGlyphTable();
                //UnityEditor.EditorUtility.SetDirty(this);
                //#endif

                return true;
            }

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width != m_AtlasWidth || m_AtlasTextures[m_AtlasTextureIndex].height != m_AtlasHeight)
            {
                //Debug.Log("Setting initial size of atlas texture used by font asset [" + this.name + "].");
                m_AtlasTextures[m_AtlasTextureIndex].Reinitialize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            Glyph glyph;

            if (FontEngine.TryAddGlyphToTexture(glyphIndex, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyph))
            {
                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                // Add new character
                character = new TMP_Character(unicode, glyph);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                //#if UNITY_EDITOR
                // Makes the changes to the font asset persistent.
                // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
                // Could also add some update registry to handle this.
                //SortGlyphTable();
                //UnityEditor.EditorUtility.SetDirty(this);
                //#endif

                return true;
            }

            return false;
        }


        /// <summary>
        /// To be removed.
        /// </summary>
        /// <param name="unicode"></param>
        /// <param name="glyph"></param>
        internal TMP_Character AddCharacter_Internal(uint unicode, Glyph glyph)
        {
            // Check if character is already contained in the character table.
            if (m_CharacterLookupDictionary.ContainsKey(unicode))
                return m_CharacterLookupDictionary[unicode];

            uint glyphIndex = glyph.index;

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width != m_AtlasWidth || m_AtlasTextures[m_AtlasTextureIndex].height != m_AtlasHeight)
            {
                //Debug.Log("Setting initial size of atlas texture used by font asset [" + this.name + "].");
                m_AtlasTextures[m_AtlasTextureIndex].Reinitialize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

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

                    //m_GlyphsToRender.Add(glyph);
                }
            }

            // Add character to font asset.
            TMP_Character character = new TMP_Character(unicode, glyph);
            m_CharacterTable.Add(character);
            m_CharacterLookupDictionary.Add(unicode, character);

            //Debug.Log("Adding character [" + (char)unicode + "] with Unicode (" + unicode + ") to [" + this.name + "] font asset.");

            // Schedule glyph to be added to the font atlas texture
            //TM_FontAssetUpdateManager.RegisterFontAssetForUpdate(this);
            //UpdateAtlasTexture(); // Temporary until callback system is revised.

            //#if UNITY_EDITOR
            // Makes the changes to the font asset persistent.
            // OPTIMIZATION: This could be handled when exiting Play mode if we added any new characters to the asset.
            // Could also add some update registry to handle this.
            //SortGlyphTable();
            //UnityEditor.EditorUtility.SetDirty(this);
            //#endif

            return character;
        }
        */


        /// <summary>
        /// Try adding character using Unicode value to font asset.
        /// Function assumes internal user has already checked to make sure the character is not already contained in the font asset.
        /// </summary>
        /// <param name="unicode">The Unicode value of the character.</param>
        /// <param name="character">The character data if successfully added to the font asset. Null otherwise.</param>
        /// <returns>Returns true if the character has been added. False otherwise.</returns>
        internal bool TryAddCharacterInternal(uint unicode, out Character character, bool shouldGetFontFeatures = false)
        {
            k_TryAddCharacterMarker.Begin();

            character = null;

            // Check if the Unicode character is already known to be missing from the source font file.
            if (m_MissingUnicodesFromFontFile.Contains(unicode))
            {
                k_TryAddCharacterMarker.End();
                return false;
            }

            // Load font face.
            if (LoadFontFace() != FontEngineError.Success)
            {
                k_TryAddCharacterMarker.End();
                return false;
            }

            uint glyphIndex = FontEngine.GetGlyphIndex(unicode);
            if (glyphIndex == 0)
            {
                // Special handling for characters with potential alternative glyph representations
                switch (unicode)
                {
                    case 0xA0: // Non Breaking Space <NBSP>
                        // Use Space
                        glyphIndex = FontEngine.GetGlyphIndex(0x20);
                        break;
                    case 0xAD: // Soft Hyphen <SHY>
                    case 0x2011: // Non Breaking Hyphen
                        // Use Hyphen Minus
                        glyphIndex = FontEngine.GetGlyphIndex(0x2D);
                        break;
                }

                // Return if no potential alternative glyph representation is present in font file.
                if (glyphIndex == 0)
                {
                    m_MissingUnicodesFromFontFile.Add(unicode);

                    k_TryAddCharacterMarker.End();
                    return false;
                }
            }

            // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
            if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
            {
                character = new Character(unicode, this, m_GlyphLookupDictionary[glyphIndex]);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                // Makes the changes to the font asset persistent.
                RegisterResourceForUpdate?.Invoke(this);

                k_TryAddCharacterMarker.End();
                return true;
            }

            Glyph glyph = null;

            // TODO: Potential new feature to explore where selected font assets share the same atlas texture(s).
            // Handling if using Dynamic Textures
            //if (m_IsUsingDynamicTextures)
            //{
            //    if (TMP_DynamicAtlasTextureGroup.AddGlyphToManagedDynamicTexture(this, glyphIndex, m_AtlasPadding, m_AtlasRenderMode, out glyph))
            //    {
            //        // Add new glyph to glyph table.
            //        m_GlyphTable.Add(glyph);
            //        m_GlyphLookupDictionary.Add(glyphIndex, glyph);

            //        // Add new character
            //        character = new TMP_Character(unicode, glyph);
            //        m_CharacterTable.Add(character);
            //        m_CharacterLookupDictionary.Add(unicode, character);

            //        m_GlyphIndexList.Add(glyphIndex);

            //        if (TMP_Settings.getFontFeaturesAtRuntime)
            //        {
            //            if (k_FontAssetsToUpdateLookup.Add(instanceID))
            //                k_FontAssetsToUpdate.Add(this);
            //        }

            //        return true;
            //    }
            //}

            // Make sure atlas texture is readable.
            if (m_AtlasTextures[m_AtlasTextureIndex].isReadable == false)
            {
                Debug.LogWarning("Unable to add the requested character to font asset [" + this.name + "]'s atlas texture. Please make the texture [" + m_AtlasTextures[m_AtlasTextureIndex].name + "] readable.", m_AtlasTextures[m_AtlasTextureIndex]);

                k_TryAddCharacterMarker.End();
                return false;
            }

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width != m_AtlasWidth || m_AtlasTextures[m_AtlasTextureIndex].height != m_AtlasHeight)
            {
                m_AtlasTextures[m_AtlasTextureIndex].Reinitialize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            // Set texture upload mode to batching texture.Apply()
            FontEngine.SetTextureUploadMode(false);

            // Try adding glyph to local atlas texture
            if (FontEngine.TryAddGlyphToTexture(glyphIndex, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyph))
            {
                // Update glyph atlas index
                glyph.atlasIndex = m_AtlasTextureIndex;

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                // Add new character
                character = new Character(unicode, this, glyph);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                m_GlyphIndexList.Add(glyphIndex);
                m_GlyphIndexListNewlyAdded.Add(glyphIndex);

                if (shouldGetFontFeatures)
                    RegisterFontAssetForFontFeatureUpdate(this);

                RegisterAtlasTextureForApply(m_AtlasTextures[m_AtlasTextureIndex]);
                FontEngine.SetTextureUploadMode(true);

                // Makes the changes to the font asset persistent.
                RegisterResourceForUpdate?.Invoke(this);

                k_TryAddCharacterMarker.End();
                return true;
            }

            // Add glyph which did not fit in current atlas texture to new atlas texture.
            if (m_IsMultiAtlasTexturesEnabled)
            {
                // Create new atlas texture
                SetupNewAtlasTexture();

                // Try adding glyph to newly created atlas texture
                if (FontEngine.TryAddGlyphToTexture(glyphIndex, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyph))
                {
                    // Update glyph atlas index
                    glyph.atlasIndex = m_AtlasTextureIndex;

                    // Add new glyph to glyph table.
                    m_GlyphTable.Add(glyph);
                    m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                    // Add new character
                    character = new Character(unicode, this, glyph);
                    m_CharacterTable.Add(character);
                    m_CharacterLookupDictionary.Add(unicode, character);

                    m_GlyphIndexList.Add(glyphIndex);
                    m_GlyphIndexListNewlyAdded.Add(glyphIndex);

                    if (shouldGetFontFeatures)
                        RegisterFontAssetForFontFeatureUpdate(this);

                    RegisterAtlasTextureForApply(m_AtlasTextures[m_AtlasTextureIndex]);
                    FontEngine.SetTextureUploadMode(true);

                    RegisterResourceForUpdate?.Invoke(this);

                    k_TryAddCharacterMarker.End();
                    return true;
                }
            }

            k_TryAddCharacterMarker.End();

            return false;
        }

        internal bool TryGetCharacter_and_QueueRenderToTexture(uint unicode, out Character character, bool shouldGetFontFeatures = false)
        {
            k_TryAddCharacterMarker.Begin();

            character = null;

            // Check if the Unicode character is already known to be missing from the source font file.
            if (m_MissingUnicodesFromFontFile.Contains(unicode))
            {
                k_TryAddCharacterMarker.End();
                return false;
            }

            // Load font face.
            if (LoadFontFace() != FontEngineError.Success)
            {
                k_TryAddCharacterMarker.End();
                return false;
            }

            uint glyphIndex = FontEngine.GetGlyphIndex(unicode);
            if (glyphIndex == 0)
            {
                // Special handling for characters with potential alternative glyph representations
                switch (unicode)
                {
                    case 0xA0: // Non Breaking Space <NBSP>
                        // Use Space
                        glyphIndex = FontEngine.GetGlyphIndex(0x20);
                        break;
                    case 0xAD: // Soft Hyphen <SHY>
                    case 0x2011: // Non Breaking Hyphen
                        // Use Hyphen Minus
                        glyphIndex = FontEngine.GetGlyphIndex(0x2D);
                        break;
                }

                // Return if no potential alternative glyph representation is present in font file.
                if (glyphIndex == 0)
                {
                    m_MissingUnicodesFromFontFile.Add(unicode);

                    k_TryAddCharacterMarker.End();
                    return false;
                }
            }

            // Check if glyph is already contained in the font asset as the same glyph might be referenced by multiple characters.
            if (m_GlyphLookupDictionary.ContainsKey(glyphIndex))
            {
                character = new Character(unicode, this, m_GlyphLookupDictionary[glyphIndex]);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                // Makes the changes to the font asset persistent.
                RegisterResourceForUpdate?.Invoke(this);

                k_TryAddCharacterMarker.End();
                return true;
            }

            GlyphLoadFlags glyphLoadFlags = (GlyphRasterModes.RASTER_MODE_NO_HINTING & (GlyphRasterModes)m_AtlasRenderMode) == GlyphRasterModes.RASTER_MODE_NO_HINTING
                ? GlyphLoadFlags.LOAD_NO_BITMAP | GlyphLoadFlags.LOAD_NO_HINTING
                : GlyphLoadFlags.LOAD_NO_BITMAP;

            Glyph glyph = null;

            if (FontEngine.TryGetGlyphWithIndexValue(glyphIndex, glyphLoadFlags, out glyph))
            {
                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                // Add new character
                character = new Character(unicode, this, glyph);
                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(unicode, character);

                m_GlyphIndexList.Add(glyphIndex);
                m_GlyphIndexListNewlyAdded.Add(glyphIndex);

                if (shouldGetFontFeatures)
                    RegisterFontAssetForFontFeatureUpdate(this);

                // Add glyph to list of glyphs to be rendered
                m_GlyphsToRender.Add(glyph);

                // Register font asset to render and add glyphs to atlas textures
                //RegisterFontAssetForAtlasTextureUpdate(this);

                // Makes the changes to the font asset persistent.
                RegisterResourceForUpdate?.Invoke(this);

                k_TryAddCharacterMarker.End();
                return true;
            }

            k_TryAddCharacterMarker.End();
            return false;
        }

        /// <summary>
        /// This function requires an update to the TextCore:FontEngine
        /// </summary>
        internal void TryAddGlyphsToAtlasTextures()
        {
            /*
            // Return if we don't have any glyphs to add.
            if (m_GlyphsToRender.Count == 0)
                return;

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width != m_AtlasWidth || m_AtlasTextures[m_AtlasTextureIndex].height != m_AtlasHeight)
            {
                //Debug.Log("Setting initial size of atlas texture used by font asset [" + this.name + "].");
                m_AtlasTextures[m_AtlasTextureIndex].Reinitialize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            bool allGlyphsAddedToTexture = FontEngine.TryAddGlyphsToTexture(m_GlyphsToRender, m_GlyphsRendered, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex]);

            // Try adding glyphs that didn't fit in the current atlas texture to new atlas texture
            if (m_IsMultiAtlasTexturesEnabled && allGlyphsAddedToTexture == false)
            {
                while (allGlyphsAddedToTexture == false)
                {
                    // Create and prepare new atlas texture
                    SetupNewAtlasTexture();

                    // Try adding remaining glyphs in the newly created atlas texture
                    allGlyphsAddedToTexture = FontEngine.TryAddGlyphsToTexture(m_GlyphsToRender, m_GlyphsRendered, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex]);
                }
            }

            if (allGlyphsAddedToTexture == false)
            {
                // TODO: Handle case when we have left over glyph to render that didn't fit in the atlas texture.
                Debug.LogError("Unable to add some glyphs to atlas texture.");
            }

            #if UNITY_EDITOR
            // Makes the changes to the font asset persistent.
            if (UnityEditor.EditorUtility.IsPersistent(this))
            {
                //SortGlyphAndCharacterTables();
                TMP_EditorResourceManager.RegisterResourceForUpdate(this);
            }
            #endif
            */
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        bool TryAddGlyphsToNewAtlasTexture()
        {
            // Create and prepare new atlas texture
            SetupNewAtlasTexture();

            Glyph[] glyphs;

            // Try adding remaining glyphs in the newly created atlas texture
            bool allGlyphsAddedToTexture = FontEngine.TryAddGlyphsToTexture(m_GlyphsToAdd, m_AtlasPadding, GlyphPackingMode.BestShortSideFit, m_FreeGlyphRects, m_UsedGlyphRects, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex], out glyphs);

            // Add new glyphs to relevant data structures.
            for (int i = 0; i < glyphs.Length && glyphs[i] != null; i++)
            {
                Glyph glyph = glyphs[i];
                uint glyphIndex = glyph.index;

                glyph.atlasIndex = m_AtlasTextureIndex;

                // Add new glyph to glyph table.
                m_GlyphTable.Add(glyph);
                m_GlyphLookupDictionary.Add(glyphIndex, glyph);

                m_GlyphIndexListNewlyAdded.Add(glyphIndex);
                m_GlyphIndexList.Add(glyphIndex);
            }

            // Clear glyph index list to allow us to track glyphs
            m_GlyphsToAdd.Clear();

            // Add new characters to relevant data structures as well as track glyphs that could not be added to the current atlas texture.
            for (int i = 0; i < m_CharactersToAdd.Count; i++)
            {
                Character character = m_CharactersToAdd[i];
                Glyph glyph;

                if (m_GlyphLookupDictionary.TryGetValue(character.glyphIndex, out glyph) == false)
                {
                    m_GlyphsToAdd.Add(character.glyphIndex);
                    continue;
                }

                // Add a reference to the source text asset and glyph
                character.glyph = glyph;
                character.textAsset = this;

                m_CharacterTable.Add(character);
                m_CharacterLookupDictionary.Add(character.unicode, character);

                // Remove character
                m_CharactersToAdd.RemoveAt(i);
                i -= 1;
            }

            return allGlyphsAddedToTexture;
        }

        /// <summary>
        ///
        /// </summary>
        void SetupNewAtlasTexture()
        {
            m_AtlasTextureIndex += 1;

            // Check size of atlas texture array
            if (m_AtlasTextures.Length == m_AtlasTextureIndex)
                Array.Resize(ref m_AtlasTextures, m_AtlasTextures.Length * 2);

            // Initialize new atlas texture
            m_AtlasTextures[m_AtlasTextureIndex] = new Texture2D(m_AtlasWidth, m_AtlasHeight, TextureFormat.Alpha8, false);
            FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);

            // Clear packing GlyphRects
            int packingModifier = ((GlyphRasterModes)m_AtlasRenderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP ? 0 : 1;
            m_FreeGlyphRects.Clear();
            m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier, m_AtlasHeight - packingModifier));
            m_UsedGlyphRects.Clear();

            // Add new texture as sub asset to font asset
            Texture2D tex = m_AtlasTextures[m_AtlasTextureIndex];
            tex.name = m_AtlasTextures[0].name + " " + m_AtlasTextureIndex;

            OnFontAssetTextureChanged?.Invoke(tex, this);
        }

        /// <summary>
        /// Not used currently
        /// </summary>
        internal void UpdateAtlasTexture()
        {
            // Return if we don't have any glyphs to add to atlas texture.
            if (m_GlyphsToRender.Count == 0)
                return;

            //Debug.Log("Updating [" + this.name + "]'s atlas texture.");

            // Pack glyphs in the given atlas texture size.
            // TODO: Packing and glyph render modes should be defined in the font asset.
            //FontEngine.PackGlyphsInAtlas(m_GlyphsToPack, m_GlyphsPacked, m_AtlasPadding, GlyphPackingMode.ContactPointRule, GlyphRenderMode.SDFAA, m_AtlasWidth, m_AtlasHeight, m_FreeGlyphRects, m_UsedGlyphRects);
            //FontEngine.RenderGlyphsToTexture(m_GlyphsPacked, m_AtlasPadding, GlyphRenderMode.SDFAA, m_AtlasTextures[m_AtlasTextureIndex]);

            // Resize the Atlas Texture to the appropriate size
            if (m_AtlasTextures[m_AtlasTextureIndex].width != m_AtlasWidth || m_AtlasTextures[m_AtlasTextureIndex].height != m_AtlasHeight)
            {
                //Debug.Log("Setting initial size of atlas texture used by font asset [" + this.name + "].");
                m_AtlasTextures[m_AtlasTextureIndex].Reinitialize(m_AtlasWidth, m_AtlasHeight);
                FontEngine.ResetAtlasTexture(m_AtlasTextures[m_AtlasTextureIndex]);
            }

            //FontEngine.RenderGlyphsToTexture(m_GlyphsToRender, m_AtlasPadding, m_AtlasRenderMode, m_AtlasTextures[m_AtlasTextureIndex]);

            // Apply changes to atlas texture
            m_AtlasTextures[m_AtlasTextureIndex].Apply(false, false);

            // Add glyphs that were successfully packed to the glyph table.
            //for (int i = 0; i < m_GlyphsToRender.Count /* m_GlyphsPacked.Count */; i++)
            //{
            //    Glyph glyph = m_GlyphsToRender[i]; // m_GlyphsPacked[i];

            //    // Update atlas texture index
            //    glyph.atlasIndex = m_AtlasTextureIndex;

            //    m_GlyphTable.Add(glyph);
            //    m_GlyphLookupDictionary.Add(glyph.index, glyph);
            //}

            // Clear list of glyphs
            //m_GlyphsPacked.Clear();
            //m_GlyphsToRender.Clear();

            // Add any remaining glyphs into new atlas texture if multi texture support if enabled.
            //if (m_GlyphsToPack.Count > 0)
            //{
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
            //}

            // Makes the changes to the font asset persistent.
            RegisterResourceForUpdate?.Invoke(this);
        }

        /// <summary>
        ///
        /// </summary>
        internal void UpdateGlyphAdjustmentRecords()
        {
            k_UpdateGlyphAdjustmentRecordsMarker.Begin();

            GlyphPairAdjustmentRecord[] pairAdjustmentRecords = FontEngine.GetGlyphPairAdjustmentRecords(m_GlyphIndexList, out int recordCount);

            // Clear newly added glyph list
            m_GlyphIndexListNewlyAdded.Clear();

            if (pairAdjustmentRecords == null || pairAdjustmentRecords.Length == 0)
            {
                k_UpdateGlyphAdjustmentRecordsMarker.End();
                return;
            }

            if (m_FontFeatureTable == null)
                m_FontFeatureTable = new FontFeatureTable();

            for (int i = 0; i < pairAdjustmentRecords.Length && pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex != 0; i++)
            {
                uint pairKey = pairAdjustmentRecords[i].secondAdjustmentRecord.glyphIndex << 16 | pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex;

                // Check if table already contains a pair adjustment record for this key.
                if (m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.ContainsKey(pairKey))
                    continue;

                GlyphPairAdjustmentRecord record = pairAdjustmentRecords[i];

                m_FontFeatureTable.m_GlyphPairAdjustmentRecords.Add(record);
                m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.Add(pairKey, record);
            }

            k_UpdateGlyphAdjustmentRecordsMarker.End();
        }

        /// <summary>
        /// Function used for debugging and performance testing.
        /// </summary>
        /// <param name="glyphIndexes"></param>
        internal void UpdateGlyphAdjustmentRecords(uint[] glyphIndexes)
        {
            k_UpdateGlyphAdjustmentRecordsMarker.Begin();

            // Get glyph pair adjustment records from font file.
            GlyphPairAdjustmentRecord[] pairAdjustmentRecords = FontEngine.GetGlyphPairAdjustmentTable(glyphIndexes);

            // Clear newly added glyph list
            //m_GlyphIndexListNewlyAdded.Clear();

            if (pairAdjustmentRecords == null || pairAdjustmentRecords.Length == 0)
            {
                k_UpdateGlyphAdjustmentRecordsMarker.End();
                return;
            }

            if (m_FontFeatureTable == null)
                m_FontFeatureTable = new FontFeatureTable();

            for (int i = 0; i < pairAdjustmentRecords.Length && pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex != 0; i++)
            {
                uint pairKey = pairAdjustmentRecords[i].secondAdjustmentRecord.glyphIndex << 16 | pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex;

                // Check if table already contains a pair adjustment record for this key.
                if (m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.ContainsKey(pairKey))
                    continue;

                GlyphPairAdjustmentRecord record = pairAdjustmentRecords[i];

                m_FontFeatureTable.m_GlyphPairAdjustmentRecords.Add(record);
                m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.Add(pairKey, record);
            }

            k_UpdateGlyphAdjustmentRecordsMarker.End();
        }

        /// <summary>
        /// Function requires an update to the TextCore:FontEngine
        /// </summary>
        /// <param name="glyphIndexes"></param>
        internal void UpdateGlyphAdjustmentRecords(List<uint> glyphIndexes)
        {
            /*
            k_UpdateGlyphAdjustmentRecordsMarker.Begin();

            // Get glyph pair adjustment records from font file.
            int recordCount;
            GlyphPairAdjustmentRecord[] pairAdjustmentRecords = FontEngine.GetGlyphPairAdjustmentRecords(glyphIndexes, out recordCount);

            // Clear newly added glyph list
            m_GlyphIndexListNewlyAdded.Clear();

            if (pairAdjustmentRecords == null || pairAdjustmentRecords.Length == 0)
            {
                k_UpdateGlyphAdjustmentRecordsMarker.End();
                return;
            }

            if (m_FontFeatureTable == null)
                m_FontFeatureTable = new TMP_FontFeatureTable();

            for (int i = 0; i < pairAdjustmentRecords.Length && pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex != 0; i++)
            {
                uint pairKey = pairAdjustmentRecords[i].secondAdjustmentRecord.glyphIndex << 16 | pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex;

                // Check if table already contains a pair adjustment record for this key.
                if (m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookupDictionary.ContainsKey(pairKey))
                    continue;

                TMP_GlyphPairAdjustmentRecord record = new TMP_GlyphPairAdjustmentRecord(pairAdjustmentRecords[i]);

                m_FontFeatureTable.m_GlyphPairAdjustmentRecords.Add(record);
                m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookupDictionary.Add(pairKey, record);
            }

            k_UpdateGlyphAdjustmentRecordsMarker.End();

            #if UNITY_EDITOR
            m_FontFeatureTable.SortGlyphPairAdjustmentRecords();
            #endif
            */
        }

        /// <summary>
        /// Function requires an update to the TextCore:FontEngine
        /// </summary>
        /// <param name="newGlyphIndexes"></param>
        /// <param name="allGlyphIndexes"></param>
        internal void UpdateGlyphAdjustmentRecords(List<uint> newGlyphIndexes, List<uint> allGlyphIndexes)
        {
            /*
            // Get glyph pair adjustment records from font file.
            GlyphPairAdjustmentRecord[] pairAdjustmentRecords = FontEngine.GetGlyphPairAdjustmentRecords(newGlyphIndexes, allGlyphIndexes);

            // Clear newly added glyph list
            m_GlyphIndexListNewlyAdded.Clear();

            if (pairAdjustmentRecords == null || pairAdjustmentRecords.Length == 0)
            {
                return;
            }

            if (m_FontFeatureTable == null)
                m_FontFeatureTable = new TMP_FontFeatureTable();

            for (int i = 0; i < pairAdjustmentRecords.Length && pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex != 0; i++)
            {
                uint pairKey = pairAdjustmentRecords[i].secondAdjustmentRecord.glyphIndex << 16 | pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex;

                // Check if table already contains a pair adjustment record for this key.
                if (m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookupDictionary.ContainsKey(pairKey))
                    continue;

                TMP_GlyphPairAdjustmentRecord record = new TMP_GlyphPairAdjustmentRecord(pairAdjustmentRecords[i]);

                m_FontFeatureTable.m_GlyphPairAdjustmentRecords.Add(record);
                m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookupDictionary.Add(pairKey, record);
            }

            #if UNITY_EDITOR
            m_FontFeatureTable.SortGlyphPairAdjustmentRecords();
            #endif
            */
        }

        /// <summary>
        /// Internal method to copy generic list data to generic array of the same type.
        /// </summary>
        /// <param name="srcList">Source</param>
        /// <param name="dstArray">Destination</param>
        /// <typeparam name="T">Element type</typeparam>
        void CopyListDataToArray<T>(List<T> srcList, ref T[] dstArray)
        {
            int size = srcList.Count;

            // Make sure destination array is appropriately sized.
            if (dstArray == null)
                dstArray = new T[size];
            else
                Array.Resize(ref dstArray, size);

            for (int i = 0; i < size; i++)
                dstArray[i] = srcList[i];
        }

        /// <summary>
        /// Clears font asset data including the glyph and character tables and textures.
        /// Function might be changed to Internal and only used in tests.
        /// </summary>
        /// <param name="setAtlasSizeToZero">Will set the atlas texture size to zero width and height if true.</param>
        public void ClearFontAssetData(bool setAtlasSizeToZero = false)
        {
            k_ClearFontAssetDataMarker.Begin();

            // Record full object undo in the Editor.
            //UnityEditor.Undo.RecordObjects(new UnityEngine.Object[] { this, this.atlasTexture }, "Resetting Font Asset");

            // Clear glyph, character and font feature tables
            ClearFontAssetTables();

            // Clear atlas textures
            ClearAtlasTextures(setAtlasSizeToZero);

            ReadFontAssetDefinition();

            //TMP_ResourceManager.RebuildFontAssetCache(instanceID);

            // Makes the changes to the font asset persistent.
            RegisterResourceForUpdate?.Invoke(this);

            k_ClearFontAssetDataMarker.End();
        }

        internal void ClearFontAssetDataInternal()
        {
            // Clear glyph, character and font feature tables
            ClearFontAssetTables();

            // Clear atlas textures
            ClearAtlasTextures(true);

            // Makes the changes to the font asset persistent.
            RegisterResourceForUpdate?.Invoke(this);
        }

        /// <summary>
        ///
        /// </summary>
        internal void UpdateFontAssetData()
        {
            k_UpdateFontAssetDataMarker.Begin();

            // Get list of all characters currently contained in the font asset.
            uint[] unicodeCharacters = new uint[m_CharacterTable.Count];

            for (int i = 0; i < m_CharacterTable.Count; i++)
                unicodeCharacters[i] = m_CharacterTable[i].unicode;

            // Clear glyph, character and font feature tables
            ClearFontAssetTables();

            // Clear atlas textures
            ClearAtlasTextures(true);

            ReadFontAssetDefinition();

            //TMP_ResourceManager.RebuildFontAssetCache(instanceID);

            // Add existing glyphs and characters back in the font asset (if any)
            if (unicodeCharacters.Length > 0)
                TryAddCharacters(unicodeCharacters, true);

            k_UpdateFontAssetDataMarker.End();
        }

        /// <summary>
        ///
        /// </summary>
        internal void ClearFontAssetTables()
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
                m_FreeGlyphRects.Clear();
                m_FreeGlyphRects.Add(new GlyphRect(0, 0, m_AtlasWidth - packingModifier, m_AtlasHeight - packingModifier));
            }

            if (m_GlyphsToRender != null)
                m_GlyphsToRender.Clear();

            if (m_GlyphsRendered != null)
                m_GlyphsRendered.Clear();

            // Clear Glyph Adjustment Table
            if (m_FontFeatureTable != null && m_FontFeatureTable.m_GlyphPairAdjustmentRecords != null)
                m_FontFeatureTable.glyphPairAdjustmentRecords.Clear();
        }

        /// <summary>
        /// Internal function to clear all atlas textures.
        /// </summary>
        /// <param name="setAtlasSizeToZero">Set main atlas texture size to zero if true.</param>
        internal void ClearAtlasTextures(bool setAtlasSizeToZero = false)
        {
            m_AtlasTextureIndex = 0;

            // Return if we don't have any atlas textures
            if (m_AtlasTextures == null)
                return;

            Texture2D texture = null;

            // Clear all additional atlas textures
            for (int i = 1; i < m_AtlasTextures.Length; i++)
            {
                texture = m_AtlasTextures[i];

                if (texture == null)
                    continue;

                DestroyImmediate(texture, true);

                RegisterResourceForReimport?.Invoke(this);
            }

            // Resize atlas texture array down to one texture
            Array.Resize(ref m_AtlasTextures, 1);

            texture = m_AtlasTexture = m_AtlasTextures[0];

            // Clear main atlas texture
            if (texture.isReadable == false)
            {
                SetAtlasTextureIsReadable?.Invoke(texture, true);
            }

            if (setAtlasSizeToZero)
            {
                texture.Reinitialize(1, 1, TextureFormat.Alpha8, false);
            }
            else if (texture.width != m_AtlasWidth || texture.height != m_AtlasHeight)
            {
                texture.Reinitialize(m_AtlasWidth, m_AtlasHeight, TextureFormat.Alpha8, false);
            }

            // Clear texture atlas
            FontEngine.ResetAtlasTexture(texture);
            texture.Apply();
        }

        void DestroyAtlasTextures()
        {
            if (m_AtlasTextures == null)
                return;

            for (int i = 0; i < m_AtlasTextures.Length; i++)
            {
                Texture2D tex = m_AtlasTextures[i];

                if (tex != null)
                    DestroyImmediate(tex);
            }
        }
    }
}
