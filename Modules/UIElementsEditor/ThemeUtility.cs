// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Canvas theme types for UI preview and editing.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal enum CanvasTheme
    {
        ProjectSettings,
        Default,
        Dark,
        Light,
        Custom,
    }

    /// <summary>
    /// Utility class for theme-related operations shared between UI Builder and UI Toolkit settings.
    /// </summary>
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal static class ThemeUtility
    {
        // Internal for tests
        internal const string defaultRuntimeThemeContent = "@import url(\"" + ThemeRegistry.kThemeScheme +
                                                         "://default\");\nVisualElement {}";

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal const string DefaultThemeSuffix = " (Default)";

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal const string ProjectThemeSuffix = " (Project)";

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal const string BuiltInDefaultRuntimeThemeName = "Built-in Default Runtime Theme";

        private static ThemeStyleSheet s_BuiltInDefaultRuntimeTheme;

        // EditorPrefs keys for theme preferences
        const string k_EditorCanvasThemeKey = "UIBuilder.EditorCanvasTheme";
        const string k_RuntimeCanvasThemeKey = "UIBuilder.RuntimeCanvasTheme";
        const string k_EditorThemePathKey = "UIBuilder.EditorThemePath";
        const string k_RuntimeThemePathKey = "UIBuilder.RuntimeThemePath";

        // Local project-wide theme preferences stored in EditorPrefs
        static CanvasTheme m_EditorCanvasTheme = CanvasTheme.ProjectSettings;
        static CanvasTheme m_RuntimeCanvasTheme = CanvasTheme.ProjectSettings;
        static LazyLoadReference<ThemeStyleSheet> m_EditorThemeReference;
        static LazyLoadReference<ThemeStyleSheet> m_RuntimeThemeReference;

        /// <summary>
        /// Gets the built-in default runtime theme. Lazy-loaded on first access.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static ThemeStyleSheet builtInDefaultRuntimeTheme
        {
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            get
            {
                if (s_BuiltInDefaultRuntimeTheme == null)
                    s_BuiltInDefaultRuntimeTheme = EditorGUIUtility.Load(ThemeRegistry.k_DefaultStyleSheetPath) as ThemeStyleSheet;
                return s_BuiltInDefaultRuntimeTheme;
            }
        }

        /// <summary>
        /// Gets display text for editor theme types.
        /// </summary>
        /// <param name="theme">The theme type</param>
        /// <returns>Display text for the theme</returns>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static string GetEditorThemeText(CanvasTheme theme)
        {
            switch (theme)
            {
                case CanvasTheme.Default: return "Active Editor Theme";
                case CanvasTheme.Dark: return "Dark Editor Theme";
                case CanvasTheme.Light: return "Light Editor Theme";
            }

            return null;
        }

        /// Gets available theme files in the project.
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static void GetRuntimeThemeFiles(SortedSet<string> themeFiles)
        {
            themeFiles.Clear();

            var searchFilter = new SearchFilter { searchArea = SearchFilter.SearchArea.AllAssets, classNames = new[] { nameof(ThemeStyleSheet) } };

            var assets = AssetDatabase.FindAllAssets(searchFilter);
            foreach (var asset in assets)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset.entityId);
                themeFiles.Add(assetPath);
            }

            // If we don't have a Default Runtime Theme in the project, we add the built-in one
            if (FindProjectDefaultRuntimeThemeAsset(themeFiles) == null)
            {
                themeFiles.Add(ThemeRegistry.k_DefaultStyleSheetPath);
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static ThemeStyleSheet FindProjectDefaultRuntimeThemeAssetOrDefault(SortedSet<string> themeFiles)
        {
            var projectDefaultTssAsset = FindProjectDefaultRuntimeThemeAsset(themeFiles);

            // Otherwise we return the built-in Default Runtime Theme
            var defaultTssAsset = projectDefaultTssAsset ?? builtInDefaultRuntimeTheme;
            return defaultTssAsset;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static ThemeStyleSheet FindProjectDefaultRuntimeThemeAsset(SortedSet<string> files)
        {
            if (files.Count <= 0)
            {
                return null;
            }

            foreach (var themeFilePath in files)
            {
                if (files.Contains(ThemeRegistry.kUnityRuntimeThemeFileName) && IsProjectDefaultRuntimeAsset(themeFilePath))
                {
                    return AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themeFilePath);
                }
            }

            if (files.Contains(ThemeRegistry.k_DefaultStyleSheetPath) && IsProjectDefaultRuntimeAsset(ThemeRegistry.k_DefaultStyleSheetPath))
            {
                return AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemeRegistry.k_DefaultStyleSheetPath);
            }

            foreach (var themeFilePath in files)
            {
                if (IsProjectDefaultRuntimeAsset(themeFilePath))
                {
                    return AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themeFilePath);
                }
            }

            return null;
        }

        internal static bool IsProjectDefaultRuntimeAsset(string path)
        {
            if (path == ThemeRegistry.k_DefaultStyleSheetPath)
            {
                return false;
            }

            if (!File.Exists(path))
            {
                return false;
            }

            var assetString = File.ReadAllText(path);

            if (!string.IsNullOrEmpty(assetString))
            {
                assetString = DeleteNewlinesAndWhitespaces(assetString);
            }

            return assetString == DeleteNewlinesAndWhitespaces(defaultRuntimeThemeContent)
                   || assetString == DeleteNewlinesAndWhitespaces(PanelSettingsCreator.GetTssTemplateContent());
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static bool IsEditorCanvasTheme(CanvasTheme theme)
        {
            return theme is CanvasTheme.Default or CanvasTheme.Dark or CanvasTheme.Light;
        }

        private static string DeleteNewlinesAndWhitespaces(string str)
        {
            return Regex.Replace(str, @"[\r\n\s]+", string.Empty);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static bool HasLocalThemeOverride()
        {
            return m_EditorCanvasTheme != CanvasTheme.ProjectSettings ||
                   m_RuntimeCanvasTheme != CanvasTheme.ProjectSettings;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static void SetEditorThemeOverride(CanvasTheme canvasTheme, ThemeStyleSheet themeSheet)
        {
            m_EditorCanvasTheme = canvasTheme;
            m_EditorThemeReference = themeSheet;
            SaveThemeOverrides();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static void SetRuntimeThemeOverride(CanvasTheme canvasTheme, ThemeStyleSheet themeSheet)
        {
            m_RuntimeCanvasTheme = canvasTheme;
            m_RuntimeThemeReference = themeSheet;
            SaveThemeOverrides();
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static void ClearThemeOverrides()
        {
            SetEditorThemeOverride(CanvasTheme.ProjectSettings, null);
            SetRuntimeThemeOverride(CanvasTheme.ProjectSettings, null);
        }

        static void SaveThemeOverrides()
        {
            EditorPrefs.SetInt(k_EditorCanvasThemeKey, (int)m_EditorCanvasTheme);
            EditorPrefs.SetInt(k_RuntimeCanvasThemeKey, (int)m_RuntimeCanvasTheme);

            var editorThemePath = m_EditorThemeReference.asset != null ? AssetDatabase.GetAssetPath(m_EditorThemeReference.asset) : string.Empty;
            var runtimeThemePath = m_RuntimeThemeReference.asset != null ? AssetDatabase.GetAssetPath(m_RuntimeThemeReference.asset) : string.Empty;

            EditorPrefs.SetString(k_EditorThemePathKey, editorThemePath);
            EditorPrefs.SetString(k_RuntimeThemePathKey, runtimeThemePath);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static void LoadThemeOverrides()
        {
            if (EditorPrefs.HasKey(k_EditorCanvasThemeKey))
            {
                m_EditorCanvasTheme = (CanvasTheme)EditorPrefs.GetInt(k_EditorCanvasThemeKey, (int)CanvasTheme.ProjectSettings);
                m_RuntimeCanvasTheme = (CanvasTheme)EditorPrefs.GetInt(k_RuntimeCanvasThemeKey, (int)CanvasTheme.ProjectSettings);

                var editorThemePath = EditorPrefs.GetString(k_EditorThemePathKey, string.Empty);
                var runtimeThemePath = EditorPrefs.GetString(k_RuntimeThemePathKey, string.Empty);

                if (!string.IsNullOrEmpty(editorThemePath))
                    m_EditorThemeReference = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(editorThemePath);

                if (!string.IsNullOrEmpty(runtimeThemePath))
                    m_RuntimeThemeReference = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(runtimeThemePath);
            }
        }

        /// <summary>
        /// Returns the effective theme to use based on a 2-layer hierarchy:
        /// 1. User override
        /// 2. Project settings (from UIToolkitProjectSettings) or Fall-backs
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static (CanvasTheme theme, ThemeStyleSheet sheet) GetEffectiveTheme(bool isEditorExtensionMode, SortedSet<string> themeFiles)
        {
            // Layer 1: Check for user override first
            var (overrideTheme, overrideSheet) = (m_RuntimeCanvasTheme, m_RuntimeThemeReference.asset);
            if (isEditorExtensionMode)
                (overrideTheme, overrideSheet) = (m_EditorCanvasTheme, m_EditorThemeReference.asset);

            if (overrideTheme != CanvasTheme.ProjectSettings)
            {
                if (isEditorExtensionMode)
                    return (overrideTheme, overrideSheet);

                // For runtime mode, only accept non-editor themes
                if (!IsEditorCanvasTheme(overrideTheme))
                    return (overrideTheme, overrideSheet);
            }

            // Layer 2: Use project settings (which handles built-in defaults internally)
            return GetProjectDefaultTheme(isEditorExtensionMode, themeFiles);
        }

        /// <summary>
        /// Returns the project-level theme preference (ignoring user overrides).
        /// This is layer 2 of the theme hierarchy: Project settings → built-in defaults.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static (CanvasTheme theme, ThemeStyleSheet sheet) GetProjectDefaultTheme(bool isEditorExtensionMode, SortedSet<string> themeFiles)
        {
            if (isEditorExtensionMode)
            {
                var editorTheme = UIToolkitProjectSettings.defaultEditorTheme;
                var editorCanvasTheme = UIToolkitProjectSettings.defaultEditorCanvasTheme;

                if (editorCanvasTheme == CanvasTheme.ProjectSettings)
                    return (CanvasTheme.Default, null);

                return (editorCanvasTheme, editorTheme);
            }

            var runtimeTheme = UIToolkitProjectSettings.defaultRuntimeTheme;
            var runtimeCanvasTheme = UIToolkitProjectSettings.defaultRuntimeCanvasTheme;

            if (runtimeTheme == null)
            {
                runtimeTheme = FindProjectDefaultRuntimeThemeAssetOrDefault(themeFiles);
                runtimeCanvasTheme = CanvasTheme.Custom;
            }

            return (runtimeCanvasTheme, runtimeTheme);
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static string NicifyThemeName(ThemeStyleSheet theme)
        {
            if (theme == builtInDefaultRuntimeTheme)
                return BuiltInDefaultRuntimeThemeName;

            return ObjectNames.NicifyVariableName(theme.name);
        }
    }
}
