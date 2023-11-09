// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEditor.Experimental;

namespace UnityEditor
{

    /// <summary>
    /// Represents text rendering settings for the editor
    /// </summary>
    [InitializeOnLoad]
    internal class EditorTextSettings : TextSettings
    {
        private static EditorTextSettings s_DefaultTextSettings;
        private static float s_CurrentEditorSharpness;
        private static bool s_CurrentEditorSharpnessLoadedOrSet = false;

        const string k_Platform =
                            " - Linux";

        static EditorTextSettings()
        {
            IMGUITextHandle.GetEditorTextSettings = () => defaultTextSettings;
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
                        UpdateLocalizationFontAsset();
                }

                return s_DefaultTextSettings;
            }
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

            defaultTextSettings.fallbackFontAssets[0] = localizationAsset;
            defaultTextSettings.fallbackFontAssets[defaultTextSettings.fallbackFontAssets.Count - 1] = globalFallbackAsset;
        }

        internal static readonly string s_DefaultEditorTextSettingPath = "UIPackageResources/Editor Text Settings.asset";
    }
}
