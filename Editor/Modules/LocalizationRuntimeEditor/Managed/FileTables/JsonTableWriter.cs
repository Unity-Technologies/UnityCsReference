// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Localization.Providers.FileTables;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Localization.Editor;

// The default ITableFileWriter, paired with the runtime JsonTableReader. Hand written rather than JsonUtility.ToJson
// because that writes every field, so a table of plain strings came out mostly empty defaults. A member left out here
// is one the reader would default to the same value anyway.
sealed class JsonTableWriter : ITableFileWriter
{
    [NoAutoStaticsCleanup] // stateless: the shared writer holds nothing a reload could invalidate
    public static readonly JsonTableWriter Instance = new();

    const string k_L1 = "    ";
    const string k_L2 = "        ";
    const string k_L3 = "            ";

    public void Write(ResourceTableData data, Stream stream)
    {
        var text = new StringBuilder();
        text.Append('{');
        Member(text, k_L1, "SchemaVersion", first: true);
        AppendString(text, data.SchemaVersion);
        Member(text, k_L1, "CollectionName");
        AppendString(text, data.CollectionName);
        Member(text, k_L1, "CollectionGuid");
        AppendString(text, data.CollectionGuid);
        Member(text, k_L1, "LocaleCode");
        AppendString(text, data.LocaleCode);
        Member(text, k_L1, "Entries");
        AppendEntries(text, data.Entries);
        text.Append("\n}");

        using var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true);
        writer.Write(text.ToString());
    }

    static void AppendEntries(StringBuilder text, List<EntryData> entries)
    {
        text.Append('[');
        var first = true;
        for (var i = 0; entries != null && i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null)
                continue;
            text.Append(first ? "\n" : ",\n").Append(k_L2).Append('{');
            first = false;

            Member(text, k_L3, "Id", first: true);
            text.Append(entry.Id.ToString(CultureInfo.InvariantCulture));
            OptionalString(text, "Key", entry.Key);
            if (entry.IsSmart)
            {
                Member(text, k_L3, "IsSmart");
                text.Append("true");
            }
            AppendVariantKeys(text, entry.VariantKeys);
            AppendSelector(text, entry.Selector);
            // An empty translation is a real value, so only a missing one is left out.
            if (entry.Value != null)
            {
                Member(text, k_L3, "Value");
                AppendString(text, entry.Value);
            }
            AppendVariants(text, entry.Variants);
            OptionalString(text, "TypeName", entry.TypeName);
            OptionalString(text, "EntryJson", entry.EntryJson);

            text.Append('\n').Append(k_L2).Append('}');
        }
        if (!first)
            text.Append('\n').Append(k_L1);
        text.Append(']');
    }

    // The three shapes below only appear on variant-driven keys, so they stay on one line and out of the way.
    static void AppendVariantKeys(StringBuilder text, List<string> keys)
    {
        if (keys == null || keys.Count == 0)
            return;
        Member(text, k_L3, "VariantKeys");
        text.Append('[');
        for (var i = 0; i < keys.Count; i++)
        {
            if (i > 0)
                text.Append(", ");
            AppendString(text, keys[i]);
        }
        text.Append(']');
    }

    static void AppendSelector(StringBuilder text, SelectorData selector)
    {
        if (string.IsNullOrEmpty(selector.TypeName))
            return;
        Member(text, k_L3, "Selector");
        text.Append("{\"TypeName\": ");
        AppendString(text, selector.TypeName);
        if (!string.IsNullOrEmpty(selector.Json))
        {
            text.Append(", \"Json\": ");
            AppendString(text, selector.Json);
        }
        text.Append('}');
    }

    static void AppendVariants(StringBuilder text, List<VariantData> variants)
    {
        if (variants == null || variants.Count == 0)
            return;
        Member(text, k_L3, "Variants");
        text.Append('[');
        for (var i = 0; i < variants.Count; i++)
        {
            text.Append(i > 0 ? ", {\"Key\": " : "{\"Key\": ");
            AppendString(text, variants[i].Key);
            text.Append(", \"Value\": ");
            AppendString(text, variants[i].Value);
            text.Append('}');
        }
        text.Append(']');
    }

    static void OptionalString(StringBuilder text, string name, string value)
    {
        if (string.IsNullOrEmpty(value))
            return;
        Member(text, k_L3, name);
        AppendString(text, value);
    }

    static void Member(StringBuilder text, string indent, string name, bool first = false)
    {
        if (!first)
            text.Append(',');
        text.Append('\n').Append(indent).Append('"').Append(name).Append("\": ");
    }

    static void AppendString(StringBuilder text, string value)
    {
        if (value == null)
        {
            text.Append("\"\"");
            return;
        }
        text.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '"': text.Append("\\\""); break;
                case '\\': text.Append("\\\\"); break;
                case '\b': text.Append("\\b"); break;
                case '\f': text.Append("\\f"); break;
                case '\n': text.Append("\\n"); break;
                case '\r': text.Append("\\r"); break;
                case '\t': text.Append("\\t"); break;
                default:
                    if (c < ' ')
                        text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                    else
                        text.Append(c);
                    break;
            }
        }
        text.Append('"');
    }
}
