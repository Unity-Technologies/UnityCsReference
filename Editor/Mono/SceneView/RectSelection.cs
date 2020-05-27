// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    // Handles picking/selection in the scene view (both "click" type and "drag-rect" type)
    internal class RectSelection
    {
        Vector2 m_SelectStartPoint;
        Vector2 m_SelectMousePoint;
        Object[] m_SelectionStart;
        bool m_RectSelecting;
        Dictionary<GameObject, bool> m_LastSelection;
        enum SelectionType { Normal, Additive, Subtractive }
        Object[] m_CurrentSelection = null;
        readonly EditorWindow m_Window;

        internal static event Action rectSelectionStarting = delegate {};
        internal static event Action rectSelectionFinished = delegate {};

        static readonly int s_RectSelectionID = GUIUtility.GetPermanentControlID();

        public RectSelection(EditorWindow window)
        {
            m_Window = window;
        }

        public void OnGUI()
        {
            Event evt = Event.current;

            Handles.BeginGUI();

            Vector2 mousePos = evt.mousePosition;
            int id = s_RectSelectionID;

            switch (evt.GetTypeForControl(id))
            {
                case EventType.Layout:
                case EventType.MouseMove:
                    if (!Tools.viewToolActive)
                        HandleUtility.AddDefaultControl(id);
                    break;

                case EventType.MouseDown:
                    if (HandleUtility.nearestControl == id && evt.button == 0)
                    {
                        GUIUtility.hotControl = id;
                        m_SelectStartPoint = mousePos;
                        m_SelectionStart = Selection.objects;
                        m_RectSelecting = false;
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == id)
                    {
                        if (!m_RectSelecting && (mousePos - m_SelectStartPoint).magnitude > 6f)
                        {
                            EditorApplication.modifierKeysChanged += SendCommandsOnModifierKeys;
                            m_RectSelecting = true;
                            ActiveEditorTracker.delayFlushDirtyRebuild = true;
                            m_LastSelection = null;
                            m_CurrentSelection = null;
                            rectSelectionStarting();
                        }
                        if (m_RectSelecting)
                        {
                            m_SelectMousePoint = new Vector2(Mathf.Max(mousePos.x, 0), Mathf.Max(mousePos.y, 0));
                            GameObject[] rectObjs = HandleUtility.PickRectObjects(EditorGUIExt.FromToRect(m_SelectStartPoint, m_SelectMousePoint));
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
                                if (evt.shift)
                                    UpdateSelection(m_SelectionStart, rectObjs, SelectionType.Additive, m_RectSelecting);
                                else if (EditorGUI.actionKey)
                                    UpdateSelection(m_SelectionStart, rectObjs, SelectionType.Subtractive, m_RectSelecting);
                                else
                                    UpdateSelection(m_SelectionStart, rectObjs, SelectionType.Normal, m_RectSelecting);
                            }
                        }
                        evt.Use();
                    }
                    break;

                case EventType.Repaint:
                    if (GUIUtility.hotControl == id && m_RectSelecting)
                        EditorStyles.selectionRect.Draw(EditorGUIExt.FromToRect(m_SelectStartPoint, m_SelectMousePoint), GUIContent.none, false, false, false, false);
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id && evt.button == 0)
                    {
                        GUIUtility.hotControl = 0;
                        if (m_RectSelecting)
                        {
                            EditorApplication.modifierKeysChanged -= SendCommandsOnModifierKeys;
                            m_RectSelecting = false;
                            ActiveEditorTracker.delayFlushDirtyRebuild = false;
                            ActiveEditorTracker.RebuildAllIfNecessary();
                            m_SelectionStart = new Object[0];
                            rectSelectionFinished();
                            evt.Use();
                        }
                        else
                        {
                            if (evt.shift || EditorGUI.actionKey)
                            {
                                // For shift, we check if EXACTLY the active GO is hovered by mouse and then subtract. Otherwise additive.
                                // For control/cmd, we check if ANY of the selected GO is hovered by mouse and then subtract. Otherwise additive.
                                // Control/cmd takes priority over shift.
                                GameObject hovered = HandleUtility.PickGameObject(evt.mousePosition, false);

                                var handledIt = false;
                                // shift-click deselects only if the active GO is exactly what we clicked on
                                if (!EditorGUI.actionKey && Selection.activeGameObject == hovered)
                                {
                                    UpdateSelection(m_SelectionStart, hovered, SelectionType.Subtractive, m_RectSelecting);
                                    handledIt = true;
                                }

                                // ctrl-click deselects everything up to prefab root, that is already selected
                                if (!handledIt && EditorGUI.actionKey)
                                {
                                    var selectedGos = Selection.gameObjects;
                                    var hoveredRoot = HandleUtility.FindSelectionBaseForPicking(hovered);
                                    var deselectList = new List<Object>();
                                    while (hovered != null)
                                    {
                                        if (selectedGos.Contains(hovered))
                                            deselectList.Add(hovered);
                                        if (hovered == hoveredRoot)
                                            break;
                                        var parent = hovered.transform.parent;
                                        if (parent)
                                            hovered = parent.gameObject;
                                        else
                                            break;
                                    }
                                    if (deselectList.Any())
                                    {
                                        UpdateSelection(m_SelectionStart, deselectList.ToArray(), SelectionType.Subtractive, m_RectSelecting);
                                        handledIt = true;
                                    }
                                }

                                // we did not deselect anything, so add the new thing into selection instead
                                if (!handledIt)
                                {
                                    UpdateSelection(m_SelectionStart, HandleUtility.PickGameObject(evt.mousePosition, true), SelectionType.Additive, m_RectSelecting);
                                }
                            }
                            else // With no modifier keys, we do the "cycle through overlapped" picking logic in SceneViewPicking.cs
                            {
                                GameObject picked = SceneViewPicking.PickGameObject(evt.mousePosition);
                                UpdateSelection(m_SelectionStart, picked, SelectionType.Normal, m_RectSelecting);
                            }

                            evt.Use();
                        }
                    }
                    break;
                case EventType.ExecuteCommand:
                    if (id == GUIUtility.hotControl && evt.commandName == EventCommandNames.ModifierKeysChanged)
                    {
                        if (evt.shift)
                            UpdateSelection(m_SelectionStart, m_CurrentSelection, SelectionType.Additive, m_RectSelecting);
                        else if (EditorGUI.actionKey)
                            UpdateSelection(m_SelectionStart, m_CurrentSelection, SelectionType.Subtractive, m_RectSelecting);
                        else
                            UpdateSelection(m_SelectionStart, m_CurrentSelection, SelectionType.Normal, m_RectSelecting);
                        evt.Use();
                    }
                    break;
            }

            Handles.EndGUI();
        }

        static void UpdateSelection(Object[] existingSelection, Object newObject, SelectionType type, bool isRectSelection)
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

        static void UpdateSelection(Object[] existingSelection, Object[] newObjects, SelectionType type, bool isRectSelection)
        {
            Object[] newSelection;
            switch (type)
            {
                case SelectionType.Additive:
                    if (newObjects.Length > 0)
                    {
                        newSelection = new Object[existingSelection.Length + newObjects.Length];
                        System.Array.Copy(existingSelection, newSelection, existingSelection.Length);
                        for (int i = 0; i < newObjects.Length; i++)
                            newSelection[existingSelection.Length + i] = newObjects[i];
                        if (!isRectSelection)
                            Selection.activeObject = newObjects[0];
                        else
                            Selection.activeObject = newSelection[0];

                        Selection.objects = newSelection;
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

        // When rect selecting, we update the selected objects based on which modifier keys are currently held down,
        // so the window needs to repaint.
        void SendCommandsOnModifierKeys()
        {
            m_Window.SendEvent(EditorGUIUtility.CommandEvent(EventCommandNames.ModifierKeysChanged));
        }
    }
} // namespace
