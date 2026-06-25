// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Sets, retrieves, and clears custom appearance for wires in the graph canvas.
/// </summary>
/// <remarks>
/// Wire visuals are a visualization feature that overrides how a <see cref="WireReference"/> is drawn in the graph canvas, for example dash pattern, width, opacity, or animation.
/// Use <see cref="Context.WireVisuals"/> to access the wire visual manager from a visualization context.
/// </remarks>
sealed class WireVisualManager
{
    readonly Session m_Session;
    bool m_Enabled = true;

    WireVisualStore WireVisualStore => m_Session.Store.WireVisualStore;

    internal WireVisualManager(Session session)
    {
        m_Session = session;
        m_Session.isAttached += OnAttached;
        m_Session.isDetached += OnDetached;
    }

    /// <summary>
    /// Whether the wire visual feature is enabled for this visualization context.
    /// </summary>
    /// <remarks>
    /// Wire visuals are enabled by default.
    /// When enabled, appearances set through this manager are applied to matching wire views in the graph canvas.
    /// When disabled, wire views revert to their default appearance until this property is enabled again.
    /// </remarks>
    public bool Enabled
    {
        get => m_Enabled;
        set
        {
            if (value == m_Enabled)
                return;

            m_Enabled = value;

            // If the value has changed, we show or hide all current wire visuals accordingly.
            ShowAll(value);
        }
    }

    /// <summary>
    /// Sets the visual data for a given wire reference in the graph canvas.
    /// </summary>
    /// <param name="wireReference">The wire to update.</param>
    /// <param name="visualData">The visualData to apply, or <see langword="null"/> to clear any custom visualData.</param>
    /// <remarks>
    /// Setting a new value overwrites any existing visualData for that wire.
    /// Set the <paramref name="visualData"/> to <see langword="null"/> to clear the wire visual for the given wire. This restores the default wire drawing until you set a new visual.
    /// This is equivalent to calling <see cref="Clear(WireReference)"/> for the given wire.
    /// If the wire no longer resolves to any wire view in the graph (for example after the connection was removed), any stored visualData for that wire is removed and the call has no further effect on the canvas.
    /// </remarks>
    public void Set(WireReference wireReference, WireVisualData visualData)
    {
        if (visualData == null || visualData.IsDefaultVisualData())
        {
            Clear(wireReference);
            return;
        }

        if (m_Session.GraphView != null && !TryGetWireViews(wireReference, out _))
        {
            WireVisualStore.Clear(wireReference);
            return;
        }

        WireVisualStore.Set(wireReference, visualData);
        SetWireVisual(wireReference, visualData);
    }

    /// <summary>
    /// Retrieves the current <see cref="WireVisualData"/> assigned to the wire visual for a given wire reference.
    /// </summary>
    /// <param name="wireReference">The wire whose stored appearance to read.</param>
    /// <param name="value">When this method returns true, contains the wire appearance; otherwise, <see langword="null"/>.</param>
    /// <returns>true if a wire appearance was successfully retrieved; otherwise, false.</returns>
    /// <remarks>
    /// If no wire visual is currently set for the given wire reference, this method returns false and <paramref name="value"/> is <see langword="null"/>.
    /// </remarks>
    public bool TryGet(WireReference wireReference, out WireVisualData value)
    {
        value = null;

        return WireVisualStore.TryGet(wireReference, out value);
    }

    internal void UpdateVisualData(WireReference wireReference, Action<WireVisualData> update)
    {
        var visualData = TryGet(wireReference, out var data) ? data : new WireVisualData();
        update(visualData);
        Set(wireReference, visualData);
    }

    internal void PlayAnimation(WireReference wireReference, float animationSpeed)
    {
        UpdateVisualData(wireReference, data =>
        {
            data.IsAnimating = true;
            data.AnimationSpeed = animationSpeed;
        });
    }

    internal void StopAnimation(WireReference wireReference)
        => UpdateVisualData(wireReference, data => data.IsAnimating = false);

    internal void PauseAnimation(WireReference wireReference)
    {
        if (!m_Enabled || m_Session.GraphView == null)
            return;

        if (!TryGetWireViews(wireReference, out var wireViews))
            return;

        foreach (var wireView in wireViews)
            m_Session.GraphView.Animator.Pause(wireView);
    }

