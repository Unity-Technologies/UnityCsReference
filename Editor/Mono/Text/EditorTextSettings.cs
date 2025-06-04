// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEditor.Experimental;
using UnityEngine.UIElements;
using TextGenerationSettings = UnityEngine.TextCore.Text.TextGenerationSettings;

namespace UnityEditor
{
    /// <summary>
    /// Represents text rendering settings for the editor
    /// </summary>
    [InitializeOnLoad]
    internal class EditorTextSettings : TextSettings
    {
        static EditorTextSettings s_DefaultTextSettings;
        static float s_CurrentEditorSharpness;
        static bool s_CurrentEditorSharpnessLoadedOrSet;

        const string k_DefaultEmojisFallback = "UIPackageResources/FontAssets/Emojis/";
        const string k_Platform =
                            " - Linux";

        [SerializeField]
        public BlurryTextCaching blurryTextCaching;

        internal static EditorTextRenderingMode currentEditorTextRenderingMode { get; private set; }
        private static bool isEditorTextRenderingModeRaster;

        static EditorTextSettings()
        {
            TextGenerationSettings.IsEditorTextRenderingModeBitmap = () => currentEditorTextRenderingMode == EditorTextRenderingMode.Bitmap;
            TextGenerationSettings.IsEditorTextRenderingModeRaster = () => isEditorTextRenderingModeRaster;
            IMGUITextHandle.GetEditorTextSettings = () => defaultTextSettings;
            IMGUITextHandle.GetBlurryFontAssetMapping = GetBlurryFontAssetMapping;
            UITKTextHandle.GetBlurryFontAssetMapping = GetBlurryFontAssetMapping;
            UITKTextHandle.CanGenerateFallbackFontAssets = CanGenerateFallbackFontAssets;
        }

        internal static void SetEditorTextRenderingMode(EditorTextRenderingMode textRenderingMode)
        {
            currentEditorTextRenderingMode = textRenderingMode;
            if (currentEditorTextRenderingMode == EditorTextRenderingMode.Bitmap)
            {
                // Inter does not support raster bitmap
                isEditorTextRenderingModeRaster = !Font.IsFontSmoothingEnabled() && EditorResources.currentFontName != FontDef.k_Inter;
            }
        }

        public static FontAsset GetBlurryFontAssetMapping(int pointSize, FontAsset ft, bool isRaster)
        {
            if (pointSize < k_MinSupportedPointSize || pointSize >= k_MaxSupportedPointSize * k_MaxSupportedScaledPointSize)
                return ft;

            s_DefaultTextSettings.blurryTextCaching.InitializeLookups();
            FontAsset fa = s_DefaultTextSettings.blurryTextCaching.Find(ft, pointSize, isRaster);
            bool isMainThread = !JobsUtility.IsExecutingJob;
            if (fa == null && isMainThread)
            {
                fa = FontAssetFactory.CloneFontAssetWithBitmapRendering(ft, pointSize, isRaster);
                s_DefaultTextSettings.blurryTextCaching.Add(ft, pointSize, isRaster, fa);
            }

            return fa;
        }

        void OnEnable()
        {
            SetEditorTextRenderingMode((EditorTextRenderingMode)EditorPrefs.GetInt("EditorTextRenderingMode", (int)EditorTextRenderingMode.SDF));
        }

        internal static void SetCurrentEditorSharpness(float sharpness)
        {
            s_CurrentEditorSharpness = sharpness;
            s_CurrentEditorSharpnessLoadedOrSet = true;
        }

        internal override float GetEditorTextSharpness()
        {
            if (!s_CurrentEditorSharpnessLoadedOrSet)
            {
                SetCurrentEditorSharpness(EditorPrefs.GetFloat($"EditorTextSharpness_{EditorResources.GetFont(FontDef.Style.Normal).name}", 0.0f));
            }
            return s_CurrentEditorSharpness;
        }

        internal override Font GetEditorFont()
        {
            return EditorResources.GetFont(FontDef.Style.Normal);
        }

