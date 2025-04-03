// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
namespace UnityEditor.UIElements.Inspector;

[CustomEditor(typeof(PanelInputConfiguration))]
internal sealed class PanelInputConfigurationEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();
        var settings = serializedObject.FindProperty(PanelInputConfiguration.SettingsProperty);

        var processWorldSpaceInputProperty = settings.FindPropertyRelative(nameof(PanelInputConfiguration.Settings.m_ProcessWorldSpaceInput));
        var processWorldSpaceInput = new PropertyField(processWorldSpaceInputProperty);
        root.Add(processWorldSpaceInput);

        var worldFoldout = new Foldout { text = L10n.Tr("World Space Options") };
        root.Add(worldFoldout);
        worldFoldout.Add(new PropertyField(settings.FindPropertyRelative(nameof(PanelInputConfiguration.Settings.m_InteractionLayers))));
        worldFoldout.Add(new PropertyField(settings.FindPropertyRelative(nameof(PanelInputConfiguration.Settings.m_MaxInteractionDistance))));
        var defaultEventCameraIsMainCameraProperty = settings.FindPropertyRelative(nameof(PanelInputConfiguration.Settings.m_DefaultEventCameraIsMainCamera));
        var defaultEventCameraIsMainCamera = new PropertyField(defaultEventCameraIsMainCameraProperty);
        worldFoldout.Add(defaultEventCameraIsMainCamera);
        var eventCamerasProperty = settings.FindPropertyRelative(nameof(PanelInputConfiguration.Settings.m_EventCameras));
        var eventCameras = new PropertyField(eventCamerasProperty);
        worldFoldout.Add(eventCameras);
        var eventCamerasWarning = new HelpBox(
            L10n.Tr("Some Cameras in the camera list are not listed with the same priority as their depth property. The order in which events will be processed may not match with the camera list."),
            HelpBoxMessageType.Warning);
        worldFoldout.Add(eventCamerasWarning);

        var uguiFoldout = new Foldout { text = L10n.Tr("Event System Interaction") };
        root.Add(uguiFoldout);
        var panelInputRedirectionProperty = settings.FindPropertyRelative(nameof(PanelInputConfiguration.Settings.m_PanelInputRedirection));
        var panelInputRedirection = new PropertyField(panelInputRedirectionProperty);
        uguiFoldout.Add(panelInputRedirection);
        var createPanelComponentsProperty = settings.FindPropertyRelative(nameof(PanelInputConfiguration.Settings.m_AutoCreatePanelComponents));
        var createPanelComponents = new PropertyField(createPanelComponentsProperty);
        uguiFoldout.Add(createPanelComponents);
        var createPanelComponentsWarning = new HelpBox(
            L10n.Tr("Panel components are required to receive Event System events on UI Toolkit panels. To enable events on UI Toolkit panels, ensure that each panel has a PanelRaycaster and PanelEventHandler component associated to it and that each world space event camera has a WorldDocumentRaycaster component associated to it. Enable the Auto Create Panel Components option to automatically create and configure the required components."),
            HelpBoxMessageType.Info);
        uguiFoldout.Add(createPanelComponentsWarning);

        //TODO: Statistics foldout, #panels of each type, #documents with % screen/world

        processWorldSpaceInput.RegisterValueChangeCallback(_ => UpdateEnabledProperties());
        defaultEventCameraIsMainCamera.RegisterValueChangeCallback(_ => UpdateEnabledProperties());
        panelInputRedirection.RegisterValueChangeCallback(_ => UpdateEnabledProperties());
        createPanelComponents.RegisterValueChangeCallback(_ => UpdateEnabledProperties());
        UpdateEnabledProperties();

        eventCameras.TrackPropertyValue(eventCamerasProperty, (_1, _2) => UpdateCameraWarning());
        eventCameras.schedule.Execute(UpdateCameraWarning).Every(200);
        UpdateCameraWarning();

        // For simplicity, we update all the properties together because registering targeted
        // callbacks and property change trackers is a bit tedious and more error-prone.
        void UpdateEnabledProperties()
        {
            worldFoldout.enabledSelf = processWorldSpaceInputProperty.boolValue;
            eventCameras.enabledSelf = !defaultEventCameraIsMainCameraProperty.boolValue;
            var enableCreatePanelComponents = panelInputRedirectionProperty.intValue != (int)PanelInputConfiguration.PanelInputRedirection.Never;
            createPanelComponents.enabledSelf = enableCreatePanelComponents;
            createPanelComponentsWarning.style.display =
                enableCreatePanelComponents && !createPanelComponentsProperty.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void UpdateCameraWarning()
        {
            var outOfOrder = false;
            for (int i = 1; i < eventCamerasProperty.arraySize; i++)
            {
                var cam0 = (Camera)eventCamerasProperty.GetArrayElementAtIndex(i-1).objectReferenceValue;
                var cam1 = (Camera)eventCamerasProperty.GetArrayElementAtIndex(i).objectReferenceValue;
                if (cam0 != null && cam1 != null && cam0.depth < cam1.depth) // we want from closest to furthest
                    outOfOrder = true;
            }
            eventCamerasWarning.style.display = outOfOrder ? DisplayStyle.Flex : DisplayStyle.None;
        }

        return root;
    }
}
