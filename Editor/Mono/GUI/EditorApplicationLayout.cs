// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Scripting;
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
        static private PlayModeView m_PlayModeView = null;
        static private bool m_MaximizePending = false;


        static internal bool IsInitializingPlaymodeLayout()
        {
            return m_PlayModeView != null;
        }

        [RequiredByNativeCode]
        static internal void SetPlaymodeLayout()
        {
            InitPlaymodeLayout();
            FinalizePlaymodeLayout();
        }

        [RequiredByNativeCode]
        static internal void SetStopmodeLayout()
        {
            WindowLayout.ShowAppropriateViewOnEnterExitPlaymode(false);
            Toolbar.RepaintToolbar();
        }

        [RequiredByNativeCode]
        static internal void SetPausemodeLayout()
        {
            // We use the stopmode layout when pausing (maximized windows are unmaximized)
            SetStopmodeLayout();
        }

        [RequiredByNativeCode]
        static internal void InitPlaymodeLayout()
        {
            m_PlayModeView = WindowLayout.ShowAppropriateViewOnEnterExitPlaymode(true) as PlayModeView;
            if (m_PlayModeView == null)
                return;

            if (m_PlayModeView.enterPlayModeBehavior == PlayModeView.EnterPlayModeBehavior.PlayMaximized)
            {
                if (m_PlayModeView.m_Parent is DockArea dockArea)
                {
                    m_MaximizePending = WindowLayout.MaximizePrepare(dockArea.actualView);

                    var gameView = dockArea.actualView as GameView;
                    if (gameView != null)
                        m_PlayModeView.m_Parent.EnableVSync(gameView.vSyncEnabled);
                }
            }

            // Mark this PlayModeView window as the start view so the backend
            // can set size and mouseoffset properly for this view
            m_PlayModeView.m_Parent.SetAsStartView();
            m_PlayModeView.m_Parent.SetAsLastPlayModeView();

            //GameView should be actively focussed If Playmode is entered in maximized state - case 1252097
            if (m_PlayModeView.maximized)
                m_PlayModeView.m_Parent.Focus();

            Toolbar.RepaintToolbar();
        }

        [RequiredByNativeCode]
        static internal void FinalizePlaymodeLayout()
        {
            if (m_PlayModeView != null)
            {
                if (m_MaximizePending)
                    WindowLayout.MaximizePresent(m_PlayModeView);

                m_PlayModeView.m_Parent.ClearStartView();
            }

            Clear();
        }

        static private void Clear()
        {
            m_MaximizePending = false;
            m_PlayModeView = null;
        }
    }
} // namespace
