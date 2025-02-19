// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEditor.Experimental;
using UnityEngine.Assertions;
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

        [SerializeField]
        public static EditorTextRenderingMode currentEditorTextRenderingMode;

        static EditorTextSettings()
        {
            TextGenerationSettings.IsEditorTextRenderingModeBitmap = () => currentEditorTextRenderingMode == EditorTextRenderingMode.Bitmap;
            IMGUITextHandle.GetEditorTextSettings = () => defaultTextSettings;
            IMGUITextHandle.GetBlurryFontAssetMapping = GetBlurryFontAssetMapping;
            UITKTextHandle.GetBlurryMapping = GetBlurryFontAssetMapping;
            UITKTextHandle.CanGenerateFallbackFontAssets = CanGenerateFallbackFontAssets;
        }

        public static FontAsset GetBlurryFontAssetMapping(int pointSize, FontAsset ft)
        {
            if (pointSize < k_MinSupportedPointSize || pointSize >= k_MaxSupportedPointSize * k_MaxSupportedScaledPointSize)
                return ft;

            s_DefaultTextSettings.blurryTextCaching.InitializeLookups();
            FontAsset fa = s_DefaultTextSettings.blurryTextCaching.Find(ft, pointSize);
            bool isMainThread = !JobsUtility.IsExecutingJob;
            if (fa == null && isMainThread)
            {
                fa = FontAssetFactory.CloneFontAssetWithBitmapRendering(ft, pointSize);
                s_DefaultTextSettings.blurryTextCaching.Add(ft, pointSize, fa);
            }

            return fa;
        }

        void OnEnable()
        {
            currentEditorTextRenderingMode = (EditorTextRenderingMode)EditorPrefs.GetInt("EditorTextRenderingMode", (int)EditorTextRenderingMode.SDF);
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
                        GUISkin.m_SkinChanged += UpdateDefaultTextStyleSheet;
                    }
                }

                return s_DefaultTextSettings;
            }
        }

        const int k_MinSupportedPointSize = 5;
        const int k_MaxSupportedPointSize = 19;
        const int k_MaxSupportedScaledPointSize = 3;
        internal override List<FontAsset> GetFallbackFontAssets(int textPixelSize = -1)
        {
            if (currentEditorTextRenderingMode != EditorTextRenderingMode.Bitmap || textPixelSize < k_MinSupportedPointSize || textPixelSize >= k_MaxSupportedPointSize * k_MaxSupportedScaledPointSize)
                return fallbackFontAssets;

            return GetFallbackFontAssetsInternal(textPixelSize);
        }

        static List<FontAsset> GetFallbackFontAssetsInternal(int textPixelSize)
        {
            var editorTextSettings = defaultTextSettings;
            var localFallback = editorTextSettings.fallbackFontAssets[0];
            var globalFallback = editorTextSettings.fallbackFontAssets[1];

            var localBitmapFallback = editorTextSettings.blurryTextCaching.Find(localFallback, textPixelSize);
            var globalBitmapFallback = editorTextSettings.blurryTextCaching.Find(globalFallback, textPixelSize);

            if (localBitmapFallback == null)
            {
                localBitmapFallback = FontAssetFactory.CloneFontAssetWithBitmapRendering(localFallback, textPixelSize);
                editorTextSettings.blurryTextCaching.Add(localFallback, textPixelSize, localBitmapFallback);
            }

            if (globalBitmapFallback == null)
            {
                globalBitmapFallback = FontAssetFactory.CloneFontAssetWithBitmapRendering(globalFallback, textPixelSize);
                editorTextSettings.blurryTextCaching.Add(globalFallback, textPixelSize, globalBitmapFallback);
            }

            return new List<FontAsset>()
            {
                localBitmapFallback,
                globalBitmapFallback
            };
        }

        static bool CanGenerateFallbackFontAssets(int textPixelSize)
        {
            if (textPixelSize < k_MinSupportedPointSize || textPixelSize >= k_MaxSupportedPointSize * k_MaxSupportedScaledPointSize)
                return true;

            bool isMainThread = !JobsUtility.IsExecutingJob;
            var editorTextSettings = defaultTextSettings;
            var localFallback = editorTextSettings.fallbackFontAssets[0];
            var globalFallback = editorTextSettings.fallbackFontAssets[1];

            localFallback = editorTextSettings.blurryTextCaching.Find(localFallback, textPixelSize);
            globalFallback = editorTextSettings.blurryTextCaching.Find(globalFallback, textPixelSize);

            if (localFallback == null || globalFallback == null)
            {
                if (!isMainThread)
                    return false;

                GetFallbackFontAssetsInternal(textPixelSize);
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

}
