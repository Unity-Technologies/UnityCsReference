// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Unity.GraphToolkit.Editor
{
    class WindowAssetModificationWatcher : AssetModificationProcessor
    {
        static bool TryGetGraphAssetPathsAtPath(string path, out List<string> graphAssetPaths)
        {
            graphAssetPaths = [];

            if (AssetDatabase.IsValidFolder(path))
            {
                var assetGUIDs = AssetDatabase.FindAssets("", [path]);

                foreach (var assetGUID in assetGUIDs)
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);

                    if (GraphObjectFactory.KnowsExtension(Path.GetExtension(assetPath))
                        || AssetDatabase.LoadAssetAtPath<GraphObject>(assetPath) != null)
                        graphAssetPaths.Add(assetPath);
                }
            }
            else
            {
                if (GraphObjectFactory.KnowsExtension(Path.GetExtension(path))
                    || AssetDatabase.LoadAssetAtPath<GraphObject>(path) != null)
                    graphAssetPaths.Add(path);
            }

            return graphAssetPaths.Count > 0;
        }

        static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
        {
            // Avoid calling FindObjectsOfTypeAll if the deleted asset does not contain a graph object.
            if (GraphViewEditorWindow.OpenedWindows.Count == 0
                || !TryGetGraphAssetPathsAtPath(path, out var graphAssetPaths))
                return AssetDeleteResult.DidNotDelete;

            foreach (var graphAssetPath in graphAssetPaths)
            {
                GraphObjectFactory.OnAssetDeleted(graphAssetPath);
                var guid = AssetDatabase.GUIDFromAssetPath(graphAssetPath);
                foreach (var window in GraphViewEditorWindow.OpenedWindows)
                {
                    if (WindowAssetPostprocessingWatcher.IsWindowDisplayingGraphAsset(window, guid))
                    {
                        // Unload graph *before* it is deleted.
                        window.GraphTool.Dispatch(new UnloadGraphCommand());

                        // Remove log entries in the console related to that asset.
                        ConsoleWindowHelper.RemoveLogEntries(window.WindowID.ToString());
                    }
                }
            }

            return AssetDeleteResult.DidNotDelete;
        }
    }
}
