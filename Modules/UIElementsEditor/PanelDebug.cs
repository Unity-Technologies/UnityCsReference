// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    class PanelOwner : ScriptableObject {}

    internal class PanelDebug : IPanelDebug
    {
        private HashSet<IPanelDebugger> m_Debuggers = new HashSet<IPanelDebugger>();

        public IPanel panel { get; }
        public IPanel debuggerOverlayPanel { get; private set; }
        public VisualElement visualTree { get { return panel?.visualTree; }}

        private VisualElement m_DebugContainer;
        public VisualElement debugContainer
        {
            get { return m_DebugContainer; }
            private set { m_DebugContainer = value; }
        }

        public bool hasAttachedDebuggers => m_Debuggers.Count > 0;

        private PanelOwner ownerObject;

        internal void InitializeDebuggerOverlayPanel()
        {
            if (debuggerOverlayPanel == null)
            {
                ownerObject = ScriptableObject.CreateInstance<PanelOwner>();
                // All debug panels are context type Editor, even if they are Runtime (Player) panels because the
                // debug panel itself are in the Editor anyway.
                var debuggerOverlayTmpPanel = new Panel(ownerObject, ContextType.Editor, EventDispatcher.CreateDefault(), EditorPanel.InitEditorUpdater);
                debuggerOverlayTmpPanel.overlayedOverPanel = panel;
                debuggerOverlayTmpPanel.clearSettings = new PanelClearSettings();
                debuggerOverlayPanel = debuggerOverlayTmpPanel;


                debuggerOverlayPanel.visualTree.style.position = Position.Absolute;
                UpdateOverlayPanelSize();
                debugContainer = new VisualElement()
                {
                    style =
                    {
                        position = Position.Absolute,
                        top = 0, left = 0, right = 0, bottom = 0,
                        backgroundColor = Color.clear
                    }
                };
                debuggerOverlayPanel.visualTree.Add(debugContainer);
            }
        }

        internal void UpdateOverlayPanelSize()
        {
            if (debuggerOverlayPanel == null)
                return;

            var overlayVisualTree = debuggerOverlayPanel.visualTree;
            if (panel is BaseRuntimePanel { drawsInCameras: true } runtimePanel)
            {
                // The world-space panel's layout is not in screen units. Size the overlay panel to
                // the display so that its coordinates map 1:1 to screen pixels; the overlay painters
                // project world-space geometry into screen space.
                var targetSize = GetTargetDisplaySize(runtimePanel);
                overlayVisualTree.style.top = 0;
                overlayVisualTree.style.left = 0;
                overlayVisualTree.style.width = targetSize.x;
                overlayVisualTree.style.height = targetSize.y;
            }
            else
            {
                overlayVisualTree.style.top = panel.visualTree.layout.yMin;
                overlayVisualTree.style.left = panel.visualTree.layout.xMin;
                overlayVisualTree.style.width = panel.visualTree.layout.width;
                overlayVisualTree.style.height = panel.visualTree.layout.height;
            }
        }

        // Note: with multiple views on the same display (e.g. two Game views, or a Game view and
        // the Device Simulator), this returns the size of the first one found, so the overlay can
        // end up sized for one view while compositing into another. Returns Vector2.zero when no
        // view targets the display, which degrades to a 0x0 overlay panel and no projection state
        // (nothing drawn) rather than an error.
        internal static Vector2 GetTargetDisplaySize(BaseRuntimePanel runtimePanel)
        {
            foreach (var playModeView in PlayModeView.GetAllPlayModeViewWindows())
            {
                if (playModeView.targetDisplay == runtimePanel.targetDisplay)
                    return playModeView.targetSize;
            }

            return Vector2.zero;
        }

        internal void RemoveDebuggerOverlayPanel()
        {
            if (debuggerOverlayPanel != null && m_Debuggers.Count == 0)
            {
                debuggerOverlayPanel.Dispose();
                debuggerOverlayPanel = null;
                debugContainer = null;
            }
        }

        public PanelDebug(IPanel panel)
        {
            this.panel = panel;
        }

        public void AttachDebugger(IPanelDebugger debugger)
        {
            if (debugger != null && m_Debuggers.Add(debugger))
            {
                debugger.panelDebug = this;
                MarkDirtyRepaint();
                InitializeDebuggerOverlayPanel();
            }
        }

        public void DetachDebugger(IPanelDebugger debugger)
        {
            if (debugger != null)
            {
                debugger.panelDebug = null;
                m_Debuggers.Remove(debugger);
                MarkDirtyRepaint();
                RemoveDebuggerOverlayPanel();
            }
        }

        public void DetachAllDebuggers()
        {
            foreach (var debugger in m_Debuggers)
            {
                debugger.panelDebug = null;
                debugger.Disconnect();
            }
            m_Debuggers.Clear();
            MarkDirtyRepaint();
            RemoveDebuggerOverlayPanel();
        }

        public IEnumerable<IPanelDebugger> GetAttachedDebuggers()
        {
            return m_Debuggers;
        }

        public void MarkDirtyRepaint()
        {
            panel.visualTree.MarkDirtyRepaint();
        }

        public void MarkDebugContainerDirtyRepaint()
        {
            if (debuggerOverlayPanel != null)
                debugContainer?.MarkDirtyRepaint();
        }

        public void Refresh()
        {
            UpdateOverlayPanelSize();

            // For world-space panels, the overlay content depends on the rendering camera, which
            // can move without any UI change. Regenerate the overlay every frame.
            if (panel is BaseRuntimePanel { drawsInCameras: true })
                MarkDebugContainerDirtyRepaint();

            foreach (var debugger in m_Debuggers)
            {
                debugger.Refresh();
            }
        }

        public void OnVersionChanged(VisualElement ele, VersionChangeType changeTypeFlag)
        {
            foreach (var debugger in m_Debuggers)
            {
                debugger.OnVersionChanged(ele, changeTypeFlag);
            }
        }

        public bool InterceptEvent(EventBase ev)
        {
            bool intercepted = false;
            foreach (var debugger in m_Debuggers)
            {
                intercepted |= debugger.InterceptEvent(ev);
            }

            return intercepted;
        }

        public void PostProcessEvent(EventBase ev)
        {
            foreach (var debugger in m_Debuggers)
            {
                debugger.PostProcessEvent(ev);
            }
        }
    }
}
