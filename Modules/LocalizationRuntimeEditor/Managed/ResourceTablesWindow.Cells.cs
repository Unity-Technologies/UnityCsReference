// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.Localization.Providers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Localization.Editor;

partial class ResourceTablesWindow
{
    sealed class KeyCellRefs { public Label Connector; public VisualElement Content; public VisualElement Tags; public Button MetadataButton; }
    sealed class ValueCellRefs { public VisualElement Field; public Button MetadataButton; }

    VisualElement MakeKeyCell()
    {
        var cell = m_KeyCellTemplate.Instantiate();
        var refs = new KeyCellRefs
        {
            Connector = cell.Q<Label>("connector"),
            Content = cell.Q("content"),
            Tags = cell.Q("tags"),
            MetadataButton = cell.Q<Button>("metadata-button")
        };
        LocIcons.Apply(refs.MetadataButton, LocIcons.Metadata);
        cell.userData = refs;
        return cell;
    }

    VisualElement MakeValueCell()
    {
        var cell = m_ValueCellTemplate.Instantiate();
        var refs = new ValueCellRefs { Field = cell.Q("field"), MetadataButton = cell.Q<Button>("metadata-button") };
        LocIcons.Apply(refs.MetadataButton, LocIcons.Metadata);
        cell.userData = refs;
        return cell;
    }

    VisualElement MakeActionsCell()
    {
        var cell = m_ActionsCellTemplate.Instantiate();
        LocIcons.Apply(cell.Q<Button>("actions-menu"), LocIcons.Menu);
        return cell;
    }

    void BindKeyCell(VisualElement element, int row)
    {
        var refs = (KeyCellRefs)element.userData;
        var connector = refs.Connector;
        var content = refs.Content;
        var tags = refs.Tags;
        content.Clear();
        tags.Clear();
        refs.MetadataButton.style.display = DisplayStyle.None;

        var data = m_Tree.GetItemDataForIndex<object>(row);
        if (data is long keyId)
        {
            connector.style.display = DisplayStyle.None;
            if (keyId == m_RenamingKey)
            {
                content.Add(BuildRenameField(keyId));
                return;
            }
            content.Add(BuildKeyLabel(keyId));
            var selector = SelectorOf(keyId);
            if (selector != null)
                tags.Add(Styled($"[{selector.SelectorId}]", "loc-type-tag"));
            var first = FirstEntry(keyId);
            if (first is IStringEntry && IsSmart(keyId))
            {
                var smartChip = Styled("{}", "loc-pill", "loc-key-cell__smart");
                smartChip.tooltip = L10n.Tr("Smart String, formatted at runtime", null);
                tags.Add(smartChip);
            }
            if (first is IAssetEntry asset)
            {
                var kindEditor = AssetEntryEditors.Get(asset.GetType());
                var kindTag = Styled($"[{kindEditor.KindLabel}]", "loc-type-tag");
                kindTag.tooltip = kindEditor.KindTooltip;
                tags.Add(kindTag);
            }
            if (SharedEntry(keyId) is { } sharedEntry)
            {
                ConfigureMetadataButton(refs.MetadataButton, sharedEntry.Metadata, m_Collection.SharedData,
                    () => SharedMetadataPath(m_Collection.SharedData, keyId), MetadataType.SharedTableEntry,
                    $"{L10n.Tr("Shared metadata", null)}: {KeyName(keyId)}");
            }
        }
        else if (data is VariantRow variantRow)
        {
            connector.style.display = DisplayStyle.Flex;
            connector.text = variantRow.Last ? "└" : "├";
            content.Add(Styled(variantRow.VariantKey, "loc-key-cell__label", "loc-key-cell__label--variant"));
        }
    }

