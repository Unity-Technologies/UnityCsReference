// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.GraphToolsFoundation.Editor
{
    /// <summary>
    /// Selects an element when it receives a mouse down event.
    /// </summary>
    abstract class ClickSelector : MouseManipulator
    {
        bool m_SelectOnMouseUp;

        static bool WasSelectableDescendantHitByMouse(ModelView currentTarget, MouseDownEvent evt)
        {
            VisualElement targetElement = evt.target as VisualElement;

            if (targetElement == null || currentTarget == targetElement)
                return false;

            VisualElement descendant = targetElement;

            while (descendant != null && currentTarget != descendant)
            {
                GraphElement selectableDescendant = descendant as GraphElement;

                if (selectableDescendant != null && selectableDescendant.enabledInHierarchy && selectableDescendant.pickingMode != PickingMode.Ignore && selectableDescendant.GraphElementModel.IsSelectable())
                {
                    Vector2 localMousePosition = currentTarget.ChangeCoordinatesTo(descendant, evt.localMousePosition);

                    if (selectableDescendant.ContainsPoint(localMousePosition))
                    {
                        return true;
                    }
                }
                descendant = descendant.parent;
            }
            return false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClickSelector"/> class.
        /// </summary>
        public ClickSelector()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt });

            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Command });
            }
            else
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        void OnMouseDown(MouseDownEvent e)
        {
            if (!(e.currentTarget is ModelView modelUI))
                return;

            if (CanStartManipulation(e) &&
                modelUI.ContainsPoint(e.localMousePosition) &&
                !WasSelectableDescendantHitByMouse(modelUI, e))
            {
                if (IsSelected(modelUI))
                {
                    // Defer the selection change to the mouse up event, to give a chance to other manipulators to
                    // act on the current selection before changing it.
                    m_SelectOnMouseUp = true;
                }
                else
                {
                    m_SelectOnMouseUp = false;
                    HandleClick(e);
                }

                // Do not stop the propagation as it is common case for a parent start to move the selection on a mouse down.
            }
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            m_SelectOnMouseUp = false;
        }

        void OnMouseUp(MouseUpEvent e)
        {
            if (m_SelectOnMouseUp)
            {
                HandleClick(e);
            }
        }

        protected abstract bool IsSelected(ModelView modelView);

        /// <summary>
        /// Handles a mouse down event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected abstract void HandleClick(IMouseEvent e);
    }
}
