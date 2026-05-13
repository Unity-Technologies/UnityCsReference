// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements;

[VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
internal partial class VisualTreeAssetExporter
{
    [OnCodeInitializing]
    static void Init()
    {
        // Intentionally left empty to trigger the `UIPrefColor` registration.
    }

    public const string ColorsPreferenceCategory = "UXML Syntax Highlighting";

    static readonly UIPrefColor k_AttributeName = new(ColorsPreferenceCategory, "Attribute Name", HtmlColor("#098658"), HtmlColor("#5DA861"));
    static readonly UIPrefColor k_AttributeValue = new(ColorsPreferenceCategory, "Attribute Value", HtmlColor("#A15000"), HtmlColor("#E0BD73"));
    static readonly UIPrefColor k_Tag = new(ColorsPreferenceCategory, "Tag", HtmlColor("#0033B3"), HtmlColor("#B464EB"));
    static readonly UIPrefColor k_TagName = new(ColorsPreferenceCategory, "Tag Name", HtmlColor("#0033B3"), HtmlColor("#C26CFD"));

    public static Color AttributeNameColor { get => k_AttributeName.Color; set { k_AttributeName.Color = value; PrefSettings.Set(k_AttributeName.StorageKey, k_AttributeName); } }
    public static Color AttributeValueColor { get => k_AttributeValue.Color; set { k_AttributeValue.Color = value; PrefSettings.Set(k_AttributeValue.StorageKey, k_AttributeValue); } }
    public static Color TagColor { get => k_Tag.Color; set { k_Tag.Color = value; PrefSettings.Set(k_Tag.StorageKey, k_Tag); } }
    public static Color TagNameColor { get => k_TagName.Color; set { k_TagName.Color = value; PrefSettings.Set(k_TagName.StorageKey, k_TagName); } }

    const string SelectedVisualElementAssetAttributeName = "__unity-builder-selected-element";
    internal static readonly string[] IgnoredAttributesWhenExporting = { SelectedVisualElementAssetAttributeName };

    public static readonly string SelectedVisualTreeAssetSpecialElementTypeName =
        "Unity.UI.Builder.UnityUIBuilderSelectionMarker";

    internal static readonly string[] IgnoredTypesWhenExporting = { SelectedVisualTreeAssetSpecialElementTypeName };

    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal struct ExportOptions
    {
        const string k_DefaultIndent = "    ";

        public static ExportOptions Default => new()
        {
            indent = k_DefaultIndent,
            ignoreAttributeList = IgnoredAttributesWhenExporting,
            ignoreTypeList = IgnoredTypesWhenExporting,
            consistentAttributeOrder = UIToolkitProjectSettings.consistentAttributeOrderingWhenExporting,
            styleExporterOptions = StyleSheetExporter.UssExportOptions.Default,
            m_UseColorHighlighting = false
        };

        private StyleSheetExporter m_Exporter;

        string m_Indent;
        bool? m_ConsistentAttributeOrder;
        string[] m_IgnoreAttributeList;
        string[] m_IgnoreTypeList;
        bool? m_UseColorHighlighting;

        public string indent
        {
            get => m_Indent ?? k_DefaultIndent;
            set => m_Indent = value;
        }

        public bool consistentAttributeOrder
        {
            get => m_ConsistentAttributeOrder ?? UIToolkitProjectSettings.consistentAttributeOrderingWhenExporting;
            set => m_ConsistentAttributeOrder = value;
        }

        public string[] ignoreAttributeList
        {
            get => m_IgnoreAttributeList ?? Array.Empty<string>();
            set => m_IgnoreAttributeList = value;
        }

        public string[] ignoreTypeList
        {
            get => m_IgnoreTypeList ?? Array.Empty<string>();
            set => m_IgnoreTypeList = value;
        }

        public bool useColorHighlighting
        {
            get => m_UseColorHighlighting.HasValue ? m_UseColorHighlighting.Value : false;
            set => m_UseColorHighlighting = value;
        }

        public StyleSheetExporter styleExporter
        {
            get => m_Exporter ??= new StyleSheetExporter();
            set => m_Exporter = value;
        }

        public StyleSheetExporter.UssExportOptions styleExporterOptions { get; set; }

        public bool IsAttributeIgnored(string attributeName)
        {
            if (ignoreAttributeList == null)
                return false;
            return Array.IndexOf(ignoreAttributeList, attributeName) >= 0;
        }

        public bool IsTypeIgnored(string typeName)
        {
            if (ignoreTypeList == null)
                return false;
            return Array.IndexOf(ignoreTypeList, typeName) >= 0;
        }
    }

    private struct HighlightingScope : IDisposable
    {
        private readonly Color m_Color;

        // Hopefully, one day
        //private ref ExportContext m_Context;
        private ExportContext m_Context;

        public HighlightingScope(ref ExportContext context, Color color)
        {
            m_Color = color;
            // Hopefully, one day
            //m_Context = ref context;
            m_Context = context;
            if (m_Context.options.useColorHighlighting)
            {
                m_Context.Append("<color=#");
                m_Context.Append(ColorUtility.ToHtmlStringRGB(m_Color));
                m_Context.Append(">");
            }
        }

        public void Dispose()
        {
            if (m_Context.options.useColorHighlighting)
            {
                m_Context.Append("</color>");
            }
        }
    }

    public struct ExportContext
    {
        private readonly StringBuilder m_Builder;
        private readonly VisualTreeAsset m_VisualTreeAsset;
        private readonly ExportOptions m_Options;

        private int m_IndentLevel = 0;

        internal ExportContext(VisualTreeAsset visualTreeAsset, StringBuilder builder, ExportOptions options)
        {
            m_VisualTreeAsset = visualTreeAsset;
            m_Builder = builder;
            m_Options = options;
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

            var namespaceDefinition =
                visualTreeAsset.FindUxmlNamespaceDefinitionFromPrefix(asset, asset.xmlNamespace.prefix);
            return namespaceDefinition != asset.xmlNamespace
                ? visualTreeAsset.FindUxmlNamespaceDefinitionForTypeName(asset, asset.fullTypeName)
                : namespaceDefinition;
        }

        public string GetProcessedPathForSrcAttribute(UnityEngine.Object asset)
        {
            return asset != null ? URIHelpers.MakeAssetUri(asset) : string.Empty;
        }
    }

    public static VisualTreeAssetExporter Default { get; } = new();

    /// <summary>
    /// Converts the provided <see cref="VisualTreeAsset"/> to <see cref="string"/> using the default options.
    /// </summary>
    /// <param name="visualTreeAsset">The stylesheet to export.</param>
    /// <returns>A <see cref="string"/> version of the <see cref="VisualTreeAsset"/>.</returns>
    public string ToUxmlString(VisualTreeAsset visualTreeAsset)
        => ToUxmlString(visualTreeAsset, ExportOptions.Default);

    /// <summary>
    /// Converts the provided <see cref="VisualTreeAsset"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="visualTreeAsset">The stylesheet to export.</param>
    /// <param name="options">To export options.</param>
    /// <returns>A <see cref="string"/> version of the <see cref="VisualTreeAsset"/>.</returns>
    public string ToUxmlString(VisualTreeAsset visualTreeAsset, ExportOptions options)
    {
        using var _ = StringBuilderPool.Get(out var stringBuilder);
        var context = new ExportContext(visualTreeAsset, stringBuilder, options);
        WriteVisualTreeAsset(ref context, visualTreeAsset);
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Converts the provided <see cref="VisualTreeAsset"/> to <see cref="string"/> using the default options.
    /// </summary>
    /// <param name="visualTreeAsset">The stylesheet to export.</param>
    /// <param name="roots">Sub-set of elements to export.</param>
    /// <returns>A <see cref="string"/> version of the <see cref="VisualTreeAsset"/>.</returns>
    public string ToUxmlString(VisualTreeAsset visualTreeAsset, List<UxmlAsset> roots)
        => ToUxmlString(visualTreeAsset, roots, ExportOptions.Default);

    /// <summary>
    /// Converts the provided <see cref="VisualTreeAsset"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="visualTreeAsset">The stylesheet to export.</param>
    /// <param name="roots">Sub-set of elements to export.</param>
    /// <param name="options">To export options.</param>
    /// <returns>A <see cref="string"/> version of the <see cref="VisualTreeAsset"/>.</returns>
    public string ToUxmlString(VisualTreeAsset visualTreeAsset, List<UxmlAsset> roots, ExportOptions options)
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

    private (string prefix, string typename) GetAssetNameAndPrefix(UxmlAsset asset,
        in UxmlNamespaceDefinition namespaceDefinition)
    {
        if (asset is UxmlObjectAsset { isField: true })
        {
            return (string.Empty, asset.fullTypeName);
        }

        var fullTypename = asset.fullTypeName;
        var typename = !string.IsNullOrEmpty(namespaceDefinition.resolvedNamespace)
            ? fullTypename[(namespaceDefinition.resolvedNamespace.Length + 1)..]
            : fullTypename;
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

    protected virtual void BeforeWriteUxmlAssetTag(ref ExportContext ctx, UxmlAsset asset)
    {
    }

    protected virtual void AfterWriteUxmlAssetTag(ref ExportContext ctx, UxmlAsset asset)
    {
    }

    protected void WriteUxmlAsset(ref ExportContext ctx, UxmlAsset asset, List<UxmlAsset> children)
    {
        var vta = ctx.visualTreeAsset;
        var namespacePrefix = ctx.GetNamespaceDefinition(asset);

        BeforeWriteUxmlAssetTag(ref ctx, asset);

        ctx.AppendIndent();
        WriteTag(ref ctx, "<");

        var resolvedTypename = GetAssetNameAndPrefix(asset, in namespacePrefix);

        WriteElementTypeName(ref ctx, resolvedTypename.prefix, resolvedTypename.typename);
        WriteXmlNamespaces(ref ctx, asset.namespaceDefinitions);

        if (asset.properties != null && ctx.options.consistentAttributeOrder)
        {
            using var _ = ListPool<UxmlProperty>.Get(out var properties);
            properties.AddRange(asset.properties);
            properties.Sort((lhs, rhs) => string.CompareOrdinal(lhs.name, rhs.name));
            WriteProperties(ref ctx, properties);
        }
        else
        {
            WriteProperties(ref ctx, asset.properties);
        }

        if (asset is VisualElementAsset vea)
        {
            WriteClasses(ref ctx, vea.classes);
            WriteInlineStyles(ref ctx, vta.inlineSheet, vea.ruleIndex);
            WriteSlots(ref ctx, vea);
        }

        var hasChildren = ExportsChildrenNodes(asset, ctx.options);
        if (!hasChildren)
        {
            WriteTag(ref ctx, "/>");
            WriteLine(ref ctx);
            AfterWriteUxmlAssetTag(ref ctx, asset);
            return;
        }

        WriteTag(ref ctx, ">");
        WriteLine(ref ctx);
        AfterWriteUxmlAssetTag(ref ctx, asset);
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

        BeforeWriteUxmlAssetTag(ref ctx, asset);
        ctx.AppendIndent();
        WriteTag(ref ctx, "</");
        WriteElementTypeName(ref ctx, resolvedTypename.prefix, resolvedTypename.typename);
        WriteTag(ref ctx, ">");
        WriteLine(ref ctx);
        AfterWriteUxmlAssetTag(ref ctx, asset);
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
        var namespaceDefinition =
            vta.FindUxmlNamespaceDefinitionForTypeName(vta.visualTree, typeof(VisualElement).FullName);

        WriteTag(ref ctx, "<");
        WriteElementTypeName(ref ctx, "UnityEngine.UIElements.Template", namespaceDefinition);
        WriteSpace(ref ctx);
        WriteAttribute(ref ctx, "name", usingEntry.alias);
        WriteSpace(ref ctx);
        WriteAttribute(ref ctx, "src", ctx.GetProcessedPathForSrcAttribute(usingEntry.asset));
        WriteTag(ref ctx, "/>");
        WriteLine(ref ctx);
    }

    protected void WriteStyleSheets(ref ExportContext ctx, List<StyleSheet> styleSheets)
    {
        for (var i = 0; i < styleSheets.Count; ++i)
        {
            var styleSheet = styleSheets[i];
            ctx.AppendIndent();
            WriteStyleSheet(ref ctx, styleSheet);
            WriteLine(ref ctx);
        }
    }

    protected void WriteStyleSheet(ref ExportContext ctx, StyleSheet styleSheet)
    {
        var path = ctx.GetProcessedPathForSrcAttribute(styleSheet);
        WriteTag(ref ctx, "<");
        WriteElementTypeName(ref ctx, "Style", UxmlNamespaceDefinition.Empty);
        WriteSpace(ref ctx);
        WriteAttribute(ref ctx, "src", path);
        WriteTag(ref ctx, "/>");
    }

    protected void WriteAttributeOverrides(ref ExportContext ctx,
        List<TemplateAsset.AttributeOverride> attributeOverrides)
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
            WriteTag(ref ctx, "<");
            WriteElementTypeName(ref ctx, "AttributeOverrides", UxmlNamespaceDefinition.Empty);
            WriteSpace(ref ctx);
            WriteAttribute(ref ctx, TemplateAsset.k_AttributeOverrideElementNameAttributeName, elementName);

            for (var index = 0; index < overrides.Count; index++)
            {
                WriteSpace(ref ctx);
                var attributeOverride = overrides[index];
                WriteAttribute(ref ctx, attributeOverride.m_AttributeName, attributeOverride.m_Value);
            }

            WriteTag(ref ctx, "/>");
            WriteLine(ref ctx);
        }
    }

    protected void WriteElementTypeName(ref ExportContext ctx, string fullTypename,
        UxmlNamespaceDefinition namespaceDefinition)
    {
        var typename = !string.IsNullOrEmpty(namespaceDefinition.resolvedNamespace)
            ? fullTypename[(namespaceDefinition.resolvedNamespace.Length + 1)..]
            : fullTypename;

        WriteElementTypeName(ref ctx, namespaceDefinition.prefix, typename);
    }

    protected void WriteElementTypeName(ref ExportContext ctx, string prefix, string typename)
    {
        using (new HighlightingScope(ref ctx, k_TagName))
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                ctx.Append(prefix);
                ctx.Append(":");
            }

            ctx.Append(typename);
        }
    }

    protected void WriteProperties(ref ExportContext ctx, List<UxmlProperty> properties)
    {
        if (!(properties?.Count > 0))
            return;

        for (var i = 0; i < properties.Count; ++i)
        {
            var property = properties[i];
            if (ctx.options.IsAttributeIgnored(property.name))
                continue;
            WriteSpace(ref ctx);
            WriteAttribute(ref ctx, property.name, property.value);
        }
    }

    protected void WriteAttribute(ref ExportContext ctx, string name, string value)
    {
        WriteAttributeName(ref ctx, name);
        using (new HighlightingScope(ref ctx, k_AttributeValue))
            ctx.Append("=");
        WriteAttributeValue(ref ctx, value);
    }

    protected void WriteClasses(ref ExportContext ctx, string[] ussClasses)
    {
        if (ussClasses is { Length: > 0 })
        {
            WriteSpace(ref ctx);
            WriteAttribute(ref ctx, "class", string.Join(" ", ussClasses));
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
                // Disable highlighting of the inline styles, since the color syntax would get encoded when written as an
                // attribute.
                var options = ctx.styleExporterOptions;
                options.useColorHighlighting = false;
                var exportedInlineStyles = ctx.styleExporter.ExportInlineRule(inlineStyleSheet, r, options);
                if (!string.IsNullOrEmpty(exportedInlineStyles))
                {
                    WriteSpace(ref ctx);
                    WriteAttribute(ref ctx, "style", exportedInlineStyles);
                }
            }
        }
    }

    protected void WriteSlots(ref ExportContext ctx, VisualElementAsset vea)
    {
        var veaId = vea.id;
        if (ctx.visualTreeAsset.TryGetSlotInsertionPoint(veaId, out var slotName))
        {
            WriteSpace(ref ctx);
            WriteAttribute(ref ctx, "slot-name", slotName);
        }

        if (vea.parentAsset is TemplateAsset { slotUsages: not null } parentTemplateAsset)
        {
            var slotIndex = parentTemplateAsset.slotUsages.FindIndex(su => su.assetId == veaId);
            if (slotIndex >= 0)
            {
                WriteSpace(ref ctx);
                WriteAttribute(ref ctx, "slot", parentTemplateAsset.slotUsages[slotIndex].slotName);
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
                WriteSpace(ref ctx);
                WriteXmlNamespace(ref ctx, xmlNamespace);
            }
        }
    }

    protected void WriteXmlNamespace(ref ExportContext ctx, UxmlNamespaceDefinition xmlNamespace)
    {
        if (string.IsNullOrEmpty(xmlNamespace.prefix))
            WriteAttribute(ref ctx, "xmlns", xmlNamespace.resolvedNamespace);
        else
            WriteAttribute(ref ctx, $"xmlns:{xmlNamespace.prefix}", xmlNamespace.resolvedNamespace);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void WriteSpace(ref ExportContext ctx)
    {
        ctx.Append(' ');
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void WriteLine(ref ExportContext ctx)
    {
        ctx.AppendLine();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void WriteTag(ref ExportContext ctx, string tag)
    {
        using (new HighlightingScope(ref ctx, k_Tag))
            ctx.Append(tag);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void WriteAttributeName(ref ExportContext ctx, string value)
    {
        using (new HighlightingScope(ref ctx, k_AttributeName))
            ctx.Append(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void WriteAttributeValue(ref ExportContext ctx, string value)
    {
        using (new HighlightingScope(ref ctx, k_AttributeValue))
        {
            ctx.Append('"');
            ctx.Append(URIHelpers.EncodeUri(value));
            ctx.Append('"');
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

    static Color HtmlColor(string htmlColor) => ColorUtility.TryParseHtmlString(htmlColor, out var color) ? color : Color.clear;
}
