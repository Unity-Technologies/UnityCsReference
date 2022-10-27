// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using JetBrains.Annotations;

namespace UnityEngine.UIElements
{
    static class DebuggerEventDispatchUtilities
    {
        internal static IGlobalPanelDebugger s_GlobalPanelDebug;

        public static bool InterceptEvent(EventBase evt, [NotNull] BaseVisualElementPanel panel)
        {
            // s_GlobalPanelDebug handle picking elements across all panels
            if (s_GlobalPanelDebug != null && evt is IMouseEvent mouseEvent)
            {
                if (s_GlobalPanelDebug.InterceptMouseEvent(panel, mouseEvent))
                {
                    evt.StopPropagation();
                    evt.PreventDefault();
                    return true;
                }
            }

            if (panel.panelDebug?.InterceptEvent(evt) ?? false)
            {
                evt.StopPropagation();
                evt.PreventDefault();
                return true;
            }

            return false;
        }

        public static void PostDispatch(EventBase evt, [NotNull] BaseVisualElementPanel panel)
        {
            panel.panelDebug?.PostProcessEvent(evt);

            if (s_GlobalPanelDebug != null && evt is IMouseEvent mouseEvent && evt.target != null &&
                !evt.isDefaultPrevented && !evt.isPropagationStopped)
            {
                // Safe to handle the event (to display the right click menu)
                s_GlobalPanelDebug.OnPostMouseEvent(panel, mouseEvent);
            }
        }
    }
}
