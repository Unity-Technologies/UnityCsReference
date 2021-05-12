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
        public static readonly FieldInfo AttributesListFieldInfo =
            typeof(VisualElementAsset).GetField("m_Properties", BindingFlags.Instance | BindingFlags.NonPublic);

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

        public static bool HasParent(this VisualElementAsset vea)
        {
            return vea.parentId != 0;
        }

        public static bool HasAttribute(this VisualElementAsset vea, string attributeName)
        {
            var fieldInfo = AttributesListFieldInfo;
            if (fieldInfo == null)
            {
                Debug.LogError("UI Builder: VisualElementAsset.m_Properties field has not been found! Update the reflection code!");
                return false;
            }

            var attributes = fieldInfo.GetValue(vea) as List<string>;
            if (attributes != null && attributes.Count > 0)
            {
                for (int i = 0; i < attributes.Count; i += 2)
                {
                    var name = attributes[i];
                    var value = attributes[i + 1];

                    if (name != attributeName)
                        continue;

                    return true;
                }
            }

            return false;
        }

        public static string GetAttributeValue(this VisualElementAsset vea, string attributeName)
        {
            var fieldInfo = AttributesListFieldInfo;
            if (fieldInfo == null)
            {
                Debug.LogError("UI Builder: VisualElementAsset.m_Properties field has not been found! Update the reflection code!");
                return null;
            }

            var attributes = fieldInfo.GetValue(vea) as List<string>;
            if (attributes != null && attributes.Count > 0)
            {
                for (int i = 0; i < attributes.Count; i += 2)
                {
                    var name = attributes[i];
                    var value = attributes[i + 1];

                    if (name != attributeName)
                        continue;

                    return value;
                }
            }

            return null;
        }

        public static void SetAttributeValue(this VisualElementAsset vea, string attributeName, string attributeValue)
        {
            vea.AddProperty(attributeName, attributeValue);
        }

        public static void RemoveAttribute(this VisualElementAsset vea, string attributeName)
        {
            var fieldInfo = AttributesListFieldInfo;
            if (fieldInfo == null)
            {
                Debug.LogError("UI Builder: VisualElementAsset.m_Properties field has not been found! Update the reflection code!");
                return;
            }

            var attributes = fieldInfo.GetValue(vea) as List<string>;
            if (attributes != null && attributes.Count > 0)
            {
                for (int i = 0; i < attributes.Count; i += 2)
                {
                    var name = attributes[i];
                    var value = attributes[i + 1];

                    if (name != attributeName)
                        continue;

                    attributes.RemoveAt(i); // Removing the name at i.
                    attributes.RemoveAt(i); // Removing the value at i + 1.
                    return;
                }
            }
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
            vea.SetAttributeValue(
                BuilderConstants.SelectedVisualElementAssetAttributeName,
                BuilderConstants.SelectedVisualElementAssetAttributeValue);
        }

        public static void Deselect(this VisualElementAsset vea)
        {
            vea.SetAttributeValue(
                BuilderConstants.SelectedVisualElementAssetAttributeName,
                string.Empty);
        }
    }
}