        internal static EditorTextSettings defaultTextSettings
        {
            get
            {
                if (s_DefaultTextSettings == null)
                {
                    s_DefaultTextSettings = EditorGUIUtility.Load(s_DefaultEditorTextSettingPath) as EditorTextSettings;
                    if (s_DefaultTextSettings)
                    {
                        UpdateLocalizationFontAsset();
                        UpdateDefaultTextStyleSheet();
                        s_DefaultTextSettings.CreateDefaultEditorFontAsset();
                        GUISkin.m_SkinChanged += UpdateDefaultTextStyleSheet;
                    }
                }

                return s_DefaultTextSettings;
            }
        }

    void CreateDefaultEditorFontAsset()
    {
        if (m_FontLookup == null)
        {
            m_FontLookup = new Dictionary<int, FontAsset>();
            InitializeFontReferenceLookup();
        }

        // The default Editor Font Asset have already been created.
        if (m_FontReferences.Count > 0)
            return;

        var systemFontConfig = EditorFontAssetFactory.FontFamilyConfig.k_SystemFontConfig;
        FontAsset systemRegularFontAsset = EditorFontAssetFactory.CreateFontFamilyAssets(systemFontConfig);
        EditorFontAssetFactory.RegisterFontWithPaths(m_FontReferences, m_FontLookup, systemRegularFontAsset,
            new[]
            {
                FontPaths.System.Normal,
                FontPaths.System.Big,
                FontPaths.System.Small,
                FontPaths.System.Warning
            });

        FontAsset systemBoldFontAsset = EditorFontAssetFactory.CreateFontAsset(EditorFontAssetFactory.SystemFontName, "Bold", 90);
        EditorFontAssetFactory.RegisterFontWithPaths(m_FontReferences, m_FontLookup, systemBoldFontAsset,
            new[]
            {
                FontPaths.System.NormalBold,
                FontPaths.System.SmallBold
            });

        var interFontConfig = EditorFontAssetFactory.FontFamilyConfig.k_InterFontConfig;
        FontAsset interRegularFontAsset = EditorFontAssetFactory.CreateFontFamilyAssets(interFontConfig);
        EditorFontAssetFactory.RegisterFontWithPaths(m_FontReferences, m_FontLookup, interRegularFontAsset, new[] { FontPaths.Inter.Regular });
    }

        const int k_MinSupportedPointSize = 5;
        const int k_MaxSupportedPointSize = 19;
        const int k_MaxSupportedScaledPointSize = 3;
        internal override List<FontAsset> GetFallbackFontAssets(bool isRaster, int textPixelSize = -1)
        {
            if (currentEditorTextRenderingMode != EditorTextRenderingMode.Bitmap || textPixelSize < k_MinSupportedPointSize || textPixelSize >= k_MaxSupportedPointSize * k_MaxSupportedScaledPointSize)
                return fallbackFontAssets;

            return GetFallbackFontAssetsInternal(textPixelSize, isRaster);
        }

        static List<FontAsset> GetFallbackFontAssetsInternal(int textPixelSize, bool isRaster)
        {
            var editorTextSettings = defaultTextSettings;
            var localFallback = editorTextSettings.fallbackFontAssets[0];
            var globalFallback = editorTextSettings.fallbackFontAssets[1];

            var localBitmapFallback = editorTextSettings.blurryTextCaching.Find(localFallback, textPixelSize, isRaster);
            var globalBitmapFallback = editorTextSettings.blurryTextCaching.Find(globalFallback, textPixelSize, isRaster);

            if (localBitmapFallback == null)
            {
                localBitmapFallback = FontAssetFactory.CloneFontAssetWithBitmapRendering(localFallback, textPixelSize, isRaster);
                editorTextSettings.blurryTextCaching.Add(localFallback, textPixelSize, isRaster, localBitmapFallback);
            }

            if (globalBitmapFallback == null)
            {
                globalBitmapFallback = FontAssetFactory.CloneFontAssetWithBitmapRendering(globalFallback, textPixelSize, isRaster);
                editorTextSettings.blurryTextCaching.Add(globalFallback, textPixelSize, isRaster, globalBitmapFallback);
            }

            return new List<FontAsset>()
            {
                localBitmapFallback,
                globalBitmapFallback
            };
        }

