// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Text;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.UIElements
{
    [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
    internal class StyleSheetExporter
    {
        [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
        internal class UssExportOptions
        {
            public string propertyIndent { get; set; } = "    ";
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal string[] ignoreSelectorList { get; set; } = Array.Empty<string>();
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal string[] ignoreSelectorPrefixList { get; set; } = Array.Empty<string>();
            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal string[] ignorePropertyList { get; set; } = Array.Empty<string>();

            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal bool IsSelectorIgnored(StyleComplexSelector selector)
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

            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal bool IsPropertyIgnored(string propertyName)
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

            [VisibleToOtherModules("UnityEditor.UIBuilderModule")]
            internal ExportContext(StyleSheet styleSheet, StringBuilder builder, UssExportOptions options = null)
            {
                m_StyleSheet = styleSheet;
                m_Builder = builder;
                m_Options = options ?? new UssExportOptions();
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

        /// <summary>
        /// Converts the provided <see cref="StyleSheet"/> to <see cref="string"/>.
        /// </summary>
        /// <param name="styleSheet">The stylesheet to export.</param>
        /// <param name="options">To export options.</param>
        /// <returns>A <see cref="string"/> version of the <see cref="StyleSheet"/>.</returns>
        public string ToUssString(StyleSheet styleSheet, UssExportOptions options = null)
        {
            using var _ = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, options);
            WriteStyleSheet(ref context, styleSheet);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts the provided <see cref="StyleRule"/> to <see cref="string"/>.
        /// </summary>
        /// <param name="styleSheet">The style sheet to export.</param>
        /// <param name="styleRule">The style rule to export.</param>
        /// <param name="options">To export options.</param>
        /// <returns>A <see cref="string"/> version of the <see cref="StyleRule"/>.</returns>
        public string ToUssString(StyleSheet styleSheet, StyleRule styleRule, UssExportOptions options = null)
        {
            using var _ = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, options);
            WriteRule(ref context, styleRule);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts the provided <see cref="StyleComplexSelector"/> to <see cref="string"/>.
        /// </summary>
        /// <param name="styleSheet">The style sheet to export.</param>
        /// <param name="selector">The selector to export.</param>
        /// <param name="options">To export options.</param>
        /// <returns>A <see cref="string"/> version of the <see cref="StyleComplexSelector"/>.</returns>
        public string ToUssString(StyleSheet styleSheet, StyleComplexSelector selector, UssExportOptions options = null)
        {
            using var _ = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, options);
            WriteSelector(ref context, selector);
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Converts the provided <see cref="StyleProperty"/> to <see cref="string"/>.
        /// </summary>
        /// <param name="styleSheet">The style sheet to export.</param>
        /// <param name="property">The property to export.</param>
        /// <param name="options">To export options.</param>
        /// <returns>A <see cref="string"/> version of the <see cref="StyleProperty"/>.</returns>
        public string ToUssString(StyleSheet styleSheet, StyleProperty property, UssExportOptions options = null)
        {
            using var _ = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, options);
            WriteProperty(ref context, property);
            return stringBuilder.ToString();
        }

        public string ExportInlineRule(StyleSheet styleSheet, StyleRule rule, UssExportOptions options = null)
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

            WriteDirective(ref ctx, "@import");
            ctx.Append(" ");
            WriteAssetReference(ref ctx, import.styleSheet);
            ctx.Append(";");
        }

        protected void WriteRuleBlock(ref ExportContext ctx, StyleRule[] rules)
        {
            for (var ruleIndex = 0; ruleIndex < rules.Length; ++ruleIndex)
            {
                WriteRule(ref ctx, rules[ruleIndex]);
                if (ruleIndex < rules.Length - 1)
                {
                    ctx.AppendLine();
                    ctx.AppendLine();
                }
            }
            ctx.AppendLine();
        }

        protected void WriteRule(ref ExportContext ctx, StyleRule rule)
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
                WriteSelectorBlock(ref ctx, selectors.ToArray());
                ctx.AppendLine(" {");
                WritePropertyBlock(ref ctx, rule.properties);
                ctx.Append("}");
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
                    ctx.AppendLine(",");
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
                        ctx.Append(" > ");
                        break;
                    case StyleSelectorRelationship.Descendent:
                        ctx.Append(" ");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                foreach (var selectorPart in selector.parts)
                {
                    switch (selectorPart.type)
                    {
                        case StyleSelectorType.Wildcard:
                            ctx.Append('*');
                            break;
                        case StyleSelectorType.Type:
                            WriteSelectorTypeName(ref ctx, selectorPart.value);
                            break;
                        case StyleSelectorType.Class:
                            ctx.Append('.');
                            WriteSelectorClassName(ref ctx, selectorPart.value);
                            break;
                        case StyleSelectorType.PseudoClass:
                            ctx.Append(':');
                            WriteSelectorPseudoClass(ref ctx, selectorPart.value);
                            break;
                        case StyleSelectorType.ID:
                            ctx.Append('#');
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
            ctx.Append(": ");
            WriteStyleValueHandleBlock(ref ctx, property.values.AsSpan());
            ctx.Append(";");
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
                    ctx.Append(" ");
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
                    WriteString(ref ctx, ctx.styleSheet.ReadString(handle));
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
                    ctx.Append(",");
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
            ctx.Append(value);
        }

        protected void WriteSelectorTypeName(ref ExportContext ctx, string value)
        {
            ctx.Append(value);
        }

        protected void WriteSelectorClassName(ref ExportContext ctx, string value)
        {
            ctx.Append(value);
        }

        protected void WriteSelectorPseudoClass(ref ExportContext ctx, string value)
        {
            ctx.Append(value);
        }

        protected void WriteSelectorId(ref ExportContext ctx, string value)
        {
            ctx.Append(value);
        }

        protected void WriteStylePropertyName(ref ExportContext ctx, string value)
        {
            ctx.Append(value);
        }

        protected void WriteKeywordValue(ref ExportContext ctx, StyleValueKeyword value)
        {
            ctx.Append(value.ToUssString());
        }

        protected void WriteFloatValue(ref ExportContext ctx, float value, string format = null)
        {
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
            ctx.Append(StyleSheetUtility.GetDimensionUnitExportString(value));
        }

        protected void WriteColor(ref ExportContext ctx, Color value)
        {
            var alpha = value.a.ToString("0.##", CultureInfo.InvariantCulture.NumberFormat);
            if (alpha != "1")
            {
                WriteFunctionName(ref ctx, "rgba");
                ctx.Append("(");
                WriteFloatValue(ref ctx, ColorComponent(value.r));
                ctx.Append(", ");
                WriteFloatValue(ref ctx, ColorComponent(value.g));
                ctx.Append(", ");
                WriteFloatValue(ref ctx, ColorComponent(value.b));
                ctx.Append(", ");
                WriteFloatValue(ref ctx, value.a, "0.##");
                ctx.Append(")");
            }
            else
            {
                WriteFunctionName(ref ctx, "rgb");
                ctx.Append("(");
                WriteFloatValue(ref ctx, ColorComponent(value.r));
                ctx.Append(", ");
                WriteFloatValue(ref ctx, ColorComponent(value.g));
                ctx.Append(", ");
                WriteFloatValue(ref ctx, ColorComponent(value.b));
                ctx.Append(")");
            }

            return;

            static int ColorComponent(float component)
            {
                return (int)Math.Round(component * byte.MaxValue, 0, MidpointRounding.AwayFromZero);
            }
        }

        protected void WriteResourcePath(ref ExportContext ctx, string value)
        {
            WriteFunctionName(ref ctx, "resource");
            ctx.Append("(");
            WritePath(ref ctx, value);
            ctx.Append(")");
        }

        protected void WriteAssetReference(ref ExportContext ctx, UnityEngine.Object value)
        {
            var path = URIHelpers.MakeAssetUri(value);
            WriteFunctionName(ref ctx, "url");
            ctx.Append("(");
            WritePath(ref ctx, path);
            ctx.Append(")");
        }

        protected void WritePath(ref ExportContext ctx, string path)
        {
            ctx.Append("\"");
            ctx.Append(path);
            ctx.Append("\"");
        }

        protected void WriteEnum(ref ExportContext ctx, string value)
        {
            ctx.Append(value);
        }

        protected void WriteVariable(ref ExportContext ctx, string value)
        {
            WriteIdentifier(ref ctx, value);
        }

        protected void WriteIdentifier(ref ExportContext ctx, string value)
        {
            ctx.Append(value);
        }

        protected void WriteFunction(ref ExportContext ctx, string functionName, Span<StyleValueHandle> handles)
        {
            WriteFunctionName(ref ctx, functionName);
            ctx.Append("(");
            WriteStyleValueHandleBlock(ref ctx, handles);
            ctx.Append(")");
        }

        protected void WriteFunctionName(ref ExportContext ctx, string functionName)
        {
            ctx.Append(functionName);
        }

        protected void WriteString(ref ExportContext ctx, string value)
        {
            ctx.Append(value);
        }

        protected void WriteScalableImage(ref ExportContext ctx, ScalableImage value)
        {
            WriteAssetReference(ref ctx, value.normalImage);
        }

        protected void WriteMissingAssetReference(ref ExportContext ctx, string value)
        {
            WriteFunctionName(ref ctx, "url");
            ctx.Append("(");
            WritePath(ref ctx, value);
            ctx.Append(")");
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
    }
}
