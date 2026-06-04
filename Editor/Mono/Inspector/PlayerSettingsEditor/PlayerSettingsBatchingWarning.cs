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
        const string k_SessionKey = "DynamicBatchingWarningShown";

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            if (SessionState.GetBool(k_SessionKey, false))
                return;

            if (EditorUtility.isInSafeMode)
                return;

            EditorApplication.update += WarnIfDynamicBatchingEnabled;
        }

        static void WarnIfDynamicBatchingEnabled()
        {
            if (!RenderPipelineManager.pipelineSwitchCompleted)
                return;

            EditorApplication.update -= WarnIfDynamicBatchingEnabled;

            if (!InternalEditorUtility.isHumanControllingUs)
                return;

            if (GraphicsSettings.currentRenderPipeline != null)
                return;

            SessionState.SetBool(k_SessionKey, true);

            foreach (var platform in BuildPlatforms.instance.GetValidPlatforms(true))
            {
                PlayerSettings.GetBatchingForPlatform(platform.defaultTarget, out _, out int dynamicBatching);
                if (dynamicBatching == 1)
                {
                    Debug.LogWarning("Dynamic Batching is deprecated and will be removed in a future release. Use GPU Instancing instead. Disable Dynamic Batching in Project Settings > Player > Other Settings to remove this warning.");
                    break;
                }
            }
        }
    }
}
