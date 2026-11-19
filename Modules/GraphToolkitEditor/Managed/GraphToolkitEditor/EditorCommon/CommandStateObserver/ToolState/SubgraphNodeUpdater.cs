// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.CSO;
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
            #pragma warning disable UAC2001, UAC2006 // Avoid Linq
            return graphModel?.NodeModels != null && graphModel.NodeModels.OfType<SubgraphNodeModel>().Any(n => n.SubgraphReference.AssetGuid == graphGuid);
#pragma warning restore UAC2001, UAC2006
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
                #pragma warning disable UAC2001 // Avoid Linq
                changedAssets.UnionWith(m_ExternalAssetsState.MovedAssets.Select(t => t.currentPath));
#pragma warning restore UAC2001
                changedAssets.UnionWith(m_ExternalAssetsState.DeletedAssets);

                // Deleted graphs have already been unloaded by WindowAssetModificationWatcher, just before they were deleted.

                #pragma warning disable UAC2001 // Avoid Linq
                var changedGuids = changedAssets.ToDictionary(path => path, AssetDatabase.GUIDFromAssetPath);
#pragma warning restore UAC2001

                #pragma warning disable UAC2001 // Avoid Linq
                var referencedSubGraphsGuids = changedGuids
#pragma warning restore UAC2001
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

                        // Local subgraph assets are part of the main graph, there should not be external modifications.
                        if (subgraphNode.IsReferencingLocalSubgraph)
                            continue;

                        // If the subgraph model is not dirty, it did not change. No need to update the subgraph node.
                        var subgraphObject = subgraphNode.GetSubgraphModel()?.GraphObject;
                        if (subgraphObject != null && !subgraphObject.Dirty)
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
