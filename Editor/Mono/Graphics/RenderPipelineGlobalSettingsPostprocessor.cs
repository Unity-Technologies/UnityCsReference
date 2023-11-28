// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    class RenderPipelineGlobalSettingsAssetProcessor : AssetModificationProcessor
    {
        public static bool s_CheckDelete = true;
        public static Lazy<bool> s_RunningTestsOrBatchMode = new Lazy<bool>(() => Environment.CommandLine.Contains("-testResults") || Application.isBatchMode);

        public static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions option)
        {
            if (s_RunningTestsOrBatchMode.Value || !s_CheckDelete)
                return AssetDeleteResult.DidNotDelete;

            if (GraphicsSettings.TryGetCurrentRenderPipelineGlobalSettings(out var renderPipelineGlobalSettings))
            {
                string currentInstanceAssetPath = AssetDatabase.GetAssetPath(renderPipelineGlobalSettings);
                if (assetPath.Equals(currentInstanceAssetPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!EditorUtility.DisplayDialog($"Deleting current {nameof(RenderPipelineGlobalSettings)}",
                        $@"This asset is assigned to the currently active {GraphicsSettings.currentRenderPipeline.GetType().Name}. Unity will recreate the asset with the default values. 
Are you sure you want to proceed?",
                        "Yes", "No"))
                    {
                        return AssetDeleteResult.FailedDelete;
                    }
                }
            }

            return AssetDeleteResult.DidNotDelete;
        }
    }

    class RenderPipelineGlobalSettingsPostprocessor : AssetPostprocessor
    {
        public static bool TryFindPath(string[] importedAssets, string pathToFind)
        {
            for (var index = 0; index < importedAssets.Length; index++)
            {
                var path = importedAssets[index];
                if (path.Equals(pathToFind, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        const string k_GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";

        public static bool RecreateGlobalSettingsNeed(string[] importedAssets, RenderPipelineAsset renderPipelineAsset)
        {
            // if the graphics settings has been imported, potentially the asset reference has been modified
            if (TryFindPath(importedAssets, k_GraphicsSettingsPath))
                return true;

            int id = renderPipelineAsset.pipelineType != null ?
                EditorGraphicsSettings.Internal_GetSettingsInstanceIDForRenderPipeline(renderPipelineAsset.pipelineType.FullName) : 0;
            if (id != 0)
            {
                string currentInstanceAssetPath = AssetDatabase.GetAssetPath(id);

                // Global settings asset has been deleted as the instance id is not null, but the asset does not longer exist
                // or the current global settings asset is on the imported assets because some modification has been done
                if (string.IsNullOrEmpty(currentInstanceAssetPath) || TryFindPath(importedAssets, currentInstanceAssetPath))
                    return true;
            }

            return false;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] _, string[] ___, string[] ____, bool didDomainReload)
        {
            var rpAsset = GraphicsSettings.currentRenderPipeline;
            if (rpAsset == null)
                return;

            if (RecreateGlobalSettingsNeed(importedAssets, rpAsset))
                RenderPipelineManager.RecreateCurrentPipeline(RenderPipelineManager.currentPipelineAsset);
        }
    }
}
