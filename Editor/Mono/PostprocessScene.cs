// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Build;

namespace UnityEditor
{
    internal class UnityBuildPostprocessor : IProcessSceneWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnProcessScene(UnityEngine.SceneManagement.Scene scene, Build.Reporting.BuildReport report)
        {
            if (report != null &&
                report.summary.buildType == Build.Reporting.BuildType.AssetBundle &&
                EditorBuildSettings.UseParallelAssetBundleBuilding)
            {
                // TODO: Remove this block once https://jira.unity3d.com/browse/UUM-10897 is fixed and increment the BuildCallback version
                // We are not Static Batching scenes if Auto Generate and either of the GI modes are enabled.
                // The reason for this is that under UCBP, we bake meshes before lightbaking, and our lightbaking systems
                // do not account for single submesh rendering causing a large build performance regression.

                if (Lightmapping.giWorkflowMode == Lightmapping.GIWorkflowMode.Iterative)
                {
                    var settings = Lightmapping.GetLightingSettingsOrDefaultsFallback();
                    BuildPipelineContext.DependOnAsset(settings);
                    if (settings.realtimeGI && settings.bakedGI)
                    {
                        Debug.LogWarning($"Static batching not performed on scene '{scene.path}' because 'Auto Generate', 'Realtime Global Illumination' & 'Baked Global Illumination' are enabled in Lighting Settings.");
                        return;
                    }
                    if (settings.realtimeGI)
                    {
                        Debug.LogWarning($"Static batching not performed on scene '{scene.path}' because 'Auto Generate' & 'Realtime Global Illumination' are enabled in Lighting Settings.");
                        return;
                    }
                    if (settings.bakedGI)
                    {
                        Debug.LogWarning($"Static batching not performed on scene '{scene.path}' because 'Auto Generate' & 'Baked Global Illumination' are enabled in Lighting Settings.");
                        return;
                    }
                }
            }

            int staticBatching, dynamicBatching;
            PlayerSettings.GetBatchingForPlatform(EditorUserBuildSettings.activeBuildTarget, out staticBatching, out dynamicBatching);
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
