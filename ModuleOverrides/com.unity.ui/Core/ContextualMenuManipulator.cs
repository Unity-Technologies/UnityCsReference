// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Manipulator that displays a contextual menu when the user clicks the right mouse button or presses the menu key on the keyboard.
    /// </summary>
    public class ContextualMenuManipulator : PointerManipulator
    {
        Action<ContextualMenuPopulateEvent> m_MenuBuilder;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ContextualMenuManipulator(Action<ContextualMenuPopulateEvent> menuBuilder)
        {
            m_MenuBuilder = menuBuilder;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            if (UIElementsUtility.isOSXContextualMenuPlatform)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
        }

        /// <summary>
        /// Register the event callbacks on the manipulator target.
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
            if (UIElementsUtility.isOSXContextualMenuPlatform)
            {
                target.RegisterCallback<PointerDownEvent>(OnPointerDownEventOSX);
                target.RegisterCallback<PointerUpEvent>(OnPointerUpEventOSX);
                target.RegisterCallback<PointerMoveEvent>(OnPointerMoveEventOSX);
            }
            else
            {
                target.RegisterCallback<PointerUpEvent>(OnPointerUpEvent);
                target.RegisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            }

            target.RegisterCallback<KeyUpEvent>(OnKeyUpEvent);
            target.RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuEvent);
        }

        /// <summary>
        /// Unregister the event callbacks from the manipulator target.
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            if (UIElementsUtility.isOSXContextualMenuPlatform)
            {
                target.UnregisterCallback<PointerDownEvent>(OnPointerDownEventOSX);
                target.UnregisterCallback<PointerUpEvent>(OnPointerUpEventOSX);
                target.UnregisterCallback<PointerMoveEvent>(OnPointerMoveEventOSX);
            }
            else
            {
                target.UnregisterCallback<PointerUpEvent>(OnPointerUpEvent);
                target.UnregisterCallback<PointerMoveEvent>(OnPointerMoveEvent);
            }

            target.UnregisterCallback<KeyUpEvent>(OnKeyUpEvent);
            target.UnregisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuEvent);
        }

        void OnPointerUpEvent(IPointerEvent evt)
        {
            ProcessPointerEvent(evt);
        }

        void OnPointerDownEventOSX(IPointerEvent evt)
        {
            if (target.elementPanel?.contextualMenuManager != null)
                target.elementPanel.contextualMenuManager.displayMenuHandledOSX = false;

            ProcessPointerEvent(evt);
        }

        void OnPointerUpEventOSX(IPointerEvent evt)
        {
            if (target.elementPanel?.contextualMenuManager != null &&
                target.elementPanel.contextualMenuManager.displayMenuHandledOSX)
                return;

            ProcessPointerEvent(evt);
        }

        void OnPointerMoveEvent(PointerMoveEvent evt)
        {
            if (evt.isPointerUp)
                OnPointerUpEvent(evt);
        }

        void OnPointerMoveEventOSX(PointerMoveEvent evt)
        {
            if (evt.isPointerUp)
                OnPointerUpEventOSX(evt);
            else if (evt.isPointerDown)
                OnPointerDownEventOSX(evt);
        }

        void ProcessPointerEvent(IPointerEvent evt)
        {
            if (CanStartManipulation(evt))
            {
                DoDisplayMenu(evt as EventBase);
            }
        }

        void OnKeyUpEvent(KeyUpEvent evt)
        {
            if (evt.keyCode == KeyCode.Menu)
            {
                DoDisplayMenu(evt);
            }
        }

        void DoDisplayMenu(EventBase evt)
        {
            if (target.elementPanel?.contextualMenuManager != null)
            {
                target.elementPanel.contextualMenuManager.DisplayMenu(evt, target);
                evt.StopPropagation();
            }
        }

        void OnContextualMenuEvent(ContextualMenuPopulateEvent evt)
        {
            m_MenuBuilder?.Invoke(evt);
        }
    }
}
