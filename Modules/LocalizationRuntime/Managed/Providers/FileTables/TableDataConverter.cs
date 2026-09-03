// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Bindings;

namespace Unity.Localization.Providers.FileTables;

/// <summary>
/// Rebuilds a table from a flat snapshot at runtime.
/// </summary>
/// <remarks>
/// Building a snapshot from a table is the reverse direction and lives on the editor side, because export is
/// editor-only. This converter carries string entries and plain-data entries that implement
/// <see cref="IFileDataEntry"/>; entries holding object references are grafted from the shipped table asset instead
/// (see <see cref="GraftNonExportable"/>).
/// </remarks>
/// <seealso cref="FileTableProvider"/>
/// <seealso cref="ResourceTableData"/>
/// <seealso cref="IFileDataEntry"/>
[VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
internal static partial class TableDataConverter
{
    // The single definition of what ships as file data; the editor's table builder classifies with it too.
    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal static bool IsFileData(IResourceEntry entry) => entry is StringEntry or IFileDataEntry;

    // ExcludeEntryFromPlayerExport entries stay in the table asset and are grafted at runtime, like object references.
    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal static bool IsExcludedFromPlayerExport(IResourceEntry entry) => HasFlag<ExcludeEntryFromPlayerExport>(entry);

    // Entries flagged ExcludeEntryFromEditorExport are left out of the files exported for translators.
    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal static bool IsExcludedFromEditorExport(IResourceEntry entry) => HasFlag<ExcludeEntryFromEditorExport>(entry);

    static bool HasFlag<T>(IResourceEntry entry) where T : class, IMetadata
        => entry != null && (entry.SharedEntry?.Metadata.Contains<T>() == true || entry.Metadata.Contains<T>());

    /// <summary>
    /// Rebuilds a table and its shared data from a snapshot.
    /// </summary>
    /// <remarks>
    /// Must run on the main thread; the created <see cref="ResourceTable"/> and <see cref="SharedTableData"/> instances
    /// use <see cref="UnityEngine.HideFlags.HideAndDontSave"/>. String values and entries that implement
    /// <see cref="IFileDataEntry"/> are rebuilt from the snapshot; entries holding object references are grafted from
    /// the shipped table asset with <see cref="GraftNonExportable"/>.
    /// </remarks>
    /// <param name="data">The snapshot to rebuild from.</param>
    /// <returns>The rebuilt table, or <see langword="null"/> when <paramref name="data"/> is <see langword="null"/>.</returns>
    public static ResourceTable FromData(ResourceTableData data)
    {
        if (data == null)
            return null;

        var shared = ScriptableObject.CreateInstance<SharedTableData>();
        shared.hideFlags = HideFlags.HideAndDontSave;
        shared.name = $"{data.CollectionName} Shared Data";
        shared.TableCollectionName = data.CollectionName;
        if (GUID.TryParse(data.CollectionGuid, out var collectionGuid))
            shared.TableCollectionNameGuid = collectionGuid;

        var table = ScriptableObject.CreateInstance<ResourceTable>();
        table.hideFlags = HideFlags.HideAndDontSave;
        table.name = $"{data.CollectionName} {data.LocaleCode}";
        table.LocaleIdentifier = new LocaleIdentifier(data.LocaleCode);
        table.SharedData = shared;

        if (data.Entries == null)
            return table;

        try
        {
            FillTable(data, shared, table);
        }
        catch
        {
            DestroyObject(table);
            DestroyObject(shared);
            throw;
        }

        return table;
    }

    static void DestroyObject(UnityEngine.Object obj)
    {
        if (Application.isPlaying)
            UnityEngine.Object.Destroy(obj);
        else
            UnityEngine.Object.DestroyImmediate(obj);
    }

    static void FillTable(ResourceTableData data, SharedTableData shared, ResourceTable table)
    {
        // Counted, not logged per row: a corrupt file can hold millions of clashing rows, and the key text is untrusted.
        var skipped = 0;
        string firstSkippedKey = null;
        foreach (var row in data.Entries)
        {
            if (row == null)
                continue;
            // An id of 0 means the file omitted it; match by key and let the shared data assign a new id.
            var keyEntry = row.Id != 0 ? shared.AddKey(row.Key, row.Id) : shared.AddKey(row.Key);
            if (keyEntry == null)
            {
                if (!string.IsNullOrEmpty(row.Key))
                {
                    skipped++;
                    firstSkippedKey ??= row.Key;
                }
                continue;
            }
            keyEntry.IsSmart = row.IsSmart;
            ApplyVariantMetadata(keyEntry, RebuildSelector(row.Selector), row.VariantKeys);

            var entry = BuildEntry(row, keyEntry.Id);
            if (entry != null)
                table.AddEntry(entry);
        }

        if (skipped > 0)
            Debug.LogWarning($"Skipped {skipped} localization key(s) whose id was already used by another key, starting with \"{firstSkippedKey}\".");
    }

    /// <summary>
    /// Copies the entries a file snapshot cannot carry from a donor table into a target table, adding any missing keys.
    /// </summary>
    /// <remarks>
    /// The entries copied are those the file does not carry: entries that are neither <see cref="StringEntry"/> nor
    /// <see cref="IFileDataEntry"/> (so they hold object references), plus any entry flagged with
    /// <see cref="ExcludeEntryFromPlayerExport"/>. Each entry is copied into the target rather than shared, so releasing the
    /// target does not disturb the donor. The object references inside the copies still point at the donor asset's
    /// assets, so the donor must stay loaded while the target is in use.
    /// </remarks>
    /// <param name="target">The table rebuilt from file data.</param>
    /// <param name="donor">The shipped table asset holding the object-reference entries.</param>
    public static void GraftNonExportable(ResourceTable target, ResourceTable donor)
    {
        if (target == null || donor == null)
            return;
        foreach (var entry in donor.Entries)
        {
            // File-data entries are already written to the file, unless flagged out of player export; graft the rest.
            if (entry == null || entry.KeyId == 0 || (IsFileData(entry) && !IsExcludedFromPlayerExport(entry)))
                continue;
            if (target.GetEntry(entry.KeyId) != null)
                continue;
            EnsureKey(target.SharedData, donor.SharedData, entry.KeyId);
            var clone = CloneEntry(entry);
            if (clone != null)
                target.AddEntry(clone);
        }
    }

    static IResourceEntry CloneEntry(IResourceEntry entry)
    {
        if (JsonUtility.FromJson(JsonUtility.ToJson(entry), entry.GetType()) is not IResourceEntry clone)
            return null;
        if (clone is ResourceEntryBase baseClone && baseClone.KeyId != entry.KeyId)
            baseClone.SetKeyId(entry.KeyId);
        return clone;
    }

    // The key id comes from the shared data, so it is right even when the file omitted ids and entries matched by key.
    static IResourceEntry BuildEntry(EntryData row, long keyId)
    {
        if (!string.IsNullOrEmpty(row.TypeName))
            return RebuildEntry(keyId, row.TypeName, row.EntryJson);

        if (row.Variants == null || row.Variants.Count == 0)
            return new StringEntry(keyId, row.Value);

        var entry = new VariantStringEntry(keyId, row.Value);
        foreach (var variant in row.Variants)
            entry.Variants.Add(new Variant<string>(variant.Key, variant.Value));
        return entry;
    }

    // The per-key selector and authored variant keys live in a single VariantSelectorMetadata on the shared entry.
    static void ApplyVariantMetadata(SharedTableData.SharedTableEntry entry, IVariantSelector selector, List<string> variantKeys)
    {
        var hasVariantKeys = variantKeys != null && variantKeys.Count > 0;
        if (selector == null && !hasVariantKeys)
            return;
        var metadata = new VariantSelectorMetadata(selector);
        if (hasVariantKeys)
        {
            foreach (var key in variantKeys)
                metadata.AddKey(key);
        }
        entry.Metadata.AddMetadata(metadata);
    }

    static void EnsureKey(SharedTableData target, SharedTableData donor, long keyId)
    {
        if (target == null || donor == null || target.GetEntry(keyId) != null)
            return;
        var source = donor.GetEntry(keyId);
        if (source == null)
            return;
        var added = target.AddKey(source.Key, source.Id);
        if (added == null)
            return;
        added.Flags = source.Flags;
        // Copied, not shared: mutating it through the built table must not write into the shipped donor's shared data.
        var sourceVariant = source.Metadata.GetMetadata<VariantSelectorMetadata>();
        if (sourceVariant != null)
            added.Metadata.AddMetadata(CloneVariantMetadata(sourceVariant));
    }

    static VariantSelectorMetadata CloneVariantMetadata(VariantSelectorMetadata source)
    {
        var clone = new VariantSelectorMetadata(source.Selector);
        var keys = source.Keys;
        for (var i = 0; i < keys.Count; i++)
            clone.AddKey(keys[i]);
        return clone;
    }

    [AutoStaticsCleanup] // caches Type handles a reload invalidates; mirrors ResetStatics()
    static readonly Dictionary<string, Type> s_ResolvedTypes = new();
    [AutoStaticsCleanup] // mirrors ResetStatics()
    static HashSet<string> s_WarnedTypes;

    [VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
    internal static void ResetStatics()
    {
        s_ResolvedTypes.Clear();
        s_WarnedTypes = null;
    }

    static Type ResolveType(string typeName)
    {
        if (s_ResolvedTypes.TryGetValue(typeName, out var cached))
            return cached;
        var type = Type.GetType(typeName);
        s_ResolvedTypes[typeName] = type;
        return type;
    }

    // One warning per type name, not per row: a table full of rows naming a missing type would flood the console.
    static void WarnUnresolvedType(string typeName, string consequence)
    {
        if ((s_WarnedTypes ??= new HashSet<string>()).Add(typeName))
            Debug.LogWarning($"Could not resolve type '{typeName}'; {consequence}");
    }

    static IResourceEntry RebuildEntry(long keyId, string typeName, string json)
    {
        var type = ResolveType(typeName);
        if (type == null || !typeof(IFileDataEntry).IsAssignableFrom(type))
        {
            WarnUnresolvedType(typeName, "its keys will have no value.");
            return null;
        }

        try
        {
            var entry = (IResourceEntry)Activator.CreateInstance(type, keyId);
            if (!string.IsNullOrEmpty(json))
                JsonUtility.FromJsonOverwrite(json, entry);
            if (entry is ResourceEntryBase baseEntry && baseEntry.KeyId != keyId)
                baseEntry.SetKeyId(keyId);
            return entry;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not rebuild file data entry of type '{typeName}': {e.Message}");
            return null;
        }
    }

    static IVariantSelector RebuildSelector(SelectorData data)
    {
        if (string.IsNullOrEmpty(data.TypeName))
            return null;

        var type = ResolveType(data.TypeName);
        if (type == null || !typeof(IVariantSelector).IsAssignableFrom(type))
        {
            WarnUnresolvedType(data.TypeName, "its keys will resolve to their default value.");
            return null;
        }

        // An escaping exception would leak the HideAndDontSave table and shared data created above.
        try
        {
            var selector = (IVariantSelector)Activator.CreateInstance(type);
            if (!string.IsNullOrEmpty(data.Json))
                JsonUtility.FromJsonOverwrite(data.Json, selector);
            return selector;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Could not rebuild variant selector '{data.TypeName}'; the key will resolve to its default value. {e.Message}");
            return null;
        }
    }
}
