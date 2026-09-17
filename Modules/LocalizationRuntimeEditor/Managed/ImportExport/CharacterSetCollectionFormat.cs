// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using Unity.SmartStrings;
using Unity.SmartStrings.Core.Formatting;
using Unity.SmartStrings.Core.Parsing;
using UnityEditor;

namespace Unity.Localization.Editor;

// Writes the distinct characters a collection's values use, for building font assets.
sealed class CharacterSetCollectionFormat : ITableCollectionExporter, ITableCollectionWindowFormat
{
    public string DisplayName => L10n.Tr("Character Set", null);

    public string FileExtension => "txt";

    public void Open(ResourceTableCollection collection) => ExportCharacterSetWindow.ShowWindow(collection);

    public void Export(TextWriter writer, ResourceTableCollection collection, ITableImportExportReporter reporter = null)
    {
        reporter?.Start("Export Character Set", collection.TableCollectionName);
        var characters = NewCharacterSet();
        foreach (var table in collection.Tables)
            CollectCharacters(table, characters);
        WriteCharacters(writer, characters);
        reporter?.Completed("Export complete");
    }

    // Entries are code points (surrogate pairs kept together), sorted by value.
    internal static SortedSet<int> NewCharacterSet() => new();

    internal static void CollectCharacters(ResourceTable table, SortedSet<int> characters)
    {
        if (table == null || table.SharedData == null)
            return;
        SmartFormatterLiteralCharacterExtractor extractor = null;
        foreach (var shared in table.SharedData.Entries)
        {
            if (shared == null)
                continue;
            var entry = table.GetEntry<StringEntry>(shared.Id);
            if (entry == null)
                continue;
            Collect(entry.Value, shared.IsSmart, ref extractor, characters);
            if (entry is VariantStringEntry variant)
            {
                foreach (var v in variant.Variants)
                    Collect(v.Value, shared.IsSmart, ref extractor, characters);
            }
        }
    }

    static void Collect(string value, bool smart, ref SmartFormatterLiteralCharacterExtractor extractor, SortedSet<int> characters)
    {
        if (string.IsNullOrEmpty(value))
            return;
        if (smart)
        {
            extractor ??= new SmartFormatterLiteralCharacterExtractor(Smart.Default);
            try
            {
                AddCodePoints(extractor.ExtractLiteralsCharacters(value), characters);
                return;
            }
            catch (ParsingErrors)
            {
                // An unparseable smart string contributes its raw text instead.
            }
            catch (FormattingException)
            {
            }
        }
        AddCodePoints(value, characters);
    }

    static void AddCodePoints(IEnumerable<char> text, SortedSet<int> characters)
    {
        var high = '\0';
        var hasHigh = false;
        foreach (var c in text)
        {
            if (hasHigh)
            {
                hasHigh = false;
                if (char.IsLowSurrogate(c))
                {
                    characters.Add(char.ConvertToUtf32(high, c));
                    continue;
                }
                characters.Add(high);
            }
            if (char.IsHighSurrogate(c))
            {
                high = c;
                hasHigh = true;
                continue;
            }
            characters.Add(c);
        }
        if (hasHigh)
            characters.Add(high);
    }

    internal static void WriteCharacters(TextWriter writer, SortedSet<int> characters)
    {
        foreach (var codePoint in characters)
        {
            // A lone surrogate from malformed input cannot go through ConvertFromUtf32; write the raw unit.
            if (codePoint <= char.MaxValue && char.IsSurrogate((char)codePoint))
                writer.Write((char)codePoint);
            else
                writer.Write(char.ConvertFromUtf32(codePoint));
        }
    }
}
