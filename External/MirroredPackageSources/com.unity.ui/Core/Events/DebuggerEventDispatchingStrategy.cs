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
            IMouseEvent mouseEvent = evt as IMouseEvent;
            if (s_GlobalPanelDebug != null && mouseEvent != null)
            {
                if (s_GlobalPanelDebug.InterceptMouseEvent(panel, mouseEvent))
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
            IMouseEvent mouseEvent = evt as IMouseEvent;
            var panelDebug = (panel as BaseVisualElementPanel)?.panelDebug;
            if (panelDebug != null)
            {
                panelDebug.PostProcessEvent(evt);
            }
            if (s_GlobalPanelDebug != null && mouseEvent != null && evt.target != null && !evt.isDefaultPrevented && !evt.isPropagationStopped)
            {
                // Safe to handle the event (to display the right click menu)
                s_GlobalPanelDebug.OnPostMouseEvent(panel, mouseEvent);
            }
        }
    }
}
