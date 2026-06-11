// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Sets, retrieves, and clears port preview labels for ports in the graph canvas.
/// </summary>
/// <remarks>
/// Port preview is a visualization feature that displays a label next to a port in the graph canvas. This label provides additional information about the port's value or state.
/// Use <see cref="Context.PortPreview"/> to access the port preview manager from a visualization context.
/// </remarks>
class PortPreviewManager
{
    readonly Session m_Session;
    bool m_Enabled = true;

    PortPreviewStore PortPreviewStore => m_Session.Store.PortPreviewStore;

    internal PortPreviewManager(Session session)
    {
        m_Session = session;
        m_Session.isAttached += OnAttached;
        m_Session.isDetached += OnDetached;
    }

    public bool Enabled
    {
        get => m_Enabled;
        set
        {
            if (value == m_Enabled)
                return;

            m_Enabled = value;

            // If the value has changed, we show or hide all current port previews accordingly.
            ShowAll(value);
        }
    }

    public void Set(Hash128 portGuid, string displayValue)
    {
        // If the value is null, we remove the port preview data from the cache (it is cleared).
        if (displayValue == null)
        {
            Clear(portGuid);
            return;
        }

        // If the port no longer exists in the graph, prune any stale store entry and bail out.
        if (m_Session.GraphView != null && !TryGetPortView(portGuid, out _))
        {
            PortPreviewStore.Clear(portGuid);
            return;
        }

        // Update the store.
        PortPreviewStore.Set(portGuid, displayValue);

        // Update the view.
        SetPortPreview(portGuid, displayValue);
    }

    public bool TryGet(Hash128 portGuid, out string value)
    {
        value = null;

        return PortPreviewStore.TryGet(portGuid, out value);
    }

    public void Clear(Hash128 portGuid)
    {
        PortPreviewStore.Clear(portGuid);

        // If the port no longer exists in the graph, nothing to remove from the view.
        if (m_Session.GraphView != null && !TryGetPortView(portGuid, out _))
            return;

        ClearPortPreview(portGuid);
    }

    /// <summary>
    /// Clears all port previews in the current visualization context.
    /// </summary>
    /// <remarks>
    /// All port previews are removed from the graph canvas until new values are set using <see cref="Set(Hash128,string)"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// context.PortPreview.ClearAll();
    /// </code>
    /// </example>
    public void ClearAll()
    {
        foreach (var (portGuid, _) in PortPreviewStore.AllPortPreviewData)
        {
            ClearPortPreview(portGuid);
        }

        PortPreviewStore.ClearAll();
    }

    internal void ShowAll(bool show)
    {
        if (m_Session.GraphView == null)
            return;

        foreach (var (portGuid, _) in PortPreviewStore.AllPortPreviewData)
        {
            if (TryGetPortView(portGuid, out var portView))
                portView.PortPreview?.Show(show);
        }
    }

    bool TryGetPortView(Hash128 portGuid, out Port portView)
    {
        portView = portGuid.GetView<Port>(m_Session.GraphView);
        return portView != null;
    }

    void ClearPortPreview(Hash128 portGuid)
    {
        if (m_Session.GraphView == null)
            return;

        if (!TryGetPortView(portGuid, out var portView))
            return;

        portView.RemovePortPreview();

        // If the port is culled, schedule removal for when it re-attaches. The immediate
        // RemovePortPreview() above handles DisconnectFromTarget() to prevent ghost re-appearance,
        // but we still register OnNextAttach in case a new preview was pending from a prior Set() call.
        if (portView.panel == null)
            portView.OnNextAttach(() => portView.RemovePortPreview());
    }

    void SetPortPreview(Hash128 portGuid, string displayValue)
    {
        if (m_Session.GraphView == null)
            return;

        if (!TryGetPortView(portGuid, out var portView))
            return;

        // Update the existing preview (works even when detached/culled).
        portView.PortPreview?.Set(displayValue, Enabled);

        // If the port is culled and has no preview yet, schedule creation for when it re-attaches.
        if (portView.panel == null)
            portView.OnNextAttach(() =>
            {
                if (PortPreviewStore.TryGet(portGuid, out var value) && value != null)
                    portView.PortPreview?.Set(value, Enabled);
            });
    }

    void OnAttached()
    {
        if (m_Session.GraphView == null)
            return;

        // When the session is attached, we need to make sure all current port previews in the store are set.
        foreach (var (portGuid, data) in PortPreviewStore.AllPortPreviewData)
        {
            // Only need to update the views.
            SetPortPreview(portGuid, data.StringValue);
        }
    }

    void OnDetached()
    {
        ClearAll();
    }
}
