// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using InstanceSettings = Unity.Multiplayer.PlayMode.Editor.LocalPlayerController.InstanceSettings;
using UserSettings = Unity.Multiplayer.PlayMode.Editor.LocalPlayerController.UserSettings;

namespace Unity.Multiplayer.PlayMode.Editor;

[CustomPropertyDrawer(typeof(InstanceItem<LocalPlayerController, InstanceSettings>))]
class LocalPlayerSettingsDrawer : InstanceItemDrawer
{
    const string k_AdvancedSettingsLabel = "Advanced Configuration";

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var instanceItem = (InstanceItem<LocalPlayerController, InstanceSettings>)property.boxedValue;
        var scenario = property.serializedObject.targetObject as OrchestratedScenario;
        var userSettingsProperty = OrchestratedScenarioUserSettings.GetSerializedSettingsProperty<UserSettings>(scenario, instanceItem);
        var container = base.CreatePropertyGUI(property);
        var deviceContainer = new VisualElement();

        var buildProfileProperty = property.FindPropertyRelative($"{IInstanceItem.k_SettingsPropertyPath}.{nameof(InstanceSettings.BuildProfile)}");
        var deviceNameProperty = userSettingsProperty.FindPropertyRelative(nameof(UserSettings.DeviceName));
        var deviceIdProperty = userSettingsProperty.FindPropertyRelative(nameof(UserSettings.DeviceID));

        container.Add(new BuildProfileField(buildProfileProperty));
        container.Add(deviceContainer);
        container.Add(CreateAdvanceSettings(property.FindPropertyRelative(IInstanceItem.k_SettingsPropertyPath)));

        deviceContainer.TrackPropertyValue(
            buildProfileProperty,
            _ => RefreshDeviceField(buildProfileProperty, deviceNameProperty, deviceIdProperty, deviceContainer));

        RefreshDeviceField(buildProfileProperty, deviceNameProperty, deviceIdProperty, deviceContainer);
        return container;
    }

    protected override VisualElement CreateSettingsField(SerializedProperty property) => null;

    static VisualElement CreateAdvanceSettings(SerializedProperty property)
    {
        var container = new Foldout
        {
            text = k_AdvancedSettingsLabel,
            viewDataKey = $"{property.propertyPath}.AdvancedSettings"
        };

        container.Add(new PropertyField(property.FindPropertyRelative(nameof(InstanceSettings.StreamLogsToMainEditor))));
        container.Add(new PropertyField(property.FindPropertyRelative(nameof(InstanceSettings.LogsColor))));
        container.Add(new PropertyField(property.FindPropertyRelative(nameof(InstanceSettings.Arguments))));

        return container;
    }

    static void RefreshDeviceField(
        SerializedProperty buildProfileProperty,
        SerializedProperty deviceNameProperty,
        SerializedProperty deviceIdProperty,
        VisualElement deviceContainer)
    {
        deviceContainer.Clear();
        deviceContainer.Add(CreateDeviceField(buildProfileProperty, deviceNameProperty, deviceIdProperty));
    }

    static VisualElement CreateDeviceField(SerializedProperty buildProfileProperty, SerializedProperty deviceNameProperty, SerializedProperty deviceIdProperty)
    {
        var profile = buildProfileProperty.objectReferenceValue as BuildProfile;
        if (profile == null || !InternalUtilities.IsAndroidBuildTarget(profile) || !InternalUtilities.IsBuildProfileSupported(profile))
            return null;

        return new AdbDeviceField(deviceNameProperty, deviceIdProperty);
    }
}
