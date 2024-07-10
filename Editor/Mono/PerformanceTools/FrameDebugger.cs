// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Networking.PlayerConnection;

using UnityEditorInternal;
using UnityEditorInternal.FrameDebuggerInternal;

using UnityEditor.IMGUI.Controls;
using UnityEditor.Networking.PlayerConnection;
using UnityEditor.Rendering.Analytics;

namespace UnityEditor
{
    internal class FrameDebuggerWindow : EditorWindow
    {
        // Serialized
        [SerializeField] private float m_TreeWidth = FrameDebuggerStyles.Window.k_MinTreeWidth;
        [SerializeField] private TreeViewState m_TreeViewState;

        // Private
        private int m_EnablingWaitCounter = 0;
        private int m_RepaintFrames = k_NeedToRepaintFrames;
        private int m_FrameEventsHash;
        private bool m_ShowTabbedErrorBox;
        private bool m_HasOpenedPlaymodeView;
        private Rect m_SearchRect;
        private string m_SearchString = String.Empty;
        private IConnectionState m_AttachToPlayerState;
        private FrameDebuggerTreeView m_TreeView;
        private FrameDebuggerEventDetailsView m_EventDetailsView;
        private FrameDebuggerToolbarView m_Toolbar;

        // Statics
        private static List<FrameDebuggerWindow> s_FrameDebuggers = new List<FrameDebuggerWindow>();

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


        [ShortcutManagement.Shortcut("Analysis/FrameDebugger/Enable")]
        public static void OpenWindowAndToggleEnabled()
        {
            FrameDebuggerWindow frameDebuggerWindow = OpenWindow();
            frameDebuggerWindow.ToggleFrameDebuggerEnabled();
        }

        [MenuItem("Window/Analysis/Frame Debugger", false, 10)]
        public static FrameDebuggerWindow OpenWindow()
        {
            var wnd = GetWindow(typeof(FrameDebuggerWindow)) as FrameDebuggerWindow;
            wnd.titleContent = EditorGUIUtility.TrTextContent("Frame Debugger");
            wnd.minSize = new Vector2(1000f, 500f);
            return wnd;
        }

        internal void ToggleFrameDebuggerEnabled()
        {
            if (FrameDebugger.enabled)
                DisableFrameDebugger();
            else
                EnableFrameDebugger();
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

        // OnGUI does not always get called, which causes issues when profiling a remote player.
        // We need to call repaint in order to get the data without having to move the mouse
        // and to get things like foldouts to open/collapse normally etc.
        void Update()
        {
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
            if (!FrameDebuggerUtility.locallySupported)
            {
                string warningMessage = (FrameDebuggerHelper.isOnLinuxOpenGL) ? FrameDebuggerStyles.EventDetails.k_WarningLinuxOpenGLMsg : FrameDebuggerStyles.EventDetails.k_WarningMultiThreadedMsg;
                EditorGUILayout.HelpBox(warningMessage, MessageType.Warning, true);
            }

            EditorGUILayout.HelpBox(FrameDebuggerStyles.EventDetails.k_DescriptionString, MessageType.Info, true);

            if (m_ShowTabbedErrorBox)
                EditorGUILayout.HelpBox(FrameDebuggerStyles.EventDetails.k_TabbedWithPlaymodeErrorString, MessageType.Error, true);
        }

        private void HandleEnablingFrameDebugger()
        {
            // Make sure the PlayMode window is enabled and shown...
            if (!OpenPlayModeView())
                return;

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

        private bool CheckIfFDIsDockedWithGameWindow(DockArea da, PlayModeView gameWindow)
        {
            for (int i = 0; i < da.m_Panes.Count; i++)
                if (gameWindow == da.m_Panes[i])
                    return true;
            return false;
        }

        private bool OpenPlayModeView()
        {
            if (m_HasOpenedPlaymodeView)
                return true;

            // When debugging remote players, we can ignore this check as it doesn't render to the Game Window.
            if (!FrameDebugger.IsLocalEnabled() && m_AttachToPlayerState.connectedToTarget != ConnectionTarget.Editor)
                return true;

            PlayModeView mainGameWindow = PlayModeView.GetMainPlayModeView();
            List<PlayModeView> allGameWindows = PlayModeView.GetAllPlayModeViewWindows();
            if (mainGameWindow || allGameWindows.Count > 0)
            {
                PlayModeView gameWindowToUse = mainGameWindow;

                // The Frame Debugger and Game Window can not be docked together in
                // the panes list (tabs) as both need to be shown in the Editor.
                bool isFDInTheSamePaneAsGameWindow = false;
                DockArea da = m_Parent as DockArea;
                if (da)
                    isFDInTheSamePaneAsGameWindow |= CheckIfFDIsDockedWithGameWindow(da, mainGameWindow);

                // If it's docked, check if there are other game windows available to use
                if (isFDInTheSamePaneAsGameWindow && allGameWindows.Count > 1)
                {
                    for (int i = 0; i < allGameWindows.Count; i++)
                    {
                        if (CheckIfFDIsDockedWithGameWindow(da, allGameWindows[i]))
                            continue;

                        isFDInTheSamePaneAsGameWindow = false;
                        gameWindowToUse = allGameWindows[i];
                        break;
                    }
                }

                // When we can't enable the FD debugger, we display an error box informing the
                // user to undock the Frame Debugger Window so it's not tabbed with the Game Window.
                if (isFDInTheSamePaneAsGameWindow)
                {
                    m_ShowTabbedErrorBox = true;
                    return false;
                }
                // Otherwise we show the Game Window
                else
                {
                    gameWindowToUse.ShowTab();
                    m_HasOpenedPlaymodeView = true;
                    return true;
                }
            }

            return false;
        }

        private void DrawEnabledFrameDebugger(bool repaint)
        {
            int oldLimit = FrameDebuggerUtility.limit;
            FrameDebuggerEvent[] descs = FrameDebuggerUtility.GetFrameEvents();

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

            Rect currentEventRect = new Rect(
                m_TreeWidth,
                toolbarHeight,
                position.width - m_TreeWidth,
                position.height - toolbarHeight
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

        private void EnableFrameDebugger()
        {
            if (FrameDebugger.enabled)
                return;

            bool enablingLocally = !FrameDebugger.enabled && m_AttachToPlayerState.connectedToTarget == ConnectionTarget.Editor;
            if (enablingLocally && !FrameDebuggerUtility.locallySupported)
                return;

            m_ShowTabbedErrorBox = false;
            m_HasOpenedPlaymodeView = false;
            if (!OpenPlayModeView())
                return;

            // pause play mode if needed
            if (enablingLocally)
                if (EditorApplication.isPlaying && !EditorApplication.isPaused)
                    EditorApplication.isPaused = true;

            FrameDebuggerUtility.SetEnabled(true, ProfilerDriver.connectedProfiler);

            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            if (m_EventDetailsView == null)
                m_EventDetailsView = new FrameDebuggerEventDetailsView(this);

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

            m_HasOpenedPlaymodeView = false;
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
