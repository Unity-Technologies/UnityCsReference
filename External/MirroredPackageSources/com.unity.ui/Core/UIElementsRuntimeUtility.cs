using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements
{
    internal static class UIElementsRuntimeUtility
    {
        static EventDispatcher s_RuntimeDispatcher = new EventDispatcher();

        private static event Action s_onRepaintOverlayPanels;
        internal static event Action onRepaintOverlayPanels
        {
            add
            {
                if (s_onRepaintOverlayPanels == null)
                {
                    RegisterPlayerloopCallback();
                }

                s_onRepaintOverlayPanels += value;
            }

            remove
            {
                s_onRepaintOverlayPanels -= value;

                if (s_onRepaintOverlayPanels == null)
                {
                    UnregisterPlayerloopCallback();
                }
            }
        }

        static UIElementsRuntimeUtility()
        {
            UIElementsRuntimeUtilityNative.RepaintOverlayPanelsCallback = RepaintOverlayPanels;
        }

        public static EventBase CreateEvent(Event systemEvent)
        {
            Debug.Assert(s_RuntimeDispatcher != null, "Call UIElementsRuntimeUtility.InitRuntimeEventSystem before sending any event.");
            return UIElementsUtility.CreateEvent(systemEvent, systemEvent.rawType);
        }

        // To be made obsolete once runtime package stop using it.
        public static IPanel CreateRuntimePanel(ScriptableObject ownerObject)
        {
            return FindOrCreateRuntimePanel(ownerObject);
        }

        public static IPanel FindOrCreateRuntimePanel(ScriptableObject ownerObject)
        {
            Panel panel;
            if (!UIElementsUtility.TryGetPanel(ownerObject.GetInstanceID(), out panel))
            {
                panel = new RuntimePanel(ownerObject, s_RuntimeDispatcher)
                {
                    IMGUIEventInterests = new EventInterests { wantsMouseMove = true, wantsMouseEnterLeaveWindow = true }
                };

                RegisterCachedPanelInternal(ownerObject.GetInstanceID(), panel);
            }
            else
            {
                Debug.Assert(ContextType.Player == panel.contextType, "Panel is not a runtime panel.");
            }

            return panel;
        }

        public static void DisposeRuntimePanel(ScriptableObject ownerObject)
        {
            Panel panel;
            if (UIElementsUtility.TryGetPanel(ownerObject.GetInstanceID(), out panel))
            {
                panel.Dispose();
                RemoveCachedPanelInternal(ownerObject.GetInstanceID());
            }
        }

        private static bool s_RegisteredPlayerloopCallback = false;

        // To be made obsolete once runtime package stop using it.
        public static void RegisterCachedPanel(int instanceID, IPanel panel)
        {
            RegisterCachedPanelInternal(instanceID, panel);
        }

        private static void RegisterCachedPanelInternal(int instanceID, IPanel panel)
        {
            UIElementsUtility.RegisterCachedPanel(instanceID, panel as Panel);
            if (!s_RegisteredPlayerloopCallback)
            {
                s_RegisteredPlayerloopCallback = true;
                RegisterPlayerloopCallback();
            }
        }

        // To be made obsolete once runtime package stop using it.
        public static void RemoveCachedPanel(int instanceID)
        {
            RemoveCachedPanelInternal(instanceID);
        }

        private static void RemoveCachedPanelInternal(int instanceID)
        {
            UIElementsUtility.RemoveCachedPanel(instanceID);
            // un-register the playerloop callback as the last panel gets un-registered
            UIElementsUtility.GetAllPanels(panelsIteration, ContextType.Player);
            if (panelsIteration.Count == 0)
            {
                s_RegisteredPlayerloopCallback = false;
                UnregisterPlayerloopCallback();
            }
        }

        static List<Panel> panelsIteration = new List<Panel>();
        internal static readonly string s_RepaintProfilerMarkerName = "UIElementsRuntimeUtility.DoDispatch(Repaint Event)";
        private static readonly ProfilerMarker s_RepaintProfilerMarker = new ProfilerMarker(s_RepaintProfilerMarkerName);

        public static void RepaintOverlayPanels()
        {
            UIElementsUtility.GetAllPanels(panelsIteration, ContextType.Player);
            foreach (RuntimePanel panel in panelsIteration)
            {
                if (!panel.drawToCameras && panel.targetTexture == null)
                {
                    using (s_RepaintProfilerMarker.Auto())
                        panel.Repaint(Event.current);
                    (panel.panelDebug?.debuggerOverlayPanel as Panel)?.Repaint(Event.current);
                }
            }

            // Call the package override of RepaintOverlayPanels, when available
            if (s_onRepaintOverlayPanels != null)
                s_onRepaintOverlayPanels();
        }

        public static void RegisterPlayerloopCallback()
        {
            UIElementsRuntimeUtilityNative.RegisterPlayerloopCallback();
        }

        public static void UnregisterPlayerloopCallback()
        {
            UIElementsRuntimeUtilityNative.RegisterPlayerloopCallback();
        }
    }
}
