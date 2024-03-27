// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEngine.Bindings;
using UnityEngine.Serialization;
using UnityEngine.TextCore.LowLevel;

using UnityEditor;

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
    [Serializable][UnityEngine.Internal.ExcludeFromDocs]
    public struct FontAssetCreationEditorSettings
    {
        //public string sourceFontFileName;
        public string sourceFontFileGUID;
        public int faceIndex;
        public int pointSizeSamplingMode;
        public int pointSize;
        public int padding;
        public int paddingMode;
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
            this.paddingMode = 2;
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

    /// <summary>
    /// Atlas population modes which ultimately defines the type of font asset.
    /// </summary>
    public enum AtlasPopulationMode
    {
        Static = 0x0,
        Dynamic = 0x1,
        DynamicOS = 0x2
    }

    /// <summary>
    ///
    /// </summary>
    [Serializable][ExcludeFromPresetAttribute]
    public partial class FontAsset : TextAsset
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
        /// The settings used in the Font Asset Creator when this font asset was created or edited.
        /// </summary>
        public FontAssetCreationEditorSettings fontAssetCreationEditorSettings
        {
            get { return m_fontAssetCreationEditorSettings; }
            set { m_fontAssetCreationEditorSettings = value; }
        }
        [SerializeField]
        internal FontAssetCreationEditorSettings m_fontAssetCreationEditorSettings;

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

        [SerializeField] private string m_SourceFontFilePath;

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
        /// Dictionary used to lookup characters contained in the font asset or its fallbacks by their unicode values.
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
        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal Dictionary<uint, Character> m_CharacterLookupDictionary;

        private readonly ReaderWriterLockSlim characterLookupLock = new ReaderWriterLockSlim();

        private bool CharacterLookupTable_ContainsKey(uint key)
        {
            characterLookupLock.EnterReadLock();
            try
            {
                return m_CharacterLookupDictionary.ContainsKey(key);
            }
            finally
            {
                characterLookupLock.ExitReadLock();
            }
        }

        private bool CharacterLookupTable_TryGet(uint key, out Character character)
        {
            characterLookupLock.EnterReadLock();
            try
            {
                return m_CharacterLookupDictionary.TryGetValue(key, out character);
            }
            finally
            {
                characterLookupLock.ExitReadLock();
            }
        }

        private void CharacterLookupTable_TryAdd(uint key, Character character)
        {
            characterLookupLock.EnterWriteLock();
            try
            {
                m_CharacterLookupDictionary.TryAdd(key, character);
            }
            finally
            {
                characterLookupLock.ExitWriteLock();
            }
        }

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
            get => m_AtlasTextures;
            set => m_AtlasTextures = value;
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
        /// Determines if OpenType font features should be retrieved from the source font file as new characters and glyphs are added dynamically to the font asset.
        /// </summary>
        public bool getFontFeatures
        {
            get { return m_GetFontFeatures; }
            set { m_GetFontFeatures = value; }
        }
        [SerializeField]
        private bool m_GetFontFeatures = true;

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
        ///
        /// </summary>
        [SerializeField] internal bool m_ShouldReimportFontFeatures;

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
                return CreateFontAsset(fontRef.filePath, fontRef.faceIndex, pointSize, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.DynamicOS, true);

            Debug.Log("Unable to find a font file with the specified Family Name [" + familyName + "] and Style [" + styleName + "].");

            return null;
        }

        /// <summary>
        /// Create a List of Font Assets with a list of fallbacks defined by FamilyNames, all regular styles
        /// </summary>
        /// <param name="fallbacksFamilyNames">The desired fonts to be included, ordered by preferred fallback</param>
        /// <param name="pointSize">Optional point size.</param>
        /// <returns>A List of FontAsset containing the available fallbacks for the current platform.</returns>
        internal static List<FontAsset> CreateFontAssetOSFallbackList(string[] fallbacksFamilyNames, Shader shader, int pointSize = 90)
        {
            List<FontAsset> fallbackList = new List<FontAsset>();
            FontAsset currentFontAsset;

            foreach (var familyName in fallbacksFamilyNames)
            {
                currentFontAsset = CreateFontAssetFromFamilyName(familyName, shader, pointSize);

                if (currentFontAsset == null)
                    continue;

                fallbackList.Add(currentFontAsset);
            }

            return fallbackList;
        }

        internal static FontAsset CreateFontAssetWithOSFallbackList(string[] fallbacksFamilyNames, Shader shader, int pointSize = 90)
        {
            FontAsset mainFontAsset = null;
            FontAsset currentFontAsset;

            foreach (var familyName in fallbacksFamilyNames)
            {
                currentFontAsset = CreateFontAssetFromFamilyName(familyName, shader, pointSize);

                if (currentFontAsset == null)
                    continue;

                if (mainFontAsset == null)
                    mainFontAsset = currentFontAsset;

                if (mainFontAsset.fallbackFontAssetTable == null)
                    mainFontAsset.fallbackFontAssetTable = new List<FontAsset>();

                mainFontAsset.fallbackFontAssetTable.Add(currentFontAsset);
            }

            return mainFontAsset;
        }

        private static FontAsset CreateFontAssetFromFamilyName(string familyName, Shader shader, int pointSize = 90)
        {
            FontAsset fontAsset = null;

            if (FontEngine.TryGetSystemFontReference(familyName, null, out FontReference fontRef))
                fontAsset = CreateFontAsset(fontRef.filePath, fontRef.faceIndex, pointSize, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.DynamicOS, true);

            if (fontAsset == null)
                return null;

            fontAsset.hideFlags = HideFlags.DontSave;
            fontAsset.atlasTextures[0].hideFlags = HideFlags.DontSave;
            fontAsset.material.hideFlags = HideFlags.DontSave;
            fontAsset.isMultiAtlasTexturesEnabled = true;
            fontAsset.material.shader = shader;

            return fontAsset;
        }

        /// <summary>
        /// Creates a new font asset instance from the font file at the given file path.
        /// </summary>
        /// <param name="fontFilePath">The file path of the font file.</param>
        /// <param name="faceIndex">The index of font face.</param>
        /// <param name="samplingPointSize">The sampling point size.</param>
        /// <param name="atlasPadding">The padding between individual glyphs in the font atlas texture.</param>
        /// <param name="renderMode">The atlas render mode.</param>
        /// <param name="atlasWidth">The atlas texture width.</param>
        /// <param name="atlasHeight">The atlas texture height.</param>
        /// <returns>An instance of the newly created font asset.</returns>
        public static FontAsset CreateFontAsset(string fontFilePath, int faceIndex, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight)
        {
            return CreateFontAsset(fontFilePath, faceIndex, samplingPointSize, atlasPadding, renderMode, atlasWidth, atlasHeight, AtlasPopulationMode.Dynamic, true);
        }

        static FontAsset CreateFontAsset(string fontFilePath, int faceIndex, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight, AtlasPopulationMode atlasPopulationMode = AtlasPopulationMode.DynamicOS, bool enableMultiAtlasSupport = true)
        {
            // Load Font Face
            if (FontEngine.LoadFontFace(fontFilePath, samplingPointSize, faceIndex) != FontEngineError.Success)
            {
                Debug.Log("Unable to load font face from [" + fontFilePath + "].");
                return null;
            }

            FontAsset fontAsset = CreateFontAssetInstance(null, atlasPadding, renderMode, atlasWidth, atlasHeight, atlasPopulationMode, enableMultiAtlasSupport);

            // Set font file path
            fontAsset.m_SourceFontFilePath = fontFilePath;

            return fontAsset;
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

        internal static FontAsset CreateFontAsset(Font font, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight, Shader shader, AtlasPopulationMode atlasPopulationMode = AtlasPopulationMode.Dynamic, bool enableMultiAtlasSupport = true)
        {
            return CreateFontAsset(font, 0, samplingPointSize, atlasPadding, renderMode, atlasWidth, atlasHeight, shader, atlasPopulationMode, enableMultiAtlasSupport);
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
            return CreateFontAsset(font, 0, samplingPointSize, atlasPadding, renderMode, atlasWidth, atlasHeight, null, atlasPopulationMode, enableMultiAtlasSupport);
        }

        static FontAsset CreateFontAsset(Font font, int faceIndex, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight, Shader shader, AtlasPopulationMode atlasPopulationMode = AtlasPopulationMode.Dynamic, bool enableMultiAtlasSupport = true)
        {
            if (font.name == "LegacyRuntime")
            {
                var fonts = Font.GetOSFallbacks();
                if (FontEngine.LoadFontFace(font, samplingPointSize, faceIndex) == FontEngineError.Success)
                {
                    var mainFontAssset = CreateFontAssetInstance(font, atlasPadding, renderMode, atlasWidth, atlasHeight, atlasPopulationMode, enableMultiAtlasSupport);
                    var fallbacks = CreateFontAssetOSFallbackList(fonts, shader, samplingPointSize);
                    mainFontAssset.fallbackFontAssetTable = fallbacks;
                    return mainFontAssset;
                }

                var fontAsset = CreateFontAssetWithOSFallbackList(fonts, shader, samplingPointSize);
                if (fontAsset != null)
                    return fontAsset;
            }

            // Load Font Face
            if (FontEngine.LoadFontFace(font, samplingPointSize, faceIndex) != FontEngineError.Success)
            {
                FontAsset systemFontAsset = CreateFontAsset(font.name, "Regular");
                if (systemFontAsset != null)
                    return systemFontAsset;

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

            if (atlasPopulationMode == AtlasPopulationMode.Dynamic && font != null)
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
            TextureFormat texFormat = ((GlyphRasterModes)renderMode & GlyphRasterModes.RASTER_MODE_COLOR) == GlyphRasterModes.RASTER_MODE_COLOR ? TextureFormat.RGBA32 : TextureFormat.Alpha8;

            Texture2D texture = new Texture2D(1, 1, texFormat, false);
            fontAsset.atlasTextures[0] = texture;

            fontAsset.isMultiAtlasTexturesEnabled = enableMultiAtlasSupport;

            // Add free rectangle of the size of the texture.
            int packingModifier;
            if (((GlyphRasterModes)renderMode & GlyphRasterModes.RASTER_MODE_BITMAP) == GlyphRasterModes.RASTER_MODE_BITMAP)
            {
                Material tmp_material = null;
                packingModifier = 0;

                if (texFormat == TextureFormat.Alpha8)
                    tmp_material = new Material(TextShaderUtilities.ShaderRef_MobileBitmap);
                else
                    tmp_material = new Material(TextShaderUtilities.ShaderRef_Sprite);

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

        // ================================================================================
        //
        // ================================================================================

        // Editor Only Callbacks
        internal static Action<Texture, FontAsset> OnFontAssetTextureChanged;
        internal static Action<FontAsset> RegisterResourceForUpdate;
        internal static Action<FontAsset> RegisterResourceForReimport;
        internal static Action<Texture2D, bool> SetAtlasTextureIsReadable;
        internal static Func<string, Font> GetSourceFontRef;
        internal static Func<Font, string> SetSourceFontGUID;
        internal static Func<bool> EditorApplicationIsUpdating;

        // Profiler Marker declarations
        private static ProfilerMarker k_ReadFontAssetDefinitionMarker = new ProfilerMarker("FontAsset.ReadFontAssetDefinition");
        private static ProfilerMarker k_AddSynthesizedCharactersMarker = new ProfilerMarker("FontAsset.AddSynthesizedCharacters");
        private static ProfilerMarker k_TryAddGlyphMarker = new ProfilerMarker("FontAsset.TryAddGlyph");
        private static ProfilerMarker k_TryAddCharacterMarker = new ProfilerMarker("FontAsset.TryAddCharacter");
        private static ProfilerMarker k_TryAddCharactersMarker = new ProfilerMarker("FontAsset.TryAddCharacters");
        private static ProfilerMarker k_UpdateLigatureSubstitutionRecordsMarker = new ProfilerMarker("FontAsset.UpdateLigatureSubstitutionRecords");
        private static ProfilerMarker k_UpdateGlyphAdjustmentRecordsMarker = new ProfilerMarker("FontAsset.UpdateGlyphAdjustmentRecords");
        private static ProfilerMarker k_UpdateDiacriticalMarkAdjustmentRecordsMarker = new ProfilerMarker("FontAsset.UpdateDiacriticalAdjustmentRecords");
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
            // Skip validation until the Editor has been fully loaded.
            if (Time.frameCount == 0)
                return;

            // See TMPB-187
            if (EditorApplicationIsUpdating?.Invoke() ?? true)
                return;

            // Make sure our lookup dictionary have been initialized.
            if (m_CharacterLookupDictionary == null || m_GlyphLookupDictionary == null)
                ReadFontAssetDefinition();
        }

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

            Character character;
            // Set Cap Line using the capital letter 'X'
            if (m_FaceInfo.capLine == 0 && m_CharacterLookupDictionary.TryGetValue('X', out character))
            {
                uint glyphIndex = character.glyphIndex;
                m_FaceInfo.capLine = m_GlyphLookupDictionary[glyphIndex].metrics.horizontalBearingY;
            }

            // Set Mean Line using the lowercase letter 'x'
            if (m_FaceInfo.meanLine == 0 && m_CharacterLookupDictionary.TryGetValue('X', out character))
            {
                uint glyphIndex = character.glyphIndex;
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

            // Update Units per EM for pre-existing font assets.
            if (m_FaceInfo.unitsPerEM == 0)
                m_FaceInfo.unitsPerEM = FontEngine.GetFaceInfo().unitsPerEM;

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

            if ((m_AtlasPopulationMode == AtlasPopulationMode.Dynamic || m_AtlasPopulationMode == AtlasPopulationMode.DynamicOS) && m_ShouldReimportFontFeatures)
                ImportFontFeatures();

            //
            InitializeLigatureSubstitutionLookupDictionary();

            // Initialize and populate glyph pair adjustment records
            InitializeGlyphPaidAdjustmentRecordsLookupDictionary();

            // Initialize and populate mark to base adjustment records
            InitializeMarkToBaseAdjustmentRecordsLookupDictionary();

            // Initialize and populate mark to base adjustment records
            InitializeMarkToMarkAdjustmentRecordsLookupDictionary();
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

            // Clear missing unicode lookup
            if (m_MissingUnicodesFromFontFile != null)
                m_MissingUnicodesFromFontFile.Clear();
        }

        internal void InitializeLigatureSubstitutionLookupDictionary()
        {
            if (m_FontFeatureTable.m_LigatureSubstitutionRecordLookup == null)
                m_FontFeatureTable.m_LigatureSubstitutionRecordLookup = new Dictionary<uint, List<LigatureSubstitutionRecord>>();
            else
                m_FontFeatureTable.m_LigatureSubstitutionRecordLookup.Clear();

            List<LigatureSubstitutionRecord> substitutionRecords = m_FontFeatureTable.m_LigatureSubstitutionRecords;
            if (substitutionRecords != null)
            {
                for (int i = 0; i < substitutionRecords.Count; i++)
                {
                    LigatureSubstitutionRecord record = substitutionRecords[i];

                    // Skip newly added records
                    if (record.componentGlyphIDs == null || record.componentGlyphIDs.Length == 0)
                        continue;

                    uint keyGlyphIndex = record.componentGlyphIDs[0];

                    if (!m_FontFeatureTable.m_LigatureSubstitutionRecordLookup.ContainsKey(keyGlyphIndex))
                        m_FontFeatureTable.m_LigatureSubstitutionRecordLookup.Add(keyGlyphIndex, new List<LigatureSubstitutionRecord> {record});
                    else
                        m_FontFeatureTable.m_LigatureSubstitutionRecordLookup[keyGlyphIndex].Add(record);
                }
            }
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

        internal void InitializeMarkToBaseAdjustmentRecordsLookupDictionary()
        {
            // Read Mark to Base adjustment records
            if (m_FontFeatureTable.m_MarkToBaseAdjustmentRecordLookup == null)
                m_FontFeatureTable.m_MarkToBaseAdjustmentRecordLookup = new Dictionary<uint, MarkToBaseAdjustmentRecord>();
            else
                m_FontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.Clear();

            List<MarkToBaseAdjustmentRecord> adjustmentRecords = m_FontFeatureTable.m_MarkToBaseAdjustmentRecords;
            if (adjustmentRecords != null)
            {
                for (int i = 0; i < adjustmentRecords.Count; i++)
                {
                    MarkToBaseAdjustmentRecord record = adjustmentRecords[i];

                    uint key = record.markGlyphID << 16 | record.baseGlyphID;

                    if (!m_FontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.ContainsKey(key))
                        m_FontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.Add(key, record);
                }
            }
        }

        internal void InitializeMarkToMarkAdjustmentRecordsLookupDictionary()
        {
            // Read Mark to Base adjustment records
            if (m_FontFeatureTable.m_MarkToMarkAdjustmentRecordLookup == null)
                m_FontFeatureTable.m_MarkToMarkAdjustmentRecordLookup = new Dictionary<uint, MarkToMarkAdjustmentRecord>();
            else
                m_FontFeatureTable.m_MarkToMarkAdjustmentRecordLookup.Clear();

            List<MarkToMarkAdjustmentRecord> adjustmentRecords = m_FontFeatureTable.m_MarkToMarkAdjustmentRecords;
            if (adjustmentRecords != null)
            {
                for (int i = 0; i < adjustmentRecords.Count; i++)
                {
                    MarkToMarkAdjustmentRecord record = adjustmentRecords[i];

                    uint key = record.combiningMarkGlyphID << 16 | record.baseMarkGlyphID;

                    if (!m_FontFeatureTable.m_MarkToMarkAdjustmentRecordLookup.ContainsKey(key))
                        m_FontFeatureTable.m_MarkToMarkAdjustmentRecordLookup.Add(key, record);
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

            // Add Zero Width Space <ZWJ> \u200D
            //AddSynthesizedCharacter(0x200D, isFontFaceLoaded);

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
            CharacterLookupTable_TryAdd(unicode, character);

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

                // Try loading the font face from source font object
                if (FontEngine.LoadFontFace(m_SourceFontFile, m_FaceInfo.pointSize, m_FaceInfo.faceIndex) == FontEngineError.Success)
                    return FontEngineError.Success;

                // Try loading the font face from file path
                if (string.IsNullOrEmpty(m_SourceFontFilePath) == false)
                    return  FontEngine.LoadFontFace(m_SourceFontFilePath, m_FaceInfo.pointSize, m_FaceInfo.faceIndex);

                return FontEngineError.Invalid_Face;
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
            m_FontFeatureTable.SortMarkToBaseAdjustmentRecords();
            m_FontFeatureTable.SortMarkToMarkAdjustmentRecords();
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
            if (characterLookupTable == null)
                return false;

            return CharacterLookupTable_ContainsKey((uint)character);
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
            return HasCharacter((uint)character, searchFallbacks, tryAddCharacter);
        }

        /// <summary>
        /// Function to check if a character is contained in the font asset with the option to also check potential local fallbacks.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="searchFallbacks"></param>
        /// <param name="tryAddCharacter"></param>
        /// <returns></returns>
        public bool HasCharacter(uint character, bool searchFallbacks = false, bool tryAddCharacter = false)
        {
            // Read font asset definition if it hasn't already been done.
            if (characterLookupTable == null)
                return false;

            // Check font asset
            if (CharacterLookupTable_ContainsKey(character))
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
            if (CharacterLookupTable_ContainsKey(character))
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
            if (characterLookupTable == null)
            {
                missingCharacters = null;
                return false;
            }

            missingCharacters = new List<char>();

            for (int i = 0; i < text.Length; i++)
            {
                if (!CharacterLookupTable_ContainsKey(text[i]))
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
        /// <returns>Returns true if all requested characters are available in the font asset and potential fallbacks.</returns>
        public bool HasCharacters(string text, out uint[] missingCharacters, bool searchFallbacks = false, bool tryAddCharacter = false)
        {
            missingCharacters = null;

            // Read font asset definition if it hasn't already been done.
            if (characterLookupTable == null)
                return false;

            // Clear internal list of
            s_MissingCharacterList.Clear();

            for (int i = 0; i < text.Length; i++)
            {
                bool isMissingCharacter = true;
                uint character = text[i];

                if (CharacterLookupTable_ContainsKey(character))
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
            if (characterLookupTable == null)
                return false;

            for (int i = 0; i < text.Length; i++)
            {
                if (!CharacterLookupTable_ContainsKey(text[i]))
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
        /// Get the glyph index for the given Unicode.
        /// This overload of GetGlyphIndex does not return the success status.
        /// </summary>
        /// <param name="unicode">The Unicode value to get the glyph index for.</param>
        /// <returns>The glyph index for the given Unicode.</returns>
        internal uint GetGlyphIndex(uint unicode)
        {
            bool success;
            return GetGlyphIndex(unicode, out success);
        }

        /// <summary>
        /// Internal function used to get the glyph index for the given Unicode.
        /// </summary>
        /// <param name="unicode"></param>
        /// <returns></returns>
        internal uint GetGlyphIndex(uint unicode, out bool success)
        {
            success = true;
            // Check if glyph already exists in font asset.
            if (CharacterLookupTable_TryGet(unicode, out Character character))
                return character.glyphIndex;

            if (JobsUtility.IsExecutingJob)
            {
                success = false;
                return 0;
            }

            // Load font face.
            return LoadFontFace() == FontEngineError.Success ? FontEngine.GetGlyphIndex(unicode) : 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="unicode"></param>
        /// <param name="variantSelectorUnicode"></param>
        /// <returns></returns>
        internal uint GetGlyphVariantIndex(uint unicode, uint variantSelectorUnicode)
        {
            // Load font face.
            return LoadFontFace() == FontEngineError.Success ? FontEngine.GetVariantGlyphIndex(unicode, variantSelectorUnicode) : 0;
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

            // Clear glyph, character
            ClearCharacterAndGlyphTables();

            // Clear font features
            ClearFontFeaturesTables();

            // Clear atlas textures
            ClearAtlasTextures(true);

            ReadFontAssetDefinition();

            //TextResourceManager.RebuildFontAssetCache();

            // Add existing glyphs and characters back in the font asset (if any)
            if (unicodeCharacters.Length > 0)
                TryAddCharacters(unicodeCharacters, m_GetFontFeatures /*&& TMP_Settings.getFontFeaturesAtRuntime*/);

            k_UpdateFontAssetDataMarker.End();
        }

        /// <summary>
        /// Clears font asset data including the glyph and character tables and textures.
        /// Function might be changed to Internal and only used in tests.
        /// </summary>
        /// <param name="setAtlasSizeToZero">Will set the atlas texture size to zero width and height if true.</param>
        public void ClearFontAssetData(bool setAtlasSizeToZero = false)
        {
            using (k_ClearFontAssetDataMarker.Auto()) {
                // Record full object undo in the Editor.
                //UnityEditor.Undo.RecordObjects(new UnityEngine.Object[] { this, this.atlasTexture }, "Resetting Font Asset");

                // Clear character and glyph tables
                ClearCharacterAndGlyphTables();

                // Clear font feature tables
                ClearFontFeaturesTables();

                // Clear atlas textures
                ClearAtlasTextures(setAtlasSizeToZero);

                ReadFontAssetDefinition();

                //TextResourceManager.RebuildFontAssetCache();

                // Makes the changes to the font asset persistent.
                RegisterResourceForUpdate?.Invoke(this);
            }
        }

        /// <summary>
        /// Clear character and glyph tables along with atlas textures.
        /// </summary>
        internal void ClearCharacterAndGlyphTablesInternal()
        {
            // Clear character and glyph tables
            ClearCharacterAndGlyphTables();

            // Clear atlas textures
            ClearAtlasTextures(true);

            ReadFontAssetDefinition();

            //TextResourceManager.RebuildFontAssetCache();

            // Makes the changes to the font asset persistent.
            RegisterResourceForUpdate?.Invoke(this);
        }

        internal void ClearFontFeaturesInternal()
        {
            ClearFontFeaturesTables();

            ReadFontAssetDefinition();

            // Makes the changes to the font asset persistent.
            RegisterResourceForUpdate?.Invoke(this);
        }

        /// <summary>
        /// Clear character and glyph tables.
        /// </summary>
        void ClearCharacterAndGlyphTables()
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
        }

        /// <summary>
        /// Clear OpenType font features
        /// </summary>
        void ClearFontFeaturesTables()
        {
            // Clear Ligature Table
            if (m_FontFeatureTable != null && m_FontFeatureTable.m_LigatureSubstitutionRecords != null)
                m_FontFeatureTable.m_LigatureSubstitutionRecords.Clear();

                // Clear Glyph Adjustment Table
                if (m_FontFeatureTable != null && m_FontFeatureTable.m_GlyphPairAdjustmentRecords != null)
                    m_FontFeatureTable.m_GlyphPairAdjustmentRecords.Clear();

                // Clear Mark-to-Base Adjustment Table
                if (m_FontFeatureTable != null && m_FontFeatureTable.m_MarkToBaseAdjustmentRecords != null)
                    m_FontFeatureTable.m_MarkToBaseAdjustmentRecords.Clear();

                // Clear Mark-to-Mark Adjustment Table
                if (m_FontFeatureTable != null && m_FontFeatureTable.m_MarkToMarkAdjustmentRecords != null)
                    m_FontFeatureTable.m_MarkToMarkAdjustmentRecords.Clear();

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

            TextureFormat texFormat = ((GlyphRasterModes)m_AtlasRenderMode & GlyphRasterModes.RASTER_MODE_COLOR) == GlyphRasterModes.RASTER_MODE_COLOR ? TextureFormat.RGBA32 : TextureFormat.Alpha8;

            if (setAtlasSizeToZero)
            {
                texture.Reinitialize(1, 1, texFormat, false);
            }
            else if (texture.width != m_AtlasWidth || texture.height != m_AtlasHeight)
            {
                texture.Reinitialize(m_AtlasWidth, m_AtlasHeight, texFormat, false);
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
