// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    internal class ContextualMenu : Manipulator
    {
        struct Action
        {
            public GUIContent name;
            public GenericMenu.MenuFunction action;
            public bool enabled;
        }

        public enum ActionStatus
        {
            Off,        // Menu item will not be shown
            Enabled,
            Disabled
        }

        public delegate ActionStatus ActionStatusCallback();

        List<Action> menuActions = new List<Action>();

        public ContextualMenu()
        {
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<IMGUIEvent>(OnIMGUIEvent, Capture.Capture);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<IMGUIEvent>(OnIMGUIEvent, Capture.Capture);
        }

        void OnIMGUIEvent(IMGUIEvent evt)
        {
            if (evt.imguiEvent.type == EventType.ContextClick)
            {
                var menu = new GenericMenu();
                foreach (var action in menuActions)
                {
                    if (action.enabled)
                        menu.AddItem(action.name, false, action.action);
                    else
                        menu.AddDisabledItem(action.name);
                }
                menu.ShowAsContext();
            }
        }

        public void AddAction(string actionName, GenericMenu.MenuFunction action, ActionStatusCallback actionStatusCallback)
        {
            ActionStatus actionStatus = (actionStatusCallback != null ? actionStatusCallback() : ActionStatus.Off);
            if (actionStatus > ActionStatus.Off)
            {
                Action menuAction = new Action();
                menuAction.name = new GUIContent(actionName);
                menuAction.action = action;
                menuAction.enabled = (actionStatus == ActionStatus.Enabled);
                menuActions.Add(menuAction);
            }
        }
    }
}
