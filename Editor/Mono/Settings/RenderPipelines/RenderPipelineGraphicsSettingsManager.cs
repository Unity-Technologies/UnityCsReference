// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
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

            var globalSettingsSupportedOn = FetchSupportedOnRenderPipelineAttribute(settings);
            foreach (var info in FetchRenderPipelineGraphicsSettingInfos())
            {
                UpdateRenderPipelineGlobalSettings(info, settings, globalSettingsSupportedOn);
            }
        }

        static SupportedOnRenderPipelineAttribute FetchSupportedOnRenderPipelineAttribute(RenderPipelineGlobalSettings settings)
        {
            var renderPipelineGlobalSettings = settings.GetType();
            var supportedOnAttribute = renderPipelineGlobalSettings.GetCustomAttribute<SupportedOnRenderPipelineAttribute>();
            if (supportedOnAttribute == null)
                Debug.LogWarning($"{renderPipelineGlobalSettings.Name} will not be filtered by {nameof(SupportedOnRenderPipelineAttribute)}. You need to add {nameof(SupportedOnRenderPipelineAttribute)} and specify correct {nameof(RenderPipelineAsset)} type.");
            else if (supportedOnAttribute.renderPipelineTypes.Length != 1)
                    throw new InvalidOperationException($"You can specify only one {nameof(RenderPipelineAsset)} type because you can't have one {nameof(RenderPipelineGlobalSettings)} for few {nameof(RenderPipeline)}.");

            return supportedOnAttribute;
        }

        static IEnumerable<RenderPipelineGraphicsSettingsInfo> FetchRenderPipelineGraphicsSettingInfos()
        {
            var graphicsSettingsTypes = TypeCache.GetTypesDerivedFrom(typeof(IRenderPipelineGraphicsSettings));
            foreach (var renderPipelineGraphicsSettingsType in graphicsSettingsTypes)
            {
                if (renderPipelineGraphicsSettingsType.GetCustomAttribute<SerializableAttribute>() == null)
                {
                    Debug.LogWarning($"{nameof(SerializableAttribute)} must be added to {renderPipelineGraphicsSettingsType}, the setting will be skipped");
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
            var isSettingsValid = IsSettingsValid(renderPipelineGraphicsSettingsType, globalSettingsSupportedOn);
            var isSettingsExist = asset.TryGet(renderPipelineGraphicsSettingsType.type, out var type);
            if (isSettingsValid)
            {
                if (!isSettingsExist && TryCreateInstance<IRenderPipelineGraphicsSettings>(renderPipelineGraphicsSettingsType.type, true, out var renderPipelineGraphicsSettings))
                    asset.Add(renderPipelineGraphicsSettings);
            }
            else if (isSettingsExist)
                asset.Remove(type);
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
