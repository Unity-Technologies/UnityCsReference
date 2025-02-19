// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using SelectionType = UnityEditor.RectSelection.SelectionType;

namespace UnityEditor
{
    static class SceneViewPiercingMenu
    {
        const string k_SelectionPiercingMenuCommand = "SelectionPiercingMenuCommand";

        static void ShowSelectionPiercingMenuNormal(SceneView view) => ShowSelectionPiercingMenu(view, false);

        static void ShowSelectionPiercingMenuSubtractive(SceneView view) => ShowSelectionPiercingMenu(view, true);

        [Shortcut("Scene View/List Select", typeof(SceneViewPickingShortcutContext), KeyCode.Mouse1, ShortcutModifiers.Action)]
        static void OpenSelectionPiercingMenuNormal(ShortcutArguments args)
        {
            if (!(args.context is SceneViewPickingShortcutContext ctx) || !(ctx.window is SceneView view))
                return;
            OpenSelectionPiercingMenu(view, SelectionType.Normal);
        }

        [Shortcut("Scene View/List Deselect", typeof(SceneViewPickingShortcutContext), KeyCode.Mouse1, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
        static void OpenSelectionPiercingMenuSubtractive(ShortcutArguments args)
        {
            if (!(args.context is SceneViewPickingShortcutContext ctx) || !(ctx.window is SceneView view))
                return;
            OpenSelectionPiercingMenu(view, SelectionType.Subtractive);
        }

        static void OpenSelectionPiercingMenu(SceneView view, SelectionType type)
        {
            SceneView.duringSceneGui += type == SelectionType.Normal ? ShowSelectionPiercingMenuNormal : ShowSelectionPiercingMenuSubtractive;
            var evt = Event.current;

            view.SendEvent(new Event()
            {
                commandName = k_SelectionPiercingMenuCommand,
                type = EventType.ValidateCommand,
                mousePosition = evt.mousePosition
            });

            view.SendEvent(new Event()
            {
                commandName = k_SelectionPiercingMenuCommand,
                type = EventType.ExecuteCommand,
                mousePosition = evt.mousePosition
            });

            SceneView.duringSceneGui -= type == SelectionType.Normal ? ShowSelectionPiercingMenuNormal : ShowSelectionPiercingMenuSubtractive;
        }

        // unfortunately there is no easy way to respect shortcut mappings in this case. we'll assume that action/shift
        // are safe bets.
        static SelectionType GetSelectionType(EventModifiers modifiers, bool forceSubtractive)
        {
            if (forceSubtractive)
                return SelectionType.Subtractive;

            if ((modifiers & EventModifiers.Shift) > 0)
                return SelectionType.Additive;

            if((modifiers & (EventModifiers.Command | EventModifiers.Control)) > 0)
                return SelectionType.Subtractive;

            return SelectionType.Normal;
        }

        //Used by tests in EditAndPlaymodeTests/Picking
        internal static Object[] GetNewSelection(Object[] existing, PickingObject incoming, SelectionType type)
        {
            Object[] newSelection;

            switch (type)
            {
                case SelectionType.Additive:
                    if (incoming.target == null)
                        return existing;

                    newSelection = new Object[existing.Length + 1];
                    Array.Copy(existing, newSelection, existing.Length);
                    newSelection[existing.Length] = incoming.target;
                    return newSelection;

                // if target is in selection as a child, remove the parent. otherwise this is equivalent to
                // HashSet.SymmetricExceptWith
                case SelectionType.Subtractive:
                    if (!incoming.TryGetComponent<Transform>(out var hoveredTransform))
                        goto case SelectionType.Additive;

                    var set = new HashSet<Object>(existing);
                    var selectionBase = HandleUtility.FindSelectionBaseForPicking(hoveredTransform)?.transform;
                    bool incomingPrefabRemovedFromSelection = false;

                    do
                    {
                        incomingPrefabRemovedFromSelection |= set.Remove(hoveredTransform.gameObject);
                        hoveredTransform = hoveredTransform.parent;
                    } while (hoveredTransform != null && hoveredTransform != selectionBase);

                    if (!incomingPrefabRemovedFromSelection)
                        goto case SelectionType.Additive;

                    newSelection = new Object[set.Count];
                    int i = 0;
                    foreach (var o in set)
                        newSelection[i++] = o;
                    return newSelection;

                case SelectionType.Normal:
                default:
                    return new [] { incoming.target };
            }
        }

        static void ShowSelectionPiercingMenu(SceneView sceneView, bool forceSubtractive)
        {
            var evt = Event.current;

            if (evt.type == EventType.ValidateCommand && evt.commandName == k_SelectionPiercingMenuCommand)
                evt.Use();

            if(evt.type != EventType.ExecuteCommand || evt.commandName != k_SelectionPiercingMenuCommand)
                return;

            evt.Use();

            var overlapping = new List<PickingObject>();
            foreach(var o in SceneViewPicking.GetAllOverlapping(evt.mousePosition))
                overlapping.Add(o);

            var selectionPiercingMenu = new DropdownMenu();
            foreach (var obj in overlapping)
            {
                var showInSelection = forceSubtractive && Selection.Contains(obj.target);
                selectionPiercingMenu.AppendAction(obj.target.name,
                    _ => Selection.objects = GetNewSelection(Selection.objects, obj, GetSelectionType(EventModifiers.None, forceSubtractive)),
                    _ => showInSelection ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }

            if (selectionPiercingMenu.MenuItems().Count == 0)
                selectionPiercingMenu.AppendAction("Nothing to Select Under Pointer", null, DropdownMenuAction.Status.Disabled);

#pragma warning disable CS0618 // Type or member is obsolete
            selectionPiercingMenu.DoDisplayEditorMenuFromImGUI(new Rect(Event.current.mousePosition, Vector2.zero));
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
