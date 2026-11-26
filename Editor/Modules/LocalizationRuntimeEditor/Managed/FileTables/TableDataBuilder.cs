// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Localization.Providers.FileTables;
using UnityEngine;

namespace Unity.Localization.Editor;

// Whether a snapshot is generated for a built player or exported for translators; each honors its own exclude flag.
enum ExportKind { Player, Editor }

static class TableDataBuilder
{
    internal readonly struct SkippedEntries
    {
        internal SkippedEntries(int count, IReadOnlyList<string> kinds)
        {
            Count = count;
            Kinds = kinds ?? Array.Empty<string>();
        }

        internal int Count { get; }
        internal IReadOnlyList<string> Kinds { get; }
    }

    // A snapshot carries file-data entries, except any flagged out of the chosen export (kept in the table asset).
    internal static bool IsExportable(IResourceEntry entry, ExportKind kind)
        => TableDataConverter.IsFileData(entry) && !IsExcluded(entry, kind);

    static bool IsExcluded(IResourceEntry entry, ExportKind kind)
        => kind == ExportKind.Player
            ? TableDataConverter.IsExcludedFromPlayerExport(entry)
            : TableDataConverter.IsExcludedFromEditorExport(entry);

    internal static SkippedEntries Classify(ResourceTable table, ExportKind kind)
    {
        var count = 0;
        List<string> kinds = null;
        if (table != null)
        {
            foreach (var entry in table.Entries)
            {
                if (entry == null || IsExportable(entry, kind))
                    continue;
                count++;
                var typeName = entry.GetType().Name;
                kinds ??= new List<string>();
                if (!kinds.Contains(typeName))
                    kinds.Add(typeName);
            }
        }
        return new SkippedEntries(count, kinds);
    }

    // Ids of keys the export cannot represent, so a replace-mode import does not delete them.
    internal static void AddNonExportableKeys(ResourceTableCollection collection, ExportKind kind, HashSet<long> ids)
    {
        var shared = collection != null ? collection.SharedData : null;
        if (shared == null)
            return;
        foreach (var keyEntry in shared.Entries)
        {
            if (keyEntry == null || ids.Contains(keyEntry.Id))
                continue;
            var excludedAtKey = kind == ExportKind.Editor
                ? keyEntry.Metadata.Contains<ExcludeEntryFromEditorExport>()
                : keyEntry.Metadata.Contains<ExcludeEntryFromPlayerExport>();
            var hasEntry = false;
            var anyExportable = false;
            foreach (var table in collection.Tables)
            {
                var entry = table?.GetEntry(keyEntry.Id);
                if (entry == null)
                    continue;
                hasEntry = true;
                if (IsExportable(entry, kind))
                {
                    anyExportable = true;
                    break;
                }
            }
            if (excludedAtKey || (hasEntry && !anyExportable))
                ids.Add(keyEntry.Id);
        }
    }

    internal static ResourceTableData ToData(ResourceTable table, ExportKind kind)
    {
        var data = ToData(table, kind, out var skipped);
        if (skipped.Count > 0)
            Debug.LogWarning($"Left {skipped.Count} entr{(skipped.Count == 1 ? "y" : "ies")} in table '{data.CollectionName}' ({data.LocaleCode}) out of the export; they stay in the table ({string.Join(", ", skipped.Kinds)}).");
        return data;
    }

    internal static ResourceTableData ToData(ResourceTable table, ExportKind kind, out SkippedEntries skipped)
    {
        var data = new ResourceTableData();
        if (table == null)
        {
            skipped = new SkippedEntries(0, null);
            return data;
        }

        var shared = table.SharedData;
        data.CollectionName = shared != null ? shared.TableCollectionName : null;
        data.CollectionGuid = shared != null && !shared.TableCollectionNameGuid.Empty() ? shared.TableCollectionNameGuid.ToString() : null;
        data.LocaleCode = table.LocaleIdentifier.Code;

        var skippedCount = 0;
        List<string> skippedKinds = null;
        foreach (var entry in table.Entries)
        {
            if (entry == null)
                continue;

            if (!IsExportable(entry, kind))
            {
                // Object-reference entries and entries flagged out of this export stay in the table asset; record once.
                skippedCount++;
                var typeName = entry.GetType().Name;
                skippedKinds ??= new List<string>();
                if (!skippedKinds.Contains(typeName))
                    skippedKinds.Add(typeName);
                continue;
            }

            EntryData row;
            if (entry is StringEntry stringEntry)
            {
                row = new EntryData { Value = stringEntry.Value };
                if (stringEntry is VariantStringEntry variant)
                {
                    foreach (var value in variant.Variants)
                        row.Variants.Add(new VariantData { Key = value.Key, Value = value.Value });
                }
            }
            else
            {
                row = new EntryData
                {
                    TypeName = entry.GetType().AssemblyQualifiedName,
                    EntryJson = JsonUtility.ToJson(entry)
                };
            }

            FillSharedFields(row, shared, entry.KeyId);
            data.Entries.Add(row);
        }

        skipped = new SkippedEntries(skippedCount, skippedKinds);
        return data;
    }

    // The shared key data repeats in every per-locale row so a single file can rebuild the shared data it needs.
    static void FillSharedFields(EntryData row, SharedTableData shared, long keyId)
    {
        row.Id = keyId;
        var keyEntry = shared != null ? shared.GetEntry(keyId) : null;
        if (keyEntry == null)
            return;
        row.Id = keyEntry.Id;
        row.Key = keyEntry.Key;
        row.IsSmart = keyEntry.IsSmart;
        var keyMeta = keyEntry.Metadata.GetMetadata<VariantSelectorMetadata>();
        var collectionMeta = shared.Metadata.GetMetadata<VariantSelectorMetadata>();
        var keys = keyMeta?.Keys ?? collectionMeta?.Keys;
        var selector = keyMeta?.Selector ?? collectionMeta?.Selector;
        if ((keys == null || keys.Count == 0) && selector == null)
            return;
        if (keys != null)
            row.VariantKeys = new List<string>(keys);
        if (selector != null)
            row.Selector = new SelectorData
            {
                TypeName = selector.GetType().AssemblyQualifiedName,
                Json = JsonUtility.ToJson(selector)
            };
    }
}
