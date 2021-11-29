using System.Collections.Generic;
using System.Text;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor.UIElements.StyleSheets;

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

        static void AppendElementTypeName(VisualElementAsset root, StringBuilder stringBuilder)
        {
            if (root is TemplateAsset)
            {
                stringBuilder.Append(BuilderConstants.UxmlEngineNamespaceReplace);
                stringBuilder.Append("Instance");
                return;
            }

            var typeName = root.fullTypeName;
            if (typeName.StartsWith(BuilderConstants.UxmlEngineNamespace))
            {
                typeName = typeName.Substring(BuilderConstants.UxmlEngineNamespace.Length);
                stringBuilder.Append(BuilderConstants.UxmlEngineNamespaceReplace);
                stringBuilder.Append(typeName);
                return;
            }
            else if (typeName.StartsWith(BuilderConstants.UxmlEditorNamespace))
            {
                typeName = typeName.Substring(BuilderConstants.UxmlEditorNamespace.Length);
                stringBuilder.Append(BuilderConstants.UxmlEditorNamespaceReplace);
                stringBuilder.Append(typeName);
                return;
            }

            stringBuilder.Append(typeName);
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
            AppendElementAttributes(vta.GetRootUXMLElement(), stringBuilder, writingToFile, "ui", "uie");
        }

        static void AppendElementAttributes(VisualElementAsset vea, StringBuilder stringBuilder, bool writingToFile, params string[] ignoredAttributes)
        {
            var fieldInfo = VisualElementAssetExtensions.AttributesListFieldInfo;
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

                        var templateAlias = uxmlObjectAsset.GetAttributeValue(Column.UxmlObjectTraits<Column>.k_HeaderTemplateAttributeName);

                        if (!string.IsNullOrEmpty(templateAlias) && !templateAliases.Contains(templateAlias))
                            templateAliases.Add(templateAlias);

                        templateAlias = uxmlObjectAsset.GetAttributeValue(Column.UxmlObjectTraits<Column>.k_CellTemplateAttributeName);

                        if (!string.IsNullOrEmpty(templateAlias) && !templateAliases.Contains(templateAlias))
                            templateAliases.Add(templateAlias);
                    }
                }
            }

            foreach (var templateAlias in templateAliases)
            {
                // Skip templates if not in filter.
                if (templatesFilter != null && !templatesFilter.Contains(templateAlias))
                    continue;

                Indent(stringBuilder, 1);
                stringBuilder.Append(BuilderConstants.UxmlOpenTagSymbol);
                stringBuilder.Append(BuilderConstants.UxmlEngineNamespaceReplace);
                stringBuilder.Append(BuilderConstants.UxmlTemplateClassTag);
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
                stringBuilder.Append(" " + BuilderConstants.UxmlEndTagSymbol);
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
            stringBuilder.Append(" " + BuilderConstants.UxmlEndTagSymbol);
            stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);

            hasChildTags = true;
        }

        static void GenerateUXMLRecursive(
            VisualTreeAsset vta, string vtaPath, VisualElementAsset root,
            Dictionary<int, List<VisualElementAsset>> idToChildren,
            StringBuilder stringBuilder, int depth, bool writingToFile)
        {
            Indent(stringBuilder, depth);

            stringBuilder.Append(BuilderConstants.UxmlOpenTagSymbol);
            AppendElementTypeName(root, stringBuilder);

            // Add all non-style attributes.
            AppendElementNonStyleAttributes(root, stringBuilder, writingToFile);

            // Add style classes to class attribute.
            if (root.classes != null && root.classes.Length > 0)
            {
                stringBuilder.Append(" class=\"");
                for (int i = 0; i < root.classes.Length; i++)
                {
                    if (i > 0)
                        stringBuilder.Append(" ");

                    stringBuilder.Append(root.classes[i]);
                }
                stringBuilder.Append("\"");
            }

            // Add inline StyleSheet attribute.
            if (root.ruleIndex != -1)
            {
                if (vta.inlineSheet == null)
                    Debug.LogWarning("VisualElementAsset has a RuleIndex but no inlineStyleSheet");
                else
                {
                    StyleRule r = vta.inlineSheet.rules[root.ruleIndex];

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

            // If we have no children, avoid adding the full end tag and just end the open tag.
            bool hasChildTags = false;

            // Add special children.
            var styleSheets = root.GetStyleSheets();
            var styleSheetPaths = root.GetStyleSheetPaths();

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

            var templateAsset = root as TemplateAsset;
            if (templateAsset != null && templateAsset.attributeOverrides != null && templateAsset.attributeOverrides.Count > 0)
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

                    stringBuilder.Append(" " + BuilderConstants.UxmlCloseTagSymbol);
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
                
                foreach (var childVea in entry.uxmlObjectAssets)
                    GenerateUXMLRecursive(
                        vta, vtaPath, childVea, idToChildren, stringBuilder,
                        depth + 1, writingToFile);

                hasChildTags = true;
            }

            if (hasChildTags)
            {
                Indent(stringBuilder, depth);
                stringBuilder.Append(BuilderConstants.UxmlOpenTagSymbol + "/");
                AppendElementTypeName(root, stringBuilder);
                stringBuilder.Append(BuilderConstants.UxmlCloseTagSymbol);
                stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);
            }
            else
            {
                stringBuilder.Append(" " + BuilderConstants.UxmlEndTagSymbol);
                stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);
            }
        }

        public static string GenerateUXML(VisualTreeAsset vta, string vtaPath, List<VisualElementAsset> veas)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(BuilderConstants.UxmlHeader);
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

            stringBuilder.Append(BuilderConstants.UxmlFooter);
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

            if ((vta.visualElementAssets == null || vta.visualElementAssets.Count <= 0) &&
                (vta.templateAssets == null || vta.templateAssets.Count <= 0))
            {
                stringBuilder.Append(BuilderConstants.UxmlHeader);
                stringBuilder.Append(BuilderConstants.UxmlCloseTagSymbol);
                stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);
                stringBuilder.Append(BuilderConstants.UxmlFooter);
                stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);
                return stringBuilder.ToString();
            }

            var idToChildren = VisualTreeAssetUtilities.GenerateIdToChildren(vta);

            stringBuilder.Append(BuilderConstants.UxmlHeader);
            AppendHeaderAttributes(vta, stringBuilder, writingToFile);
            stringBuilder.Append(BuilderConstants.UxmlCloseTagSymbol);
            stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);

            // Templates
            AppendTemplateRegistrations(vta, vtaPath, stringBuilder);

            GenerateUXMLFromRootElements(vta, idToChildren, stringBuilder, vtaPath, writingToFile);

            stringBuilder.Append(BuilderConstants.UxmlFooter);
            stringBuilder.Append(BuilderConstants.newlineCharFromEditorSettings);

            return stringBuilder.ToString();
        }
    }
}
