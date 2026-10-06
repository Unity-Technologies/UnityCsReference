// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Pool;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Sets, retrieves, and clears custom appearance for transitions in the graph canvas.
/// </summary>
/// <remarks>
/// Transition visuals are a visualization feature that overrides how a transition is drawn in the graph canvas, for example dash pattern, width, opacity, color, or animation.
/// </remarks>
sealed class TransitionVisualManager
{
    readonly Session m_Session;
    bool m_Enabled = true;

    TransitionVisualStore TransitionVisualStore => m_Session.Store.TransitionVisualStore;

    internal TransitionVisualManager(Session session)
    {
        m_Session = session;
        m_Session.isAttached += OnAttached;
        m_Session.isDetached += OnDetached;
    }

    /// <summary>
    /// Whether the transition visual feature is enabled for this visualization context.
    /// </summary>
    /// <remarks>
    /// Transition visuals are enabled by default.
    /// When enabled, appearances set through this manager are applied to matching transition views in the graph canvas.
    /// When disabled, transition views revert to their default appearance until this property is enabled again.
    /// </remarks>
    public bool Enabled
    {
        get => m_Enabled;
        set
        {
            if (value == m_Enabled)
                return;

            m_Enabled = value;

            // If the value has changed, we show or hide all current transition visuals accordingly.
            ShowAll(value);
        }
    }

    /// <summary>
    /// Sets the visual data for a given transition in the graph canvas.
    /// </summary>
    /// <param name="transitionID">The ID of the transition to update.</param>
    /// <param name="visualData">The visual data to apply, or <see langword="null"/> to clear any custom visual data.</param>
    /// <remarks>
    /// Setting a new value overwrites any existing visual data for that transition.
    /// Set the <paramref name="visualData"/> to <see langword="null"/> to clear the transition visual for the given transition. This restores the default transition drawing until you set a new visual.
    /// This is equivalent to calling <see cref="Clear(Hash128)"/> for the given transition.
    /// If the transition no longer resolves to any transition view in the graph (for example after it was removed), any stored visual data for that transition is removed and the call has no further effect on the canvas.
    /// </remarks>
    public void Set(Hash128 transitionID, TransitionVisualData visualData)
    {
        if (visualData == null || visualData.IsDefaultVisualData())
        {
            Clear(transitionID);
            return;
        }

        if (m_Session.GraphView != null && !TryGetTransitionView(transitionID, out _))
        {
            TransitionVisualStore.Clear(transitionID);
            NotifyIconChanged(transitionID);
            return;
        }

        TransitionVisualStore.Set(transitionID, visualData);
        SetTransitionVisual(transitionID, visualData);
        NotifyIconChanged(transitionID);
    }

    /// <summary>
    /// Retrieves the current <see cref="TransitionVisualData"/> assigned to the transition visual for a given transition.
    /// </summary>
    /// <param name="transitionID">The ID of the transition whose stored appearance to read.</param>
    /// <param name="value">When this method returns true, contains the transition appearance; otherwise, <see langword="null"/>.</param>
    /// <returns>true if a transition appearance was successfully retrieved; otherwise, false.</returns>
    /// <remarks>
    /// If no transition visual is currently set for the given transition, this method returns false and <paramref name="value"/> is <see langword="null"/>.
    /// </remarks>
    public bool TryGet(Hash128 transitionID, out TransitionVisualData value)
    {
        value = null;

        return TransitionVisualStore.TryGet(transitionID, out value);
    }

    internal void UpdateVisualData(Hash128 transitionID, Action<TransitionVisualData> update)
    {
        var visualData = TryGet(transitionID, out var data) ? data : new TransitionVisualData();
        update(visualData);
        Set(transitionID, visualData);
    }

    internal void PlayAnimation(Hash128 transitionID, float animationSpeed)
    {
        UpdateVisualData(transitionID, data =>
        {
            data.IsAnimating = true;
            data.AnimationSpeed = animationSpeed;
        });
    }

    internal void StopAnimation(Hash128 transitionID)
        => UpdateVisualData(transitionID, data => data.IsAnimating = false);

