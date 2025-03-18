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
            if (s_GlobalPanelDebug != null)
            {
                if (s_GlobalPanelDebug.InterceptEvent(panel, evt))
                {
                    evt.StopPropagation();
                    return true;
                }
            }

            if (panel.panelDebug?.InterceptEvent(evt) ?? false)
            {
                evt.StopPropagation();
                return true;
            }

            return false;
        }

        public static void PostDispatch(EventBase evt, [NotNull] BaseVisualElementPanel panel)
        {
            panel.panelDebug?.PostProcessEvent(evt);

            if (s_GlobalPanelDebug != null && evt.eventTypeId == ContextClickEvent.TypeId() && evt.target != null &&
                !evt.isPropagationStopped)
            {
                // Safe to handle the event (to display the right click menu)
                s_GlobalPanelDebug.OnContextClick(panel, evt as ContextClickEvent);
            }
        }
    }
}
