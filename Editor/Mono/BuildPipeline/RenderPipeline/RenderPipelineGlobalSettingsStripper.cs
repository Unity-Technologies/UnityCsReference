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

        static bool s_IsCurrentRenderPipelineGlobalsSettingsDirty;
        static bool s_IsGraphicsSettingsDirty;

        public void OnPreprocessBuild(BuildReport report)
        {
            s_IsCurrentRenderPipelineGlobalsSettingsDirty = false;
            s_IsGraphicsSettingsDirty = false;

            var renderPipelineAssets = ListPool<RenderPipelineAsset>.Get();

            var buildTargetGroupName = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget).ToString();
            QualitySettings.GetAllRenderPipelineAssetsForPlatform(buildTargetGroupName, ref renderPipelineAssets);

            if (renderPipelineAssets.Count > 0 && renderPipelineAssets[0] != null)
            {
                // Top level stripping, even if there are multiple pipelines registered into the project, as we are building we are making sure the only one that is being transferred into the player is the current one.
                if (renderPipelineAssets[0].pipelineType != null)
                {
                    var renderPipelineGlobalSettingsAsset = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset(renderPipelineAssets[0].pipelineType);

                    if (renderPipelineGlobalSettingsAsset != null)
                    {
                        s_IsGraphicsSettingsDirty = EditorUtility.IsDirty(GraphicsSettings.GetGraphicsSettings());
                        s_IsCurrentRenderPipelineGlobalsSettingsDirty = EditorUtility.IsDirty(renderPipelineGlobalSettingsAsset);

                        // The asset needs to be dirty, in order to tell the BuildPlayer to always transfer the latest state of the asset.
                        // The main reason is due to IRenderPipelineGraphicsSettings stripping from the user side.

                        if (!s_IsCurrentRenderPipelineGlobalsSettingsDirty)
                            EditorUtility.SetDirty(renderPipelineGlobalSettingsAsset);

                        GraphicsSettings.currentRenderPipelineGlobalSettings = renderPipelineGlobalSettingsAsset;
                    }
                }
                else
                    Debug.LogWarning($"{renderPipelineAssets[0].GetType().Name} must inherit from {nameof(RenderPipelineAsset)}<T> instead of {nameof(RenderPipelineAsset)} to benefit from {nameof(RenderPipelineGlobalSettingsStripper)}.");
            }

            ListPool<RenderPipelineAsset>.Release(renderPipelineAssets);
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (GraphicsSettings.currentRenderPipelineGlobalSettings != null)
            {
                // Clean the previous dirty flag.
                if (!s_IsCurrentRenderPipelineGlobalsSettingsDirty)
                    EditorUtility.ClearDirty(GraphicsSettings.currentRenderPipelineGlobalSettings);

                // Set to null the asset, and clean the dirty flag
                GraphicsSettings.currentRenderPipelineGlobalSettings = null;
                if (!s_IsGraphicsSettingsDirty)
                    EditorUtility.ClearDirty(GraphicsSettings.GetGraphicsSettings());
            }
        }
    }
}
