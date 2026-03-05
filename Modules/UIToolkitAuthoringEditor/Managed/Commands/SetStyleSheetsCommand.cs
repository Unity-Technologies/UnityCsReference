// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal readonly record struct SetStyleSheetsCommand
{
    const string CommandUndoName = "Set Stylesheets";

    readonly VisualTreeAsset VisualTreeAsset;
    readonly IReadOnlyList<StyleSheet> StyleSheets;

    public SetStyleSheetsCommand(VisualTreeAsset visualTreeAsset, IReadOnlyList<StyleSheet> styleSheets)
    {
        VisualTreeAsset = visualTreeAsset;
        StyleSheets = styleSheets;
    }

    public bool Execute()
    {
        Assert.IsNotNull(VisualTreeAsset);
        Assert.IsNotNull(StyleSheets);

        // Check for cyclic dependencies before applying changes
        if (HasCyclicDependencies(StyleSheets, out var cyclicPath))
        {
            Debug.LogError($"Cannot set stylesheets: Cyclic dependency detected in import chain: {cyclicPath}");
            return false;
        }

        var visualTree = VisualTreeAsset.visualTreeNoAlloc;

        Undo.RegisterCompleteObjectUndo(VisualTreeAsset, CommandUndoName);

        // Clear and repopulate the stylesheets list with the new order
        visualTree.stylesheets.Clear();
        foreach (var styleSheet in StyleSheets)
        {
            visualTree.stylesheets.Add(styleSheet);
            EditorUtility.SetDirty(styleSheet);
        }

        EditorUtility.SetDirty(VisualTreeAsset);
        UIElementsUtility.MarkVisualTreeAssetAsChanged(VisualTreeAsset);
        return true;
    }

    static bool HasCyclicDependencies(IReadOnlyList<StyleSheet> styleSheets, out string cyclicPath)
    {
        cyclicPath = null;

        using var _ = HashSetPool<StyleSheet>.Get(out var visited);
        using var __ = ListPool<StyleSheet>.Get(out var currentPath);

        foreach (var styleSheet in styleSheets)
        {
            if (styleSheet == null)
                continue;

            visited.Clear();
            currentPath.Clear();

            if (HasCyclicDependenciesRecursive(styleSheet, visited, currentPath))
            {
                cyclicPath = BuildCyclicPathString(currentPath);
                return true;
            }
        }

        return false;
    }

    static bool HasCyclicDependenciesRecursive(StyleSheet styleSheet, HashSet<StyleSheet> visited, List<StyleSheet> currentPath)
    {
        if (styleSheet == null)
            return false;

        // If we've seen this stylesheet in the current path, we have a cycle
        if (currentPath.Contains(styleSheet))
        {
            // Add it again to show the cycle closes
            currentPath.Add(styleSheet);
            return true;
        }

        // If we've already fully explored this stylesheet in a previous branch, skip it
        if (visited.Contains(styleSheet))
            return false;

        currentPath.Add(styleSheet);
        visited.Add(styleSheet);

        // Check all imported stylesheets
        if (styleSheet.imports != null)
        {
            foreach (var import in styleSheet.imports)
            {
                if (import.styleSheet == null)
                    continue;

                if (HasCyclicDependenciesRecursive(import.styleSheet, visited, currentPath))
                    return true;
            }
        }

        // Remove from current path when backtracking
        currentPath.RemoveAt(currentPath.Count - 1);
        return false;
    }

    static string BuildCyclicPathString(List<StyleSheet> path)
    {
        using var _ = ListPool<string>.Get(out var names);

        foreach (var sheet in path)
        {
            if (sheet != null)
                names.Add(sheet.name);
        }

        return string.Join(" -> ", names);
    }
}
