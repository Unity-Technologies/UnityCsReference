// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Serialization;
using UnityEngine.TextCore.LowLevel;


namespace UnityEngine.TextCore.Text
{
    [System.Serializable][ExcludeFromPresetAttribute][ExcludeFromObjectFactory]
    public class TextSettings : ScriptableObject
    {
        internal string k_SystemFontName =
            "Lucida Grande"
        ;

        /// <summary>
        /// The version of the TextSettings class.
        /// Version 1.2.0 was introduced with the TextCore package
        /// </summary>
        public string version
        {
            get => m_Version;
            internal set => m_Version = value;
        }
        [SerializeField]
        protected string m_Version;

        /// <summary>
        /// The Font Asset automatically assigned to newly created text objects.
        /// </summary>
        public FontAsset defaultFontAsset
        {
            get => m_DefaultFontAsset;
            set => m_DefaultFontAsset = value;
        }
        [FormerlySerializedAs("m_defaultFontAsset")][SerializeField]
        protected FontAsset m_DefaultFontAsset;

        /// <summary>
        /// The relative path to a Resources folder in the project where the text system will look to load font assets.
        /// The default location is "Resources/Fonts & Materials".
        /// </summary>
        public string defaultFontAssetPath
        {
            get => m_DefaultFontAssetPath;
            set => m_DefaultFontAssetPath = value;
        }
        [FormerlySerializedAs("m_defaultFontAssetPath")][SerializeField]
        protected string m_DefaultFontAssetPath = "Fonts & Materials/";

        /// <summary>
        /// List of potential font assets the text system will search recursively to look for requested characters.
        /// </summary>
        public List<FontAsset> fallbackFontAssets
        {
            get => m_FallbackFontAssets;
            set => m_FallbackFontAssets = value;
        }
        [FormerlySerializedAs("m_fallbackFontAssets")][SerializeField]
        protected List<FontAsset> m_FallbackFontAssets;

        /// <summary>
        /// Determines if the text system will use an instance material derived from the primary material preset or use the default material of the fallback font asset.
        /// </summary>
        public bool matchMaterialPreset
        {
            get => m_MatchMaterialPreset;
            set => m_MatchMaterialPreset = value;
        }
        [FormerlySerializedAs("m_matchMaterialPreset")][SerializeField]
        protected bool m_MatchMaterialPreset;

        /// <summary>
        /// Determines if OpenType Font Features should be retrieved at runtime from the source font file.
        /// </summary>
        // public bool getFontFeaturesAtRuntime
        // {
        //     get { return m_GetFontFeaturesAtRuntime; }
        // }
        // [SerializeField]
        // private bool m_GetFontFeaturesAtRuntime = true;

        /// <summary>
        /// The unicode value of the character that will be used when the requested character is missing from the font asset and potential fallbacks.
        /// </summary>
        public int missingCharacterUnicode
        {
            get => m_MissingCharacterUnicode;
            set => m_MissingCharacterUnicode = value;
        }
        [FormerlySerializedAs("m_missingGlyphCharacter")][SerializeField]
        protected int m_MissingCharacterUnicode;


        /// <summary>
        /// Determines if the "Clear Dynamic Data on Build" property will be set to true or false on newly created dynamic font assets.
        /// </summary>
        public bool clearDynamicDataOnBuild
        {
            get => m_ClearDynamicDataOnBuild;
            set => m_ClearDynamicDataOnBuild = value;
        }
        [SerializeField]
        protected bool m_ClearDynamicDataOnBuild = true;

        /// <summary>
        /// Determines if Emoji support is enabled in the Input Field TouchScreenKeyboard.
        /// </summary>
        public bool enableEmojiSupport
        {
            get { return m_EnableEmojiSupport; }
            set { m_EnableEmojiSupport = value; }
        }
        [SerializeField]
        private bool m_EnableEmojiSupport;

