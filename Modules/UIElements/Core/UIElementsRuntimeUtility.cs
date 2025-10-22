// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.UIElements.Layout;
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

            UIElementsRuntimeUtilityNative.SetUpdateCallback(UpdatePanels);
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

        private static void GetPlayerPanelsByRenderMode(List<BaseRuntimePanel> outScreenSpaceOverlayPanels, List<BaseRuntimePanel> outWorldSpacePanels)
        {
            using (Pool.ListPool<Panel>.Get(out var panels))
            {
                UIElementsUtility.GetAllPanels(panels, ContextType.Player);
                foreach (var panel in panels)
                {
                    if (!(panel is BaseRuntimePanel runtimePanel))
                        continue;

                    if (runtimePanel.drawsInCameras)
                        outWorldSpacePanels.Add(runtimePanel);
                    else
                        outScreenSpaceOverlayPanels.Add(runtimePanel);
                }
            }
        }

        private static bool s_RegisteredPlayerloopCallback = false;

        private static void RegisterCachedPanelInternal(int instanceID, IPanel panel)
        {
            UIElementsUtility.RegisterCachedPanel(instanceID, panel as Panel);
            s_PanelOrderingOrDrawInCameraDirty = true;
            if (!s_RegisteredPlayerloopCallback)
            {
                s_RegisteredPlayerloopCallback = true;
                EnableRenderingAndInputCallbacks();
                Canvas.SetExternalCanvasEnabled(true);
            }
        }

        private static void RemoveCachedPanelInternal(int instanceID)
        {
            UIElementsUtility.RemoveCachedPanel(instanceID);

            s_PanelOrderingOrDrawInCameraDirty = true;

            // We don't call GetSortedPanels() here to avoid always sorting when we remove multiple panels in a row
            // the ordering is dirty anyways, it will eventually get recreated
            using (Pool.ListPool<Panel>.Get(out var panels))
            {
                UIElementsUtility.GetAllPanels(panels, ContextType.Player);

                // un-register the playerloop callback as the last panel gets un-registered
                if (panels.Count == 0)
                {
                    SortPanels(); // Clear the cached lists
                    s_RegisteredPlayerloopCallback = false;
                    DisableRenderingAndInputCallbacks();
                    Canvas.SetExternalCanvasEnabled(false);
                }
            }
        }

        private static readonly List<BaseRuntimePanel> s_SortedScreenOverlayPanels = new();
        private static readonly List<BaseRuntimePanel> s_CachedWorldSpacePanels = new();
        private static readonly List<BaseRuntimePanel> s_SortedPlayerPanels = new();
        private static bool s_PanelOrderingOrDrawInCameraDirty = true;
        internal static int s_ResolvedSortingIndexMax = 0;

        public static void RenderOffscreenPanels()
        {
            var oldCam = Camera.current;
            var oldRT = RenderTexture.active;

            foreach (var panel in GetSortedScreenOverlayPlayerPanels())
            {
                if (panel.targetTexture != null)
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

            var overlayPanels = GetSortedScreenOverlayPlayerPanels();

            for (; currentOverlayIndex < overlayPanels.Count; ++currentOverlayIndex)
            {
                var overlayPanel = overlayPanels[currentOverlayIndex];
                if (overlayPanel.sortingPriority >= maxPriority)
                    return;

                if (overlayPanel.targetDisplay == displayIndex && overlayPanel.targetTexture == null)
                {
                    RenderPanel(overlayPanel);
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
        internal static bool useDefaultEventSystem => overrideUseDefaultEventSystem ?? activeEventSystem == null;
        internal static bool? overrideUseDefaultEventSystem { get; set; }
        internal static bool autoUpdateEventSystem { get; set; } = true;

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

            if (LayoutManager.IsSharedManagerCreated)
            {
                // This is already called in the Editor loop (UIElementsUtility) but we also need this in the Player
                // When profiling in Editor this may not show accurate results however
                LayoutManager.SharedManager.Collect();
            }

            List<BaseRuntimePanel> sortedPlayerPanels = GetSortedPlayerPanels();

            // Early out to skip the loop below, and to avoid an Input Update when there are no panels
            if (sortedPlayerPanels.Count == 0)
                return;

            // Update panels from back to front. World space first, then screen overlay.
			foreach (BaseRuntimePanel panel in sortedPlayerPanels)
            {
                panel.Update();
            }

            UpdateEventSystem();
        }

        internal static void UpdateEventSystem()
        {
            if (s_IsPlayMode)
            {
                if (useDefaultEventSystem)
                {
                    defaultEventSystem.isInputReady = true;

                    if (autoUpdateEventSystem)
                    {
                        defaultEventSystem.Update(DefaultEventSystem.UpdateMode.IgnoreIfAppNotFocused);
                    }
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

        public static void EnableRenderingAndInputCallbacks()
        {
            UIElementsRuntimeUtilityNative.SetRenderingCallbacks(RepaintPanels, RenderOffscreenPanels);
        }

        public static void DisableRenderingAndInputCallbacks()
        {
            UIElementsRuntimeUtilityNative.UnsetRenderingCallbacks();

            if (s_DefaultEventSystem != null)
                s_DefaultEventSystem.isInputReady = false;
        }

        internal static void SetPanelOrderingDirty()
        {
            s_PanelOrderingOrDrawInCameraDirty = true;
        }

        internal static void SetPanelsDrawInCameraDirty()
        {
            s_PanelOrderingOrDrawInCameraDirty = true;
        }

        internal static List<BaseRuntimePanel> GetWorldSpacePlayerPanels()
        {
            if (s_PanelOrderingOrDrawInCameraDirty)
                SortPanels();
            return s_CachedWorldSpacePanels;
        }

        public static List<BaseRuntimePanel> GetSortedScreenOverlayPlayerPanels()
        {
            if (s_PanelOrderingOrDrawInCameraDirty)
                SortPanels();
            return s_SortedScreenOverlayPanels;
        }

        public static List<BaseRuntimePanel> GetSortedPlayerPanels()
        {
            if (s_PanelOrderingOrDrawInCameraDirty)
                SortPanels();
            return s_SortedPlayerPanels;
        }

        // For unit tests
        internal static List<IPanel> GetSortedPlayerPanelsInternal()
        {
            List<IPanel> outPanels = new ();
            foreach (var panel in GetSortedPlayerPanels())
                outPanels.Add(panel);
            return outPanels;
        }

        private static void SortPanels()
        {
            s_SortedScreenOverlayPanels.Clear();
            s_CachedWorldSpacePanels.Clear();
            GetPlayerPanelsByRenderMode(s_SortedScreenOverlayPanels, s_CachedWorldSpacePanels);

            s_SortedScreenOverlayPanels.Sort((runtimePanelA, runtimePanelB) =>
            {
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

            for (var i = 0; i < s_SortedScreenOverlayPanels.Count; i++)
            {
                var runtimePanel = s_SortedScreenOverlayPanels[i];
                runtimePanel.resolvedSortingIndex = i;
            }
            s_ResolvedSortingIndexMax = s_SortedScreenOverlayPanels.Count - 1;

            // Update panels from back to front. World space first, then screen overlay.
            s_SortedPlayerPanels.Clear();
            foreach (var panel in s_CachedWorldSpacePanels)
                s_SortedPlayerPanels.Add(panel);
            foreach (var panel in s_SortedScreenOverlayPanels)
                s_SortedPlayerPanels.Add(panel);

            s_PanelOrderingOrDrawInCameraDirty = false;
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
            return FlipY(position, GetRuntimeDisplayHeight(targetDisplay));
        }

        internal static Vector2 ScreenBottomLeftToPanelDelta(Vector2 delta)
        {
            // Flip deltas Y axis between input and UITK
            return FlipDeltaY(delta);
        }

        internal static Vector2 PanelToScreenBottomLeftPosition(Vector2 panelPosition, int targetDisplay)
        {
            // Flip positions Y axis between input and UITK
            return FlipY(panelPosition, GetRuntimeDisplayHeight(targetDisplay));
        }

        internal static Vector2 FlipY(Vector2 p, float displayHeight)
        {
            p.y = displayHeight - p.y;
            return p;
        }

        private static Vector2 FlipDeltaY(Vector2 delta)
        {
            delta.y = -delta.y;
            return delta;
        }

        private static float GetRuntimeDisplayHeight(int targetDisplay)
        {
            if (targetDisplay > 0 && targetDisplay < Display.displays.Length)
                return Display.displays[targetDisplay].systemHeight;

            return Screen.height;
        }

        // Seems to not work well if used in unit tests, e.g. MacEditor Arm64 EventSystemTests.ClickEventIsSent
        internal static float GetEditorDisplayHeight(int targetDisplay)
        {
            var gameViewResolution = PanelSettings.GetGameViewResolution(targetDisplay);
            if (gameViewResolution.HasValue)
                return gameViewResolution.Value.y;
            return GetRuntimeDisplayHeight(targetDisplay);
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
