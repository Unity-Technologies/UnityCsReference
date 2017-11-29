// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental.UIElements
{
    public delegate EventPropagation ContextualMenuDelegate(IMGUIEvent evt, Object customData);

    internal class ContextualMenu : Manipulator
    {
        public ContextualMenuDelegate callback { get; set; }

        private readonly Object m_CustomData;

        struct Action
        {
            public GUIContent name;
            public GenericMenu.MenuFunction action;
            public bool enabled { get { return status == ActionStatus.Enabled; } }
            public ActionStatus status { get; private set; }
            public ActionStatusCallback actionStatusCallback;

            public void UpdateActionStatus()
            {
                status = (actionStatusCallback != null ? actionStatusCallback() : ActionStatus.Off);
            }
        }

        public enum ActionStatus
        {
            Off,        // Menu item will not be shown
            Enabled,
            Disabled
        }

        public Vector2 menuMousePosition { get; private set; }

        public delegate ActionStatus ActionStatusCallback();

        List<Action> menuActions = new List<Action>();

        public ContextualMenu()
        {
            menuMousePosition = Vector2.negativeInfinity;
        }

        public ContextualMenu(ContextualMenuDelegate callback) : this()
        {
            this.callback = callback;
        }

        public ContextualMenu(ContextualMenuDelegate callback, Object customData) : this(callback)
        {
            m_CustomData = customData;
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
            if (MouseCaptureController.IsMouseCaptureTaken())
                return;

            if (evt.imguiEvent.type == EventType.ContextClick)
            {
                if (callback != null)
                {
                    callback(evt, m_CustomData);
                    return;
                }

                Vector2 mousePos = evt.imguiEvent.mousePosition + new Vector2(0, DockArea.kTabHeight);

                menuMousePosition = mousePos;

                var menu = new GenericMenu();
                foreach (var action in menuActions)
                {
                    action.UpdateActionStatus();

                    switch (action.status)
                    {
                        case ActionStatus.Enabled:
                            menu.AddItem(action.name, false, () =>
                            {
                                menuMousePosition = mousePos;
                                action.action();
                                menuMousePosition = Vector2.negativeInfinity;
                            });
                            break;
                        case ActionStatus.Disabled:
                            menu.AddDisabledItem(action.name);
                            break;
                    }
                }
                menuMousePosition = Vector2.negativeInfinity;
                menu.ShowAsContext();
            }
        }

        public void AddAction(string actionName, GenericMenu.MenuFunction action, ActionStatusCallback actionStatusCallback)
        {
            Action menuAction = new Action();
            menuAction.name = new GUIContent(actionName);
            menuAction.action = action;
            menuAction.actionStatusCallback = actionStatusCallback;
            menuAction.UpdateActionStatus();
            menuActions.Add(menuAction);
        }
    }
}
