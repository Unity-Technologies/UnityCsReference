// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Localization.Providers;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

// The footer button offers every IAssetProvider type, because the chain is order-based and allows repeats.
[CustomPropertyDrawer(typeof(AssetProvider))]
class AssetProviderDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var providers = property.FindPropertyRelative("m_Providers");
        var path = providers.propertyPath;
        var serializedObject = property.serializedObject;

        var listView = new ListView
        {
            showFoldoutHeader = true,
            headerTitle = L10n.Tr("Asset Providers", null),
            showAddRemoveFooter = true,
            reorderable = true,
            reorderMode = ListViewReorderMode.Animated,
            showBoundCollectionSize = false,
            virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
            selectionType = SelectionType.Single
        };
        LocStyles.Apply(listView);
        listView.AddToClassList(LocClasses.LocList);
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
        listView.BindProperty(providers);
        listView.overridingAddButtonBehavior = (view, button) => ShowAddMenu(path, serializedObject, view, button);
        listView.onRemove = view => RemoveProvider(path, serializedObject, view);
        return listView;
    }

    static void RemoveProvider(string arrayPath, SerializedObject serializedObject, BaseListView listView)
    {
        var array = serializedObject.FindProperty(arrayPath);
        if (array == null || array.arraySize == 0)
            return;
        var index = listView.selectedIndex >= 0 && listView.selectedIndex < array.arraySize ? listView.selectedIndex : array.arraySize - 1;
        array.DeleteArrayElementAtIndex(index);
        serializedObject.ApplyModifiedProperties();

        var settings = serializedObject.targetObject as LocalizationSettings;
        settings?.Database?.AssetProvider?.RevalidateSelection();
        if (settings != null && settings == LocalizationEditorSettings.ActiveSettings)
            AssetProviderEditors.RebuildRegistrations();
        EditorUtility.SetDirty(serializedObject.targetObject);
        serializedObject.Update();
        listView.Rebuild();
    }

    static void ShowAddMenu(string arrayPath, SerializedObject serializedObject, BaseListView listView, Button addButton)
    {
        var types = AssetProviderEditors.AddableProviderTypes(ExistingProviders(serializedObject, arrayPath));

        var menu = new GenericDropdownMenu();
        if (types.Count == 0)
            menu.AddDisabledItem(L10n.Tr("Every content source is already added", null), false);
        foreach (var type in types)
        {
            var captured = type;
            menu.AddItem(Name(type), false, () => AddType(serializedObject, arrayPath, captured, listView));
        }
        menu.DropDown(addButton.worldBound, addButton, DropdownMenuSizeMode.Auto);
    }

    static IEnumerable<IAssetProvider> ExistingProviders(SerializedObject serializedObject, string arrayPath)
    {
        var array = serializedObject.FindProperty(arrayPath);
        for (var i = 0; array != null && i < array.arraySize; i++)
            yield return array.GetArrayElementAtIndex(i).managedReferenceValue as IAssetProvider;
    }

    static void AddType(SerializedObject serializedObject, string arrayPath, Type type, BaseListView listView)
    {
        var instance = (IAssetProvider)Activator.CreateInstance(type);
        serializedObject.Update();
        var arrayProperty = serializedObject.FindProperty(arrayPath);
        var index = arrayProperty.arraySize;
        arrayProperty.InsertArrayElementAtIndex(index);
        arrayProperty.GetArrayElementAtIndex(index).managedReferenceValue = instance;
        serializedObject.ApplyModifiedProperties();

        // Adding a source can supply the first valid backend, so re-register collections left unassigned while none existed.
        if (serializedObject.targetObject is LocalizationSettings settings && settings == LocalizationEditorSettings.ActiveSettings)
            AssetProviderEditors.RebuildRegistrations();
        listView.Rebuild();
    }

    static string GetElementLabel(SerializedProperty element)
    {
        var value = element.managedReferenceValue;
        return value != null ? Name(value.GetType()) : L10n.Tr("None", null);
    }

    static string Name(Type type) => ObjectNames.NicifyVariableName(type.Name);
}
