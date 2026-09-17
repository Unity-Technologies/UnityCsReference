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
class AccentManager<TView> where TView : ChildView, IAccentAnimatableView
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
            foreach (var requestPerGroup in req.Value)
            {
                switch (requestPerGroup)
                {
                    case PlayLoopAnimationRequest playRequest:
                        ExecutePlayLoopAnimation(playRequest);
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
            foreach (var requestPerGroup in req.Value)
            {
                switch (requestPerGroup)
                {
                    case PlayLoopAnimationRequest playRequest:
                        ExecuteStopLoopAnimation(new StopLoopAnimationRequest(playRequest.TargetID));
                        break;
                    case FillAmountRequest fillAmountRequest:
                        ExecuteClearFillAmount(fillAmountRequest.TargetID);
                        break;
                }
            }
        }
    }

    TView GetView(Hash128 id)
    {
        var view = id.GetView<TView>(m_Session.GraphView);
        if (view == null)
            throw new ArgumentException($"Invalid {typeof(TView).Name} id", nameof(id));
        return view;
    }

    /// <summary>
    /// Sets the fill amount on an element's accent bar.
    /// </summary>
    /// <param name="id">The ID of the target element.</param>
    /// <param name="percentage">Fill value in the range [-100, 100].</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> does not correspond to an element in the current session.</exception>
    public void SetFillAmount(Hash128 id, float percentage)
    {
        var request = new FillAmountRequest(id, percentage);
        m_Store.CacheRequest(request);

        if (!Enabled)
            return;

        if (m_Session.GraphView != null)
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
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> does not correspond to an element in the current session.</exception>
    public void PlayLoopAnimation(Hash128 id, float animationSpeed)
    {
        var request = new PlayLoopAnimationRequest(id, animationSpeed);
        m_Store.CacheRequest(request);

        if (!Enabled)
            return;

        if (m_Session.GraphView != null)
            ExecutePlayLoopAnimation(request);
    }

    /// <summary>
    /// Pauses the looping animation on an element's accent bar, preserving its current position.
    /// </summary>
    /// <param name="id">The ID of the target element.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> does not correspond to an element in the current session.</exception>
    public void PauseLoopAnimation(Hash128 id)
    {
        if (!Enabled)
            return;

        if (m_Session.GraphView != null)
            m_Session.GraphView.Animator.Pause(GetView(id));
    }

    /// <summary>
    /// Stops and hides the looping animation on an element's accent bar.
    /// </summary>
    /// <param name="id">The ID of the target element.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> does not correspond to an element in the current session.</exception>
    public void StopLoopAnimation(Hash128 id)
    {
        var request = new StopLoopAnimationRequest(id);
        m_Store.CacheRequest(request);

        if (!Enabled)
            return;

        if (m_Session.GraphView != null)
        {
            ExecuteStopLoopAnimation(request);

            // Restore the fill amount.
            // If an override was set through a reference, the restored value corresponds to the override value.
            // Otherwise, the restored value corresponds to the model's fill amount.
            if (TryGetFillAmountRequest(id, out var fillRequest))
                ExecuteFillAmountRequest(fillRequest);
        }
    }

    void ExecutePlayLoopAnimation(PlayLoopAnimationRequest request)
    {
        m_Session.GraphView.Animator.Play(GetView(request.TargetID), request.animationSpeed);
    }

    void ExecuteStopLoopAnimation(StopLoopAnimationRequest request)
    {
        m_Session.GraphView.Animator.Stop(GetView(request.TargetID));
    }

    void ExecuteFillAmountRequest(FillAmountRequest request)
    {
        GetView(request.TargetID).OverrideFillAmount(request.amount);
    }

    void ExecuteClearFillAmount(Hash128 id)
    {
        GetView(id).ClearFillAmountOverride();
    }

    /// <summary>
    /// Removes all cached visual requests for the specified element, restoring the fill amount defined on its model and stopping any looping animation.
    /// </summary>
    /// <param name="id">The ID of the target element.</param>
    /// <exception cref="ArgumentException">Thrown when the element has cached requests but no corresponding view exists in the current session.</exception>
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
