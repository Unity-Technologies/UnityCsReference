// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace Unity.Localization;

// Parses the "path[SubAssetName]" store form used by asset entries that load by path or address.
[VisibleToOtherModules("UnityEditor.LocalizationRuntimeModule")]
internal static class SubAssetAddress
{
    // The split uses the last '[' so a path that itself contains brackets still parses.
    public static bool IsSubAsset(string address)
        => !string.IsNullOrEmpty(address) && address[^1] == ']' && address.LastIndexOf('[') > 0;

    public static string GetPath(string address)
        => IsSubAsset(address) ? address[..address.LastIndexOf('[')] : address;

    public static string GetSubAssetName(string address)
    {
        if (!IsSubAsset(address))
            return null;
        var start = address.LastIndexOf('[') + 1;
        return address[start..^1];
    }

    public static string Format(string path, string subAssetName) => $"{path}[{subAssetName}]";

    // A store that only looks like a sub-asset (a literal path with brackets) falls back to a direct load.
    public static Object LoadFromResources(string store)
    {
        if (IsSubAsset(store))
        {
            var asset = Select(Resources.LoadAll(GetPath(store)), GetSubAssetName(store));
            if (asset != null)
                return asset;
        }
        return Resources.Load<Object>(store);
    }

    public static Object Select(Object[] assets, string name)
    {
        if (assets == null)
            return null;
        foreach (var asset in assets)
        {
            if (asset != null && asset.name == name)
                return asset;
        }
        return null;
    }
}
