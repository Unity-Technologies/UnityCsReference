// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents text rendering settings for a specific UI panel.
    /// <seealso cref="PanelSettings.textSettings"/>
    /// </summary>
    public class PanelTextSettings : TextSettings
    {
        private static PanelTextSettings s_DefaultPanelTextSettings;

        internal static PanelTextSettings defaultPanelTextSettings
        {
            get
            {
                if (s_DefaultPanelTextSettings == null)
                {
                    s_DefaultPanelTextSettings = EditorGUIUtilityLoad(s_DefaultEditorPanelTextSettingPath) as PanelTextSettings;
                    UpdateLocalizationFontAsset();

                    if (s_DefaultPanelTextSettings == null)
                        s_DefaultPanelTextSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
                }

                return s_DefaultPanelTextSettings;
            }
        }

        internal static void UpdateLocalizationFontAsset()
        {
            string platform = " - Linux";
            var localizationAssetPathPerSystemLanguage = new Dictionary<SystemLanguage, string>()
            {
                { SystemLanguage.English, Path.Combine(UIElementsPackageUtility.EditorResourcesBasePath, $"UIPackageResources/FontAssets/DynamicOSFontAssets/Localization/English{platform}.asset") },
                { SystemLanguage.Japanese, Path.Combine(UIElementsPackageUtility.EditorResourcesBasePath, $"UIPackageResources/FontAssets/DynamicOSFontAssets/Localization/Japanese{platform}.asset") },
                { SystemLanguage.ChineseSimplified, Path.Combine(UIElementsPackageUtility.EditorResourcesBasePath, $"UIPackageResources/FontAssets/DynamicOSFontAssets/Localization/ChineseSimplified{platform}.asset") },
                { SystemLanguage.ChineseTraditional, Path.Combine(UIElementsPackageUtility.EditorResourcesBasePath, $"UIPackageResources/FontAssets/DynamicOSFontAssets/Localization/ChineseTraditional{platform}.asset") },
                { SystemLanguage.Korean, Path.Combine(UIElementsPackageUtility.EditorResourcesBasePath, $"UIPackageResources/FontAssets/DynamicOSFontAssets/Localization/Korean{platform}.asset") }
            };

            var globalFallbackAssetPath = Path.Combine(UIElementsPackageUtility.EditorResourcesBasePath, $"UIPackageResources/FontAssets/DynamicOSFontAssets/GlobalFallback/GlobalFallback{platform}.asset");

            var localizationAsset = EditorGUIUtilityLoad(localizationAssetPathPerSystemLanguage[GetCurrentLanguage()]) as FontAsset;
            var globalFallbackAsset = EditorGUIUtilityLoad(globalFallbackAssetPath) as FontAsset;

            defaultPanelTextSettings.fallbackFontAssets[0] = localizationAsset;
            defaultPanelTextSettings.fallbackFontAssets[defaultPanelTextSettings.fallbackFontAssets.Count - 1] = globalFallbackAsset;
        }

        internal FontAsset GetCachedFontAsset(Font font)
        {
            return GetCachedFontAssetInternal(font);
        }

        internal static readonly string s_DefaultEditorPanelTextSettingPath =
            Path.Combine(UIElementsPackageUtility.EditorResourcesBasePath, "UIPackageResources/Default Editor Text Settings.asset");

        internal static Func<string, Object> EditorGUIUtilityLoad;
        internal static Func<SystemLanguage> GetCurrentLanguage;
    }
}
