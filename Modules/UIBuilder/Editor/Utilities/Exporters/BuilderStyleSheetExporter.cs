// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    class BuilderStyleSheetExporter : StyleSheetExporter
    {
        private static readonly BuilderStyleSheetExporter m_Instance = new BuilderStyleSheetExporter();
        public static BuilderStyleSheetExporter instance => m_Instance;

        private static readonly UssExportOptions s_Options = new()
        {
            ignoreSelectorList = BuilderConstants.IgnoredSelectorsWhenExporting,
            ignoreSelectorPrefixList = BuilderConstants.IgnoredSelectorPrefixesWhenExporting,
            ignorePropertyList = BuilderConstants.IgnoredStylePropertiesWhenExporting
        };

        public static UssExportOptions options => s_Options;

        public static string GetExportString(StyleSheet styleSheet)
        {
            return instance.ToUssString(styleSheet, s_Options);
        }

        public static string ExportInlineRule(StyleSheet styleSheet, StyleRule rule)
        {
            using var builderHandle = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, s_Options);
            for (var i = 0; i < rule.properties?.Length; ++i)
            {
                var property = rule.properties[i];
                if (context.options.IsPropertyIgnored(property.name))
                    continue;
                instance.WriteProperty(ref context,  property);
                context.Append(" ");
            }

            return stringBuilder.ToString().Trim();
        }

        public static string GetSelectorString(StyleComplexSelector selector)
        {
            return instance.ToUssString(null, selector, s_Options);
        }

        public static string ExportSelectorAsRule(StyleSheet styleSheet, StyleComplexSelector selector)
        {
            using var listHandle = ListPool<StyleComplexSelector>.Get(out var selectors);
            selectors.Add(selector);
            using var builderHandle = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, s_Options);
            instance.WriteSelectorAsRule(ref context, selector);
            return stringBuilder.ToString();
        }

        public static string GetStylePropertyHandlesString(StyleSheet styleSheet, Span<StyleValueHandle> handles)
        {
            using var builderHandle = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, s_Options);
            instance.WriteStyleValueHandleBlock(ref context, handles);
            return stringBuilder.ToString();
        }

        public static string GetStylePropertyHandleString(StyleSheet styleSheet, Span<StyleValueHandle> handles, int index)
        {
            using var _ = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, s_Options);
            instance.WriteStyleValueHandle(ref context, handles, ref index);
            return stringBuilder.ToString();
        }

        protected override void WriteStyleSheet(ref ExportContext ctx, StyleSheet styleSheet)
        {
            if (styleSheet.imports?.Length > 0)
            {
                WriteImportBlock(ref ctx, styleSheet.imports);
                ctx.AppendLine();
            }

            // The UI Builder operates in a selector-based fashion instead of a rule-based one.
            // What this means is that a rule containing multiple selectors will be exported as
            // distinct "rules".
            // For example:
            //     .some-selector, .some-other-selector {}
            // will become:
            //     .some-selector {}
            //     .some-other-selector {}
            // Thus here we override the behavior of the base exporter.
            using var _ = ListPool<StyleComplexSelector>.Get(out var selectors);
            selectors.AddRange(styleSheet.complexSelectors);
            selectors.RemoveAll(ctx.options.IsSelectorIgnored);

            for (var selectorIndex = 0; selectorIndex < selectors.Count; ++selectorIndex)
            {
                var styleComplexSelector = selectors[selectorIndex];

                WriteSelectorAsRule(ref ctx, styleComplexSelector);
                ctx.AppendLine();

                if (selectorIndex < selectors.Count - 1)
                    ctx.AppendLine();
            }
        }

        private void WriteSelectorAsRule(ref ExportContext ctx, StyleComplexSelector selector)
        {
            WriteSelector(ref ctx, selector);
            ctx.AppendLine(" {");
            WritePropertyBlock(ref ctx, selector.rule.properties);
            ctx.Append("}");
        }
    }
}
