// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor
{
    class WindowAssetModificationWatcher : AssetModificationProcessor
    {
        static bool AssetAtPathHasGraphObject(string path)
        {
            return GraphObjectFactory.KnowsExtension(Path.GetExtension(path)) || AssetDatabase.LoadAssetAtPath<GraphObject>(path) != null;
        }

        static string[] OnWillSaveAssets(string[] paths)
        {
            var graphWindows = Resources.FindObjectsOfTypeAll<GraphViewEditorWindow>();
            foreach (var graphViewEditorWindow in graphWindows)
            {
                graphViewEditorWindow.SaveOverlayPositions();
            }
            return paths;
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            // Avoid calling FindObjectsOfTypeAll if the deleted asset does not contain a graph object.
            if (AssetAtPathHasGraphObject(assetPath))
            {
                GraphObjectFactory.OnAssetDeleted(assetPath);
                var guid = AssetDatabase.GUIDFromAssetPath(assetPath);
                var windows = Resources.FindObjectsOfTypeAll<GraphViewEditorWindow>();
                foreach (var window in windows)
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
