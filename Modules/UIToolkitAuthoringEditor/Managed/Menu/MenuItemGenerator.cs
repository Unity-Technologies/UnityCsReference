// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using UnityEditor;
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

internal static class MenuItemGenerator
{
    static List<ControlTypeInfo> s_AvailableControlTypes;
    static int s_HighestItemPriority;

    // The current value represents the first item of the "GameObject/UI Toolkit" menu item. Update this value to change the position within the menu item.
    internal const int k_DefaultStandardElementsPriority = 6;
    internal const int k_DefaultProjectElementsPriority = 7;
    internal const int k_LibraryMenuItemPriorityOffset = 20;
    internal const string k_MenuPrefix = "GameObject";
    internal static readonly string k_GameObjectMenuPath = $"{k_MenuPrefix}/{ControlTypeInfo.k_ContextMenuPrefix}";

    const string k_StandardElementsPath = "Standard Elements";
    const string k_ProjectElementsPath = "Project Elements";

    /// <summary>
    /// Unity core controls to display in "Standard Elements".
    /// Only these controls will appear (unless they have subcategories defined in s_Categories).
    /// </summary>
    static readonly HashSet<string> s_StandardElementControls = new(new[]
    {
        nameof(VisualElement),
        nameof(ScrollView),
        nameof(Image),
        nameof(Label),
        nameof(Button),
        nameof(Toggle),
        nameof(DropdownField),
        nameof(TextField),
        nameof(Slider),
        nameof(IntegerField),
        nameof(FloatField),
        nameof(DoubleField),
        nameof(LongField),
    });

    internal static readonly string[] k_BaseLibraryPaths = new[]
    {
        k_StandardElementsPath,
        k_ProjectElementsPath
    };

    static readonly Dictionary<string, HashSet<string>> s_CategoriesByType = new()
    {
        ["Numeric Fields"] = [
            nameof(IntegerField),
            nameof(FloatField),
            nameof(LongField),
            nameof(DoubleField)
        ]
    };

    [InitializeOnLoadMethod]
    static void Initialize()
    {
        // Defer to the first idle tick so UxmlSerializedDataRegistry (which feeds GetControlElements) is fully populated.
        EditorApplication.delayCall += RegisterMenuItems;
        AssemblyReloadEvents.beforeAssemblyReload += UnregisterMenuItems;
    }

    public static List<ControlTypeInfo> GetAvailableControlTypes()
    {
        return s_AvailableControlTypes ??= GetControlElements();
    }

    public static bool TryGetTypeForMenuPath(string fullMenuPath, out Type type)
    {
        foreach (var control in GetAvailableControlTypes())
        {
            if ($"{k_MenuPrefix}/{control.GetMenuPath()}" == fullMenuPath)
            {
                type = control.libraryType.type;
                return true;
            }
        }
        type = null;
        return false;
    }

    public static void RegisterMenuItems()
    {
        var availableControlTypes = GetAvailableControlTypes();
        foreach (var control in availableControlTypes)
        {
            var elementType = control.libraryType.type;
            var menuPath = $"{k_MenuPrefix}/{control.GetMenuPath()}";
            Menu.AddMenuItem(
                menuPath,
                "",
                false,
                control.priority,
                () =>
                {
                    var fromHierarchyContextMenu = Menu.HasContext(menuPath);
                    if (fromHierarchyContextMenu)
                        MenuUtility.AddElementAsLastChild(elementType);
                    else
                        MenuUtility.AddElementAsSibling(elementType);
                },
                null
            );
        }

        // Add "Project Elements" menu item and disable it if there's no items
        if (!HasControlsUnderBasePath(k_ProjectElementsPath))
        {
            Menu.AddMenuItem(
                $"{k_GameObjectMenuPath}/{k_ProjectElementsPath}",
                "",
                false,
                k_DefaultProjectElementsPriority,
                () => { },
                () => false
            );
        }

        foreach (var basePath in k_BaseLibraryPaths)
        {
            if (HasControlsUnderBasePath(basePath))
            {
                var menuPath = $"{k_GameObjectMenuPath}/{basePath}/UI Library...";
                Menu.AddMenuItem(
                    menuPath,
                    "",
                    false,
                    s_HighestItemPriority + k_LibraryMenuItemPriorityOffset,
                    UIElementsProvider.OpenUIElementsPicker,
                    null
                );
            }
        }

        // Add separator after the "Project Elements" subgroup
        Menu.AddSeparator($"{k_GameObjectMenuPath}/", k_DefaultProjectElementsPriority);
    }

