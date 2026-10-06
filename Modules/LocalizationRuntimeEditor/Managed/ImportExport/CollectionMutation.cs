// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Unity.Localization.Editor;

static class CollectionMutation
{
    /// <summary>
    /// Returns whether an imported file's key ids belong to the collection it is being imported into.
    /// </summary>
    /// <remarks>
    /// Only formats that record the collection they were exported from can answer this, by writing the collection's
    /// <see cref="SharedTableData.TableCollectionNameGuid"/> into the file and passing it back here on import. When
    /// either side has no readable guid there is nothing to compare, so the answer is
    /// <paramref name="ambiguous"/> and the caller keeps whatever it would have done otherwise.
    /// Key ids are only unique within one collection, so a caller that gets <see langword="false"/> back must pass
    /// <c>0</c> to <see cref="ResolveKey"/> in place of the file's id. Passing it through would let a row rename or
    /// overwrite whichever unrelated key in the destination happens to hold the same number.
    /// </remarks>
    /// <param name="collection">The collection being imported into.</param>
    /// <param name="collectionGuid">The collection guid recorded in the file, in its 32-character hexadecimal form.</param>
    /// <param name="ambiguous">The answer to give when either guid is missing or unreadable.</param>
    /// <returns><c>true</c> when the file's ids can be trusted against this collection; otherwise, <c>false</c>.</returns>
    public static bool CameFrom(ResourceTableCollection collection, string collectionGuid, bool ambiguous)
    {
        var destination = collection.SharedData.TableCollectionNameGuid;
        if (destination.Empty() || string.IsNullOrEmpty(collectionGuid) || !UnityEngine.GUID.TryParse(collectionGuid, out var source))
            return ambiguous;
        return source == destination;
    }

    // Finds or (per options) creates the shared key for a row, honoring an id hint for renames.
    public static SharedTableData.SharedTableEntry ResolveKey(ResourceTableCollection collection, string keyName, long id, TableImportOptions options)
    {
        var shared = collection.SharedData;
        if (shared == null)
            return null;

        if (id != 0 && shared.GetEntry(id) is { } byId)
        {
            if (!string.IsNullOrEmpty(keyName) && byId.Key != keyName)
                shared.RenameKey(id, keyName);
            return byId;
        }

        if (string.IsNullOrEmpty(keyName))
            return null;

        if (shared.GetEntry(keyName) is { } byName)
            return byName;

        if (!options.CreateMissingKeys)
            return null;

        return id != 0 ? shared.AddKey(keyName, id) ?? shared.AddKey(keyName) : shared.AddKey(keyName);
    }

    public static void SetSmart(SharedTableData.SharedTableEntry entry, bool smart)
    {
        if (entry != null)
            entry.IsSmart = smart;
    }

    // Sets, updates, or (when empty) removes a Comment on a metadata collection.
    public static void SetComment(MetadataCollection metadata, string comment)
    {
        var existing = metadata.GetMetadata<Comment>();
        if (string.IsNullOrEmpty(comment))
        {
            if (existing != null)
                metadata.RemoveMetadata(existing);
            return;
        }
        if (existing == null)
        {
            existing = new Comment();
            metadata.AddMetadata(existing);
        }
        existing.CommentText = comment;
    }

    public static ResourceTable ResolveTable(ResourceTableCollection collection, LocaleIdentifier locale, TableImportOptions options)
    {
        var table = collection.GetTable(locale);
        if (table != null || !options.CreateMissingLocaleTables)
            return table;
        var loc = LocalizationEditorSettings.ActiveSettings != null ? LocalizationEditorSettings.ActiveSettings.GetLocale(locale) : null;
        return loc != null ? LocalizationTableAuthoring.EnsureLocaleTable(collection, loc) : null;
    }

    public static void RemoveMissing(ResourceTableCollection collection, HashSet<long> processedIds)
    {
        var shared = collection.SharedData;
        if (shared == null)
            return;

        var toRemove = new List<SharedTableData.SharedTableEntry>();
        foreach (var entry in shared.Entries)
        {
            if (entry != null && !processedIds.Contains(entry.Id))
                toRemove.Add(entry);
        }
        foreach (var entry in toRemove)
        {
            foreach (var table in collection.Tables)
                table?.RemoveEntry(entry.Id);
            shared.RemoveKey(entry.Key);
        }
    }

    public static Object[] UndoTargets(ResourceTableCollection collection)
    {
        var targets = new List<Object> { collection };
        if (collection.SharedData != null)
            targets.Add(collection.SharedData);
        foreach (var table in collection.Tables)
        {
            if (table != null)
                targets.Add(table);
        }
        return targets.ToArray();
    }

    public static void MarkImportDirty(ResourceTableCollection collection)
    {
        if (collection.SharedData != null)
        {
            collection.SharedData.InvalidateCache();
            EditorUtility.SetDirty(collection.SharedData);
        }
        EditorUtility.SetDirty(collection);
        foreach (var table in collection.Tables)
        {
            if (table == null)
                continue;
            table.InvalidateCache();
            EditorUtility.SetDirty(table);
        }
    }

    public static void SaveImport(ResourceTableCollection collection)
    {
        MarkImportDirty(collection);
        AssetDatabase.SaveAssetIfDirty(collection.SharedData);
        AssetDatabase.SaveAssetIfDirty(collection);
        foreach (var table in collection.Tables)
        {
            if (table != null)
                AssetDatabase.SaveAssetIfDirty(table);
        }
        LocalizationEditorSettings.RaiseCollectionsChanged();
    }
}