    internal void PauseAnimation(Hash128 transitionID)
    {
        if (!m_Enabled || m_Session.GraphView == null)
            return;

        if (!TryGetTransitionView(transitionID, out var transitionView))
            return;

        m_Session.GraphView.Animator.Pause(transitionView);
    }

    /// <summary>
    /// Clears the transition visual for the specified transition.
    /// </summary>
    /// <param name="transitionID">The ID of the transition whose custom appearance should be cleared.</param>
    /// <remarks>
    /// The transition reverts to the default drawing until you set a new appearance with <see cref="Set(Hash128, TransitionVisualData)"/>.
    /// If the transition no longer resolves to any transition view in the graph, any stored appearance for that transition is removed and the call has no further effect on the canvas.
    /// </remarks>
    public void Clear(Hash128 transitionID)
    {
        TransitionVisualStore.Clear(transitionID);
        NotifyIconChanged(transitionID);

        if (m_Session.GraphView != null && !TryGetTransitionView(transitionID, out _))
            return;

        ClearTransitionVisual(transitionID);
    }

    /// <summary>
    /// Clears all transition visuals in the current visualization context.
    /// </summary>
    /// <remarks>
    /// All transitions revert to default drawing until you set a new appearance with <see cref="Set(Hash128, TransitionVisualData)"/>.
    /// </remarks>
    public void ClearAll()
    {
        ClearAllInternal();
    }

    void ClearAllInternal()
    {
        using var pooled = ListPool<Hash128>.Get(out var transitionIDs);

        foreach (var (transitionID, _) in TransitionVisualStore.AllTransitionVisuals)
        {
            transitionIDs.Add(transitionID);
            ClearTransitionVisual(transitionID);
        }

        TransitionVisualStore.ClearAll();

        foreach (var transitionID in transitionIDs)
            NotifyIconChanged(transitionID);
    }

    internal void ShowAll(bool show)
    {
        if (m_Session.GraphView == null)
            return;

        foreach (var (transitionID, appearance) in TransitionVisualStore.AllTransitionVisuals)
        {
            NotifyIconChanged(transitionID);

            if (!TryGetTransitionView(transitionID, out var transitionView))
                continue;

            transitionView.SetAppearance(show ? appearance : null);
        }
    }

    bool TryGetTransitionView(Hash128 transitionID, out TransitionView transitionView)
    {
        transitionView = null;

        if (m_Session.GraphView == null)
            return false;

        var graphModel = m_Session.GraphView.GraphModel;
        if (!graphModel.TryGetModelFromGuid<TransitionSupportModel>(transitionID, out var transitionSupportModel))
            return false;

        transitionView = transitionSupportModel.GetView<TransitionView>(m_Session.GraphView);

        return transitionView != null;
    }

    void ClearTransitionVisual(Hash128 transitionID)
    {
        if (m_Session.GraphView == null)
            return;

        if (!TryGetTransitionView(transitionID, out var transitionView))
            return;

        transitionView.SetAppearance(null);
    }

    void SetTransitionVisual(Hash128 transitionID, TransitionVisualData visualData)
    {
        if (m_Session.GraphView == null || !Enabled)
            return;

        if (!TryGetTransitionView(transitionID, out var transitionView))
            return;

        transitionView.SetAppearance(visualData);
    }

    // The debug icon lives in the transition inspector, not the graph canvas, so an override never rides
    // TransitionView.SetAppearance and is pushed directly to the inspector views instead.
    void NotifyIconChanged(Hash128 transitionID)
    {
        var modelInspectorView = m_Session.ModelInspectorView;
        if (modelInspectorView == null)
            return;

        var overrideIcon = m_Enabled && TransitionVisualStore.TryGet(transitionID, out var data) ? data.Icon : null;

        using var pooled = ListPool<TransitionSupportEditor>.Get(out var views);
        transitionID.AppendAllViews(modelInspectorView, (TransitionSupportEditor _) => true, views);

        foreach (var view in views)
            view.SetIconOverride(overrideIcon);
    }

    void OnAttached()
    {
        // When the session is attached, we need to make sure all current transition visuals in the store are set.
        ShowAll(Enabled);
    }

    void OnDetached()
    {
        ClearAll();
    }
}
