// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Unity.Localization.Components;

namespace Unity.Localization.Editor;

// Adds a kind dropdown for the polymorphic list reference; the default managed-reference field offers no way to switch.
[CustomEditor(typeof(LocalizeStringListEvent))]
class LocalizeStringListEventEditor : UnityEditor.Editor
{
    [NoAutoStaticsCleanup] // immutable label table; a readonly field cannot be reassigned anyway
    static readonly (string Label, Type Type)[] k_Kinds =
    {
        (L10n.Tr("Single entry, split on a separator", null), typeof(LocalizedStringList)),
        (L10n.Tr("Group of entries", null), typeof(LocalizedStringGroup)),
    };

    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        var listProperty = serializedObject.FindProperty("m_List");

        var kindField = new DropdownField(LocLabels.ListKind);
        foreach (var kind in k_Kinds)
            kindField.choices.Add(kind.Label);
        kindField.SetValueWithoutNotify(KindLabel(listProperty.managedReferenceValue?.GetType()));

        var content = new VisualElement();
        kindField.RegisterValueChangedCallback(evt =>
        {
            var index = kindField.choices.IndexOf(evt.newValue);
            if (index < 0 || listProperty.managedReferenceValue?.GetType() == k_Kinds[index].Type)
                return;
            serializedObject.Update();
            listProperty.managedReferenceValue = Activator.CreateInstance(k_Kinds[index].Type);
            serializedObject.ApplyModifiedProperties();
            RebuildContent(content, listProperty);
        });

        RebuildContent(content, listProperty);

        root.Add(kindField);
        root.Add(content);
        root.Add(new PropertyField(serializedObject.FindProperty("m_OnUpdateList")));
        return root;
    }

    static string KindLabel(Type type)
    {
        foreach (var kind in k_Kinds)
        {
            if (kind.Type == type)
                return kind.Label;
        }
        return k_Kinds[0].Label;
    }

    static void RebuildContent(VisualElement content, SerializedProperty listProperty)
    {
        content.Clear();
        var reference = new PropertyField(listProperty, L10n.Tr("List reference", null));
        reference.Bind(listProperty.serializedObject);
        content.Add(reference);

        var separator = listProperty.FindPropertyRelative("m_Separator");
        if (separator != null)
        {
            var separatorField = new PropertyField(separator);
            separatorField.Bind(listProperty.serializedObject);
            content.Add(separatorField);
        }
    }
}
