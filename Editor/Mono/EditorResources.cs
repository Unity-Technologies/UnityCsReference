// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_EDITOR_RESOURCES // ONLY NEEDED BY STYLING DEVS AND DESIGNERS.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.StyleSheets;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.Internal;

namespace UnityEditor.Experimental
{
    internal class FontDef
    {
        class FontData
        {
            private string m_File;
            private Font m_LoadedFont;
            private string[] m_FontNames;

            public FontData(string file, string[] fontNames)
            {
                m_File = file;
                m_FontNames = fontNames;
            }

            public Font font
            {
                get
                {
                    if (m_LoadedFont == null)
                    {
                        m_LoadedFont = EditorGUIUtility.LoadRequired(m_File) as Font;
                        if (m_LoadedFont != null && m_FontNames != null)
                        {
                            m_LoadedFont.fontNames = m_FontNames;
                        }
                    }

                    return m_LoadedFont;
                }
            }
        }

        public const string k_Inter = "Inter";
        public const string k_LucidaGrande = "Lucida Grande";
        public const string k_Verdana = "Verdana";

        private Dictionary<Style, FontData> m_Fonts = new Dictionary<Style, FontData>();

        public enum Style
        {
            Normal = FontStyle.Normal,
            Bold = FontStyle.Bold,
            Italic = FontStyle.Italic,
            BoldAndItalic = FontStyle.BoldAndItalic,
            Small
        }

        public string name { get; set; }

        public FontDef(string name)
        {
            this.name = name;
        }

        public Font GetFont(Style style)
        {
            FontData data;

            if (m_Fonts.TryGetValue(style, out data))
            {
                return data.font;
            }

            if (style != Style.Normal)
                return GetFont(Style.Normal);
            return null;
        }

        public void SetFont(Style style, string file, string[] fontNames = null)
        {
            m_Fonts[style] = new FontData(file, fontNames);
        }

        public static FontDef CreateFromResources(string fontName, Dictionary<Style, string> fonts)
        {
            FontDef fontDef = new FontDef(fontName);

            foreach (var font in fonts)
            {
                fontDef.SetFont(font.Key, font.Value);
            }

            return fontDef;
        }

        public static FontDef CreateSystemFont(string fontName)
        {
            FontDef fontDef = new FontDef(fontName);

            fontDef.SetFont(Style.Small, "Fonts/System/System Small.ttf", new string[] {fontName});
            fontDef.SetFont(Style.Normal, "Fonts/System/System Normal.ttf", new string[] { fontName });
            fontDef.SetFont(Style.Bold, "Fonts/System/System Normal Bold.ttf", new string[] { fontName + " Bold"});
            return fontDef;
        }
    }

    [ExcludeFromDocs]
    public partial class EditorResources
    {
        private const string k_PrefsUserFontKey = "user_editor_font";
        const string k_GlobalStyleCatalogCacheFilePath = "Library/Style.catalog";

        private static StyleCatalog s_StyleCatalog;
        private static bool s_RefreshGlobalStyleCatalog = false;

        static class Constants
        {
            public static bool isDarkTheme => EditorGUIUtility.isProSkin;
        }

        // Global editor styles
        internal static StyleCatalog styleCatalog
        {
            get
            {
                if (s_StyleCatalog == null || s_RefreshGlobalStyleCatalog)
                    BuildCatalog();
                return s_StyleCatalog;
            }
        }

        private static bool CanEnableExtendedStyles()
        {
            var rootBlock = styleCatalog.GetStyle(StyleCatalogKeyword.root, StyleState.root);
            var styleExtensionDisabled = rootBlock.GetBool("-unity-disable-style-extensions");
            if (!styleExtensionDisabled)
                return true;
            return false;
        }

