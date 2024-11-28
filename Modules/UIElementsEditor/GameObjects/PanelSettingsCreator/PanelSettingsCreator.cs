// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine.Bindings;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    static class PanelSettingsCreator
    {
        static void CreateDirectoryRecursively(string dirPath)
        {
            var paths = dirPath.Split('/');
            string currentPath = "";

            foreach (var path in paths)
            {
                currentPath += path;
                if (!Directory.Exists(currentPath))
                {
                    Directory.CreateDirectory(currentPath);
                }
                currentPath += "/";
            }
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static string GetTssTemplateContent()
        {
            return "@import url(\"" + ThemeRegistry.kThemeScheme + "://default\");";
        }

        internal static ThemeStyleSheet GetFirstThemeOrCreateDefaultTheme()
        {
            // Find first runtime theme
            var runtimeThemes = AssetDatabase.FindAssets(new SearchFilter
            {
                searchArea = SearchFilter.SearchArea.InAssetsOnly,
                classNames = new[] { nameof(ThemeStyleSheet) }
            });

            if (runtimeThemes.Length > 0)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(runtimeThemes[0]);
                return AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(assetPath);
            }

            return CreateDefaultTheme();
        }

        private static ThemeStyleSheet CreateDefaultTheme()
        {
            CreateDirectoryRecursively(ThemeRegistry.kUnityThemesPath);
            File.WriteAllText(ThemeRegistry.kUnityRuntimeThemePath, GetTssTemplateContent());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var defaultTssAsset = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemeRegistry.kUnityRuntimeThemePath);

            return defaultTssAsset;
        }

        [MenuItem("Assets/Create/UI Toolkit/Panel Settings Asset", priority = 702)]
        static void CreatePanelSettings()
        {
            var defaultTssAsset = GetFirstThemeOrCreateDefaultTheme();

            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.themeStyleSheet = defaultTssAsset;
            settings.AssignICUData();
            ProjectWindowUtil.CreateAsset(settings, "New Panel Settings.asset");
        }
    }
}