    public static void UnregisterMenuItems()
    {
        if (s_AvailableControlTypes == null)
            return;

        foreach (var control in s_AvailableControlTypes)
        {
            Menu.RemoveMenuItem($"{k_MenuPrefix}/{control.GetMenuPath()}");
        }

        // Remove the Disabled "Project Elements" item
        if (!HasControlsUnderBasePath(k_ProjectElementsPath))
        {
            Menu.RemoveMenuItem($"{k_GameObjectMenuPath}/{k_ProjectElementsPath}");
        }

        foreach (var basePath in k_BaseLibraryPaths)
        {
            if (HasControlsUnderBasePath(basePath))
            {
                var menuPath = $"{k_GameObjectMenuPath}/{basePath}/UI Library...";
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
        s_HighestItemPriority = k_DefaultStandardElementsPriority;

        var standardElements = ListPool<(LibraryTypeKey libraryType, string libraryPath)>.Get();
        var projectElements = ListPool<(LibraryTypeKey libraryType, string libraryPath)>.Get();
        var libraryTypeKeys = LibraryContent.GetAllLibraryTypes();

        foreach (var (key, item) in libraryTypeKeys)
        {
            var menuPath = GetContextMenuPath(key, item.libraryPath);
            if (string.IsNullOrEmpty(menuPath))
                continue;

            if (menuPath.StartsWith(k_BaseLibraryPaths[0]))
                standardElements.Add((key, menuPath));
            else if (menuPath.StartsWith(k_BaseLibraryPaths[1]))
                projectElements.Add((key, menuPath));
        }

        standardElements.Sort(CompareControlElements);
        projectElements.Sort(CompareControlElements);

        var result = new List<ControlTypeInfo>();

        AddControlTypesWithPriority(result, standardElements, k_DefaultStandardElementsPriority);
        AddControlTypesWithPriority(result, projectElements, k_DefaultProjectElementsPriority);

        ListPool<(LibraryTypeKey type, string libraryPath)>.Release(standardElements);
        ListPool<(LibraryTypeKey type, string libraryPath)>.Release(projectElements);
        return result;
    }

    static void AddControlTypesWithPriority(List<ControlTypeInfo> result, List<(LibraryTypeKey libraryType, string libraryPath)> elements, int priority)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            result.Add(new ControlTypeInfo(elements[i].libraryType, elements[i].libraryPath, priority));
            priority++;

            // Niche request: Only want the top container type items to have a separator
            if (IsContainer(elements[i].libraryType.type.Name) && i + 1 < elements.Count && !IsContainer(elements[i + 1].libraryType.type.Name))
            {
                priority += 10;
            }
        }

        s_HighestItemPriority = Math.Max(s_HighestItemPriority, priority);
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
        var aIsContainer = IsContainer(a.libraryType.type.Name);
        var bIsContainer = IsContainer(b.libraryType.type.Name);

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

    static string GetCategoryForType(string typeName)
    {
        foreach (var (category, types) in s_CategoriesByType)
        {
            if (types.Contains(typeName))
                return category;
        }

        return null;
    }

    static string GetContextMenuPath(LibraryTypeKey key, string libraryPath)
    {
        if (key.type.Namespace == "UnityEngine.UIElements")
        {
            // Only whitelist specific controls for context menu
            if (!s_StandardElementControls.Contains(key.type.Name))
                return null;

            var category = GetCategoryForType(key.type.Name);
            return category != null ? $"{k_StandardElementsPath}/{category}" : k_StandardElementsPath;
        }

        // Empty libraryPath will be placed at the root
        if (libraryPath != null)
        {
            return string.IsNullOrEmpty(libraryPath) ? k_ProjectElementsPath : $"{k_ProjectElementsPath}/{libraryPath}";
        }

        return null;
    }

    /// <summary>
    /// Checks if a control is a container type that should appear at the top.
    /// </summary>
    static bool IsContainer(string typeName)
    {
        return typeName == nameof(VisualElement) || typeName == nameof(ScrollView);
    }
}
