// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal static class Resources
    {
        private const string TemplateRoot = "UXML/PackageManager/";

        private const string PackageManagerCommonStyleSheetPath = "StyleSheets/PackageManager/Common.uss";
        private const string PackageManagerDarkVariablesSheetPath = "StyleSheets/PackageManager/Dark.uss";
        private const string PackageManagerLightVariablesSheetPath = "StyleSheets/PackageManager/Light.uss";

        private static StyleSheet _darkStyleSheet;
        private static StyleSheet DarkStyleSheet
        {
            get
            {
                if (_darkStyleSheet == null)
                    _darkStyleSheet = LoadAndResolveStyleSheet(true);
                return _darkStyleSheet;
            }
        }

        private static StyleSheet _lightStyleSheet;
        private static StyleSheet LightStyleSheet
        {
            get
            {
                if (_lightStyleSheet == null)
                    _lightStyleSheet = LoadAndResolveStyleSheet(false);
                return _lightStyleSheet;
            }
        }

        private static StyleSheet LoadAndResolveStyleSheet(bool isDarkTheme)
        {
            var styleSheet = ScriptableObject.CreateInstance<StyleSheet>();
            styleSheet.hideFlags = HideFlags.HideAndDontSave;
            styleSheet.isUnityStyleSheet = true;

            var packageManagerThemeVariablesSheetPath = isDarkTheme ? PackageManagerDarkVariablesSheetPath : PackageManagerLightVariablesSheetPath;

            var packageManagerCommon = EditorGUIUtility.Load(PackageManagerCommonStyleSheetPath) as StyleSheet;
            var packageManagerTheme = EditorGUIUtility.Load(packageManagerThemeVariablesSheetPath) as StyleSheet;

            var resolver = new StyleSheets.StyleSheetResolver();
            resolver.AddStyleSheets(packageManagerCommon, packageManagerTheme);
            resolver.ResolveTo(styleSheet);

            return styleSheet;
        }

        private static string TemplatePath(string filename)
        {
            return TemplateRoot + filename;
        }

        public static VisualTreeAsset GetVisualTreeAsset(string templateFilename)
        {
            return EditorGUIUtility.Load(TemplatePath(templateFilename)) as VisualTreeAsset;
        }

        public static VisualElement GetTemplate(string templateFilename)
        {
            return GetVisualTreeAsset(templateFilename)?.CloneTree();
        }

        public static StyleSheet GetStyleSheet()
        {
            return EditorGUIUtility.isProSkin ? DarkStyleSheet : LightStyleSheet;
        }
    }
}
