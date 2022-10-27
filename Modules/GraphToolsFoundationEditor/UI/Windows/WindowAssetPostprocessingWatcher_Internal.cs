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

            var importedOrMoved = new HashSet<string>(importedAssets);
            importedOrMoved.UnionWith(movedAssets);
            importedOrMoved.ExceptWith(deletedAssets);

            // Deleted graphs have already been unloaded by WindowAssetModificationWatcher, just before they were deleted.

            // Reload imported or moved graphs.
            foreach (var path in importedOrMoved)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);

                foreach (var window in windows)
                {
                    if (IsWindowReferencingGraphAsset(window, guid) || IsWindowDisplayingGraphAsset_Internal(window, guid))
                    {
                        window.GraphTool.Dispatch(new LoadGraphCommand(window.GraphView.GraphModel,
                            loadStrategy: LoadGraphCommand.LoadStrategies.KeepHistory));
                    }
                }
            }

            // Reload graphs that reference deleted assets.
            foreach (var path in deletedAssets)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);

                foreach (var window in windows)
                {
                    if (IsWindowReferencingGraphAsset(window, guid))
                    {
                        window.GraphTool.Dispatch(new LoadGraphCommand(window.GraphView.GraphModel,
                            loadStrategy: LoadGraphCommand.LoadStrategies.KeepHistory));
                    }
                }
            }
        }

        // Returns true if the window is displaying the graph graphGuid.
        internal static bool IsWindowDisplayingGraphAsset_Internal(GraphViewEditorWindow window, string graphGuid)
        {
            return window.GraphTool != null && window.GraphTool.ToolState.CurrentGraph.GraphAssetGuid == graphGuid;
        }

        // Returns true if the window is displaying a graph that has subgraph nodes that reference graphGuid.
        static bool IsWindowReferencingGraphAsset(GraphViewEditorWindow window, string graphGuid)
        {
            return window.GraphView?.GraphModel?.NodeModels != null && window.GraphView.GraphModel.NodeModels.OfType<SubgraphNodeModel>().Any(n => n.SubgraphGuid == graphGuid);
        }
    }
}