        private static bool IsEditorStyleSheet(string path)
        {
            if (!SearchUtils.EndsWith(path, ".uss"))
                return false;
            return path.IndexOf("/stylesheets/extensions/", StringComparison.OrdinalIgnoreCase) != -1 &&
                (SearchUtils.EndsWith(path, "common.uss") || SearchUtils.EndsWith(path, Constants.isDarkTheme ? "dark.uss" : "light.uss"));
        }

        internal static string GetDefaultFont()
        {
            // In languages other than english, we use 'lucida grande' because the localization settings are currently mapped to 'Lucida Grande'.
            // see Editor/Resources/Windows/fontsettings.text for instance
            if (LocalizationDatabase.currentEditorLanguage == SystemLanguage.English)
            {
                return FontDef.k_Inter;
            }
            else
            {
                return FontDef.k_LucidaGrande;
            }
        }

        internal static IEnumerable<string> supportedFontNames => EditorResources.supportedFonts.Keys;

        private static string s_CurrentFontName = null;

        internal static string currentFontName
        {
            get
            {
                if (s_CurrentFontName == null)
                {
                    if (LocalizationDatabase.currentEditorLanguage == SystemLanguage.English)
                    {
                        s_CurrentFontName = EditorPrefs.GetString(k_PrefsUserFontKey, GetDefaultFont());

                        // If the current is not available then fallback to the default font
                        if (!supportedFontNames.Contains(s_CurrentFontName))
                        {
                            s_CurrentFontName = GetDefaultFont();
                            EditorPrefs.DeleteKey(k_PrefsUserFontKey);
                        }
                    }
                    else
                    {
                        s_CurrentFontName = GetDefaultFont();
                    }
                }

                return s_CurrentFontName;
            }
        }

        internal static Font GetFont(FontDef.Style fontStyle)
        {
            var currentFontDef = supportedFonts[currentFontName];

            return currentFontDef.GetFont(fontStyle);
        }

        static void AddLucidaGrande()
        {
            s_SupportedFonts[FontDef.k_LucidaGrande] = FontDef.CreateFromResources(FontDef.k_LucidaGrande,
                new Dictionary<FontDef.Style, string>
                {
                    {FontDef.Style.Small, "Fonts/Lucida Grande small.ttf"},
                    {FontDef.Style.Normal, "Fonts/Lucida Grande.ttf"},
                    {FontDef.Style.Bold, "Fonts/Lucida Grande Bold.ttf"}
                });
        }

        private static Dictionary<string, FontDef> s_SupportedFonts = null;
        internal static Dictionary<string, FontDef> supportedFonts
        {
            get
            {
                if (s_SupportedFonts == null)
                {
                    s_SupportedFonts = new Dictionary<string, FontDef>();

                    // In languages other than english, we use 'lucida grande' because the localization settings are currently are mapped to 'lucida grande'.
                    // see Editor/Resources/Windows/fontsettings.text for instance
                    if (LocalizationDatabase.currentEditorLanguage != SystemLanguage.English)
                    {
                        AddLucidaGrande();
                    }
                    else
                    {
                        s_SupportedFonts[FontDef.k_Inter] = FontDef.CreateFromResources(FontDef.k_Inter,
                            new Dictionary<FontDef.Style, string>
                            {
                                {FontDef.Style.Small, "Fonts/Inter/Inter-Small.ttf"},
                                {FontDef.Style.Normal, "Fonts/Inter/Inter-Regular.ttf"},
                                {FontDef.Style.Bold, "Fonts/Inter/Inter-SemiBold.ttf"},
                                {FontDef.Style.Italic, "Fonts/Inter/Inter-Italic.ttf"},
                                {FontDef.Style.BoldAndItalic, "Fonts/Inter/Inter-SemiBoldItalic.ttf"}
                            });

                        if (Application.platform == RuntimePlatform.WindowsEditor)
                        {
                            s_SupportedFonts[FontDef.k_Verdana] = FontDef.CreateSystemFont(FontDef.k_Verdana);
                        }
                        else
                        {
                            AddLucidaGrande();
                        }
                    }
                }

                return s_SupportedFonts;
            }
        }

