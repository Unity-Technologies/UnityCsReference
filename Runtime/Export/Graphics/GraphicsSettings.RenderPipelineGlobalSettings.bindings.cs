// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.Rendering
{
    public sealed partial class GraphicsSettings
    {
        [NativeName("RegisterRenderPipelineSettings")] static extern void Internal_RegisterRenderPipeline(string renderpipelineName, Object settings);
        [NativeName("UnregisterRenderPipelineSettings")] static extern void Internal_UnregisterRenderPipeline(string renderpipelineName);
        [NativeName("GetSettingsForRenderPipeline")] static extern Object Internal_GetSettingsForRenderPipeline(string renderpipelineName);

        private static void CheckRenderPipelineType(Type renderPipelineType)
        {
            if (renderPipelineType == null)
                throw new ArgumentNullException(nameof(renderPipelineType));

            if (!typeof(RenderPipeline).IsAssignableFrom(renderPipelineType))
                throw new ArgumentException($"{renderPipelineType} must be a valid {nameof(RenderPipeline)}");
        }

        public static void UpdateGraphicsSettings(RenderPipelineGlobalSettings newSettings, Type renderPipelineType)
        {
            CheckRenderPipelineType(renderPipelineType);

            if (newSettings != null)
                Internal_RegisterRenderPipeline(renderPipelineType.FullName, newSettings);
            else
                Internal_UnregisterRenderPipeline(renderPipelineType.FullName);
        }

        public static void RegisterRenderPipelineSettings(Type renderPipelineType, RenderPipelineGlobalSettings settings)
        {
            CheckRenderPipelineType(renderPipelineType);
            Internal_RegisterRenderPipeline(renderPipelineType.FullName, settings);
        }

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

        public static void UnregisterRenderPipelineSettings(Type renderPipelineType)
        {
            CheckRenderPipelineType(renderPipelineType);
            Internal_UnregisterRenderPipeline(renderPipelineType.FullName);
        }

        public static RenderPipelineGlobalSettings GetSettingsForRenderPipeline<T>()
            where T : RenderPipeline
        {
            return  Internal_GetSettingsForRenderPipeline(typeof(T).FullName) as RenderPipelineGlobalSettings;
        }

        public static RenderPipelineGlobalSettings GetSettingsForRenderPipeline(Type renderPipelineType)
        {
            CheckRenderPipelineType(renderPipelineType);
            return Internal_GetSettingsForRenderPipeline(renderPipelineType.FullName) as RenderPipelineGlobalSettings;
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

        public static T GetSRPGraphicsSetting<T>() where T : class, ISRPGraphicsSetting
        {
            if (!TryGetCurrentRenderPipelineGlobalSettings(out RenderPipelineGlobalSettings currentRenderPipelineGlobalSettings))
                throw new Exception($"The current render pipeline does not have {nameof(RenderPipelineGlobalSettings)} registered");

            if (!currentRenderPipelineGlobalSettings.TryGet(typeof(T), out var baseSetting))
                throw new Exception($"Unable to find a setting of type {typeof(T)} on {currentRenderPipelineGlobalSettings.GetType()}");

            return baseSetting as T;
        }

        public static bool TryGetSRPGraphicsSetting<T>(out T setting) where T : class, ISRPGraphicsSetting
        {
            try
            {
                setting = GetSRPGraphicsSetting<T>();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                setting = null;
            }

            return setting != null;
        }
    }
}
