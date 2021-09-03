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

namespace UnityEditor
{
    // This is what we (not users) derive from to create various views. (Main Toolbar, etc.)
    [StructLayout(LayoutKind.Sequential)]
    internal partial class GUIView : View
    {
        // Case 1183719 - The delegate getEditorShader is being reset upon domain reload and InitializeOnLoad is not rerun
        // Hence a static constructor to Initialize the Delegate. EditorShaderLoader is still needed for Batch mode where GUIView may not be created
        static GUIView()
        {
            // TODO: Remove this once case 1148851 has been fixed.
            UnityEngine.UIElements.UIR.UIRenderDevice.getEditorShader = () => EditorShader;
        }

        [InitializeOnLoad]
        static class EditorShaderLoader
        {
            static EditorShaderLoader()
            {
                // TODO: Remove this once case 1148851 has been fixed.
                UnityEngine.UIElements.UIR.UIRenderDevice.getEditorShader = () => EditorShader;
            }
        }

        internal static event Action<GUIView> positionChanged = null;

        Panel m_Panel = null;
        readonly EditorCursorManager m_CursorManager = new EditorCursorManager();
        static EditorContextualMenuManager s_ContextualMenuManager = new EditorContextualMenuManager();

        static Shader s_EditorShader = null;

        static Shader EditorShader
        {
            get
            {
                if (s_EditorShader == null)
                {
                    s_EditorShader = EditorGUIUtility.LoadRequired("Shaders/UIElements/EditorUIE.shader") as Shader;
                }

                return s_EditorShader;
            }
        }

        protected Panel panel
        {
            get
            {
                if (m_Panel == null)
                {
                    m_Panel = UIElementsUtility.FindOrCreateEditorPanel(this);
                    m_Panel.name = GetType().Name;
                    m_Panel.cursorManager = m_CursorManager;
                    m_Panel.contextualMenuManager = s_ContextualMenuManager;
                    m_Panel.panelDebug = new PanelDebug(m_Panel);
                    m_Panel.standardShader = EditorShader;
                    UpdateDrawChainRegistration(true);
                    if (imguiContainer != null)
                        m_Panel.visualTree.Insert(0, imguiContainer);

                    panel.visualTree.SetSize(windowPosition.size);
                }

                return m_Panel;
            }
        }

        public VisualElement visualTree => panel.visualTree;
        protected IMGUIContainer imguiContainer { get; private set; }

        int m_DepthBufferBits = 0;
        int m_AntiAliasing = 1;
        EventInterests m_EventInterests;
        bool m_AutoRepaintOnSceneChange = false;

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
        internal override void SetWindow(ContainerWindow win)
        {
            ContainerWindow oldWindow = this.window;
            base.SetWindow(win); // Sets this.window(m_Window) to win
            Internal_Init(m_DepthBufferBits, m_AntiAliasing);
            if (!win)
            {
                // Tell the native ContainerWindow we were attached to that we
                // are no longer attached to it.
                Internal_UnsetWindow(oldWindow);
            }
            else
            {
                Internal_SetWindow(win);
            }

            Internal_SetAutoRepaint(m_AutoRepaintOnSceneChange);
            Internal_SetPosition(windowPosition);
            Internal_SetWantsMouseMove(m_EventInterests.wantsMouseMove);
            Internal_SetWantsMouseEnterLeaveWindow(m_EventInterests.wantsMouseMove);

            panel.visualTree.SetSize(windowPosition.size);
        }

        internal void RecreateContext()
        {
            Internal_Recreate(m_DepthBufferBits, m_AntiAliasing);
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
                imguiContainer = new IMGUIContainer(OldOnGUI) { useOwnerObjectGUIState = true };
                imguiContainer.StretchToParentSize();
                imguiContainer.viewDataKey = "Dockarea";

                if (m_Panel != null)
                    m_Panel.visualTree.Insert(0, imguiContainer);
            }

            Panel.BeforeUpdaterChange += OnBeforeUpdaterChange;
            Panel.AfterUpdaterChange += OnAfterUpdaterChange;
        }

        protected virtual void OnDisable()
        {
            if (imguiContainer.HasMouseCapture())
                imguiContainer.ReleaseMouse();
            imguiContainer.RemoveFromHierarchy();
            imguiContainer = null;

            if (m_Panel != null)
            {
                UpdateDrawChainRegistration(false);
                m_Panel.Dispose();
                /// We don't set <c>m_Panel</c> to null to prevent it from being re-created from <c>panel</c>.
            }

            Panel.BeforeUpdaterChange -= OnBeforeUpdaterChange;
            Panel.AfterUpdaterChange -= OnAfterUpdaterChange;
        }

        private void OnBeforeUpdaterChange()
        {
            UpdateDrawChainRegistration(false);
        }

        private void OnAfterUpdaterChange()
        {
            UpdateDrawChainRegistration(true);
        }

        private void UpdateDrawChainRegistration(bool register)
        {
            var p = panel as BaseVisualElementPanel;
            if (p != null)
            {
                var updater = p.GetUpdater(VisualTreeUpdatePhase.Repaint) as UIRRepaintUpdater;
                if (updater != null)
                {
                    if (register)
                        updater.BeforeDrawChain += OnBeforeDrawChain;
                    else updater.BeforeDrawChain -= OnBeforeDrawChain;
                }
            }
        }

        static readonly int s_EditorColorSpaceID = Shader.PropertyToID("_EditorColorSpace");

        void OnBeforeDrawChain(UnityEngine.UIElements.UIR.UIRenderDevice device)
        {
            Material mat = device.GetStandardMaterial();
            mat.SetFloat(s_EditorColorSpaceID, QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
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

            panel.visualTree.SetSize(windowPosition.size);

            positionChanged?.Invoke(this);

            Repaint();
        }

        protected override void OnDestroy()
        {
            Internal_Close();

            base.OnDestroy();
        }

        // Draw resize handles, etc.
        internal virtual void DoWindowDecorationStart()
        {
        }

        internal virtual void DoWindowDecorationEnd()
        {
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