        /// <summary>
        /// list of Fallback Text Assets (Font Assets and Sprite Assets) used to lookup characters defined in the Unicode as Emojis.
        /// </summary>
        public List<TextAsset> emojiFallbackTextAssets
        {
            get => m_EmojiFallbackTextAssets;
            set => m_EmojiFallbackTextAssets = value;
        }
        [SerializeField]
        private List<TextAsset> m_EmojiFallbackTextAssets;

        /// <summary>
        /// The Sprite Asset to be used by default.
        /// </summary>
        public SpriteAsset defaultSpriteAsset
        {
            get => m_DefaultSpriteAsset;
            set => m_DefaultSpriteAsset = value;
        }
        [FormerlySerializedAs("m_defaultSpriteAsset")][SerializeField]
        protected SpriteAsset m_DefaultSpriteAsset;

        /// <summary>
        /// The relative path to a Resources folder in the project where the text system will look to load sprite assets.
        /// The default location is "Resources/Sprite Assets".
        /// </summary>
        public string defaultSpriteAssetPath
        {
            get => m_DefaultSpriteAssetPath;
            set => m_DefaultSpriteAssetPath = value;
        }
        [FormerlySerializedAs("m_defaultSpriteAssetPath")][SerializeField]
        protected string m_DefaultSpriteAssetPath = "Sprite Assets/";

        [Obsolete("The Fallback Sprite Assets list is now obsolete. Use the emojiFallbackTextAssets instead.", true)]
        public List<SpriteAsset> fallbackSpriteAssets
        {
            get => m_FallbackSpriteAssets;
            set => m_FallbackSpriteAssets = value;
        }
        [SerializeField]
        protected List<SpriteAsset> m_FallbackSpriteAssets;

        /// <summary>
        /// The unicode value of the sprite character that will be used when the requested character sprite is missing from the sprite asset and potential fallbacks.
        /// </summary>
        public uint missingSpriteCharacterUnicode
        {
            get => m_MissingSpriteCharacterUnicode;
            set => m_MissingSpriteCharacterUnicode = value;
        }
        [SerializeField]
        protected uint m_MissingSpriteCharacterUnicode;

        /// <summary>
        /// The Default Style Sheet used by the text objects.
        /// </summary>
        public TextStyleSheet defaultStyleSheet
        {
            get => m_DefaultStyleSheet;
            set => m_DefaultStyleSheet = value;
        }
        [FormerlySerializedAs("m_defaultStyleSheet")][SerializeField]
        protected TextStyleSheet m_DefaultStyleSheet;

        /// <summary>
        /// The relative path to a Resources folder in the project where the text system will look to load style sheets.
        /// The default location is "Resources/Style Sheets".
        /// </summary>
        public string styleSheetsResourcePath
        {
            get => m_StyleSheetsResourcePath;
            set => m_StyleSheetsResourcePath = value;
        }
        [SerializeField]
        protected string m_StyleSheetsResourcePath = "Text Style Sheets/";

        /// <summary>
        /// The relative path to a Resources folder in the project where the text system will look to load color gradient presets.
        /// The default location is "Resources/Color Gradient Presets".
        /// </summary>
        public string defaultColorGradientPresetsPath
        {
            get => m_DefaultColorGradientPresetsPath;
            set => m_DefaultColorGradientPresetsPath = value;
        }
        [FormerlySerializedAs("m_defaultColorGradientPresetsPath")][SerializeField]
        protected string m_DefaultColorGradientPresetsPath = "Text Color Gradients/";

        // =============================================
        // Line breaking rules
        // =============================================

        /// <summary>
        /// Text file that contains the line breaking rules for all unicode characters.
        /// </summary>
        public UnicodeLineBreakingRules lineBreakingRules
        {
            get
            {
                if (m_UnicodeLineBreakingRules == null)
                {
                    m_UnicodeLineBreakingRules = new UnicodeLineBreakingRules();
                    m_UnicodeLineBreakingRules.LoadLineBreakingRules();
                }

                return m_UnicodeLineBreakingRules;
            }
            set => m_UnicodeLineBreakingRules = value;
        }
        [SerializeField]
        protected UnicodeLineBreakingRules m_UnicodeLineBreakingRules;


