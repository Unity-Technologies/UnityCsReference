// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Stores and implements the logic for visualization data related to transition visuals in a graph visualization session.
/// </summary>
class TransitionVisualStore
{
    readonly Dictionary<Hash128, TransitionVisualData> m_TransitionVisuals = new();
    internal IReadOnlyDictionary<Hash128, TransitionVisualData> AllTransitionVisuals => m_TransitionVisuals;

    internal void Set(Hash128 transitionID, TransitionVisualData transitionVisualData)
    {
        if (transitionVisualData == null)
        {
            Clear(transitionID);
            return;
        }

        m_TransitionVisuals[transitionID] = transitionVisualData;
    }

    internal bool TryGet(Hash128 transitionID, out TransitionVisualData value)
    {
        value = null;

        if (!m_TransitionVisuals.TryGetValue(transitionID, out var transitionVisualData))
            return false;

        value = transitionVisualData;

        return true;
    }

    internal void Clear(Hash128 transitionID)
    {
        m_TransitionVisuals.Remove(transitionID);
    }

    internal void ClearAll()
    {
        m_TransitionVisuals.Clear();
    }
}
