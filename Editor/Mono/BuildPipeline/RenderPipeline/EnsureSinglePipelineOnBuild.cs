// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.Build.Rendering
{
    class EnsureSinglePipelineOnBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => (int) ExecutionOrder.EnsureSinglePipeline;

        public void OnPreprocessBuild(BuildReport report)
        {
            var buildTargetGroupName = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget).ToString();
            if (!QualitySettings.SamePipelineAssetsForPlatform(buildTargetGroupName))
                throw new BuildFailedException($"The current build target has assets in its associated Quality levels and Graphics Settings that belong to different render pipelines. Please check your settings.");
        }
    }
}
