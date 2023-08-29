// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Events;

namespace UnityEngine.Rendering
{
    public sealed partial class GraphicsSettings
    {
        internal static PropertyHelper<IRenderPipelineGraphicsSettings> s_PropertyHelper = new();

        public static void Subscribe<TChild>(Action<TChild, string> callback)
            where TChild : class, IRenderPipelineGraphicsSettings
        {
            s_PropertyHelper.propertyChangedEvent.Subscribe(callback);
        }

        public static void Unsubscribe<TChild>(Action<TChild, string> callback)
            where TChild : class, IRenderPipelineGraphicsSettings
        {
            s_PropertyHelper.propertyChangedEvent.Unsubscribe(callback);
        }

        [NativeName("RegisterRenderPipelineSettings")] static extern void Internal_RegisterRenderPipeline(string renderpipelineName, Object settings);
        [NativeName("UnregisterRenderPipelineSettings")] static extern void Internal_UnregisterRenderPipeline(string renderpipelineName);
        [NativeName("GetSettingsForRenderPipeline")] static extern Object Internal_GetSettingsForRenderPipeline(string renderpipelineName);

        [NativeName("CurrentRenderPipelineGlobalSettings")] private static extern Object INTERNAL_currentRenderPipelineGlobalSettings { set; }
        internal static RenderPipelineGlobalSettings currentRenderPipelineGlobalSettings
        {
            set => INTERNAL_currentRenderPipelineGlobalSettings = value;
        }

        private static void CheckRenderPipelineType(Type renderPipelineType)
        {
            if (renderPipelineType == null)
                throw new ArgumentNullException(nameof(renderPipelineType));

            if (!typeof(RenderPipeline).IsAssignableFrom(renderPipelineType))
                throw new ArgumentException($"{renderPipelineType} must be a valid {nameof(RenderPipeline)}");
        }

        [Obsolete("Please use EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset(renderPipelineType, newSettings). #from(23.2)", false)]
        public static void UpdateGraphicsSettings(RenderPipelineGlobalSettings newSettings, Type renderPipelineType)
        {
            CheckRenderPipelineType(renderPipelineType);

            if (newSettings != null)
                Internal_RegisterRenderPipeline(renderPipelineType.FullName, newSettings);
            else
                Internal_UnregisterRenderPipeline(renderPipelineType.FullName);
        }

        [Obsolete("Please use EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset(renderPipelineType, settings). #from(23.2)", false)]
        public static void RegisterRenderPipelineSettings(Type renderPipelineType, RenderPipelineGlobalSettings settings)
        {
            CheckRenderPipelineType(renderPipelineType);
            Internal_RegisterRenderPipeline(renderPipelineType.FullName, settings);
        }

        [Obsolete("Please use EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<TRenderPipelineType>(settings). #from(23.2)", false)]
        public static void RegisterRenderPipelineSettings<T>(RenderPipelineGlobalSettings settings)
            where T : RenderPipeline
        {
            Internal_RegisterRenderPipeline(typeof(T).FullName, settings);
        }

        [Obsolete("Please use EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset<TRenderPipelineType>(null). #from(23.2)", false)]
        public static void UnregisterRenderPipelineSettings<T>()
            where T : RenderPipeline
        {
            Internal_UnregisterRenderPipeline(typeof(T).FullName);
        }

        [Obsolete("Please use EditorGraphicsSettings.SetRenderPipelineGlobalSettingsAsset(renderPipelineType, null). #from(23.2)", false)]
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

        private static RenderPipelineGlobalSettings Internal_GetCurrentRenderPipelineGlobalSettings()
        {
            RenderPipelineGlobalSettings asset = null;

            if (currentRenderPipeline != null)
            {
                asset = Internal_GetSettingsForRenderPipeline(currentRenderPipeline.pipelineType?.FullName ?? string.Empty) as RenderPipelineGlobalSettings;
            }

            return asset;
        }


        public static bool TryGetCurrentRenderPipelineGlobalSettings(out RenderPipelineGlobalSettings asset)
        {
            asset = Internal_GetCurrentRenderPipelineGlobalSettings();
            return asset != null;
        }

        public static T GetRenderPipelineSettings<T>()
            where T : class, IRenderPipelineGraphicsSettings
        {
            TryGetRenderPipelineSettings<T>(out var settings);
            return settings;
        }

        public static bool TryGetRenderPipelineSettings<T>(out T settings)
            where T : class, IRenderPipelineGraphicsSettings
        {
            settings = null;

            if (!TryGetCurrentRenderPipelineGlobalSettings(out RenderPipelineGlobalSettings asset))
                return false;

            if (asset.TryGet(typeof(T), out var baseSettings))
                settings = baseSettings as T;

            return settings != null;
        }

        [NativeName("SetAllRenderPipelineSettingsDirty")] internal static extern void Internal_SetAllRenderPipelineSettingsDirty();

    }
}
