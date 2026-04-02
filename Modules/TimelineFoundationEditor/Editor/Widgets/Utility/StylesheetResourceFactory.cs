// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    readonly struct StylesheetResource
    {
        readonly string m_StylesheetPath;
        readonly string m_DarkStylesheetPath;
        readonly string m_LightStylesheetPath;

        public StylesheetResource(string stylesheetPath, string darkStylesheetPath, string lightStylesheetPath)
        {
            m_StylesheetPath = stylesheetPath;
            m_DarkStylesheetPath = darkStylesheetPath;
            m_LightStylesheetPath = lightStylesheetPath;
        }

        public void ApplyTo(VisualElement visualElement)
        {
            visualElement.ApplyStyleSheetWithValidation(m_StylesheetPath);
            visualElement.ApplyStyleSheet(EditorGUIUtility.isProSkin ? m_DarkStylesheetPath : m_LightStylesheetPath);
        }
    }

    class StylesheetResourceFactory
    {
        const string k_StylesheetExtension = ".uss";
        const string k_DarkThemeExtension = "Dark" + k_StylesheetExtension;
        const string k_LightThemeExtension = "Light" + k_StylesheetExtension;

        readonly string m_StylesheetDirectory;

        public StylesheetResourceFactory(string stylesheetDirectory)
        {
            m_StylesheetDirectory = stylesheetDirectory;
        }

        public StylesheetResource Get(string stylesheetName)
        {
            var stylesheetFileName = $"{stylesheetName}{k_StylesheetExtension}";
            string stylesheetPath = Path.Join(m_StylesheetDirectory, stylesheetFileName);
            string darkStyleSheetPath = stylesheetPath.Replace(k_StylesheetExtension, k_DarkThemeExtension);
            string lightStyleSheetPath = stylesheetPath.Replace(k_StylesheetExtension, k_LightThemeExtension);
            return new StylesheetResource(stylesheetPath, darkStyleSheetPath, lightStyleSheetPath);
        }

        public StylesheetResource Get<T>()
        {
            return Get(typeof(T).Name);
        }
    }
}
