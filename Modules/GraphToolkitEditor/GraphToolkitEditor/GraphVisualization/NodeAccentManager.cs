// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Provides control over the visual progress and animation state of nodes in a graph visualization session.
/// </summary>
/// <remarks>
/// Obtain an instance from <see cref="Session"/>. All methods operate on nodes identified by their
/// <see cref="Hash128"/> ID and silently no-op when the target node does not support animation.
/// </remarks>
class NodeAccentManager
{
    bool m_Enabled = true;

    /// <summary>
    /// Gets or sets whether node accent requests are applied to the graph canvas.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, all cached requests are re-executed against the current graph canvas.
    /// When set to <c>false</c>, any active animations are stopped and progress fills are cleared,
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

    internal NodeAccentManager(Session session)
    {
        m_Session = session;

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
        foreach (var req in m_Session.Store.NodeAccentStore.Requests)
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
        foreach (var req in m_Session.Store.NodeAccentStore.Requests)
        {
            foreach (var requestPerGroup in req.Value)
            {
                switch (requestPerGroup)
                {
                    case PlayLoopAnimationRequest playRequest:
                        ExecuteStopLoopAnimation(new StopLoopAnimationRequest(playRequest.NodeID));
                        break;
                    case FillAmountRequest fillAmountRequest:
                        ExecuteFillAmountRequest(new FillAmountRequest(fillAmountRequest.NodeID, 0f));
                        break;
                }
            }
        }
    }

    CollapsibleInOutNodeView GetNodeView(Hash128 NodeID)
    {
        var nodeView = NodeID.GetView<CollapsibleInOutNodeView>(m_Session.GraphView);
        if (nodeView == null)
            throw new ArgumentException("Invalid node ID", nameof(NodeID));
        return nodeView;
    }

    /// <summary>
    /// Sets the fill amount on a node's progress bar.
    /// </summary>
    /// <param name="NodeID">The ID of the target node.</param>
    /// <param name="percentage">Progress value in the range [-100, 100].</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="NodeID"/> does not correspond to a node in the current session.</exception>
    public void SetFillAmount(Hash128 NodeID, float percentage)
    {
        var request = new FillAmountRequest(NodeID, percentage);
        m_Session.Store.NodeAccentStore.CacheRequest(request);

        if (!Enabled)
            return;

        if (m_Session.GraphView != null)
            ExecuteFillAmountRequest(request);
    }

    /// <summary>
    /// Retrieves the most recently requested fill amount for the specified node.
    /// </summary>
    /// <param name="NodeID">The ID of the target node.</param>
    /// <param name="percentage">When this method returns, contains the last requested fill amount when one exists. Otherwise, <c>0</c>.</param>
    /// <returns>Returns <c>true</c> when a fill amount has previously been set on the node. Otherwise, returns <c>false</c>.</returns>
    public bool TryGetFillAmount(Hash128 NodeID, out float percentage)
    {
        if (TryGetFillAmountRequest(NodeID, out var request))
        {
            percentage = request.amount;
            return true;
        }

        percentage = 0f;
        return false;
    }

    bool TryGetFillAmountRequest(Hash128 NodeID, out FillAmountRequest request)
    {
        if (m_Session.Store.NodeAccentStore.Requests.TryGetValue(NodeID, out var requests))
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
    /// Starts or resumes the looping animation on a node's color line.
    /// </summary>
    /// <param name="NodeID">The ID of the target node.</param>
    /// <param name="animationSpeed">Speed multiplier for the animation. Higher values move the animated segment faster.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="NodeID"/> does not correspond to a node in the current session.</exception>
    public void PlayLoopAnimation(Hash128 NodeID, float animationSpeed)
    {
        var request = new PlayLoopAnimationRequest(NodeID, animationSpeed);
        m_Session.Store.NodeAccentStore.CacheRequest(request);

        if (!Enabled)
            return;

        if (m_Session.GraphView != null)
            ExecutePlayLoopAnimation(request);
    }

    /// <summary>
    /// Pauses the looping animation on a node's color line, preserving its current position.
    /// </summary>
    /// <param name="NodeID">The ID of the target node.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="NodeID"/> does not correspond to a node in the current session.</exception>
    public void PauseLoopAnimation(Hash128 NodeID)
    {
        if (!Enabled)
            return;

        if (m_Session.GraphView != null)
            m_Session.GraphView.Animator.Pause(GetNodeView(NodeID));
    }

    /// <summary>
    /// Stops the looping animation on a node's color line and resets it to the initial position.
    /// </summary>
    /// <param name="NodeID">The ID of the target node.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="NodeID"/> does not correspond to a node in the current session.</exception>
    public void StopLoopAnimation(Hash128 NodeID)
    {
        var request = new StopLoopAnimationRequest(NodeID);
        m_Session.Store.NodeAccentStore.CacheRequest(request);

        if (!Enabled)
            return;

        if (m_Session.GraphView != null)
            ExecuteStopLoopAnimation(request);
    }

    void ExecutePlayLoopAnimation(PlayLoopAnimationRequest request)
    {
        m_Session.GraphView.Animator.Play(GetNodeView(request.NodeID), request.animationSpeed);
    }

    void ExecuteStopLoopAnimation(StopLoopAnimationRequest request)
    {
        m_Session.GraphView.Animator.Stop(GetNodeView(request.NodeID));
    }

    void ExecuteFillAmountRequest(FillAmountRequest request)
    {
        GetNodeView(request.NodeID).SetFillAmount(request.amount);
    }

    /// <summary>
    /// Removes all cached visual requests for the specified node and resets its visual state, hiding any fill and stopping any looping animation.
    /// </summary>
    /// <param name="NodeID">The ID of the target node.</param>
    /// <exception cref="ArgumentException">Thrown when the node has cached requests but no corresponding view exists in the current session.</exception>
    public void ClearNodeAccent(Hash128 NodeID)
    {
        if (!m_Session.Store.NodeAccentStore.Requests.TryGetValue(NodeID, out var requests))
            return;

        if (Enabled && m_Session.GraphView != null)
        {
            foreach (var request in requests)
            {
                switch (request)
                {
                    case PlayLoopAnimationRequest:
                        ExecuteStopLoopAnimation(new StopLoopAnimationRequest(NodeID));
                        break;
                    case FillAmountRequest:
                        ExecuteFillAmountRequest(new FillAmountRequest(NodeID, 0f));
                        break;
                }
            }
        }

        m_Session.Store.NodeAccentStore.ClearAllRequestFor(NodeID);
    }

    public void ClearAll()
    {
        if (Enabled)
            DisableAllRequests();

        m_Session.Store.NodeAccentStore.ClearAll();
    }
}
