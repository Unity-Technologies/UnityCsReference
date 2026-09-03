// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using System.IO;
using Object = UnityEngine.Object;
using Unity.Localization.Providers;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

partial class ResourceTablesWindow
{
    IResourceEntry FirstEntry(long keyId)
    {
        if (m_Collection == null)
            return null;
        var tables = m_Collection.Tables;
        for (var i = 0; i < tables.Count; i++)
        {
            var entry = tables[i]?.GetEntry(keyId);
            if (entry != null)
                return entry;
        }
        return null;
    }

    bool IsSmart(long keyId) => HasCollection && m_Collection.SharedData.IsSmart(keyId);

    void SetSmart(long keyId, bool smart)
    {
        var shared = m_Collection != null ? m_Collection.SharedData : null;
        var entry = shared != null ? shared.GetEntry(keyId) : null;
        if (entry == null)
            return;
        RecordUndo("Toggle Smart String");
        entry.IsSmart = smart;
        EditorUtility.SetDirty(shared);
        RebuildTree();
    }

    SharedTableData.SharedTableEntry SharedEntry(long keyId)
        => HasCollection ? m_Collection.SharedData.GetEntry(keyId) : null;

    bool IsVariant(long keyId) => KeyVariantMeta(keyId) != null || CollectionVariantMeta() != null;

    IVariantSelector SelectorOf(long keyId) => KeyVariantMeta(keyId)?.Selector ?? CollectionVariantMeta()?.Selector;

    VariantSelectorMetadata CollectionVariantMeta()
        => HasCollection ? m_Collection.SharedData.Metadata.GetMetadata<VariantSelectorMetadata>() : null;

    // The per-key variant selector and authored keys live in a single VariantSelectorMetadata on the shared entry.
    VariantSelectorMetadata KeyVariantMeta(long keyId, bool create = false)
    {
        var entry = SharedEntry(keyId);
        if (entry == null)
            return null;
        var meta = entry.Metadata.GetMetadata<VariantSelectorMetadata>();
        if (meta == null && create)
        {
            meta = new VariantSelectorMetadata();
            entry.Metadata.AddMetadata(meta);
        }
        return meta;
    }

    // Mirrors the exporter, so what the window shows is what a build materializes.
    IReadOnlyList<string> AuthoredVariantKeys(long keyId)
    {
        var keyMeta = KeyVariantMeta(keyId);
        return keyMeta != null ? keyMeta.Keys : CollectionVariantMeta()?.Keys;
    }

    List<string> VariantKeys(long keyId)
        => AuthoredVariantKeys(keyId) is { } keys ? new List<string>(keys) : new List<string>();

    void AddVariant(long keyId, string variantKey)
    {
        if (!HasCollection)
            return;
        RecordUndo("Add Variant");
        var meta = KeyVariantMeta(keyId, create: true);
        if (meta != null)
        {
            if (meta.Keys.Count == 0 && CollectionVariantMeta() is { } inherited)
            {
                for (var i = 0; i < inherited.Keys.Count; i++)
                    meta.AddKey(inherited.Keys[i]);
            }
            meta.AddKey(variantKey);
        }
        EditorUtility.SetDirty(m_Collection.SharedData);
        // Promote each locale entry to its variant subtype and seed the value from the default so the cell binds.
        foreach (var table in Tables())
        {
            var entry = table.GetEntry(keyId);
            if (entry == null)
                continue;
            var promoted = PromoteToVariant(table, entry);
            if (promoted is VariantStringEntry vs && vs.Variants.FindIndex(v => v.Key == variantKey) < 0)
                vs.Variants.Add(new Variant<string>(variantKey, vs.Value));
            EditorUtility.SetDirty(table);
        }
        m_ExpandedKeys[keyId] = true;
        RebuildTree();
    }

    static IResourceEntry PromoteToVariant(ResourceTable table, IResourceEntry entry)
    {
        // Copied field by field: a JsonUtility round-trip would drop the entry's [SerializeReference] metadata.
        IResourceEntry promoted = entry switch
        {
            VariantStringEntry or VariantAssetEntry or VariantResourceAssetEntry => null,
            StringEntry s => new VariantStringEntry(s.KeyId, s.Value),
            ResourceAssetEntry r => new VariantResourceAssetEntry(r.KeyId) { Default = r.Default },
            AssetEntry a => new VariantAssetEntry(a.KeyId) { Default = a.Default },
            _ => null
        };
        if (promoted == null)
            return entry;
        foreach (var metadata in entry.Metadata.GetMetadatas<IMetadata>())
            promoted.Metadata.AddMetadata(metadata);
        table.AddEntry(promoted);
        return promoted;
    }

