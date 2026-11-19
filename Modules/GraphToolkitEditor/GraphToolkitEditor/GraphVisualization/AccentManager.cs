// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Provides control over the visual progress and animation state of graph elements in a graph visualization session.
/// </summary>
/// <remarks>
/// Obtain an instance from <see cref="Session"/>. All methods operate on elements identified by their
/// <see cref="Hash128"/> ID and silently no-op when the target element does not support animation.
/// </remarks>
/// <typeparam name="TView">The concrete view type this manager targets, for example <see cref="CollapsibleInOutNodeView"/> or <see cref="StateView"/>.</typeparam>
/// <typeparam name="TModel">The model type an element ID must resolve to, for example <see cref="NodeModel"/> or <see cref="StateModel"/>.</typeparam>
class AccentManager<TView, TModel>
    where TView : ChildView, IAccentAnimatableView
    where TModel : GraphElementModel
{
    bool m_Enabled = true;

    /// <summary>
    /// Gets or sets whether accent requests are applied to the graph canvas.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, all cached requests are re-executed against the current graph canvas.
    /// When set to <c>false</c>, any active animations are stopped and fill amount overrides are cleared,
    /// but cached requests are preserved so they can be restored when re-enabled.
    /// </remarks>
    public bool Enabled
    {
        get => m_Enabled;
        set
        {
            if (m_Enabled == value)
                return;

            m_Enabled = value;
            if (m_Enabled)
                SetEnable();
            else
                SetDisable();
        }
    }

    readonly Session m_Session;
    readonly AccentStore m_Store;

    internal AccentManager(Session session, AccentStore store)
    {
        m_Session = session;
        m_Store = store;

        m_Session.isAttached += OnAttached;
        m_Session.isDetached += OnDetached;
    }

    void SetEnable()
    {
        if (m_Session.GraphView == null)
            return;

        ExecuteAllRequests();
    }

    void SetDisable()
    {
        if (m_Session.GraphView == null)
            return;

        DisableAllRequests();
    }

    void OnAttached()
    {
        if (m_Session.GraphView == null)
            return;

        if (Enabled)
            ExecuteAllRequests();
    }

    void OnDetached()
    {
        if (m_Session.GraphView == null)
            return;

        ClearAll();
    }

    void ExecuteAllRequests()
    {
        foreach (var req in m_Store.Requests)
        {
            if (!HasView(req.Key))
                continue;

            foreach (var requestPerGroup in req.Value)
            {
                switch (requestPerGroup)
                {
                    case PlayLoopAnimationRequest playRequest:
                        ExecutePlayLoopAnimation(playRequest);
                        break;
                    case PauseLoopAnimationRequest pauseRequest:
                        ExecuteRestorePausedLoopAnimation(pauseRequest);
                        break;
                    case StopLoopAnimationRequest stopRequest:
                        ExecuteStopLoopAnimation(stopRequest);
                        break;
                    case FillAmountRequest fillAmountRequest:
                        ExecuteFillAmountRequest(fillAmountRequest);
                        break;
                }
            }
        }
    }

    void DisableAllRequests()
    {
        foreach (var req in m_Store.Requests)
        {
            if (!HasView(req.Key))
                continue;

            foreach (var requestPerGroup in req.Value)
            {
                switch (requestPerGroup)
                {
                    case PlayLoopAnimationRequest:
                    case PauseLoopAnimationRequest:
                        ExecuteStopLoopAnimation(new StopLoopAnimationRequest(requestPerGroup.TargetID));
                        break;
                    case FillAmountRequest fillAmountRequest:
                        ExecuteClearFillAmount(fillAmountRequest.TargetID);
                        break;
                }
            }
        }
    }

    bool TryGetView(Hash128 id, out TView view)
    {
        return TryGetView(id, out view, out _);
    }

    bool TryGetView(Hash128 id, out TView view, out GraphView graphView)
    {
        graphView = m_Session.GraphView;
        view = graphView == null ? null : id.GetView<TView>(graphView);
        return view != null;
    }

    void ValidateTarget(Hash128 id)
    {
        if (!m_Session.IsElementValid<TModel>(id, out var element))
            throw new ArgumentException($"No element with ID {id} exists in graph \"{m_Session.GraphModel?.Name}\", its local subgraphs, or the graph the window currently displays.", nameof(id));

        if (element is AbstractNodeModel { HasAccentBar: false })
            throw new ArgumentException($"The element with ID {id} in graph \"{element.GraphModel?.Name}\" has no accent bar.", nameof(id));
    }

    bool HasView(Hash128 id) => id.GetView<TView>(m_Session.GraphView) != null;

    /// <summary>
    /// Sets the fill amount on an element's accent bar.
    /// </summary>
    /// <param name="id">The ID of the target element.</param>
    /// <param name="percentage">Fill value in the range [-100, 100].</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> does not correspond to an element in the session's graph, one of its local subgraphs, or the graph the window currently displays, or when the element has no accent bar.</exception>
    public void SetFillAmount(Hash128 id, float percentage)
    {
        ValidateTarget(id);

        var request = new FillAmountRequest(id, percentage);
        m_Store.CacheRequest(request);

        if (Enabled)
            ExecuteFillAmountRequest(request);
    }

    /// <summary>
    /// Retrieves the most recently requested fill amount for the specified element.
    /// </summary>
    /// <param name="id">The ID of the target element.</param>
    /// <param name="percentage">When this method returns, contains the last requested fill amount when one exists. Otherwise, <c>0</c>.</param>
    /// <returns>Returns <c>true</c> when a fill amount has previously been set on the element. Otherwise, returns <c>false</c>.</returns>
    public bool TryGetFillAmount(Hash128 id, out float percentage)
    {
        if (TryGetFillAmountRequest(id, out var request))
        {
            percentage = request.amount;
            return true;
        }

        percentage = 0f;
        return false;
    }

    bool TryGetFillAmountRequest(Hash128 id, out FillAmountRequest request)
    {
        if (m_Store.Requests.TryGetValue(id, out var requests))
        {
            foreach (var cached in requests)
            {
                if (cached is FillAmountRequest fillAmountRequest)
                {
                    request = fillAmountRequest;
                    return true;
                }
            }
        }

        request = null;
        return false;
    }

    /// <summary>
    /// Starts or resumes the looping animation on an element's accent bar.
    /// </summary>
    /// <param name="id">The ID of the target element.</param>
    /// <param name="animationSpeed">Speed multiplier for the animation. Higher values move the animated segment faster.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> does not correspond to an element in the session's graph, one of its local subgraphs, or the graph the window currently displays, or when the element has no accent bar.</exception>
    public void PlayLoopAnimation(Hash128 id, float animationSpeed)
    {
        ValidateTarget(id);

        var request = new PlayLoopAnimationRequest(id, animationSpeed);
        m_Store.CacheRequest(request);

        if (Enabled)
            ExecutePlayLoopAnimation(request);
    }

    /// <summary>
    /// Pauses the looping animation on an element's accent bar, preserving its current position.
    /// </summary>
    /// <param name="id">The ID of the target element.</param>
    /// <remarks>
    /// Does nothing when the element has no playing looping animation.
    /// Resuming the animation always restarts it from the beginning.
    /// Throws <see cref="ArgumentException"/> when <paramref name="id"/> does not correspond to an element in the session's graph, one of its local subgraphs, or the graph the window currently displays, or when the element has no accent bar.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> does not correspond to an element in the session's graph, one of its local subgraphs, or the graph the window currently displays, or when the element has no accent bar.</exception>
    public void PauseLoopAnimation(Hash128 id)
    {
        ValidateTarget(id);

        // Only a playing animation can be paused, matching GraphAnimator.Pause.
        if (!TryGetAnimationRequest(id, out var animationRequest) || animationRequest is not PlayLoopAnimationRequest playRequest)
            return;

        m_Store.CacheRequest(new PauseLoopAnimationRequest(id, playRequest.animationSpeed));

        if (Enabled && TryGetView(id, out var view, out var graphView))
            graphView.Animator.Pause(view);
    }

    bool TryGetAnimationRequest(Hash128 id, out AccentRequest request)
    {
        if (m_Store.Requests.TryGetValue(id, out var requests))
        {
            foreach (var cached in requests)
            {
                if (cached.requestGroup == AccentRequest.Group.Animation)
                {
                    request = cached;
                    return true;
                }
            }
        }

        request = null;
        return false;
    }

    /// <summary>
    /// Stops and hides the looping animation on an element's accent bar.
    /// </summary>
    /// <param name="id">The ID of the target element.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> does not correspond to an element in the session's graph, one of its local subgraphs, or the graph the window currently displays, or when the element has no accent bar.</exception>
    public void StopLoopAnimation(Hash128 id)
    {
        ValidateTarget(id);

        var request = new StopLoopAnimationRequest(id);
        m_Store.CacheRequest(request);

        if (!Enabled)
            return;

        ExecuteStopLoopAnimation(request);

        // Restore the fill amount.
        // If an override was set through a reference, the restored value corresponds to the override value.
        // Otherwise, the restored value corresponds to the model's fill amount.
        if (TryGetFillAmountRequest(id, out var fillRequest))
            ExecuteFillAmountRequest(fillRequest);
    }

    void ExecutePlayLoopAnimation(PlayLoopAnimationRequest request)
    {
        if (TryGetView(request.TargetID, out var view, out var graphView))
            graphView.Animator.Play(view, request.animationSpeed);
    }

    // The animation must be playing before the animator can pause it, so restoring a paused animation restarts it then pauses it.
    // The animation position is not preserved and restarts from the beginning when resumed.
    void ExecuteRestorePausedLoopAnimation(PauseLoopAnimationRequest request)
    {
        if (!TryGetView(request.TargetID, out var view, out var graphView))
            return;

        graphView.Animator.Play(view, request.animationSpeed);
        graphView.Animator.Pause(view);
    }

    void ExecuteStopLoopAnimation(StopLoopAnimationRequest request)
    {
        if (TryGetView(request.TargetID, out var view, out var graphView))
            graphView.Animator.Stop(view);
    }

    void ExecuteFillAmountRequest(FillAmountRequest request)
    {
        if (TryGetView(request.TargetID, out var view))
            view.OverrideFillAmount(request.amount);
    }

    void ExecuteClearFillAmount(Hash128 id)
    {
        if (TryGetView(id, out var view))
            view.ClearFillAmountOverride();
    }

    /// <summary>
    /// Removes all cached visual requests for the specified element, restoring the fill amount defined on its model and stopping any looping animation.
    /// </summary>
    /// <param name="id">The ID of the target element.</param>
    /// <remarks>
    /// The cached requests are removed whether or not the element currently has a view in the graph canvas.
    /// </remarks>
    public void ClearAccent(Hash128 id)
    {
        if (!m_Store.Requests.TryGetValue(id, out var requests))
            return;

        if (Enabled && m_Session.GraphView != null)
        {
            foreach (var request in requests)
            {
                switch (request)
                {
                    case PlayLoopAnimationRequest:
                    case PauseLoopAnimationRequest:
                        ExecuteStopLoopAnimation(new StopLoopAnimationRequest(id));
                        break;
                    case FillAmountRequest:
                        ExecuteClearFillAmount(id);
                        break;
                }
            }
        }

        m_Store.ClearAllRequestFor(id);
    }

    public void ClearAll()
    {
        if (Enabled)
            DisableAllRequests();

        m_Store.ClearAll();
    }
}
