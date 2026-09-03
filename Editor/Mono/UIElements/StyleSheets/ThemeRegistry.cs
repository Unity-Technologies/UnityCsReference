// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements.StyleSheets
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "Unity.Modules.Core.TextureStreaming.Tests.Editor")]
    static class ThemeRegistry
    {
        internal static string k_DefaultStyleSheetPath
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "Unity.Modules.Core.TextureStreaming.Tests.Editor")]
            get => "StyleSheets/Generated/Default.tss.asset";
        }

        public const string kThemeScheme = "unity-theme";
        public const string kUnityThemesPath = "Assets/UI Toolkit/UnityThemes";
        public const string kUnityRuntimeThemeFileName = "UnityDefaultRuntimeTheme.tss";
        public const string kUnityRuntimeThemePath = kUnityThemesPath + "/" + kUnityRuntimeThemeFileName;


        internal const string k_ThemeDependencyPrefix = "uitk/builtin-theme/";

        [NoAutoStaticsCleanup]
        private static Dictionary<string, string> m_Themes;

        public static Dictionary<string, string> themes
        {
            get
            {
                if (m_Themes == null)
                {
                    m_Themes = new Dictionary<string, string>();

                    RegisterTheme("default", k_DefaultStyleSheetPath);
                }
                return m_Themes;
            }
        }

        public static void RegisterTheme(string themeName, string path)
        {
            themes[themeName] = path;
        }

        public static void UnregisterTheme(string themeName)
        {
            themes.Remove(themeName);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static string FormatThemeDependencyKey(string themeName)
        {
            return k_ThemeDependencyPrefix + themeName;
        }

        // Computes a stable hash of a theme stylesheet that captures both its own content
        // and the content of any stylesheets it imports transitively. This is what gets
        // published as the custom dependency value: dependent UXML/USS will be reimported
        // when (and only when) the theme content actually changes.
        static Hash128 ComputeThemeContentHash(StyleSheet styleSheet)
        {
            var hash = new Hash128();
            AppendStyleSheetContentHash(styleSheet, ref hash, visited: null);
            return hash;
        }

        static void AppendStyleSheetContentHash(StyleSheet styleSheet, ref Hash128 hash, HashSet<StyleSheet> visited)
        {
            if (styleSheet == null)
                return;

            visited ??= new HashSet<StyleSheet>();
            if (!visited.Add(styleSheet))
                return;

            hash.Append(styleSheet.contentHash);

            var imports = styleSheet.imports;
            if (imports == null)
                return;

            for (int i = 0; i < imports.Length; ++i)
                AppendStyleSheetContentHash(imports[i].styleSheet, ref hash, visited);
        }

        // Publishes the custom dependency hashes for all currently registered themes.
        // Must be called outside of an asset import (AssetDatabase.RegisterCustomDependency
        // throws while importing). The expected caller is editor initialization, before
        // any UXML/USS reimport runs.
        internal static void RegisterCustomDependencies()
        {
            // Drop any stale entries from a previous Editor session (e.g. if a theme was
            // renamed or removed). New ones get republished below.
            AssetDatabase.UnregisterCustomDependencyPrefixFilter(k_ThemeDependencyPrefix);

            foreach (var pair in themes)
            {
                var themeName = pair.Key;
                var themePath = pair.Value;

                var styleSheet = EditorGUIUtility.Load(themePath) as StyleSheet;
                var hash = ComputeThemeContentHash(styleSheet);

                AssetDatabase.RegisterCustomDependency(FormatThemeDependencyKey(themeName), hash);
            }
        }
    }
}
