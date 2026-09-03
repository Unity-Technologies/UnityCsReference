// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.Localization.Editor.Search;

static partial class AssetEntryTypeResolver
{
    [AutoStaticsCleanup] // caches Type handles a reload invalidates
    static Dictionary<string, Type> s_ByResourcesPath;

    // Dropped when the picker opens: an asset added, moved or reimported since the last open would be missed.
    internal static void InvalidateResourcesIndex() => s_ByResourcesPath = null;

    // Null when the entry holds no value; a key with nothing assigned cannot be judged incompatible, so callers keep it.
    internal static Type Resolve(IAssetEntry entry)
    {
        switch (entry)
        {
            case null:
                return null;
            case ResourceAssetEntry resource:
                return FromResourcesPath(resource.Default);
            case AssetEntry direct:
                return direct.Default != null ? direct.Default.GetType() : null;
            case ISynchronousAssetEntry sync:
                // A custom kind: take whatever it resolves immediately, without forcing an asynchronous load.
                return sync.TryLoadAsset(out var asset) && asset != null ? asset.GetType() : null;
            default:
                return null;
        }
    }

    static Type FromResourcesPath(string resourcesPath)
    {
        if (string.IsNullOrEmpty(resourcesPath))
            return null;
        s_ByResourcesPath ??= BuildResourcesIndex();
        return s_ByResourcesPath.TryGetValue(resourcesPath, out var type) ? type : null;
    }

    static Dictionary<string, Type> BuildResourcesIndex()
    {
        var index = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var guid in AssetDatabase.FindAssets(string.Empty))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!AssetProviderEditors.IsUnderResources(path))
                continue;
            var relative = AssetProviderEditors.ResourcesRelativePath(path);
            if (string.IsNullOrEmpty(relative) || index.ContainsKey(relative))
                continue;
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (type != null)
                index[relative] = type;
        }
        return index;
    }
}
