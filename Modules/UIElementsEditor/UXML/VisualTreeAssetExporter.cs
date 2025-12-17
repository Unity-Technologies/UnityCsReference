// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal class VisualTreeAssetExporter
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class ExportOptions
    {
        private StyleSheetExporter m_Exporter;

        public string indent { get; set; } = "    ";

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal string[] ignoreAttributeList { get; set; } = Array.Empty<string>();

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal string[] ignoreTypeList { get; set; } = Array.Empty<string>();

        public StyleSheetExporter styleExporter
        {
            get => m_Exporter ??= new StyleSheetExporter();
            set => m_Exporter = value;
        }

        public StyleSheetExporter.UssExportOptions styleExporterOptions { get; set; }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool IsAttributeIgnored(string attributeName)
        {
            if (ignoreAttributeList == null)
                return false;
            return Array.IndexOf(ignoreAttributeList, attributeName) >= 0;
        }

        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal bool IsTypeIgnored(string typeName)
        {
            if (ignoreTypeList == null)
                return false;
            return Array.IndexOf(ignoreTypeList, typeName) >= 0;
        }
    }

    public struct ExportContext
    {
        private readonly StringBuilder m_Builder;
        private readonly VisualTreeAsset m_VisualTreeAsset;
        private readonly ExportOptions m_Options;

        private int m_IndentLevel = 0;

        internal ExportContext(VisualTreeAsset visualTreeAsset, StringBuilder builder, ExportOptions options = null)
        {
            m_VisualTreeAsset = visualTreeAsset;
            m_Builder = builder;
            m_Options = options ?? new ExportOptions();
        }

        public VisualTreeAsset visualTreeAsset => m_VisualTreeAsset;

        public ExportOptions options => m_Options;

        public StyleSheetExporter styleExporter => m_Options.styleExporter;
        public StyleSheetExporter.UssExportOptions styleExporterOptions => m_Options.styleExporterOptions;

        public void IncreaseIndent()
        {
            ++m_IndentLevel;
        }

        public void DecreaseIndent()
        {
            if (m_IndentLevel == 0)
                throw new InvalidOperationException("Cannot decrease indent level");

            --m_IndentLevel;
        }

        public void Append(char c)
        {
            m_Builder.Append(c);
        }

        public void AppendLine(char c)
        {
            m_Builder.Append(c);
            m_Builder.Append(UXMLConstants.newlineCharFromEditorSettings);
        }

        public void Append(string str)
        {
            m_Builder.Append(str);
        }

        public void AppendLine()
        {
            m_Builder.Append(UXMLConstants.newlineCharFromEditorSettings);
        }

        public void AppendLine(string str)
        {
            m_Builder.Append(str);
            m_Builder.Append(UXMLConstants.newlineCharFromEditorSettings);
        }

        public void AppendIndent()
        {
            for (var i = 0; i < m_IndentLevel; ++i)
                m_Builder.Append(options.indent);
        }

        public UxmlNamespaceDefinition GetNamespaceDefinition(UxmlAsset asset)
        {
            if (asset.xmlNamespace == UxmlNamespaceDefinition.Empty)
                return asset.xmlNamespace;

            var namespaceDefinition = visualTreeAsset.FindUxmlNamespaceDefinitionFromPrefix(asset, asset.xmlNamespace.prefix);
            return namespaceDefinition != asset.xmlNamespace
                ? visualTreeAsset.FindUxmlNamespaceDefinitionForTypeName(asset, asset.fullTypeName)
                : namespaceDefinition;
        }

        public string GetProcessedPathForSrcAttribute(UnityEngine.Object asset)
        {
            return asset != null ? URIHelpers.MakeAssetUri(asset) : string.Empty;
        }
    }

    /// <summary>
    /// Converts the provided <see cref="VisualTreeAsset"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="visualTreeAsset">The stylesheet to export.</param>
    /// <param name="options">To export options.</param>
    /// <returns>A <see cref="string"/> version of the <see cref="VisualTreeAsset"/>.</returns>
    public string ToUxmlString(VisualTreeAsset visualTreeAsset, ExportOptions options = null)
    {
        using var _ = StringBuilderPool.Get(out var stringBuilder);
        var context = new ExportContext(visualTreeAsset, stringBuilder, options);
        WriteVisualTreeAsset(ref context, visualTreeAsset);
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Converts the provided <see cref="VisualTreeAsset"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="visualTreeAsset">The stylesheet to export.</param>
    /// <param name="roots">Sub-set of elements to export.</param>
    /// <param name="options">To export options.</param>
    /// <returns>A <see cref="string"/> version of the <see cref="VisualTreeAsset"/>.</returns>
    public string ToUxmlString(VisualTreeAsset visualTreeAsset, List<UxmlAsset> roots, ExportOptions options = null)
    {
        using var _ = StringBuilderPool.Get(out var stringBuilder);
        var context = new ExportContext(visualTreeAsset, stringBuilder, options);
        WriteUxmlAsset(ref context, visualTreeAsset.visualTree, roots);
        return stringBuilder.ToString();
    }

    protected virtual void WriteVisualTreeAsset(ref ExportContext ctx, VisualTreeAsset visualTreeAsset)
    {
        WriteRootElement(ref ctx, visualTreeAsset.visualTree);
    }

    private (string prefix, string typename) GetAssetNameAndPrefix(UxmlAsset asset, in UxmlNamespaceDefinition namespaceDefinition)
    {
        if (asset is UxmlObjectAsset { isField: true })
        {
            return (string.Empty, asset.fullTypeName);
        }

        var fullTypename = asset.fullTypeName;
        var typename = !string.IsNullOrEmpty(namespaceDefinition.resolvedNamespace) ? fullTypename[(namespaceDefinition.resolvedNamespace.Length + 1)..] : fullTypename;
        return (namespaceDefinition.prefix, typename);
    }


    protected void WriteRootElement(ref ExportContext ctx, VisualElementAsset visualTree)
    {
        Assert.IsTrue(ctx.visualTreeAsset.visualTree == visualTree);
        WriteUxmlAsset(ref ctx, visualTree);
    }

    protected void WriteUxmlAsset(ref ExportContext ctx, UxmlAsset asset)
    {
        using (ListPool<UxmlAsset>.Get(out var children))
        {
            asset.GetChildren(children);
            WriteUxmlAsset(ref ctx, asset, children);
        }
    }

    protected void WriteUxmlAsset(ref ExportContext ctx, UxmlAsset asset, List<UxmlAsset> children)
    {
        var vta = ctx.visualTreeAsset;
        var namespacePrefix = ctx.GetNamespaceDefinition(asset);

        ctx.AppendIndent();
        ctx.Append('<');

        var resolvedTypename = GetAssetNameAndPrefix(asset, in namespacePrefix);

        WriteElementTypeName(ref ctx, resolvedTypename.prefix, resolvedTypename.typename);
        WriteXmlNamespaces(ref ctx, asset.namespaceDefinitions);
        WriteProperties(ref ctx, asset.properties);

        if (asset is VisualElementAsset vea)
        {
            WriteClasses(ref ctx, vea.classes);
            WriteInlineStyles(ref ctx, vta.inlineSheet, vea.ruleIndex);
            WriteSlots(ref ctx, vea);
        }

        var hasChildren = ExportsChildrenNodes(asset, ctx.options);
        if (!hasChildren)
        {
            ctx.AppendLine("/>");
            return;
        }
        ctx.AppendLine('>');

        ctx.IncreaseIndent();

        if (vta.visualTree == asset)
        {
            WriteTemplates(ref ctx, vta.usings);
        }

        if (asset is VisualElementAsset stylableAsset)
        {
            WriteStyleSheets(ref ctx, stylableAsset.stylesheets);
        }

        if (asset is TemplateAsset template && template.hasAttributeOverride)
        {
            WriteAttributeOverrides(ref ctx, template.attributeOverrides);
        }

        WriteChildren(ref ctx, children);

        ctx.DecreaseIndent();
        ctx.AppendIndent();
        ctx.Append("</");
        WriteElementTypeName(ref ctx, resolvedTypename.prefix, resolvedTypename.typename);
        ctx.AppendLine('>');
    }

    protected void WriteTemplates(ref ExportContext ctx, List<VisualTreeAsset.UsingEntry> usingEntries)
    {
        for (var i = 0; i < usingEntries.Count; ++i)
        {
            var usingEntry = usingEntries[i];
            WriteTemplate(ref ctx, usingEntry);
        }
    }

    protected void WriteTemplate(ref ExportContext ctx, VisualTreeAsset.UsingEntry usingEntry)
    {
        ctx.AppendIndent();
        var vta = ctx.visualTreeAsset;
        var namespaceDefinition = vta.FindUxmlNamespaceDefinitionForTypeName(vta.visualTree, typeof(VisualElement).FullName);

        ctx.Append('<');
        WriteElementTypeName(ref ctx, "UnityEngine.UIElements.Template", namespaceDefinition);
        ctx.Append(" ");
        WriteProperty(ref ctx, "name", usingEntry.alias);
        ctx.Append(" ");
        WriteProperty(ref ctx, "src", ctx.GetProcessedPathForSrcAttribute(usingEntry.asset));
        ctx.AppendLine("/>");
    }

    protected void WriteStyleSheets(ref ExportContext ctx, List<StyleSheet> styleSheets)
    {
        for (var i = 0; i < styleSheets.Count; ++i)
        {
            var styleSheet = styleSheets[i];
            ctx.AppendIndent();
            WriteStyleSheet(ref ctx, styleSheet);
            ctx.AppendLine();
        }
    }

    protected void WriteStyleSheet(ref ExportContext ctx, StyleSheet styleSheet)
    {
        var path = ctx.GetProcessedPathForSrcAttribute(styleSheet);
        ctx.Append("<Style");
        ctx.Append(' ');
        WriteProperty(ref ctx, "src", path);
        ctx.Append("/>");
    }

    protected void WriteAttributeOverrides(ref ExportContext ctx, List<TemplateAsset.AttributeOverride> attributeOverrides)
    {
        var overridesMap = new Dictionary<string, List<TemplateAsset.AttributeOverride>>();
        foreach (var attributeOverride in attributeOverrides)
        {
            if (!overridesMap.ContainsKey(attributeOverride.m_ElementName))
                overridesMap.Add(attributeOverride.m_ElementName, new List<TemplateAsset.AttributeOverride>());

            overridesMap[attributeOverride.m_ElementName].Add(attributeOverride);
        }
        foreach (var attributeOverridePair in overridesMap)
        {
            var elementName = attributeOverridePair.Key;
            var overrides = attributeOverridePair.Value;

            ctx.AppendIndent();
            ctx.Append("<AttributeOverrides");
            ctx.Append(" ");
            WriteProperty(ref ctx, TemplateAsset.k_AttributeOverrideElementNameAttributeName, elementName);

            for (var index = 0; index < overrides.Count; index++)
            {
                ctx.Append(" ");
                var attributeOverride = overrides[index];
                WriteProperty(ref ctx, attributeOverride.m_AttributeName, attributeOverride.m_Value);
            }

            ctx.AppendLine("/>");
        }
    }

    protected void WriteElementTypeName(ref ExportContext ctx, string fullTypename, UxmlNamespaceDefinition namespaceDefinition)
    {
        var typename = !string.IsNullOrEmpty(namespaceDefinition.resolvedNamespace) ? fullTypename[(namespaceDefinition.resolvedNamespace.Length + 1)..] : fullTypename;

        WriteElementTypeName(ref ctx, namespaceDefinition.prefix, typename);
    }

    protected void WriteElementTypeName(ref ExportContext ctx, string prefix, string typename)
    {
        if (!string.IsNullOrEmpty(prefix))
        {
            ctx.Append(prefix);
            ctx.Append(':');
        }
        ctx.Append(typename);
    }

    protected void WriteProperties(ref ExportContext ctx, List<UxmlProperty> properties)
    {
        if (!(properties?.Count > 0))
            return;

        for(var i = 0; i < properties.Count; ++i)
        {
            var property = properties[i];
            if (ctx.options.IsAttributeIgnored(property.name))
                continue;
            ctx.Append(" ");
            WriteProperty(ref ctx, property.name, property.value);
        }
    }

    protected void WriteProperty(ref ExportContext ctx, string name, string value)
    {
        ctx.Append(name);
        ctx.Append('=');
        ctx.Append('"');
        ctx.Append(URIHelpers.EncodeUri(value));
        ctx.Append('"');
    }

    protected void WriteClasses(ref ExportContext ctx, string[] ussClasses)
    {
        if (ussClasses is { Length: > 0 })
        {
            ctx.Append(" ");
            WriteProperty(ref ctx, "class", string.Join(" ", ussClasses));
        }
    }

    protected void WriteInlineStyles(ref ExportContext ctx, StyleSheet inlineStyleSheet, int ruleIndex)
    {
        // Add inline StyleSheet attribute.
        if (ruleIndex != -1)
        {
            if (inlineStyleSheet == null)
                Debug.LogWarning("VisualElementAsset has a RuleIndex but no inlineStyleSheet");
            else
            {
                var r = inlineStyleSheet.rules[ruleIndex];
                var exportedInlineStyles = ctx.styleExporter.ExportInlineRule(inlineStyleSheet, r, ctx.styleExporterOptions);
                if (!string.IsNullOrEmpty(exportedInlineStyles))
                {
                    ctx.Append(" ");
                    WriteProperty(ref ctx, "style",  exportedInlineStyles);
                }
            }
        }
    }

    protected void WriteSlots(ref ExportContext ctx, VisualElementAsset vea)
    {
        var veaId = vea.id;
        if (ctx.visualTreeAsset.TryGetSlotInsertionPoint(veaId, out var slotName))
        {
            ctx.Append($" slot-name=\"{ slotName }\"");
        }

        if (vea.parentAsset is TemplateAsset { slotUsages: not null } parentTemplateAsset)
        {
            var slotIndex = parentTemplateAsset.slotUsages.FindIndex(su => su.assetId == veaId);
            if (slotIndex >= 0)
            {
                ctx.Append($" slot=\"{ parentTemplateAsset.slotUsages[slotIndex].slotName }\"");
            }
        }
    }

    protected void WriteXmlNamespaces(ref ExportContext ctx, List<UxmlNamespaceDefinition> xmlNamespaces)
    {
        for (var i = 0; i < xmlNamespaces?.Count; ++i)
        {
            var xmlNamespace = xmlNamespaces[i];
            if (xmlNamespace != UxmlNamespaceDefinition.Empty)
            {
                ctx.Append(" ");
                WriteXmlNamespace(ref ctx, xmlNamespace);
            }
        }
    }

    protected void WriteXmlNamespace(ref ExportContext ctx, UxmlNamespaceDefinition xmlNamespace)
    {
        if (string.IsNullOrEmpty(xmlNamespace.prefix))
        {
            ctx.Append("xmlns=");
            ctx.Append('"');
            ctx.Append(xmlNamespace.resolvedNamespace);
            ctx.Append('"');
        }
        else
        {
            ctx.Append("xmlns:");
            ctx.Append(xmlNamespace.prefix);
            ctx.Append("=");
            ctx.Append('"');
            ctx.Append(xmlNamespace.resolvedNamespace);
            ctx.Append('"');
        }
    }

    protected void WriteChildren(ref ExportContext ctx, List<UxmlAsset> children)
    {
        for (var i = 0; i < children.Count; ++i)
        {
            var child = children[i];
            if (ctx.options.IsTypeIgnored(child.fullTypeName))
                continue;
            WriteUxmlAsset(ref ctx, child);
        }
    }

    protected bool ExportsChildrenNodes(UxmlAsset asset, ExportOptions options)
    {
        var childCount = 0;
        for (var i = 0; i < asset.childCount; ++i)
        {
            var child = asset[i];
            if (!options.IsTypeIgnored(child.fullTypeName))
                ++childCount;
        }

        switch (asset)
        {
            case TemplateAsset ta:
                if (childCount > 0 || ta.stylesheets.Count > 0 || ta.hasAttributeOverride)
                    return true;
                break;
            case VisualElementAsset vea:
                if (childCount > 0 || vea.stylesheets.Count > 0)
                    return true;

                if (vea.isRoot) //  && vea.visualTreeAsset.usings.Count > 0)
                    return true;
                break;
            case UxmlObjectAsset oa:
                if (childCount > 0)
                    return true;
                break;
        }

        return false;
    }
}
