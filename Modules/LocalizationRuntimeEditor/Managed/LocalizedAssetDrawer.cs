// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

[CustomPropertyDrawer(typeof(LocalizedAsset<>), true)]
class LocalizedAssetDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var label = string.IsNullOrEmpty(preferredLabel) ? property.displayName : preferredLabel;
        var assetType = ResolveAssetType(fieldInfo?.FieldType) ?? typeof(Object);
        return new LocalizedAssetElement(property, label, assetType);
    }

    internal static Type ResolveAssetType(Type fieldType)
    {
        if (fieldType == null)
            return null;
        if (fieldType.IsArray)
            fieldType = fieldType.GetElementType();
        else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            fieldType = fieldType.GetGenericArguments()[0];

        for (var type = fieldType; type != null && type != typeof(object); type = type.BaseType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(LocalizedAsset<>))
                return type.GetGenericArguments()[0];
        }
        return null;
    }
}

class LocalizedAssetElement : LocalizedReferenceElement
{
    const string k_StorageRowUxml = "LocalizationRuntime/UXML/LocalizedAssetStorageRow.uxml";

    [NoAutoStaticsCleanup] // loaded asset; outlives a code reload
    static VisualTreeAsset s_StorageRow;

    readonly Type m_AssetType;

    public LocalizedAssetElement(SerializedProperty property, string label, Type assetType)
        : base(property, label, 0)
    {
        m_AssetType = assetType;
        RebuildBody();
    }

    private protected LocalizedAssetElement(SerializedProperty property, string label, Type assetType, Layout paths)
        : base(property, label, 0, paths)
    {
        m_AssetType = assetType;
        RebuildBody();
    }

    protected override string NoneLabel => L10n.Tr("None", null);

    protected override Type EntryType => typeof(AssetEntry);

    protected override Type FilterAssetType => m_AssetType;

    protected override string LocalePreview(Locale locale, ResourceTable table)
        => AssetPreview(table.GetEntry(SharedEntry.Id) as IAssetEntry);

    protected override void BuildLocaleDetail(VisualElement detail, Locale locale, ResourceTable table)
    {
        var existing = table.GetEntry(SharedEntry.Id);
        if (existing != null && existing is not IAssetEntry)
        {
            detail.Add(new HelpBox(L10n.Tr("This key is a string entry in this locale, not an asset.", null), HelpBoxMessageType.Warning));
            return;
        }

        var entry = existing as IAssetEntry;
        if (entry == null)
        {
            detail.Add(BuildMissingAssetField(table));
            return;
        }

        var field = AssetEntryEditors.Get(entry.GetType()).CreateDefaultField(entry, table);
        field.AddToClassList(LocClasses.LaValueField);
        if (field is ObjectField objectField)
        {
            objectField.objectType = m_AssetType;
            objectField.RegisterValueChangedCallback(_ => UpdateRailPreview(table.LocaleIdentifier.Code));
        }
        detail.Add(field);

        detail.Add(BuildStorageRow(table, entry));
    }

    VisualElement BuildMissingAssetField(ResourceTable table)
    {
        var field = new ObjectField { objectType = m_AssetType, allowSceneObjects = false };
        field.AddToClassList(LocClasses.LaValueField);
        field.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == null)
                return;
            Undo.RegisterCompleteObjectUndo(table, "Assign Asset");
            table.AddEntry(new AssetEntry(SharedEntry.Id) { Default = evt.newValue });
            EditorUtility.SetDirty(table);
            UpdateRailPreview(table.LocaleIdentifier.Code);
            RefreshDetail();
        });
        return field;
    }

    VisualElement BuildStorageRow(ResourceTable table, IAssetEntry entry)
    {
        s_StorageRow ??= EditorGUIUtility.LoadRequired(k_StorageRowUxml) as VisualTreeAsset;
        if (s_StorageRow == null)
            return new VisualElement();

        var row = s_StorageRow.Instantiate();
        row.Q<Label>("storage-label").text = L10n.Tr("Storage", null);

        var current = AssetEntryEditors.Get(entry.GetType());
        var button = row.Q<Button>("storage-menu");
        button.text = L10n.Tr(current.KindLabel, null);
        button.tooltip = L10n.Tr(current.KindTooltip, null);
        button.clicked += () => ShowStorageMenu(button, table, entry.GetType());
        return row;
    }

    void ShowStorageMenu(Button anchor, ResourceTable table, Type currentType)
        => ShowMenu(anchor, AssetEntryEditors.Kinds(), currentType, type => SwitchStorage(table, type));

    void SwitchStorage(ResourceTable table, Type entryType)
    {
        var current = table.GetEntry(SharedEntry.Id) as IAssetEntry;
        if (current == null || current.GetType() == entryType)
            return;
        var carried = CurrentObject(current);
        Undo.RegisterCompleteObjectUndo(table, "Change Asset Storage");
        var created = (IAssetEntry)Activator.CreateInstance(entryType, SharedEntry.Id);
        AssignObject(created, carried);
        table.AddEntry(created);
        EditorUtility.SetDirty(table);
        UpdateRailPreview(table.LocaleIdentifier.Code);
        RefreshDetail();
    }

    // Reads the object a kind currently holds, so a storage switch can carry it over instead of clearing it.
    static Object CurrentObject(IAssetEntry entry) => entry switch
    {
        AssetEntry direct => direct.Default,
        ResourceAssetEntry resource => string.IsNullOrEmpty(resource.Default) ? null : Resources.Load(resource.Default),
        _ => null,
    };

    static void AssignObject(IAssetEntry entry, Object value)
    {
        switch (entry)
        {
            case AssetEntry direct:
                direct.Default = value;
                break;
            case ResourceAssetEntry resource:
                resource.Default = ResourcesPath(value);
                break;
        }
    }

    static string ResourcesPath(Object value)
    {
        if (value == null)
            return null;
        var relative = AssetProviderEditors.ResourcesRelativePath(AssetDatabase.GetAssetPath(value));
        if (relative == null)
            Debug.LogWarning($"'{value.name}' is not under a Resources folder; it was not carried over when switching storage.");
        return relative;
    }

    static string AssetPreview(IAssetEntry entry)
    {
        return entry switch
        {
            AssetEntry direct => direct.Default != null ? direct.Default.name : null,
            ResourceAssetEntry resource => string.IsNullOrEmpty(resource.Default) ? null : resource.Default,
            _ => null,
        };
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
