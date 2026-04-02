// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Serialization;

#pragma warning disable CS0618 // Obsolete types/members: UnicodeLineBreakingRules; AtlasPopulationMode.Static, characterTable; TextCoreShaderGUI, TextCoreShaderGUISDF, TextCoreShaderGUIBitmap, TextShaderUtilities; handled natively by ATG

namespace UnityEngine.TextCore.Text
{
    [System.Serializable]
    [ExcludeFromPresetAttribute]
    [ExcludeFromObjectFactory]
    [NativeHeader("Modules/TextCoreTextEngine/Native/TextSettings.h")]
    public partial class TextSettings : ScriptableObject
    {
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
        /// Editor-only setting to show obsolete TextCore properties in the inspector.
        /// </summary>
        [SerializeField]
        internal bool m_ShowObsoleteProperties;

        [FormerlySerializedAs("m_defaultFontAsset")]
        [SerializeField]
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

        [FormerlySerializedAs("m_defaultFontAssetPath")]
        [SerializeField]
        protected string m_DefaultFontAssetPath = "Fonts & Materials/";

        /// <summary>
        /// List of potential font assets the text system will search recursively to look for requested characters.
        /// </summary>
        public List<FontAsset> fallbackFontAssets
        {
            get => m_FallbackFontAssets;
            set
            {
                m_FallbackFontAssets = value;
                m_IsNativeTextSettingsDirty = true;
            }
        }

        [FormerlySerializedAs("m_fallbackFontAssets")]
        [SerializeField]
        protected List<FontAsset> m_FallbackFontAssets;

        [FormerlySerializedAs("m_matchMaterialPreset")]
        [SerializeField]
        protected bool m_MatchMaterialPreset;

        [FormerlySerializedAs("m_missingGlyphCharacter")]
        [SerializeField]
        protected int m_MissingCharacterUnicode;

        internal List<FontAsset> fallbackOSFontAssets
        {
            [VisibleToOtherModules("UnityEngine.UIElementsModule")]
            get
            {
                if (m_FallbackOSFontAssets == null)
                {
                    m_FallbackOSFontAssets = GetOSFontAssetList();
                }
                return m_FallbackOSFontAssets;
            }
        }

        [SerializeField]
        List<FontAsset> m_FallbackOSFontAssets;

        internal bool isFallbackOSFontAssetsInitialized => m_FallbackOSFontAssets != null;

        static FontAsset s_RuntimeDefault;

        private FontAsset GetDefaultFont()
        {
            if (s_RuntimeDefault == null)
            {
                s_RuntimeDefault = GetCachedFontAsset(Font.GetDefault());
            }
            return s_RuntimeDefault;
        }

        internal virtual List<FontAsset> GetFallbackFontAssets(bool isRaster, int textPixelSize = -1)
        {
            return fallbackFontAssets;
        }

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
        /// Determines if the "Clear Dynamic Data on Build" property will be set to true or false on newly created dynamic font assets.
        /// </summary>
        public bool clearDynamicDataOnBuild
        {
            get => m_ClearDynamicDataOnBuild;
            set => m_ClearDynamicDataOnBuild = value;
        }
        [SerializeField]
        protected bool m_ClearDynamicDataOnBuild = true;

        [SerializeField]
        private bool m_EnableEmojiSupport;

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

        [FormerlySerializedAs("m_defaultSpriteAsset")]
        [SerializeField]
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

        [FormerlySerializedAs("m_defaultSpriteAssetPath")]
        [SerializeField]
        protected string m_DefaultSpriteAssetPath = "Sprite Assets/";

        [SerializeField]
        protected List<SpriteAsset> m_FallbackSpriteAssets;

        internal static SpriteAsset s_GlobalSpriteAsset { private set; get; }

        [SerializeField]
        protected uint m_MissingSpriteCharacterUnicode;

        /// <summary>
        /// list of Fallback Text Assets (Font Assets and Sprite Assets) used to lookup characters defined in the Unicode as Emojis.
        /// </summary>
        public List<TextAsset> emojiFallbackTextAssets
        {
            get => m_EmojiFallbackTextAssets;
            set
            {
                m_EmojiFallbackTextAssets = value;
                m_IsNativeTextSettingsDirty = true;
            }
        }

        /// <summary>
        /// The Default Style Sheet used by the text objects.
        /// </summary>
        public TextStyleSheet defaultStyleSheet
        {
            get => m_DefaultStyleSheet;
            set => m_DefaultStyleSheet = value;
        }

        [FormerlySerializedAs("m_defaultStyleSheet")]
        [SerializeField]
        protected TextStyleSheet m_DefaultStyleSheet;


        /// <summary>
        /// The relative path to a Resources folder in the project where the text system will look to load color gradient presets.
        /// The default location is "Resources/Color Gradient Presets".
        /// </summary>
        public string defaultColorGradientPresetsPath
        {
            get => m_DefaultColorGradientPresetsPath;
            set => m_DefaultColorGradientPresetsPath = value;
        }

        [FormerlySerializedAs("m_defaultColorGradientPresetsPath")]
        [SerializeField]
        protected string m_DefaultColorGradientPresetsPath = "Text Color Gradients/";

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

        [FormerlySerializedAs("m_warningsDisabled")]
        [SerializeField]
        protected bool m_DisplayWarnings = false;

        // =============================================
        // Functions
        // =============================================

        void OnEnable()
        {
            lineBreakingRules.LoadLineBreakingRules();
            if (s_GlobalSpriteAsset == null)
                s_GlobalSpriteAsset = Resources.Load<SpriteAsset>("Sprite Assets/Default Sprite Asset");
        }

