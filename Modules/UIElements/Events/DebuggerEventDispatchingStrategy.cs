// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Experimental.UIElements
{
    class DebuggerEventDispatchingStrategy : IEventDispatchingStrategy
    {
        public bool CanDispatchEvent(EventBase evt)
        {
            return true;
        }

        public void DispatchEvent(EventBase evt, IPanel panel)
        {
            var panelDebug = (panel as BaseVisualElementPanel)?.panelDebug;
            if (panelDebug != null && panelDebug.showOverlay)
            {
                if (panelDebug.InterceptEvents(evt.imguiEvent))
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                    evt.stopDispatch = true;
                }
            }
        }
    }
}
