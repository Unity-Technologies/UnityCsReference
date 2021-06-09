// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements.StyleSheets;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.UIElements
{
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

        internal static string GetTssTemplateContent()
        {
            return "@import url(\"" + ThemeRegistry.kThemeScheme + "://default\");";
        }

        internal static ThemeStyleSheet GetOrCreateDefaultTheme()
        {
            // Create unity default theme if it is not there
            var defaultTssPath = ThemeRegistry.kUnityRuntimeThemePath;

            var defaultTssAsset = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(defaultTssPath);
            if (defaultTssAsset == null)
            {
                CreateDirectoryRecursively(ThemeRegistry.kUnityThemesPath);
                File.WriteAllText(defaultTssPath, GetTssTemplateContent());
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                defaultTssAsset = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(defaultTssPath);
            }

            return defaultTssAsset;
        }

        [MenuItem("Assets/Create/UI Toolkit/Panel Settings Asset", false, 701, false)]
        static void CreatePanelSettings()
        {
            var defaultTssAsset = GetOrCreateDefaultTheme();

            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.themeStyleSheet = defaultTssAsset;
            ProjectWindowUtil.CreateAsset(settings, "New Panel Settings.asset");
        }
    }
}
