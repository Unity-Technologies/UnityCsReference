// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.Networking.PlayerConnection;
using UnityEditor.Rendering.Analytics;
using UnityEditorInternal;
using UnityEditorInternal.FrameDebuggerInternal;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine.Pool;
using UnityEngine.Profiling;
using TreeViewState = UnityEditor.IMGUI.Controls.TreeViewState<int>;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    internal partial class FrameDebuggerWindow : EditorWindow
    {
        // Serialized
        [SerializeField] private float m_TreeWidth = FrameDebuggerStyles.Window.k_MinTreeWidth;
        [SerializeField] private TreeViewState m_TreeViewState;

        // Private
        private int m_EnablingWaitCounter = 0;
        private int m_RepaintFrames = k_NeedToRepaintFrames;
        private int m_FrameEventsHash;
        private Rect m_SearchRect;
        private string m_SearchString = String.Empty;
        private IConnectionState m_AttachToPlayerState;
        private FrameDebuggerTreeView m_TreeView;
        private FrameDebuggerEventDetailsView m_EventDetailsView;
        private FrameDebuggerToolbarView m_Toolbar;

        [NoAutoStaticsCleanup] // lifecycle managed by ReleaseGraphicsBuffers (OnDestroy/playModeStateChanged) and [OnCodeUnloading] for code-reload safety
        private static Lazy<GraphicsBuffer> m_ShadingRateLut =
                    new Lazy<GraphicsBuffer>(CreateShadingRateLutGraphicsBuffer);
        internal static GraphicsBuffer shadingRateLut => m_ShadingRateLut.Value;

        static GraphicsBuffer CreateShadingRateLutGraphicsBuffer()
        {
            Color[] bufferData =
            {
                (new Color(0.785f, 0.23f, 0.20f, 1)).linear, (new Color(1.00f, 0.80f, 0.80f, 1)).linear,
                (new Color(0.60f, 0.80f, 1.00f, 1)).linear, Color.black.linear,
                (new Color(0.40f, 0.20f, 0.20f, 1)).linear, (new Color(0.51f, 0.80f, 0.60f, 1)).linear,
                (new Color(0.80f, 1.00f, 0.80f, 1)).linear, Color.black.linear,
                (new Color(0.20f, 0.40f, 0.60f, 1)).linear, (new Color(0.20f, 0.40f, 0.20f, 1)).linear,
                (new Color(0.125f, 0.22f, 0.36f, 1)).linear
            };

            var stride = Marshal.SizeOf(typeof(Color));
            var buf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferData.Length, stride);
            buf.SetData(bufferData);
            return buf;
        }

        private static void ReleaseGraphicsBuffers()
        {
            if (m_ShadingRateLut is { IsValueCreated: true })
            {
                m_ShadingRateLut.Value.Dispose();
            }

            m_ShadingRateLut = null;
            m_ShadingRateLut =
                new Lazy<GraphicsBuffer>(CreateShadingRateLutGraphicsBuffer);
        }

        [OnCodeUnloading]
        private static void OnCodeUnloading()
        {
            ReleaseGraphicsBuffers();
        }

        // Statics
        [AutoStaticsCleanupOnCodeReload]
        private static readonly List<FrameDebuggerWindow> s_FrameDebuggers = new();

        // Constants

        // Sometimes when disabling the frame debugger, the UI does not update automatically -
        // the repaint happens, but we still think there's zero events present
        // (on Mac at least). Haven't figured out why, so whenever changing the
        // enable/limit state, just repaint a couple of times. Yeah...
        private const int k_NeedToRepaintFrames = 4;

        // Properties
        private bool IsDisabledInEditor => !FrameDebugger.enabled;
        private bool IsEnablingFrameDebugger => m_EnablingWaitCounter < k_NeedToRepaintFrames;
        private bool HasEventHashChanged => FrameDebuggerUtility.eventsHash != m_FrameEventsHash;
        
        private readonly Lazy<GUIContent> m_HelpButtonContent = new(() => EditorGUIUtility.TrIconContent("_Help", "Open Manual (in a web browser)"));


        [ShortcutManagement.Shortcut("Analysis/FrameDebugger/Enable")]
        public static void OpenWindowAndToggleEnabled()
        {
            FrameDebuggerWindow frameDebuggerWindow = OpenWindow();
            frameDebuggerWindow.RequestTogglingFrameDebugger();
        }

        [MenuItem("Window/Analysis/Frame Debugger", false, 10)]
        public static FrameDebuggerWindow OpenWindow()
        {
            var wnd = GetWindow(typeof(FrameDebuggerWindow)) as FrameDebuggerWindow;
            wnd.titleContent = EditorGUIUtility.TrTextContent("Frame Debugger");
            wnd.minSize = new Vector2(1000f, 500f);
            return wnd;
        }
        
        void ShowButton(Rect r)
        {
            if (GUI.Button(r, m_HelpButtonContent.Value, EditorStyles.iconButton))
            {
                var url = $"https://docs.unity3d.com/{Help.GetShortReleaseVersion()}/Documentation/Manual/FrameDebugger.html";
                Help.BrowseURL(url);
            }
        }

        internal void RequestTogglingFrameDebugger()
        {
            // Don't toggle immediately - defer it to the next Update call.
            // Reason: OnGUI can be invoked multiple times per frame (e.g., Layout event, then Repaint event).
            // If we toggle during OnGUI, the Layout and Repaint passes might see different widget counts,
            // which causes Unity to throw GUI errors. By deferring to Update, we avoid this issue.
            togglingFrameDebuggerRequested = true;
        }

        internal override void OnResized()
        {
            if (PopupWindowWithoutFocus.IsVisible())
                PopupWindowWithoutFocus.Hide();

            base.OnResized();
        }

        internal void ChangeFrameEventLimit(int newLimit)
        {
            if (newLimit <= 0 || newLimit > FrameDebuggerUtility.count)
                return;

            FrameDebuggerUtility.limit = newLimit;
            m_EventDetailsView?.OnNewFrameEventSelected();
            m_TreeView?.SelectFrameEventIndex(newLimit);
        }

        internal void ChangeFrameEventLimit(int newLimit, FrameDebuggerTreeView.FrameDebuggerTreeViewItem originalItem)
        {
            if (newLimit <= 0 || newLimit > FrameDebuggerUtility.count)
                return;

            FrameDebuggerUtility.limit = newLimit;
            m_EventDetailsView?.OnNewFrameEventSelected();
            m_TreeView?.SelectFrameEventIndex(newLimit);
        }

        internal void OnConnectedProfilerChange()
        {
            DisableFrameDebugger();
            EnableFrameDebugger();
        }

        internal static void RepaintAll()
        {
            foreach (var fd in s_FrameDebuggers)
                fd.Repaint();
        }

        internal void ReselectItemOnCountChange()
        {
            if (m_TreeView == null)
                return;

            m_TreeView.ReselectFrameEventIndex();
        }

        internal void RepaintAllNeededThings()
        {
            // indicate that editor needs a redraw (mostly to get offscreen cameras rendered)
            EditorApplication.SetSceneRepaintDirty();

            // Note: do NOT add GameView.RepaintAll here; that would cause really confusing
            // behaviors when there are offscreen (rendering into RTs) cameras.

            // redraw ourselves
            Repaint();
        }

        internal void DrawSearchField(string str)
        {
            m_SearchString = EditorGUI.ToolbarSearchField(m_SearchRect, str, false);
        }

        private bool togglingFrameDebuggerRequested { get; set; } = false;

        private bool shouldToggleFrameDebugger => togglingFrameDebuggerRequested ||
            (FrameDebugger.enabled && !IsDebuggingAvailable());

        // OnGUI does not always get called, which causes issues when profiling a remote player.
        // We need to call repaint in order to get the data without having to move the mouse
        // and to get things like foldouts to open/collapse normally etc.
        void Update()
        {
            if (shouldToggleFrameDebugger)
            {
                togglingFrameDebuggerRequested = false;

                if (FrameDebugger.enabled)
                    DisableFrameDebugger();
                else
                    EnableFrameDebugger();

                m_RepaintFrames = k_NeedToRepaintFrames;
                Repaint();
            }

            if (IsDisabledInEditor)
                return;

            if (IsEnablingFrameDebugger || m_AttachToPlayerState.connectedToTarget == ConnectionTarget.Player)
                RepaintOnLimitChange();
        }

        private void OnGUI()
        {
            // We always draw the top toolbar with the enable/disable/etc...
            bool repaint = DrawToolBar();

            // If the debugger has not been enabled...
            if (IsDisabledInEditor)
                DrawDisabledFrameDebugger();

            // After first clicking the enable button we need to wait a few frames before starting to draw...
            else if (IsEnablingFrameDebugger)
                HandleEnablingFrameDebugger();

            // Draw the enabled debugger...
            else
                DrawEnabledFrameDebugger(repaint);
        }

        private bool DrawToolBar()
        {
            if (m_Toolbar == null)
                m_Toolbar = new FrameDebuggerToolbarView();

            Profiler.BeginSample("DrawToolbar");
            bool repaint = m_Toolbar.DrawToolbar(this, m_AttachToPlayerState);
            Profiler.EndSample();

            return repaint;
        }

        private void DrawDisabledFrameDebugger()
        {
            GUI.enabled = true;

            string message = FrameDebuggerStyles.EventDetails.k_DescriptionString;
            MessageType messageType = MessageType.Info;

            if (!FrameDebuggerUtility.locallySupported)
            {
                message = (FrameDebuggerHelper.isOnLinuxOpenGL) ? FrameDebuggerStyles.EventDetails.k_WarningLinuxOpenGLMsg : FrameDebuggerStyles.EventDetails.k_WarningMultiThreadedMsg;
                messageType = MessageType.Warning;
            }
            else if (!IsDebuggingAvailable())
            {
                message = m_AttachToPlayerState.connectedToTarget switch
                {
                    ConnectionTarget.Editor => FrameDebuggerStyles.EventDetails.k_PlaymodeViewsErrorStringEditor,
                    ConnectionTarget.Player => FrameDebuggerStyles.EventDetails.k_ErrorInvalidPlayerGUID,
                    _ => string.Empty
                };
                messageType = MessageType.Error;
            }

            EditorGUILayout.HelpBox(message, messageType, true);
        }

        private void HandleEnablingFrameDebugger()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            m_EnablingWaitCounter++;
            if (m_EnablingWaitCounter == k_NeedToRepaintFrames)
            {
                FrameDebuggerEvent[] descs = FrameDebuggerUtility.GetFrameEvents();
                m_TreeView = new FrameDebuggerTreeView(descs, m_TreeViewState, this, new Rect(50, 50, 500, 100));
                m_FrameEventsHash = FrameDebuggerUtility.eventsHash;
                ChangeFrameEventLimit(FrameDebuggerUtility.count);
            }
        }

        int GetAllAvailablePlayModeViews(ref List<PlayModeView> playModeViews)
        {
            if (playModeViews == null)
                throw new ArgumentNullException(nameof(playModeViews));

            var mainPlaymodeView = PlayModeView.GetMainPlayModeView();
            if (mainPlaymodeView != null)
                playModeViews.Add(mainPlaymodeView);

            foreach (var playModeView in PlayModeView.GetAllPlayModeViewWindows())
            {
                if (playModeView != null && !playModeViews.Contains(playModeView))
                    playModeViews.Add(playModeView);
            }

            return playModeViews.Count;
        }

        internal bool GetFirstAvailablePlayModeView(out PlayModeView playModeView)
        {
            playModeView = null;

            using (ListPool<PlayModeView>.Get(out var availablePlaymodeViews))
            {
                if (GetAllAvailablePlayModeViews(ref availablePlaymodeViews) > 0)
                {
                    DockArea da = m_Parent as DockArea;
                    foreach (var view in availablePlaymodeViews)
                    {
                        // valid if FD is not docked OR we found a view not docked with it
                        if (da == null || !da.m_Panes.Contains(view))
                        {
                            playModeView = view;
                            break;
                        }
                    }
                }
            }

            return playModeView != null;
        }

        const int k_RemotePlayerDisconnected = -1;
        internal bool IsDebuggingAvailable()
        {
            return m_AttachToPlayerState.connectedToTarget switch
            {
                ConnectionTarget.Editor => FrameDebuggerUtility.locallySupported && GetFirstAvailablePlayModeView(out _),
                ConnectionTarget.Player => ProfilerDriver.connectedProfiler != k_RemotePlayerDisconnected,
                _ => false
            };
        }

        private bool OpenPlayModeView()
        {
            if (GetFirstAvailablePlayModeView(out var view))
            {
                view.ShowTab();
                return true;
            }

            return false;
        }

        private void DrawEnabledFrameDebugger(bool repaint)
        {
            int oldLimit = FrameDebuggerUtility.limit;
            FrameDebuggerEvent[] descs = FrameDebuggerUtility.GetFrameEvents();

            if (m_AttachToPlayerState.connectedToTarget == ConnectionTarget.Player && descs.Length == 0)
            {
                EditorGUILayout.HelpBox(FrameDebuggerStyles.EventDetails.k_WarningPlayerNotSendingData, MessageType.Warning, true);
                return;
            }

            // captured frame event contents have changed, rebuild the tree data
            if (HasEventHashChanged)
            {
                m_TreeView.m_DataSource.SetEvents(descs);
                m_FrameEventsHash = FrameDebuggerUtility.eventsHash;
            }

            float toolbarHeight = EditorStyles.toolbar.fixedHeight;

            Rect dragRect = new Rect(m_TreeWidth, toolbarHeight, FrameDebuggerStyles.Window.k_ResizerWidth, position.height - toolbarHeight);
            dragRect = EditorGUIUtility.HandleHorizontalSplitter(dragRect, position.width, FrameDebuggerStyles.Window.k_MinTreeWidth, FrameDebuggerStyles.Window.k_MinDetailsWidth);
            m_TreeWidth = dragRect.x;

            // Search area
            m_SearchRect = EditorGUILayout.GetControlRect();
            m_SearchRect.width = m_TreeWidth - 5;
            DrawSearchField(m_SearchString);

            Rect listRect = new Rect(
                0,
                toolbarHeight + m_SearchRect.y,
                m_TreeWidth,
                position.height - toolbarHeight - m_SearchRect.height - 5
            );

            float verticalPadding = maximized ? 4f : 0f;

            Rect currentEventRect = new Rect(
                m_TreeWidth,
                toolbarHeight,
                position.width - m_TreeWidth,
                position.height - toolbarHeight - verticalPadding
            );

            Profiler.BeginSample("DrawTree");
            m_TreeView.m_TreeView.searchString = m_SearchString;
            m_TreeView.DrawTree(listRect);
            Profiler.EndSample();

            EditorGUIUtility.DrawHorizontalSplitter(dragRect);

            Profiler.BeginSample("DrawEventDetails");
            m_EventDetailsView.DrawEventDetails(currentEventRect, descs, m_AttachToPlayerState.connectedToTarget == ConnectionTarget.Editor);
            Profiler.EndSample();

            if (repaint || oldLimit != FrameDebuggerUtility.limit)
                RepaintOnLimitChange();

            if (m_RepaintFrames > 0)
            {
                m_TreeView.SelectFrameEventIndex(FrameDebuggerUtility.limit);
                RepaintAllNeededThings();
                --m_RepaintFrames;
            }
        }

        private void OnDidOpenScene()
        {
            DisableFrameDebugger();
        }

        private void OnPauseStateChanged(PauseState state)
        {
            RepaintOnLimitChange();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            RepaintOnLimitChange();
            ReleaseGraphicsBuffers();
        }

        private void OnEnable()
        {
            if (m_AttachToPlayerState == null)
                m_AttachToPlayerState = PlayerConnectionGUIUtility.GetConnectionState(this);

            wantsLessLayoutEvents = true;
            autoRepaintOnSceneChange = true;
            s_FrameDebuggers.Add(this);
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            m_RepaintFrames = k_NeedToRepaintFrames;

            GraphicsToolLifetimeAnalytic.WindowOpened<FrameDebuggerWindow>();
        }

        private void OnDisable()
        {
            m_AttachToPlayerState?.Dispose();
            m_AttachToPlayerState = null;

            s_FrameDebuggers.Remove(this);
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            DisableFrameDebugger();

            GraphicsToolLifetimeAnalytic.WindowClosed<FrameDebuggerWindow>();
        }

        private void OnDestroy()
        {
            ReleaseGraphicsBuffers();
        }


        private void EnableFrameDebugger()
        {
            if (FrameDebugger.enabled)
                return;

            if (!IsDebuggingAvailable())
                return;

            if (m_AttachToPlayerState.connectedToTarget == ConnectionTarget.Editor)
            {
                if (!OpenPlayModeView())
                    return;

                // pause play mode if needed
                if (EditorApplication.isPlaying && !EditorApplication.isPaused)
                    EditorApplication.isPaused = true;
            }

            FrameDebuggerUtility.SetEnabled(true, ProfilerDriver.connectedProfiler);

            m_TreeViewState ??= new TreeViewState();
            m_EventDetailsView ??= new FrameDebuggerEventDetailsView(this);

            m_EnablingWaitCounter = 0;
            m_EventDetailsView.Reset();
            RepaintOnLimitChange();

            GraphicsToolUsageAnalytic.ActionPerformed<FrameDebuggerWindow>(nameof(EnableFrameDebugger), Array.Empty<string>());
        }

        private void DisableFrameDebugger()
        {
            if (!FrameDebugger.enabled)
                return;

            // if it was true before, we disabled and ask the game scene to repaint
            if (FrameDebugger.IsLocalEnabled())
                EditorApplication.SetSceneRepaintDirty();

            FrameDebuggerUtility.SetEnabled(false, FrameDebuggerUtility.GetRemotePlayerGUID());

            if (m_EventDetailsView != null)
            {
                m_EventDetailsView.OnDisable();
                m_EventDetailsView = null;
            }

            FrameDebuggerStyles.OnDisable();
            m_TreeViewState = null;
            m_TreeView = null;

            GraphicsToolUsageAnalytic.ActionPerformed<FrameDebuggerWindow>(nameof(DisableFrameDebugger), Array.Empty<string>());
        }

        internal void RepaintOnLimitChange()
        {
            m_RepaintFrames = k_NeedToRepaintFrames;
            RepaintAllNeededThings();
        }
    }
}
