// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;

namespace UnityEditor.Build.Rendering
{
    class RenderPipelineGlobalSettingsStripper : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => (int) ExecutionOrder.StripRenderPipelineGlobalSettingsAsset;

        public void OnPreprocessBuild(BuildReport report)
        {
            var renderPipelineAssets = ListPool<RenderPipelineAsset>.Get();

            var buildTargetGroupName = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget).ToString();
            QualitySettings.GetAllRenderPipelineAssetsForPlatform(buildTargetGroupName, ref renderPipelineAssets);

            if (renderPipelineAssets.Count > 0 && renderPipelineAssets[0] != null)
            {
                // Top level stripping, even if there are multiple pipelines registered into the project, as we are building we are making sure the only one that is being transferred into the player is the current one.
                GraphicsSettings.currentRenderPipelineGlobalSettings = EditorGraphicsSettings.GetSettingsForRenderPipeline(renderPipelineAssets[0].renderPipelineType);
            }

            ListPool<RenderPipelineAsset>.Release(renderPipelineAssets);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            GraphicsSettings.currentRenderPipelineGlobalSettings = null;
        }
    }
}
