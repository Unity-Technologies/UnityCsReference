// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolkit.CSO;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// An observer that updates subgraph nodes when their referenced external asset graph is changed.
    /// </summary>
    [UnityRestricted]
    internal class SubgraphNodeUpdater : StateObserver
    {
        ExternalAssetsStateComponent m_ExternalAssetsState;
        GraphModelStateComponent m_GraphModelState;

        /// <inheritdoc cref="StateObserver(IStateComponent[], IStateComponent[])"/>
        public SubgraphNodeUpdater(ExternalAssetsStateComponent externalAssetsState, GraphModelStateComponent graphModelState)
            : base(new IStateComponent[] { externalAssetsState },
                   new IStateComponent[] { graphModelState })
        {
            m_ExternalAssetsState = externalAssetsState;
            m_GraphModelState = graphModelState;
        }

        static bool IsGraphReferencingGraphAsset(GraphModel graphModel, GUID graphGuid)
        {
            return graphModel?.NodeModels != null && graphModel.NodeModels.OfType<SubgraphNodeModel>().Any(n => n.SubgraphReference.AssetGuid == graphGuid);
        }

        /// <inheritdoc />
        public override void Observe()
        {
            using var observation = this.ObserveState(m_ExternalAssetsState);
            if (observation.UpdateType != UpdateType.None)
            {
                var graphModel = m_GraphModelState.GraphModel;
                if (graphModel == null)
                    return;
                if (!graphModel.AllowSubgraphCreation)
                    return;

                using var updater = m_GraphModelState.UpdateScope;
                using var changeScope = graphModel.ChangeDescriptionScope;

                var changedAssets = new HashSet<string>(m_ExternalAssetsState.ImportedAssets);
                changedAssets.UnionWith(m_ExternalAssetsState.MovedAssets.Select(t => t.currentPath));
                changedAssets.UnionWith(m_ExternalAssetsState.DeletedAssets);

                // Deleted graphs have already been unloaded by WindowAssetModificationWatcher, just before they were deleted.

                var changedGuids = changedAssets.ToDictionary(path => path, AssetDatabase.GUIDFromAssetPath);

                var referencedSubGraphsGuids = changedGuids
                    .Where(kvp => IsGraphReferencingGraphAsset(graphModel, kvp.Value))
                    .Select(kvp => kvp.Value);

                var subGraphNodeModels = new List<SubgraphNodeModel>();
                foreach (var subgraphGuid in referencedSubGraphsGuids)
                {
                    subGraphNodeModels.Clear();
                    for (var i = 0; i < graphModel.NodeModels.Count; i++)
                    {
                        if (graphModel.NodeModels[i] is not SubgraphNodeModel subgraphNode || subgraphNode.IsReferencingLocalSubgraph || subgraphNode.SubgraphReference.AssetGuid != subgraphGuid)
                            continue;

                        // Local subgraph assets are hidden from the users, there should not be external modifications.
                        if (subgraphNode.IsReferencingLocalSubgraph)
                            continue;

                        subGraphNodeModels.Add(subgraphNode);
                    }

                    foreach (var subgraphNodeModel in subGraphNodeModels)
                    {
                        // The subgraph was changed or deleted. Update it.
                        subgraphNodeModel.Update();
                    }
                }

                updater.MarkUpdated(changeScope.ChangeDescription);
            }
        }
    }
}
