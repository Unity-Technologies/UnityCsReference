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
            if (renderPipelineGlobalSettings == null || !renderPipelineGlobalSettings.TryGet(graphicsSettingsType, out var graphicsSettingsCurrentInstance))
                return;

            if (!TryCreateInstance(graphicsSettingsType, true, out IRenderPipelineGraphicsSettings createdForCSharpDefaultValues))
                return;
            
            if (createdForCSharpDefaultValues is IRenderPipelineResources resource)
                RenderPipelineResourcesEditorUtils.TryReloadContainedNullFields(resource);

            //Retrieve the SerializedProperty on the settings to reset in the global settings for the Notifier
            var serializedGlobalSettings = new SerializedObject(renderPipelineGlobalSettings);
            var settingsIterator = serializedGlobalSettings.FindProperty(serializationPathToCollection);
            settingsIterator.NextVisible(true); //enter the collection
            while (settingsIterator.boxedValue?.GetType() != graphicsSettingsType)
                settingsIterator.NextVisible(false);

            using (var notifier = new Notifier.Scope(settingsIterator))
            {
                //Transfer default C# values (+ null reloading for IRenderPipelineResources)
                CopyBySerialization.Copy(createdForCSharpDefaultValues, settingsIterator);

                if (serializedGlobalSettings.ApplyModifiedProperties())
                    Undo.SetCurrentGroupName($"{undoResetName}{graphicsSettingsType.Name}");
            }

            graphicsSettingsCurrentInstance.Reset();
        }

        // Note: to keep reference when copying, we need to go through the serializedObject and update all field.
        // Using directly ` property.boxedValue = value; ` change the reference. So, any local cache of the settings
        // will be invalid after. Instead we need to shallow copy the content into the reference, as follow.
        // As a IRenderPipelineGraphicsSettings is not a UnityEngine.Object, we need to host it in one to serialize it.
        class CopyBySerialization : ScriptableObject
        {
            [SerializeReference] IRenderPipelineGraphicsSettings content;

            public static void Copy(IRenderPipelineGraphicsSettings source, SerializedProperty destinationProperty)
            {
                if (destinationProperty == null || source == null || destinationProperty.boxedValue.GetType() != source.GetType())
                    return;

                var container = ScriptableObject.CreateInstance<CopyBySerialization>();
                container.content = source;

                // Get a SerializedProperty on the 'content' property
                SerializedObject sp = new(container);
                var iterator = sp.GetIterator();
                iterator.NextVisible(true); //enter CopyBySerialization
                while (iterator.boxedValue?.GetType() != source.GetType())
                    iterator.NextVisible(false);

                ShallowCopyContent(iterator, destinationProperty);

                container.content = null;
                UnityEngine.Object.DestroyImmediate(container);
            }

            static void ShallowCopyContent(SerializedProperty source, SerializedProperty destination)
            {
                void Copy(SerializedProperty source, SerializedProperty destination)
                {
                    if (source.propertyType == SerializedPropertyType.String)
                    {
                        //array are perceive as string by isArray
                        destination.boxedValue = source.boxedValue;
                    }
                    else if (source.isArray)
                    {
                        //Array cannot directly be copied so we need to go one step deeper for them
                        destination.arraySize = source.arraySize;
                        source.NextVisible(true); 
                        destination.NextVisible(true);
                    }

                    destination.boxedValue = source.boxedValue;
                }

                var sourceIterator = source.Copy();
                var destinationIterator = destination.Copy();
                var sourceEnd = sourceIterator.GetEndProperty();

                //step into
                sourceIterator.NextVisible(true);
                destinationIterator.NextVisible(true);

                while (!SerializedProperty.EqualContents(sourceIterator, sourceEnd))
                {
                    Copy(sourceIterator, destinationIterator);
                    sourceIterator.NextVisible(false);
                    destinationIterator.NextVisible(false);
                }
            }
        }
    }
}