    VisualElement BuildKeyLabel(long keyId)
    {
        var label = Styled(KeyName(keyId), "loc-key-cell__label");
        label.tooltip = L10n.Tr("Double-click to rename", null);
        label.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.clickCount != 2)
                return;
            evt.StopPropagation();
            m_RenamingKey = keyId;
            RebuildTree();
        });
        return label;
    }

    VisualElement BuildRenameField(long keyId)
    {
        var field = new TextField { value = KeyName(keyId) };
        field.AddToClassList(LocClasses.LocRenameField);
        field.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
                field.Blur();
            else if (evt.keyCode == KeyCode.Escape)
            {
                field.SetValueWithoutNotify(KeyName(keyId)); // commit unchanged -> no rename
                field.Blur();
            }
        });
        field.RegisterCallback<FocusOutEvent>(_ => RenameKey(keyId, field.value));
        field.schedule.Execute(() => { field.Focus(); field.SelectAll(); });
        return field;
    }

    void RenameKey(long keyId, string newKey)
    {
        if (m_RenamingKey != keyId)
            return;
        m_RenamingKey = 0;
        var shared = m_Collection != null ? m_Collection.SharedData : null;
        newKey = newKey?.Trim();
        if (shared != null && !string.IsNullOrEmpty(newKey) && newKey != KeyName(keyId))
        {
            RecordUndo("Rename Key");
            if (shared.RenameKey(keyId, newKey))
                EditorUtility.SetDirty(shared);
            else
                Debug.LogWarning($"A key named '{newKey}' already exists in this collection.");
        }
        RebuildTree();
    }

    void BindIdCell(VisualElement element, int row)
    {
        var label = (Label)element;
        var data = m_Tree.GetItemDataForIndex<object>(row);
        label.text = data is long keyId ? keyId.ToString() : string.Empty;
    }

    void BindLocaleCell(VisualElement element, ResourceTable table, int row)
    {
        var refs = (ValueCellRefs)element.userData;
        var field = refs.Field;
        var metadataButton = refs.MetadataButton;
        field.Clear();
        var data = m_Tree.GetItemDataForIndex<object>(row);
        var context = new ResourceEntryContext
        {
            Table = table,
            MarkDirty = () => EditorUtility.SetDirty(table),
            Rebuild = RebuildTree
        };

        if (data is long keyId)
        {
            var entry = table.GetEntry(keyId);
            if (entry != null)
            {
                var entryProp = EntryProperty(table, keyId);
                if (entry is IAssetEntry assetEntry)
                {
                    field.Add(AssetEntryEditors.Get(entry.GetType()).CreateDefaultField(assetEntry, table));
                }
                else
                {
                    context.EntryProperty = entryProp;
                    field.Add(ResourceEntryDrawer.Get(entry.GetType()).CreateCell(entry, context));
                }
                ConfigureMetadataButton(metadataButton, entry.Metadata, table, () =>
                {
                    var current = EntryProperty(table, keyId);
                    return current != null ? current.propertyPath + "." + k_MetadataField : null;
                }, MetadataType.ResourceEntry, $"{L10n.Tr("Metadata", null)}: {KeyName(keyId)} · {table.LocaleIdentifier.Code}");
            }
            else
            {
                metadataButton.style.display = DisplayStyle.None;
                var first = FirstEntry(keyId);
                field.Add(first is IAssetEntry
                    ? BuildMissingAssetField(field, table, keyId, first.GetType())
                    : BuildMissingStringField(table, keyId));
            }
        }
        else if (data is VariantRow variantRow)
        {
            metadataButton.style.display = DisplayStyle.None;
            field.Add(BuildVariantCell(table, variantRow));
        }
    }

    TextField BuildMissingStringField(ResourceTable table, long keyId)
    {
        var key = KeyName(keyId);
        var newField = new TextField { multiline = true };
        newField.AddToClassList(LocClasses.LocValueField);
        newField.RegisterValueChangedCallback(evt =>
        {
            var existing = table.GetEntry(keyId) as StringEntry;
            if (existing == null)
            {
                RecordUndo("Add Entry Value");
                table.AddStringEntry(key, evt.newValue);
                // A variant key needs the variant subtype so its per-variant child cells can be authored.
                if (IsVariant(keyId))
                    PromoteToVariant(table, table.GetEntry(keyId));
            }
            else
            {
                existing.Value = evt.newValue;
            }
            EditorUtility.SetDirty(table);
        });
        // Rebuild once editing ends so the cell switches to the serialized-bound editor.
        newField.RegisterCallback<FocusOutEvent>(_ =>
        {
            if (table.GetEntry(keyId) != null)
                m_Root.schedule.Execute(RebuildTree);
        });
        return newField;
    }

    VisualElement BuildMissingAssetField(VisualElement field, ResourceTable table, long keyId, Type firstEntryType)
    {
        var kind = AssetEntryEditors.RegisteredKind(firstEntryType);
        var editor = AssetEntryEditors.Get(kind);
        var picker = new ObjectField { objectType = editor.AssetType, allowSceneObjects = false };
        picker.AddToClassList(LocClasses.LocValueField);
        picker.RegisterValueChangedCallback(evt =>
        {
            if (evt.newValue == null)
                return;
            RecordUndo("Add Entry Value");
            var entry = (IResourceEntry)Activator.CreateInstance(kind, keyId);
            table.AddEntry(entry);
            // A variant key needs the variant subtype so its per-variant child cells can be authored.
            if (IsVariant(keyId))
                entry = PromoteToVariant(table, entry);
            var real = editor.CreateDefaultField((IAssetEntry)entry, table);
            field.Clear();
            field.Add(real);
            if (real is ObjectField bound)
                bound.value = evt.newValue;
            EditorUtility.SetDirty(table);
            m_Root.schedule.Execute(RebuildTree);
        });
        return picker;
    }

    internal static string SharedMetadataPath(SharedTableData shared, long keyId)
    {
        if (shared == null)
            return null;
        var index = -1;
        for (var i = 0; i < shared.Entries.Count; i++)
        {
            if (shared.Entries[i] != null && shared.Entries[i].Id == keyId)
            {
                index = i;
                break;
            }
        }
        if (index < 0)
            return null;
        using var so = new SerializedObject(shared);
        var entries = so.FindProperty(k_EntriesField);
        if (entries == null || index >= entries.arraySize)
            return null;
        return entries.GetArrayElementAtIndex(index).FindPropertyRelative(k_MetadataField)?.propertyPath;
    }

    void ConfigureMetadataButton(Button button, MetadataCollection collection, Object owner, Func<string> metadataPath, MetadataType target, string title)
    {
        button.style.display = DisplayStyle.Flex;
        button.tooltip = L10n.Tr("Metadata", null);
        button.EnableInClassList(LocClasses.LocIconBtnActive, collection != null && collection.HasData);
        button.clickable = new Clickable(() => MetadataPopup.Show(button.worldBound, title, owner, metadataPath?.Invoke(), target,
            () => { if (owner != null) EditorUtility.SetDirty(owner); RebuildTree(); }));
    }

    VisualElement BuildVariantCell(ResourceTable table, VariantRow variantRow)
    {
        var entry = table.GetEntry(variantRow.KeyId);

        if (entry is IAssetEntry assetEntry)
            return AssetEntryEditors.Get(entry.GetType()).CreateVariantField(assetEntry, variantRow.VariantKey, table);

        if (entry is VariantStringEntry stringVariant)
        {
            var entryProp = EntryProperty(table, variantRow.KeyId);
            var valueProp = FindVariantValueProperty(entryProp, variantRow.VariantKey);
            var field = new TextField { multiline = true };
            field.AddToClassList(LocClasses.LocValueField);
            if (valueProp != null)
            {
                field.BindProperty(valueProp);
            }
            else
            {
                var index = stringVariant.Variants.FindIndex(v => v.Key == variantRow.VariantKey);
                field.SetValueWithoutNotify(index >= 0 ? stringVariant.Variants[index].Value : string.Empty);
                // One undo step per editing session, capturing the pre-edit state before the variant exists in the array.
                field.RegisterCallback<FocusInEvent>(_ => RecordUndo("Edit Variant Value"));
                field.RegisterValueChangedCallback(evt => { SetStringVariant(stringVariant, variantRow.VariantKey, evt.newValue); EditorUtility.SetDirty(table); });
                // Rebuild once editing ends so the cell binds to the now-serialized variant value.
                field.RegisterCallback<FocusOutEvent>(_ => m_Root.schedule.Execute(RebuildTree));
            }
            return field;
        }
        return new Label();
    }

    static SerializedProperty FindVariantValueProperty(SerializedProperty entryProp, string variantKey)
    {
        var variants = entryProp?.FindPropertyRelative(k_VariantsField);
        if (variants == null || !variants.isArray)
            return null;
        for (var i = 0; i < variants.arraySize; i++)
        {
            var element = variants.GetArrayElementAtIndex(i);
            var keyProp = element.FindPropertyRelative(k_VariantKeyField);
            if (keyProp != null && keyProp.stringValue == variantKey)
                return element.FindPropertyRelative(k_ValueField);
        }
        return null;
    }

    static void SetStringVariant(VariantStringEntry entry, string variantKey, string value)
    {
        var i = entry.Variants.FindIndex(v => v.Key == variantKey);
        if (i >= 0)
            entry.Variants[i] = new Variant<string>(variantKey, value);
        else
            entry.Variants.Add(new Variant<string>(variantKey, value));
    }

    void BindActionsCell(VisualElement element, int row)
    {
        var button = element.Q<Button>("actions-menu");
        var data = m_Tree.GetItemDataForIndex<object>(row);
        // Replace the click target for the recycled row; a new Clickable drops the previous row's handler.
        button.clickable = new Clickable(() =>
        {
            if (data is long keyId)
                ShowRowMenu(button, keyId);
            else if (data is VariantRow variantRow)
                ShowVariantRowMenu(button, variantRow);
        });
    }

    void ShowRowMenu(VisualElement anchor, long keyId)
    {
        var menu = new GenericMenu();
        var first = FirstEntry(keyId);

        menu.AddItem(new GUIContent(L10n.Tr("Rename key", null)), false, () => { m_RenamingKey = keyId; RebuildTree(); });

        if (first is IStringEntry)
        {
            var smart = IsSmart(keyId);
            menu.AddItem(new GUIContent(L10n.Tr("Smart string", null)), smart, () => SetSmart(keyId, !smart));
        }

        if (IsVariant(keyId) && SelectorOf(keyId) is { } selector)
        {
            var used = new HashSet<string>(VariantKeys(keyId));
            foreach (var variantKey in selector.AvailableKeys)
            {
                if (used.Contains(variantKey))
                    continue;
                var captured = variantKey;
                menu.AddItem(new GUIContent($"{L10n.Tr("Add variant", null)}/{captured}"), false, () => AddVariant(keyId, captured));
            }
        }

        menu.AddItem(new GUIContent(L10n.Tr("Delete key", null)), false, () => DeleteKey(keyId));
        menu.DropDown(anchor.worldBound);
    }

    void ShowVariantRowMenu(VisualElement anchor, VariantRow variantRow)
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent(L10n.Tr("Remove variant", null)), false, () => RemoveVariant(variantRow));
        menu.DropDown(anchor.worldBound);
    }
}
