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
            public bool isSupported;
        }

        internal static void PopulateRenderPipelineGraphicsSettings(RenderPipelineGlobalSettings settings)
        {
            if (settings == null)
                return;

            if (!GraphicsSettingsInspectorUtility.TryExtractSupportedOnRenderPipelineAttribute(settings.GetType(), out var globalSettingsSupportedOn, out var message))
                throw new InvalidOperationException(message);

            var globalSettingsRenderPipelineAssetType = globalSettingsSupportedOn.renderPipelineTypes[0];

            bool assetModified = false;

            List<IRenderPipelineGraphicsSettings> createdSettingsObjects = new();

            foreach (var info in FetchRenderPipelineGraphicsSettingInfos(globalSettingsRenderPipelineAssetType, true))
            {
                UpdateRenderPipelineGlobalSettings(info, settings, out var modified, out var createdSetting);
                assetModified |= modified;
                if (createdSetting != null)
                    createdSettingsObjects.Add(createdSetting);
            }

            foreach (var created in createdSettingsObjects)
            {
                created.Reset();
            }

            if (assetModified)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }
        }

        internal static IEnumerable<RenderPipelineGraphicsSettingsInfo> FetchRenderPipelineGraphicsSettingInfos(Type globalSettingsRenderPipelineAssetType, bool includeUnsupported = false)
        {
            foreach (var renderPipelineGraphicsSettingsType in TypeCache.GetTypesDerivedFrom(typeof(IRenderPipelineGraphicsSettings)))
            {
                if (!IsSettingsValid(renderPipelineGraphicsSettingsType))
                    continue;

                // The Setting has been completely deprecated or not supported on render pipeline anymore
                if (!IsSettingsSupported(renderPipelineGraphicsSettingsType, globalSettingsRenderPipelineAssetType, includeUnsupported, out var isSupported))
                    continue;

                yield return new RenderPipelineGraphicsSettingsInfo()
                {
                    type = renderPipelineGraphicsSettingsType,
                    isSupported = isSupported
                };
            }
        }

        static void UpdateRenderPipelineGlobalSettings(
            RenderPipelineGraphicsSettingsInfo renderPipelineGraphicsSettingsType,
            RenderPipelineGlobalSettings asset,
            out bool assetModified,
            out IRenderPipelineGraphicsSettings createdSetting)
        {
            assetModified = false;
            createdSetting = null;

            var hasSettings = asset.TryGet(renderPipelineGraphicsSettingsType.type, out var renderPipelineGraphicsSettings);
            if (!renderPipelineGraphicsSettingsType.isSupported)
            {
                if (!hasSettings)
                    return;

                asset.Remove(renderPipelineGraphicsSettings);
                assetModified = true;
                return;
            }

            if (!hasSettings && TryCreateInstance(renderPipelineGraphicsSettingsType.type, true, out renderPipelineGraphicsSettings))
            {
                assetModified = true;
                createdSetting = renderPipelineGraphicsSettings;
                asset.Add(renderPipelineGraphicsSettings);
            }

            if (renderPipelineGraphicsSettings is IRenderPipelineResources resource)
            {
                var reloadingStatus = RenderPipelineResourcesEditorUtils.TryReloadContainedNullFields(resource);
                assetModified |= reloadingStatus == RenderPipelineResourcesEditorUtils.ResultStatus.ResourceReloaded;
            }
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

        static bool IsSettingsSupported(Type renderPipelineGraphicsSettingsType, Type globalSettingsRenderPipelineAssetType, bool includeUnsupported, out bool isSupported)
        {
            isSupported = !(renderPipelineGraphicsSettingsType.GetCustomAttribute<ObsoleteAttribute>()?.IsError ?? false);
            isSupported &= SupportedOnRenderPipelineAttribute.IsTypeSupportedOnRenderPipeline(renderPipelineGraphicsSettingsType, globalSettingsRenderPipelineAssetType);
            return includeUnsupported || isSupported;
        }

        static bool IsSettingsValid(Type renderPipelineGraphicsSettingsType)
        {
            if (renderPipelineGraphicsSettingsType.IsAbstract || renderPipelineGraphicsSettingsType.IsGenericType || renderPipelineGraphicsSettingsType.IsInterface)
                return false;

            if (renderPipelineGraphicsSettingsType.GetCustomAttribute<SerializableAttribute>() == null)
            {
                Debug.LogWarning($"{nameof(SerializableAttribute)} must be added to {renderPipelineGraphicsSettingsType}, the setting will be skipped");
                return false;
            }

            if (renderPipelineGraphicsSettingsType.GetCustomAttribute<SupportedOnRenderPipelineAttribute>() == null)
            {
                Debug.LogWarning($"{nameof(SupportedOnRenderPipelineAttribute)} must be added to {renderPipelineGraphicsSettingsType}, the setting will be skipped");
                return false;
            }

            return true;
        }
        internal static void ResetRenderPipelineGraphicsSettings(Type graphicsSettingsType, Type renderPipelineType)
        {
            if (graphicsSettingsType == null || renderPipelineType == null)
                return;

            var renderPipelineGlobalSettings = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset(renderPipelineType);
            if (renderPipelineGlobalSettings == null || !renderPipelineGlobalSettings.TryGet(graphicsSettingsType, out var srpGraphicSetting))
                return;

            if (!TryCreateInstance(graphicsSettingsType, true, out srpGraphicSetting))
                return;

            srpGraphicSetting.Reset();

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
