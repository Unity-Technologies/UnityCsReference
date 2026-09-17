// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

class AccentStore
{
    readonly Dictionary<Hash128, List<AccentRequest>> m_Accents = new();

    public IReadOnlyDictionary<Hash128, List<AccentRequest>> Requests => m_Accents;

    public void CacheRequest(AccentRequest request)
    {
        bool TryReplaceRequestFromSameGroup(AccentRequest newRequest, List<AccentRequest> requests)
        {
            int index = requests.FindIndex(r => r.requestGroup == newRequest.requestGroup);
            if (index > -1)
            {
                requests[index] = newRequest;
                return true;
            }
            return false;
        }

        if (!m_Accents.TryGetValue(request.TargetID, out var list))
        {
            list = new List<AccentRequest>();
            m_Accents.Add(request.TargetID, list);
        }

        if (!TryReplaceRequestFromSameGroup(request, list))
        {
            list.Add(request);
        }
    }

    public void ClearAllRequestFor(Hash128 id)
    {
        m_Accents.Remove(id);
    }

    public void ClearAll()
    {
        m_Accents.Clear();
    }
}
