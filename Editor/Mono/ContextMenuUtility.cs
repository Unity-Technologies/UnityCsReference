// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEditor.Snap;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Actions
{
    public static class ContextMenuUtility
    {
        static void AddAction(DropdownMenu menu, string path, Action action, bool active = true)
        {
            menu.AppendAction(path, _ => action?.Invoke(),
                _ => active ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        public static void AddMenuItem(DropdownMenu menu, string menuItemPath, string contextMenuPath = "")
        {
            AddMenuItemWithContext(menu, Selection.objects, menuItemPath, contextMenuPath);
        }

        public static void AddMenuItemWithContext(DropdownMenu menu, IEnumerable<Object> context, string menuItemPath, string contextMenuPath = "")
        {
            var contextArray = ToArray(context);
            var enabled = Menu.GetEnabledWithContext(menuItemPath, contextArray);
            AddMenuItemWithContext(menu, context, enabled, menuItemPath, contextMenuPath);
        }

        internal static void AddMenuItemWithContext(DropdownMenu menu, IEnumerable<Object> context, bool enabled, string menuItemPath, string contextMenuPath = "")
        {
            var contextArray = ToArray(context);
            string shortcut = Menu.GetHotkey(menuItemPath);
            string path = (string.IsNullOrEmpty(contextMenuPath) ? menuItemPath : contextMenuPath)
                + (string.IsNullOrEmpty(shortcut) ? "" : " " + shortcut);

            AddAction(menu, path,
                () => { EditorApplication.delayCall += () => ExecuteMenuItem(contextArray, menuItemPath); }, enabled);
        }

        static void ExecuteMenuItem(Object[] context, string menuItemPath)
        {
            // Claudia Antoun, 05-16-23, can safely be removed when UW-153 is fixed.
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.Focus();

            EditorApplication.ExecuteMenuItemWithTemporaryContext(menuItemPath, context);
        }

        public static void AddMenuItemsForType<T>(DropdownMenu menu, IEnumerable<T> targets) where T : Object
        {
            AddMenuItemsForType(menu, typeof(T), targets);
        }

        public static void AddMenuItemsForType(DropdownMenu menu, Type type, IEnumerable<Object> targets, string submenu = "")
        {
            var componentName = type.Name;
            Menu.UpdateContextMenu(ToArray(targets), 0);
            AddMenuItems(menu, componentName, Menu.GetMenuItems($"CONTEXT/{componentName}/", false, true), targets, submenu);
        }

        static void AddMenuItems(DropdownMenu menu, string componentName, ScriptingMenuItem[] items, IEnumerable<Object> targets, string submenu)
        {
            string context = $"CONTEXT/{componentName}/";
            if (!string.IsNullOrEmpty(submenu) && submenu[^1] != '/')
                submenu += '/';

            int lastItemPriority = -1;
            foreach (var menuItem in items)
            {
                var menuPath = menuItem.path;
                var newPath = $"{submenu}{menuPath.Substring(context.Length)}";

                // Add separator between items with priority differing by over 10 points
                if (lastItemPriority != -1 && menuItem.priority > lastItemPriority + 10)
                    menu.AppendSeparator(newPath.Substring(0, newPath.LastIndexOf('/') + 1));
                lastItemPriority = menuItem.priority;

                if (!menuItem.isSeparator)
                    AddMenuItemWithContext(menu, targets, menuPath, newPath);
            }
        }

        static void AddGameObjectClipboardEntriesTo(DropdownMenu menu)
        {
            var isThereGameObjectInSelection = Selection.gameObjects.Length > 0;
            var isPasteEnabled = CutBoard.CanGameObjectsBePasted() || Unsupported.CanPasteGameObjectsFromPasteboard();
            AddClipboardEntriesTo(menu, isThereGameObjectInSelection, isThereGameObjectInSelection, isPasteEnabled, isThereGameObjectInSelection, isThereGameObjectInSelection);
        }

        public static void AddClipboardEntriesTo(DropdownMenu menu)
        {
            var isThereGameObjectInSelection = Selection.gameObjects.Length > 0;
            var isPasteEnabled = GUIUtility.systemCopyBuffer.Length > 0;
            AddClipboardEntriesTo(menu, isThereGameObjectInSelection, isThereGameObjectInSelection, isPasteEnabled, isThereGameObjectInSelection, isThereGameObjectInSelection);
        }

        public static void AddClipboardEntriesTo(DropdownMenu menu, bool cutEnabled, bool copyEnabled, bool pasteEnabled, bool duplicateEnabled, bool deleteEnabled)
        {
            AddMenuItemWithContext(menu, null, cutEnabled, "Edit/Cut", "Cut");
            AddMenuItemWithContext(menu, null, copyEnabled, "Edit/Copy", "Copy");
            AddMenuItemWithContext(menu, null, pasteEnabled, "Edit/Paste", "Paste");
            AddMenuItemWithContext(menu, null, duplicateEnabled, "Edit/Duplicate", "Duplicate");
            AddMenuItemWithContext(menu, null, deleteEnabled, "Edit/Delete", "Delete");
        }

        public static void AddComponentEntriesTo(DropdownMenu menu)
        {
            var editors = ActiveEditorTracker.sharedTracker.activeEditors;
            foreach (var editor in editors)
            {
                var type = editor.target.GetType();
                if (type == typeof(GameObject) || type == typeof(Material))
                    continue;

                Menu.UpdateContextMenu(editor.targets, 0);

                var items = Menu.GetMenuItems($"CONTEXT/{type.Name}/", false, true);
                if (items.Length == 0)
                    continue;

                AddMenuItems(menu, type.Name, items, editor.targets, type.Name);
            }
        }

        public static void AddGameObjectEntriesTo(DropdownMenu menu)
        {
            bool hasSelectedGO = Selection.gameObjects.Length > 0;
            bool hasSelectedExactlyOneGO = Selection.gameObjects.Length == 1;
            AddGameObjectClipboardEntriesTo(menu);

            menu.AppendSeparator();

            AddMenuItemWithContext(menu, null, "GameObject/Move To View", "Move to View");
            AddMenuItemWithContext(menu, null, "GameObject/Align With View", "Align with View");
            AddAction(menu, "Move to Grid Position", Shortcuts.PushToGrid, hasSelectedGO);

            menu.AppendSeparator();

            AddAction(menu,
                SceneVisibilityState.isolation ? "Exit Isolation" : "Isolate",
                SceneVisibilityManager.ToggleIsolateSelectionShortcut,
                SceneVisibilityState.isolation || hasSelectedGO);

            menu.AppendSeparator();

            AddMenuItemWithContext(menu, null, "Component/Add...", "Add Component...");
            AddMenuItemWithContext(menu, null, "Assets/Properties...", "Properties...");

            // Prefab menu items that only make sense if a single object is selected.
            var listOfSceneHierarchyWindows = SceneHierarchyWindow.GetAllSceneHierarchyWindows();
            if (hasSelectedExactlyOneGO && listOfSceneHierarchyWindows.Count > 0)
                listOfSceneHierarchyWindows[0].sceneHierarchy?.PopulateDropdownMenuWithPrefabMenuItems(menu);

            // Component entries
            if (hasSelectedGO)
            {
                menu.AppendSeparator();
                AddComponentEntriesTo(menu);
            }
        }

        internal static DropdownMenu CreateActionMenu()
        {
            var contextMenu = new DropdownMenu();
            EditorToolManager.activeToolContext.PopulateMenu(contextMenu);
            AddMenuItemsForType(contextMenu, ToolManager.activeContextType, EditorToolManager.activeToolContext.targets);
            EditorToolManager.activeTool.PopulateMenu(contextMenu);
            AddMenuItemsForType(contextMenu, ToolManager.activeToolType, EditorToolManager.activeTool.targets);

            return contextMenu;
        }

        internal static void ShowActionMenu()
        {
            if (Event.current == null)
                return;

            var dropdownMenu = CreateActionMenu();
            if (dropdownMenu.MenuItems().Count == 0)
                AddAction(dropdownMenu, "No Actions for this Context", null, false);

            dropdownMenu.DoDisplayEditorMenu(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        static T[] ToArray<T>(IEnumerable<T> enumerable) where T : Object
        {
            if (enumerable == null)
                return null;

            if (enumerable is T[] arr)
                return arr;

            var size = 0;
            foreach (var item in enumerable)
                size++;

            T[] items = new T[size];
            var index = 0;
            foreach (var item in enumerable)
            {
                items[index] = item;
                index++;
            }

            return items;
        }
    }
}
