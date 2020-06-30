using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements
{
    internal static class UIElementsRuntimeUtility
    {
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
            // Uncomment when the PR lands in trunk, and remove Panel.Update reference from PanelSettings/PanelRenderer
            //UIElementsRuntimeUtilityNative.UpdateOverlayPanelsCallback = UpdateOverlayPanels;
        }

        public static EventBase CreateEvent(Event systemEvent)
        {
            return UIElementsUtility.CreateEvent(systemEvent, systemEvent.rawType);
        }

        public delegate BaseRuntimePanel CreateRuntimePanelDelegate(ScriptableObject ownerObject);

        public static BaseRuntimePanel FindOrCreateRuntimePanel(ScriptableObject ownerObject,
            CreateRuntimePanelDelegate createDelegate)
        {
            if (UIElementsUtility.TryGetPanel(ownerObject.GetInstanceID(), out Panel cachedPanel))
            {
                if (cachedPanel is BaseRuntimePanel runtimePanel)
                    return runtimePanel;
                RemoveCachedPanelInternal(ownerObject.GetInstanceID()); // Maybe throw exception instead?
            }

            var panel = createDelegate(ownerObject);
            panel.IMGUIEventInterests = new EventInterests {wantsMouseMove = true, wantsMouseEnterLeaveWindow = true};
            RegisterCachedPanelInternal(ownerObject.GetInstanceID(), panel);
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

        private static void RegisterCachedPanelInternal(int instanceID, IPanel panel)
        {
            UIElementsUtility.RegisterCachedPanel(instanceID, panel as Panel);
            s_PanelOrderingDirty = true;
            if (!s_RegisteredPlayerloopCallback)
            {
                s_RegisteredPlayerloopCallback = true;
                RegisterPlayerloopCallback();
            }
        }

        private static void RemoveCachedPanelInternal(int instanceID)
        {
            UIElementsUtility.RemoveCachedPanel(instanceID);

            s_PanelOrderingDirty = true;

            // We don't call GetSortedPanels() here to avoid always sorting when we remove multiple panels in a row
            // the ordering is dirty anyways, it will eventually get recreated
            s_SortedRuntimePanels.Clear();
            UIElementsUtility.GetAllPanels(s_SortedRuntimePanels, ContextType.Player);

            // un-register the playerloop callback as the last panel gets un-registered
            if (s_SortedRuntimePanels.Count == 0)
            {
                s_RegisteredPlayerloopCallback = false;
                UnregisterPlayerloopCallback();
            }
        }

        static List<Panel> s_SortedRuntimePanels = new List<Panel>();
        private static bool s_PanelOrderingDirty = true;

        internal static readonly string s_RepaintProfilerMarkerName = "UIElementsRuntimeUtility.DoDispatch(Repaint Event)";
        private static readonly ProfilerMarker s_RepaintProfilerMarker = new ProfilerMarker(s_RepaintProfilerMarkerName);

        public static void RepaintOverlayPanels()
        {
            foreach (BaseRuntimePanel panel in GetSortedPlayerPanels())
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

        public static void UpdateOverlayPanels()
        {
            foreach (BaseRuntimePanel panel in GetSortedPlayerPanels())
            {
                panel.Update();
            }
        }

        public static void RegisterPlayerloopCallback()
        {
            UIElementsRuntimeUtilityNative.RegisterPlayerloopCallback();
        }

        public static void UnregisterPlayerloopCallback()
        {
            UIElementsRuntimeUtilityNative.UnregisterPlayerloopCallback();
        }

        internal static void SetPanelOrderingDirty()
        {
            s_PanelOrderingDirty = true;
        }

        internal static List<Panel> GetSortedPlayerPanels()
        {
            if (s_PanelOrderingDirty)
                SortPanels();
            return s_SortedRuntimePanels;
        }

        static void SortPanels()
        {
            s_SortedRuntimePanels.Clear();
            UIElementsUtility.GetAllPanels(s_SortedRuntimePanels, ContextType.Player);

            s_SortedRuntimePanels.Sort((a, b) =>
            {
                var diff = a.sortingPriority - b.sortingPriority;

                if (Mathf.Approximately(0, diff))
                {
                    return 0;
                }

                return (diff < 0) ? -1 : 1;
            });

            s_PanelOrderingDirty = false;
        }
    }
}
