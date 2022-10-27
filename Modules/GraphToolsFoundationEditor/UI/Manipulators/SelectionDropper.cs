// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Manipulator to initiate a drag and drop operation.
    /// </summary>
    class SelectionDropper : Manipulator
    {
        const string k_DragAndDropKey = "SelectionDropperElements";
        static IReadOnlyList<GraphElementModel> s_EmptyElementList = new List<GraphElementModel>();
        const float k_StartDragThreshold = 4.0f;

        Vector2 m_MouseDownPosition;
        bool m_Active;
        bool m_Dragging;
        readonly MouseButton m_ActivateButton;

        // selectedElement is used to store a unique selection candidate for cases where user clicks on an item not to
        // drag it but just to reset the selection -- we only know this after the manipulation has ended
        BlackboardElement m_SelectedElement;
        IDragSource m_SelectionContainer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionDropper"/> class.
        /// </summary>
        public SelectionDropper(MouseButton activateButton = MouseButton.LeftMouse)
        {
            m_Active = false;
            m_ActivateButton = activateButton;
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        void Reset()
        {
            m_Active = false;
            m_Dragging = false;
        }

        /// <summary>
        /// Callback for the MouseCaptureOut event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseCaptureOutEvent(MouseCaptureOutEvent e)
        {
            if (m_Active)
            {
                Reset();
            }
        }

        /// <summary>
        /// Callback for the MouseDown event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            m_Active = false;
            m_Dragging = false;

            if (target == null)
                return;

            m_SelectionContainer = target.GetFirstAncestorOfType<IDragSource>();

            if (m_SelectionContainer == null)
            {
                // Keep for potential later use in OnMouseUp (where e.target might be different then)
                m_SelectionContainer = target.GetFirstOfType<IDragSource>();
                m_SelectedElement = e.target as BlackboardElement;
                return;
            }

            m_SelectedElement = target.GetFirstOfType<BlackboardElement>();

            if (m_SelectedElement == null)
                return;

            if (e.button == (int)m_ActivateButton)
            {
                // avoid starting a manipulation on a non movable object

                if (!m_SelectedElement.GraphElementModel.IsDroppable())
                    return;

                m_MouseDownPosition = e.localMousePosition;
                m_Active = true;
                target.CaptureMouse();
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Callback for the MouseMove event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Active && !m_Dragging && m_SelectionContainer != null)
            {
                // Keep a copy of the selection
                var selection = new List<GraphElementModel>(m_SelectionContainer.GetSelection());
                if (selection.Count > 0)
                {
                    if (m_SelectedElement != null &&
                        m_SelectedElement.GraphElementModel.IsDroppable() &&
                        Vector2.Distance(m_MouseDownPosition, e.localMousePosition) > k_StartDragThreshold)
                    {
                        DragAndDrop.PrepareStartDrag();
                        DragAndDrop.objectReferences = new Object[] {};      // this IS required for dragging to work
                        DragAndDrop.SetGenericData(k_DragAndDropKey, selection);
                        m_Dragging = true;

                        DragAndDrop.StartDrag("");
                        DragAndDrop.visualMode = e.actionKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
                        target.ReleaseMouse();
                    }

                    e.StopPropagation();
                }
            }
        }

        /// <summary>
        /// Callback for the MouseUp event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseUp(MouseUpEvent e)
        {
            if (e.button == (int)m_ActivateButton)
            {
                target.ReleaseMouse();
                e.StopPropagation();
                Reset();
            }
        }

        /// <summary>
        /// Gets the list of dragged element models from the global <see cref="DragAndDrop"/> object.
        /// </summary>
        /// <returns>The list of dragged element models.</returns>
        public static IReadOnlyList<GraphElementModel> GetDraggedElements()
        {
            if (DragAndDrop.objectReferences.Length != 0)
                return s_EmptyElementList;

            return DragAndDrop.GetGenericData(k_DragAndDropKey) as IReadOnlyList<GraphElementModel> ?? s_EmptyElementList;
        }
    }
}
