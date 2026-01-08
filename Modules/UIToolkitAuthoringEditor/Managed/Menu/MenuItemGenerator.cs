// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Reflection;
using UnityEngine.Pool;
using Menu = UnityEditor.Menu;

namespace Unity.UIToolkit.Editor;

readonly struct ControlTypeInfo
{
    public LibraryTypeKey libraryType { get; }
    public string libraryPath { get; }
    public int priority { get; }

    public ControlTypeInfo(LibraryTypeKey libraryType, string libraryPath, int priority)
    {
        this.libraryType = libraryType;
        this.libraryPath = libraryPath;
        this.priority = priority;
    }

    public string GetMenuPath() => $"{libraryPath}/{libraryType.name}";
}

internal static class MenuItemGenerator
{
    static readonly List<ControlTypeInfo> s_AvailableControlTypes;
    // The current value represents the first item of the "GameObject/UI Toolkit" menu item. Update this value to change the position within the menu item.
    internal const int k_DefaultPriority = 8;
    internal const string k_MenuPrefix = "GameObject/UI Toolkit";

    static MenuItemGenerator()
    {
        s_AvailableControlTypes = GetControlElements();
    }

    public static List<ControlTypeInfo> GetAvailableControlTypes()
    {
        return s_AvailableControlTypes;
    }

    public static void RegisterMenuItems()
    {
        foreach (var control in s_AvailableControlTypes)
        {
            Menu.AddMenuItem(
                $"{k_MenuPrefix}/{control.GetMenuPath()}",
                "",
                false,
                control.priority,
                () => new AddElementCommand(control.libraryType.type, Selection.activeGameObject?.GetComponent<UIDocument>()).Execute(),
                null
            );
        }
    }

    public static void UnregisterMenuItems()
    {
        foreach (var control in s_AvailableControlTypes)
        {
            Menu.RemoveMenuItem($"{k_MenuPrefix}/{control.GetMenuPath()}");
        }
    }

    static List<ControlTypeInfo> GetControlElements()
    {
        var elements = ListPool<(LibraryTypeKey libraryType, string libraryPath)>.Get();
        var libraryTypeKeys = LibraryContent.GetAllLibraryTypes().Keys;

        foreach (var key in libraryTypeKeys)
        {
            // Look for UxmlElement attribute
            var uxmlElementAttr = key.type.GetCustomAttribute<UxmlElementAttribute>();
            if (uxmlElementAttr == null)
                continue;

            // We only want to add the items that contain a "libraryPath" uxml attribute.
            if (string.IsNullOrEmpty(uxmlElementAttr.libraryPath))
                continue;

            elements.Add((key, uxmlElementAttr.libraryPath));
        }

        // Sort by type name and assign priorities
        elements.Sort((a, b) => string.Compare(a.libraryType.name, b.libraryType.name, StringComparison.Ordinal));

        var result = new List<ControlTypeInfo>();
        var priority = k_DefaultPriority;
        for (var i = 0; i < elements.Count; i++)
        {
            result.Add(new ControlTypeInfo(elements[i].libraryType, elements[i].libraryPath, priority: priority++));
        }

        ListPool<(LibraryTypeKey type, string libraryPath)>.Release(elements);
        return result;
    }
}
