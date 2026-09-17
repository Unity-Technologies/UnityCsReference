// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

// Lists every collection in the project with a toggle per locale table, for exports that span collections.
class CollectionTableSelector : VisualElement
{
    readonly List<(ResourceTableCollection Collection, ResourceTable Table, Toggle Toggle)> m_Entries = new();
    readonly VisualElement m_List;

    public CollectionTableSelector()
    {
        var toolbar = new VisualElement();
        toolbar.AddToClassList(LocClasses.LocExtensionRow);
        var search = new ToolbarSearchField();
        search.AddToClassList(LocClasses.LsGrow);
        search.RegisterValueChangedCallback(evt => Filter(evt.newValue));
        toolbar.Add(search);
        toolbar.Add(new Button(() => SelectVisible(true)) { text = LocLabels.SelectAll });
        toolbar.Add(new Button(() => SelectVisible(false)) { text = LocLabels.SelectNone });
        Add(toolbar);

        m_List = new ScrollView();
        m_List.AddToClassList(LocClasses.LsGrow);
        Add(m_List);

        RegisterCallback<AttachToPanelEvent>(_ => LocalizationEditorSettings.CollectionsChanged += Rebuild);
        RegisterCallback<DetachFromPanelEvent>(_ => LocalizationEditorSettings.CollectionsChanged -= Rebuild);

        Rebuild();
    }

    public bool AnySelected => m_Entries.Exists(e => e.Toggle.value);

    public IEnumerable<(ResourceTableCollection Collection, ResourceTable Table)> SelectedTables()
    {
        foreach (var entry in m_Entries)
        {
            if (entry.Toggle.value)
                yield return (entry.Collection, entry.Table);
        }
    }

    public void SetSelection(ResourceTableCollection collection)
    {
        foreach (var entry in m_Entries)
            entry.Toggle.SetValueWithoutNotify(entry.Collection == collection);
    }

    void Rebuild()
    {
        var selected = new HashSet<ResourceTable>();
        foreach (var entry in m_Entries)
        {
            if (entry.Toggle.value)
                selected.Add(entry.Table);
        }

        m_List.Clear();
        m_Entries.Clear();

        foreach (var collection in AssetProviderEditors.GetKnownCollections())
        {
            if (collection == null)
                continue;
            var foldout = new Foldout { text = collection.TableCollectionName, value = true };
            foreach (var table in collection.Tables)
            {
                if (table == null)
                    continue;
                var toggle = new Toggle(LocaleLabel(table.LocaleIdentifier)) { value = selected.Contains(table) };
                m_Entries.Add((collection, table, toggle));
                foldout.Add(toggle);
            }
            m_List.Add(foldout);
        }
    }

    static string LocaleLabel(LocaleIdentifier identifier)
    {
        var settings = LocalizationEditorSettings.ActiveSettings;
        var locale = settings != null ? settings.GetLocale(identifier) : null;
        return locale != null ? $"{locale.LocaleName} ({identifier.Code})" : identifier.Code;
    }

    void Filter(string search)
    {
        var showAll = string.IsNullOrEmpty(search);
        m_List.Query<Foldout>().ForEach(foldout =>
        {
            var matches = showAll || foldout.text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
            foldout.style.display = matches ? DisplayStyle.Flex : DisplayStyle.None;
        });
    }

    void SelectVisible(bool selected)
    {
        m_List.Query<Foldout>().ForEach(foldout =>
        {
            if (foldout.style.display == DisplayStyle.None)
                return;
            foldout.Query<Toggle>().ForEach(toggle => toggle.value = selected);
        });
    }
}
