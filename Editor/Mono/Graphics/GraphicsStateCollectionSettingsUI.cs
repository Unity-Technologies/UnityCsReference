// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Rendering
{
    internal enum GraphicsStateCollectionStartupAction
    {
        None = 0,
        BeginTrace = 1,
        Warmup = 2
    }

    // UI for the GraphicsStateCollection-related settings inside Graphics Settings (also used in Build Profiles when overriding Graphics Settings)
    internal static class GraphicsStateCollectionSettingsUI
    {
        public static void BindGraphicsStateCollection(VisualElement root, SerializedObject settingsDataStore)
        {
           // Group visibility based on collection startup action
            var loadedGSCProperty = settingsDataStore.FindProperty("m_GraphicsStateCollection");
            var loadedGSCElement = root.MandatoryQ<ObjectField>("GraphicsStateCollection");
            var warningElement = root.MandatoryQ<HelpBox>("WarmupWithoutCollectionWarning");
            var startupProperty = settingsDataStore.FindProperty("m_CollectionStartupAction");
            var startupEnumElement = root.MandatoryQ<EnumField>("CollectionStartupAction");
            var traceElements = root.MandatoryQ<VisualElement>("TraceCollectionSettings");
            var warmupElements = root.MandatoryQ<VisualElement>("WarmupCollectionSettings");
            UIElementsEditorUtility.SetVisibility(traceElements, (GraphicsStateCollectionStartupAction)startupProperty.enumValueFlag == GraphicsStateCollectionStartupAction.BeginTrace);
            UIElementsEditorUtility.SetVisibility(warmupElements, (GraphicsStateCollectionStartupAction)startupProperty.enumValueFlag == GraphicsStateCollectionStartupAction.Warmup);
            UIElementsEditorUtility.BindSerializedProperty<GraphicsStateCollectionStartupAction>(startupEnumElement, startupProperty, mode => 
            {
                UIElementsEditorUtility.SetVisibility(traceElements, mode == GraphicsStateCollectionStartupAction.BeginTrace);
                UIElementsEditorUtility.SetVisibility(warmupElements, mode == GraphicsStateCollectionStartupAction.Warmup);
                if (!loadedGSCProperty.objectReferenceValue)
                    UIElementsEditorUtility.SetVisibility(warningElement, mode == GraphicsStateCollectionStartupAction.Warmup);
            });

            // Warmup settings
            loadedGSCElement.RegisterValueChangedCallback(evt =>
            {
                loadedGSCProperty.objectReferenceValue = evt.newValue;
                loadedGSCProperty.serializedObject.ApplyModifiedProperties();
                warmupElements.SetEnabled(loadedGSCProperty.objectReferenceValue);
                if ((GraphicsStateCollectionStartupAction)startupProperty.enumValueFlag == GraphicsStateCollectionStartupAction.Warmup)
                    UIElementsEditorUtility.SetVisibility(warningElement, !loadedGSCProperty.objectReferenceValue);
            });

            var warmupAfterFirstSceneToggle = root.MandatoryQ<Toggle>("WarmupAfterFirstSceneToggle");
            var warmupProgressivelyElement = root.MandatoryQ<IntegerField>("WarmupProgressivelyLimit");
            var warmupProgressivelyProperty = settingsDataStore.FindProperty("m_WarmupProgressivelyLimit");
            warmupAfterFirstSceneToggle.RegisterValueChangedCallback(evt =>
            {
                warmupProgressivelyElement.SetEnabled(evt.newValue);
                // Use the m_WarmupProgressivelyLimit property to represent both the limit and the warmupAfterFirstScene toggle value
                var defaultLimit = evt.newValue ? 0 : -1;
                if (warmupProgressivelyProperty.intValue != defaultLimit)
                {
                    warmupProgressivelyProperty.intValue = defaultLimit;
                    warmupProgressivelyProperty.serializedObject.ApplyModifiedProperties();
                }
            });

            var cacheMissTracingToggle = root.MandatoryQ<Toggle>("CacheMissTracingToggle");
            var cacheMissCollectionProperty = settingsDataStore.FindProperty("m_EnableCacheMissTracing");
            var cacheMissCollectionPathElement = root.MandatoryQ<TextField>("CacheMissCollectionPath");
            cacheMissTracingToggle.RegisterValueChangedCallback(evt =>
            {
                cacheMissCollectionProperty.boolValue = evt.newValue;
                cacheMissCollectionProperty.serializedObject.ApplyModifiedProperties();
                cacheMissCollectionPathElement.SetEnabled(cacheMissCollectionProperty.boolValue);
            });
        }
    }
}