        // =============================================
        // Text object specific settings
        // =============================================

        // To be implemented in the derived classes

        // =============================================
        //
        // =============================================

        /// <summary>
        /// Controls the display of warning messages in the console.
        /// </summary>
        public bool displayWarnings
        {
            get => m_DisplayWarnings;
            set => m_DisplayWarnings = value;
        }
        [FormerlySerializedAs("m_warningsDisabled")][SerializeField]
        protected bool m_DisplayWarnings = false;

        // =============================================
        // Functions
        // =============================================

        void OnEnable()
        {
            lineBreakingRules.LoadLineBreakingRules();
        }

        protected void InitializeFontReferenceLookup()
        {
            if (m_FontReferences == null)
                m_FontReferences = new List<FontReferenceMap>();

            for (int i = 0; i < m_FontReferences.Count; i++)
            {
                FontReferenceMap fontRef = m_FontReferences[i];

                // Validate fontRef data
                if (fontRef.font == null || fontRef.fontAsset == null)
                {
                    Debug.LogWarning("Deleting invalid font reference.");
                    m_FontReferences.RemoveAt(i);
                    i -= 1;
                    continue;
                }

                int id = fontRef.font.GetHashCode() + fontRef.fontAsset.material.shader.GetHashCode();

                if (!m_FontLookup.ContainsKey(id))
                    m_FontLookup.Add(id, fontRef.fontAsset);
            }
        }

        [System.Serializable]
        struct FontReferenceMap
        {
            public Font font;
            public FontAsset fontAsset;

            public FontReferenceMap(Font font, FontAsset fontAsset)
            {
                this.font = font;
                this.fontAsset = fontAsset;
            }
        }

        // Internal for testing purposes
        internal Dictionary<int, FontAsset> m_FontLookup;
        private List<FontReferenceMap> m_FontReferences = new List<FontReferenceMap>();


        protected FontAsset GetCachedFontAssetInternal(Font font)
        {
            return GetCachedFontAsset(font, TextShaderUtilities.ShaderRef_MobileSDF);
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal FontAsset GetCachedFontAsset(Font font, Shader shader)
        {
            if (font == null || shader == null)
                return null;

            if (m_FontLookup == null)
            {
                m_FontLookup = new Dictionary<int, FontAsset>();
                InitializeFontReferenceLookup();
            }

            int id = font.GetHashCode() + shader.GetHashCode();

            if (m_FontLookup.ContainsKey(id))
                return m_FontLookup[id];

            if (JobsUtility.IsExecutingJob)
                return null;

            FontAsset fontAsset;
            if (font.name == "System Normal" || font.name == "System Small" || font.name == "System Big" || font.name == "System Warning")
            {
                fontAsset = FontAsset.CreateFontAsset(k_SystemFontName, "Regular", 90);
            }
            else if (font.name == "System Normal Bold" || font.name == "System Small Bold")
            {
                fontAsset = FontAsset.CreateFontAsset(k_SystemFontName, "Bold", 90);
            }
            else
            {
                fontAsset = FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            }

            if (fontAsset != null)
            {
                fontAsset.hideFlags = HideFlags.DontSave;
                fontAsset.atlasTextures[0].hideFlags = HideFlags.DontSave;
                fontAsset.material.hideFlags = HideFlags.DontSave;
                fontAsset.isMultiAtlasTexturesEnabled = true;
                fontAsset.material.shader = shader;

                m_FontReferences.Add(new FontReferenceMap(font, fontAsset));
                m_FontLookup.Add(id, fontAsset);
            }

            return fontAsset;
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal virtual float GetEditorTextSharpness()
        {
            Debug.LogWarning("GetEditorTextSettings() should only be called on EditorTextSettings");
            return 0.0f;
        }

        [VisibleToOtherModules("UnityEngine.IMGUIModule")]
        internal virtual Font GetEditorFont()
        {
            Debug.LogWarning("GetEditorTextSettings() should only be called on EditorTextSettings");
            return null;
        }
    }
}
