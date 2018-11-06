// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Runtime.InteropServices;
using System;
using UnityEditor.StyleSheets;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Scripting;

//temporary until everyone is out of experimental
using ExperimentalUI = UnityEngine.Experimental.UIElements;
using EditorExperimentalUI = UnityEditor.Experimental.UIElements;


namespace UnityEditor
{
    // This is what we (not users) derive from to create various views. (Main Toolbar, etc.)
    [StructLayout(LayoutKind.Sequential)]
    internal partial class GUIView : View
    {
        internal static event Action<GUIView> positionChanged = null;

        Panel m_Panel = null;
        readonly EditorCursorManager m_CursorManager = new EditorCursorManager();
        static EditorContextualMenuManager s_ContextualMenuManager = new EditorContextualMenuManager();

        internal ExperimentalUI.Panel m_ExperimentalPanel = null;
        readonly EditorExperimentalUI.EditorCursorManager m_ExperimentalCursorManager = new EditorExperimentalUI.EditorCursorManager();
        static EditorExperimentalUI.EditorContextualMenuManager s_ExperimentalContextualMenuManager = new EditorExperimentalUI.EditorContextualMenuManager();

        protected Panel panel
        {
            get
            {
                if (m_Panel == null)
                {
                    if (m_ExperimentalPanel != null)
                    {
                        throw new InvalidOperationException("UIElements can't run in Experimental and Public mode at the same time. Please update your code to use the UnityEngine.UIElements namespace. Use SwitchUIElementMode() to change namespaces");
                    }
                    uieMode = GUIView.UIElementsMode.Public;
                    m_Panel = UIElementsUtility.FindOrCreatePanel(this, ContextType.Editor);
                    m_Panel.cursorManager = m_CursorManager;
                    m_Panel.contextualMenuManager = s_ContextualMenuManager;
                    m_Panel.panelDebug = new PanelDebug(m_Panel);

                    if (imguiContainer != null)
                        m_Panel.visualTree.Insert(0, imguiContainer);

                    panel.visualTree.SetSize(windowPosition.size);
                }

                return m_Panel;
            }
        }

        protected ExperimentalUI.Panel experimentalPanel
        {
            get
            {
                if (m_ExperimentalPanel == null)
                {
                    if (m_Panel != null)
                    {
                        throw new InvalidOperationException("UIElements can't run in Experimental and Public mode at the same time. Please update your code to use the UnityEngine.UIElements namespace. Use SwitchUIElementMode() to change namespaces");
                    }
                    uieMode = UIElementsMode.Experimental;
                    EditorExperimentalUI.UXMLEditorFactories.RegisterAll();
                    m_ExperimentalPanel = ExperimentalUI.UIElementsUtility.FindOrCreatePanel(this, ExperimentalUI.ContextType.Editor, DataWatchService.sharedInstance);
                    m_ExperimentalPanel.cursorManager = m_ExperimentalCursorManager;
                    m_ExperimentalPanel.contextualMenuManager = s_ExperimentalContextualMenuManager;
                    m_ExperimentalPanel.panelDebug = new EditorExperimentalUI.PanelDebug(m_ExperimentalPanel);

                    if (experimentalImguiContainer != null)
                        m_ExperimentalPanel.visualTree.Insert(0, experimentalImguiContainer);

                    m_ExperimentalPanel.visualTree.SetSize(windowPosition.size);
                }

                return m_ExperimentalPanel;
            }
        }

        //Remove this once we remove the Experimental namespace
        internal enum UIElementsMode
        {
            Unset,
            Experimental,
            Public,
        }

        internal UIElementsMode m_UIElementsMode = UIElementsMode.Unset;
        public UnityEditor.GUIView.UIElementsMode uieMode
        {
            get { return m_UIElementsMode; }
            set { m_UIElementsMode = value; }
        }

        internal void SwitchUIElementsMode(UIElementsMode mode)
        {
            if (mode == UIElementsMode.Experimental)
            {
                if (m_Panel != null)
                {
                    imguiContainer.RemoveFromHierarchy();
                    m_Panel.Dispose();
                    m_Panel = null;
                }

                if (m_ExperimentalPanel == null)
                {
                    var p = experimentalPanel;
                }
            }
            else
            {
                if (m_ExperimentalPanel != null)
                {
                    experimentalImguiContainer.RemoveFromHierarchy();
                    m_ExperimentalPanel.Dispose();
                    m_ExperimentalPanel = null;
                }
                if (m_Panel == null)
                {
                    var p = panel;
                }
            }
        }

        public VisualElement visualTree => panel.visualTree;
        public ExperimentalUI.VisualElement experimentalVisualTree => experimentalPanel.visualTree;

        protected IMGUIContainer imguiContainer { get; private set; }
        protected ExperimentalUI.IMGUIContainer experimentalImguiContainer { get; private set; }

        int m_DepthBufferBits = 0;
        int m_AntiAliasing = 1;
        EventInterests m_EventInterests;
        bool m_AutoRepaintOnSceneChange = false;
        private bool m_BackgroundValid = false;

        internal bool SendEvent(Event e)
        {
            int depth = SavedGUIState.Internal_GetGUIDepth();
            if (depth > 0)
            {
                SavedGUIState oldState = SavedGUIState.Create();
                var retval = Internal_SendEvent(e);
                oldState.ApplyAndForget();
                return retval;
            }

            return Internal_SendEvent(e);
        }

