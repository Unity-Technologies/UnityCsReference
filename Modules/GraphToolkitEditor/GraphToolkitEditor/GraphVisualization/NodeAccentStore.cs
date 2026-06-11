// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

class NodeAccentStore
{
    readonly Dictionary<Hash128, List<NodeAccentRequest>> m_NodeAccents = new();

    public IReadOnlyDictionary<Hash128, List<NodeAccentRequest>> Requests => m_NodeAccents;

    public int GetExistingRequests(List<NodeAccentRequest> preallocatedBuffer)
    {
        if (preallocatedBuffer == null)
            throw new ArgumentNullException(nameof(preallocatedBuffer));

        preallocatedBuffer.Clear();
        int count = 0;
        foreach (var request in m_NodeAccents)
        {
            foreach (var requestPerGroup in request.Value)
            {
                preallocatedBuffer.Add(requestPerGroup);
                count++;
            }
        }

        return count;
    }

    public void PlayLoopAnimation(PlayLoopAnimationRequest request)
    {
        CacheRequest(request);
    }

    public void StopLoopAnimation(StopLoopAnimationRequest request)
    {
        CacheRequest(request);
    }

    public void FillAmountRequest(FillAmountRequest request)
    {
        CacheRequest(request);
    }

    public void CacheRequest(NodeAccentRequest request)
    {
        bool TryReplaceRequestFromSameGroup(NodeAccentRequest newRequest, List<NodeAccentRequest> requests)
        {
            int index = requests.FindIndex(r => r.requestGroup == newRequest.requestGroup);
            if (index > -1)
            {
                requests[index] = newRequest;
                return true;
            }
            return false;
        }

        if (!m_NodeAccents.TryGetValue(request.NodeID, out var list))
        {
            list = new List<NodeAccentRequest>();
            m_NodeAccents.Add(request.NodeID, list);
        }

        if (!TryReplaceRequestFromSameGroup(request, list))
        {
            list.Add(request);
        }
    }

    public void ClearAllRequestFor(Hash128 nodeID)
    {
        m_NodeAccents.Remove(nodeID);
    }

    public void ClearAll()
    {
        m_NodeAccents.Clear();
    }
}
