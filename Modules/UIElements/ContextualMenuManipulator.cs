// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    public class ContextualMenuManipulator : MouseManipulator
    {
        System.Action<ContextualMenuPopulateEvent> m_MenuBuilder;

        public ContextualMenuManipulator(System.Action<ContextualMenuPopulateEvent> menuBuilder)
        {
            m_MenuBuilder = menuBuilder;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
        }

        protected override void RegisterCallbacksOnTarget()
        {
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                target.RegisterCallback<MouseDownEvent>(OnMouseUpDownEvent);
            }
            else
            {
                target.RegisterCallback<MouseUpEvent>(OnMouseUpDownEvent);
            }
            target.RegisterCallback<KeyUpEvent>(OnKeyUpEvent);
            target.RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseUpDownEvent);
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
                if (target.elementPanel != null && target.elementPanel.contextualMenuManager != null)
                {
                    EventBase e = evt as EventBase;
                    target.elementPanel.contextualMenuManager.DisplayMenu(e, target);
                    e.StopPropagation();
                    e.PreventDefault();
                }
            }
        }

        void OnKeyUpEvent(KeyUpEvent evt)
        {
            if (evt.keyCode == KeyCode.Menu)
            {
                if (target.elementPanel != null && target.elementPanel.contextualMenuManager != null)
                {
                    target.elementPanel.contextualMenuManager.DisplayMenu(evt, target);
                    evt.StopPropagation();
                    evt.PreventDefault();
                }
            }
        }

        void OnContextualMenuEvent(ContextualMenuPopulateEvent evt)
        {
            if (m_MenuBuilder != null)
            {
                m_MenuBuilder(evt);
            }
        }
    }
}