        // Call into C++ here to move the underlying NSViews around
        protected override void SetWindow(ContainerWindow win)
        {
            base.SetWindow(win);
            Internal_Init(m_DepthBufferBits, m_AntiAliasing);
            if (win)
                Internal_SetWindow(win);
            Internal_SetAutoRepaint(m_AutoRepaintOnSceneChange);
            Internal_SetPosition(windowPosition);
            Internal_SetWantsMouseMove(m_EventInterests.wantsMouseMove);
            Internal_SetWantsMouseEnterLeaveWindow(m_EventInterests.wantsMouseMove);

            if (m_ExperimentalPanel != null)
            {
                m_ExperimentalPanel.visualTree.SetSize(windowPosition.size);
            }
            else
            {
                panel.visualTree.SetSize(windowPosition.size);
            }


            m_BackgroundValid = false;
        }

        internal void RecreateContext()
        {
            Internal_Recreate(m_DepthBufferBits, m_AntiAliasing);
            m_BackgroundValid = false;
        }

        public EventInterests eventInterests
        {
            get { return m_EventInterests; }
            set
            {
                m_EventInterests = value;

                if (m_ExperimentalPanel != null)
                {
                    m_ExperimentalPanel.IMGUIEventInterests = m_EventInterests;
                }
                else
                {
                    panel.IMGUIEventInterests = m_EventInterests;
                }

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

                if (m_ExperimentalPanel != null)
                {
                    m_ExperimentalPanel.IMGUIEventInterests = m_EventInterests;
                }
                else
                {
                    panel.IMGUIEventInterests = m_EventInterests;
                }

                Internal_SetWantsMouseMove(wantsMouseMove);
            }
        }

        public bool wantsMouseEnterLeaveWindow
        {
            get { return m_EventInterests.wantsMouseEnterLeaveWindow; }
            set
            {
                m_EventInterests.wantsMouseEnterLeaveWindow = value;

                if (m_ExperimentalPanel != null)
                {
                    m_ExperimentalPanel.IMGUIEventInterests = m_EventInterests;
                }
                else
                {
                    panel.IMGUIEventInterests = m_EventInterests;
                }

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

        public int antiAliasing
        {
            get { return m_AntiAliasing; }
            set { m_AntiAliasing = value; }
        }

        [Obsolete("AA is not supported on GUIViews", false)]
        public int antiAlias
        {
            get { return 1; }
            set { throw new NotSupportedException("AA is not supported on GUIViews"); }
        }

        protected virtual void OnEnable()
        {
            {
                experimentalImguiContainer = new ExperimentalUI.IMGUIContainer(OldOnGUI) { useOwnerObjectGUIState = true };
                ExperimentalUI.VisualElementExtensions.StretchToParentSize(experimentalImguiContainer);
                experimentalImguiContainer.persistenceKey = "Dockarea";

                if (m_ExperimentalPanel != null)
                    m_ExperimentalPanel.visualTree.Insert(0, experimentalImguiContainer);
            }
            {
                imguiContainer = new IMGUIContainer(OldOnGUI) { useOwnerObjectGUIState = true };
                imguiContainer.StretchToParentSize();
                imguiContainer.viewDataKey = "Dockarea";

                if (m_Panel != null)
                    m_Panel.visualTree.Insert(0, imguiContainer);
            }
        }

        protected virtual void OnDisable()
        {
            if (uieMode == UIElementsMode.Experimental)
            {
                if (ExperimentalUI.MouseCaptureController.HasMouseCapture(experimentalImguiContainer))
                    MouseCaptureController.ReleaseMouse();
                experimentalImguiContainer.RemoveFromHierarchy();
                experimentalImguiContainer = null;

                if (m_ExperimentalPanel != null)
                {
                    m_ExperimentalPanel.Dispose();
                    /// We don't set <c>m_Panel</c> to null to prevent it from being re-created from <c>panel</c>.
                }
            }
            else
            {
                if (imguiContainer.HasMouseCapture())
                    MouseCaptureController.ReleaseMouse();
                imguiContainer.RemoveFromHierarchy();
                imguiContainer = null;

                if (m_Panel != null)
                {
                    m_Panel.Dispose();
                    /// We don't set <c>m_Panel</c> to null to prevent it from being re-created from <c>panel</c>.
                }
            }
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

            if (m_ExperimentalPanel != null)
            {
                m_ExperimentalPanel.visualTree.SetSize(windowPosition.size);
            }
            else
            {
                panel.visualTree.SetSize(windowPosition.size);
            }

            positionChanged?.Invoke(this);

            Repaint();
        }

        protected override void OnDestroy()
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

        [RequiredByNativeCode]
        internal string GetViewName()
        {
            var hostView = this as HostView;
            if (hostView != null && hostView.actualView != null)
                return hostView.actualView.GetType().Name;

            return GetType().Name;
        }

        [RequiredByNativeCode]
        internal static string GetTypeNameOfMostSpecificActiveView()
        {
            var currentView = current;
            if (currentView == null)
                return string.Empty;

            var hostView = currentView as HostView;
            if (hostView != null && hostView.actualView != null)
                return hostView.actualView.GetType().FullName;

            return currentView.GetType().FullName;
        }
    }
} //namespace
