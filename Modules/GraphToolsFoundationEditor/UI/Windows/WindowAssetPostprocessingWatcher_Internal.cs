// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    class WindowAssetPostprocessingWatcher_Internal : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (importedAssets.Length == 0 && deletedAssets.Length == 0 && movedAssets.Length == 0)
                return;

            var windows = Resources.FindObjectsOfTypeAll<GraphViewEditorWindow>();

            var changedAssets = new HashSet<string>(importedAssets);
            changedAssets.UnionWith(movedAssets);
            changedAssets.UnionWith(deletedAssets);

            // Deleted graphs have already been unloaded by WindowAssetModificationWatcher, just before they were deleted.

            var changedGuids = changedAssets.ToDictionary(path => path, AssetDatabase.AssetPathToGUID);
            foreach (var window in windows)
            {
                // Update all subgraph nodes displayed in windows
                var referencedSubGraphsGuids = changedGuids
                    .Where(kvp => IsWindowReferencingGraphAsset(window, kvp.Value))
                    .Select(kvp => kvp.Value);
                foreach (var subgraphGuid in referencedSubGraphsGuids)
                {
                    window.GraphView?.Dispatch(new UpdateSubgraphCommand(subgraphGuid));
                }

                // Update all breadcrumb overlays for moved assets
                foreach (var movedAsset in movedAssets)
                {
                    if (!changedGuids.TryGetValue(movedAsset, out var guid))
                        continue;

                    if (!IsWindowDisplayingGraphAsset_Internal(window, guid) && !IsWindowDisplayingSubgraphAssetOfParentGraphAsset(window, guid))
                        continue;

                    if (window.TryGetOverlay(BreadcrumbsToolbar.toolbarId, out var overlay))
                    {
                        var graphBreadcrumbs = overlay.rootVisualElement.SafeQ<GraphBreadcrumbs>();
                        graphBreadcrumbs?.Update();
                    }
                    break;
                }
            }
        }

        // Returns true if the window is displaying the graph graphGuid.
        internal static bool IsWindowDisplayingGraphAsset_Internal(GraphViewEditorWindow window, string graphGuid)
        {
            return window.GraphTool != null && window.GraphTool.ToolState.CurrentGraph.GraphAssetGuid == graphGuid;
        }

        // Returns true if the window is displaying a subgraph of the graph parentGraphGuid.
        static bool IsWindowDisplayingSubgraphAssetOfParentGraphAsset(GraphViewEditorWindow window, string parentGraphGuid)
        {
            return window.GraphTool != null && window.GraphTool.ToolState.SubGraphStack.Any(openedGraph => openedGraph.GraphAssetGuid == parentGraphGuid);
        }

        // Returns true if the window is displaying a graph that has subgraph nodes that reference graphGuid.
        static bool IsWindowReferencingGraphAsset(GraphViewEditorWindow window, string graphGuid)
        {
            return window.GraphView?.GraphModel?.NodeModels != null && window.GraphView.GraphModel.NodeModels.OfType<SubgraphNodeModel>().Any(n => n.SubgraphGuid == graphGuid);
        }
    }
}
