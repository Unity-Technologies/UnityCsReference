// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

static class MetadataEditor
{
    const string k_Uxml = "LocalizationRuntime/UXML/MetadataEditor.uxml";

    public static VisualElement Create(SerializedProperty metadataProp, MetadataType target, Action onChanged)
    {
        var root = (EditorGUIUtility.Load(k_Uxml) as VisualTreeAsset).Instantiate();
        var list = root.Q<ListView>("items-list");
        var itemsProp = metadataProp.FindPropertyRelative("m_Items");
        if (itemsProp == null || !itemsProp.isArray)
            return root;

        list.bindItem = (element, index) => BindItem(element, itemsProp, index);
        list.unbindItem = (element, _) => element.Q<VisualElement>("body").Clear();
        list.BindProperty(itemsProp);

        list.overridingAddButtonBehavior = (_, button) => ShowAddMenu(button, metadataProp, list, target, onChanged);
        list.itemsRemoved += _ => onChanged?.Invoke();
        return root;
    }

    static void BindItem(VisualElement element, SerializedProperty itemsProp, int index)
    {
        var body = element.Q<VisualElement>("body");
        body.Clear();
        if (index < 0 || index >= itemsProp.arraySize)
            return;

        var itemProp = itemsProp.GetArrayElementAtIndex(index);
        var value = itemProp.managedReferenceValue;
        element.Q<Label>("type-label").text = ObjectNames.NicifyVariableName(value?.GetType().Name ?? "Metadata");

        if (value is Comment)
        {
            var textProp = itemProp.FindPropertyRelative("m_CommentText");
            var field = new TextField { multiline = true };
            field.AddToClassList(LocClasses.LocMetadataItemComment);
            if (textProp != null)
                field.BindProperty(textProp);
            body.Add(field);
        }
    }

    static void ShowAddMenu(VisualElement anchor, SerializedProperty metadataProp, ListView list, MetadataType target, Action onChanged)
    {
        var menu = new GenericDropdownMenu();
        var any = false;
        foreach (var type in TypeCache.GetTypesDerivedFrom<IMetadata>())
        {
            if (type.IsAbstract || type.IsGenericType || type.GetConstructor(Type.EmptyTypes) == null)
                continue;
            var attribute = (MetadataAttribute)Attribute.GetCustomAttribute(type, typeof(MetadataAttribute));
            var allowed = attribute?.AllowedTypes ?? MetadataType.All;
            if ((allowed & target) == 0)
                continue;

            var name = attribute != null && !string.IsNullOrEmpty(attribute.MenuItem) ? attribute.MenuItem : ObjectNames.NicifyVariableName(type.Name);
            var allowMultiple = attribute?.AllowMultiple ?? true;
            any = true;

            if (!allowMultiple && ContainsType(metadataProp, type))
            {
                menu.AddDisabledItem(name, true);
                continue;
            }
            var captured = type;
            menu.AddItem(name, false, () => AddMetadata(metadataProp, list, captured, onChanged));
        }
        if (!any)
            menu.AddDisabledItem(L10n.Tr("No metadata types", null), false);
        menu.DropDown(anchor.worldBound, anchor, DropdownMenuSizeMode.Auto);
    }

    static void AddMetadata(SerializedProperty metadataProp, ListView list, Type type, Action onChanged)
    {
        var so = metadataProp.serializedObject;
        so.Update();
        var items = metadataProp.FindPropertyRelative("m_Items");
        var index = items.arraySize;
        items.arraySize++;
        items.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(type);
        so.ApplyModifiedProperties();
        list.RefreshItems();
        onChanged?.Invoke();
    }

    static bool ContainsType(SerializedProperty metadataProp, Type type)
    {
        var items = metadataProp.FindPropertyRelative("m_Items");
        if (items == null || !items.isArray)
            return false;
        for (var i = 0; i < items.arraySize; i++)
        {
            var value = items.GetArrayElementAtIndex(i).managedReferenceValue;
            if (value != null && value.GetType() == type)
                return true;
        }
        return false;
    }
}