    /// <summary>
    /// Clears the wire visual for the specified wire reference.
    /// </summary>
    /// <param name="wireReference">The wire whose custom appearance should be cleared.</param>
    /// <remarks>
    /// The wire reverts to the default drawing until you set a new appearance with <see cref="Set(WireReference, WireVisualData)"/>.
    /// If the wire reference no longer resolves to any wire view in the graph, any stored appearance for that wire reference is removed and the call has no further effect on the canvas.
    /// </remarks>
    public void Clear(WireReference wireReference)
    {
        WireVisualStore.Clear(wireReference);

        if (m_Session.GraphView != null && !TryGetWireViews(wireReference, out _))
            return;

        ClearWireVisual(wireReference);
    }

    /// <summary>
    /// Clears all wire visuals in the current visualization context.
    /// </summary>
    /// <remarks>
    /// All wires revert to default drawing until you set a new appearances with <see cref="Set(WireReference, WireVisualData)"/>.
    /// </remarks>
    public void ClearAll()
    {
        ClearAllInternal();
    }

    void ClearAllInternal()
    {
        foreach (var (key, _) in WireVisualStore.AllWireVisuals)
        {
            ClearWireVisual(key.OutputPortID, key.InputPortID);
        }

        WireVisualStore.ClearAll();
    }

    internal void ShowAll(bool show)
    {
        if (m_Session.GraphView == null)
            return;

        foreach (var (key, appearance) in WireVisualStore.AllWireVisuals)
        {
            if (!TryGetWireViews(key.OutputPortID, key.InputPortID, out var wireViews))
                continue;

            foreach (var wireView in wireViews)
            {
                wireView.SetAppearance(show ? appearance : null);
            }
        }
    }

    bool TryGetWireViews(WireReference wireReference, out List<WireView> wireViews)
        => TryGetWireViews(wireReference.OutputPortID, wireReference.InputPortID, out wireViews);

    bool TryGetWireViews(Hash128 outputPortID, Hash128 inputPortID, out List<WireView> wireViews)
    {
        wireViews = null;

        if (m_Session.GraphView == null)
            return false;

        var graphModel = m_Session.GraphView.GraphModel;
        if (!graphModel.TryGetModelFromGuid(outputPortID, out PortModel outputPort) || outputPort.Direction != PortDirection.Output)
            return false;

        if (!graphModel.TryGetModelFromGuid(inputPortID, out PortModel inputPort) || inputPort.Direction != PortDirection.Input)
            return false;

        if (outputPort.GraphModel != graphModel || inputPort.GraphModel != graphModel)
            return false;

        // A single public wire reference can map to multiple WireModels via the virtual wire (e.g. when
        // portals are involved), so we need to walk every underlying model to collect its view.
        if (!VirtualWireBuilder.TryGetVirtualWire(outputPort, inputPort, out var virtualWire))
            return false;

        var wireModels = virtualWire.Wires;
        for (var i = 0; i < wireModels.Count; i++)
        {
            var wireModel = wireModels[i];
            if (wireModel == null)
                continue;

            var wireView = wireModel.GetView<WireView>(m_Session.GraphView);
            if (wireView == null)
                continue;

            wireViews ??= new List<WireView>(wireModels.Count);
            wireViews.Add(wireView);
        }

        return wireViews != null;
    }

    void ClearWireVisual(WireReference wireReference)
        => ClearWireVisual(wireReference.OutputPortID, wireReference.InputPortID);

    void ClearWireVisual(Hash128 outputPortID, Hash128 inputPortID)
    {
        if (m_Session.GraphView == null)
            return;

        if (!TryGetWireViews(outputPortID, inputPortID, out var wireViews))
            return;

        foreach (var wireView in wireViews)
        {
            wireView.SetAppearance(null);
        }
    }

    void SetWireVisual(WireReference wireReference, WireVisualData visualData)
    {
        if (m_Session.GraphView == null || !Enabled)
            return;

        if (!TryGetWireViews(wireReference, out var wireViews))
            return;

        foreach (var wireView in wireViews)
        {
            wireView.SetAppearance(visualData);
        }
    }

    void OnAttached()
    {
        // When the session is attached, we need to make sure all current wire visuals in the store are set.
        ShowAll(Enabled);
    }

    void OnDetached()
    {
        ClearAll();
    }
}
