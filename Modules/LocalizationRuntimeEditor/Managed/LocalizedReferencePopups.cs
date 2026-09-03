// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: UIToolkitFramework not yet converted
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

/// <summary>Small form for creating a new table entry: a name plus the collection to add it to, validated before creation.</summary>
class NewEntryPopup : EditorWindow
{
    const string k_Uxml = "LocalizationRuntime/UXML/NewEntryPopup.uxml";

    Action<ResourceTableCollection, string> m_OnCreate;
    List<ResourceTableCollection> m_Collections;
    ResourceTableCollection m_Selected;
    string m_Name = string.Empty;
    Label m_Message;
    Button m_Create;

    /// <summary>Shows the form below <paramref name="activatorWorldBound"/>, preselecting <paramref name="preferred"/> when given.</summary>
    public static void Show(Rect activatorWorldBound, ResourceTableCollection preferred, Action<ResourceTableCollection, string> onCreate)
    {
        var window = CreateInstance<NewEntryPopup>();
        window.m_OnCreate = onCreate;
        window.m_Collections = AssetProviderEditors.GetKnownCollections();
        window.m_Selected = preferred != null ? preferred : (window.m_Collections.Count > 0 ? window.m_Collections[0] : null);
        window.titleContent = new GUIContent(L10n.Tr("New table entry", null));
        var screenRect = GUIUtility.GUIToScreenRect(activatorWorldBound);
        window.ShowAsDropDown(screenRect, new Vector2(260, 240));
    }

    void CreateGUI()
    {
        var root = rootVisualElement;
        LocStyles.Apply(root, LocalizedReferenceElement.StyleSheet);
        if (EditorGUIUtility.LoadRequired(k_Uxml) is not VisualTreeAsset tree)
            return;
        tree.CloneTree(root);

        root.Q<Label>("name-label").text = L10n.Tr("Name", null);
        root.Q<Label>("collection-label").text = L10n.Tr("Collection", null);
        m_Message = root.Q<Label>("message");

        var nameField = root.Q<TextField>("name-field");
        nameField.SetValueWithoutNotify(m_Name);
        nameField.RegisterValueChangedCallback(evt => { m_Name = evt.newValue; Validate(); });

        var list = root.Q<ScrollView>("collection-list");
        foreach (var collection in m_Collections)
        {
            var captured = collection;
            var row = new VisualElement();
            row.AddToClassList(LocClasses.LsRailRow);
            if (collection == m_Selected)
                row.AddToClassList(LocClasses.LsRailRowSelected);
            row.Add(new Label(collection.TableCollectionName).WithClass(LocClasses.LsPopupCollection));
            row.RegisterCallback<PointerDownEvent>(_ => { m_Selected = captured; RebuildSelection(list); Validate(); });
            list.Add(row);
        }

        var cancel = root.Q<Button>("cancel");
        cancel.text = L10n.Tr("Cancel", null);
        cancel.clicked += Close;
        m_Create = root.Q<Button>("create");
        m_Create.text = L10n.Tr("Create", null);
        m_Create.clicked += Create;

        nameField.schedule.Execute(() => nameField.Focus());
        Validate();
    }

    void RebuildSelection(VisualElement list)
    {
        var index = 0;
        foreach (var row in list.Query(className: "ls-rail-row").ToList())
        {
            row.RemoveFromClassList(LocClasses.LsRailRowSelected);
            if (index < m_Collections.Count && m_Collections[index] == m_Selected)
                row.AddToClassList(LocClasses.LsRailRowSelected);
            index++;
        }
    }

    void Validate()
    {
        var trimmed = m_Name?.Trim();
        string error = null;
        if (m_Selected == null)
            error = L10n.Tr("No collection to add the entry to.", null);
        else if (string.IsNullOrEmpty(trimmed))
            error = L10n.Tr("Enter a name.", null);
        else if (m_Selected.SharedData != null && m_Selected.SharedData.GetEntry(trimmed) != null)
            error = L10n.Tr("A key with this name already exists.", null);

        m_Message.text = error ?? string.Empty;
        m_Create?.SetEnabled(error == null);
    }

    void Create()
    {
        var trimmed = m_Name?.Trim();
        if (m_Selected == null || string.IsNullOrEmpty(trimmed))
            return;
        m_OnCreate?.Invoke(m_Selected, trimmed);
        Close();
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
