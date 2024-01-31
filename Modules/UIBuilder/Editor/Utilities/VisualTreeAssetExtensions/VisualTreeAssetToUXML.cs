// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Unity.UI.Builder
{
    internal static class VisualTreeAssetToUXML
    {
        static readonly string k_ColumnFullName = typeof(Column).FullName;

        static void Indent(StringBuilder stringBuilder, int depth)
        {
            for (int i = 0; i < depth; ++i)
                stringBuilder.Append("    ");
        }

        static void AppendElementTypeName(VisualTreeAsset vta, UxmlAsset root, StringBuilder stringBuilder)
        {
            if (root is UxmlObjectAsset uxmlObjectAsset)
            {
                if (uxmlObjectAsset.isField)
                {
                    stringBuilder.Append(uxmlObjectAsset.fullTypeName);
                    return;
                }
            }

            var xmlNamespace = root.xmlNamespace;

            if (xmlNamespace != UxmlNamespaceDefinition.Empty)
            {
                var namespaceDefinition = vta.FindUxmlNamespaceDefinitionFromPrefix(root, xmlNamespace.prefix);
                if (namespaceDefinition != xmlNamespace)
                {
                    xmlNamespace = vta.FindUxmlNamespaceDefinitionForTypeName(root, root.fullTypeName);
                }
            }

            if (string.IsNullOrEmpty(xmlNamespace.prefix))
            {
                if (string.IsNullOrEmpty(xmlNamespace.resolvedNamespace))
                {
                    stringBuilder.Append(root.fullTypeName);
                }
                else
                {
                    var name = root.fullTypeName.Substring(xmlNamespace.resolvedNamespace.Length + 1);
                    stringBuilder.Append(name);
                }
            }
            else
            {
                stringBuilder.Append($"{xmlNamespace.prefix}:");
                var name = root.fullTypeName.Substring(xmlNamespace.resolvedNamespace.Length + 1);
                stringBuilder.Append(name);
            }
        }

        static void AppendElementAttribute(string name, string value, StringBuilder stringBuilder)
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (name == "picking-mode" && value == "Position")
                return;

            // Clean up value and make it ready for XML.
            value = URIHelpers.EncodeUri(value);

            stringBuilder.Append(" ");
            stringBuilder.Append(name);
            stringBuilder.Append("=\"");
            stringBuilder.Append(value);
            stringBuilder.Append("\"");
        }

        static void AppendElementNonStyleAttributes(VisualElementAsset vea, StringBuilder stringBuilder, bool writingToFile)
        {
            AppendElementAttributes(vea, stringBuilder, writingToFile, "class", "style");
        }

        static void AppendHeaderAttributes(VisualTreeAsset vta, StringBuilder stringBuilder, bool writingToFile)
        {
            AppendElementAttributes(vta.GetRootUxmlElement(), stringBuilder, writingToFile);
        }

        static void AppendNamespaceDefinition(UxmlNamespaceDefinition definition, StringBuilder stringBuilder)
        {
            AppendElementAttribute(string.IsNullOrEmpty(definition.prefix) ? "xmlns" : $"xmlns:{definition.prefix}", definition.resolvedNamespace, stringBuilder);
        }

        static void AppendElementAttributes(UxmlAsset uxmlAsset, StringBuilder stringBuilder, bool writingToFile, params string[] ignoredAttributes)
        {
            var namespaceDefinitions = uxmlAsset.namespaceDefinitions;
            if (namespaceDefinitions is {Count: > 0})
            {
                for (var i = 0; i < namespaceDefinitions.Count; ++i)
                {
                    AppendNamespaceDefinition(namespaceDefinitions[i], stringBuilder);
                }
            }

            var attributes = uxmlAsset.GetProperties();
            if (attributes is {Count: > 0})
            {
                for (var i = 0; i < attributes.Count; i += 2)
                {
                    var name = attributes[i];
                    var value = attributes[i + 1];

                    // Avoid writing the selection attribute to UXML.
                    if (writingToFile && name == BuilderConstants.SelectedVisualElementAssetAttributeName)
                        continue;

                    if (ignoredAttributes.Contains(name))
                        continue;

                    AppendElementAttribute(name, value, stringBuilder);
                }
            }
        }

        static void AppendTemplateRegistrations(
            VisualTreeAsset vta, string vtaPath, StringBuilder stringBuilder, HashSet<string> templatesFilter = null)
        {
            var templateAliases = new List<string>();

            if (vta.templateAssets != null && vta.templateAssets.Count > 0)
            {
                foreach (var templateAsset in vta.templateAssets)
                {
                    if (!templateAliases.Contains(templateAsset.templateAlias))
                        templateAliases.Add(templateAsset.templateAlias);
                }
            }

            if (vta.uxmlObjectEntries != null && vta.uxmlObjectEntries.Count > 0)
            {
                foreach (var entry in vta.uxmlObjectEntries)
                {
                    if (entry.uxmlObjectAssets == null)
                        continue;

                    foreach (var uxmlObjectAsset in entry.uxmlObjectAssets)
                    {
                        if (uxmlObjectAsset.fullTypeName != k_ColumnFullName)
                            continue;

                        var templateAlias = uxmlObjectAsset.GetAttributeValue(Column.k_HeaderTemplateAttributeName);

                        if (!string.IsNullOrEmpty(templateAlias) && !templateAliases.Contains(templateAlias))
                            templateAliases.Add(templateAlias);

                        templateAlias = uxmlObjectAsset.GetAttributeValue(Column.k_CellTemplateAttributeName);

                        if (!string.IsNullOrEmpty(templateAlias) && !templateAliases.Contains(templateAlias))
                            templateAliases.Add(templateAlias);
                    }
                }
            }

            var engineNamespaceDefinition = vta.FindUxmlNamespaceDefinitionForTypeName(vta.GetRootUxmlElement(), typeof(VisualElement).FullName);
            foreach (var templateAlias in templateAliases)
            {
                // Skip templates if not in filter.
                if (templatesFilter != null && !templatesFilter.Contains(templateAlias))
                    continue;

                Indent(stringBuilder, 1);

                stringBuilder.Append(BuilderConstants.UxmlOpenTagSymbol);

                if (engineNamespaceDefinition != UxmlNamespaceDefinition.Empty)
                {
                    stringBuilder.Append(string.IsNullOrEmpty(engineNamespaceDefinition.prefix)
                        ? BuilderConstants.UxmlTemplateClassTag
                        : $"{engineNamespaceDefinition.prefix}:{BuilderConstants.UxmlTemplateClassTag}");
                }
                else
                    stringBuilder.Append($"{BuilderConstants.UxmlEngineNamespace}.{BuilderConstants.UxmlTemplateClassTag}");

                AppendElementAttribute(BuilderConstants.UxmlNameAttr, templateAlias, stringBuilder);

                var fieldInfo = VisualTreeAssetExtensions.UsingsListFieldInfo;
                if (fieldInfo != null)
                {
                    var usings = fieldInfo.GetValue(vta) as List<VisualTreeAsset.UsingEntry>;
                    if (usings != null && usings.Count > 0)
                    {
                        var lookingFor = new VisualTreeAsset.UsingEntry(templateAlias, string.Empty);
                        int index = usings.BinarySearch(lookingFor, VisualTreeAsset.UsingEntry.comparer);
                        if (index >= 0)
                        {
                            var usingEntry = usings[index];

                            var path = GetProcessedPathForSrcAttribute(usingEntry.asset, vtaPath, usingEntry.path);
                            AppendElementAttribute("src", path, stringBuilder);
                        }
                    }
                }
                else
                {
                    Debug.LogError("UI Builder: VisualTreeAsset.m_Usings field has not been found! Update the reflection code!");
                }
                stringBuilder.Append(BuilderConstants.UxmlEndTagSymbol);
                stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);
            }

        }

        static void GatherUsedTemplates(
            VisualTreeAsset vta, VisualElementAsset root,
            Dictionary<int, List<VisualElementAsset>> idToChildren,
            HashSet<string> templates)
        {
            if (root is TemplateAsset)
                templates.Add((root as TemplateAsset).templateAlias);

            // Iterate through child elements.
            List<VisualElementAsset> children;
            if (idToChildren != null && idToChildren.TryGetValue(root.id, out children) && children.Count > 0)
            {
                foreach (VisualElementAsset childVea in children)
                    GatherUsedTemplates(vta, childVea, idToChildren, templates);
            }
        }

        public static string GetProcessedPathForSrcAttribute(Object asset, string vtaPath, string assetPath)
        {
            if (asset)
                return URIHelpers.MakeAssetUri(asset);

            if (string.IsNullOrEmpty(assetPath))
                return assetPath;

            var result = string.Empty;
            if (!string.IsNullOrEmpty(vtaPath))
            {
                var vtaDir = Path.GetDirectoryName(vtaPath);
                vtaDir = vtaDir.Replace('\\', '/');
                vtaDir += "/";

                var assetPathDir = Path.GetDirectoryName(assetPath);
                assetPathDir = assetPathDir.Replace('\\', '/');
                assetPathDir += "/";

                if (assetPathDir.StartsWith(vtaDir))
                    result = assetPath.Substring(vtaDir.Length); // +1 for the /
            }

            if (string.IsNullOrEmpty(result))
                result = "/" + assetPath;

            return result;
        }

        static void ProcessStyleSheetPath(
            string vtaPath,
            StyleSheet styleSheet, string styleSheetPath, StringBuilder stringBuilder, int depth,
            ref bool newLineAdded, ref bool hasChildTags)
        {
            if (!newLineAdded)
            {
                stringBuilder.Append(BuilderConstants.UxmlCloseTagSymbol);
                stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);
                newLineAdded = true;
            }

            Indent(stringBuilder, depth + 1);
            stringBuilder.Append("<Style");
            {
                styleSheetPath = GetProcessedPathForSrcAttribute(styleSheet, vtaPath, styleSheetPath);
                AppendElementAttribute("src", styleSheetPath, stringBuilder);
            }
            stringBuilder.Append(BuilderConstants.UxmlEndTagSymbol);
            stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);

            hasChildTags = true;
        }

        static void GenerateUXMLRecursive(
            VisualTreeAsset vta, string vtaPath, UxmlAsset root,
            Dictionary<int, List<VisualElementAsset>> idToChildren,
            StringBuilder stringBuilder, int depth, bool writingToFile)
        {
            Indent(stringBuilder, depth);

            stringBuilder.Append(BuilderConstants.UxmlOpenTagSymbol);
            AppendElementTypeName(vta, root, stringBuilder);

            // If we have no children, avoid adding the full end tag and just end the open tag.
            bool hasChildTags = false;
            if (root is VisualElementAsset vea)
            {
                // Add all non-style attributes.
                AppendElementNonStyleAttributes(vea, stringBuilder, writingToFile);

                // Add style classes to class attribute.
                if (vea.classes != null && vea.classes.Length > 0)
                {
                    stringBuilder.Append(" class=\"");
                    for (int i = 0; i < vea.classes.Length; i++)
                    {
                        if (i > 0)
                            stringBuilder.Append(" ");

                        stringBuilder.Append(vea.classes[i]);
                    }
                    stringBuilder.Append("\"");
                }

                // Add inline StyleSheet attribute.
                if (vea.ruleIndex != -1)
                {
                    if (vta.inlineSheet == null)
                        Debug.LogWarning("VisualElementAsset has a RuleIndex but no inlineStyleSheet");
                    else
                    {
                        StyleRule r = vta.inlineSheet.rules[vea.ruleIndex];

                        if (r.properties != null && r.properties.Length > 0)
                        {
                            var ruleBuilder = new StringBuilder();
                            var exportOptions = new UssExportOptions();
                            exportOptions.propertyIndent = string.Empty;
                            StyleSheetToUss.ToUssString(vta.inlineSheet, exportOptions, r, ruleBuilder);
                            var ruleStr = ruleBuilder.ToString();

                            // Need to remove newlines here before we give it to
                            // AppendElementAttribute() so we don't add "&#10;" everywhere.
                            ruleStr = ruleStr.Replace("\n", " ");
                            ruleStr = ruleStr.Replace("\r", "");
                            ruleStr = ruleStr.Trim();

                            AppendElementAttribute("style", ruleStr, stringBuilder);
                        }
                    }
                }

                // Add special children.
                var styleSheets = vea.GetStyleSheets();
                var styleSheetPaths = vea.GetStyleSheetPaths();

                if (styleSheetPaths != null && styleSheetPaths.Count > 0)
                {
                    Assert.IsNotNull(styleSheets);
                    Assert.AreEqual(styleSheetPaths.Count, styleSheets.Count);

                    bool newLineAdded = false;

                    for (var i = 0; i < styleSheetPaths.Count; ++i)
                    {
                        var styleSheet = styleSheets[i];
                        var styleSheetPath = styleSheetPaths[i];
                        ProcessStyleSheetPath(
                            vtaPath,
                            styleSheet, styleSheetPath, stringBuilder, depth,
                            ref newLineAdded, ref hasChildTags);
                    }
                }
            }
            else
            {
                AppendElementAttributes(root, stringBuilder, writingToFile);
            }

            var templateAsset = root as TemplateAsset;
            if (templateAsset != null && templateAsset.attributeOverrides.Count > 0)
            {
                if (!hasChildTags)
                {
                    stringBuilder.Append(BuilderConstants.UxmlCloseTagSymbol);
                    stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);
                }

                var overridesMap = new Dictionary<string, List<TemplateAsset.AttributeOverride>>();
                foreach (var attributeOverride in templateAsset.attributeOverrides)
                {
                    if (!overridesMap.ContainsKey(attributeOverride.m_ElementName))
                        overridesMap.Add(attributeOverride.m_ElementName, new List<TemplateAsset.AttributeOverride>());

                    overridesMap[attributeOverride.m_ElementName].Add(attributeOverride);
                }
                foreach (var attributeOverridePair in overridesMap)
                {
                    var elementName = attributeOverridePair.Key;
                    var overrides = attributeOverridePair.Value;

                    Indent(stringBuilder, depth + 1);
                    stringBuilder.Append(BuilderConstants.UxmlOpenTagSymbol + "AttributeOverrides");
                    AppendElementAttribute("element-name", elementName, stringBuilder);

                    foreach (var attributeOverride in overrides)
                        AppendElementAttribute(attributeOverride.m_AttributeName, attributeOverride.m_Value, stringBuilder);

                    stringBuilder.Append(BuilderConstants.UxmlEndTagSymbol);
                    stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);
                }

                hasChildTags = true;
            }

            // Iterate through child elements.
            List<VisualElementAsset> children;
            if (idToChildren != null && idToChildren.TryGetValue(root.id, out children) && children.Count > 0)
            {
                if (!hasChildTags)
                {
                    stringBuilder.Append(BuilderConstants.UxmlCloseTagSymbol);
                    stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);
                }

                children.Sort(VisualTreeAssetUtilities.CompareForOrder);

                foreach (var childVea in children)
                    GenerateUXMLRecursive(
                        vta, vtaPath, childVea, idToChildren, stringBuilder,
                        depth + 1, writingToFile);

                hasChildTags = true;
            }

            // Iterate through Uxml Objects
            var entry = vta.GetUxmlObjectEntry(root.id);
            if (entry.uxmlObjectAssets != null && entry.uxmlObjectAssets.Count > 0)
            {
                if (!hasChildTags)
                {
                    stringBuilder.Append(BuilderConstants.UxmlCloseTagSymbol);
                    stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);
                }

                foreach (var childAsset in entry.uxmlObjectAssets)
                {
                    GenerateUXMLRecursive(
                        vta, vtaPath, childAsset, idToChildren, stringBuilder,
                        depth + 1, writingToFile);
                }

                hasChildTags = true;
            }

            if (hasChildTags)
            {
                Indent(stringBuilder, depth);
                stringBuilder.Append(BuilderConstants.UxmlOpenTagSymbol + "/");
                AppendElementTypeName(vta, root, stringBuilder);
                stringBuilder.Append(BuilderConstants.UxmlCloseTagSymbol);
                stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);
            }
            else
            {
                stringBuilder.Append(BuilderConstants.UxmlEndTagSymbol);
                stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);
            }
        }

        public static string GenerateUXML(VisualTreeAsset vta, string vtaPath, List<VisualElementAsset> veas)
        {
            var stringBuilder = new StringBuilder();

            var rootElement = vta.GetRootUxmlElement();
            var engineNamespaceDefinition = vta.FindUxmlNamespaceDefinitionForTypeName(rootElement, typeof(VisualElement).FullName);

            if (engineNamespaceDefinition != UxmlNamespaceDefinition.Empty)
            {
                stringBuilder.Append(string.IsNullOrEmpty(engineNamespaceDefinition.prefix)
                    ? "<UXML"
                    : $"<{engineNamespaceDefinition.prefix}:UXML");
            }
            else
                stringBuilder.Append("<UXML");

            AppendHeaderAttributes(vta, stringBuilder, false);

            using var setHandle = HashSetPool<UxmlNamespaceDefinition>.Get(out var definitionsSet);
            {
                for (var i = 0; i < rootElement.namespaceDefinitions.Count; ++i)
                {
                    definitionsSet.Add(rootElement.namespaceDefinitions[i]);
                }

                foreach (var vea in veas)
                {
                    using var listHandle = ListPool<UxmlNamespaceDefinition>.Get(out var definitions);

                    vta.GatherUxmlNamespaceDefinitions(vea, definitions);

                    for (var i = 0; i < definitions.Count; ++i)
                    {
                        var definition = definitions[i];
                        if (definitionsSet.Add(definition))
                        {
                            AppendNamespaceDefinition(definition, stringBuilder);
                        }
                    }
                }
            }

            stringBuilder.Append(BuilderConstants.UxmlCloseTagSymbol);
            stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);

            var idToChildren = VisualTreeAssetUtilities.GenerateIdToChildren(vta);

            var usedTemplates = new HashSet<string>();

            foreach (var vea in veas)
            {
                // Templates
                GatherUsedTemplates(vta, vea, idToChildren, usedTemplates);
            }

            AppendTemplateRegistrations(vta, vtaPath, stringBuilder, usedTemplates);

            foreach (var vea in veas)
            {
                GenerateUXMLRecursive(vta, vtaPath, vea, idToChildren, stringBuilder, 1, true);
            }

            if (engineNamespaceDefinition != UxmlNamespaceDefinition.Empty)
            {
                stringBuilder.Append(string.IsNullOrEmpty(engineNamespaceDefinition.prefix)
                    ? "</UXML>"
                    : $"</{engineNamespaceDefinition.prefix}:UXML>");
            }
            else
                stringBuilder.Append("</UXML>");
            stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);

            return stringBuilder.ToString();
        }

        static void GenerateUXMLFromRootElements(
            VisualTreeAsset vta,
            Dictionary<int, List<VisualElementAsset>> idToChildren,
            StringBuilder stringBuilder,
            string vtaPath,
            bool writingToFile)
        {
            List<VisualElementAsset> rootAssets;

            // Tree root has parentId == 0
            idToChildren.TryGetValue(0, out rootAssets);
            if (rootAssets == null || rootAssets.Count == 0)
                return;

            var uxmlRootAsset = rootAssets[0];

            bool tempHasChildTags = false;
            var styleSheets = uxmlRootAsset.GetStyleSheets();
            var styleSheetPaths = uxmlRootAsset.GetStyleSheetPaths();

            if (styleSheetPaths != null && styleSheetPaths.Count > 0)
            {
                Assert.IsNotNull(styleSheets);
                Assert.AreEqual(styleSheetPaths.Count, styleSheets.Count);

                bool newLineAdded = true;

                for (var i = 0; i < styleSheetPaths.Count; ++i)
                {
                    var styleSheet = styleSheets[i];
                    var styleSheetPath = styleSheetPaths[i];
                    ProcessStyleSheetPath(
                        vtaPath,
                        styleSheet, styleSheetPath, stringBuilder, 0,
                        ref newLineAdded, ref tempHasChildTags);
                }
            }

            // Get the first-level elements. These will be instantiated and added to target.
            idToChildren.TryGetValue(uxmlRootAsset.id, out rootAssets);
            if (rootAssets == null || rootAssets.Count == 0)
                return;

            rootAssets.Sort(VisualTreeAssetUtilities.CompareForOrder);
            foreach (VisualElementAsset rootElement in rootAssets)
            {
                Assert.IsNotNull(rootElement);

                // Don't try to include the special selection tracking element.
                if (writingToFile && rootElement.fullTypeName == BuilderConstants.SelectedVisualTreeAssetSpecialElementTypeName)
                    continue;

                GenerateUXMLRecursive(vta, vtaPath, rootElement, idToChildren, stringBuilder, 1, writingToFile);
            }
        }

        public static string GenerateUXML(VisualTreeAsset vta, string vtaPath, bool writingToFile = false)
        {
            var stringBuilder = new StringBuilder();

            if (vta.visualElementAssets is not {Count: > 0} &&
                vta.templateAssets is not {Count: > 0})
            {
                stringBuilder.Append($"{BuilderConstants.UxmlOpenTagSymbol}{BuilderConstants.UxmlDefaultEngineNamespacePrefix}:{BuilderConstants.UxmlEngineNamespace}{BuilderConstants.UxmlCloseTagSymbol}");
                stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);

                stringBuilder.Append($"{BuilderConstants.UxmlOpenTagSymbol}/{BuilderConstants.UxmlDefaultEngineNamespacePrefix}:{BuilderConstants.UxmlEngineNamespace}{BuilderConstants.UxmlCloseTagSymbol}");
                stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);
                return stringBuilder.ToString();
            }

            var idToChildren = VisualTreeAssetUtilities.GenerateIdToChildren(vta);

            stringBuilder.Append(BuilderConstants.UxmlOpenTagSymbol);
            AppendElementTypeName(vta, vta.GetRootUxmlElement(), stringBuilder);
            AppendHeaderAttributes(vta, stringBuilder, writingToFile);
            stringBuilder.Append(BuilderConstants.UxmlCloseTagSymbol);
            stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);

            // Templates
            AppendTemplateRegistrations(vta, vtaPath, stringBuilder);

            GenerateUXMLFromRootElements(vta, idToChildren, stringBuilder, vtaPath, writingToFile);

            stringBuilder.Append(BuilderConstants.UxmlOpenTagSymbol);
            stringBuilder.Append("/");
            AppendElementTypeName(vta, vta.GetRootUxmlElement(), stringBuilder);
            stringBuilder.Append(BuilderConstants.UxmlCloseTagSymbol);
            stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);

            return stringBuilder.ToString();
        }
    }
}
