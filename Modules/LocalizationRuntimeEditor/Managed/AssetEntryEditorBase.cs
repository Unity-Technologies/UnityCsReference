// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Localization.Editor;

abstract class AssetEntryEditorBase<TEntry, TStore> : IAssetEntryEditor where TEntry : AssetEntryBase<TStore>
{
    public virtual Type AssetType => typeof(Object);
    public abstract string KindLabel { get; }
    public abstract string KindTooltip { get; }

    protected abstract Object ToObject(TStore store);
    protected abstract TStore FromObject(Object asset);

    protected abstract List<Variant<TStore>> Variants(IAssetEntry entry);

    public VisualElement CreateDefaultField(IAssetEntry entry, ResourceTable table)
    {
        if (entry is not TEntry typed)
            return new Label(entry?.GetType().Name ?? "<null>");
        return Field(ToObject(typed.Default), asset => typed.Default = FromObject(asset), table);
    }

    public VisualElement CreateVariantField(IAssetEntry entry, string variantKey, ResourceTable table)
    {
        var variants = Variants(entry);
        if (variants == null)
            return Field(null, _ => { }, table);
        var index = variants.FindIndex(v => v.Key == variantKey);
        var current = index >= 0 ? ToObject(variants[index].Value) : null;
        return Field(current, asset =>
        {
            var store = FromObject(asset);
            var i = variants.FindIndex(v => v.Key == variantKey);
            if (i >= 0)
                variants[i] = new Variant<TStore>(variantKey, store);
            else
                variants.Add(new Variant<TStore>(variantKey, store));
        }, table);
    }

    VisualElement Field(Object current, Action<Object> assign, ResourceTable table)
    {
        var field = new ObjectField { objectType = AssetType, allowSceneObjects = false };
        field.AddToClassList(LocClasses.LocValueField);
        field.SetValueWithoutNotify(current);
        field.RegisterValueChangedCallback(evt =>
        {
            Undo.RegisterCompleteObjectUndo(table, "Assign Asset");
            assign(evt.newValue);
            EditorUtility.SetDirty(table);
        });
        return field;
    }
}

[AssetEntryEditor(typeof(AssetEntry))]
class DirectAssetEntryEditor : AssetEntryEditorBase<AssetEntry, Object>
{
    public override string KindLabel => L10n.Tr("Direct", null);
    public override string KindTooltip => L10n.Tr("Stores the asset as a direct reference that is loaded when the table is loaded.", null);
    protected override Object ToObject(Object store) => store;
    protected override Object FromObject(Object asset) => asset;
    protected override List<Variant<Object>> Variants(IAssetEntry entry) => (entry as VariantAssetEntry)?.Variants;
}

[AssetEntryEditor(typeof(ResourceAssetEntry))]
class ResourceAssetEntryEditor : AssetEntryEditorBase<ResourceAssetEntry, string>
{
    public override string KindLabel => L10n.Tr("Resources", null);
    public override string KindTooltip => L10n.Tr("Stores the asset in a Resources folder and loads it on demand.", null);

    protected override Object ToObject(string store) => string.IsNullOrEmpty(store) ? null : Resources.Load(store);

    protected override string FromObject(Object asset)
    {
        if (asset == null)
            return null;
        var relative = AssetProviderEditors.ResourcesRelativePath(AssetDatabase.GetAssetPath(asset));
        if (relative == null)
            Debug.LogWarning($"'{asset.name}' is not under a Resources folder; a Resource entry loads by Resources path.");
        return relative;
    }

    protected override List<Variant<string>> Variants(IAssetEntry entry) => (entry as VariantResourceAssetEntry)?.Variants;
}
