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

        private PanelOwner ownerObject;

        private void InitializeDebuggerOverlayPanel()
        {
            if (debuggerOverlayPanel == null)
            {
                ownerObject = ScriptableObject.CreateInstance<PanelOwner>();
                // All debug panels are context type Editor, even if they are Runtime (Player) panels because the
                // debug panel itself are in the Editor anyway.
                var debuggerOverlayTmpPanel = new Panel(ownerObject, ContextType.Editor, EventDispatcher.CreateDefault());
                debuggerOverlayTmpPanel.clearSettings = new PanelClearSettings();
                debuggerOverlayPanel = debuggerOverlayTmpPanel;
                debuggerOverlayPanel.visualTree.layout = panel.visualTree.layout;
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

        private void RemoveDebuggerOverlayPanel()
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
