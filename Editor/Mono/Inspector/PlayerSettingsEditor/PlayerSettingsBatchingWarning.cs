// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.Build;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor
{
    // TODO: Remove this class once Dynamic Batching has been fully removed.
    static class PlayerSettingsBatchingWarning
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            if (!InternalEditorUtility.isHumanControllingUs)
                return;

            EditorApplication.update += WarnIfDynamicBatchingEnabled;
        }

        static void WarnIfDynamicBatchingEnabled()
        {
            if (!RenderPipelineManager.pipelineSwitchCompleted)
                return;

            EditorApplication.update -= WarnIfDynamicBatchingEnabled;

            if (GraphicsSettings.currentRenderPipeline != null)
                return;

            foreach (var platform in BuildPlatforms.instance.GetValidPlatforms(true))
            {
                PlayerSettings.GetBatchingForPlatform(platform.defaultTarget, out _, out int dynamicBatching);
                if (dynamicBatching == 1)
                {
                    Debug.LogError("Dynamic Batching has been removed and no longer has any effect. Use GPU Instancing instead. Disable Dynamic Batching in Project Settings > Player > Other Settings to remove this error.");
                    break;
                }
            }
        }
    }
}
