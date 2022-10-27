// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolsFoundation.Editor
{
    class WindowAssetModificationWatcher_Internal : AssetModificationProcessor
    {
        static bool AssetAtPathIsGraphAsset(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GraphAsset>(path) != null;
        }

        static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            // Avoid calling FindObjectsOfTypeAll if the deleted asset is not a graph asset.
            if (AssetAtPathIsGraphAsset(assetPath))
            {
                var guid = AssetDatabase.AssetPathToGUID(assetPath);
                var windows = Resources.FindObjectsOfTypeAll<GraphViewEditorWindow>();
                foreach (var window in windows)
                {
                    if (WindowAssetPostprocessingWatcher_Internal.IsWindowDisplayingGraphAsset_Internal(window, guid))
                    {
                        // Unload graph *before* it is deleted.
                        window.GraphTool.Dispatch(new UnloadGraphCommand());
                    }
                }
            }
            return AssetDeleteResult.DidNotDelete;
        }
    }
}
