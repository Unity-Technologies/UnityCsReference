// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.UIElements
{
    internal class ContextualMenuManipulator : MouseManipulator
    {
        System.Action<ContextualMenuPopulateEvent> m_MenuBuilder;

        public ContextualMenuManipulator(System.Action<ContextualMenuPopulateEvent> menuBuilder)
        {
            m_MenuBuilder = menuBuilder;
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseUpEvent>(OnMouseUpEvent);
            target.RegisterCallback<KeyUpEvent>(OnKeyUpEvent);
            target.RegisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(OnMouseUpEvent);
            target.UnregisterCallback<KeyUpEvent>(OnKeyUpEvent);
            target.UnregisterCallback<ContextualMenuPopulateEvent>(OnContextualMenuEvent);
        }

        void OnMouseUpEvent(MouseUpEvent evt)
        {
            if (CanStartManipulation(evt))
            {
                if (target.elementPanel != null && target.elementPanel.contextualMenuManager != null)
                {
                    target.elementPanel.contextualMenuManager.DisplayMenu(evt, target);
                    evt.StopPropagation();
                    evt.PreventDefault();
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
