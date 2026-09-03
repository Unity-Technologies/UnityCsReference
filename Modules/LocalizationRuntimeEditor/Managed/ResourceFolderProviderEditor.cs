// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.Localization.Providers;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.Localization.Editor;

[AssetProviderEditor(typeof(ResourceFolderProvider))]
partial class ResourceFolderProviderEditor : AssetProviderEditor
{
    [AutoStaticsCleanup] // warn once bookkeeping; a reload should let the warning fire again
    static HashSet<EntityId> s_WarnedTables;

    protected override void RegisterTable(IAssetProvider provider, ResourceTableCollection collection, ResourceTable table)
    {
        if (provider is not ResourceFolderProvider resources)
            return;
        var resourcesPath = ResourcesPath(table);
        if (string.IsNullOrEmpty(resourcesPath))
        {
            if ((s_WarnedTables ??= new HashSet<EntityId>()).Add(table.GetEntityId()))
                Debug.LogWarning($"Localization: table '{table.name}' in collection '{collection.TableCollectionName}' is not under a Resources folder, so the Resources content source cannot serve it and it will not resolve at runtime. Move the table under a Resources folder.", table);
            return;
        }
        s_WarnedTables?.Remove(table.GetEntityId());
        resources.RemoveAliasesTo(resourcesPath);
        var guidAddress = GuidTableAddress(collection, table);
        if (!string.IsNullOrEmpty(guidAddress))
            resources.AddOwnedAlias(guidAddress, resourcesPath);
        resources.AddOwnedAlias(TableAddress(collection, table), resourcesPath);
    }

    protected override void UnregisterTable(IAssetProvider provider, ResourceTableCollection collection, ResourceTable table)
    {
        if (provider is not ResourceFolderProvider resources)
            return;
        var guidAddress = GuidTableAddress(collection, table);
        if (!string.IsNullOrEmpty(guidAddress))
            resources.Remove(guidAddress);
        resources.Remove(TableAddress(collection, table));
    }

    public override void ClearRegistrations(IAssetProvider provider)
    {
        if (provider is ResourceFolderProvider resources)
            resources.ClearOwned();
    }

    // Collections saved under a Resources folder must load via Resources; the path decides, not configuration.
    public override bool ClaimsPath(string assetPath) => AssetProviderEditors.IsUnderResources(assetPath);

    static string ResourcesPath(ResourceTable table)
        => AssetProviderEditors.ResourcesRelativePath(AssetDatabase.GetAssetPath(table)) ?? string.Empty;
}
