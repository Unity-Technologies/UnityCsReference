// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Networking.PlayerConnection;

using UnityEditorInternal;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Networking.PlayerConnection;


namespace UnityEditor
{
    internal class FrameDebuggerWindow : EditorWindow
    {
        // Serialized
        [SerializeField] private float m_TreeWidth = FrameDebuggerStyles.Window.k_MinTreeWidth;
        [SerializeField] private TreeViewState m_TreeViewState;

        // Private
        private int m_RepaintFrames = k_NeedToRepaintFrames;
        private int m_FrameEventsHash;
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
        private bool IsDisabledInEditor => !FrameDebugger.enabled && m_AttachToPlayerState.connectedToTarget == ConnectionTarget.Editor;
        private bool HasEventHashChanged => FrameDebuggerUtility.eventsHash != m_FrameEventsHash;


        [MenuItem("Window/Analysis/Frame Debugger", false, 10)]
        public static FrameDebuggerWindow ShowFrameDebuggerWindow()
        {
            var wnd = GetWindow(typeof(FrameDebuggerWindow)) as FrameDebuggerWindow;
            wnd.titleContent = EditorGUIUtility.TrTextContent("Frame Debug");
            return wnd;
        }

        private void OnGUI()
        {
            FrameDebuggerEvent[] descs = FrameDebuggerUtility.GetFrameEvents();
            Initialize(descs);

            int oldLimit = FrameDebuggerUtility.limit;

            Profiler.BeginSample("DrawToolbar");
            bool repaint = m_Toolbar.DrawToolbar(this, m_AttachToPlayerState);
            Profiler.EndSample();

            if (IsDisabledInEditor)
            {
                GUI.enabled = true;
                if (!FrameDebuggerUtility.locallySupported)
                {
                    string warningMessage = (FrameDebuggerHelper.IsOnLinuxOpenGL) ? FrameDebuggerStyles.EventDetails.warningLinuxOpenGLMsg : FrameDebuggerStyles.EventDetails.warningMultiThreadedMsg;
                    EditorGUILayout.HelpBox(warningMessage, MessageType.Warning, true);
                }

                EditorGUILayout.HelpBox(FrameDebuggerStyles.EventDetails.descriptionString, MessageType.Info, true);
            }
            else
            {
                if (FrameDebugger.IsLocalEnabled())
                {
                    PlayModeView playModeView = PlayModeView.GetMainPlayModeView();
                    if (playModeView)
                        playModeView.ShowTab();
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

                Profiler.BeginSample("DrawEvent");
                m_EventDetailsView.DrawEvent(currentEventRect, descs, m_AttachToPlayerState.connectedToTarget == ConnectionTarget.Editor);
                Profiler.EndSample();
            }

            if (repaint || oldLimit != FrameDebuggerUtility.limit)
                RepaintOnLimitChange();

            if (m_RepaintFrames > 0)
            {
                m_TreeView.SelectFrameEventIndex(FrameDebuggerUtility.limit);
                RepaintAllNeededThings();
                --m_RepaintFrames;
            }
        }

        internal void DrawSearchField(string str)
        {
            m_SearchString = EditorGUI.ToolbarSearchField(m_SearchRect, str, false);
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
        }

        private void OnDisable()
        {
            if (m_EventDetailsView != null)
                m_EventDetailsView.OnDisable();

            FrameDebuggerStyles.OnDisable();

            m_AttachToPlayerState?.Dispose();
            m_AttachToPlayerState = null;

            s_FrameDebuggers.Remove(this);
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            DisableFrameDebugger();
        }

        internal override void OnResized()
        {
            if (PopupWindowWithoutFocus.IsVisible())
                PopupWindowWithoutFocus.Hide();

            base.OnResized();
        }

        private void Initialize(FrameDebuggerEvent[] descs)
        {
            if (m_Toolbar == null)
                m_Toolbar = new FrameDebuggerToolbarView();

            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();

            if (m_TreeView == null)
            {
                m_TreeView = new FrameDebuggerTreeView(descs, m_TreeViewState, this, new Rect(
                    50,
                    50,
                    500, 100
                ));
                m_FrameEventsHash = FrameDebuggerUtility.eventsHash;
                m_TreeView.m_DataSource.SetExpanded(m_TreeView.m_DataSource.root, true);

                // Expand root's children only
                foreach (var treeViewItem in m_TreeView.m_DataSource.root.children)
                    if (treeViewItem != null)
                        m_TreeView.m_DataSource.SetExpanded(treeViewItem, true);
            }

            if (m_EventDetailsView == null)
                m_EventDetailsView = new FrameDebuggerEventDetailsView(this);
        }

        internal void ChangeFrameEventLimit(int newLimit)
        {
            if (newLimit <= 0 || newLimit > FrameDebuggerUtility.count)
                return;

            FrameDebuggerUtility.limit = newLimit;
            m_EventDetailsView?.OnNewFrameEventSelected();
            m_TreeView?.SelectFrameEventIndex(newLimit);
        }

        bool hasChosenParent = false;
        FrameDebuggerTreeView.FrameDebuggerTreeViewItem parentItem = null;

        internal void ChangeFrameEventLimit(int newLimit, FrameDebuggerTreeView.FrameDebuggerTreeViewItem originalItem)
        {
            if (newLimit <= 0 || newLimit > FrameDebuggerUtility.count)
                return;

            hasChosenParent = (originalItem != null);
            parentItem = originalItem;

            FrameDebuggerUtility.limit = newLimit;
            m_EventDetailsView?.OnNewFrameEventSelected();
            m_TreeView?.SelectFrameEventIndex(newLimit);
        }

        private static void DisableFrameDebugger()
        {
            // if it was true before, we disabled and ask the game scene to repaint
            if (FrameDebugger.IsLocalEnabled())
                EditorApplication.SetSceneRepaintDirty();

            FrameDebuggerUtility.SetEnabled(false, FrameDebuggerUtility.GetRemotePlayerGUID());
        }

        internal void EnableIfNeeded()
        {
            if (FrameDebugger.enabled)
                return;

            m_EventDetailsView.Reset();
            ClickEnableFrameDebugger();
            RepaintOnLimitChange();
        }

        internal void ClickEnableFrameDebugger()
        {
            bool isEnabled = FrameDebugger.enabled;
            bool enablingLocally = !isEnabled && m_AttachToPlayerState.connectedToTarget == ConnectionTarget.Editor;

            if (enablingLocally && !FrameDebuggerUtility.locallySupported)
                return;

            // pause play mode if needed
            if (enablingLocally)
                if (EditorApplication.isPlaying && !EditorApplication.isPaused)
                    EditorApplication.isPaused = true;

            if (!isEnabled)
                FrameDebuggerUtility.SetEnabled(true, ProfilerDriver.connectedProfiler);
            else
                FrameDebuggerUtility.SetEnabled(false, FrameDebuggerUtility.GetRemotePlayerGUID());

            // Make sure game view is visible when enabling frame debugger locally
            if (FrameDebugger.IsLocalEnabled())
            {
                PlayModeView playModeView = PlayModeView.GetMainPlayModeView();
                if (playModeView)
                    playModeView.ShowTab();
            }
        }

        internal static void RepaintAll()
        {
            foreach (var fd in s_FrameDebuggers)
                fd.Repaint();
        }

        private void RepaintOnLimitChange()
        {
            m_RepaintFrames = k_NeedToRepaintFrames;
            RepaintAllNeededThings();
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
    }
}
