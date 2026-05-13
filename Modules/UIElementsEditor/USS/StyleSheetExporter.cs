// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
    internal partial class StyleSheetExporter
    {
        [OnCodeInitializing]
        static void Init()
        {
            // Intentionally left empty to trigger the `UIPrefColor` registration.
        }

        public const string ColorsPreferenceCategory = "USS Syntax Highlighting";

        static readonly UIPrefColor k_AtKeywordColor = new (ColorsPreferenceCategory, "At Keyword", HtmlColor("#871094"), HtmlColor("#BC8DF8"));
        static readonly UIPrefColor k_FunctionColor = new (ColorsPreferenceCategory, "Function", HtmlColor("#0058A8"), HtmlColor("#70B0FF"));
        static readonly UIPrefColor k_KeywordColor = new (ColorsPreferenceCategory, "Keyword", HtmlColor("#0033B3"), HtmlColor("#FFFFFF"));
        static readonly UIPrefColor k_NumberColor = new (ColorsPreferenceCategory, "Number", HtmlColor("#1750EB"), HtmlColor("#FF9668"));
        static readonly UIPrefColor k_PunctuationColor = new (ColorsPreferenceCategory, "Punctuation", HtmlColor("#000000"), HtmlColor("#70B0FF"));
        static readonly UIPrefColor k_PropertyColor = new (ColorsPreferenceCategory, "Property Name", HtmlColor("#0058A8"), HtmlColor("#B6C4F2"));
        static readonly UIPrefColor k_SelectorClassColor = new (ColorsPreferenceCategory, "Selector Class", HtmlColor("#9C5F00"), HtmlColor("#DBBE7F"));
        static readonly UIPrefColor k_SelectorIdColor = new (ColorsPreferenceCategory,  "Selector Id", HtmlColor("#7F0055"), HtmlColor("#70B0FF"));
        static readonly UIPrefColor k_SelectorPseudoClassColor = new (ColorsPreferenceCategory, "Selector Pseudo-Class", HtmlColor("#00796B"), HtmlColor("#4FD6BE"));
        static readonly UIPrefColor k_SelectorTypeColor = new (ColorsPreferenceCategory, "Selector Type", HtmlColor("#5B3F00"), HtmlColor("#DBBE7F"));
        static readonly UIPrefColor k_String = new (ColorsPreferenceCategory, "String", HtmlColor("#067D17"), HtmlColor("#64D1A9"));
        static readonly UIPrefColor k_UnitColor = new (ColorsPreferenceCategory, "Unit", HtmlColor("#871094"), HtmlColor("#F3C1FF"));
        static readonly UIPrefColor k_ValueColor = new (ColorsPreferenceCategory, "Value", HtmlColor("#871094"), HtmlColor("#F3C1FF"));

        public static Color AtKeywordColor { get => k_AtKeywordColor.Color; set { k_AtKeywordColor.Color = value; PrefSettings.Set(k_AtKeywordColor.StorageKey, k_AtKeywordColor); } }
        public static Color FunctionColor { get => k_FunctionColor.Color; set { k_FunctionColor.Color = value; PrefSettings.Set(k_FunctionColor.StorageKey, k_FunctionColor); } }
        public static Color KeywordColor { get => k_KeywordColor.Color; set { k_KeywordColor.Color = value; PrefSettings.Set(k_KeywordColor.StorageKey, k_KeywordColor); } }
        public static Color NumberColor { get => k_NumberColor.Color; set { k_NumberColor.Color = value; PrefSettings.Set(k_NumberColor.StorageKey, k_NumberColor); } }
        public static Color PunctuationColor { get => k_PunctuationColor.Color; set { k_PunctuationColor.Color = value; PrefSettings.Set(k_PunctuationColor.StorageKey, k_PunctuationColor); } }
        public static Color PropertyColor { get => k_PropertyColor.Color; set { k_PropertyColor.Color = value; PrefSettings.Set(k_PropertyColor.StorageKey, k_PropertyColor); } }
        public static Color SelectorClassColor { get => k_SelectorClassColor.Color; set { k_SelectorClassColor.Color = value; PrefSettings.Set(k_SelectorClassColor.StorageKey, k_SelectorClassColor); } }
        public static Color SelectorIdColor { get => k_SelectorIdColor.Color; set { k_SelectorIdColor.Color = value; PrefSettings.Set(k_SelectorIdColor.StorageKey, k_SelectorIdColor); } }
        public static Color SelectorPseudoClassColor { get => k_SelectorPseudoClassColor.Color; set { k_SelectorPseudoClassColor.Color = value; PrefSettings.Set(k_SelectorPseudoClassColor.StorageKey, k_SelectorPseudoClassColor); } }
        public static Color SelectorTypeColor { get => k_SelectorTypeColor.Color; set { k_SelectorTypeColor.Color = value; PrefSettings.Set(k_SelectorTypeColor.StorageKey, k_SelectorTypeColor); } }
        public static Color StringColor { get => k_String.Color; set { k_String.Color = value; PrefSettings.Set(k_String.StorageKey, k_String); } }
        public static Color UnitColor { get => k_UnitColor.Color; set { k_UnitColor.Color = value; PrefSettings.Set(k_UnitColor.StorageKey, k_UnitColor); } }
        public static Color ValueColor { get => k_ValueColor.Color; set { k_ValueColor.Color = value; PrefSettings.Set(k_ValueColor.StorageKey, k_ValueColor); } }

        public static readonly string SelectedStyleSheetSelectorName = "__unity_ui_builder_selected_stylesheet";
        internal static readonly string[] IgnoredSelectorsWhenExporting = { SelectedStyleSheetSelectorName };

        const string StyleSelectorElementName = "__unity-selector-element";
        internal static readonly string[] IgnoredSelectorPrefixesWhenExporting = { StyleSelectorElementName };

        const string SelectedStyleRulePropertyName = "--ui-builder-selected-style-property";
        internal static readonly string[] IgnoredStylePropertiesWhenExporting = { SelectedStyleRulePropertyName };

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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal struct UssExportOptions
        {
            const string k_DefaultIndex = "    ";

            public static UssExportOptions Default => new()
            {
                propertyIndent  = k_DefaultIndex,
                ignoreSelectorList = IgnoredSelectorsWhenExporting,
                ignoreSelectorPrefixList = IgnoredSelectorPrefixesWhenExporting,
                ignorePropertyList = IgnoredStylePropertiesWhenExporting,
                useColorHighlighting = false
            };

            string m_PropertyIndent;
            string[] m_IgnoreSelectorList;
            string[] m_IgnoreSelectorPrefixList;
            string[] m_IgnorePropertyList;
            bool? m_UseColorHighlighting;

            public string propertyIndent
            {
                get => m_PropertyIndent ?? k_DefaultIndex;
                set => m_PropertyIndent = value;
            }

            public string[] ignoreSelectorList
            {
                get => m_IgnoreSelectorList ?? Array.Empty<string>();
                set => m_IgnoreSelectorList = value;
            }

            public string[] ignoreSelectorPrefixList
            {
                get => m_IgnoreSelectorPrefixList ?? Array.Empty<string>();
                set => m_IgnoreSelectorPrefixList = value;
            }

            public string[] ignorePropertyList
            {
                get => m_IgnorePropertyList ?? Array.Empty<string>();
                set => m_IgnorePropertyList = value;
            }

            public bool useColorHighlighting
            {
                get => m_UseColorHighlighting.HasValue ? m_UseColorHighlighting.Value : false;
                set => m_UseColorHighlighting = value;
            }

            public UssExportOptions()
            {
                propertyIndent = "    ";
                ignoreSelectorList = Array.Empty<string>();
                ignoreSelectorPrefixList = Array.Empty<string>();
                ignorePropertyList = Array.Empty<string>();
                m_UseColorHighlighting = null;
            }

            public bool IsSelectorIgnored(StyleComplexSelector selector)
            {
                if (selector.selectors.Length == 0 ||
                    selector.selectors[0].parts.Length == 0)
                    return true;

                var selectorFirstPart = selector.selectors[0].parts[0].value;

                if (ignoreSelectorList != null)
                {
                    if (Array.IndexOf(ignoreSelectorList, selectorFirstPart) >= 0)
                        return true;
                }

                if (ignoreSelectorPrefixList != null)
                {
                    if (Array.FindIndex(ignoreSelectorPrefixList, selectorFirstPart.StartsWith) >= 0)
                        return true;
                }

                return false;
            }

            public bool IsPropertyIgnored(string propertyName)
            {
                if (ignorePropertyList == null)
                    return false;
                return Array.IndexOf(ignorePropertyList, propertyName) >= 0;
            }
        }

        public readonly struct ExportContext
        {
            private readonly StringBuilder m_Builder;
            private readonly StyleSheet m_StyleSheet;
            private readonly UssExportOptions m_Options;

            [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
            internal ExportContext(StyleSheet styleSheet, StringBuilder builder, UssExportOptions options)
            {
                m_StyleSheet = styleSheet;
                m_Builder = builder;
                m_Options = options;
            }

            public StyleSheet styleSheet => m_StyleSheet;

            public UssExportOptions options => m_Options;

            public void Append(char c)
            {
                m_Builder.Append(c);
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
                m_Builder.Append(m_Options.propertyIndent);
            }
        }

        public static StyleSheetExporter Default { get; } = new();

        /// <summary>
        /// Converts the provided <see cref="StyleSheet"/> to <see cref="string"/> using the default parameters.
        /// </summary>
        /// <param name="styleSheet">The stylesheet to export.</param>
        /// <returns>A <see cref="string"/> version of the <see cref="StyleSheet"/>.</returns>
        public string ToUssString(StyleSheet styleSheet) => ToUssString(styleSheet, UssExportOptions.Default);

        /// <summary>
        /// Converts the provided <see cref="StyleSheet"/> to <see cref="string"/>.
        /// </summary>
        /// <param name="styleSheet">The stylesheet to export.</param>
        /// <param name="options">To export options.</param>
        /// <returns>A <see cref="string"/> version of the <see cref="StyleSheet"/>.</returns>
        public string ToUssString(StyleSheet styleSheet, UssExportOptions options)
        {
            using var _ = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, options);
            WriteStyleSheet(ref context, styleSheet);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts the provided <see cref="StyleRule"/> to <see cref="string"/> using the default options.
        /// </summary>
        /// <param name="styleSheet">The style sheet to export.</param>
        /// <param name="styleRule">The style rule to export.</param>
        /// <returns>A <see cref="string"/> version of the <see cref="StyleRule"/>.</returns>
        public string ToUssString(StyleSheet styleSheet, StyleRule styleRule) =>
            ToUssString(styleSheet, styleRule, UssExportOptions.Default);

        /// <summary>
        /// Converts the provided <see cref="StyleRule"/> to <see cref="string"/>.
        /// </summary>
        /// <param name="styleSheet">The style sheet to export.</param>
        /// <param name="styleRule">The style rule to export.</param>
        /// <param name="options">To export options.</param>
        /// <returns>A <see cref="string"/> version of the <see cref="StyleRule"/>.</returns>
        public string ToUssString(StyleSheet styleSheet, StyleRule styleRule, UssExportOptions options)
        {
            using var _ = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, options);
            var ruleIndex = Array.IndexOf(styleSheet.rules, styleRule);
            WriteRule(ref context, styleRule, ruleIndex);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts the provided <see cref="StyleComplexSelector"/> to <see cref="string"/> using the default options.
        /// </summary>
        /// <param name="styleSheet">The style sheet to export.</param>
        /// <param name="selector">The selector to export.</param>
        /// <returns>A <see cref="string"/> version of the <see cref="StyleComplexSelector"/>.</returns>
        public string ToUssString(StyleSheet styleSheet, StyleComplexSelector selector)
            => ToUssString(styleSheet, selector, UssExportOptions.Default);

        /// <summary>
        /// Converts the provided <see cref="StyleComplexSelector"/> to <see cref="string"/>.
        /// </summary>
        /// <param name="styleSheet">The style sheet to export.</param>
        /// <param name="selector">The selector to export.</param>
        /// <param name="options">To export options.</param>
        /// <returns>A <see cref="string"/> version of the <see cref="StyleComplexSelector"/>.</returns>
        public string ToUssString(StyleSheet styleSheet, StyleComplexSelector selector, UssExportOptions options)
        {
            using var _ = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, options);
            WriteSelector(ref context, selector);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts the provided <see cref="StyleProperty"/> to <see cref="string"/> using the default options.
        /// </summary>
        /// <param name="styleSheet">The style sheet to export.</param>
        /// <param name="property">The property to export.</param>
        /// <returns>A <see cref="string"/> version of the <see cref="StyleProperty"/>.</returns>
        public string ToUssString(StyleSheet styleSheet, StyleProperty property)
            => ToUssString(styleSheet, property, UssExportOptions.Default);

        /// <summary>
        /// Converts the provided <see cref="StyleProperty"/> to <see cref="string"/>.
        /// </summary>
        /// <param name="styleSheet">The style sheet to export.</param>
        /// <param name="property">The property to export.</param>
        /// <param name="options">To export options.</param>
        /// <returns>A <see cref="string"/> version of the <see cref="StyleProperty"/>.</returns>
        public string ToUssString(StyleSheet styleSheet, StyleProperty property, UssExportOptions options)
        {
            using var _ = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, options);
            WriteProperty(ref context, property);
            return stringBuilder.ToString();
        }

        public string ExportInlineRule(StyleSheet styleSheet, StyleRule rule)
            => ExportInlineRule(styleSheet, rule, UssExportOptions.Default);

        public string ExportInlineRule(StyleSheet styleSheet, StyleRule rule, UssExportOptions options)
        {
            using var builderHandle = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, options);
            for (var i = 0; i < rule.properties?.Length; ++i)
            {
                var property = rule.properties[i];
                if (context.options.IsPropertyIgnored(property.name))
                    continue;
                WriteProperty(ref context,  property);
                context.Append(" ");
            }

            return stringBuilder.ToString().Trim();
        }

        protected virtual void WriteStyleSheet(ref ExportContext ctx, StyleSheet styleSheet)
        {
            if (styleSheet.imports?.Length > 0)
            {
                WriteImportBlock(ref ctx, styleSheet.imports);
                ctx.AppendLine();
            }

            if (styleSheet.rules?.Length > 0)
            {
                WriteRuleBlock(ref ctx, styleSheet.rules);
            }
        }

        protected void WriteImportBlock(ref ExportContext ctx, StyleSheet.ImportStruct[] imports)
        {
            for (var importIndex = 0; importIndex < imports?.Length; ++importIndex)
            {
                var import = imports[importIndex];
                if (!import.styleSheet)
                    continue;

                WriteImport(ref ctx, import);
                ctx.AppendLine();
            }
        }

        protected void WriteImport(ref ExportContext ctx, StyleSheet.ImportStruct import)
        {
            // Skip invalid references.
            var path = URIHelpers.MakeAssetUri(import.styleSheet);
            if (string.IsNullOrEmpty(path) || path == "none")
                return;

            WriteDirective(ref ctx, "@import ");
            WriteAssetReference(ref ctx, import.styleSheet);
            WritePunctuation(ref ctx, ";");
        }

        protected void WriteRuleBlock(ref ExportContext ctx, StyleRule[] rules)
        {
            for (var ruleIndex = 0; ruleIndex < rules.Length; ++ruleIndex)
            {
                WriteRule(ref ctx, rules[ruleIndex], ruleIndex);
                if (ruleIndex < rules.Length - 1)
                {
                    ctx.AppendLine();
                    ctx.AppendLine();
                }
            }
            ctx.AppendLine();
        }

        protected virtual void BeforeWritingRule(ref ExportContext ctx, StyleRule rule, int ruleIndex)
        {
        }

        protected virtual void AfterWritingRule(ref ExportContext ctx, StyleRule rule, int ruleIndex)
        {
        }

        protected void WriteRule(ref ExportContext ctx, StyleRule rule, int index)
        {
            using var selectorsHandle = ListPool<StyleComplexSelector>.Get(out var selectors);

            foreach (var selector in rule.complexSelectors)
            {
                if (ctx.options.IsSelectorIgnored(selector))
                    continue;
                selectors.Add(selector);
            }

            if (selectors.Count > 0)
            {
                BeforeWritingRule(ref ctx, rule, index);
                WriteSelectorBlock(ref ctx, selectors.ToArray());
                WriteSpace(ref ctx);
                WritePunctuation(ref ctx, "{");
                WriteLine(ref ctx);
                WritePropertyBlock(ref ctx, rule.properties);
                WritePunctuation(ref ctx, "}");
                AfterWritingRule(ref ctx, rule, index);
            }
        }

        protected void WriteSelectorBlock(ref ExportContext ctx, StyleComplexSelector[] selectors)
        {
            for (var selectorIndex = 0; selectorIndex < selectors.Length; ++selectorIndex)
            {
                var selector = selectors[selectorIndex];

                WriteSelector(ref ctx, selector);

                if (selectorIndex != selectors.Length - 1)
                {
                    WritePunctuation(ref ctx, ",");
                    WriteLine(ref ctx);
                }
            }
        }

        protected void WriteSelector(ref ExportContext ctx, StyleComplexSelector complexSelector)
        {
            foreach (var selector in complexSelector.selectors)
            {
                switch (selector.previousRelationship)
                {
                    case StyleSelectorRelationship.None:
                        break;
                    case StyleSelectorRelationship.Child:
                        WritePunctuation(ref ctx, " > ");
                        break;
                    case StyleSelectorRelationship.Descendent:
                        WriteSpace(ref ctx);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                foreach (var selectorPart in selector.parts)
                {
                    switch (selectorPart.type)
                    {
                        case StyleSelectorType.Wildcard:
                            WritePunctuation(ref ctx, "*");
                            break;
                        case StyleSelectorType.Type:
                            WriteSelectorTypeName(ref ctx, selectorPart.value);
                            break;
                        case StyleSelectorType.Class:
                            WritePunctuation(ref ctx, ".");
                            WriteSelectorClassName(ref ctx, selectorPart.value);
                            break;
                        case StyleSelectorType.PseudoClass:
                            WritePunctuation(ref ctx, ":");
                            WriteSelectorPseudoClass(ref ctx, selectorPart.value);
                            break;
                        case StyleSelectorType.ID:
                            WriteSelectorId(ref ctx, selectorPart.value);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        protected void WritePropertyBlock(ref ExportContext ctx, StyleProperty[] properties)
        {
            for (var propertyIndex = 0; propertyIndex < properties.Length; ++propertyIndex)
            {
                var styleProperty = properties[propertyIndex];
                if (ctx.options.IsPropertyIgnored(styleProperty.name))
                    continue;
                // Validate property

                ctx.AppendIndent();
                WriteProperty(ref ctx, styleProperty);
                ctx.AppendLine();
            }
        }

        protected void WriteProperty(ref ExportContext ctx, StyleProperty property)
        {
            WriteStylePropertyName(ref ctx, property.name);
            WritePunctuation(ref ctx, ": ");
            WriteStyleValueHandleBlock(ref ctx, property.values.AsSpan());
            WritePunctuation(ref ctx, ";");
        }

        protected void WriteStyleValueHandleBlock(ref ExportContext ctx, Span<StyleValueHandle> handles)
        {
            for (var handleIndex = 0; handleIndex < handles.Length; ++handleIndex)
            {
                WriteStyleValueHandle(ref ctx, handles, ref handleIndex);

                if (handleIndex != handles.Length - 1 &&
                    Peek(handles, handleIndex + 1) != StyleValueType.CommaSeparator &&
                    Peek(handles, handleIndex) != StyleValueType.Function)
                {
                    WriteSpace(ref ctx);
                }
            }
        }

        protected void WriteStyleValueHandle(ref ExportContext ctx, Span<StyleValueHandle> handles, ref int handleIndex)
        {
            var handle = handles[handleIndex];
            switch (handle.valueType)
            {
                case StyleValueType.Keyword:
                    WriteKeywordValue(ref ctx, ctx.styleSheet.ReadKeyword(handle));
                    break;
                case StyleValueType.Float:
                    WriteFloatValue(ref ctx, ctx.styleSheet.ReadFloat(handle));
                    break;
                case StyleValueType.Dimension:
                    WriteDimensionValue(ref ctx, ctx.styleSheet.ReadDimension(handle));
                    break;
                case StyleValueType.Color:
                    WriteColor(ref ctx, ctx.styleSheet.ReadColor(handle));
                    break;
                case StyleValueType.ResourcePath:
                    WriteResourcePath(ref ctx, ctx.styleSheet.ReadResourcePath(handle));
                    break;
                case StyleValueType.AssetReference:
                    WriteAssetReference(ref ctx, ctx.styleSheet.ReadAssetReference(handle));
                    break;
                case StyleValueType.Enum:
                    WriteEnum(ref ctx, ctx.styleSheet.ReadEnum(handle));
                    break;
                case StyleValueType.Variable:
                    WriteVariable(ref ctx, ctx.styleSheet.ReadVariable(handle));
                    break;
                case StyleValueType.String:
                    WriteString(ref ctx, $"\"{ctx.styleSheet.ReadString(handle)}\"");
                    break;
                case StyleValueType.Function:
                    var functionName = ctx.styleSheet.ReadFunctionName(handle);
                    var argStart = handleIndex + 2;
                    GetLastFunctionHandleIndex(ref ctx, handles, ref handleIndex);
                    WriteFunction(ref ctx, functionName, handles.Slice(argStart, handleIndex - argStart));
                    // We need to rollback
                    --handleIndex;
                    break;
                case StyleValueType.CommaSeparator:
                    WritePunctuation(ref ctx, ",");
                    break;
                case StyleValueType.ScalableImage:
                    WriteScalableImage(ref ctx, ctx.styleSheet.ReadScalableImage(handle));
                    break;
                case StyleValueType.MissingAssetReference:
                    WriteMissingAssetReference(ref ctx, ctx.styleSheet.ReadMissingAssetReferenceUrl(handle));
                    break;
                case StyleValueType.Invalid:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected void WriteDirective(ref ExportContext ctx, string value)
        {
            using (new HighlightingScope(ref ctx, k_AtKeywordColor))
                ctx.Append(value);
        }

        protected void WriteSelectorTypeName(ref ExportContext ctx, string value)
        {
            using (new HighlightingScope(ref ctx, k_SelectorTypeColor))
                ctx.Append(value);
        }

        protected void WriteSelectorClassName(ref ExportContext ctx, string value)
        {
            using (new HighlightingScope(ref ctx, k_SelectorClassColor))
                ctx.Append(value);
        }

        protected void WriteSelectorPseudoClass(ref ExportContext ctx, string value)
        {
            using (new HighlightingScope(ref ctx, k_SelectorPseudoClassColor))
                ctx.Append(value);
        }

        protected void WriteSelectorId(ref ExportContext ctx, string value)
        {
            using (new HighlightingScope(ref ctx, k_SelectorIdColor))
            {
                ctx.Append('#');
                ctx.Append(value);
            }
        }

        protected void WriteStylePropertyName(ref ExportContext ctx, string value)
        {
            using (new HighlightingScope(ref ctx, k_PropertyColor))
                ctx.Append(value);
        }

        protected void WriteKeywordValue(ref ExportContext ctx, StyleValueKeyword value)
        {
            using (new HighlightingScope(ref ctx, k_KeywordColor))
                ctx.Append(value.ToUssString());
        }

        protected void WriteFloatValue(ref ExportContext ctx, float value, string format = null)
        {
            using (new HighlightingScope(ref ctx, k_NumberColor))
                ctx.Append(value.ToString(format, CultureInfo.InvariantCulture.NumberFormat));
        }

        protected void WriteDimensionValue(ref ExportContext ctx, Dimension dimension)
        {
            // Display 0 without a unit when using the default unit for the type. For time values, always include the unit regardless. (UUM-99023)
            if (dimension.value == 0 && (dimension.unit == Dimension.Unit.Pixel || dimension.unit == Dimension.Unit.Degree))
                WriteFloatValue(ref ctx, 0);
            else
            {
                WriteFloatValue(ref ctx, dimension.value);
                WriteDimensionUnit(ref ctx, dimension.unit);
            }
        }

        protected void WriteDimensionUnit(ref ExportContext ctx, Dimension.Unit value)
        {
            using (new HighlightingScope(ref ctx, k_UnitColor))
                ctx.Append(StyleSheetUtility.GetDimensionUnitExportString(value));
        }

        protected void WriteColor(ref ExportContext ctx, Color value)
        {
            var alpha = value.a.ToString("0.###", CultureInfo.InvariantCulture.NumberFormat);
            if (alpha != "1")
            {
                WriteFunctionName(ref ctx, "rgba");
                WritePunctuation(ref ctx, "(");
                WriteFloatValue(ref ctx, ColorComponent(value.r));
                WritePunctuation(ref ctx, ", ");
                WriteFloatValue(ref ctx, ColorComponent(value.g));
                WritePunctuation(ref ctx, ", ");
                WriteFloatValue(ref ctx, ColorComponent(value.b));
                WritePunctuation(ref ctx, ", ");
                WriteFloatValue(ref ctx, value.a, "0.###");
                WritePunctuation(ref ctx, ")");
            }
            else
            {
                WriteFunctionName(ref ctx, "rgb");
                WritePunctuation(ref ctx, "(");
                WriteFloatValue(ref ctx, ColorComponent(value.r));
                WritePunctuation(ref ctx, ", ");
                WriteFloatValue(ref ctx, ColorComponent(value.g));
                WritePunctuation(ref ctx, ", ");
                WriteFloatValue(ref ctx, ColorComponent(value.b));
                WritePunctuation(ref ctx, ")");
            }

            return;

            static int ColorComponent(float component)
            {
                return (int)Math.Round(component * byte.MaxValue, 0, MidpointRounding.AwayFromZero);
            }
        }

        protected void WriteResourcePath(ref ExportContext ctx, ResolvedResourcePath value)
        {
            WriteFunctionName(ref ctx, "resource");
            WritePunctuation(ref ctx, "(");
            WritePath(ref ctx, value.ToString());
            WritePunctuation(ref ctx, ")");
        }

        protected void WriteAssetReference(ref ExportContext ctx, UnityEngine.Object value)
        {
            var path = URIHelpers.MakeAssetUri(value);
            WriteFunctionName(ref ctx, "url");
            WritePunctuation(ref ctx, "(");
            WritePath(ref ctx, path);
            WritePunctuation(ref ctx, ")");
        }

        protected void WritePath(ref ExportContext ctx, string path)
        {
            using (new HighlightingScope(ref ctx, k_String))
            {
                ctx.Append("\"");
                ctx.Append(path);
                ctx.Append("\"");
            }
        }

        protected void WriteEnum(ref ExportContext ctx, string value)
        {
            using (new HighlightingScope(ref ctx, k_ValueColor))
                ctx.Append(value);
        }

        protected void WriteVariable(ref ExportContext ctx, string value)
        {
            using (new HighlightingScope(ref ctx, k_PropertyColor))
                WriteIdentifier(ref ctx, value);
        }

        protected void WriteIdentifier(ref ExportContext ctx, string value)
        {
            ctx.Append(value);
        }

        protected void WriteFunction(ref ExportContext ctx, string functionName, Span<StyleValueHandle> handles)
        {
            WriteFunctionName(ref ctx, functionName);
            WritePunctuation(ref ctx, "(");
            WriteStyleValueHandleBlock(ref ctx, handles);
            WritePunctuation(ref ctx, ")");
        }

        protected void WriteFunctionName(ref ExportContext ctx, string functionName)
        {
            using (new HighlightingScope(ref ctx, k_FunctionColor))
                ctx.Append(functionName);
        }

        protected void WriteString(ref ExportContext ctx, string value)
        {
            using (new HighlightingScope(ref ctx, k_String))
                ctx.Append(value);
        }

        protected void WriteScalableImage(ref ExportContext ctx, ScalableImage value)
        {
            WriteAssetReference(ref ctx, value.normalImage);
        }

        protected void WriteMissingAssetReference(ref ExportContext ctx, string value)
        {
            WriteFunctionName(ref ctx, "url");
            WritePunctuation(ref ctx, "(");
            WritePath(ref ctx, value);
            WritePunctuation(ref ctx, ")");
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
        protected void WritePunctuation(ref ExportContext ctx, string value)
        {
            using (new HighlightingScope(ref ctx, k_PunctuationColor))
                ctx.Append(value);
        }

        private static void GetLastFunctionHandleIndex(ref ExportContext ctx, Span<StyleValueHandle> handles, ref int index)
        {
            if (handles[index].valueType != StyleValueType.Function)
                return;

            ++index;
            var argCount = ctx.styleSheet.ReadFloat(handles[index]);

            if (argCount <= 0)
            {
                ++index;
                return;
            }

            ++index;
            for (var arg = 0; arg < argCount; ++arg)
            {
                if (handles[index].valueType == StyleValueType.Function)
                {
                    GetLastFunctionHandleIndex(ref ctx, handles, ref index);
                }
                else
                {
                    ++index;
                }
            }
        }

        private static StyleValueType Peek(Span<StyleValueHandle> handles, int index)
        {
            return index < handles.Length ? handles[index].valueType : StyleValueType.Invalid;
        }

        public static string GetStylePropertyValueString(StyleSheet styleSheet, StyleProperty property) =>
            GetStylePropertyValueString(styleSheet, property, UssExportOptions.Default);

        public static string GetStylePropertyValueString(StyleSheet styleSheet, StyleProperty property, UssExportOptions options)
        {
            using var builderHandle = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, options);
            Default.WriteStyleValueHandleBlock(ref context, property.values.AsSpan());
            return stringBuilder.ToString();
        }

        public static string GetStylePropertyValueString(StyleSheet styleSheet, StyleProperty property, int index) =>
            GetStylePropertyValueString(styleSheet, property, index, UssExportOptions.Default);

        public static string GetStylePropertyValueString(StyleSheet styleSheet, StyleProperty property, int index, UssExportOptions options)
        {
            using var builderHandle = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, options);
            var handles = property.values.AsSpan();
            Default.WriteStyleValueHandle(ref context, handles, ref index);
            return stringBuilder.ToString();
        }

        public static string GetStyleVariableValueString(StyleSheet styleSheet, StyleVariable variable, int index) =>
            GetStyleVariableValueString(styleSheet, variable, index, UssExportOptions.Default);

        public static string GetStyleVariableValueString(StyleSheet styleSheet, StyleVariable variable, int index, UssExportOptions options)
        {
            using var builderHandle = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, options);
            var handles = variable.handles.AsSpan();
            Default.WriteStyleValueHandle(ref context, handles, ref index);
            return stringBuilder.ToString();
        }

        static Color HtmlColor(string htmlColor) => ColorUtility.TryParseHtmlString(htmlColor, out var color) ? color : Color.clear;
    }
}
