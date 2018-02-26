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
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
using UnityEditor.StyleSheets;

namespace UnityEditor
{
    // See GUIView.bindings for bindings

    // This is what we (not users) derive from to create various views. (Main Toolbar, etc.)
    [StructLayout(LayoutKind.Sequential)]
    internal partial class GUIView : View
    {
        internal static event Action<GUIView> positionChanged = null;


        protected Panel panel
        {
            get
            {
                if (Panel.loadResourceFunc == null)
                    Panel.loadResourceFunc = StyleSheetResourceUtil.LoadResource;

                Panel p = UIElementsUtility.FindOrCreatePanel(this, ContextType.Editor, DataWatchService.sharedInstance);
                AddDefaultEditorStyleSheets(p.visualTree);
                return p;
            }
        }

        internal static void AddDefaultEditorStyleSheets(VisualElement p)
        {
            if (p.styleSheets == null)
            {
                p.AddStyleSheetPath("StyleSheets/DefaultCommon.uss");
                if (EditorGUIUtility.isProSkin)
                {
                    p.AddStyleSheetPath("StyleSheets/DefaultCommonDark.uss");
                }
                else
                {
                    p.AddStyleSheetPath("StyleSheets/DefaultCommonLight.uss");
                }
            }
        }

        public VisualElement visualTree
        {
            get
            {
                return panel.visualTree;
            }
        }

        protected IMGUIContainer imguiContainer { get; private set; }

        int m_DepthBufferBits = 0;
        EventInterests m_EventInterests;
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
            Internal_SetWantsMouseMove(m_EventInterests.wantsMouseMove);
            Internal_SetWantsMouseEnterLeaveWindow(m_EventInterests.wantsMouseMove);

            panel.visualTree.SetSize(windowPosition.size);

            m_BackgroundValid = false;
        }

        internal void RecreateContext()
        {
            Internal_Recreate(m_DepthBufferBits);
            m_BackgroundValid = false;
        }

        public EventInterests eventInterests
        {
            get { return m_EventInterests; }
            set
            {
                m_EventInterests = value;
                panel.IMGUIEventInterests = m_EventInterests;
                Internal_SetWantsMouseMove(wantsMouseMove);
                Internal_SetWantsMouseEnterLeaveWindow(wantsMouseEnterLeaveWindow);
            }
        }

        public bool wantsMouseMove
        {
            get { return m_EventInterests.wantsMouseMove; }
            set
            {
                m_EventInterests.wantsMouseMove = value;
                panel.IMGUIEventInterests = m_EventInterests;
                Internal_SetWantsMouseMove(wantsMouseMove);
            }
        }

        public bool wantsMouseEnterLeaveWindow
        {
            get { return m_EventInterests.wantsMouseEnterLeaveWindow; }
            set
            {
                m_EventInterests.wantsMouseEnterLeaveWindow = value;
                panel.IMGUIEventInterests = m_EventInterests;
                Internal_SetWantsMouseEnterLeaveWindow(wantsMouseEnterLeaveWindow);
            }
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

        protected virtual void OnEnable()
        {
            imguiContainer = new IMGUIContainer(OldOnGUI) { useOwnerObjectGUIState = true };
            imguiContainer.StretchToParentSize();
            imguiContainer.persistenceKey = "Dockarea";
            visualTree.Insert(0, imguiContainer);
        }

        protected virtual void OnDisable()
        {
            if (imguiContainer.HasCapture())
                imguiContainer.RemoveCapture();
            visualTree.Remove(imguiContainer);
            imguiContainer = null;
        }

        protected virtual void OldOnGUI() {}
        // Without leaving this in here for MonoBehaviour::DoGUI(), GetMethod(MonoScriptCache::kGUI) will return null.
        // In that case, commands are not delegated (e.g., keyboard-based delete in Hierarchy/Project)
        protected virtual void OnGUI() {}

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

            panel.visualTree.SetSize(windowPosition.size);
            if (positionChanged != null)
                positionChanged(this);
            Repaint();
        }

        protected override void OnDestroy()
        {
            Internal_Close();

            UIElementsUtility.RemoveCachedPanel(this.GetInstanceID());

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
