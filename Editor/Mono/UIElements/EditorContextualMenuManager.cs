// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.Experimental.UIElements
{
    class EditorContextualMenuManager : ContextualMenuManager
    {
        public override void DisplayMenuIfEventMatches(EventBase evt, IEventHandler eventHandler)
        {
            if (evt.GetEventTypeId() == MouseUpEvent.TypeId())
            {
                MouseUpEvent e = evt as MouseUpEvent;
                if (e.button == (int)MouseButton.RightMouse)
                {
                    DisplayMenu(evt, eventHandler);
                    evt.StopPropagation();
                }
            }
            else if (evt.GetEventTypeId() == KeyUpEvent.TypeId())
            {
                KeyUpEvent e = evt as KeyUpEvent;
                if (e.keyCode == KeyCode.Menu)
                {
                    DisplayMenu(evt, eventHandler);
                    evt.StopPropagation();
                }
            }
        }

        protected internal override void DoDisplayMenu(DropdownMenu menu, EventBase triggerEvent)
        {
            menu.DoDisplayEditorMenu(triggerEvent);
        }
    }
}
