// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    internal class EditorDragging
    {
        const string k_DraggingModeKey = "InspectorEditorDraggingMode";

        IPropertyView m_InspectorWindow;
        bool m_TargetAbove;
        int m_TargetIndex = -1;
        int m_LastIndex = -1;

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

        public EditorDragging(IPropertyView inspectorWindow)
        {
            m_InspectorWindow = inspectorWindow;
        }

        public void HandleDraggingToEditor(Editor[] editors, int editorIndex, Rect dragRect, Rect contentRect)
        {
            if (dragRect.height == 0f)
                return;

            if (contentRect.height == 0f)
                contentRect = dragRect;

            var targetRect = GetTargetRect(contentRect);

            var markerY = contentRect.yMax;

            m_LastIndex = editorIndex;

            HandleEditorDragging(editors, editorIndex, targetRect, markerY, false);
        }

        int m_BottomAreaDropIndex = -1;
        Rect m_BottomArea;

        public void HandleDraggingInBottomArea(Editor[] editors, Rect bottomRect, Rect contentRect)
        {
            HandleNativeDragDropInBottomArea(editors, bottomRect);

            if (m_LastIndex >= 0 && m_LastIndex < editors.Length)
            {
                m_BottomArea = bottomRect;
                m_BottomAreaDropIndex = m_LastIndex;
                HandleEditorDragging(editors, m_LastIndex, bottomRect, contentRect.yMax, true);
            }
            else
            {
                m_BottomAreaDropIndex = -1;
                m_BottomArea = Rect.zero;
            }
        }

        internal void HandleDragPerformInBottomArea(Editor[] editors, Rect bottomRect, Rect targetRect)
        {
            HandleNativeDragDropInBottomArea(editors, bottomRect);

            if (m_LastIndex >= 0 && m_LastIndex  < editors.Length)
            {
                HandleEditorDragging(editors, m_LastIndex, GetTargetRect(targetRect), targetRect.yMax, true);
            }

            m_BottomAreaDropIndex = -1;
            m_BottomArea = Rect.zero;
        }

        void HandleNativeDragDropInBottomArea(Editor[] editors, Rect rect)
        {
            if (!DraggingOverRect(rect))
            {
                return;
            }

            Editor editor = InspectorWindowUtils.GetFirstNonImportInspectorEditor(editors);
            if (editor == null)
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropService.Drop(DragAndDropService.kInspectorDropDstId, editor.targets, Event.current.type == EventType.DragPerform);

            if (Event.current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                m_TargetIndex = -1;
                GUIUtility.ExitGUI();
            }
        }

        void HandleEditorDragging(Editor[] editors, int editorIndex, Rect targetRect, float markerY, bool bottomTarget)
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

                            if (m_TargetAbove && InspectorWindow.EditorHasLargeHeader(m_TargetIndex, editors))
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
                        HandleDragPerformEvent(editors, evt, ref m_TargetIndex);
                    }
                    break;

                case EventType.DragExited:
                    m_TargetIndex = -1;
                    break;

                case EventType.Repaint:
                    if (m_TargetIndex != -1 && editorIndex == m_TargetIndex &&
                        (targetRect.Contains(evt.mousePosition) ||
                         m_BottomArea.Contains(GUIClip.UnclipToWindow(evt.mousePosition)) &&
                         m_BottomAreaDropIndex == editors.Length - 1))
                    {
                        Styles.insertionMarker.Draw(GetMarkerRect(targetRect, markerY, m_TargetAbove), false, false, false, false);
                    }
                    break;
            }
        }

        static bool DraggingOverRect(Rect rect)
        {
            return (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform) && rect.Contains(Event.current.mousePosition);
        }

        void HandleDragPerformEvent(Editor[] editors, Event evt, ref int targetIndex)
        {
            if (targetIndex != -1)
            {
                var draggingMode = DragAndDrop.GetGenericData(k_DraggingModeKey) as DraggingMode ? ;
                if (!draggingMode.HasValue || draggingMode.Value == DraggingMode.NotApplicable)
                {
                    targetIndex = -1;
                    return;
                }

                if (!editors[targetIndex].targets.All(t => t is Component))
                    return;

                var targetComponents = editors[targetIndex].targets.Cast<Component>().ToArray();

                if (draggingMode.Value == DraggingMode.Script)
                {
                    var scripts = DragAndDrop.objectReferences.Cast<MonoScript>();

                    // Ensure all script components can be added
                    var valid = true;
                    foreach (var targetComponent in targetComponents)
                    {
                        var gameObject = targetComponent.gameObject;
                        if (scripts.Any(s => !ComponentUtility.WarnCanAddScriptComponent(gameObject, s)))
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
                                addedComponents[index++] = ObjectFactory.AddComponent(gameObject, script.GetClass());

                            // If the target is a Transform, the AddComponent might have replaced it with a RectTransform.
                            // Handle this possibility by updating the target component.
                            if (targetIsTransform)
                                targetComponents[i] = gameObject.transform;
                        }

                        // Move added components relative to target components
                        if (!ComponentUtility.MoveComponentsRelativeToComponents(addedComponents, targetComponents, m_TargetAbove))
                        {
                            // Ensure we have the same selection after calling RevertAllDownToGroup below (MoveComponentsRelativeToComponents can have opened a Prefab in Prefab Mode and changed selection to that root)
                            var wantedSelectedGameObject = Selection.activeGameObject;

                            // Revert added components if move operation fails (e.g. user has been shown the dialog with 'prefab instance restructuring is not posssible' or object is not editable)
                            Undo.RevertAllDownToGroup(undoGroup);

                            if (wantedSelectedGameObject != Selection.activeGameObject)
                                Selection.activeGameObject = wantedSelectedGameObject;
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

                targetIndex = -1;
                DragAndDrop.AcceptDrag();
                evt.Use();
                EditorGUIUtility.ExitGUI();
            }
        }

        private static Rect GetTargetRect(Rect contentRect)
        {
            var targetHeight = 9f;
            var yPos = contentRect.yMax - targetHeight * .75f;
            var uiDragTargetHeight = targetHeight * .75f;
            return new Rect(contentRect.x, yPos + uiDragTargetHeight / (targetHeight * .25f), contentRect.width, uiDragTargetHeight);
        }

        private static Rect GetMarkerRect(Rect targetRect, float markerY, bool targetAbove)
        {
            var markerRect = new Rect(targetRect);
            if (!targetAbove)
                markerRect.y += 2f;

            return markerRect;
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
