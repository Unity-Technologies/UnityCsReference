// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal static class VisualTreeAssetExtensions
    {
        static readonly IComparer<VisualTreeAsset.UsingEntry> s_UsingEntryPathComparer = new UsingEntryPathComparer();

        class UsingEntryPathComparer : IComparer<VisualTreeAsset.UsingEntry>
        {
            public int Compare(VisualTreeAsset.UsingEntry x, VisualTreeAsset.UsingEntry y)
            {
                return Comparer<string>.Default.Compare(x.path, y.path);
            }
        }

        public static VisualElementAsset AddVisualElementAssetFromVisualElement(
            this VisualTreeAsset vta, VisualElementAsset parent, VisualElement visualElement)
        {
            var fullTypeName = visualElement.GetUxmlFullTypeName();
            var xmlns = vta.FindUxmlNamespaceDefinitionForTypeName(parent, fullTypeName);
            var vea = new VisualElementAsset(fullTypeName, xmlns);

            var overriddenAttributes = visualElement.GetOverriddenAttributes();
            foreach (var attribute in overriddenAttributes)
                vea.SetAttribute(attribute.Key, attribute.Value);

            parent ??= vta.visualTree;
            parent.Add(vea);
            return vea;
        }

        public static TemplateAsset AddTemplateInstance(
            this VisualTreeAsset vta, VisualElementAsset parent, string path)
        {
            var templateName = vta.GetTemplateNameFromPath(path);
            if (!vta.TemplateExists(templateName))
            {
                var resolvedAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                if (resolvedAsset)
                {
                    vta.RegisterTemplate(templateName, resolvedAsset);
                }
                else
                {
                    vta.RegisterTemplate(templateName, path);
                }
            }

            var xmlns = vta.FindUxmlNamespaceDefinitionForTypeName(parent, TemplateAsset.UxmlInstanceTypeName);
            var templateAsset = new TemplateAsset(templateName, xmlns);

            templateAsset.SetAttribute("template", templateName);
            parent ??= vta.visualTree;
            parent.Add(templateAsset);

            return templateAsset;
        }

        public static string GetTemplateNameFromPath(this VisualTreeAsset vta, string path)
        {
            var usings = vta.usings;
            if (usings != null && usings.Count > 0)
            {
                var lookingFor = new VisualTreeAsset.UsingEntry(null, path);
                int index = usings.BinarySearch(lookingFor, s_UsingEntryPathComparer);
                if (index >= 0 && usings[index].path == path)
                {
                    return usings[index].alias;
                }
            }

            return Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// Gets whether the UXML document is in editor extension mode.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal static bool IsEditorExtensionMode(this VisualTreeAsset vta)
        {
            const string k_EditorExtensionModeAttributeName = "editor-extension-mode";

            if (vta == null)
                return false;

            var rootElement = vta.visualTree;

            if (rootElement != null && rootElement.HasAttribute(k_EditorExtensionModeAttributeName))
            {
                return System.Convert.ToBoolean(rootElement.GetAttributeValue(k_EditorExtensionModeAttributeName));
            }

            return false;
        }

        /// <summary>
        /// Gets all stylesheets referenced by the root element of the VisualTreeAsset.
        /// Fills the provided list with stylesheets. The list is not cleared before filling.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static void GetAllReferencedStyleSheets(this VisualTreeAsset vta, List<StyleSheet> outStyleSheets)
        {
            var visualTree = vta.visualTreeNoAlloc;

            if (visualTree == null)
                return;

            using var _ = HashSetPool<StyleSheet>.Get(out var sheets);

            // Get stylesheets directly attached to the root element
            var styleSheets = visualTree.stylesheets;
            if (styleSheets != null)
            {
                foreach (var styleSheet in styleSheets)
                {
                    if (styleSheet != null)
                        sheets.Add(styleSheet);
                }
            }

            // Get stylesheets from paths
            var styleSheetPaths = visualTree.GetStyleSheetPaths();
            if (styleSheetPaths != null)
            {
                foreach (var sheetPath in styleSheetPaths)
                {
                    var sheetAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(sheetPath);
                    if (sheetAsset == null)
                    {
                        sheetAsset = UnityEngine.Resources.Load<StyleSheet>(sheetPath);
                        if (sheetAsset == null)
                            continue;
                    }
                    sheets.Add(sheetAsset);
                }
            }

            outStyleSheets.AddRange(sheets);
        }

        /// <summary>
        /// Gets all stylesheets referenced by the root element of the VisualTreeAsset.
        /// Returns a new list containing the stylesheets.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static List<StyleSheet> GetAllReferencedStyleSheets(this VisualTreeAsset vta)
        {
            var result = new List<StyleSheet>();
            vta.GetAllReferencedStyleSheets(result);
            return result;
        }
    }
}
