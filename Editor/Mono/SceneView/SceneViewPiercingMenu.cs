// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Object = UnityEngine.Object;
using SelectionType = UnityEditor.RectSelection.SelectionType;

namespace UnityEditor
{
    static class SceneViewPiercingMenu
    {
        const string k_SelectionPiercingMenuCommand = "SelectionPiercingMenuCommand";

        static void ShowSelectionPiercingMenuNormal(SceneView view) => ShowSelectionPiercingMenu(view, false);

        static void ShowSelectionPiercingMenuSubtractive(SceneView view) => ShowSelectionPiercingMenu(view, true);

        [Shortcut("Scene View/Show Selection Piercing Menu", typeof(SceneViewPickingShortcutContext), KeyCode.Mouse1, ShortcutModifiers.Action)]
        static void OpenSelectionPiercingMenuNormal(ShortcutArguments args)
        {
            if (!(args.context is SceneViewPickingShortcutContext ctx) || !(ctx.window is SceneView view))
                return;
            OpenSelectionPiercingMenu(view, SelectionType.Normal);
        }

        [Shortcut("Scene View/Show Selection Piercing Menu Subtractive", typeof(SceneViewPickingShortcutContext), KeyCode.Mouse1, ShortcutModifiers.Action | ShortcutModifiers.Shift)]
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

        static Object[] GetNewSelection(Object[] existing, PickingObject incoming, SelectionType type)
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

        // used by EditModeAndPlayModeTests/Picking
        // ReSharper disable once MemberCanBePrivate.Global
        internal class PiercingContext : IDisposable
        {
            readonly SceneView m_SceneView;
            public readonly List<PickingObject> overlapping;
            readonly Object[] m_ExistingSelection;
            readonly Object[] m_Preview = new Object[1];
            readonly List<GameObject> m_GameObjectPreview;
            bool m_SelectionUpdated;

            public PiercingContext(SceneView view, Vector2 point)
            {
                m_SceneView = view;
                m_GameObjectPreview = new List<GameObject>();
                overlapping = new List<PickingObject>();
                foreach(var o in SceneViewPicking.GetAllOverlapping(point))
                    overlapping.Add(o);
                m_ExistingSelection = Selection.objects;
            }

            List<GameObject> CastGameObjects(IEnumerable<Object> objects)
            {
                m_GameObjectPreview.Clear();
                foreach(var o in objects)
                    if(o is GameObject go)
                        m_GameObjectPreview.Add(go);
                return m_GameObjectPreview;
            }

            public void UpdatePreview(PickingObject target, SelectionType type)
            {
                m_Preview[0] = target.target;
                HandleUtility.FilterInstanceIDs(CastGameObjects(GetNewSelection(m_ExistingSelection, target, type)),
                    out SceneView.s_CachedParentRenderersForOutlining,
                    out SceneView.s_CachedChildRenderersForOutlining);
                m_SceneView.Repaint();
            }

            public void ResetPreview(bool showExistingSelection)
            {
                if (showExistingSelection)
                {
                    HandleUtility.FilterInstanceIDs(CastGameObjects(m_ExistingSelection),
                        out SceneView.s_CachedParentRenderersForOutlining,
                        out SceneView.s_CachedChildRenderersForOutlining);
                }
                else
                {
                    SceneView.s_CachedParentRenderersForOutlining = new int[] { };
                    SceneView.s_CachedChildRenderersForOutlining = new int[] { };
                }
            }

            public void UpdateSelection(PickingObject incoming, SelectionType type)
            {
                Selection.objects = GetNewSelection(m_ExistingSelection, incoming, type);
                m_SelectionUpdated = true;
            }

            public void Dispose()
            {
                if (!m_SelectionUpdated)
                {
                    Selection.objects = m_ExistingSelection;
                    ResetPreview(true);
                }
                else if (Selection.objects.Length < 1)
                {
                    ResetPreview(false);
                }
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

            var context = new PiercingContext(sceneView, Event.current.mousePosition);
            var selectionPiercingMenu = new DropdownMenu();
            var piercingMenuDesc = new DropdownMenuDescriptor();
            piercingMenuDesc.onDetachedFromMenuContainerCallback = () => { context.Dispose(); };
            selectionPiercingMenu.SetDescriptor(piercingMenuDesc);

            foreach (var obj in context.overlapping)
            {
                var item = GenericDropdownMenu.BuildItem(obj.target.name, false, true, false, null, null, "");

                item.RegisterCallback<MouseDownEvent>(x =>
                {
                    // intentionally not passing x.modifiers here because there is no way to update the preview when
                    // modifier keys are pressed while already hovering the menu item. it means that the results from
                    // a click and preview are not guaranteed or even likely to be in sync, which is a pretty terrible
                    // user experience.
                    context.UpdateSelection(obj, GetSelectionType(EventModifiers.None, forceSubtractive));
                });

                // outline preview only works with renderer instance ids
                if(obj.TryGetGameObject(out var gameObject))
                {
                    item.RegisterCallback<MouseEnterEvent>(x => { context.UpdatePreview(obj, GetSelectionType(EventModifiers.None, forceSubtractive)); });
                    item.RegisterCallback<MouseOutEvent>(x => { context.ResetPreview(forceSubtractive); });
                }

                selectionPiercingMenu.AppendContent(obj.target.name, item, DropdownMenuAction.AlwaysEnabled);
            }

            if (selectionPiercingMenu.MenuItems().Count == 0)
            {
                selectionPiercingMenu.AppendAction("Nothing to Select Under Pointer", null, DropdownMenuAction.Status.Disabled);
            }
            else
            {
                // selection changes will flush the preview outline, which we want to avoid if ultimately the current
                // selection is sticking around.
                if(!forceSubtractive)
                    Selection.objects = new Object[] { };

                context.ResetPreview(forceSubtractive);
            }

            selectionPiercingMenu.DisplayEditorMenu(new Rect(Event.current.mousePosition, Vector2.zero));
        }
    }
}