        private static List<string> GetDefaultStyleCatalogPaths()
        {
            bool useDarkTheme = Constants.isDarkTheme;
            var catalogFiles = new List<string>
            {
                "StyleSheets/Extensions/base/common.uss",
                "UIPackageResources/StyleSheets/Default/Variables/Public/common.uss",
                "StyleSheets/Northstar/common.uss"
            };

            if (LocalizationDatabase.currentEditorLanguage == SystemLanguage.English)
            {
                string currentFontStyleSheet = "StyleSheets/Extensions/fonts/" + currentFontName.ToLower() + ".uss";

                catalogFiles.Add(currentFontStyleSheet);
            }

            catalogFiles.Add(useDarkTheme ? "StyleSheets/Extensions/base/dark.uss" : "StyleSheets/Extensions/base/light.uss");
            catalogFiles.Add(useDarkTheme ? "UIPackageResources/StyleSheets/Default/Northstar/Palette/dark.uss" : "UIPackageResources/StyleSheets/Default/Northstar/Palette/light.uss");
            return catalogFiles;
        }

        static string ComputeCatalogHash(List<string> paths)
        {
            var hash = $"__StyleCatalog_Hash_{Application.unityVersion}_";
            foreach (var path in paths)
            {
                hash += path.GetHashCode().ToString("X2");
                var fi = new FileInfo(path);
                if (fi.Exists)
                    hash += fi.Length.ToString("X2");
            }
            return hash;
        }

        static void SaveCatalogToDisk(StyleCatalog catalog, string hash, string savePath)
        {
            using (var stream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(hash);
                catalog.Save(writer);
            }
        }

        internal static void BuildCatalog()
        {
            using (new EditorPerformanceTracker(nameof(BuildCatalog)))
            {
                s_StyleCatalog = new StyleCatalog();

                bool rebuildCatalog = true;
                List<string> paths = new List<string>();
                string catalogHash = "";

                if (!EditorApplication.isBuildingAnyResources)
                {
                    paths = GetDefaultStyleCatalogPaths();
                    foreach (var editorUssPath in AssetDatabase.FindAssets("t:StyleSheet")
                             .Select(AssetDatabase.GUIDToAssetPath).Where(IsEditorStyleSheet))
                        paths.Add(editorUssPath);

                    var forceRebuild = s_RefreshGlobalStyleCatalog;
                    s_RefreshGlobalStyleCatalog = false;

                    catalogHash = ComputeCatalogHash(paths);
                    if (!forceRebuild && File.Exists(k_GlobalStyleCatalogCacheFilePath))
                    {
                        using (var cacheCatalogStream = new FileStream(k_GlobalStyleCatalogCacheFilePath, FileMode.Open,
                            FileAccess.Read, FileShare.Read))
                        {
                            using (var ccReader = new BinaryReader(cacheCatalogStream))
                            {
                                string cacheHash = ccReader.ReadString();
                                if (cacheHash == catalogHash)
                                    rebuildCatalog = !styleCatalog.Load(ccReader);
                            }
                        }
                    }
                }

                if (rebuildCatalog)
                {
                    Console.WriteLine($"Loading style catalogs ({paths.Count})\r\n\t{String.Join("\r\n\t", paths.ToArray())}");

                    styleCatalog.Load(paths);

                    if (paths.Count != 0)
                    {
                        SaveCatalogToDisk(styleCatalog, catalogHash, k_GlobalStyleCatalogCacheFilePath);
                    }
                }
            }
        }

