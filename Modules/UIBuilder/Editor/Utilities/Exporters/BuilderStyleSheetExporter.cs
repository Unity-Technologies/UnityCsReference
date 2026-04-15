// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderStyleSheetExporter : StyleSheetExporter
    {
        private static readonly BuilderStyleSheetExporter m_Instance = new BuilderStyleSheetExporter();
        public static BuilderStyleSheetExporter instance => m_Instance;

        public static string GetExportString(StyleSheet styleSheet)
        {
            return instance.ToUssString(styleSheet, UssExportOptions.Default);
        }

        public static string GetSelectorString(StyleComplexSelector selector)
        {
            return instance.ToUssString(null, selector, UssExportOptions.Default);
        }

        public static string ExportSelectorAsRule(StyleSheet styleSheet, StyleComplexSelector selector)
        {
            using var listHandle = ListPool<StyleComplexSelector>.Get(out var selectors);
            selectors.Add(selector);
            using var builderHandle = StringBuilderPool.Get(out var stringBuilder);
            var context = new ExportContext(styleSheet, stringBuilder, UssExportOptions.Default);
            instance.WriteSelectorAsRule(ref context, selector);
            return stringBuilder.ToString();
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
