// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    static class UIElementsRuntimeUtility
    {
        public static event Action<BaseRuntimePanel> onCreatePanel;

        static UIElementsRuntimeUtility()
        {
            Canvas.externBeginRenderOverlays = BeginRenderOverlays;
            Canvas.externRenderOverlaysBefore = (displayIndex, sortOrder) => RenderOverlaysBeforePriority(displayIndex, sortOrder);
            Canvas.externEndRenderOverlays = EndRenderOverlays;
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
            onCreatePanel?.Invoke(panel);
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
                Canvas.SetExternalCanvasEnabled(true);
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
                Canvas.SetExternalCanvasEnabled(false);
            }
        }

        static List<Panel> s_SortedRuntimePanels = new List<Panel>();
        private static bool s_PanelOrderingDirty = true;

        internal static int s_ResolvedSortingIndexMax = 0;

        public static void RenderOffscreenPanels()
        {
            var oldCam = Camera.current;
            var oldRT = RenderTexture.active;

            foreach (BaseRuntimePanel panel in GetSortedPlayerPanels())
            {
                if (!panel.drawsInCameras && panel.targetTexture != null)
                {
                    // We don't want the state to be restored immediately because the next panel might be rendering
                    // to the same render texture
                    RenderPanel(panel, false);
                }
            }

            Camera.SetupCurrent(oldCam);
            RenderTexture.active = oldRT;
        }

        public static void RepaintPanel(BaseRuntimePanel panel)
        {
            var oldCam = Camera.current;
            var oldRT = RenderTexture.active;

            panel.Repaint(Event.current);

            Camera.SetupCurrent(oldCam);
            RenderTexture.active = oldRT;
        }

        public static void RenderPanel(BaseRuntimePanel panel, bool restoreState = true)
        {
            // Panel must NOT have drawsInCameras set. Such panels are drawn by the render nodes.
            Debug.Assert(!panel.drawsInCameras);

            var oldCam = Camera.current;
            var oldRT = RenderTexture.active;

            panel.Render();

            if (!panel.drawsInCameras && restoreState)
            {
                Camera.SetupCurrent(oldCam);
                RenderTexture.active = oldRT;
            }
        }

        private static int currentOverlayIndex = -1;
        internal static void BeginRenderOverlays(int displayIndex)
        {
            currentOverlayIndex = 0;
        }

        internal static void RenderOverlaysBeforePriority(int displayIndex, float maxPriority)
        {
            if (currentOverlayIndex < 0)
                return;

            var runTimePanels = GetSortedPlayerPanels();

            for (; currentOverlayIndex < runTimePanels.Count; ++currentOverlayIndex)
            {
                if (runTimePanels[currentOverlayIndex] is BaseRuntimePanel p)
                {
                    if (p.sortingPriority >= maxPriority)
                        return;

                    if (p.targetDisplay == displayIndex && !p.drawsInCameras && p.targetTexture == null)
                        RenderPanel(p);
                }
            }
        }

        internal static void EndRenderOverlays(int displayIndex)
        {
            RenderOverlaysBeforePriority(displayIndex, float.MaxValue);
            currentOverlayIndex = -1;
        }

        public static void RepaintPanels(bool onlyOffscreen)
        {
            foreach (BaseRuntimePanel panel in GetSortedPlayerPanels())
            {
                if (!onlyOffscreen || panel.targetTexture != null)
                    RepaintPanel(panel);
            }
        }

        internal static Object activeEventSystem { get; private set; }
        internal static bool useDefaultEventSystem => activeEventSystem == null;

        private static bool s_IsPlayMode = false;

        public static void RegisterEventSystem(Object eventSystem)
        {
            if (activeEventSystem != null && activeEventSystem != eventSystem && eventSystem.GetType().Name == "EventSystem")
                Debug.LogWarning("There can be only one active Event System.");
            activeEventSystem = eventSystem;
        }

        public static void UnregisterEventSystem(Object eventSystem)
        {
            if (activeEventSystem == eventSystem)
                activeEventSystem = null;
        }

        private static DefaultEventSystem s_DefaultEventSystem;
        internal static DefaultEventSystem defaultEventSystem =>
            s_DefaultEventSystem ?? (s_DefaultEventSystem = new DefaultEventSystem());

        public static void UpdatePanels()
        {
            RemoveUnusedPanels();
            UIRenderDevice.ProcessDeviceFreeQueue();

            foreach (BaseRuntimePanel panel in GetSortedPlayerPanels())
            {
                panel.Update();
            }

            if (s_IsPlayMode)
            {
                if (useDefaultEventSystem)
                {
                    defaultEventSystem.isInputReady = true;
                    defaultEventSystem.Update(DefaultEventSystem.UpdateMode.IgnoreIfAppNotFocused);
                }
                else if (s_DefaultEventSystem != null)
                {
                    s_DefaultEventSystem.isInputReady = false;
                }
            }
        }

        internal static void MarkPotentiallyEmpty(PanelSettings settings)
        {
            if (!s_PotentiallyEmptyPanelSettings.Contains(settings))
                s_PotentiallyEmptyPanelSettings.Add(settings);
        }

        private static List<PanelSettings> s_PotentiallyEmptyPanelSettings = new List<PanelSettings>();
        internal static void RemoveUnusedPanels()
        {
            foreach (PanelSettings psetting in s_PotentiallyEmptyPanelSettings)
            {
                var m_AttachedUIDocumentsList = psetting.m_AttachedUIDocumentsList;
                if (m_AttachedUIDocumentsList == null || m_AttachedUIDocumentsList.m_AttachedUIDocuments.Count == 0)
                {
                    // The runtime panel is unused, dispose it immediately as we dont want any side effect of keeping the panel alive.
                    // It'll be recreated if it's used again.
                    psetting.DisposePanel();
                }
            }
            s_PotentiallyEmptyPanelSettings.Clear();

            // This check is necessary because neither OnDisable or OnDestroy are called when deleting an asset
            // from the project browser.
            List<PanelSettings> toDispose = null;
            foreach (var panel in GetSortedPlayerPanels())
            {
                if (!panel.ownerObject && panel.ownerObject is PanelSettings psettings)
                    (toDispose ??= new()).Add(psettings);
            }
            if (toDispose != null)
            {
                foreach (var psettings in toDispose)
                    psettings.DisposePanel();
            }
        }

        public static void RegisterPlayerloopCallback()
        {
            UIElementsRuntimeUtilityNative.RegisterPlayerloopCallback();
            UIElementsRuntimeUtilityNative.UpdatePanelsCallback = UpdatePanels;
            UIElementsRuntimeUtilityNative.RepaintPanelsCallback = RepaintPanels;
            UIElementsRuntimeUtilityNative.RenderOffscreenPanelsCallback = RenderOffscreenPanels;
        }

        public static void UnregisterPlayerloopCallback()
        {
            UIElementsRuntimeUtilityNative.UnregisterPlayerloopCallback();
            UIElementsRuntimeUtilityNative.UpdatePanelsCallback = null;
            UIElementsRuntimeUtilityNative.RepaintPanelsCallback = null;
            UIElementsRuntimeUtilityNative.RenderOffscreenPanelsCallback = null;

            if (s_DefaultEventSystem != null)
                s_DefaultEventSystem.isInputReady = false;
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
                var runtimePanelA = a as BaseRuntimePanel;
                var runtimePanelB = b as BaseRuntimePanel;

                if (runtimePanelA == null || runtimePanelB == null)
                {
                    // Should never happen, so just being safe (after all there's a cast happening).
                    return 0;
                }

                var diff = runtimePanelA.sortingPriority - runtimePanelB.sortingPriority;

                if (Mathf.Approximately(0, diff))
                {
                    // They're the same value, compare their count (panels created first show up first).
                    return runtimePanelA.m_RuntimePanelCreationIndex.CompareTo(runtimePanelB.m_RuntimePanelCreationIndex);
                }

                return (diff < 0) ? -1 : 1;
            });

            for (var i = 0; i < s_SortedRuntimePanels.Count; i++)
            {
                var runtimePanel = s_SortedRuntimePanels[i] as BaseRuntimePanel;
                if (runtimePanel != null)
                    runtimePanel.resolvedSortingIndex = i;
            }
            s_ResolvedSortingIndexMax = s_SortedRuntimePanels.Count - 1;

            s_PanelOrderingDirty = false;
        }

        internal static Vector2 MultiDisplayBottomLeftToPanelPosition(Vector2 position, out int? targetDisplay)
        {
            var screenPosition = MultiDisplayToLocalScreenPosition(position, out targetDisplay);
            return ScreenBottomLeftToPanelPosition(screenPosition, targetDisplay ?? 0);
        }

        internal static Vector2 MultiDisplayToLocalScreenPosition(Vector2 position, out int? targetDisplay)
        {
            var relativePosition = Display.RelativeMouseAt(position);
            if (relativePosition != Vector3.zero)
            {
                targetDisplay = (int)relativePosition.z;
                return relativePosition;
            }
            targetDisplay = Display.activeEditorGameViewTarget;
            return position;
        }

        internal static Vector2 ScreenBottomLeftToPanelPosition(Vector2 position, int targetDisplay)
        {
            // Flip positions Y axis between input and UITK
            var screenHeight = Screen.height;
            if (targetDisplay > 0 && targetDisplay < Display.displays.Length)
                screenHeight = Display.displays[targetDisplay].systemHeight;
            position.y = screenHeight - position.y;
            return position;
        }

        internal static Vector2 ScreenBottomLeftToPanelDelta(Vector2 delta)
        {
            // Flip deltas Y axis between input and UITK
            delta.y = -delta.y;
            return delta;
        }

        // Don't rely on Application.isPlaying because its value is true for a few extra frames
        // where some objects are not created yet or already destroyed.
        internal static void OnEnteredPlayMode()
        {
            s_IsPlayMode = true;
        }

        internal static void OnExitingPlayMode()
        {
            s_IsPlayMode = false;

            if (s_DefaultEventSystem != null)
                s_DefaultEventSystem.isInputReady = false;
        }
    }
}
