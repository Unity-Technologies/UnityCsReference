// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.StyleSheets;
using UnityEngine;
using UnityEngine.Internal;

namespace UnityEditor.Experimental
{
    [ExcludeFromDocs]
    public partial class EditorResources
    {
        // Global editor styles
        internal static StyleCatalog styleCatalog { get; private set; }

        static EditorResources()
        {
            styleCatalog = new StyleCatalog();
            styleCatalog.Load(GetDefaultStyleCatalogPaths());
        }

        private static bool CanEnableExtendedStyles()
        {
            var rootBlock = styleCatalog.GetStyle(StyleCatalogKeyword.root, StyleState.root);
            var styleExtensionDisabled = rootBlock.GetBool("-unity-disable-style-extensions");
            var runningTests = Application.isTestRun || Application.isBatchMode;
            if (!styleExtensionDisabled && !runningTests)
                return true;
            return false;
        }

        private static bool IsEditorStyleSheet(string path)
        {
            var pathLowerCased = path.ToLower();
            return pathLowerCased.Contains("/editor/") && (pathLowerCased.EndsWith("common.uss") ||
                pathLowerCased.EndsWith(EditorGUIUtility.isProSkin ? "dark.uss" : "light.uss"));
        }

        private static List<string> GetDefaultStyleCatalogPaths()
        {
            bool useDarkTheme = EditorGUIUtility.isProSkin;
            var catalogFiles = new List<string>
            {
                "StyleSheets/Extensions/common.uss",
                useDarkTheme ? "StyleSheets/Extensions/dark.uss" : "StyleSheets/Extensions/light.uss"
            };

            return catalogFiles;
        }

        internal static void BuildCatalog()
        {
            styleCatalog = new StyleCatalog();
            var paths = GetDefaultStyleCatalogPaths();
            foreach (var editorUssPath in AssetDatabase.GetAllAssetPaths().Where(IsEditorStyleSheet))
                paths.Add(editorUssPath);

            styleCatalog.Load(paths);
            if (CanEnableExtendedStyles())
            {
                // Update gui skin style layouts
                var skin = GUIUtility.GetDefaultSkin();
                if (skin != null)
                {
                    UpdateGUIStyleProperties(skin);
                }
            }
        }

        internal static void UpdateGUIStyleProperties(GUISkin skin)
        {
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
    }
}
