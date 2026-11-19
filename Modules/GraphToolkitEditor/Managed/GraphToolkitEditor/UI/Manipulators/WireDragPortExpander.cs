// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Temporarily expands the collapsed expandable ports that are hovered while a wire is dragged.
    /// </summary>
    /// <remarks>
    /// A port expands once the mouse rests on it for <see cref="k_AutoExpandDelayMs"/>.
    /// It stays expanded while the mouse is over it or over one of its sub ports, and all ports collapse when the drag stops.
    /// </remarks>
    class WireDragPortExpander
    {
        // Same delay as the editor tooltips.
        internal const long k_AutoExpandDelayMs = 400;
        const long k_UpdateIntervalMs = 50;

        readonly WireDragHelper m_WireDragHelper;
        readonly List<PortModel> m_ExpandedPorts = new();
        IVisualElementScheduledItem m_ScheduledUpdate;
        Vector2 m_MousePosition;
        PortModel m_PortWaitingToExpand;
        long m_WaitStartTime;
        long m_AutoExpandDelayMs = k_AutoExpandDelayMs;
        bool m_IsActive;
        bool m_ScheduledUpdatesEnabled = true;

        GraphView GraphView => m_WireDragHelper.GraphView;

        public WireDragPortExpander(WireDragHelper wireDragHelper)
        {
            m_WireDragHelper = wireDragHelper;
        }

        public void Start(Vector2 mousePosition)
        {
            m_IsActive = true;
            m_MousePosition = mousePosition;
            m_PortWaitingToExpand = null;

            // Mouse events stop when the mouse stays still, but the expand delay must still elapse and the graph can auto-pan under it.
            m_ScheduledUpdate ??= GraphView.schedule.Execute(OnScheduledUpdate).Every(k_UpdateIntervalMs);
            m_ScheduledUpdate.Resume();
        }

        public void SetMousePosition(Vector2 mousePosition)
        {
            m_MousePosition = mousePosition;
        }

        public void Stop()
        {
            if (!m_IsActive)
                return;

            m_IsActive = false;
            m_ScheduledUpdate?.Pause();

            if (m_ExpandedPorts.Count > 0)
            {
                var portsToCollapse = m_ExpandedPorts.ToArray();
                m_ExpandedPorts.Clear();
                SetTemporarilyExpanded(portsToCollapse, false);
            }
        }

        void OnScheduledUpdate(TimerState timerState)
        {
            if (m_ScheduledUpdatesEnabled)
                Update(timerState.now);
        }

        void Update(long now)
        {
            if (!m_IsActive)
                return;

            var collapsed = CollapsePortsNoLongerHovered();
            var expanded = ExpandPortHoveredLongEnough(now);

            // Sub port views are greyed out when they are attached, but their models must be compatible ports right away.
            if (collapsed || expanded)
                m_WireDragHelper.RefreshCompatiblePorts(m_MousePosition);
        }

        bool CollapsePortsNoLongerHovered()
        {
            using var dispose = ListPool<PortModel>.Get(out var portsToCollapse);

            for (var i = m_ExpandedPorts.Count - 1; i >= 0; i--)
            {
                var port = m_ExpandedPorts[i];
                if (ShouldStayExpanded(port))
                    continue;

                portsToCollapse.Add(port);
                m_ExpandedPorts.RemoveAt(i);
            }

            if (portsToCollapse.Count == 0)
                return false;

            SetTemporarilyExpanded(portsToCollapse, false);
            return true;
        }

        bool ExpandPortHoveredLongEnough(long now)
        {
            var hoveredPort = GetHoveredCollapsedPort();
            if (hoveredPort != m_PortWaitingToExpand)
            {
                m_PortWaitingToExpand = hoveredPort;
                m_WaitStartTime = now;
            }

            if (m_PortWaitingToExpand == null || now - m_WaitStartTime < m_AutoExpandDelayMs)
                return false;

            var port = m_PortWaitingToExpand;
            m_PortWaitingToExpand = null;
            SetTemporarilyExpanded(new[] { port }, true);

            // The command ignores replaced ports. The refresh that follows replaces them in the ports to hover.
            if (port.IsShownExpanded)
                m_ExpandedPorts.Add(port);
            return true;
        }

        void SetTemporarilyExpanded(IReadOnlyList<PortModel> ports, bool expand)
        {
            GraphView.Dispatch(new TemporarilyExpandPortCommand(expand, ports));
        }

        PortModel GetHoveredCollapsedPort()
        {
            var allPorts = m_WireDragHelper.AllPorts;
            if (allPorts == null)
                return null;

            for (var i = 0; i < allPorts.Count; i++)
            {
                var port = allPorts[i];
                if (!port.IsShownExpanded && m_WireDragHelper.CanHaveCompatibleSubPorts(port) && IsMouseOverPort(port))
                    return port;
            }

            return null;
        }

        // A port whose layout is pending is kept expanded, otherwise its new sub ports would collapse it before they are laid out.
        bool ShouldStayExpanded(PortModel port)
        {
            if (IsLayoutPending(port) || IsMouseOverPort(port))
                return true;

            foreach (var subPort in port.SubPorts)
            {
                if (ShouldStayExpanded(subPort))
                    return true;
            }

            return false;
        }

        bool IsLayoutPending(PortModel port)
        {
            var portUI = GetPortViewOnVisibleNode(port);
            if (portUI == null || portUI.resolvedStyle.display == DisplayStyle.None)
                return false;

            // NaN sizes also fail these comparisons.
            var bounds = portUI.worldBound;
            return portUI.panel == null || !(bounds.width > 0 && bounds.height > 0);
        }

        bool IsMouseOverPort(PortModel port)
        {
            var portUI = GetPortViewOnVisibleNode(port);
            if (portUI?.panel == null || portUI.resolvedStyle.visibility != Visibility.Visible || portUI.resolvedStyle.display == DisplayStyle.None)
                return false;

            return portUI.worldBound.Contains(m_MousePosition) || Port.GetPortHitBoxBounds(portUI).Contains(m_MousePosition);
        }

        // Culled nodes are detached from the panel and get no layout until they are visible again.
        Port GetPortViewOnVisibleNode(PortModel port)
        {
            var portUI = port.GetView<Port>(GraphView);
            if (portUI == null)
                return null;

            var nodeUI = port.NodeModel?.GetView<NodeView>(GraphView);
            return nodeUI != null && nodeUI.IsCulled() ? null : portUI;
        }

        internal class TestAccess
        {
            readonly WireDragPortExpander m_Expander;

            public TestAccess(WireDragPortExpander expander)
            {
                m_Expander = expander;
            }

            public long AutoExpandDelayMs
            {
                get => m_Expander.m_AutoExpandDelayMs;
                set => m_Expander.m_AutoExpandDelayMs = value;
            }

            public IReadOnlyList<PortModel> ExpandedPorts => m_Expander.m_ExpandedPorts;

            public bool ScheduledUpdatesEnabled
            {
                get => m_Expander.m_ScheduledUpdatesEnabled;
                set => m_Expander.m_ScheduledUpdatesEnabled = value;
            }

            public void Update(long now)
            {
                m_Expander.Update(now);
            }
        }
    }
}
