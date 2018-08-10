// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal class EditorDragging
    {
        const string k_DraggingModeKey = "InspectorEditorDraggingMode";

        InspectorWindow m_InspectorWindow;
        bool m_TargetAbove;
        int m_TargetIndex = -1;
        int m_LastIndex = -1;
        float m_LastMarkerY = 0f;

        enum DraggingMode
        {
            NotApplicable,
            Component,
            Script,
        }

        static class Styles
        {
            public static readonly GUIStyle insertionMarker = "InsertionMarker";
        }

        public EditorDragging(InspectorWindow inspectorWindow)
        {
            m_InspectorWindow = inspectorWindow;
        }

        public void HandleDraggingToEditor(int editorIndex, Rect dragRect, Rect contentRect, ActiveEditorTracker tracker)
        {
            if (dragRect.height == 0f)
                return;

            if (contentRect.height == 0f)
                contentRect = dragRect;

            var targetHeight = 8f;
            var targetRect = new Rect(contentRect.x, contentRect.yMax - (targetHeight - 2f), contentRect.width, targetHeight * 2f + 1f);

            var markerY = contentRect.yMax;

            m_LastIndex = editorIndex;
            m_LastMarkerY = markerY;

            HandleEditorDragging(editorIndex, targetRect, markerY, false, tracker);
        }

        public void HandleDraggingToBottomArea(Rect bottomRect, ActiveEditorTracker tracker)
        {
            var editorIndex = m_LastIndex;
            if (editorIndex >= 0 && editorIndex < tracker.activeEditors.Length)
                HandleEditorDragging(editorIndex, bottomRect, m_LastMarkerY, true, tracker);
        }

        void HandleEditorDragging(int editorIndex, Rect targetRect, float markerY, bool bottomTarget, ActiveEditorTracker tracker)
        {
            var evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                    if (targetRect.Contains(evt.mousePosition))
                    {
                        var draggingMode = DragAndDrop.GetGenericData(k_DraggingModeKey) as DraggingMode ? ;
                        if (!draggingMode.HasValue)
                        {
                            var draggedObjects = DragAndDrop.objectReferences;

                            if (draggedObjects.Length == 0)
                                draggingMode = DraggingMode.NotApplicable;
                            else if (draggedObjects.All(o => o is Component && !(o is Transform)))
                                draggingMode = DraggingMode.Component;
                            else if (draggedObjects.All(o => o is MonoScript))
                                draggingMode = DraggingMode.Script;
                            else
                                draggingMode = DraggingMode.NotApplicable;

                            DragAndDrop.SetGenericData(k_DraggingModeKey, draggingMode);
                        }

                        if (draggingMode.Value != DraggingMode.NotApplicable)
                        {
                            var editors = tracker.activeEditors;
                            var draggedObjects = DragAndDrop.objectReferences;

                            if (bottomTarget)
                            {
                                m_TargetAbove = false;
                                m_TargetIndex = m_LastIndex;
                            }
                            else
                            {
                                m_TargetAbove = evt.mousePosition.y < targetRect.y + targetRect.height / 2f;
                                m_TargetIndex = editorIndex;

                                if (m_TargetAbove)
                                {
                                    m_TargetIndex++;
                                    while (m_TargetIndex < editors.Length && m_InspectorWindow.ShouldCullEditor(editors, m_TargetIndex))
                                        m_TargetIndex++;

                                    if (m_TargetIndex == editors.Length)
                                    {
                                        m_TargetIndex = -1;
                                        return;
                                    }
                                }
                            }

                            if (m_TargetAbove && m_InspectorWindow.EditorHasLargeHeader(m_TargetIndex, editors))
                            {
                                m_TargetIndex--;
                                while (m_TargetIndex >= 0 && m_InspectorWindow.ShouldCullEditor(editors, m_TargetIndex))
                                    m_TargetIndex--;

                                if (m_TargetIndex == -1)
                                    return;

                                m_TargetAbove = false;
                            }

                            if (draggingMode.Value == DraggingMode.Script)
                            {
                                // Validate dragging scripts
                                // Always allow script dragging, instead fail during DragPerform with a dialog box
                                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                            }
                            else
                            {
                                // Validate dragging components
                                var valid = false;
                                if (editors[m_TargetIndex].targets.All(t => t is Component))
                                {
                                    var targetComponents = editors[m_TargetIndex].targets.Cast<Component>().ToArray();
                                    var sourceComponents = DragAndDrop.objectReferences.Cast<Component>().ToArray();
                                    valid = MoveOrCopyComponents(sourceComponents, targetComponents, EditorUtility.EventHasDragCopyModifierPressed(evt), true);
                                }

                                if (valid)
                                    DragAndDrop.visualMode = EditorUtility.EventHasDragCopyModifierPressed(evt) ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
                                else
                                {
                                    DragAndDrop.visualMode = DragAndDropVisualMode.None;
                                    m_TargetIndex = -1;
                                    return;
                                }
                            }

                            evt.Use();
                        }
                    }
                    else
                        m_TargetIndex = -1;
                    break;

                case EventType.DragPerform:
                    if (m_TargetIndex != -1)
                    {
                        var draggingMode = DragAndDrop.GetGenericData(k_DraggingModeKey) as DraggingMode ? ;
                        if (!draggingMode.HasValue || draggingMode.Value == DraggingMode.NotApplicable)
                        {
                            m_TargetIndex = -1;
                            return;
                        }

                        var editors = tracker.activeEditors;
                        if (!editors[m_TargetIndex].targets.All(t => t is Component))
                            return;

                        var targetComponents = editors[m_TargetIndex].targets.Cast<Component>().ToArray();

                        if (draggingMode.Value == DraggingMode.Script)
                        {
                            var scripts = DragAndDrop.objectReferences.Cast<MonoScript>();

                            // Ensure all script components can be added
                            var valid = true;
                            foreach (var targetComponent in targetComponents)
                            {
                                var gameObject = targetComponent.gameObject;
                                if (scripts.Any(s => !ComponentUtility.WarnCanAddScriptComponent(targetComponent.gameObject, s)))
                                {
                                    valid = false;
                                    break;
                                }
                            }

                            if (valid)
                            {
                                Undo.IncrementCurrentGroup();
                                var undoGroup = Undo.GetCurrentGroup();

                                // Add script components
                                var index = 0;
                                var addedComponents = new Component[targetComponents.Length * scripts.Count()];
                                for (int i = 0; i < targetComponents.Length; i++)
                                {
                                    var targetComponent = targetComponents[i];
                                    var gameObject = targetComponent.gameObject;
                                    bool targetIsTransform = targetComponent is Transform;
                                    foreach (var script in scripts)
                                        addedComponents[index++] = Undo.AddComponent(gameObject, script.GetClass());

                                    // If the target is a Transform, the AddComponent might have replaced it with a RectTransform.
                                    // Handle this possibility by updating the target component.
                                    if (targetIsTransform)
                                        targetComponents[i] = gameObject.transform;
                                }

                                // Move added components relative to target components
                                if (!ComponentUtility.MoveComponentsRelativeToComponents(addedComponents, targetComponents, m_TargetAbove))
                                {
                                    // Revert added components if move operation fails (e.g. user aborts when asked to break prefab instance)
                                    Undo.RevertAllDownToGroup(undoGroup);
                                }
                            }
                        }
                        else
                        {
                            // Handle dragging components
                            var sourceComponents = DragAndDrop.objectReferences.Cast<Component>().ToArray();
                            if (sourceComponents.Length == 0 || targetComponents.Length == 0)
                                return;

                            MoveOrCopyComponents(sourceComponents, targetComponents, EditorUtility.EventHasDragCopyModifierPressed(evt), false);
                        }

                        m_TargetIndex = -1;
                        DragAndDrop.AcceptDrag();
                        evt.Use();
                        EditorGUIUtility.ExitGUI();
                    }
                    break;

                case EventType.DragExited:
                    m_TargetIndex = -1;
                    break;

                case EventType.Repaint:
                    if (m_TargetIndex != -1 && targetRect.Contains(evt.mousePosition))
                    {
                        var markerRect = new Rect(targetRect.x, markerY, targetRect.width, 3f);
                        if (!m_TargetAbove)
                            markerRect.y += 2f;

                        Styles.insertionMarker.Draw(markerRect, false, false, false, false);
                    }
                    break;
            }
        }

        bool MoveOrCopyComponents(Component[] sourceComponents, Component[] targetComponents, bool copy, bool validateOnly)
        {
            // This version only allows reordering of components

            if (copy)
                return false;

            if (sourceComponents.Length == 1 && targetComponents.Length == 1)
            {
                if (sourceComponents[0].gameObject != targetComponents[0].gameObject)
                    return false;

                return ComponentUtility.MoveComponentRelativeToComponent(sourceComponents[0], targetComponents[0], m_TargetAbove, validateOnly);
            }
            else
                return ComponentUtility.MoveComponentsRelativeToComponents(sourceComponents, targetComponents, m_TargetAbove, validateOnly);
        }

    }
}
