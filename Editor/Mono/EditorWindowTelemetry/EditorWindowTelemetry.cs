// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Profiling;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.MPE;
using UnityEngine;

namespace UnityEditor
{
    internal static partial class EditorWindowTelemetry
    {
        const string k_UsageSessionStateKey = "EditorWindowTelemetry.Usage";
        internal const int k_SamplePeriodSeconds = 5;
        const int k_SamplePeriodMs = k_SamplePeriodSeconds * 1000;

        const int k_ActiveSamplesPerEvent = 15 * 60 / k_SamplePeriodSeconds;
        const int k_MaxContainerWindows = 128;

        static readonly ProfilerMarker k_SampleMarker = new ProfilerMarker("EditorWindowTelemetry.Sample");

        [AutoStaticsCleanupOnCodeReload]
        static readonly WindowSampleTracker s_Tracker = new WindowSampleTracker();

        [AutoStaticsCleanupOnCodeReload]
        static int s_LastSampleTick;

        [AutoStaticsCleanupOnCodeReload]
        static EditorWindow s_LastFocusedWindow;

        internal static WindowSampleTracker tracker => s_Tracker;

        internal static bool isEditorActive => EditorApplication.isFocused && EditorAnalytics.isUserActive;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            if (!EditorAnalytics.enabled || ProcessService.level != ProcessLevel.Main || AssetDatabase.IsAssetImportWorkerProcess())
                return;

            s_LastSampleTick = Environment.TickCount;

            // Seeded so a domain reload does not count the focused window as a gain.
            s_LastFocusedWindow = EditorWindow.focusedWindow;
            Restore();

            EditorApplication.update += OnUpdate;
            EditorWindow.windowFocusChanged += OnWindowFocusChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Persist;
            EditorApplication.wantsToQuit += OnWantsToQuit;
        }

        static void OnUpdate()
        {
            var tick = Environment.TickCount;
            if (unchecked(tick - s_LastSampleTick) < k_SamplePeriodMs)
                return;

            s_LastSampleTick = tick;
            if (!isEditorActive)
                return;

            Sample();
            if (s_Tracker.activeSamples >= k_ActiveSamplesPerEvent)
                Send();
        }

        // Editor analytics stops accepting events before EditorApplication.quitting runs.
        static bool OnWantsToQuit()
        {
            Send();
            return true;
        }

        internal static void Send()
        {
            if (s_Tracker.activeSamples > 0)
            {
                EditorAnalytics.SendAnalytic(
                    new EditorWindowUsageAnalytic(s_Tracker.Snapshot(), s_Tracker.activeSamples, s_Tracker.playModeSamples));
            }

            s_Tracker.Clear();
        }

        internal static void Sample()
        {
            using (k_SampleMarker.Auto())
            {
                s_Tracker.BeginSample(EditorApplication.isPlayingOrWillChangePlaymode);

                var focused = EditorWindow.focusedWindow;
                // TODO: read the containers from the managed WindowManager once it exists, and remove GetWindowEntityIds.
                Span<EntityId> ids = stackalloc EntityId[k_MaxContainerWindows];
                var count = ContainerWindow.GetWindowEntityIds(ids);
                for (int i = 0; i < count; ++i)
                {
                    if (EditorUtility.EntityIdToObject(ids[i]) is ContainerWindow container && !IsTransient(container.showMode))
                        WalkView(container.rootView, container.minimized, focused);
                }
            }
        }

        static bool IsTransient(ShowMode showMode)
        {
            return showMode == ShowMode.PopupMenu || showMode == ShowMode.Tooltip || showMode == ShowMode.NoShadow
                || showMode == ShowMode.ModalUtility;
        }

        static void WalkView(View view, bool minimized, EditorWindow focused)
        {
            if (view == null)
                return;

            if (view is DockArea dockArea)
            {
                var panes = dockArea.m_Panes;
                var front = dockArea.actualView;
                for (int i = 0; i < panes.Count; ++i)
                {
                    var pane = panes[i];
                    if (pane != null)
                        CountEditorWindow(pane, !minimized && pane == front, focused);
                }
            }
            else if (view is HostView hostView)
            {
                var window = hostView.actualView;
                if (window != null)
                    CountEditorWindow(window, !minimized, focused);
            }

            var children = view.children;
            for (int i = 0; i < children.Length; ++i)
            {
                WalkView(children[i], minimized, focused);
            }
        }

        static bool IsCounted(EditorWindow window)
        {
            return !(window is MainToolbarWindow);
        }

        static bool IsTracked(EditorWindow window)
        {
            var container = window.m_Parent != null ? window.m_Parent.window : null;
            return container != null && !IsTransient(container.showMode) && IsCounted(window);
        }

        static void CountEditorWindow(EditorWindow window, bool visible, EditorWindow focused)
        {
            if (IsCounted(window))
                s_Tracker.CountInstance(WindowIdentityResolver.Resolve(window.GetType()), visible, window == focused);
        }

        static void OnWindowFocusChanged()
        {
            RecordFocusChange(isEditorActive);
        }

        internal static void RecordFocusChange(bool editorActive)
        {
            var focused = EditorWindow.focusedWindow;

            // Returning from a popup or the toolbar is not a new focus gain.
            if (focused == s_LastFocusedWindow || (focused != null && !IsTracked(focused)))
                return;

            s_LastFocusedWindow = focused;
            if (focused != null && editorActive)
                s_Tracker.AddFocusGain(WindowIdentityResolver.Resolve(focused.GetType()), EditorApplication.isPlayingOrWillChangePlaymode);
        }

        internal static void Persist()
        {
            SessionState.SetString(k_UsageSessionStateKey, s_Tracker.ToJson());
        }

        internal static void Restore()
        {
            var json = SessionState.GetString(k_UsageSessionStateKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
                s_Tracker.FromJson(json);
        }
    }
}
