// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using Unity.CSO;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// Updates the breadcrumbs when the tool state changes or when a subgraph node they show changes.
    /// </summary>
    class BreadcrumbsUpdateObserver : StateObserver
    {
        IToolbarElement m_Breadcrumbs;

        ToolStateComponent m_ToolState;

        GraphModelStateComponent m_GraphModelState;

        public BreadcrumbsUpdateObserver(IToolbarElement breadcrumbs, ToolStateComponent toolState, GraphModelStateComponent graphModelState)
            : base(toolState, graphModelState)
        {
            m_Breadcrumbs = breadcrumbs;
            m_ToolState = toolState;
            m_GraphModelState = graphModelState;
        }

        public override void Observe()
        {
            using (var toolObservation = this.ObserveState(m_ToolState))
            using (var graphModelObservation = this.ObserveState(m_GraphModelState))
            {
                if (toolObservation.UpdateType != UpdateType.None ||
                    graphModelObservation.UpdateType == UpdateType.Complete ||
                    (graphModelObservation.UpdateType == UpdateType.Partial && HasChangedSubgraphNode(graphModelObservation.LastObservedVersion)))
                {
                    m_Breadcrumbs.Update();
                }
            }
        }

        // A breadcrumb node can only be in the displayed graph when that graph is also on the stack (recursive subgraphs).
        bool HasChangedSubgraphNode(uint sinceVersion)
        {
            if (m_ToolState.SubgraphStack.Count == 0)
                return false;

            var changeset = m_GraphModelState.GetAggregatedChangeset(sinceVersion);
            if (changeset == null)
                return false;

            for (var i = -1; i < m_ToolState.SubgraphStack.Count; i++)
            {
                if (changeset.ChangedModelsAndHints.ContainsKey(GetSubgraphNodeGuid(i)))
                    return true;
            }

            return ContainsSubgraphNode(changeset.NewModels) || ContainsSubgraphNode(changeset.DeletedModels);
        }

        bool ContainsSubgraphNode(IReadOnlyCollection<Hash128> guids)
        {
            if (guids.Count == 0)
                return false;

            foreach (var guid in guids)
            {
                for (var i = -1; i < m_ToolState.SubgraphStack.Count; i++)
                {
                    if (GetSubgraphNodeGuid(i) == guid)
                        return true;
                }
            }

            return false;
        }

        Hash128 GetSubgraphNodeGuid(int index)
        {
            return index == -1 ? m_ToolState.CurrentSubgraphNodeGuid : m_ToolState.GetSubgraphNodeGuid(index);
        }
    }
}
