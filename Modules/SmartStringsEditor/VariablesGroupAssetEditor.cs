// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.SmartStrings.PersistentVariables;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.SmartStrings.Editor;

[CustomEditor(typeof(VariablesGroupAsset), true)]
class VariablesGroupAssetEditor : UnityEditor.Editor
{
    internal const string k_VariablesPath = "m_Variables";
    internal const string k_NamePath = "name";
    internal const string k_VariablePath = "variable";

    const string k_DefaultName = "variable";
    const int k_NameWidth = 120;
    const int k_TypeWidth = 120;

    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        // This editor also serves subclasses, whose own fields would otherwise never be drawn.
        InspectorElement.FillDefaultInspector(root, serializedObject, this, k_VariablesPath);
        root.Add(BuildList(serializedObject));
        return root;
    }

    static ListView BuildList(SerializedObject serializedObject)
    {
        var listView = ManagedReferenceUI.CreateList(L10n.Tr("Variables", null),
            L10n.Tr("Named values a Smart String reads as {group-name.variable-name}.", null));

        listView.makeItem = () => new VisualElement();
        listView.bindItem = (element, index) =>
        {
            var items = serializedObject.FindProperty(k_VariablesPath);
            element.Clear();
            // A removal can rebind a row before the list has shrunk.
            if (index >= items.arraySize)
                return;
            element.Add(BuildRow(items.GetArrayElementAtIndex(index), listView.Rebuild));
        };
        listView.unbindItem = (element, index) => element.Clear();
        listView.BindProperty(serializedObject.FindProperty(k_VariablesPath));
        listView.overridingAddButtonBehavior = (view, button) => ShowAddMenu(button, serializedObject, view);
        return listView;
    }

    internal static VisualElement BuildRow(SerializedProperty element, Action onTypeChanged)
    {
        var nameProperty = element.FindPropertyRelative(k_NamePath);
        var variableProperty = element.FindPropertyRelative(k_VariablePath);

        var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexShrink = 1, minWidth = 0 } };

        var nameField = new TextField
        {
            value = nameProperty.stringValue,
            isDelayed = true,
            tooltip = L10n.Tr("Read from the string as {group-name.name}; whitespace becomes '-'.", null),
            style = { width = k_NameWidth, flexShrink = 0 }
        };
        nameField.RegisterValueChangedCallback(evt => SetName(nameProperty, evt.newValue, nameField));
        row.Add(nameField);

        Button typeButton = null;
        typeButton = new Button(() => ShowChangeTypeMenu(typeButton, variableProperty, onTypeChanged))
        {
            text = TypeLabel(variableProperty),
            tooltip = L10n.Tr("The kind of value this variable holds.", null),
            style = { width = k_TypeWidth, flexShrink = 0 }
        };
        row.Add(typeButton);

        var fields = VariableFields(variableProperty);
        if (fields.Count == 1)
        {
            var valueField = new PropertyField(fields[0], string.Empty) { style = { flexGrow = 1, minWidth = 0 } };
            valueField.BindProperty(fields[0]);
            row.Add(valueField);
            return row;
        }

        row.Add(new VisualElement { style = { flexGrow = 1, minWidth = 0 } });
        if (fields.Count == 0)
            return row;

        // Several fields do not fit on the row, so the variable draws itself underneath instead.
        var container = new VisualElement();
        container.Add(row);
        var nested = new PropertyField(variableProperty, string.Empty);
        nested.BindProperty(variableProperty);
        container.Add(nested);
        return container;
    }

    internal static void AddVariable(SerializedObject serializedObject, Type type)
    {
        serializedObject.Update();
        var array = serializedObject.FindProperty(k_VariablesPath);

        // Read before the insert, which copies the preceding element's name.
        var name = UniqueName(array);
        var index = array.arraySize;
        array.InsertArrayElementAtIndex(index);

        var element = array.GetArrayElementAtIndex(index);
        element.FindPropertyRelative(k_VariablePath).managedReferenceValue = Activator.CreateInstance(type);
        element.FindPropertyRelative(k_NamePath).stringValue = name;
        serializedObject.ApplyModifiedProperties();
    }

    internal static void SetVariableType(SerializedProperty variableProperty, Type type)
    {
        // Re-picking the type the variable already has would throw its value away.
        if (variableProperty.managedReferenceValue?.GetType() == type)
            return;

        var serializedObject = variableProperty.serializedObject;
        serializedObject.Update();
        variableProperty.managedReferenceValue = Activator.CreateInstance(type);
        serializedObject.ApplyModifiedProperties();
    }

    internal static void SetName(SerializedProperty nameProperty, string value, TextField field)
    {
        var name = value.ReplaceWhiteSpaces("-");

        // The group drops an empty name from its lookup and lets a duplicate shadow the earlier
        // entry, so refuse both and put the old name back.
        if (name.Length == 0 || IsNameTaken(nameProperty, name))
        {
            field?.SetValueWithoutNotify(nameProperty.stringValue);
            return;
        }

        if (field != null && name != value)
            field.SetValueWithoutNotify(name);

        var serializedObject = nameProperty.serializedObject;
        serializedObject.Update();
        nameProperty.stringValue = name;
        serializedObject.ApplyModifiedProperties();
    }

    static bool IsNameTaken(SerializedProperty nameProperty, string name)
    {
        var array = nameProperty.serializedObject.FindProperty(k_VariablesPath);
        for (var i = 0; i < array.arraySize; ++i)
        {
            var sibling = array.GetArrayElementAtIndex(i).FindPropertyRelative(k_NamePath);
            if (sibling.propertyPath != nameProperty.propertyPath && sibling.stringValue == name)
                return true;
        }
        return false;
    }

    static List<SerializedProperty> VariableFields(SerializedProperty variableProperty)
    {
        var fields = new List<SerializedProperty>();
        if (variableProperty.managedReferenceValue == null)
            return fields;

        // Next rather than NextVisible, which would report no fields while the row is collapsed.
        var iterator = variableProperty.Copy();
        var enterChildren = true;
        while (iterator.Next(enterChildren) && iterator.depth > variableProperty.depth)
        {
            enterChildren = false;
            fields.Add(iterator.Copy());
        }
        return fields;
    }

    static string UniqueName(SerializedProperty array)
    {
        var existing = new HashSet<string>();
        for (var i = 0; i < array.arraySize; ++i)
            existing.Add(array.GetArrayElementAtIndex(i).FindPropertyRelative(k_NamePath).stringValue);

        if (!existing.Contains(k_DefaultName))
            return k_DefaultName;
        for (var i = 2; ; ++i)
        {
            var candidate = $"{k_DefaultName}-{i}";
            if (!existing.Contains(candidate))
                return candidate;
        }
    }

    static string TypeLabel(SerializedProperty variableProperty)
    {
        var type = variableProperty.managedReferenceValue?.GetType();
        return type == null ? L10n.Tr("None", null) : ManagedReferenceUI.DisplayName(type);
    }

    static void ShowAddMenu(Button anchor, SerializedObject serializedObject, BaseListView listView)
        => ShowTypeMenu(anchor, null, type =>
        {
            AddVariable(serializedObject, type);
            listView.Rebuild();
        });

    static void ShowChangeTypeMenu(Button anchor, SerializedProperty variableProperty, Action onTypeChanged)
        => ShowTypeMenu(anchor, variableProperty.managedReferenceValue?.GetType(), type =>
        {
            SetVariableType(variableProperty, type);
            onTypeChanged?.Invoke();
        });

    static void ShowTypeMenu(Button anchor, Type current, Action<Type> onPick)
    {
        var menu = new GenericDropdownMenu();
        foreach (var type in ManagedReferenceUI.ConcreteTypes(typeof(IVariable)))
            menu.AddItem(ManagedReferenceUI.DisplayName(type), type == current, () => onPick(type));
        menu.DropDown(anchor.worldBound, anchor, DropdownMenuSizeMode.Auto);
    }
}
