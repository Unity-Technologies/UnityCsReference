// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    public sealed partial class GraphicsSettings
    {
        [NativeName("RegisterRenderPipelineSettings")] static extern void Internal_RegisterRenderPipeline(string renderpipelineName, Object settings);
        [NativeName("UnregisterRenderPipelineSettings")] static extern void Internal_UnregisterRenderPipeline(string renderpipelineName);
        [NativeName("GetSettingsForRenderPipeline")] static extern Object Internal_GetSettingsForRenderPipeline(string renderpipelineName);

        public static void RegisterRenderPipelineSettings<T>(RenderPipelineGlobalSettings settings)
            where T : RenderPipeline
        {
            Internal_RegisterRenderPipeline(typeof(T).FullName, settings);
        }

        public static void UnregisterRenderPipelineSettings<T>()
            where T : RenderPipeline
        {
            Internal_UnregisterRenderPipeline(typeof(T).FullName);
        }

        public static RenderPipelineGlobalSettings GetSettingsForRenderPipeline<T>()
            where T : RenderPipeline
        {
            return  Internal_GetSettingsForRenderPipeline(typeof(T).FullName) as RenderPipelineGlobalSettings;
        }

        public static bool TryGetCurrentRenderPipelineGlobalSettings(out RenderPipelineGlobalSettings currentRenderPipelineGlobalSettings)
        {
            currentRenderPipelineGlobalSettings = null;
            if (currentRenderPipeline == null)
                return false;

            currentRenderPipelineGlobalSettings = Internal_GetSettingsForRenderPipeline(
                    currentRenderPipeline.renderPipelineType?.FullName ?? string.Empty) as RenderPipelineGlobalSettings;

            return currentRenderPipelineGlobalSettings != null;
        }
    }
}
