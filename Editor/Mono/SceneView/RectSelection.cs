// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Handles picking/selection in the scene view (both "click" type and "drag-rect" type)
    class RectSelection
    {
        public enum SelectionType { Normal, Additive, Subtractive }

        Vector2 m_SelectMousePoint;
        Vector2 m_StartPoint;

        SelectionType m_CurrentSelectionType;

        Object[] m_SelectionStart = null;
        Object[] m_CurrentSelection = null;

        Dictionary<GameObject, bool> m_LastSelection;

        readonly SceneViewRectSelection m_RectSelectionShortcutContext = new SceneViewRectSelection();

        public static event Action rectSelectionStarting = delegate { };
        public static event Action rectSelectionFinished = delegate { };

        bool m_IsNearestControl = false;

        const string k_PickingEventCommandName = "SceneViewPickingEventCommand";
        const string k_SetRectSelectionHotControlEventCommandName = "SetRectSelectionHotControlEventCommand";

        const string k_RectSelectionNormal = "Scene View/Rect Selection Normal";
        const string k_RectSelectionAdditive = "Scene View/Rect Selection Additive";
        const string k_RectSelectionSubtractive = "Scene View/Rect Selection Subtractive";
        const string k_PickingNormal = "Scene View/Picking Normal";
        const string k_PickingAdditive = "Scene View/Picking Additive";
        const string k_PickingSubtractive = "Scene View/Picking Subtractive";

        readonly int k_RectSelectionID = GUIUtility.GetPermanentControlID();

        public void RegisterShortcutContext()
        {
            ShortcutIntegration.instance.contextManager.RegisterToolContext(m_RectSelectionShortcutContext);
        }

        public void UnregisterShortcutContext()
        {
            ShortcutIntegration.instance.contextManager.DeregisterToolContext(m_RectSelectionShortcutContext);
        }

        class SceneViewRectSelection : IShortcutToolContext
        {
            public SceneView window => EditorWindow.focusedWindow as SceneView;

            public bool active => IsActive;

            public static bool IsActive
            {
                get
                {
                    if (!(EditorWindow.focusedWindow is SceneView view) || view.sceneViewMotion == null)
                        return false;

                    return view.sceneViewMotion.viewportsUnderMouse && Tools.current != Tool.View;
                }
            }
        }

        [ClutchShortcut(k_RectSelectionNormal, typeof(SceneViewRectSelection), KeyCode.Mouse0)]
        static void OnNormalRectSelection(ShortcutArguments args)
        {
            if (args.context is SceneViewRectSelection ctx && ctx.window != null && ctx.window.rectSelection != null)
                ctx.window.rectSelection.OnRectSelection(args, SelectionType.Normal, ctx.window);
        }

        [ClutchShortcut(k_RectSelectionAdditive, typeof(SceneViewRectSelection), KeyCode.Mouse0, ShortcutModifiers.Shift)]
        static void OnAdditiveRectSelection(ShortcutArguments args)
        {
            if (args.context is SceneViewRectSelection ctx && ctx.window != null && ctx.window.rectSelection != null)
                ctx.window.rectSelection.OnRectSelection(args, SelectionType.Additive, ctx.window);
        }

        [ClutchShortcut(k_RectSelectionSubtractive, typeof(SceneViewRectSelection), KeyCode.Mouse0, ShortcutModifiers.Action)]
        static void OnSubtractiveRectSelection(ShortcutArguments args)
        {
            if (args.context is SceneViewRectSelection ctx && ctx.window != null && ctx.window.rectSelection != null)
                ctx.window.rectSelection.OnRectSelection(args, SelectionType.Subtractive, ctx.window);
        }

        void OnRectSelection(ShortcutArguments args, SelectionType selectionType, SceneView view)
        {
            // Validating that the hotControl is either equal to 0 or k_RectSelectionID allows to only start the rect selection
            // when no other tool overrides the shortcut key and to change modifiers while rect selecting.
            if (args.stage == ShortcutStage.Begin && (GUIUtility.hotControl == 0 || GUIUtility.hotControl == k_RectSelectionID))
            {
                m_CurrentSelectionType = selectionType;
                StartRectSelection(view);
            }
            else if (args.stage == ShortcutStage.End)
            {
                CompleteRectSelection();
            }
        }

        [Shortcut(k_PickingNormal, typeof(SceneViewRectSelection), KeyCode.Mouse0)]
        static void OnNormalPicking(ShortcutArguments args)
        {
            if (args.context is SceneViewRectSelection ctx && ctx.window != null && ctx.window.rectSelection != null)
                ctx.window.rectSelection.DelayPicking(ctx.window, SelectionType.Normal);
        }

        [Shortcut(k_PickingAdditive, typeof(SceneViewRectSelection), KeyCode.Mouse0, ShortcutModifiers.Shift)]
        static void OnAdditivePicking(ShortcutArguments args)
        {
            if (args.context is SceneViewRectSelection ctx && ctx.window != null && ctx.window.rectSelection != null)
                ctx.window.rectSelection.DelayPicking(ctx.window, SelectionType.Additive);
        }

        [Shortcut(k_PickingSubtractive, typeof(SceneViewRectSelection), KeyCode.Mouse0, ShortcutModifiers.Action)]
        static void OnSubtractivePicking(ShortcutArguments args)
        {
            if (args.context is SceneViewRectSelection ctx && ctx.window != null && ctx.window.rectSelection != null)
                ctx.window.rectSelection.DelayPicking(ctx.window, SelectionType.Subtractive);
        }

        // Delaying the picking to a command event is necessary because some HandleUtility methods
        // need to be called in an OnGUI.
        void DelayPicking(SceneView sceneview, SelectionType selectionType)
        {
            if (sceneview == null)
                return;

            m_CurrentSelectionType = selectionType;
            sceneview.SendEvent(EditorGUIUtility.CommandEvent(k_PickingEventCommandName));
        }

        void Pick(SelectionType selectionType, Vector2 mousePos, Event evt)
        {
            if (selectionType == SelectionType.Subtractive || selectionType == SelectionType.Additive)
            {
                // For shift, we check if EXACTLY the active GO is hovered by mouse and then subtract. Otherwise additive.
                // For control/cmd, we check if ANY of the selected GO is hovered by mouse and then subtract. Otherwise additive.
                // Control/cmd takes priority over shift.
                var hovered = HandleUtility.PickObject(mousePos, false);

                var handledIt = false;
                // shift-click deselects only if the active GO is exactly what we clicked on
                if (selectionType != SelectionType.Subtractive && Selection.activeObject == hovered.target)
                {
                    UpdateSelection(m_SelectionStart, hovered.target, SelectionType.Subtractive, false);
                    handledIt = true;
                }

                // ctrl-click deselects everything up to prefab root, that is already selected
                if (!handledIt && selectionType == SelectionType.Subtractive)
                {
                    var selectedObjects = Selection.objects;
                    hovered.TryGetComponent<Transform>(out var hoveredTransform);
                    var hoveredRoot = HandleUtility.FindSelectionBaseForPicking(hoveredTransform);
                    var deselectList = new List<Object>();

                    while (hovered.target != null)
                    {
                        foreach (var obj in selectedObjects)
                        {
                            if (obj.Equals(hovered.target))
                            {
                                deselectList.Add(hovered.target);
                                break;
                            }
                        }

                        if (hovered.target == hoveredRoot)
                            break;

                        if (!hovered.TryGetParent(out var parent))
                            break;

                        hovered = new PickingObject(parent.gameObject);
                    }

                    if (deselectList.Count > 0)
                    {
                        UpdateSelection(m_SelectionStart, deselectList.ToArray(), SelectionType.Subtractive, false);
                        handledIt = true;
                    }
                }

                // we did not deselect anything, so add the new thing into selection instead
                if (!handledIt)
                {
                    var picked = HandleUtility.PickObject(mousePos, true);
                    UpdateSelection(m_SelectionStart, picked.target, SelectionType.Additive, false);
                }
            }
            else // With no modifier keys, we do the "cycle through overlapped" picking logic in SceneViewPicking.cs
            {
                var picked = SceneViewPicking.PickGameObject(mousePos);
                UpdateSelection(m_SelectionStart, picked.target, selectionType, false);
            }

            evt.Use();
        }

        public void OnGUI()
        {
            Event evt = Event.current;

            Handles.BeginGUI();

            switch (evt.GetTypeForControl(k_RectSelectionID))
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    if (!Tools.viewToolActive)
                        HandleUtility.AddDefaultControl(k_RectSelectionID);
                    break;
                case EventType.MouseDown:
                    HandleOnMouseDown(evt);
                    break;
                case EventType.MouseUp:
                    HandleOnMouseUp();
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == k_RectSelectionID && m_IsNearestControl)
                    {
                        m_SelectMousePoint = evt.mousePosition;
                        GameObject[] rectObjs = HandleUtility.PickRectObjects(EditorGUIExt.FromToRect(m_StartPoint, m_SelectMousePoint));
                        m_CurrentSelection = rectObjs;
                        bool setIt = false;

                        if (m_LastSelection == null)
                        {
                            m_LastSelection = new Dictionary<GameObject, bool>();
                            setIt = true;
                        }

                        setIt |= m_LastSelection.Count != rectObjs.Length;

                        if (!setIt)
                        {
                            Dictionary<GameObject, bool> set = new Dictionary<GameObject, bool>(rectObjs.Length);
                            foreach (GameObject g in rectObjs)
                                set.Add(g, false);

                            foreach (GameObject g in m_LastSelection.Keys)
                            {
                                if (!set.ContainsKey(g))
                                {
                                    setIt = true;
                                    break;
                                }
                            }
                        }

                        if (setIt)
                        {
                            m_LastSelection = new Dictionary<GameObject, bool>(rectObjs.Length);

                            foreach (GameObject g in rectObjs)
                                m_LastSelection.Add(g, false);

                            UpdateSelection(m_SelectionStart, rectObjs, m_CurrentSelectionType, true);
                        }

                        evt.Use();
                    }
                    break;
                case EventType.KeyDown: // Escape
                    if (evt.keyCode == KeyCode.Escape && GUIUtility.hotControl == k_RectSelectionID)
                    {
                        CompleteRectSelection();

                        GUIUtility.hotControl = 0;

                        // Set the current selection to the previous selection.
                        Selection.objects = m_SelectionStart;

                        HandleOnMouseUp();
                    }
                    break;
                case EventType.Repaint:
                    if (GUIUtility.hotControl == k_RectSelectionID && m_IsNearestControl && m_StartPoint != m_SelectMousePoint)
                    {
                        EditorStyles.selectionRect.Draw(EditorGUIExt.FromToRect(m_StartPoint, m_SelectMousePoint),
                            GUIContent.none, false, false, false, false);
                    }
                    break;
                case EventType.ExecuteCommand:
                    if (evt.commandName == k_PickingEventCommandName && m_IsNearestControl)
                    {
                        Pick(m_CurrentSelectionType, m_StartPoint, evt);
                    }
                    else if (evt.commandName == k_SetRectSelectionHotControlEventCommandName)
                    {
                        GUIUtility.hotControl = k_RectSelectionID;
                        evt.Use();
                    }
                    break;
            }

            Handles.EndGUI();
        }

        void HandleOnMouseDown(Event evt)
        {
            if (m_IsNearestControl)
                m_IsNearestControl = false;

            if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == k_RectSelectionID)
            {
                m_StartPoint = evt.mousePosition;
                m_SelectMousePoint = m_StartPoint;
                m_IsNearestControl = true;
            }

            m_SelectionStart = Selection.objects;
            m_CurrentSelection = null;
            m_LastSelection = null;
        }

        void HandleOnMouseUp()
        {
            if (GUIUtility.hotControl == k_RectSelectionID)
            {
                m_IsNearestControl = false;
                GUIUtility.hotControl = 0;
            }
        }

        void CompleteRectSelection()
        {
            ActiveEditorTracker.delayFlushDirtyRebuild = false;
            ActiveEditorTracker.RebuildAllIfNecessary();
            rectSelectionFinished();
        }

        void StartRectSelection(SceneView view)
        {
            ActiveEditorTracker.delayFlushDirtyRebuild = true;

            // The hot control needs to be set in an OnGUI call.
            view.SendEvent(EditorGUIUtility.CommandEvent(k_SetRectSelectionHotControlEventCommandName));

            rectSelectionStarting();

            // This is needed to update the selection in case the modifier keys changed.
            UpdateSelection(m_SelectionStart, m_CurrentSelection, m_CurrentSelectionType, true);
        }

        void UpdateSelection(Object[] existingSelection, Object newObject, SelectionType type, bool isRectSelection)
        {
            Object[] objs;
            if (newObject == null)
            {
                objs = new Object[0];
            }
            else
            {
                objs = new Object[1];
                objs[0] = newObject;
            }

            UpdateSelection(existingSelection, objs, type, isRectSelection);
        }

        void UpdateSelection(Object[] existingSelection, Object[] newObjects, SelectionType type, bool isRectSelection)
        {
            if (existingSelection == null || newObjects == null)
                return;

            Object[] newSelection;
            switch (type)
            {
                case SelectionType.Additive:
                    if (newObjects.Length > 0)
                    {
                        newSelection = new Object[existingSelection.Length + newObjects.Length];
                        Array.Copy(existingSelection, newSelection, existingSelection.Length);

                        for (int i = 0; i < newObjects.Length; i++)
                            newSelection[existingSelection.Length + i] = newObjects[i];

                        Object active = isRectSelection ? newSelection[0] : newObjects[0];
                        Selection.SetSelectionWithActiveObject(newSelection, active);
                    }
                    else
                    {
                        Selection.objects = existingSelection;
                    }
                    break;
                case SelectionType.Subtractive:
                    Dictionary<Object, bool> set = new Dictionary<Object, bool>(existingSelection.Length);
                    foreach (Object g in existingSelection)
                        set.Add(g, false);
                    foreach (Object g in newObjects)
                    {
                        if (set.ContainsKey(g))
                            set.Remove(g);
                    }
                    newSelection = new Object[set.Keys.Count];
                    set.Keys.CopyTo(newSelection, 0);
                    Selection.objects = newSelection;
                    break;
                case SelectionType.Normal:
                default:
                    Selection.objects = newObjects;
                    break;
            }
        }
    }
} // namespace
