// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using Object = UnityEngine.Object;
using Unity.GraphToolkit.Editor.ContextualMenuItems;

namespace Unity.GraphToolkit.Editor
{
    internal static class SubgraphNodeModelHelper
    {
        /// <summary>
        /// The icon type string for subgraph nodes referencing a local subgraph.
        /// </summary>
        internal static readonly string k_LocalSubgraphIconTypeString = "subgraph";

        /// <summary>
        /// The icon type string for subgraph nodes referencing an asset subgraph.
        /// </summary>
        internal static readonly string k_AssetSubgraphIconTypeString = "graph-object";

        internal const string DefaultLocalSubtitle = "Local Subgraph";
        internal const string DefaultAssetSubtitle = "Asset Subgraph";

        internal static GraphModel GetSubgraphModel(GraphModel ownerGraphModel, GraphReference subgraphReference, GraphModel copyPasteRef)
            => ownerGraphModel?.ResolveGraphModelFromReference(subgraphReference) ?? copyPasteRef;

        internal static bool IsReferencingLocalSubgraph(GraphModel ownerGraphModel, GraphReference subgraphReference, GraphModel copyPasteRef)
            => GetSubgraphModel(ownerGraphModel, subgraphReference, copyPasteRef)?.IsLocalSubgraph ?? false;

        internal static bool GraphModelReferenceIsValid(GraphModel ownerGraphModel, GraphReference subgraphReference)
            => ownerGraphModel?.ResolveGraphModelFromReference(subgraphReference) != null;

        internal static string GetTitle(GraphModel ownerGraph, string titleOverride, GraphReference subgraphReference, GraphModel copyPasteRef)
            => string.IsNullOrEmpty(titleOverride) ? GetSubgraphModel(ownerGraph, subgraphReference, copyPasteRef)?.Name ?? string.Empty : titleOverride;

        internal static string ComputeNewTitle(GraphModel ownerGraph, string value, GraphReference subgraphReference, GraphModel copyPasteRef)
        {
            var newName = value?.Trim();
            if (string.IsNullOrEmpty(newName))
                return string.Empty;
            var subgraphModel = GetSubgraphModel(ownerGraph, subgraphReference, copyPasteRef);
            return subgraphModel != null && subgraphModel.Name == newName ? string.Empty : newName;
        }

        // Returns false if the reference is unchanged (early out for callers).
        // On success, outputs the new asset property and state flags.
        internal static bool SetSubgraphReference(GraphModel ownerGraph, GraphReference value, ref GraphReference subgraphReference, GraphModel copyPasteRef,
            out SubgraphAssetProperty assetProperty, out bool referenceIsValid, out bool isReferencingLocal)
        {
            if (!UpdateReference(ownerGraph, value, ref subgraphReference, copyPasteRef))
            {
                assetProperty = default;
                referenceIsValid = false;
                isReferencingLocal = false;
                return false;
            }
            assetProperty = new SubgraphAssetProperty(subgraphReference);
            referenceIsValid = GraphModelReferenceIsValid(ownerGraph, subgraphReference);
            isReferencingLocal = IsReferencingLocalSubgraph(ownerGraph, subgraphReference, copyPasteRef);
            return true;
        }

        internal static bool UpdateReference(GraphModel ownerGraph, GraphReference value, ref GraphReference subgraphReference, GraphModel copyPasteRef)
        {
            if (subgraphReference.Equals(value))
                return false;

            if (!subgraphReference.HasAssetReference)
            {
                var previousLocalSubGraph = GetSubgraphModel(ownerGraph, subgraphReference, copyPasteRef);
                if (previousLocalSubGraph != null && previousLocalSubGraph.IsLocalSubgraph)
                    ownerGraph.RemoveLocalSubgraph(previousLocalSubGraph);
            }

            subgraphReference = value;
            return true;
        }