    void RemoveVariant(VariantRow variantRow)
    {
        if (!HasCollection)
            return;
        RecordUndo("Remove Variant");
        var meta = KeyVariantMeta(variantRow.KeyId);
        if (meta == null && CollectionVariantMeta() is { } inherited)
        {
            // Removing an inherited row for one key takes a per-key override seeded with the rest.
            meta = KeyVariantMeta(variantRow.KeyId, create: true);
            if (meta != null)
            {
                for (var i = 0; i < inherited.Keys.Count; i++)
                    meta.AddKey(inherited.Keys[i]);
            }
        }
        meta?.RemoveKey(variantRow.VariantKey);
        EditorUtility.SetDirty(m_Collection.SharedData);
        foreach (var table in Tables())
        {
            if (table.GetEntry(variantRow.KeyId) is IVariantEntry entry)
            {
                entry.RemoveVariant(variantRow.VariantKey);
                EditorUtility.SetDirty(table);
            }
        }
        RebuildTree();
    }

    void DeleteKey(long keyId)
    {
        RecordUndo("Delete Key");
        foreach (var table in Tables())
        {
            table.RemoveEntry(keyId);
            EditorUtility.SetDirty(table);
        }
        var shared = m_Collection != null ? m_Collection.SharedData : null;
        if (shared != null)
        {
            var key = shared.GetKey(keyId);
            if (key != null)
                shared.RemoveKey(key);
            EditorUtility.SetDirty(shared);
        }
        m_ExpandedKeys.Remove(keyId);
        RebuildTree();
    }

    string KeyName(long keyId)
    {
        var key = HasCollection ? m_Collection.SharedData.GetKey(keyId) : null;
        return key ?? keyId.ToString();
    }

    void ShowAddEntryMenu(VisualElement anchor)
    {
        var key = m_NewKeyField.value?.Trim();
        if (!HasCollection || string.IsNullOrEmpty(key))
            return;

        // IMGUI GenericMenu nests the "/"-separated paths into real drill-down submenus (String, Asset ▸, Variant ▸).
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent(L10n.Tr("String", null)), false, () => AddEntry(key, typeof(StringEntry), null));
        foreach (var (entryType, label) in AssetEntryEditors.Kinds())
        {
            var captured = entryType;
            menu.AddItem(new GUIContent($"{L10n.Tr("Asset", null)}/{label}"), false, () => AddEntry(key, captured, null));
        }
        foreach (var selectorType in SelectorTypes())
        {
            var captured = selectorType;
            menu.AddItem(new GUIContent($"{L10n.Tr("Variant", null)}/{L10n.Tr("String", null)}/{SelectorName(captured)}"), false, () => AddEntry(key, typeof(StringEntry), captured));
        }
        foreach (var (entryType, label) in AssetEntryEditors.Kinds())
        {
            foreach (var selectorType in SelectorTypes())
            {
                var capturedType = entryType;
                var capturedSelector = selectorType;
                menu.AddItem(new GUIContent($"{L10n.Tr("Variant", null)}/{L10n.Tr("Asset", null)}/{label}/{SelectorName(capturedSelector)}"), false, () => AddEntry(key, capturedType, capturedSelector));
            }
        }
        menu.DropDown(anchor.worldBound);
    }

    [AutoStaticsCleanup] // caches Type handles a reload invalidates
    static List<Type> s_SelectorTypes;

    // Cached once per domain (TypeCache is stable within a domain); the Add Entry menu queries this repeatedly.
    static IEnumerable<Type> SelectorTypes()
    {
        if (s_SelectorTypes != null)
            return s_SelectorTypes;
        s_SelectorTypes = new List<Type>();
        foreach (var type in TypeCache.GetTypesDerivedFrom<IVariantSelector>())
        {
            if (!type.IsAbstract && !type.IsGenericType && type.GetConstructor(Type.EmptyTypes) != null)
                s_SelectorTypes.Add(type);
        }
        return s_SelectorTypes;
    }

    static string SelectorName(Type selectorType)
    {
        var name = selectorType.Name;
        if (name.EndsWith("VariantSelector", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "VariantSelector".Length);
        else if (name.EndsWith("Selector", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "Selector".Length);
        return ObjectNames.NicifyVariableName(name);
    }

    void AddEntry(string key, Type entryType, Type selectorType)
    {
        // AddKey returns an existing key, so guard here or adding would overwrite that key's entries in every locale.
        if (m_Collection.SharedData.GetEntry(key) != null)
        {
            Debug.LogWarning($"A key named '{key}' already exists in '{m_Collection.TableCollectionName}'.");
            return;
        }
        RecordUndo("Add Entry");
        var id = m_Collection.SharedData.AddKey(key)?.Id ?? 0;
        if (id == 0)
            return;
        foreach (var table in Tables())
        {
            if (entryType == typeof(StringEntry))
                table.AddStringEntry(key, string.Empty);
            else
                table.AddEntry((IResourceEntry)Activator.CreateInstance(entryType, id));
            EditorUtility.SetDirty(table);
        }
        if (selectorType != null && id != 0 && m_Collection.SharedData.GetEntry(id) is { } shared)
        {
            var meta = shared.Metadata.GetMetadata<VariantSelectorMetadata>();
            if (meta == null)
            {
                meta = new VariantSelectorMetadata();
                shared.Metadata.AddMetadata(meta);
            }
            meta.Selector = (IVariantSelector)Activator.CreateInstance(selectorType);
        }
        EditorUtility.SetDirty(m_Collection.SharedData);
        m_NewKeyField.value = string.Empty;
        RebuildTree();
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
