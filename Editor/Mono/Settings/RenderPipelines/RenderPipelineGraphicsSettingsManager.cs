// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering.Settings
{
    internal static class RenderPipelineGraphicsSettingsManager
    {
        public struct RenderPipelineGraphicsSettingsInfo
        {
            public Type type;
            public bool isDeprecated;
        }

        internal static void PopulateRenderPipelineGraphicsSettings(RenderPipelineGlobalSettings settings)
        {
            if (settings == null)
                return;

            if (!GraphicsSettingsUtils.ExtractSupportedOnRenderPipelineAttribute(settings.GetType(), out var globalSettingsSupportedOn, out var message))
                throw new InvalidOperationException(message);

            var globalSettingsRenderPipelineAssetType = globalSettingsSupportedOn.renderPipelineTypes[0];

            foreach (var info in FetchRenderPipelineGraphicsSettingInfos(globalSettingsRenderPipelineAssetType))
            {
                UpdateRenderPipelineGlobalSettings(info, settings);
            }
        }

        public static IEnumerable<RenderPipelineGraphicsSettingsInfo> FetchRenderPipelineGraphicsSettingInfos(Type globalSettingsRenderPipelineAssetType)
        {
            var graphicsSettingsTypes = TypeCache.GetTypesDerivedFrom(typeof(IRenderPipelineGraphicsSettings));

            foreach (var renderPipelineGraphicsSettingsType in graphicsSettingsTypes)
            {
                if (renderPipelineGraphicsSettingsType.IsAbstract || renderPipelineGraphicsSettingsType.IsGenericType || renderPipelineGraphicsSettingsType.IsInterface)
                    continue;

                if (!SupportedOnRenderPipelineAttribute.IsTypeSupportedOnRenderPipeline(renderPipelineGraphicsSettingsType, globalSettingsRenderPipelineAssetType))
                    continue;

                if (renderPipelineGraphicsSettingsType.GetCustomAttribute<SerializableAttribute>() == null)
                {
                    Debug.LogWarning($"{nameof(SerializableAttribute)} must be added to {renderPipelineGraphicsSettingsType}, the setting will be skipped");
                    continue;
                }

                if (renderPipelineGraphicsSettingsType.GetCustomAttribute<SupportedOnRenderPipelineAttribute>() == null)
                {
                    Debug.LogWarning($"{nameof(SupportedOnRenderPipelineAttribute)} must be added to {renderPipelineGraphicsSettingsType}, the setting will be skipped");
                    continue;
                }

                yield return new RenderPipelineGraphicsSettingsInfo()
                {
                    type = renderPipelineGraphicsSettingsType,
                    isDeprecated = renderPipelineGraphicsSettingsType.GetCustomAttribute<ObsoleteAttribute>()?.IsError ?? false
                };
            }
        }

        static void UpdateRenderPipelineGlobalSettings(RenderPipelineGraphicsSettingsInfo renderPipelineGraphicsSettingsType, RenderPipelineGlobalSettings asset)
        {
            IRenderPipelineGraphicsSettings renderPipelineGraphicsSettings;

            if (renderPipelineGraphicsSettingsType.isDeprecated)
            {
                if (asset.TryGet(renderPipelineGraphicsSettingsType.type, out renderPipelineGraphicsSettings))
                {
                    asset.Remove(renderPipelineGraphicsSettings);
                }

                return;
            }

            if (TryCreateInstance(renderPipelineGraphicsSettingsType.type, true, out renderPipelineGraphicsSettings))
                asset.Add(renderPipelineGraphicsSettings);

            if (renderPipelineGraphicsSettings is IRenderPipelineResources resource)
                RenderPipelineResourcesEditorUtils.TryReloadContainedNullFields(resource);
        }

        static bool TryCreateInstance<T>(Type type, bool nonPublic, out T instance)
        {
            try
            {
                instance = (T)Activator.CreateInstance(type, nonPublic);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            instance = default;
            return false;
        }
    }
}
