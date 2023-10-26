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
        internal const string serializationPathToContainer = "m_Settings";
        internal const string serializationPathToCollection = serializationPathToContainer + ".m_SettingsList.m_List";
        internal const string undoResetName = "Reset IRenderPipelineGraphicsSettings: ";

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

            bool hasSettings = asset.TryGet(renderPipelineGraphicsSettingsType.type, out renderPipelineGraphicsSettings);

            if (renderPipelineGraphicsSettingsType.isDeprecated)
            {
                if (hasSettings)
                    asset.Remove(renderPipelineGraphicsSettings);

                return;
            }

            if (!hasSettings && TryCreateInstance(renderPipelineGraphicsSettingsType.type, true, out renderPipelineGraphicsSettings))
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

        internal static void ResetRenderPipelineGraphicsSettings(Type graphicsSettingsType, Type renderPipelineType)
        {
            if (graphicsSettingsType == null || renderPipelineType == null)
                return;

            var renderPipelineGlobalSettings = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset(renderPipelineType);
            if (!renderPipelineGlobalSettings.TryGet(graphicsSettingsType, out var srpGraphicSetting))
                return;

            if (!TryCreateInstance(graphicsSettingsType, true, out srpGraphicSetting))
                return;

            var serializedGlobalSettings = new SerializedObject(renderPipelineGlobalSettings);
            var settingsIterator = serializedGlobalSettings.FindProperty(serializationPathToCollection);
            settingsIterator.NextVisible(true); //enter the collection
            while (settingsIterator.boxedValue?.GetType() != graphicsSettingsType)
                settingsIterator.NextVisible(false);

            if (srpGraphicSetting is IRenderPipelineResources resource)
                RenderPipelineResourcesEditorUtils.TryReloadContainedNullFields(resource);
            
            using (var notifier = new Notifier.Scope(settingsIterator))
            {
                settingsIterator.boxedValue = srpGraphicSetting;
                if (serializedGlobalSettings.ApplyModifiedProperties())
                    Undo.SetCurrentGroupName($"{undoResetName}{graphicsSettingsType.Name}");
            }
        }
    }
}
