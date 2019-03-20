// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal static class Resources
    {
        private const string TemplateRoot = "UXML/PackageManager/";

        private const string DarkStylePath = "StyleSheets/PackageManager/Main_Dark.uss";
        private const string LightStylePath = "StyleSheets/PackageManager/Main_Light.uss";

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
            var path = EditorGUIUtility.isProSkin ? DarkStylePath : LightStylePath;
            return EditorGUIUtility.LoadRequired(path) as StyleSheet;
        }
    }
}