        void OnDestroy()
        {
            if (m_NativeTextSettings != IntPtr.Zero)
                DestroyNativeObject(m_NativeTextSettings, MarshalledUnityObject.MarshalNotNull(this));
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

                int id = fontRef.font.GetHashCode();

                if (!m_FontLookup.ContainsKey(id))
                    m_FontLookup.Add(id, fontRef.fontAsset);
            }
        }

        [System.Serializable]
        internal struct FontReferenceMap
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

        [SerializeField]
        internal List<FontReferenceMap> m_FontReferences = new List<FontReferenceMap>();

        [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
        internal FontAsset GetCachedFontAsset(Font font)
        {
            if (ReferenceEquals(font, null))
                return null;

            if (m_FontLookup == null)
            {
                m_FontLookup = new Dictionary<int, FontAsset>();
                InitializeFontReferenceLookup();
            }

            int id = font.GetHashCode();

            if (m_FontLookup.ContainsKey(id))
                return m_FontLookup[id];

            if (TextGenerator.IsExecutingJob)
                return null;

            FontAsset fontAsset = FontAssetFactory.ConvertFontToFontAsset(font);

            if (fontAsset != null)
            {
                m_FontReferences.Add(new FontReferenceMap(font, fontAsset));
                m_FontLookup.Add(id, fontAsset);
            }

            return fontAsset;
        }

        private List<FontAsset> GetOSFontAssetList()
        {
            var fonts = Font.GetOSFallbacks();
            return FontAsset.CreateFontAssetOSFallbackList(fonts);
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
        [NativeMethod(Name = "TextSettings::Create")]
        static extern IntPtr CreateNativeObject(IntPtr[] fallbacks, IntPtr managedObject);
        [NativeMethod(Name = "TextSettings::Destroy")]
        static extern void DestroyNativeObject(IntPtr m_NativeTextSettings, IntPtr managedObject);
        static extern void UpdateFallbacks(IntPtr ptr, IntPtr[] fallbacks);

        IntPtr m_NativeTextSettings = IntPtr.Zero;
        internal IntPtr nativeTextSettings
        {
            [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule")]
            get
            {
                UpdateNativeTextSettings();
                return m_NativeTextSettings;
            }
        }

        IntPtr[] GetGlobalFallbacks()
        {
            List<IntPtr> globalFontAssetFallbacks = new List<IntPtr>();
            fallbackFontAssets?.ForEach(fallback =>
            {
                if (fallback == null)
                    return;
                if (fallback.atlasPopulationMode == AtlasPopulationMode.Static && fallback.characterTable.Count > 0)
                {
                    Debug.LogWarning($"Advanced text system cannot use static font asset {fallback.name} as fallback.");
                    return;
                }
                globalFontAssetFallbacks.Add(fallback.nativeFontAsset);
            });

            emojiFallbackTextAssets?.ForEach(fallback =>
            {
                if (fallback is FontAsset fontAsset)
                {
                    if (fontAsset.atlasPopulationMode == AtlasPopulationMode.Static && fontAsset.characterTable.Count > 0)
                    {
                        Debug.LogWarning($"Advanced text system cannot use static font asset {fallback.name} as fallback.");
                        return;
                    }
                    globalFontAssetFallbacks.Add(fontAsset.nativeFontAsset);
                }
            });

            fallbackOSFontAssets?.ForEach(fallback =>
            {
                if (fallback == null)
                    return;
                if (fallback.atlasPopulationMode == AtlasPopulationMode.Static && fallback.characterTable.Count > 0)
                {
                    Debug.LogWarning($"Advanced text system cannot use static font asset {fallback.name} as fallback.");
                    return;
                }
                globalFontAssetFallbacks.Add(fallback.nativeFontAsset);
            });

            emojiFallbackTextAssets?.ForEach(fallback =>
            {
                // emojiFallbackTextAssets could contain both FontAsset and SpriteAsset
                if (fallback is FontAsset fontAsset)
                {
                    if (fontAsset == null)
                        return;
                    if (fontAsset.atlasPopulationMode == AtlasPopulationMode.Static && fontAsset.characterTable.Count > 0)
                    {
                        Debug.LogWarning($"Advanced text system cannot use static font asset {fallback.name} as fallback.");
                        return;
                    }
                    globalFontAssetFallbacks.Add(fontAsset.nativeFontAsset);
                }
            });

            return globalFontAssetFallbacks.ToArray();
        }

        bool m_IsNativeTextSettingsDirty = true;
        internal void SetNativeTextSettingsDirty()
        {
            m_IsNativeTextSettingsDirty = true;
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal void UpdateNativeTextSettings()
        {
            if (m_NativeTextSettings == IntPtr.Zero)
            {
                m_NativeTextSettings = CreateNativeObject(GetGlobalFallbacks(), MarshalledUnityObject.MarshalNotNull(this));
                m_IsNativeTextSettingsDirty = false;
            }
            else if (m_IsNativeTextSettingsDirty && m_NativeTextSettings != IntPtr.Zero)
            {
                UpdateFallbacks(m_NativeTextSettings, GetGlobalFallbacks());
                m_IsNativeTextSettingsDirty = false;
            }
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal FontAsset GetFontAsset()
        {
            if (defaultFontAsset != null)
                return defaultFontAsset;

            return GetDefaultFont();
        }
    }
}

#pragma warning restore CS0618
