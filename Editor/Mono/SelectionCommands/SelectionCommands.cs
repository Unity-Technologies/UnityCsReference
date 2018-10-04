// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.ShortcutManagement;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal static class SelectionCommands
    {
        private const int SelectionMenuPriority = 80;

        [MenuItem("Edit/Deselect All #d", false, SelectionMenuPriority + 1)]
        internal static void DeselectAll()
        {
            InternalEditorUtility.ExecuteCommandOnKeyWindow(EventCommandNames.DeselectAll);
        }

        [MenuItem("Edit/Select Children #c", false, SelectionMenuPriority + 2)]
        internal static void SelectChildren()
        {
            InternalEditorUtility.ExecuteCommandOnKeyWindow(EventCommandNames.SelectChildren);
        }

        [MenuItem("Edit/Select Prefab Root %#r", false, SelectionMenuPriority + 3)]
        internal static void SelectPrefabRoot()
        {
            InternalEditorUtility.ExecuteCommandOnKeyWindow(EventCommandNames.SelectPrefabRoot);
        }

        [MenuItem("Edit/Invert Selection %i", false, SelectionMenuPriority + 4)]
        internal static void InvertSelection()
        {
            InternalEditorUtility.ExecuteCommandOnKeyWindow(EventCommandNames.InvertSelection);
        }
    }
}
