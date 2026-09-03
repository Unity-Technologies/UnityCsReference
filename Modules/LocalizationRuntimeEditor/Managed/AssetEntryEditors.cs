// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace Unity.Localization.Editor;

static partial class AssetEntryEditors
{
    [AutoStaticsCleanup] // keyed by Type and holds editor instances; both go stale on reload
    static Dictionary<Type, IAssetEntryEditor> s_Editors;
    [NoAutoStaticsCleanup] // stateless fallback; a readonly field cannot be reassigned anyway
    static readonly IAssetEntryEditor s_Fallback = new DirectAssetEntryEditor();
    // Resolved base-type walks, cached because Get/RegisteredKind run per cell while the table scrolls.
    [AutoStaticsCleanup] // caches Type handles a reload invalidates
    static readonly Dictionary<Type, Type> s_ResolvedKind = new();

    public static IAssetEntryEditor Get(Type entryType)
    {
        var kind = RegisteredKind(entryType);
        return s_Editors.TryGetValue(kind, out var editor) ? editor : s_Fallback;
    }

    // The base kind, walking past variant subtypes, or the type itself when none is registered.
    public static Type RegisteredKind(Type entryType)
    {
        if (s_ResolvedKind.TryGetValue(entryType, out var cached))
            return cached;
        s_Editors ??= Build();
        var type = entryType;
        while (type != null && type != typeof(object))
        {
            if (s_Editors.ContainsKey(type))
                return s_ResolvedKind[entryType] = type;
            type = type.BaseType;
        }
        return s_ResolvedKind[entryType] = entryType;
    }

    public static IEnumerable<(Type entryType, string label)> Kinds()
    {
        s_Editors ??= Build();
        foreach (var pair in s_Editors)
            yield return (pair.Key, pair.Value.KindLabel);
    }

    static Dictionary<Type, IAssetEntryEditor> Build()
    {
        var map = new Dictionary<Type, IAssetEntryEditor>();
        foreach (var type in TypeCache.GetTypesWithAttribute<AssetEntryEditorAttribute>())
        {
            // Skip a custom editor with no parameterless constructor rather than throw during discovery.
            if (type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
                continue;
            var attribute = (AssetEntryEditorAttribute)Attribute.GetCustomAttribute(type, typeof(AssetEntryEditorAttribute));
            if (attribute?.EntryType != null)
                map[attribute.EntryType] = (IAssetEntryEditor)Activator.CreateInstance(type);
        }
        return map;
    }
}
