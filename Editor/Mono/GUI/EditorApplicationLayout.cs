// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEditorInternal;

// Description:
// EditorApplicationLayout handles the GUI when playmode changes (on a high level).

// When entering playmode the flow is as follows (also see Application::SetIsPlaying() for actual loading):
// 1) Calling InitPlaymodeLayout prepares the main gameView WITHOUT rendering it. Sets it up, maximizes it if needed and intitializes its size.
// 2) The current scene is loaded from Application.cp and initialized (Awake(), OnEnable(), Start() and first Update() is called (this takes time for large projects))
// 3) Calling FinalizePlaymodeLayout finalizes and renders maximized window (if set).


namespace UnityEditor
{
    internal class EditorApplicationLayout
    {
        static private bool m_MaximizePending = false;
        static List<PlayModeView> m_PlayModeViewList = null;

        static internal bool IsInitializingPlaymodeLayout()
        {
            return m_PlayModeViewList != null && m_PlayModeViewList.Count > 0;
        }

        static internal void SetPlaymodeLayout()
        {
            InitPlaymodeLayout();
            FinalizePlaymodeLayout();
        }

        static internal void SetStopmodeLayout()
        {
            if (m_PlayModeViewList != null && m_PlayModeViewList.Count > 0)
            {
                var monitorNames = EditorFullscreenController.GetConnectedDisplayNames();
                foreach (var playModeView in m_PlayModeViewList)
                {
                    if (playModeView.fullscreenMonitorIdx >= monitorNames.Length)
                        continue;

                    EditorFullscreenController.SetSettingsForCurrentDisplay(playModeView.fullscreenMonitorIdx);
                    EditorFullscreenController.OnExitPlaymode();
                }

                m_PlayModeViewList.Clear();
                m_PlayModeViewList = null;
            }

            WindowLayout.ShowAppropriateViewOnEnterExitPlaymode(false);
            Toolbar.RepaintToolbar();
        }

        static internal void SetPausemodeLayout()
        {
            // We use the stopmode layout when pausing (maximized windows are unmaximized)
            SetStopmodeLayout();
        }

        static internal void InitializePlaymodeViewList()
        {
            if (m_PlayModeViewList == null)
            {
                m_PlayModeViewList = new List<PlayModeView>();
            }
            else
            {
                m_PlayModeViewList.Clear();
            }
        }

        static internal void InitPlaymodeLayout()
        {
            InitializePlaymodeViewList();
            WindowLayout.ShowAppropriateViewOnEnterExitPlaymodeList(true, out m_PlayModeViewList);

            var fullscreenDetected = false;
            var monitorNames = EditorFullscreenController.GetConnectedDisplayNames();

            foreach (var playModeView in m_PlayModeViewList)
            {
                if (playModeView == null)
                    continue;

                if (playModeView.fullscreenMonitorIdx >= monitorNames.Length)
                    continue;

                if (playModeView.enterPlayModeBehavior == PlayModeView.EnterPlayModeBehavior.PlayFullscreen)
                {
                    EditorFullscreenController.SetSettingsForCurrentDisplay(playModeView.fullscreenMonitorIdx);
                    EditorFullscreenController.isFullscreenOnPlay = true;
                    EditorFullscreenController.fullscreenDisplayId = playModeView.fullscreenMonitorIdx;
                    EditorFullscreenController.isToolbarEnabledOnFullscreen = false;
                    EditorFullscreenController.targetDisplayID = playModeView.targetDisplay;

                    if (playModeView.m_Parent is DockArea dockArea && dockArea.actualView is GameView gv)
                    {
                        playModeView.m_Parent.EnableVSync(gv.vSyncEnabled);
                        EditorFullscreenController.enableVSync = gv.vSyncEnabled;
                        EditorFullscreenController.selectedSizeIndex = gv.selectedSizeIndex;
                    }
                    fullscreenDetected = true;
                }
                else if (!fullscreenDetected)
                {
                    EditorFullscreenController.isFullscreenOnPlay = false;
                }

                if (playModeView.enterPlayModeBehavior == PlayModeView.EnterPlayModeBehavior.PlayMaximized)
                {
                    if (playModeView.m_Parent is DockArea dockArea)
                    {
                        m_MaximizePending = WindowLayout.MaximizePrepare(dockArea.actualView);
                        var gv = dockArea.actualView as GameView;
                        if (gv != null)
                        {
                            playModeView.m_Parent.EnableVSync(gv.vSyncEnabled);
                        }
                    }
                }

                EditorFullscreenController.OnEnterPlaymode();

                if (!EditorFullscreenController.isFullscreenOnPlay)
                {
                    playModeView.m_Parent.SetAsStartView();
                    playModeView.m_Parent.SetAsLastPlayModeView();

                    if (playModeView is IGameViewOnPlayMenuUser)
                    {
                        if (((IGameViewOnPlayMenuUser)playModeView).playFocused)
                        {
                            playModeView.Focus();
                        }
                    }
                }
                Toolbar.RepaintToolbar();
            }
        }

        static internal void FinalizePlaymodeLayout()
        {
            foreach (var playModeView in m_PlayModeViewList)
            {
                if (playModeView != null)
                {
                    if (m_MaximizePending)
                        WindowLayout.MaximizePresent(playModeView);

                    // All StartView references on all play mode views must be cleared before play mode starts. Otherwise it may cause issues
                    // with input being routed to the correct game window. See case 1381985
                    playModeView.m_Parent.ClearStartView();
                }
            }
        }
    }
} // namespace
