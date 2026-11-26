// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Localization.Providers;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

static class StartupSelectorList
{
    internal const string PropertyPath = "m_StartupSelectors";

    internal static ListView Create(SerializedProperty property, Action onChanged = null)
    {
        var serialized = property.serializedObject;
        var path = property.propertyPath;
        // StartupLocaleSelectorDrawer already titles each row by its concrete type, so no element label is needed here.
        return LocalizationSettingsProvider.MakeManagedReferenceList(serialized, property,
            (list, button) => ShowAddMenu(serialized, path, list, button, onChanged));
    }

    internal static void AddSelector(SerializedObject serialized, string path, Type type)
    {
        serialized.Update();
        var array = serialized.FindProperty(path);
        var index = array.arraySize;
        array.InsertArrayElementAtIndex(index);
        array.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(type);
        serialized.ApplyModifiedProperties();
    }

    static void ShowAddMenu(SerializedObject serialized, string path, BaseListView list, Button button, Action onChanged)
    {
        // The startup-selector list is order-based and allows repeated selector types, so offer every type.
        var types = new List<Type>(AssetProviderEditors.GetInstantiableTypes<IStartupLocaleSelector>(excludeUnityObjects: true));
        types.Sort((a, b) => string.Compare(LocalizationSettingsProvider.SelectorDisplayName(a),
            LocalizationSettingsProvider.SelectorDisplayName(b), StringComparison.Ordinal));

        var menu = new GenericDropdownMenu();
        foreach (var type in types)
        {
            var captured = type;
            menu.AddItem(LocalizationSettingsProvider.SelectorDisplayName(type), false, () =>
            {
                AddSelector(serialized, path, captured);
                list.Rebuild();
                onChanged?.Invoke();
            });
        }
        menu.DropDown(button.worldBound, button, DropdownMenuSizeMode.Auto);
    }
}
