using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Manipulator that displays a contextual menu when the user clicks the right mouse button or presses the menu key on the keyboard.
    /// </summary>
    public class ContextualMenuManipulator : MouseManipulator
    {
        System.Action<ContextualMenuPopulateEvent> m_MenuBuilder;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ContextualMenuManipulator(System.Action<ContextualMenuPopulateEvent> menuBuilder)
        {
            m_MenuBuilder = menuBuilder;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
        }

        /// <summary>
        /// Register the event callbacks on the manipulator target.
        /// </summary>
        protected override void RegisterCallbacksOnTarget()
        {
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                target.RegisterCallback<MouseDownEvent>(OnMouseDownEventOSX);
                target.RegisterCallback<MouseUpEvent>(OnMouseUpEventOSX);
            }
            else
            {
                target.RegisterCallback<MouseUpEvent>(OnMouseUpDownEvent);
            }
            target.RegisterCallback<KeyUpEvent>(OnKeyUpEvent);
            target.RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuEvent);
        }

        /// <summary>
        /// Unregister the event callbacks from the manipulator target.
        /// </summary>
        protected override void UnregisterCallbacksFromTarget()
        {
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDownEventOSX);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUpEventOSX);
            }
            else
            {
                target.UnregisterCallback<MouseUpEvent>(OnMouseUpDownEvent);
            }

            target.UnregisterCallback<KeyUpEvent>(OnKeyUpEvent);
            target.UnregisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuEvent);
        }

        void OnMouseUpDownEvent(IMouseEvent evt)
        {
            if (CanStartManipulation(evt))
            {
                DoDisplayMenu(evt as EventBase);
            }
        }

        void OnMouseDownEventOSX(MouseDownEvent evt)
        {
            if (target.elementPanel?.contextualMenuManager != null)
                target.elementPanel.contextualMenuManager.displayMenuHandledOSX = false;

            var eventBase = evt as EventBase;
            if (eventBase.isDefaultPrevented)
                return;

            OnMouseUpDownEvent(evt);
        }

        void OnMouseUpEventOSX(MouseUpEvent evt)
        {
            if (target.elementPanel?.contextualMenuManager != null &&
                target.elementPanel.contextualMenuManager.displayMenuHandledOSX)
                return;

            OnMouseUpDownEvent(evt);
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
                evt.PreventDefault();
            }
        }

        void OnContextualMenuEvent(ContextualMenuPopulateEvent evt)
        {
            m_MenuBuilder?.Invoke(evt);
        }
    }
}