        internal static void RefreshSkin()
        {
            using (new EditorPerformanceTracker(nameof(RefreshSkin)))
            {
                if (!CanEnableExtendedStyles())
                    return;

                GUIStyle.onDraw = StylePainter.DrawStyle;

                // Update gui skin style layouts
                var skin = GUIUtility.GetDefaultSkin();
                if (skin != null)
                {
                    // TODO: Emit OnStyleCatalogLoaded
                    if (Path.GetFileName(Path.GetDirectoryName(Application.dataPath)) == "editor_resources")
                        ConverterUtils.ResetSkinToPristine(skin, Constants.isDarkTheme ? SkinTarget.Dark : SkinTarget.Light);
                    skin.font = GetFont(FontDef.Style.Normal);
                    UpdateGUIStyleProperties(skin);
                }
            }
        }

        internal static void UpdateGUIStyleProperties(GUISkin skin)
        {
            LocalizedEditorFontManager.LocalizeEditorFonts();

            skin.ForEachGUIStyleProperty(UpdateGUIStyleProperties);
            foreach (var style in skin.customStyles)
                UpdateGUIStyleProperties(style.name, style);
        }

        internal static List<GUIStyle> GetExtendedStylesFromSkin(GUISkin skin)
        {
            var extendedStyles = new List<GUIStyle>();
            Action<string, GUIStyle> appendExtendedBlock = (name, style) =>
            {
                var sname = GUIStyleExtensions.StyleNameToBlockName(style.name, false);
                if (styleCatalog.GetStyle(sname).IsValid())
                {
                    extendedStyles.Add(style);
                }
            };

            skin.ForEachGUIStyleProperty(appendExtendedBlock);
            foreach (var style in skin.customStyles)
                appendExtendedBlock(style.name, style);

            return extendedStyles;
        }


        private static void UpdateGUIStyleProperties(string name, GUIStyle style)
        {
            var sname = GUIStyleExtensions.StyleNameToBlockName(style.name, false);
            var block = styleCatalog.GetStyle(sname);
            if (!block.IsValid())
                return;

            try
            {
                GUIStyleExtensions.PopulateStyle(styleCatalog, style, sname);
                GUIStyleExtensions.ConvertToExtendedStyle(style);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse extended style properties for {sname}\n{ex.Message}");
            }
        }

        // Returns the editor resources absolute file system path.
        public static string dataPath => Application.dataPath;

        // Resolve an editor resource asset path.
        public static string ExpandPath(string path)
        {
            return path.Replace("\\", "/");
        }

        // Returns the full file system path of an editor resource asset path.
        public static string GetFullPath(string path)
        {
            if (File.Exists(path))
                return path;
            return new FileInfo(ExpandPath(path)).FullName;
        }

        // Checks if an editor resource asset path exists.
        public static bool Exists(string path)
        {
            if (File.Exists(path))
                return true;
            return File.Exists(ExpandPath(path));
        }

        // Loads an editor resource asset.
        public static T Load<T>(string assetPath, bool isRequired = true) where T : UnityEngine.Object
        {
            var obj = Load(assetPath, typeof(T));
            if (!obj && isRequired)
                throw new FileNotFoundException("Could not find editor resource " + assetPath);
            return obj as T;
        }

        // Returns a globally defined style element by name.
        internal static StyleBlock GetStyle(string selectorName, params StyleState[] states) { return styleCatalog.GetStyle(selectorName, states); }
        internal static StyleBlock GetStyle(int selectorKey, params StyleState[] states) { return styleCatalog.GetStyle(selectorKey, states); }

        // Loads an USS asset into the global style catalog
        internal static void LoadStyles(string ussPath)
        {
            styleCatalog.Load(ussPath);
        }

        // Creates a new style catalog from a set of USS assets.
        internal static StyleCatalog LoadCatalog(string[] ussPaths)
        {
            var catalog = new StyleCatalog();
            catalog.Load(ussPaths);
            return catalog;
        }

        [UsedImplicitly]
        private class StyleCatalogPostProcessor : AssetPostprocessor
        {
            [UsedImplicitly]
            static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                if (styleCatalog == null)
                    return;

                if (importedAssets.Concat(deletedAssets).Any(path => IsEditorStyleSheet(path)))
                    s_RefreshGlobalStyleCatalog = true;
            }
        }
    }
}
