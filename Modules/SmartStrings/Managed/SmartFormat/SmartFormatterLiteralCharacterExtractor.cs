// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.SmartStrings.Core.Extensions;
using Unity.SmartStrings.Core.Formatting;
using Unity.SmartStrings.Core.Parsing;
using Unity.SmartStrings.Extensions;
using UnityEngine.Bindings;

namespace Unity.SmartStrings;

[VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
internal class SmartFormatterLiteralCharacterExtractor : SmartFormatter
{
    List<char> m_Characters;

    public SmartFormatterLiteralCharacterExtractor(SmartFormatter parent)
    {
        Settings = parent.Settings;
        SourceExtensions.AddRange(parent.SourceExtensions);
        FormatterExtensions.AddRange(parent.FormatterExtensions);
    }

    public IEnumerable<char> ExtractLiteralsCharacters(string value)
    {
        m_Characters = new List<char>();
        Format(value, null);
        return m_Characters;
    }

    public override void Format(FormattingInfo formattingInfo)
    {
        foreach (var item in formattingInfo.Format.Items)
        {
            if (item is LiteralText literalItem)
            {
                for (var i = item.StartIndex; i < item.EndIndex; i++)
                    m_Characters.Add(item.BaseString[i]);
                continue;
            }

            // Otherwise, the item must be a placeholder.
            var placeholder = (Placeholder)item;
            var childFormattingInfo = formattingInfo.CreateChild(placeholder);

            var formatterName = childFormattingInfo.Placeholder.FormatterName;
            var comparison = Settings.GetCaseSensitivityComparison();

            // Compatibility mode does not support formatter extensions except this one:
            if (Settings.StringFormatCompatibility)
            {
                var defaultFormatter = FormatterExtensions.Find(fe => fe is DefaultFormatter);
                if (defaultFormatter is IFormatterLiteralExtractor literalExtractor)
                    literalExtractor.WriteAllLiterals(childFormattingInfo);
                continue;
            }

            // Try to evaluate using the not empty formatter name from the format string
            if (formatterName != string.Empty)
            {
                IFormatter formatterExtension = null;
                // less GC than using Linq
                foreach (var fe in FormatterExtensions)
                {
                    if (!fe.Name.Equals(formatterName, comparison)) continue;

                    formatterExtension = fe;
                    break;
                }

                if (formatterExtension is IFormatterLiteralExtractor literalExtractor)
                    literalExtractor.WriteAllLiterals(childFormattingInfo);
                continue;
            }

            // Go through all (implicit) formatters which contain an empty name
            // much higher performance and less GC than using Linq
            foreach (var fe in FormatterExtensions)
            {
                if (!fe.CanAutoDetect) continue;
                if (fe is IFormatterLiteralExtractor literalExtractor)
                    literalExtractor.WriteAllLiterals(childFormattingInfo);
            }
        }
    }
}
