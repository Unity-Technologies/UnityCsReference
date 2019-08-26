// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental
{
    [ExcludeFromDocs]
    public partial class EditorResources
    {
        private static StyleCatalog s_StyleCatalog;
        private static bool s_RefreshGlobalStyleCatalog = false;
        private static Dictionary<string, string> s_BuiltInFonts = null;

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
            var pathLowerCased = path.ToLower();
            return pathLowerCased.Contains("/stylesheets/extensions/") && (pathLowerCased.EndsWith("common.uss") ||
                pathLowerCased.EndsWith(EditorGUIUtility.isProSkin ? "dark.uss" : "light.uss"));
        }

        internal static string GetDefaultFont()
        {
            if (Application.platform == RuntimePlatform.WindowsEditor)
                return "Verdana";
            else
                return "Roboto";
        }

        internal static string GetCurrentFont()
        {
            var currentFont = EditorPrefs.GetString("user_editor_font", GetDefaultFont());

            // If the current is not available then fallback to the default font
            if (!GetSupportedFonts().Contains(currentFont))
            {
                currentFont = GetDefaultFont();
                EditorPrefs.DeleteKey("user_editor_font");
            }

            return currentFont;
        }

        private static Font s_SmallFont;
        internal static Font GetSmallFont()
        {
            if (s_SmallFont == null)
            {
                var currentFont = GetCurrentFont();

                if (IsSystemFont(currentFont))
                {
                    s_SmallFont = EditorGUIUtility.LoadRequired("Fonts/System/System Small.ttf") as Font;
                    s_SmallFont.fontNames = new[] { currentFont };
                }
                else
                {
                    if (currentFont == "Roboto")
                    {
                        s_SmallFont = EditorGUIUtility.LoadRequired("Fonts/roboto/Roboto-Small.ttf") as Font;
                    }
                    else if (currentFont == "Lucida Grande")
                    {
                        s_SmallFont = EditorGUIUtility.LoadRequired("Fonts/Lucida Grande small.ttf") as Font;
                    }
                    else
                    {
                        s_SmallFont = GetNormalFont();
                    }
                }
            }

            return s_SmallFont;
        }

        private static Font s_NormalFont;
        internal static Font GetNormalFont()
        {
            if (s_NormalFont == null)
            {
                var currentFont = GetCurrentFont();

                if (IsSystemFont(currentFont))
                {
                    s_NormalFont = EditorGUIUtility.LoadRequired("Fonts/System/System Normal.ttf") as Font;
                    s_NormalFont.fontNames = new[] { currentFont };
                }
                else
                {
                    s_NormalFont = EditorGUIUtility.LoadRequired(builtInFonts[currentFont]) as Font;
                }
            }

            return s_NormalFont;
        }

        private static Font s_BoldFont;
        internal static Font GetBoldFont()
        {
            if (s_BoldFont == null)
            {
                var currentFont = GetCurrentFont();

                if (IsSystemFont(currentFont))
                {
                    s_BoldFont = EditorGUIUtility.LoadRequired("Fonts/System/System Normal Bold.ttf") as Font;
                    s_BoldFont.fontNames = new[] { currentFont + " Bold" };
                }
                else
                {
                    if (currentFont == "Roboto")
                    {
                        s_BoldFont = EditorGUIUtility.LoadRequired("Fonts/roboto/Roboto-Bold.ttf") as Font;
                    }
                    else if (currentFont == "Lucida Grande")
                    {
                        s_BoldFont = EditorGUIUtility.LoadRequired("Fonts/Lucida Grande Bold.ttf") as Font;
                    }
                    else
                    {
                        s_BoldFont = EditorGUIUtility.LoadRequired(builtInFonts[currentFont]) as Font;
                    }
                }
            }

            return s_BoldFont;
        }

        private static List<string> s_SupportedFonts = null;

        internal static List<string> GetSupportedFonts()
        {
            if (s_SupportedFonts == null)
            {
                s_SupportedFonts = new List<string>();

                if (Application.platform == RuntimePlatform.WindowsEditor)
                {
                    s_SupportedFonts.Add("Segoe UI");
                }

                foreach (var builtinFont in EditorResources.builtInFonts.Keys)
                {
                    s_SupportedFonts.Add(builtinFont);
                }

                if (!s_SupportedFonts.Contains(EditorResources.GetDefaultFont()))
                    s_SupportedFonts.Add(EditorResources.GetDefaultFont());
            }

            return s_SupportedFonts;
        }

        internal static Dictionary<string, string> builtInFonts
        {
            get
            {
                if (s_BuiltInFonts == null)
                {
                    s_BuiltInFonts = new Dictionary<string, string>
                    {
                        ["Roboto"] = "Fonts/roboto/Roboto-Regular.ttf"
                    };

                    if (Application.platform != RuntimePlatform.WindowsEditor)
                    {
                        s_BuiltInFonts["Lucida Grande"] = "Fonts/Lucida Grande.ttf";
                    }
                }

                return s_BuiltInFonts;
            }
        }

        internal static bool IsSystemFont(string font)
        {
            // TODO: Can be better
            if (builtInFonts.ContainsKey(font))
                return false;
            return true;
        }

        private static List<string> GetDefaultStyleCatalogPaths()
        {
            bool useDarkTheme = EditorGUIUtility.isProSkin;
            var catalogFiles = new List<string>
            {
                "StyleSheets/Extensions/base/common.uss",
                "StyleSheets/Northstar/common.uss"
            };

            if (LocalizationDatabase.currentEditorLanguage == SystemLanguage.English)
            {
                string currentFontStyleSheet = "StyleSheets/Extensions/fonts/" + GetCurrentFont().ToLower() + ".uss";

                catalogFiles.Add(currentFontStyleSheet);
            }

            catalogFiles.Add(useDarkTheme ? "StyleSheets/Extensions/base/dark.uss" : "StyleSheets/Extensions/base/light.uss");

            return catalogFiles;
        }

        [UsedImplicitly, RequiredByNativeCode]
        internal static void BuildCatalog()
        {
            s_StyleCatalog = new StyleCatalog();
            s_RefreshGlobalStyleCatalog = false;

            var paths = GetDefaultStyleCatalogPaths();
            foreach (var editorUssPath in AssetDatabase.GetAllAssetPaths().Where(IsEditorStyleSheet))
                paths.Add(editorUssPath);

            Console.WriteLine($"Building style catalogs ({paths.Count})\r\n\t{String.Join("\r\n\t", paths.ToArray())}");
            styleCatalog.Load(paths);
        }

        internal static void RefreshSkin()
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
                    ConverterUtils.ResetSkinToPristine(skin, EditorGUIUtility.isProSkin ? SkinTarget.Dark : SkinTarget.Light);
                skin.font = GetNormalFont();
                UpdateGUIStyleProperties(skin);
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

        private static void ResetDeprecatedBackgroundImage(GUIStyleState state)
        {
            state.background = null;
            if (state.scaledBackgrounds != null && state.scaledBackgrounds.Length >= 1)
                state.scaledBackgrounds[0] = null;
        }

        /* ONLY NEEDED BY STYLING DEVS AND DESIGNERS.
        [MenuItem("Theme/Refresh Styles &r", priority = 420)]
        internal static void RefreshStyles()
        {
            Unsupported.ClearSkinCache();
            EditorUtility.RequestScriptReload();
            InternalEditorUtility.RepaintAllViews();
            Debug.Log($"Style refreshed {DateTime.Now}");
        }

        [MenuItem("Theme/Switch Theme And Repaint", priority = 420)]
        internal static void SwitchTheme()
        {
            AssetPreview.ClearTemporaryAssetPreviews();
            InternalEditorUtility.SwitchSkinAndRepaintAllViews();
        }*/

        private static void UpdateGUIStyleProperties(string name, GUIStyle style)
        {
            var sname = GUIStyleExtensions.StyleNameToBlockName(style.name, false);
            var block = styleCatalog.GetStyle(sname);
            if (!block.IsValid())
                return;

            try
            {
                GUIStyleExtensions.PopulateStyle(styleCatalog, style, sname);

                // The new style extension do not support state backgrounds anymore.
                // Any background images needs to be defined in the uss data files.
                ResetDeprecatedBackgroundImage(style.normal);
                ResetDeprecatedBackgroundImage(style.hover);
                ResetDeprecatedBackgroundImage(style.active);
                ResetDeprecatedBackgroundImage(style.focused);
                ResetDeprecatedBackgroundImage(style.onNormal);
                ResetDeprecatedBackgroundImage(style.onHover);
                ResetDeprecatedBackgroundImage(style.onActive);
                ResetDeprecatedBackgroundImage(style.onFocused);
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

                foreach (var assetPath in importedAssets.Concat(deletedAssets))
                {
                    if (!IsEditorStyleSheet(assetPath))
                        continue;
                    s_RefreshGlobalStyleCatalog = true;
                }
            }
        }
    }
}
