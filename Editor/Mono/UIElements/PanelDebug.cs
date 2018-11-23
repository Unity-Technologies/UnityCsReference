// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.UIElements
{
    internal class PanelDebug : IPanelDebug
    {
        private HashSet<IPanelDebugger> m_Debuggers = new HashSet<IPanelDebugger>();

        public IPanel panel { get; }
        public VisualElement visualTree { get { return panel?.visualTree; }}

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
            }
        }

        public void DetachDebugger(IPanelDebugger debugger)
        {
            if (debugger != null)
            {
                debugger.panelDebug = null;
                m_Debuggers.Remove(debugger);
                MarkDirtyRepaint();
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
        }

        public IEnumerable<IPanelDebugger> GetAttachedDebuggers()
        {
            return m_Debuggers;
        }

        public void MarkDirtyRepaint()
        {
            panel.visualTree.MarkDirtyRepaint();
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
