using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Unity.UI.Builder
{
    internal static class VisualElementAssetExtensions
    {
        public static List<StyleSheet> GetStyleSheets(this VisualElementAsset vea)
        {
            return vea.stylesheets;
        }

        public static List<string> GetStyleSheetPaths(this VisualElementAsset vea)
        {
            List<string> ret = new List<string>();

            foreach (var sheet in vea.stylesheets)
            {
                var path = AssetDatabase.GetAssetPath(sheet);

                if (!string.IsNullOrEmpty(path))
                    ret.Add(path);
            }
            return ret;
        }

        public static void AddStyleClass(this VisualElementAsset vea, string className)
        {
            var classList = vea.classes.ToList();

            if (!classList.Contains(className))
                classList.Add(className);

            vea.classes = classList.ToArray();
        }

        public static void RemoveStyleClass(this VisualElementAsset vea, string className)
        {
            var classList = vea.classes.ToList();

            classList.Remove(className);

            vea.classes = classList.ToArray();
        }

        public static void ClearStyleSheets(this VisualElementAsset vea)
        {
            vea.stylesheets.Clear();
            vea.GetStyleSheetPaths().Clear();
        }

        public static void AddStyleSheet(this VisualElementAsset vea, StyleSheet styleSheet)
        {
            if (styleSheet == null || vea.stylesheets.Contains(styleSheet))
                return;

            vea.stylesheets.Add(styleSheet);
        }

        public static void AddStyleSheetPath(this VisualElementAsset vea, string styleSheetPath)
        {
            if (string.IsNullOrEmpty(styleSheetPath) || vea.GetStyleSheetPaths().Contains(styleSheetPath))
                return;

            vea.GetStyleSheetPaths().Add(styleSheetPath);
        }

        public static void RemoveStyleSheet(this VisualElementAsset vea, StyleSheet styleSheet)
        {
            vea.stylesheets.RemoveAll((s) => s == styleSheet);
        }

        public static void RemoveStyleSheetPath(this VisualElementAsset vea, string styleSheetPath)
        {
            vea.GetStyleSheetPaths().RemoveAll((s) => s == styleSheetPath);
        }

        public static bool IsSelected(this VisualElementAsset vea)
        {
            var value = vea.GetAttributeValue(BuilderConstants.SelectedVisualElementAssetAttributeName);
            return value == BuilderConstants.SelectedVisualElementAssetAttributeValue;
        }

        public static void Select(this VisualElementAsset vea)
        {
            vea.SetAttribute(
                BuilderConstants.SelectedVisualElementAssetAttributeName,
                BuilderConstants.SelectedVisualElementAssetAttributeValue);
        }

        public static void Deselect(this VisualElementAsset vea)
        {
            vea.SetAttribute(
                BuilderConstants.SelectedVisualElementAssetAttributeName,
                string.Empty);
        }
    }
}
