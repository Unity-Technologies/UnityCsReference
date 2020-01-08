// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Profiling;

namespace UnityEngine.UIElements
{
    // This is the required interface to UIElementsUtility for Runtime game components.
    [NativeHeader("Modules/UIElements/UIElementsRuntimeUtility.h")]
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

        [RequiredByNativeCode]
        public static void RepaintOverlayPanels()
        {
            UIElementsUtility.GetAllPanels(panelsIteration, ContextType.Player);
            foreach (var panel in panelsIteration)
            {
                // at the moment, all runtime panels who do not use a rendertexure are rendered as overlays.
                // later on, they'll be filtered based on render mode
                if ((panel as RuntimePanel).targetTexture == null)
                {
                    using (s_RepaintProfilerMarker.Auto())
                        panel.Repaint(Event.current);
                    (panel.panelDebug?.debuggerOverlayPanel as Panel)?.Repaint(Event.current);
                }
            }
        }

        public extern static void RegisterPlayerloopCallback();
        public extern static void UnregisterPlayerloopCallback();
    }
}
