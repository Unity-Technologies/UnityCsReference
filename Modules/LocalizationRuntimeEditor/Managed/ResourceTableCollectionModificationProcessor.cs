// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.Localization.Editor;

partial class ResourceTableCollectionModificationProcessor : AssetModificationProcessor
{
    [AutoStaticsCleanup] // pending work for the current import; a reload abandons it
    static readonly List<string> s_PendingSiblings = new();
    [AutoStaticsCleanup] // pending work for the current import; a reload abandons it
    static readonly List<string> s_PendingFolders = new();
    [AutoStaticsCleanup] // the scheduled callback does not survive the reload
    static bool s_CleanupScheduled;

    static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
    {
        var collection = AssetDatabase.LoadAssetAtPath<ResourceTableCollection>(path);
        if (collection == null)
            return AssetDeleteResult.DidNotDelete;

        foreach (var sibling in CollectionSiblingAssetPaths(collection))
        {
            if (!s_PendingSiblings.Contains(sibling))
                s_PendingSiblings.Add(sibling);
        }
        var folder = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(folder) && !s_PendingFolders.Contains(folder))
            s_PendingFolders.Add(folder);

        // One deferred pass covers a whole multi-select delete, so the project is scanned once, not per collection.
        if (!s_CleanupScheduled)
        {
            s_CleanupScheduled = true;
            EditorApplication.delayCall += DeletePendingSiblings;
        }

        return AssetDeleteResult.DidNotDelete;
    }

    static void DeletePendingSiblings()
    {
        s_CleanupScheduled = false;

        var inUse = new HashSet<string>();
        foreach (var collection in AssetProviderEditors.FindAllCollections())
            inUse.UnionWith(CollectionSiblingAssetPaths(collection));
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var sibling in s_PendingSiblings)
            {
                if (!inUse.Contains(sibling))
                    AssetDatabase.DeleteAsset(sibling);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        s_PendingSiblings.Clear();

        foreach (var folder in s_PendingFolders)
        {
            if (folder != "Assets" && AssetDatabase.IsValidFolder(folder)
                && AssetDatabase.FindAssets(string.Empty, new[] { folder }).Length == 0)
                AssetDatabase.DeleteAsset(folder);
        }
        s_PendingFolders.Clear();
    }

    /// <summary>The separate shared-data and per-locale table asset paths owned by <paramref name="collection"/>, excluding the collection asset itself.</summary>
    /// <param name="collection">The collection being deleted.</param>
    internal static List<string> CollectionSiblingAssetPaths(ResourceTableCollection collection)
    {
        var result = new List<string>();
        if (collection == null)
            return result;
        var collectionPath = AssetDatabase.GetAssetPath(collection);
        Add(result, collection.SharedData != null ? AssetDatabase.GetAssetPath(collection.SharedData) : null, collectionPath);
        foreach (var table in collection.Tables)
        {
            if (table != null)
                Add(result, AssetDatabase.GetAssetPath(table), collectionPath);
        }
        return result;
    }

    static void Add(List<string> list, string assetPath, string collectionPath)
    {
        if (!string.IsNullOrEmpty(assetPath) && assetPath != collectionPath && !list.Contains(assetPath))
            list.Add(assetPath);
    }
}