        // sourceGraph is the graph that owns the source node — needed to resolve local subgraph
        // references that only exist in the source. ownerGraph is the destination graph.
        internal static void HandleDuplicate(GraphModel ownerGraph, GraphModel sourceGraph, GraphReference sourceSubgraphReference, GraphModel sourceCopyPasteRef, string sourceTitle, Action<GraphReference> setModel)
        {
            if (!IsReferencingLocalSubgraph(sourceGraph, sourceSubgraphReference, sourceCopyPasteRef) && sourceCopyPasteRef == null)
                return;

            var sourceGraphModel = GetSubgraphModel(sourceGraph, sourceSubgraphReference, sourceCopyPasteRef);
            if (sourceGraphModel is null)
            {
                setModel(default);
            }
            else
            {
                // Each duplicated local subgraph node should have their own instance of graph model
                var newSubgraph = ownerGraph.DuplicateLocalSubGraph(sourceGraphModel, sourceTitle);
                if (newSubgraph == null)
                    return;

                setModel(newSubgraph.GetGraphReference(true));
            }
        }

        // Local subgraphs need to be duplicated by the copy/paste operation.
        internal static void BeginCopy(GraphModel ownerGraph, ref GraphReference subgraphReference, ref GraphModel copyPasteRef)
        {
            if (IsReferencingLocalSubgraph(ownerGraph, subgraphReference, copyPasteRef))
            {
                copyPasteRef = GetSubgraphModel(ownerGraph, subgraphReference, copyPasteRef);
                if (copyPasteRef is ICopyPasteCallbackReceiver r)
                    r.OnBeforeCopy();
                subgraphReference = default;
            }
            else
            {
                copyPasteRef = null;
            }
        }

        internal static void EndCopy(GraphModel ownerGraph, ref GraphReference subgraphReference, ref GraphModel copyPasteRef)
        {
            if (copyPasteRef != null)
            {
                subgraphReference = ownerGraph.GetGraphModelReference(copyPasteRef, true);
                // Set the reference back to null, as we do not want to serialize the local subgraph model to disk.
                copyPasteRef = null;
            }
        }

        internal static void ClearPaste(ref GraphModel copyPasteRef)
        {
            copyPasteRef = null;
        }

        internal static void CloneAssets(GraphModel ownerGraph, GraphReference subgraphReference, GraphModel copyPasteRef, List<Object> clones, Dictionary<Object, Object> originalToCloneMap)
        {
            if (IsReferencingLocalSubgraph(ownerGraph, subgraphReference, copyPasteRef))
                GetSubgraphModel(ownerGraph, subgraphReference, copyPasteRef).CloneAssets(clones, originalToCloneMap);
        }

        internal static void OnAfterAssetClone(GraphModel ownerGraph, GraphReference subgraphReference, GraphModel copyPasteRef, IReadOnlyDictionary<Object, Object> originalToCloneMap)
        {
            if (IsReferencingLocalSubgraph(ownerGraph, subgraphReference, copyPasteRef))
                // m_SubgraphReference has just been updated: GetSubgraphModel() will return the graph model in the cloned asset.
                GetSubgraphModel(ownerGraph, subgraphReference, copyPasteRef).OnAfterAssetClone(originalToCloneMap);
        }

        internal static List<ContextualMenuItem> GetContextualMenuItems(GraphModel ownerGraph, GraphReference subgraphReference, GraphModel copyPasteRef, IReadOnlyList<ContextualMenuItem> baseContextualMenuItems)
        {
            var menuItems = new List<ContextualMenuItem>(baseContextualMenuItems);
            menuItems.AddRange(IsReferencingLocalSubgraph(ownerGraph, subgraphReference, copyPasteRef) ? s_LocalSubgraphContextualMenuItems : s_AssetSubgraphContextualMenuItems);
            return menuItems;
        }

        [NoAutoStaticsCleanup] // fixed menu-item descriptors (enum/string/int value data, no delegates); safe to persist across reload
        internal static readonly List<ContextualMenuItem> s_LocalSubgraphContextualMenuItems = new()
        {
            ContextualMenuHelpers.extractContentsToPlacematItem,
            ContextualMenuHelpers.openLocalSubgraphItem,
            ContextualMenuHelpers.convertToAssetSubgraphItem,
        };

        [NoAutoStaticsCleanup] // fixed menu-item descriptors (enum/string/int value data, no delegates); safe to persist across reload
        internal static readonly List<ContextualMenuItem> s_AssetSubgraphContextualMenuItems = new()
        {
            ContextualMenuHelpers.extractContentsToPlacematItem,
            ContextualMenuHelpers.openAssetSubgraphItem,
            ContextualMenuHelpers.unpackToLocalSubgraphItem,
            ContextualMenuHelpers.findAssetInProjectItem,
        };
    }
}
