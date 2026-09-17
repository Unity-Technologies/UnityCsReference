// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization
{
    /// <summary>
    /// Stores icon overrides for condition visuals in a graph visualization session.
    /// </summary>
    class ConditionVisualStore
    {
        readonly Dictionary<Hash128, Texture2D> m_ConditionIcons = new();

        internal IReadOnlyCollection<Hash128> OverriddenConditionIDs => m_ConditionIcons.Keys;

        internal void Set(Hash128 conditionID, Texture2D icon)
        {
            if (icon == null)
            {
                Clear(conditionID);
                return;
            }

            m_ConditionIcons[conditionID] = icon;
        }

        internal bool TryGet(Hash128 conditionID, out Texture2D icon)
        {
            return m_ConditionIcons.TryGetValue(conditionID, out icon);
        }

        internal void Clear(Hash128 conditionID)
        {
            m_ConditionIcons.Remove(conditionID);
        }

        internal void ClearAll()
        {
            m_ConditionIcons.Clear();
        }
    }
}
