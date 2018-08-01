// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.StyleSheets;

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]

namespace UnityEditor.StyleSheets
{
    internal class UssComments
    {
        public Dictionary<StyleRule, string> ruleComments { get; private set; }
        public Dictionary<StyleProperty, string> propertyComments { get; private set; }
        public UssComments()
        {
            ruleComments = new Dictionary<StyleRule, string>();
            propertyComments = new Dictionary<StyleProperty, string>();
        }

        public string Get(StyleRule rule)
        {
            string comment;
            if (!ruleComments.TryGetValue(rule, out comment))
            {
                comment = "";
            }
            return comment;
        }

        public string Get(StyleProperty property)
        {
            string comment;
            if (!propertyComments.TryGetValue(property, out comment))
            {
                comment = "";
            }
            return comment;
        }

        public void TryGet(StyleRule rule, Action<string> next)
        {
            string comment;
            if (ruleComments.TryGetValue(rule, out comment))
            {
                next(comment);
            }
        }

        public void TryGet(StyleProperty property, Action<string> next)
        {
            string comment;
            if (propertyComments.TryGetValue(property, out comment))
            {
                next(comment);
            }
        }

        public void AddComment(StyleRule rule, string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                ruleComments.Add(rule, comment);
            }
        }

        public void AddComment(StyleProperty property, string comment)
        {
            if (!string.IsNullOrEmpty(comment))
            {
                propertyComments.Add(property, comment);
            }
        }
    }

    internal class UssExportOptions
    {
        public UssExportOptions()
        {
            comments = new UssComments();
            propertyIndent = "    ";
            withComment = true;
            exportDefaultValues = true;
        }

        public UssExportOptions(UssExportOptions opts)
            : base()
        {
            comments = opts.comments ?? new UssComments();
            propertyIndent = opts.propertyIndent;
            withComment = opts.withComment;
            exportDefaultValues = opts.exportDefaultValues;
        }

        public string propertyIndent { get; set; }
        public bool useColorCode { get; set; }
        public bool withComment { get; set; }
        public UssComments comments { get; set; }
        public bool exportDefaultValues { get; set; }

        public void AddComment(StyleRule rule, string comment)
        {
            if (withComment && !string.IsNullOrEmpty(comment))
            {
                comments.ruleComments.Add(rule, comment);
            }
        }

        public void AddComment(StyleProperty property, string comment)
        {
            if (withComment && !string.IsNullOrEmpty(comment))
            {
                comments.propertyComments.Add(property, comment);
            }
        }
    }

    internal class StyleSheetToUss
    {
        static void AddComment(StringBuilder sb, string comment, string indent = "")
        {
            sb.Append(indent);
            var lines = comment.Split('\n');
            if (lines.Length == 1)
            {
                sb.Append("/* ");
                sb.Append(comment);
                sb.Append(" */\n");
            }
            else
            {
                sb.Append("/*\n");
                foreach (var line in lines)
                {
                    sb.Append("   ");
                    sb.Append(indent);
                    sb.Append(line);
                    sb.Append("\n");
                }
                sb.Append(indent);
                sb.Append("*/\n");
            }
        }

        static int ColorComponent(float component)
        {
            return (int)Math.Round(component * byte.MaxValue, 0, MidpointRounding.AwayFromZero);
        }

        public static string ToUssString(UnityEngine.Color color, bool useColorCode = false)
        {
            string str;
            string alpha = color.a.ToString("0.##");
            if (alpha != "1")
            {
                str = string.Format("rgba({0}, {1}, {2}, {3:F2})", ColorComponent(color.r),
                    ColorComponent(color.g),
                    ColorComponent(color.b),
                    alpha);
            }
            else if (!useColorCode)
            {
                str = string.Format("rgb({0}, {1}, {2})",
                    ColorComponent(color.r),
                    ColorComponent(color.g),
                    ColorComponent(color.b));
            }
            else
            {
                str = string.Format("#{0}", ColorUtility.ToHtmlStringRGB(color));
            }
            return str;
        }

        public static string ToUssString(StyleSheet sheet, UssExportOptions options, StyleValueHandle handle)
        {
            string str = "";
            switch (handle.valueType)
            {
                case StyleValueType.Keyword:
                    str = sheet.ReadKeyword(handle).ToString().ToLower();
                    break;
                case StyleValueType.Float:
                    str = sheet.ReadFloat(handle).ToString();
                    break;
                case StyleValueType.Color:
                    UnityEngine.Color color = sheet.ReadColor(handle);
                    str = ToUssString(color, options.useColorCode);
                    break;
                case StyleValueType.ResourcePath:
                    str = string.Format("resource(\"{0}\")", sheet.ReadResourcePath(handle));
                    break;
                case StyleValueType.Enum:
                    str = sheet.ReadEnum(handle);
                    break;
                case StyleValueType.String:
                    str = string.Format("\"{0}\"", sheet.ReadString(handle));
                    break;
                default:
                    throw new ArgumentException("Unhandled type " + handle.valueType);
            }
            return str;
        }

        public static void ToUssString(StyleSheet sheet, UssExportOptions options, StyleRule rule, StringBuilder sb)
        {
            foreach (var property in rule.properties)
            {
                options.comments.TryGet(property, comment =>
                {
                    if (rule.properties[0] != property)
                    {
                        sb.Append("\n");
                    }
                    AddComment(sb, comment, options.propertyIndent);
                });
                sb.Append(options.propertyIndent);
                sb.Append(property.name);
                sb.Append(":");
                if (property.name == "cursor" && property.values.Length > 1)
                {
                    int i;
                    string propertyValueStr;
                    for (i = 0; i < property.values.Length - 1; i++)
                    {
                        propertyValueStr = ToUssString(sheet, options, property.values[i]);
                        sb.Append(" ");
                        sb.Append(propertyValueStr);
                    }
                    sb.Append(", ");
                    propertyValueStr = ToUssString(sheet, options,  property.values[i]);
                    sb.Append(propertyValueStr);
                }
                else
                {
                    foreach (var propertyValue in property.values)
                    {
                        var propertyValueStr = ToUssString(sheet, options, propertyValue);
                        sb.Append(" ");
                        sb.Append(propertyValueStr);
                    }
                }

                sb.Append(";\n");
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

        public static void ToUssString(StyleSheet sheet, UssExportOptions options, StyleComplexSelector complexSelector, StringBuilder sb)
        {
            options.comments.TryGet(complexSelector.rule, comment => AddComment(sb, comment));
            foreach (var selector in complexSelector.selectors)
            {
                ToUssString(selector.previousRelationship, selector.parts, sb);
            }

            sb.Append(" {\n");

            ToUssString(sheet, options, complexSelector.rule, sb);

            sb.Append("}");
            sb.Append("\n");
        }

        public static string ToUssString(StyleSheet sheet, UssExportOptions options = null)
        {
            if (options == null)
            {
                options = new UssExportOptions();
            }
            var sb = new StringBuilder();
            if (sheet.complexSelectors != null)
            {
                for (var complexSelectorIndex = 0; complexSelectorIndex < sheet.complexSelectors.Length; ++complexSelectorIndex)
                {
                    var complexSelector = sheet.complexSelectors[complexSelectorIndex];
                    ToUssString(sheet, options, complexSelector, sb);
                    if (complexSelectorIndex != sheet.complexSelectors.Length - 1)
                    {
                        sb.Append("\n");
                    }
                }
            }

            return sb.ToString();
        }

        public static void WriteStyleSheet(StyleSheet sheet, string path)
        {
            File.WriteAllText(path, ToUssString(sheet));
        }
    }
}
