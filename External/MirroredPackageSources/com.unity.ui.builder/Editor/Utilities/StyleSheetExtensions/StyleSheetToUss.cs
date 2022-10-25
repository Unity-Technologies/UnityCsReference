using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class UssExportOptions
    {
        public UssExportOptions()
        {
            propertyIndent = "    ";
            exportDefaultValues = true;
        }

        public UssExportOptions(UssExportOptions opts)
            : base()
        {
            propertyIndent = opts.propertyIndent;
            exportDefaultValues = opts.exportDefaultValues;
        }

        public string propertyIndent { get; set; }
        public bool useColorCode { get; set; }
        public bool exportDefaultValues { get; set; }
    }

    internal class StyleSheetToUss
    {
        static int ColorComponent(float component)
        {
            return (int)Math.Round(component * byte.MaxValue, 0, MidpointRounding.AwayFromZero);
        }

        public static string ToUssString(Color color, bool useColorCode = false)
        {
            string str;
            string alpha = color.a.ToString("0.##", CultureInfo.InvariantCulture.NumberFormat);
            if (alpha != "1")
            {
                str = UnityString.Format("rgba({0}, {1}, {2}, {3:F2})", ColorComponent(color.r),
                    ColorComponent(color.g),
                    ColorComponent(color.b),
                    alpha);
            }
            else if (!useColorCode)
            {
                str = UnityString.Format("rgb({0}, {1}, {2})",
                    ColorComponent(color.r),
                    ColorComponent(color.g),
                    ColorComponent(color.b));
            }
            else
            {
                str = UnityString.Format("#{0}", ColorUtility.ToHtmlStringRGB(color));
            }
            return str;
        }

        public static string ValueHandleToUssString(StyleSheet sheet, UssExportOptions options, string propertyName, StyleValueHandle handle)
        {
            string str = "";
            switch (handle.valueType)
            {
                case StyleValueType.Keyword:
                    str = sheet.ReadKeyword(handle).ToString().ToLower();
                    break;
                case StyleValueType.Float:
                {
                    var num = sheet.ReadFloat(handle);
                    if (num == 0)
                    {
                        str = "0";
                    }
                    else
                    {
                        str = num.ToString(CultureInfo.InvariantCulture.NumberFormat);
                        if (IsLength(propertyName))
                            str += "px";
                    }
                }
                break;
                case StyleValueType.Dimension:
                    var dim = sheet.ReadDimension(handle);
                    if (dim.value == 0
                        && !dim.unit.IsTimeUnit()
                    )
                        str = "0";
                    else
                        str = dim.ToString();
                    break;
                case StyleValueType.Color:
                    UnityEngine.Color color = sheet.ReadColor(handle);
                    str = ToUssString(color, options.useColorCode);
                    break;
                case StyleValueType.ResourcePath:
                    str = $"resource('{sheet.ReadResourcePath(handle)}')";
                    break;
                case StyleValueType.Enum:
                    str = sheet.ReadEnum(handle);
                    break;
                case StyleValueType.String:
                    str = $"\"{sheet.ReadString(handle)}\"";
                    break;
                case StyleValueType.MissingAssetReference:
                    str = $"url('{sheet.ReadMissingAssetReferenceUrl(handle)}')";
                    break;
                case StyleValueType.AssetReference:
                    var assetRef = sheet.ReadAssetReference(handle);
                    var assetPath = URIHelpers.MakeAssetUri(assetRef);
                    str = assetRef == null ? "none" : $"url('{assetPath}')";
                    break;
                case StyleValueType.Variable:
                    str = sheet.ReadVariable(handle);
                    break;
                default:
                    throw new ArgumentException("Unhandled type " + handle.valueType);
            }
            return str;
        }

        private static string GetPathValueFromAssetRef(UnityEngine.Object assetRef)
        {
            var assetPath = URIHelpers.MakeAssetUri(assetRef);
            return assetRef == null ? "none" : $"url('{assetPath}')";
        }

        public static void ValueHandlesToUssString(StringBuilder sb, StyleSheet sheet, UssExportOptions options, string propertyName, StyleValueHandle[] values, ref int valueIndex, int valueCount = -1)
        {
            for (; valueIndex < values.Length && valueCount != 0; --valueCount)
            {
                var propertyValue = values[valueIndex++];
                switch (propertyValue.valueType)
                {
                    case StyleValueType.Function:
                        // First param: function name
                        sb.Append(sheet.ReadFunctionName(propertyValue));
                        sb.Append("(");

                        // Second param: number of arguments
                        var nbParams = (int)sheet.ReadFloat(values[valueIndex++]);
                        ValueHandlesToUssString(sb, sheet, options, propertyName, values, ref valueIndex, nbParams);
                        sb.Append(")");

                        break;
                    case StyleValueType.CommaSeparator:
                        sb.Append(",");
                        break;
                    default:
                    {
                        var propertyValueStr = ValueHandleToUssString(sheet, options, propertyName, propertyValue);
                        sb.Append(propertyValueStr);
                        break;
                    }
                }

                if (valueIndex < values.Length && values[valueIndex].valueType != StyleValueType.CommaSeparator && valueCount != 1)
                {
                    sb.Append(" ");
                }
            }
        }

        static bool IsLength(string name)
        {
            if (BuilderConstants.SpecialSnowflakeLengthStyles.Contains(name))
                return true;

            return false;
        }

        public static void ToUssString(StyleSheet sheet, UssExportOptions options, StyleRule rule, StringBuilder sb)
        {
            foreach (var property in rule.properties)
            {
                if (property.name == BuilderConstants.SelectedStyleRulePropertyName)
                    continue;

                sb.Append(options.propertyIndent);
                sb.Append(property.name);
                sb.Append(":");

                ToUssString(sheet, options, property, sb);
                sb.Append(";");
                sb.Append(BuilderConstants.newlineCharFromEditorSettings);
            }
        }

        public static void ToUssString(StyleSheet sheet, UssExportOptions options, StyleProperty property, StringBuilder sb)
        {
            if (property.name == "cursor" && property.values.Length > 1 && !property.IsVariable())
            {
                for (var i = 0; i < property.values.Length; i++)
                {
                    var propertyValueStr = ValueHandleToUssString(sheet, options, property.name, property.values[i]);
                    sb.Append(" ");
                    sb.Append(propertyValueStr);
                }
            }
            else
            {
                var valueIndex = 0;
                sb.Append(" ");
                ValueHandlesToUssString(sb, sheet, options, property.name, property.values, ref valueIndex);
            }
        }

        public static void ToUssString(StyleSelectorRelationship previousRelationship, StyleSelectorPart[] parts, StringBuilder sb)
        {
            if (previousRelationship != StyleSelectorRelationship.None)
                sb.Append(previousRelationship == StyleSelectorRelationship.Child ? " > " : " ");
            foreach (var selectorPart in parts)
            {
                switch (selectorPart.type)
                {
                    case StyleSelectorType.Wildcard:
                        sb.Append('*');
                        break;
                    case StyleSelectorType.Type:
                        sb.Append(selectorPart.value);
                        break;
                    case StyleSelectorType.Class:
                        sb.Append('.');
                        sb.Append(selectorPart.value);
                        break;
                    case StyleSelectorType.PseudoClass:
                        sb.Append(':');
                        sb.Append(selectorPart.value);
                        break;
                    case StyleSelectorType.ID:
                        sb.Append('#');
                        sb.Append(selectorPart.value);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public static string ToUssSelector(StyleComplexSelector complexSelector)
        {
            var sb = new StringBuilder();
            foreach (var selector in complexSelector.selectors)
            {
                ToUssString(selector.previousRelationship, selector.parts, sb);
            }
            return sb.ToString();
        }

        public static string ToUssString(StyleSheet sheet, StyleComplexSelector complexSelector, StringBuilder stringBuilder = null)
        {
            var inlineBuilder = stringBuilder == null ? new StringBuilder() : stringBuilder;

            ToUssString(sheet, new UssExportOptions(), complexSelector, inlineBuilder);

            var result = inlineBuilder.ToString();
            return result;
        }

        public static void ToUssString(StyleSheet sheet, UssExportOptions options, StyleComplexSelector complexSelector, StringBuilder sb)
        {
            foreach (var selector in complexSelector.selectors)
                ToUssString(selector.previousRelationship, selector.parts, sb);

            sb.Append(" {");
            sb.Append(BuilderConstants.newlineCharFromEditorSettings);

            ToUssString(sheet, options, complexSelector.rule, sb);

            sb.Append("}");
            sb.Append(BuilderConstants.newlineCharFromEditorSettings);
        }

        public static string ToUssString(StyleSheet sheet, UssExportOptions options = null)
        {
            if (options == null)
                options = new UssExportOptions();

            var sb = new StringBuilder();
            if (sheet.imports != null)
            {
                for (var i = 0; i < sheet.imports.Length; ++i)
                {
                    var import = sheet.imports[i];
                    if (!import.styleSheet)
                        continue;
                    var stylesheetImportPath = GetPathValueFromAssetRef(import.styleSheet);

                    // Skip invalid references.
                    if (stylesheetImportPath == "none")
                        continue;

                    sb.Append($"@import {stylesheetImportPath};");
                    sb.Append(BuilderConstants.newlineCharFromEditorSettings);
                }
            }

            if (sheet.complexSelectors != null)
            {
                bool isFirst = true;

                for (var complexSelectorIndex = 0; complexSelectorIndex < sheet.complexSelectors.Length; ++complexSelectorIndex)
                {
                    var complexSelector = sheet.complexSelectors[complexSelectorIndex];

                    // Omit special selection rule.
                    if (complexSelector.selectors.Length > 0 &&
                        complexSelector.selectors[0].parts.Length > 0 &&
                        (complexSelector.selectors[0].parts[0].value == BuilderConstants.SelectedStyleSheetSelectorName
                         || complexSelector.selectors[0].parts[0].value.StartsWith(BuilderConstants.StyleSelectorElementName)
                        )
                    )
                        continue;

                    if (isFirst)
                        isFirst = false;
                    else
                        sb.Append(BuilderConstants.newlineCharFromEditorSettings);

                    ToUssString(sheet, options, complexSelector, sb);
                }
            }

            return sb.ToString();
        }

        public static void WriteStyleSheet(StyleSheet sheet, string path, UssExportOptions options = null)
        {
            File.WriteAllText(path, ToUssString(sheet, options));
        }
    }
}