        static bool CanGenerateFallbackFontAssets(int textPixelSize, bool isRaster)
        {
            if (textPixelSize < k_MinSupportedPointSize || textPixelSize >= k_MaxSupportedPointSize * k_MaxSupportedScaledPointSize)
                return true;

            bool isMainThread = !JobsUtility.IsExecutingJob;
            var editorTextSettings = defaultTextSettings;
            var localFallback = editorTextSettings.fallbackFontAssets[0];
            var globalFallback = editorTextSettings.fallbackFontAssets[1];

            localFallback = editorTextSettings.blurryTextCaching.Find(localFallback, textPixelSize, isRaster);
            globalFallback = editorTextSettings.blurryTextCaching.Find(globalFallback, textPixelSize, isRaster);

            if (localFallback == null || globalFallback == null)
            {
                if (!isMainThread)
                    return false;

                GetFallbackFontAssetsInternal(textPixelSize, isRaster);
            }

            return true;
        }

        internal static void UpdateLocalizationFontAsset()
        {
            var localizationAssetPathPerSystemLanguage = new Dictionary<SystemLanguage, string>()
                    {
                        { SystemLanguage.English, $"UIPackageResources/FontAssets/DynamicOSFontAssets/Localization/English{k_Platform}.asset" },
                        { SystemLanguage.Japanese, $"UIPackageResources/FontAssets/DynamicOSFontAssets/Localization/Japanese{k_Platform}.asset" },
                        { SystemLanguage.ChineseSimplified, $"UIPackageResources/FontAssets/DynamicOSFontAssets/Localization/ChineseSimplified{k_Platform}.asset" },
                        { SystemLanguage.ChineseTraditional, $"UIPackageResources/FontAssets/DynamicOSFontAssets/Localization/ChineseTraditional{k_Platform}.asset" },
                        { SystemLanguage.Korean, $"UIPackageResources/FontAssets/DynamicOSFontAssets/Localization/Korean{k_Platform}.asset" }
                    };

            var globalFallbackAssetPath = $"UIPackageResources/FontAssets/DynamicOSFontAssets/GlobalFallback/GlobalFallback{k_Platform}.asset";

            FontAsset localizationAsset = null;

            if (localizationAssetPathPerSystemLanguage.ContainsKey(LocalizationDatabase.currentEditorLanguage))
            {
                localizationAsset = EditorGUIUtility.Load(localizationAssetPathPerSystemLanguage[LocalizationDatabase.currentEditorLanguage]) as FontAsset;
            }

            var globalFallbackAsset = EditorGUIUtility.Load(globalFallbackAssetPath) as FontAsset;

            defaultTextSettings.fallbackFontAssets.Clear();
            defaultTextSettings.fallbackFontAssets.Add(localizationAsset);
            defaultTextSettings.fallbackFontAssets.Add(globalFallbackAsset);

            var emojiFallbackPath = $"{k_DefaultEmojisFallback}Emojis{k_Platform}.asset";
            var emojiFallback = EditorGUIUtility.Load(emojiFallbackPath) as FontAsset;
            var emojiFallbackList = defaultTextSettings.emojiFallbackTextAssets;
            if (emojiFallback != null && !emojiFallbackList.Contains(emojiFallback))
                defaultTextSettings.emojiFallbackTextAssets.Add(emojiFallback);
        }

        internal static void UpdateDefaultTextStyleSheet()
        {
            s_DefaultTextSettings.defaultStyleSheet = EditorGUIUtility.Load(EditorGUIUtility.skinIndex == EditorResources.darkSkinIndex ? s_DarkEditorTextStyleSheetPath : s_LightEditorTextStyleSheetPath) as TextStyleSheet;
        }

        internal static readonly string s_DefaultEditorTextSettingPath = "UIPackageResources/Editor Text Settings.asset";
        internal static readonly string s_DarkEditorTextStyleSheetPath = "UIPackageResources/Dark Editor Text StyleSheet.asset";
        internal static readonly string s_LightEditorTextStyleSheetPath = "UIPackageResources/Light Editor Text StyleSheet.asset";
    }

    internal static class FontPaths
    {
        public static class System
        {
            public const string Big = "Fonts/System/System Big.ttf";
            public const string Normal = "Fonts/System/System Normal.ttf";
            public const string NormalBold = "Fonts/System/System Normal Bold.ttf";
            public const string Small = "Fonts/System/System Small.ttf";
            public const string SmallBold = "Fonts/System/System Small Bold.ttf";
            public const string Warning = "Fonts/System/System Warning.ttf";
        }

        public static class Inter
        {
            public const string Regular = "Fonts/Inter/Inter-Regular.ttf";
        }
    }
}
