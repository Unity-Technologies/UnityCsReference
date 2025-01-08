// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEditor.Inspector.GraphicsSettingsInspectors;
using UnityEditor.Rendering.Settings;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    public static class RenderPipelineGraphicsSettingsEditorUtility
    {
        internal const string undoRebindName = "Update IRenderPipelineGraphicsSettings: ";

        public static void ForEachFieldOfType<T>(this IRenderPipelineResources resource, Action<T> callback, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        {
            if (resource == null)
                return;

            var fields = resource.GetType().GetFields(flags);
            foreach (var fieldInfo in fields)
                if (fieldInfo.GetValue(resource) is T fieldValue)
                    callback(fieldValue);
        }

        public static void RecreateRenderPipelineMatchingSettings(IRenderPipelineGraphicsSettings settings)
        {
            if (settings == null || RenderPipelineManager.currentPipeline == null)
                return;

            var renderPipelineType = RenderPipelineManager.currentPipeline.GetType();
            var renderPipelineGlobalSettings = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset(renderPipelineType);
            if (!renderPipelineGlobalSettings.TryGet(settings.GetType(), out var srpGraphicSetting))
                return;

            if (Object.ReferenceEquals(srpGraphicSetting, settings))
                RenderPipelineManager.RecreateCurrentPipeline(RenderPipelineManager.currentPipelineAsset);
        }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            GraphicsSettings.OnIRenderPipelineGraphicsSettingsChange -= Callback;
            GraphicsSettings.OnIRenderPipelineGraphicsSettingsChange += Callback;
        }

        static void Callback(IRenderPipelineGraphicsSettings instance, string propertyName)
        {
            if (instance
                .GetType()
                .GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                ?.GetCustomAttribute<RecreatePipelineOnChangeAttribute>() == null)
                return;

            RecreateRenderPipelineMatchingSettings(instance);
        }

        public static void RemoveRenderPipelineGraphicsSettingsWithMissingScript()
        {
            EditorGraphicsSettings.ForEachPipelineSettings((settings) => settings.CleanNullSettings());
        }

        public static void Rebind(IRenderPipelineGraphicsSettings settings, Type renderPipelineType)
        {
            if (settings == null || renderPipelineType == null || !typeof(RenderPipeline).IsAssignableFrom(renderPipelineType))
                return;

            var renderPipelineGlobalSettings = EditorGraphicsSettings.GetRenderPipelineGlobalSettingsAsset(renderPipelineType);
            var serializedGlobalSettings = new SerializedObject(renderPipelineGlobalSettings);
            var settingsIterator = serializedGlobalSettings.FindProperty(RenderPipelineGraphicsSettingsManager.serializationPathToCollection);
            var end = settingsIterator.GetEndProperty();
            settingsIterator.NextVisible(true); //enter the collection
            while (true)
            {
                if (SerializedProperty.EqualContents(settingsIterator, end))
                    return;

                if (settingsIterator.boxedValue?.GetType() == settings.GetType())
                    break;

                settingsIterator.NextVisible(false);
            }
            
            using (var notifier = new Notifier.Scope(settingsIterator))
            {
                settingsIterator.boxedValue = settings;
                if (serializedGlobalSettings.ApplyModifiedProperties())
                    Undo.SetCurrentGroupName($"{undoRebindName}{settings.GetType().Name}");
            }
        }
    }

}
