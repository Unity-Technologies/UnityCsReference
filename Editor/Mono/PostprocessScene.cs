// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Build.Content;

namespace UnityEditor
{
    internal class UnityBuildPostprocessor : AssetPostprocessor
    {
        // Has to be keep in sync with "EditorOnlyPlayerSettings::kAssetDependencyKey_BuildPipelineStaticBatching"
        const string kAssetDependencyKey_BuildPipelineStaticBatching = "BP/StaticBatching";

        public override uint GetVersion()
        {
            return 1;
        }

        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, SceneImportContext sceneContext)
        {
            context.DependsOnCustomDependency(kAssetDependencyKey_BuildPipelineStaticBatching);

            // This is explicitly using EditorUserBuildSettings.activeBuildTarget instead of context.selectedBuildTarget
            // Because we do not want the postprocessor to depend on the buildtarget.
            // Depending on kAssetDependencyKey_BuildPipelineStaticBatching satisfies the implicit platform change dependency.
            PlayerSettings.GetBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget, out var staticBatching, out _);
            if (staticBatching != 0)
            {
                using (StaticBatchingUtility.s_CombineMarker.Auto())
                {
                    ulong sceneHash = Hash128.Compute(AssetDatabase.AssetPathToGUID(scene.path)).u64_0;
                    StaticBatchingEditorHelper.CombineAllStaticMeshesForScenePostProcessing(sceneHash, scene);
                }
            }
        }
    }
}
