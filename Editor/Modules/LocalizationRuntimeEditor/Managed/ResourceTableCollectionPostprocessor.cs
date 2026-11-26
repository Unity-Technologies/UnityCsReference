// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using UnityEditor;
using UnityEngine;

namespace Unity.Localization.Editor;

class ResourceTableCollectionPostprocessor : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        var deletedLocalizationAsset = AssetProviderEditors.MayHaveDeletedCollectionAssets(deletedAssets);
        var registered = new HashSet<ResourceTableCollection>();
        var visited = new HashSet<SharedTableData>();
        RegisterOwningCollections(importedAssets, registered, visited);
        RegisterOwningCollections(movedAssets, registered, visited);

        if (registered.Count > 0 || deletedLocalizationAsset)
            LocalizationEditorSettings.RaiseCollectionsChanged();

        if (deletedLocalizationAsset)
            AssetProviderEditors.RebuildRegistrations();
    }

    static void RegisterOwningCollections(string[] paths, HashSet<ResourceTableCollection> registered, HashSet<SharedTableData> visited)
    {
        foreach (var path in paths)
        {
            if (!LocalizationAssetPaths.IsLocalizationAsset(path))
                continue;

            switch (AssetDatabase.LoadAssetAtPath<Object>(path))
            {
                case ResourceTableCollection collection:
                    Register(collection, registered, visited);
                    break;
                case SharedTableData shared:
                    EnsureUniqueEntries(shared, visited);
                    foreach (var owner in AssetProviderEditors.CollectionsUsing(shared))
                        Register(owner, registered, visited);
                    break;
                case ResourceTable table:
                    foreach (var owner in AssetProviderEditors.CollectionsWith(table))
                        Register(owner, registered, visited);
                    break;
            }
        }
    }

    static void Register(ResourceTableCollection collection, HashSet<ResourceTableCollection> registered, HashSet<SharedTableData> visited)
    {
        if (collection == null || !registered.Add(collection))
            return;
        if (collection.SharedData == null)
            Debug.LogWarning($"'{AssetDatabase.GetAssetPath(collection)}' has no shared key table, so it holds no keys and importing into it does nothing.", collection);
        EnsureCollectionGuid(collection);
        EnsureUniqueEntries(collection.SharedData, visited);
        AssetProviderEditors.RegisterCollection(collection);
    }

    // Renaming rather than dropping keeps each id, so the values stored against it in every locale table survive.
    internal static void EnsureUniqueEntries(SharedTableData shared, HashSet<SharedTableData> visited)
    {
        // One import reaches a shared table through its own path and through every collection using it.
        if (shared == null || !visited.Add(shared))
            return;

        var renamed = false;
        var seen = new HashSet<string>();
        var byId = new Dictionary<long, SharedTableData.SharedTableEntry>();
        foreach (var entry in shared.Entries)
        {
            if (entry == null)
                continue;

            // Every locale table stores its values against the id, so two entries holding one id share one value.
            if (entry.Id != 0 && !byId.TryAdd(entry.Id, entry))
                Debug.LogWarning($"'{AssetDatabase.GetAssetPath(shared)}' has two keys holding id {entry.Id}, '{byId[entry.Id].Key}' and '{entry.Key}', so they share one value in every locale. Remove one of them and add it again to give it a fresh id.", shared);

            if (string.IsNullOrEmpty(entry.Key) || seen.Add(entry.Key))
                continue;

            var key = entry.Key;
            var free = key;
            for (var n = 1; shared.GetEntry(free) != null; n++)
                free = $"{key} ({n})";

            // Renaming reaches the entry through its id, so an id that resolves elsewhere would rename the wrong key.
            if (shared.GetEntry(entry.Id) == entry && shared.RenameKey(entry.Id, free))
            {
                renamed = true;
                Debug.LogWarning($"'{AssetDatabase.GetAssetPath(shared)}' had more than one key named '{key}'; renamed one of them to '{free}'.", shared);
            }
            else
            {
                Debug.LogWarning($"'{AssetDatabase.GetAssetPath(shared)}' has more than one key named '{key}' and the duplicate cannot be renamed, so only one of them answers to that key.", shared);
            }
        }
        if (renamed)
            EditorUtility.SetDirty(shared);
    }

    static void EnsureCollectionGuid(ResourceTableCollection collection)
    {
        var shared = collection.SharedData;
        if (shared == null)
            return;
        var assetGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(collection));
        if (!GUID.TryParse(assetGuid, out var guid))
            return;
        if (shared.TableCollectionNameGuid == guid)
            return;
        if (!shared.TableCollectionNameGuid.Empty() && OtherCollectionOwnsGuid(shared, collection))
        {
            Debug.LogWarning($"'{AssetDatabase.GetAssetPath(collection)}' shares its SharedTableData with '{AssetDatabase.GUIDToAssetPath(shared.TableCollectionNameGuid.ToString())}'; guid references resolve to the latter. Give the duplicate its own shared data.", collection);
            return;
        }
        shared.TableCollectionNameGuid = guid;
        EditorUtility.SetDirty(shared);
    }

    static bool OtherCollectionOwnsGuid(SharedTableData shared, ResourceTableCollection except)
    {
        var path = AssetDatabase.GUIDToAssetPath(shared.TableCollectionNameGuid.ToString());
        if (string.IsNullOrEmpty(path))
            return false;
        var other = AssetDatabase.LoadAssetAtPath<ResourceTableCollection>(path);
        return other != null && other != except && other.SharedData == shared;
    }
}

static class LocalizationAssetPaths
{
    internal static bool IsLocalizationAsset(string path)
    {
        if (!path.EndsWith(".asset", StringComparison.Ordinal))
            return false;
        var type = AssetDatabase.GetMainAssetTypeAtPath(path);
        return type != null && (typeof(ResourceTableCollection).IsAssignableFrom(type)
            || typeof(ResourceTable).IsAssignableFrom(type)
            || typeof(SharedTableData).IsAssignableFrom(type));
    }
}
