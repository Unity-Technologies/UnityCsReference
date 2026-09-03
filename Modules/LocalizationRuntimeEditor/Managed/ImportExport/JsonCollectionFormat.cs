// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Localization.Providers.FileTables;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Localization.Editor;

sealed class JsonCollectionFormat : ITableCollectionExporter, ITableCollectionImporter
{
    [Serializable]
    class CollectionSnapshot
    {
        public string CollectionName;
        public string CollectionGuid;
        public List<ResourceTableData> Locales = new();
    }

    public string DisplayName => "JSON";

    public string FileExtension => "json";

    public void Export(TextWriter writer, ResourceTableCollection collection, ITableImportExportReporter reporter = null)
    {
        var shared = collection != null ? collection.SharedData : null;
        if (shared == null)
            return;

        reporter?.Start("Export JSON", collection.TableCollectionName);
        var snapshot = new CollectionSnapshot
        {
            CollectionName = shared.TableCollectionName,
            CollectionGuid = shared.TableCollectionNameGuid.ToString()
        };
        // ToData drops entries flagged out of the editor export (they stay in the collection), so no extra filter here.
        foreach (var table in collection.Tables)
        {
            if (table != null)
                snapshot.Locales.Add(TableDataBuilder.ToData(table, ExportKind.Editor));
        }
        writer.Write(JsonUtility.ToJson(snapshot, true));
        reporter?.Completed("Export complete");
    }

    public void ImportInto(TextReader reader, ResourceTableCollection collection, TableImportOptions options, ITableImportExportReporter reporter = null)
    {
        if (collection == null || collection.SharedData == null)
            return;

        var snapshot = JsonUtility.FromJson<CollectionSnapshot>(reader.ReadToEnd());
        if (snapshot?.Locales == null)
            return;

        reporter?.Start("Import JSON", collection.TableCollectionName);
        try
        {
            if (options.CreateUndo)
                Undo.RegisterCompleteObjectUndo(CollectionMutation.UndoTargets(collection), "Import JSON");

            var processed = new HashSet<long>();
            foreach (var data in snapshot.Locales)
                ApplyLocale(collection, data, options, processed);

            if (options.RemoveMissingEntries)
            {
                // Keys the export could not represent are not in the file; keep them rather than deleting them.
                TableDataBuilder.AddNonExportableKeys(collection, ExportKind.Editor, processed);
                CollectionMutation.RemoveMissing(collection, processed);
            }

            CollectionMutation.SaveImport(collection);
            reporter?.Completed("Import complete");
        }
        catch (Exception e)
        {
            CollectionMutation.MarkImportDirty(collection);
            reporter?.Fail(e.Message);
            throw;
        }
    }

    // Rebuilt into throwaway instances to reuse the runtime converter's id, variant and selector handling, then merged.
    static void ApplyLocale(ResourceTableCollection collection, ResourceTableData data, TableImportOptions options, HashSet<long> processed)
    {
        var temp = TableDataConverter.FromData(data);
        if (temp == null)
            return;

        var tempShared = temp.SharedData;
        try
        {
            if (tempShared != null)
            {
                foreach (var tempKey in tempShared.Entries)
                {
                    if (tempKey == null)
                        continue;
                    var keyEntry = CollectionMutation.ResolveKey(collection, tempKey.Key, tempKey.Id, options);
                    if (keyEntry == null)
                        continue;
                    keyEntry.Flags = tempKey.Flags;
                    CopyVariantMetadata(tempKey, keyEntry);
                    processed.Add(keyEntry.Id);
                }
            }

            var table = CollectionMutation.ResolveTable(collection, temp.LocaleIdentifier, options);
            if (table != null)
            {
                var importedIds = new HashSet<long>();
                foreach (var entry in temp.Entries)
                {
                    if (entry is not ResourceEntryBase dataEntry)
                        continue;
                    var keyName = tempShared != null ? tempShared.GetKey(entry.KeyId) : null;
                    var id = string.IsNullOrEmpty(keyName) ? 0 : collection.SharedData.GetId(keyName);
                    if (id == 0)
                        continue;
                    // AddEntry reparents the entry, so the temp table can be destroyed without disturbing it.
                    dataEntry.SetKeyId(id);
                    table.AddEntry(dataEntry);
                    importedIds.Add(id);
                }
                if (options.RemoveMissingEntries)
                    RemoveMissingLocaleEntries(table, importedIds);
            }
        }
        finally
        {
            Object.DestroyImmediate(temp);
            if (tempShared != null)
                Object.DestroyImmediate(tempShared);
        }
    }

    static void RemoveMissingLocaleEntries(ResourceTable table, HashSet<long> importedIds)
    {
        List<long> toRemove = null;
        foreach (var entry in table.Entries)
        {
            if (entry == null || importedIds.Contains(entry.KeyId))
                continue;
            if (TableDataBuilder.IsExportable(entry, ExportKind.Editor))
                (toRemove ??= new List<long>()).Add(entry.KeyId);
        }
        if (toRemove == null)
            return;
        foreach (var id in toRemove)
            table.RemoveEntry(id);
    }

    // Mirror the per-key variant structure by replacing the live key's VariantSelectorMetadata with the rebuilt one.
    static void CopyVariantMetadata(SharedTableData.SharedTableEntry from, SharedTableData.SharedTableEntry to)
    {
        var existing = to.Metadata.GetMetadata<VariantSelectorMetadata>();
        if (existing != null)
            to.Metadata.RemoveMetadata(existing);
        var source = from.Metadata.GetMetadata<VariantSelectorMetadata>();
        if (source != null)
            to.Metadata.AddMetadata(source);
    }
}
