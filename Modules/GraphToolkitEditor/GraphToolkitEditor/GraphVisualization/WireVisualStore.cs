// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization;

/// <summary>
/// Stores and implements the logic for visualization data related to wire visuals in a graph visualization session.
/// </summary>
class WireVisualStore
{
    readonly Dictionary<(Hash128 OutputPortID, Hash128 InputPortID), WireVisualData> m_WireVisuals = new();
    internal IReadOnlyDictionary<(Hash128 OutputPortID, Hash128 InputPortID), WireVisualData> AllWireVisuals => m_WireVisuals;

    static (Hash128 OutputPortID, Hash128 InputPortID) GetKey(WireReference wireReference)
        => (wireReference.OutputPortID, wireReference.InputPortID);

    internal void Set(WireReference wireReference, WireVisualData wireVisualData)
    {
        if (wireVisualData == null)
        {
            Clear(wireReference);
            return;
        }

        m_WireVisuals[GetKey(wireReference)] = wireVisualData;
    }

    internal bool TryGet(WireReference wireReference, out WireVisualData value)
    {
        value = null;

        if (!m_WireVisuals.TryGetValue(GetKey(wireReference), out var wireVisualData))
            return false;

        value = wireVisualData;

        return true;
    }

    internal void Clear(WireReference wireReference)
    {
        m_WireVisuals.Remove(GetKey(wireReference));
    }

    internal void ClearAll()
    {
        m_WireVisuals.Clear();
    }
}
