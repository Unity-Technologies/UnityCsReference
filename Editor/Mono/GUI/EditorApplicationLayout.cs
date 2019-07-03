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
        static private PreviewEditorWindow m_PreviewWindow = null;
        static private bool m_MaximizePending = false;


        static internal bool IsInitializingPlaymodeLayout()
        {
            return m_PreviewWindow != null;
        }

        static internal void SetPlaymodeLayout()
        {
            InitPlaymodeLayout();
            FinalizePlaymodeLayout();
        }

        static internal void SetStopmodeLayout()
        {
            WindowLayout.ShowAppropriateViewOnEnterExitPlaymode(false);
            Toolbar.RepaintToolbar();
        }

        static internal void SetPausemodeLayout()
        {
            // We use the stopmode layout when pausing (maximized windows are unmaximized)
            SetStopmodeLayout();
        }

        static internal void InitPlaymodeLayout()
        {
            m_PreviewWindow = WindowLayout.ShowAppropriateViewOnEnterExitPlaymode(true) as PreviewEditorWindow;
            if (m_PreviewWindow == null)
                return;

            if (m_PreviewWindow.maximizeOnPlay)
            {
                DockArea da = m_PreviewWindow.m_Parent as DockArea;

                if (da != null)
                    m_MaximizePending = WindowLayout.MaximizePrepare(da.actualView);
            }

            // Mark this preview window as the start preview so the backend
            // can set size and mouseoffset properly for this preview
            m_PreviewWindow.m_Parent.SetAsStartView();

            Toolbar.RepaintToolbar();
        }

        static internal void FinalizePlaymodeLayout()
        {
            if (m_PreviewWindow != null)
            {
                if (m_MaximizePending)
                    WindowLayout.MaximizePresent(m_PreviewWindow);

                m_PreviewWindow.m_Parent.ClearStartView();
            }

            Clear();
        }

        static private void Clear()
        {
            m_MaximizePending = false;
            m_PreviewWindow = null;
        }
    }
} // namespace
