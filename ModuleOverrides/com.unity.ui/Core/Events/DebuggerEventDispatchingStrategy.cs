// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    class DebuggerEventDispatchingStrategy : IEventDispatchingStrategy
    {
        internal static IGlobalPanelDebugger s_GlobalPanelDebug;

        public bool CanDispatchEvent(EventBase evt)
        {
            return true;
        }

        public void DispatchEvent(EventBase evt, IPanel panel)
        {
            // s_GlobalPanelDebug handle picking elements across all panels
            if (s_GlobalPanelDebug != null)
            {
                if (s_GlobalPanelDebug.InterceptEvent(panel, evt))
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                    evt.stopDispatch = true;
                    return;
                }
            }

            var panelDebug = (panel as BaseVisualElementPanel)?.panelDebug;
            if (panelDebug != null)
            {
                if (panelDebug.InterceptEvent(evt))
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                    evt.stopDispatch = true;
                }
            }
        }

        public void PostDispatch(EventBase evt, IPanel panel)
        {
            var panelDebug = (panel as BaseVisualElementPanel)?.panelDebug;
            panelDebug?.PostProcessEvent(evt);

            if (s_GlobalPanelDebug != null && evt.eventTypeId == ContextClickEvent.TypeId() && evt.target != null && !evt.isDefaultPrevented && !evt.isPropagationStopped)
            {
                // Safe to handle the event (to display the right click menu)
                s_GlobalPanelDebug.OnContextClick(panel, evt as ContextClickEvent);
            }
        }
    }
}
