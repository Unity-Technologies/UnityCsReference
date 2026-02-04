// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using System.Threading;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using Menu = UnityEditor.Menu;

namespace Unity.UIToolkit.Editor;

readonly struct ControlTypeInfo
{
    internal static readonly string k_ContextMenuPrefix = "UI Toolkit";

    public LibraryTypeKey libraryType { get; }
    public string libraryPath { get; }
    public int priority { get; }

    public ControlTypeInfo(LibraryTypeKey libraryType, string libraryPath, int priority)
    {
        this.libraryType = libraryType;
        this.libraryPath = libraryPath;
        this.priority = priority;
    }

    public string GetMenuPath() => string.IsNullOrEmpty(libraryPath)
        ? $"{k_ContextMenuPrefix}/{libraryType.name}"
        : $"{k_ContextMenuPrefix}/{libraryPath}/{libraryType.name}";
}

class ScopedMenuItemGenerator : IDisposable
{
    static int s_ReferenceCount = 0;
    public ScopedMenuItemGenerator()
    {
        if (Interlocked.Increment(ref s_ReferenceCount) == 1)
        {
            MenuItemGenerator.RegisterMenuItems();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Decrement(ref s_ReferenceCount) == 0)
        {
            MenuItemGenerator.UnregisterMenuItems();
        }
    }
}

internal static class MenuItemGenerator
{
    static readonly List<ControlTypeInfo> s_AvailableControlTypes;
    // The current value represents the first item of the "GameObject/UI Toolkit" menu item. Update this value to change the position within the menu item.
    internal const int k_DefaultPriority = 7;
    internal const int k_LibraryMenuItemPriorityOffset = 20;
    internal const string k_MenuPrefix = "GameObject";

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
                () =>
                {
                    var stage = StageUtility.GetCurrentStage() as VisualElementEditingStage;
                    var cmd = new AddElementCommand(
                        control.libraryType.type,
                        stage.EditedVisualTreeAsset,
                        (Selection.activeObject as VisualElementSelection)?.Element?.visualElementAsset
                    );
                    cmd.Execute();
                    stage.RequestRefresh();
                },
                null
            );
        }

        var maxPriority = s_AvailableControlTypes.Count > 0
            ? s_AvailableControlTypes[^1].priority
            : k_DefaultPriority;

        foreach (var basePath in LibraryContent.k_BaseLibraryPaths)
        {
            if (HasControlsUnderBasePath(basePath))
            {
                var menuPath = $"{k_MenuPrefix}/{ControlTypeInfo.k_ContextMenuPrefix}/{basePath}/UI Library...";
                Menu.AddMenuItem(
                    menuPath,
                    "",
                    false,
                    maxPriority + k_LibraryMenuItemPriorityOffset,
                    UIElementsProvider.OpenUIElementsPicker,
                    null
                );
            }
        }
    }

    public static void UnregisterMenuItems()
    {
        foreach (var control in s_AvailableControlTypes)
        {
            Menu.RemoveMenuItem($"{k_MenuPrefix}/{control.GetMenuPath()}");
        }

        foreach (var basePath in LibraryContent.k_BaseLibraryPaths)
        {
            if (HasControlsUnderBasePath(basePath))
            {
                var menuPath = $"{k_MenuPrefix}/{ControlTypeInfo.k_ContextMenuPrefix}/{basePath}/UI Library...";
                Menu.RemoveMenuItem(menuPath);
            }
        }
    }

    static bool HasControlsUnderBasePath(string basePath)
    {
        foreach (var control in s_AvailableControlTypes)
        {
            if (!string.IsNullOrEmpty(control.libraryPath) && control.libraryPath.StartsWith(basePath))
            {
                return true;
            }
        }
        return false;
    }

    static List<ControlTypeInfo> GetControlElements()
    {
        var elements = ListPool<(LibraryTypeKey libraryType, string libraryPath)>.Get();
        var libraryTypeKeys = LibraryContent.GetAllLibraryTypes();

        foreach (var (key, item) in libraryTypeKeys)
        {
            if (string.IsNullOrEmpty(item.libraryPath))
                continue;

            elements.Add((key, item.libraryPath));
        }

        // Sort by type name and assign priorities
        elements.Sort(CompareControlElements);

        var result = new List<ControlTypeInfo>();
        var priority = k_DefaultPriority;
        for (var i = 0; i < elements.Count; i++)
        {
            // Niche request: Only want the top container type items to have a separator
            if (i > 0 && LibraryContent.IsContainer(elements[i - 1].libraryType.type.Name) && !LibraryContent.IsContainer(elements[i].libraryType.type.Name))
            {
                // Gap > 10 creates separator
                priority += 11;
            }

            result.Add(new ControlTypeInfo(elements[i].libraryType, elements[i].libraryPath, priority: priority++));
        }

        ListPool<(LibraryTypeKey type, string libraryPath)>.Release(elements);
        return result;
    }

    /// <summary>
    /// Compares two control elements for sorting.
    /// Ungrouped items appear first, then grouped items, alphabetically within each level.
    /// </summary>
    internal static int CompareControlElements(
        (LibraryTypeKey libraryType, string libraryPath) a,
        (LibraryTypeKey libraryType, string libraryPath) b)
    {
        // Containers come first
        var aIsContainer = LibraryContent.IsContainer(a.libraryType.type.Name);
        var bIsContainer = LibraryContent.IsContainer(b.libraryType.type.Name);

        if (aIsContainer != bIsContainer)
            return bIsContainer.CompareTo(aIsContainer);

        // Sort by name within containers
        if (aIsContainer && bIsContainer)
        {
            var aIsVisualElement = a.libraryType.type.Name == nameof(VisualElement);
            var bIsVisualElement = b.libraryType.type.Name == nameof(VisualElement);

            // We want VisualElement to come first
            if (aIsVisualElement != bIsVisualElement)
                return bIsVisualElement.CompareTo(aIsVisualElement);

            return string.Compare(a.libraryType.name, b.libraryType.name, StringComparison.Ordinal);
        }

        // Check if items have subcategory (path contains more than one '/')
        var aHasCategory = a.libraryPath.Split('/').Length > 2;
        var bHasCategory = b.libraryPath.Split('/').Length > 2;

        // Ungrouped items come first
        if (aHasCategory != bHasCategory)
            return aHasCategory.CompareTo(bHasCategory);

        // Within same group level, sort by path then name
        var pathCompare = string.Compare(a.libraryPath, b.libraryPath, StringComparison.Ordinal);
        if (pathCompare != 0)
            return pathCompare;

        return string.Compare(a.libraryType.name, b.libraryType.name, StringComparison.Ordinal);
    }
}
