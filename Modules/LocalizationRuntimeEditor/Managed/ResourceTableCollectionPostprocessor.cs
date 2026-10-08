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
        // Checked before registering, which re-scans and refills the path cache with the post-delete state.
        var deletedLocalizationAsset = AssetProviderEditors.MayHaveDeletedCollectionAssets(deletedAssets);

        var changed = false;
        changed |= RegisterOwningCollections(importedAssets);
        changed |= RegisterOwningCollections(movedAssets);

        if (changed || deletedLocalizationAsset)
            LocalizationEditorSettings.RaiseCollectionsChanged();

        if (deletedLocalizationAsset)
            AssetProviderEditors.RebuildRegistrations();
    }

    static bool RegisterOwningCollections(string[] paths)
    {
        var registered = new HashSet<ResourceTableCollection>();
        foreach (var path in paths)
        {
            if (!LocalizationAssetPaths.IsLocalizationAsset(path))
                continue;

            switch (AssetDatabase.LoadAssetAtPath<Object>(path))
            {
                case ResourceTableCollection collection:
                    Register(collection, registered);
                    break;
                case SharedTableData shared:
                    foreach (var owner in AssetProviderEditors.CollectionsUsing(shared))
                        Register(owner, registered);
                    break;
                case ResourceTable table:
                    foreach (var owner in AssetProviderEditors.CollectionsWith(table))
                        Register(owner, registered);
                    break;
            }
        }
        return registered.Count > 0;
    }

    static void Register(ResourceTableCollection collection, HashSet<ResourceTableCollection> registered)
    {
        if (collection == null || !registered.Add(collection))
            return;
        EnsureCollectionGuid(collection);
        AssetProviderEditors.RegisterCollection(collection);
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
