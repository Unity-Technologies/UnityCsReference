// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using IntPtr = System.IntPtr;
using System;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor
{
    // See GUIView.bindings for bindings

    // This is what we (not users) derive from to create various views. (Main Toolbar, etc.)
    [StructLayout(LayoutKind.Sequential)]
    internal partial class GUIView : View
    {

        DataWatchService s_DataWatch = new DataWatchService();

        Panel panel
        {
            get
            {
                return UIElementsUtility.FindOrCreatePanel(GetInstanceID(), ContextType.Editor, s_DataWatch, LoadResourceWrapper);
            }
        }

        static UnityEngine.Object LoadResourceWrapper(string pathName, System.Type type)
        {
            var resource = EditorGUIUtility.Load(pathName);
            if (resource == null)
            {
                resource = Resources.Load(pathName, type);
            }
            if (resource != null)
            {
                Debug.Assert(type.IsAssignableFrom(resource.GetType()), "Resource type mismatch");
            }
            return resource;
        }

        public VisualContainer visualTree
        {
            get
            {
                return panel.visualTree;
            }
        }

        int m_DepthBufferBits = 0;
        bool m_WantsMouseMove = false;
        bool m_WantsMouseEnterLeaveWindow = false;
        bool m_AutoRepaintOnSceneChange = false;
        private bool m_BackgroundValid = false;

        internal bool SendEvent(Event e)
        {
            int depth = SavedGUIState.Internal_GetGUIDepth();
            bool retval = false;
            if (depth > 0)
            {
                SavedGUIState oldState = SavedGUIState.Create();
                retval = Internal_SendEvent(e);
                oldState.ApplyAndForget();
            }
            else
            {
                retval = Internal_SendEvent(e);
            }
            return retval;
        }

        // Call into C++ here to move the underlying NSViews around
        protected override void SetWindow(ContainerWindow win)
        {
            base.SetWindow(win);
            Internal_Init(m_DepthBufferBits);
            if (win)
                Internal_SetWindow(win);
            Internal_SetAutoRepaint(m_AutoRepaintOnSceneChange);
            Internal_SetPosition(windowPosition);
            Internal_SetWantsMouseMove(m_WantsMouseMove);
            Internal_SetWantsMouseEnterLeaveWindow(m_WantsMouseEnterLeaveWindow);

            panel.SetSize(windowPosition.size);

            m_BackgroundValid = false;
        }

        internal void RecreateContext()
        {
            Internal_Recreate(m_DepthBufferBits);
            m_BackgroundValid = false;
        }

        public bool wantsMouseMove
        {
            get { return m_WantsMouseMove; }
            set { m_WantsMouseMove = value; Internal_SetWantsMouseMove(m_WantsMouseMove); }
        }

        public bool wantsMouseEnterLeaveWindow
        {
            get { return m_WantsMouseEnterLeaveWindow; }
            set { m_WantsMouseEnterLeaveWindow = value; Internal_SetWantsMouseEnterLeaveWindow(m_WantsMouseEnterLeaveWindow); }
        }

        internal bool backgroundValid
        {
            get { return m_BackgroundValid; }
            set { m_BackgroundValid = value; }
        }

        public bool autoRepaintOnSceneChange
        {
            get { return m_AutoRepaintOnSceneChange; }
            set { m_AutoRepaintOnSceneChange = value; Internal_SetAutoRepaint(m_AutoRepaintOnSceneChange); }
        }

        public int depthBufferBits
        {
            get { return m_DepthBufferBits; }
            set { m_DepthBufferBits = value; }
        }

        [Obsolete("AA is not supported on GUIViews", false)]
        public int antiAlias
        {
            get { return 1; }
            set {}
        }

        protected override void SetPosition(Rect newPos)
        {
            Rect oldWinPos = windowPosition;

            base.SetPosition(newPos);
            if (oldWinPos == windowPosition)
            {
                Internal_SetPosition(windowPosition);
                return;
            }

            Internal_SetPosition(windowPosition);

            m_BackgroundValid = false;

            panel.SetSize(windowPosition.size);
            Repaint();
        }

        public new void OnDestroy()
        {
            Internal_Close();
            base.OnDestroy();
        }

        // Draw resize handles, etc.
        internal void DoWindowDecorationStart()
        {
            // On windows, we want both close window and side resizes.
            // Titlebar dragging is done at the end, so we can drag next to tabs.
            if (window != null)
                window.HandleWindowDecorationStart(windowPosition);
        }

        internal void DoWindowDecorationEnd()
        {
            if (window != null)
                window.HandleWindowDecorationEnd(windowPosition);
        }
    }
} //namespace
