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
        struct RenderPipelineGraphicsSettingsInfo
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

            foreach (var info in FetchRenderPipelineGraphicsSettingInfos())
            {
                UpdateRenderPipelineGlobalSettings(info, settings, globalSettingsSupportedOn);
            }
        }

        static IEnumerable<RenderPipelineGraphicsSettingsInfo> FetchRenderPipelineGraphicsSettingInfos()
        {
            var graphicsSettingsTypes = TypeCache.GetTypesDerivedFrom(typeof(IRenderPipelineGraphicsSettings));
            foreach (var renderPipelineGraphicsSettingsType in graphicsSettingsTypes)
            {
                if (renderPipelineGraphicsSettingsType.IsAbstract || renderPipelineGraphicsSettingsType.IsGenericType || renderPipelineGraphicsSettingsType.IsInterface)
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

        static void UpdateRenderPipelineGlobalSettings(RenderPipelineGraphicsSettingsInfo renderPipelineGraphicsSettingsType, RenderPipelineGlobalSettings asset,
            SupportedOnRenderPipelineAttribute globalSettingsSupportedOn)
        {
            bool isSettingsValid = IsSettingsValid(renderPipelineGraphicsSettingsType, globalSettingsSupportedOn);
            bool isSettingsExist = asset.TryGet(renderPipelineGraphicsSettingsType.type, out var srpGraphicSetting);
            if (!isSettingsValid)
            {
                if (isSettingsExist)
                    asset.Remove(srpGraphicSetting);
                return;
            }

            if (!isSettingsExist && TryCreateInstance(renderPipelineGraphicsSettingsType.type, true, out srpGraphicSetting))
                asset.Add(srpGraphicSetting);
                
            if (srpGraphicSetting is IRenderPipelineResources resource)
                RenderPipelineResourcesEditorUtils.TryReloadContainedNullFields(resource);
        }

        // The Setting has been completely deprecated or not supported on render pipeline anymore, that means that it will be removed from the settings as any usage on code will be an error
        static bool IsSettingsValid(RenderPipelineGraphicsSettingsInfo renderPipelineGraphicsSettingsType, SupportedOnRenderPipelineAttribute globalSettingsSupportedOn)
        {
            return !renderPipelineGraphicsSettingsType.isDeprecated && !renderPipelineGraphicsSettingsType.type.IsGenericType &&
                   IsTypeSupportedOnRenderPipeline(renderPipelineGraphicsSettingsType.type, globalSettingsSupportedOn);
        }

        static bool IsTypeSupportedOnRenderPipeline(Type settingsType, SupportedOnRenderPipelineAttribute globalSettingsSupportedOn)
        {
            return globalSettingsSupportedOn == null || SupportedOnRenderPipelineAttribute.IsTypeSupportedOnRenderPipeline(settingsType, globalSettingsSupportedOn.renderPipelineTypes[0]);
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
