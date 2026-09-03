// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.SmartStrings.Core.Extensions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.SmartStrings.Editor;

// UI Toolkit drawer for SmartFormatter: the Sources and Formatters lists, each with an Add button
// that opens a menu of the available ISource / IFormatter implementations.
[CustomPropertyDrawer(typeof(SmartFormatter))]
class SmartFormatterPropertyDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new VisualElement { style = { flexShrink = 1, minWidth = 0 } };
        HideHostHorizontalScroller(root);

        var settings = property.FindPropertyRelative("m_SmartSettings");
        if (settings != null)
            root.Add(new PropertyField(settings, L10n.Tr("Settings", null)));

        root.Add(BuildList(L10n.Tr("Sources", null), L10n.Tr("Evaluates a selector. Checked in order, top first.", null),
            property.FindPropertyRelative("m_Sources"), typeof(ISource), property.serializedObject));
        root.Add(BuildList(L10n.Tr("Formatters", null), L10n.Tr("Converts a value to a string. Checked in order, top first.", null),
            property.FindPropertyRelative("m_Formatters"), typeof(IFormatter), property.serializedObject));

        return root;
    }

    static ListView BuildList(string title, string tooltip, SerializedProperty arrayProperty, Type baseType, SerializedObject serializedObject)
    {
        var path = arrayProperty.propertyPath;
        var listView = new ListView
        {
            showFoldoutHeader = true,
            headerTitle = title,
            tooltip = tooltip,
            showAddRemoveFooter = true,
            reorderable = true,
            reorderMode = ListViewReorderMode.Animated,
            showBoundCollectionSize = false,
            horizontalScrollingEnabled = false,
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            selectionType = SelectionType.Single,
            style = { marginTop = 4, flexShrink = 1, minWidth = 0 }
        };
        listView.makeItem = () => new PropertyField();
        listView.bindItem = (element, index) =>
        {
            var array = serializedObject.FindProperty(path);
            if (array == null || index >= array.arraySize)
                return;
            var elementProperty = array.GetArrayElementAtIndex(index);
            var field = (PropertyField)element;
            field.label = GetElementLabel(elementProperty);
            field.BindProperty(elementProperty);
        };
        listView.unbindItem = (element, index) => ((PropertyField)element).Unbind();
        listView.BindProperty(arrayProperty);
        listView.overridingAddButtonBehavior = (view, button) => ShowAddMenu(baseType, path, serializedObject, view, button);
        return listView;
    }

    static void ShowAddMenu(Type baseType, string arrayPath, SerializedObject serializedObject, BaseListView listView, Button addButton)
    {
        serializedObject.Update();
        var arrayProperty = serializedObject.FindProperty(arrayPath);

        var present = new HashSet<Type>();
        for (var i = 0; i < arrayProperty.arraySize; ++i)
        {
            var value = arrayProperty.GetArrayElementAtIndex(i).managedReferenceValue;
            if (value != null)
                present.Add(value.GetType());
        }

        var types = new List<Type>();
        foreach (var type in TypeCache.GetTypesDerivedFrom(baseType))
        {
            if (type.IsAbstract || type.IsGenericType)
                continue;
            // Managed references cannot be UnityEngine.Object instances.
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                continue;
            if (Attribute.IsDefined(type, typeof(ObsoleteAttribute)))
                continue;
            // Needs a parameterless constructor to be instantiated and serialized as a managed reference.
            if (type.GetConstructor(Type.EmptyTypes) == null)
                continue;
            types.Add(type);
        }
        types.Sort((a, b) => string.Compare(GetDisplayName(a), GetDisplayName(b), StringComparison.Ordinal));

        var menu = new GenericDropdownMenu();
        foreach (var type in types)
        {
            var name = GetDisplayName(type);
            if (present.Contains(type))
            {
                // Already added: the formatter would be skipped (sources) or rejected for a duplicate name (formatters).
                menu.AddDisabledItem(name, true);
            }
            else
            {
                var captured = type;
                menu.AddItem(name, false, () => AddType(serializedObject, arrayPath, captured, listView));
            }
        }

        menu.DropDown(addButton.worldBound, addButton, DropdownMenuSizeMode.Auto);
    }

    static void AddType(SerializedObject serializedObject, string arrayPath, Type type, BaseListView listView)
    {
        serializedObject.Update();
        var arrayProperty = serializedObject.FindProperty(arrayPath);
        var index = arrayProperty.arraySize;
        arrayProperty.InsertArrayElementAtIndex(index);
        arrayProperty.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(type);
        serializedObject.ApplyModifiedProperties();
        listView.Rebuild();
    }

    // The host ScrollView (inspector window or settings page) otherwise shows a spurious horizontal
    // scroller for this content. Hide it while the drawer is attached, and restore it on detach so it
    // does not linger for other inspectors.
    static void HideHostHorizontalScroller(VisualElement root)
    {
        var hidden = new List<ScrollView>();
        root.RegisterCallback<AttachToPanelEvent>(_ =>
        {
            for (var p = root.hierarchy.parent; p != null; p = p.hierarchy.parent)
            {
                if (p is ScrollView scrollView && scrollView.horizontalScrollerVisibility != ScrollerVisibility.Hidden)
                {
                    scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                    hidden.Add(scrollView);
                }
            }
        });
        root.RegisterCallback<DetachFromPanelEvent>(_ =>
        {
            foreach (var scrollView in hidden)
                scrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            hidden.Clear();
        });
    }

    static string GetElementLabel(SerializedProperty element)
    {
        var value = element.managedReferenceValue;
        return value != null ? GetDisplayName(value.GetType()) : L10n.Tr("None", null);
    }

    static string GetDisplayName(Type type) => L10n.Tr(ObjectNames.NicifyVariableName(type.Name), null);
}
